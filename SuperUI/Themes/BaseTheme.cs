namespace SuperUI.Themes;

/// <summary>
/// Base primitives for built-in themes. Inherited by <see cref="ThemeBuilder"/>-generated themes.
/// </summary>
internal class BasePrimitives : IThemePrimitives
{
    // Neutral — Tailwind Slate (calmer than Gray, the classic strict palette)
    public virtual string Neutral0   => "#ffffff";
    public virtual string Neutral50  => "#f8fafc";
    public virtual string Neutral100 => "#f1f5f9";
    public virtual string Neutral200 => "#e2e8f0";
    public virtual string Neutral300 => "#cbd5e1";
    public virtual string Neutral400 => "#94a3b8";
    public virtual string Neutral500 => "#64748b";
    public virtual string Neutral600 => "#475569";
    public virtual string Neutral700 => "#334155";
    public virtual string Neutral800 => "#1e293b";
    public virtual string Neutral900 => "#0f172a";

    // Primary — Azure Blue (pure blue without violet tint)
    public virtual string Primary50  => "#eef6ff";
    public virtual string Primary100 => "#d8e4ff";
    public virtual string Primary200 => "#b1c9ff";
    public virtual string Primary300 => "#89aeff";
    public virtual string Primary400 => "#6293ff";
    public virtual string Primary500 => "#4f86ff";
    public virtual string Primary600 => "#3B78FF";
    public virtual string Primary700 => "#2f60cc";
    public virtual string Primary800 => "#234899";
    public virtual string Primary900 => "#183066";

    // Success — Emerald
    public virtual string Success50  => "#ecfdf5";
    public virtual string Success100 => "#d1fae5";
    public virtual string Success500 => "#10b981";
    public virtual string Success600 => "#059669";
    public virtual string Success700 => "#047857";

    // Danger — Red 600 (strict, not rose)
    public virtual string Danger50  => "#fef2f2";
    public virtual string Danger100 => "#fee2e2";
    public virtual string Danger500 => "#ef4444";
    public virtual string Danger600 => "#dc2626";
    public virtual string Danger700 => "#b91c1c";

    // Warning — Amber
    public virtual string Warning50  => "#fffbeb";
    public virtual string Warning100 => "#fef3c7";
    public virtual string Warning500 => "#f59e0b";
    public virtual string Warning600 => "#d97706";

    // Info — Sky (cooler than Primary, distinguishable on info banners)
    public virtual string Info50  => "#f0f9ff";
    public virtual string Info100 => "#e0f2fe";
    public virtual string Info500 => "#0ea5e9";
    public virtual string Info600 => "#0284c7";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    // φ/Fibonacci radii — organic proportions, compact scale
    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";    // fib-1
    public virtual string RadiusSm   => "5px";    // fib-2
    public virtual string RadiusMd   => "8px";    // fib-3
    public virtual string RadiusLg   => "13px";   // fib-4
    public virtual string RadiusXl   => "21px";   // fib-5
    public virtual string Radius2Xl  => "34px";   // fib-6
    public virtual string RadiusFull => "9999px";

    // ── v2.0 additional font slots ────────────────────────────────
    public virtual string FontDisplay => FontSans;
    public virtual string FontMedical => FontMono;

    // ── v2.0 Fibonacci spacing scale (px) ─────────────────────────
    public virtual string Spacing0 => "0";
    public virtual string Spacing1 => "2px";      // fib-1
    public virtual string Spacing2 => "3px";      // fib-2
    public virtual string Spacing3 => "5px";      // fib-3
    public virtual string Spacing4 => "8px";      // fib-4
    public virtual string Spacing5 => "13px";     // fib-5
    public virtual string Spacing6 => "21px";     // fib-6
    public virtual string Spacing7 => "34px";     // fib-7
    public virtual string Spacing8 => "55px";     // fib-8

    // ── v2.0 Icon size scale (px) ─────────────────────────────────
    public virtual string IconSizeSm  => "8px";
    public virtual string IconSizeMd  => "13px";
    public virtual string IconSizeLg  => "21px";
    public virtual string IconSizeXl  => "34px";
    public virtual string IconSize2Xl => "55px";

    // ── v2.0 Border width scale ───────────────────────────────────
    public virtual string BorderWidthDefault => "1px";
    public virtual string BorderWidthStrong  => "2px";
    public virtual string BorderWidthAccent  => "3px";
}

/// <summary>
/// Base light semantic colors for built-in themes. Inherited by <see cref="ThemeBuilder"/>-generated themes.
/// </summary>
internal class BaseSemanticLight : IThemeSemantic
{
    // Surfaces — clean slate-based
    public virtual string BgDefault     => "#ffffff";
    public virtual string BgSubtle      => "#f8fafc";   // slate-50
    public virtual string BgMuted       => "#f1f5f9";   // slate-100
    public virtual string BgEmphasized  => "#e2e8f0";   // slate-200
    public virtual string BgOverlay     => "rgba(15, 23, 42, 0.45)";
    public virtual string BgGlass       => "rgba(255, 255, 255, 0.7)";
    public virtual string BorderGlass   => "rgba(255, 255, 255, 0.3)";
    public virtual string BlurGlass     => "8px";

    public virtual string Surface         => "#ffffff";
    public virtual string SurfaceRaised   => "#ffffff";
    public virtual string SurfaceOverlay  => "#ffffff";

    // Text — slate scale
    public virtual string FgDefault   => "#0f172a";    // slate-900
    public virtual string FgSubtle    => "#475569";    // slate-600
    public virtual string FgMuted     => "#64748b";    // slate-500
    public virtual string FgDisabled  => "#94a3b8";    // slate-400
    public virtual string FgInverse   => "#ffffff";
    public virtual string FgLink      => "#3B78FF";    // azure blue
    public virtual string FgLinkHover => "#2f60cc";    // azure blue hover

