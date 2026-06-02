// superui-platform.js
// Платформенные JS-обёртки для SuperUI-сервисов:
// SgStorageService, SgClipboardService, SgDownloadService, SgPrintService,
// SgFullscreenService, SgViewportService, SgBreakpointService,
// SgNetworkService, SgVisibilityService, SgHotkeyService,
// SgIntersectionService, SgResizeService, SgErrorService.
//
// Загружается один раз через SgJsModuleCache, путь "./_content/SuperUI/superui-platform.js".
// Все функции idempotent и безопасно работают в SSR (отсутствие браузера).

(function (global) {
    'use strict';

    const SuperUI = global.SuperUI = global.SuperUI || {};

    // ── Storage ─────────────────────────────────────────────────────────────

    SuperUI.localStorageKeys = function () {
        try { return Object.keys(global.localStorage || {}); }
        catch (e) { return []; }
    };

    SuperUI.sessionStorageKeys = function () {
        try { return Object.keys(global.sessionStorage || {}); }
        catch (e) { return []; }
    };

    // ── Clipboard ───────────────────────────────────────────────────────────

    SuperUI.copyText = async function (text) {
        if (!text && text !== '') return;
        try {
            if (global.navigator?.clipboard?.writeText) {
                await global.navigator.clipboard.writeText(text);
                return;
            }
            // Fallback: textarea + execCommand
            const ta = document.createElement('textarea');
            ta.value = text;
            ta.setAttribute('readonly', '');
            ta.style.position = 'absolute';
            ta.style.left = '-9999px';
            document.body.appendChild(ta);
            ta.select();
            try { document.execCommand('copy'); } catch (_) { }
            document.body.removeChild(ta);
        } catch (e) { /* ignore */ }
    };

    SuperUI.readClipboardText = async function () {
        try {
            if (global.navigator?.clipboard?.readText) {
                return await global.navigator.clipboard.readText();
            }
            return null;
        } catch (e) { return null; }
    };

    // ── Download ────────────────────────────────────────────────────────────

    SuperUI.downloadText = function (fileName, content, mimeType) {
        try {
            const blob = new Blob([content ?? ''], { type: mimeType || 'text/plain' });
            triggerDownload(blob, fileName);
        } catch (e) { /* ignore */ }
    };

    SuperUI.downloadBytes = function (fileName, base64, mimeType) {
        try {
            const bin = atob(base64);
            const arr = new Uint8Array(bin.length);
            for (let i = 0; i < bin.length; i++) arr[i] = bin.charCodeAt(i);
            const blob = new Blob([arr], { type: mimeType || 'application/octet-stream' });
            triggerDownload(blob, fileName);
        } catch (e) { /* ignore */ }
    };

    SuperUI.downloadUrl = function (fileName, url) {
        try {
            const a = document.createElement('a');
            a.href = url;
            a.download = fileName;
            a.target = '_self';
            a.rel = 'noopener';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        } catch (e) { /* ignore */ }
    };

    function triggerDownload(blob, fileName) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(() => URL.revokeObjectURL(url), 200);
    }

    // ── Print ───────────────────────────────────────────────────────────────

    SuperUI.print = function () {
        try { global.print?.(); } catch (e) { /* ignore */ }
    };

    SuperUI.printWithToggles = function (hideSelector, showSelector) {
        try {
            const hideEls = hideSelector ? Array.from(document.querySelectorAll(hideSelector)) : [];
            const showEls = showSelector ? Array.from(document.querySelectorAll(showSelector)) : [];
            const prevHide = hideEls.map(el => el.style.visibility);
            const prevShow = showEls.map(el => el.style.visibility);
            hideEls.forEach(el => el.style.visibility = 'hidden');
            showEls.forEach(el => el.style.visibility = 'visible');
            const restore = () => {
                hideEls.forEach((el, i) => el.style.visibility = prevHide[i]);
                showEls.forEach((el, i) => el.style.visibility = prevShow[i]);
            };
            global.print?.();
            // After print dialog closes (sync on most browsers).
            setTimeout(restore, 0);
        } catch (e) { /* ignore */ }
    };

    // ── Fullscreen ──────────────────────────────────────────────────────────

    SuperUI.isFullscreen = function () {
        try { return !!document.fullscreenElement; } catch (e) { return false; }
    };

    SuperUI.requestFullscreen = function () {
        try {
            const target = document.documentElement;
            const req = target.requestFullscreen || target.webkitRequestFullscreen || target.mozRequestFullScreen || target.msRequestFullscreen;
            if (req) { req.call(target); return true; }
            return false;
        } catch (e) { return false; }
    };

    SuperUI.exitFullscreen = function () {
        try {
            const exit = document.exitFullscreen || document.webkitExitFullscreen || document.mozCancelFullScreen || document.msExitFullscreen;
            if (exit) { exit.call(document); return true; }
            return false;
        } catch (e) { return false; }
    };

    let _fullscreenRef = null;
    SuperUI.subscribeFullscreen = function (dotNetRef) {
        try {
            if (_fullscreenRef) return; // already subscribed
            const handler = () => {
                try { dotNetRef.invokeMethodAsync('OnFullscreenChanged', !!document.fullscreenElement); }
                catch (e) { /* ignore */ }
            };
            document.addEventListener('fullscreenchange', handler);
            document.addEventListener('webkitfullscreenchange', handler);
            document.addEventListener('mozfullscreenchange', handler);
            document.addEventListener('MSFullscreenChange', handler);
            _fullscreenRef = { dotNetRef, handler };
        } catch (e) { /* ignore */ }
    };

    SuperUI.unsubscribeFullscreen = function () {
        try {
            if (!_fullscreenRef) return;
            const { handler } = _fullscreenRef;
            document.removeEventListener('fullscreenchange', handler);
            document.removeEventListener('webkitfullscreenchange', handler);
            document.removeEventListener('mozfullscreenchange', handler);
            document.removeEventListener('MSFullscreenChange', handler);
            _fullscreenRef = null;
        } catch (e) { /* ignore */ }
    };

    // ── Viewport ────────────────────────────────────────────────────────────

    SuperUI.readViewport = function () {
        return {
            Width: global.innerWidth || 0,
            Height: global.innerHeight || 0,
            DevicePixelRatio: global.devicePixelRatio || 1.0,
            ScrollX: global.scrollX || 0,
            ScrollY: global.scrollY || 0,
        };
    };

    let _viewportRef = null;
    let _viewportResizeRaf = 0;
    SuperUI.startViewport = function (dotNetRef) {
        try {
            if (_viewportRef) return;
            const onScroll = () => { scheduleViewportUpdate(dotNetRef, true); };
            const onResize = () => { scheduleViewportUpdate(dotNetRef, false); };
            global.addEventListener('scroll', onScroll, { passive: true });
            global.addEventListener('resize', onResize, { passive: true });
            _viewportRef = { dotNetRef, onScroll, onResize };
            // initial fire
            scheduleViewportUpdate(dotNetRef, true);
        } catch (e) { /* ignore */ }
    };

    function scheduleViewportUpdate(dotNetRef, isScroll) {
        if (_viewportResizeRaf) cancelAnimationFrame(_viewportResizeRaf);
        _viewportResizeRaf = requestAnimationFrame(() => {
            _viewportResizeRaf = 0;
            try { dotNetRef.invokeMethodAsync('OnChanged', SuperUI.readViewport()); }
            catch (e) { /* ignore */ }
        });
    }

    SuperUI.stopViewport = function () {
        try {
            if (!_viewportRef) return;
            const { onScroll, onResize } = _viewportRef;
            global.removeEventListener('scroll', onScroll);
            global.removeEventListener('resize', onResize);
            if (_viewportResizeRaf) cancelAnimationFrame(_viewportResizeRaf);
            _viewportRef = null;
        } catch (e) { /* ignore */ }
    };

    // ── Network ─────────────────────────────────────────────────────────────

    SuperUI.readOnline = function () { return global.navigator?.onLine ?? true; };

    let _networkRef = null;
    SuperUI.startNetwork = function (dotNetRef) {
        try {
            if (_networkRef) return;
            const onOnline = () => dotNetRef.invokeMethodAsync('OnChanged', true);
            const onOffline = () => dotNetRef.invokeMethodAsync('OnChanged', false);
            global.addEventListener('online', onOnline);
            global.addEventListener('offline', onOffline);
            _networkRef = { dotNetRef, onOnline, onOffline };
        } catch (e) { /* ignore */ }
    };

    SuperUI.stopNetwork = function () {
        try {
            if (!_networkRef) return;
            const { onOnline, onOffline } = _networkRef;
            global.removeEventListener('online', onOnline);
            global.removeEventListener('offline', onOffline);
            _networkRef = null;
        } catch (e) { /* ignore */ }
    };

    // ── Visibility ──────────────────────────────────────────────────────────

    SuperUI.readVisibility = function () { return document.visibilityState || 'visible'; };

    let _visibilityRef = null;
    SuperUI.startVisibility = function (dotNetRef) {
        try {
            if (_visibilityRef) return;
            const onChange = () => dotNetRef.invokeMethodAsync('OnChanged', document.visibilityState || 'visible');
            document.addEventListener('visibilitychange', onChange);
            _visibilityRef = { dotNetRef, onChange };
        } catch (e) { /* ignore */ }
    };

    SuperUI.stopVisibility = function () {
        try {
            if (!_visibilityRef) return;
            const { onChange } = _visibilityRef;
            document.removeEventListener('visibilitychange', onChange);
            _visibilityRef = null;
        } catch (e) { /* ignore */ }
    };

    // ── Hotkeys ─────────────────────────────────────────────────────────────

    const _hotkeyMap = new Map();
    let _hotkeyHandler = null;
    let _hotkeyListenerEl = null;

    function comboMatches(event, combo) {
        const parts = combo.toLowerCase().split('+').map(s => s.trim());
        const key = parts.pop();
        const needCtrl = parts.includes('ctrl') || parts.includes('cmd') || parts.includes('mod');
        const needShift = parts.includes('shift');
        const needAlt = parts.includes('alt');
        const needMeta = parts.includes('meta');

        if (needCtrl) {
            if (parts.includes('mod')) {
                if (!(event.ctrlKey || event.metaKey)) return false;
            } else if (parts.includes('cmd')) {
                if (!event.metaKey) return false;
            } else if (!event.ctrlKey) {
                return false;
            }
        }
        if (needShift && !event.shiftKey) return false;
        if (needAlt && !event.altKey) return false;
        if (needMeta && !event.metaKey) return false;

        const k = (event.key || '').toLowerCase();
        if (key === 'space') return k === ' ' || k === 'spacebar';
        if (key === 'esc' || key === 'escape') return k === 'escape';
        if (key === 'enter' || key === 'return') return k === 'enter';
        if (key === 'arrowup' || key === 'up') return k === 'arrowup';
        if (key === 'arrowdown' || key === 'down') return k === 'arrowdown';
        if (key === 'arrowleft' || key === 'left') return k === 'arrowleft';
        if (key === 'arrowright' || key === 'right') return k === 'arrowright';
        if (key === 'tab') return k === 'tab';
        return k === key;
    }

    function ensureHotkeyHandler() {
        if (_hotkeyHandler) return;
        _hotkeyHandler = (event) => {
            for (const [combo, entry] of _hotkeyMap) {
                if (comboMatches(event, combo)) {
                    if (entry.scope && !event.target.closest(entry.scope)) continue;
                    if (entry.preventDefault) event.preventDefault();
                    if (entry.stopPropagation) event.stopPropagation();
                    try { entry.dotNetRef.invokeMethodAsync('OnHotkeyAsync', combo); }
                    catch (e) { /* ignore */ }
                    return;
                }
            }
        };
        _hotkeyListenerEl = global.document || global;
        _hotkeyListenerEl.addEventListener('keydown', _hotkeyHandler, true);
    }

    SuperUI.registerHotkey = function (combo, scope, preventDefault, stopPropagation, dotNetRef) {
        try {
            ensureHotkeyHandler();
            _hotkeyMap.set(combo, { scope, preventDefault, stopPropagation, dotNetRef });
        } catch (e) { /* ignore */ }
    };

    SuperUI.unregisterHotkey = function (combo) {
        try { _hotkeyMap.delete(combo); } catch (e) { /* ignore */ }
    };

    SuperUI.clearHotkeys = function () {
        try {
            _hotkeyMap.clear();
            if (_hotkeyListenerEl && _hotkeyHandler) {
                _hotkeyListenerEl.removeEventListener('keydown', _hotkeyHandler, true);
            }
            _hotkeyHandler = null;
            _hotkeyListenerEl = null;
        } catch (e) { /* ignore */ }
    };

    // ── Intersection ────────────────────────────────────────────────────────

    const _intersectionObservers = new Map();
    SuperUI.observeIntersection = function (elementId, rootSelector, rootMargin, threshold, once, dotNetRef) {
        try {
            const el = document.getElementById(elementId);
            if (!el) return;
            if (_intersectionObservers.has(elementId)) SuperUI.unobserveIntersection(elementId);

            let root = null;
            if (rootSelector) root = document.querySelector(rootSelector);

            const obs = new IntersectionObserver((entries) => {
                for (const e of entries) {
                    try { dotNetRef.invokeMethodAsync('OnIntersect', elementId, e.isIntersecting, e.intersectionRatio); }
                    catch (err) { /* ignore */ }
                    if (once && e.isIntersecting) {
                        SuperUI.unobserveIntersection(elementId);
                    }
                }
            }, { root, rootMargin: rootMargin || '0px', threshold: threshold || 0 });
            obs.observe(el);
            _intersectionObservers.set(elementId, obs);
        } catch (e) { /* ignore */ }
    };

    SuperUI.unobserveIntersection = function (elementId) {
        try {
            const obs = _intersectionObservers.get(elementId);
            if (obs) { obs.disconnect(); _intersectionObservers.delete(elementId); }
        } catch (e) { /* ignore */ }
    };

    SuperUI.clearIntersections = function () {
        try {
            for (const obs of _intersectionObservers.values()) obs.disconnect();
            _intersectionObservers.clear();
        } catch (e) { /* ignore */ }
    };

    // ── Resize ──────────────────────────────────────────────────────────────

    const _resizeObservers = new Map();
    SuperUI.observeResize = function (elementId, dotNetRef) {
        try {
            const el = document.getElementById(elementId);
            if (!el) return;
            if (_resizeObservers.has(elementId)) SuperUI.unobserveResize(elementId);
            const obs = new ResizeObserver((entries) => {
                for (const e of entries) {
                    const cr = e.contentRect;
                    try { dotNetRef.invokeMethodAsync('OnResize', elementId, Math.round(cr.width), Math.round(cr.height), global.devicePixelRatio || 1.0); }
                    catch (err) { /* ignore */ }
                }
            });
            obs.observe(el);
            _resizeObservers.set(elementId, obs);
        } catch (e) { /* ignore */ }
    };

    SuperUI.unobserveResize = function (elementId) {
        try {
            const obs = _resizeObservers.get(elementId);
            if (obs) { obs.disconnect(); _resizeObservers.delete(elementId); }
        } catch (e) { /* ignore */ }
    };

    SuperUI.clearResizes = function () {
        try {
            for (const obs of _resizeObservers.values()) obs.disconnect();
            _resizeObservers.clear();
        } catch (e) { /* ignore */ }
    };

    // ── Errors ──────────────────────────────────────────────────────────────

    let _errorRef = null;
    SuperUI.startErrorCapture = function (dotNetRef) {
        try {
            if (_errorRef) return;
            const onError = (message, source, lineno, colno, error) => {
                try { dotNetRef.invokeMethodAsync('OnError', String(message), source || null, lineno || 0, colno || 0, error?.stack || null); }
                catch (e) { /* ignore */ }
            };
            const onRejection = (event) => {
                const reason = event.reason?.message || event.reason || 'unknown';
                const stack = event.reason?.stack || null;
                try { dotNetRef.invokeMethodAsync('OnUnhandledRejection', String(reason), stack); }
                catch (e) { /* ignore */ }
            };
            global.addEventListener('error', onError);
            global.addEventListener('unhandledrejection', onRejection);
            _errorRef = { dotNetRef, onError, onRejection };
        } catch (e) { /* ignore */ }
    };

    SuperUI.stopErrorCapture = function () {
        try {
            if (!_errorRef) return;
            const { onError, onRejection } = _errorRef;
            global.removeEventListener('error', onError);
            global.removeEventListener('unhandledrejection', onRejection);
            _errorRef = null;
        } catch (e) { /* ignore */ }
    };

    // ── CSS variables (custom theme overrides) ──────────────────────────────

    SuperUI.setCssVariable = function (name, value) {
        try {
            if (typeof document === 'undefined') return;
            document.documentElement.style.setProperty(name, value);
        } catch (e) { /* ignore */ }
    };

    SuperUI.setCssVariables = function (dict) {
        try {
            if (typeof document === 'undefined' || !dict) return;
            for (const k in dict) {
                if (Object.prototype.hasOwnProperty.call(dict, k)) {
                    document.documentElement.style.setProperty(k, dict[k]);
                }
            }
        } catch (e) { /* ignore */ }
    };

    SuperUI.removeCssVariable = function (name) {
        try {
            if (typeof document === 'undefined') return;
            document.documentElement.style.removeProperty(name);
        } catch (e) { /* ignore */ }
    };
})(typeof window !== 'undefined' ? window : globalThis);
