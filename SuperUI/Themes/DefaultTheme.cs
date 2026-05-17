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
        /* ═══════════════════════════════════════════════════════════════════════════ 
           superui-components.css 
           Глобальные стили и общие паттерны компонентов SuperUI. 
           Использует семантические токены --sg-*. 
           ═══════════════════════════════════════════════════════════════════════════ */ 
        
        /* ── Алиасы токенов для обратной совместимости ───────────────────────────── */ 
        :root,
        [data-theme="light"],
        [data-theme="dark"] { 
            /* Backward-compat aliases: --sui-* → --sg-* (mapping to internal theme tokens) */
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

            --sui-hover-bg:    rgba(0, 0, 0, 0.04);
            --sui-active-bg:   rgba(0, 0, 0, 0.08);
            --sui-selected-bg: var(--sg-color-primary-muted);

            --sui-font-family:   var(--sg-font);
            --sui-font-size-xs:   var(--sg-text-xs);
            --sui-font-size-sm:   var(--sg-text-sm);
            --sui-font-size-base: var(--sg-text-base);
            --sui-font-size-lg:   var(--sg-text-lg);

            --sui-radius-sm:   var(--sg-radius-sm);
            --sui-radius-md:   var(--sg-radius-md);
            --sui-radius-lg:   var(--sg-radius-lg);
            --sui-radius-full: var(--sg-radius-full);

            /* Legacy variable aliases - map to new theme system */ 
            --sui-bg: var(--sui-bg-primary);
            --sui-bg-alt: var(--sui-bg-secondary); 
            --sui-bg-hover: var(--sui-hover-bg);
            --sui-fg: var(--sui-text-primary); 
            --sui-text: var(--sui-text-primary); 
            --sui-muted: var(--sui-text-muted); 
            --sui-border-strong: var(--sui-border-hover); 
            --sui-border-soft: var(--sui-border); 
            --sui-accent-soft: var(--sui-selected-bg); 
            --sui-toolbar-bg: var(--sui-bg-secondary); 
            --sui-hover: var(--sui-hover-bg); 
            --sui-danger-hover: #e11d48; 
            --sui-disabled: var(--sui-text-disabled); 
            --sui-focus: 0 0 0 2px rgba(0, 111, 238, 0.2); 
            --sui-primary: var(--sui-accent); 
            --sui-font: var(--sui-font-family); 
            --sui-radius: var(--sui-radius-md); 
            --sui-end-translate: 100%; 
            --sui-start-translate: -100%; 

            /* Shared design tokens for components using --sg-* naming */ 
            --sg-bg-primary:      var(--sg-bg); 
            --sg-bg-secondary:    var(--sg-bg-subtle); 
            --sg-bg-tertiary:     var(--sg-bg-muted); 
            --sg-text-primary:    var(--sg-fg); 
            --sg-text-secondary:  var(--sg-fg-subtle); 
            --sg-border-color:    var(--sg-border); 
            --sg-border-radius:   var(--sg-radius-md); 
            --sg-border-radius-sm: var(--sg-radius-sm); 
            --sg-font-family:     var(--sg-font); 
            --sg-primary:         var(--sg-color-primary); 
            --sg-primary-hover:   var(--sg-color-primary-hover); 
            --sg-danger:          var(--sg-color-danger); 
            --sg-success:         var(--sg-color-success); 
            --sg-warn:            var(--sg-color-warning); 
            --sg-muted:           var(--sg-fg-muted); 
        } 
        
        [dir="rtl"] { 
            --sui-end-translate: -100%; 
            --sui-start-translate: 100%; 
        } 

        /* ── Контейнер с рамкой и тенью (карточка-обёртка) ──────────────────────── */ 
        .sg-panel-container { 
            position: relative; 
            border: 1px solid var(--sg-border); 
            border-radius: var(--sg-radius-lg); 
            overflow: hidden; 
            box-shadow: var(--sg-shadow-sm); 
            transition: box-shadow var(--sg-transition-base), transform var(--sg-transition-base); 
            box-sizing: border-box; 
            background: var(--sg-surface);
        } 
        
        .sg-panel-container:hover { 
            box-shadow: var(--sg-shadow-md); 
        } 
        
        /* ── Абсолютный оверлей (loading / error поверх canvas) ──────────────────── */ 
        .sg-overlay { 
            position: absolute; 
            inset: 0; 
            display: flex; 
            flex-direction: column; 
            align-items: center; 
            justify-content: center; 
            gap: 16px; 
        } 
        
        /* ── Skeleton-анимация (5 баров) ─────────────────────────────────────────── */ 
        .sg-skeleton-bars { 
            display: flex; 
            align-items: flex-end; 
            gap: 6px; 
            height: 48px; 
        } 
        
        .sg-skeleton-bars span { 
            width: 10px; 
            border-radius: 3px 3px 0 0; 
            background: var(--sg-border); 
            animation: sg-bar-pulse 1.2s ease-in-out infinite; 
        } 
        
        .sg-skeleton-bars span:nth-child(1) { height: 60%; animation-delay: 0s;   } 
        .sg-skeleton-bars span:nth-child(2) { height: 90%; animation-delay: 0.1s; } 
        .sg-skeleton-bars span:nth-child(3) { height: 45%; animation-delay: 0.2s; } 
        .sg-skeleton-bars span:nth-child(4) { height: 75%; animation-delay: 0.3s; } 
        .sg-skeleton-bars span:nth-child(5) { height: 55%; animation-delay: 0.4s; } 
        
        @keyframes sg-bar-pulse { 
            0%, 100% { opacity: 0.3; } 
            50%       { opacity: 1;   } 
        } 
        
        /* ── Текст под skeleton ──────────────────────────────────────────────────── */ 
        .sg-loading-text { 
            color: var(--sg-fg-subtle, #6b7280); 
            font-size: 13px; 
        } 
        
        /* ── Toolbar (fade-in при hover на контейнер) ────────────────────────────── */ 
        .sg-hover-toolbar { 
            position: absolute; 
            top: 8px; 
            right: 8px; 
            display: flex; 
            gap: 4px; 
            z-index: 2; 
            opacity: 0; 
            transform: translateY(-4px); 
            transition: opacity 0.18s ease, transform 0.18s ease; 
            pointer-events: none; 
        } 
        
        .sg-panel-container:hover .sg-hover-toolbar, 
        .sg-panel-container:focus-within .sg-hover-toolbar { 
            opacity: 1; 
            transform: translateY(0); 
            pointer-events: auto; 
        } 
        
        @media (max-width: 480px) { 
            .sg-hover-toolbar { 
                opacity: 1; 
                transform: none; 
                pointer-events: auto; 
            } 
        } 
        
        /* ── Кнопка-иконка в toolbar ─────────────────────────────────────────────── */ 
        .sg-tool-btn { 
            display: inline-flex; 
            align-items: center; 
            justify-content: center; 
            width: 28px; 
            height: 28px; 
            border: 1px solid var(--sg-border); 
            background: var(--sgc-card-bg, var(--sg-bg)); 
            color: var(--sg-fg-subtle); 
            border-radius: var(--sg-radius-md, 4px); 
            cursor: pointer; 
            transition: background 0.15s, color 0.15s, border-color 0.15s; 
            padding: 0; 
        } 
        
        .sg-tool-btn:hover, 
        .sg-tool-btn:focus-visible { 
            background: var(--sg-color-primary); 
            color: #fff; 
            border-color: var(--sg-color-primary); 
            outline: none; 
        } 
        
        /* ── CSS Spinner ─────────────────────────────────────────────────────────── */ 
        .sg-spinner { 
            display: inline-block; 
            width: 20px; 
            height: 20px; 
            border: 2px solid var(--sg-border, #e5e7eb); 
            border-top-color: var(--sg-color-primary, #006fee); 
            border-radius: 50%; 
            animation: sg-spin 0.7s linear infinite; 
            flex-shrink: 0; 
        } 
        
        .sg-spinner-sm { 
            width: 13px; 
            height: 13px; 
            border-width: 2px; 
            border-color: rgba(255,255,255,0.35); 
            border-top-color: #fff; 
        } 
        
        @keyframes sg-spin { 
            to { transform: rotate(360deg); } 
        } 
        
        /* ── Блок ошибки ─────────────────────────────────────────────────────────── */ 
        .sg-error-state { 
            display: flex; 
            flex-direction: column; 
            align-items: center; 
            justify-content: center; 
            gap: 12px; 
            padding: 24px; 
            height: 100%; 
            color: var(--sg-color-danger, #ef4444); 
            font-size: 13px; 
            text-align: center; 
        } 
        
        /* ── Блок ошибки (строчный, с иконкой) ──────────────────────────────────── */ 
        .sg-error-alert { 
            display: flex; 
            align-items: center; 
            gap: 8px; 
            padding: 10px 14px; 
            background: rgba(239, 68, 68, 0.08); 
            border: 1px solid rgba(239, 68, 68, 0.3); 
            border-radius: var(--sg-radius-md, 6px); 
            color: var(--sg-color-danger, #ef4444); 
            font-size: 13px; 
        } 
        
        .sg-error-alert-icon { flex-shrink: 0; } 
        
        /* ── Кнопка "Retry" внутри ошибки ────────────────────────────────────────── */ 
        .sg-retry-btn { 
            margin-left: auto; 
            padding: 3px 10px; 
            font-size: 12px; 
            border: 1px solid currentColor; 
            border-radius: 4px; 
            background: transparent; 
            color: inherit; 
            cursor: pointer; 
            transition: background 0.15s; 
        } 
        
        .sg-retry-btn:hover { background: rgba(239, 68, 68, 0.12); } 
        
        /* ── Пустое состояние ────────────────────────────────────────────────────── */ 
        .sg-empty-state { 
            display: flex; 
            flex-direction: column; 
            align-items: center; 
            justify-content: center; 
            gap: 10px; 
            padding: 40px 20px; 
            color: var(--sg-fg-subtle, #6b7280); 
            font-size: 13px; 
            text-align: center; 
        } 
        
        /* ── Shimmer (горизонтальный) ────────────────────────────────────────────── */ 
        @keyframes sg-shimmer { 
            0%   { background-position: 200% 0; } 
            100% { background-position: -200% 0; } 
        } 
        
        .sg-shimmer { 
            background: linear-gradient( 
                90deg, 
                var(--sg-bg-subtle, #f3f4f6) 25%, 
                var(--sg-bg, #fff) 50%, 
                var(--sg-bg-subtle, #f3f4f6) 75% 
            ); 
            background-size: 200% 100%; 
            animation: sg-shimmer 1.5s infinite; 
        } 
        
        /* ── Тонкий scrollbar (для overflow-контейнеров) ─────────────────────────── */ 
        .sg-thin-scroll,
        .sg-scroll, 
        [data-scroll], 
        body { 
            scrollbar-width: thin; 
            scrollbar-color: var(--sg-border-strong, #b0b0b0) transparent; 
        } 
        
        .sg-thin-scroll::-webkit-scrollbar,
        ::-webkit-scrollbar { width: 6px; height: 6px; } 
        
        .sg-thin-scroll::-webkit-scrollbar-track,
        ::-webkit-scrollbar-track { background: transparent; } 
        
        .sg-thin-scroll::-webkit-scrollbar-thumb,
        ::-webkit-scrollbar-thumb { 
            background: var(--sg-border-strong, #b0b0b0); 
            border-radius: 3px; 
            transition: background 0.15s;
        } 
        
        .sg-thin-scroll::-webkit-scrollbar-thumb:hover,
        ::-webkit-scrollbar-thumb:hover { 
            background: var(--sg-fg-muted, #7a7a7a); 
        } 
        
        .sg-thin-scroll::-webkit-scrollbar-corner,
        ::-webkit-scrollbar-corner { background: transparent; } 
        
        /* ── Tooltip (тёмный) ────────────────────────────────────────────────────── */ 
        .sg-tooltip-dark { 
            position: absolute; 
            pointer-events: none; 
            background: var(--sg-bg-muted, rgba(17,24,39,.92)); 
            color: #f3f4f6; 
            padding: 7px 10px; 
            border-radius: 6px; 
            font-size: 12px; 
            line-height: 1.5; 
            white-space: nowrap; 
            opacity: 0; 
            transition: opacity 0.12s ease; 
            z-index: 10; 
            box-shadow: 0 4px 12px rgba(0,0,0,.18); 
        } 
        
        /* ── Левая акцентная полоса (декор карточки) ─────────────────────────────── */ 
        .sg-accent-bar { 
            position: relative; 
        } 
        
        .sg-accent-bar::before { 
            content: ""; 
            position: absolute; 
            left: 0; 
            top: 0; 
            bottom: 0; 
            width: 3px; 
            background: transparent; 
            transition: background 0.15s; 
        } 
        
        .sg-accent-bar:hover::before { 
            background: var(--sg-color-primary, #1568c6); 
        } 
        
        /* ── Группа кнопок (segmented / button-group) ────────────────────────────── */ 
        .sg-btn-group-inline { 
            display: inline-flex; 
            border: 1px solid var(--sg-border-strong, #b0b0b0); 
            border-radius: var(--sg-radius-md, 4px); 
            overflow: hidden; 
        } 
        
        .sg-btn-group-inline > * { 
            border-radius: 0; 
            border: none; 
            border-right: 1px solid var(--sg-border-strong, #b0b0b0); 
        } 
        
        .sg-btn-group-inline > *:last-child { 
            border-right: none; 
        } 
        
        /* ── Бейдж (pill) ────────────────────────────────────────────────────────── */ 
        .sg-badge { 
            display: inline-flex; 
            align-items: center; 
            padding: 2px 8px; 
            font-size: 11px; 
            font-weight: 500; 
            border-radius: 10px; 
            border: 1px solid transparent; 
        } 
        
        .sg-badge-success { background: rgba(16,185,129,.1);  color: #059669; border-color: rgba(16,185,129,.25); } 
        .sg-badge-warn    { background: rgba(245,158,11,.1);  color: #d97706; border-color: rgba(245,158,11,.25); } 
        .sg-badge-danger  { background: rgba(239,68,68,.1);   color: #dc2626; border-color: rgba(239,68,68,.25);  } 
        .sg-badge-info    { background: rgba(0,111,238,.08);  color: var(--sg-color-primary, #006fee); border-color: rgba(0,111,238,.2); } 
        .sg-badge-neutral { background: var(--sg-bg-subtle, #f3f4f6); color: var(--sg-fg-subtle, #6b7280); border-color: var(--sg-border, #e5e7eb); } 
        
        /* ── Прогресс-бар ────────────────────────────────────────────────────────── */ 
        .sg-progress-wrap { 
            display: flex; 
            flex-direction: column; 
            gap: 4px; 
        } 
        
        .sg-progress-bar { 
            height: 4px; 
            background: var(--sg-border, #e5e7eb); 
            border-radius: 2px; 
            overflow: hidden; 
        } 
        
        .sg-progress-fill { 
            height: 100%; 
            background: var(--sg-color-primary, #006fee); 
            border-radius: 2px; 
            transition: width 0.3s ease; 
        } 
        
        .sg-progress-wrap.sgc-vertical .sgc-progress-bar { 
            width: 4px; 
            height: 100%; 
            border-radius: 2px; 
        } 
        
        .sg-progress-wrap.sgc-vertical .sgc-progress-fill { 
            width: 100%; 
            height: 100%; 
            transition: height 0.3s ease; 
        } 
        
        .sg-progress-wrap.sgc-vertical .sgc-progress-buffer { 
            width: 100%; 
            height: 100%; 
        } 
        
        .sgc-progress-buffer { 
            background: var(--sg-border-strong, rgba(229, 231, 235, 1)); 
            border-radius: 2px; 
            opacity: 0.8; 
        } 
        
        .sg-progress-indeterminate { 
            width: 40% !important; 
            animation: sg-indeterminate 1.4s ease-in-out infinite; 
        } 
        
        @keyframes sg-indeterminate { 
            0%   { transform: translateX(-100%); } 
            100% { transform: translateX(250%); } 
        } 
        
        .sg-progress-label { 
            font-size: 11px; 
            color: var(--sg-fg-subtle, #6b7280); 
        } 
        
        @media (prefers-reduced-motion: reduce) { 
            .sgc-progress-indeterminate, 
            .sgc-progress-fill, 
            .sgc-striped { 
                animation: none !important; 
                transition: none !important; 
            } 
        } 
        
        /* ── Drag-and-drop зона ──────────────────────────────────────────────────── */ 
        .sg-dropzone { 
            border: 2px dashed var(--sg-border, #e5e7eb); 
            border-radius: var(--sg-radius-md, 8px); 
            background: var(--sg-bg-subtle, #f9fafb); 
            transition: border-color 0.2s, background 0.2s; 
        } 
        
        .sg-dropzone-drag { 
            border-color: var(--sg-color-primary, #006fee); 
            background: rgba(0, 111, 238, 0.04); 
        } 
        
        .sg-dropzone-hint { 
            display: flex; 
            flex-direction: column; 
            align-items: center; 
            gap: 8px; 
            padding: 32px 24px; 
            text-align: center; 
        } 
        
        /* ── Кнопка "выбрать файл" ───────────────────────────────────────────────── */ 
        .sg-file-btn { 
            display: inline-flex; 
            align-items: center; 
            gap: 6px; 
            padding: 6px 16px; 
            font-size: 13px; 
            font-weight: 500; 
            border: 1px solid var(--sg-color-primary, #006fee); 
            border-radius: var(--sg-radius-sm, 6px); 
            color: var(--sg-color-primary, #006fee); 
            background: transparent; 
            cursor: pointer; 
            transition: background 0.15s, color 0.15s; 
            user-select: none; 
            margin-top: 4px; 
        } 
        
        .sg-file-btn:hover { 
            background: var(--sg-color-primary, #006fee); 
            color: #fff; 
        } 
        """;
}

internal class DefaultPrimitives : IThemePrimitives
{
    
    public string Neutral0 => "#ffffff";
    public string Neutral50 => "#fafafa";
    public string Neutral100 => "#f5f5f5";
    public string Neutral200 => "#f0f0f0";
    public string Neutral300 => "#d9d9d9";
    public string Neutral400 => "#bfbfbf";
    public string Neutral500 => "#8c8c8c";
    public string Neutral600 => "#595959";
    public string Neutral700 => "#434343";
    public string Neutral800 => "#262626";
    public string Neutral900 => "#000000";

    // Ant Design Blue Palette
    public string Primary50 => "#e6f7ff";
    public string Primary100 => "#bae7ff";
    public string Primary200 => "#91d5ff";
    public string Primary300 => "#69c0ff";
    public string Primary400 => "#40a9ff";
    public string Primary500 => "#1890ff";
    public string Primary600 => "#096dd9";
    public string Primary700 => "#0050b3";
    public string Primary800 => "#003a8c";
    public string Primary900 => "#002329";

    public string Success50 => "#f6ffed";
    public string Success100 => "#d9f7be";
    public string Success500 => "#52c41a";
    public string Success600 => "#389e0d";
    public string Success700 => "#237804";

    public string Danger50 => "#fff1f0";
    public string Danger100 => "#fff1f0";
    public string Danger500 => "#ff4d4f";
    public string Danger600 => "#cf1322";
    public string Danger700 => "#a8071a";

    public string Warning50 => "#fffbe6";
    public string Warning100 => "#fff7e6";
    public string Warning500 => "#faad14";
    public string Warning600 => "#d48806";

    public string Info50 => "#e6f7ff";
    public string Info100 => "#bae7ff";
    public string Info500 => "#1890ff";
    public string Info600 => "#096dd9";

    public string FontSans => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public string FontMono => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public string FontSerif => "Georgia, 'Times New Roman', serif";

    public string RadiusNone => "0";
    public string RadiusXs => "2px";
    public string RadiusSm => "3px";
    public string RadiusMd => "4px";
    public string RadiusLg => "5px";
    public string RadiusXl => "7px";
    public string Radius2Xl => "9px";
    public string RadiusFull => "9999px";
}

internal class DefaultSemanticLight : IThemeSemantic
{
    public string BgDefault => "var(--sg-p-neutral-0)";
    public string BgSubtle => "#f0f2f5"; // Specific classic background
    public string BgMuted => "var(--sg-p-neutral-100)";
    public string BgEmphasized => "var(--sg-p-neutral-200)";
    public string BgOverlay => "rgba(0, 0, 0, 0.45)";

    public string Surface => "var(--sg-p-neutral-0)";
    public string SurfaceRaised => "var(--sg-p-neutral-0)";
    public string SurfaceOverlay => "var(--sg-p-neutral-0)";

    public string FgDefault => "rgba(0, 0, 0, 0.85)";
    public string FgSubtle => "rgba(0, 0, 0, 0.65)"; // More visible
    public string FgMuted => "rgba(0, 0, 0, 0.45)"; // More visible
    public string FgDisabled => "rgba(0, 0, 0, 0.25)";
    public string FgInverse => "var(--sg-p-neutral-0)";
    public string FgLink => "var(--sg-p-blue-500)";
    public string FgLinkHover => "var(--sg-p-blue-400)";

    public string BorderDefault => "var(--sg-p-neutral-300)"; // #d9d9d9 (Classic visible border)
    public string BorderSubtle => "var(--sg-p-neutral-200)"; // #f0f0f0
    public string BorderStrong => "var(--sg-p-neutral-400)"; // #bfbfbf
    public string BorderFocus => "var(--sg-p-blue-400)";
    public string Divider => "#e8e8e8"; // Classic Ant Design divider color (slightly lighter than border)

    public string ColorPrimary => "var(--sg-p-blue-500)"; // #1890ff
    public string ColorPrimarySubtle => "var(--sg-p-blue-50)";
    public string ColorPrimaryMuted => "var(--sg-p-blue-100)";
    public string ColorPrimaryHover => "var(--sg-p-blue-400)";
    public string ColorPrimaryActive => "var(--sg-p-blue-600)";
    public string ColorPrimaryFg => "var(--sg-p-neutral-0)";

    public string ColorSuccess => "var(--sg-p-emerald-500)";
    public string ColorSuccessSubtle => "var(--sg-p-emerald-50)";
    public string ColorSuccessHover => "var(--sg-p-emerald-600)";
    public string ColorSuccessFg => "var(--sg-p-neutral-0)";

    public string ColorDanger => "var(--sg-p-rose-500)";
    public string ColorDangerSubtle => "var(--sg-p-rose-50)";
    public string ColorDangerHover => "var(--sg-p-rose-600)";
    public string ColorDangerFg => "var(--sg-p-neutral-0)";

    public string ColorWarning => "var(--sg-p-amber-500)";
    public string ColorWarningSubtle => "var(--sg-p-amber-50)";
    public string ColorWarningHover => "var(--sg-p-amber-600)";
    public string ColorWarningFg => "var(--sg-p-neutral-0)";

    public string ColorInfo => "var(--sg-p-sky-500)";
    public string ColorInfoSubtle => "var(--sg-p-sky-50)";
    public string ColorInfoHover => "var(--sg-p-sky-600)";
    public string ColorInfoFg => "var(--sg-p-neutral-0)";

    public string Font => "var(--sg-p-font-sans)";
    public string FontMono => "var(--sg-p-font-mono)";
    public string TextSm => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg => "1rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.05)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.1)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.1)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.1)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.1)";

    public string RadiusSm => "var(--sg-p-radius-sm)";
    public string RadiusMd => "var(--sg-p-radius-md)";
    public string RadiusLg => "var(--sg-p-radius-lg)";
    public string RadiusXl => "var(--sg-p-radius-xl)";
    public string RadiusFull => "var(--sg-p-radius-full)";

    public string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public string TransitionBase => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing => "0 0 0 2px var(--sg-p-blue-100), 0 0 0 4px var(--sg-p-blue-500)";
    public string FocusRingDanger => "0 0 0 2px var(--sg-p-rose-100), 0 0 0 4px var(--sg-p-rose-500)";

    public int ZDropdown => 1000;
    public int ZSticky => 100;
    public int ZModal => 2000;
    public int ZToast => 3000;
    public int ZTooltip => 4000;
}

internal class DefaultSemanticDark : IThemeSemantic
{
    public string BgDefault => "var(--sg-p-neutral-900)";
    public string BgSubtle => "var(--sg-p-neutral-800)";
    public string BgMuted => "var(--sg-p-neutral-700)";
    public string BgEmphasized => "var(--sg-p-neutral-600)";
    public string BgOverlay => "rgba(0, 0, 0, 0.75)";

    public string Surface => "var(--sg-p-neutral-800)";
    public string SurfaceRaised => "var(--sg-p-neutral-700)";
    public string SurfaceOverlay => "var(--sg-p-neutral-700)";

    public string FgDefault => "var(--sg-p-neutral-50)";
    public string FgSubtle => "var(--sg-p-neutral-400)";
    public string FgMuted => "var(--sg-p-neutral-500)";
    public string FgDisabled => "var(--sg-p-neutral-600)";
    public string FgInverse => "var(--sg-p-neutral-900)";
    public string FgLink => "var(--sg-p-blue-400)";
    public string FgLinkHover => "var(--sg-p-blue-300)";

    public string BorderDefault => "var(--sg-p-neutral-600)";
    public string BorderSubtle => "var(--sg-p-neutral-700)";
    public string BorderStrong => "var(--sg-p-neutral-500)";
    public string BorderFocus => "var(--sg-p-blue-500)";
    public string Divider => "var(--sg-p-neutral-700)";

    public string ColorPrimary => "var(--sg-p-blue-500)";
    public string ColorPrimarySubtle => "rgba(24, 144, 255, 0.15)";
    public string ColorPrimaryMuted => "rgba(24, 144, 255, 0.25)";
    public string ColorPrimaryHover => "var(--sg-p-blue-400)";
    public string ColorPrimaryActive => "var(--sg-p-blue-300)";
    public string ColorPrimaryFg => "#ffffff";

    public string ColorSuccess => "var(--sg-p-emerald-500)";
    public string ColorSuccessSubtle => "rgba(82, 196, 26, 0.15)";
    public string ColorSuccessHover => "var(--sg-p-emerald-400)";
    public string ColorSuccessFg => "#ffffff";

    public string ColorDanger => "var(--sg-p-rose-500)";
    public string ColorDangerSubtle => "rgba(255, 77, 79, 0.15)";
    public string ColorDangerHover => "var(--sg-p-rose-400)";
    public string ColorDangerFg => "#ffffff";

    public string ColorWarning => "var(--sg-p-amber-500)";
    public string ColorWarningSubtle => "rgba(250, 173, 20, 0.15)";
    public string ColorWarningHover => "var(--sg-p-amber-400)";
    public string ColorWarningFg => "var(--sg-p-neutral-900)";

    public string ColorInfo => "var(--sg-p-sky-500)";
    public string ColorInfoSubtle => "rgba(24, 144, 255, 0.15)";
    public string ColorInfoHover => "var(--sg-p-sky-400)";
    public string ColorInfoFg => "#ffffff";

    public string Font => "var(--sg-p-font-sans)";
    public string FontMono => "var(--sg-p-font-mono)";
    public string TextSm => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg => "1rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.5)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.6)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.6)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.7)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.8)";

    public string RadiusSm => "var(--sg-p-radius-sm)";
    public string RadiusMd => "var(--sg-p-radius-md)";
    public string RadiusLg => "var(--sg-p-radius-lg)";
    public string RadiusXl => "var(--sg-p-radius-xl)";
    public string RadiusFull => "var(--sg-p-radius-full)";

    public string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public string TransitionBase => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing => "0 0 0 2px rgba(24, 144, 255, 0.20), 0 0 0 4px var(--sg-p-blue-500)";
    public string FocusRingDanger => "0 0 0 2px rgba(255, 77, 79, 0.20), 0 0 0 4px var(--sg-p-rose-500)";

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

