using SuperUI.Base.Builders;
using Xunit;

namespace SuperUI.Tests.Base;

public class StyleBuilderTests
{
    [Fact] void Empty_returns_empty_string()
        => Assert.Equal("", StyleBuilder.Empty().Build());

    [Fact] void Single_style_has_semicolon()
        => Assert.Equal("display:flex;", StyleBuilder.Empty().AddStyle("display", "flex").Build());

    [Fact] void Null_value_ignored()
        => Assert.Equal("", StyleBuilder.Empty().AddStyle("color", null).Build());

    [Fact] void Conditional_false_skips()
        => Assert.Equal("", StyleBuilder.Empty().AddStyle("width", "100%", false).Build());

    [Fact] void Multiple_properties_semicolon_separated()
    {
        var result = StyleBuilder.Empty()
            .AddStyle("display", "flex")
            .AddStyle("color", "red")
            .Build();
        Assert.Equal("display:flex;color:red;", result);
    }

    [Fact] void Raw_string_appended()
        => Assert.Equal("display:flex;color:red;",
            StyleBuilder.Default("display:flex").AddStyle("color:red").Build());

    [Fact] void NullIfEmpty_returns_null()
        => Assert.Null(StyleBuilder.Empty().NullIfEmpty());

    [Fact] void Merge_from_attributes()
    {
        var attrs = new Dictionary<string, object> { ["style"] = "color:red" };
        var result = StyleBuilder.Default("display:flex")
            .AddStyleFromAttributes(attrs)
            .Build();
        Assert.Contains("color:red", result);
    }

    [Fact] void Add_alias_with_when()
    {
        var result = StyleBuilder.Empty().Add("display", "flex", true).Build();
        Assert.Equal("display:flex;", result);
    }

    [Fact] void Add_alias_with_when_false()
    {
        var result = StyleBuilder.Empty().Add("display", "flex", false).Build();
        Assert.Equal("", result);
    }

    [Fact] void Invalid_property_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StyleBuilder.Empty().AddStyle("display:flex;injected", "x"));
    }

    [Fact] void Custom_property_accepted()
    {
        var result = StyleBuilder.Empty().AddStyle("--my-color", "red").Build();
        Assert.Equal("--my-color:red;", result);
    }

    [Fact] void Vendor_prefix_accepted()
    {
        var result = StyleBuilder.Empty().AddStyle("-webkit-transform", "rotate(45deg)").Build();
        Assert.Equal("-webkit-transform:rotate(45deg);", result);
    }

    [Fact] void IsValidPropertyName_rejects_dangerous()
    {
        Assert.False(StyleBuilder.IsValidPropertyName("display: flex; background: url(javascript:alert(1))"));
        Assert.False(StyleBuilder.IsValidPropertyName(""));
        Assert.False(StyleBuilder.IsValidPropertyName("1width")); // starts with digit
        Assert.False(StyleBuilder.IsValidPropertyName("color;"));
    }
}