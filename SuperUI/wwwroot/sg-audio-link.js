/**
 * sg-audio-link.js — SuperUI Acoustic Data Link
 * ─────────────────────────────────────────────
 * FSK (Frequency Shift Keying) over Web Audio API.
 *
 * Protocol:
 *   • 16 carrier frequencies: 18000–21750 Hz, step 250 Hz
 *     → 4 bits per symbol (nibble)
 *   • Preamble: 3 sync tones (17500, 17750, 17500 Hz) + 1 start tone (17250 Hz)
 *   • Frame: [PREAMBLE][LEN_HI][LEN_LO][DATA...][CRC8]
 *   • Symbol duration: configurable (default 80 ms)
 *   • Receiver: FFT 8192 → peak detection → nibble decode → CRC verify
 *
 * Browser APIs used:
 *   AudioContext, OscillatorNode, GainNode, AnalyserNode,
 *   MediaDevices.getUserMedia, Float32Array FFT, OfflineAudioContext,
 *   AudioWorkletNode (optional), Canvas 2D (spectrum visualizer)
 */

'use strict';

// ── Constants ──────────────────────────────────────────────────────────────────

const SAMPLE_RATE      = 48000;
const FFT_SIZE         = 8192;
const SYMBOL_MS        = 80;          // ms per symbol
const GUARD_MS         = 10;          // silence between symbols
const BASE_FREQ        = 18000;       // Hz — lowest data carrier
const FREQ_STEP        = 250;         // Hz between carriers
const NUM_CARRIERS     = 16;          // 4-bit nibble per symbol
const SYNC_A           = 17500;       // preamble tone A
const SYNC_B           = 17750;       // preamble tone B
const START_TONE       = 17250;       // frame-start marker
const PREAMBLE_REPS    = 3;           // how many sync pairs to send
const DETECT_THRESHOLD = 0.15;        // relative FFT peak threshold (0–1)
const LOCK_THRESHOLD   = 0.25;        // preamble lock threshold

// ── Instances map ──────────────────────────────────────────────────────────────

/** @type {Map<string, AudioLinkInstance>} */
const instances = new Map();

// ── CRC-8 (poly 0x07, Dallas/Maxim) ───────────────────────────────────────────

function crc8(bytes) {
    let crc = 0x00;
    for (const b of bytes) {
        crc ^= b;
        for (let i = 0; i < 8; i++) {
            crc = (crc & 0x80) ? ((crc << 1) ^ 0x07) & 0xFF : (crc << 1) & 0xFF;
        }
    }
    return crc;
}

// ── Frequency ↔ nibble mapping ─────────────────────────────────────────────────

/** nibble (0–15) → carrier frequency Hz */
function nibbleToFreq(n) { return BASE_FREQ + n * FREQ_STEP; }

/** Hz → nearest nibble index, or -1 if out of range */
function freqToNibble(hz) {
    const idx = Math.round((hz - BASE_FREQ) / FREQ_STEP);
    return (idx >= 0 && idx < NUM_CARRIERS) ? idx : -1;
}

// ── FFT peak detection ─────────────────────────────────────────────────────────

/**
 * Find the dominant frequency in a Float32Array of FFT magnitude data.
 * Returns { freq, magnitude } or null.
 */
function detectPeak(fftData, sampleRate, fftSize, minHz, maxHz) {
    const binHz = sampleRate / fftSize;
    const minBin = Math.floor(minHz / binHz);
    const maxBin = Math.ceil(maxHz / binHz);

    let maxMag = -Infinity;
    let maxBinIdx = -1;

    for (let i = minBin; i <= maxBin && i < fftData.length; i++) {
        if (fftData[i] > maxMag) {
            maxMag = fftData[i];
            maxBinIdx = i;
        }
    }

    if (maxBinIdx < 0) return null;

    // Parabolic interpolation for sub-bin accuracy
    const prev = maxBinIdx > 0 ? fftData[maxBinIdx - 1] : maxMag;
    const next = maxBinIdx < fftData.length - 1 ? fftData[maxBinIdx + 1] : maxMag;
    const denom = prev - 2 * maxMag + next;
    const offset = denom !== 0 ? 0.5 * (prev - next) / denom : 0;
    const refinedBin = maxBinIdx + offset;

    return { freq: refinedBin * binHz, magnitude: maxMag };
}

// ── Encode string → nibble array ───────────────────────────────────────────────

