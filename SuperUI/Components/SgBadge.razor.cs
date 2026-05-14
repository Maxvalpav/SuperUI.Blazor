using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Base.Optimization;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;
using SuperUI.Base.Utilities;

namespace SuperUI.Components;

public partial class SgBadge : SgReactiveComponentBase
{
    private readonly SgRenderBudgetGuard _renderGuard = new(maxPerSecond: 60);

    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? InnerContent { get; set; }
    [Parameter] public string? Text { get; set; }
    [Parameter] public int? Count { get; set; }
    [Parameter] public int Max { get; set; } = 99;
    [Parameter] public bool ShowZero { get; set; }
    [Parameter] public bool Dot { get; set; }
    [Parameter] public bool Hidden { get; set; }
    [Parameter] public bool Pulse { get; set; }
    [Parameter] public bool Processing { get; set; }
    [Parameter] public bool Outlined { get; set; }
    [Parameter] public bool Square { get; set; }
    [Parameter] public bool Ribbon { get; set; }
    [Parameter] public bool Standalone { get; set; }
    [Parameter] public bool Block { get; set; }
    [Parameter] public SgBadgeVariant Variant { get; set; } = SgBadgeVariant.Default;
    [Parameter] public SgSize Size { get; set; } = SgSize.Md;
    [Parameter] public SgBadgePlacement Placement { get; set; } = SgBadgePlacement.TopRight;
    [Parameter] public int? OffsetX { get; set; }
    [Parameter] public int? OffsetY { get; set; }
    [Parameter] public string? Color { get; set; }
    [Parameter] public string? Icon { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Inject] protected ISuperUILocalizer Localizer { get; set; } = default!;

    protected override string ComponentPrefix => "bdg";

    protected override void BuildReactiveRenderTree(RenderTreeBuilder builder) { }

    protected override bool ShouldRender()
    {
        if (!_renderGuard.TryRender()) return false;
        return base.ShouldRender();
    }

    private string GetWrapperClasses() => Css("sgc-badge-wrap")
        .AddIf("sgc-badge-block", Block)
        .Add(Class)
        .ToString();

    private string GetBadgeClasses(bool isStandalone) => Css("sgc-badge")
        .AddEnum(Variant, "sgc-")
        .AddEnum(Size, "sgc-badge-")
        .Add(GetPlacementClass())
        .AddIf("sgc-badge-dot", Dot)
        .AddIf("sgc-badge-pulse", Pulse)
        .AddIf("sgc-badge-processing", Processing)
        .AddIf("sgc-badge-outlined", Outlined)
        .AddIf("sgc-badge-square", Square)
        .AddIf("sgc-badge-inline", isStandalone)
        .ToString();

    private string GetRibbonClasses() => Css("sgc-badge-ribbon")
        .AddEnum(Variant, "sgc-")
        .Add(GetPlacementClass())
        .Add(Class)
        .ToString();

    private string? ComputedOffsetStyle => (OffsetX.HasValue || OffsetY.HasValue)
        ? StyleBuilder.Default()
            .AddCustomProperty("sgc-badge-ox", $"{ResolveOffsetX()}px", OffsetX.HasValue)
            .AddCustomProperty("sgc-badge-oy", $"{ResolveOffsetY()}px", OffsetY.HasValue)
            .NullIfEmpty()
        : null;

    private string? GetRibbonStyle() => StyleBuilder.Default()
        .Add("background-color", Color, !string.IsNullOrEmpty(Color))
        .AddUserStyle(Style)
        .NullIfEmpty();

    private string? GetStandaloneStyle() => StyleBuilder.Default()
        .Add("background-color", Color, !string.IsNullOrEmpty(Color))
        .AddUserStyle(Style)
        .NullIfEmpty();

    private string BadgeText => Count.HasValue
        ? (Count.Value > Max ? $"{Max}+" : Count.Value.ToString())
        : Text ?? string.Empty;

    private bool IsZero => Count == 0 && !ShowZero;

    private string BadgeAriaLabel => AriaLabel ?? (Count.HasValue ? $"{Count} notifications" : Text ?? Localizer["Badge"]);

    private string GetPlacementClass() => Placement switch
    {
        SgBadgePlacement.TopRight => "sgc-badge-tr",
        SgBadgePlacement.TopLeft => "sgc-badge-tl",
        SgBadgePlacement.BottomRight => "sgc-badge-br",
        SgBadgePlacement.BottomLeft => "sgc-badge-bl",
        _ => "sgc-badge-tr"
    };

    private int ResolveOffsetX() => Placement switch
    {
        SgBadgePlacement.TopRight or SgBadgePlacement.BottomRight => OffsetX ?? 0,
        SgBadgePlacement.TopLeft or SgBadgePlacement.BottomLeft => -(OffsetX ?? 0),
        _ => OffsetX ?? 0
    };

    private int ResolveOffsetY() => Placement switch
    {
        SgBadgePlacement.TopRight or SgBadgePlacement.TopLeft => OffsetY ?? 0,
        SgBadgePlacement.BottomRight or SgBadgePlacement.BottomLeft => -(OffsetY ?? 0),
        _ => OffsetY ?? 0
    };

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var builder = new AriaBuilder();
        builder.Label(BadgeAriaLabel);
        return builder.Build();
    }
}
