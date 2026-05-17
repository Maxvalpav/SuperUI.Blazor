namespace SuperUI.Themes;

/// <summary>
/// Tailwind CSS v4 / Tailwind UI design system theme for SuperUI.
/// Faithful to Tailwind's actual design language:
///   - Slate neutrals (not gray/zinc)
///   - Indigo-600 primary (Tailwind UI default)
///   - ring / ring-offset focus system
///   - Precise shadow scale matching Tailwind's shadow-* utilities
///   - Cubic-bezier easing matching Tailwind's transition-* utilities
///   - Compact sizing (text-sm everywhere, tight line-heights)
/// </summary>
public sealed class TailwindTheme : ThemeBase
{
    public override string Id          => "tailwind-v3";
    public override string Name        => "Tailwind CSS v3";
    public override string? Description => "Tailwind UI design system — Slate neutrals, Indigo primary, ring focus.";
    public override string? Author     => "SuperUI";
    public override string Version     => "4.0.0";

    protected override IThemePrimitives  CreatePrimitives()  => new TailwindPrimitives();
    protected override IThemeSemantic    CreateLight()        => new TailwindSemanticLight();
    protected override IThemeSemantic?   CreateDark()         => new TailwindSemanticDark();
    protected override IThemeComponents? CreateComponents()   => new TailwindComponents();