function encodePayload(text) {
    const bytes = new TextEncoder().encode(text);
    const frame = new Uint8Array(bytes.length + 3);
    frame[0] = (bytes.length >> 8) & 0xFF;  // LEN_HI
    frame[1] = bytes.length & 0xFF;          // LEN_LO
    for (let i = 0; i < bytes.length; i++) frame[2 + i] = bytes[i];
    frame[frame.length - 1] = crc8(frame.subarray(0, frame.length - 1));

    // Each byte → 2 nibbles (high nibble first)
    const nibbles = [];
    for (const b of frame) {
        nibbles.push((b >> 4) & 0x0F);
        nibbles.push(b & 0x0F);
    }
    return nibbles;
}

// ── AudioLinkInstance ──────────────────────────────────────────────────────────

class AudioLinkInstance {
    constructor(dotNetRef, instanceId, options) {
        this.dotNetRef  = dotNetRef;
        this.id         = instanceId;
        this.opts       = Object.assign({
            symbolMs:   SYMBOL_MS,
            guardMs:    GUARD_MS,
            volume:     0.5,
            canvasId:   null,
        }, options);

        this.audioCtx   = null;
        this.analyser   = null;
        this.micStream  = null;
        this.micSource  = null;
        this.rxActive   = false;
        this.txActive   = false;

        // RX state machine
        this._rxState   = 'idle';   // idle | preamble | data
        this._rxBuf     = [];       // received nibbles
        this._rxExpLen  = 0;        // expected nibble count
        this._syncHist  = [];       // last N detected freqs for preamble lock
        this._rafId     = null;

        // Spectrum canvas
        this._canvas    = null;
        this._canvasCtx = null;
        this._animId    = null;
    }

    // ── Audio context ──────────────────────────────────────────────────────────

    async ensureContext() {
        if (this.audioCtx && this.audioCtx.state !== 'closed') {
            if (this.audioCtx.state === 'suspended') await this.audioCtx.resume();
            return;
        }
        this.audioCtx = new AudioContext({ sampleRate: SAMPLE_RATE });
        this.analyser = this.audioCtx.createAnalyser();
        this.analyser.fftSize = FFT_SIZE;
        this.analyser.smoothingTimeConstant = 0.1;
        this.analyser.connect(this.audioCtx.destination);  // needed for some browsers
    }

    // ── TX: transmit ───────────────────────────────────────────────────────────

    async transmit(text) {
        if (this.txActive) return;
        this.txActive = true;

        try {
            await this.ensureContext();
            const ctx = this.audioCtx;
            const nibbles = encodePayload(text);

            // Build tone sequence: preamble + data nibbles
            const tones = [];

            // Preamble: SYNC_A, SYNC_B alternating × PREAMBLE_REPS
            for (let i = 0; i < PREAMBLE_REPS; i++) {
                tones.push({ freq: SYNC_A, dur: this.opts.symbolMs });
                tones.push({ freq: SYNC_B, dur: this.opts.symbolMs });
            }
            // Start marker
            tones.push({ freq: START_TONE, dur: this.opts.symbolMs });

            // Data nibbles
            for (const n of nibbles) {
                tones.push({ freq: nibbleToFreq(n), dur: this.opts.symbolMs });
            }

            // Render to OfflineAudioContext for gapless playback
            const totalSamples = tones.reduce((acc, t) =>
                acc + Math.ceil((t.dur + this.opts.guardMs) / 1000 * SAMPLE_RATE), 0);

            const offline = new OfflineAudioContext(1, totalSamples, SAMPLE_RATE);
            let offset = 0;

            for (const tone of tones) {
                const symSamples  = Math.ceil(tone.dur / 1000 * SAMPLE_RATE);
                const guardSamples = Math.ceil(this.opts.guardMs / 1000 * SAMPLE_RATE);
                const startTime   = offset / SAMPLE_RATE;
                const endTime     = startTime + symSamples / SAMPLE_RATE;

                const osc  = offline.createOscillator();
                const gain = offline.createGain();

                osc.type      = 'sine';
                osc.frequency.setValueAtTime(tone.freq, startTime);

                // Smooth envelope: 5ms attack / 5ms release
                gain.gain.setValueAtTime(0, startTime);
                gain.gain.linearRampToValueAtTime(this.opts.volume, startTime + 0.005);
                gain.gain.setValueAtTime(this.opts.volume, endTime - 0.005);
                gain.gain.linearRampToValueAtTime(0, endTime);

                osc.connect(gain);
                gain.connect(offline.destination);
                osc.start(startTime);
                osc.stop(endTime);

                offset += symSamples + guardSamples;
            }

            const rendered = await offline.startRendering();

            // Play rendered buffer
            const src = ctx.createBufferSource();
            src.buffer = rendered;

            // Compressor to protect speakers
            const comp = ctx.createDynamicsCompressor();
            comp.threshold.value = -6;
            comp.ratio.value = 4;
            src.connect(comp);
            comp.connect(ctx.destination);

            await new Promise((resolve) => {
                src.onended = resolve;
                src.start();
            });

            await this.dotNetRef.invokeMethodAsync('OnTxCompleteAsync', text, nibbles.length);
        } catch (err) {
            console.error('[SgAudioLink] TX error:', err);
            await this.dotNetRef.invokeMethodAsync('OnErrorAsync', err.message, 'tx');
        } finally {
            this.txActive = false;
        }
    }

