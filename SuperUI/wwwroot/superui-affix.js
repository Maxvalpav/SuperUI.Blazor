// Affix: pins a placeholder->fixed positioned wrapper when scroll passes threshold.
// Also reports active state to .NET so the component can render different classes.

export function attach(host, fixedEl, dotnet, opts) {
    if (!host || !fixedEl) return;
    if (host._sgAffix) detach(host);

    const options = {
        offsetTop: opts?.offsetTop ?? null,
        offsetBottom: opts?.offsetBottom ?? null,
        target: opts?.target || null,   // CSS selector for scroll container; null = window
    };

    const scroller = options.target ? (document.querySelector(options.target) || window) : window;
    let active = false;
    let placeholderHeight = 0;
    let rafId = 0;
    let isDisposed = false;

    function getViewportRect() {
        if (scroller === window) {
            return { top: 0, bottom: window.innerHeight, left: 0, right: window.innerWidth };
        }
        return scroller.getBoundingClientRect();
    }

    function compute() {
        rafId = 0;
        if (!host.isConnected || isDisposed) return;

        // Measure the natural (unfixed) host rect — when active, the placeholder keeps the size.
        const hostRect = host.getBoundingClientRect();
        const vp = getViewportRect();
        const w = host.offsetWidth;

        let shouldFix = false;
        let top = null, bottom = null, left = null;

        if (options.offsetTop !== null) {
            // host's distance from viewport-top; pin once it scrolls above the threshold.
            if (hostRect.top <= vp.top + options.offsetTop) {
                shouldFix = true;
                top = vp.top + options.offsetTop;
                left = hostRect.left;
            }
        } else if (options.offsetBottom !== null) {
            if (hostRect.bottom >= vp.bottom - options.offsetBottom) {
                shouldFix = true;
                // distance from bottom of viewport (window coords) to desired position
                bottom = (window.innerHeight - vp.bottom) + options.offsetBottom;
                left = hostRect.left;
            }
        }

        if (shouldFix) {
            // Lock the placeholder height once, so layout doesn't jitter on first pin.
            if (placeholderHeight === 0) {
                placeholderHeight = fixedEl.offsetHeight || hostRect.height;
                host.style.height = placeholderHeight + 'px';
            }
            fixedEl.style.position = 'fixed';
            fixedEl.style.width = w + 'px';
            fixedEl.style.left = left + 'px';
            if (top !== null) {
                fixedEl.style.top = top + 'px';
                fixedEl.style.bottom = '';
            } else if (bottom !== null) {
                fixedEl.style.bottom = bottom + 'px';
                fixedEl.style.top = '';
            }
            if (!active) {
                active = true;
                try { 
                    if (dotnet && !isDisposed) {
                        dotnet.invokeMethodAsync('OnAffixed', true).catch(() => {});
                    }
                } catch { /* noop */ }
            }
        } else {
            // Reset placeholder + fixed styles.
            host.style.height = '';
            placeholderHeight = 0;
            fixedEl.style.position = '';
            fixedEl.style.width = '';
            fixedEl.style.left = '';
            fixedEl.style.top = '';
            fixedEl.style.bottom = '';
            if (active) {
                active = false;
                try { 
                    if (dotnet && !isDisposed) {
                        dotnet.invokeMethodAsync('OnAffixed', false).catch(() => {});
                    }
                } catch { /* noop */ }
            }
        }
    }

    function update() {
        if (rafId || isDisposed) return;
        rafId = requestAnimationFrame(compute);
    }

    const onScroll = update;
    const onResize = update;

    scroller.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onResize);

    // ResizeObserver re-pins when the inner element changes height (e.g. expanded panels).
    let resizeObserver = null;
    if (typeof ResizeObserver !== 'undefined') {
        resizeObserver = new ResizeObserver(update);
        resizeObserver.observe(fixedEl);
    }

    host._sgAffix = { 
        scroller, onScroll, onResize, resizeObserver, update,
        cancel: () => { if (rafId) cancelAnimationFrame(rafId); rafId = 0; },
        dispose: () => {
            isDisposed = true;
            dotnet = null;
        }
    };
    update();
}

export function detach(host) {
    if (!host || !host._sgAffix) return;
    const { scroller, onScroll, onResize, resizeObserver, cancel, dispose } = host._sgAffix;
    
    // Mark as disposed first to prevent any pending callbacks
    if (dispose) dispose();
    
    scroller.removeEventListener('scroll', onScroll);
    window.removeEventListener('resize', onResize);
    if (resizeObserver) resizeObserver.disconnect();
    if (cancel) cancel();
    delete host._sgAffix;
}

export function refresh(host) {
    if (host && host._sgAffix) host._sgAffix.update();
}

// BackTop module: report scroll position past threshold and provide scrollToTop.
const _backtopHandles = new Map();
let _backtopSeq = 0;

function findTarget(selector) {
    if (!selector) return window;
    let target = document.querySelector(selector);
    return target || window;
}

export function backtopAttach(dotnet, opts) {
    const targetSelector = opts?.target;
    let target = targetSelector ? (document.querySelector(targetSelector) || window) : window;
    const threshold = opts?.threshold ?? 200;
    let visible = false;
    let isDisposed = false;

    function getY() { return target === window ? window.scrollY : target.scrollTop; }

    function check() {
        if (isDisposed || !dotnet) return;
        
        if (targetSelector && (!target || !document.querySelector(targetSelector))) {
            target = document.querySelector(targetSelector) || window;
        }
        const next = getY() > threshold;
        if (next !== visible) {
            visible = next;
            try { 
                if (dotnet && !isDisposed) {
                    dotnet.invokeMethodAsync('OnVisibilityChanged', visible).catch(() => {});
                }
            } catch { /* noop */ }
        }
    }

    function attachToTarget() {
        if (target && target.addEventListener) {
            target.addEventListener('scroll', check, { passive: true });
        }
    }

    attachToTarget();
    const id = ++_backtopSeq;
    _backtopHandles.set(id, { 
        targetSelector, 
        target, 
        check, 
        attachToTarget,
        dispose: () => {
            isDisposed = true;
            dotnet = null;
        }
    });
    // Initial check after registration so first visibility report fires.
    check();
    return id;
}

export function backtopDetach(id) {
    const handle = _backtopHandles.get(id);
    if (!handle) return;
    
    // Mark as disposed first
    if (handle.dispose) handle.dispose();
    
    if (handle.target && handle.target.removeEventListener) {
        handle.target.removeEventListener('scroll', handle.check);
    }
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
