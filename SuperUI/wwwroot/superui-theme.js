// superui-theme.js - Theme management (light/dark mode)

const THEME_KEY = 'superui-theme';
const DEFAULT_THEME = 'light';

// Global namespace for SuperUI runtime operations
window.SuperUI = window.SuperUI || {};

window.SuperUI.applyThemeCss = function(css) {
    let styleEl = document.getElementById('sg-dynamic-theme');
    if (!styleEl) {
        styleEl = document.createElement('style');
        styleEl.id = 'sg-dynamic-theme';
        document.head.appendChild(styleEl);
    }
    styleEl.textContent = css;
};

export function getTheme() {
    try {
        if (typeof localStorage !== 'undefined') {
            return localStorage.getItem(THEME_KEY) || DEFAULT_THEME;
        }
    } catch { }
    return DEFAULT_THEME;
}

export function setTheme(theme) {
    try {
        if (typeof localStorage !== 'undefined') {
            localStorage.setItem(THEME_KEY, theme);
        }
    } catch { }
    applyTheme(theme);
}

export function getEffectiveTheme() {
    const theme = getTheme();
    if (theme === 'auto') {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return theme;
}

function applyTheme(theme) {
    const root = document.documentElement;

    if (theme === 'auto') {
        root.removeAttribute('data-theme');
    } else {
        root.setAttribute('data-theme', theme);
    }
}

// Initialize theme on load — default is light
applyTheme(getTheme());

// Listen for system theme changes when in auto mode
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (getTheme() === 'auto') applyTheme('auto');
});
