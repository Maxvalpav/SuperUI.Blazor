// SuperUI Components JavaScript Helpers
// RichTextEditor and CommandBar support functions

// ----- RichTextEditor -----

const savedRanges = new WeakMap();
const selectionThrottle = new WeakMap();
const editorInstances = new WeakMap();

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

function debounce(fn, delay) {
    let timer;
    return function(...args) {
        clearTimeout(timer);
        timer = setTimeout(() => fn.apply(this, args), delay);
    };
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
            if (/^on[a-z]+$/i.test(attr.name)) node.removeAttribute(attr.name);
            if (/^(href|src|action|formaction|xlink:href)$/i.test(attr.name)) {
                if (typeof attr.value === 'string' && attr.value.trim().toLowerCase().startsWith('javascript:'))
                    node.removeAttribute(attr.name);
            }
        });
    }
    toRemove.forEach(n => n.remove());
    return tpl.innerHTML;
}

function cleanWordHtml(html) {
    if (!html) return html;
    return html
        .replace(/<!--[\s\S]*?-->/g, '')
        .replace(/<meta[^>]*>/gi, '')
        .replace(/<link[^>]*>/gi, '')
        .replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '')
        .replace(/<xml[^>]*>[\s\S]*?<\/xml>/gi, '')
        .replace(/<o:[^>]*>[\s\S]*?<\/o:[^>]*>/gi, '')
        .replace(/<w:[^>]*>[\s\S]*?<\/w:[^>]*>/gi, '')
        .replace(/class="[^"]*Mso[^"]*"/gi, '')
        .replace(/class="[^"]*"/g, '')
        .replace(/style="[^"]*"/g, '');
}

function insertTableCmd(editorElement, rows, cols) {
    if (!editorElement) return;
    editorElement.focus();
    restoreRange(editorElement);
    let table = '<table style="width:100%;border-collapse:collapse;">';
    for (let r = 0; r < rows; r++) {
        table += '<tr>';
        for (let c = 0; c < cols; c++)
            table += `<td style="border:1px solid #ccc;padding:8px;min-width:40px;">&nbsp;</td>`;
        table += '</tr>';
    }
    table += '</table><br>';
    document.execCommand('insertHTML', false, table);
    rememberRange(editorElement);
}

// ─── Markdown Shortcuts ─────────────────────────────────────────────
function handleMarkdownShortcut(editor, e, dotnetRef) {
    if (!editor || e.isComposing) return false;
    const sel = window.getSelection();
    if (!sel || !sel.rangeCount) return false;
    const range = sel.getRangeAt(0);
    const node = range.startContainer;
    if (node.nodeType !== Node.TEXT_NODE) return false;
    const text = node.textContent;
    const pos = range.startOffset;
    const lineStart = text.lastIndexOf('\n', pos - 1) + 1;
    const before = text.substring(lineStart, pos);

    let handled = false;
    if (e.key === ' ') {
        const trimmed = before.trimEnd();
        const insertAfter = (html) => {
            e.preventDefault();
            node.textContent = text.substring(0, lineStart) + text.substring(pos);
            const r = document.createRange();
            r.setStart(node, lineStart);
            r.collapse(true);
            sel.removeAllRanges();
            sel.addRange(r);
            document.execCommand('insertHTML', false, html);
            dotnetRef.invokeMethodAsync('NotifyContentChanged').catch(()=>{});
            handled = true;
        };
        if (trimmed === '#') { insertAfter('<h1>&nbsp;</h1>'); }
        else if (trimmed === '##') { insertAfter('<h2>&nbsp;</h2>'); }
        else if (trimmed === '###') { insertAfter('<h3>&nbsp;</h3>'); }
        else if (trimmed === '>') { insertAfter('<blockquote>&nbsp;</blockquote>'); }
        else if (trimmed === '-') { insertAfter('<ul><li>&nbsp;</li></ul>'); }
        else if (/^1\d*$/.test(trimmed)) { insertAfter('<ol><li>&nbsp;</li></ol>'); }
    }
    else if (e.key === 'Enter') {
        const trimmed = before.trim();
        if (trimmed === '---') {
            e.preventDefault();
            node.textContent = text.substring(0, lineStart) + text.substring(pos);
            const r = document.createRange();
            r.setStart(node, lineStart);
            r.collapse(true);
            sel.removeAllRanges();
            sel.addRange(r);
            document.execCommand('insertHTML', false, '<hr><br>');
            dotnetRef.invokeMethodAsync('NotifyContentChanged').catch(()=>{});
            handled = true;
        }
    }
    return handled;
}

// ─── Slash Command Menu ─────────────────────────────────────────────
function createSlashMenu(editor) {
    const existing = editor.closest('.sgc-richtext').querySelector('.sgc-richtext-slash-menu');
    if (existing) return existing;
    const menu = document.createElement('div');
    menu.className = 'sgc-richtext-slash-menu';
    menu.style.display = 'none';
    editor.closest('.sgc-richtext').appendChild(menu);
    return menu;
}

function getSlashItems(dotnetRef) {
    return [
        { icon: '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 3h18v18H3z"/><path d="M3 9h18"/><path d="M9 3v18"/></svg>', label: 'Table', action: () => dotnetRef.invokeMethodAsync('InsertTableCommand').catch(()=>{}) },
        { icon: '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>', label: 'Link', action: () => dotnetRef.invokeMethodAsync('ToggleLinkPopover').catch(()=>{}) },
        { icon: '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>', label: 'Image', action: () => dotnetRef.invokeMethodAsync('ToggleImagePopover').catch(()=>{}) },
        { icon: '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 7h16"/><path d="M10 7l-3 13"/><path d="M14 7l1 5"/></svg>', label: 'Quote', action: () => execCommandOn(editor, 'formatBlock', 'blockquote') },
        { icon: '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2"><polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/></svg>', label: 'Code Block', action: () => execCommandOn(editor, 'formatBlock', 'pre') },
        { icon: '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2"><line x1="3" y1="12" x2="21" y2="12"/><line x1="6" y1="5" x2="18" y2="5" stroke-dasharray="2 3"/></svg>', label: 'Divider', action: () => execCommandOn(editor, 'insertHTML', '<hr>') },
        { icon: '<span style="font-size:16px">😊</span>', label: 'Emoji', action: () => dotnetRef.invokeMethodAsync('ToggleEmojiPopover').catch(()=>{}) },
    ];
}

