// SuperUI Components JavaScript Helpers
// RichTextEditor and CommandBar support functions

// ----- RichTextEditor -----

const savedRanges = new WeakMap();

function isInsideEditor(editor, node) {
    if (!editor || !node) return false;
    return editor === node || editor.contains(node);
}

function rememberRange(editor) {
    const sel = window.getSelection();
    if (!sel || sel.rangeCount === 0) return;
    const range = sel.getRangeAt(0);
    if (isInsideEditor(editor, range.commonAncestorContainer)) {
        savedRanges.set(editor, range.cloneRange());
    }
}

function restoreRange(editor) {
    const range = savedRanges.get(editor);
    if (!range) return false;
    const sel = window.getSelection();
    sel.removeAllRanges();
    sel.addRange(range);
    return true;
}

function sanitizeHtml(html) {
    const tpl = document.createElement('template');
    tpl.innerHTML = html;
    
    const dangerousTags = new Set([
        'script', 'style', 'link', 'meta', 'iframe', 'object', 
        'embed', 'form', 'input', 'button', 'select', 'textarea',
        'svg', 'math', 'base', 'frameset'
    ]);
    
    const walker = document.createTreeWalker(tpl.content, NodeFilter.SHOW_ELEMENT);
    const toRemove = [];
    let node;
    while (node = walker.nextNode()) {
        if (dangerousTags.has(node.tagName.toLowerCase())) {
            toRemove.push(node);
            continue;
        }
        [...node.attributes].forEach(attr => {
            if (/^on[a-z]+$/i.test(attr.name)) {
                node.removeAttribute(attr.name);
            }
            if (/^(href|src|action|formaction|xlink:href)$/i.test(attr.name)) {
                if (typeof attr.value === 'string' && attr.value.trim().toLowerCase().startsWith('javascript:')) {
                    node.removeAttribute(attr.name);
                }
            }
        });
    }
    toRemove.forEach(n => n.remove());
    return tpl.innerHTML;
}

export function initRichTextEditor(editorElement, dotnetRef, placeholder) {
    if (!editorElement) return;

    let isDisposed = false;
    editorElement._dotnetRef = dotnetRef;
    editorElement._isDisposed = false;

    if (placeholder) {
        editorElement.setAttribute('data-placeholder', placeholder);
    }

    // Sanitised paste — strip scripts and event handlers, keep formatting.
    editorElement.addEventListener('paste', (e) => {
        if (isDisposed || !dotnetRef) return;
        e.preventDefault();
        const html = e.clipboardData.getData('text/html');
        const text = e.clipboardData.getData('text/plain');
        if (html) {
            const sanitized = sanitizeHtml(html);
            document.execCommand('insertHTML', false, sanitized);
        } else if (text) {
            document.execCommand('insertText', false, text);
        }
    });

    // Drag & drop image support — embeds as data URL.
    editorElement.addEventListener('dragover', (e) => {
        if (isDisposed || !dotnetRef) return;
        if (e.dataTransfer && [...e.dataTransfer.items].some(i => i.kind === 'file')) {
            e.preventDefault();
            editorElement.classList.add('sgc-richtext-dropping');
        }
    });
    editorElement.addEventListener('dragleave', () => {
        editorElement.classList.remove('sgc-richtext-dropping');
    });
    editorElement.addEventListener('drop', (e) => {
        if (isDisposed || !dotnetRef) return;
        editorElement.classList.remove('sgc-richtext-dropping');
        const files = e.dataTransfer?.files;
        if (!files || files.length === 0) return;
        const images = [...files].filter(f => f.type.startsWith('image/'));
        if (images.length === 0) return;
        e.preventDefault();
        editorElement.focus();
        images.forEach(file => {
            const reader = new FileReader();
            reader.onload = () => {
                if (!isDisposed && dotnetRef) {
                    document.execCommand('insertHTML', false,
                        `<img src="${reader.result}" alt="${file.name.replace(/"/g, '')}" />`);
                    try {
                        dotnetRef.invokeMethodAsync('NotifyContentChanged').catch(() => {});
                    } catch { }
                }
            };
            reader.readAsDataURL(file);
        });
    });

    // Track caret so toolbar popovers can restore the selection.
    const trackSelection = () => rememberRange(editorElement);
    editorElement.addEventListener('keyup', trackSelection);
    editorElement.addEventListener('mouseup', trackSelection);
    editorElement.addEventListener('focus', trackSelection);
    editorElement._trackSelection = trackSelection;

    const onSelectionChange = () => {
        if (isDisposed || !dotnetRef) return;
        const sel = window.getSelection();
        if (sel && sel.rangeCount > 0 &&
            isInsideEditor(editorElement, sel.anchorNode)) {
            rememberRange(editorElement);
            try {
                dotnetRef.invokeMethodAsync('UpdateActiveFormats').catch(() => {});
            } catch { }
        }
    };
    document.addEventListener('selectionchange', onSelectionChange);
    editorElement._selectionChangeHandler = onSelectionChange;
    
    // Store dispose function
    editorElement._dispose = function() {
        isDisposed = true;
        dotnetRef = null;
    };
}

