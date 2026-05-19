namespace SuperUI;

public sealed class SuperUiOptions
{
    public int DefaultToastDurationMs { get; set; } = 4000;
    public int MaxVisibleToasts { get; set; } = 5;

    public string? DefaultConfirmTitle { get; set; }
    public string? DefaultConfirmText { get; set; }
    public string? DefaultCancelText { get; set; }
}
