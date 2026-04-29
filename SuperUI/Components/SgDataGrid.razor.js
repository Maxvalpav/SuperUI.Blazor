/**
 * SgDataGrid Row Click No Re-render Module
 * 
 * This module provides JavaScript functions to manage the sg-active CSS class
 * on row elements without triggering Blazor's StateHasChanged() re-renders.
 * 
 * By managing the active row state in JavaScript instead of Blazor component state,
 * we achieve immediate visual feedback (under 50ms) without full grid re-renders.
 */

/**
 * Set the active row by adding the sg-active class to the specified row element
 * and removing it from all other rows.
 * 
 * @param {HTMLElement} rowElement - The row element to mark as active
 * @returns {void}
 */
export function setActiveRow(rowElement) {
    if (!rowElement) {
        clearActiveRow();
        return;
    }

    // Remove sg-active class from all rows
    const allRows = document.querySelectorAll('tr[data-row-key]');
    allRows.forEach(row => {
        row.classList.remove('sg-active');
    });

    // Add sg-active class to the clicked row
    rowElement.classList.add('sg-active');
}

/**
 * Clear the active row by removing the sg-active class from all rows.
 * 
 * @returns {void}
 */
export function clearActiveRow() {
    const allRows = document.querySelectorAll('tr[data-row-key]');
    allRows.forEach(row => {
        row.classList.remove('sg-active');
    });
}

/**
 * Get the currently active row element.
 * 
 * @returns {HTMLElement|null} The active row element, or null if no row is active
 */
export function getActiveRowElement() {
    return document.querySelector('tr.sg-active');
}

/**
 * Check if a specific row element is active.
 * 
 * @param {HTMLElement} rowElement - The row element to check
 * @returns {boolean} True if the row is active, false otherwise
 */
export function isRowActive(rowElement) {
    return rowElement && rowElement.classList.contains('sg-active');
}

/**
 * Get the row key from a row element's data attribute.
 * 
 * @param {HTMLElement} rowElement - The row element
 * @returns {string|null} The row key, or null if not found
 */
export function getRowKey(rowElement) {
    return rowElement ? rowElement.getAttribute('data-row-key') : null;
}

/**
 * Find a row element by its data-row-key attribute.
 * 
 * @param {string} rowKey - The row key to search for
 * @returns {HTMLElement|null} The row element, or null if not found
 */
export function findRowByKey(rowKey) {
    return document.querySelector(`tr[data-row-key="${rowKey}"]`);
}

/**
 * Initialize the SgDataGrid row click handler.
 * This function sets up event delegation for row clicks.
 * 
 * @param {HTMLElement} gridElement - The grid container element
 * @returns {void}
 */
export function initializeRowClickHandler(gridElement) {
    if (!gridElement) {
        return;
    }

    // Event delegation: handle clicks on any row within the grid
    gridElement.addEventListener('click', (event) => {
        const row = event.target.closest('tr[data-row-key]');
        if (row) {
            setActiveRow(row);
        }
    });
}

/**
 * Restore the active row state from a saved row key.
 * This is useful when the grid is re-rendered and we need to restore the active row.
 * 
 * @param {string} rowKey - The row key to restore
 * @returns {boolean} True if the row was found and restored, false otherwise
 */
export function restoreActiveRow(rowKey) {
    if (!rowKey) {
        clearActiveRow();
        return false;
    }

    const row = findRowByKey(rowKey);
    if (row) {
        setActiveRow(row);
        return true;
    }

    return false;
}


/**
 * Download CSV file
 * 
 * @param {string} filename - The filename for the downloaded file
 * @param {string} csvContent - The CSV content to download
 * @returns {void}
 */
export function downloadCsv(filename, csvContent) {
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    downloadFile(filename, blob);
}

/**
 * Download Excel file
 * 
 * @param {string} filename - The filename for the downloaded file
 * @param {string} htmlContent - The HTML table content to download
 * @returns {void}
 */
export function downloadExcel(filename, htmlContent) {
    const blob = new Blob([htmlContent], { type: 'application/vnd.ms-excel;charset=utf-8;' });
    downloadFile(filename, blob);
}

/**
 * Generic file download helper
 * 
 * @param {string} filename - The filename for the downloaded file
 * @param {Blob} blob - The blob content to download
 * @returns {void}
 */
function downloadFile(filename, blob) {
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    
    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    // Clean up the URL object
    URL.revokeObjectURL(url);
}
