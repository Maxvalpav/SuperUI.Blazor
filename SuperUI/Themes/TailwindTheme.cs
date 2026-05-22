namespace SuperUI.Themes;

/// <summary>
/// Tailwind UI design system theme for SuperUI.
/// Mirrors Tailwind's actual design language as shipped in the official Tailwind UI kit:
/// Slate neutrals, Indigo-600 primary, tight text-sm sizing, white-on-white surfaces
/// with subtle slate borders, ring/ring-offset focus, no transform-on-hover for buttons
/// (Tailwind UI uses pure color shifts), and the canonical shadow scale matching
/// shadow-sm/md/lg/xl utilities exactly.
/// </summary>
public sealed class TailwindTheme : ThemeBase
{
    public override string Id          => "tailwind-v3";
    public override string Name        => "Tailwind CSS v3";
    public override string? Description => "Tailwind UI: Slate + Indigo, ring-focus, чистые поверхности, плотная типографика.";
    public override string? Author     => "SuperUI";
    public override string Version     => "4.1.0";

    protected override IThemePrimitives  CreatePrimitives()  => new TailwindPrimitives();
    protected override IThemeSemantic    CreateLight()        => new TailwindSemanticLight();
    protected override IThemeSemantic?   CreateDark()         => new TailwindSemanticDark();
    protected override IThemeComponents? CreateComponents()   => new TailwindComponents();

