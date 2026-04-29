using Bunit;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgBreadcrumbTests : BunitContext
{
    [Fact]
    public void RendersBreadcrumbWithProperARIAAttributes()
    {
        var items = new[]
        {
            new BreadcrumbItem("Home", "/"),
            new BreadcrumbItem("Products", "/products"),
            new BreadcrumbItem("Electronics", "/products/electronics")
        };

        var cut = Render<SgBreadcrumb>(parameters => parameters
            .Add(x => x.Items, items));

        // Check navigation role
        var nav = cut.Find("nav");
        Assert.NotNull(nav);
        Assert.Equal("navigation", nav.GetAttribute("role"));
        Assert.Equal("breadcrumb", nav.GetAttribute("aria-label"));

        // Check list role
        var ol = cut.Find("ol");
        Assert.NotNull(ol);
        Assert.Equal("list", ol.GetAttribute("role"));

        // Check listitem roles
        var listItems = cut.FindAll("li");
        Assert.Equal(3, listItems.Count);
        foreach (var li in listItems)
        {
            Assert.Equal("listitem", li.GetAttribute("role"));
        }
    }

    [Fact]
    public void LastBreadcrumbItemHasAriaCurrentPage()
    {
        var items = new[]
        {
            new BreadcrumbItem("Home", "/"),
            new BreadcrumbItem("Products", "/products"),
            new BreadcrumbItem("Electronics", "/products/electronics")
        };

        var cut = Render<SgBreadcrumb>(parameters => parameters
            .Add(x => x.Items, items));

        var spans = cut.FindAll("span.sgc-bc-current");
        Assert.Single(spans);
        Assert.Equal("page", spans[0].GetAttribute("aria-current"));
    }

    [Fact]
    public void BreadcrumbLinksHaveAriaLabel()
    {
        var items = new[]
        {
            new BreadcrumbItem("Home", "/"),
            new BreadcrumbItem("Products", "/products")
        };

        var cut = Render<SgBreadcrumb>(parameters => parameters
            .Add(x => x.Items, items));

        var links = cut.FindAll("a.sgc-bc-link");
        Assert.Single(links);
        // The first link should be "Home" since it's the first item
        Assert.Equal("Home", links[0].GetAttribute("aria-label"));
    }

    [Fact]
    public void SeparatorIsHiddenFromScreenReaders()
    {
        var items = new[]
        {
            new BreadcrumbItem("Home", "/"),
            new BreadcrumbItem("Products", "/products")
        };

        var cut = Render<SgBreadcrumb>(parameters => parameters
            .Add(x => x.Items, items));

        var separators = cut.FindAll("span.sgc-bc-sep");
        Assert.Single(separators);
        Assert.Equal("true", separators[0].GetAttribute("aria-hidden"));
    }

    [Fact]
    public void AutoGeneratesBreadcrumbsFromUri()
    {
        var cut = Render<SgBreadcrumb>(parameters => parameters
            .Add(x => x.AutoGenerate, true));

        var listItems = cut.FindAll("li");
        // Should have at least Home item
        Assert.NotEmpty(listItems);
    }

    [Fact]
    public void CustomSeparatorIsDisplayed()
    {
        var items = new[]
        {
            new BreadcrumbItem("Home", "/"),
            new BreadcrumbItem("Products", "/products")
        };

        var cut = Render<SgBreadcrumb>(parameters => parameters
            .Add(x => x.Items, items)
            .Add(x => x.Separator, " > "));

        var separator = cut.Find("span.sgc-bc-sep");
        Assert.NotNull(separator);
        Assert.Contains(">", separator.TextContent);
    }
}
