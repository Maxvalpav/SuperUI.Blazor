// SuperUI/Base/SgOverlayBase.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для оверлейных компонентов.
///
/// ИСПРАВЛЕНИЯ:
/// 1. Двойной вызов OnOpenAsync/OnCloseAsync — флаг _isProcessingOverlay
/// 2. Task.Delay в OnCloseAsync — не прерывает cleanup при OperationCanceledException
/// 3. DisposeAsync: закрытие без анимации при dispose
/// 4. OpenAsync/CloseAsync: идемпотентны с проверкой состояния
/// </summary>
public abstract class SgOverlayBase : SgInteractiveBase
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected IZIndexService      ZIndexService      { get; set; } = null!;
    [Inject] protected IFocusTrapService   FocusTrapService   { get; set; } = null!;

    // ── Параметры ─────────────────────────────────────────────────────────────
    [Parameter] public bool Open                    { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public bool CloseOnEscape           { get; set; } = true;
    [Parameter] public bool CloseOnBackdropClick    { get; set; } = true;
    [Parameter] public bool LockBodyScroll          { get; set; } = true;
    [Parameter] public bool TrapFocus               { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // ── Внутреннее состояние ──────────────────────────────────────────────────
    private int    _zIndex;
    private bool   _wasOpen;
    private string? _focusTrapId;
    private bool   _isProcessingOverlay; // ИСПРАВЛЕНО: защита от двойного вызова

    protected int  EffectiveZIndex    => _zIndex;
    protected bool IsAnimatingClose   { get; private set; }

    // ── ARIA ──────────────────────────────────────────────────────────────────
    protected virtual string AriaRole => "dialog";

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal)
        {
            ["role"]       = AriaRole,
            ["aria-modal"] = "true",
            ["aria-hidden"] = (!Open).ToString().ToLower()
        };
        return attrs;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        OnKey("Escape", async () =>
        {
            if (CloseOnEscape && Open) await CloseAsync();
        });
        base.OnInitialized();
    }

    /// <summary>
    /// ИСПРАВЛЕНО: _isProcessingOverlay предотвращает двойной вызов
    /// из OnParametersSetAsync и OpenAsync/CloseAsync одновременно.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // ИСПРАВЛЕНО: только если не обрабатывается программное открытие/закрытие
        if (_isProcessingOverlay) return;

        if (Open && !_wasOpen)
        {
            _wasOpen = Open;
            await OnOpenCoreAsync();
        }
        else if (!Open && _wasOpen)
        {
            _wasOpen = Open;
            await OnCloseCoreAsync(animate: true);
        }
    }

    // ── Открытие / Закрытие ───────────────────────────────────────────────────
    private async Task OnOpenCoreAsync()
    {
        _zIndex = ZIndexService.GetNext();

        if (LockBodyScroll)
            await SafeInvokeVoidAsync("lockBodyScroll", ComponentId);

        if (TrapFocus)
        {
            _focusTrapId = ComponentId;
            await FocusTrapService.ActivateAsync(_focusTrapId);
        }

        await OnOpenAsync();
        StateHasChanged();
    }

    /// <summary>
    /// ИСПРАВЛЕНО: cleanup гарантирован даже при OperationCanceledException.
    /// Используем отдельный CTS для анимации, не ComponentToken.
    /// </summary>
    private async Task OnCloseCoreAsync(bool animate = true)
    {
        if (animate)
        {
            IsAnimatingClose = true;
            StateHasChanged();

            // ИСПРАВЛЕНО: используем отдельный cts для анимации с timeout
            using var animCts = new CancellationTokenSource(
                TimeSpan.FromMilliseconds(AnimationDurationMs + 100)); // +100ms buffer
            try
            {
                await Task.Delay(AnimationDurationMs, animCts.Token);
            }
            catch (OperationCanceledException) { /* анимация прервана — продолжаем cleanup */ }

            IsAnimatingClose = false;
        }

        // ИСПРАВЛЕНО: cleanup всегда выполняется
        if (TrapFocus && _focusTrapId != null)
        {
            await FocusTrapService.DeactivateAsync(_focusTrapId);
            _focusTrapId = null;
        }

        if (LockBodyScroll)
            await SafeInvokeVoidAsync("unlockBodyScroll", ComponentId);

        ZIndexService.Release(_zIndex);
        _zIndex = 0;

        await OnCloseAsync();
        StateHasChanged();
    }

    /// Переопределить для дополнительной логики при открытии.
    protected virtual Task OnOpenAsync() => Task.CompletedTask;

    /// Переопределить для дополнительной логики при закрытии.
    protected virtual Task OnCloseAsync() => Task.CompletedTask;

    protected virtual int AnimationDurationMs => 300;

    /// <summary>
    /// ИСПРАВЛЕНО: _isProcessingOverlay предотвращает дублирование из OnParametersSetAsync.
    /// </summary>
    public async Task OpenAsync()
    {
        if (Open || _isProcessingOverlay) return;
        _isProcessingOverlay = true;
        try
        {
            Open = true;
            _wasOpen = true;
            await OpenChanged.InvokeAsync(true);
            await OnOpenCoreAsync();
        }
        finally { _isProcessingOverlay = false; }
    }

    public async Task CloseAsync()
    {
        if (!Open || _isProcessingOverlay) return;
        _isProcessingOverlay = true;
        try
        {
            Open = false;
            _wasOpen = false;
            await OpenChanged.InvokeAsync(false);
            await OnCloseCoreAsync(animate: true);
        }
        finally { _isProcessingOverlay = false; }
    }

    protected async Task HandleBackdropClickAsync(MouseEventArgs e)
    {
        if (CloseOnBackdropClick) await CloseAsync();
    }

    // ── Dispose — ИСПРАВЛЕНО ─────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        // ИСПРАВЛЕНО: закрываем без анимации при dispose
        if (Open || _focusTrapId != null || _zIndex > 0)
            await OnCloseCoreAsync(animate: false);

        await base.DisposeComponentAsync();
    }
}
