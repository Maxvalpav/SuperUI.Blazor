// Draggable & resizable floating window support.

const SNAP_THRESHOLD = 15;
const ARROW_KEY_STEP = 10;

export function attach(el, dotnetRef) {
    if (!el || el._sgWinAttached) return;
    el._sgWinAttached = true;

    el.addEventListener('pointerdown', () => {
        dotnetRef.invokeMethodAsync('FocusAsync');
    }, true);

    // Keyboard handlers
    el.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            dotnetRef.invokeMethodAsync('CloseAsync');
        }
        
        // Ctrl+F4 to close
        if (e.ctrlKey && e.key === 'F4') {
            e.preventDefault();
            dotnetRef.invokeMethodAsync('CloseAsync');
        }
        
        // Arrow keys to move window (only if not in a text input)
        const isTextInput = ['INPUT', 'TEXTAREA', 'SELECT'].includes(document.activeElement?.tagName) || 
                            (document.activeElement?.hasAttribute('contenteditable') && document.activeElement.getAttribute('contenteditable') !== 'false');
        if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(e.key) && !el.classList.contains('sgc-win-maximized') && !isTextInput) {
            e.preventDefault();
            const windowWidth = window.innerWidth;
            const windowHeight = window.innerHeight;
            const rect = el.getBoundingClientRect();
            let left = parseFloat(el.style.left) || rect.left;
            let top = parseFloat(el.style.top) || rect.top;
            
            switch (e.key) {
                case 'ArrowUp':
                    top -= ARROW_KEY_STEP;
                    break;
                case 'ArrowDown':
                    top += ARROW_KEY_STEP;
                    break;
                case 'ArrowLeft':
                    left -= ARROW_KEY_STEP;
                    break;
                case 'ArrowRight':
                    left += ARROW_KEY_STEP;
                    break;
            }
            
            // Keep window inside viewport
            left = Math.max(0, Math.min(windowWidth - rect.width, left));
            top = Math.max(0, Math.min(windowHeight - rect.height, top));
            
            el.style.left = left + 'px';
            el.style.top = top + 'px';
            el.style.right = 'auto';
            el.style.bottom = 'auto';
            
            dotnetRef.invokeMethodAsync('UpdateBoundsAsync', left, top, rect.width, rect.height);
        }
        
        if (e.key === 'Tab') {
            const items = Array.from(el.querySelectorAll('button, a, input, select, textarea, [tabindex]:not([tabindex="-1"])'));
            if (items.length === 0) return;
            const first = items[0];
            const last = items[items.length - 1];
            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        }
    });

    const header = el.querySelector('.sgc-win-header');
    if (header) {
        // Double-click to toggle maximize
        header.addEventListener('dblclick', (e) => {
            if (e.target.closest('.sgc-win-btn')) return;
            dotnetRef.invokeMethodAsync('ToggleMaximizeAsync');
        });
        
        header.addEventListener('pointerdown', (e) => {
            if (e.target.closest('.sgc-win-btn')) return;
            if (el.classList.contains('sgc-win-maximized')) return;
            e.preventDefault();
            const rect = el.getBoundingClientRect();
            const offX = e.clientX - rect.left;
            const offY = e.clientY - rect.top;
            el.classList.add('sgc-win-dragging');
            
            const onMove = (ev) => {
                const windowWidth = window.innerWidth;
                const windowHeight = window.innerHeight;
                const winWidth = rect.width;
                const winHeight = rect.height;
                
                let nx = ev.clientX - offX;
                let ny = ev.clientY - offY;
                
                // Keep window completely inside viewport
                nx = Math.max(0, Math.min(windowWidth - winWidth, nx));
                ny = Math.max(0, Math.min(windowHeight - winHeight, ny));
                
                // Snap to viewport edges
                if (nx < SNAP_THRESHOLD) nx = 0;
                if (ny < SNAP_THRESHOLD) ny = 0;
                if (windowWidth - winWidth - nx < SNAP_THRESHOLD) nx = windowWidth - winWidth;
                if (windowHeight - winHeight - ny < SNAP_THRESHOLD) ny = windowHeight - winHeight;
                
                // Snap to other windows
                const otherWindows = Array.from(document.querySelectorAll('.sgc-win')).filter(w => w !== el && !w.classList.contains('sgc-win-maximized'));
                for (const other of otherWindows) {
                    const otherRect = other.getBoundingClientRect();
                    
                    if (Math.abs(nx - otherRect.right) < SNAP_THRESHOLD) {
                        nx = otherRect.right;
                    }
                    if (Math.abs(nx + winWidth - otherRect.left) < SNAP_THRESHOLD) {
                        nx = otherRect.left - winWidth;
                    }
                    if (Math.abs(ny - otherRect.bottom) < SNAP_THRESHOLD) {
                        ny = otherRect.bottom;
                    }
                    if (Math.abs(ny + winHeight - otherRect.top) < SNAP_THRESHOLD) {
                        ny = otherRect.top - winHeight;
                    }
                    if (Math.abs(ny - otherRect.top) < SNAP_THRESHOLD) {
                        ny = otherRect.top;
                    }
                    if (Math.abs(nx - otherRect.left) < SNAP_THRESHOLD) {
                        nx = otherRect.left;
                    }
                }
                
                el.style.left = nx + 'px';
                el.style.top = ny + 'px';
                el.style.right = 'auto';
                el.style.bottom = 'auto';
            };
            
            const onUp = () => {
                window.removeEventListener('pointermove', onMove);
                window.removeEventListener('pointerup', onUp);
                el.classList.remove('sgc-win-dragging');
                const finalRect = el.getBoundingClientRect();
                dotnetRef.invokeMethodAsync('UpdateBoundsAsync', 
                    parseFloat(el.style.left), 
                    parseFloat(el.style.top), 
                    finalRect.width, 
                    finalRect.height);
            };
            
            window.addEventListener('pointermove', onMove);
            window.addEventListener('pointerup', onUp, { once: true });
        });
    }

    const handle = el.querySelector('.sgc-win-resize');
    if (handle) {
        handle.addEventListener('pointerdown', (e) => {
            if (el.classList.contains('sgc-win-maximized')) return;
            e.preventDefault();
            e.stopPropagation();
            const rect = el.getBoundingClientRect();
            const startW = rect.width;
            const startH = rect.height;
            const startX = e.clientX;
            const startY = e.clientY;
            el.classList.add('sgc-win-resizing');
            
            const onMove = (ev) => {
                const windowWidth = window.innerWidth;
                const windowHeight = window.innerHeight;
                const left = parseFloat(el.style.left) || rect.left;
                const top = parseFloat(el.style.top) || rect.top;
                
                const nw = Math.max(180, Math.min(windowWidth - left, startW + (ev.clientX - startX)));
                const nh = Math.max(100, Math.min(windowHeight - top, startH + (ev.clientY - startY)));
                
                el.style.width = nw + 'px';
                el.style.height = nh + 'px';
            };
            
            const onUp = () => {
                window.removeEventListener('pointermove', onMove);
                window.removeEventListener('pointerup', onUp);
                el.classList.remove('sgc-win-resizing');
                const finalRect = el.getBoundingClientRect();
                dotnetRef.invokeMethodAsync('UpdateBoundsAsync', 
                    parseFloat(el.style.left), 
                    parseFloat(el.style.top), 
                    finalRect.width, 
                    finalRect.height);
            };
            
            window.addEventListener('pointermove', onMove);
            window.addEventListener('pointerup', onUp, { once: true });
        });
    }
}
