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

    const scroller = options.target ? document.querySelector(options.target) : window;
    let active = false;
    let lastWidth = 0;

    function getScrollY() {
        return scroller === window ? window.scrollY : scroller.scrollTop;
    }

    function getViewportRect() {
        if (scroller === window) {
            return { top: 0, bottom: window.innerHeight, left: 0 };
        }
        return scroller.getBoundingClientRect();
    }

    function update() {
        const hostRect = host.getBoundingClientRect();
        const vp = getViewportRect();
        const w = host.offsetWidth;
        if (w !== lastWidth) lastWidth = w;

        let shouldFix = false;
        let top = null, bottom = null, left = null;

        if (options.offsetTop !== null) {
            if (hostRect.top <= vp.top + options.offsetTop) {
                shouldFix = true;
                top = vp.top + options.offsetTop;
                left = hostRect.left;
            }
        } else if (options.offsetBottom !== null) {
            if (hostRect.bottom >= vp.bottom - options.offsetBottom) {
                shouldFix = true;
                bottom = (window.innerHeight - vp.bottom) + options.offsetBottom;
                left = hostRect.left;
            }
        }

        if (shouldFix) {
            host.style.height = host.style.height || (hostRect.height + 'px');
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
                try { dotnet.invokeMethodAsync('OnAffixed', true); } catch { /* noop */ }
            }
        } else {
            host.style.height = '';
            fixedEl.style.position = '';
            fixedEl.style.width = '';
            fixedEl.style.left = '';
            fixedEl.style.top = '';
            fixedEl.style.bottom = '';
            if (active) {
                active = false;
                try { dotnet.invokeMethodAsync('OnAffixed', false); } catch { /* noop */ }
            }
        }
    }

    const onScroll = () => update();
    const onResize = () => update();

    scroller.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onResize);

    host._sgAffix = { scroller, onScroll, onResize, update };
    update();
}

export function detach(host) {
    if (!host || !host._sgAffix) return;
    const { scroller, onScroll, onResize } = host._sgAffix;
    scroller.removeEventListener('scroll', onScroll);
    window.removeEventListener('resize', onResize);
    delete host._sgAffix;
}

// BackTop module: report scroll position past threshold and provide scrollToTop.
export function backtopAttach(dotnet, opts) {
    const target = opts?.target ? document.querySelector(opts.target) : window;
    const threshold = opts?.threshold ?? 200;
    let visible = false;

    function getY() { return target === window ? window.scrollY : target.scrollTop; }

    function check() {
        const next = getY() > threshold;
        if (next !== visible) {
            visible = next;
            try { dotnet.invokeMethodAsync('OnVisibilityChanged', visible); } catch { /* noop */ }
        }
    }

    target.addEventListener('scroll', check, { passive: true });
    check();
    return {
        target,
        check,
    };
}

export function backtopDetach(handle) {
    if (!handle) return;
    handle.target.removeEventListener('scroll', handle.check);
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
