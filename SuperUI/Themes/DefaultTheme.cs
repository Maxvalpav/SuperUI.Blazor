namespace SuperUI.Themes;

/// <summary>
/// Default SuperUI theme — provides backward compatibility.
/// </summary>
public sealed class DefaultTheme : ThemeBase
{
    public override string Id => "superui-default";
    public override string Name => "SuperUI Default";
    public override string? Description => "Стандартная тема SuperUI с поддержкой light/dark.";
    public override string Version => "2.0.0";

    protected override IThemePrimitives CreatePrimitives() => new DefaultPrimitives();
    protected override IThemeSemantic CreateLight() => new DefaultSemanticLight();
    protected override IThemeSemantic? CreateDark() => new DefaultSemanticDark();
    protected override IThemeComponents? CreateComponents() => new DefaultComponents();

    public override string? AdditionalCss => """
        /* Backward-compat aliases: --sui-* → --sg-* */
        :root,
        [data-theme="light"],
        [data-theme="dark"] {
            --sui-bg-primary:   var(--sg-bg);
            --sui-bg-secondary: var(--sg-bg-subtle);
            --sui-bg-tertiary:  var(--sg-bg-muted);

            --sui-text-primary:   var(--sg-fg);
            --sui-text-secondary: var(--sg-fg-subtle);
            --sui-text-muted:     var(--sg-fg-muted);
            --sui-text-disabled:  var(--sg-fg-disabled);

            --sui-border:       var(--sg-border);
            --sui-divider:      var(--sg-divider);
            --sui-border-hover: var(--sg-border-strong);
            --sui-border-focus: var(--sg-border-focus);

            --sui-accent:        var(--sg-color-primary);
            --sui-accent-hover:  var(--sg-color-primary-hover);
            --sui-accent-active: var(--sg-color-primary-active);

            --sui-success:        var(--sg-color-success);
            --sui-success-bg:     var(--sg-color-success-subtle);
            --sui-success-border: var(--sg-border-subtle);

            --sui-danger:        var(--sg-color-danger);
            --sui-danger-bg:     var(--sg-color-danger-subtle);
            --sui-danger-border: var(--sg-border-subtle);

            --sui-warn:        var(--sg-color-warning);
            --sui-warn-bg:     var(--sg-color-warning-subtle);
            --sui-warn-border: var(--sg-border-subtle);

            --sui-info:        var(--sg-color-info);
            --sui-info-bg:     var(--sg-color-info-subtle);
            --sui-info-border: var(--sg-border-subtle);

            --sui-shadow-sm: var(--sg-shadow-sm);
            --sui-shadow-md: var(--sg-shadow-md);
            --sui-shadow-lg: var(--sg-shadow-lg);

            --sui-overlay-bg: var(--sg-bg-overlay);
            --sui-glass-bg:     var(--sg-bg-glass);
            --sui-glass-border: var(--sg-border-glass);
            --sui-glass-blur:   var(--sg-blur-glass);

            --sui-hover-bg:    rgba(0, 0, 0, 0.04);
            --sui-active-bg:   rgba(0, 0, 0, 0.08);
            --sui-selected-bg: var(--sg-color-primary-muted);

            --sui-font-family:   var(--sg-font);
            --sui-font-size-xs:   var(--sg-text-xs);
            --sui-font-size-sm:   var(--sg-text-sm);
            --sui-font-size-base: var(--sg-text-base);
            --sui-font-size-lg:   var(--sg-text-lg);

            --sg-text-xs: 0.75rem;

            --sui-radius-sm:   var(--sg-radius-sm);
            --sui-radius-md:   var(--sg-radius-md);
            --sui-radius-lg:   var(--sg-radius-lg);
            --sui-radius-full: var(--sg-radius-full);
        }
        """;
}

internal class DefaultPrimitives : IThemePrimitives
{
    public string Neutral0 => "#ffffff";
    public string Neutral50 => "#f9fafb";
    public string Neutral100 => "#f3f4f6";
    public string Neutral200 => "#e5e7eb";
    public string Neutral300 => "#d1d5db";
    public string Neutral400 => "#9ca3af";
    public string Neutral500 => "#6b7280";
    public string Neutral600 => "#4b5563";
    public string Neutral700 => "#374151";
    public string Neutral800 => "#1f2937";
    public string Neutral900 => "#111827";

