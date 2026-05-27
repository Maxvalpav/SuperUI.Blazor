// sg-blob.js — helpers for downloading binary/text/base64 content from Blazor.
// Used by SgLlmSpeaker (TTS .mp3), SgLlmImageStudio (image download),
// SgLlmTranscriber (export transcript) and any consumer that needs to save
// generated artifacts to disk.
//
// API is intentionally tiny and side-effect free except for the actual download click.
// All functions are SAFE in the browser sandbox — no eval, no innerHTML.
//
// Loaded via `import("./_content/SuperUI/sg-blob.js")` — ESM module.

function triggerDownload(url, filename) {
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'download';
    a.rel = 'noopener';
    a.style.display = 'none';
    document.body.appendChild(a);
    a.click();
    setTimeout(() => document.body.removeChild(a), 0);
}

function base64ToBytes(base64) {
    const idx = base64.indexOf(',');
    const clean = idx >= 0 ? base64.substring(idx + 1) : base64;
    const binary = atob(clean);
    const len = binary.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) bytes[i] = binary.charCodeAt(i);
    return bytes;
}

export function downloadBase64(base64, mime, filename) {
    if (!base64) return false;
    const bytes = base64ToBytes(base64);
    const blob = new Blob([bytes], { type: mime || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    try { triggerDownload(url, filename); }
    finally { setTimeout(() => URL.revokeObjectURL(url), 1500); }
    return true;
}

export function downloadBytes(byteArray, mime, filename) {
    if (!byteArray) return false;
    const u8 = byteArray instanceof Uint8Array ? byteArray : new Uint8Array(byteArray);
    const blob = new Blob([u8], { type: mime || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    try { triggerDownload(url, filename); }
    finally { setTimeout(() => URL.revokeObjectURL(url), 1500); }
    return true;
}

export function downloadText(text, filename, mime) {
    const blob = new Blob([text || ''], { type: mime || 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    try { triggerDownload(url, filename); }
    finally { setTimeout(() => URL.revokeObjectURL(url), 1500); }
    return true;
}

export function downloadUrl(srcUrl, filename) {
    if (!srcUrl) return false;
    triggerDownload(srcUrl, filename);
    return true;
}

// Smoothly scroll the first element matching `selector` to its bottom.
// Returns true if an element was found, false otherwise. Safe to call when
// the element does not exist yet (e.g. before first render).
export function scrollSelectorToBottom(selector, behavior) {
    if (!selector) return false;
    const el = document.querySelector(selector);
    if (!el) return false;
    try {
        el.scrollTo({ top: el.scrollHeight, behavior: behavior || 'smooth' });
    } catch {
        // Older browsers without scrollTo({behavior}) — fall back to direct assignment.
        el.scrollTop = el.scrollHeight;
    }
    return true;
}

// Copy text to the clipboard. Uses the modern async API when available,
// falls back to a hidden textarea + execCommand for older browsers.
export async function copyText(text) {
    if (text == null) return false;
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        }
    } catch { /* fall through to legacy path */ }

    const ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select();
    let ok = false;
    try { ok = document.execCommand('copy'); } catch { ok = false; }
    document.body.removeChild(ta);
    return ok;
}

// Convenience default export so consumers can do
//   const blob = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/sg-blob.js");
//   await blob.InvokeVoidAsync("downloadBase64", b64, mime, name);
export default { downloadBase64, downloadBytes, downloadText, downloadUrl, scrollSelectorToBottom, copyText };
