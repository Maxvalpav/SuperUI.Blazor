using System.Globalization;

namespace SuperUI.Base;

/// <summary>
/// Конфигурация SuperUI провайдера.
/// </summary>
public sealed record SgConfig
{
    public string Theme { get; init; } = "light";
    public bool IsRtl { get; init; }
    public CultureInfo DefaultCulture { get; init; } = CultureInfo.CurrentUICulture;
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? DangerColor { get; init; }
    public string? SuccessColor { get; init; }
    public string? WarningColor { get; init; }
    public string? BorderRadius { get; init; }
    public string? FontFamily { get; init; }
}