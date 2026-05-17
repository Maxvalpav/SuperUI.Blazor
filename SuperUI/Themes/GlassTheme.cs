namespace SuperUI.Themes;

/// <summary>
/// Glassmorphism theme for SuperUI.
/// Features translucent surfaces, backdrop blurs, and vibrant accents.
/// </summary>
public sealed class GlassTheme : ThemeBase
{
    public override string Id => "superui-glass";
    public override string Name => "Glassmorphism";
    public override string? Description => "Современный стиль с эффектом матового стекла и яркими акцентами.";
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() => new GlassPrimitives();
    protected override IThemeSemantic CreateLight() => new GlassSemanticLight();
    protected override IThemeSemantic? CreateDark() => new GlassSemanticDark();
    protected override IThemeComponents? CreateComponents() => new GlassComponents();

    public override string? AdditionalCss => """
        /* Glassmorphism Global Effects */
        :root {
            --sg-glass-blur: blur(12px);
            --sg-glass-border: rgba(255, 255, 255, 0.2);
            --sg-glass-bg: rgba(255, 255, 255, 0.7);
        }

        [data-theme="dark"] {
            --sg-glass-border: rgba(255, 255, 255, 0.1);
            --sg-glass-bg: rgba(15, 23, 42, 0.6);
        }

        /* Applying glass effect to components */
        .sgc-card, 
        .sgc-modal-content, 
        .sgc-drawer-content,
        .sgc-popover-content,
        .sgc-dropdown-menu {
            backdrop-filter: var(--sg-glass-blur);
            -webkit-backdrop-filter: var(--sg-glass-blur);
            border: 1px solid var(--sg-glass-border) !important;
        }

        /* Specific component enhancements for glass style */
        .sgc-button.sgc-primary {
            box-shadow: 0 4px 15px rgba(0, 111, 238, 0.3);
        }

        .sgc-tabs-strip {
            background: var(--sg-bg-subtle);
            backdrop-filter: var(--sg-glass-blur);
            border-radius: var(--sg-radius-lg);
            padding: 4px;
        }
        """;
}

internal class GlassPrimitives : DefaultPrimitives
{
    // Keeping most primitives but can override colors if needed for better contrast with glass
}

internal class GlassSemanticLight : IThemeSemantic
{
    public string BgDefault => "rgba(248, 250, 252, 0.8)";
    public string BgSubtle => "rgba(241, 245, 249, 0.5)";
    public string BgMuted => "rgba(226, 232, 240, 0.4)";
    public string BgEmphasized => "rgba(203, 213, 225, 0.6)";
    public string BgOverlay => "rgba(15, 23, 42, 0.4)";

    public string Surface => "rgba(255, 255, 255, 0.7)";
    public string SurfaceRaised => "rgba(255, 255, 255, 0.85)";
    public string SurfaceOverlay => "rgba(255, 255, 255, 0.9)";

    public string FgDefault => "#0f172a";
    public string FgSubtle => "#475569";
    public string FgMuted => "#94a3b8";
    public string FgDisabled => "#cbd5e1";
    public string FgInverse => "#ffffff";
    public string FgLink => "#3b82f6";
    public string FgLinkHover => "#2563eb";

    public string BorderDefault => "rgba(203, 213, 225, 0.8)";
    public string BorderSubtle => "rgba(226, 232, 240, 0.6)";
    public string BorderStrong => "rgba(148, 163, 184, 1)";
    public string BorderFocus => "#3b82f6";
    public string Divider => "rgba(203, 213, 225, 0.4)";

    public string ColorPrimary => "#0ea5e9";
    public string ColorPrimarySubtle => "rgba(14, 165, 233, 0.1)";
    public string ColorPrimaryMuted => "rgba(14, 165, 233, 0.2)";
    public string ColorPrimaryHover => "#0284c7";
    public string ColorPrimaryActive => "#0369a1";
    public string ColorPrimaryFg => "#ffffff";

    public string ColorSuccess => "#10b981";
    public string ColorSuccessSubtle => "rgba(16, 185, 129, 0.1)";
    public string ColorSuccessHover => "#059669";
    public string ColorSuccessFg => "#ffffff";

    public string ColorDanger => "#f43f5e";
    public string ColorDangerSubtle => "rgba(244, 63, 94, 0.1)";
    public string ColorDangerHover => "#e11d48";
    public string ColorDangerFg => "#ffffff";

    public string ColorWarning => "#f59e0b";
    public string ColorWarningSubtle => "rgba(245, 158, 11, 0.1)";
    public string ColorWarningHover => "#d97706";
    public string ColorWarningFg => "#0f172a";

    public string ColorInfo => "#6366f1";
    public string ColorInfoSubtle => "rgba(99, 102, 241, 0.1)";
    public string ColorInfoHover => "#4f46e5";
    public string ColorInfoFg => "#ffffff";

    public string Font => "'Inter', sans-serif";
    public string FontMono => "'JetBrains Mono', monospace";
    public string TextSm => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg => "1rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.05)";
    public string ShadowSm => "0 4px 6px -1px rgba(0, 0, 0, 0.1)";
    public string ShadowMd => "0 10px 15px -3px rgba(0, 0, 0, 0.1)";
    public string ShadowLg => "0 20px 25px -5px rgba(0, 0, 0, 0.1)";
    public string ShadowXl => "0 25px 50px -12px rgba(0, 0, 0, 0.25)";

    public string RadiusSm => "8px";
    public string RadiusMd => "12px";
    public string RadiusLg => "16px";
    public string RadiusXl => "24px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms";
    public string TransitionBase => "200ms";
    public string TransitionSlow => "300ms";

