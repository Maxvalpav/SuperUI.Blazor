using System.Text;

namespace SuperUI.Themes;

/// <summary>
/// Service to generate CSS from theme definitions.
/// </summary>
public static class SgThemeGenerator
{
    public static string GenerateFullThemeCss(IThemeDefinition theme)
    {
        var sb = new StringBuilder();

        // 0. Google Fonts @import (must precede all other CSS)
        if (theme.Typography?.EmbedGoogleFontsImport == true &&
            !string.IsNullOrEmpty(theme.Typography.GoogleFontsImportUrl))
        {
            sb.AppendLine($"@import url('{theme.Typography.GoogleFontsImportUrl}');");
            sb.AppendLine();
        }

        // 1. Light Mode (Default)
        sb.AppendLine(GenerateCss(theme.Primitives, theme.Light, ":root"));

        // 2. Dark Mode (if supported)
        if (theme.Dark != null)
        {
            sb.AppendLine();
            sb.AppendLine(GenerateCss(theme.Primitives, theme.Dark, "[data-theme=\"dark\"]"));
        }

        // 3. Typography settings (optional, in :root)
        if (theme.Typography != null)
        {
            AppendTypographyCss(sb, theme.Typography);
        }

        // 4. Component Overrides
        if (theme.Components != null)
        {
            sb.AppendLine();
            sb.AppendLine(":root {");
            sb.AppendLine($"    --sgc-btn-radius: {theme.Components.BtnRadius};");
            sb.AppendLine($"    --sgc-btn-font-size: {theme.Components.BtnFontSize};");
            sb.AppendLine($"    --sgc-btn-font-weight: {theme.Components.BtnFontWeight};");
            sb.AppendLine($"    --sgc-btn-height: {theme.Components.BtnHeight};");
            sb.AppendLine($"    --sgc-btn-height-sm: {theme.Components.BtnHeightSm};");
            sb.AppendLine($"    --sgc-btn-height-lg: {theme.Components.BtnHeightLg};");

            sb.AppendLine($"    --sgc-input-radius: {theme.Components.InputRadius};");
            sb.AppendLine($"    --sgc-input-font-size: {theme.Components.InputFontSize};");
            sb.AppendLine($"    --sgc-input-height: {theme.Components.InputHeight};");
            sb.AppendLine($"    --sgc-input-height-sm: {theme.Components.InputHeightSm};");
            sb.AppendLine($"    --sgc-input-height-lg: {theme.Components.InputHeightLg};");

            sb.AppendLine($"    --sgc-card-radius: {theme.Components.CardRadius};");
            sb.AppendLine($"    --sgc-card-padding: {theme.Components.CardPadding};");
            sb.AppendLine($"    --sgc-card-border-color: {theme.Components.CardBorderColor};");
            sb.AppendLine($"    --sgc-card-bg: {theme.Components.CardBg};");

            sb.AppendLine($"    --sgc-header-bg: {theme.Components.HeaderBg};");
            sb.AppendLine($"    --sgc-header-fg: {theme.Components.HeaderFg};");
            sb.AppendLine($"    --sgc-nav-bg: {theme.Components.NavBg};");
            sb.AppendLine($"    --sgc-nav-fg: {theme.Components.NavFg};");
            sb.AppendLine($"    --sgc-nav-active-bg: {theme.Components.NavActiveBg};");
            sb.AppendLine($"    --sgc-nav-active-fg: {theme.Components.NavActiveFg};");

            sb.AppendLine($"    --sgc-modal-radius: {theme.Components.ModalRadius};");

            sb.AppendLine($"    --sgc-table-radius: {theme.Components.TableRadius};");
            sb.AppendLine($"    --sgc-table-header-font-weight: {theme.Components.TableHeaderFontWeight};");

            sb.AppendLine($"    --sgc-tabs-indicator-height: {theme.Components.TabsIndicatorHeight};");
            sb.AppendLine($"    --sgc-tooltip-max-width: {theme.Components.TooltipMaxWidth};");
            sb.AppendLine("}");
        }

        // 5. Additional CSS
        if (!string.IsNullOrEmpty(theme.AdditionalCss))
        {
            sb.AppendLine();
            sb.AppendLine(theme.AdditionalCss);
        }

        return sb.ToString();
    }

