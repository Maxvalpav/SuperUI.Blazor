using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Base.Optimization;
using SuperUI.Base.Reactive;
using SuperUI.Base.Utilities;

namespace SuperUI.Components;

public partial class SgCard : SgReactiveComponentBase
{
    private SgSignal<bool> _internalSelected = default!;
    private SgSignal<bool> _internalCollapsed = default!;
    private readonly SgRenderBudgetGuard _renderGuard = new(maxPerSecond: 30);

    [Parameter] public bool Loading { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public string? Icon { get; set; }
    [Parameter] public SgCardVariant Variant { get; set; } = SgCardVariant.Default;
    [Parameter] public SgSize Size { get; set; } = SgSize.Md;
    [Parameter] public SgCardStatus Status { get; set; } = SgCardStatus.None;
    [Parameter] public bool Bordered { get; set; } = true;
    [Parameter] public bool Hoverable { get; set; }
    [Parameter] public bool NoPadding { get; set; }
    [Parameter] public bool Selectable { get; set; }
    [Parameter] public bool Selected { get; set; }
    [Parameter] public EventCallback<bool> SelectedChanged { get; set; }
    [Parameter] public bool Collapsible { get; set; }
    [Parameter] public bool Collapsed { get; set; }
    [Parameter] public EventCallback<bool> CollapsedChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    [Parameter] public RenderFragment? ActionContent { get; set; }
    [Parameter] public RenderFragment? CoverContent { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    protected override string ComponentPrefix => "crd";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _internalSelected = Signal(Selected, "card_selected");
        _internalCollapsed = Signal(Collapsed, "card_collapsed");
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_internalSelected.Value != Selected) _internalSelected.Set(Selected);
        if (_internalCollapsed.Value != Collapsed) _internalCollapsed.Set(Collapsed);
    }

    protected override void BuildReactiveRenderTree(RenderTreeBuilder builder) { }

    protected override bool ShouldRender()
    {
        if (!_renderGuard.TryRender()) return false;
        return base.ShouldRender();
    }

    private bool IsCollapsedValue => Collapsible && _internalCollapsed.Value;
    private bool IsSelectedValue => Selectable && _internalSelected.Value;
    private bool HasHeader => HeaderContent is not null || !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Subtitle) || !string.IsNullOrEmpty(Icon) || ActionContent is not null || Collapsible;

    private string GetRootClasses() => Css("sgc-card")
        .AddEnum(Variant, "sgc-")
        .AddEnum(Size, "sgc-")
        .AddEnum(Status, "sgc-status-")
        .AddIf("sgc-card-bordered", Bordered)
        .AddIf("sgc-card-hoverable", Hoverable && !Disabled)
        .AddIf("sgc-card-selectable", Selectable && !Disabled)
        .AddIf("sgc-card-selected", IsSelectedValue)
        .AddIf("sgc-card-disabled", Disabled)
        .AddIf("sgc-card-collapsed", IsCollapsedValue)
        .AddIf("sgc-card-loading", Loading)
        .Add(Class)
        .ToString();

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (Disabled || Loading) return;

        if (Selectable)
        {
            _internalSelected.Set(!_internalSelected.Value);
            await SelectedChanged.InvokeAsync(_internalSelected.Value);
        }

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (Disabled || Loading) return;
        if (Selectable && (e.Key == "Enter" || e.Key == " "))
        {
            await HandleClickAsync(new MouseEventArgs());
        }
    }

    private async Task ToggleCollapsedAsync()
    {
        if (!Collapsible || Disabled) return;
        _internalCollapsed.Set(!_internalCollapsed.Value);
        await CollapsedChanged.InvokeAsync(_internalCollapsed.Value);
    }

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var builder = new AriaBuilder();
        if (Selectable)
        {
            builder.Pressed(IsSelectedValue);
        }
        if (Collapsible)
        {
            builder.Expanded(!IsCollapsedValue);
        }
        builder.Disabled(Disabled);
        return builder.Build();
    }
}
