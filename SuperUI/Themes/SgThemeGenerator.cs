using System.Text;

namespace SuperUI.Themes;

/// <summary>
/// Service to generate CSS from theme definitions.
/// v2.0 — emits spacing/iconSize/borderWidth/elevation/motion/density/measure/state tokens
/// and 16+ per-component groups plus φ-typography and font variants.
/// All previous vars (v1.x) are preserved for backward compatibility.
/// </summary>
public static class SgThemeGenerator
{
    public static string GenerateFullThemeCss(IThemeDefinition theme)
    {
        var sb = new StringBuilder();

        // 0. Google Fonts loading moved to <head> in index.html.
        //    Emitting @import from inside a per-theme CSS would block the
        //    link-swap runtime (the rest of the file waits on the remote
        //    fetch). 2.0-rc3 (PR #5b) initially put it here; the runtime
        //    fix in 2.0-rc3 PR #5c removed it. The Typography fields
        //    GoogleFontsImportUrl / EmbedGoogleFontsImport are still
        //    read from JSON so themes can declare their font family
        //    (consumed by AppendTypographyCss below), but no @import
        //    is emitted.

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
            AppendComponentsCss(sb, theme.Components);
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
        if (!string.IsNullOrEmpty(t.SerifFont))
        {
            sb.AppendLine($"    --sg-font-serif: {t.SerifFont};");
        }
        if (!string.IsNullOrEmpty(t.DisplayFont))
        {
            sb.AppendLine($"    --sg-font-display: {t.DisplayFont};");
        }
        if (!string.IsNullOrEmpty(t.MedicalFont))
        {
            sb.AppendLine($"    --sg-font-medical: {t.MedicalFont};");
        }

        AppendHeadingCss(sb, "h1", t.H1, t.HeadingFont);
        AppendHeadingCss(sb, "h2", t.H2, t.HeadingFont);
        AppendHeadingCss(sb, "h3", t.H3, t.HeadingFont);
        AppendHeadingCss(sb, "h4", t.H4, t.HeadingFont);
        AppendHeadingCss(sb, "h5", t.H5, t.HeadingFont);
        AppendHeadingCss(sb, "h6", t.H6, t.HeadingFont);

        // φ (Fibonacci × 16) text scale.
        var phi = t.PhiScale;
        sb.AppendLine($"    --sg-text-phi-micro:   {phi.Micro};");
        sb.AppendLine($"    --sg-text-phi-caption: {phi.Caption};");
        sb.AppendLine($"    --sg-text-phi-body:    {phi.Body};");
        sb.AppendLine($"    --sg-text-phi-lead:    {phi.Lead};");
        sb.AppendLine($"    --sg-text-phi-h3:      {phi.H3};");
        sb.AppendLine($"    --sg-text-phi-h2:      {phi.H2};");
        sb.AppendLine($"    --sg-text-phi-h1:      {phi.H1};");
        sb.AppendLine($"    --sg-text-phi-display: {phi.Display};");
        sb.AppendLine($"    --sg-text-phi-poster:  {phi.Poster};");

        // φ line-height scale.
        var lh = t.PhiLineHeight;
        sb.AppendLine($"    --sg-lh-phi-caption: {lh.Caption};");
        sb.AppendLine($"    --sg-lh-phi-body:    {lh.Body};");
        sb.AppendLine($"    --sg-lh-phi-display: {lh.Display};");

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

    private static void AppendComponentsCss(StringBuilder sb, IThemeComponents c)
    {
        sb.AppendLine();
        sb.AppendLine(":root {");

        // Button
        sb.AppendLine("    /* Button */");
        sb.AppendLine($"    --sgc-btn-radius:      {c.BtnRadius};");
        sb.AppendLine($"    --sgc-btn-font-size:   {c.BtnFontSize};");
        sb.AppendLine($"    --sgc-btn-font-weight: {c.BtnFontWeight};");
        sb.AppendLine($"    --sgc-btn-height:      {c.BtnHeight};");
        sb.AppendLine($"    --sgc-btn-height-sm:   {c.BtnHeightSm};");
        sb.AppendLine($"    --sgc-btn-height-lg:   {c.BtnHeightLg};");
        sb.AppendLine($"    --sgc-btn-padding-x:   {c.BtnPaddingX};");
        sb.AppendLine($"    --sgc-btn-padding-y:   {c.BtnPaddingY};");
        sb.AppendLine($"    --sgc-btn-gap:         {c.BtnGap};");
        sb.AppendLine($"    --sgc-btn-icon-size:   {c.BtnIconSize};");
        sb.AppendLine($"    --sgc-btn-min-width:   {c.BtnMinWidth};");

        // Input
        sb.AppendLine("    /* Input */");
        sb.AppendLine($"    --sgc-input-radius:      {c.InputRadius};");
        sb.AppendLine($"    --sgc-input-font-size:   {c.InputFontSize};");
        sb.AppendLine($"    --sgc-input-height:      {c.InputHeight};");
        sb.AppendLine($"    --sgc-input-height-sm:   {c.InputHeightSm};");
        sb.AppendLine($"    --sgc-input-height-lg:   {c.InputHeightLg};");
        sb.AppendLine($"    --sgc-input-padding-x:   {c.InputPaddingX};");
        sb.AppendLine($"    --sgc-input-padding-y:   {c.InputPaddingY};");
        sb.AppendLine($"    --sgc-input-border-width:{c.InputBorderWidth};");
        sb.AppendLine($"    --sgc-input-icon-size:   {c.InputIconSize};");

        // Select
        sb.AppendLine("    /* Select */");
        sb.AppendLine($"    --sgc-select-radius:     {c.SelectRadius};");
        sb.AppendLine($"    --sgc-select-font-size:  {c.SelectFontSize};");
        sb.AppendLine($"    --sgc-select-height:     {c.SelectHeight};");
        sb.AppendLine($"    --sgc-select-height-sm:  {c.SelectHeightSm};");
        sb.AppendLine($"    --sgc-select-height-lg:  {c.SelectHeightLg};");
        sb.AppendLine($"    --sgc-select-padding-x:  {c.SelectPaddingX};");
        sb.AppendLine($"    --sgc-select-icon-size:  {c.SelectIconSize};");

        // Checkbox
        sb.AppendLine("    /* Checkbox */");
        sb.AppendLine($"    --sgc-checkbox-size:        {c.CheckboxSize};");
        sb.AppendLine($"    --sgc-checkbox-size-sm:     {c.CheckboxSizeSm};");
        sb.AppendLine($"    --sgc-checkbox-size-lg:     {c.CheckboxSizeLg};");
        sb.AppendLine($"    --sgc-checkbox-radius:      {c.CheckboxRadius};");
        sb.AppendLine($"    --sgc-checkbox-icon-size:   {c.CheckboxIconSize};");
        sb.AppendLine($"    --sgc-checkbox-border-width:{c.CheckboxBorderWidth};");

        // Switch
        sb.AppendLine("    /* Switch */");
        sb.AppendLine($"    --sgc-switch-width:     {c.SwitchWidth};");
        sb.AppendLine($"    --sgc-switch-height:    {c.SwitchHeight};");
        sb.AppendLine($"    --sgc-switch-thumb-size:{c.SwitchThumbSize};");
        sb.AppendLine($"    --sgc-switch-radius:    {c.SwitchRadius};");
        sb.AppendLine($"    --sgc-switch-padding:   {c.SwitchPadding};");

        // Card
        sb.AppendLine("    /* Card */");
        sb.AppendLine($"    --sgc-card-radius:           {c.CardRadius};");
        sb.AppendLine($"    --sgc-card-padding:          {c.CardPadding};");
        sb.AppendLine($"    --sgc-card-padding-sm:       {c.CardPaddingSm};");
        sb.AppendLine($"    --sgc-card-padding-lg:       {c.CardPaddingLg};");
        sb.AppendLine($"    --sgc-card-border-color:     {c.CardBorderColor};");
        sb.AppendLine($"    --sgc-card-bg:               {c.CardBg};");
        sb.AppendLine($"    --sgc-card-header-font-weight:{c.CardHeaderFontWeight};");
        sb.AppendLine($"    --sgc-card-gap:              {c.CardGap};");

        // Modal
        sb.AppendLine("    /* Modal */");
        sb.AppendLine($"    --sgc-modal-radius:       {c.ModalRadius};");
        sb.AppendLine($"    --sgc-modal-width:        {c.ModalWidth};");
        sb.AppendLine($"    --sgc-modal-width-sm:     {c.ModalWidthSm};");
        sb.AppendLine($"    --sgc-modal-width-lg:     {c.ModalWidthLg};");
        sb.AppendLine($"    --sgc-modal-width-xl:     {c.ModalWidthXl};");
        sb.AppendLine($"    --sgc-modal-padding:      {c.ModalPadding};");
        sb.AppendLine($"    --sgc-modal-backdrop-blur:{c.ModalBackdropBlur};");

        // Dropdown
        sb.AppendLine("    /* Dropdown */");
        sb.AppendLine($"    --sgc-dropdown-radius:        {c.DropdownRadius};");
        sb.AppendLine($"    --sgc-dropdown-padding:       {c.DropdownPadding};");
        sb.AppendLine($"    --sgc-dropdown-item-height:   {c.DropdownItemHeight};");
        sb.AppendLine($"    --sgc-dropdown-item-padding-x:{c.DropdownItemPaddingX};");
        sb.AppendLine($"    --sgc-dropdown-item-padding-y:{c.DropdownItemPaddingY};");
        sb.AppendLine($"    --sgc-dropdown-gap:           {c.DropdownGap};");

        // Tooltip
        sb.AppendLine("    /* Tooltip */");
        sb.AppendLine($"    --sgc-tooltip-max-width: {c.TooltipMaxWidth};");
        sb.AppendLine($"    --sgc-tooltip-radius:    {c.TooltipRadius};");
        sb.AppendLine($"    --sgc-tooltip-padding:   {c.TooltipPadding};");
        sb.AppendLine($"    --sgc-tooltip-font-size: {c.TooltipFontSize};");
        sb.AppendLine($"    --sgc-tooltip-arrow-size:{c.TooltipArrowSize};");

        // Tabs
        sb.AppendLine("    /* Tabs */");
        sb.AppendLine($"    --sgc-tabs-indicator-height: {c.TabsIndicatorHeight};");
        sb.AppendLine($"    --sgc-tabs-radius:           {c.TabsRadius};");
        sb.AppendLine($"    --sgc-tabs-height:           {c.TabsHeight};");
        sb.AppendLine($"    --sgc-tabs-padding-x:        {c.TabsPaddingX};");
        sb.AppendLine($"    --sgc-tabs-padding-y:        {c.TabsPaddingY};");
        sb.AppendLine($"    --sgc-tabs-gap:              {c.TabsGap};");

        // Table
        sb.AppendLine("    /* Table */");
        sb.AppendLine($"    --sgc-table-radius:           {c.TableRadius};");
        sb.AppendLine($"    --sgc-table-header-font-weight:{c.TableHeaderFontWeight};");
        sb.AppendLine($"    --sgc-table-row-height:       {c.TableRowHeight};");
        sb.AppendLine($"    --sgc-table-row-height-sm:    {c.TableRowHeightSm};");
        sb.AppendLine($"    --sgc-table-header-height:    {c.TableHeaderHeight};");
        sb.AppendLine($"    --sgc-table-cell-padding-x:   {c.TableCellPaddingX};");
        sb.AppendLine($"    --sgc-table-cell-padding-y:   {c.TableCellPaddingY};");
        sb.AppendLine($"    --sgc-table-border-width:     {c.TableBorderWidth};");

        // Alert
        sb.AppendLine("    /* Alert */");
        sb.AppendLine($"    --sgc-alert-radius:    {c.AlertRadius};");
        sb.AppendLine($"    --sgc-alert-padding:   {c.AlertPadding};");
        sb.AppendLine($"    --sgc-alert-padding-sm:{c.AlertPaddingSm};");
        sb.AppendLine($"    --sgc-alert-icon-size: {c.AlertIconSize};");
        sb.AppendLine($"    --sgc-alert-gap:       {c.AlertGap};");

        // Badge
        sb.AppendLine("    /* Badge */");
        sb.AppendLine($"    --sgc-badge-radius:    {c.BadgeRadius};");
        sb.AppendLine($"    --sgc-badge-height:    {c.BadgeHeight};");
        sb.AppendLine($"    --sgc-badge-height-sm: {c.BadgeHeightSm};");
        sb.AppendLine($"    --sgc-badge-height-lg: {c.BadgeHeightLg};");
        sb.AppendLine($"    --sgc-badge-padding-x: {c.BadgePaddingX};");
        sb.AppendLine($"    --sgc-badge-font-size: {c.BadgeFontSize};");
        sb.AppendLine($"    --sgc-badge-font-weight:{c.BadgeFontWeight};");

        // Chip
        sb.AppendLine("    /* Chip */");
        sb.AppendLine($"    --sgc-chip-radius:    {c.ChipRadius};");
        sb.AppendLine($"    --sgc-chip-height:    {c.ChipHeight};");
        sb.AppendLine($"    --sgc-chip-height-sm: {c.ChipHeightSm};");
        sb.AppendLine($"    --sgc-chip-height-lg: {c.ChipHeightLg};");
        sb.AppendLine($"    --sgc-chip-padding-x: {c.ChipPaddingX};");
        sb.AppendLine($"    --sgc-chip-gap:       {c.ChipGap};");
        sb.AppendLine($"    --sgc-chip-icon-size: {c.ChipIconSize};");

        // Spinner
        sb.AppendLine("    /* Spinner */");
        sb.AppendLine($"    --sgc-spinner-size:        {c.SpinnerSize};");
        sb.AppendLine($"    --sgc-spinner-size-sm:     {c.SpinnerSizeSm};");
        sb.AppendLine($"    --sgc-spinner-size-lg:     {c.SpinnerSizeLg};");
        sb.AppendLine($"    --sgc-spinner-border-width:{c.SpinnerBorderWidth};");
        sb.AppendLine($"    --sgc-spinner-track-opacity:{c.SpinnerTrackOpacity};");

        // Progress
        sb.AppendLine("    /* Progress */");
        sb.AppendLine($"    --sgc-progress-height:          {c.ProgressHeight};");
        sb.AppendLine($"    --sgc-progress-height-sm:       {c.ProgressHeightSm};");
        sb.AppendLine($"    --sgc-progress-height-lg:       {c.ProgressHeightLg};");
        sb.AppendLine($"    --sgc-progress-radius:          {c.ProgressRadius};");
        sb.AppendLine($"    --sgc-progress-indicator-radius:{c.ProgressIndicatorRadius};");

        // Header & Navigation
        sb.AppendLine("    /* Header & Nav */");
        sb.AppendLine($"    --sgc-header-bg:     {c.HeaderBg};");
        sb.AppendLine($"    --sgc-header-fg:     {c.HeaderFg};");
        sb.AppendLine($"    --sgc-nav-bg:        {c.NavBg};");
        sb.AppendLine($"    --sgc-nav-fg:        {c.NavFg};");
        sb.AppendLine($"    --sgc-nav-active-bg: {c.NavActiveBg};");
        sb.AppendLine($"    --sgc-nav-active-fg: {c.NavActiveFg};");
        sb.AppendLine($"    --sgc-nav-item-height:{c.NavItemHeight};");
        sb.AppendLine($"    --sgc-nav-item-padding-x:{c.NavItemPaddingX};");

        sb.AppendLine("}");
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

        sb.AppendLine($"    --sg-p-font-sans:    {p.FontSans};");
        sb.AppendLine($"    --sg-p-font-mono:    {p.FontMono};");
        sb.AppendLine($"    --sg-p-font-serif:   {p.FontSerif};");
        sb.AppendLine($"    --sg-p-font-display: {p.FontDisplay};");
        sb.AppendLine($"    --sg-p-font-medical: {p.FontMedical};");

        sb.AppendLine($"    --sg-p-radius-none: {p.RadiusNone};");
        sb.AppendLine($"    --sg-p-radius-xs: {p.RadiusXs};");
        sb.AppendLine($"    --sg-p-radius-sm: {p.RadiusSm};");
        sb.AppendLine($"    --sg-p-radius-md: {p.RadiusMd};");
        sb.AppendLine($"    --sg-p-radius-lg: {p.RadiusLg};");
        sb.AppendLine($"    --sg-p-radius-xl: {p.RadiusXl};");
        sb.AppendLine($"    --sg-p-radius-2xl: {p.Radius2Xl};");
        sb.AppendLine($"    --sg-p-radius-full: {p.RadiusFull};");

        // v2.0 spacing scale (Fibonacci px).
        sb.AppendLine("    /* Spacing scale (Fibonacci) */");
        sb.AppendLine($"    --sg-p-spacing-0: {p.Spacing0};");
        sb.AppendLine($"    --sg-p-spacing-1: {p.Spacing1};");
        sb.AppendLine($"    --sg-p-spacing-2: {p.Spacing2};");
        sb.AppendLine($"    --sg-p-spacing-3: {p.Spacing3};");
        sb.AppendLine($"    --sg-p-spacing-4: {p.Spacing4};");
        sb.AppendLine($"    --sg-p-spacing-5: {p.Spacing5};");
        sb.AppendLine($"    --sg-p-spacing-6: {p.Spacing6};");
        sb.AppendLine($"    --sg-p-spacing-7: {p.Spacing7};");
        sb.AppendLine($"    --sg-p-spacing-8: {p.Spacing8};");

        // v2.0 icon size scale.
        sb.AppendLine("    /* Icon size scale */");
        sb.AppendLine($"    --sg-p-icon-size-sm:  {p.IconSizeSm};");
        sb.AppendLine($"    --sg-p-icon-size-md:  {p.IconSizeMd};");
        sb.AppendLine($"    --sg-p-icon-size-lg:  {p.IconSizeLg};");
        sb.AppendLine($"    --sg-p-icon-size-xl:  {p.IconSizeXl};");
        sb.AppendLine($"    --sg-p-icon-size-2xl: {p.IconSize2Xl};");

        // v2.0 border-width scale.
        sb.AppendLine("    /* Border width scale */");
        sb.AppendLine($"    --sg-p-border-width-default: {p.BorderWidthDefault};");
        sb.AppendLine($"    --sg-p-border-width-strong:  {p.BorderWidthStrong};");
        sb.AppendLine($"    --sg-p-border-width-accent:  {p.BorderWidthAccent};");

        // Semantic
        sb.AppendLine("    /* Semantic */");
        sb.AppendLine($"    --sg-bg: {s.BgDefault};");
        sb.AppendLine($"    --sg-bg-subtle: {s.BgSubtle};");
        sb.AppendLine($"    --sg-bg-muted: {s.BgMuted};");
        sb.AppendLine($"    --sg-bg-emphasized: {s.BgEmphasized};");
        sb.AppendLine($"    --sg-bg-overlay: {s.BgOverlay};");
        sb.AppendLine($"    --sg-bg-glass: {s.BgGlass};");
        sb.AppendLine($"    --sg-border-glass: {s.BorderGlass};");
        sb.AppendLine($"    --sg-blur-glass: {s.BlurGlass};");

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

        // v2.0 state tokens.
        sb.AppendLine("    /* v2.0 State tokens */");
        sb.AppendLine($"    --sg-color-primary-active-bg:   {s.ColorPrimaryActiveBg};");
        sb.AppendLine($"    --sg-color-primary-disabled:    {s.ColorPrimaryDisabled};");
        sb.AppendLine($"    --sg-color-primary-disabled-bg: {s.ColorPrimaryDisabledBg};");
        sb.AppendLine($"    --sg-color-primary-selected-bg: {s.ColorPrimarySelectedBg};");
        sb.AppendLine($"    --sg-color-success-active-bg:   {s.ColorSuccessActiveBg};");
        sb.AppendLine($"    --sg-color-success-disabled:    {s.ColorSuccessDisabled};");
        sb.AppendLine($"    --sg-color-danger-active-bg:    {s.ColorDangerActiveBg};");
        sb.AppendLine($"    --sg-color-danger-disabled:     {s.ColorDangerDisabled};");
        sb.AppendLine($"    --sg-color-warning-active-bg:   {s.ColorWarningActiveBg};");
        sb.AppendLine($"    --sg-color-warning-disabled:    {s.ColorWarningDisabled};");
        sb.AppendLine($"    --sg-color-info-active-bg:      {s.ColorInfoActiveBg};");
        sb.AppendLine($"    --sg-color-info-disabled:       {s.ColorInfoDisabled};");
        sb.AppendLine($"    --sg-fg-placeholder:            {s.FgPlaceholder};");
        sb.AppendLine($"    --sg-surface-hover:             {s.SurfaceHover};");
        sb.AppendLine($"    --sg-surface-active:            {s.SurfaceActive};");
        sb.AppendLine($"    --sg-surface-selected:          {s.SurfaceSelected};");
        sb.AppendLine($"    --sg-border-hover:              {s.BorderHover};");

        // v2.0 elevation scale.
        sb.AppendLine("    /* v2.0 Elevation scale */");
        sb.AppendLine($"    --sg-elevation-1: {s.Elevation1};");
        sb.AppendLine($"    --sg-elevation-2: {s.Elevation2};");
        sb.AppendLine($"    --sg-elevation-3: {s.Elevation3};");
        sb.AppendLine($"    --sg-elevation-4: {s.Elevation4};");
        sb.AppendLine($"    --sg-elevation-5: {s.Elevation5};");

        // v2.0 motion tokens (Fibonacci ms + easings).
        sb.AppendLine("    /* v2.0 Motion (Fibonacci ms) */");
        sb.AppendLine($"    --sg-motion-instant:        {s.MotionInstant};");
        sb.AppendLine($"    --sg-motion-fast:           {s.MotionFast};");
        sb.AppendLine($"    --sg-motion-base:           {s.MotionBase};");
        sb.AppendLine($"    --sg-motion-slow:           {s.MotionSlow};");
        sb.AppendLine($"    --sg-motion-slower:         {s.MotionSlower};");
        sb.AppendLine($"    --sg-easing-standard:       {s.EasingStandard};");
        sb.AppendLine($"    --sg-easing-emphasis:       {s.EasingEmphasis};");
        sb.AppendLine($"    --sg-easing-decel:          {s.EasingDecel};");

        // v2.0 density tokens.
        sb.AppendLine("    /* v2.0 Density */");
        sb.AppendLine($"    --sg-density-compact:       {s.DensityCompact};");
        sb.AppendLine($"    --sg-density-comfortable:   {s.DensityComfortable};");
        sb.AppendLine($"    --sg-density-spacious:      {s.DensitySpacious};");

        // v2.0 measure (ch) tokens.
        sb.AppendLine("    /* v2.0 Measure (ch) */");
        sb.AppendLine($"    --sg-measure-narrow:  {s.MeasureNarrow};");
        sb.AppendLine($"    --sg-measure-optimal: {s.MeasureOptimal};");
        sb.AppendLine($"    --sg-measure-wide:    {s.MeasureWide};");

        sb.AppendLine("}");
        return sb.ToString();
    }
}