    private static void AppendTypographyCss(StringBuilder sb, IThemeTypography t)
    {
        sb.AppendLine();
        sb.AppendLine(":root {");

        if (!string.IsNullOrEmpty(t.HeadingFont))
        {
            sb.AppendLine($"    --sg-font-heading: {t.HeadingFont};");
        }

        AppendHeadingCss(sb, "h1", t.H1, t.HeadingFont);
        AppendHeadingCss(sb, "h2", t.H2, t.HeadingFont);
        AppendHeadingCss(sb, "h3", t.H3, t.HeadingFont);
        AppendHeadingCss(sb, "h4", t.H4, t.HeadingFont);
        AppendHeadingCss(sb, "h5", t.H5, t.HeadingFont);
        AppendHeadingCss(sb, "h6", t.H6, t.HeadingFont);

        sb.AppendLine("}");
    }

    private static void AppendHeadingCss(StringBuilder sb, string tag, HeadingSettings h, string? defaultFont)
    {
        sb.AppendLine($"    --sg-{tag}-font-size: {h.FontSize};");
        if (h.FontFamily != null)
            sb.AppendLine($"    --sg-{tag}-font-family: {h.FontFamily};");
        else if (defaultFont != null)
            sb.AppendLine($"    --sg-{tag}-font-family: var(--sg-font-heading);");
        if (h.FontWeight != null)
            sb.AppendLine($"    --sg-{tag}-font-weight: {h.FontWeight};");
        if (h.LineHeight != null)
            sb.AppendLine($"    --sg-{tag}-line-height: {h.LineHeight};");
        if (h.LetterSpacing != null)
            sb.AppendLine($"    --sg-{tag}-letter-spacing: {h.LetterSpacing};");
    }

