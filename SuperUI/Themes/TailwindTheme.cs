namespace SuperUI.Themes;

/// <summary>
/// Tailwind CSS v3 theme for SuperUI.
/// </summary>
public sealed class TailwindTheme : ThemeBase
{
    public override string Id => "tailwind-v3";
    public override string Name => "Tailwind CSS v3";
    public override string? Description => "Tailwind CSS v3 color palette and design system.";
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() => new TailwindPrimitives();
    protected override IThemeSemantic CreateLight() => new TailwindSemanticLight();
    protected override IThemeSemantic? CreateDark() => new TailwindSemanticDark();
    protected override IThemeComponents? CreateComponents() => new TailwindComponents();

    public override string? AdditionalCss => """
        /* Ring system (фокусное кольцо в Tailwind-стиле) */
        [data-theme-id="tailwind-v3"] .sgc-input:focus,
        [data-theme-id="tailwind-v3"] .sgc-input-wrap:focus-within .sgc-input {
            --tw-ring-color: rgba(59, 130, 246, 0.5);
            box-shadow: 0 0 0 3px var(--tw-ring-color);
            border-color: #3B82F6;
        }

        /* Tailwind-like button reset */
        [data-theme-id="tailwind-v3"] .sgc-btn {
            font-weight: 600;
            letter-spacing: 0;
            transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);
        }
        """;
}

internal class TailwindPrimitives : IThemePrimitives
{
    public string Neutral0   => "#FFFFFF";
    public string Neutral50  => "#F8FAFC";
    public string Neutral100 => "#F1F5F9";
    public string Neutral200 => "#E2E8F0";
    public string Neutral300 => "#CBD5E1";
    public string Neutral400 => "#94A3B8";
    public string Neutral500 => "#64748B";
    public string Neutral600 => "#475569";
    public string Neutral700 => "#334155";
    public string Neutral800 => "#1E293B";
    public string Neutral900 => "#0F172A";

    public string Primary50  => "#EFF6FF";
    public string Primary100 => "#DBEAFE";
    public string Primary200 => "#BFDBFE";
    public string Primary300 => "#93C5FD";
    public string Primary400 => "#60A5FA";
    public string Primary500 => "#3B82F6";
    public string Primary600 => "#2563EB";
    public string Primary700 => "#1D4ED8";
    public string Primary800 => "#1E40AF";
    public string Primary900 => "#1E3A8A";

    public string Success50  => "#ECFDF5";
    public string Success100 => "#D1FAE5";
    public string Success500 => "#10B981";
    public string Success600 => "#059669";
    public string Success700 => "#047857";

    public string Danger50  => "#FEF2F2";
    public string Danger100 => "#FEE2E2";
    public string Danger500 => "#EF4444";
    public string Danger600 => "#DC2626";
    public string Danger700 => "#B91C1C";

    public string Warning50  => "#FFFBEB";
    public string Warning100 => "#FEF3C7";
    public string Warning500 => "#F59E0B";
    public string Warning600 => "#D97706";

    public string Info50  => "#F0F9FF";
    public string Info100 => "#E0F2FE";
    public string Info500 => "#0EA5E9";
    public string Info600 => "#0284c7";

    public string FontSans  => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    public string FontMono  => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string FontSerif => "ui-serif, Georgia, Cambria, 'Times New Roman', Times, serif";

    public string RadiusNone => "0";
    public string RadiusXs   => "2px";
    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string Radius2Xl  => "16px";
    public string RadiusFull => "9999px";
}

internal class TailwindSemanticLight : IThemeSemantic
{
    public string BgDefault    => "#FFFFFF";
    public string BgSubtle     => "#F1F5F9"; // Neutral 100
    public string BgMuted      => "#E2E8F0"; // Neutral 200
    public string BgEmphasized => "#CBD5E1"; // Neutral 300
    public string BgOverlay    => "rgba(0, 0, 0, 0.5)";

    public string Surface         => "#FFFFFF";
    public string SurfaceRaised   => "#FFFFFF";
    public string SurfaceOverlay  => "#FFFFFF";

    public string FgDefault    => "#0F172A"; // Neutral 900
    public string FgSubtle     => "#475569"; // Neutral 600
    public string FgMuted      => "#94A3B8"; // Neutral 400
    public string FgDisabled   => "#CBD5E1"; // Neutral 300
    public string FgInverse    => "#FFFFFF";
    public string FgLink       => "#3B82F6";
    public string FgLinkHover  => "#2563EB";

    public string BorderDefault => "#E2E8F0"; // Neutral 200
    public string BorderSubtle  => "#F1F5F9"; // Neutral 100
    public string BorderStrong => "#475569"; // Neutral 600
    public string BorderFocus  => "#3B82F6"; // Primary 500
    public string Divider      => "#F1F5F9"; // Neutral 100

    public string ColorPrimary       => "#2563EB";
    public string ColorPrimarySubtle => "#EFF6FF";
    public string ColorPrimaryMuted  => "#DBEAFE";
    public string ColorPrimaryHover  => "#1D4ED8";
    public string ColorPrimaryActive => "#1E40AF";
    public string ColorPrimaryFg     => "#FFFFFF";

    public string ColorSuccess       => "#10B981";
    public string ColorSuccessSubtle => "#ECFDF5";
    public string ColorSuccessHover  => "#059669";
    public string ColorSuccessFg     => "#FFFFFF";

    public string ColorDanger        => "#EF4444";
    public string ColorDangerSubtle  => "#FEF2F2";
    public string ColorDangerHover   => "#DC2626";
    public string ColorDangerFg      => "#FFFFFF";

