using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Components;
using SuperUI.Enums;
using Xunit;

namespace SuperUI.Tests.Navigation;

public class NavMenuTests : BunitContext
{
    [Fact]
    public void NavMenu_Width_AppliesInlineCssVariable()
    {
        var cut = RenderComponent<SgNavMenu>(p => p
            .Add(x => x.Width, "260px"));

        var nav = cut.Find("nav.sgc-nav");
        var style = nav.GetAttribute("style") ?? string.Empty;
        Assert.Contains("--sgc-nav-w: 260px", style);
    }

    [Fact]
    public void NavMenu_Width_NotSet_OmitsStyle()
    {
        var cut = RenderComponent<SgNavMenu>();

        var nav = cut.Find("nav.sgc-nav");
        var style = nav.GetAttribute("style");
        Assert.True(string.IsNullOrEmpty(style));
    }

    [Fact]
    public void NavMenu_DoesNotRenderSearchHint()
    {
        var cut = RenderComponent<SgNavMenu>(p => p
            .Add(x => x.ShowSearch, true));

        Assert.Empty(cut.FindAll(".sgc-nav-search-hint"));
    }

    [Fact]
    public void NavMenu_DoesNotRenderSearchHint_EvenWithQuery()
    {
        var cut = RenderComponent<SgNavMenu>(p => p
            .Add(x => x.ShowSearch, true)
            .AddChildContent<SgNavLink>(c => c
                .Add(x => x.Href, "/")
                .Add(x => x.Text, "Home")));

        Assert.Empty(cut.FindAll(".sgc-nav-search-hint"));
    }

    [Fact]
    public void NavGroup_TopLevel_ExposesDepthZero()
    {
        var cut = RenderComponent<SgNavMenu>(p => p
            .AddChildContent<SgNavGroup>(g => g
                .Add(x => x.Title, "Layout")
                .AddChildContent<SgNavLink>(l => l
                    .Add(x => x.Href, "/x")
                    .Add(x => x.Text, "Item"))));

        var group = cut.Find("div.sgc-nav-group");
        var style = group.GetAttribute("style") ?? string.Empty;
        Assert.Contains("--sgc-nav-depth: 0", style);
    }

    [Fact]
    public void NavGroup_Nested_ExposesIncreasingDepth()
    {
        var cut = RenderComponent<SgNavMenu>(p => p
            .AddChildContent<SgNavGroup>(outer => outer
                .Add(x => x.Title, "Outer")
                .Add(x => x.Expanded, true)
                .AddChildContent<SgNavGroup>(inner => inner
                    .Add(x => x.Title, "Inner")
                    .Add(x => x.Expanded, true)
                    .AddChildContent<SgNavLink>(l => l
                        .Add(x => x.Href, "/x")
                        .Add(x => x.Text, "Item")))));

        // Find the two groups by title and check their depth styles
        var groups = cut.FindAll("div.sgc-nav-group");
        var depth0 = groups.First(g => (g.GetAttribute("style") ?? string.Empty).Contains("--sgc-nav-depth: 0"));
        var depth1 = groups.First(g => (g.GetAttribute("style") ?? string.Empty).Contains("--sgc-nav-depth: 1"));
        Assert.NotNull(depth0);
        Assert.NotNull(depth1);
    }

    [Fact]
    public void NavGroup_SubGroup_AppliesSubClass()
    {
        var cut = RenderComponent<SgNavMenu>(p => p
            .AddChildContent<SgNavGroup>(outer => outer
                .Add(x => x.Title, "Outer")
                .Add(x => x.Expanded, true)
                .AddChildContent<SgNavGroup>(inner => inner
                    .Add(x => x.Title, "Inner")
                    .Add(x => x.Expanded, true))));

        var subGroups = cut.FindAll("div.sgc-nav-group.sgc-nav-group-sub");
        // Only the Inner (depth=1) should be sub. Outer (depth=0) is not.
        Assert.Single(subGroups);
        var inner = subGroups[0];
        Assert.Contains("Inner", inner.OuterHtml);
        Assert.Contains("--sgc-nav-depth: 1", inner.GetAttribute("style") ?? string.Empty);
    }
}
