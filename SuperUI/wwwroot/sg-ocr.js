// SgOcr - Tesseract.js OCR Integration Module for SuperUI Blazor
// Provides JS interop for SgOcr component.

const _workers  = new Map();   // instanceId -> Tesseract.Worker
const _loaded   = new Set();   // loaded script URLs

// ── Loader ────────────────────────────────────────────────────────────────────

function _loadScript(url) {
    if (!url || _loaded.has(url)) return Promise.resolve();
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${url}"]`)) {
            _loaded.add(url); resolve(); return;
        }
        const s = document.createElement('script');
        s.src = url;
        s.onload  = () => { _loaded.add(url); resolve(); };
        s.onerror = () => reject(new Error(`Failed to load: ${url}`));
        document.head.appendChild(s);
    });
}

async function _ensureTesseract(sources) {
    if (sources?.tesseractScript) await _loadScript(sources.tesseractScript);
    let T = window.Tesseract;
    let attempts = 0;
    while (!T && attempts < 50) {
        await new Promise(r => setTimeout(r, 100));
        T = window.Tesseract; attempts++;
    }
    if (!T) throw new Error('Tesseract.js not loaded');
    return T;
}

// ── Language helper ───────────────────────────────────────────────────────────

function _langCode(lang) {
    const map = {
        Eng: 'eng', Rus: 'rus', EngRus: 'eng+rus',
        Deu: 'deu', Fra: 'fra', Spa: 'spa',
        Chi_Sim: 'chi_sim', Jpn: 'jpn', Ara: 'ara',
    };
    return map[lang] ?? 'eng';
}

// ── Public API ────────────────────────────────────────────────────────────────

export async function initOcr(dotnetRef, instanceId, sources) {
    // Dispose any previous worker for this instance
    await disposeOcr(instanceId);

    const T = await _ensureTesseract(sources);

    _workers.set(instanceId, {
        dotnetRef,
        sources,
        T,
        worker: null,   // created lazily on first recognize call
        currentLang: null,
    });
}

export async function recognizeOcr(instanceId, imageDataUrl, lang, sources) {
    const inst = _workers.get(instanceId);
    if (!inst) throw new Error(`SgOcr instance ${instanceId} not found`);

    const langCode = _langCode(lang);
    const T = inst.T;

    // Re-create worker if language changed or first call
    if (!inst.worker || inst.currentLang !== langCode) {
        if (inst.worker) {
            try { await inst.worker.terminate(); } catch {}
        }

        const worker = await T.createWorker(langCode, 1, {
            workerPath: sources?.workerPath,
            corePath:   sources?.corePath,
            langPath:   sources?.langPath,
            logger: (m) => {
                if (!m || !inst.dotnetRef) return;
                const pct = typeof m.progress === 'number' ? Math.round(m.progress * 100) : -1;
                try {
                    inst.dotnetRef.invokeMethodAsync('OnProgressAsync', {
                        status:   m.status ?? '',
                        progress: pct,
                    });
                } catch {}
            },
        });

        inst.worker      = worker;
        inst.currentLang = langCode;
        _workers.set(instanceId, inst);
    }

    const t0 = Date.now();
    const { data } = await inst.worker.recognize(imageDataUrl);

    const words = (data.words ?? []).map(w => ({
        text:       w.text ?? '',
        confidence: w.confidence ?? 0,
        bbox:       { x0: w.bbox?.x0 ?? 0, y0: w.bbox?.y0 ?? 0, x1: w.bbox?.x1 ?? 0, y1: w.bbox?.y1 ?? 0 },
    }));

    const lines = (data.lines ?? []).map(l => ({
        text:       l.text ?? '',
        confidence: l.confidence ?? 0,
        bbox:       { x0: l.bbox?.x0 ?? 0, y0: l.bbox?.y0 ?? 0, x1: l.bbox?.x1 ?? 0, y1: l.bbox?.y1 ?? 0 },
    }));

    return {
        text:       data.text ?? '',
        confidence: data.confidence ?? 0,
        words,
        lines,
        language:   langCode,
        durationMs: Date.now() - t0,
    };
}

export async function disposeOcr(instanceId) {
    const inst = _workers.get(instanceId);
    if (!inst) return;
    if (inst.worker) {
        try { await inst.worker.terminate(); } catch {}
    }
    _workers.delete(instanceId);
}