let _slashMenuActive = false;
let _slashFilterIndex = 0;
let _slashFiltered = [];

function handleSlashKeydown(editor, e, dotnetRef) {
    const sel = window.getSelection();
    if (!sel || !sel.rangeCount) return false;
    const range = sel.getRangeAt(0);
    const node = range.startContainer;
    if (node.nodeType !== Node.TEXT_NODE) return false;
    const text = node.textContent;
    const pos = range.startOffset;

    if (e.key === '/' && !e.ctrlKey && !e.metaKey) {
        const charBefore = pos > 0 ? text[pos - 1] : '';
        if (charBefore === '' || charBefore === '\n' || charBefore === ' ') {
            // Start slash menu after a brief delay to capture subsequent typing
            const menu = createSlashMenu(editor);
            const items = getSlashItems(dotnetRef);
            _slashFiltered = items;
            _slashFilterIndex = 0;
            renderSlashMenu(menu, items, 0, '');
            positionSlashMenu(menu, editor);
            menu.style.display = 'block';
            _slashMenuActive = true;
            return false; // Don't prevent default - let '/' be typed
        }
    }

    if (_slashMenuActive) {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            _slashFilterIndex = Math.min(_slashFilterIndex + 1, _slashFiltered.length - 1);
            const menu = editor.closest('.sgc-richtext').querySelector('.sgc-richtext-slash-menu');
            if (menu) renderSlashMenu(menu, _slashFiltered, _slashFilterIndex, '');
            return true;
        }
        if (e.key === 'ArrowUp') {
            e.preventDefault();
            _slashFilterIndex = Math.max(_slashFilterIndex - 1, 0);
            const menu = editor.closest('.sgc-richtext').querySelector('.sgc-richtext-slash-menu');
            if (menu) renderSlashMenu(menu, _slashFiltered, _slashFilterIndex, '');
            return true;
        }
        if (e.key === 'Enter' || e.key === 'Tab') {
            e.preventDefault();
            const item = _slashFiltered[_slashFilterIndex];
            if (item) {
                hideSlashMenu(editor);
                // Remove the '/' prefix
                const prefix = '/';
                const beforeSlash = text.lastIndexOf('\n', pos - 1) + 1;
                const slashPos = text.indexOf('/', beforeSlash);
                if (slashPos >= 0 && slashPos < pos) {
                    node.textContent = text.substring(0, slashPos) + text.substring(pos);
                    const r = document.createRange();
                    r.setStart(node, slashPos);
                    r.collapse(true);
                    sel.removeAllRanges();
                    sel.addRange(r);
                }
                item.action();
            }
            return true;
        }
        if (e.key === 'Escape') {
            e.preventDefault();
            hideSlashMenu(editor);
            return true;
        }
        // Filter
        if (e.key.length === 1) {
            const menu = editor.closest('.sgc-richtext').querySelector('.sgc-richtext-slash-menu');
            if (menu) {
                const items = getSlashItems(dotnetRef);
                // Get text after slash
                const beforeSlash = text.lastIndexOf('\n', pos - 1) + 1;
                const slashIdx = text.indexOf('/', beforeSlash);
                const query = slashIdx >= 0 ? text.substring(slashIdx + 1, pos) + e.key : e.key;
                _slashFiltered = items.filter(i => i.label.toLowerCase().includes(query.toLowerCase()));
                _slashFilterIndex = 0;
                renderSlashMenu(menu, _slashFiltered, 0, query);
                positionSlashMenu(menu, editor);
                if (_slashFiltered.length === 0) {
                    menu.style.display = 'none';
                } else {
                    menu.style.display = 'block';
                }
            }
        }
    }
    return false;
}

function renderSlashMenu(menu, items, selectedIndex, query) {
    menu.innerHTML = '';
    if (items.length === 0) {
        menu.innerHTML = '<div class="sgc-richtext-slash-empty">No results</div>';
        return;
    }
    items.forEach((item, i) => {
        const div = document.createElement('div');
        div.className = `sgc-richtext-slash-item${i === selectedIndex ? ' sgc-active' : ''}`;
        div.innerHTML = `<span class="sgc-richtext-slash-icon">${item.icon}</span><span class="sgc-richtext-slash-label">${item.label}</span>`;
        div.onmouseenter = () => { _slashFilterIndex = i; renderSlashMenu(menu, items, i, query); };
        div.onclick = (e) => {
            e.stopPropagation();
            hideSlashMenu(menu.parentNode?.querySelector('[contenteditable]') || menu);
            item.action();
        };
        menu.appendChild(div);
    });
}

function positionSlashMenu(menu, editor) {
    const sel = window.getSelection();
    if (!sel || !sel.rangeCount) return;
    const range = sel.getRangeAt(0);
    const rect = range.getBoundingClientRect();
    const editorRect = editor.getBoundingClientRect();
    const top = rect.top - editorRect.top + rect.height + 4;
    const left = Math.max(0, rect.left - editorRect.left);
    menu.style.top = `${top}px`;
    menu.style.left = `${left}px`;
}

function hideSlashMenu(editor) {
    _slashMenuActive = false;
    const menu = editor.closest('.sgc-richtext')?.querySelector('.sgc-richtext-slash-menu');
    if (menu) menu.style.display = 'none';
}

