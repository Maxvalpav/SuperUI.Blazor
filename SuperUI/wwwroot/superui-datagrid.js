// DataGrid JavaScript module for performance optimizations

// DOM element cache to reduce querySelectorAll calls (PERF-03)
const domCache = new WeakMap();

function getDomCache(gridElement) {
    if (!domCache.has(gridElement)) {
        domCache.set(gridElement, {
            tbody: null,
            headerCheckbox: null,
            cacheVersion: 0
        });
    }
    return domCache.get(gridElement);
}

function invalidateDomCache(gridElement) {
    const cache = getDomCache(gridElement);
    cache.cacheVersion++;
    cache.tbody = null;
    cache.headerCheckbox = null;
}

function getTbody(gridElement) {
    const cache = getDomCache(gridElement);
    if (!cache.tbody) {
        cache.tbody = gridElement.querySelector('tbody');
    }
    return cache.tbody;
}

function getHeaderCheckbox(gridElement) {
    const cache = getDomCache(gridElement);
    if (!cache.headerCheckbox) {
        cache.headerCheckbox = gridElement.querySelector('thead .sg-col-check input[type="checkbox"]');
    }
    return cache.headerCheckbox;
}

export function init(gridElement, dotNetRef) {
    if (!gridElement) return;
    
    gridElement._sgDataGrid = {
        dotNetRef: dotNetRef,
        selectedRows: new Set()
    };
    
    // Initialize DOM cache
    getDomCache(gridElement);
}

export function updateRowSelection(gridElement, rowKey, selected) {
    if (!gridElement) return;
    
    try {
        const state = gridElement._sgDataGrid;
        if (!state) return;
        
        // Find the row by data-row-key attribute
        const tbody = gridElement.querySelector('tbody');
        if (!tbody) return;
        
        const row = tbody.querySelector(`tr[data-row-key="${rowKey}"]`);
        if (!row) return;
        
        const checkbox = row.querySelector('.sg-row-checkbox');
        
        if (checkbox) {
            checkbox.checked = selected;
        }
        
        // Update row class
        if (selected) {
            row.classList.add('sg-selected');
            state.selectedRows.add(rowKey);
        } else {
            row.classList.remove('sg-selected');
            state.selectedRows.delete(rowKey);
        }
    } catch (e) {
        console.error('Error updating row selection:', e);
    }
}

export function updateAllRowsSelection(gridElement, selected) {
    if (!gridElement) return;
    
    try {
        const state = gridElement._sgDataGrid;
        if (!state) return;
        
        const tbody = getTbody(gridElement);
        if (!tbody) return;
        
        const rows = tbody.querySelectorAll('tr:not(.sg-group-row)');
        
        // PERF-04: Batch DOM operations to prevent layout thrashing
        // Collect all changes first, then apply them in a single batch
        const changes = [];
        
        rows.forEach((row, index) => {
            const checkbox = row.querySelector('.sg-row-checkbox');
            changes.push({ row, checkbox, index });
        });
        
        // Apply all changes in a single batch
        changes.forEach(({ row, checkbox, index }) => {
            if (checkbox) {
                checkbox.checked = selected;
            }
            
            if (selected) {
                row.classList.add('sg-selected');
                state.selectedRows.add(index);
            } else {
                row.classList.remove('sg-selected');
                state.selectedRows.delete(index);
            }
        });
        
        // Update header checkbox
        const headerCheckbox = getHeaderCheckbox(gridElement);
        if (headerCheckbox) {
            headerCheckbox.checked = selected;
        }
    } catch (e) {
        console.error('Error updating all rows selection:', e);
    }
}

export function setActiveRow(gridElement, rowKey) {
    if (!gridElement) return;
    
    try {
        const tbody = getTbody(gridElement);
        if (!tbody) return;
        
        const rows = tbody.querySelectorAll('tr:not(.sg-group-row)');
        
        // Remove active class from all rows
        rows.forEach(row => row.classList.remove('sg-active'));
        
        // Add active class to the specific row by data-row-key
        if (rowKey) {
            const row = tbody.querySelector(`tr[data-row-key="${rowKey}"]`);
            if (row) {
                row.classList.add('sg-active');
                
                // JS-03: Check visibility before scrolling
                // Only scroll if row is not already visible
                const rect = row.getBoundingClientRect();
                const scrollContainer = gridElement.querySelector('.sg-scroll');
                
                if (scrollContainer) {
                    const scrollRect = scrollContainer.getBoundingClientRect();
                    const isVisible = (
                        rect.top >= scrollRect.top &&
                        rect.bottom <= scrollRect.bottom
                    );
                    
                    if (!isVisible) {
                        try {
                            row.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
                        } catch (e) {
                            console.warn('Failed to scroll row into view:', e);
                        }
                    }
                }
            }
        }
    } catch (e) {
        console.error('Error setting active row:', e);
    }
}

export function dispose(gridElement) {
    if (gridElement && gridElement._sgDataGrid) {
        try {
            const dotNetRef = gridElement._sgDataGrid.dotNetRef;
            gridElement._sgDataGrid.selectedRows.clear();
            delete gridElement._sgDataGrid;
            
            // Invalidate DOM cache
            invalidateDomCache(gridElement);
            
            // Dispose the .NET reference to allow garbage collection
            if (dotNetRef) {
                try {
                    dotNetRef.dispose();
                } catch (e) {
                    console.warn('Failed to dispose dotNetRef:', e);
                }
            }
        } catch (e) {
            console.error('Error during dispose:', e);
        }
    }
}
