namespace SuperUI.Themes;

/// <summary>
/// Fluent builder for creating custom themes programmatically.
/// </summary>
public sealed class ThemeBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "Custom Theme";
    private string _description = "";
    private string _author = "";
    private string _primaryColor = "#2563EB";
    private string _primaryColorDark = "#3B82F6";
    private string? _successColor;
    private string? _dangerColor;
    private string? _warningColor;
    private string? _infoColor;
    private string? _fontFamily;
    private string? _fontMono;
    private string? _radiusSm;
    private string? _radiusMd;
    private string? _radiusLg;
    private string? _radiusFull;
    private string? _additionalCss;

    private ThemeBuilder() { }

    public static ThemeBuilder Create() => new();

    public ThemeBuilder WithId(string id) { _id = id; return this; }
    public ThemeBuilder WithName(string name) { _name = name; return this; }
    public ThemeBuilder WithDescription(string d) { _description = d; return this; }
    public ThemeBuilder WithAuthor(string author) { _author = author; return this; }
    public ThemeBuilder WithAdditionalCss(string css) { _additionalCss = css; return this; }

    public ThemeBuilder WithPrimaryColor(string light, string? dark = null)
    {
        _primaryColor = light;
        _primaryColorDark = dark ?? light;
        return this;
    }

    public ThemeBuilder WithSuccessColor(string color) { _successColor = color; return this; }
    public ThemeBuilder WithDangerColor(string color) { _dangerColor = color; return this; }
    public ThemeBuilder WithWarningColor(string color) { _warningColor = color; return this; }
    public ThemeBuilder WithInfoColor(string color) { _infoColor = color; return this; }

    public ThemeBuilder WithFontFamily(string font, string? mono = null)
    {
        _fontFamily = font;
        _fontMono = mono;
        return this;
    }

    public ThemeBuilder WithBorderRadius(
        string? sm = null,
        string? md = null,
        string? lg = null,
        string? full = null)
    {
        _radiusSm = sm;
        _radiusMd = md;
        _radiusLg = lg;
        _radiusFull = full;
        return this;
    }

    /// <summary>Rounded style (pill-buttons, large radius).</summary>
    public ThemeBuilder AsRounded()
    {
        _radiusSm = "8px";
        _radiusMd = "12px";
        _radiusLg = "16px";
        _radiusFull = "9999px";
        return this;
    }

    /// <summary>Sharp corners style.</summary>
    public ThemeBuilder AsSharp()
    {
        _radiusSm = "0";
        _radiusMd = "2px";
        _radiusLg = "4px";
        _radiusFull = "4px";
        return this;
    }

    public IThemeDefinition Build()
    {
        return new BuiltTheme(
            id: _id,
            name: _name,
            description: _description,
            author: _author,
            primary: _primaryColor,
            primaryDark: _primaryColorDark,
            success: _successColor,
            danger: _dangerColor,
            warning: _warningColor,
            info: _infoColor,
            font: _fontFamily,
            fontMono: _fontMono,
            radiusSm: _radiusSm,
            radiusMd: _radiusMd,
            radiusLg: _radiusLg,
            radiusFull: _radiusFull,
            additionalCss: _additionalCss
        );
    }
}

internal sealed class BuiltTheme : ThemeBase
{
    private readonly string _id, _name, _desc, _author;
    private readonly string _primary, _primaryDark;
    private readonly string? _success, _danger, _warning, _info;
    private readonly string? _font, _fontMono;
    private readonly string? _rSm, _rMd, _rLg, _rFull;
    private readonly string? _css;

    public BuiltTheme(string id, string name, string description, string author,
        string primary, string primaryDark,
        string? success, string? danger, string? warning, string? info,
        string? font, string? fontMono,
        string? radiusSm, string? radiusMd, string? radiusLg, string? radiusFull,
        string? additionalCss)
    {
        _id = id; _name = name; _desc = description; _author = author;
        _primary = primary; _primaryDark = primaryDark;
        _success = success; _danger = danger; _warning = warning; _info = info;
        _font = font; _fontMono = fontMono;
        _rSm = radiusSm; _rMd = radiusMd; _rLg = radiusLg; _rFull = radiusFull;
        _css = additionalCss;
    }

