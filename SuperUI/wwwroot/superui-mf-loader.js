export async function loadMicroFrontend(source, componentType, containerId, parameters) {
    console.log(`Loading Micro Frontend: ${componentType} from ${source} into ${containerId}`);
    
    const container = document.getElementById(containerId);
    if (!container) return;

    // 1. Handle Demo / Mock
    if (source === "demo") {
        await new Promise(r => setTimeout(r, 1000));
        container.innerHTML = `
            <div style="padding: 20px; border: 2px solid var(--sui-primary-color); border-radius: 12px; background: var(--sui-bg-secondary); box-shadow: var(--sui-shadow-md);">
                <h4 style="color: var(--sui-primary-color); margin-top: 0; display: flex; align-items: center; gap: 8px;">
                    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M9 3v18M3 9h18"/></svg>
                    Удаленный модуль: ${componentType}
                </h4>
                <p style="font-size: 14px; color: var(--sui-fg-muted);">Этот контент был загружен динамически через SgMicroFrontend.</p>
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
                // Also set as property for Blazor Custom Elements compatibility
                el[key] = parameters[key];
            });
        }
        container.innerHTML = '';
        container.appendChild(el);
        return;
    }

    // 3. Handle External JS Modules
    try {
        if (source.endsWith('.js')) {
            const module = await import(source);
            if (module.mount) {
                module.mount(container, parameters);
            } else if (module.default && typeof module.default === 'function') {
                new module.default(container, parameters);
            }
        }
    } catch (e) {
        console.error('Failed to load micro frontend module', e);
        container.innerHTML = `<div style="color: var(--sui-danger-color); padding: 12px; border: 1px solid currentColor; border-radius: 4px;">Error: ${e.message}</div>`;
    }
}
