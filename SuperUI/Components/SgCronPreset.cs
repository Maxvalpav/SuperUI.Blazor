namespace SuperUI.Components;

/// <summary>
/// Quick preset for the <see cref="SgCron"/> component.
/// </summary>
public sealed class SgCronPreset
{
    public string Label { get; init; } = "";
    public string Expression { get; init; } = "* * * * *";

    public SgCronPreset() { }

    public SgCronPreset(string label, string expression)
    {
        Label = label;
        Expression = expression;
    }

    public static IReadOnlyList<SgCronPreset> Defaults { get; } = new[]
    {
        new SgCronPreset("Каждую минуту", "* * * * *"),
        new SgCronPreset("Каждые 5 мин", "*/5 * * * *"),
        new SgCronPreset("Каждый час", "0 * * * *"),
        new SgCronPreset("Ежедневно в 00:00", "0 0 * * *"),
        new SgCronPreset("Ежедневно в 09:00", "0 9 * * *"),
        new SgCronPreset("По будням в 09:00", "0 9 * * 1-5"),
        new SgCronPreset("По выходным", "0 10 * * 0,6"),
        new SgCronPreset("1-го числа в 00:00", "0 0 1 * *"),
        new SgCronPreset("Раз в год", "0 0 1 1 *"),
    };
}