// ─── Floating Toolbar ───────────────────────────────────────────────
function createFloatingToolbar(editor) {
    const existing = editor.closest('.sgc-richtext').querySelector('.sgc-richtext-floatbar');
    if (existing) return existing;
    const bar = document.createElement('div');
    bar.className = 'sgc-richtext-floatbar';
    bar.style.display = 'none';
    bar.innerHTML = `
        <button type="button" class="sgc-richtext-floatbar-btn" data-cmd="bold" title="Bold"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M6 4h8a4 4 0 0 1 4 4 4 4 0 0 1-4 4H6z"/><path d="M6 12h9a4 4 0 0 1 4 4 4 4 0 0 1-4 4H6z"/></svg></button>
        <button type="button" class="sgc-richtext-floatbar-btn" data-cmd="italic" title="Italic"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.2"><line x1="19" y1="4" x2="10" y2="4"/><line x1="14" y1="20" x2="5" y2="20"/><line x1="15" y1="4" x2="9" y2="20"/></svg></button>
        <button type="button" class="sgc-richtext-floatbar-btn" data-cmd="underline" title="Underline"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.2"><path d="M6 3v7a6 6 0 0 0 12 0V3"/><line x1="4" y1="21" x2="20" y2="21"/></svg></button>
        <span class="sgc-richtext-floatbar-sep"></span>
        <button type="button" class="sgc-richtext-floatbar-btn" data-cmd="link" title="Link"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg></button>
        <button type="button" class="sgc-richtext-floatbar-btn" data-cmd="code" title="Inline code"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2"><polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/></svg></button>
        <span class="sgc-richtext-floatbar-sep"></span>
        <button type="button" class="sgc-richtext-floatbar-btn" data-cmd="color" title="Text color"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 20h14"/><path d="M7 16l5-12 5 12"/></svg></button>
    `;
    editor.closest('.sgc-richtext').appendChild(bar);

    bar.addEventListener('click', (e) => {
        const btn = e.target.closest('[data-cmd]');
        if (!btn) return;
        const cmd = btn.dataset.cmd;
        if (cmd === 'link') {
            const sel = window.getSelection();
            const text = sel ? sel.toString() : '';
            rememberRange(editor);
            const inst = editorInstances.get(editor);
            if (inst && inst.dotnetRef) inst.dotnetRef.invokeMethodAsync('ToggleLinkPopover').catch(()=>{});
        } else if (cmd === 'color') {
            const inst = editorInstances.get(editor);
            if (inst && inst.dotnetRef) inst.dotnetRef.invokeMethodAsync('ToggleTextColorPopover').catch(()=>{});
        } else {
            document.execCommand(cmd, false, null);
            editor.focus();
        }
    });
    return bar;
}

function updateFloatingToolbar(editor) {
    const bar = editor.closest('.sgc-richtext').querySelector('.sgc-richtext-floatbar');
    if (!bar) return;
    const sel = window.getSelection();
    if (!sel || sel.isCollapsed || sel.rangeCount === 0 ||
        !isInsideEditor(editor, sel.anchorNode)) {
        bar.style.display = 'none';
        return;
    }
    const text = sel.toString().trim();
    if (text.length === 0) { bar.style.display = 'none'; return; }

    const range = sel.getRangeAt(0);
    const rect = range.getBoundingClientRect();
    const editorRect = editor.getBoundingClientRect();
    const barW = bar.offsetWidth || 200;

    let top = rect.top - editorRect.top - bar.offsetHeight - 8;
    if (top < 4) top = rect.bottom - editorRect.top + 8;

    let left = rect.left - editorRect.left + (rect.width / 2) - (barW / 2);
    left = Math.max(4, Math.min(left, editorRect.width - barW - 4));

    bar.style.top = `${top}px`;
    bar.style.left = `${left}px`;
    bar.style.display = 'flex';

    // Update active state
    bar.querySelectorAll('[data-cmd]').forEach(btn => {
        const cmd = btn.dataset.cmd;
        if (cmd === 'link' || cmd === 'color') return;
        try {
            btn.classList.toggle('sgc-active', document.queryCommandState(cmd));
        } catch {}
    });
}