    public override string? AdditionalCss => """
        /* ═══════════════════════════════════════════════════════
           TAILWIND CSS — Ring focus system & component overrides
           ═══════════════════════════════════════════════════════ */

        /* Tailwind ring focus variables */
        :root {
            --tw-ring-color:        rgba(99, 102, 241, 0.5);   /* indigo-500/50 */
            --tw-ring-offset-color: #ffffff;
            --tw-ring-offset-width: 2px;
            --tw-ring-shadow:       0 0 0 calc(3px + var(--tw-ring-offset-width)) var(--tw-ring-color);
            --tw-ring-offset-shadow: 0 0 0 var(--tw-ring-offset-width) var(--tw-ring-offset-color);

            /* Tailwind shadow scale — exact values */
            --tw-shadow-sm:  0 1px 2px 0 rgb(0 0 0 / 0.05);
            --tw-shadow:     0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1);
            --tw-shadow-md:  0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
            --tw-shadow-lg:  0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1);
            --tw-shadow-xl:  0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1);
            --tw-shadow-2xl: 0 25px 50px -12px rgb(0 0 0 / 0.25);
            --tw-shadow-inner: inset 0 2px 4px 0 rgb(0 0 0 / 0.05);

            /* Tailwind prose-like spacing */
            --tw-prose-body:    #374151;
            --tw-prose-headings: #111827;
            --tw-prose-links:   #4f46e5;
        }

        [data-theme="dark"] {
            --tw-ring-offset-color: #0f172a;
            --tw-prose-body:    #d1d5db;
            --tw-prose-headings: #f9fafb;
            --tw-prose-links:   #818cf8;
        }

        /* ── Focus ring — Tailwind ring system ─────────────── */
        [data-theme-id="tailwind-v3"] *:focus-visible {
            outline: none !important;
            box-shadow:
                var(--tw-ring-offset-shadow),
                var(--tw-ring-shadow),
                var(--tw-shadow, 0 0 #0000) !important;
        }

        [data-theme-id="tailwind-v3"] .sgc-input:focus,
        [data-theme-id="tailwind-v3"] .sgc-select:focus,
        [data-theme-id="tailwind-v3"] .sgc-textarea:focus,
        [data-theme-id="tailwind-v3"] .sgc-combo.sgc-open .sgc-combo-control {
            outline: none !important;
            border-color: #6366f1 !important;
            box-shadow:
                var(--tw-ring-offset-shadow),
                var(--tw-ring-shadow),
                var(--tw-shadow-sm) !important;
        }

        /* ── Buttons — Tailwind UI style ───────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-btn {
            font-weight: 600 !important;
            font-size: 0.875rem !important;
            letter-spacing: 0 !important;
            transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1) !important;
            box-shadow: var(--tw-shadow-sm) !important;
        }

        /* Primary — indigo filled */
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-primary {
            background: #4f46e5 !important;
            border-color: #4f46e5 !important;
            color: #fff !important;
            box-shadow: var(--tw-shadow-sm) !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: #4338ca !important;
            border-color: #4338ca !important;
            transform: none !important;
            box-shadow: var(--tw-shadow) !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            background: #3730a3 !important;
        }

        /* Default — white with border */
        [data-theme-id="tailwind-v3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-ghost):not(.sgc-outlined) {
            background: #fff !important;
            border: 1px solid #d1d5db !important;
            color: #374151 !important;
            box-shadow: var(--tw-shadow-sm) !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-ghost):not(.sgc-outlined):hover:not(:disabled) {
            background: #f9fafb !important;
            transform: none !important;
        }

        /* Ghost / text */
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-ghost {
            background: transparent !important;
            border-color: transparent !important;
            color: #4f46e5 !important;
            box-shadow: none !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-ghost:hover:not(:disabled) {
            background: #eef2ff !important;
            transform: none !important;
        }

        /* Outlined */
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-outlined {
            background: transparent !important;
            border-color: #6366f1 !important;
            color: #4f46e5 !important;
            box-shadow: none !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-outlined:hover:not(:disabled) {
            background: #eef2ff !important;
            transform: none !important;
        }

        /* Dark mode buttons */
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-ghost):not(.sgc-outlined) {
            background: #1e293b !important;
            border-color: #334155 !important;
            color: #f1f5f9 !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-ghost):not(.sgc-outlined):hover:not(:disabled) {
            background: #334155 !important;
        }

        /* ── Inputs — Tailwind form style ──────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-input,
        [data-theme-id="tailwind-v3"] .sgc-select,
        [data-theme-id="tailwind-v3"] .sgc-textarea {
            background: #fff !important;
            border: 1px solid #d1d5db !important;
            border-radius: 6px !important;
            color: #111827 !important;
            font-size: 0.875rem !important;
            box-shadow: var(--tw-shadow-sm) !important;
            transition: border-color 150ms, box-shadow 150ms !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-input:hover,
        [data-theme-id="tailwind-v3"] .sgc-select:hover,
        [data-theme-id="tailwind-v3"] .sgc-textarea:hover {
            border-color: #9ca3af !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-input::placeholder,
        [data-theme-id="tailwind-v3"] .sgc-textarea::placeholder {
            color: #9ca3af !important;
        }

        /* Dark mode inputs */
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-input,
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-select,
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-textarea {
            background: #1e293b !important;
            border-color: #334155 !important;
            color: #f1f5f9 !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-input::placeholder,
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-textarea::placeholder {
            color: #64748b !important;
        }

        /* ── Cards — Tailwind UI card style ────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-card {
            background: #fff !important;
            border: 1px solid #e5e7eb !important;
            box-shadow: var(--tw-shadow) !important;
            border-radius: 12px !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-card:hover {
            box-shadow: var(--tw-shadow-md) !important;
        }

        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-card {
            background: #1e293b !important;
            border-color: #334155 !important;
        }

        /* ── Dropdown / Menu ────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-dropdown-menu {
            background: #fff !important;
            border: 1px solid #e5e7eb !important;
            border-radius: 8px !important;
            box-shadow: var(--tw-shadow-lg) !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-dropdown-item:hover {
            background: #f9fafb !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-dropdown-item.sgc-selected {
            background: #eef2ff !important;
            color: #4f46e5 !important;
        }

        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-dropdown-menu {
            background: #1e293b !important;
            border-color: #334155 !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-dropdown-item:hover {
            background: #334155 !important;
        }

        /* ── Table ──────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-table thead th {
            background: #f9fafb !important;
            font-size: 0.75rem !important;
            font-weight: 600 !important;
            text-transform: uppercase !important;
            letter-spacing: 0.05em !important;
            color: #6b7280 !important;
            border-bottom: 1px solid #e5e7eb !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-table tbody tr {
            border-bottom: 1px solid #f3f4f6 !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-table tbody tr:hover td {
            background: #f9fafb !important;
        }

        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-table thead th {
            background: #1e293b !important;
            color: #94a3b8 !important;
            border-bottom-color: #334155 !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-table tbody tr {
            border-bottom-color: #1e293b !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-table tbody tr:hover td {
            background: #1e293b !important;
        }

        /* ── Tabs ───────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-tabs-strip {
            background: transparent !important;
            border-bottom: 1px solid #e5e7eb !important;
            border-radius: 0 !important;
            padding: 0 !important;
            gap: 0 !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-tab-item {
            border-radius: 0 !important;
            font-size: 0.875rem !important;
            font-weight: 500 !important;
            color: #6b7280 !important;
            padding: 0 4px !important;
            margin-bottom: -1px !important;
            border-bottom: 2px solid transparent !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-tab-item:hover {
            color: #374151 !important;
            border-bottom-color: #d1d5db !important;
            background: transparent !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-tab-item.is-active {
            color: #4f46e5 !important;
            border-bottom: 2px solid #4f46e5 !important;
            background: transparent !important;
            box-shadow: none !important;
        }

        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-tabs-strip {
            border-bottom-color: #334155 !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-tab-item {
            color: #94a3b8 !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-tab-item:hover {
            color: #e2e8f0 !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-tab-item.is-active {
            color: #818cf8 !important;
            border-bottom-color: #818cf8 !important;
        }

        /* ── Badges / Tags ──────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sg-badge {
            font-size: 0.75rem !important;
            font-weight: 500 !important;
            border-radius: 9999px !important;
            padding: 2px 10px !important;
        }
        [data-theme-id="tailwind-v3"] .sg-badge-info {
            background: #eef2ff !important;
            color: #4338ca !important;
            border-color: #c7d2fe !important;
        }
        [data-theme-id="tailwind-v3"] .sg-badge-success {
            background: #f0fdf4 !important;
            color: #15803d !important;
            border-color: #bbf7d0 !important;
        }
        [data-theme-id="tailwind-v3"] .sg-badge-danger {
            background: #fef2f2 !important;
            color: #b91c1c !important;
            border-color: #fecaca !important;
        }
        [data-theme-id="tailwind-v3"] .sg-badge-warn {
            background: #fffbeb !important;
            color: #b45309 !important;
            border-color: #fde68a !important;
        }
        [data-theme-id="tailwind-v3"] .sg-badge-neutral {
            background: #f9fafb !important;
            color: #374151 !important;
            border-color: #e5e7eb !important;
        }

        /* ── Modal / Dialog ─────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-modal-content {
            background: #fff !important;
            border: none !important;
            border-radius: 12px !important;
            box-shadow: var(--tw-shadow-xl) !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-modal-content {
            background: #1e293b !important;
        }

        /* ── Toast / Snackbar ───────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-toast {
            background: #111827 !important;
            color: #f9fafb !important;
            border-radius: 8px !important;
            box-shadow: var(--tw-shadow-lg) !important;
            font-size: 0.875rem !important;
        }

        /* ── Tooltip ────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-tooltip,
        [data-theme-id="tailwind-v3"] .sg-tooltip-dark {
            background: #111827 !important;
            color: #f9fafb !important;
            border-radius: 6px !important;
            font-size: 0.75rem !important;
            box-shadow: var(--tw-shadow-md) !important;
        }

        /* ── Navigation ─────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-nav-item.is-active,
        [data-theme-id="tailwind-v3"] .sgc-nav-link.is-active {
            background: #eef2ff !important;
            color: #4f46e5 !important;
            border-radius: 6px !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-nav-item.is-active,
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-nav-link.is-active {
            background: rgba(99, 102, 241, 0.15) !important;
            color: #818cf8 !important;
        }

        /* ── Progress ───────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-progress {
            background: #e0e7ff !important;
            border-radius: 9999px !important;
            height: 6px !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-progress-fill {
            background: #4f46e5 !important;
            border-radius: 9999px !important;
        }

        /* ── Switch ─────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-switch-slider {
            background: #d1d5db !important;
            border-radius: 9999px !important;
        }
        [data-theme-id="tailwind-v3"] .sgc-switch input:checked + .sgc-switch-slider {
            background: #4f46e5 !important;
        }

        /* ── Divider ────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-divider,
        [data-theme-id="tailwind-v3"] hr {
            border-color: #e5e7eb !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] .sgc-divider,
        [data-theme="dark"][data-theme-id="tailwind-v3"] hr {
            border-color: #334155 !important;
        }

        /* ── Scrollbar ──────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] ::-webkit-scrollbar-thumb {
            background: #cbd5e1 !important;
            border-radius: 9999px !important;
        }
        [data-theme="dark"][data-theme-id="tailwind-v3"] ::-webkit-scrollbar-thumb {
            background: #334155 !important;
        }
        """;
}

