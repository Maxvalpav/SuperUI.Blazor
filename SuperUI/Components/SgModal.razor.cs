using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Base;
using SuperUI.Base.Services;

namespace SuperUI.Components;

public partial class SgModal : SgOverlayBase
{
    private IJSObjectReference? _module;
    private ElementReference _modalRef;
    private ElementReference _headerRef;
    private bool _jsAttached;

    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }
    [Parameter] public string Width { get; set; } = "500px";
    [Parameter] public bool Draggable { get; set; }
    [Parameter] public bool ShowClose { get; set; } = true;
    [Parameter] public SgModalAnimation Animation { get; set; } = SgModalAnimation.Scale;

    protected override int GetBaseZIndex() => IZIndexService.ModalBase;

    protected override async Task InitializeJsAsync(DotNetObjectReference<SgOverlayBase> dotNetRef)
    {
        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui-modal.js");
    }

    protected override async Task ShowJsAsync(int zIndex)
    {
        if (_module != null && _modalRef.Context != null)
        {
            await _module.InvokeVoidAsync("attach", _modalRef, DotNetObjectReference.Create(this), CloseOnEscape);
            if (Draggable)
            {
                await _module.InvokeVoidAsync("initDrag", _modalRef, _headerRef);
            }
            _jsAttached = true;
        }
    }

    protected override async Task HideJsAsync()
    {
        if (_module != null && _jsAttached)
        {
            await _module.InvokeVoidAsync("detach");
            _jsAttached = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (Visible && !_jsAttached && _module != null && _modalRef.Context != null)
        {
            await ShowJsAsync(CurrentZIndex);
        }
    }

    protected override string GetOverlayElementId() => EffectiveId;

    [JSInvokable]
    public async Task CloseFromJsAsync()
    {
        await HideAsync();
    }

    private async Task OnBackdropClick()
    {
        if (CloseOnBackdropClick)
        {
            await HideAsync();
        }
    }

    private string GetModalClasses() => Css("sgc-modal")
        .AddEnum(Animation, "sgc-modal-anim-")
        .ToString();

    private string GetBackdropStyle() => $"z-index: {CurrentZIndex - 5};";
    private string GetModalStyle() => $"z-index: {CurrentZIndex}; width: {Width};";

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_module != null)
        {
            try
            {
                if (_jsAttached) await _module.InvokeVoidAsync("detach");
                await _module.DisposeAsync();
            }
            catch { }
        }
        await base.DisposeAsyncCore();
    }
}