    // Borders
    public virtual string BorderDefault => "#e2e8f0";  // slate-200
    public virtual string BorderSubtle  => "#f1f5f9";  // slate-100
    public virtual string BorderStrong  => "#cbd5e1";  // slate-300
    public virtual string BorderFocus   => "#3B78FF";  // azure blue
    public virtual string Divider       => "#f1f5f9";  // slate-100

    // Primary — Azure Blue
    public virtual string ColorPrimary        => "#3B78FF";
    public virtual string ColorPrimarySubtle  => "#eef6ff";
    public virtual string ColorPrimaryMuted   => "#d8e4ff";
    public virtual string ColorPrimaryHover   => "#2f60cc";
    public virtual string ColorPrimaryActive  => "#234899";
    public virtual string ColorPrimaryFg      => "#ffffff";

    // Success — Emerald 600
    public virtual string ColorSuccess        => "#059669";
    public virtual string ColorSuccessSubtle  => "#ecfdf5";
    public virtual string ColorSuccessHover   => "#047857";
    public virtual string ColorSuccessFg      => "#ffffff";

    // Danger — Red 600 (strict)
    public virtual string ColorDanger         => "#dc2626";
    public virtual string ColorDangerSubtle   => "#fef2f2";
    public virtual string ColorDangerHover    => "#b91c1c";
    public virtual string ColorDangerFg       => "#ffffff";

    // Warning — Amber 600
    public virtual string ColorWarning        => "#d97706";
    public virtual string ColorWarningSubtle  => "#fffbeb";
    public virtual string ColorWarningHover   => "#b45309";
    public virtual string ColorWarningFg      => "#ffffff";

    // Info — Sky 600
    public virtual string ColorInfo           => "#0284c7";
    public virtual string ColorInfoSubtle     => "#f0f9ff";
    public virtual string ColorInfoHover      => "#0369a1";
    public virtual string ColorInfoFg         => "#ffffff";

    public virtual string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextXs   => "0.6875rem";   // 11px
    public virtual string TextSm   => "0.75rem";     // 12px
    public virtual string TextBase => "0.8125rem";   // 13px (compact)
    public virtual string TextLg   => "0.9375rem";   // 15px
    public virtual string TextXl   => "1.125rem";    // 18px
    public virtual string Text2Xl  => "1.375rem";    // 22px
    public virtual string Text3Xl  => "1.75rem";     // 28px

    public virtual string FontWeightNormal   => "400";
    public virtual string FontWeightMedium   => "500";
    public virtual string FontWeightSemibold => "600";
    public virtual string FontWeightBold     => "700";

    public virtual string LineHeightTight   => "1.25";
    public virtual string LineHeightNormal  => "1.5";
    public virtual string LineHeightRelaxed => "1.75";

    public virtual string ShadowXs => "0 1px 1px 0 rgba(15, 23, 42, 0.04)";
    public virtual string ShadowSm => "0 1px 2px 0 rgba(15, 23, 42, 0.06), 0 1px 1px -1px rgba(15, 23, 42, 0.06)";
    public virtual string ShadowMd => "0 2px 4px -1px rgba(15, 23, 42, 0.08), 0 1px 2px -1px rgba(15, 23, 42, 0.06)";
    public virtual string ShadowLg => "0 8px 16px -4px rgba(15, 23, 42, 0.10), 0 2px 4px -2px rgba(15, 23, 42, 0.06)";
    public virtual string ShadowXl => "0 16px 32px -8px rgba(15, 23, 42, 0.14), 0 4px 8px -4px rgba(15, 23, 42, 0.08)";

    // φ/Fibonacci radii — organic proportions
    public virtual string RadiusSm   => "5px";    // fib-2
    public virtual string RadiusMd   => "8px";    // fib-3
    public virtual string RadiusLg   => "13px";   // fib-4
    public virtual string RadiusXl   => "21px";   // fib-5
    public virtual string RadiusFull => "9999px";

    // φ natural easing curves
    public virtual string TransitionFast => "120ms cubic-bezier(0.37, 0, 0.63, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.19, 1, 0.22, 1)";
    public virtual string TransitionSlow => "350ms cubic-bezier(0.19, 1, 0.22, 1)";

    public virtual string FocusRing       => "0 0 0 2px #ffffff, 0 0 0 4px #3B78FF";
    public virtual string FocusRingDanger => "0 0 0 2px #ffffff, 0 0 0 4px #dc2626";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;

    // ── v2.0 State tokens ─────────────────────────────────────────
    public virtual string ColorPrimaryActiveBg   => ColorPrimaryActive;
    public virtual string ColorPrimaryDisabled   => FgDisabled;
    public virtual string ColorPrimaryDisabledBg => BgMuted;
    public virtual string ColorPrimarySelectedBg => ColorPrimarySubtle;
    public virtual string ColorSuccessActiveBg   => ColorSuccessHover;
    public virtual string ColorSuccessDisabled   => FgDisabled;
    public virtual string ColorDangerActiveBg    => ColorDangerHover;
    public virtual string ColorDangerDisabled    => FgDisabled;
    public virtual string ColorWarningActiveBg   => ColorWarningHover;
    public virtual string ColorWarningDisabled   => FgDisabled;
    public virtual string ColorInfoActiveBg      => ColorInfoHover;
    public virtual string ColorInfoDisabled      => FgDisabled;
    public virtual string FgPlaceholder          => FgMuted;
    public virtual string SurfaceHover           => BgSubtle;
    public virtual string SurfaceActive          => BgMuted;
    public virtual string SurfaceSelected        => ColorPrimarySubtle;
    public virtual string BorderHover            => BorderStrong;

    // ── v2.0 Elevation scale (5 levels) ──────────────────────────
    public virtual string Elevation1 => ShadowXs;
    public virtual string Elevation2 => ShadowSm;
    public virtual string Elevation3 => ShadowMd;
    public virtual string Elevation4 => ShadowLg;
    public virtual string Elevation5 => ShadowXl;

