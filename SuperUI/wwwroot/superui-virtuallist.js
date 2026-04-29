function attachObservers(container, dotnet, topSentinel, bottomSentinel, useIntersectionObserver, endThreshold) {
    detachObservers(container);

    if (!useIntersectionObserver) return;

    container._sgViewportObserver = new IntersectionObserver((entries) => {
        const entry = entries[0];
        if (!entry || !container._sgDotNet) return;
        container._sgDotNet.invokeMethodAsync('OnViewportVisibilityChanged', entry.isIntersecting);
    }, { threshold: 0.01 });

    // We split observers if endThreshold > 0 to apply rootMargin only to the bottom sentinel
    if (endThreshold > 0) {
        container._sgTopObserver = new IntersectionObserver((entries) => {
            if (!container._sgDotNet) return;
            for (const entry of entries) {
                container._sgDotNet.invokeMethodAsync('OnEdgeIntersectionChanged', 'start', entry.isIntersecting);
            }
        }, { root: container, threshold: 0.01 });

        container._sgBottomObserver = new IntersectionObserver((entries) => {
            if (!container._sgDotNet) return;
            for (const entry of entries) {
                container._sgDotNet.invokeMethodAsync('OnEdgeIntersectionChanged', 'end', entry.isIntersecting);
            }
        }, { 
            root: container, 
            threshold: 0.01,
            rootMargin: `0px 0px ${endThreshold}px 0px` 
        });

        if (topSentinel) container._sgTopObserver.observe(topSentinel);
        if (bottomSentinel) container._sgBottomObserver.observe(bottomSentinel);
    } else {
        container._sgEdgeObserver = new IntersectionObserver((entries) => {
            if (!container._sgDotNet) return;
            for (const entry of entries) {
                const edge = entry.target === topSentinel ? 'start' : 'end';
                container._sgDotNet.invokeMethodAsync('OnEdgeIntersectionChanged', edge, entry.isIntersecting);
            }
        }, {
            root: container,
            threshold: 0.01
        });

        if (topSentinel) container._sgEdgeObserver.observe(topSentinel);
        if (bottomSentinel) container._sgEdgeObserver.observe(bottomSentinel);
    }

    container._sgViewportObserver.observe(container);
}

function detachObservers(container) {
    if (container?._sgViewportObserver) {
        container._sgViewportObserver.disconnect();
        container._sgViewportObserver = null;
    }

    if (container?._sgEdgeObserver) {
        container._sgEdgeObserver.disconnect();
        container._sgEdgeObserver = null;
    }

    if (container?._sgTopObserver) {
        container._sgTopObserver.disconnect();
        container._sgTopObserver = null;
    }

    if (container?._sgBottomObserver) {
        container._sgBottomObserver.disconnect();
        container._sgBottomObserver = null;
    }
}

export function init(container, dotnet, topSentinel, bottomSentinel, useIntersectionObserver, endThreshold) {
    if (!container) return;

    const notifyScroll = () => {
        container._sgScrollFrame = null;
        if (!container._sgDotNet) return;
        container._sgDotNet.invokeMethodAsync('OnScroll', container.scrollTop);
    };

    const onScroll = () => {
        if (!container._sgDotNet) return;
        if (container._sgScrollFrame !== null && container._sgScrollFrame !== undefined) return;
        container._sgScrollFrame = requestAnimationFrame(notifyScroll);
    };

    container.addEventListener('scroll', onScroll, { passive: true });
    container._sgOnScroll = onScroll;
    container._sgDotNet = dotnet;
    container._sgScrollFrame = null;

    // Resize observer for items
    container._sgResizeObserver = new ResizeObserver((entries) => {
        const updates = [];
        for (const entry of entries) {
            const index = parseInt(entry.target.getAttribute('data-index'));
            if (!isNaN(index)) {
                updates.push({ index, height: entry.target.offsetHeight }); // Use offsetHeight to include padding/borders
            }
        }
        if (updates.length > 0 && container._sgDotNet) {
            container._sgDotNet.invokeMethodAsync('OnItemsResized', updates);
        }
    });

    // Mutation observer to automatically observe new items
    container._sgMutationObserver = new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (node.nodeType === 1 && node.classList.contains('sg-virtual-list-item')) {
                    container._sgResizeObserver.observe(node);
                }
            }
            for (const node of mutation.removedNodes) {
                if (node.nodeType === 1 && node.classList.contains('sg-virtual-list-item')) {
                    container._sgResizeObserver.unobserve(node);
                }
            }
        }
    });

    const content = container.querySelector('.sg-virtual-list-content');
    if (content) {
        container._sgMutationObserver.observe(content, { childList: true });
        // Initial observation
        content.querySelectorAll('.sg-virtual-list-item').forEach(item => {
            container._sgResizeObserver.observe(item);
        });
    }

    attachObservers(container, dotnet, topSentinel, bottomSentinel, useIntersectionObserver, endThreshold);

    // Initial call
    notifyScroll();
}

export function observeItem(container, itemElement) {
    if (container?._sgResizeObserver && itemElement) {
        container._sgResizeObserver.observe(itemElement);
    }
}

export function unobserveItem(container, itemElement) {
    if (container?._sgResizeObserver && itemElement) {
        container._sgResizeObserver.unobserve(itemElement);
    }
}

export function refreshObservers(container, topSentinel, bottomSentinel, useIntersectionObserver, endThreshold) {
    if (!container || !container._sgDotNet) return;
    attachObservers(container, container._sgDotNet, topSentinel, bottomSentinel, useIntersectionObserver, endThreshold);
}

export function setScrollTop(container, scrollTop) {
    if (container) {
        container.scrollTop = scrollTop;
    }
}

export function dispose(container) {
    if (container && container._sgOnScroll) {
        container.removeEventListener('scroll', container._sgOnScroll);
        if (container._sgScrollFrame !== null && container._sgScrollFrame !== undefined) {
            cancelAnimationFrame(container._sgScrollFrame);
        }
        detachObservers(container);
        if (container._sgResizeObserver) {
            container._sgResizeObserver.disconnect();
            container._sgResizeObserver = null;
        }
        if (container._sgMutationObserver) {
            container._sgMutationObserver.disconnect();
            container._sgMutationObserver = null;
        }
        container._sgOnScroll = null;
        container._sgDotNet = null;
        container._sgScrollFrame = null;
    }
}
