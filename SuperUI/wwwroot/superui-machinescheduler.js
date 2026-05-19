
export function observeResize(element, dotnetRef) {
    if (!element || !dotnetRef) return;
    
    const canvas = element.querySelector('canvas');
    let isDisposed = false;
    
    const ro = new ResizeObserver(entries => {
        if (isDisposed) return;
        
        for (let entry of entries) {
            const { width, height } = entry.contentRect;
            
            if (canvas) {
                const dpr = window.devicePixelRatio || 1;
                canvas.width = width * dpr;
                canvas.height = height * dpr;
            }
            
            try {
                dotnetRef.invokeMethodAsync('OnResize', width, height);
            } catch (e) {
                console.warn('SgMachineScheduler: Failed to invoke OnResize, object might be disposed', e);
                isDisposed = true;
            }
        }
    });
    
    ro.observe(element);
    
    // Store a way to mark as disposed
    ro._markDisposed = () => { isDisposed = true; };
    
    return ro;
}

export function unobserveResize(ro) {
    if (ro) {
        if (ro._markDisposed) ro._markDisposed();
        ro.disconnect();
    }
}