    // ── v2.0 Motion (Fibonacci ms + easings) ──────────────────────
    public virtual string MotionInstant   => "89ms";
    public virtual string MotionFast      => "144ms";
    public virtual string MotionBase      => "233ms";
    public virtual string MotionSlow      => "377ms";
    public virtual string MotionSlower    => "610ms";
    public virtual string EasingStandard  => "cubic-bezier(0.4, 0, 0.2, 1)";
    public virtual string EasingEmphasis  => "cubic-bezier(0.2, 0, 0, 1)";
    public virtual string EasingDecel     => "cubic-bezier(0, 0, 0.2, 1)";

    // ── v2.0 Density (offset multipliers) ─────────────────────────
    public virtual string DensityCompact     => "-2px";
    public virtual string DensityComfortable => "0px";
    public virtual string DensitySpacious    => "+2px";

    // ── v2.0 Measure (ch) ─────────────────────────────────────────
    public virtual string MeasureNarrow  => "45ch";
    public virtual string MeasureOptimal => "66ch";
    public virtual string MeasureWide    => "75ch";
}

/// <summary>
/// Base dark semantic colors — deprecated. Use <see cref="BaseDarkConsistent"/> instead.
/// Preserved for backward compat with <see cref="ThemeBuilder"/>'s OverrideSemanticDark.
/// </summary>
internal class BaseSemanticDark : IThemeSemantic
{
    // Dark — layers and backgrounds (slate, strict)
    public virtual string BgDefault     => "oklch(0.11 0.008 240)";
    public virtual string BgSubtle      => "oklch(0.15 0.010 240)";
    public virtual string BgMuted       => "oklch(0.18 0.012 240)";
    public virtual string BgEmphasized  => "oklch(0.22 0.014 240)";
    public virtual string BgOverlay     => "oklch(0 0 0 / 0.72)";
    public virtual string BgGlass       => "oklch(0.11 0.008 240 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.99 0 0 / 0.10)";
    public virtual string BlurGlass     => "12px";

    public virtual string Surface         => "oklch(0.16 0.012 240)";
    public virtual string SurfaceRaised   => "oklch(0.20 0.014 240)";
    public virtual string SurfaceOverlay  => "oklch(0.20 0.014 240)";

    // Foreground — WCAG-optimized
    public virtual string FgDefault   => "oklch(0.95 0.005 240)";  // >15:1
    public virtual string FgSubtle    => "oklch(0.82 0.008 240)";  // ~10:1 AAA
    public virtual string FgMuted     => "oklch(0.65 0.010 240)";  // ~7:1  AAA
    public virtual string FgDisabled  => "oklch(0.55 0.010 240)";  // ~4.5:1 AA
    public virtual string FgInverse   => "oklch(0.11 0.008 240)";
    public virtual string FgLink      => "oklch(0.62 0.20 240)";
    public virtual string FgLinkHover => "oklch(0.67 0.18 240)";

    // Borders
    public virtual string BorderDefault => "oklch(0.25 0.016 240)";
    public virtual string BorderSubtle  => "oklch(0.18 0.012 240)";
    public virtual string BorderStrong  => "oklch(0.32 0.018 240)";
    public virtual string BorderFocus   => "oklch(0.62 0.20 240)";
    public virtual string Divider       => "oklch(0.18 0.012 240)";

    // Primary — Azure Blue
    public virtual string ColorPrimary        => "oklch(0.62 0.20 240)";
    public virtual string ColorPrimarySubtle  => "oklch(0.22 0.05 240)";
    public virtual string ColorPrimaryMuted   => "oklch(0.30 0.08 240)";
    public virtual string ColorPrimaryHover   => "oklch(0.67 0.18 240)";
    public virtual string ColorPrimaryActive  => "oklch(0.57 0.20 240)";
    public virtual string ColorPrimaryFg      => "oklch(0.98 0 0)";

    public virtual string ColorSuccess        => "oklch(0.627 0.194 153.2)";
    public virtual string ColorSuccessSubtle  => "oklch(0.22 0.05 153)";
    public virtual string ColorSuccessHover   => "oklch(0.70 0.18 153)";
    public virtual string ColorSuccessFg      => "oklch(0.98 0 0)";

    public virtual string ColorDanger         => "oklch(0.58 0.22 22)";
    public virtual string ColorDangerSubtle   => "oklch(0.24 0.06 22)";
    public virtual string ColorDangerHover    => "oklch(0.65 0.20 22)";
    public virtual string ColorDangerFg       => "oklch(0.98 0 0)";

    public virtual string ColorWarning        => "oklch(0.74 0.16 75)";
    public virtual string ColorWarningSubtle  => "oklch(0.26 0.05 75)";
    public virtual string ColorWarningHover   => "oklch(0.80 0.14 75)";
    public virtual string ColorWarningFg      => "oklch(0.11 0.008 240)";

    public virtual string ColorInfo           => "oklch(0.55 0.15 254)";
    public virtual string ColorInfoSubtle     => "oklch(0.22 0.05 254)";
    public virtual string ColorInfoHover      => "oklch(0.60 0.14 254)";
    public virtual string ColorInfoFg         => "oklch(0.98 0 0)";

    public virtual string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextXs   => "0.6875rem";
    public virtual string TextSm   => "0.75rem";
    public virtual string TextBase => "0.8125rem";
    public virtual string TextLg   => "0.9375rem";
    public virtual string TextXl   => "1.125rem";
    public virtual string Text2Xl  => "1.375rem";
    public virtual string Text3Xl  => "1.75rem";

    public virtual string FontWeightNormal   => "400";
    public virtual string FontWeightMedium   => "500";
    public virtual string FontWeightSemibold => "600";
    public virtual string FontWeightBold     => "700";