export function disposeRichTextEditor(editorElement) {
    if (!editorElement) return;

    if (editorElement._dispose) {
        editorElement._dispose();
    }

    if (editorElement._selectionChangeHandler) {
        document.removeEventListener('selectionchange', editorElement._selectionChangeHandler);
    }
    if (editorElement._trackSelection) {
        editorElement.removeEventListener('keyup', editorElement._trackSelection);
        editorElement.removeEventListener('mouseup', editorElement._trackSelection);
        editorElement.removeEventListener('focus', editorElement._trackSelection);
    }

    editorElement._dotnetRef = null;
}

export function execCommand(command, value = null) {
    document.execCommand(command, false, value);
}

export function execCommandOn(editorElement, command, value = null) {
    if (editorElement) {
        editorElement.focus();
        restoreRange(editorElement);
    }
    document.execCommand(command, false, value);
    if (editorElement) rememberRange(editorElement);
}

export function queryCommandValue(command) {
    return document.queryCommandValue(command);
}

export function queryActiveFormats() {
    const formats = [];
    const commands = [
        'bold', 'italic', 'underline', 'strikeThrough',
        'subscript', 'superscript',
        'insertUnorderedList', 'insertOrderedList',
        'justifyLeft', 'justifyCenter', 'justifyRight', 'justifyFull'
    ];

    commands.forEach(cmd => {
        try { if (document.queryCommandState(cmd)) formats.push(cmd); } catch { }
    });

    return formats;
}

export function getHtmlContent(editorElement) {
    return editorElement?.innerHTML || '';
}

export function getTextContent(editorElement) {
    return editorElement?.innerText || '';
}

export function getSelectedText() {
    const sel = window.getSelection();
    return sel ? sel.toString() : '';
}

export function setHtmlContent(editorElement, html) {
    if (editorElement) {
        editorElement.innerHTML = html;
    }
}

export function insertHtml(html) {
    document.execCommand('insertHTML', false, html);
}

export function insertHtmlAt(editorElement, html) {
    if (!editorElement) return;
    editorElement.focus();
    restoreRange(editorElement);
    document.execCommand('insertHTML', false, html);
    rememberRange(editorElement);
}

export function focus(editorElement) {
    editorElement?.focus();
}

export function saveSelection(editorElement) {
    rememberRange(editorElement);
}

export function restoreSelection(editorElement) {
    if (!editorElement) return;
    editorElement.focus();
    restoreRange(editorElement);
}

export function setBlockFormat(editorElement, tag) {
    if (!editorElement) return;
    editorElement.focus();
    restoreRange(editorElement);
    document.execCommand('formatBlock', false, tag);
    rememberRange(editorElement);
}

export function readImageFile(input) {
    return new Promise((resolve, reject) => {
        if (!input || !input.files || input.files.length === 0) {
            resolve(null);
            return;
        }
        const file = input.files[0];
        if (!file.type.startsWith('image/')) {
            resolve(null);
            return;
        }
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

// ----- CommandBar -----

// WeakMap keyed by DOM element — не удерживает узлы и DotNetObjectReference
// если Blazor забыл вызвать disposeCommandBar.
const commandBarInstances = new WeakMap();

export function initCommandBar(cmdBarElement, dotnetRef) {
    if (!cmdBarElement) return;
    
    let isDisposed = false;
    
    const resizeObserver = new ResizeObserver((entries) => {
        if (isDisposed || !dotnetRef) return;
        
        try {
            for (const entry of entries) {
                const width = entry.contentRect.width;
                // Account for overflow button and far content
                const availableWidth = Math.round(width - 60);
                if (dotnetRef && !isDisposed) {
                    dotnetRef.invokeMethodAsync('UpdateOverflow', Math.max(0, availableWidth)).catch(() => {});
                }
            }
        } catch { }
    });
    
    resizeObserver.observe(cmdBarElement);
    
    commandBarInstances.set(cmdBarElement, { 
        resizeObserver, 
        dotnetRef,
        isDisposed: false,
        dispose: function() {
            this.isDisposed = true;
            dotnetRef = null;
        }
    });
}

export function disposeCommandBar(cmdBarElement) {
    const instance = commandBarInstances.get(cmdBarElement);
    if (instance) {
        if (instance.dispose) {
            instance.dispose();
        }
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
