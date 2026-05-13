// ================================================================
// Файл: SuperUI/Base/SgAsyncButton.cs
// ИСПРАВЛЕНО: ButtonType → определён в этом же файле
// ================================================================

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SuperUI.Base;

/// <summary>
/// HTML button type enumeration.
/// </summary>
public enum ButtonType
{
    Button,
    Submit,
    Reset
}

/// <summary>
/// Button with built-in async operation support,
/// loading state, success/error feedback, and debouncing.
/// </summary>
public class SgAsyncButton : ComponentBase, IDisposable
{
    [Parameter] public string? Text { get; set; } = "Submit";
    [Parameter] public string? LoadingText { get; set; } = "Loading...";
    [Parameter] public string? SuccessText { get; set; }
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public string? IconCssClass { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public ButtonType Type { get; set; } = ButtonType.Button;
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public Func<Task>? OnClickAsync { get; set; }
    [Parameter] public int DebounceMs { get; set; } = 0;
    [Parameter] public int SuccessDisplayMs { get; set; } = 2000;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Inject] private ILogger<SgAsyncButton> Logger { get; set; } = NullLogger<SgAsyncButton>.Instance;

    private AsyncOperationState _state = AsyncOperationState.Idle;
    private DateTime _lastClickTime = DateTime.MinValue;
    private bool _disposed;

    protected bool IsLoading => _state == AsyncOperationState.Loading;
    protected bool IsSuccess => _state == AsyncOperationState.Success;
    protected bool IsError => _state == AsyncOperationState.Error;

    protected string CurrentText => _state switch
    {
        AsyncOperationState.Loading => LoadingText ?? Text ?? "Loading...",
        AsyncOperationState.Success => SuccessText ?? Text ?? "Done!",
        AsyncOperationState.Error => ErrorText ?? Text ?? "Error",
        _ => Text ?? "Submit"
    };

    protected async Task HandleClickAsync(MouseEventArgs e)
    {
        if (_disposed || Disabled || IsLoading) return;

        if (DebounceMs > 0)
        {
            var elapsed = (DateTime.UtcNow - _lastClickTime).TotalMilliseconds;
            if (elapsed < DebounceMs) return;
        }

        _lastClickTime = DateTime.UtcNow;

        try
        {
            _state = AsyncOperationState.Loading;
            StateHasChanged();

            await OnClick.InvokeAsync();
            if (OnClickAsync != null)
                await OnClickAsync();

            _state = AsyncOperationState.Success;
            StateHasChanged();

            if (SuccessDisplayMs > 0)
            {
                await Task.Delay(SuccessDisplayMs);
                if (!_disposed)
                {
                    _state = AsyncOperationState.Idle;
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in SgAsyncButton click handler");
            _state = AsyncOperationState.Error;
            StateHasChanged();

            await Task.Delay(3000);
            if (!_disposed)
            {
                _state = AsyncOperationState.Idle;
                StateHasChanged();
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private enum AsyncOperationState
    {
        Idle,
        Loading,
        Success,
        Error
    }
}
