using SuperUI.Base.Builders;
using Xunit;

namespace SuperUI.Tests.Base;

public class CssBuilderTests
{
    [Fact] void Empty_builder_returns_empty_string()
        => Assert.Equal("", CssBuilder.Empty().Build());

    [Fact] void Null_class_is_ignored()
        => Assert.Equal("", CssBuilder.Default(null).AddClass(null).Build());

    [Fact] void Whitespace_class_is_ignored()
        => Assert.Equal("", CssBuilder.Default("  ").Build());

    [Fact] void Single_class_returns_trimmed()
        => Assert.Equal("sg-btn", CssBuilder.Default("sg-btn").Build());

    [Fact] void Multiple_classes_joined_by_space()
        => Assert.Equal("a b c", CssBuilder.Default("a").AddClass("b").AddClass("c").Build());

    [Fact] void Conditional_false_skips_class()
        => Assert.Equal("base", CssBuilder.Default("base").AddClass("extra", false).Build());

    [Fact] void Conditional_true_adds_class()
        => Assert.Equal("base extra", CssBuilder.Default("base").AddClass("extra", true).Build());

    [Fact] void Func_condition_evaluated()
        => Assert.Equal("base x", CssBuilder.Default("base").AddClass("x", () => true).Build());

    [Fact] void Merge_from_attributes_adds_class()
    {
        var attrs = new Dictionary<string, object> { ["class"] = "user-class" };
        var result = CssBuilder.Default("base")
            .AddClassFromAttributes(attrs)
            .Build();
        Assert.Equal("base user-class", result);
    }

    [Fact] void NullIfEmpty_returns_null_for_empty()
        => Assert.Null(CssBuilder.Empty().NullIfEmpty());

    [Fact] void NullIfEmpty_returns_value_if_not_empty()
        => Assert.Equal("x", CssBuilder.Default("x").NullIfEmpty());

    [Fact] void Implicit_string_conversion()
    {
        string s = CssBuilder.Default("btn");
        Assert.Equal("btn", s);
    }
}