    // ── RX: start listening ────────────────────────────────────────────────────

    async startReceive() {
        if (this.rxActive) return;

        try {
            await this.ensureContext();

            this.micStream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: false,
                    noiseSuppression: false,
                    autoGainControl:  false,
                    sampleRate:       SAMPLE_RATE,
                    channelCount:     1,
                }
            });

            this.micSource = this.audioCtx.createMediaStreamSource(this.micStream);

            // Highpass filter to cut below 16 kHz (remove voice/noise)
            const hpf = this.audioCtx.createBiquadFilter();
            hpf.type            = 'highpass';
            hpf.frequency.value = 16000;
            hpf.Q.value         = 0.7;

            this.micSource.connect(hpf);
            hpf.connect(this.analyser);

            this.rxActive  = true;
            this._rxState  = 'idle';
            this._rxBuf    = [];
            this._syncHist = [];

            this._startRxLoop();
            this._startSpectrumDraw();

            await this.dotNetRef.invokeMethodAsync('OnRxStartedAsync');
        } catch (err) {
            console.error('[SgAudioLink] RX start error:', err);
            await this.dotNetRef.invokeMethodAsync('OnErrorAsync', err.message, 'rx_start');
        }
    }

    // ── RX: stop ───────────────────────────────────────────────────────────────

    async stopReceive() {
        this.rxActive = false;
        if (this._rafId) { cancelAnimationFrame(this._rafId); this._rafId = null; }
        if (this._animId) { cancelAnimationFrame(this._animId); this._animId = null; }

        if (this.micSource) { try { this.micSource.disconnect(); } catch {} this.micSource = null; }
        if (this.micStream) {
            this.micStream.getTracks().forEach(t => t.stop());
            this.micStream = null;
        }

        await this.dotNetRef.invokeMethodAsync('OnRxStoppedAsync');
    }

    // ── RX loop (rAF-based, ~60 fps) ──────────────────────────────────────────

    _startRxLoop() {
        const fftData = new Float32Array(this.analyser.frequencyBinCount);
        const symMs   = this.opts.symbolMs + this.opts.guardMs;
        let lastSymTime = 0;

        const loop = (ts) => {
            if (!this.rxActive) return;
            this._rafId = requestAnimationFrame(loop);

            // Throttle to ~1 sample per symbol period
            if (ts - lastSymTime < symMs * 0.6) return;
            lastSymTime = ts;

            this.analyser.getFloatFrequencyData(fftData);

            // Convert dB to linear magnitude (0–1)
            const linData = new Float32Array(fftData.length);
            let maxLin = 0;
            for (let i = 0; i < fftData.length; i++) {
                linData[i] = Math.pow(10, fftData[i] / 20);
                if (linData[i] > maxLin) maxLin = linData[i];
            }
            if (maxLin < 1e-6) return; // silence

            // Normalize
            for (let i = 0; i < linData.length; i++) linData[i] /= maxLin;

            // Detect peak in full ultrasonic range (17000–22000 Hz)
            const peak = detectPeak(linData, SAMPLE_RATE, FFT_SIZE * 2, 17000, 22000);
            if (!peak || peak.magnitude < DETECT_THRESHOLD) return;

            this._processTone(peak.freq, peak.magnitude);
        };

        this._rafId = requestAnimationFrame(loop);
    }

    // ── RX state machine ───────────────────────────────────────────────────────

    _processTone(freq, mag) {
        switch (this._rxState) {

            case 'idle': {
                // Look for SYNC_A / SYNC_B alternation
                this._syncHist.push(freq);
                if (this._syncHist.length > PREAMBLE_REPS * 2 + 2) this._syncHist.shift();

                if (this._checkPreamble()) {
                    this._rxState  = 'start';
                    this._syncHist = [];
                    this.dotNetRef.invokeMethodAsync('OnPreambleDetectedAsync');
                }
                break;
            }

            case 'start': {
                // Expect START_TONE
                if (Math.abs(freq - START_TONE) < FREQ_STEP * 0.6) {
                    this._rxState = 'data';
                    this._rxBuf   = [];
                    this._rxExpLen = 0;
                } else {
                    // False preamble — back to idle
                    this._rxState = 'idle';
                }
                break;
            }

            case 'data': {
                const nibble = freqToNibble(freq);
                if (nibble < 0) {
                    // Out-of-band tone — abort frame
                    this._rxState = 'idle';
                    this._rxBuf   = [];
                    return;
                }

                this._rxBuf.push(nibble);

                // After 4 nibbles (2 bytes) we know the length
                if (this._rxBuf.length === 4 && this._rxExpLen === 0) {
                    const lenHi = (this._rxBuf[0] << 4) | this._rxBuf[1];
                    const lenLo = (this._rxBuf[2] << 4) | this._rxBuf[3];
                    const dataLen = (lenHi << 8) | lenLo;

                    if (dataLen === 0 || dataLen > 4096) {
                        // Sanity check failed
                        this._rxState = 'idle';
                        this._rxBuf   = [];
                        return;
                    }
                    // Total nibbles = 4 (len) + dataLen*2 (data) + 2 (crc)
                    this._rxExpLen = 4 + dataLen * 2 + 2;
                }

                if (this._rxExpLen > 0 && this._rxBuf.length >= this._rxExpLen) {
                    this._decodeFrame();
                    this._rxState  = 'idle';
                    this._rxBuf    = [];
                    this._rxExpLen = 0;
                }
                break;
            }
        }
    }

    _checkPreamble() {
        const h = this._syncHist;
        if (h.length < PREAMBLE_REPS * 2) return false;

        let matches = 0;
        for (let i = h.length - PREAMBLE_REPS * 2; i < h.length; i++) {
            const expected = (i % 2 === 0) ? SYNC_A : SYNC_B;
            if (Math.abs(h[i] - expected) < FREQ_STEP * 0.6) matches++;
        }
        return matches >= PREAMBLE_REPS * 2 - 1; // allow 1 miss
    }

    _decodeFrame() {
        // Nibbles → bytes
        const bytes = [];
        for (let i = 0; i < this._rxBuf.length; i += 2) {
            bytes.push((this._rxBuf[i] << 4) | this._rxBuf[i + 1]);
        }

        const payload = new Uint8Array(bytes);
        const dataLen = (payload[0] << 8) | payload[1];
        const data    = payload.subarray(2, 2 + dataLen);
        const rxCrc   = payload[2 + dataLen];
        const calcCrc = crc8(payload.subarray(0, 2 + dataLen));

        if (rxCrc !== calcCrc) {
            this.dotNetRef.invokeMethodAsync('OnRxErrorAsync',
                `CRC mismatch: got 0x${rxCrc.toString(16)}, expected 0x${calcCrc.toString(16)}`);
            return;
        }

        const text = new TextDecoder().decode(data);
        this.dotNetRef.invokeMethodAsync('OnDataReceivedAsync', text, data.length, rxCrc);
    }

    // ── Spectrum visualizer ────────────────────────────────────────────────────

    attachCanvas(canvasId) {
        this._canvas    = document.getElementById(canvasId);
        if (!this._canvas) return;
        this._canvasCtx = this._canvas.getContext('2d');
        if (this.rxActive || this.txActive) this._startSpectrumDraw();
    }

    _startSpectrumDraw() {
        if (!this._canvas || !this._canvasCtx || !this.analyser) return;
        if (this._animId) cancelAnimationFrame(this._animId);

        const fftData = new Float32Array(this.analyser.frequencyBinCount);
        const ctx     = this._canvasCtx;
        const canvas  = this._canvas;

        const draw = () => {
            this._animId = requestAnimationFrame(draw);
            this.analyser.getFloatFrequencyData(fftData);

            const W = canvas.width;
            const H = canvas.height;
            ctx.clearRect(0, 0, W, H);

            // Background
            ctx.fillStyle = '#0f172a';
            ctx.fillRect(0, 0, W, H);

            // Grid lines at carrier frequencies
            const binHz = SAMPLE_RATE / (FFT_SIZE * 2);
            ctx.strokeStyle = 'rgba(59,130,246,0.15)';
            ctx.lineWidth = 1;
            for (let n = 0; n < NUM_CARRIERS; n++) {
                const f = nibbleToFreq(n);
                const x = ((f - 16000) / (23000 - 16000)) * W;
                ctx.beginPath();
                ctx.moveTo(x, 0);
                ctx.lineTo(x, H);
                ctx.stroke();
            }

            // Spectrum bars
            const minBin = Math.floor(16000 / binHz);
            const maxBin = Math.ceil(23000 / binHz);
            const barW   = W / (maxBin - minBin);

            for (let i = minBin; i < maxBin && i < fftData.length; i++) {
                const db  = fftData[i];
                const norm = Math.max(0, (db + 100) / 60); // -100dB...-40dB → 0...1
                const x   = (i - minBin) * barW;
                const h   = norm * H;

                // Color: blue → cyan → green based on intensity
                const r = Math.round(norm * 59);
                const g = Math.round(norm * 200);
                const b = Math.round(59 + norm * 197);
                ctx.fillStyle = `rgb(${r},${g},${b})`;
                ctx.fillRect(x, H - h, barW - 0.5, h);
            }

            // Carrier frequency labels
            ctx.fillStyle = 'rgba(148,163,184,0.7)';
            ctx.font = '9px monospace';
            ctx.textAlign = 'center';
            for (let n = 0; n < NUM_CARRIERS; n += 4) {
                const f = nibbleToFreq(n);
                const x = ((f - 16000) / (23000 - 16000)) * W;
                ctx.fillText(`${(f / 1000).toFixed(1)}k`, x, H - 2);
            }

            // RX state indicator
            const stateColor = {
                idle:     '#64748b',
                start:    '#f59e0b',
                preamble: '#3b82f6',
                data:     '#10b981',
            }[this._rxState] || '#64748b';

            ctx.fillStyle = stateColor;
            ctx.beginPath();
            ctx.arc(W - 10, 10, 5, 0, Math.PI * 2);
            ctx.fill();
        };

        draw();
    }

    stopSpectrumDraw() {
        if (this._animId) { cancelAnimationFrame(this._animId); this._animId = null; }
    }

    // ── Dispose ────────────────────────────────────────────────────────────────

    async dispose() {
        this.rxActive = false;
        this.txActive = false;
        if (this._rafId)  { cancelAnimationFrame(this._rafId);  this._rafId  = null; }
        if (this._animId) { cancelAnimationFrame(this._animId); this._animId = null; }

        if (this.micSource) { try { this.micSource.disconnect(); } catch {} }
        if (this.micStream) { this.micStream.getTracks().forEach(t => t.stop()); }

        if (this.audioCtx && this.audioCtx.state !== 'closed') {
            try { await this.audioCtx.close(); } catch {}
        }

        this.audioCtx  = null;
        this.analyser  = null;
        this.micSource = null;
        this.micStream = null;
    }
}