    public override string Id => _id;
    public override string Name => _name;
    public override string? Description => _desc;
    public override string? Author => _author;
    public override string Version => "custom";
    public override string? AdditionalCss => _css;

    protected override IThemePrimitives CreatePrimitives() => new DefaultPrimitives();

    protected override IThemeSemantic CreateLight()
    {
        var base_ = new DefaultSemanticLight();
        return new OverrideSemanticLight(base_,
            primary: _primary,
            success: _success,
            danger: _danger,
            warning: _warning,
            info: _info,
            font: _font,
            fontMono: _fontMono,
            rSm: _rSm, rMd: _rMd, rLg: _rLg, rFull: _rFull);
    }

    protected override IThemeSemantic? CreateDark()
    {
        var base_ = new DefaultSemanticDark();
        return new OverrideSemanticDark(base_,
            primary: _primaryDark,
            success: _success,
            danger: _danger,
            warning: _warning,
            info: _info,
            font: _font,
            fontMono: _fontMono,
            rSm: _rSm, rMd: _rMd, rLg: _rLg, rFull: _rFull);
    }

    protected override IThemeComponents? CreateComponents() => new DefaultComponents();
}

internal sealed class OverrideSemanticLight : DefaultSemanticLight
{
    private readonly string? _p, _s, _d, _w, _i, _f, _fm, _rSm, _rMd, _rLg, _rFull;

    public OverrideSemanticLight(DefaultSemanticLight _, string? primary,
        string? success, string? danger, string? warning, string? info,
        string? font, string? fontMono,
        string? rSm, string? rMd, string? rLg, string? rFull)
    {
        _p = primary; _s = success; _d = danger; _w = warning; _i = info;
        _f = font; _fm = fontMono;
        _rSm = rSm; _rMd = rMd; _rLg = rLg; _rFull = rFull;
    }

    public new string ColorPrimary => _p ?? base.ColorPrimary;
    public new string ColorSuccess => _s ?? base.ColorSuccess;
    public new string ColorDanger => _d ?? base.ColorDanger;
    public new string ColorWarning => _w ?? base.ColorWarning;
    public new string ColorInfo => _i ?? base.ColorInfo;
    public new string Font => _f ?? base.Font;
    public new string FontMono => _fm ?? base.FontMono;
    public new string RadiusSm => _rSm ?? base.RadiusSm;
    public new string RadiusMd => _rMd ?? base.RadiusMd;
    public new string RadiusLg => _rLg ?? base.RadiusLg;
    public new string RadiusFull => _rFull ?? base.RadiusFull;
}

internal sealed class OverrideSemanticDark : DefaultSemanticDark
{
    private readonly string? _p, _s, _d, _w, _i, _f, _fm, _rSm, _rMd, _rLg, _rFull;

    public OverrideSemanticDark(DefaultSemanticDark _, string? primary,
        string? success, string? danger, string? warning, string? info,
        string? font, string? fontMono,
        string? rSm, string? rMd, string? rLg, string? rFull)
    {
        _p = primary; _s = success; _d = danger; _w = warning; _i = info;
        _f = font; _fm = fontMono;
        _rSm = rSm; _rMd = rMd; _rLg = rLg; _rFull = rFull;
    }

    public new string ColorPrimary => _p ?? base.ColorPrimary;
    public new string ColorSuccess => _s ?? base.ColorSuccess;
    public new string ColorDanger => _d ?? base.ColorDanger;
    public new string ColorWarning => _w ?? base.ColorWarning;
    public new string ColorInfo => _i ?? base.ColorInfo;
    public new string Font => _f ?? base.Font;
    public new string FontMono => _fm ?? base.FontMono;
    public new string RadiusSm => _rSm ?? base.RadiusSm;
    public new string RadiusMd => _rMd ?? base.RadiusMd;
    public new string RadiusLg => _rLg ?? base.RadiusLg;
    public new string RadiusFull => _rFull ?? base.RadiusFull;
}
