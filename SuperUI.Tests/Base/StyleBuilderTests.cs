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
}