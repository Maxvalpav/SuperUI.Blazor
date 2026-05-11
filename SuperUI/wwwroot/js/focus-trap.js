// focus-trap.js - Управление фокусом в модальных окнах и overlay компонентах
// Реализует trap focus внутри контейнера (как в accessibility best practices)

window.SuperUI = window.SuperUI || {};

window.SuperUI.focusTrap = {
    _traps: new Map(),

    // Активировать trap фокуса внутри элемента
    activate(elementId, options = {}) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.warn(`[SuperUI.focusTrap] Элемент #${elementId} не найден`);
            return false;
        }

        // Найти все фокусируемые элементы внутри контейнера
        const focusableSelectors = [
            'a[href]',
            'button:not([disabled])',
            'input:not([disabled]):not([type="hidden"])',
            'select:not([disabled])',
            'textarea:not([disabled])',
            '[tabindex]:not([tabindex="-1"])',
            '[contenteditable="true"]'
        ].join(',');

        const focusableElements = Array.from(
            element.querySelectorAll(focusableSelectors)
        ).filter(el => {
            return el.offsetParent !== null && getComputedStyle(el).visibility !== 'hidden';
        });

        if (focusableElements.length === 0) {
            console.warn(`[SuperUI.focusTrap] Нет фокусируемых элементов в #${elementId}`);
            return false;
        }

        const firstElement = focusableElements[0];
        const lastElement = focusableElements[focusableElements.length - 1];

        // Сохранить текущий активный элемент для восстановления
        const previouslyFocused = document.activeElement;

        // Обработчик для перехвата Tab
        const handleKeyDown = (e) => {
            if (e.key !== 'Tab') return;

            if (e.shiftKey) {
                // Shift+Tab: если на первом элементе → перейти на последний
                if (document.activeElement === firstElement) {
                    e.preventDefault();
                    lastElement.focus();
                }
            } else {
                // Tab: если на последнем элементе → перейти на первый
                if (document.activeElement === lastElement) {
                    e.preventDefault();
                    firstElement.focus();
                }
            }
        };

        // Сохранить trap
        this._traps.set(elementId, {
            element,
            focusableElements,
            previouslyFocused,
            handleKeyDown
        });

        // Добавить listener
        element.addEventListener('keydown', handleKeyDown);

        // Сфокусировать первый элемент
        firstElement.focus();

        return true;
    },

    // Деактивировать trap и вернуть фокус
    deactivate(elementId) {
        const trap = this._traps.get(elementId);
        if (!trap) return false;

        // Удалить listener
        trap.element.removeEventListener('keydown', trap.handleKeyDown);

        // Вернуть фокус предыдущему элементу
        if (trap.previouslyFocused && typeof trap.previouslyFocused.focus === 'function') {
            trap.previouslyFocused.focus();
        }

        // Удалить из мапы
        this._traps.delete(elementId);
        return true;
    },

    // Проверить, активен ли trap
    isActive(elementId) {
        return this._traps.has(elementId);
    }
};

// Helper функции для focus/blur (используются в SgInteractiveBase)
window.SuperUI.focus = function(elementId) {
    const el = document.getElementById(elementId);
    if (el && typeof el.focus === 'function') {
        el.focus();
        return true;
    }
    console.warn(`[SuperUI.focus] Элемент #${elementId} не найден или не фокусируемый`);
    return false;
};

window.SuperUI.blur = function(elementId) {
    const el = document.getElementById(elementId);
    if (el && typeof el.blur === 'function') {
        el.blur();
        return true;
    }
    console.warn(`[SuperUI.blur] Элемент #${elementId} не найден`);
    return false;
};