// ─── Emoji Picker Data ──────────────────────────────────────────────
const EMOJI_CATEGORIES = [
    {
        name: 'Smileys',
        items: ['😀','😃','😄','😁','😅','😂','🤣','😊','😇','🙂','😉','😌','😍','🥰','😘','😗','😋','😛','😜','🤪','😝','🤑','🤗','🤭','🤫','🤔','🤐','🤨','😐','😑','😶','😏','😒','🙄','😬','🤥','😌','😔','😪','🤤','😴','😷','🤒','🤕','🤢','🤮','🥴','😵','🤯','🥳','🥺','😢','😭','😤','😡','🤬','💀','☠️']
    },
    {
        name: 'Gestures',
        items: ['👋','🤚','🖐','✋','🖖','👌','🤌','🤏','✌','🤞','🫰','🤟','🤘','🤙','👈','👉','👆','🖕','👇','☝','👍','👎','✊','👊','🤛','🤜','👏','🙌','👐','🤲','🤝','🙏','✍','💅','🤳','💪','🦵','🦶','👂','🦻','👃','🧠','🫀','🫁','🦷','🦴','👀','👁','👅','👄']
    },
    {
        name: 'Food',
        items: ['🍏','🍎','🍐','🍊','🍋','🍌','🍉','🍇','🍓','🫐','🍈','🍒','🍑','🥭','🍍','🥥','🥝','🍅','🍆','🥑','🥦','🥬','🥒','🌽','🥕','🧄','🧅','🥔','🍠','🫘','🥐','🍞','🥖','🥨','🧀','🥚','🍳','🧈','🥞','🧇','🥓','🥩','🍗','🍖','🦴','🌭','🍔','🍟','🍕','🫓','🥪','🥙','🧆','🌮','🌯','🫔','🥗','🥘','🫕','🥫','🍝','🍜','🍲','🍛','🍣','🍱','🥟','🦪','🍤','🍙','🍚','🍘','🍥','🥠','🥮','🍢','🍡','🍧','🍨','🍦','🥧','🧁','🍰','🎂','🍮','🍭','🍬','🍫','🍿','🍩','🍪','🌰','🥜','🍯']
    },
    {
        name: 'Travel',
        items: ['🚗','🚕','🚙','🚌','🚎','🏎','🚓','🚑','🚒','🚐','🛻','🚚','🚛','🚜','🏍','🛵','🛺','🚲','🛴','🛹','🚏','🛣','🛤','⛽','🛳','⛵','🚤','🛶','✈️','🛩','🛫','🛬','🚁','🚟','🚠','🚡','🛰','🚀','🪐','🌍','🌎','🌏','🌐','🗺','🏔','⛰','🌋','🗻','🏕','🏖','🏜','🏝','🏞','🏟','🏛','🏗','🏘','🏚','🏠','🏡','🏢','🏣','🏤','🏥','🏦','🏨','🏩','🏪','🏫','🏬','🏭','🏯','🏰','💒','🗼','🗽','⛪','🕌','🛕','🕍','⛩','🕋']
    },
    {
        name: 'Symbols',
        items: ['❤️','🧡','💛','💚','💙','💜','🖤','🤍','🤎','💔','❣','💕','💞','💓','💗','💖','💘','💝','💟','☮','✝','☪','🕉','☸','✡','🔯','🕎','☯','☦','🛐','⛎','♈','♉','♊','♋','♌','♍','♎','♏','♐','♑','♒','♓','🆔','⚕','♿','⚠','⛔','🚫','❌','⭕','💢','♨','🛑','⛽','💱','💲','♻','🈯','❓','❔','❕','❗','‼','⁉','➕','➖','➗','✖','✔','🔃','🔄','⭐','🌟','✨','💫','🌟','🔥','💯']
    }
];

// ─── Find & Replace ─────────────────────────────────────────────────
function createFindBar(editor) {
    const existing = editor.closest('.sgc-richtext').querySelector('.sgc-richtext-findbar');
    if (existing) return existing;
    const bar = document.createElement('div');
    bar.className = 'sgc-richtext-findbar';
    bar.style.display = 'none';
    bar.innerHTML = `
        <div class="sgc-richtext-findbar-row">
            <input type="text" class="sgc-richtext-findbar-input" placeholder="Find..." id="sgrt-find-input">
            <input type="text" class="sgc-richtext-findbar-input" placeholder="Replace..." id="sgrt-replace-input" style="width:120px;">
            <button type="button" class="sgc-richtext-findbar-btn" id="sgrt-find-prev" title="Previous">▲</button>
            <button type="button" class="sgc-richtext-findbar-btn" id="sgrt-find-next" title="Next">▼</button>
            <span class="sgc-richtext-findbar-count" id="sgrt-find-count">0/0</span>
            <button type="button" class="sgc-richtext-findbar-btn" id="sgrt-replace-btn" title="Replace">R</button>
            <button type="button" class="sgc-richtext-findbar-btn" id="sgrt-replace-all-btn" title="Replace All">R♻</button>
            <button type="button" class="sgc-richtext-findbar-close" id="sgrt-find-close">✕</button>
        </div>
    `;
    editor.closest('.sgc-richtext').appendChild(bar);

    let findIndex = 0;
    let findMatches = [];

    function findText(query) {
        findMatches = [];
        findIndex = 0;
        if (!query || !editor) return;
        const walker = document.createTreeWalker(editor, NodeFilter.SHOW_TEXT);
        let node;
        while (node = walker.nextNode()) {
            const idx = node.textContent.toLowerCase().indexOf(query.toLowerCase());
            if (idx >= 0) findMatches.push({ node, idx, len: query.length });
        }
        highlightMatches(query);
        updateFindCount();
        if (findMatches.length > 0) navigateFind(0);
    }

    function highlightMatches(query) {
        editor.querySelectorAll('.sgc-richtext-find-highlight').forEach(el => {
            const parent = el.parentNode;
            parent.replaceChild(document.createTextNode(el.textContent), el);
            parent.normalize();
        });
        if (!query) return;
        const walker = document.createTreeWalker(editor, NodeFilter.SHOW_TEXT);
        const toReplace = [];
        while (node = walker.nextNode()) {
            const text = node.textContent;
            const idx = text.toLowerCase().indexOf(query.toLowerCase());
            if (idx < 0) continue;
            toReplace.push({ node, idx, len: query.length, text });
        }
        toReplace.forEach(({ node, idx, len, text }) => {
            const after = text.substring(idx + len);
            const span = document.createElement('span');
            span.className = 'sgc-richtext-find-highlight';
            span.textContent = text.substring(idx, idx + len);
            const fragment = document.createDocumentFragment();
            if (idx > 0) fragment.appendChild(document.createTextNode(text.substring(0, idx)));
            fragment.appendChild(span);
            if (after) fragment.appendChild(document.createTextNode(after));
            node.parentNode.replaceChild(fragment, node);
        });
    }

    function navigateFind(dir) {
        editor.querySelectorAll('.sgc-richtext-find-highlight').forEach(el => el.classList.remove('sgc-active'));
        findIndex = ((findIndex + dir) % findMatches.length + findMatches.length) % findMatches.length;
        const match = findMatches[findIndex];
        if (!match) return;
        const highlights = editor.querySelectorAll('.sgc-richtext-find-highlight');
        if (highlights[findIndex]) {
            highlights[findIndex].classList.add('sgc-active');
            highlights[findIndex].scrollIntoView({ block: 'nearest' });
        }
        updateFindCount();
    }

    function updateFindCount() {
        const count = bar.querySelector('#sgrt-find-count');
        if (count) count.textContent = `${Math.min(findIndex + 1, findMatches.length)}/${findMatches.length}`;
    }

    function replaceCurrent(replaceText) {
        const match = findMatches[findIndex];
        if (!match) return;
        const range = document.createRange();
        range.setStart(match.node, match.idx);
        range.setEnd(match.node, match.idx + match.len);
        range.deleteContents();
        range.insertNode(document.createTextNode(replaceText));
        editor.normalize();
        findText(bar.querySelector('#sgrt-find-input')?.value || '');
    }

    function replaceAll(replaceText) {
        while (findMatches.length > 0) {
            const m = findMatches[0];
            const range = document.createRange();
            range.setStart(m.node, m.idx);
            range.setEnd(m.node, m.idx + m.len);
            range.deleteContents();
            range.insertNode(document.createTextNode(replaceText));
            editor.normalize();
            findText(bar.querySelector('#sgrt-find-input')?.value || '');
        }
    }

    bar._findText = findText;
    bar._navigateFind = navigateFind;
    bar._replaceCurrent = replaceCurrent;
    bar._replaceAll = replaceAll;

    return bar;
}

