// SuperUI/Base/SgAsyncButton.cs 
// Улучшения: 
// - CancellationToken передаётся в обработчик 
// - Отмена предыдущей операции при новом клике (опционально) 
// - Защита от параллельных кликов 
// - IsInteractive проверка (не кликабельна в SSR) 
// - Таймаут операции через TimeProvider 
 
using System; 
using System.Threading; 
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Components.Web; 
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Базовый класс для кнопок с асинхронными операциями. 
/// Автоматически управляет состоянием загрузки, отменой и ошибками. 
/// </summary> 
public abstract class SgAsyncButton : SgComponentBase 
{ 
    private CancellationTokenSource? _currentCts; 
    private bool _isExecuting; 
    private Exception? _lastError; 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Параметры 
    // ────────────────────────────────────────────────────────────────────── 
 
    /// <summary>Обработчик клика с CancellationToken.</summary> 
    [Parameter] public Func<CancellationToken, Task>? OnClickAsync { get; set; } 
 
    /// <summary>Кнопка заблокирована.</summary> 
    [Parameter] public bool Disabled { get; set; } 
 
    /// <summary>Отменять предыдущую операцию при новом клике.</summary> 
    [Parameter] public bool CancelPreviousOnClick { get; set; } = false; 
 
    /// <summary>Таймаут операции (null = бесконечно).</summary> 
    [Parameter] public TimeSpan? OperationTimeout { get; set; } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Состояние 
    // ────────────────────────────────────────────────────────────────────── 
 
    /// <summary>Операция выполняется.</summary> 
    public bool IsExecuting => _isExecuting; 
 
    /// <summary>Последняя ошибка операции.</summary> 
    public Exception? LastError => _lastError; 
 
    /// <summary>Кнопка неактивна (disabled или выполняется).</summary> 
    protected bool IsEffectivelyDisabled => Disabled || (_isExecuting && !CancelPreviousOnClick) || !IsInteractive; 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Обработка клика 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected async Task HandleClickAsync(MouseEventArgs? args = null) 
    { 
        if (Disabled || !IsInteractive) return; 
 
        // Отмена предыдущей операции 
        if (_isExecuting) 
        { 
            if (!CancelPreviousOnClick) return; 
            _currentCts?.Cancel(); 
        } 
 
        _lastError = null; 
        _isExecuting = true; 
 
        // Создаём новый CTS с опциональным таймаутом 
        _currentCts?.Dispose(); 
        _currentCts = OperationTimeout.HasValue 
            ? new CancellationTokenSource(OperationTimeout.Value) 
            : new CancellationTokenSource(); 
 
        var ct = _currentCts.Token; 
 
        try 
        { 
            await NotifyStateChangedAsync(); 
            if (OnClickAsync != null) 
                await OnClickAsync(ct); 
        } 
        catch (OperationCanceledException) when (ct.IsCancellationRequested) 
        { 
            // Ожидаемая отмена — не логируем как ошибку 
        } 
        catch (Exception ex) 
        { 
            _lastError = ex; 
            Logger.LogError(ex, "SgAsyncButton operation failed"); 
            await OnErrorAsync(ex); 
        } 
        finally 
        { 
            _isExecuting = false; 
            await NotifyStateChangedAsync(); 
        } 
    } 
 
    /// <summary>Отменяет текущую операцию.</summary> 
    public void Cancel() => _currentCts?.Cancel(); 
 
    /// <summary>Вызывается при ошибке. Переопределите для кастомной обработки.</summary> 
    protected virtual Task OnErrorAsync(Exception ex) => Task.CompletedTask; 
 
    protected override async ValueTask DisposeAsyncCore() 
    { 
        _currentCts?.Cancel(); 
        _currentCts?.Dispose(); 
        await base.DisposeAsyncCore(); 
    } 
}