internal class TailwindPrimitives : IThemePrimitives
{
    // Tailwind Slate — the neutral palette used by Tailwind UI
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

    // Tailwind Indigo — Tailwind UI primary
    public string Primary50  => "#EEF2FF";
    public string Primary100 => "#E0E7FF";
    public string Primary200 => "#C7D2FE";
    public string Primary300 => "#A5B4FC";
    public string Primary400 => "#818CF8";
    public string Primary500 => "#6366F1";
    public string Primary600 => "#4F46E5";
    public string Primary700 => "#4338CA";
    public string Primary800 => "#3730A3";
    public string Primary900 => "#312E81";

    // Tailwind Emerald
    public string Success50  => "#ECFDF5";
    public string Success100 => "#D1FAE5";
    public string Success500 => "#10B981";
    public string Success600 => "#059669";
    public string Success700 => "#047857";

    // Tailwind Red
    public string Danger50  => "#FEF2F2";
    public string Danger100 => "#FEE2E2";
    public string Danger500 => "#EF4444";
    public string Danger600 => "#DC2626";
    public string Danger700 => "#B91C1C";

    // Tailwind Amber
    public string Warning50  => "#FFFBEB";
    public string Warning100 => "#FEF3C7";
    public string Warning500 => "#F59E0B";
    public string Warning600 => "#D97706";

