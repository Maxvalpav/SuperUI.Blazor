namespace SuperUI.Themes;

/// <summary>
/// Semantic tokens of the theme — abstractions over primitives.
/// "What it means" instead of "what it is".
/// </summary>
public interface IThemeSemantic
{
    string BgDefault { get; }
    string BgSubtle { get; }
    string BgMuted { get; }
    string BgEmphasized { get; }
    string BgOverlay { get; }
    string BgGlass { get; }
    string BorderGlass { get; }
    string BlurGlass { get; }

    string Surface { get; }
    string SurfaceRaised { get; }
    string SurfaceOverlay { get; }

    string FgDefault { get; }
    string FgSubtle { get; }
    string FgMuted { get; }
    string FgDisabled { get; }
    string FgInverse { get; }
    string FgLink { get; }
    string FgLinkHover { get; }

    string BorderDefault { get; }
    string BorderSubtle { get; }
    string BorderStrong { get; }
    string BorderFocus { get; }
    string Divider { get; }

    string ColorPrimary { get; }
    string ColorPrimarySubtle { get; }
    string ColorPrimaryMuted { get; }
    string ColorPrimaryHover { get; }
    string ColorPrimaryActive { get; }
    string ColorPrimaryFg { get; }

    string ColorSuccess { get; }
    string ColorSuccessSubtle { get; }
    string ColorSuccessHover { get; }
    string ColorSuccessFg { get; }

    string ColorDanger { get; }
    string ColorDangerSubtle { get; }
    string ColorDangerHover { get; }
    string ColorDangerFg { get; }

    string ColorWarning { get; }
    string ColorWarningSubtle { get; }
    string ColorWarningHover { get; }
    string ColorWarningFg { get; }

    string ColorInfo { get; }
    string ColorInfoSubtle { get; }
    string ColorInfoHover { get; }
    string ColorInfoFg { get; }

    string Font { get; }
    string FontMono { get; }
    string TextSm { get; }
    string TextBase { get; }
    string TextLg { get; }

    string ShadowXs { get; }
    string ShadowSm { get; }
    string ShadowMd { get; }
    string ShadowLg { get; }
    string ShadowXl { get; }

    string RadiusSm { get; }
    string RadiusMd { get; }
    string RadiusLg { get; }
    string RadiusXl { get; }
    string RadiusFull { get; }

    string TransitionFast { get; }
    string TransitionBase { get; }
    string TransitionSlow { get; }

    string FocusRing { get; }
    string FocusRingDanger { get; }

    int ZDropdown { get; }
    int ZSticky { get; }
    int ZModal { get; }
    int ZToast { get; }
    int ZTooltip { get; }
}
