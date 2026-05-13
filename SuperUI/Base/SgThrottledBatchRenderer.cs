// SuperUI/Base/SgThrottledBatchRenderer.cs 
// Улучшения: 
// - TimeProvider (.NET 8) вместо DateTime.UtcNow + Timer 
// - PeriodicTimer вместо System.Threading.Timer 
// - Один глобальный таймер на всех подписчиков (батч рендер) 
// - Корректная отмена через CancellationToken 
// - Не вызывает StateHasChanged если компонент disposed 
 
using System; 
using System.Collections.Concurrent; 
using System.Collections.Generic; 
using System.Threading; 
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Components; 
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Планировщик batch рендеринга. 
/// Группирует несколько StateHasChanged в один вызов per tick. 
/// Использует TimeProvider для тестируемости. 
/// </summary> 
public sealed class SgThrottledBatchRenderer : IAsyncDisposable 
{ 
    private readonly TimeProvider _timeProvider; 
    private readonly TimeSpan _interval; 
    private readonly ConcurrentDictionary<ComponentBase, byte> _pendingComponents = new(); 
    private readonly CancellationTokenSource _cts = new(); 
    private Task? _runLoop; 
 
    /// <summary> 
    /// Создаёт планировщик. 
    /// </summary> 
    /// <param name="timeProvider">TimeProvider (инжектируется через DI).</param> 
    /// <param name="intervalMs">Интервал батч-рендера в мс (по умолчанию 16мс ≈ 60fps).</param> 
    public SgThrottledBatchRenderer(TimeProvider timeProvider, int intervalMs = 16) 
    { 
        _timeProvider = timeProvider; 
        _interval = TimeSpan.FromMilliseconds(intervalMs); 
        _runLoop = RunAsync(_cts.Token); 
    } 
 
    /// <summary> 
    /// Регистрирует компонент для рендера в следующем тике. 
    /// Если компонент уже зарегистрирован — дублирования нет. 
    /// </summary> 
    public void RequestRender(ComponentBase component) 
    { 
        _pendingComponents.TryAdd(component, 0); 
    } 
 
    /// <summary> 
    /// Отменяет запрос рендера для компонента (например, при dispose). 
    /// </summary> 
    public void CancelRender(ComponentBase component) 
    { 
        _pendingComponents.TryRemove(component, out _); 
    } 
 
    private async Task RunAsync(CancellationToken ct) 
    { 
        // PeriodicTimer (.NET 6+) — более эффективен чем System.Threading.Timer 
        using var timer = new PeriodicTimer(_interval, _timeProvider); 
 
        try 
        { 
            while (await timer.WaitForNextTickAsync(ct)) 
            { 
                if (_pendingComponents.IsEmpty) continue; 
 
                // Снимаем снапшот и очищаем очередь 
                var snapshot = _pendingComponents.Keys; 
                foreach (var component in snapshot) 
                { 
                    _pendingComponents.TryRemove(component, out _); 
                    try 
                    { 
                        // InvokeAsync гарантирует вызов на правильном потоке 
                        await ((ComponentBase)component).InvokeAsync( 
                            ((ComponentBase)component).StateHasChanged); 
                    } 
                    catch (ObjectDisposedException) { /* компонент уничтожен — норма */ } 
                    catch (Exception) { /* изолируем ошибки */ } 
                } 
            } 
        } 
        catch (OperationCanceledException) { /* ожидаемо при dispose */ } 
    } 
 
    public async ValueTask DisposeAsync() 
    { 
        _cts.Cancel(); 
        if (_runLoop != null) 
        { 
            try { await _runLoop; } 
            catch (OperationCanceledException) { } 
        } 
        _cts.Dispose(); 
    } 
}