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
            -webkit-font-smoothing: antialiased;
        }

        /* Micro-twist: Subtle top-accent on cards */
        [data-theme-id="superui-default"] .sgc-card {
            border-top: 2px solid var(--sg-border);
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="superui-default"] .sgc-card:hover {
            border-top-color: var(--sg-color-primary);
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
    public virtual string Neutral0 => "#ffffff";
    public virtual string Neutral50 => "#f9fafb";
    public virtual string Neutral100 => "#f3f4f6";
    public virtual string Neutral200 => "#e5e7eb";
    public virtual string Neutral300 => "#d1d5db";
    public virtual string Neutral400 => "#9ca3af";
    public virtual string Neutral500 => "#6b7280";
    public virtual string Neutral600 => "#4b5563";
    public virtual string Neutral700 => "#374151";
    public virtual string Neutral800 => "#1f2937";
    public virtual string Neutral900 => "#111827";

    public virtual string Primary50 => "#eff6ff";
    public virtual string Primary100 => "#dbeafe";
    public virtual string Primary200 => "#bfdbfe";
    public virtual string Primary300 => "#93c5fd";
    public virtual string Primary400 => "#60a5fa";
    public virtual string Primary500 => "#3b82f6";
    public virtual string Primary600 => "#2563eb";
    public virtual string Primary700 => "#1d4ed8";
    public virtual string Primary800 => "#1e40af";
    public virtual string Primary900 => "#1e3a8a";

    public virtual string Success50 => "#f0fdf4";
    public virtual string Success100 => "#dcfce7";
    public virtual string Success500 => "#22c55e";
    public virtual string Success600 => "#16a34a";
    public virtual string Success700 => "#15803d";

    public virtual string Danger50 => "#fef2f2";
    public virtual string Danger100 => "#fee2e2";
    public virtual string Danger500 => "#ef4444";
    public virtual string Danger600 => "#dc2626";
    public virtual string Danger700 => "#b91c1c";

    public virtual string Warning50 => "#fffbeb";
    public virtual string Warning100 => "#fef3c7";
    public virtual string Warning500 => "#f59e0b";
    public virtual string Warning600 => "#d97706";

    public virtual string Info50 => "#ecfeff";
    public virtual string Info100 => "#cffafe";
    public virtual string Info500 => "#0ea5e9";
    public virtual string Info600 => "#0284c7";

    public virtual string FontSans => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs => "2px";
    public virtual string RadiusSm => "4px";
    public virtual string RadiusMd => "8px";
    public virtual string RadiusLg => "12px";
    public virtual string RadiusXl => "16px";
    public virtual string Radius2Xl => "24px";
    public virtual string RadiusFull => "9999px";
}

internal class DefaultSemanticLight : IThemeSemantic
{
    public virtual string BgDefault => "#ffffff";
    public virtual string BgSubtle => "#f9fafb";
    public virtual string BgMuted => "#f3f4f6";
    public virtual string BgEmphasized => "#e5e7eb";
    public virtual string BgOverlay => "rgba(0, 0, 0, 0.4)";
    public virtual string BgGlass => "rgba(255, 255, 255, 0.7)";
    public virtual string BorderGlass => "rgba(255, 255, 255, 0.3)";
    public virtual string BlurGlass => "8px";

    public virtual string Surface => "#ffffff";
    public virtual string SurfaceRaised => "#ffffff";
    public virtual string SurfaceOverlay => "#ffffff";

    public virtual string FgDefault => "#111827";
    public virtual string FgSubtle => "#4b5563";
    public virtual string FgMuted => "#6b7280";
    public virtual string FgDisabled => "#9ca3af";
    public virtual string FgInverse => "#ffffff";
    public virtual string FgLink => "#2563eb";
    public virtual string FgLinkHover => "#1d4ed8";

    public virtual string BorderDefault => "#e5e7eb";
    public virtual string BorderSubtle => "#f3f4f6";
    public virtual string BorderStrong => "#d1d5db";
    public virtual string BorderFocus => "#3b82f6";
    public virtual string Divider => "#f3f4f6";

