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
    string TextXs { get; }
    string TextSm { get; }
    string TextBase { get; }
    string TextLg { get; }
    string TextXl { get; }
    string Text2Xl { get; }
    string Text3Xl { get; }

    string FontWeightNormal { get; }
    string FontWeightMedium { get; }
    string FontWeightSemibold { get; }
    string FontWeightBold { get; }

    string LineHeightTight { get; }
    string LineHeightNormal { get; }
    string LineHeightRelaxed { get; }

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

    // ── v2.0 Organic proportional additions ────────────────────────

    // State tokens (per primary/success/danger/warning/info).
    string ColorPrimaryActiveBg { get; }
    string ColorPrimaryDisabled { get; }
    string ColorPrimaryDisabledBg { get; }
    string ColorPrimarySelectedBg { get; }

    string ColorSuccessActiveBg { get; }
    string ColorSuccessDisabled { get; }

    string ColorDangerActiveBg { get; }
    string ColorDangerDisabled { get; }

    string ColorWarningActiveBg { get; }
    string ColorWarningDisabled { get; }

    string ColorInfoActiveBg { get; }
    string ColorInfoDisabled { get; }

    // Interactive state for fg/border/surface.
    string FgPlaceholder { get; }
    string SurfaceHover { get; }
    string SurfaceActive { get; }
    string SurfaceSelected { get; }
    string BorderHover { get; }

    // Elevation scale.
    string Elevation1 { get; }
    string Elevation2 { get; }
    string Elevation3 { get; }
    string Elevation4 { get; }
    string Elevation5 { get; }

    // Motion (Fibonacci ms).
    string MotionInstant { get; }
    string MotionFast { get; }
    string MotionBase { get; }
    string MotionSlow { get; }
    string MotionSlower { get; }
    string EasingStandard { get; }
    string EasingEmphasis { get; }
    string EasingDecel { get; }

    // Density multipliers.
    string DensityCompact { get; }
    string DensityComfortable { get; }
    string DensitySpacious { get; }

    // Measure (ch).
    string MeasureNarrow { get; }
    string MeasureOptimal { get; }
    string MeasureWide { get; }
}
