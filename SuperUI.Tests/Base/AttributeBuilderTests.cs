using SuperUI.Base.Builders;
using Xunit;

namespace SuperUI.Tests.Base;

public class AttributeBuilderTests
{
    [Fact]
    public void Empty_builds_empty_string()
    {
        Assert.Equal("", AttributeBuilder.Empty().Build());
    }

    [Fact]
    public void Single_attribute()
    {
        var result = AttributeBuilder.Empty().Set("id", "main").Build();
        Assert.Equal("id=\"main\"", result);
    }

    [Fact]
    public void Multiple_attributes()
    {
        var result = AttributeBuilder.Empty()
            .Set("id", "main")
            .Set("tabindex", "0")
            .Build();
        Assert.Contains("id=\"main\"", result);
        Assert.Contains("tabindex=\"0\"", result);
    }

    [Fact]
    public void Null_value_skipped()
    {
        var result = AttributeBuilder.Empty().Set("disabled", (string?)null).Build();
        Assert.Equal("", result);
    }

    [Fact]
    public void Flag_renders_as_attribute_name_only()
    {
        var result = AttributeBuilder.Empty().SetFlag("disabled").Build();
        Assert.Equal("disabled", result);
    }

    [Fact]
    public void Flag_skipped_when_false()
    {
        var result = AttributeBuilder.Empty().SetFlag("disabled", false).Build();
        Assert.Equal("", result);
    }

    [Fact]
    public void Invalid_attribute_name_ignored_silently()
    {
        // IsValidName is defensive — invalid names are silently skipped.
        var result = AttributeBuilder.Empty().Set("on click=alert(1)", "x").Build();
        Assert.Equal("", result);
    }

    [Fact]
    public void Numeric_value_renders_as_string()
    {
        var result = AttributeBuilder.Empty().Set("tabindex", 0).Build();
        Assert.Equal("tabindex=\"0\"", result);
    }

    [Fact]
    public void Html_escaping_in_value()
    {
        var result = AttributeBuilder.Empty().Set("data-text", "<script>").Build();
        // " is escaped to &quot;, < to &lt;, > is safe in attribute context.
        Assert.Contains("data-text=\"&lt;script>\"", result);
    }

    [Fact]
    public void Default_with_existing_attributes()
    {
        var attrs = new Dictionary<string, object> { ["id"] = "main", ["data-test"] = "1" };
        var result = AttributeBuilder.Default(attrs).Build();
        Assert.Contains("id=\"main\"", result);
        Assert.Contains("data-test=\"1\"", result);
    }
}
