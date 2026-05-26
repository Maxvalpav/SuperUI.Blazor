// Affix: pins a placeholder->fixed positioned wrapper when scroll passes threshold.
// Also reports active state to .NET so the component can render different classes.

export function attach(host, fixedEl, dotnet, opts) {
    if (!host || !fixedEl) return;
    if (host._sgAffix) detach(host);

    const options = {
        offsetTop:    opts?.offsetTop    ?? null,
        offsetBottom: opts?.offsetBottom ?? null,
        target:       opts?.target       || null,  // CSS selector for scroll container; null = window
        zIndex:       opts?.ZIndex       ?? 100,
    };

    // Resolve scroller lazily — element may not be in DOM yet on first render
    let scroller = options.target
        ? (document.querySelector(options.target) || null)
        : window;

    let active = false;
    let placeholderHeight = 0;
    let rafId = 0;
    let isDisposed = false;
    let scrollListenerAttached = false;

    function getScroller() {
        if (!scroller && options.target) {
            scroller = document.querySelector(options.target) || window;
        }
        return scroller || window;
    }

    function ensureScrollListener() {
        if (scrollListenerAttached) return;
        const s = getScroller();
        s.addEventListener('scroll', onScroll, { passive: true });
        scrollListenerAttached = true;
    }

    function getViewportRect() {
        const s = getScroller();
        if (s === window) {
            return { top: 0, bottom: window.innerHeight, left: 0, right: window.innerWidth };
        }
        return s.getBoundingClientRect();
    }

    function compute() {
        rafId = 0;
        if (!host.isConnected || isDisposed) return;

        const hostRect = host.getBoundingClientRect();
        const vp = getViewportRect();
        const w = host.offsetWidth;

        let shouldFix = false;
        let top = null, bottom = null, left = null;

        if (options.offsetTop !== null) {
            if (hostRect.top <= vp.top + options.offsetTop) {
                shouldFix = true;
                top  = vp.top + options.offsetTop;
                left = hostRect.left;
            }
        } else if (options.offsetBottom !== null) {
            if (hostRect.bottom >= vp.bottom - options.offsetBottom) {
                shouldFix = true;
                bottom = (window.innerHeight - vp.bottom) + options.offsetBottom;
                left   = hostRect.left;
            }
        }

        if (shouldFix) {
            if (placeholderHeight === 0) {
                placeholderHeight = fixedEl.offsetHeight || hostRect.height;
                host.style.height = placeholderHeight + 'px';
            }
            fixedEl.style.position = 'fixed';
            fixedEl.style.width    = w + 'px';
            fixedEl.style.left     = left + 'px';
            fixedEl.style.zIndex   = options.zIndex + '';
            if (top !== null) {
                fixedEl.style.top    = top + 'px';
                fixedEl.style.bottom = '';
            } else if (bottom !== null) {
                fixedEl.style.bottom = bottom + 'px';
                fixedEl.style.top    = '';
            }
            if (!active) {
                active = true;
                try {
                    if (dotnet && !isDisposed)
                        dotnet.invokeMethodAsync('OnAffixed', true).catch(() => {});
                } catch { /* noop */ }
            }
        } else {
            host.style.height      = '';
            placeholderHeight      = 0;
            fixedEl.style.position = '';
            fixedEl.style.width    = '';
            fixedEl.style.left     = '';
            fixedEl.style.top      = '';
            fixedEl.style.bottom   = '';
            fixedEl.style.zIndex   = '';
            if (active) {
                active = false;
                try {
                    if (dotnet && !isDisposed)
                        dotnet.invokeMethodAsync('OnAffixed', false).catch(() => {});
                } catch { /* noop */ }
            }
        }
    }

    function update() {
        if (rafId || isDisposed) return;
        ensureScrollListener();
        rafId = requestAnimationFrame(compute);
    }

    const onScroll = update;
    const onResize = update;

    // Attach scroll listener now if scroller already resolved, otherwise
    // ensureScrollListener() will do it on first update() call.
    if (scroller) {
        scroller.addEventListener('scroll', onScroll, { passive: true });
        scrollListenerAttached = true;
    }
    window.addEventListener('resize', onResize);

    // ResizeObserver re-pins when the inner element changes height.
    let resizeObserver = null;
    if (typeof ResizeObserver !== 'undefined') {
        resizeObserver = new ResizeObserver(update);
        resizeObserver.observe(fixedEl);
    }

    host._sgAffix = {
        get scroller() { return getScroller(); },
        onScroll, onResize, resizeObserver, update,
        cancel:  () => { if (rafId) { cancelAnimationFrame(rafId); rafId = 0; } },
        dispose: () => { isDisposed = true; dotnet = null; }
    };

    // Retry after DOM settles if target not found yet
    if (options.target && !scroller) {
        setTimeout(() => {
            if (isDisposed) return;
            update();
        }, 100);
    } else {
        update();
    }
}