// ── Exported API ───────────────────────────────────────────────────────────────

export function init(dotNetRef, instanceId, options) {
    if (instances.has(instanceId)) return;
    instances.set(instanceId, new AudioLinkInstance(dotNetRef, instanceId, options || {}));
}

export function attachCanvas(instanceId, canvasId) {
    instances.get(instanceId)?.attachCanvas(canvasId);
}

export async function transmit(instanceId, text) {
    const inst = instances.get(instanceId);
    if (!inst) return;
    await inst.transmit(text);
}

export async function startReceive(instanceId) {
    const inst = instances.get(instanceId);
    if (!inst) return;
    await inst.startReceive();
}

export async function stopReceive(instanceId) {
    const inst = instances.get(instanceId);
    if (!inst) return;
    await inst.stopReceive();
}

export function getProtocolInfo() {
    return {
        baseFreq:     BASE_FREQ,
        freqStep:     FREQ_STEP,
        numCarriers:  NUM_CARRIERS,
        symbolMs:     SYMBOL_MS,
        guardMs:      GUARD_MS,
        syncA:        SYNC_A,
        syncB:        SYNC_B,
        startTone:    START_TONE,
        fftSize:      FFT_SIZE,
        sampleRate:   SAMPLE_RATE,
        bitsPerSymbol: 4,
        maxPayloadBytes: 4096,
    };
}

export async function dispose(instanceId) {
    const inst = instances.get(instanceId);
    if (!inst) return;
    await inst.dispose();
    instances.delete(instanceId);
}