// ─── Image Resize ───────────────────────────────────────────────────
function addImageResizeHandles(editor) {
    editor.querySelectorAll('img:not([data-resizable])').forEach(img => {
        img.setAttribute('data-resizable', 'true');
        img.style.cursor = 'se-resize';
        img.addEventListener('mousedown', function(e) {
            if (e.target !== img) return;
            const startX = e.clientX;
            const startW = img.offsetWidth;
            const startH = img.offsetHeight;

            function onMouseMove(ev) {
                const dx = ev.clientX - startX;
                const ratio = startH / startW;
                img.style.width = `${Math.max(40, startW + dx)}px`;
                img.style.height = `${Math.max(30, (startW + dx) * ratio)}px`;
            }
            function onMouseUp() {
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
                const inst = editorInstances.get(editor);
                if (inst && inst.dotnetRef)
                    inst.dotnetRef.invokeMethodAsync('NotifyContentChanged').catch(()=>{});
            }
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        });
    });
}

// ─── Export as Markdown ─────────────────────────────────────────────
function htmlToMarkdown(html) {
    const div = document.createElement('div');
    div.innerHTML = html;
    let md = '';

    function convertNode(node, indent) {
        if (node.nodeType === Node.TEXT_NODE) {
            md += node.textContent;
            return;
        }
        if (node.nodeType !== Node.ELEMENT_NODE) return;
        const tag = node.tagName.toLowerCase();
        const style = node.style;

        switch (tag) {
            case 'h1': md += `\n# ${node.textContent.trim()}\n\n`; break;
            case 'h2': md += `\n## ${node.textContent.trim()}\n\n`; break;
            case 'h3': md += `\n### ${node.textContent.trim()}\n\n`; break;
            case 'h4': md += `\n#### ${node.textContent.trim()}\n\n`; break;
            case 'p': md += `${node.textContent.trim()}\n\n`; break;
            case 'br': md += '\n'; break;
            case 'hr': md += '\n---\n\n'; break;
            case 'blockquote': md += `> ${node.textContent.trim()}\n\n`; break;
            case 'ul':
                node.childNodes.forEach(li => {
                    if (li.nodeType === Node.ELEMENT_NODE && li.tagName.toLowerCase() === 'li')
                        md += `${indent}- ${li.textContent.trim()}\n`;
                });
                md += '\n';
                break;
            case 'ol':
                let idx = 1;
                node.childNodes.forEach(li => {
                    if (li.nodeType === Node.ELEMENT_NODE && li.tagName.toLowerCase() === 'li')
                        md += `${indent}${idx++}. ${li.textContent.trim()}\n`;
                });
                md += '\n';
                break;
            case 'li': break;
            case 'pre': md += `\`\`\`\n${node.textContent}\n\`\`\`\n\n`; break;
            case 'code': md += `\`${node.textContent}\``; break;
            case 'a': {
                const href = node.getAttribute('href') || '';
                md += `[${node.textContent}](${href})`;
                break;
            }
            case 'img': {
                const src = node.getAttribute('src') || '';
                const alt = node.getAttribute('alt') || '';
                md += `![${alt}](${src})`;
                break;
            }
            case 'strong': case 'b': md += `**${node.textContent}**`; break;
            case 'em': case 'i': md += `*${node.textContent}*`; break;
            case 'u': md += `<u>${node.textContent}</u>`; break;
            case 's': case 'strike': md += `~~${node.textContent}~~`; break;
            case 'table': {
                node.querySelectorAll('tr').forEach(tr => {
                    const cells = tr.querySelectorAll('th, td');
                    cells.forEach(cell => md += `| ${cell.textContent.trim()} `);
                    md += '|\n';
                });
                md += '\n';
                break;
            }
            default:
                node.childNodes.forEach(child => convertNode(child, indent));
        }
    }

    div.childNodes.forEach(child => convertNode(child, ''));
    return md.trim();
}