    public string Primary50 => "#eff6ff";
    public string Primary100 => "#dbeafe";
    public string Primary200 => "#bfdbfe";
    public string Primary300 => "#93c5fd";
    public string Primary400 => "#60a5fa";
    public string Primary500 => "#3b82f6";
    public string Primary600 => "#2563eb";
    public string Primary700 => "#1d4ed8";
    public string Primary800 => "#1e40af";
    public string Primary900 => "#1e3a8a";

    public string Success50 => "#f0fdf4";
    public string Success100 => "#dcfce7";
    public string Success500 => "#22c55e";
    public string Success600 => "#16a34a";
    public string Success700 => "#15803d";

    public string Danger50 => "#fef2f2";
    public string Danger100 => "#fee2e2";
    public string Danger500 => "#ef4444";
    public string Danger600 => "#dc2626";
    public string Danger700 => "#b91c1c";

    public string Warning50 => "#fffbeb";
    public string Warning100 => "#fef3c7";
    public string Warning500 => "#f59e0b";
    public string Warning600 => "#d97706";

    public string Info50 => "#ecfeff";
    public string Info100 => "#cffafe";
    public string Info500 => "#0ea5e9";
    public string Info600 => "#0284c7";

    public string FontSans => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public string FontMono => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public string FontSerif => "Georgia, 'Times New Roman', serif";

    public string RadiusNone => "0";
    public string RadiusXs => "2px";
    public string RadiusSm => "4px";
    public string RadiusMd => "6px";
    public string RadiusLg => "8px";
    public string RadiusXl => "12px";
    public string Radius2Xl => "16px";
    public string RadiusFull => "9999px";
}

internal class DefaultSemanticLight : IThemeSemantic
{
    public string BgDefault => "#ffffff";
    public string BgSubtle => "#f8fafc";
    public string BgMuted => "#f1f5f9";
    public string BgEmphasized => "#e2e8f0";
    public string BgOverlay => "rgba(0, 0, 0, 0.4)";
    public string BgGlass => "rgba(255, 255, 255, 0.6)";
    public string BorderGlass => "rgba(255, 255, 255, 0.5)";
    public string BlurGlass => "12px";

    public string Surface => "#ffffff";
    public string SurfaceRaised => "#ffffff";
    public string SurfaceOverlay => "#ffffff";

    public string FgDefault => "#1e293b";
    public string FgSubtle => "#64748b";
    public string FgMuted => "#94a3b8";
    public string FgDisabled => "#cbd5e1";
    public string FgInverse => "#ffffff";
    public string FgLink => "#006fee";
    public string FgLinkHover => "#3b82f6";

    public string BorderDefault => "#e2e8f0";
    public string BorderSubtle => "#f1f5f9";
    public string BorderStrong => "#cbd5e1";
    public string BorderFocus => "#006fee";
    public string Divider => "#f1f5f9";

    public string ColorPrimary => "#006fee";
    public string ColorPrimarySubtle => "#eff6ff";
    public string ColorPrimaryMuted => "#dbeafe";
    public string ColorPrimaryHover => "#3b82f6";
    public string ColorPrimaryActive => "#2563eb";
    public string ColorPrimaryFg => "#ffffff";

    public string ColorSuccess => "#52c41a";
    public string ColorSuccessSubtle => "#f6ffed";
    public string ColorSuccessHover => "#73d13d";
    public string ColorSuccessFg => "#ffffff";

    public string ColorDanger => "#ff4d4f";
    public string ColorDangerSubtle => "#fff1f0";
    public string ColorDangerHover => "#ff7875";
    public string ColorDangerFg => "#ffffff";

    public string ColorWarning => "#faad14";
    public string ColorWarningSubtle => "#fffbe6";
    public string ColorWarningHover => "#ffc53d";
    public string ColorWarningFg => "#ffffff";

    public string ColorInfo => "#1890ff";
    public string ColorInfoSubtle => "#e6f7ff";
    public string ColorInfoHover => "#40a9ff";
    public string ColorInfoFg => "#ffffff";

    public string Font => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    public string FontMono => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public string TextSm => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg => "1rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.05)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.1)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.1)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.1)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.1)";

    public string RadiusSm => "4px";
    public string RadiusMd => "6px";
    public string RadiusLg => "8px";
    public string RadiusXl => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public string TransitionBase => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing => "0 0 0 3px rgba(0, 111, 238, 0.2)";
    public string FocusRingDanger => "0 0 0 3px rgba(244, 63, 94, 0.2)";

    public int ZDropdown => 100;
    public int ZSticky => 200;
    public int ZModal => 300;
    public int ZToast => 400;
    public int ZTooltip => 500;
}

