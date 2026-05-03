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

export function initRichTextEditor(editorElement, dotnetRef, placeholder) {
    if (!editorElement) return;

    editorElement._dotnetRef = dotnetRef;

    if (placeholder) {
        editorElement.setAttribute('data-placeholder', placeholder);
    }

    // Sanitised paste — strip scripts and event handlers, keep formatting.
    editorElement.addEventListener('paste', (e) => {
        e.preventDefault();
        const html = e.clipboardData.getData('text/html');
        const text = e.clipboardData.getData('text/plain');
        if (html) {
            const tpl = document.createElement('template');
            tpl.innerHTML = html;
            tpl.content.querySelectorAll('script, style, link, meta').forEach(n => n.remove());
            tpl.content.querySelectorAll('*').forEach(el => {
                [...el.attributes].forEach(a => {
                    if (a.name.startsWith('on')) el.removeAttribute(a.name);
                });
            });
            document.execCommand('insertHTML', false, tpl.innerHTML);
        } else if (text) {
            document.execCommand('insertText', false, text);
        }
    });

    // Drag & drop image support — embeds as data URL.
    editorElement.addEventListener('dragover', (e) => {
        if (e.dataTransfer && [...e.dataTransfer.items].some(i => i.kind === 'file')) {
            e.preventDefault();
            editorElement.classList.add('sgc-richtext-dropping');
        }
    });
    editorElement.addEventListener('dragleave', () => {
        editorElement.classList.remove('sgc-richtext-dropping');
    });
    editorElement.addEventListener('drop', (e) => {
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
                document.execCommand('insertHTML', false,
                    `<img src="${reader.result}" alt="${file.name.replace(/"/g, '')}" />`);
                if (dotnetRef) dotnetRef.invokeMethodAsync('NotifyContentChanged');
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
        const sel = window.getSelection();
        if (sel && sel.rangeCount > 0 &&
            isInsideEditor(editorElement, sel.anchorNode)) {
            rememberRange(editorElement);
            if (dotnetRef) dotnetRef.invokeMethodAsync('UpdateActiveFormats');
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
