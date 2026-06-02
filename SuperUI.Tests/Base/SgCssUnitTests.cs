using SuperUI.Base.Utilities;
using Xunit;

namespace SuperUI.Tests.Base;

public class SgCssUnitTests
{
    [Fact]
    public void EnsureUnit_appends_px_when_missing()
    {
        Assert.Equal("12px", SgCssUnit.EnsureUnit("12"));
    }

    [Fact]
    public void EnsureUnit_keeps_existing_unit()
    {
        Assert.Equal("12em", SgCssUnit.EnsureUnit("12em"));
        Assert.Equal("50%", SgCssUnit.EnsureUnit("50%"));
        Assert.Equal("1rem", SgCssUnit.EnsureUnit("1rem"));
    }

    [Fact]
    public void EnsureUnit_empty_returns_zero()
    {
        Assert.Equal("0", SgCssUnit.EnsureUnit(""));
        Assert.Equal("0", SgCssUnit.EnsureUnit(null));
    }

    [Fact]
    public void EnsureUnit_custom_unit()
    {
        Assert.Equal("12em", SgCssUnit.EnsureUnit("12", "em"));
    }

    [Fact]
    public void ParsePixels_returns_double()
    {
        Assert.Equal(12.0, SgCssUnit.ParsePixels("12px"));
        Assert.Equal(12.5, SgCssUnit.ParsePixels("12.5px"));
        Assert.Equal(0.0, SgCssUnit.ParsePixels("not-a-number"));
    }

    [Fact]
    public void ParsePixels_with_fallback()
    {
        Assert.Equal(42.0, SgCssUnit.ParsePixels("invalid", 42.0));
    }

    [Fact]
    public void Scale_multiplies_pixels()
    {
        Assert.Equal("24px", SgCssUnit.Scale("12px", 2.0));
        Assert.Equal("6px", SgCssUnit.Scale("12px", 0.5));
    }

    [Fact]
    public void Scale_passes_through_relative_units()
    {
        Assert.Equal("1.5rem", SgCssUnit.Scale("1.5rem", 2.0));
        Assert.Equal("50%", SgCssUnit.Scale("50%", 2.0));
        Assert.Equal("100vh", SgCssUnit.Scale("100vh", 2.0));
    }

    [Fact]
    public void Scale_passes_through_var_and_calc()
    {
        Assert.Equal("var(--foo)", SgCssUnit.Scale("var(--foo)", 2.0));
        Assert.Equal("calc(100% - 10px)", SgCssUnit.Scale("calc(100% - 10px)", 2.0));
    }

    [Fact]
    public void Scale_one_returns_input_unchanged()
    {
        Assert.Equal("12px", SgCssUnit.Scale("12px", 1.0));
    }

    [Fact]
    public void DetectUnit_recognizes_common_units()
    {
        Assert.Equal("px", SgCssUnit.DetectUnit("12px"));
        Assert.Equal("em", SgCssUnit.DetectUnit("1.2em"));
        Assert.Equal("rem", SgCssUnit.DetectUnit("1.2rem"));
        Assert.Equal("%", SgCssUnit.DetectUnit("100%"));
        Assert.Equal("vh", SgCssUnit.DetectUnit("50vh"));
        Assert.Equal("vw", SgCssUnit.DetectUnit("100vw"));
        Assert.Equal("", SgCssUnit.DetectUnit("12"));
    }

    [Fact]
    public void IsAlreadyUnit_checks_known_units()
    {
        Assert.True(SgCssUnit.IsAlreadyUnit("12px"));
        Assert.True(SgCssUnit.IsAlreadyUnit("50%"));
        Assert.True(SgCssUnit.IsAlreadyUnit("1.2em"));
        Assert.False(SgCssUnit.IsAlreadyUnit("12"));
        Assert.False(SgCssUnit.IsAlreadyUnit(""));
    }
}