    public string ColorWarning       => "#F59E0B";
    public string ColorWarningSubtle => "#FFFBEB";
    public string ColorWarningHover  => "#D97706";
    public string ColorWarningFg     => "#0F172A";

    public string ColorInfo          => "#0EA5E9";
    public string ColorInfoSubtle    => "#F0F9FF";
    public string ColorInfoHover     => "#0284C7";
    public string ColorInfoFg        => "#FFFFFF";

    public string Font     => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    public string FontMono => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string TextSm   => "0.875rem";
    public string TextBase => "1rem";
    public string TextLg   => "1.125rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.05)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.1)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.1)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.1)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.1)";

    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing => "0 0 0 3px rgba(59, 130, 246, 0.5)";
    public string FocusRingDanger => "0 0 0 3px rgba(239, 68, 68, 0.5)";

    public int ZDropdown => 100;
    public int ZSticky   => 200;
    public int ZModal    => 300;
    public int ZToast    => 400;
    public int ZTooltip  => 500;
}

internal class TailwindSemanticDark : IThemeSemantic
{
    public string BgDefault    => "#0F172A";
    public string BgSubtle     => "#1E293B";
    public string BgMuted      => "#334155";
    public string BgEmphasized => "#475569";
    public string BgOverlay    => "rgba(0, 0, 0, 0.8)";

    public string Surface        => "#1E293B";
    public string SurfaceRaised  => "#334155";
    public string SurfaceOverlay => "#1E293B";

    public string FgDefault  => "#F1F5F9";
    public string FgSubtle   => "#94A3B8";
    public string FgMuted    => "#64748B";
    public string FgDisabled => "#475569";
    public string FgInverse  => "#0F172A";
    public string FgLink     => "#60A5FA";
    public string FgLinkHover => "#93C5FD";

    public string BorderDefault => "#334155"; // Neutral 700
    public string BorderSubtle  => "#1E293B"; // Neutral 800
    public string BorderStrong  => "#475569"; // Neutral 600
    public string BorderFocus   => "#3B82F6";
    public string Divider       => "#1E293B"; // Neutral 800

    public string ColorPrimary       => "#3B82F6";
    public string ColorPrimarySubtle => "rgba(59, 130, 246, 0.15)";
    public string ColorPrimaryMuted  => "rgba(59, 130, 246, 0.25)";
    public string ColorPrimaryHover  => "#60A5FA";
    public string ColorPrimaryActive => "#93C5FD";
    public string ColorPrimaryFg     => "#FFFFFF";

    public string ColorSuccess       => "#10B981";
    public string ColorSuccessSubtle => "rgba(16, 185, 129, 0.15)";
    public string ColorSuccessHover  => "#34D399";
    public string ColorSuccessFg     => "#FFFFFF";

    public string ColorDanger        => "#EF4444";
    public string ColorDangerSubtle  => "rgba(239, 68, 68, 0.15)";
    public string ColorDangerHover   => "#F87171";
    public string ColorDangerFg      => "#FFFFFF";

    public string ColorWarning       => "#F59E0B";
    public string ColorWarningSubtle => "rgba(245, 158, 11, 0.15)";
    public string ColorWarningHover  => "#FBBF24";
    public string ColorWarningFg     => "#0F172A";

    public string ColorInfo          => "#0EA5E9";
    public string ColorInfoSubtle    => "rgba(14, 165, 233, 0.15)";
    public string ColorInfoHover     => "#38BDF8";
    public string ColorInfoFg        => "#FFFFFF";

    public string Font     => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    public string FontMono => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string TextSm   => "0.875rem";
    public string TextBase => "1rem";
    public string TextLg   => "1.125rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.5)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.6)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.6)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.7)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.8)";

    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing      => "0 0 0 3px rgba(59, 130, 246, 0.5)";
    public string FocusRingDanger => "0 0 0 3px rgba(239, 68, 68, 0.5)";

    public int ZDropdown => 100;
    public int ZSticky   => 200;
    public int ZModal    => 300;
    public int ZToast    => 400;
    public int ZTooltip  => 500;
}

internal class TailwindComponents : IThemeComponents
{
    public string BtnRadius      => "6px";
    public string BtnFontSize    => "0.875rem";
    public string BtnFontWeight  => "600";
    public string BtnHeight      => "2.25rem";
    public string BtnHeightSm   => "1.75rem";
    public string BtnHeightLg   => "2.5rem";

    public string InputRadius    => "6px";
    public string InputFontSize  => "0.875rem";
    public string InputHeight    => "2.25rem";
    public string InputHeightSm => "1.75rem";
    public string InputHeightLg => "2.5rem";

    public string CardRadius     => "12px";
    public string CardPadding    => "24px";
    public string CardBorderColor => "var(--sg-border)";
    public string CardBg         => "var(--sg-surface)";

    public string ModalRadius    => "12px";

    public string TableRadius    => "8px";
    public string TableHeaderFontWeight => "600";

    public string TabsIndicatorHeight => "2px";

    public string TooltipMaxWidth => "240px";

    public string HeaderBg => "var(--sg-surface)";
    public string HeaderFg => "var(--sg-fg)";
    public string NavBg => "var(--sg-surface)";
    public string NavFg => "var(--sg-fg-subtle)";
    public string NavActiveBg => "var(--sg-color-primary-subtle)";
    public string NavActiveFg => "var(--sg-color-primary)";
}