    public virtual string LineHeightTight   => "1.25";
    public virtual string LineHeightNormal  => "1.5";
    public virtual string LineHeightRelaxed => "1.75";

    public virtual string ShadowXs => "0 1px 2px 0 oklch(0 0 0 / 0.40)";
    public virtual string ShadowSm => "0 2px 4px -1px oklch(0 0 0 / 0.50)";
    public virtual string ShadowMd => "0 4px 12px -2px oklch(0 0 0 / 0.55)";
    public virtual string ShadowLg => "0 8px 24px -4px oklch(0 0 0 / 0.60)";
    public virtual string ShadowXl => "0 16px 48px -8px oklch(0 0 0 / 0.65)";

    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "120ms cubic-bezier(0.37, 0, 0.63, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.19, 1, 0.22, 1)";
    public virtual string TransitionSlow => "350ms cubic-bezier(0.19, 1, 0.22, 1)";

    public virtual string FocusRing       => "0 0 0 2px oklch(0.11 0.008 240), 0 0 0 4px oklch(0.62 0.20 240)";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(0.11 0.008 240), 0 0 0 4px oklch(0.58 0.22 22)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;

    // ── v2.0 State tokens ─────────────────────────────────────────
    public virtual string ColorPrimaryActiveBg   => ColorPrimaryActive;
    public virtual string ColorPrimaryDisabled   => FgDisabled;
    public virtual string ColorPrimaryDisabledBg => BgMuted;
    public virtual string ColorPrimarySelectedBg => ColorPrimarySubtle;
    public virtual string ColorSuccessActiveBg   => ColorSuccessHover;
    public virtual string ColorSuccessDisabled   => FgDisabled;
    public virtual string ColorDangerActiveBg    => ColorDangerHover;
    public virtual string ColorDangerDisabled    => FgDisabled;
    public virtual string ColorWarningActiveBg   => ColorWarningHover;
    public virtual string ColorWarningDisabled   => FgDisabled;
    public virtual string ColorInfoActiveBg      => ColorInfoHover;
    public virtual string ColorInfoDisabled      => FgDisabled;
    public virtual string FgPlaceholder          => FgMuted;
    public virtual string SurfaceHover           => BgSubtle;
    public virtual string SurfaceActive          => BgMuted;
    public virtual string SurfaceSelected        => ColorPrimarySubtle;
    public virtual string BorderHover            => BorderStrong;

    // ── v2.0 Elevation scale (5 levels) ──────────────────────────
    public virtual string Elevation1 => ShadowXs;
    public virtual string Elevation2 => ShadowSm;
    public virtual string Elevation3 => ShadowMd;
    public virtual string Elevation4 => ShadowLg;
    public virtual string Elevation5 => ShadowXl;

    // ── v2.0 Motion (Fibonacci ms + easings) ──────────────────────
    public virtual string MotionInstant   => "89ms";
    public virtual string MotionFast      => "144ms";
    public virtual string MotionBase      => "233ms";
    public virtual string MotionSlow      => "377ms";
    public virtual string MotionSlower    => "610ms";
    public virtual string EasingStandard  => "cubic-bezier(0.4, 0, 0.2, 1)";
    public virtual string EasingEmphasis  => "cubic-bezier(0.2, 0, 0, 1)";
    public virtual string EasingDecel     => "cubic-bezier(0, 0, 0.2, 1)";

    // ── v2.0 Density (offset multipliers) ─────────────────────────
    public virtual string DensityCompact     => "-2px";
    public virtual string DensityComfortable => "0px";
    public virtual string DensitySpacious    => "+2px";

    // ── v2.0 Measure (ch) ─────────────────────────────────────────
    public virtual string MeasureNarrow  => "45ch";
    public virtual string MeasureOptimal => "66ch";
    public virtual string MeasureWide    => "75ch";
}

/// <summary>
/// Brand-consistent dark mode base.
/// All surfaces use elevation-based layering (lighter = closer to user).
/// Same brand hue as light mode, only lightness adjusted for dark background.
/// WCAG AAA contrast on foreground/muted, AA on disabled.
/// </summary>
internal class BaseDarkConsistent : IThemeSemantic
{
    private readonly double _hue;

    /// <param name="hue">Brand hue in degrees (same as light mode). Default 240° (cool base).</param>
    public BaseDarkConsistent(double hue = 240)
    {
        _hue = hue;
    }

    // ── Background — elevation 0 (deepest) ────────────────────────
    public virtual string BgDefault     => $"oklch(0.11 0.008 {_hue})";
    public virtual string BgSubtle      => $"oklch(0.15 0.010 {_hue})";
    public virtual string BgMuted       => $"oklch(0.18 0.012 {_hue})";
    public virtual string BgEmphasized  => $"oklch(0.22 0.014 {_hue})";
    public virtual string BgOverlay     => "oklch(0 0 0 / 0.72)";
    public virtual string BgGlass       => $"oklch(0.11 0.008 {_hue} / 0.7)";
    public virtual string BorderGlass   => "oklch(0.99 0 0 / 0.10)";
    public virtual string BlurGlass     => "12px";

    // ── Surfaces — elevation-based (lighter = higher elevation) ───
    public virtual string Surface         => $"oklch(0.16 0.012 {_hue})";
    public virtual string SurfaceRaised   => $"oklch(0.20 0.014 {_hue})";
    public virtual string SurfaceOverlay  => $"oklch(0.20 0.014 {_hue})";

    // ── Foreground — WCAG AAA optimized ───────────────────────────
    public virtual string FgDefault   => $"oklch(0.95 0.005 {_hue})";  // >15:1
    public virtual string FgSubtle    => $"oklch(0.82 0.008 {_hue})";  // ~10:1 AAA
    public virtual string FgMuted     => $"oklch(0.65 0.010 {_hue})";  // ~7:1  AAA
    public virtual string FgDisabled  => $"oklch(0.55 0.010 {_hue})";  // ~4.5:1 AA
    public virtual string FgInverse   => $"oklch(0.11 0.008 {_hue})";
    public virtual string FgLink      => $"oklch(0.62 0.20 {_hue})";
    public virtual string FgLinkHover => $"oklch(0.67 0.18 {_hue})";

