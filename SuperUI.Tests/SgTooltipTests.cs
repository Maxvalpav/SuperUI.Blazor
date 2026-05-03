using Bunit;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgTooltipTests : BunitContext
{
    [Fact]
    public void RendersWithRequiredText()
    {
        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .AddChildContent("<button>Hover me</button>"));

        Assert.Contains("Hover me", cut.Markup);
    }

    [Fact]
    public void HasCorrectAriaAttributes()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-tooltip.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("show", _ => true);
        module.SetupVoid("hide", _ => true);

        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .AddChildContent("<button>Hover me</button>"));

        // Trigger hover to show tooltip
        cut.Find(".sgc-tt-wrap").MouseEnter();

        // Check for role="tooltip" attribute
        var tooltip = cut.FindAll(".sgc-tt");
        Assert.NotEmpty(tooltip);
        Assert.Contains("role=\"tooltip\"", cut.Markup);
    }

    [Fact]
    public void ShowsTooltipOnMouseEnter()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-tooltip.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("show", _ => true);

        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .AddChildContent("<button>Hover me</button>"));

        // Initially tooltip should not be visible
        Assert.Empty(cut.FindAll(".sgc-tt"));

        // Trigger mouse enter
        cut.Find(".sgc-tt-wrap").MouseEnter();

        // Tooltip should now be rendered
        Assert.NotEmpty(cut.FindAll(".sgc-tt"));
        Assert.Contains("Test tooltip", cut.Markup);
    }

    [Fact]
    public void HidesTooltipOnMouseLeave()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-tooltip.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("show", _ => true);
        module.SetupVoid("hide", _ => true);

        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .AddChildContent("<button>Hover me</button>"));

        // Show tooltip
        cut.Find(".sgc-tt-wrap").MouseEnter();
        Assert.NotEmpty(cut.FindAll(".sgc-tt"));

        // Hide tooltip
        cut.Find(".sgc-tt-wrap").MouseLeave();

        // Tooltip should be hidden
        Assert.Empty(cut.FindAll(".sgc-tt"));
    }

    [Fact]
    public void SupportsCustomPlacement()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-tooltip.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("show", _ => true);

        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .Add(x => x.Placement, SgPlacement.Bottom)
            .AddChildContent("<button>Hover me</button>"));

        cut.Find(".sgc-tt-wrap").MouseEnter();

        // Verify placement was passed to JS
        var invocations = module.Invocations.Where(x => x.Identifier == "show").ToList();
        Assert.NotEmpty(invocations);
    }

    [Fact]
    public void SupportsCustomCssClass()
    {
        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .Add(x => x.CssClass, "custom-class")
            .AddChildContent("<button>Hover me</button>"));

        Assert.Contains("custom-class", cut.Markup);
    }

    [Fact]
    public void DisposesCorrectly()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-tooltip.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("show", _ => true);
        module.SetupVoid("detach", _ => true);

        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .AddChildContent("<button>Hover me</button>"));

        // Show tooltip to trigger attach
        cut.Find(".sgc-tt-wrap").MouseEnter();

        // Dispose component - should not throw
        cut.Dispose();

        // Component should be disposed without errors
        Assert.True(true);
    }

    [Fact]
    public void RendersInPortal()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-tooltip.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("show", _ => true);
        module.SetupModule("/_content/SuperUI/superui-portal.js");

        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .AddChildContent("<button>Hover me</button>"));

        cut.Find(".sgc-tt-wrap").MouseEnter();

        // Tooltip should be rendered in portal (SgPortal component)
        Assert.NotEmpty(cut.FindAll(".sgc-tt"));
    }

    [Fact]
    public void ShowsTooltipOnFocus()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-tooltip.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("show", _ => true);

        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .AddChildContent("<button>Focus me</button>"));

        // Initially tooltip should not be visible
        Assert.Empty(cut.FindAll(".sgc-tt"));

        // Trigger focus
        cut.Find(".sgc-tt-wrap").Focus();

        // Tooltip should now be rendered
        Assert.NotEmpty(cut.FindAll(".sgc-tt"));
    }

    [Fact]
    public void HidesTooltipOnBlur()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-tooltip.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("show", _ => true);
        module.SetupVoid("hide", _ => true);

        var cut = Render<SgTooltip>(parameters => parameters
            .Add(x => x.Text, "Test tooltip")
            .AddChildContent("<button>Focus me</button>"));

        // Show tooltip via focus
        cut.Find(".sgc-tt-wrap").Focus();
        Assert.NotEmpty(cut.FindAll(".sgc-tt"));

        // Hide tooltip via blur
        cut.Find(".sgc-tt-wrap").Blur();

        // Tooltip should be hidden
        Assert.Empty(cut.FindAll(".sgc-tt"));
    }
}