    public static string GenerateCss(IThemePrimitives p, IThemeSemantic s, string selector = ":root")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{selector} {{");

        // Primitives
        sb.AppendLine("    /* Primitives */");
        sb.AppendLine($"    --sg-p-neutral-0: {p.Neutral0};");
        sb.AppendLine($"    --sg-p-neutral-50: {p.Neutral50};");
        sb.AppendLine($"    --sg-p-neutral-100: {p.Neutral100};");
        sb.AppendLine($"    --sg-p-neutral-200: {p.Neutral200};");
        sb.AppendLine($"    --sg-p-neutral-300: {p.Neutral300};");
        sb.AppendLine($"    --sg-p-neutral-400: {p.Neutral400};");
        sb.AppendLine($"    --sg-p-neutral-500: {p.Neutral500};");
        sb.AppendLine($"    --sg-p-neutral-600: {p.Neutral600};");
        sb.AppendLine($"    --sg-p-neutral-700: {p.Neutral700};");
        sb.AppendLine($"    --sg-p-neutral-800: {p.Neutral800};");
        sb.AppendLine($"    --sg-p-neutral-900: {p.Neutral900};");

        sb.AppendLine($"    --sg-p-blue-50: {p.Primary50};");
        sb.AppendLine($"    --sg-p-blue-100: {p.Primary100};");
        sb.AppendLine($"    --sg-p-blue-200: {p.Primary200};");
        sb.AppendLine($"    --sg-p-blue-300: {p.Primary300};");
        sb.AppendLine($"    --sg-p-blue-400: {p.Primary400};");
        sb.AppendLine($"    --sg-p-blue-500: {p.Primary500};");
        sb.AppendLine($"    --sg-p-blue-600: {p.Primary600};");
        sb.AppendLine($"    --sg-p-blue-700: {p.Primary700};");
        sb.AppendLine($"    --sg-p-blue-800: {p.Primary800};");
        sb.AppendLine($"    --sg-p-blue-900: {p.Primary900};");

        sb.AppendLine($"    --sg-p-emerald-50: {p.Success50};");
        sb.AppendLine($"    --sg-p-emerald-100: {p.Success100};");
        sb.AppendLine($"    --sg-p-emerald-500: {p.Success500};");
        sb.AppendLine($"    --sg-p-emerald-600: {p.Success600};");
        sb.AppendLine($"    --sg-p-emerald-700: {p.Success700};");

        sb.AppendLine($"    --sg-p-rose-50: {p.Danger50};");
        sb.AppendLine($"    --sg-p-rose-100: {p.Danger100};");
        sb.AppendLine($"    --sg-p-rose-500: {p.Danger500};");
        sb.AppendLine($"    --sg-p-rose-600: {p.Danger600};");
        sb.AppendLine($"    --sg-p-rose-700: {p.Danger700};");

        sb.AppendLine($"    --sg-p-amber-50: {p.Warning50};");
        sb.AppendLine($"    --sg-p-amber-100: {p.Warning100};");
        sb.AppendLine($"    --sg-p-amber-500: {p.Warning500};");
        sb.AppendLine($"    --sg-p-amber-600: {p.Warning600};");

        sb.AppendLine($"    --sg-p-sky-50: {p.Info50};");
        sb.AppendLine($"    --sg-p-sky-100: {p.Info100};");
        sb.AppendLine($"    --sg-p-sky-500: {p.Info500};");
        sb.AppendLine($"    --sg-p-sky-600: {p.Info600};");

        sb.AppendLine($"    --sg-p-font-sans: {p.FontSans};");
        sb.AppendLine($"    --sg-p-font-mono: {p.FontMono};");
        sb.AppendLine($"    --sg-p-font-serif: {p.FontSerif};");

        sb.AppendLine($"    --sg-p-radius-none: {p.RadiusNone};");
        sb.AppendLine($"    --sg-p-radius-xs: {p.RadiusXs};");
        sb.AppendLine($"    --sg-p-radius-sm: {p.RadiusSm};");
        sb.AppendLine($"    --sg-p-radius-md: {p.RadiusMd};");
        sb.AppendLine($"    --sg-p-radius-lg: {p.RadiusLg};");
        sb.AppendLine($"    --sg-p-radius-xl: {p.RadiusXl};");
        sb.AppendLine($"    --sg-p-radius-2xl: {p.Radius2Xl};");
        sb.AppendLine($"    --sg-p-radius-full: {p.RadiusFull};");

        // Semantic
        sb.AppendLine("    /* Semantic */");
        sb.AppendLine($"    --sg-bg: {s.BgDefault};");
        sb.AppendLine($"    --sg-bg-subtle: {s.BgSubtle};");
        sb.AppendLine($"    --sg-bg-muted: {s.BgMuted};");
        sb.AppendLine($"    --sg-bg-emphasized: {s.BgEmphasized};");
        sb.AppendLine($"    --sg-bg-overlay: {s.BgOverlay};");

        sb.AppendLine($"    --sg-surface: {s.Surface};");
        sb.AppendLine($"    --sg-surface-raised: {s.SurfaceRaised};");
        sb.AppendLine($"    --sg-surface-overlay: {s.SurfaceOverlay};");

        sb.AppendLine($"    --sg-fg: {s.FgDefault};");
        sb.AppendLine($"    --sg-fg-subtle: {s.FgSubtle};");
        sb.AppendLine($"    --sg-fg-muted: {s.FgMuted};");
        sb.AppendLine($"    --sg-fg-disabled: {s.FgDisabled};");
        sb.AppendLine($"    --sg-fg-inverse: {s.FgInverse};");
        sb.AppendLine($"    --sg-fg-link: {s.FgLink};");
        sb.AppendLine($"    --sg-fg-link-hover: {s.FgLinkHover};");

        sb.AppendLine($"    --sg-border: {s.BorderDefault};");
        sb.AppendLine($"    --sg-border-subtle: {s.BorderSubtle};");
        sb.AppendLine($"    --sg-border-strong: {s.BorderStrong};");
        sb.AppendLine($"    --sg-border-focus: {s.BorderFocus};");
        sb.AppendLine($"    --sg-divider: {s.Divider};");

        sb.AppendLine($"    --sg-color-primary: {s.ColorPrimary};");
        sb.AppendLine($"    --sg-color-primary-subtle: {s.ColorPrimarySubtle};");
        sb.AppendLine($"    --sg-color-primary-muted: {s.ColorPrimaryMuted};");
        sb.AppendLine($"    --sg-color-primary-hover: {s.ColorPrimaryHover};");
        sb.AppendLine($"    --sg-color-primary-active: {s.ColorPrimaryActive};");
        sb.AppendLine($"    --sg-color-primary-fg: {s.ColorPrimaryFg};");

        sb.AppendLine($"    --sg-color-success: {s.ColorSuccess};");
        sb.AppendLine($"    --sg-color-success-subtle: {s.ColorSuccessSubtle};");
        sb.AppendLine($"    --sg-color-success-hover: {s.ColorSuccessHover};");
        sb.AppendLine($"    --sg-color-success-fg: {s.ColorSuccessFg};");

        sb.AppendLine($"    --sg-color-danger: {s.ColorDanger};");
        sb.AppendLine($"    --sg-color-danger-subtle: {s.ColorDangerSubtle};");
        sb.AppendLine($"    --sg-color-danger-hover: {s.ColorDangerHover};");
        sb.AppendLine($"    --sg-color-danger-fg: {s.ColorDangerFg};");

        sb.AppendLine($"    --sg-color-warning: {s.ColorWarning};");
        sb.AppendLine($"    --sg-color-warning-subtle: {s.ColorWarningSubtle};");
        sb.AppendLine($"    --sg-color-warning-hover: {s.ColorWarningHover};");
        sb.AppendLine($"    --sg-color-warning-fg: {s.ColorWarningFg};");

        sb.AppendLine($"    --sg-color-info: {s.ColorInfo};");
        sb.AppendLine($"    --sg-color-info-subtle: {s.ColorInfoSubtle};");
        sb.AppendLine($"    --sg-color-info-hover: {s.ColorInfoHover};");
        sb.AppendLine($"    --sg-color-info-fg: {s.ColorInfoFg};");

        sb.AppendLine($"    --sg-font: {s.Font};");
        sb.AppendLine($"    --sg-font-mono: {s.FontMono};");
        sb.AppendLine($"    --sg-text-xs: {s.TextXs};");
        sb.AppendLine($"    --sg-text-sm: {s.TextSm};");
        sb.AppendLine($"    --sg-text-base: {s.TextBase};");
        sb.AppendLine($"    --sg-text-lg: {s.TextLg};");
        sb.AppendLine($"    --sg-text-xl: {s.TextXl};");
        sb.AppendLine($"    --sg-text-2xl: {s.Text2Xl};");
        sb.AppendLine($"    --sg-text-3xl: {s.Text3Xl};");
        sb.AppendLine();
        sb.AppendLine($"    --sg-font-weight-normal: {s.FontWeightNormal};");
        sb.AppendLine($"    --sg-font-weight-medium: {s.FontWeightMedium};");
        sb.AppendLine($"    --sg-font-weight-semibold: {s.FontWeightSemibold};");
        sb.AppendLine($"    --sg-font-weight-bold: {s.FontWeightBold};");
        sb.AppendLine();
        sb.AppendLine($"    --sg-line-height-tight: {s.LineHeightTight};");
        sb.AppendLine($"    --sg-line-height-normal: {s.LineHeightNormal};");
        sb.AppendLine($"    --sg-line-height-relaxed: {s.LineHeightRelaxed};");

        sb.AppendLine($"    --sg-radius-sm: {s.RadiusSm};");
        sb.AppendLine($"    --sg-radius-md: {s.RadiusMd};");
        sb.AppendLine($"    --sg-radius-lg: {s.RadiusLg};");
        sb.AppendLine($"    --sg-radius-xl: {s.RadiusXl};");
        sb.AppendLine($"    --sg-radius-full: {s.RadiusFull};");

        sb.AppendLine($"    --sg-shadow-xs: {s.ShadowXs};");
        sb.AppendLine($"    --sg-shadow-sm: {s.ShadowSm};");
        sb.AppendLine($"    --sg-shadow-md: {s.ShadowMd};");
        sb.AppendLine($"    --sg-shadow-lg: {s.ShadowLg};");
        sb.AppendLine($"    --sg-shadow-xl: {s.ShadowXl};");

        sb.AppendLine($"    --sg-transition-fast: {s.TransitionFast};");
        sb.AppendLine($"    --sg-transition-base: {s.TransitionBase};");
        sb.AppendLine($"    --sg-transition-slow: {s.TransitionSlow};");

        sb.AppendLine($"    --sg-focus-ring: {s.FocusRing};");
        sb.AppendLine($"    --sg-focus-ring-danger: {s.FocusRingDanger};");

        sb.AppendLine($"    --sg-z-dropdown: {s.ZDropdown};");
        sb.AppendLine($"    --sg-z-sticky: {s.ZSticky};");
        sb.AppendLine($"    --sg-z-modal: {s.ZModal};");
        sb.AppendLine($"    --sg-z-toast: {s.ZToast};");
        sb.AppendLine($"    --sg-z-tooltip: {s.ZTooltip};");

        sb.AppendLine("}");
        return sb.ToString();
    }
}
