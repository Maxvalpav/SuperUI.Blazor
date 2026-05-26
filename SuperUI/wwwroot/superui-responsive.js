// Responsive container: monitors element width via ResizeObserver,
// reports size changes back to .NET with configurable debounce.

export function observeResize(element, dotNetRef, debounceMs) {
    if (!element) return;
    if (element._sgResize) disconnect(element);

    const delay = debounceMs ?? 50;
    let timer = null;
    let pendingWidth = null;

    function report(width) {
        try {
            dotNetRef.invokeMethodAsync('OnResize', width);
        } catch { /* disposed */ }
    }

    function onEntry(entry) {
        const w = entry.contentRect.width;
        if (delay > 0) {
            pendingWidth = w;
            if (!timer) {
                timer = setTimeout(() => {
                    timer = null;
                    if (pendingWidth !== null) {
                        report(pendingWidth);
                        pendingWidth = null;
                    }
                }, delay);
            }
        } else {
            report(w);
        }
    }

    const observer = new ResizeObserver(entries => {
        for (const entry of entries) onEntry(entry);
    });

    observer.observe(element);

    element._sgResize = { observer, dotNetRef, timer };
}

export function disconnect(element) {
    if (!element || !element._sgResize) return;
    const { observer, timer } = element._sgResize;
    if (timer) clearTimeout(timer);
    if (observer) observer.disconnect();
    delete element._sgResize;
}
