// SuperUI/Base/SgConfig.cs
//
// Конфигурация SuperUI — передаётся через CascadingParameter.
// SgConfigContext — контекст каскадного параметра.
// SgConfigProvider.razor предоставляет его дочерним компонентам.

namespace SuperUI.Base;

/// <summary>
/// Глобальная конфигурация SuperUI (каскадный параметр).
/// </summary>
public sealed record SgConfigContext
{
    /// <summary>Тема: "light" | "dark" | "auto".</summary>
    public string Theme { get; init; } = "auto";

    /// <summary>Культура/Locale: "en-US" | "ru-RU".</summary>
    public string Culture { get; init; } = "en-US";

    /// <summary>Направление текста.</summary>
    public bool IsRtl { get; init; } = false;

    /// <summary>Режим компактного отображения.</summary>
    public bool Compact { get; init; } = false;

    /// <summary>Анимации включены.</summary>
    public bool AnimationsEnabled { get; init; } = true;

    /// <summary>Префикс CSS-классов (для кастомизации).</summary>
    public string CssPrefix { get; init; } = "sg";
}

/// <summary>
/// Контекст темы (каскадный параметр от SgThemeProvider).
/// </summary>
public sealed record SgThemeContext
{
    public string ThemeName     { get; init; } = "light";
    public bool   IsDark        { get; init; } = false;
    public string PrimaryColor  { get; init; } = "#4f6af5";
}