    // ── Borders ──────────────────────────────────────────────────
    public virtual string BorderDefault => $"oklch(0.25 0.016 {_hue})";
    public virtual string BorderSubtle  => $"oklch(0.18 0.012 {_hue})";
    public virtual string BorderStrong  => $"oklch(0.32 0.018 {_hue})";
    public virtual string BorderFocus   => $"oklch(0.62 0.20 {_hue})";
    public virtual string Divider       => $"oklch(0.18 0.012 {_hue})";

    // ── Brand colors — same hue, lighter for dark bg ──────────────
    public virtual string ColorPrimary        => $"oklch(0.62 0.20 {_hue})";
    public virtual string ColorPrimarySubtle  => $"oklch(0.22 0.05 {_hue})";
    public virtual string ColorPrimaryMuted   => $"oklch(0.30 0.08 {_hue})";
    public virtual string ColorPrimaryHover   => $"oklch(0.67 0.18 {_hue})";
    public virtual string ColorPrimaryActive  => $"oklch(0.57 0.20 {_hue})";
    public virtual string ColorPrimaryFg      => "oklch(0.98 0 0)";

    // ── Semantic states ───────────────────────────────────────────
    public virtual string ColorSuccess        => "oklch(0.627 0.194 153.2)";
    public virtual string ColorSuccessSubtle  => "oklch(0.22 0.05 153)";
    public virtual string ColorSuccessHover   => "oklch(0.70 0.18 153)";
    public virtual string ColorSuccessFg      => "oklch(0.98 0 0)";

    public virtual string ColorDanger         => "oklch(0.58 0.22 22)";
    public virtual string ColorDangerSubtle   => "oklch(0.24 0.06 22)";
    public virtual string ColorDangerHover    => "oklch(0.65 0.20 22)";
    public virtual string ColorDangerFg       => "oklch(0.98 0 0)";

    public virtual string ColorWarning        => "oklch(0.74 0.16 75)";
    public virtual string ColorWarningSubtle  => "oklch(0.26 0.05 75)";
    public virtual string ColorWarningHover   => "oklch(0.80 0.14 75)";
    public virtual string ColorWarningFg      => $"oklch(0.11 0.008 {_hue})";

    public virtual string ColorInfo           => "oklch(0.55 0.15 254)";
    public virtual string ColorInfoSubtle     => "oklch(0.22 0.05 254)";
    public virtual string ColorInfoHover      => "oklch(0.60 0.14 254)";
    public virtual string ColorInfoFg         => "oklch(0.98 0 0)";

    // ── Typography ───────────────────────────────────────────────
    public virtual string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextXs   => "0.6875rem";
    public virtual string TextSm   => "0.75rem";
    public virtual string TextBase => "0.8125rem";
    public virtual string TextLg   => "0.9375rem";
    public virtual string TextXl   => "1.125rem";
    public virtual string Text2Xl  => "1.375rem";
    public virtual string Text3Xl  => "1.75rem";

    public virtual string FontWeightNormal   => "400";
    public virtual string FontWeightMedium   => "500";
    public virtual string FontWeightSemibold => "600";
    public virtual string FontWeightBold     => "700";

    public virtual string LineHeightTight   => "1.25";
    public virtual string LineHeightNormal  => "1.5";
    public virtual string LineHeightRelaxed => "1.75";

    // ── Shadows — pure black on dark bg for elevation feel ───────
    public virtual string ShadowXs => "0 1px 2px 0 oklch(0 0 0 / 0.40)";
    public virtual string ShadowSm => "0 2px 4px -1px oklch(0 0 0 / 0.50)";
    public virtual string ShadowMd => "0 4px 12px -2px oklch(0 0 0 / 0.55)";
    public virtual string ShadowLg => "0 8px 24px -4px oklch(0 0 0 / 0.60)";
    public virtual string ShadowXl => "0 16px 48px -8px oklch(0 0 0 / 0.65)";

    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "120ms cubic-bezier(0.37, 0, 0.63, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.19, 1, 0.22, 1)";
    public virtual string TransitionSlow => "350ms cubic-bezier(0.19, 1, 0.22, 1)";

    public virtual string FocusRing       => $"0 0 0 2px oklch(0.11 0.008 {_hue}), 0 0 0 4px oklch(0.62 0.20 {_hue})";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(0.11 0.008 240), 0 0 0 4px oklch(0.58 0.22 22)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;

    // ── v2.0 State tokens ─────────────────────────────────────────
    public virtual string ColorPrimaryActiveBg   => ColorPrimaryActive;
    public virtual string ColorPrimaryDisabled   => FgDisabled;
    public virtual string ColorPrimaryDisabledBg => BgMuted;
    public virtual string ColorPrimarySelectedBg => ColorPrimarySubtle;
    public virtual string ColorSuccessActiveBg   => ColorSuccessHover;
    public virtual string ColorSuccessDisabled   => FgDisabled;
    public virtual string ColorDangerActiveBg    => ColorDangerHover;
    public virtual string ColorDangerDisabled    => FgDisabled;
    public virtual string ColorWarningActiveBg   => ColorWarningHover;
    public virtual string ColorWarningDisabled   => FgDisabled;
    public virtual string ColorInfoActiveBg      => ColorInfoHover;
    public virtual string ColorInfoDisabled      => FgDisabled;
    public virtual string FgPlaceholder          => FgMuted;
    public virtual string SurfaceHover           => BgSubtle;
    public virtual string SurfaceActive          => BgMuted;
    public virtual string SurfaceSelected        => ColorPrimarySubtle;
    public virtual string BorderHover            => BorderStrong;