    // Tailwind Sky
    public string Info50  => "#F0F9FF";
    public string Info100 => "#E0F2FE";
    public string Info500 => "#0EA5E9";
    public string Info600 => "#0284C7";

    // Tailwind system font stack
    public string FontSans  => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
    public string FontMono  => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace";
    public string FontSerif => "ui-serif, Georgia, Cambria, 'Times New Roman', Times, serif";

    // Tailwind border-radius scale
    public string RadiusNone => "0px";
    public string RadiusXs   => "2px";    // rounded-sm
    public string RadiusSm   => "4px";    // rounded
    public string RadiusMd   => "6px";    // rounded-md
    public string RadiusLg   => "8px";    // rounded-lg
    public string RadiusXl   => "12px";   // rounded-xl
    public string Radius2Xl  => "16px";   // rounded-2xl
    public string RadiusFull => "9999px"; // rounded-full
}

/// <summary>Light mode — white backgrounds, slate text, indigo accents.</summary>
internal class TailwindSemanticLight : IThemeSemantic
{
    // Backgrounds
    public string BgDefault    => "#FFFFFF";
    public string BgSubtle     => "#F8FAFC";   // slate-50
    public string BgMuted      => "#F1F5F9";   // slate-100
    public string BgEmphasized => "#E2E8F0";   // slate-200
    public string BgOverlay    => "rgba(15, 23, 42, 0.5)";
    public string BgGlass      => "rgba(255, 255, 255, 0.60)";
    public string BorderGlass  => "rgba(255, 255, 255, 0.50)";
    public string BlurGlass    => "12px";

    // Surfaces
    public string Surface        => "#FFFFFF";
    public string SurfaceRaised  => "#FFFFFF";
    public string SurfaceOverlay => "#FFFFFF";

    // Foreground — slate scale
    public string FgDefault  => "#0F172A";   // slate-900
    public string FgSubtle   => "#475569";   // slate-600
    public string FgMuted    => "#94A3B8";   // slate-400
    public string FgDisabled => "#CBD5E1";   // slate-300
    public string FgInverse  => "#FFFFFF";
    public string FgLink     => "#4F46E5";   // indigo-600
    public string FgLinkHover => "#4338CA";  // indigo-700

