// SuperUI/Base/State/SgCircuitStateManager.cs
// ✅ NEW: Автоматическое сохранение/восстановление состояния
//         при переходе Server→WASM в InteractiveAuto режиме.
// ✅ Обёртка над PersistentComponentState с типизированным API.
// ✅ NET8+: PersistentComponentState.RegisterOnPersisting

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.State;

/// <summary>
/// Управляет сохранением и восстановлением состояния компонента
/// при переходе между Server и WebAssembly (InteractiveAuto).
/// </summary>
public sealed class SgCircuitStateManager
{
    private readonly PersistentComponentState      _persistentState;
    private readonly ILogger<SgCircuitStateManager> _logger;
    private readonly string                         _componentId;
    private readonly Dictionary<string, object?>    _pendingPersist = new();
    private          PersistingComponentStateSubscription? _subscription;
    private volatile bool _disposed;

    public SgCircuitStateManager(PersistentComponentState      persistentState,
                                ILogger<SgCircuitStateManager> logger,
                                string                         componentId)
    {
        _persistentState = persistentState;
        _logger          = logger;
        _componentId     = componentId;

        // Регистрируем callback для сохранения при переходе
        _subscription = _persistentState.RegisterOnPersisting(PersistAsync);
    }

    /// <summary>
    /// Зарегистрировать значение для автоматического сохранения.
    /// Вызывать в OnInitializedAsync или OnParametersSetAsync.
    /// </summary>
    public void Register<T>(string key, Func<T> getter)
    {
        var fullKey = BuildKey(key);
        _pendingPersist[fullKey] = new PersistEntry<T>(getter);
    }

    /// <summary>
    /// Попытаться восстановить значение (вызвать до Register для восстановления).
    /// </summary>
    public bool TryRestore<T>(string key, out T? value)
    {
        var fullKey = BuildKey(key);
        if (_persistentState.TryTakeFromJson<T>(fullKey, out var result))
        {
            value = result;
            _logger.LogDebug("[{Id}] Restored state for key '{Key}'", _componentId, key);
            return true;
        }
        value = default;
        return false;
    }

    private Task PersistAsync()
    {
        foreach (var (key, entry) in _pendingPersist)
        {
            try
            {
                if (entry is IPersistEntry pe)
                    pe.Persist(_persistentState, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Id}] Failed to persist state for key '{Key}'", _componentId, key);
            }
        }
        return Task.CompletedTask;
    }

    private string BuildKey(string key) => $"{_componentId}:{key}";

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subscription?.Dispose();
        _subscription = null;
        _pendingPersist.Clear();
    }

    // ── Вложенные типы ─────────────────────────────────────────────────────

    private interface IPersistEntry
    {
        void Persist(PersistentComponentState state, string key);
    }

    private sealed class PersistEntry<T> : IPersistEntry
    {
        private readonly Func<T> _getter;

        public PersistEntry(Func<T> getter) => _getter = getter;

        public void Persist(PersistentComponentState state, string key)
            => state.PersistAsJson(key, _getter());
    }
}