    // ── v2.0 Elevation scale (5 levels) ──────────────────────────
    public virtual string Elevation1 => ShadowXs;
    public virtual string Elevation2 => ShadowSm;
    public virtual string Elevation3 => ShadowMd;
    public virtual string Elevation4 => ShadowLg;
    public virtual string Elevation5 => ShadowXl;

    // ── v2.0 Motion (Fibonacci ms + easings) ──────────────────────
    public virtual string MotionInstant   => "89ms";
    public virtual string MotionFast      => "144ms";
    public virtual string MotionBase      => "233ms";
    public virtual string MotionSlow      => "377ms";
    public virtual string MotionSlower    => "610ms";
    public virtual string EasingStandard  => "cubic-bezier(0.4, 0, 0.2, 1)";
    public virtual string EasingEmphasis  => "cubic-bezier(0.2, 0, 0, 1)";
    public virtual string EasingDecel     => "cubic-bezier(0, 0, 0.2, 1)";

    // ── v2.0 Density (offset multipliers) ─────────────────────────
    public virtual string DensityCompact     => "-2px";
    public virtual string DensityComfortable => "0px";
    public virtual string DensitySpacious    => "+2px";

    // ── v2.0 Measure (ch) ─────────────────────────────────────────
    public virtual string MeasureNarrow  => "45ch";
    public virtual string MeasureOptimal => "66ch";
    public virtual string MeasureWide    => "75ch";
}

/// <summary>
/// Brand-consistent light mode base.
/// Warm off-white background reduces eye strain vs pure #fff (Blehm+ 2005).
/// Same brand hue as dark mode — only lightness adjusted for light background.
/// WCAG AAA contrast on fg/fg-subtle/fg-muted, AA on fg-disabled.
/// 60–30–10 balance: 60% neutral bg, 30% surfaces, 10% brand accent.
/// </summary>
internal class BaseLightConsistent : IThemeSemantic
{
    private readonly double _hue;

    /// <param name="hue">Brand hue in degrees (same as dark mode). Default 262° (Natura cool blue).</param>
    public BaseLightConsistent(double hue = 262)
    {
        _hue = hue;
    }

    // ── Background — warm off-white (60% tier) ─────────────────────
    public virtual string BgDefault     => "oklch(0.99 0.005 60)";      // warm off-white
    public virtual string BgSubtle      => "oklch(0.97 0.008 60)";      // slightly warmer
    public virtual string BgMuted       => "oklch(0.935 0.012 60)";     // muted section
    public virtual string BgEmphasized  => "oklch(0.89 0.016 60)";      // emphasized
    public virtual string BgOverlay     => $"oklch(0.14 0.02 {_hue} / 0.35)";
    public virtual string BgGlass       => "oklch(0.99 0.005 60 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.87 0.015 60 / 0.3)";
    public virtual string BlurGlass     => "12px";

    // ── Surfaces — pure white cards float above warm bg (30% tier) ─
    public virtual string Surface         => "oklch(1 0 0)";
    public virtual string SurfaceRaised   => "oklch(1 0 0)";
    public virtual string SurfaceOverlay  => "oklch(1 0 0)";

    // ── Foreground — WCAG AAA optimized ────────────────────────────
    public virtual string FgDefault   => $"oklch(0.14 0.020 {_hue})";   // >15:1
    public virtual string FgSubtle    => $"oklch(0.36 0.015 {_hue})";   // ~10:1 AAA
    public virtual string FgMuted     => $"oklch(0.52 0.012 {_hue})";   //  ~7:1 AAA
    public virtual string FgDisabled  => $"oklch(0.66 0.008 {_hue})";   // ~4.5:1 AA
    public virtual string FgInverse   => "oklch(0.99 0.005 60)";
    public virtual string FgLink      => $"oklch(0.56 0.22 {_hue})";
    public virtual string FgLinkHover => $"oklch(0.50 0.22 {_hue})";

    // ── Borders ────────────────────────────────────────────────────
    public virtual string BorderDefault => $"oklch(0.88 0.012 {_hue})";
    public virtual string BorderSubtle  => $"oklch(0.93 0.010 {_hue})";
    public virtual string BorderStrong  => $"oklch(0.80 0.015 {_hue})";
    public virtual string BorderFocus   => $"oklch(0.56 0.22 {_hue})";
    public virtual string Divider       => $"oklch(0.93 0.010 {_hue})";

    // ── Brand colors — same hue as dark mode (10% accent tier) ─────
    public virtual string ColorPrimary        => $"oklch(0.56 0.20 {_hue})";
    public virtual string ColorPrimarySubtle  => $"oklch(0.94 0.04 {_hue})";
    public virtual string ColorPrimaryMuted   => $"oklch(0.85 0.08 {_hue})";
    public virtual string ColorPrimaryHover   => $"oklch(0.50 0.20 {_hue})";
    public virtual string ColorPrimaryActive  => $"oklch(0.44 0.19 {_hue})";
    public virtual string ColorPrimaryFg      => "oklch(0.99 0 0)";

    // ── Semantic states ────────────────────────────────────────────
    public virtual string ColorSuccess        => "oklch(0.627 0.194 153.2)";
    public virtual string ColorSuccessSubtle  => "oklch(0.94 0.04 153)";
    public virtual string ColorSuccessHover   => "oklch(0.57 0.19 153)";
    public virtual string ColorSuccessFg      => "oklch(0.99 0 0)";

    public virtual string ColorDanger         => "oklch(0.552 0.244 19.3)";
    public virtual string ColorDangerSubtle   => "oklch(0.94 0.05 19)";
    public virtual string ColorDangerHover    => "oklch(0.50 0.25 19)";
    public virtual string ColorDangerFg       => "oklch(0.99 0 0)";

    public virtual string ColorWarning        => "oklch(0.767 0.181 83.1)";
    public virtual string ColorWarningSubtle  => "oklch(0.96 0.04 83)";
    public virtual string ColorWarningHover   => "oklch(0.70 0.18 83)";
    public virtual string ColorWarningFg      => $"oklch(0.14 0.020 {_hue})";

