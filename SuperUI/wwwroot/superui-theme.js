// SuperUI 2.0-rc3 (PR #5, B1 + B2) — runtime theme state, batched.
//
// One `applyThemeState(state)` call from C# does what used to require
// 5× `localStorage.setItem` + 6× `eval(...)` interop hops. The matching
// `getSavedState()` round-trips the persisted values at init. The
// `initAutoMode(true)` call wires a single matchMedia subscription that
// re-applies the theme when the OS color scheme flips (only when the user
// is in "auto" mode).

(function () {
    'use strict';

    const THEME_ID_KEY = 'superui-theme-id';
    const MODE_KEY     = 'superui-dark-mode';
    const FONT_SIZE_KEY = 'superui-font-size';
    const FONT_FAMILY_KEY = 'superui-font-family';
    const DENSITY_KEY  = 'superui-density';

    const STYLE_ID = 'sg-dynamic-theme';

    // Internal cache of the last applied state so we can decide whether
    // anything actually changed before touching the DOM.
    let lastApplied = null;

    let autoModeActive = false;
    let systemDarkQuery = null;
    let systemDarkListener = null;

    function safeGet(key) {
        try { return localStorage.getItem(key); }
        catch { return null; }
    }
    function safeSet(key, value) {
        try { localStorage.setItem(key, value); }
        catch { /* private mode / quota — ignore */ }
    }

    // ═══════════════════════════════════════════════════════════════════
    // CSS injection (kept for now; PR #5b will switch to <link> swap).
    // ═══════════════════════════════════════════════════════════════════
    function applyThemeCss(css) {
        let styleEl = document.getElementById(STYLE_ID);
        if (!styleEl) {
            styleEl = document.createElement('style');
            styleEl.id = STYLE_ID;
            document.head.appendChild(styleEl);
        }
        styleEl.textContent = css;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Batched state apply.
    // state: {
    //   themeId, mode, fontSize, fontFamily, density, css,
    //   dataTheme, attrFontFamily, attrDensity, attrFontSize
    // }
    // ═══════════════════════════════════════════════════════════════════
    function applyThemeState(state) {
        if (!state) return;

        // 1) Persist (5 keys → 1 batched write per key, still synchronous).
        if (state.themeId    !== undefined && state.themeId    !== null) safeSet(THEME_ID_KEY,     state.themeId);
        if (state.mode       !== undefined && state.mode       !== null) safeSet(MODE_KEY,         state.mode);
        if (state.fontSize   !== undefined && state.fontSize   !== null) safeSet(FONT_SIZE_KEY,    state.fontSize);
        if (state.fontFamily !== undefined && state.fontFamily !== null) safeSet(FONT_FAMILY_KEY,  state.fontFamily);
        if (state.density    !== undefined && state.density    !== null) safeSet(DENSITY_KEY,      state.density);

        // 2) DOM updates — coalesce into a single reflow by writing to
        //    attributes on documentElement in one synchronous block.
        const root = document.documentElement;
        if (state.dataTheme) root.setAttribute('data-theme', state.dataTheme);
        if (state.themeId)   root.setAttribute('data-theme-id', state.themeId);
        if (state.attrFontFamily) root.setAttribute('data-font-family', state.attrFontFamily);
        if (state.attrDensity)    root.setAttribute('data-density', state.attrDensity);
        if (state.attrFontSize)   root.setAttribute('data-font-size', state.attrFontSize);

        // 3) Dynamic CSS.
        if (typeof state.css === 'string') {
            applyThemeCss(state.css);
        }

        lastApplied = state;
    }

    function getSavedState() {
        return {
            themeId:    safeGet(THEME_ID_KEY),
            mode:       safeGet(MODE_KEY),
            fontSize:   safeGet(FONT_SIZE_KEY),
            fontFamily: safeGet(FONT_FAMILY_KEY),
            density:    safeGet(DENSITY_KEY),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // B2 — prefers-color-scheme: dark subscription.
    // Activated only while mode === 'auto'. Toggling autoModeActive off
    // detaches the listener so we never pay for it when the user has
    // picked light/dark explicitly.
    // ═══════════════════════════════════════════════════════════════════
    function initAutoMode(enabled) {
        const want = !!enabled;
        if (want === autoModeActive) return; // idempotent

        if (want) {
            if (typeof window.matchMedia !== 'function') return;
            systemDarkQuery = window.matchMedia('(prefers-color-scheme: dark)');
            systemDarkListener = function (ev) {
                // Only react if the user is still in auto mode — they
                // might have flipped to "light"/"dark" between the
                // event firing and the handler running.
                if (safeGet(MODE_KEY) !== 'auto') return;
                document.documentElement.setAttribute(
                    'data-theme',
                    ev.matches ? 'dark' : 'light'
                );
            };
            // Modern API: addEventListener. Safari < 14 used
            // addListener which is deprecated; matchMedia in
            // any browser Blazor supports has addEventListener.
            if (systemDarkQuery.addEventListener) {
                systemDarkQuery.addEventListener('change', systemDarkListener);
            } else if (systemDarkQuery.addListener) {
                systemDarkQuery.addListener(systemDarkListener);
            }
        } else {
            if (systemDarkQuery && systemDarkListener) {
                if (systemDarkQuery.removeEventListener) {
                    systemDarkQuery.removeEventListener('change', systemDarkListener);
                } else if (systemDarkQuery.removeListener) {
                    systemDarkQuery.removeListener(systemDarkListener);
                }
            }
            systemDarkQuery = null;
            systemDarkListener = null;
        }

        autoModeActive = want;
    }

    // Expose to the Blazor IIFE namespace.
    window.SuperUI = window.SuperUI || {};
    window.SuperUI.applyThemeCss = applyThemeCss;
    window.SuperUI.applyThemeState = applyThemeState;
    window.SuperUI.getSavedState = getSavedState;
    window.SuperUI.initAutoMode = initAutoMode;

    // Named exports for ESM consumers (the file is loaded as a module by
    // Blazor's IJSRuntime `import`). C# will use the global namespace for
    // invokeVoidAsync, but tests/Node tooling can `import` the same
    // functions via these exports.
    if (typeof exports !== 'undefined') {
        exports.applyThemeCss = applyThemeCss;
        exports.applyThemeState = applyThemeState;
        exports.getSavedState = getSavedState;
        exports.initAutoMode = initAutoMode;
    }
})();
