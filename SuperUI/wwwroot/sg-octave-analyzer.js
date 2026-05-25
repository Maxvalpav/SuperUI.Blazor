// sg-octave-analyzer.js — Octave Band RTA Analyzer for SuperUI Blazor
// Uses Web Audio API: AudioContext, AnalyserNode, getUserMedia
// Sends FFT data to C# via dotNetRef.invokeMethodAsync('OnFftDataAsync', ...)
// Also renders a canvas waterfall / bar spectrum in real time.

const _instances = new Map();

// ── Center frequencies for 10 octave bands ────────────────────────────────────
const CENTER_FREQS = [31.5, 63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000];
const FREQ_LABELS  = ['31.5', '63', '125', '250', '500', '1k', '2k', '4k', '8k', '16k'];

// ── Bar colors (gradient from low to high level) ──────────────────────────────
function _barColor(db) {
    if (db > -10) return '#ef4444';   // red — danger
    if (db > -20) return '#f97316';   // orange — warning
    if (db > -40) return '#22c55e';   // green — ok
    return '#3b82f6';                 // blue — low
}

// ── Canvas renderer ───────────────────────────────────────────────────────────

function _renderCanvas(inst) {
    const { canvas, bands, showPeakHold } = inst;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const W   = canvas.width;
    const H   = canvas.height;

    // Background
    ctx.fillStyle = '#0f172a';
    ctx.fillRect(0, 0, W, H);

    // Grid lines
    const dbMin = -96, dbMax = 0;
    const gridLevels = [-80, -60, -40, -20, -10, 0];
    ctx.strokeStyle = 'rgba(255,255,255,0.07)';
    ctx.lineWidth   = 1;
    ctx.font        = '10px monospace';
    ctx.fillStyle   = 'rgba(255,255,255,0.3)';
    ctx.textAlign   = 'right';

    for (const db of gridLevels) {
        const y = H - ((db - dbMin) / (dbMax - dbMin)) * H;
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(W, y);
        ctx.stroke();
        ctx.fillText(`${db}`, 36, y - 2);
    }

    // Bars
    const n       = bands.length;
    const barW    = Math.floor((W - 40) / n) - 4;
    const offsetX = 40;

    for (let i = 0; i < n; i++) {
        const band = bands[i];
        const db   = Math.max(dbMin, Math.min(dbMax, band.weightedDb));
        const barH = ((db - dbMin) / (dbMax - dbMin)) * H;
        const x    = offsetX + i * ((W - offsetX) / n) + 2;
        const y    = H - barH;

        // Bar gradient
        const grad = ctx.createLinearGradient(x, y, x, H);
        const col  = _barColor(db);
        grad.addColorStop(0, col);
        grad.addColorStop(1, col + '44');
        ctx.fillStyle = grad;
        ctx.fillRect(x, y, barW, barH);

        // Peak hold line
        if (showPeakHold && band.peakDb > dbMin) {
            const peakDb = Math.max(dbMin, Math.min(dbMax, band.peakDb));
            const peakY  = H - ((peakDb - dbMin) / (dbMax - dbMin)) * H;
            ctx.fillStyle = '#fff';
            ctx.fillRect(x, peakY - 2, barW, 2);
        }

        // Frequency label
        ctx.fillStyle   = 'rgba(255,255,255,0.5)';
        ctx.font        = '9px monospace';
        ctx.textAlign   = 'center';
        ctx.fillText(FREQ_LABELS[i], x + barW / 2, H - 4);
    }
}

// ── Public API ────────────────────────────────────────────────────────────────

export function init(dotNetRef, canvasRef, instanceId, opts) {
    dispose(instanceId);

    const bands = CENTER_FREQS.map((f, i) => ({
        centerFreq: f,
        label:      FREQ_LABELS[i],
        rawDb:      -96,
        weightedDb: -96,
        peakDb:     -96,
    }));

    _instances.set(instanceId, {
        dotNetRef,
        canvas:          canvasRef,
        bands,
        showPeakHold:    opts?.showPeakHold !== false,
        updateIntervalMs:opts?.updateIntervalMs ?? 100,
        audioCtx:        null,
        analyser:        null,
        stream:          null,
        rafId:           null,
        timerId:         null,
    });

    // Initial empty render
    _renderCanvas(_instances.get(instanceId));
}

