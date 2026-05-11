using System.Globalization;

namespace SuperUI.Base;

/// <summary>
/// Контекст темы для каскадных параметров.
/// </summary>
public sealed class SgThemeContext
{
    public string? Theme { get; set; }
    public bool IsRtl { get; set; }
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentUICulture;
}

/// <summary>
/// Контекст конфигурации для каскадных параметров.
/// </summary>
public sealed class SgConfigContext
{
    public SgConfig Config { get; set; } = new();
}