namespace SuperUI.Components;

/// <summary>
/// Quick preset for the <see cref="SgCron"/> component.
/// </summary>
public sealed class SgCronPreset
{
    /// <summary>Display label for the preset.</summary>
    public string Label { get; init; } = "";
    /// <summary>The cron expression for this preset.</summary>
    public string Expression { get; init; } = "* * * * *";

    /// <summary>Initializes a new empty preset.</summary>
    public SgCronPreset() { }

    /// <summary>Initializes a new preset with label and expression.</summary>
    public SgCronPreset(string label, string expression)
    {
        Label = label;
        Expression = expression;
    }

    /// <summary>Default English cron presets.</summary>
    public static IReadOnlyList<SgCronPreset> Defaults { get; } = new[]
    {
        new SgCronPreset("Every minute", "* * * * *"),
        new SgCronPreset("Every 5 min", "*/5 * * * *"),
        new SgCronPreset("Every hour", "0 * * * *"),
        new SgCronPreset("Daily at 00:00", "0 0 * * *"),
        new SgCronPreset("Daily at 09:00", "0 9 * * *"),
        new SgCronPreset("Weekdays at 09:00", "0 9 * * 1-5"),
        new SgCronPreset("Weekends at 10:00", "0 10 * * 0,6"),
        new SgCronPreset("1st at 00:00", "0 0 1 * *"),
        new SgCronPreset("Once a year", "0 0 1 1 *"),
    };

    [Obsolete("Use Defaults with a localized label via ISuperUILocalizer instead.")]
    public static IReadOnlyList<SgCronPreset> DefaultsRu { get; } = new[]
    {
        new SgCronPreset("Каждую минуту", "* * * * *"),
        new SgCronPreset("Каждые 5 мин", "*/5 * * * *"),
        new SgCronPreset("Каждый час", "0 * * * *"),
        new SgCronPreset("Ежедневно в 00:00", "0 0 * * *"),
        new SgCronPreset("Ежедневно в 09:00", "0 9 * * *"),
        new SgCronPreset("По будням в 09:00", "0 9 * * 1-5"),
        new SgCronPreset("По выходным в 10:00", "0 10 * * 0,6"),
        new SgCronPreset("1-го числа в 00:00", "0 0 1 * *"),
        new SgCronPreset("Раз в год", "0 0 1 1 *"),
    };
}