    // Borders
    public string BorderDefault => "#E2E8F0";  // slate-200
    public string BorderSubtle  => "#F1F5F9";  // slate-100
    public string BorderStrong  => "#94A3B8";  // slate-400
    public string BorderFocus   => "#6366F1";  // indigo-500
    public string Divider       => "#E5E7EB";  // gray-200

    // Primary — Indigo (Tailwind UI default)
    public string ColorPrimary        => "#4F46E5";   // indigo-600
    public string ColorPrimarySubtle  => "#EEF2FF";   // indigo-50
    public string ColorPrimaryMuted   => "#E0E7FF";   // indigo-100
    public string ColorPrimaryHover   => "#4338CA";   // indigo-700
    public string ColorPrimaryActive  => "#3730A3";   // indigo-800
    public string ColorPrimaryFg      => "#FFFFFF";

    // Success — Emerald
    public string ColorSuccess       => "#059669";   // emerald-600
    public string ColorSuccessSubtle => "#ECFDF5";   // emerald-50
    public string ColorSuccessHover  => "#047857";   // emerald-700
    public string ColorSuccessFg     => "#FFFFFF";

    // Danger — Red
    public string ColorDanger        => "#DC2626";   // red-600
    public string ColorDangerSubtle  => "#FEF2F2";   // red-50
    public string ColorDangerHover   => "#B91C1C";   // red-700
    public string ColorDangerFg      => "#FFFFFF";

    // Warning — Amber
    public string ColorWarning       => "#D97706";   // amber-600
    public string ColorWarningSubtle => "#FFFBEB";   // amber-50
    public string ColorWarningHover  => "#B45309";   // amber-700
    public string ColorWarningFg     => "#FFFFFF";

    // Info — Sky
    public string ColorInfo        => "#0284C7";   // sky-600
    public string ColorInfoSubtle  => "#F0F9FF";   // sky-50
    public string ColorInfoHover   => "#0369A1";   // sky-700
    public string ColorInfoFg      => "#FFFFFF";

    // Typography — Tailwind system stack
    public string Font     => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
    public string FontMono => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string TextSm   => "0.875rem";   // text-sm
    public string TextBase => "1rem";       // text-base
    public string TextLg   => "1.125rem";   // text-lg

    // Tailwind shadow scale
    public string ShadowXs => "0 1px 2px 0 rgb(0 0 0 / 0.05)";
    public string ShadowSm => "0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)";
    public string ShadowMd => "0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)";
    public string ShadowLg => "0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)";
    public string ShadowXl => "0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)";

    // Tailwind border-radius
    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    // Tailwind easing
    public string TransitionFast => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    // Ring focus system
    public string FocusRing       => "0 0 0 2px #fff, 0 0 0 4px rgba(99, 102, 241, 0.5)";
    public string FocusRingDanger => "0 0 0 2px #fff, 0 0 0 4px rgba(239, 68, 68, 0.5)";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

/// <summary>Dark mode — slate-900 bg, slate-100 text, indigo-400 accents.</summary>
internal class TailwindSemanticDark : IThemeSemantic
{
    public string BgDefault    => "#0F172A";   // slate-900
    public string BgSubtle     => "#1E293B";   // slate-800
    public string BgMuted      => "#334155";   // slate-700
    public string BgEmphasized => "#475569";   // slate-600
    public string BgOverlay    => "rgba(0, 0, 0, 0.8)";
    public string BgGlass      => "rgba(30, 41, 59, 0.60)";
    public string BorderGlass  => "rgba(255, 255, 255, 0.10)";
    public string BlurGlass    => "12px";

    public string Surface        => "#1E293B";  // slate-800
    public string SurfaceRaised  => "#334155";  // slate-700
    public string SurfaceOverlay => "#1E293B";

