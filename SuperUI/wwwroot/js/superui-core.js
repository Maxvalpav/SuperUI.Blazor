// superui-core.js - Базовые JS утилиты для SuperUI компонентов
// Включает focus, blur и другие общие функции

window.SuperUI = window.SuperUI || {};

// Focus элемент по ID
window.SuperUI.focus = function(elementId) {
    const el = document.getElementById(elementId);
    if (el && typeof el.focus === 'function') {
        el.focus();
        return true;
    }
    console.warn(`[SuperUI.focus] Элемент #${elementId} не найден или не фокусируемый`);
    return false;
};

// Blur элемент по ID
window.SuperUI.blur = function(elementId) {
    const el = document.getElementById(elementId);
    if (el && typeof el.blur === 'function') {
        el.blur();
        return true;
    }
    console.warn(`[SuperUI.blur] Элемент #${elementId} не найден`);
    return false;
};

// Получить размеры элемента
window.SuperUI.getBounds = function(elementId) {
    const el = document.getElementById(elementId);
    if (!el) return null;
    const rect = el.getBoundingClientRect();
    return {
        top: rect.top,
        left: rect.left,
        width: rect.width,
        height: rect.height,
        right: rect.right,
        bottom: rect.bottom
    };
};

// Проверка видимости элемента
window.SuperUI.isVisible = function(elementId) {
    const el = document.getElementById(elementId);
    if (!el) return false;
    const style = getComputedStyle(el);
    return style.display !== 'none' && style.visibility !== 'hidden' && style.opacity !== '0';
};
