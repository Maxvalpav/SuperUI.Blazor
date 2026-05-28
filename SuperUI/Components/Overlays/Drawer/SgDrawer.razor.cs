using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Base.Utilities;
using SuperUI.Enums;
using SuperUI.Services;

namespace SuperUI.Components;

public partial class SgDrawer : SgOverlayComponentBase
{
    private readonly string _titleId = SgIdGenerator.NewId("drawer-title");
    private string _runtimeSize = string.Empty;

    protected override string ModulePath => "./_content/SuperUI/superui-drawer.js";
    protected override int ZIndexBase => SgZIndexService.DrawerBase;
    protected override int ClosingAnimationMs => 300;
    protected override string IdPrefix => "sg-drawer";

    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    [Parameter] public RenderFragment? TitleTemplate { get; set; }

    [Parameter] public SgPlacement Placement { get; set; } = SgPlacement.Right;
    [Parameter] public string Size { get; set; } = "360px";
    [Parameter] public EventCallback<string> SizeChanged { get; set; }
    [Parameter] public bool Resizable { get; set; }
    [Parameter] public bool ShowClose { get; set; } = true;
    [Parameter] public bool ShowHeader { get; set; } = true;
    [Parameter] public bool NoPadding { get; set; }
    [Parameter] public bool CloseOnBackdrop { get; set; } = true;
    [Parameter] public bool CloseOnEscape { get; set; } = true;
    [Parameter] public string? CloseIcon { get; set; }
    [Parameter] public string? MinSize { get; set; } = "200px";
    [Parameter] public string? MaxSize { get; set; }

    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnOpened { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }

    private string PlacementClass => Placement switch
    {
        SgPlacement.Left => "sgc-drawer-left",
        SgPlacement.Top => "sgc-drawer-top",
        SgPlacement.Bottom => "sgc-drawer-bottom",
        _ => "sgc-drawer-right"
    };

    private string ResizerClass => Placement switch
    {
        SgPlacement.Left => "sgc-drawer-resizer-right",
        SgPlacement.Top => "sgc-drawer-resizer-bottom",
        SgPlacement.Bottom => "sgc-drawer-resizer-top",
        _ => "sgc-drawer-resizer-left"
    };

    private string GetDrawerStyle()
    {
        var sz = string.IsNullOrEmpty(_runtimeSize) ? Size : _runtimeSize;
        return Styles()
            .AddStyle("z-index", ZIndexValue.ToString(), ZIndexValue > 0)
            .AddStyle("width", sz, Placement is SgPlacement.Left or SgPlacement.Right)
            .AddStyle("max-width", MaxSize ?? "100vw", Placement is SgPlacement.Left or SgPlacement.Right)
            .AddStyle("height", sz, Placement is SgPlacement.Top or SgPlacement.Bottom)
            .AddStyle("max-height", MaxSize ?? "100vh", Placement is SgPlacement.Top or SgPlacement.Bottom)
            .AddStyle("--sgc-drawer-min-size", MinSize ?? "200px")
            .Build();
    }

    protected override async ValueTask OnOpeningAsync()
    {
        await SafeInvokeVoidAsync("attach", RootRef, SelfRef, CloseOnEscape);
        if (Resizable)
            await SafeInvokeVoidAsync("initResize", RootRef, SelfRef, PlacementClass, MinSize ?? "200px", MaxSize ?? "");
    }

    protected override async ValueTask OnOpenedAsync()
    {
        if (OnOpened.HasDelegate)
            await OnOpened.InvokeAsync();
    }

    protected override async ValueTask OnClosingAsync()
    {
        await SafeInvokeVoidAsync("detach", RootRef);
    }

    protected override async ValueTask OnClosedAsync()
    {
        if (OnClose.HasDelegate)
            await OnClose.InvokeAsync();
        if (OnClosed.HasDelegate)
            await OnClosed.InvokeAsync();
    }

    [JSInvokable]
    public override Task RequestCloseAsync() => CloseAsync();

    [JSInvokable]
    public async Task UpdateSizeFromJs(string newSize)
    {
        _runtimeSize = newSize;
        if (SizeChanged.HasDelegate)
            await SizeChanged.InvokeAsync(newSize);
        await InvokeAsync(StateHasChanged);
    }

    private Task BackdropClickAsync() =>
        CloseOnBackdrop ? CloseAsync() : Task.CompletedTask;
}
