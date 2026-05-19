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
            --sui-primary:       var(--sg-color-primary);
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

        /* ── Strict & Compact Default Theme Polish ────────── */
        
        [data-theme-id="superui-default"] {
            letter-spacing: -0.011em;
            -webkit-font-smoothing: antialiased;
        }

        /* Micro-twist: Subtle top-accent on cards */
        [data-theme-id="superui-default"] .sgc-card {
            border-top: 2px solid var(--sg-border);
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="superui-default"] .sgc-card:hover {
            border-top-color: var(--sg-color-primary);
            transform: translateY(-1px);
            box-shadow: var(--sg-shadow-md);
        }

        /* Compact Table tweaks */
        [data-theme-id="superui-default"] .sg-table {
            --sg-table-padding: 8px 12px;
        }

        /* Strict Sidebar */
        [data-theme-id="superui-default"] .sgc-nav-link {
            border-left: 3px solid transparent;
            margin: 1px 0;
            border-radius: 0;
            padding: 8px 16px;
        }

        [data-theme-id="superui-default"] .sgc-nav-link.active {
            background: var(--sg-bg-subtle);
            border-left-color: var(--sg-color-primary);
            font-weight: 600;
        }

        /* Effect: Gradient primary buttons */
        [data-theme-id="superui-default"] .sgc-btn-primary {
            background: linear-gradient(180deg, var(--sg-color-primary) 0%, var(--sg-color-primary-hover) 100%);
            border: 1px solid var(--sg-color-primary-active);
            box-shadow: inset 0 1px 0 rgba(255,255,255,0.15), var(--sg-shadow-xs);
        }

        [data-theme-id="superui-default"] .sgc-btn-primary:hover:not(:disabled) {
            filter: brightness(1.05);
            box-shadow: inset 0 1px 0 rgba(255,255,255,0.2), var(--sg-shadow-sm);
        }

        /* Modern Input focus effect */
        [data-theme-id="superui-default"] .sgc-input:focus,
        [data-theme-id="superui-default"] .sgc-select:focus {
            background: var(--sg-bg);
            border-color: var(--sg-color-primary);
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
    public string RadiusMd => "8px";
    public string RadiusLg => "12px";
    public string RadiusXl => "16px";
    public string Radius2Xl => "24px";
    public string RadiusFull => "9999px";
}

internal class DefaultSemanticLight : IThemeSemantic
{
    public string BgDefault => "#ffffff";
    public string BgSubtle => "#f9fafb";
    public string BgMuted => "#f3f4f6";
    public string BgEmphasized => "#e5e7eb";
    public string BgOverlay => "rgba(0, 0, 0, 0.4)";
    public string BgGlass => "rgba(255, 255, 255, 0.7)";
    public string BorderGlass => "rgba(255, 255, 255, 0.3)";
    public string BlurGlass => "8px";

    public string Surface => "#ffffff";
    public string SurfaceRaised => "#ffffff";
    public string SurfaceOverlay => "#ffffff";

    public string FgDefault => "#111827";
    public string FgSubtle => "#4b5563";
    public string FgMuted => "#6b7280";
    public string FgDisabled => "#9ca3af";
    public string FgInverse => "#ffffff";
    public string FgLink => "#2563eb";
    public string FgLinkHover => "#1d4ed8";

    public string BorderDefault => "#e5e7eb";
    public string BorderSubtle => "#f3f4f6";
    public string BorderStrong => "#d1d5db";
    public string BorderFocus => "#3b82f6";
    public string Divider => "#f3f4f6";

    public string ColorPrimary => "#3b82f6";
    public string ColorPrimarySubtle => "#eff6ff";
    public string ColorPrimaryMuted => "#dbeafe";
    public string ColorPrimaryHover => "#2563eb";
    public string ColorPrimaryActive => "#1d4ed8";
    public string ColorPrimaryFg => "#ffffff";

    public string ColorSuccess => "#10b981";
    public string ColorSuccessSubtle => "#f0fdf4";
    public string ColorSuccessHover => "#059669";
    public string ColorSuccessFg => "#ffffff";

    public string ColorDanger => "#f43f5e";
    public string ColorDangerSubtle => "#fef2f2";
    public string ColorDangerHover => "#e11d48";
    public string ColorDangerFg => "#ffffff";

    public string ColorWarning => "#f59e0b";
    public string ColorWarningSubtle => "#fffbeb";
    public string ColorWarningHover => "#d97706";
    public string ColorWarningFg => "#ffffff";

    public string ColorInfo => "#3b82f6";
    public string ColorInfoSubtle => "#eff6ff";
    public string ColorInfoHover => "#2563eb";
    public string ColorInfoFg => "#ffffff";