// ─── Main Init ──────────────────────────────────────────────────────
export function initRichTextEditor(editorElement, dotnetRef, placeholder) {
    if (!editorElement) return;

    let isDisposed = false;
    const inst = { isDisposed: false, dotnetRef };
    editorInstances.set(editorElement, inst);
    editorElement._dotnetRef = dotnetRef;
    editorElement._isDisposed = false;

    if (placeholder) editorElement.setAttribute('data-placeholder', placeholder);

    // ── Paste ──
    editorElement.addEventListener('paste', (e) => {
        if (isDisposed || !dotnetRef) return;
        e.preventDefault();
        const html = e.clipboardData.getData('text/html');
        const text = e.clipboardData.getData('text/plain');
        if (html) {
            const sanitized = sanitizeHtml(cleanWordHtml(html) || html);
            document.execCommand('insertHTML', false, sanitized);
        } else if (text) {
            document.execCommand('insertText', false, text);
        }
    });

    // ── Drag & Drop ──
    editorElement.addEventListener('dragover', (e) => {
        if (isDisposed || !dotnetRef) return;
        if (e.dataTransfer && [...e.dataTransfer.items].some(i => i.kind === 'file')) {
            e.preventDefault();
            editorElement.classList.add('sgc-richtext-dropping');
        }
    });
    editorElement.addEventListener('dragleave', () => editorElement.classList.remove('sgc-richtext-dropping'));
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
                        `<img src="${reader.result}" alt="${file.name.replace(/"/g, '')}" style="max-width:100%;" />`);
                    addImageResizeHandles(editorElement);
                    try { dotnetRef.invokeMethodAsync('NotifyContentChanged').catch(()=>{}); } catch {}
                }
            };
            reader.readAsDataURL(file);
        });
    });

    // ── Track selection ──
    const trackSelection = () => rememberRange(editorElement);
    editorElement.addEventListener('keyup', trackSelection);
    editorElement.addEventListener('mouseup', trackSelection);
    editorElement.addEventListener('focus', trackSelection);
    editorElement._trackSelection = trackSelection;

    // ── Throttled selection change ──
    const onSelectionChange = () => {
        if (isDisposed || !dotnetRef) return;
        const sel = window.getSelection();
        if (sel && sel.rangeCount > 0 && isInsideEditor(editorElement, sel.anchorNode)) {
            rememberRange(editorElement);
            const now = Date.now();
            const last = selectionThrottle.get(editorElement) || 0;
            if (now - last >= 80) {
                selectionThrottle.set(editorElement, now);
                try { dotnetRef.invokeMethodAsync('UpdateActiveFormats').catch(()=>{}); } catch {}
            }
            // Floating toolbar
            const ft = editorElement.closest('.sgc-richtext')?.querySelector('.sgc-richtext-floatbar');
            if (ft) updateFloatingToolbar(editorElement);
        } else {
            const ft = editorElement.closest('.sgc-richtext')?.querySelector('.sgc-richtext-floatbar');
            if (ft) ft.style.display = 'none';
        }
    };
    document.addEventListener('selectionchange', onSelectionChange);
    editorElement._selectionChangeHandler = onSelectionChange;

    // ── Markdown + Slash commands via keydown ──
    const onKeyDown = (e) => {
        if (isDisposed) return;
        // Markdown shortcuts
        if (!e.ctrlKey && !e.metaKey && (e.key === ' ' || e.key === 'Enter')) {
            if (handleMarkdownShortcut(editorElement, e, dotnetRef)) return;
        }
        // Slash menu
        if (!e.ctrlKey && !e.metaKey) {
            handleSlashKeydown(editorElement, e, dotnetRef);
        }
        // Image resize check on paste/insert
        setTimeout(() => addImageResizeHandles(editorElement), 100);
    };
    editorElement.addEventListener('keydown', onKeyDown);
    editorElement._onKeyDown = onKeyDown;

    // ── MutationObserver for image resize ──
    const observer = new MutationObserver(() => {
        addImageResizeHandles(editorElement);
    });
    observer.observe(editorElement, { childList: true, subtree: true, attributes: false });
    editorElement._mutationObserver = observer;

    // Initial image resize
    addImageResizeHandles(editorElement);

    // ── Line Numbers ──
    function updateLineNumbers() {
        const area = editorElement.closest('.sgc-richtext-editor-area');
        const ln = area?.querySelector('.sgc-richtext-linenumbers');
        if (!ln) return;
        const text = editorElement.innerText || '';
        const lines = text.split('\n');
        const count = Math.max(lines.length, 1);
        const currentCount = ln.querySelectorAll('span').length;
        if (currentCount !== count) {
            ln.innerHTML = '';
            for (let i = 1; i <= count; i++) {
                const span = document.createElement('span');
                span.textContent = i;
                ln.appendChild(span);
            }
        }
    }
    const debouncedLineNumbers = debounce(updateLineNumbers, 150);
    editorElement.addEventListener('input', debouncedLineNumbers);
    editorElement.addEventListener('keyup', debouncedLineNumbers);
    editorElement.addEventListener('mouseup', debouncedLineNumbers);
    editorElement._lineNumbersHandler = debouncedLineNumbers;
    updateLineNumbers();

    // ── Dispose ──
    editorElement._dispose = function() {
        isDisposed = true;
        inst.isDisposed = true;
        dotnetRef = null;
        inst.dotnetRef = null;
        if (observer) observer.disconnect();
    };
}