    public override string? AdditionalCss => """
        /* ═══════════════════════════════════════════════════════════════
           TAILWIND UI — Slate + Indigo, canonical component patterns
           ═══════════════════════════════════════════════════════════════ */

        [data-theme-id="tailwind-v3"] {
            font-family: ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont,
                         'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            letter-spacing: -0.005em;
        }

        /* ── Shell ──────────────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sui-shell,
        [data-theme-id="tailwind-v3"] .sui-main,
        [data-theme-id="tailwind-v3"] .sui-content {
            background: var(--sg-bg) !important;
        }

        /* ── Cards ──────────────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border);
            border-radius: 8px;                       /* rounded-lg */
            box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05);
            transition: box-shadow 150ms cubic-bezier(0.4, 0, 0.2, 1);
        }
        [data-theme-id="tailwind-v3"] .sgc-card:hover {
            box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1),
                        0 2px 4px -2px rgb(0 0 0 / 0.1);
        }
        [data-theme-id="tailwind-v3"] .sgc-card-outlined {
            box-shadow: none;
            border-color: var(--sg-border);
        }
        [data-theme-id="tailwind-v3"] .sgc-card-filled {
            background: var(--sg-bg-subtle);
            border: none;
            box-shadow: none;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-card {
            background: var(--sg-surface);
            border-color: var(--sg-border);
        }

        /* ── Buttons (Tailwind UI: solid, ghost, soft) ──────────────── */
        [data-theme-id="tailwind-v3"] .sgc-btn {
            border-radius: 6px;                       /* rounded-md */
            font-weight: 600;
            font-size: 0.875rem;                      /* text-sm */
            line-height: 1.25rem;
            padding: 0 12px;
            height: 36px;                             /* h-9 */
            transition: background-color 150ms cubic-bezier(0.4, 0, 0.2, 1),
                        color 150ms cubic-bezier(0.4, 0, 0.2, 1),
                        box-shadow 150ms cubic-bezier(0.4, 0, 0.2, 1);
        }

        /* Primary (filled indigo) — bg-indigo-600 hover:bg-indigo-500 */
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-primary {
            background: #4F46E5;                      /* indigo-600 */
            color: #fff;
            border: 1px solid transparent;
            box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05);
        }
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: #6366F1;                      /* indigo-500 */
            transform: none;
        }
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-primary:focus-visible {
            outline: 2px solid #6366F1;
            outline-offset: 2px;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-btn.sgc-btn-primary {
            background: #6366F1;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: #818CF8;
        }

        /* Default (white with slate-300 border) — Tailwind UI "Secondary button" */
        [data-theme-id="tailwind-v3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost) {
            background: #fff;
            color: #0F172A;
            border: 1px solid #CBD5E1;                /* slate-300 (ring-1) */
            box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05);
        }
        [data-theme-id="tailwind-v3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):hover:not(:disabled) {
            background: #F8FAFC;                      /* slate-50 */
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost) {
            background: #1E293B;                      /* slate-800 */
            color: #F1F5F9;
            border-color: #475569;                    /* slate-600 */
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):hover:not(:disabled) {
            background: #334155;                      /* slate-700 */
        }

        /* Ghost / text button */
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-ghost {
            background: transparent;
            color: #4F46E5;
            border: 1px solid transparent;
            box-shadow: none;
            padding: 0 8px;
        }
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-ghost:hover:not(:disabled) {
            background: #EEF2FF;                      /* indigo-50 */
            color: #4338CA;
        }

        /* Outlined */
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-outlined {
            background: #fff;
            color: #0F172A;
            border: 1px solid #CBD5E1;
            box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05);
        }
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-outlined:hover:not(:disabled) {
            background: #F8FAFC;
            border-color: #94A3B8;                    /* slate-400 */
        }

        /* Danger */
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-danger {
            background: #DC2626;                      /* red-600 */
            color: #fff;
            border: 1px solid transparent;
            box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05);
        }
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-danger:hover:not(:disabled) {
            background: #B91C1C;                      /* red-700 */
        }

        /* ── Inputs (Tailwind UI text-sm form pattern) ──────────────── */
        [data-theme-id="tailwind-v3"] .sgc-input,
        [data-theme-id="tailwind-v3"] .sgc-select,
        [data-theme-id="tailwind-v3"] .sgc-textarea {
            background: #fff;
            color: #0F172A;
            border: 1px solid #CBD5E1;                /* slate-300 (= ring-1 ring-inset) */
            border-radius: 6px;                       /* rounded-md */
            padding: 6px 12px;
            font-size: 0.875rem;                      /* text-sm */
            line-height: 1.25rem;
            box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05);
            transition: border-color 150ms cubic-bezier(0.4, 0, 0.2, 1),
                        box-shadow    150ms cubic-bezier(0.4, 0, 0.2, 1);
        }
        [data-theme-id="tailwind-v3"] .sgc-input::placeholder,
        [data-theme-id="tailwind-v3"] .sgc-textarea::placeholder {
            color: #94A3B8;                           /* slate-400 */
        }
        [data-theme-id="tailwind-v3"] .sgc-input:focus,
        [data-theme-id="tailwind-v3"] .sgc-select:focus,
        [data-theme-id="tailwind-v3"] .sgc-textarea:focus {
            outline: none;
            border-color: #4F46E5;                    /* indigo-600 ring */
            box-shadow: 0 0 0 1px #4F46E5;            /* focus:ring-2 ring-indigo-600 */
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-input,
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-select,
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-textarea {
            background: #1E293B;
            color: #F1F5F9;
            border-color: #475569;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-input:focus,
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-select:focus,
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-textarea:focus {
            border-color: #6366F1;
            box-shadow: 0 0 0 1px #6366F1;
        }

        /* ── Top app bar (Tailwind UI: white shell + slate-200 border) ─ */
        [data-theme-id="tailwind-v3"] .sgc-header {
            background: #fff;
            border-bottom: 1px solid #E2E8F0;         /* slate-200 */
            height: 64px;
            padding: 0 16px;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-header {
            background: #0F172A;                      /* slate-900 */
            border-bottom-color: #1E293B;             /* slate-800 */
        }

        /* ── Sidebar (Tailwind UI Application Shell — dark slate-900) ─ */
        [data-theme-id="tailwind-v3"] .sgc-nav {
            background: #0F172A;                      /* slate-900 */
            color: #E2E8F0;
            border-right: 1px solid #1E293B;
        }
        [data-theme-id="tailwind-v3"] .sgc-nav-link {
            border-left: none !important;
            margin: 2px 8px;
            padding: 8px 12px;
            border-radius: 6px;
            font-weight: 500;
            font-size: 0.875rem;
            color: #94A3B8;                            /* slate-400 */
            transition: background 150ms ease, color 150ms ease;
        }
        [data-theme-id="tailwind-v3"] .sgc-nav-link:hover {
            background: #1E293B;                       /* slate-800 */
            color: #fff;
        }
        [data-theme-id="tailwind-v3"] .sgc-nav-link.active {
            background: #1E293B;                       /* Tailwind UI: bg-slate-800 */
            color: #fff;
            font-weight: 600;
        }
        [data-theme-id="tailwind-v3"] .sgc-nav-link.active .sgc-nav-icon {
            color: #fff;
        }
        [data-theme-id="tailwind-v3"] .sgc-nav-icon {
            color: #94A3B8;
            opacity: 1;
            margin-right: 12px;
        }
        [data-theme-id="tailwind-v3"] .sgc-nav-section {
            color: #64748B;                            /* slate-500 */
            padding: 20px 16px 8px;
            font-size: 0.6875rem;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }
        [data-theme-id="tailwind-v3"] .sgc-nav-group-header {
            margin: 2px 8px;
            padding: 8px 12px;
            border-radius: 6px;
            color: #E2E8F0;
            font-size: 0.875rem;
            font-weight: 500;
        }
        [data-theme-id="tailwind-v3"] .sgc-nav-group-header:hover {
            background: #1E293B;
            color: #fff;
        }

        /* ── Modal / Drawer ─────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-modal-content,
        [data-theme-id="tailwind-v3"] .sgc-drawer-content {
            background: #fff;
            border-radius: 8px;                       /* rounded-lg */
            box-shadow: 0 25px 50px -12px rgb(0 0 0 / 0.25);  /* shadow-2xl */
            border: 1px solid #E2E8F0;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-modal-content,
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-drawer-content {
            background: #1E293B;
            border-color: #334155;
        }

        /* ── Alerts (Tailwind UI Banner pattern) ────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-alert {
            border: 1px solid transparent;
            border-radius: 6px;
            padding: 12px 16px;
            font-size: 0.875rem;
            box-shadow: none;
        }
        [data-theme-id="tailwind-v3"] .sgc-alert.sgc-info    { background: #F0F9FF; border-color: #BAE6FD; color: #0369A1; }
        [data-theme-id="tailwind-v3"] .sgc-alert.sgc-success { background: #F0FDF4; border-color: #BBF7D0; color: #15803D; }
        [data-theme-id="tailwind-v3"] .sgc-alert.sgc-warn    { background: #FFFBEB; border-color: #FDE68A; color: #B45309; }
        [data-theme-id="tailwind-v3"] .sgc-alert.sgc-danger  { background: #FEF2F2; border-color: #FECACA; color: #B91C1C; }

        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-alert.sgc-info    { background: rgba(14, 165, 233, 0.10); border-color: rgba(14, 165, 233, 0.30); color: #7DD3FC; }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-alert.sgc-success { background: rgba(16, 185, 129, 0.10); border-color: rgba(16, 185, 129, 0.30); color: #6EE7B7; }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-alert.sgc-warn    { background: rgba(245, 158, 11, 0.10); border-color: rgba(245, 158, 11, 0.30); color: #FCD34D; }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-alert.sgc-danger  { background: rgba(239, 68, 68, 0.10);  border-color: rgba(239, 68, 68, 0.30);  color: #FCA5A5; }

        /* ── Tabs (Tailwind UI underlined tabs) ─────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 1px solid #E2E8F0;
            border-radius: 0;
            padding: 0;
            gap: 32px;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-tabs-strip {
            border-bottom-color: #334155;
        }
        [data-theme-id="tailwind-v3"] .sgc-tab {
            border-radius: 0;
            padding: 12px 0;
            border-bottom: 2px solid transparent;
            margin-bottom: -1px;
            font-weight: 500;
            font-size: 0.875rem;
            color: #64748B;                            /* slate-500 */
            transition: color 150ms ease, border-color 150ms ease;
        }
        [data-theme-id="tailwind-v3"] .sgc-tab:hover {
            color: #334155;                            /* slate-700 */
            border-bottom-color: #CBD5E1;              /* slate-300 */
        }
        [data-theme-id="tailwind-v3"] .sgc-tab.sgc-active {
            color: #4F46E5;                            /* indigo-600 */
            border-bottom-color: #4F46E5;
            background: transparent;
            box-shadow: none;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-tab {
            color: #94A3B8;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-tab:hover {
            color: #E2E8F0;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-tab.sgc-active {
            color: #818CF8;                            /* indigo-400 */
            border-bottom-color: #818CF8;
        }

        /* ── Badges / Chips (Tailwind UI Badge pattern) ────────────── */
        [data-theme-id="tailwind-v3"] .sgc-chip,
        [data-theme-id="tailwind-v3"] .sgc-badge {
            background: #F1F5F9;                       /* slate-100 */
            color: #334155;                            /* slate-700 */
            border: 1px solid transparent;
            border-radius: 9999px;
            padding: 2px 10px;
            font-size: 0.75rem;                        /* text-xs */
            font-weight: 500;
            line-height: 1rem;
        }
        [data-theme-id="tailwind-v3"] .sgc-chip.sgc-chip-selected {
            background: #EEF2FF;                       /* indigo-50 */
            color: #3730A3;                            /* indigo-800 */
            border-color: #C7D2FE;                     /* indigo-200 */
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-chip,
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-badge {
            background: rgba(255, 255, 255, 0.10);
            color: #E2E8F0;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-chip.sgc-chip-selected {
            background: rgba(99, 102, 241, 0.20);
            color: #C7D2FE;
            border-color: rgba(99, 102, 241, 0.40);
        }

        /* ── Table (Tailwind UI table pattern) ──────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-table thead th {
            background: transparent;
            border-bottom: 1px solid #E2E8F0;
            color: #0F172A;
            font-weight: 600;
            font-size: 0.875rem;
            text-align: left;
            padding: 12px;
        }
        [data-theme-id="tailwind-v3"] .sgc-table tbody td {
            border-bottom: 1px solid #F1F5F9;
            padding: 12px;
            font-size: 0.875rem;
        }
        [data-theme-id="tailwind-v3"] .sgc-table tbody tr:hover td {
            background: #F8FAFC;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-table thead th {
            border-bottom-color: #334155;
            color: #F1F5F9;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-table tbody td {
            border-bottom-color: #1E293B;
            color: #E2E8F0;
        }
        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-table tbody tr:hover td {
            background: #1E293B;
        }

        /* ── Focus ring helper (Tailwind ring-2 ring-offset-2) ──────── */
        [data-theme-id="tailwind-v3"] *:focus-visible {
            outline: none;
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

    // Tailwind border-radius scale (rounded-sm…2xl)
    public string RadiusNone => "0px";
    public string RadiusXs   => "2px";    // rounded-sm
    public string RadiusSm   => "4px";    // rounded
    public string RadiusMd   => "6px";    // rounded-md
    public string RadiusLg   => "8px";    // rounded-lg
    public string RadiusXl   => "12px";   // rounded-xl
    public string Radius2Xl  => "16px";   // rounded-2xl
    public string RadiusFull => "9999px"; // rounded-full
}

/// <summary>Tailwind UI light mode — white shell, slate text, indigo accents.</summary>
internal class TailwindSemanticLight : IThemeSemantic
{
    // Backgrounds
    public string BgDefault    => "#FFFFFF";
    public string BgSubtle     => "#F8FAFC";   // slate-50
    public string BgMuted      => "#F1F5F9";   // slate-100
    public string BgEmphasized => "#E2E8F0";   // slate-200
    public string BgOverlay    => "rgba(15, 23, 42, 0.50)";  // slate-900 / 50
    public string BgGlass      => "rgba(255, 255, 255, 0.60)";
    public string BorderGlass  => "rgba(255, 255, 255, 0.50)";
    public string BlurGlass    => "12px";

    public string Surface        => "#FFFFFF";
    public string SurfaceRaised  => "#FFFFFF";
    public string SurfaceOverlay => "#FFFFFF";

    // Foreground — slate scale
    public string FgDefault   => "#0F172A";   // slate-900
    public string FgSubtle    => "#475569";   // slate-600
    public string FgMuted     => "#94A3B8";   // slate-400
    public string FgDisabled  => "#CBD5E1";   // slate-300
    public string FgInverse   => "#FFFFFF";
    public string FgLink      => "#4F46E5";   // indigo-600
    public string FgLinkHover => "#4338CA";   // indigo-700

    // Borders
    public string BorderDefault => "#E2E8F0";  // slate-200
    public string BorderSubtle  => "#F1F5F9";  // slate-100
    public string BorderStrong  => "#CBD5E1";  // slate-300
    public string BorderFocus   => "#4F46E5";  // indigo-600
    public string Divider       => "#E2E8F0";  // slate-200

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
    public string ColorInfo        => "#0284C7";    // sky-600
    public string ColorInfoSubtle  => "#F0F9FF";    // sky-50
    public string ColorInfoHover   => "#0369A1";    // sky-700
    public string ColorInfoFg      => "#FFFFFF";

    // Typography — Tailwind system stack
    public string Font     => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
    public string FontMono => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string TextSm   => "0.75rem";     // text-xs
    public string TextBase => "0.875rem";    // text-sm (Tailwind UI default for forms)
    public string TextLg   => "1rem";        // text-base

    // Tailwind shadow scale (rgba via rgb()/% syntax matches Tailwind v3)
    public string ShadowXs => "0 1px 2px 0 rgb(0 0 0 / 0.05)";
    public string ShadowSm => "0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)";
    public string ShadowMd => "0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)";
    public string ShadowLg => "0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)";
    public string ShadowXl => "0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)";

    // Tailwind border-radius (rounded-md default for forms/buttons, rounded-lg for cards)
    public string RadiusSm   => "4px";    // rounded
    public string RadiusMd   => "6px";    // rounded-md
    public string RadiusLg   => "8px";    // rounded-lg
    public string RadiusXl   => "12px";   // rounded-xl
    public string RadiusFull => "9999px";

    // Tailwind easing
    public string TransitionFast => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    // Ring focus system (ring-2 ring-indigo-600 ring-offset-2)
    public string FocusRing       => "0 0 0 2px #fff, 0 0 0 4px #4F46E5";
    public string FocusRingDanger => "0 0 0 2px #fff, 0 0 0 4px #DC2626";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

/// <summary>Tailwind UI dark mode — slate-900 bg, slate-100 text, indigo-400 accents.</summary>
internal class TailwindSemanticDark : IThemeSemantic
{
    public string BgDefault    => "#0F172A";   // slate-900
    public string BgSubtle     => "#1E293B";   // slate-800
    public string BgMuted      => "#334155";   // slate-700
    public string BgEmphasized => "#475569";   // slate-600
    public string BgOverlay    => "rgba(0, 0, 0, 0.75)";
    public string BgGlass      => "rgba(30, 41, 59, 0.60)";
    public string BorderGlass  => "rgba(255, 255, 255, 0.10)";
    public string BlurGlass    => "12px";

    public string Surface        => "#1E293B";  // slate-800
    public string SurfaceRaised  => "#334155";  // slate-700
    public string SurfaceOverlay => "#1E293B";

    public string FgDefault   => "#F1F5F9";   // slate-100
    public string FgSubtle    => "#94A3B8";   // slate-400
    public string FgMuted     => "#64748B";   // slate-500
    public string FgDisabled  => "#475569";   // slate-600
    public string FgInverse   => "#0F172A";
    public string FgLink      => "#818CF8";   // indigo-400
    public string FgLinkHover => "#A5B4FC";   // indigo-300

    public string BorderDefault => "#334155";  // slate-700
    public string BorderSubtle  => "#1E293B";  // slate-800
    public string BorderStrong  => "#475569";  // slate-600
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
    public string TextSm   => "0.75rem";
    public string TextBase => "0.875rem";
    public string TextLg   => "1rem";

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

    // Ring on slate-900 background
    public string FocusRing       => "0 0 0 2px #0F172A, 0 0 0 4px #6366F1";
    public string FocusRingDanger => "0 0 0 2px #0F172A, 0 0 0 4px #EF4444";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

internal class TailwindComponents : IThemeComponents
{
    // Tailwind UI button — h-9 px-3 text-sm font-semibold rounded-md
    public string BtnRadius     => "6px";       // rounded-md
    public string BtnFontSize   => "0.875rem";  // text-sm
    public string BtnFontWeight => "600";       // font-semibold
    public string BtnHeight     => "36px";      // h-9
    public string BtnHeightSm   => "28px";      // h-7
    public string BtnHeightLg   => "40px";      // h-10

    // Tailwind UI form input — h-9 px-3 text-sm rounded-md ring-1 ring-slate-300
    public string InputRadius   => "6px";
    public string InputFontSize => "0.875rem";
    public string InputHeight   => "36px";
    public string InputHeightSm => "28px";
    public string InputHeightLg => "40px";

    // Tailwind UI card — rounded-lg shadow + border slate-200
    public string CardRadius      => "8px";       // rounded-lg
    public string CardPadding     => "20px";      // p-5 (more reasonable than p-6)
    public string CardBorderColor => "var(--sg-border)";
    public string CardBg          => "var(--sg-surface)";

    // Tailwind UI modal — rounded-lg shadow-2xl
    public string ModalRadius => "8px";

    public string TableRadius          => "8px";
    public string TableHeaderFontWeight => "600";

    public string TabsIndicatorHeight => "2px";
    public string TooltipMaxWidth     => "256px";

    // Navigation — Tailwind UI Application Shell sidebar = slate-900
    public string HeaderBg    => "var(--sg-surface)";
    public string HeaderFg    => "var(--sg-fg)";
    public string NavBg       => "#0F172A";    // slate-900
    public string NavFg       => "#94A3B8";    // slate-400
    public string NavActiveBg => "#1E293B";    // slate-800
    public string NavActiveFg => "#FFFFFF";
}
