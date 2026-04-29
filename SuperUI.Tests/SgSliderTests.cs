using Bunit;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgSliderTests : BunitContext
{
    [Fact]
    public void RendersSliderWithProperARIAAttributes()
    {
        var cut = Render<SgSlider<double>>(parameters => parameters
            .Add(x => x.Value, 50)
            .Add(x => x.Min, 0)
            .Add(x => x.Max, 100)
            .Add(x => x.Step, 1)
            .Add(x => x.Label, "Volume"));

        var input = cut.Find("input[type='range']");
        Assert.NotNull(input);
        
        // Check ARIA attributes
        Assert.Equal("slider", input.GetAttribute("role"));
        Assert.Equal("0", input.GetAttribute("aria-valuemin"));
        Assert.Equal("100", input.GetAttribute("aria-valuemax"));
        Assert.Equal("50", input.GetAttribute("aria-valuenow"));
        Assert.Equal("horizontal", input.GetAttribute("aria-orientation"));
    }

    [Fact]
    public void SliderHasAriaLabelledBy()
    {
        var cut = Render<SgSlider<double>>(parameters => parameters
            .Add(x => x.Value, 50)
            .Add(x => x.Label, "Volume"));

        var input = cut.Find("input[type='range']");
        var labelledBy = input.GetAttribute("aria-labelledby");
        Assert.NotNull(labelledBy);
        
        var label = cut.Find($"label#{labelledBy}");
        Assert.NotNull(label);
        Assert.Equal("Volume", label.TextContent);
    }

    [Fact]
    public void SliderDisplaysCurrentValue()
    {
        var cut = Render<SgSlider<double>>(parameters => parameters
            .Add(x => x.Value, 75)
            .Add(x => x.ShowValue, true)
            .Add(x => x.Suffix, "%"));

        var valueSpan = cut.Find("span.sgc-slider-value");
        Assert.NotNull(valueSpan);
        Assert.Contains("75", valueSpan.TextContent);
        Assert.Contains("%", valueSpan.TextContent);
    }

    [Fact]
    public void SliderHasValidationErrorARIA()
    {
        var cut = Render<SgSlider<double>>(parameters => parameters
            .Add(x => x.Value, 50));

        // Simulate validation error by checking aria-invalid attribute
        var input = cut.Find("input[type='range']");
        Assert.NotNull(input);
        // Initially should not have aria-invalid or it should be null
        var ariaInvalid = input.GetAttribute("aria-invalid");
        Assert.True(ariaInvalid == null || ariaInvalid == "false");
    }

    [Fact]
    public void SliderIsDisabled()
    {
        var cut = Render<SgSlider<double>>(parameters => parameters
            .Add(x => x.Value, 50)
            .Add(x => x.Disabled, true));

        var input = cut.Find("input[type='range']");
        Assert.NotNull(input.GetAttribute("disabled"));
    }

    [Fact]
    public void SliderHasCorrectMinMaxStep()
    {
        var cut = Render<SgSlider<double>>(parameters => parameters
            .Add(x => x.Value, 50)
            .Add(x => x.Min, 10)
            .Add(x => x.Max, 200)
            .Add(x => x.Step, 5));

        var input = cut.Find("input[type='range']");
        Assert.Equal("10", input.GetAttribute("min"));
        Assert.Equal("200", input.GetAttribute("max"));
        Assert.Equal("5", input.GetAttribute("step"));
    }

    [Fact]
    public void SliderBlockClassApplied()
    {
        var cut = Render<SgSlider<double>>(parameters => parameters
            .Add(x => x.Value, 50)
            .Add(x => x.Block, true));

        var field = cut.Find("div.sgc-field");
        Assert.Contains("sgc-block", field.GetAttribute("class"));
    }

    [Fact]
    public void SliderCustomCssClassApplied()
    {
        var cut = Render<SgSlider<double>>(parameters => parameters
            .Add(x => x.Value, 50)
            .Add(x => x.CssClass, "custom-slider"));

        var field = cut.Find("div.sgc-field");
        Assert.Contains("custom-slider", field.GetAttribute("class"));
    }
}