export function disposeRichTextEditor(editorElement) {
    if (!editorElement) return;
    if (editorElement._dispose) editorElement._dispose();
    if (editorElement._selectionChangeHandler)
        document.removeEventListener('selectionchange', editorElement._selectionChangeHandler);
    if (editorElement._trackSelection) {
        editorElement.removeEventListener('keyup', editorElement._trackSelection);
        editorElement.removeEventListener('mouseup', editorElement._trackSelection);
        editorElement.removeEventListener('focus', editorElement._trackSelection);
    }
    if (editorElement._onKeyDown)
        editorElement.removeEventListener('keydown', editorElement._onKeyDown);
    if (editorElement._mutationObserver)
        editorElement._mutationObserver.disconnect();
    if (editorElement._lineNumbersHandler) {
        editorElement.removeEventListener('input', editorElement._lineNumbersHandler);
        editorElement.removeEventListener('keyup', editorElement._lineNumbersHandler);
        editorElement.removeEventListener('mouseup', editorElement._lineNumbersHandler);
    }

    // Remove floating toolbar
    const ft = editorElement.closest('.sgc-richtext')?.querySelector('.sgc-richtext-floatbar');
    if (ft) ft.remove();
    const sm = editorElement.closest('.sgc-richtext')?.querySelector('.sgc-richtext-slash-menu');
    if (sm) sm.remove();
    const fb = editorElement.closest('.sgc-richtext')?.querySelector('.sgc-richtext-findbar');
    if (fb) fb.remove();

    // Clean up find state
    delete window.__sg_findMatches;
    delete window.__sg_findIndex;

    editorElement._dotnetRef = null;
    savedRanges.delete(editorElement);
    selectionThrottle.delete(editorElement);
    editorInstances.delete(editorElement);
}

// ─── Exports ────────────────────────────────────────────────────────

export function execCommand(command, value = null) {
    document.execCommand(command, false, value);
}

export function execCommandOn(editorElement, command, value = null) {
    if (editorElement) { editorElement.focus(); restoreRange(editorElement); }
    document.execCommand(command, false, value);
    if (editorElement) rememberRange(editorElement);
}

export function queryCommandValue(command) {
    try { return document.queryCommandValue(command); } catch { return ''; }
}

export function queryActiveFormats() {
    const formats = [];
    const commands = ['bold', 'italic', 'underline', 'strikeThrough', 'subscript', 'superscript',
        'insertUnorderedList', 'insertOrderedList', 'justifyLeft', 'justifyCenter', 'justifyRight', 'justifyFull'];
    commands.forEach(cmd => { try { if (document.queryCommandState(cmd)) formats.push(cmd); } catch {} });
    return formats;
}

export function getHtmlContent(editorElement) { return editorElement?.innerHTML || ''; }
export function getTextContent(editorElement) { return editorElement?.innerText || ''; }
export function getSelectedText() { const s = window.getSelection(); return s ? s.toString() : ''; }

export function setHtmlContent(editorElement, html) {
    if (editorElement) { editorElement.innerHTML = html; addImageResizeHandles(editorElement); }
}

export function insertHtml(html) { document.execCommand('insertHTML', false, html); }
export function insertTableOn(editorElement, rows, cols) { insertTableCmd(editorElement, rows, cols); }

export function insertHtmlAt(editorElement, html) {
    if (!editorElement) return;
    editorElement.focus(); restoreRange(editorElement);
    document.execCommand('insertHTML', false, html);
    rememberRange(editorElement);
    addImageResizeHandles(editorElement);
}

export function focus(editorElement) { editorElement?.focus(); }
export function saveSelection(editorElement) { rememberRange(editorElement); }
export function restoreSelection(editorElement) { if (editorElement) { editorElement.focus(); restoreRange(editorElement); } }

export function setBlockFormat(editorElement, tag) {
    if (!editorElement) return;
    editorElement.focus(); restoreRange(editorElement);
    document.execCommand('formatBlock', false, tag);
    rememberRange(editorElement);
}

export function getWordCount(editorElement) {
    const t = editorElement?.innerText || '';
    if (!t.trim()) return 0;
    return t.trim().split(/\s+/).length;
}
export function getCharCount(editorElement) { return (editorElement?.innerText || '').length; }

export function getSelectedHtml(editorElement) {
    const sel = window.getSelection();
    if (!sel || sel.rangeCount === 0 || !isInsideEditor(editorElement, sel.anchorNode)) return '';
    const range = sel.getRangeAt(0);
    const div = document.createElement('div');
    div.appendChild(range.cloneContents());
    return div.innerHTML;
}

// ─── Emoji ──────────────────────────────────────────────────────────
export function getEmojiCategories() { return JSON.stringify(EMOJI_CATEGORIES); }

export function insertEmoji(editorElement, emoji) {
    if (!editorElement) return;
    editorElement.focus();
    restoreRange(editorElement);
    document.execCommand('insertText', false, emoji);
    rememberRange(editorElement);
}

// ─── Find & Replace ─────────────────────────────────────────────────
export function toggleFindReplace(editorElement) {
    if (!editorElement) return;
    const bar = createFindBar(editorElement);
    if (bar.style.display === 'none' || !bar.style.display) {
        bar.style.display = 'block';
        const input = bar.querySelector('#sgrt-find-input');
        if (input) { input.value = ''; input.focus(); }

        // Wire up events once
        if (!bar._wired) {
            bar._wired = true;
            bar.querySelector('#sgrt-find-input')?.addEventListener('input', function() {
                bar._findText(this.value);
            });
            bar.querySelector('#sgrt-find-next')?.addEventListener('click', () => bar._navigateFind(1));
            bar.querySelector('#sgrt-find-prev')?.addEventListener('click', () => bar._navigateFind(-1));
            bar.querySelector('#sgrt-replace-btn')?.addEventListener('click', () => {
                const r = bar.querySelector('#sgrt-replace-input');
                bar._replaceCurrent(r?.value || '');
            });
            bar.querySelector('#sgrt-replace-all-btn')?.addEventListener('click', () => {
                const r = bar.querySelector('#sgrt-replace-input');
                bar._replaceAll(r?.value || '');
            });
            bar.querySelector('#sgrt-find-close')?.addEventListener('click', () => { bar.style.display = 'none'; });
            // Enter in find field
            bar.querySelector('#sgrt-find-input')?.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') { e.preventDefault(); bar._navigateFind(1); }
            });
        }
    } else {
        bar.style.display = 'none';
    }
}

// ─── Export as Markdown ─────────────────────────────────────────────
export function copyAsMarkdown(editorElement) {
    const html = editorElement?.innerHTML || '';
    const md = htmlToMarkdown(html);
    navigator.clipboard.writeText(md).catch(() => {});
    return md;
}

