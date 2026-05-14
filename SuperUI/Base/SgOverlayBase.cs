// SuperUI/Base/SgOverlayBase.cs 
// Улучшения: 
// - Проверка IsInteractive перед JS interop 
// - Корректная работа при prerendering 
// - Focus trap интеграция с IFocusTrapService 
// - Z-index management через IZIndexService 
// - Поддержка Escape key для закрытия 
 
using System; 
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Components.Web; 
using Microsoft.JSInterop; 
using SuperUI.Base.Services; 
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Базовый класс для overlay-компонентов (Modal, Drawer, Tooltip и т.д.). 
/// Безопасен в SSR — не вызывает JS interop при статичном рендере. 
/// </summary> 
public abstract class SgOverlayBase : SgComponentBase 
{ 
    [Inject] protected IJSRuntime JS { get; set; } = default!; 
    [Inject] protected IZIndexService ZIndexService { get; set; } = default!; 
    [Inject] protected IFocusTrapService FocusTrapService { get; set; } = default!; 
 
    private bool _visible; 
    private int _zIndex;
    protected int CurrentZIndex => _zIndex;
    private bool _focusTrapActive; 
    private DotNetObjectReference<SgOverlayBase>? _dotNetRef; 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Параметры 
    // ────────────────────────────────────────────────────────────────────── 
 
    [Parameter] public override bool Visible { get; set; } 
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; } 
    [Parameter] public bool TrapFocus { get; set; } = true; 
    [Parameter] public bool CloseOnEscape { get; set; } = true; 
    [Parameter] public bool CloseOnBackdropClick { get; set; } = true; 
    [Parameter] public RenderFragment? ChildContent { get; set; } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Жизненный цикл 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected override async Task OnParametersSetAsync() 
    { 
        await base.OnParametersSetAsync(); 
 
        bool visibilityChanged = _visible != Visible; 
        _visible = Visible; 
 
        if (!IsInteractive) return; // Не трогаем DOM в SSR 
 
        if (visibilityChanged) 
        { 
            if (Visible) 
                await OnShowAsync(); 
            else 
                await OnHideAsync(); 
        } 
    } 
 
    protected override async Task OnAfterRenderAsync(bool firstRender) 
    { 
        await base.OnAfterRenderAsync(firstRender); 
 
        if (!IsInteractive) return; 
 
        if (firstRender) 
        { 
            _dotNetRef = DotNetObjectReference.Create(this); 
            await InitializeJsAsync(_dotNetRef); 
        } 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Показ/скрытие 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected virtual int GetBaseZIndex() => 0;

    protected virtual async Task OnShowAsync() 
    { 
        // Получаем z-index 
        int baseZIndex = GetBaseZIndex();
        _zIndex = baseZIndex > 0 ? ZIndexService.Allocate(baseZIndex) : ZIndexService.GetNext(); 
 
        // Focus trap 
        if (TrapFocus && !_focusTrapActive) 
        { 
            _focusTrapActive = true; 
            await FocusTrapService.ActivateAsync(GetOverlayElementId()); 
        } 
 
        await ShowJsAsync(_zIndex); 
    } 
 
    protected virtual async Task OnHideAsync() 
    { 
        if (TrapFocus && _focusTrapActive) 
        { 
            _focusTrapActive = false; 
            await FocusTrapService.DeactivateAsync(GetOverlayElementId()); 
        } 
 
        ZIndexService.Release(_zIndex); 
        await HideJsAsync(); 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Публичные методы 
    // ────────────────────────────────────────────────────────────────────── 
 
    public async Task ShowAsync() 
    { 
        if (!IsInteractive) return; 
        Visible = true; 
        await VisibleChanged.InvokeAsync(true); 
        await NotifyStateChangedAsync(); 
    } 
 
    public async Task HideAsync() 
    { 
        if (!IsInteractive) return; 
        Visible = false; 
        await VisibleChanged.InvokeAsync(false); 
        await NotifyStateChangedAsync(); 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Обработка событий клавиатуры 
    // ────────────────────────────────────────────────────────────────────── 
 
    [JSInvokable] 
    public async Task OnKeyDownAsync(string key) 
    { 
        if (CloseOnEscape && key == "Escape" && Visible) 
            await HideAsync(); 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Абстрактные методы для JS interop 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected abstract Task InitializeJsAsync(DotNetObjectReference<SgOverlayBase> dotNetRef); 
    protected abstract Task ShowJsAsync(int zIndex); 
    protected abstract Task HideJsAsync(); 
    protected abstract string GetOverlayElementId(); 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Dispose 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected override async ValueTask DisposeAsyncCore() 
    { 
        if (_focusTrapActive) 
        { 
            _focusTrapActive = false; 
            try { await FocusTrapService.DeactivateAsync(GetOverlayElementId()); } 
            catch { } 
        } 
 
        _dotNetRef?.Dispose(); 
        await base.DisposeAsyncCore(); 
    } 
}