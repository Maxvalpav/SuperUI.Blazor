using Bunit;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgPopoverTests : BunitContext
{
    [Fact]
    public void OpensAndRendersBodyWhenTriggerClicked()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-popover.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("detach", _ => true);

        var cut = Render<SgPopover>(parameters => parameters
            .Add(x => x.ButtonText, "Toggle")
            .AddChildContent("<div id='popover-body'>Body</div>"));

        Assert.Empty(cut.FindAll(".sgc-pop"));

        cut.Find(".sgc-pop-trigger").Click();

        Assert.Single(cut.FindAll(".sgc-pop"));
        Assert.Contains("Body", cut.Markup);
    }
}