export function getAsMarkdown(editorElement) {
    const html = editorElement?.innerHTML || '';
    return htmlToMarkdown(html);
}

// ─── Floating toolbar init (called from component) ──────────────────
export function initFloatingToolbar(editorElement) {
    if (!editorElement) return;
    createFloatingToolbar(editorElement);
}

// ─── Read image file ────────────────────────────────────────────────
export function readImageFile(input) {
    return new Promise((resolve, reject) => {
        if (!input || !input.files || input.files.length === 0) { resolve(null); return; }
        const file = input.files[0];
        if (!file.type.startsWith('image/')) { resolve(null); return; }
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

// ----- CommandBar -----

const commandBarInstances = new WeakMap();

export function initCommandBar(cmdBarElement, dotnetRef) {
    if (!cmdBarElement) return;
    let isDisposed = false;
    const resizeObserver = new ResizeObserver((entries) => {
        if (isDisposed || !dotnetRef) return;
        try {
            for (const entry of entries) {
                const width = entry.contentRect.width;
                const availableWidth = Math.round(width - 60);
                if (dotnetRef && !isDisposed)
                    dotnetRef.invokeMethodAsync('UpdateOverflow', Math.max(0, availableWidth)).catch(()=>{});
            }
        } catch {}
    });
    resizeObserver.observe(cmdBarElement);
    commandBarInstances.set(cmdBarElement, {
        resizeObserver, dotnetRef, isDisposed: false,
        dispose: function() { this.isDisposed = true; dotnetRef = null; }
    });
}

export function disposeCommandBar(cmdBarElement) {
    const instance = commandBarInstances.get(cmdBarElement);
    if (instance) {
        if (instance.dispose) instance.dispose();
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
    if (found) { sel.removeAllRanges(); sel.addRange(range); element.focus(); }
}

// ─── Find & Replace standalone exports (for Razor-based find bar) ──
export function findInEditor(editorElement, query) {
    if (!editorElement || !query) return;
    editorElement.querySelectorAll('.sgc-richtext-find-highlight').forEach(el => {
        const p = el.parentNode;
        p.replaceChild(document.createTextNode(el.textContent), el);
        p.normalize();
    });
    const walker = document.createTreeWalker(editorElement, NodeFilter.SHOW_TEXT);
    const matches = [];
    while (node = walker.nextNode()) {
        const text = node.textContent;
        const idx = text.toLowerCase().indexOf(query.toLowerCase());
        if (idx < 0) continue;
        matches.push({ node, idx, len: query.length, text });
    }
    window.__sg_findMatches = matches;
    window.__sg_findIndex = 0;
    matches.forEach(({ node, idx, len, text }) => {
        const after = text.substring(idx + len);
        const span = document.createElement('span');
        span.className = 'sgc-richtext-find-highlight';
        span.textContent = text.substring(idx, idx + len);
        const frag = document.createDocumentFragment();
        if (idx > 0) frag.appendChild(document.createTextNode(text.substring(0, idx)));
        frag.appendChild(span);
        if (after) frag.appendChild(document.createTextNode(after));
        node.parentNode.replaceChild(frag, node);
    });
    if (matches.length > 0) {
        const all = editorElement.querySelectorAll('.sgc-richtext-find-highlight');
        if (all[0]) all[0].classList.add('sgc-active');
    }
}

export function findNext(editorElement) {
    const matches = window.__sg_findMatches || [];
    if (!matches.length) return;
    const all = editorElement?.querySelectorAll('.sgc-richtext-find-highlight');
    if (all) all.forEach(el => el.classList.remove('sgc-active'));
    window.__sg_findIndex = (window.__sg_findIndex + 1) % matches.length;
    if (all && all[window.__sg_findIndex]) {
        all[window.__sg_findIndex].classList.add('sgc-active');
        all[window.__sg_findIndex].scrollIntoView({ block: 'nearest' });
    }
}

export function findPrev(editorElement) {
    const matches = window.__sg_findMatches || [];
    if (!matches.length) return;
    const all = editorElement?.querySelectorAll('.sgc-richtext-find-highlight');
    if (all) all.forEach(el => el.classList.remove('sgc-active'));
    window.__sg_findIndex = ((window.__sg_findIndex - 1) % matches.length + matches.length) % matches.length;
    if (all && all[window.__sg_findIndex]) {
        all[window.__sg_findIndex].classList.add('sgc-active');
        all[window.__sg_findIndex].scrollIntoView({ block: 'nearest' });
    }
}

export function replaceInEditor(editorElement, findText, replaceText) {
    const matches = window.__sg_findMatches || [];
    const idx = window.__sg_findIndex || 0;
    const match = matches[idx];
    if (!match) return;
    const range = document.createRange();
    range.setStart(match.node, match.idx);
    range.setEnd(match.node, match.idx + match.len);
    range.deleteContents();
    range.insertNode(document.createTextNode(replaceText));
    editorElement?.normalize();
    if (findText) findInEditor(editorElement, findText);
}

export function replaceAllInEditor(editorElement, findText, replaceText) {
    if (!editorElement || !findText) return;
    const html = editorElement.innerHTML;
    const regex = new RegExp(findText.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
    editorElement.innerHTML = html.replace(regex, replaceText);
}

// ─── Export as Markdown ──────────────────────────────────────────
export function exportAsMarkdown(editorElement) {
    const html = editorElement?.innerHTML || '';
    return htmlToMarkdown(html);
}

// ─── DOM Rect (used by SgProgress Clickable) ──────────────────
export function getBoundingRect(element) {
    const rect = element.getBoundingClientRect();
    return { left: rect.left, top: rect.top, width: rect.width, height: rect.height };
}