export async function start(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) throw new Error('Instance not found: ' + instanceId);

    // Stop previous if any
    _stopAudio(inst);

    // Request microphone
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
    const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    const source   = audioCtx.createMediaStreamSource(stream);
    const analyser = audioCtx.createAnalyser();
    analyser.fftSize             = 8192;
    analyser.smoothingTimeConstant = 0.75;
    source.connect(analyser);

    inst.audioCtx = audioCtx;
    inst.analyser = analyser;
    inst.stream   = stream;

    const fftData = new Float32Array(analyser.frequencyBinCount);

    // Canvas animation loop
    function renderLoop() {
        if (!inst.analyser) return;
        analyser.getFloatFrequencyData(fftData);

        // Convert dBFS to linear magnitude for C# processing
        const linear = new Float32Array(fftData.length);
        for (let i = 0; i < fftData.length; i++) {
            // fftData values are in dBFS (negative), convert to 0..1 magnitude
            linear[i] = Math.pow(10, fftData[i] / 20);
        }

        // Update local band levels for canvas rendering
        const sampleRate = audioCtx.sampleRate;
        const fftSize    = analyser.fftSize;
        const freqRes    = sampleRate / fftSize;

        for (let i = 0; i < CENTER_FREQS.length; i++) {
            const fc    = CENTER_FREQS[i];
            const fLow  = fc / Math.SQRT2;
            const fHigh = fc * Math.SQRT2;
            const bLow  = Math.max(1, Math.floor(fLow  / freqRes));
            const bHigh = Math.min(linear.length - 1, Math.ceil(fHigh / freqRes));

            let energy = 0, count = 0;
            for (let b = bLow; b <= bHigh; b++) {
                energy += linear[b] * linear[b];
                count++;
            }
            const db = count > 0 ? 10 * Math.log10(energy / count + 1e-12) : -96;
            inst.bands[i].rawDb      = Math.max(-96, Math.min(0, db));
            inst.bands[i].weightedDb = inst.bands[i].rawDb; // weighting applied in C#
            if (inst.bands[i].weightedDb > inst.bands[i].peakDb)
                inst.bands[i].peakDb = inst.bands[i].weightedDb;
        }

        _renderCanvas(inst);
        inst.rafId = requestAnimationFrame(renderLoop);
    }
    inst.rafId = requestAnimationFrame(renderLoop);

    // Send FFT data to C# at updateIntervalMs
    inst.timerId = setInterval(() => {
        if (!inst.analyser || !inst.dotNetRef) return;
        analyser.getFloatFrequencyData(fftData);
        const linear = new Float32Array(fftData.length);
        for (let i = 0; i < fftData.length; i++) {
            linear[i] = Math.pow(10, fftData[i] / 20);
        }
        try {
            inst.dotNetRef.invokeMethodAsync('OnFftDataAsync', Array.from(linear), audioCtx.sampleRate);
        } catch {}
    }, inst.updateIntervalMs);
}

export function stop(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    _stopAudio(inst);
}

export function resetPeaks(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    inst.bands.forEach(b => b.peakDb = -96);
    _renderCanvas(inst);
}

export function dispose(instanceId) {
    const inst = _instances.get(instanceId);
    if (!inst) return;
    _stopAudio(inst);
    _instances.delete(instanceId);
}

// ── Internal ──────────────────────────────────────────────────────────────────

function _stopAudio(inst) {
    if (inst.timerId) { clearInterval(inst.timerId); inst.timerId = null; }
    if (inst.rafId)   { cancelAnimationFrame(inst.rafId); inst.rafId = null; }
    if (inst.stream)  { inst.stream.getTracks().forEach(t => t.stop()); inst.stream = null; }
    if (inst.audioCtx){ try { inst.audioCtx.close(); } catch {} inst.audioCtx = null; }
    inst.analyser = null;
}