    public string Font => "'Inter', system-ui, sans-serif";
    public string FontMono => "'JetBrains Mono', monospace";
    public string TextSm => "0.75rem";   // 12px
    public string TextBase => "0.8125rem"; // 13px
    public string TextLg => "0.9375rem"; // 15px

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.05)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px -1px rgba(0, 0, 0, 0.1)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.1)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1)";

    public string RadiusSm => "2px"; // Stricter
    public string RadiusMd => "4px"; // Stricter
    public string RadiusLg => "6px"; // Stricter
    public string RadiusXl => "8px"; // Stricter
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "400ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing => "0 0 0 2px #ffffff, 0 0 0 4px #0ea5e9";
    public string FocusRingDanger => "0 0 0 2px #ffffff, 0 0 0 4px #f43f5e";

    public int ZDropdown => 1000;
    public int ZSticky => 1020;
    public int ZModal => 1050;
    public int ZToast => 1070;
    public int ZTooltip => 1100;
}

internal class DefaultSemanticDark : IThemeSemantic
{
    public string BgDefault => "#0f172a";
    public string BgSubtle => "#1e293b";
    public string BgMuted => "#334155";
    public string BgEmphasized => "#475569";
    public string BgOverlay => "rgba(0, 0, 0, 0.6)";
    public string BgGlass => "rgba(15, 23, 42, 0.7)";
    public string BorderGlass => "rgba(255, 255, 255, 0.1)";
    public string BlurGlass => "10px";

    public string Surface => "#1e293b";
    public string SurfaceRaised => "#334155";
    public string SurfaceOverlay => "#334155";

    public string FgDefault => "#f8fafc";
    public string FgSubtle => "#cbd5e1";
    public string FgMuted => "#94a3b8";
    public string FgDisabled => "#64748b";
    public string FgInverse => "#0f172a";
    public string FgLink => "#60a5fa";
    public string FgLinkHover => "#93c5fd";

    public string BorderDefault => "#334155";
    public string BorderSubtle => "#1e293b";
    public string BorderStrong => "#475569";
    public string BorderFocus => "#3b82f6";
    public string Divider => "#334155";

    public string ColorPrimary => "#3b82f6";
    public string ColorPrimarySubtle => "rgba(59, 130, 246, 0.1)";
    public string ColorPrimaryMuted => "rgba(59, 130, 246, 0.2)";
    public string ColorPrimaryHover => "#60a5fa";
    public string ColorPrimaryActive => "#93c5fd";
    public string ColorPrimaryFg => "#ffffff";

    public string ColorSuccess => "#34d399";
    public string ColorSuccessSubtle => "rgba(52, 211, 153, 0.1)";
    public string ColorSuccessHover => "#6ee7b7";
    public string ColorSuccessFg => "#064e3b";

    public string ColorDanger => "#fb7185";
    public string ColorDangerSubtle => "rgba(251, 113, 133, 0.1)";
    public string ColorDangerHover => "#fda4af";
    public string ColorDangerFg => "#ffffff";

    public string ColorWarning => "#fbbf24";
    public string ColorWarningSubtle => "rgba(251, 191, 36, 0.1)";
    public string ColorWarningHover => "#fcd34d";
    public string ColorWarningFg => "#451a03";

    public string ColorInfo => "#60a5fa";
    public string ColorInfoSubtle => "rgba(96, 165, 250, 0.1)";
    public string ColorInfoHover => "#93c5fd";
    public string ColorInfoFg => "#1e3a8a";

    public string Font => "'Inter', system-ui, sans-serif";
    public string FontMono => "'JetBrains Mono', monospace";
    public string TextSm => "0.75rem";
    public string TextBase => "0.8125rem";
    public string TextLg => "0.9375rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.5)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.6)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.6)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.7)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.8)";

    public string RadiusSm => "2px";
    public string RadiusMd => "4px";
    public string RadiusLg => "6px";
    public string RadiusXl => "8px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "400ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing => "0 0 0 2px #010409, 0 0 0 4px #38bdf8";
    public string FocusRingDanger => "0 0 0 2px #010409, 0 0 0 4px #fb7185";

    public int ZDropdown => 1000;
    public int ZSticky => 1020;
    public int ZModal => 1050;
    public int ZToast => 1070;
    public int ZTooltip => 1100;
}

internal class DefaultComponents : IThemeComponents
{
    public string BtnRadius => "4px";
    public string BtnFontSize => "0.75rem"; // 12px for compact look
    public string BtnFontWeight => "600";
    public string BtnHeight => "30px";     // Very compact
    public string BtnHeightSm => "24px";
    public string BtnHeightLg => "38px";

    public string InputRadius => "4px";
    public string InputFontSize => "0.8125rem";
    public string InputHeight => "30px";
    public string InputHeightSm => "24px";
    public string InputHeightLg => "38px";

    public string CardRadius => "4px"; // Strict
    public string CardPadding => "12px"; // Compact
    public string CardBorderColor => "var(--sg-border)";
    public string CardBg => "var(--sg-surface)";

    public string ModalRadius => "8px";

    public string TableRadius => "4px";
    public string TableHeaderFontWeight => "700";

    public string TabsIndicatorHeight => "2px";
    public string TooltipMaxWidth => "240px";

    public string HeaderBg => "var(--sg-bg)";
    public string HeaderFg => "var(--sg-fg)";
    public string NavBg => "var(--sg-bg-subtle)";
    public string NavFg => "var(--sg-fg-subtle)";
    public string NavActiveBg => "var(--sg-color-primary-subtle)";
    public string NavActiveFg => "var(--sg-color-primary)";
}

