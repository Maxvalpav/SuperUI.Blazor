// SuperUI/Base/SgAsyncButton.cs
// ✅ UX-1 NEW: паттерн кнопки с loading state, предотвращение двойного клика
// ✅ Поддержка CancellationToken для отмены операции
// ✅ Доступность: aria-busy, aria-label при загрузке

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для кнопок с асинхронными операциями.
/// Автоматически показывает loading состояние и предотвращает двойной клик.
/// </summary>
public abstract class SgAsyncButtonBase : SgInteractiveBase
{
    /// <summary>Простой async callback без CancellationToken.</summary>
    [Parameter] public EventCallback OnClickAsync { get; set; }

    /// <summary>Async callback с поддержкой отмены.</summary>
    [Parameter] public Func<CancellationToken, Task>? OnClickCancelable { get; set; }

    /// <summary>Текст/aria-label во время выполнения операции.</summary>
    [Parameter] public string? LoadingText { get; set; }

    /// <summary>Блокировать повторный клик пока операция выполняется.</summary>
    [Parameter] public bool PreventDoubleClick { get; set; } = true;

    /// <summary>Автоматически отменить операцию через N мс (null = без таймаута).</summary>
    [Parameter] public int? AutoCancelAfterMs { get; set; }

    /// <summary>Операция выполняется прямо сейчас.</summary>
    protected bool IsExecuting { get; private set; }

    private CancellationTokenSource? _executionCts;

    // BuildAriaAttributes возвращает IReadOnlyDictionary<string, object> — как в базовом классе
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        if (!IsExecuting) return base_;

        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal)
        {
            ["aria-busy"] = "true"
        };
        if (LoadingText is not null)
            attrs["aria-label"] = LoadingText;

        return attrs;
    }

    // Скрываем базовый HandleClickAsync — добавляем логику loading/cancel
    protected new async Task HandleClickAsync(MouseEventArgs e)
    {
        if (IsStaticSSR || IsEffectivelyDisabled || IsDisposed) return;
        if (PreventDoubleClick && IsExecuting) return;

        IsExecuting = true;

        _executionCts?.Cancel();
        _executionCts?.Dispose();
        _executionCts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);

        if (AutoCancelAfterMs.HasValue)
            _executionCts.CancelAfter(AutoCancelAfterMs.Value);

        try
        {
            await InvokeAsync(StateHasChanged);

            if (OnClickCancelable is not null)
                await OnClickCancelable(_executionCts.Token);
            else
                await OnClickAsync.InvokeAsync();
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug("[{Id}] Async button operation cancelled", ComponentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Id}] Async button operation failed", ComponentId);
        }
        finally
        {
            IsExecuting = false;
            _executionCts?.Dispose();
            _executionCts = null;

            if (!IsDisposed)
                await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Отменить текущую выполняемую операцию.</summary>
    public void CancelOperation() => _executionCts?.Cancel();

    protected override async ValueTask DisposeComponentAsync()
    {
        _executionCts?.Cancel();
        _executionCts?.Dispose();
        await base.DisposeComponentAsync();
    }
}