export function detach(host) {
    if (!host || !host._sgAffix) return;
    const { scroller, onScroll, onResize, resizeObserver, cancel, dispose } = host._sgAffix;
    if (dispose) dispose();
    if (scroller && scroller.removeEventListener) scroller.removeEventListener('scroll', onScroll);
    window.removeEventListener('resize', onResize);
    if (resizeObserver) resizeObserver.disconnect();
    if (cancel) cancel();
    delete host._sgAffix;
}

export function refresh(host) {
    if (host && host._sgAffix) host._sgAffix.update();
}

// ── BackTop ───────────────────────────────────────────────────────────────────
const _backtopHandles = new Map();
let _backtopSeq = 0;

export function backtopAttach(dotnet, opts) {
    const targetSelector = opts?.target;
    let target    = targetSelector ? document.querySelector(targetSelector) : window;
    const threshold = opts?.threshold ?? 200;
    const direction = opts?.direction ?? 'top';
    const trackProgress = opts?.trackProgress ?? false;
    let visible   = false;
    let isDisposed = false;
    let listenerAttached = false;
    let lastProgress = -1;

    function getY() {
        if (!target) return 0;
        return target === window ? window.scrollY : target.scrollTop;
    }

    function getMaxY() {
        if (!target) return 0;
        if (target === window) {
            return document.documentElement.scrollHeight - window.innerHeight;
        }
        return target.scrollHeight - target.clientHeight;
    }

    function getScrollPercentage() {
        const maxY = getMaxY();
        if (maxY <= 0) return 0;
        const y = getY();
        if (direction === 'top') {
            return Math.round((y / maxY) * 100);
        }
        return Math.round(((maxY - y) / maxY) * 100);
    }

    function check() {
        if (isDisposed || !dotnet) return;
        let next = false;
        if (direction === 'top') {
            next = getY() > threshold;
        } else {
            next = getY() < getMaxY() - threshold;
        }
        
        if (next !== visible) {
            visible = next;
            try {
                if (dotnet && !isDisposed)
                    dotnet.invokeMethodAsync('OnVisibilityChanged', visible).catch(() => {});
            } catch { /* noop */ }
        }

        if (trackProgress) {
            const pct = getScrollPercentage();
            if (pct !== lastProgress) {
                lastProgress = pct;
                try {
                    if (dotnet && !isDisposed)
                        dotnet.invokeMethodAsync('OnScrollProgress', pct).catch(() => {});
                } catch { /* noop */ }
            }
        }
    }

    function attachListener() {
        if (listenerAttached || !target) return;
        target.addEventListener('scroll', check, { passive: true });
        listenerAttached = true;
    }

    if (target) {
        attachListener();
    } else if (targetSelector) {
        // Target not in DOM yet — retry after render
        setTimeout(() => {
            if (isDisposed) return;
            target = document.querySelector(targetSelector) || window;
            attachListener();
            check();
        }, 100);
    }

    const id = ++_backtopSeq;
    _backtopHandles.set(id, {
        get target() { return target; },
        check,
        dispose: () => { isDisposed = true; dotnet = null; }
    });

    check();
    return id;
}

export function backtopDetach(id) {
    const handle = _backtopHandles.get(id);
    if (!handle) return;
    if (handle.dispose) handle.dispose();
    const t = handle.target;
    if (t && t.removeEventListener) t.removeEventListener('scroll', handle.check);
    _backtopHandles.delete(id);
}

export function scrollToTop(targetSelector, smooth) {
    const target = targetSelector ? document.querySelector(targetSelector) : window;
    if (!target) return;
    if (target === window) {
        window.scrollTo({ top: 0, behavior: smooth ? 'smooth' : 'auto' });
    } else {
        target.scrollTo({ top: 0, behavior: smooth ? 'smooth' : 'auto' });
    }
}

export function scrollToBottom(targetSelector, smooth) {
    const target = targetSelector ? document.querySelector(targetSelector) : window;
    if (!target) return;
    if (target === window) {
        window.scrollTo({ top: document.documentElement.scrollHeight, behavior: smooth ? 'smooth' : 'auto' });
    } else {
        target.scrollTo({ top: target.scrollHeight, behavior: smooth ? 'smooth' : 'auto' });
    }
}
