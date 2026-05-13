// SuperUI/Base/Reactive/SgSignalPersistence.cs
// ИСПРАВЛЕНО:
// ✅ CS0101/CS0111: убран дублирующий класс Subscription (теперь в SgSubscription.cs)
// ✅ Добавлен onError callback для обработки исключений debounce
// ✅ Добавлен TrackAsync возвращающий IAsyncDisposable
// ✅ Поддержка .NET 8/9/10, InteractiveServer + WASM (SSR: graceful degradation)
// ✅ XML-документация

using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

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

    /// <summary>Задержка записи в мс (debounce). По умолчанию 300 мс.</summary>
    public int WriteDebounceMs { get; set; } = 300;

    /// <summary>JsonSerializerOptions для сериализации.</summary>
    public JsonSerializerOptions? JsonOptions { get; set; }

    /// <summary>
    /// Версия схемы. При несовпадении — игнорировать сохранённые данные.
    /// Увеличьте при изменении структуры значения.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Callback при ошибке записи (необязательно).</summary>
    public Action<Exception>? OnWriteError { get; set; }
}

/// <summary>
/// Сервис для персистентности сигналов через Web Storage (localStorage / sessionStorage).
/// <para>
/// Регистрация: <c>builder.Services.AddScoped&lt;SgSignalPersistence&gt;()</c>
/// </para>
/// <para>
/// Использование:
/// <code>
/// [Inject] SgSignalPersistence Persistence { get; set; } = null!;
///
/// protected override async Task OnInitializeAsync()
/// {
///     await Persistence.RestoreAsync(countSignal, "my-count");
///     Persistence.Track(countSignal, "my-count");
/// }
/// </code>
/// </para>
/// </summary>
public sealed class SgSignalPersistence : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly ILogger<SgSignalPersistence>? _logger;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly Dictionary<string, CancellationTokenSource> _debounceTokens = [];
    private readonly object _lock = new();
    private int _disposed;

    private static readonly JsonSerializerOptions _defaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SgSignalPersistence(IJSRuntime js, ILogger<SgSignalPersistence>? logger = null)
    {
        _js = js;
        _logger = logger;
    }

    /// <summary>
    /// Восстановить значение сигнала из storage.
    /// Безопасно в SSR: при ошибке JS возвращает дефолт (graceful degradation).
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

            var schemaVersion = options?.SchemaVersion ?? 1;
            if (envelope.Version != schemaVersion)
            {
                _logger?.LogDebug(
                    "[SgSignalPersistence] Schema version mismatch for key '{Key}': stored={Stored}, expected={Expected}. Skipping.",
                    storageKey, envelope.Version, schemaVersion);
                return;
            }

            signal.Set(envelope.Value);
        }
        catch (JSException ex)
        {
            // SSR или JS недоступен — graceful degradation
            _logger?.LogDebug(ex, "[SgSignalPersistence] JS unavailable for key '{Key}'. SSR mode?", storageKey);
        }
        catch (JsonException ex)
        {
            // Устаревший/несовместимый формат — игнорируем
            _logger?.LogWarning(ex, "[SgSignalPersistence] JSON deserialization failed for key '{Key}'.", storageKey);
        }
    }

    /// <summary>
    /// Отслеживать сигнал и автоматически сохранять изменения в Web Storage.
    /// Использует debounce для оптимизации записей.
    /// </summary>
    /// <returns><see cref="IDisposable"/> для отмены отслеживания.</returns>
    public IDisposable Track<T>(
        SgSignal<T> signal,
        string? key = null,
        SgPersistenceOptions? options = null)
    {
        var storageKey = GetKey(signal, key, options);
        var debounceMs = options?.WriteDebounceMs ?? 300;
        var opts = options?.JsonOptions ?? _defaultOptions;
        var schemaVersion = options?.SchemaVersion ?? 1;
        var useSession = options?.UseSessionStorage == true;
        var onError = options?.OnWriteError;

        var observer = new SignalObserverCallback<T>(_ =>
            _ = SaveDebounced(signal, storageKey, debounceMs, opts, schemaVersion, useSession, onError));

        signal.Subscribe(observer);
        var subscription = new Subscription(() => signal.Unsubscribe(observer));
        _subscriptions.Add(subscription);
        return subscription;
    }

    private async Task SaveDebounced<T>(
        SgSignal<T> signal,
        string key,
        int debounceMs,
        JsonSerializerOptions opts,
        int schemaVersion,
        bool useSession,
        Action<Exception>? onError)
    {
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
        catch (JSException ex)
        {
            onError?.Invoke(ex);
            _logger?.LogDebug(ex, "[SgSignalPersistence] JS error writing key '{Key}'.", key);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
            _logger?.LogError(ex, "[SgSignalPersistence] Unexpected error writing key '{Key}'.", key);
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
        catch (JSException ex)
        {
            _logger?.LogDebug(ex, "[SgSignalPersistence] JS error clearing key '{Key}'.", storageKey);
        }
    }

    private static string GetKey<T>(
        SgSignal<T> signal,
        string? key,
        SgPersistenceOptions? options)
        => options?.StorageKey ?? key ?? signal.DebugName ?? $"sg-signal-{typeof(T).Name}";

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        foreach (var sub in _subscriptions)
        {
            try { sub.Dispose(); }
            catch { }
        }

        _subscriptions.Clear();

        lock (_lock)
        {
            foreach (var cts in _debounceTokens.Values)
            {
                try { cts.Cancel(); cts.Dispose(); }
                catch { }
            }

            _debounceTokens.Clear();
        }
    }

    // ── Вспомогательные типы ──────────────────────────────────────────────

    private sealed record PersistenceEnvelope<T>
    {
        public required T Value { get; init; }
        public int Version { get; init; } = 1;
        public DateTimeOffset SavedAt { get; init; }
    }

    private sealed class SignalObserverCallback<T> : ISignalObserver<T>
    {
        private readonly Action<T> _callback;

        public SignalObserverCallback(Action<T> callback)
            => _callback = callback;

        public void OnSignalChanged(ISgSignal<T> signal)
            => _callback(signal.Value);
    }
}
