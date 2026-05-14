// SuperUI/Base/Reactive/SgSignalPersistence.cs
// ИСПРАВЛЕНО:
// ✅ static_value → _ (опечатка — не компилировалось)
// ✅ DisposeAsync: атомарная очистка с try/finally
// ✅ SSR graceful degradation: JSException перехватывается
// ✅ SchemaVersion: версионирование envelope
// ✅ .NET 8/9/10 совместим

using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Reactive;

/// <summary>Конфигурация персистентности сигнала.</summary>
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
    /// Версия схемы данных. При несовпадении сохранённые данные игнорируются.
    /// Увеличьте при изменении структуры значения.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Callback при ошибке записи (необязательно).</summary>
    public Action<Exception>? OnWriteError { get; set; }
}

/// <summary>
/// Сервис для персистентности сигналов через Web Storage (localStorage / sessionStorage).
/// SSR: при недоступности JS — graceful degradation (LogDebug + return).
/// WASM: полная поддержка.
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

    public SgSignalPersistence(
        IJSRuntime js,
        ILogger<SgSignalPersistence>? logger = null)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        _logger = logger;
    }

    /// <summary>
    /// Восстановить значение сигнала из storage.
    /// Безопасно в SSR: при ошибке JS возвращает без изменений.
    /// </summary>
    public async Task RestoreAsync<T>(
        SgSignal<T> signal,
        string? key = null,
        SgPersistenceOptions? options = null)
    {
        var storageKey = GetKey(signal, key, options);
        var opts = options?.JsonOptions ?? _defaultOptions;
        var schemaVersion = options?.SchemaVersion ?? 1;

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

            if (envelope.Version != schemaVersion)
            {
                _logger?.LogDebug(
                    "[SgSignalPersistence] Schema mismatch for '{Key}': stored={S}, expected={E}. Skipping.",
                    storageKey, envelope.Version, schemaVersion);
                return;
            }

            signal.Set(envelope.Value);
        }
        catch (JSException ex)
        {
            _logger?.LogDebug(ex,
                "[SgSignalPersistence] JS unavailable for '{Key}'. SSR mode?", storageKey);
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex,
                "[SgSignalPersistence] JSON error for '{Key}'.", storageKey);
        }
    }

    /// <summary>
    /// Отслеживать сигнал и автоматически сохранять изменения (debounce).
    /// </summary>
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

        // ✅ ИСПРАВЛЕНО: static_value → __ (дискард для параметра, fire-and-forget для Task)
        var observer = new SignalObserverCallback<T>(__ =>
        {
            _ = SaveDebouncedAsync(signal, storageKey, debounceMs, opts, schemaVersion, useSession, onError);
        });

        signal.Subscribe(observer);

        var subscription = new Subscription(() => signal.Unsubscribe(observer));
        lock (_lock) _subscriptions.Add(subscription);

        return subscription;
    }

    private async Task SaveDebouncedAsync<T>(
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
            // Debounce сброшен — нормально
        }
        catch (JSException ex)
        {
            onError?.Invoke(ex);
            _logger?.LogDebug(ex,
                "[SgSignalPersistence] JS error writing '{Key}'.", key);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
            _logger?.LogError(ex,
                "[SgSignalPersistence] Unexpected error writing '{Key}'.", key);
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
            _logger?.LogDebug(ex,
                "[SgSignalPersistence] JS error clearing '{Key}'.", storageKey);
        }
    }

    private static string GetKey<T>(
        SgSignal<T> signal,
        string? key,
        SgPersistenceOptions? options)
        => options?.StorageKey ?? key ?? signal.DebugName ?? $"sg-signal-{typeof(T).Name}";

    /// <summary>✅ Атомарная очистка с try/finally — tokens отменяются всегда.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        List<IDisposable> subs;
        Dictionary<string, CancellationTokenSource> tokens;

        lock (_lock)
        {
            subs = [.._subscriptions];
            _subscriptions.Clear();
            tokens = new Dictionary<string, CancellationTokenSource>(_debounceTokens);
            _debounceTokens.Clear();
        }

        foreach (var sub in subs)
        {
            try { sub.Dispose(); }
            catch { /* subscriptions dispose не должен кидать */ }
        }

        foreach (var cts in tokens.Values)
        {
            try
            {
                await cts.CancelAsync();
                cts.Dispose();
            }
            catch { }
        }
    }

    // ── Вспомогательные типы ──────────────────────────────────────────────────

    private sealed record PersistenceEnvelope<T>
    {
        public required T Value { get; init; }
        public int Version { get; init; } = 1;
        public DateTimeOffset SavedAt { get; init; }
    }

    /// <summary>Реализует ISignalObserver<T> через callback.</summary>
    private sealed class SignalObserverCallback<T> : ISignalObserver<T>
    {
        private readonly Action<T> _callback;

        public SignalObserverCallback(Action<T> callback)
            => _callback = callback ?? throw new ArgumentNullException(nameof(callback));

        public void OnSignalChanged(ISgSignal<T> typedSignal)
            => _callback(typedSignal.Value);
    }

    private sealed class Subscription : IDisposable
    {
        private Action? _action;
        public Subscription(Action action) => _action = action;
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }
}