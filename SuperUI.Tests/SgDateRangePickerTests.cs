using Bunit;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgDateRangePickerTests : BunitContext
{
    [Fact]
    public void RendersSelectedRangeText()
    {
        var start = new DateTime(2026, 4, 1);
        var end = new DateTime(2026, 4, 7);

        var cut = Render<SgDateRangePicker>(parameters => parameters
            .Add(x => x.StartValue, start)
            .Add(x => x.EndValue, end)
            .Add(x => x.Format, "dd.MM.yyyy"));

        Assert.Contains("01.04.2026 - 07.04.2026", cut.Markup);
    }

    [Fact]
    public void CanPickStartAndEndDates()
    {
        DateTime? start = null;
        DateTime? end = null;

        var cut = Render<SgDateRangePicker>(parameters => parameters
            .Add(x => x.StartValue, start)
            .Add(x => x.StartValueChanged, v => start = v)
            .Add(x => x.EndValue, end)
            .Add(x => x.EndValueChanged, v => end = v));

        cut.Find(".sgc-date-control").Click();

        var buttons = cut.FindAll("button.sgc-date-cell:not([disabled])");
        buttons[10].Click();
        buttons = cut.FindAll("button.sgc-date-cell:not([disabled])");
        buttons[14].Click();

        Assert.NotNull(start);
        Assert.NotNull(end);
        Assert.True(end >= start);
    }
}