    public string FgDefault  => "#F1F5F9";   // slate-100
    public string FgSubtle   => "#94A3B8";   // slate-400
    public string FgMuted    => "#64748B";   // slate-500
    public string FgDisabled => "#475569";   // slate-600
    public string FgInverse  => "#0F172A";
    public string FgLink     => "#818CF8";   // indigo-400
    public string FgLinkHover => "#A5B4FC";  // indigo-300

    public string BorderDefault => "#334155";  // slate-700
    public string BorderSubtle  => "#1E293B";  // slate-800
    public string BorderStrong  => "#64748B";  // slate-500
    public string BorderFocus   => "#818CF8";  // indigo-400
    public string Divider       => "#1E293B";  // slate-800

    // Primary — lighter indigo for dark bg
    public string ColorPrimary        => "#6366F1";   // indigo-500
    public string ColorPrimarySubtle  => "rgba(99, 102, 241, 0.15)";
    public string ColorPrimaryMuted   => "rgba(99, 102, 241, 0.25)";
    public string ColorPrimaryHover   => "#818CF8";   // indigo-400
    public string ColorPrimaryActive  => "#A5B4FC";   // indigo-300
    public string ColorPrimaryFg      => "#FFFFFF";

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

    public string ColorInfo        => "#0EA5E9";
    public string ColorInfoSubtle  => "rgba(14, 165, 233, 0.15)";
    public string ColorInfoHover   => "#38BDF8";
    public string ColorInfoFg      => "#FFFFFF";

    public string Font     => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
    public string FontMono => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string TextSm   => "0.875rem";
    public string TextBase => "1rem";
    public string TextLg   => "1.125rem";

    public string ShadowXs => "0 1px 2px 0 rgb(0 0 0 / 0.4)";
    public string ShadowSm => "0 1px 3px 0 rgb(0 0 0 / 0.5), 0 1px 2px -1px rgb(0 0 0 / 0.5)";
    public string ShadowMd => "0 4px 6px -1px rgb(0 0 0 / 0.5), 0 2px 4px -2px rgb(0 0 0 / 0.5)";
    public string ShadowLg => "0 10px 15px -3px rgb(0 0 0 / 0.5), 0 4px 6px -4px rgb(0 0 0 / 0.5)";
    public string ShadowXl => "0 20px 25px -5px rgb(0 0 0 / 0.6), 0 8px 10px -6px rgb(0 0 0 / 0.6)";

    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing       => "0 0 0 2px #0f172a, 0 0 0 4px rgba(99, 102, 241, 0.5)";
    public string FocusRingDanger => "0 0 0 2px #0f172a, 0 0 0 4px rgba(239, 68, 68, 0.5)";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

internal class TailwindComponents : IThemeComponents
{
    // Tailwind UI button sizing
    public string BtnRadius     => "6px";      // rounded-md
    public string BtnFontSize   => "0.875rem"; // text-sm
    public string BtnFontWeight => "600";      // font-semibold
    public string BtnHeight     => "36px";     // h-9
    public string BtnHeightSm   => "28px";     // h-7
    public string BtnHeightLg   => "40px";     // h-10

    // Tailwind form inputs
    public string InputRadius   => "6px";
    public string InputFontSize => "0.875rem";
    public string InputHeight   => "36px";
    public string InputHeightSm => "28px";
    public string InputHeightLg => "40px";

    // Cards
    public string CardRadius      => "12px";   // rounded-xl
    public string CardPadding     => "24px";   // p-6
    public string CardBorderColor => "#E2E8F0";
    public string CardBg          => "#FFFFFF";

    // Modal
    public string ModalRadius => "12px";

    // Table
    public string TableRadius          => "8px";
    public string TableHeaderFontWeight => "600";

    // Tabs
    public string TabsIndicatorHeight => "2px";
    public string TooltipMaxWidth     => "256px";

    // Navigation
    public string HeaderBg    => "#FFFFFF";
    public string HeaderFg    => "#0F172A";
    public string NavBg       => "#FFFFFF";
    public string NavFg       => "#475569";
    public string NavActiveBg => "#EEF2FF";   // indigo-50
    public string NavActiveFg => "#4F46E5";   // indigo-600
}
