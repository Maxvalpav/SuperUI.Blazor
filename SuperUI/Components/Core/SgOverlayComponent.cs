using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Services;

namespace SuperUI.Core;

/// <summary>
/// Base for overlay components (modal, drawer, popover, tooltip, dropdown).
/// Provides:
/// <list type="bullet">
///   <item>two-way <see cref="IsOpen"/> binding with <see cref="IsOpenChanged"/>;</item>
///   <item>open/close events (<see cref="OnOpen"/>, <see cref="OnClose"/>) and an
///         <see cref="OnBeforeClose"/> hook that can veto the close;</item>
///   <item>z-index allocation through <see cref="SgZIndexService"/> on open and release
///         on close so nested overlays stack correctly;</item>
///   <item><see cref="CloseOnEsc"/> + <see cref="CloseOnBackdropClick"/> handling;</item>
///   <item>idempotent <see cref="OpenAsync"/> / <see cref="CloseAsync"/> / <see cref="ToggleAsync"/>.</item>
/// </list>
/// </summary>
public abstract class SgOverlayComponent : SgComponentBase
{
    private bool _wasOpen;
    private int? _allocatedZIndex;

    /// <summary>Service that hands out z-index slots so the most recent overlay wins.</summary>
    [Inject] protected SgZIndexService ZIndexService { get; set; } = default!;

    /// <summary>Whether the overlay is currently shown.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Two-way binding for <see cref="IsOpen"/>.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Raised after the overlay opens (post-allocation).</summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>Raised after the overlay closes (post-release).</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// Optional pre-close hook. Return <c>false</c> to veto the close — useful for confirming
    /// unsaved changes. Synchronous handlers can be wrapped via <c>EventCallback.Factory.Create</c>.
    /// </summary>
    [Parameter] public Func<Task<bool>>? OnBeforeClose { get; set; }

    /// <summary>Close when the user presses Escape. Defaults to <c>true</c>.</summary>
    [Parameter] public bool CloseOnEsc { get; set; } = true;

    /// <summary>Close when the user clicks the backdrop. Defaults to <c>true</c>.</summary>
    [Parameter] public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>Base z-index requested from <see cref="SgZIndexService"/>. Override per overlay type.</summary>
    protected virtual int ZIndexBase => SgZIndexService.ModalBase;

    /// <summary>The z-index assigned to this overlay while open, or <c>null</c> when closed.</summary>
    protected int? CurrentZIndex => _allocatedZIndex;

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (IsOpen && !_wasOpen)
        {
            await HandleOpenedAsync();
        }
        else if (!IsOpen && _wasOpen)
        {
            await HandleClosedAsync();
        }
    }

    /// <summary>Opens the overlay. No-op if already open.</summary>
    public Task OpenAsync() => SetOpenAsync(true);

    /// <summary>Closes the overlay. Runs <see cref="OnBeforeClose"/> if set; honours its veto.</summary>
    public async Task CloseAsync()
    {
        if (!IsOpen) return;
        if (OnBeforeClose is not null)
        {
            bool canClose;
            try { canClose = await OnBeforeClose(); }
            catch { canClose = true; }
            if (!canClose) return;
        }
        await SetOpenAsync(false);
    }

    /// <summary>Toggles the overlay.</summary>
    public Task ToggleAsync() => IsOpen ? CloseAsync() : OpenAsync();

    /// <summary>Handler for backdrop pointer events. Closes when <see cref="CloseOnBackdropClick"/> is true.</summary>
    protected Task OnBackdropClickAsync()
        => CloseOnBackdropClick ? CloseAsync() : Task.CompletedTask;

    /// <summary>Handler for global key events. Closes on Escape when <see cref="CloseOnEsc"/> is true.</summary>
    protected Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (CloseOnEsc && SgKeyboardHandler.Match(e, "Esc"))
        {
            return CloseAsync();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override to run JS-side work when the overlay opens (focus trap, body scroll lock).
    /// </summary>
    protected virtual ValueTask OnOpenedAsync() => ValueTask.CompletedTask;

    /// <summary>Override to undo JS-side work when the overlay closes.</summary>
    protected virtual ValueTask OnClosedAsync() => ValueTask.CompletedTask;

    private async Task SetOpenAsync(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        if (IsOpenChanged.HasDelegate) await IsOpenChanged.InvokeAsync(open);
        if (open) await HandleOpenedAsync();
        else await HandleClosedAsync();
    }

    private async Task HandleOpenedAsync()
    {
        _wasOpen = true;
        _allocatedZIndex = ZIndexService?.Allocate(this, ZIndexBase) ?? ZIndexBase;
        await OnOpenedAsync();
        if (OnOpen.HasDelegate) await OnOpen.InvokeAsync();
    }

    private async Task HandleClosedAsync()
    {
        _wasOpen = false;
        try { await OnClosedAsync(); }
        finally
        {
            if (_allocatedZIndex is not null)
            {
                ZIndexService?.Release(this);
                _allocatedZIndex = null;
            }
        }
        if (OnClose.HasDelegate) await OnClose.InvokeAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _allocatedZIndex is not null)
        {
            try { ZIndexService?.Release(this); } catch { /* swallow */ }
            _allocatedZIndex = null;
        }
        base.Dispose(disposing);
    }
}