    public virtual string ColorInfo           => "oklch(0.55 0.15 254)";
    public virtual string ColorInfoSubtle     => "oklch(0.94 0.035 254)";
    public virtual string ColorInfoHover      => "oklch(0.50 0.15 254)";
    public virtual string ColorInfoFg         => "oklch(0.99 0 0)";

    // ── Typography ─────────────────────────────────────────────────
    public virtual string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextXs   => "0.6875rem";
    public virtual string TextSm   => "0.75rem";
    public virtual string TextBase => "0.8125rem";
    public virtual string TextLg   => "0.9375rem";
    public virtual string TextXl   => "1.125rem";
    public virtual string Text2Xl  => "1.375rem";
    public virtual string Text3Xl  => "1.75rem";

    public virtual string FontWeightNormal   => "400";
    public virtual string FontWeightMedium   => "500";
    public virtual string FontWeightSemibold => "600";
    public virtual string FontWeightBold     => "700";

    public virtual string LineHeightTight   => "1.25";
    public virtual string LineHeightNormal  => "1.5";
    public virtual string LineHeightRelaxed => "1.75";

    // ── Shadows — brand-tinted on light bg ─────────────────────────
    public virtual string ShadowXs => $"0 1px 1px 0 oklch(0.14 0.02 {_hue} / 0.04)";
    public virtual string ShadowSm => $"0 1px 2px 0 oklch(0.14 0.02 {_hue} / 0.06), 0 1px 1px -1px oklch(0.14 0.02 {_hue} / 0.06)";
    public virtual string ShadowMd => $"0 2px 4px -1px oklch(0.14 0.02 {_hue} / 0.08), 0 1px 2px -1px oklch(0.14 0.02 {_hue} / 0.06)";
    public virtual string ShadowLg => $"0 8px 16px -4px oklch(0.14 0.02 {_hue} / 0.10), 0 2px 4px -2px oklch(0.14 0.02 {_hue} / 0.06)";
    public virtual string ShadowXl => $"0 16px 32px -8px oklch(0.14 0.02 {_hue} / 0.14), 0 4px 8px -4px oklch(0.14 0.02 {_hue} / 0.08)";

    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "120ms cubic-bezier(0.37, 0, 0.63, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.19, 1, 0.22, 1)";
    public virtual string TransitionSlow => "350ms cubic-bezier(0.19, 1, 0.22, 1)";

    public virtual string FocusRing       => $"0 0 0 2px oklch(0.99 0.005 60), 0 0 0 4px oklch(0.56 0.20 {_hue})";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(0.99 0.005 60), 0 0 0 4px oklch(0.552 0.244 19.3)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;

    // ── v2.0 State tokens ─────────────────────────────────────────
    public virtual string ColorPrimaryActiveBg   => ColorPrimaryActive;
    public virtual string ColorPrimaryDisabled   => FgDisabled;
    public virtual string ColorPrimaryDisabledBg => BgMuted;
    public virtual string ColorPrimarySelectedBg => ColorPrimarySubtle;
    public virtual string ColorSuccessActiveBg   => ColorSuccessHover;
    public virtual string ColorSuccessDisabled   => FgDisabled;
    public virtual string ColorDangerActiveBg    => ColorDangerHover;
    public virtual string ColorDangerDisabled    => FgDisabled;
    public virtual string ColorWarningActiveBg   => ColorWarningHover;
    public virtual string ColorWarningDisabled   => FgDisabled;
    public virtual string ColorInfoActiveBg      => ColorInfoHover;
    public virtual string ColorInfoDisabled      => FgDisabled;
    public virtual string FgPlaceholder          => FgMuted;
    public virtual string SurfaceHover           => BgSubtle;
    public virtual string SurfaceActive          => BgMuted;
    public virtual string SurfaceSelected        => ColorPrimarySubtle;
    public virtual string BorderHover            => BorderStrong;

    // ── v2.0 Elevation scale (5 levels) ──────────────────────────
    public virtual string Elevation1 => ShadowXs;
    public virtual string Elevation2 => ShadowSm;
    public virtual string Elevation3 => ShadowMd;
    public virtual string Elevation4 => ShadowLg;
    public virtual string Elevation5 => ShadowXl;

    // ── v2.0 Motion (Fibonacci ms + easings) ──────────────────────
    public virtual string MotionInstant   => "89ms";
    public virtual string MotionFast      => "144ms";
    public virtual string MotionBase      => "233ms";
    public virtual string MotionSlow      => "377ms";
    public virtual string MotionSlower    => "610ms";
    public virtual string EasingStandard  => "cubic-bezier(0.4, 0, 0.2, 1)";
    public virtual string EasingEmphasis  => "cubic-bezier(0.2, 0, 0, 1)";
    public virtual string EasingDecel     => "cubic-bezier(0, 0, 0.2, 1)";

    // ── v2.0 Density (offset multipliers) ─────────────────────────
    public virtual string DensityCompact     => "-2px";
    public virtual string DensityComfortable => "0px";
    public virtual string DensitySpacious    => "+2px";

    // ── v2.0 Measure (ch) ─────────────────────────────────────────
    public virtual string MeasureNarrow  => "45ch";
    public virtual string MeasureOptimal => "66ch";
    public virtual string MeasureWide    => "75ch";
}

/// <summary>
/// Base component defaults for built-in themes. Inherited by <see cref="ThemeBuilder"/>-generated themes.
/// </summary>
internal class BaseComponents : IThemeComponents
{
    // φ/Fibonacci proportions — compact scale
    public virtual string BtnRadius     => "5px";    // fib-2
    public virtual string BtnFontSize   => "0.75rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "30px";
    public virtual string BtnHeightSm   => "24px";
    public virtual string BtnHeightLg   => "36px";
    public virtual string BtnPaddingX   => "8px";
    public virtual string BtnPaddingY   => "3px";
    public virtual string BtnGap        => "3px";
    public virtual string BtnIconSize   => "12px";
    public virtual string BtnMinWidth   => "55px";

