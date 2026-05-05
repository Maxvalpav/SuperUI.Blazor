// SgMermaid - Mermaid.js Integration Module for SuperUI Blazor

const _instances = new Map();
const _loaded    = new Set();
let   _mermaidInitialized = false;

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadScript(url) {
    if (!url || _loaded.has(url)) return Promise.resolve();
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${url}"]`)) { _loaded.add(url); resolve(); return; }
        const s = document.createElement('script');
        s.src = url;
        s.onload  = () => { _loaded.add(url); resolve(); };
        s.onerror = () => reject(new Error(`Failed to load: ${url}`));
        document.head.appendChild(s);
    });
}

async function _ensureMermaid(sources) {
    if (sources?.mermaidScript) await _loadScript(sources.mermaidScript);
    let m = window.mermaid;
    let n = 0;
    while (!m && n++ < 80) { await new Promise(r => setTimeout(r, 100)); m = window.mermaid; }
    if (!m) throw new Error('Mermaid.js not loaded');
    return m;
}

// ── CSS variable helper ───────────────────────────────────────────────────────

function _cssVar(name, fallback) {
    try { const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim(); return v || fallback; }
    catch { return fallback; }
}

// ── Theme variables ───────────────────────────────────────────────────────────

function _buildThemeVars(opts) {
    const bg     = _cssVar('--sui-card-bg',        '#ffffff');
    const text   = _cssVar('--sui-text-primary',   '#1e293b');
    const border = _cssVar('--sui-border',         '#e2e8f0');
    const accent = _cssVar('--sui-accent',         '#006fee');
    const muted  = _cssVar('--sui-text-secondary', '#64748b');

    if (opts.theme !== 'Base') return {};

    return {
        primaryColor:       accent,
        primaryTextColor:   '#ffffff',
        primaryBorderColor: accent,
        lineColor:          border,
        secondaryColor:     _cssVar('--sui-bg-secondary', '#f8fafc'),
        tertiaryColor:      _cssVar('--sui-bg-tertiary',  '#f1f5f9'),
        background:         bg,
        mainBkg:            bg,
        nodeBorder:         border,
        clusterBkg:         _cssVar('--sui-bg-secondary', '#f8fafc'),
        titleColor:         text,
        edgeLabelBackground:bg,
        attributeBackgroundColorEven: _cssVar('--sui-bg-secondary', '#f8fafc'),
        attributeBackgroundColorOdd:  bg,
        fontFamily:         _cssVar('--sui-font-family', 'system-ui, sans-serif'),
        fontSize:           `${opts.fontSize ?? 14}px`,
    };
}

// ── Render ────────────────────────────────────────────────────────────────────

async function _render(mermaid, instanceId, containerRef, definition, opts, dotnetRef) {
    if (!definition || !definition.trim()) {
        containerRef.innerHTML = '<div class="sg-mermaid-empty">Нет определения диаграммы</div>';
        return;
    }

    const themeMap = { Default: 'default', Dark: 'dark', Forest: 'forest', Neutral: 'neutral', Base: 'base' };
    const theme    = themeMap[opts.theme] ?? 'default';

    const config = {
        startOnLoad:   false,
        theme,
        themeVariables: _buildThemeVars(opts),
        securityLevel:  opts.securityLevel ?? 'strict',
        fontFamily:     _cssVar('--sui-font-family', 'system-ui, sans-serif'),
        fontSize:       opts.fontSize ?? 14,
        flowchart:      { curve: opts.flowchartCurve ?? 'basis', htmlLabels: opts.securityLevel !== 'strict' },
        sequence:       { mirrorActors: opts.sequenceMirrorActors ?? false },
        maxTextSize:    opts.maxTextSize ?? 50000,
    };

    mermaid.initialize(config);

    const id  = `sg-mm-${instanceId}`;
    let   svg = '';

    try {
        const result = await mermaid.render(id, definition.trim());
        svg = result.svg ?? result;
    } catch (err) {
        containerRef.innerHTML = `<div class="sg-mermaid-error"><b>Ошибка синтаксиса:</b><br/><code>${String(err).replace(/</g,'&lt;')}</code></div>`;
        return;
    }

    containerRef.innerHTML = svg;

    // Make SVG responsive
    const svgEl = containerRef.querySelector('svg');
    if (svgEl) {
        svgEl.style.width  = '100%';
        svgEl.style.height = 'auto';
        svgEl.style.maxWidth = '100%';
        svgEl.removeAttribute('width');
        svgEl.removeAttribute('height');
    }

    // Wire up node click callbacks
    if (dotnetRef) {
        containerRef.querySelectorAll('[id]').forEach(el => {
            const nodeId = el.getAttribute('id');
            if (!nodeId || nodeId.startsWith('sg-mm-')) return;
            el.style.cursor = 'pointer';
            el.addEventListener('click', () => {
                try { dotnetRef.invokeMethodAsync('OnNodeClickedAsync', { nodeId: String(nodeId) }); } catch {}
            });
        });
    }
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function initMermaid(dotnetRef, containerRef, instanceId, definition, opts, sources) {
    await disposeMermaid(instanceId);

    const mermaid = await _ensureMermaid(sources);

    await _render(mermaid, instanceId, containerRef, definition, opts ?? {}, dotnetRef);

    // Resize observer — re-render on container resize for responsive SVG
    let ro = null;
    if (typeof ResizeObserver !== 'undefined') {
        let raf = 0;
        ro = new ResizeObserver(() => {
            cancelAnimationFrame(raf);
            raf = requestAnimationFrame(() => {
                const inst = _instances.get(instanceId);
                if (inst) _render(mermaid, instanceId, containerRef, inst.definition, inst.opts, inst.dotnetRef);
            });
        });
        ro.observe(containerRef);
    }

    _instances.set(instanceId, { mermaid, containerRef, dotnetRef, definition, opts, ro });
}

export async function updateMermaid(instanceId, definition, opts) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.definition = definition;
    inst.opts       = opts;
    await _render(inst.mermaid, instanceId, inst.containerRef, definition, opts ?? {}, inst.dotnetRef);
}

export function exportSvg(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const svgEl = inst.containerRef.querySelector('svg');
    if (!svgEl) return;
    const serializer = new XMLSerializer();
    const svgStr = serializer.serializeToString(svgEl);
    const blob = new Blob([svgStr], { type: 'image/svg+xml' });
    const url  = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = `diagram-${Date.now()}.svg`;
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

export function exportPng(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    const svgEl = inst.containerRef.querySelector('svg');
    if (!svgEl) return;
    const w = svgEl.getBoundingClientRect().width  || 800;
    const h = svgEl.getBoundingClientRect().height || 600;
    const serializer = new XMLSerializer();
    const svgStr = serializer.serializeToString(svgEl);
    const img = new Image();
    img.onload = () => {
        const canvas = document.createElement('canvas');
        canvas.width = w * 2; canvas.height = h * 2;
        const ctx = canvas.getContext('2d');
        ctx.scale(2, 2);
        ctx.fillStyle = '#ffffff'; ctx.fillRect(0, 0, w, h);
        ctx.drawImage(img, 0, 0, w, h);
        const a = document.createElement('a');
        a.href = canvas.toDataURL('image/png'); a.download = `diagram-${Date.now()}.png`;
        document.body.appendChild(a); a.click(); document.body.removeChild(a);
    };
    img.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgStr)));
}

export async function disposeMermaid(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    try { inst.ro?.disconnect(); } catch {}
    try { if (inst.containerRef) inst.containerRef.innerHTML = ''; } catch {}
    _instances.delete(instanceId);
}
