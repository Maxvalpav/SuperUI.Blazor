using Bunit;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgAutoCompleteTests : BunitContext
{
    [Fact]
    public void FiltersItemsByTypedText()
    {
        var items = new[] { "Moscow", "Madrid", "Berlin" };
        var cut = Render<SgAutoComplete<string>>(parameters => parameters
            .Add(x => x.Items, items)
            .Add(x => x.MinCharacters, 1));

        cut.Find("input").Input("Ma");

        Assert.Contains("Madrid", cut.Markup);
        Assert.DoesNotContain("Berlin", cut.Markup);
    }

    [Fact]
    public void SelectsItemOnClick()
    {
        string? selected = null;
        var items = new[] { "Alice", "Bob", "Carol" };
        var cut = Render<SgAutoComplete<string>>(parameters => parameters
            .Add(x => x.Items, items)
            .Add(x => x.ValueChanged, v => selected = v)
            .Add(x => x.MinCharacters, 1));

        cut.Find("input").Input("Bo");
        cut.FindAll(".sgc-combo-option")[0].Click();

        Assert.Equal("Bob", selected);
    }
}
