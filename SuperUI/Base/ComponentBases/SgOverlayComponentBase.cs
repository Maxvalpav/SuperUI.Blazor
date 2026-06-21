// SuperUI/Base/ComponentBases/SgOverlayComponentBase.cs
// Базовый класс для оверлеев: Modal, Drawer, Popover, Tooltip, ToastHost, Dialog, CommandPalette.
// Убирает паттерн _previousVisible / Allocate / Release из каждого оверлея и
// добавляет focus restore + nested overlay support.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Utilities;
using SuperUI.Services;

namespace SuperUI.Base.ComponentBases;

/// <summary>
/// Базовый класс для компонентов-оверлеев SuperUI.
/// </summary>
/// <remarks>
/// <para>Управляет:</para>
/// <list type="bullet">
///   <item>Z-index (через <see cref="SgZIndexService"/>): Allocate при открытии, Release при закрытии.</item>
///   <item>Состоянием visible/closing: <see cref="IsOpening"/>, <see cref="IsClosing"/>.</item>
///   <item>Lifecycle-хуками: OnOpeningAsync, OnOpenedAsync, OnClosingAsync, OnClosedAsync.</item>
///   <item>Стандартным каналом закрытия: <see cref="RequestCloseAsync"/> (JS-вызов из ESC/backdrop).</item>
///   <item>Анимациями через <see cref="SgAnimationCoordinator"/> (с учётом prefers-reduced-motion).</item>
///   <item>Focus restore при закрытии (через <see cref="SgFocusManager"/>).</item>
/// </list>
/// <para><b>Использование (пример миграции SgModal):</b></para>
/// <code>
/// public partial class SgModal : SgOverlayComponentBase
/// {
///     protected override string ModulePath => "./_content/SuperUI/superui-modal.js";
///     protected override int ZIndexBase => SgZIndexService.ModalBase;
///     protected override string IdPrefix => "sg-modal";
///
///     protected override async ValueTask OnOpeningAsync()
///     {
///         await SafeInvokeVoidAsync("attach", RootRef, SelfRef, CloseOnEscape);
///     }
///
///     [JSInvokable] public override Task RequestCloseAsync() =&gt; CloseAsync();
/// }
/// </code>
/// </remarks>
public abstract class SgOverlayComponentBase : SgJsComponentBase
{
    private bool _previousVisible;
    private bool _isOpening;
    private bool _isClosing;
    private int _zIndexValue;
    private IAsyncDisposable? _focusTrap;

    /// <summary>Z-index service for stacking overlays.</summary>
    [Inject] protected SgZIndexService ZIndex { get; set; } = default!;

    /// <summary>Animation coordinator (reduced-motion aware).</summary>
    [Inject] protected SgAnimationCoordinator Animations { get; set; } = default!;

    /// <summary>Focus manager for trap/restore.</summary>
    [Inject] protected SgFocusManager Focus { get; set; } = default!;

    /// <summary>Controls overlay visibility.</summary>
    [Parameter] public bool Visible { get; set; }