internal class DefaultSemanticDark : IThemeSemantic
{
    public string BgDefault => "#0a0a0a";
    public string BgSubtle => "#171717";
    public string BgMuted => "#262626";
    public string BgEmphasized => "#383838";
    public string BgOverlay => "rgba(0, 0, 0, 0.75)";
    public string BgGlass => "rgba(20, 20, 20, 0.6)";
    public string BorderGlass => "rgba(255, 255, 255, 0.15)";
    public string BlurGlass => "12px";

    public string Surface => "#171717";
    public string SurfaceRaised => "#1c1c1c";
    public string SurfaceOverlay => "#1c1c1c";

    public string FgDefault => "#fafafa";
    public string FgSubtle => "#a3a3a3";
    public string FgMuted => "#737373";
    public string FgDisabled => "#404040";
    public string FgInverse => "#0a0a0a";
    public string FgLink => "#60a5fa";
    public string FgLinkHover => "#93c5fd";

    public string BorderDefault => "#404040";
    public string BorderSubtle => "#262626";
    public string BorderStrong => "#525252";
    public string BorderFocus => "#3b82f6";
    public string Divider => "#1f1f1f";

    public string ColorPrimary => "#3b82f6";
    public string ColorPrimarySubtle => "rgba(59, 130, 246, 0.12)";
    public string ColorPrimaryMuted => "rgba(59, 130, 246, 0.20)";
    public string ColorPrimaryHover => "#60a5fa";
    public string ColorPrimaryActive => "#93c5fd";
    public string ColorPrimaryFg => "#ffffff";

    public string ColorSuccess => "#10b981";
    public string ColorSuccessSubtle => "rgba(16, 185, 129, 0.12)";
    public string ColorSuccessHover => "#34d399";
    public string ColorSuccessFg => "#ffffff";

    public string ColorDanger => "#f43f5e";
    public string ColorDangerSubtle => "rgba(244, 63, 94, 0.12)";
    public string ColorDangerHover => "#fb7185";
    public string ColorDangerFg => "#ffffff";

    public string ColorWarning => "#f59e0b";
    public string ColorWarningSubtle => "rgba(245, 158, 11, 0.12)";
    public string ColorWarningHover => "#fbbf24";
    public string ColorWarningFg => "#0a0a0a";

    public string ColorInfo => "#38bdf8";
    public string ColorInfoSubtle => "rgba(56, 189, 248, 0.12)";
    public string ColorInfoHover => "#7dd3fc";
    public string ColorInfoFg => "#ffffff";

    public string Font => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    public string FontMono => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public string TextSm => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg => "1rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.5)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.6)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.6)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.7)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.8)";

    public string RadiusSm => "4px";
    public string RadiusMd => "6px";
    public string RadiusLg => "8px";
    public string RadiusXl => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public string TransitionBase => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing => "0 0 0 2px rgba(59, 130, 246, 0.20), 0 0 0 4px #3b82f6";
    public string FocusRingDanger => "0 0 0 2px rgba(244, 63, 94, 0.20), 0 0 0 4px #f43f5e";

    public int ZDropdown => 100;
    public int ZSticky => 200;
    public int ZModal => 300;
    public int ZToast => 400;
    public int ZTooltip => 500;
}

internal class DefaultComponents : IThemeComponents
{
    public string BtnRadius => "6px";
    public string BtnFontSize => "0.8125rem";
    public string BtnFontWeight => "500";
    public string BtnHeight => "2rem";
    public string BtnHeightSm => "1.625rem";
    public string BtnHeightLg => "2.375rem";

    public string InputRadius => "6px";
    public string InputFontSize => "0.8125rem";
    public string InputHeight => "2rem";
    public string InputHeightSm => "1.625rem";
    public string InputHeightLg => "2.375rem";

    public string CardRadius => "12px";
    public string CardPadding => "16px";
    public string CardBorderColor => "var(--sg-border)";
    public string CardBg => "var(--sg-surface)";

    public string ModalRadius => "16px";

    public string TableRadius => "8px";
    public string TableHeaderFontWeight => "600";

    public string TabsIndicatorHeight => "2px";

    public string TooltipMaxWidth => "250px";

    public string HeaderBg => "var(--sg-color-primary)";
    public string HeaderFg => "#ffffff";
    public string NavBg => "#ffffff";
    public string NavFg => "var(--sg-fg)";
    public string NavActiveBg => "var(--sg-color-primary)";
    public string NavActiveFg => "#ffffff";
}

