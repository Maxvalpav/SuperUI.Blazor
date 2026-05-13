// SuperUI/Base/State/SgPersistentState.cs
// ✅ .NET 8+ InteractiveAuto: сохранение состояния между Server и WASM
// ✅ Интегрируется с SgComponentBase
// ✅ Типобезопасный API с поддержкой сложных объектов

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.State;

/// <summary>
/// Абстракция над PersistentComponentState для сохранения состояния
/// при переходе между Server-side prerendering и WASM интерактивностью.
///
/// Использование:
/// <code>
/// public class MyPage : SgComponentBase
/// {
///     [Inject] PersistentComponentState PersistentState { get; set; }
///     private SgPersistentState _state = null!;
///
///     protected override void OnInitialized()
///     {
///         _state = new SgPersistentState(PersistentState, Logger);
///     }
///
///     protected override async Task OnFirstRenderAsync()
///     {
///         if (!_state.TryTake("weather", out WeatherData[]? data))
///         {
///             data = await WeatherService.GetAsync();
///             _state.Persist("weather", data);
///         }
///     }
/// }
/// </code>
/// </summary>
public sealed class SgPersistentState
{
    private readonly PersistentComponentState? _state;
    private readonly ILogger? _logger;
    private readonly Dictionary<string, object> _pendingPersist = new();

    public SgPersistentState(PersistentComponentState? state, ILogger? logger = null)
    {
        _state = state;
        _logger = logger;
    }

    /// <summary>
    /// Попытаться восстановить состояние из PersistentComponentState.
    /// Возвращает true если состояние было сохранено и восстановлено.
    /// </summary>
    public bool TryTake<T>(string key, out T? value)
    {
        if (_state is not null && _state.TryTakeFromJson<T>(key, out var result))
        {
            value = result;
            _logger?.LogDebug("[SgPersistentState] Restored '{Key}': {Type}", key, typeof(T).Name);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Сохранить состояние для последующего восстановления в WASM.
    /// </summary>
    public void Persist<T>(string key, T value)
    {
        if (_state is null) return;

        _state.PersistAsJson(key, value);
        _logger?.LogDebug("[SgPersistentState] Persisted '{Key}': {Type}", key, typeof(T).Name);
    }

    /// <summary>
    /// Отложенное сохранение — будет выполнено при вызове Flush().
    /// Полезно когда нужно сохранить несколько значений атомарно.
    /// </summary>
    public void DeferPersist<T>(string key, T value)
    {
        _pendingPersist[key] = value!;
    }

    /// <summary>
    /// Применить все отложенные сохранения.
    /// </summary>
    public void Flush()
    {
        if (_state is null) return;

        foreach (var (key, value) in _pendingPersist)
        {
            _state.PersistAsJson(key, value);
            _logger?.LogDebug("[SgPersistentState] Flushed '{Key}'", key);
        }
        _pendingPersist.Clear();
    }

    /// <summary>
    /// Зарегистрировать callback, который выполнится перед сохранением состояния.
    /// </summary>
    public void OnPersisting(Action callback)
    {
        _state?.RegisterOnPersisting(() => { callback(); return Task.CompletedTask; });
    }

    /// <summary>
    /// Зарегистрировать async callback перед сохранением состояния.
    /// </summary>
    public void OnPersisting(Func<Task> callback)
    {
        _state?.RegisterOnPersisting(() => callback());
    }
}
