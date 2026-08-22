export function sanitizeHtml(html, opts) {
    opts = opts || {};
    // Prefer DOMPurify if loaded
    if (typeof DOMPurify !== 'undefined' && DOMPurify.sanitize) {
        return DOMPurify.sanitize(html, {USE_PROFILES: {html: true}, FORBID_TAGS: ['script','style','iframe','object','embed','link','meta'], FORBID_ATTR: ['onerror','onload','onclick','style'], ...opts});
    }
    // Fallback: use browser Sanitizer API or naive strip
    if (typeof Sanitizer !== 'undefined') {
        try { const s = new Sanitizer(); const tmpl = document.createElement('template'); tmpl.innerHTML = html; const frag = s.sanitize(tmpl); const div = document.createElement('div'); div.appendChild(frag); return div.innerHTML; } catch {}
    }
    // Naive: strip script/style/iframe and event attrs
    let out = html.replace(/<script[\s\S]*?<\/script>/gi, '').replace(/<style[\s\S]*?<\/style>/gi, '').replace(/<iframe[\s\S]*?<\/iframe>/gi,'');
    out = out.replace(/\son\w+="[^"]*"/gi,'').replace(/\son\w+='[^']*'/gi,'').replace(/\sstyle\s*=\s*"[^"]*"/gi,'');
    // Strip srcdoc/src potentially dangerous
    out = out.replace(/\ssrcdoc\s*=\s*"[^"]*"/gi,'').replace(/\ssrcdoc\s*=\s*'[^']*'/gi,'');
    return out;
}
export function sanitizeSvg(svg) {
    // Mermaid SVG sanitization - forbid foreignObject script
    return sanitizeHtml(svg, {FORBID_TAGS: ['script','foreignObject']});
}
