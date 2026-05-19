window.SuperUI = window.SuperUI || {};

window.SuperUI.applyThemeCss = function (css) {
    let styleEl = document.getElementById('sg-dynamic-theme');
    if (!styleEl) {
        styleEl = document.createElement('style');
        styleEl.id = 'sg-dynamic-theme';
        document.head.appendChild(styleEl);
    }
    styleEl.textContent = css;
};

const THEME_ID_KEY = 'superui-theme-id';
const MODE_KEY = 'superui-dark-mode';

export function getSavedThemeId() {
    try {
        return localStorage.getItem(THEME_ID_KEY);
    } catch {
        return null;
    }
}

export function getSavedMode() {
    try {
        return localStorage.getItem(MODE_KEY);
    } catch {
        return null;
    }
}
