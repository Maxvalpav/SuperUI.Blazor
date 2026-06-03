// SuperUI 2.0-rc3 (PR #5 B1+B2, PR #5b B3) — runtime theme state + link swap.
//
// B1 (C#): debounced + batched `applyThemeState(state)` DTO replaces the
//   old 5×localStorage + 6×eval interop pattern.
// B2 (JS):  `initAutoMode(true)` wires a single matchMedia subscription
//   that re-applies data-theme when the OS color scheme flips, and only
//   while the user is in 'auto' mode.
// B3 (this): instead of C# generating a 30KB CSS string and pushing it
//   into a <style> element on every state change, we pre-generate one
//   .css per theme at design time (tools/ThemeCssExporter →
//   wwwroot/themes/css/{id}.css) and swap a <link rel="stylesheet">
//   element at runtime. Browser cache + gzip handle the rest.

(function () {
    'use strict';

    const THEME_ID_KEY = 'superui-theme-id';
    const MODE_KEY     = 'superui-dark-mode';
    const FONT_SIZE_KEY = 'superui-font-size';
    const FONT_FAMILY_KEY = 'superui-font-family';
    const DENSITY_KEY  = 'superui-density';

    const STYLE_ID  = 'sg-dynamic-theme';
    const LINK_ID   = 'sg-theme-link';

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
    // B3 — <link rel="stylesheet"> swap. One <link> per page, mutated in
    // place. C# no longer ships CSS strings; it ships paths like
    // "_content/SuperUI/themes/css/natura-ui.css".
    // ═══════════════════════════════════════════════════════════════════
    let currentThemeHref = null;

    function applyThemeLink(href) {
        if (!href || typeof href !== 'string') return;
        if (href === currentThemeHref) return; // nothing to do
        currentThemeHref = href;

        let linkEl = document.getElementById(LINK_ID);
        if (!linkEl) {
            // Create the <link> with href set BEFORE appending. Some
            // browsers (older WebKit) can miss the fetch if the link
            // is appended without a href and the attribute is mutated
            // afterwards. Setting the property first is the reliable path.
            linkEl = document.createElement('link');
            linkEl.id = LINK_ID;
            linkEl.rel = 'stylesheet';
            linkEl.href = href;
            document.head.appendChild(linkEl);
        } else {
            // Mutating `href` swaps the stylesheet in place; the browser
            // re-evaluates cached layers under the same id.
            linkEl.setAttribute('href', href);
        }

        // Best-effort: drop the now-redundant dynamic <style> element
        // that earlier 2.0-alpha runs may have left in <head>. It no
        // longer gets written to, and any selectors it contains were
        // also emitted by the pre-built .css, so removal is safe.
        const legacy = document.getElementById(STYLE_ID);
        if (legacy && legacy.parentNode) legacy.parentNode.removeChild(legacy);
    }

    function preloadThemeLink(href) {
        if (!href || typeof href !== 'string') return;
        if (document.querySelector(`link[rel="preload"][as="style"][href="${href}"]`)) return;
        const pre = document.createElement('link');
        pre.rel = 'preload';
        pre.as = 'style';
        pre.setAttribute('href', href);
        document.head.appendChild(pre);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Legacy CSS injection (kept for back-compat with external callers
    // that still push raw CSS strings; SgThemeService no longer uses
    // this path as of PR #5b).
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
    //   themeId, mode, fontSize, fontFamily, density,
    //   themeHref,                                // PR #5b: link-swap path
    //   dataTheme, attrFontFamily, attrDensity, attrFontSize
    // }
    // (legacy `css` field is still accepted but ignored by the link-swap
    //  path; SgThemeService stopped sending it in PR #5b.)
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

        // 3) Theme stylesheet — prefer link-swap (PR #5b).
        if (typeof state.themeHref === 'string' && state.themeHref.length > 0) {
            applyThemeLink(state.themeHref);
        } else if (typeof state.css === 'string') {
            // Back-compat: still fall back to inline <style> if a caller
            // ships a raw CSS string and no href.
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

    // ═══════════════════════════════════════════════════════════════════
    // Circadian rhythm — warm filter after sunset
    // ═══════════════════════════════════════════════════════════════════
    const CIRCADIAN_KEY = 'superui-circadian';

    function initCircadianMode() {
        var stored;
        try { stored = localStorage.getItem(CIRCADIAN_KEY); }
        catch { stored = null; }

        var enabled = stored === 'true';
        var html = document.documentElement;

        function applyCircadian() {
            if (!enabled) {
                html.style.removeProperty('--sg-circadian-filter');
                return;
            }
            var hour = new Date().getHours();
            var isNight = hour < 6 || hour >= 18;
            if (isNight) {
                html.style.setProperty('--sg-circadian-filter',
                    'sepia(0.25) saturate(0.85) hue-rotate(-5deg)');
                html.style.transition = 'filter 2s ease';
            } else {
                html.style.removeProperty('--sg-circadian-filter');
            }
        }

        applyCircadian();
        setInterval(applyCircadian, 600000); // re-check every 10 min
    }

    // Expose to the Blazor IIFE namespace.
    window.SuperUI = window.SuperUI || {};
    window.SuperUI.applyThemeCss = applyThemeCss;
    window.SuperUI.applyThemeState = applyThemeState;
    window.SuperUI.applyThemeLink = applyThemeLink;
    window.SuperUI.preloadThemeLink = preloadThemeLink;
    window.SuperUI.getSavedState = getSavedState;
    window.SuperUI.initAutoMode = initAutoMode;
    window.SuperUI.initCircadianMode = initCircadianMode;

    // Named exports for ESM consumers (the file is loaded as a module by
    // Blazor's IJSRuntime `import`). C# will use the global namespace for
    // invokeVoidAsync, but tests/Node tooling can `import` the same
    // functions via these exports.
    if (typeof exports !== 'undefined') {
        exports.applyThemeCss = applyThemeCss;
        exports.applyThemeState = applyThemeState;
        exports.applyThemeLink = applyThemeLink;
        exports.preloadThemeLink = preloadThemeLink;
        exports.getSavedState = getSavedState;
        exports.initAutoMode = initAutoMode;
        exports.initCircadianMode = initCircadianMode;
    }
})();
