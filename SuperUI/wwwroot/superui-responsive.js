export function observeResize(element, dotNetRef) {
    const observer = new ResizeObserver(entries => {
        for (let entry of entries) {
            const { width } = entry.contentRect;
            dotNetRef.invokeMethodAsync('OnResize', width);
        }
    });
    observer.observe(element);
    return {
        disconnect: () => observer.disconnect()
    };
}
