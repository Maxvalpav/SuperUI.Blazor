// Файл: Services/ThemeContext.cs

namespace SuperUI.Services;

/// <summary>
/// Контекст темы — передаётся через CascadingValue.
/// Содержит дизайн-токены для компонентов.
/// </summary>
public sealed class SgThemeContext
{
    public string ThemeName { get; set; } = "light";
    public bool IsDark => ThemeName == "dark";

    /// <summary>
    /// Дизайн-токены (CSS custom properties mapping).
    /// Компоненты читают токены и генерируют inline CSS variables.
    /// </summary>
    public IReadOnlyDictionary<string, string> Tokens { get; set; }
        = new Dictionary<string, string>();

    public string GetToken(string tokenName, string fallback = "")
        => Tokens.TryGetValue(tokenName, out var v) ? v : fallback;
}

/// <summary>
/// Fluent конфигуратор токенов темы.
/// АНАЛОГ: Ant Design ConfigProvider, Telerik ThemeBuilder.
/// </summary>
public sealed class SgThemeBuilder
{
    private readonly Dictionary<string, string> _tokens = new();

    public SgThemeBuilder SetToken(string name, string value)
    {
        _tokens[name] = value;
        return this;
    }

    public SgThemeBuilder SetPrimaryColor(string color) => SetToken("--sg-primary", color);
    public SgThemeBuilder SetBorderRadius(string radius) => SetToken("--sg-border-radius", radius);
    public SgThemeBuilder SetFontFamily(string font) => SetToken("--sg-font-family", font);
    public SgThemeBuilder SetFontSize(string size) => SetToken("--sg-font-size-base", size);
    public SgThemeBuilder SetSpacing(string spacing) => SetToken("--sg-spacing-unit", spacing);

    public SgThemeContext Build() => new() { Tokens = _tokens };
}
