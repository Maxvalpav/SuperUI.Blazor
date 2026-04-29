// DataGrid JavaScript module for performance optimizations

export function init(gridElement, dotNetRef) {
    if (!gridElement) return;
    
    gridElement._sgDataGrid = {
        dotNetRef: dotNetRef,
        selectedRows: new Set()
    };
}

export function updateRowSelection(gridElement, rowIndex, selected) {
    if (!gridElement) return;
    
    const state = gridElement._sgDataGrid;
    if (!state) return;
    
    // Find the checkbox for this row
    const tbody = gridElement.querySelector('tbody');
    if (!tbody) return;
    
    const rows = tbody.querySelectorAll('tr:not(.sg-group-row)');
    if (rowIndex < 0 || rowIndex >= rows.length) return;
    
    const row = rows[rowIndex];
    const checkbox = row.querySelector('.sg-row-checkbox');
    
    if (checkbox) {
        checkbox.checked = selected;
    }
    
    // Update row class
    if (selected) {
        row.classList.add('sg-selected');
        state.selectedRows.add(rowIndex);
    } else {
        row.classList.remove('sg-selected');
        state.selectedRows.delete(rowIndex);
    }
}

export function updateAllRowsSelection(gridElement, selected) {
    if (!gridElement) return;
    
    const state = gridElement._sgDataGrid;
    if (!state) return;
    
    const tbody = gridElement.querySelector('tbody');
    if (!tbody) return;
    
    const rows = tbody.querySelectorAll('tr:not(.sg-group-row)');
    
    rows.forEach((row, index) => {
        const checkbox = row.querySelector('.sg-row-checkbox');
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
    const headerCheckbox = gridElement.querySelector('thead .sg-col-check input[type="checkbox"]');
    if (headerCheckbox) {
        headerCheckbox.checked = selected;
    }
}

export function setActiveRow(gridElement, rowIndex) {
    if (!gridElement) return;
    
    const tbody = gridElement.querySelector('tbody');
    if (!tbody) return;
    
    const rows = tbody.querySelectorAll('tr:not(.sg-group-row)');
    
    // Remove active class from all rows
    rows.forEach(row => row.classList.remove('sg-active'));
    
    // Add active class to the specific row
    if (rowIndex >= 0 && rowIndex < rows.length) {
        const row = rows[rowIndex];
        row.classList.add('sg-active');
        
        // Ensure row is visible
        row.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    }
}

export function dispose(gridElement) {
    if (gridElement && gridElement._sgDataGrid) {
        gridElement._sgDataGrid.dotNetRef = null;
        gridElement._sgDataGrid.selectedRows.clear();
        delete gridElement._sgDataGrid;
    }
}
