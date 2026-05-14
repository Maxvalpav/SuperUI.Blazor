using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SuperUI.Base.Optimization;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;
using SuperUI.Base.Utilities;

namespace SuperUI.Components;

public partial class SgAlert : SgReactiveComponentBase
{
    private SgSignal<bool> _internalVisible = default!;
    private readonly SgRenderBudgetGuard _renderGuard = new(maxPerSecond: 30);

    [Parameter] public SgAlertVariant Variant { get; set; } = SgAlertVariant.Info;
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Text { get; set; }
    [Parameter] public string? Icon { get; set; }
    [Parameter] public bool ShowIcon { get; set; } = true;
    [Parameter] public bool Dismissible { get; set; }
    [Parameter] public bool Dense { get; set; }
    [Parameter] public string? Role { get; set; }
    [Parameter] public RenderFragment? ActionsContent { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Inject] protected ISuperUILocalizer Localizer { get; set; } = default!;

    protected override string ComponentPrefix => "alr";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _internalVisible = Signal(true, "alert_visible");
    }

    protected override void BuildReactiveRenderTree(RenderTreeBuilder builder)
    {
    }

    protected override bool ShouldRender()
    {
        if (!_renderGuard.TryRender()) return false;
        return base.ShouldRender();
    }

    private string GetAlertClasses() => Css("sgc-alert")
        .AddEnum(Variant, "sgc-")
        .AddIf("sgc-alert-dense", Dense)
        .Add(Class)
        .ToString();

    private string IconToRender => !string.IsNullOrWhiteSpace(Icon)
        ? Icon!
        : Variant switch
        {
            SgAlertVariant.Success => "✓",
            SgAlertVariant.Warn    => "!",
            SgAlertVariant.Danger  => "✗",
            _                      => "i"
        };

    private string ResolvedRole => Role ?? (Variant is SgAlertVariant.Danger or SgAlertVariant.Warn ? "alert" : "status");

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var builder = new AriaBuilder();
        builder.Live(Variant is SgAlertVariant.Danger or SgAlertVariant.Warn ? "assertive" : "polite");
        return builder.Build();
    }

    private async Task CloseAsync()
    {
        _internalVisible.Set(false);
        await OnClose.InvokeAsync();
    }
}
