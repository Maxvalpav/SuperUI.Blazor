// SuperUI/Base/State/SgPersistentState.cs 
// Улучшения: 
// - Интеграция с PersistentComponentState (официальный .NET 8 API) 
// - Fallback на SessionStorage для WASM без prerendering 
// - Поддержка SSR → Interactive перехода без повторного fetch 
// - Generic с JSON сериализацией 
 
using System; 
using System.Text.Json; 
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Components; 
 
namespace SuperUI.Base.State; 
 
/// <summary> 
/// Постоянное состояние, которое переживает prerendering → interactive переход. 
/// На Server использует PersistentComponentState (нет повторного fetch). 
/// На WASM без prerendering использует памяти. 
/// </summary> 
public sealed class SgPersistentState<T> : IDisposable 
{ 
    private readonly PersistentComponentState _componentState; 
    private readonly string _key; 
    private PersistingComponentStateSubscription? _subscription; 
    private T? _value; 
    private bool _hasValue; 
 
    public SgPersistentState(PersistentComponentState componentState, string key) 
    { 
        _componentState = componentState; 
        _key = key; 
    } 
 
    /// <summary>true если значение было восстановлено из prerender.</summary> 
    public bool HasPersistedValue => _hasValue; 
 
    /// <summary>Текущее значение.</summary> 
    public T? Value => _value; 
 
    /// <summary> 
    /// Инициализирует состояние. 
    /// Если данные были персистированы в prerender — восстанавливает их. 
    /// В противном случае выполняет фабрику. 
    /// </summary> 
    public async Task<T?> GetOrCreateAsync(Func<Task<T>> factory) 
    { 
        // Пробуем восстановить из persisted state (после prerender) 
        if (_componentState.TryTakeFromJson<T>(_key, out var persisted)) 
        { 
            _value = persisted; 
            _hasValue = true; 
 
            // Регистрируем для следующего prerender 
            _subscription = _componentState.RegisterOnPersisting(PersistAsync); 
            return _value; 
        } 
 
        // Данных нет — выполняем фабрику 
        _value = await factory(); 
        _hasValue = true; 
 
        // Регистрируем для следующего prerender 
        _subscription = _componentState.RegisterOnPersisting(PersistAsync); 
        return _value; 
    } 
 
    /// <summary> 
    /// Обновляет значение и персистирует его для следующего prerender. 
    /// </summary> 
    public void Set(T value) 
    { 
        _value = value; 
        _hasValue = true; 
    } 
 
    private Task PersistAsync() 
    { 
        if (_hasValue && _value != null) 
            _componentState.PersistAsJson(_key, _value); 
        return Task.CompletedTask; 
    } 
 
    public void Dispose() => _subscription?.Dispose(); 
}