// SgSpreadsheet JavaScript module - ES6 module for Blazor JS isolation
// Provides optional JS features like clipboard handling, editor focus, etc.

let _dotNetRef = null;
let _rootElement = null;
let _clipboardData = null;

export function init(dotNetRef, rootElement) {
    if (typeof window === 'undefined') return;
    
    _dotNetRef = dotNetRef;
    _rootElement = rootElement;
    
    // Add clipboard event listeners
    if (_rootElement) {
        _rootElement.addEventListener('copy', handleCopy);
        _rootElement.addEventListener('cut', handleCut);
        _rootElement.addEventListener('paste', handlePaste);
    }
    
    // Add keyboard shortcut listeners for formatting (Ctrl+B, Ctrl+I, Ctrl+U)
    if (_rootElement) {
        _rootElement.addEventListener('keydown', handleKeyDown);
    }
}

export function dispose() {
    if (_rootElement) {
        _rootElement.removeEventListener('copy', handleCopy);
        _rootElement.removeEventListener('cut', handleCut);
        _rootElement.removeEventListener('paste', handlePaste);
        _rootElement.removeEventListener('keydown', handleKeyDown);
    }
    _dotNetRef = null;
    _rootElement = null;
    _clipboardData = null;
}

export function focusEditor(editorElement) {
    if (editorElement && typeof editorElement.focus === 'function') {
        // Small delay to ensure element is rendered
        setTimeout(() => editorElement.focus(), 10);
    }
}

// Clipboard handlers
function handleCopy(e) {
    if (!_dotNetRef) return;
    // Let Blazor handle copy via C# logic if needed
    // For now, just prevent default to avoid conflict
    // _dotNetRef.invokeMethodAsync('OnJsCopy');
}

function handleCut(e) {
    if (!_dotNetRef) return;
    // _dotNetRef.invokeMethodAsync('OnJsCut');
}

function handlePaste(e) {
    if (!_dotNetRef) return;
    // _dotNetRef.invokeMethodAsync('OnJsPaste', e.clipboardData.getData('text'));
}

// Keyboard shortcuts
function handleKeyDown(e) {
    if (!_dotNetRef) return;
    
    // Ctrl+B: Bold
    if (e.ctrlKey && e.key === 'b') {
        e.preventDefault();
        _dotNetRef.invokeMethodAsync('OnJsFormat', 'bold');
    }
    // Ctrl+I: Italic
    else if (e.ctrlKey && e.key === 'i') {
        e.preventDefault();
        _dotNetRef.invokeMethodAsync('OnJsFormat', 'italic');
    }
    // Ctrl+U: Underline
    else if (e.ctrlKey && e.key === 'u') {
        e.preventDefault();
        _dotNetRef.invokeMethodAsync('OnJsFormat', 'underline');
    }
}

// Helper: get selected cell range (if needed in future)
export function getSelectedRange() {
    // Placeholder for future selection API
    return null;
}