    public string FocusRing => "0 0 0 3px rgba(14, 165, 233, 0.3)";
    public string FocusRingDanger => "0 0 0 3px rgba(244, 63, 94, 0.3)";

    public int ZDropdown => 1000;
    public int ZSticky => 1020;
    public int ZModal => 1050;
    public int ZToast => 1070;
    public int ZTooltip => 1100;
}

internal class GlassSemanticDark : IThemeSemantic
{
    public string BgDefault => "rgba(15, 23, 42, 0.9)";
    public string BgSubtle => "rgba(30, 41, 59, 0.6)";
    public string BgMuted => "rgba(51, 65, 85, 0.5)";
    public string BgEmphasized => "rgba(71, 85, 105, 0.4)";
    public string BgOverlay => "rgba(0, 0, 0, 0.7)";

    public string Surface => "rgba(30, 41, 59, 0.7)";
    public string SurfaceRaised => "rgba(51, 65, 85, 0.6)";
    public string SurfaceOverlay => "rgba(71, 85, 105, 0.8)";

    public string FgDefault => "#f8fafc";
    public string FgSubtle => "#cbd5e1";
    public string FgMuted => "#94a3b8";
    public string FgDisabled => "#475569";
    public string FgInverse => "#0f172a";
    public string FgLink => "#38bdf8";
    public string FgLinkHover => "#7dd3fc";

    public string BorderDefault => "rgba(71, 85, 105, 0.8)";
    public string BorderSubtle => "rgba(51, 65, 85, 0.6)";
    public string BorderStrong => "rgba(100, 116, 139, 1)";
    public string BorderFocus => "#38bdf8";
    public string Divider => "rgba(71, 85, 105, 0.4)";

    public string ColorPrimary => "#38bdf8";
    public string ColorPrimarySubtle => "rgba(56, 189, 248, 0.15)";
    public string ColorPrimaryMuted => "rgba(56, 189, 248, 0.25)";
    public string ColorPrimaryHover => "#7dd3fc";
    public string ColorPrimaryActive => "#bae6fd";
    public string ColorPrimaryFg => "#0f172a";

    public string ColorSuccess => "#34d399";
    public string ColorSuccessSubtle => "rgba(52, 211, 153, 0.15)";
    public string ColorSuccessHover => "#6ee7b7";
    public string ColorSuccessFg => "#0f172a";

    public string ColorDanger => "#fb7185";
    public string ColorDangerSubtle => "rgba(251, 113, 133, 0.15)";
    public string ColorDangerHover => "#fda4af";
    public string ColorDangerFg => "#0f172a";

    public string ColorWarning => "#fbbf24";
    public string ColorWarningSubtle => "rgba(251, 191, 36, 0.15)";
    public string ColorWarningHover => "#fcd34d";
    public string ColorWarningFg => "#0f172a";

    public string ColorInfo => "#818cf8";
    public string ColorInfoSubtle => "rgba(129, 140, 248, 0.15)";
    public string ColorInfoHover => "#a5b4fc";
    public string ColorInfoFg => "#0f172a";

    public string Font => "'Inter', sans-serif";
    public string FontMono => "'JetBrains Mono', monospace";
    public string TextSm => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg => "1rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.3)";
    public string ShadowSm => "0 4px 6px -1px rgba(0, 0, 0, 0.4)";
    public string ShadowMd => "0 10px 15px -3px rgba(0, 0, 0, 0.4)";
    public string ShadowLg => "0 20px 25px -5px rgba(0, 0, 0, 0.4)";
    public string ShadowXl => "0 25px 50px -12px rgba(0, 0, 0, 0.6)";

    public string RadiusSm => "8px";
    public string RadiusMd => "12px";
    public string RadiusLg => "16px";
    public string RadiusXl => "24px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms";
    public string TransitionBase => "200ms";
    public string TransitionSlow => "300ms";

    public string FocusRing => "0 0 0 3px rgba(56, 189, 248, 0.4)";
    public string FocusRingDanger => "0 0 0 3px rgba(251, 113, 133, 0.4)";

    public int ZDropdown => 1000;
    public int ZSticky => 1020;
    public int ZModal => 1050;
    public int ZToast => 1070;
    public int ZTooltip => 1100;
}

internal class GlassComponents : IThemeComponents
{
    public string BtnRadius => "12px";
    public string BtnFontSize => "0.875rem";
    public string BtnFontWeight => "600";
    public string BtnHeight => "40px";
    public string BtnHeightSm => "32px";
    public string BtnHeightLg => "48px";

    public string InputRadius => "12px";
    public string InputFontSize => "0.875rem";
    public string InputHeight => "40px";
    public string InputHeightSm => "32px";
    public string InputHeightLg => "48px";

    public string CardRadius => "24px";
    public string CardPadding => "24px";
    public string CardBorderColor => "var(--sg-glass-border)";
    public string CardBg => "var(--sg-glass-bg)";

    public string ModalRadius => "24px";

    public string TableRadius => "16px";
    public string TableHeaderFontWeight => "700";

    public string TabsIndicatorHeight => "2px";

    public string TooltipMaxWidth => "240px";

    public string HeaderBg => "var(--sg-glass-bg)";
    public string HeaderFg => "var(--sg-fg)";
    public string NavBg => "var(--sg-glass-bg)";
    public string NavFg => "var(--sg-fg-subtle)";
    public string NavActiveBg => "var(--sg-color-primary)";
    public string NavActiveFg => "var(--sg-color-primary-fg)";
}