    public virtual string ColorPrimary => "#3b82f6";
    public virtual string ColorPrimarySubtle => "#eff6ff";
    public virtual string ColorPrimaryMuted => "#dbeafe";
    public virtual string ColorPrimaryHover => "#2563eb";
    public virtual string ColorPrimaryActive => "#1d4ed8";
    public virtual string ColorPrimaryFg => "#ffffff";

    public virtual string ColorSuccess => "#10b981";
    public virtual string ColorSuccessSubtle => "#f0fdf4";
    public virtual string ColorSuccessHover => "#059669";
    public virtual string ColorSuccessFg => "#ffffff";

    public virtual string ColorDanger => "#f43f5e";
    public virtual string ColorDangerSubtle => "#fef2f2";
    public virtual string ColorDangerHover => "#e11d48";
    public virtual string ColorDangerFg => "#ffffff";

    public virtual string ColorWarning => "#f59e0b";
    public virtual string ColorWarningSubtle => "#fffbeb";
    public virtual string ColorWarningHover => "#d97706";
    public virtual string ColorWarningFg => "#ffffff";

    public virtual string ColorInfo => "#3b82f6";
    public virtual string ColorInfoSubtle => "#eff6ff";
    public virtual string ColorInfoHover => "#2563eb";
    public virtual string ColorInfoFg => "#ffffff";

    public virtual string Font => "'Inter', system-ui, sans-serif";
    public virtual string FontMono => "'JetBrains Mono', monospace";
    public virtual string TextSm => "0.75rem";   // 12px
    public virtual string TextBase => "0.8125rem"; // 13px
    public virtual string TextLg => "0.9375rem"; // 15px

    public virtual string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.05)";
    public virtual string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px -1px rgba(0, 0, 0, 0.1)";
    public virtual string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1)";
    public virtual string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.1)";
    public virtual string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1)";

    public virtual string RadiusSm => "2px"; // Stricter
    public virtual string RadiusMd => "4px"; // Stricter
    public virtual string RadiusLg => "6px"; // Stricter
    public virtual string RadiusXl => "8px"; // Stricter
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public virtual string TransitionSlow => "400ms cubic-bezier(0.4, 0, 0.2, 1)";

    public virtual string FocusRing => "0 0 0 2px #ffffff, 0 0 0 4px #0ea5e9";
    public virtual string FocusRingDanger => "0 0 0 2px #ffffff, 0 0 0 4px #f43f5e";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky => 1020;
    public virtual int ZModal => 1050;
    public virtual int ZToast => 1070;
    public virtual int ZTooltip => 1100;
}

internal class DefaultSemanticDark : IThemeSemantic
{
    public virtual string BgDefault => "#0f172a";
    public virtual string BgSubtle => "#1e293b";
    public virtual string BgMuted => "#334155";
    public virtual string BgEmphasized => "#475569";
    public virtual string BgOverlay => "rgba(0, 0, 0, 0.6)";
    public virtual string BgGlass => "rgba(15, 23, 42, 0.7)";
    public virtual string BorderGlass => "rgba(255, 255, 255, 0.1)";
    public virtual string BlurGlass => "10px";

    public virtual string Surface => "#1e293b";
    public virtual string SurfaceRaised => "#334155";
    public virtual string SurfaceOverlay => "#334155";

    public virtual string FgDefault => "#f8fafc";
    public virtual string FgSubtle => "#cbd5e1";
    public virtual string FgMuted => "#94a3b8";
    public virtual string FgDisabled => "#64748b";
    public virtual string FgInverse => "#0f172a";
    public virtual string FgLink => "#60a5fa";
    public virtual string FgLinkHover => "#93c5fd";

    public virtual string BorderDefault => "#334155";
    public virtual string BorderSubtle => "#1e293b";
    public virtual string BorderStrong => "#475569";
    public virtual string BorderFocus => "#3b82f6";
    public virtual string Divider => "#334155";

