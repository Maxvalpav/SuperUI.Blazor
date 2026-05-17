namespace SuperUI.Themes;

/// <summary>
/// Material Design 3 inspired theme for SuperUI.
/// </summary>
public sealed class MaterialTheme : ThemeBase
{
    public override string Id => "material-design-3";
    public override string Name => "Material Design 3";
    public override string? Description => "Material 3 style with purple palette and rounded shapes.";
    public override string Version => "3.0.0";

    protected override IThemePrimitives CreatePrimitives() => new MaterialPrimitives();
    protected override IThemeSemantic CreateLight() => new MaterialSemanticLight();
    protected override IThemeSemantic? CreateDark() => new MaterialSemanticDark();
    protected override IThemeComponents? CreateComponents() => new MaterialComponents();

    public override string? AdditionalCss => """
        /* Input — Material outlined style */
        [data-theme-id="material-design-3"] .sgc-input-wrap {
            position: relative;
        }

        [data-theme-id="material-design-3"] .sgc-input {
            border-radius: 4px;
            padding-top:   1.25rem;
            padding-bottom: 0.5rem;
        }

        /* Chip */
        [data-theme-id="material-design-3"] .sg-badge {
            border-radius: 8px;
            font-weight: 500;
            letter-spacing: 0.00625em;
        }

        /* Card — Material filled card */
        [data-theme-id="material-design-3"] .sgc-card {
            background: var(--sg-bg-subtle);
            border: none;
            box-shadow: none;
        }
        """;
}

internal class MaterialPrimitives : DefaultPrimitives
{
    public new string Primary50  => "#F3EDF7";
    public new string Primary100 => "#E8DEF8";
    public new string Primary200 => "#CCC2DC";
    public new string Primary300 => "#B69DF8";
    public new string Primary400 => "#9A82DB";
    public new string Primary500 => "#7965AF";
    public new string Primary600 => "#6750A4";
    public new string Primary700 => "#4F378B";
    public new string Primary800 => "#381E72";
    public new string Primary900 => "#21005D";

    public new string RadiusXs   => "4px";
    public new string RadiusSm   => "8px";
    public new string RadiusMd   => "12px";
    public new string RadiusLg   => "16px";
    public new string RadiusXl   => "28px";
    public new string Radius2Xl  => "28px";
}

internal class MaterialSemanticLight : DefaultSemanticLight
{
    public new string ColorPrimary       => "#6750A4";
    public new string ColorPrimarySubtle => "#E8DEF8";
    public new string ColorPrimaryMuted  => "#CCC2DC";
    public new string ColorPrimaryHover  => "#4F378B";
    public new string ColorPrimaryActive => "#381E72";

    public new string RadiusSm   => "8px";
    public new string RadiusMd   => "12px";
    public new string RadiusLg   => "16px";
    public new string RadiusXl   => "28px";

    public new string ShadowSm => "0px 1px 2px rgba(0,0,0,0.3), 0px 1px 3px 1px rgba(0,0,0,0.15)";
    public new string ShadowMd => "0px 1px 2px rgba(0,0,0,0.3), 0px 2px 6px 2px rgba(0,0,0,0.15)";
    public new string ShadowLg => "0px 4px 8px 3px rgba(0,0,0,0.15), 0px 1px 3px rgba(0,0,0,0.3)";

    public new string Divider => "#CAC4D0"; // Material Outline Variant
}

internal class MaterialSemanticDark : DefaultSemanticDark
{
    public new string ColorPrimary       => "#D0BCFF";
    public new string ColorPrimarySubtle => "rgba(208, 188, 255, 0.12)";
    public new string ColorPrimaryMuted  => "rgba(208, 188, 255, 0.20)";
    public new string ColorPrimaryHover  => "#E8DEF8";
    public new string ColorPrimaryActive => "#F3EDF7";

    public new string Surface        => "#141218";
    public new string SurfaceRaised  => "#1C1B1F";
    public new string SurfaceOverlay => "#211F26";

    public new string BgDefault  => "#141218";
    public new string BgSubtle   => "#1C1B1F";
    public new string BgMuted    => "#211F26";

    public new string Divider => "#49454F"; // Material Outline Variant
}

internal class MaterialComponents : DefaultComponents
{
    public new string BtnRadius     => "20px";
    public new string BtnHeight     => "2.5rem";
    public new string BtnHeightSm  => "2rem";
    public new string BtnHeightLg  => "3rem";
    public new string BtnFontSize   => "0.875rem";
    public new string BtnFontWeight => "500";

    public new string InputRadius   => "4px";
    public new string InputHeight   => "3.5rem";
    public new string InputHeightSm => "3rem";
    public new string InputHeightLg => "4rem";

    public new string CardRadius    => "28px";
    public new string CardPadding   => "24px";
    public new string CardBorderColor => "var(--sg-border)";
    public new string CardBg        => "var(--sg-surface)";
    public new string ModalRadius   => "28px";

    public new string TabsIndicatorHeight => "3px";

    public new string TooltipMaxWidth => "240px";

    public new string HeaderBg => "var(--sg-surface)";
    public new string HeaderFg => "var(--sg-fg)";
    public new string NavBg => "var(--sg-surface)";
    public new string NavFg => "var(--sg-fg)";
    public new string NavActiveBg => "var(--sg-color-primary-subtle)";
    public new string NavActiveFg => "var(--sg-color-primary)";
}