    public virtual string InputRadius      => "3px";    // fib-1
    public virtual string InputFontSize    => "0.8125rem";
    public virtual string InputHeight      => "30px";
    public virtual string InputHeightSm    => "24px";
    public virtual string InputHeightLg    => "36px";
    public virtual string InputPaddingX    => "5px";
    public virtual string InputPaddingY    => "3px";
    public virtual string InputBorderWidth => "1px";
    public virtual string InputIconSize    => "12px";

    public virtual string SelectRadius   => "3px";
    public virtual string SelectFontSize => "0.8125rem";
    public virtual string SelectHeight   => "30px";
    public virtual string SelectHeightSm => "24px";
    public virtual string SelectHeightLg => "36px";
    public virtual string SelectPaddingX => "5px";
    public virtual string SelectIconSize => "12px";

    public virtual string CheckboxSize        => "13px";
    public virtual string CheckboxSizeSm      => "8px";
    public virtual string CheckboxSizeLg      => "21px";
    public virtual string CheckboxRadius      => "2px";
    public virtual string CheckboxIconSize    => "8px";
    public virtual string CheckboxBorderWidth => "1px";

    public virtual string SwitchWidth     => "34px";
    public virtual string SwitchHeight    => "21px";
    public virtual string SwitchThumbSize => "13px";
    public virtual string SwitchRadius    => "9999px";
    public virtual string SwitchPadding   => "2px";

    public virtual string CardRadius            => "5px";  // fib-2
    public virtual string CardPadding           => "8px";  // fib-3
    public virtual string CardPaddingSm         => "5px";
    public virtual string CardPaddingLg         => "13px";
    public virtual string CardBorderColor       => "var(--sg-border)";
    public virtual string CardBg                => "var(--sg-surface)";
    public virtual string CardHeaderFontWeight  => "600";
    public virtual string CardGap               => "5px";

    public virtual string ModalRadius        => "8px";     // fib-3
    public virtual string ModalWidth         => "377px";   // fib-13
    public virtual string ModalWidthSm       => "233px";
    public virtual string ModalWidthLg       => "610px";
    public virtual string ModalWidthXl       => "987px";
    public virtual string ModalPadding       => "13px";
    public virtual string ModalBackdropBlur  => "5px";

    public virtual string DropdownRadius       => "5px";
    public virtual string DropdownPadding      => "3px";
    public virtual string DropdownItemHeight   => "21px";
    public virtual string DropdownItemPaddingX => "8px";
    public virtual string DropdownItemPaddingY => "0";
    public virtual string DropdownGap          => "2px";

    public virtual string TooltipMaxWidth  => "233px";
    public virtual string TooltipRadius    => "3px";
    public virtual string TooltipPadding   => "5px 8px";
    public virtual string TooltipFontSize  => "0.75rem";
    public virtual string TooltipArrowSize => "3px";

    public virtual string TabsIndicatorHeight => "2px";
    public virtual string TabsRadius          => "3px";
    public virtual string TabsHeight          => "34px";
    public virtual string TabsPaddingX        => "8px";
    public virtual string TabsPaddingY        => "0";
    public virtual string TabsGap             => "2px";

    public virtual string TableRadius             => "5px";   // fib-2
    public virtual string TableHeaderFontWeight   => "600";
    public virtual string TableRowHeight          => "34px";
    public virtual string TableRowHeightSm        => "21px";
    public virtual string TableHeaderHeight       => "34px";
    public virtual string TableCellPaddingX       => "8px";
    public virtual string TableCellPaddingY       => "0";
    public virtual string TableBorderWidth        => "1px";

    public virtual string AlertRadius     => "5px";
    public virtual string AlertPadding    => "8px 13px";
    public virtual string AlertPaddingSm  => "5px 8px";
    public virtual string AlertIconSize   => "13px";
    public virtual string AlertGap        => "8px";

    public virtual string BadgeRadius     => "9999px";
    public virtual string BadgeHeight     => "13px";
    public virtual string BadgeHeightSm   => "8px";
    public virtual string BadgeHeightLg   => "21px";
    public virtual string BadgePaddingX   => "5px";
    public virtual string BadgeFontSize   => "0.625rem";
    public virtual string BadgeFontWeight => "600";

    public virtual string ChipRadius     => "9999px";
    public virtual string ChipHeight     => "21px";
    public virtual string ChipHeightSm   => "13px";
    public virtual string ChipHeightLg   => "34px";
    public virtual string ChipPaddingX   => "8px";
    public virtual string ChipGap        => "3px";
    public virtual string ChipIconSize   => "8px";

    public virtual string SpinnerSize         => "13px";
    public virtual string SpinnerSizeSm       => "8px";
    public virtual string SpinnerSizeLg       => "21px";
    public virtual string SpinnerBorderWidth  => "1px";
    public virtual string SpinnerTrackOpacity => "0.2";

    public virtual string ProgressHeight          => "5px";
    public virtual string ProgressHeightSm        => "2px";
    public virtual string ProgressHeightLg        => "8px";
    public virtual string ProgressRadius          => "9999px";
    public virtual string ProgressIndicatorRadius => "9999px";

    public virtual string HeaderBg         => "var(--sg-bg)";
    public virtual string HeaderFg         => "var(--sg-fg)";
    public virtual string NavBg            => "var(--sg-bg-subtle)";
    public virtual string NavFg            => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg      => "var(--sg-bg-subtle)";
    public virtual string NavActiveFg      => "var(--sg-color-primary)";
    public virtual string NavItemHeight     => "34px";
    public virtual string NavItemPaddingX  => "8px";
}