    /// <summary>Two-way binding callback for <see cref="Visible"/>.</summary>
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }

    /// <summary>Base z-index for this overlay kind (e.g. <see cref="SgZIndexService.ModalBase"/>).</summary>
    protected abstract int ZIndexBase { get; }

    /// <summary>Current allocated z-index (0 = closed).</summary>
    protected int ZIndexValue => _zIndexValue;

    /// <summary>Backdrop z-index (5 below the overlay).</summary>
    protected int BackdropZIndex => _zIndexValue > 0 ? _zIndexValue - 5 : 0;

    /// <summary>True during the opening animation.</summary>
    protected bool IsOpening => _isOpening;

    /// <summary>True during the closing animation.</summary>
    protected bool IsClosing => _isClosing;

    // ── Lifecycle hooks ───────────────────────────────────────────────────────

    /// <summary>Called before show (before animation).</summary>
    protected virtual ValueTask OnOpeningAsync() => default;

    /// <summary>Called after open animation completes.</summary>
    protected virtual ValueTask OnOpenedAsync() => default;

    /// <summary>Called before hide (before animation).</summary>
    protected virtual ValueTask OnClosingAsync() => default;

    /// <summary>Called after hide animation completes.</summary>
    protected virtual ValueTask OnClosedAsync() => default;

    /// <summary>Override to disable or alter the closing animation duration (ms).</summary>
    protected virtual int ClosingAnimationMs => 200;

    /// <summary>
    /// Override to disable focus trap (default: true if the overlay contains focusable content).
    /// </summary>
    protected virtual bool UseFocusTrap => true;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>JS channel: ESC key or backdrop click.</summary>
    [Microsoft.JSInterop.JSInvokable]
    public virtual Task RequestCloseAsync() => CloseAsync();

    /// <summary>Programmatically closes the overlay.</summary>
    public virtual async Task CloseAsync()
    {
        if (IsDisposed || _isClosing || !Visible) return;

        _isClosing = true;
        StateHasChanged();

        // 1) user-defined teardown (JS detach).
        try { await OnClosingAsync(); }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (ObjectDisposedException) { }

        // 2) wait for the animation (or reduced-motion = 0).
        try
        {
            var delay = await Animations.BeginAsync(ClosingAnimationMs);
            await delay.WaitAsync();
        }
        catch (OperationCanceledException) { return; }

        _isClosing = false;
        Visible = false;
        _previousVisible = false;
        ReleaseZIndex();

        // 3) dispose focus trap.
        if (_focusTrap is not null)
        {
            try { await _focusTrap.DisposeAsync(); } catch { /* swallow */ }
            _focusTrap = null;
        }

        // 4) fire VisibleChanged.
        if (VisibleChanged.HasDelegate)
            await VisibleChanged.InvokeAsync(false);

        // 5) restore focus to the trigger.
        await Focus.RestoreAsync();

        // 6) user-defined post-close.
        try { await OnClosedAsync(); }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (ObjectDisposedException) { }
    }

    // ── Override OnInteractiveAsync ──────────────────────────────────────────

    /// <inheritdoc/>
    protected override async ValueTask OnInteractiveAsync()
    {
        // If the component is created with Visible=true (e.g. SSR→interactive handoff
        // or default-open), perform the full open cycle here and mark _previousVisible
        // so OnAfterRenderSafeAsync doesn't re-run it.
        if (Visible && !_previousVisible)
        {
            await RunOpenSequenceAsync();
        }
    }

    /// <summary>
    /// Shared open sequence: z-index allocation, focus capture, focus trap install, and
    /// the OnOpening/OnOpened hooks. Invoked from BOTH <see cref="OnInteractiveAsync"/>
    /// (default-open overlays) and <see cref="OnAfterRenderSafeAsync"/> (Visible→true at runtime)
    /// so a default-open overlay still gets focus capture and a focus trap.
    /// </summary>
    private async Task RunOpenSequenceAsync()
    {
        _previousVisible = true;
        _zIndexValue = ZIndex.Allocate(this, ZIndexBase);

        // Capture the current focus so CloseAsync can restore it.
        await Focus.CaptureAsync();

        if (UseFocusTrap && RootRef.Id is not null)
        {
            _focusTrap = await Focus.TrapAsync(RootRef);
        }

        _isOpening = true;
        StateHasChanged();
        try { await OnOpeningAsync(); } catch (Exception ex) { Logger.LogError(ex, "OnOpeningAsync failed."); }
        _isOpening = false;
        try { await OnOpenedAsync(); } catch (Exception ex) { Logger.LogError(ex, "OnOpenedAsync failed."); }
    }

    // ── Override OnAfterRenderSafeAsync ───────────────────────────────────────

    /// <inheritdoc/>
    protected override async Task OnAfterRenderSafeAsync(bool firstRender)
    {
        if (!IsInteractive) return; // SSR guard

        // Detect Visible → true (opening)
        if (Visible && !_previousVisible)
        {
            await RunOpenSequenceAsync();
        }
        // Detect Visible → false (closing via parameter, not CloseAsync)
        else if (!Visible && _previousVisible)
        {
            _previousVisible = false;
            ReleaseZIndex();
            if (_focusTrap is not null)
            {
                try { await _focusTrap.DisposeAsync(); } catch { /* swallow */ }
                _focusTrap = null;
            }
            try { await OnClosingAsync(); } catch (Exception ex) { Logger.LogError(ex, "OnClosingAsync failed."); }
            try { await OnClosedAsync(); } catch (Exception ex) { Logger.LogError(ex, "OnClosedAsync failed."); }
            await Focus.RestoreAsync();
        }
    }

    // ── Override OnDisposingAsync ─────────────────────────────────────────────

    /// <inheritdoc/>
    protected override async ValueTask OnDisposingAsync()
    {
        if (_focusTrap is not null)
        {
            try { await _focusTrap.DisposeAsync(); } catch { /* swallow */ }
            _focusTrap = null;
        }
        ReleaseZIndex();
    }

    /// <summary>Re-allocates z-index to bring the overlay to the top of its stack.</summary>
    protected void BringToFront()
    {
        if (!Visible || IsDisposed) return;
        if (_zIndexValue > 0) ZIndex.Release(this);
        _zIndexValue = ZIndex.Allocate(this, ZIndexBase);
    }

    private void ReleaseZIndex()
    {
        if (_zIndexValue > 0)
        {
            ZIndex.Release(this);
            _zIndexValue = 0;
        }
    }
}
