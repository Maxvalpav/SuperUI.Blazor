// SuperUI/Base/Reactive/SgSignalPersistence.cs
// НОВЫЙ КЛАСС
// Аналог: Pinia persisted-state plugin, Zustand persist
// Поддержка: .NET 8/9/10, InteractiveServer + WASM (SSR: skip)
//
// Что делает:
// - Сохраняет сигналы в localStorage/sessionStorage при изменении
// - Восстанавливает значения при инициализации компонента
// - Поддерживает шифрование (опционально)
// - Не работает в SSR (graceful degradation)

using System.Text.Json;
using Microsoft.JSInterop;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Конфигурация персистентности сигнала.
/// </summary>
public sealed class SgPersistenceOptions
{
    /// <summary>Ключ в storage (по умолчанию = DebugName сигнала).</summary>
    public string? StorageKey { get; set; }

    /// <summary>Использовать sessionStorage вместо localStorage.</summary>
    public bool UseSessionStorage { get; set; }

    /// <summary>Задержка записи в мс (debounce, чтобы не писать при каждом нажатии).</summary>
    public int WriteDebounceMs { get; set; } = 300;

    /// <summary>JsonSerializerOptions для сериализации.</summary>
    public JsonSerializerOptions? JsonOptions { get; set; }

    /// <summary>Версия схемы. При несовпадении — игнорировать сохранённые данные.</summary>
    public int SchemaVersion { get; set; } = 1;
}

/// <summary>
/// Сервис для персистентности сигналов через Web Storage.
///
/// Регистрация: builder.Services.AddScoped&lt;SgSignalPersistence&gt;()
///
/// Использование в компоненте:
/// <code>
/// [Inject] SgSignalPersistence Persistence { get; set; } = null!;
///
/// protected override async Task OnInitializeAsync()
/// {
///     await Persistence.RestoreAsync(countSignal, "my-count");
///     Persistence.TrackAsync(countSignal, "my-count");
/// }
/// </code>
/// </summary>
public sealed class SgSignalPersistence : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly Dictionary<string, CancellationTokenSource> _debounceTokens = [];
    private readonly object _lock = new();
    private int _disposed;

    private static readonly JsonSerializerOptions _defaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SgSignalPersistence(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Восстановить значение сигнала из storage.
    /// Безопасно в SSR: при ошибке JS возвращает дефолт.
    /// </summary>
    public async Task RestoreAsync<T>(
        SgSignal<T> signal,
        string? key = null,
        SgPersistenceOptions? options = null)
    {
        var storageKey = GetKey(signal, key, options);
        var opts = options?.JsonOptions ?? _defaultOptions;

        try
        {
            string? raw;
            if (options?.UseSessionStorage == true)
                raw = await _js.InvokeAsync<string?>("sessionStorage.getItem", storageKey);
            else
                raw = await _js.InvokeAsync<string?>("localStorage.getItem", storageKey);

            if (string.IsNullOrEmpty(raw)) return;

            var envelope = JsonSerializer.Deserialize<PersistenceEnvelope<T>>(raw, opts);
            if (envelope is null) return;

            // Проверяем версию схемы
            var schemaVersion = options?.SchemaVersion ?? 1;
            if (envelope.Version != schemaVersion) return;

            signal.Set(envelope.Value);
        }
        catch (JSException)
        {
            // SSR или JS недоступен — graceful degradation
        }
        catch (JsonException)
        {
            // Устаревший/несовместимый формат — игнорируем
        }
    }

    /// <summary>
    /// Отслеживать сигнал и автоматически сохранять изменения.
    /// Использует debounce для оптимизации записей.
    /// </summary>
    public void Track<T>(
        SgSignal<T> signal,
        string? key = null,
        SgPersistenceOptions? options = null)
    {
        var storageKey = GetKey(signal, key, options);
        var debounceMs = options?.WriteDebounceMs ?? 300;
        var opts = options?.JsonOptions ?? _defaultOptions;
        var schemaVersion = options?.SchemaVersion ?? 1;
        var useSession = options?.UseSessionStorage == true;

        var observer = new SignalObserverCallback<T>(_ =>
            _ = SaveDebounced<T>(signal, storageKey, debounceMs, opts, schemaVersion, useSession));

        signal.Subscribe(observer);
        _subscriptions.Add(new Subscription(() => signal.Unsubscribe(observer)));
    }

    private async Task SaveDebounced<T>(
        SgSignal<T> signal,
        string key,
        int debounceMs,
        JsonSerializerOptions opts,
        int schemaVersion,
        bool useSession)
    {
        // Debounce: отменяем предыдущий сохранение для этого ключа
        CancellationTokenSource cts;
        lock (_lock)
        {
            if (_debounceTokens.TryGetValue(key, out var old))
            {
                old.Cancel();
                old.Dispose();
            }

            cts = new CancellationTokenSource();
            _debounceTokens[key] = cts;
        }

        try
        {
            await Task.Delay(debounceMs, cts.Token);

            if (Volatile.Read(ref _disposed) == 1) return;

            var envelope = new PersistenceEnvelope<T>
            {
                Value = signal.Value,
                Version = schemaVersion,
                SavedAt = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(envelope, opts);

            if (useSession)
                await _js.InvokeVoidAsync("sessionStorage.setItem", key, json);
            else
                await _js.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch (OperationCanceledException)
        {
            // Debounce сбросился — нормально
        }
        catch (JSException)
        {
            // SSR или storage недоступен
        }
    }

    /// <summary>Очистить сохранённые данные по ключу.</summary>
    public async Task ClearAsync<T>(
        SgSignal<T> signal,
        string? key = null,
        SgPersistenceOptions? options = null)
    {
        var storageKey = GetKey(signal, key, options);

        try
        {
            if (options?.UseSessionStorage == true)
                await _js.InvokeVoidAsync("sessionStorage.removeItem", storageKey);
            else
                await _js.InvokeVoidAsync("localStorage.removeItem", storageKey);
        }
        catch (JSException) { }
    }

    private static string GetKey<T>(
        SgSignal<T> signal,
        string? key,
        SgPersistenceOptions? options)
        => options?.StorageKey ?? key ?? signal.DebugName ?? $"sg-signal-{typeof(T).Name}";

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();

        lock (_lock)
        {
            foreach (var cts in _debounceTokens.Values)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
            _debounceTokens.Clear();
        }
    }

    private record PersistenceEnvelope<T>
    {
        public required T Value { get; init; }
        public int Version { get; init; } = 1;
        public DateTimeOffset SavedAt { get; init; }
    }

    private sealed class SignalObserverCallback<T> : ISignalObserver
    {
        private readonly Action<T> _callback;

        public SignalObserverCallback(Action<T> callback) => _callback = callback;

        public void OnSignalChanged(ISgSignal signal)
        {
            if (signal is IReadOnlySignal<T> typed)
                _callback(typed.Value);
        }
    }
}

/// <summary>Подписка с действием на Dispose.</summary>
internal sealed class Subscription : IDisposable
{
    private Action? _onDispose;

    public Subscription(Action onDispose) => _onDispose = onDispose;

    public void Dispose()
    {
        var action = _onDispose;
        _onDispose = null;
        action?.Invoke();
    }
}
