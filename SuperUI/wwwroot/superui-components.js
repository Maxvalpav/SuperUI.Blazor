// SuperUI Components JavaScript Helpers
// RichTextEditor and CommandBar support functions

// ----- RichTextEditor -----

export function initRichTextEditor(editorElement, dotnetRef, placeholder) {
    if (!editorElement) return;
    
    editorElement._dotnetRef = dotnetRef;
    
    if (placeholder) {
        editorElement.setAttribute('data-placeholder', placeholder);
    }
    
    // Handle paste to clean up unwanted content
    editorElement.addEventListener('paste', (e) => {
        e.preventDefault();
        const text = e.clipboardData.getData('text/html') || e.clipboardData.getData('text/plain');
        document.execCommand('insertHTML', false, text);
    });
    
    // Update active formats on selection change
    const onSelectionChange = () => {
        if (dotnetRef) {
            dotnetRef.invokeMethodAsync('UpdateActiveFormats');
        }
    };
    
    document.addEventListener('selectionchange', onSelectionChange);
    editorElement._selectionChangeHandler = onSelectionChange;
}

export function disposeRichTextEditor(editorElement) {
    if (!editorElement) return;
    
    if (editorElement._selectionChangeHandler) {
        document.removeEventListener('selectionchange', editorElement._selectionChangeHandler);
    }
    
    editorElement._dotnetRef = null;
}

export function execCommand(command, value = null) {
    document.execCommand(command, false, value);
}

export function queryCommandValue(command) {
    return document.queryCommandValue(command);
}

export function queryActiveFormats() {
    const formats = [];
    const commands = [
        'bold', 'italic', 'underline', 'strikeThrough',
        'insertUnorderedList', 'insertOrderedList',
        'justifyLeft', 'justifyCenter', 'justifyRight', 'justifyFull'
    ];
    
    commands.forEach(cmd => {
        if (document.queryCommandState(cmd)) {
            formats.push(cmd);
        }
    });
    
    return formats;
}

export function getHtmlContent(editorElement) {
    return editorElement?.innerHTML || '';
}

export function getTextContent(editorElement) {
    return editorElement?.innerText || '';
}

export function setHtmlContent(editorElement, html) {
    if (editorElement) {
        editorElement.innerHTML = html;
    }
}

export function insertHtml(html) {
    document.execCommand('insertHTML', false, html);
}

export function focus(editorElement) {
    editorElement?.focus();
}

// ----- CommandBar -----

const commandBarInstances = new Map();

export function initCommandBar(cmdBarElement, dotnetRef) {
    if (!cmdBarElement) return;
    
    const resizeObserver = new ResizeObserver((entries) => {
        for (const entry of entries) {
            const width = entry.contentRect.width;
            // Account for overflow button and far content
            const availableWidth = Math.round(width - 60);
            if (dotnetRef) {
                dotnetRef.invokeMethodAsync('UpdateOverflow', Math.max(0, availableWidth));
            }
        }
    });
    
    resizeObserver.observe(cmdBarElement);
    
    commandBarInstances.set(cmdBarElement, { resizeObserver, dotnetRef });
}

export function disposeCommandBar(cmdBarElement) {
    const instance = commandBarInstances.get(cmdBarElement);
    if (instance) {
        instance.resizeObserver.disconnect();
        commandBarInstances.delete(cmdBarElement);
    }
}

// ----- Utilities -----

export function selectRange(element, start, end) {
    const range = document.createRange();
    const sel = window.getSelection();
    
    if (element.childNodes.length > 0) {
        range.setStart(element.childNodes[0], start);
        range.setEnd(element.childNodes[0], end);
        sel.removeAllRanges();
        sel.addRange(range);
    }
}

export function getCaretPosition(element) {
    let caretOffset = 0;
    const doc = element.ownerDocument || element.document;
    const win = doc.defaultView || doc.parentWindow;
    const sel = win.getSelection();
    
    if (sel.rangeCount > 0) {
        const range = sel.getRangeAt(0);
        const preCaretRange = range.cloneRange();
        preCaretRange.selectNodeContents(element);
        preCaretRange.setEnd(range.endContainer, range.endOffset);
        caretOffset = preCaretRange.toString().length;
    }
    
    return caretOffset;
}

export function setCaretPosition(element, offset) {
    const range = document.createRange();
    const sel = window.getSelection();
    
    let charCount = 0;
    let found = false;
    
    function traverseNodes(node) {
        if (found) return;
        
        if (node.nodeType === Node.TEXT_NODE) {
            const nextCharCount = charCount + node.length;
            if (offset >= charCount && offset <= nextCharCount) {
                range.setStart(node, offset - charCount);
                range.setEnd(node, offset - charCount);
                found = true;
            }
            charCount = nextCharCount;
        } else {
            for (let i = 0; i < node.childNodes.length; i++) {
                traverseNodes(node.childNodes[i]);
                if (found) return;
            }
        }
    }
    
    traverseNodes(element);
    
    if (found) {
        sel.removeAllRanges();
        sel.addRange(range);
        element.focus();
    }
}
