// superui-theme.js - Theme management (light/dark mode)

const THEME_KEY = 'superui-theme';

export function getTheme() {
    return localStorage.getItem(THEME_KEY) || 'auto';
}

export function setTheme(theme) {
    localStorage.setItem(THEME_KEY, theme);
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
        // Remove explicit theme attribute, let CSS media query handle it
        root.removeAttribute('data-theme');
    } else {
        root.setAttribute('data-theme', theme);
    }
}

// Initialize theme on load
applyTheme(getTheme());

// Listen for system theme changes when in auto mode
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    const currentTheme = getTheme();
    if (currentTheme === 'auto') {
        // Re-apply auto theme to trigger CSS updates
        applyTheme('auto');
    }
});