    public virtual string ColorPrimary => "#3b82f6";
    public virtual string ColorPrimarySubtle => "rgba(59, 130, 246, 0.1)";
    public virtual string ColorPrimaryMuted => "rgba(59, 130, 246, 0.2)";
    public virtual string ColorPrimaryHover => "#60a5fa";
    public virtual string ColorPrimaryActive => "#93c5fd";
    public virtual string ColorPrimaryFg => "#ffffff";

    public virtual string ColorSuccess => "#34d399";
    public virtual string ColorSuccessSubtle => "rgba(52, 211, 153, 0.1)";
    public virtual string ColorSuccessHover => "#6ee7b7";
    public virtual string ColorSuccessFg => "#064e3b";

    public virtual string ColorDanger => "#fb7185";
    public virtual string ColorDangerSubtle => "rgba(251, 113, 133, 0.1)";
    public virtual string ColorDangerHover => "#fda4af";
    public virtual string ColorDangerFg => "#ffffff";

    public virtual string ColorWarning => "#fbbf24";
    public virtual string ColorWarningSubtle => "rgba(251, 191, 36, 0.1)";
    public virtual string ColorWarningHover => "#fcd34d";
    public virtual string ColorWarningFg => "#451a03";

    public virtual string ColorInfo => "#60a5fa";
    public virtual string ColorInfoSubtle => "rgba(96, 165, 250, 0.1)";
    public virtual string ColorInfoHover => "#93c5fd";
    public virtual string ColorInfoFg => "#1e3a8a";

    public virtual string Font => "'Inter', system-ui, sans-serif";
    public virtual string FontMono => "'JetBrains Mono', monospace";
    public virtual string TextSm => "0.75rem";
    public virtual string TextBase => "0.8125rem";
    public virtual string TextLg => "0.9375rem";

    public virtual string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.5)";
    public virtual string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.6)";
    public virtual string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.6)";
    public virtual string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.7)";
    public virtual string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.8)";

    public virtual string RadiusSm => "2px";
    public virtual string RadiusMd => "4px";
    public virtual string RadiusLg => "6px";
    public virtual string RadiusXl => "8px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public virtual string TransitionSlow => "400ms cubic-bezier(0.4, 0, 0.2, 1)";

    public virtual string FocusRing => "0 0 0 2px #010409, 0 0 0 4px #38bdf8";
    public virtual string FocusRingDanger => "0 0 0 2px #010409, 0 0 0 4px #fb7185";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky => 1020;
    public virtual int ZModal => 1050;
    public virtual int ZToast => 1070;
    public virtual int ZTooltip => 1100;
}

internal class DefaultComponents : IThemeComponents
{
    public virtual string BtnRadius => "4px";
    public virtual string BtnFontSize => "0.75rem"; // 12px for compact look
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight => "30px";     // Very compact
    public virtual string BtnHeightSm => "24px";
    public virtual string BtnHeightLg => "38px";

    public virtual string InputRadius => "4px";
    public virtual string InputFontSize => "0.8125rem";
    public virtual string InputHeight => "30px";
    public virtual string InputHeightSm => "24px";
    public virtual string InputHeightLg => "38px";

    public virtual string CardRadius => "4px"; // Strict
    public virtual string CardPadding => "12px"; // Compact
    public virtual string CardBorderColor => "var(--sg-border)";
    public virtual string CardBg => "var(--sg-surface)";

    public virtual string ModalRadius => "8px";

    public virtual string TableRadius => "4px";
    public virtual string TableHeaderFontWeight => "700";

    public virtual string TabsIndicatorHeight => "2px";
    public virtual string TooltipMaxWidth => "240px";

    public virtual string HeaderBg => "var(--sg-bg)";
    public virtual string HeaderFg => "var(--sg-fg)";
    public virtual string NavBg => "var(--sg-bg-subtle)";
    public virtual string NavFg => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg => "var(--sg-color-primary-subtle)";
    public virtual string NavActiveFg => "var(--sg-color-primary)";
}

