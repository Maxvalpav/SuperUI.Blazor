function _esc(str) {
    const d = document.createElement('div');
    d.textContent = str;
    return d.innerHTML;
}

function _loadScript(url) {
    return new Promise((resolve, reject) => {
        const existing = document.querySelector(`script[src="${url}"]`);
        if (existing) { resolve(); return; }
        const s = document.createElement('script');
        s.src = url;
        s.onload = () => resolve();
        s.onerror = () => reject(new Error(`Failed to load script: ${url}`));
        document.head.appendChild(s);
    });
}

export async function loadMicroFrontend(source, componentType, containerId, parameters) {
    const container = document.getElementById(containerId);
    if (!container) return;

    // 1. Handle Demo / Mock
    if (source === "demo") {
        await new Promise(r => setTimeout(r, 1000));
        container.innerHTML = `
            <div style="padding: 20px; border: 2px solid var(--sui-primary-color); border-radius: 12px; background: var(--sui-bg-secondary); box-shadow: var(--sui-shadow-md);">
                <h4 style="color: var(--sui-primary-color); margin-top: 0; display: flex; align-items: center; gap: 8px;">
                    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M9 3v18M3 9h18"/></svg>
                    ${componentType}
                </h4>
                <p style="font-size: 14px; color: var(--sui-fg-muted);">Dynamic micro frontend content.</p>
                <div style="display: flex; gap: 8px;">
                    <span style="background: var(--sui-primary-color); color: white; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">EXTERNAL</span>
                    <span style="background: var(--sui-success-color); color: white; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 600;">ACTIVE</span>
                </div>
            </div>
        `;
        return;
    }

    // 2. Handle Web Components / Custom Elements
    if (source === "web-component") {
        const el = document.createElement(componentType);
        if (parameters) {
            Object.keys(parameters).forEach(key => {
                el.setAttribute(key, parameters[key]);
                el[key] = parameters[key];
            });
        }
        container.innerHTML = '';
        container.appendChild(el);
        return;
    }

    // 3. Handle External JS Modules / CDN Scripts
    try {
        if (source.endsWith('.js')) {
            // Try dynamic import first (supports ES modules with 'export')
            try {
                const mod = await import(source);
                const mountFn = mod?.default || mod?.mount || window[`__mf_${componentType}`] || window[componentType];
                if (typeof mountFn === 'function') {
                    mountFn(container, parameters);
                    return;
                }
            } catch {
                // Fall back to classic script load
            }
            await _loadScript(source);
            // Check if script registered a global mount function
            const mountFn = window[`__mf_${componentType}`] || window[componentType];
            if (typeof mountFn === 'function') {
                mountFn(container, parameters);
            }
        }
    } catch (e) {
        console.error('Failed to load micro frontend module', e);
        container.innerHTML = `<div style="color: var(--sui-danger-color); padding: 12px; border: 1px solid currentColor; border-radius: 4px;">Error: ${_esc(e.message)}</div>`;
    }
}
