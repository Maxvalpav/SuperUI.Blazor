using Bunit;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgAlertTests : BunitContext
{
    [Fact]
    public void RendersTitleVariantAndText()
    {
        var cut = Render<SgAlert>(parameters => parameters
            .Add(x => x.Title, "Saved")
            .Add(x => x.Text, "Settings were updated.")
            .Add(x => x.Variant, "success"));

        cut.MarkupMatches(@"
<div class=""sgc-alert sgc-success  "" role=""status"">
  <div class=""sgc-alert-icon"" aria-hidden=""true"">✓</div>
  <div class=""sgc-alert-body"">
    <div class=""sgc-alert-title"">Saved</div>
    <div class=""sgc-alert-content"">Settings were updated.</div>
  </div>
</div>");
    }

    [Fact]
    public void DismissibleAlertHidesAfterClose()
    {
        var cut = Render<SgAlert>(parameters => parameters
            .Add(x => x.Text, "Close me")
            .Add(x => x.Dismissible, true));

        cut.Find("button.sgc-alert-close").Click();

        Assert.Empty(cut.FindAll(".sgc-alert"));
    }
}
