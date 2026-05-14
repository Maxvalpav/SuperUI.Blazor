// SuperUI/Base/SgAsyncButton.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ W9: Volatile.Write для _lastClickTick
// ✅ ShouldRender: базовая оптимизация для кнопки
// ✅ ResetAfterDelayAsync: идемпотентен через LinkedCTS

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Base;

public enum ButtonType { Button, Submit, Reset }

public class SgAsyncButton : SgComponentBase
{
    [Parameter] public string? Text { get; set; } = "Submit";
    [Parameter] public string? LoadingText { get; set; } = "Loading...";
    [Parameter] public string? SuccessText { get; set; }
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public ButtonType Type { get; set; } = ButtonType.Button;
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public Func<Task>? OnClickAsync { get; set; }
    [Parameter] public int DebounceMs { get; set; } = 0;
    [Parameter] public int SuccessDisplayMs { get; set; } = 2000;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private AsyncButtonState _state = AsyncButtonState.Idle;
    private readonly object _stateLock = new();
    private long _lastClickTick;
    private CancellationTokenSource? _feedbackCts;

    protected bool IsLoading => _state == AsyncButtonState.Loading;
    protected bool IsSuccess => _state == AsyncButtonState.Success;
    protected bool IsError => _state == AsyncButtonState.Error;

    protected string CurrentText => _state switch
    {
        AsyncButtonState.Loading => LoadingText ?? Text ?? "Loading...",
        AsyncButtonState.Success => SuccessText ?? Text ?? "Done!",
        AsyncButtonState.Error   => ErrorText ?? Text ?? "Error",
        _                        => Text ?? "Submit"
    };

    // ✅ FIX: ShouldRender оптимизирован — рендеримся только при смене состояния
    private AsyncButtonState _lastRenderedState = AsyncButtonState.Idle;
    private bool _lastRenderedDisabled;

    protected override bool ShouldRender()
    {
        var currentState = _state;
        var currentDisabled = Disabled;
        if (currentState == _lastRenderedState && currentDisabled == _lastRenderedDisabled)
            return false;
        _lastRenderedState = currentState;
        _lastRenderedDisabled = currentDisabled;
        return true;
    }

    protected async Task HandleClickAsync(MouseEventArgs e)
    {
        if (IsDisposed || Disabled || IsLoading) return;

        if (DebounceMs > 0)
        {
            var nowTick = TimeProvider.GetTimestamp();
            var lastTick = Volatile.Read(ref _lastClickTick);
            var elapsedMs = (nowTick - lastTick) * 1000.0 / TimeProvider.TimestampFrequency;
            if (elapsedMs < DebounceMs) return;
        }

        // ✅ FIX W9: Volatile.Write для _lastClickTick
        Volatile.Write(ref _lastClickTick, TimeProvider.GetTimestamp());

        SetState(AsyncButtonState.Loading);

        try
        {
            if (OnClickAsync is not null)
                await OnClickAsync();
            else
                await OnClick.InvokeAsync(e);

            if (!IsDisposed)
            {
                SetState(AsyncButtonState.Success);
                if (SuccessDisplayMs > 0)
                    await ResetAfterDelayAsync(SuccessDisplayMs);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Id}] SgAsyncButton click error", ComponentId);
            if (!IsDisposed)
            {
                SetState(AsyncButtonState.Error);
                await ResetAfterDelayAsync(3000);
            }
        }
    }

    private void SetState(AsyncButtonState state)
    {
        lock (_stateLock) _state = state;
        if (!IsDisposed) _ = InvokeAsync(StateHasChanged);
    }

    private async Task ResetAfterDelayAsync(int delayMs)
    {
        var oldCts = Interlocked.Exchange(ref _feedbackCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
        Interlocked.Exchange(ref _feedbackCts, cts);
        try
        {
            await Task.Delay(delayMs, cts.Token);
            if (!IsDisposed && !cts.Token.IsCancellationRequested)
                SetState(AsyncButtonState.Idle);
        }
        catch (OperationCanceledException) { }
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        var cts = Interlocked.Exchange(ref _feedbackCts, null);
        cts?.Cancel();
        cts?.Dispose();
        await base.DisposeComponentAsync();
    }

    private enum AsyncButtonState { Idle, Loading, Success, Error }
}