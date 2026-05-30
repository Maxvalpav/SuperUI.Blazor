namespace SuperUI.Themes;

/// <summary>
/// Base class for all SuperUI themes.
/// </summary>
public abstract class ThemeBase : IThemeDefinition
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public virtual string? Description => null;
    public virtual string? Author => "SuperUI";
    public virtual string Version => "1.0.0";

    private IThemePrimitives? _primitives;
    public IThemePrimitives Primitives => _primitives ??= CreatePrimitives();

    private IThemeSemantic? _light;
    public IThemeSemantic Light => _light ??= CreateLight();

    private IThemeSemantic? _dark;
    public IThemeSemantic? Dark => _dark ??= CreateDark();

    private IThemeComponents? _components;
    public IThemeComponents? Components => _components ??= CreateComponents();

    private IThemeTypography? _typography;
    public IThemeTypography? Typography => _typography ??= CreateTypography();

    public virtual string? AdditionalCss => null;

    protected abstract IThemePrimitives CreatePrimitives();
    protected abstract IThemeSemantic CreateLight();
    protected virtual IThemeSemantic? CreateDark() => null;
    protected virtual IThemeComponents? CreateComponents() => null;
    protected virtual IThemeTypography? CreateTypography() => null;

    public virtual string GenerateCss()
    {
        return SgThemeGenerator.GenerateFullThemeCss(this);
    }
}
