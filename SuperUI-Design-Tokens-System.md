# SuperUI Blazor — Система дизайн-токенов

> **Полное руководство для агента**: архитектура, реализация, интеграция тем (Default / Material / Tailwind / Custom).  
> Все шаги сопровождаются готовым кодом и последовательностью действий.

---

## Содержание

1. [Обзор архитектуры](#1-обзор-архитектуры)
2. [Текущее состояние и анализ](#2-текущее-состояние-и-анализ)
3. [Новая архитектура токенов](#3-новая-архитектура-токенов)
4. [Файловая структура](#4-файловая-структура)
5. [Уровень 0 — Примитивные токены (Primitives)](#5-уровень-0--примитивные-токены-primitives)
6. [Уровень 1 — Семантические токены (Semantic)](#6-уровень-1--семантические-токены-semantic)
7. [Уровень 2 — Компонентные токены (Component)](#7-уровень-2--компонентные-токены-component)
8. [Интерфейсы C# для тем](#8-интерфейсы-c-для-тем)
9. [Реализация тем](#9-реализация-тем)
   - 9.1 [Default (текущая тема)](#91-default-текущая-тема)
   - 9.2 [Material Design 3](#92-material-design-3)
   - 9.3 [Tailwind CSS](#93-tailwind-css)
   - 9.4 [Custom Theme](#94-custom-theme)
10. [ThemeService — расширенная реализация](#10-themeservice--расширенная-реализация)
11. [ThemeBuilder — Fluent API](#11-themebuilder--fluent-api)
12. [CSS — полный код файлов](#12-css--полный-код-файлов)
13. [Blazor-компоненты](#13-blazor-компоненты)
14. [Регистрация в DI](#14-регистрация-в-di)
15. [Шаги для агента — пошаговая инструкция](#15-шаги-для-агента--пошаговая-инструкция)
16. [Тестирование и валидация](#16-тестирование-и-валидация)
17. [Миграция с текущей версии](#17-миграция-с-текущей-версии)

---

## 1. Обзор архитектуры

```
┌─────────────────────────────────────────────────────────────────┐
│                     ТЕМА (IThemeDefinition)                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Primitives  │  │   Semantic   │  │     Component        │   │
│  │  (raw vals)  │→ │  (abstract)  │→ │  (specific tokens)   │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
           ↓ генерирует
┌─────────────────────────────────────────────────────────────────┐
│              CSS Custom Properties (:root / [data-theme])        │
│   --sg-primitive-*   --sg-*   --sgc-button-*  --sgc-input-*    │
└─────────────────────────────────────────────────────────────────┘
           ↓ потребляют
┌─────────────────────────────────────────────────────────────────┐
│             Blazor-компоненты (.razor / .razor.css)              │
│   SgButton   SgTextBox   SgDataGrid   SgModal   SgCard  ...     │
└─────────────────────────────────────────────────────────────────┘
```

### Принципы

| Принцип | Описание |
|---------|----------|
| **3-уровневая иерархия** | Primitives → Semantic → Component |
| **Модульность** | Каждая тема реализует единый интерфейс `IThemeDefinition` |
| **CSS Custom Properties** | Единственный механизм применения токенов |
| **Без JS во время выполнения** | Тема генерируется один раз при инициализации |
| **Обратная совместимость** | Алиасы `--sui-*` → `--sg-*` сохранены |
| **Автоматическая генерация** | C# → CSS через `ThemeGenerator` |

---

## 2. Текущее состояние и анализ

### Что есть сейчас

**`superui-theme.css`** содержит:
- CSS-переменные с префиксом `--sui-*` (светлая/тёмная темы)
- Жёстко зашитые значения без иерархии
- Смешивание токенов темы и стилей компонентов (SgScheduler в том же файле)

**`superui-components.css`** содержит:
- Алиасы `--sg-*` → `--sui-*`
- Shared паттерны (skeleton, spinner, badge, progress и др.)
- Стили конкретных компонентов (кнопки, инпуты, таблицы...)

**`SgThemeService.cs`**:
- Управляет только `light`/`dark`/`auto`
- Нет поддержки множественных тем
- Нет типизации токенов

**`SgThemeEditor.razor`**:
- Редактирует 5 переменных через `eval()`
- Не связан с типизированной системой

### Проблемы

1. ❌ Нет иерархии токенов — невозможно безопасно переопределить часть темы
2. ❌ Нет интерфейса для создания кастомных тем
3. ❌ Смешаны токены и стили компонентов
4. ❌ Дублирование: `--sui-*` и `--sg-*` делают одно и то же
5. ❌ Тяжело внедрить Material/Tailwind без полного переписывания CSS

---

## 3. Новая архитектура токенов

### Соглашение об именовании

```
--sg-{уровень}-{категория}-{свойство}-{состояние?}-{модификатор?}

Примеры:
--sg-p-blue-500          ← primitive: синий 500
--sg-color-primary       ← semantic: основной цвет
--sg-color-primary-hover ← semantic: hover-состояние основного
--sgc-btn-bg             ← component: фон кнопки
--sgc-btn-bg-hover       ← component: фон кнопки при hover
--sgc-btn-bg-primary     ← component: фон кнопки variant=primary
```

### Уровни токенов

```
Уровень 0 (Primitives)  → --sg-p-*
Уровень 1 (Semantic)    → --sg-*
Уровень 2 (Component)   → --sgc-*

Обратная совместимость  → --sui-* (алиасы → --sg-*)
```

---

## 4. Файловая структура

```
SuperUI/
├── wwwroot/
│   ├── themes/
│   │   ├── sg-tokens-primitives.css     ← Уровень 0: raw значения
│   │   ├── sg-tokens-semantic.css       ← Уровень 1: semantic light
│   │   ├── sg-tokens-semantic-dark.css  ← Уровень 1: semantic dark
│   │   ├── sg-tokens-component.css      ← Уровень 2: component defaults
│   │   ├── sg-tokens-compat.css         ← Алиасы --sui-* / --sg-* (обратная совместимость)
│   │   ├── themes/
│   │   │   ├── sg-theme-default.css     ← Default тема (текущая)
│   │   │   ├── sg-theme-material.css    ← Material Design 3
│   │   │   ├── sg-theme-tailwind.css    ← Tailwind CSS
│   │   │   └── sg-theme-custom.css      ← Шаблон для кастомной темы
│   │   └── sg-theme-bundle.css          ← Объединённый файл (import all)
│   ├── superui-theme.css                ← УСТАРЕВШИЙ (переадресует на новый)
│   └── superui-components.css           ← Сохраняется, добавляется import
│
├── Components/
│   ├── SgThemeEditor.razor              ← Расширенный редактор тем
│   ├── SgThemeEditor.razor.cs           ← Code-behind
│   ├── SgThemeSwitcher.razor            ← Переключатель тем
│   └── SgThemeToggle.razor              ← Переключатель light/dark
│
├── Services/
│   ├── SgThemeService.cs                ← Расширенный сервис
│   └── SgThemeGenerator.cs             ← Генератор CSS из C# объектов
│
└── Themes/
    ├── IThemeDefinition.cs              ← Основной интерфейс
    ├── IThemePrimitives.cs              ← Интерфейс примитивов
    ├── IThemeSemantic.cs                ← Интерфейс семантики
    ├── IThemeComponents.cs              ← Интерфейс компонентов
    ├── ThemeBase.cs                     ← Базовый класс
    ├── ThemeBuilder.cs                  ← Fluent builder
    ├── ThemeRegistry.cs                 ← Реестр тем
    ├── DefaultTheme.cs                  ← Default реализация
    ├── MaterialTheme.cs                 ← Material Design 3
    ├── TailwindTheme.cs                 ← Tailwind CSS
    └── Models/
        ├── ColorScale.cs                ← Цветовая шкала
        ├── SpacingScale.cs              ← Шкала отступов
        ├── TypographyScale.cs           ← Типографика
        └── ShadowScale.cs              ← Тени
```

---

## 5. Уровень 0 — Примитивные токены (Primitives)

**Файл: `wwwroot/themes/sg-tokens-primitives.css`**

```css
/* =============================================================================
   SuperUI — Primitive Tokens (Level 0)
   Чистые значения без семантики. Используются только внутри semantic-токенов.
   Не используйте --sg-p-* напрямую в компонентах!
   ============================================================================= */

:root {
    /* ── Цветовые шкалы ──────────────────────────────────────────────────── */

    /* Neutral (Gray) */
    --sg-p-neutral-0:   #ffffff;
    --sg-p-neutral-50:  #f9fafb;
    --sg-p-neutral-100: #f3f4f6;
    --sg-p-neutral-150: #eaecf0;
    --sg-p-neutral-200: #e5e7eb;
    --sg-p-neutral-300: #d1d5db;
    --sg-p-neutral-400: #9ca3af;
    --sg-p-neutral-500: #6b7280;
    --sg-p-neutral-600: #4b5563;
    --sg-p-neutral-700: #374151;
    --sg-p-neutral-800: #1f2937;
    --sg-p-neutral-900: #111827;
    --sg-p-neutral-950: #030712;
    --sg-p-neutral-1000: #000000;

    /* Neutral Dark (для dark mode) */
    --sg-p-dark-0:   #0a0a0a;
    --sg-p-dark-50:  #141414;
    --sg-p-dark-100: #171717;
    --sg-p-dark-150: #1c1c1c;
    --sg-p-dark-200: #262626;
    --sg-p-dark-300: #383838;
    --sg-p-dark-400: #404040;
    --sg-p-dark-500: #525252;
    --sg-p-dark-600: #737373;
    --sg-p-dark-700: #a3a3a3;
    --sg-p-dark-800: #d4d4d4;
    --sg-p-dark-900: #e5e5e5;
    --sg-p-dark-950: #fafafa;

    /* Blue (Primary/Accent) */
    --sg-p-blue-50:  #eff6ff;
    --sg-p-blue-100: #dbeafe;
    --sg-p-blue-200: #bfdbfe;
    --sg-p-blue-300: #93c5fd;
    --sg-p-blue-400: #60a5fa;
    --sg-p-blue-500: #3b82f6;
    --sg-p-blue-600: #2563eb;
    --sg-p-blue-700: #1d4ed8;
    --sg-p-blue-800: #1e40af;
    --sg-p-blue-900: #1e3a8a;

    /* Cyan (Info) */
    --sg-p-cyan-50:  #ecfeff;
    --sg-p-cyan-100: #cffafe;
    --sg-p-cyan-200: #a5f3fc;
    --sg-p-cyan-400: #22d3ee;
    --sg-p-cyan-500: #06b6d4;
    --sg-p-cyan-600: #0891b2;
    --sg-p-cyan-700: #0e7490;

    /* Sky (Link/Info variant) */
    --sg-p-sky-400: #38bdf8;
    --sg-p-sky-500: #0ea5e9;
    --sg-p-sky-600: #0284c7;

    /* Green (Success) */
    --sg-p-green-50:  #f0fdf4;
    --sg-p-green-100: #dcfce7;
    --sg-p-green-200: #bbf7d0;
    --sg-p-green-300: #86efac;
    --sg-p-green-400: #4ade80;
    --sg-p-green-500: #22c55e;
    --sg-p-green-600: #16a34a;
    --sg-p-green-700: #15803d;

    /* Emerald (Success variant) */
    --sg-p-emerald-50:  #ecfdf5;
    --sg-p-emerald-100: #d1fae5;
    --sg-p-emerald-200: #a7f3d0;
    --sg-p-emerald-500: #10b981;
    --sg-p-emerald-600: #059669;
    --sg-p-emerald-700: #047857;

    /* Amber (Warning) */
    --sg-p-amber-50:  #fffbeb;
    --sg-p-amber-100: #fef3c7;
    --sg-p-amber-200: #fde68a;
    --sg-p-amber-400: #fbbf24;
    --sg-p-amber-500: #f59e0b;
    --sg-p-amber-600: #d97706;
    --sg-p-amber-700: #b45309;

    /* Red (Danger/Error) */
    --sg-p-red-50:  #fef2f2;
    --sg-p-red-100: #fee2e2;
    --sg-p-red-200: #fecaca;
    --sg-p-red-300: #fca5a5;
    --sg-p-red-400: #f87171;
    --sg-p-red-500: #ef4444;
    --sg-p-red-600: #dc2626;
    --sg-p-red-700: #b91c1c;

    /* Rose (Danger variant) */
    --sg-p-rose-50:  #fff1f2;
    --sg-p-rose-100: #ffe4e6;
    --sg-p-rose-200: #fecdd3;
    --sg-p-rose-400: #fb7185;
    --sg-p-rose-500: #f43f5e;
    --sg-p-rose-600: #e11d48;

    /* Purple (Secondary accent) */
    --sg-p-purple-50:  #faf5ff;
    --sg-p-purple-100: #f3e8ff;
    --sg-p-purple-400: #c084fc;
    --sg-p-purple-500: #a855f7;
    --sg-p-purple-600: #9333ea;
    --sg-p-purple-700: #7e22ce;

    /* ── Типографика ────────────────────────────────────────────────────── */

    /* Font Families */
    --sg-p-font-sans:  'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    --sg-p-font-serif: Georgia, 'Times New Roman', serif;
    --sg-p-font-mono:  'JetBrains Mono', 'Fira Code', 'Cascadia Code', ui-monospace, monospace;

    /* Font Weights */
    --sg-p-fw-thin:       100;
    --sg-p-fw-extralight: 200;
    --sg-p-fw-light:      300;
    --sg-p-fw-normal:     400;
    --sg-p-fw-medium:     500;
    --sg-p-fw-semibold:   600;
    --sg-p-fw-bold:       700;
    --sg-p-fw-extrabold:  800;
    --sg-p-fw-black:      900;

    /* Font Sizes */
    --sg-p-text-2xs: 0.625rem;   /* 10px */
    --sg-p-text-xs:  0.75rem;    /* 12px */
    --sg-p-text-sm:  0.8125rem;  /* 13px */
    --sg-p-text-md:  0.875rem;   /* 14px */
    --sg-p-text-base: 1rem;      /* 16px */
    --sg-p-text-lg:  1.125rem;   /* 18px */
    --sg-p-text-xl:  1.25rem;    /* 20px */
    --sg-p-text-2xl: 1.5rem;     /* 24px */
    --sg-p-text-3xl: 1.875rem;   /* 30px */
    --sg-p-text-4xl: 2.25rem;    /* 36px */

    /* Line Heights */
    --sg-p-lh-none:    1;
    --sg-p-lh-tight:   1.25;
    --sg-p-lh-snug:    1.375;
    --sg-p-lh-normal:  1.5;
    --sg-p-lh-relaxed: 1.625;
    --sg-p-lh-loose:   2;

    /* Letter Spacing */
    --sg-p-ls-tighter: -0.05em;
    --sg-p-ls-tight:   -0.025em;
    --sg-p-ls-normal:   0em;
    --sg-p-ls-wide:     0.025em;
    --sg-p-ls-wider:    0.05em;
    --sg-p-ls-widest:   0.1em;

    /* ── Отступы и размеры ──────────────────────────────────────────────── */

    --sg-p-space-0:    0;
    --sg-p-space-px:   1px;
    --sg-p-space-0-5:  0.125rem;  /* 2px  */
    --sg-p-space-1:    0.25rem;   /* 4px  */
    --sg-p-space-1-5:  0.375rem;  /* 6px  */
    --sg-p-space-2:    0.5rem;    /* 8px  */
    --sg-p-space-2-5:  0.625rem;  /* 10px */
    --sg-p-space-3:    0.75rem;   /* 12px */
    --sg-p-space-3-5:  0.875rem;  /* 14px */
    --sg-p-space-4:    1rem;      /* 16px */
    --sg-p-space-5:    1.25rem;   /* 20px */
    --sg-p-space-6:    1.5rem;    /* 24px */
    --sg-p-space-7:    1.75rem;   /* 28px */
    --sg-p-space-8:    2rem;      /* 32px */
    --sg-p-space-9:    2.25rem;   /* 36px */
    --sg-p-space-10:   2.5rem;    /* 40px */
    --sg-p-space-12:   3rem;      /* 48px */
    --sg-p-space-14:   3.5rem;    /* 56px */
    --sg-p-space-16:   4rem;      /* 64px */
    --sg-p-space-20:   5rem;      /* 80px */
    --sg-p-space-24:   6rem;      /* 96px */

    /* ── Border Radius ──────────────────────────────────────────────────── */

    --sg-p-radius-none: 0;
    --sg-p-radius-xs:   2px;
    --sg-p-radius-sm:   4px;
    --sg-p-radius-md:   6px;
    --sg-p-radius-lg:   8px;
    --sg-p-radius-xl:   12px;
    --sg-p-radius-2xl:  16px;
    --sg-p-radius-3xl:  24px;
    --sg-p-radius-full: 9999px;

    /* ── Border Width ───────────────────────────────────────────────────── */

    --sg-p-border-0:   0;
    --sg-p-border-1:   1px;
    --sg-p-border-2:   2px;
    --sg-p-border-4:   4px;

    /* ── Z-Index ─────────────────────────────────────────────────────────── */

    --sg-p-z-0:       0;
    --sg-p-z-10:      10;
    --sg-p-z-20:      20;
    --sg-p-z-30:      30;
    --sg-p-z-40:      40;
    --sg-p-z-50:      50;
    --sg-p-z-100:     100;
    --sg-p-z-200:     200;
    --sg-p-z-300:     300;
    --sg-p-z-400:     400;
    --sg-p-z-500:     500;
    --sg-p-z-9999:    9999;

    /* ── Transitions ─────────────────────────────────────────────────────── */

    --sg-p-duration-75:   75ms;
    --sg-p-duration-100:  100ms;
    --sg-p-duration-150:  150ms;
    --sg-p-duration-200:  200ms;
    --sg-p-duration-300:  300ms;
    --sg-p-duration-500:  500ms;
    --sg-p-duration-700:  700ms;

    --sg-p-ease-linear:  linear;
    --sg-p-ease-in:      cubic-bezier(0.4, 0, 1, 1);
    --sg-p-ease-out:     cubic-bezier(0, 0, 0.2, 1);
    --sg-p-ease-in-out:  cubic-bezier(0.4, 0, 0.2, 1);

    /* ── Тени (raw values) ──────────────────────────────────────────────── */

    --sg-p-shadow-xs:    0 1px 2px 0 rgba(0, 0, 0, 0.05);
    --sg-p-shadow-sm:    0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px -1px rgba(0, 0, 0, 0.1);
    --sg-p-shadow-md:    0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1);
    --sg-p-shadow-lg:    0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.1);
    --sg-p-shadow-xl:    0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
    --sg-p-shadow-2xl:   0 25px 50px -12px rgba(0, 0, 0, 0.25);
    --sg-p-shadow-inner: inset 0 2px 4px 0 rgba(0, 0, 0, 0.05);
    --sg-p-shadow-none:  none;

    /* Dark mode shadows (усиленные) */
    --sg-p-shadow-dark-xs: 0 1px 2px 0 rgba(0, 0, 0, 0.5);
    --sg-p-shadow-dark-sm: 0 1px 3px 0 rgba(0, 0, 0, 0.6), 0 1px 2px -1px rgba(0, 0, 0, 0.4);
    --sg-p-shadow-dark-md: 0 4px 6px -1px rgba(0, 0, 0, 0.6), 0 2px 4px -2px rgba(0, 0, 0, 0.4);
    --sg-p-shadow-dark-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.7), 0 4px 6px -4px rgba(0, 0, 0, 0.5);
    --sg-p-shadow-dark-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.8), 0 8px 10px -6px rgba(0, 0, 0, 0.6);

    /* ── Blur ───────────────────────────────────────────────────────────── */

    --sg-p-blur-none: blur(0);
    --sg-p-blur-sm:   blur(4px);
    --sg-p-blur-md:   blur(8px);
    --sg-p-blur-lg:   blur(16px);
    --sg-p-blur-xl:   blur(24px);
    --sg-p-blur-2xl:  blur(40px);
    --sg-p-blur-3xl:  blur(64px);

    /* ── Opacity ────────────────────────────────────────────────────────── */

    --sg-p-opacity-0:    0;
    --sg-p-opacity-5:    0.05;
    --sg-p-opacity-10:   0.10;
    --sg-p-opacity-20:   0.20;
    --sg-p-opacity-25:   0.25;
    --sg-p-opacity-30:   0.30;
    --sg-p-opacity-40:   0.40;
    --sg-p-opacity-50:   0.50;
    --sg-p-opacity-60:   0.60;
    --sg-p-opacity-70:   0.70;
    --sg-p-opacity-75:   0.75;
    --sg-p-opacity-80:   0.80;
    --sg-p-opacity-90:   0.90;
    --sg-p-opacity-95:   0.95;
    --sg-p-opacity-100:  1;
}
```

---

## 6. Уровень 1 — Семантические токены (Semantic)

**Файл: `wwwroot/themes/sg-tokens-semantic.css`**

```css
/* =============================================================================
   SuperUI — Semantic Tokens (Level 1) — LIGHT MODE
   Все значения ссылаются на Primitives (--sg-p-*).
   Это слой абстракции: "primary color" вместо "blue-600".
   ============================================================================= */

:root,
[data-theme="light"] {

    /* ── Цвета фона ─────────────────────────────────────────────────────── */

    --sg-bg:              var(--sg-p-neutral-0);      /* страница/основной фон */
    --sg-bg-subtle:       var(--sg-p-neutral-50);     /* слегка выделенный */
    --sg-bg-muted:        var(--sg-p-neutral-100);    /* приглушённый */
    --sg-bg-emphasized:   var(--sg-p-neutral-150);    /* усиленный акцент */
    --sg-bg-overlay:      rgba(0, 0, 0, 0.5);         /* оверлей модалки */

    /* Surface (карточки, панели) */
    --sg-surface:         var(--sg-p-neutral-0);
    --sg-surface-raised:  var(--sg-p-neutral-0);      /* приподнятые элементы */
    --sg-surface-overlay: var(--sg-p-neutral-0);      /* поверх оверлея */

    /* ── Цвета текста ───────────────────────────────────────────────────── */

    --sg-fg:              var(--sg-p-neutral-900);    /* основной текст */
    --sg-fg-subtle:       var(--sg-p-neutral-600);    /* второстепенный */
    --sg-fg-muted:        var(--sg-p-neutral-400);    /* приглушённый */
    --sg-fg-disabled:     var(--sg-p-neutral-300);    /* недоступный */
    --sg-fg-inverse:      var(--sg-p-neutral-0);      /* на тёмном фоне */
    --sg-fg-link:         var(--sg-p-blue-600);       /* ссылки */
    --sg-fg-link-hover:   var(--sg-p-blue-700);       /* ссылки hover */

    /* ── Границы ────────────────────────────────────────────────────────── */

    --sg-border:          var(--sg-p-neutral-200);
    --sg-border-subtle:   var(--sg-p-neutral-150);
    --sg-border-strong:   var(--sg-p-neutral-300);
    --sg-border-focus:    var(--sg-p-blue-500);
    --sg-divider:         var(--sg-p-neutral-150);
    --sg-border-disabled: var(--sg-p-neutral-200);

    /* ── Основной цвет (Primary/Brand) ──────────────────────────────────── */

    --sg-color-primary:         var(--sg-p-blue-600);
    --sg-color-primary-subtle:  var(--sg-p-blue-50);
    --sg-color-primary-muted:   var(--sg-p-blue-100);
    --sg-color-primary-hover:   var(--sg-p-blue-700);
    --sg-color-primary-active:  var(--sg-p-blue-800);
    --sg-color-primary-fg:      var(--sg-p-neutral-0);   /* текст на primary фоне */

    /* ── Успех (Success) ────────────────────────────────────────────────── */

    --sg-color-success:        var(--sg-p-emerald-500);
    --sg-color-success-subtle: var(--sg-p-emerald-50);
    --sg-color-success-muted:  var(--sg-p-emerald-100);
    --sg-color-success-border: var(--sg-p-emerald-200);
    --sg-color-success-hover:  var(--sg-p-emerald-600);
    --sg-color-success-fg:     var(--sg-p-neutral-0);

    /* ── Опасность (Danger/Error) ────────────────────────────────────────── */

    --sg-color-danger:        var(--sg-p-rose-500);
    --sg-color-danger-subtle: var(--sg-p-rose-50);
    --sg-color-danger-muted:  var(--sg-p-rose-100);
    --sg-color-danger-border: var(--sg-p-rose-200);
    --sg-color-danger-hover:  var(--sg-p-rose-600);
    --sg-color-danger-fg:     var(--sg-p-neutral-0);

    /* ── Предупреждение (Warning) ────────────────────────────────────────── */

    --sg-color-warning:        var(--sg-p-amber-500);
    --sg-color-warning-subtle: var(--sg-p-amber-50);
    --sg-color-warning-muted:  var(--sg-p-amber-100);
    --sg-color-warning-border: var(--sg-p-amber-200);
    --sg-color-warning-hover:  var(--sg-p-amber-600);
    --sg-color-warning-fg:     var(--sg-p-neutral-900);   /* тёмный текст на жёлтом */

    /* ── Информация (Info) ──────────────────────────────────────────────── */

    --sg-color-info:        var(--sg-p-sky-500);
    --sg-color-info-subtle: var(--sg-p-cyan-50);
    --sg-color-info-muted:  var(--sg-p-cyan-100);
    --sg-color-info-border: var(--sg-p-cyan-200);
    --sg-color-info-hover:  var(--sg-p-sky-600);
    --sg-color-info-fg:     var(--sg-p-neutral-0);

    /* ── Нейтральный ────────────────────────────────────────────────────── */

    --sg-color-neutral:        var(--sg-p-neutral-600);
    --sg-color-neutral-subtle: var(--sg-p-neutral-50);
    --sg-color-neutral-muted:  var(--sg-p-neutral-100);
    --sg-color-neutral-border: var(--sg-p-neutral-200);
    --sg-color-neutral-hover:  var(--sg-p-neutral-700);
    --sg-color-neutral-fg:     var(--sg-p-neutral-0);

    /* ── Типографика ────────────────────────────────────────────────────── */

    --sg-font:         var(--sg-p-font-sans);
    --sg-font-mono:    var(--sg-p-font-mono);
    --sg-font-serif:   var(--sg-p-font-serif);

    --sg-text-xs:   var(--sg-p-text-xs);
    --sg-text-sm:   var(--sg-p-text-sm);
    --sg-text-base: var(--sg-p-text-md);
    --sg-text-lg:   var(--sg-p-text-base);
    --sg-text-xl:   var(--sg-p-text-xl);
    --sg-text-2xl:  var(--sg-p-text-2xl);

    --sg-fw-normal:   var(--sg-p-fw-normal);
    --sg-fw-medium:   var(--sg-p-fw-medium);
    --sg-fw-semibold: var(--sg-p-fw-semibold);
    --sg-fw-bold:     var(--sg-p-fw-bold);

    --sg-lh-tight:   var(--sg-p-lh-tight);
    --sg-lh-normal:  var(--sg-p-lh-normal);
    --sg-lh-relaxed: var(--sg-p-lh-relaxed);

    /* ── Пространство ───────────────────────────────────────────────────── */

    --sg-space-1:  var(--sg-p-space-0-5);  /* 2px  */
    --sg-space-2:  var(--sg-p-space-1);    /* 4px  */
    --sg-space-3:  var(--sg-p-space-1-5);  /* 6px  */
    --sg-space-4:  var(--sg-p-space-2);    /* 8px  */
    --sg-space-5:  var(--sg-p-space-2-5);  /* 10px */
    --sg-space-6:  var(--sg-p-space-3);    /* 12px */
    --sg-space-8:  var(--sg-p-space-4);    /* 16px */
    --sg-space-10: var(--sg-p-space-5);    /* 20px */
    --sg-space-12: var(--sg-p-space-6);    /* 24px */
    --sg-space-16: var(--sg-p-space-8);    /* 32px */
    --sg-space-20: var(--sg-p-space-10);   /* 40px */
    --sg-space-24: var(--sg-p-space-12);   /* 48px */

    /* ── Скругления ─────────────────────────────────────────────────────── */

    --sg-radius-none: var(--sg-p-radius-none);
    --sg-radius-xs:   var(--sg-p-radius-xs);
    --sg-radius-sm:   var(--sg-p-radius-sm);
    --sg-radius-md:   var(--sg-p-radius-md);
    --sg-radius-lg:   var(--sg-p-radius-lg);
    --sg-radius-xl:   var(--sg-p-radius-xl);
    --sg-radius-2xl:  var(--sg-p-radius-2xl);
    --sg-radius-full: var(--sg-p-radius-full);

    /* ── Тени ───────────────────────────────────────────────────────────── */

    --sg-shadow-xs:  var(--sg-p-shadow-xs);
    --sg-shadow-sm:  var(--sg-p-shadow-sm);
    --sg-shadow-md:  var(--sg-p-shadow-md);
    --sg-shadow-lg:  var(--sg-p-shadow-lg);
    --sg-shadow-xl:  var(--sg-p-shadow-xl);

    /* ── Z-Index ────────────────────────────────────────────────────────── */

    --sg-z-dropdown: var(--sg-p-z-100);
    --sg-z-sticky:   var(--sg-p-z-200);
    --sg-z-modal:    var(--sg-p-z-300);
    --sg-z-toast:    var(--sg-p-z-400);
    --sg-z-tooltip:  var(--sg-p-z-500);
    --sg-z-overlay:  9999;

    /* ── Переходы ───────────────────────────────────────────────────────── */

    --sg-transition-fast:   var(--sg-p-duration-100) var(--sg-p-ease-out);
    --sg-transition-base:   var(--sg-p-duration-150) var(--sg-p-ease-in-out);
    --sg-transition-slow:   var(--sg-p-duration-300) var(--sg-p-ease-in-out);

    /* ── Focus Ring ─────────────────────────────────────────────────────── */

    --sg-focus-ring: 0 0 0 2px var(--sg-color-primary-subtle),
                     0 0 0 4px var(--sg-color-primary);
    --sg-focus-ring-danger: 0 0 0 2px var(--sg-color-danger-subtle),
                            0 0 0 4px var(--sg-color-danger);
}
```

**Файл: `wwwroot/themes/sg-tokens-semantic-dark.css`**

```css
/* =============================================================================
   SuperUI — Semantic Tokens (Level 1) — DARK MODE
   ============================================================================= */

[data-theme="dark"] {

    /* ── Фон ─────────────────────────────────────────────────────────────── */

    --sg-bg:              var(--sg-p-dark-0);
    --sg-bg-subtle:       var(--sg-p-dark-100);
    --sg-bg-muted:        var(--sg-p-dark-200);
    --sg-bg-emphasized:   var(--sg-p-dark-300);
    --sg-bg-overlay:      rgba(0, 0, 0, 0.75);

    --sg-surface:         var(--sg-p-dark-100);
    --sg-surface-raised:  var(--sg-p-dark-150);
    --sg-surface-overlay: var(--sg-p-dark-150);

    /* ── Текст ──────────────────────────────────────────────────────────── */

    --sg-fg:              var(--sg-p-dark-950);
    --sg-fg-subtle:       var(--sg-p-dark-700);
    --sg-fg-muted:        var(--sg-p-dark-600);
    --sg-fg-disabled:     var(--sg-p-dark-400);
    --sg-fg-inverse:      var(--sg-p-dark-0);
    --sg-fg-link:         var(--sg-p-blue-400);
    --sg-fg-link-hover:   var(--sg-p-blue-300);

    /* ── Границы ────────────────────────────────────────────────────────── */

    --sg-border:          var(--sg-p-dark-200);
    --sg-border-subtle:   var(--sg-p-dark-150);
    --sg-border-strong:   var(--sg-p-dark-400);
    --sg-border-focus:    var(--sg-p-blue-400);
    --sg-divider:         var(--sg-p-dark-150);
    --sg-border-disabled: var(--sg-p-dark-200);

    /* ── Primary ────────────────────────────────────────────────────────── */

    --sg-color-primary:        var(--sg-p-blue-500);
    --sg-color-primary-subtle: rgba(59, 130, 246, 0.12);
    --sg-color-primary-muted:  rgba(59, 130, 246, 0.20);
    --sg-color-primary-hover:  var(--sg-p-blue-400);
    --sg-color-primary-active: var(--sg-p-blue-300);
    --sg-color-primary-fg:     var(--sg-p-neutral-0);

    /* ── Success ────────────────────────────────────────────────────────── */

    --sg-color-success:        var(--sg-p-emerald-500);
    --sg-color-success-subtle: rgba(16, 185, 129, 0.12);
    --sg-color-success-muted:  rgba(16, 185, 129, 0.20);
    --sg-color-success-border: rgba(16, 185, 129, 0.30);
    --sg-color-success-hover:  var(--sg-p-emerald-400);
    --sg-color-success-fg:     var(--sg-p-neutral-0);

    /* ── Danger ─────────────────────────────────────────────────────────── */

    --sg-color-danger:        var(--sg-p-rose-500);
    --sg-color-danger-subtle: rgba(244, 63, 94, 0.12);
    --sg-color-danger-muted:  rgba(244, 63, 94, 0.20);
    --sg-color-danger-border: rgba(244, 63, 94, 0.30);
    --sg-color-danger-hover:  var(--sg-p-rose-400);
    --sg-color-danger-fg:     var(--sg-p-neutral-0);

    /* ── Warning ─────────────────────────────────────────────────────────── */

    --sg-color-warning:        var(--sg-p-amber-500);
    --sg-color-warning-subtle: rgba(245, 158, 11, 0.12);
    --sg-color-warning-muted:  rgba(245, 158, 11, 0.20);
    --sg-color-warning-border: rgba(245, 158, 11, 0.30);
    --sg-color-warning-hover:  var(--sg-p-amber-400);
    --sg-color-warning-fg:     var(--sg-p-neutral-900);

    /* ── Info ───────────────────────────────────────────────────────────── */

    --sg-color-info:        var(--sg-p-sky-400);
    --sg-color-info-subtle: rgba(14, 165, 233, 0.12);
    --sg-color-info-muted:  rgba(14, 165, 233, 0.20);
    --sg-color-info-border: rgba(14, 165, 233, 0.30);
    --sg-color-info-hover:  var(--sg-p-sky-500);
    --sg-color-info-fg:     var(--sg-p-neutral-0);

    /* ── Тени (dark mode) ───────────────────────────────────────────────── */

    --sg-shadow-xs: var(--sg-p-shadow-dark-xs);
    --sg-shadow-sm: var(--sg-p-shadow-dark-sm);
    --sg-shadow-md: var(--sg-p-shadow-dark-md);
    --sg-shadow-lg: var(--sg-p-shadow-dark-lg);
    --sg-shadow-xl: var(--sg-p-shadow-dark-xl);

    /* ── Focus Ring ─────────────────────────────────────────────────────── */

    --sg-focus-ring: 0 0 0 2px rgba(59, 130, 246, 0.20),
                     0 0 0 4px var(--sg-color-primary);
    --sg-focus-ring-danger: 0 0 0 2px rgba(244, 63, 94, 0.20),
                            0 0 0 4px var(--sg-color-danger);
}
```

---

## 7. Уровень 2 — Компонентные токены (Component)

**Файл: `wwwroot/themes/sg-tokens-component.css`**

```css
/* =============================================================================
   SuperUI — Component Tokens (Level 2)
   Все значения ссылаются на Semantic-токены (--sg-*).
   ============================================================================= */

:root {

    /* ════════════════════════════════════════════════════════════════════
       BUTTON
       ════════════════════════════════════════════════════════════════════ */

    /* Default */
    --sgc-btn-bg:             var(--sg-surface);
    --sgc-btn-border:         var(--sg-border-strong);
    --sgc-btn-fg:             var(--sg-fg);
    --sgc-btn-bg-hover:       var(--sg-bg-muted);
    --sgc-btn-border-hover:   var(--sg-border-strong);
    --sgc-btn-shadow:         var(--sg-shadow-xs);
    --sgc-btn-radius:         var(--sg-radius-md);
    --sgc-btn-font-size:      var(--sg-text-sm);
    --sgc-btn-font-weight:    var(--sg-fw-medium);
    --sgc-btn-padding-x:      var(--sg-space-6);
    --sgc-btn-padding-y:      var(--sg-space-3);
    --sgc-btn-height:         2rem;        /* 32px */
    --sgc-btn-height-sm:      1.625rem;    /* 26px */
    --sgc-btn-height-lg:      2.375rem;    /* 38px */
    --sgc-btn-transition:     background var(--sg-transition-fast),
                              border-color var(--sg-transition-fast),
                              color var(--sg-transition-fast),
                              box-shadow var(--sg-transition-fast);

    /* Primary */
    --sgc-btn-primary-bg:           var(--sg-color-primary);
    --sgc-btn-primary-border:       var(--sg-color-primary);
    --sgc-btn-primary-fg:           var(--sg-color-primary-fg);
    --sgc-btn-primary-bg-hover:     var(--sg-color-primary-hover);
    --sgc-btn-primary-border-hover: var(--sg-color-primary-hover);
    --sgc-btn-primary-bg-active:    var(--sg-color-primary-active);

    /* Danger */
    --sgc-btn-danger-bg:            var(--sg-color-danger);
    --sgc-btn-danger-border:        var(--sg-color-danger);
    --sgc-btn-danger-fg:            var(--sg-color-danger-fg);
    --sgc-btn-danger-bg-hover:      var(--sg-color-danger-hover);

    /* Success */
    --sgc-btn-success-bg:           var(--sg-color-success);
    --sgc-btn-success-border:       var(--sg-color-success);
    --sgc-btn-success-fg:           var(--sg-color-success-fg);
    --sgc-btn-success-bg-hover:     var(--sg-color-success-hover);

    /* Ghost */
    --sgc-btn-ghost-bg:             transparent;
    --sgc-btn-ghost-border:         transparent;
    --sgc-btn-ghost-fg:             var(--sg-fg-subtle);
    --sgc-btn-ghost-bg-hover:       var(--sg-bg-muted);
    --sgc-btn-ghost-fg-hover:       var(--sg-fg);

    /* Outlined */
    --sgc-btn-outlined-bg:          transparent;
    --sgc-btn-outlined-border:      var(--sg-color-primary);
    --sgc-btn-outlined-fg:          var(--sg-color-primary);
    --sgc-btn-outlined-bg-hover:    var(--sg-color-primary-subtle);

    /* Dashed */
    --sgc-btn-dashed-border-style:  dashed;

    /* Disabled */
    --sgc-btn-disabled-opacity: 0.45;

    /* ════════════════════════════════════════════════════════════════════
       INPUT / TEXTBOX
       ════════════════════════════════════════════════════════════════════ */

    --sgc-input-bg:               var(--sg-bg);
    --sgc-input-border:           var(--sg-border);
    --sgc-input-border-hover:     var(--sg-border-strong);
    --sgc-input-border-focus:     var(--sg-color-primary);
    --sgc-input-border-disabled:  var(--sg-border-disabled);
    --sgc-input-border-invalid:   var(--sg-color-danger);
    --sgc-input-fg:               var(--sg-fg);
    --sgc-input-placeholder:      var(--sg-fg-muted);
    --sgc-input-disabled-bg:      var(--sg-bg-muted);
    --sgc-input-disabled-fg:      var(--sg-fg-disabled);
    --sgc-input-radius:           var(--sg-radius-md);
    --sgc-input-padding-x:        var(--sg-space-4);
    --sgc-input-padding-y:        var(--sg-space-3);
    --sgc-input-font-size:        var(--sg-text-sm);
    --sgc-input-height:           2rem;       /* 32px */
    --sgc-input-height-sm:        1.625rem;   /* 26px */
    --sgc-input-height-lg:        2.375rem;   /* 38px */
    --sgc-input-focus-ring:       var(--sg-focus-ring);
    --sgc-input-shadow:           var(--sg-shadow-xs);
    --sgc-input-adornment-fg:     var(--sg-fg-muted);

    /* ════════════════════════════════════════════════════════════════════
       SELECT / COMBOBOX / AUTOCOMPLETE / MULTISELECT
       ════════════════════════════════════════════════════════════════════ */

    --sgc-select-bg:              var(--sgc-input-bg);
    --sgc-select-border:          var(--sgc-input-border);
    --sgc-select-border-focus:    var(--sgc-input-border-focus);
    --sgc-select-fg:              var(--sgc-input-fg);
    --sgc-select-radius:          var(--sgc-input-radius);
    --sgc-select-height:          var(--sgc-input-height);
    --sgc-select-dropdown-bg:     var(--sg-surface-overlay);
    --sgc-select-dropdown-border: var(--sg-border);
    --sgc-select-dropdown-shadow: var(--sg-shadow-lg);
    --sgc-select-option-hover-bg: var(--sg-bg-muted);
    --sgc-select-option-selected-bg:  var(--sg-color-primary-subtle);
    --sgc-select-option-selected-fg:  var(--sg-color-primary);
    --sgc-select-tag-bg:          var(--sg-bg-muted);
    --sgc-select-tag-border:      var(--sg-border);
    --sgc-select-tag-fg:          var(--sg-fg);

    /* ════════════════════════════════════════════════════════════════════
       CHECKBOX / SWITCH / RADIO
       ════════════════════════════════════════════════════════════════════ */

    --sgc-check-bg:               var(--sg-bg);
    --sgc-check-border:           var(--sg-border-strong);
    --sgc-check-border-focus:     var(--sg-color-primary);
    --sgc-check-checked-bg:       var(--sg-color-primary);
    --sgc-check-checked-border:   var(--sg-color-primary);
    --sgc-check-checked-mark:     var(--sg-color-primary-fg);
    --sgc-check-radius:           var(--sg-radius-xs);
    --sgc-check-size:             1rem;    /* 16px */
    --sgc-check-size-sm:          0.8125rem;
    --sgc-check-size-lg:          1.25rem;

    --sgc-radio-bg:               var(--sgc-check-bg);
    --sgc-radio-border:           var(--sgc-check-border);
    --sgc-radio-checked-bg:       var(--sg-color-primary);
    --sgc-radio-dot:              var(--sg-color-primary-fg);

    --sgc-switch-track-bg:        var(--sg-bg-emphasized);
    --sgc-switch-track-checked:   var(--sg-color-primary);
    --sgc-switch-thumb:           var(--sg-p-neutral-0);
    --sgc-switch-shadow:          var(--sg-shadow-xs);
    --sgc-switch-width:           2.25rem;   /* 36px */
    --sgc-switch-height:          1.25rem;   /* 20px */

    /* ════════════════════════════════════════════════════════════════════
       CARD
       ════════════════════════════════════════════════════════════════════ */

    --sgc-card-bg:                var(--sg-surface);
    --sgc-card-border:            var(--sg-border);
    --sgc-card-shadow:            var(--sg-shadow-sm);
    --sgc-card-radius:            var(--sg-radius-xl);
    --sgc-card-padding:           var(--sg-space-8);
    --sgc-card-header-fg:         var(--sg-fg);
    --sgc-card-body-fg:           var(--sg-fg-subtle);
    --sgc-card-hover-shadow:      var(--sg-shadow-md);
    --sgc-card-hover-border:      var(--sg-border-strong);

    /* ════════════════════════════════════════════════════════════════════
       MODAL / DRAWER
       ════════════════════════════════════════════════════════════════════ */

    --sgc-modal-bg:               var(--sg-surface-overlay);
    --sgc-modal-border:           var(--sg-border);
    --sgc-modal-shadow:           var(--sg-shadow-xl);
    --sgc-modal-radius:           var(--sg-radius-2xl);
    --sgc-modal-overlay:          var(--sg-bg-overlay);
    --sgc-modal-header-fg:        var(--sg-fg);
    --sgc-modal-padding:          var(--sg-space-8);
    --sgc-modal-header-border:    var(--sg-border);
    --sgc-modal-footer-border:    var(--sg-border);
    --sgc-modal-footer-bg:        var(--sg-bg-subtle);

    /* ════════════════════════════════════════════════════════════════════
       TABLE / DATA GRID
       ════════════════════════════════════════════════════════════════════ */

    --sgc-table-bg:               var(--sg-surface);
    --sgc-table-border:           var(--sg-border);
    --sgc-table-radius:           var(--sg-radius-lg);
    --sgc-table-header-bg:        var(--sg-bg-subtle);
    --sgc-table-header-fg:        var(--sg-fg-subtle);
    --sgc-table-header-border:    var(--sg-border-strong);
    --sgc-table-header-font-weight: var(--sg-fw-semibold);
    --sgc-table-header-font-size: var(--sg-text-xs);
    --sgc-table-row-even-bg:      var(--sg-bg-subtle);
    --sgc-table-row-odd-bg:       var(--sg-surface);
    --sgc-table-row-hover-bg:     var(--sg-color-primary-subtle);
    --sgc-table-row-selected-bg:  var(--sg-color-primary-muted);
    --sgc-table-row-selected-border: var(--sg-color-primary);
    --sgc-table-cell-padding-x:   var(--sg-space-6);
    --sgc-table-cell-padding-y:   var(--sg-space-3);
    --sgc-table-font-size:        var(--sg-text-sm);
    --sgc-table-pinned-shadow:    2px 0 5px rgba(0, 0, 0, 0.08);

    /* ════════════════════════════════════════════════════════════════════
       ALERT / TOAST / NOTIFICATION
       ════════════════════════════════════════════════════════════════════ */

    --sgc-alert-radius:           var(--sg-radius-lg);
    --sgc-alert-padding:          var(--sg-space-4) var(--sg-space-6);
    --sgc-alert-border-width:     var(--sg-p-border-1);
    --sgc-alert-font-size:        var(--sg-text-sm);

    --sgc-alert-success-bg:       var(--sg-color-success-subtle);
    --sgc-alert-success-border:   var(--sg-color-success-border);
    --sgc-alert-success-fg:       var(--sg-color-success-hover);

    --sgc-alert-danger-bg:        var(--sg-color-danger-subtle);
    --sgc-alert-danger-border:    var(--sg-color-danger-border);
    --sgc-alert-danger-fg:        var(--sg-color-danger-hover);

    --sgc-alert-warning-bg:       var(--sg-color-warning-subtle);
    --sgc-alert-warning-border:   var(--sg-color-warning-border);
    --sgc-alert-warning-fg:       var(--sg-color-warning-hover);

    --sgc-alert-info-bg:          var(--sg-color-info-subtle);
    --sgc-alert-info-border:      var(--sg-color-info-border);
    --sgc-alert-info-fg:          var(--sg-color-info-hover);

    /* ════════════════════════════════════════════════════════════════════
       BADGE / CHIP / TAG
       ════════════════════════════════════════════════════════════════════ */

    --sgc-badge-radius:           var(--sg-radius-full);
    --sgc-badge-padding-x:        var(--sg-space-3);
    --sgc-badge-padding-y:        var(--sg-space-1);
    --sgc-badge-font-size:        var(--sg-text-xs);
    --sgc-badge-font-weight:      var(--sg-fw-medium);

    /* ════════════════════════════════════════════════════════════════════
       TABS
       ════════════════════════════════════════════════════════════════════ */

    --sgc-tabs-border:            var(--sg-border);
    --sgc-tabs-item-fg:           var(--sg-fg-subtle);
    --sgc-tabs-item-fg-active:    var(--sg-color-primary);
    --sgc-tabs-item-bg-hover:     var(--sg-bg-muted);
    --sgc-tabs-indicator:         var(--sg-color-primary);
    --sgc-tabs-indicator-height:  2px;
    --sgc-tabs-font-size:         var(--sg-text-sm);
    --sgc-tabs-font-weight:       var(--sg-fw-medium);

    /* ════════════════════════════════════════════════════════════════════
       MENU / DROPDOWN
       ════════════════════════════════════════════════════════════════════ */

    --sgc-menu-bg:                var(--sg-surface-overlay);
    --sgc-menu-border:            var(--sg-border);
    --sgc-menu-shadow:            var(--sg-shadow-lg);
    --sgc-menu-radius:            var(--sg-radius-lg);
    --sgc-menu-item-fg:           var(--sg-fg);
    --sgc-menu-item-fg-subtle:    var(--sg-fg-subtle);
    --sgc-menu-item-bg-hover:     var(--sg-bg-muted);
    --sgc-menu-item-bg-active:    var(--sg-color-primary-subtle);
    --sgc-menu-item-fg-active:    var(--sg-color-primary);
    --sgc-menu-item-padding-x:    var(--sg-space-4);
    --sgc-menu-item-padding-y:    var(--sg-space-3);
    --sgc-menu-separator:         var(--sg-border);
    --sgc-menu-font-size:         var(--sg-text-sm);

    /* ════════════════════════════════════════════════════════════════════
       TOOLTIP / POPOVER
       ════════════════════════════════════════════════════════════════════ */

    --sgc-tooltip-bg:             rgba(17, 24, 39, 0.95);
    --sgc-tooltip-fg:             var(--sg-p-neutral-50);
    --sgc-tooltip-radius:         var(--sg-radius-md);
    --sgc-tooltip-shadow:         var(--sg-shadow-md);
    --sgc-tooltip-font-size:      var(--sg-text-xs);
    --sgc-tooltip-padding-x:      var(--sg-space-4);
    --sgc-tooltip-padding-y:      var(--sg-space-2);
    --sgc-tooltip-max-width:      200px;

    --sgc-popover-bg:             var(--sg-surface-overlay);
    --sgc-popover-border:         var(--sg-border);
    --sgc-popover-shadow:         var(--sg-shadow-xl);
    --sgc-popover-radius:         var(--sg-radius-xl);
    --sgc-popover-padding:        var(--sg-space-6);

    /* ════════════════════════════════════════════════════════════════════
       PROGRESS / SLIDER
       ════════════════════════════════════════════════════════════════════ */

    --sgc-progress-track-bg:      var(--sg-bg-emphasized);
    --sgc-progress-fill:          var(--sg-color-primary);
    --sgc-progress-height:        4px;
    --sgc-progress-radius:        var(--sg-radius-full);

    --sgc-slider-track:           var(--sg-bg-emphasized);
    --sgc-slider-fill:            var(--sg-color-primary);
    --sgc-slider-thumb:           var(--sg-p-neutral-0);
    --sgc-slider-thumb-border:    var(--sg-color-primary);
    --sgc-slider-thumb-shadow:    var(--sg-shadow-sm);

    /* ════════════════════════════════════════════════════════════════════
       AVATAR
       ════════════════════════════════════════════════════════════════════ */

    --sgc-avatar-bg:              var(--sg-color-primary-subtle);
    --sgc-avatar-fg:              var(--sg-color-primary);
    --sgc-avatar-border:          var(--sg-surface);
    --sgc-avatar-size-sm:         1.5rem;    /* 24px */
    --sgc-avatar-size-md:         2rem;      /* 32px */
    --sgc-avatar-size-lg:         2.5rem;    /* 40px */
    --sgc-avatar-size-xl:         3rem;      /* 48px */
    --sgc-avatar-radius:          var(--sg-radius-full);
    --sgc-avatar-font-weight:     var(--sg-fw-semibold);

    /* ════════════════════════════════════════════════════════════════════
       DATE PICKER / TIME PICKER / CALENDAR
       ════════════════════════════════════════════════════════════════════ */

    --sgc-datepicker-bg:          var(--sg-surface-overlay);
    --sgc-datepicker-border:      var(--sg-border);
    --sgc-datepicker-shadow:      var(--sg-shadow-lg);
    --sgc-datepicker-radius:      var(--sg-radius-xl);
    --sgc-datepicker-cell-radius: var(--sg-radius-md);
    --sgc-datepicker-today-border: var(--sg-color-primary);
    --sgc-datepicker-selected-bg: var(--sg-color-primary);
    --sgc-datepicker-selected-fg: var(--sg-color-primary-fg);
    --sgc-datepicker-hover-bg:    var(--sg-bg-muted);
    --sgc-datepicker-weekend-fg:  var(--sg-color-danger);
    --sgc-datepicker-disabled-fg: var(--sg-fg-disabled);

    /* ════════════════════════════════════════════════════════════════════
       DRAWER / SPLITTER / DOCK
       ════════════════════════════════════════════════════════════════════ */

    --sgc-drawer-bg:              var(--sg-surface-overlay);
    --sgc-drawer-shadow:          var(--sg-shadow-xl);
    --sgc-drawer-overlay:         var(--sg-bg-overlay);
    --sgc-drawer-header-border:   var(--sg-border);
    --sgc-drawer-footer-border:   var(--sg-border);

    --sgc-splitter-handle:        var(--sg-border-strong);
    --sgc-splitter-handle-hover:  var(--sg-color-primary);
    --sgc-splitter-handle-size:   4px;

    /* ════════════════════════════════════════════════════════════════════
       BREADCRUMB / PAGINATION / STEPPER
       ════════════════════════════════════════════════════════════════════ */

    --sgc-breadcrumb-fg:          var(--sg-fg-subtle);
    --sgc-breadcrumb-separator:   var(--sg-fg-muted);
    --sgc-breadcrumb-active-fg:   var(--sg-fg);
    --sgc-breadcrumb-link-fg:     var(--sg-fg-link);

    --sgc-pagination-bg:          var(--sg-surface);
    --sgc-pagination-border:      var(--sg-border);
    --sgc-pagination-fg:          var(--sg-fg);
    --sgc-pagination-active-bg:   var(--sg-color-primary);
    --sgc-pagination-active-fg:   var(--sg-color-primary-fg);
    --sgc-pagination-hover-bg:    var(--sg-bg-muted);
    --sgc-pagination-radius:      var(--sg-radius-md);
    --sgc-pagination-size:        2rem;

    --sgc-stepper-line:           var(--sg-border-strong);
    --sgc-stepper-step-bg:        var(--sg-bg-muted);
    --sgc-stepper-step-fg:        var(--sg-fg-muted);
    --sgc-stepper-step-active-bg: var(--sg-color-primary);
    --sgc-stepper-step-active-fg: var(--sg-color-primary-fg);
    --sgc-stepper-step-done-bg:   var(--sg-color-success);
    --sgc-stepper-step-done-fg:   var(--sg-color-success-fg);
    --sgc-stepper-size:           2rem;

    /* ════════════════════════════════════════════════════════════════════
       TREE VIEW / ACCORDION
       ════════════════════════════════════════════════════════════════════ */

    --sgc-tree-item-fg:           var(--sg-fg);
    --sgc-tree-item-bg-hover:     var(--sg-bg-muted);
    --sgc-tree-item-bg-selected:  var(--sg-color-primary-subtle);
    --sgc-tree-item-fg-selected:  var(--sg-color-primary);
    --sgc-tree-indent:            var(--sg-space-6);
    --sgc-tree-icon-fg:           var(--sg-fg-muted);

    --sgc-accordion-border:       var(--sg-border);
    --sgc-accordion-header-bg:    var(--sg-bg-subtle);
    --sgc-accordion-header-fg:    var(--sg-fg);
    --sgc-accordion-header-hover: var(--sg-bg-muted);
    --sgc-accordion-body-bg:      var(--sg-surface);
    --sgc-accordion-radius:       var(--sg-radius-lg);

    /* ════════════════════════════════════════════════════════════════════
       TIMELINE / FEED
       ════════════════════════════════════════════════════════════════════ */

    --sgc-timeline-line:          var(--sg-border-strong);
    --sgc-timeline-dot-bg:        var(--sg-color-primary);
    --sgc-timeline-dot-border:    var(--sg-surface);
    --sgc-timeline-dot-size:      0.75rem;

    /* ════════════════════════════════════════════════════════════════════
       STATISTIC / KPI CARD
       ════════════════════════════════════════════════════════════════════ */

    --sgc-stat-value-fg:          var(--sg-fg);
    --sgc-stat-label-fg:          var(--sg-fg-subtle);
    --sgc-stat-positive-fg:       var(--sg-color-success);
    --sgc-stat-negative-fg:       var(--sg-color-danger);

    /* ════════════════════════════════════════════════════════════════════
       LOADER / SPINNER / SKELETON
       ════════════════════════════════════════════════════════════════════ */

    --sgc-spinner-color:          var(--sg-color-primary);
    --sgc-spinner-track:          var(--sg-border);

    --sgc-skeleton-bg:            var(--sg-bg-muted);
    --sgc-skeleton-shimmer:       var(--sg-bg-subtle);

    /* ════════════════════════════════════════════════════════════════════
       EMPTY STATE / RESULT
       ════════════════════════════════════════════════════════════════════ */

    --sgc-empty-icon-fg:          var(--sg-fg-muted);
    --sgc-empty-title-fg:         var(--sg-fg-subtle);
    --sgc-empty-desc-fg:          var(--sg-fg-muted);

    /* ════════════════════════════════════════════════════════════════════
       FORM / LABEL
       ════════════════════════════════════════════════════════════════════ */

    --sgc-form-label-fg:          var(--sg-fg-subtle);
    --sgc-form-label-font-size:   var(--sg-text-sm);
    --sgc-form-label-font-weight: var(--sg-fw-medium);
    --sgc-form-label-required:    var(--sg-color-danger);
    --sgc-form-hint-fg:           var(--sg-fg-muted);
    --sgc-form-hint-font-size:    var(--sg-text-xs);
    --sgc-form-error-fg:          var(--sg-color-danger);
    --sgc-form-error-font-size:   var(--sg-text-xs);
    --sgc-form-gap:               var(--sg-space-6);
    --sgc-form-row-gap:           var(--sg-space-8);

    /* ════════════════════════════════════════════════════════════════════
       SCROLLBAR
       ════════════════════════════════════════════════════════════════════ */

    --sgc-scrollbar-width:        6px;
    --sgc-scrollbar-thumb:        var(--sg-border-strong);
    --sgc-scrollbar-thumb-hover:  var(--sg-fg-muted);
    --sgc-scrollbar-track:        transparent;

    /* ════════════════════════════════════════════════════════════════════
       CODE EDITOR (Monaco)
       ════════════════════════════════════════════════════════════════════ */

    --sgc-monaco-bg:              var(--sg-surface);
    --sgc-monaco-border:          var(--sg-border);
    --sgc-monaco-radius:          var(--sg-radius-lg);
    --sgc-monaco-font:            var(--sg-font-mono);
    --sgc-monaco-font-size:       var(--sg-text-sm);

    /* ════════════════════════════════════════════════════════════════════
       KANBAN
       ════════════════════════════════════════════════════════════════════ */

    --sgc-kanban-column-bg:       var(--sg-bg-subtle);
    --sgc-kanban-column-border:   var(--sg-border);
    --sgc-kanban-column-radius:   var(--sg-radius-xl);
    --sgc-kanban-card-bg:         var(--sg-surface);
    --sgc-kanban-card-border:     var(--sg-border);
    --sgc-kanban-card-shadow:     var(--sg-shadow-xs);
    --sgc-kanban-card-radius:     var(--sg-radius-lg);
    --sgc-kanban-card-hover-shadow: var(--sg-shadow-md);

    /* ════════════════════════════════════════════════════════════════════
       GANTT / SCHEDULER / CALENDAR
       ════════════════════════════════════════════════════════════════════ */

    --sgc-gantt-header-bg:        var(--sg-bg-subtle);
    --sgc-gantt-grid-line:        var(--sg-border);
    --sgc-gantt-bar-bg:           var(--sg-color-primary);
    --sgc-gantt-bar-fg:           var(--sg-color-primary-fg);
    --sgc-gantt-milestone:        var(--sg-color-danger);
    --sgc-gantt-today-line:       var(--sg-color-danger);

    --sgc-scheduler-event-bg:     var(--sg-color-primary-subtle);
    --sgc-scheduler-event-border: var(--sg-color-primary);
    --sgc-scheduler-event-fg:     var(--sg-color-primary);
    --sgc-scheduler-time-bg:      var(--sg-bg-subtle);
    --sgc-scheduler-time-border:  var(--sg-border);
}
```

---

## 8. Интерфейсы C# для тем

**Файл: `Themes/IThemePrimitives.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Примитивные значения темы — цветовые шкалы, размеры, типографика.
/// Не используются напрямую в компонентах.
/// </summary>
public interface IThemePrimitives
{
    // Нейтральные цвета (шкала 0-1000)
    string Neutral0 { get; }
    string Neutral50 { get; }
    string Neutral100 { get; }
    string Neutral200 { get; }
    string Neutral300 { get; }
    string Neutral400 { get; }
    string Neutral500 { get; }
    string Neutral600 { get; }
    string Neutral700 { get; }
    string Neutral800 { get; }
    string Neutral900 { get; }

    // Основной цвет (шкала)
    string Primary50 { get; }
    string Primary100 { get; }
    string Primary200 { get; }
    string Primary300 { get; }
    string Primary400 { get; }
    string Primary500 { get; }
    string Primary600 { get; }
    string Primary700 { get; }
    string Primary800 { get; }
    string Primary900 { get; }

    // Успех
    string Success50 { get; }
    string Success100 { get; }
    string Success500 { get; }
    string Success600 { get; }
    string Success700 { get; }

    // Опасность
    string Danger50 { get; }
    string Danger100 { get; }
    string Danger500 { get; }
    string Danger600 { get; }
    string Danger700 { get; }

    // Предупреждение
    string Warning50 { get; }
    string Warning100 { get; }
    string Warning500 { get; }
    string Warning600 { get; }

    // Информация
    string Info50 { get; }
    string Info100 { get; }
    string Info500 { get; }
    string Info600 { get; }

    // Шрифты
    string FontSans { get; }
    string FontMono { get; }
    string FontSerif { get; }

    // Скругления
    string RadiusNone { get; }
    string RadiusXs { get; }
    string RadiusSm { get; }
    string RadiusMd { get; }
    string RadiusLg { get; }
    string RadiusXl { get; }
    string Radius2Xl { get; }
    string RadiusFull { get; }
}
```

**Файл: `Themes/IThemeSemantic.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Семантические токены темы — абстракции над примитивами.
/// "Что это значит" вместо "какое это значение".
/// </summary>
public interface IThemeSemantic
{
    // Фон
    string BgDefault { get; }
    string BgSubtle { get; }
    string BgMuted { get; }
    string BgEmphasized { get; }
    string BgOverlay { get; }

    // Surface
    string Surface { get; }
    string SurfaceRaised { get; }
    string SurfaceOverlay { get; }

    // Текст
    string FgDefault { get; }
    string FgSubtle { get; }
    string FgMuted { get; }
    string FgDisabled { get; }
    string FgInverse { get; }
    string FgLink { get; }
    string FgLinkHover { get; }

    // Границы
    string BorderDefault { get; }
    string BorderSubtle { get; }
    string BorderStrong { get; }
    string BorderFocus { get; }

    // Primary
    string ColorPrimary { get; }
    string ColorPrimarySubtle { get; }
    string ColorPrimaryMuted { get; }
    string ColorPrimaryHover { get; }
    string ColorPrimaryActive { get; }
    string ColorPrimaryFg { get; }   // текст на primary фоне

    // Success
    string ColorSuccess { get; }
    string ColorSuccessSubtle { get; }
    string ColorSuccessHover { get; }
    string ColorSuccessFg { get; }

    // Danger
    string ColorDanger { get; }
    string ColorDangerSubtle { get; }
    string ColorDangerHover { get; }
    string ColorDangerFg { get; }

    // Warning
    string ColorWarning { get; }
    string ColorWarningSubtle { get; }
    string ColorWarningHover { get; }
    string ColorWarningFg { get; }

    // Info
    string ColorInfo { get; }
    string ColorInfoSubtle { get; }
    string ColorInfoHover { get; }
    string ColorInfoFg { get; }

    // Шрифт
    string Font { get; }
    string FontMono { get; }
    string TextSm { get; }
    string TextBase { get; }
    string TextLg { get; }

    // Тени
    string ShadowXs { get; }
    string ShadowSm { get; }
    string ShadowMd { get; }
    string ShadowLg { get; }
    string ShadowXl { get; }

    // Скругления (пробрасываются из Primitives)
    string RadiusSm { get; }
    string RadiusMd { get; }
    string RadiusLg { get; }
    string RadiusXl { get; }
    string RadiusFull { get; }

    // Переходы
    string TransitionFast { get; }
    string TransitionBase { get; }
    string TransitionSlow { get; }

    // Focus ring
    string FocusRing { get; }
    string FocusRingDanger { get; }

    // Z-Index
    int ZDropdown { get; }
    int ZSticky { get; }
    int ZModal { get; }
    int ZToast { get; }
    int ZTooltip { get; }
}
```

**Файл: `Themes/IThemeComponents.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Компонентные токены — специфические переопределения для каждого компонента.
/// </summary>
public interface IThemeComponents
{
    // Button
    string BtnRadius { get; }
    string BtnFontSize { get; }
    string BtnFontWeight { get; }
    string BtnHeight { get; }
    string BtnHeightSm { get; }
    string BtnHeightLg { get; }

    // Input
    string InputRadius { get; }
    string InputFontSize { get; }
    string InputHeight { get; }
    string InputHeightSm { get; }
    string InputHeightLg { get; }

    // Card
    string CardRadius { get; }
    string CardPadding { get; }

    // Modal
    string ModalRadius { get; }

    // Table
    string TableRadius { get; }
    string TableHeaderFontWeight { get; }

    // Tabs
    string TabsIndicatorHeight { get; }

    // Tooltip
    string TooltipMaxWidth { get; }
}
```

**Файл: `Themes/IThemeDefinition.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Главный интерфейс темы SuperUI.
/// Реализуйте этот интерфейс для создания полной кастомной темы.
/// </summary>
public interface IThemeDefinition
{
    /// <summary>Уникальный идентификатор темы.</summary>
    string Id { get; }

    /// <summary>Отображаемое имя.</summary>
    string Name { get; }

    /// <summary>Описание темы.</summary>
    string? Description { get; }

    /// <summary>Имя автора темы.</summary>
    string? Author { get; }

    /// <summary>Версия темы.</summary>
    string Version { get; }

    /// <summary>Примитивные токены.</summary>
    IThemePrimitives Primitives { get; }

    /// <summary>Семантические токены для светлого режима.</summary>
    IThemeSemantic Light { get; }

    /// <summary>
    /// Семантические токены для тёмного режима.
    /// Если null, тёмный режим не поддерживается.
    /// </summary>
    IThemeSemantic? Dark { get; }

    /// <summary>Компонентные переопределения.</summary>
    IThemeComponents? Components { get; }

    /// <summary>Дополнительный CSS для этой темы.</summary>
    string? AdditionalCss { get; }

    /// <summary>
    /// Генерирует полный CSS с CSS Custom Properties.
    /// Используется ThemeGenerator для внедрения в DOM.
    /// </summary>
    string GenerateCss();
}
```

---

## 9. Реализация тем

### 9.1 Default (текущая тема)

**Файл: `Themes/DefaultTheme.cs`**

```csharp
using SuperUI.Themes.Models;

namespace SuperUI.Themes;

/// <summary>
/// Default тема SuperUI — сохраняет обратную совместимость с текущими
/// компонентами и CSS-переменными --sui-* / --sg-*.
/// </summary>
public sealed class DefaultTheme : ThemeBase
{
    public override string Id => "superui-default";
    public override string Name => "SuperUI Default";
    public override string Description => "Стандартная тема SuperUI с поддержкой light/dark.";
    public override string Author => "SuperUI Team";
    public override string Version => "2.0.0";

    protected override IThemePrimitives CreatePrimitives() => new DefaultPrimitives();
    protected override IThemeSemantic CreateLight()       => new DefaultSemanticLight();
    protected override IThemeSemantic CreateDark()        => new DefaultSemanticDark();
    protected override IThemeComponents CreateComponents() => new DefaultComponents();

    // Дополнительный CSS: алиасы --sui-* для обратной совместимости
    public override string? AdditionalCss => """
        /* Backward-compat aliases: --sui-* → --sg-* */
        :root,
        [data-theme="light"],
        [data-theme="dark"] {
            --sui-bg-primary:   var(--sg-bg);
            --sui-bg-secondary: var(--sg-bg-subtle);
            --sui-bg-tertiary:  var(--sg-bg-muted);

            --sui-text-primary:   var(--sg-fg);
            --sui-text-secondary: var(--sg-fg-subtle);
            --sui-text-muted:     var(--sg-fg-muted);
            --sui-text-disabled:  var(--sg-fg-disabled);

            --sui-border:       var(--sg-border);
            --sui-border-hover: var(--sg-border-strong);
            --sui-border-focus: var(--sg-border-focus);

            --sui-accent:        var(--sg-color-primary);
            --sui-accent-hover:  var(--sg-color-primary-hover);
            --sui-accent-active: var(--sg-color-primary-active);

            --sui-success:        var(--sg-color-success);
            --sui-success-bg:     var(--sg-color-success-subtle);
            --sui-success-border: var(--sg-color-success-border, var(--sg-color-success-subtle));

            --sui-danger:        var(--sg-color-danger);
            --sui-danger-bg:     var(--sg-color-danger-subtle);
            --sui-danger-border: var(--sg-color-danger-border, var(--sg-color-danger-subtle));

            --sui-warn:        var(--sg-color-warning);
            --sui-warn-bg:     var(--sg-color-warning-subtle);
            --sui-warn-border: var(--sg-color-warning-border, var(--sg-color-warning-subtle));

            --sui-info:        var(--sg-color-info);
            --sui-info-bg:     var(--sg-color-info-subtle);
            --sui-info-border: var(--sg-color-info-border, var(--sg-color-info-subtle));

            --sui-shadow-sm: var(--sg-shadow-sm);
            --sui-shadow-md: var(--sg-shadow-md);
            --sui-shadow-lg: var(--sg-shadow-lg);

            --sui-overlay-bg: var(--sg-bg-overlay);

            --sui-input-bg:             var(--sgc-input-bg);
            --sui-input-border:         var(--sgc-input-border);
            --sui-input-focus-border:   var(--sgc-input-border-focus);
            --sui-input-disabled-bg:    var(--sgc-input-disabled-bg);

            --sui-card-bg:     var(--sgc-card-bg);
            --sui-card-border: var(--sgc-card-border);

            --sui-hover-bg:    rgba(0, 0, 0, 0.03);
            --sui-active-bg:   rgba(0, 0, 0, 0.06);
            --sui-selected-bg: var(--sg-color-primary-subtle);

            --sui-font-family:   var(--sg-font);
            --sui-font-size-xs:   var(--sg-text-xs);
            --sui-font-size-sm:   var(--sg-text-sm);
            --sui-font-size-base: var(--sg-text-base);
            --sui-font-size-lg:   var(--sg-text-lg);

            --sui-radius-sm:   var(--sg-radius-sm);
            --sui-radius-md:   var(--sg-radius-md);
            --sui-radius-lg:   var(--sg-radius-lg);
            --sui-radius-full: var(--sg-radius-full);

            --sui-spacing-1:  var(--sg-p-space-0-5);
            --sui-spacing-2:  var(--sg-p-space-1);
            --sui-spacing-3:  var(--sg-p-space-1-5);
            --sui-spacing-4:  var(--sg-p-space-2);
            --sui-spacing-5:  var(--sg-p-space-2-5);
            --sui-spacing-6:  var(--sg-p-space-3);
            --sui-spacing-8:  var(--sg-p-space-4);
            --sui-spacing-10: var(--sg-p-space-5);
            --sui-spacing-12: var(--sg-p-space-6);
            --sui-spacing-16: var(--sg-p-space-8);

            --sui-z-dropdown: var(--sg-z-dropdown);
            --sui-z-sticky:   var(--sg-z-sticky);
            --sui-z-modal:    var(--sg-z-modal);
            --sui-z-toast:    var(--sg-z-toast);
            --sui-z-tooltip:  var(--sg-z-tooltip);
        }
        """;
}

internal class DefaultPrimitives : IThemePrimitives
{
    public string Neutral0   => "#ffffff";
    public string Neutral50  => "#f9fafb";
    public string Neutral100 => "#f3f4f6";
    public string Neutral200 => "#e5e7eb";
    public string Neutral300 => "#d1d5db";
    public string Neutral400 => "#9ca3af";
    public string Neutral500 => "#6b7280";
    public string Neutral600 => "#4b5563";
    public string Neutral700 => "#374151";
    public string Neutral800 => "#1f2937";
    public string Neutral900 => "#111827";

    public string Primary50  => "#eff6ff";
    public string Primary100 => "#dbeafe";
    public string Primary200 => "#bfdbfe";
    public string Primary300 => "#93c5fd";
    public string Primary400 => "#60a5fa";
    public string Primary500 => "#3b82f6";
    public string Primary600 => "#2563eb";
    public string Primary700 => "#1d4ed8";
    public string Primary800 => "#1e40af";
    public string Primary900 => "#1e3a8a";

    public string Success50  => "#ecfdf5";
    public string Success100 => "#d1fae5";
    public string Success500 => "#10b981";
    public string Success600 => "#059669";
    public string Success700 => "#047857";

    public string Danger50  => "#fff1f2";
    public string Danger100 => "#ffe4e6";
    public string Danger500 => "#f43f5e";
    public string Danger600 => "#e11d48";
    public string Danger700 => "#be123c";

    public string Warning50  => "#fffbeb";
    public string Warning100 => "#fef3c7";
    public string Warning500 => "#f59e0b";
    public string Warning600 => "#d97706";

    public string Info50  => "#f0f9ff";
    public string Info100 => "#e0f2fe";
    public string Info500 => "#0ea5e9";
    public string Info600 => "#0284c7";

    public string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public string FontSerif => "Georgia, 'Times New Roman', serif";

    public string RadiusNone => "0";
    public string RadiusXs   => "2px";
    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string Radius2Xl  => "16px";
    public string RadiusFull => "9999px";
}

internal class DefaultSemanticLight : IThemeSemantic
{
    public string BgDefault    => "#ffffff";
    public string BgSubtle     => "#f9fafb";
    public string BgMuted      => "#f3f4f6";
    public string BgEmphasized => "#eaecf0";
    public string BgOverlay    => "rgba(0, 0, 0, 0.5)";

    public string Surface        => "#ffffff";
    public string SurfaceRaised  => "#ffffff";
    public string SurfaceOverlay => "#ffffff";

    public string FgDefault  => "#111827";
    public string FgSubtle   => "#4b5563";
    public string FgMuted    => "#9ca3af";
    public string FgDisabled => "#d1d5db";
    public string FgInverse  => "#ffffff";
    public string FgLink     => "#2563eb";
    public string FgLinkHover => "#1d4ed8";

    public string BorderDefault  => "#e5e7eb";
    public string BorderSubtle   => "#eaecf0";
    public string BorderStrong   => "#d1d5db";
    public string BorderFocus    => "#2563eb";

    public string ColorPrimary       => "#006fee";
    public string ColorPrimarySubtle => "rgba(0, 111, 238, 0.08)";
    public string ColorPrimaryMuted  => "rgba(0, 111, 238, 0.15)";
    public string ColorPrimaryHover  => "#005bc4";
    public string ColorPrimaryActive => "#004494";
    public string ColorPrimaryFg     => "#ffffff";

    public string ColorSuccess       => "#10b981";
    public string ColorSuccessSubtle => "#ecfdf5";
    public string ColorSuccessHover  => "#059669";
    public string ColorSuccessFg     => "#ffffff";

    public string ColorDanger        => "#f43f5e";
    public string ColorDangerSubtle  => "#fff1f2";
    public string ColorDangerHover   => "#e11d48";
    public string ColorDangerFg      => "#ffffff";

    public string ColorWarning       => "#f59e0b";
    public string ColorWarningSubtle => "#fffbeb";
    public string ColorWarningHover  => "#d97706";
    public string ColorWarningFg     => "#111827";

    public string ColorInfo          => "#0ea5e9";
    public string ColorInfoSubtle    => "#f0f9ff";
    public string ColorInfoHover     => "#0284c7";
    public string ColorInfoFg        => "#ffffff";

    public string Font     => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    public string FontMono => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public string TextSm   => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg   => "1rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.05)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px -1px rgba(0, 0, 0, 0.1)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.1)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1)";

    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public string TransitionBase => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing =>
        "0 0 0 2px rgba(0, 111, 238, 0.12), 0 0 0 4px rgba(0, 111, 238, 0.50)";
    public string FocusRingDanger =>
        "0 0 0 2px rgba(244, 63, 94, 0.12), 0 0 0 4px rgba(244, 63, 94, 0.50)";

    public int ZDropdown => 100;
    public int ZSticky   => 200;
    public int ZModal    => 300;
    public int ZToast    => 400;
    public int ZTooltip  => 500;
}

internal class DefaultSemanticDark : IThemeSemantic
{
    public string BgDefault    => "#0a0a0a";
    public string BgSubtle     => "#171717";
    public string BgMuted      => "#262626";
    public string BgEmphasized => "#383838";
    public string BgOverlay    => "rgba(0, 0, 0, 0.75)";

    public string Surface        => "#171717";
    public string SurfaceRaised  => "#1c1c1c";
    public string SurfaceOverlay => "#1c1c1c";

    public string FgDefault  => "#fafafa";
    public string FgSubtle   => "#a3a3a3";
    public string FgMuted    => "#737373";
    public string FgDisabled => "#404040";
    public string FgInverse  => "#0a0a0a";
    public string FgLink     => "#60a5fa";
    public string FgLinkHover => "#93c5fd";

    public string BorderDefault  => "#262626";
    public string BorderSubtle   => "#1c1c1c";
    public string BorderStrong   => "#404040";
    public string BorderFocus    => "#3b82f6";

    public string ColorPrimary       => "#3b82f6";
    public string ColorPrimarySubtle => "rgba(59, 130, 246, 0.12)";
    public string ColorPrimaryMuted  => "rgba(59, 130, 246, 0.20)";
    public string ColorPrimaryHover  => "#60a5fa";
    public string ColorPrimaryActive => "#93c5fd";
    public string ColorPrimaryFg     => "#ffffff";

    public string ColorSuccess       => "#10b981";
    public string ColorSuccessSubtle => "rgba(16, 185, 129, 0.12)";
    public string ColorSuccessHover  => "#34d399";
    public string ColorSuccessFg     => "#ffffff";

    public string ColorDanger        => "#f43f5e";
    public string ColorDangerSubtle  => "rgba(244, 63, 94, 0.12)";
    public string ColorDangerHover   => "#fb7185";
    public string ColorDangerFg      => "#ffffff";

    public string ColorWarning       => "#f59e0b";
    public string ColorWarningSubtle => "rgba(245, 158, 11, 0.12)";
    public string ColorWarningHover  => "#fbbf24";
    public string ColorWarningFg     => "#0a0a0a";

    public string ColorInfo          => "#38bdf8";
    public string ColorInfoSubtle    => "rgba(56, 189, 248, 0.12)";
    public string ColorInfoHover     => "#7dd3fc";
    public string ColorInfoFg        => "#ffffff";

    public string Font     => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    public string FontMono => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public string TextSm   => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg   => "1rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.5)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.6), 0 1px 2px -1px rgba(0, 0, 0, 0.4)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.6), 0 2px 4px -2px rgba(0, 0, 0, 0.4)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.7), 0 4px 6px -4px rgba(0, 0, 0, 0.5)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.8), 0 8px 10px -6px rgba(0, 0, 0, 0.6)";

    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms cubic-bezier(0, 0, 0.2, 1)";
    public string TransitionBase => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing =>
        "0 0 0 2px rgba(59, 130, 246, 0.20), 0 0 0 4px rgba(59, 130, 246, 0.60)";
    public string FocusRingDanger =>
        "0 0 0 2px rgba(244, 63, 94, 0.20), 0 0 0 4px rgba(244, 63, 94, 0.60)";

    public int ZDropdown => 100;
    public int ZSticky   => 200;
    public int ZModal    => 300;
    public int ZToast    => 400;
    public int ZTooltip  => 500;
}

internal class DefaultComponents : IThemeComponents
{
    public string BtnRadius      => "6px";
    public string BtnFontSize    => "0.8125rem";
    public string BtnFontWeight  => "500";
    public string BtnHeight      => "2rem";
    public string BtnHeightSm   => "1.625rem";
    public string BtnHeightLg   => "2.375rem";

    public string InputRadius    => "6px";
    public string InputFontSize  => "0.8125rem";
    public string InputHeight    => "2rem";
    public string InputHeightSm => "1.625rem";
    public string InputHeightLg => "2.375rem";

    public string CardRadius     => "12px";
    public string CardPadding    => "1rem";

    public string ModalRadius    => "16px";

    public string TableRadius    => "8px";
    public string TableHeaderFontWeight => "600";

    public string TabsIndicatorHeight => "2px";

    public string TooltipMaxWidth => "200px";
}
```

---

### 9.2 Material Design 3

**Файл: `Themes/MaterialTheme.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Material Design 3 тема для SuperUI.
/// Использует систему токенов Material You (Dynamic Color).
/// Документация: https://m3.material.io/styles/color/roles
/// </summary>
public sealed class MaterialTheme : ThemeBase
{
    public override string Id => "material-design-3";
    public override string Name => "Material Design 3";
    public override string Description => "Google Material Design 3 (Material You)";
    public override string Author => "SuperUI Team";
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() => new MaterialPrimitives();
    protected override IThemeSemantic CreateLight()        => new MaterialSemanticLight();
    protected override IThemeSemantic CreateDark()         => new MaterialSemanticDark();
    protected override IThemeComponents CreateComponents() => new MaterialComponents();

    public override string? AdditionalCss => """
        /* ── Material Design 3 специфика ────────────────────────────────────── */

        /* Ripple эффект */
        :root {
            --md-ripple-color:    rgba(103, 80, 164, 0.12);
            --md-ripple-duration: 300ms;
            --md-state-layer-opacity-hover:   0.08;
            --md-state-layer-opacity-press:   0.12;
            --md-state-layer-opacity-focus:   0.12;
            --md-state-layer-opacity-drag:    0.16;
        }

        /* Material Elevation система */
        [data-theme-id="material-design-3"] {
            --md-elevation-0: none;
            --md-elevation-1:
                0px 1px 2px rgba(0, 0, 0, 0.3),
                0px 1px 3px 1px rgba(0, 0, 0, 0.15);
            --md-elevation-2:
                0px 1px 2px rgba(0, 0, 0, 0.3),
                0px 2px 6px 2px rgba(0, 0, 0, 0.15);
            --md-elevation-3:
                0px 4px 8px 3px rgba(0, 0, 0, 0.15),
                0px 1px 3px rgba(0, 0, 0, 0.3);
            --md-elevation-4:
                0px 6px 10px 4px rgba(0, 0, 0, 0.15),
                0px 2px 3px rgba(0, 0, 0, 0.3);
            --md-elevation-5:
                0px 8px 12px 6px rgba(0, 0, 0, 0.15),
                0px 4px 4px rgba(0, 0, 0, 0.3);
        }

        /* Кнопки с Ripple */
        [data-theme-id="material-design-3"] .sgc-btn {
            position: relative;
            overflow: hidden;
            font-weight: 500;
            letter-spacing: 0.00625em;
            text-transform: none;
        }

        /* Elevated Button */
        [data-theme-id="material-design-3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger) {
            box-shadow: var(--md-elevation-1);
        }
        [data-theme-id="material-design-3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):hover {
            box-shadow: var(--md-elevation-2);
        }

        /* Filled Tonal Button */
        [data-theme-id="material-design-3"] .sgc-btn-tonal {
            background: var(--md-color-secondary-container);
            color:      var(--md-color-on-secondary-container);
        }

        /* FAB */
        [data-theme-id="material-design-3"] .sgc-fab {
            border-radius: 16px;
            box-shadow: var(--md-elevation-3);
        }

        /* Input — Material outlined style */
        [data-theme-id="material-design-3"] .sgc-input-wrap {
            position: relative;
        }

        [data-theme-id="material-design-3"] .sgc-input {
            border-radius: 4px;
            padding-top:   1.25rem;
            padding-bottom: 0.5rem;
        }

        /* Chip */
        [data-theme-id="material-design-3"] .sg-badge {
            border-radius: 8px;
            font-weight: 500;
            letter-spacing: 0.00625em;
        }

        /* Navigation Rail (боковая навигация Material) */
        [data-theme-id="material-design-3"] .sg-nav-menu {
            background: var(--md-color-surface);
            border-right: 1px solid var(--md-color-outline-variant);
        }

        /* Card — Material filled card */
        [data-theme-id="material-design-3"] .sgc-card {
            background: var(--md-color-surface-container);
            border: none;
            box-shadow: none;
        }
        [data-theme-id="material-design-3"] .sgc-card:hover {
            box-shadow: var(--md-elevation-1);
        }

        /* Material 3 токены цветов */
        [data-theme-id="material-design-3"] {
            /* Primary */
            --md-color-primary:              var(--sg-color-primary);
            --md-color-on-primary:           var(--sg-color-primary-fg);
            --md-color-primary-container:    var(--sg-color-primary-subtle);
            --md-color-on-primary-container: var(--sg-color-primary-active);

            /* Secondary */
            --md-color-secondary:              #625B71;
            --md-color-on-secondary:           #FFFFFF;
            --md-color-secondary-container:    #E8DEF8;
            --md-color-on-secondary-container: #1D192B;

            /* Tertiary */
            --md-color-tertiary:              #7D5260;
            --md-color-on-tertiary:           #FFFFFF;
            --md-color-tertiary-container:    #FFD8E4;
            --md-color-on-tertiary-container: #31111D;

            /* Error */
            --md-color-error:              var(--sg-color-danger);
            --md-color-on-error:           var(--sg-color-danger-fg);
            --md-color-error-container:    var(--sg-color-danger-subtle);
            --md-color-on-error-container: var(--sg-color-danger-hover);

            /* Background / Surface */
            --md-color-background:         var(--sg-bg);
            --md-color-on-background:      var(--sg-fg);
            --md-color-surface:            var(--sg-surface);
            --md-color-on-surface:         var(--sg-fg);
            --md-color-surface-variant:    var(--sg-bg-muted);
            --md-color-on-surface-variant: var(--sg-fg-subtle);
            --md-color-surface-container:  var(--sg-bg-subtle);

            /* Outline */
            --md-color-outline:         var(--sg-border-strong);
            --md-color-outline-variant: var(--sg-border);

            /* Motion */
            --md-motion-easing-standard:        cubic-bezier(0.2, 0, 0, 1);
            --md-motion-easing-emphasized:       cubic-bezier(0.2, 0, 0, 1);
            --md-motion-easing-decelerated:      cubic-bezier(0, 0, 0, 1);
            --md-motion-easing-accelerated:      cubic-bezier(0.3, 0, 1, 1);
            --md-motion-duration-short1: 50ms;
            --md-motion-duration-short2: 100ms;
            --md-motion-duration-short3: 150ms;
            --md-motion-duration-short4: 200ms;
            --md-motion-duration-medium1: 250ms;
            --md-motion-duration-medium2: 300ms;
            --md-motion-duration-medium3: 350ms;
            --md-motion-duration-medium4: 400ms;
            --md-motion-duration-long1: 450ms;
            --md-motion-duration-long2: 500ms;

            /* Shape */
            --md-shape-none:       0;
            --md-shape-extra-small: 4px;
            --md-shape-small:       8px;
            --md-shape-medium:     12px;
            --md-shape-large:      16px;
            --md-shape-extra-large: 28px;
            --md-shape-full:       9999px;
        }
        """;
}

internal class MaterialPrimitives : DefaultPrimitives
{
    // Material использует свою фиолетовую палитру
    public new string Primary50  => "#F3EDF7";
    public new string Primary100 => "#E8DEF8";
    public new string Primary200 => "#CCC2DC";
    public new string Primary300 => "#B69DF8";
    public new string Primary400 => "#9A82DB";
    public new string Primary500 => "#7965AF";
    public new string Primary600 => "#6750A4";
    public new string Primary700 => "#4F378B";
    public new string Primary800 => "#381E72";
    public new string Primary900 => "#21005D";

    // Material rounded corners
    public new string RadiusXs   => "4px";
    public new string RadiusSm   => "8px";
    public new string RadiusMd   => "12px";
    public new string RadiusLg   => "16px";
    public new string RadiusXl   => "28px";
    public new string Radius2Xl  => "28px";
}

internal class MaterialSemanticLight : DefaultSemanticLight
{
    public new string ColorPrimary       => "#6750A4";
    public new string ColorPrimarySubtle => "#E8DEF8";
    public new string ColorPrimaryMuted  => "#CCC2DC";
    public new string ColorPrimaryHover  => "#4F378B";
    public new string ColorPrimaryActive => "#381E72";

    public new string RadiusSm   => "8px";
    public new string RadiusMd   => "12px";
    public new string RadiusLg   => "16px";
    public new string RadiusXl   => "28px";

    // Material использует систему elevation вместо теней
    public new string ShadowSm =>
        "0px 1px 2px rgba(0,0,0,0.3), 0px 1px 3px 1px rgba(0,0,0,0.15)";
    public new string ShadowMd =>
        "0px 1px 2px rgba(0,0,0,0.3), 0px 2px 6px 2px rgba(0,0,0,0.15)";
    public new string ShadowLg =>
        "0px 4px 8px 3px rgba(0,0,0,0.15), 0px 1px 3px rgba(0,0,0,0.3)";
}

internal class MaterialSemanticDark : DefaultSemanticDark
{
    public new string ColorPrimary       => "#D0BCFF";
    public new string ColorPrimarySubtle => "rgba(208, 188, 255, 0.12)";
    public new string ColorPrimaryMuted  => "rgba(208, 188, 255, 0.20)";
    public new string ColorPrimaryHover  => "#E8DEF8";
    public new string ColorPrimaryActive => "#F3EDF7";

    public new string Surface        => "#141218";
    public new string SurfaceRaised  => "#1C1B1F";
    public new string SurfaceOverlay => "#211F26";

    public new string BgDefault  => "#141218";
    public new string BgSubtle   => "#1C1B1F";
    public new string BgMuted    => "#211F26";
}

internal class MaterialComponents : DefaultComponents
{
    public new string BtnRadius     => "20px";    // Pill shape в Material 3
    public new string BtnHeight     => "2.5rem";  // 40px — MD3 standard
    public new string BtnHeightSm  => "2rem";
    public new string BtnHeightLg  => "3rem";
    public new string BtnFontSize   => "0.875rem"; // 14px
    public new string BtnFontWeight => "500";

    public new string InputRadius   => "4px";     // Outlined TextField
    public new string InputHeight   => "3.5rem";  // 56px — MD3 standard
    public new string InputHeightSm => "3rem";
    public new string InputHeightLg => "4rem";

    public new string CardRadius    => "12px";    // MD3 Medium shape
    public new string ModalRadius   => "28px";    // MD3 Extra-large shape

    public new string TabsIndicatorHeight => "3px";
}
```

---

### 9.3 Tailwind CSS

**Файл: `Themes/TailwindTheme.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Tailwind CSS v3 тема для SuperUI.
/// Использует стандартную цветовую палитру Tailwind и утилитарный подход.
/// Документация: https://tailwindcss.com/docs/customizing-colors
/// </summary>
public sealed class TailwindTheme : ThemeBase
{
    public override string Id => "tailwind-v3";
    public override string Name => "Tailwind CSS v3";
    public override string Description => "Tailwind CSS v3 color palette and design system";
    public override string Author => "SuperUI Team";
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() => new TailwindPrimitives();
    protected override IThemeSemantic CreateLight()        => new TailwindSemanticLight();
    protected override IThemeSemantic CreateDark()         => new TailwindSemanticDark();
    protected override IThemeComponents CreateComponents() => new TailwindComponents();

    public override string? AdditionalCss => """
        /* ── Tailwind CSS совместимые утилиты ────────────────────────────────── */

        /* Ring system (фокусное кольцо в Tailwind-стиле) */
        [data-theme-id="tailwind-v3"] .sgc-input:focus,
        [data-theme-id="tailwind-v3"] .sgc-input-wrap:focus-within .sgc-input {
            --tw-ring-color: rgba(59, 130, 246, 0.5);
            box-shadow: 0 0 0 3px var(--tw-ring-color);
            border-color: #3B82F6;
        }

        /* Tailwind Prose-подобные стили */
        [data-theme-id="tailwind-v3"] {
            /* Slate palette references */
            --tw-slate-50:  #F8FAFC;
            --tw-slate-100: #F1F5F9;
            --tw-slate-200: #E2E8F0;
            --tw-slate-300: #CBD5E1;
            --tw-slate-400: #94A3B8;
            --tw-slate-500: #64748B;
            --tw-slate-600: #475569;
            --tw-slate-700: #334155;
            --tw-slate-800: #1E293B;
            --tw-slate-900: #0F172A;
            --tw-slate-950: #020617;

            /* Blue palette */
            --tw-blue-50:  #EFF6FF;
            --tw-blue-100: #DBEAFE;
            --tw-blue-500: #3B82F6;
            --tw-blue-600: #2563EB;
            --tw-blue-700: #1D4ED8;

            /* Indigo palette */
            --tw-indigo-50:  #EEF2FF;
            --tw-indigo-500: #6366F1;
            --tw-indigo-600: #4F46E5;
            --tw-indigo-700: #4338CA;

            /* Violet palette */
            --tw-violet-500: #8B5CF6;
            --tw-violet-600: #7C3AED;

            /* Transition classes */
            --tw-transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);
        }

        /* Tailwind-like button reset */
        [data-theme-id="tailwind-v3"] .sgc-btn {
            font-weight: 600;
            letter-spacing: 0;
            transition: var(--tw-transition);
        }

        /* Tailwind-like input */
        [data-theme-id="tailwind-v3"] .sgc-input {
            transition: border-color 150ms ease, box-shadow 150ms ease;
        }

        /* Tailwind table */
        [data-theme-id="tailwind-v3"] .sg-table {
            border-collapse: separate;
            border-spacing: 0;
        }

        /* Tailwind divider */
        [data-theme-id="tailwind-v3"] .sg-divider {
            border-color: var(--tw-slate-200);
        }
        [data-theme="dark"] [data-theme-id="tailwind-v3"] .sg-divider {
            border-color: var(--tw-slate-700);
        }
        """;
}

internal class TailwindPrimitives : IThemePrimitives
{
    // Tailwind v3 Slate (нейтральная серая с синеватым оттенком)
    public string Neutral0   => "#FFFFFF";
    public string Neutral50  => "#F8FAFC";
    public string Neutral100 => "#F1F5F9";
    public string Neutral200 => "#E2E8F0";
    public string Neutral300 => "#CBD5E1";
    public string Neutral400 => "#94A3B8";
    public string Neutral500 => "#64748B";
    public string Neutral600 => "#475569";
    public string Neutral700 => "#334155";
    public string Neutral800 => "#1E293B";
    public string Neutral900 => "#0F172A";

    // Tailwind Blue
    public string Primary50  => "#EFF6FF";
    public string Primary100 => "#DBEAFE";
    public string Primary200 => "#BFDBFE";
    public string Primary300 => "#93C5FD";
    public string Primary400 => "#60A5FA";
    public string Primary500 => "#3B82F6";
    public string Primary600 => "#2563EB";
    public string Primary700 => "#1D4ED8";
    public string Primary800 => "#1E40AF";
    public string Primary900 => "#1E3A8A";

    // Tailwind Emerald (Green)
    public string Success50  => "#ECFDF5";
    public string Success100 => "#D1FAE5";
    public string Success500 => "#10B981";
    public string Success600 => "#059669";
    public string Success700 => "#047857";

    // Tailwind Red
    public string Danger50  => "#FEF2F2";
    public string Danger100 => "#FEE2E2";
    public string Danger500 => "#EF4444";
    public string Danger600 => "#DC2626";
    public string Danger700 => "#B91C1C";

    // Tailwind Amber
    public string Warning50  => "#FFFBEB";
    public string Warning100 => "#FEF3C7";
    public string Warning500 => "#F59E0B";
    public string Warning600 => "#D97706";

    // Tailwind Sky
    public string Info50  => "#F0F9FF";
    public string Info100 => "#E0F2FE";
    public string Info500 => "#0EA5E9";
    public string Info600 => "#0284C7";

    // Tailwind использует Inter как дефолтный шрифт
    public string FontSans  => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
    public string FontMono  => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace";
    public string FontSerif => "ui-serif, Georgia, Cambria, 'Times New Roman', Times, serif";

    // Tailwind border-radius — более скруглённые
    public string RadiusNone => "0";
    public string RadiusXs   => "2px";
    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string Radius2Xl  => "16px";
    public string RadiusFull => "9999px";
}

internal class TailwindSemanticLight : IThemeSemantic
{
    public string BgDefault    => "#FFFFFF";
    public string BgSubtle     => "#F8FAFC";
    public string BgMuted      => "#F1F5F9";
    public string BgEmphasized => "#E2E8F0";
    public string BgOverlay    => "rgba(0, 0, 0, 0.5)";

    public string Surface        => "#FFFFFF";
    public string SurfaceRaised  => "#FFFFFF";
    public string SurfaceOverlay => "#FFFFFF";

    public string FgDefault  => "#0F172A";
    public string FgSubtle   => "#475569";
    public string FgMuted    => "#94A3B8";
    public string FgDisabled => "#CBD5E1";
    public string FgInverse  => "#FFFFFF";
    public string FgLink     => "#2563EB";
    public string FgLinkHover => "#1D4ED8";

    public string BorderDefault  => "#E2E8F0";
    public string BorderSubtle   => "#F1F5F9";
    public string BorderStrong   => "#CBD5E1";
    public string BorderFocus    => "#3B82F6";

    public string ColorPrimary       => "#2563EB";
    public string ColorPrimarySubtle => "#EFF6FF";
    public string ColorPrimaryMuted  => "#DBEAFE";
    public string ColorPrimaryHover  => "#1D4ED8";
    public string ColorPrimaryActive => "#1E40AF";
    public string ColorPrimaryFg     => "#FFFFFF";

    public string ColorSuccess       => "#10B981";
    public string ColorSuccessSubtle => "#ECFDF5";
    public string ColorSuccessHover  => "#059669";
    public string ColorSuccessFg     => "#FFFFFF";

    public string ColorDanger        => "#EF4444";
    public string ColorDangerSubtle  => "#FEF2F2";
    public string ColorDangerHover   => "#DC2626";
    public string ColorDangerFg      => "#FFFFFF";

    public string ColorWarning       => "#F59E0B";
    public string ColorWarningSubtle => "#FFFBEB";
    public string ColorWarningHover  => "#D97706";
    public string ColorWarningFg     => "#0F172A";

    public string ColorInfo          => "#0EA5E9";
    public string ColorInfoSubtle    => "#F0F9FF";
    public string ColorInfoHover     => "#0284C7";
    public string ColorInfoFg        => "#FFFFFF";

    public string Font     => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    public string FontMono => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string TextSm   => "0.875rem";   // Tailwind text-sm = 14px
    public string TextBase => "1rem";       // Tailwind text-base = 16px
    public string TextLg   => "1.125rem";   // Tailwind text-lg = 18px

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.05)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px -1px rgba(0, 0, 0, 0.1)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.1)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1)";

    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    // Tailwind ring — синие 3px кольца
    public string FocusRing =>
        "0 0 0 3px rgba(59, 130, 246, 0.5)";
    public string FocusRingDanger =>
        "0 0 0 3px rgba(239, 68, 68, 0.5)";

    public int ZDropdown => 100;
    public int ZSticky   => 200;
    public int ZModal    => 300;
    public int ZToast    => 400;
    public int ZTooltip  => 500;
}

internal class TailwindSemanticDark : IThemeSemantic
{
    // Tailwind Dark mode обычно использует slate-900 как фон
    public string BgDefault    => "#0F172A";
    public string BgSubtle     => "#1E293B";
    public string BgMuted      => "#334155";
    public string BgEmphasized => "#475569";
    public string BgOverlay    => "rgba(0, 0, 0, 0.8)";

    public string Surface        => "#1E293B";
    public string SurfaceRaised  => "#334155";
    public string SurfaceOverlay => "#1E293B";

    public string FgDefault  => "#F1F5F9";
    public string FgSubtle   => "#94A3B8";
    public string FgMuted    => "#64748B";
    public string FgDisabled => "#475569";
    public string FgInverse  => "#0F172A";
    public string FgLink     => "#60A5FA";
    public string FgLinkHover => "#93C5FD";

    public string BorderDefault  => "#334155";
    public string BorderSubtle   => "#1E293B";
    public string BorderStrong   => "#475569";
    public string BorderFocus    => "#60A5FA";

    public string ColorPrimary       => "#3B82F6";
    public string ColorPrimarySubtle => "rgba(59, 130, 246, 0.15)";
    public string ColorPrimaryMuted  => "rgba(59, 130, 246, 0.25)";
    public string ColorPrimaryHover  => "#60A5FA";
    public string ColorPrimaryActive => "#93C5FD";
    public string ColorPrimaryFg     => "#FFFFFF";

    public string ColorSuccess       => "#10B981";
    public string ColorSuccessSubtle => "rgba(16, 185, 129, 0.15)";
    public string ColorSuccessHover  => "#34D399";
    public string ColorSuccessFg     => "#FFFFFF";

    public string ColorDanger        => "#EF4444";
    public string ColorDangerSubtle  => "rgba(239, 68, 68, 0.15)";
    public string ColorDangerHover   => "#F87171";
    public string ColorDangerFg      => "#FFFFFF";

    public string ColorWarning       => "#F59E0B";
    public string ColorWarningSubtle => "rgba(245, 158, 11, 0.15)";
    public string ColorWarningHover  => "#FBBF24";
    public string ColorWarningFg     => "#0F172A";

    public string ColorInfo          => "#0EA5E9";
    public string ColorInfoSubtle    => "rgba(14, 165, 233, 0.15)";
    public string ColorInfoHover     => "#38BDF8";
    public string ColorInfoFg        => "#FFFFFF";

    public string Font     => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    public string FontMono => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string TextSm   => "0.875rem";
    public string TextBase => "1rem";
    public string TextLg   => "1.125rem";

    public string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.5)";
    public string ShadowSm => "0 1px 3px 0 rgba(0, 0, 0, 0.6), 0 1px 2px -1px rgba(0, 0, 0, 0.4)";
    public string ShadowMd => "0 4px 6px -1px rgba(0, 0, 0, 0.6), 0 2px 4px -2px rgba(0, 0, 0, 0.4)";
    public string ShadowLg => "0 10px 15px -3px rgba(0, 0, 0, 0.7), 0 4px 6px -4px rgba(0, 0, 0, 0.5)";
    public string ShadowXl => "0 20px 25px -5px rgba(0, 0, 0, 0.8), 0 8px 10px -6px rgba(0, 0, 0, 0.6)";

    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing      => "0 0 0 3px rgba(59, 130, 246, 0.5)";
    public string FocusRingDanger => "0 0 0 3px rgba(239, 68, 68, 0.5)";

    public int ZDropdown => 100;
    public int ZSticky   => 200;
    public int ZModal    => 300;
    public int ZToast    => 400;
    public int ZTooltip  => 500;
}

internal class TailwindComponents : IThemeComponents
{
    public string BtnRadius      => "6px";
    public string BtnFontSize    => "0.875rem";
    public string BtnFontWeight  => "600";
    public string BtnHeight      => "2.25rem";     // 36px
    public string BtnHeightSm   => "1.75rem";     // 28px
    public string BtnHeightLg   => "2.5rem";      // 40px

    public string InputRadius    => "6px";
    public string InputFontSize  => "0.875rem";
    public string InputHeight    => "2.25rem";
    public string InputHeightSm => "1.75rem";
    public string InputHeightLg => "2.5rem";

    public string CardRadius     => "8px";
    public string CardPadding    => "1.5rem";

    public string ModalRadius    => "12px";

    public string TableRadius    => "8px";
    public string TableHeaderFontWeight => "600";

    public string TabsIndicatorHeight => "2px";

    public string TooltipMaxWidth => "250px";
}
```

---

### 9.4 Custom Theme

**Файл: `Themes/CustomTheme.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Шаблон для создания полностью кастомной темы.
/// Скопируйте этот класс и измените значения токенов.
/// </summary>
public class CustomTheme : ThemeBase
{
    private readonly string _id;
    private readonly string _name;
    private readonly string _primaryColor;
    private readonly string _primaryLight;

    public CustomTheme(
        string id,
        string name,
        string primaryColor = "#6366F1",    // Indigo-500
        string primaryLight = "#EEF2FF")    // Indigo-50
    {
        _id = id;
        _name = name;
        _primaryColor = primaryColor;
        _primaryLight = primaryLight;
    }

    public override string Id      => _id;
    public override string Name    => _name;
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() =>
        new DefaultPrimitives();

    protected override IThemeSemantic CreateLight() =>
        new CustomSemanticLight(_primaryColor, _primaryLight);

    protected override IThemeSemantic CreateDark() =>
        new CustomSemanticDark(_primaryColor);

    protected override IThemeComponents CreateComponents() =>
        new DefaultComponents();
}

internal class CustomSemanticLight : DefaultSemanticLight
{
    private readonly string _primary;
    private readonly string _primaryLight;

    public CustomSemanticLight(string primary, string primaryLight)
    {
        _primary = primary;
        _primaryLight = primaryLight;
    }

    public new string ColorPrimary       => _primary;
    public new string ColorPrimarySubtle => _primaryLight;
    public new string ColorPrimaryMuted  => _primaryLight;
    public new string ColorPrimaryHover  => AdjustBrightness(_primary, -20);
    public new string ColorPrimaryActive => AdjustBrightness(_primary, -40);

    private static string AdjustBrightness(string hex, int amount)
    {
        // Упрощённая реализация — в реальности используйте HslColor
        return hex; // placeholder
    }
}

internal class CustomSemanticDark : DefaultSemanticDark
{
    private readonly string _primary;

    public CustomSemanticDark(string primary)
    {
        _primary = primary;
    }

    public new string ColorPrimary       => _primary;
    public new string ColorPrimarySubtle => $"rgba({HexToRgb(_primary)}, 0.12)";
    public new string ColorPrimaryMuted  => $"rgba({HexToRgb(_primary)}, 0.20)";

    private static string HexToRgb(string hex)
    {
        hex = hex.TrimStart('#');
        var r = Convert.ToInt32(hex[..2], 16);
        var g = Convert.ToInt32(hex[2..4], 16);
        var b = Convert.ToInt32(hex[4..6], 16);
        return $"{r}, {g}, {b}";
    }
}
```

---

## 10. ThemeService — расширенная реализация

**Файл: `Services/SgThemeService.cs`** (полная замена текущего файла)

```csharp
using Microsoft.JSInterop;
using SuperUI.Themes;

namespace SuperUI.Services;

/// <summary>
/// Расширенный сервис управления темами SuperUI.
/// Поддерживает множественные темы и light/dark режимы.
/// </summary>
public sealed class SgThemeService : IAsyncDisposable
{
    private const string StorageKeyThemeId   = "superui-theme-id";
    private const string StorageKeyDarkMode  = "superui-dark-mode";

    private readonly IJSRuntime  _js;
    private readonly ThemeRegistry _registry;
    private IJSObjectReference?  _module;
    private bool _isDisposed;

    /// <summary>Текущая тема.</summary>
    public IThemeDefinition CurrentTheme { get; private set; }

    /// <summary>Текущий режим: "light" | "dark" | "auto".</summary>
    public string CurrentMode { get; private set; } = "light";

    /// <summary>true если сейчас тёмный режим.</summary>
    public bool IsDark => CurrentMode == "dark" ||
        (CurrentMode == "auto" && _systemPrefersDark);

    private bool _systemPrefersDark;

    /// <summary>Событие изменения темы.</summary>
    public event Action<IThemeDefinition, string>? ThemeChanged;

    public SgThemeService(IJSRuntime js, ThemeRegistry registry)
    {
        _js = js;
        _registry = registry;
        CurrentTheme = registry.GetDefault();
    }

    /// <summary>Инициализация: загрузка сохранённых настроек.</summary>
    public async Task InitializeAsync()
    {
        if (_isDisposed) return;
        try
        {
            _module = await _js.InvokeAsync<IJSObjectReference>(
                "import", "./_content/SuperUI/superui-theme.js");

            // Загрузить сохранённые настройки
            var savedThemeId = await _js.InvokeAsync<string?>(
                "localStorage.getItem", StorageKeyThemeId);
            var savedMode = await _js.InvokeAsync<string?>(
                "localStorage.getItem", StorageKeyDarkMode);

            // Определить системное предпочтение
            _systemPrefersDark = await _js.InvokeAsync<bool>(
                "eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");

            // Применить сохранённую тему
            if (!string.IsNullOrEmpty(savedThemeId) &&
                _registry.TryGet(savedThemeId, out var theme))
            {
                CurrentTheme = theme!;
            }

            CurrentMode = savedMode ?? "light";

            await ApplyThemeAsync();
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException)
        {
            // Тихое падение — используем defaults
        }
    }

    /// <summary>Установить тему по ID.</summary>
    public async Task SetThemeAsync(string themeId)
    {
        if (_isDisposed) return;
        if (!_registry.TryGet(themeId, out var theme)) return;

        CurrentTheme = theme!;
        await SaveAndApplyAsync();
    }

    /// <summary>Установить тему по объекту.</summary>
    public async Task SetThemeAsync(IThemeDefinition theme)
    {
        if (_isDisposed) return;
        CurrentTheme = theme;
        await SaveAndApplyAsync();
    }

    /// <summary>Установить режим: "light" | "dark" | "auto".</summary>
    public async Task SetModeAsync(string mode)
    {
        if (_isDisposed) return;
        CurrentMode = mode;
        await SaveAndApplyAsync();
    }

    /// <summary>Переключить light ↔ dark.</summary>
    public async Task ToggleModeAsync()
    {
        var newMode = IsDark ? "light" : "dark";
        await SetModeAsync(newMode);
    }

    private async Task SaveAndApplyAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem",
                StorageKeyThemeId, CurrentTheme.Id);
            await _js.InvokeVoidAsync("localStorage.setItem",
                StorageKeyDarkMode, CurrentMode);

            await ApplyThemeAsync();
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException) { }
    }

    private async Task ApplyThemeAsync()
    {
        if (_isDisposed) return;

        var effectiveDark = IsDark;
        var css = CurrentTheme.GenerateCss();
        var dataTheme = effectiveDark ? "dark" : "light";

        try
        {
            // 1. Обновить data-theme на <html>
            await _js.InvokeVoidAsync("eval",
                $"document.documentElement.setAttribute('data-theme', '{dataTheme}')");

            // 2. Обновить data-theme-id для CSS-селекторов
            await _js.InvokeVoidAsync("eval",
                $"document.documentElement.setAttribute('data-theme-id', '{CurrentTheme.Id}')");

            // 3. Вставить/обновить CSS переменные
            await _js.InvokeVoidAsync("eval", $$"""
                (function() {
                    const id = 'sg-theme-vars';
                    let el = document.getElementById(id);
                    if (!el) {
                        el = document.createElement('style');
                        el.id = id;
                        document.head.appendChild(el);
                    }
                    el.textContent = {{EscapeForJs(css)}};
                })();
                """);

            ThemeChanged?.Invoke(CurrentTheme, CurrentMode);
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException) { }
    }

    private static string EscapeForJs(string css) =>
        $"`{css.Replace("`", "\\`").Replace("$", "\\$")}`";

    public IReadOnlyList<IThemeDefinition> GetAvailableThemes() =>
        _registry.GetAll();

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch { /* ignore */ }
        }
    }
}
```

---

## 11. ThemeBuilder — Fluent API

**Файл: `Themes/ThemeBuilder.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Fluent builder для создания кастомных тем программно.
/// </summary>
public sealed class ThemeBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "Custom Theme";
    private string _description = "";
    private string _author = "";
    private string _primaryColor = "#2563EB";
    private string _primaryColorDark = "#3B82F6";
    private string? _successColor;
    private string? _dangerColor;
    private string? _warningColor;
    private string? _infoColor;
    private string? _fontFamily;
    private string? _fontMono;
    private string? _radiusSm;
    private string? _radiusMd;
    private string? _radiusLg;
    private string? _radiusFull;
    private string? _additionalCss;
    private bool _isDark;

    private ThemeBuilder() { }

    public static ThemeBuilder Create() => new();

    public ThemeBuilder WithId(string id)          { _id = id;           return this; }
    public ThemeBuilder WithName(string name)      { _name = name;       return this; }
    public ThemeBuilder WithDescription(string d)  { _description = d;   return this; }
    public ThemeBuilder WithAuthor(string author)  { _author = author;   return this; }
    public ThemeBuilder WithDarkMode(bool dark)    { _isDark = dark;     return this; }
    public ThemeBuilder WithAdditionalCss(string css) { _additionalCss = css; return this; }

    public ThemeBuilder WithPrimaryColor(string light, string? dark = null)
    {
        _primaryColor     = light;
        _primaryColorDark = dark ?? light;
        return this;
    }

    public ThemeBuilder WithSuccessColor(string color) { _successColor = color; return this; }
    public ThemeBuilder WithDangerColor(string color)  { _dangerColor  = color; return this; }
    public ThemeBuilder WithWarningColor(string color) { _warningColor = color; return this; }
    public ThemeBuilder WithInfoColor(string color)    { _infoColor    = color; return this; }

    public ThemeBuilder WithFontFamily(string font, string? mono = null)
    {
        _fontFamily = font;
        _fontMono   = mono;
        return this;
    }

    public ThemeBuilder WithBorderRadius(
        string? sm   = null,
        string? md   = null,
        string? lg   = null,
        string? full = null)
    {
        _radiusSm   = sm;
        _radiusMd   = md;
        _radiusLg   = lg;
        _radiusFull = full;
        return this;
    }

    /// <summary>Скруглённый стиль (pill-buttons, large radius).</summary>
    public ThemeBuilder AsRounded()
    {
        _radiusSm   = "8px";
        _radiusMd   = "12px";
        _radiusLg   = "16px";
        _radiusFull = "9999px";
        return this;
    }

    /// <summary>Острые углы (sharp style).</summary>
    public ThemeBuilder AsSharp()
    {
        _radiusSm   = "0";
        _radiusMd   = "2px";
        _radiusLg   = "4px";
        _radiusFull = "4px";
        return this;
    }

    public IThemeDefinition Build()
    {
        return new BuiltTheme(
            id:          _id,
            name:        _name,
            description: _description,
            author:      _author,
            primary:     _primaryColor,
            primaryDark: _primaryColorDark,
            success:     _successColor,
            danger:      _dangerColor,
            warning:     _warningColor,
            info:        _infoColor,
            font:        _fontFamily,
            fontMono:    _fontMono,
            radiusSm:    _radiusSm,
            radiusMd:    _radiusMd,
            radiusLg:    _radiusLg,
            radiusFull:  _radiusFull,
            additionalCss: _additionalCss
        );
    }
}

/// <summary>
/// Собранная тема из ThemeBuilder.
/// </summary>
internal sealed class BuiltTheme : ThemeBase
{
    private readonly string _id, _name, _desc, _author;
    private readonly string _primary, _primaryDark;
    private readonly string? _success, _danger, _warning, _info;
    private readonly string? _font, _fontMono;
    private readonly string? _rSm, _rMd, _rLg, _rFull;
    private readonly string? _css;

    public BuiltTheme(string id, string name, string description, string author,
        string primary, string primaryDark,
        string? success, string? danger, string? warning, string? info,
        string? font, string? fontMono,
        string? radiusSm, string? radiusMd, string? radiusLg, string? radiusFull,
        string? additionalCss)
    {
        _id = id; _name = name; _desc = description; _author = author;
        _primary = primary; _primaryDark = primaryDark;
        _success = success; _danger = danger; _warning = warning; _info = info;
        _font = font; _fontMono = fontMono;
        _rSm = radiusSm; _rMd = radiusMd; _rLg = radiusLg; _rFull = radiusFull;
        _css = additionalCss;
    }

    public override string Id          => _id;
    public override string Name        => _name;
    public override string? Description => _desc;
    public override string? Author     => _author;
    public override string Version     => "custom";
    public override string? AdditionalCss => _css;

    protected override IThemePrimitives CreatePrimitives() => new DefaultPrimitives();

    protected override IThemeSemantic CreateLight()
    {
        var base_ = new DefaultSemanticLight();
        return new OverrideSemanticLight(base_,
            primary:  _primary,
            success:  _success,
            danger:   _danger,
            warning:  _warning,
            info:     _info,
            font:     _font,
            fontMono: _fontMono,
            rSm: _rSm, rMd: _rMd, rLg: _rLg, rFull: _rFull);
    }

    protected override IThemeSemantic CreateDark()
    {
        var base_ = new DefaultSemanticDark();
        return new OverrideSemanticDark(base_,
            primary:  _primaryDark,
            success:  _success,
            danger:   _danger,
            warning:  _warning,
            info:     _info,
            font:     _font,
            fontMono: _fontMono,
            rSm: _rSm, rMd: _rMd, rLg: _rLg, rFull: _rFull);
    }

    protected override IThemeComponents CreateComponents() => new DefaultComponents();
}

// Прокси-классы с переопределениями
internal sealed class OverrideSemanticLight : DefaultSemanticLight
{
    private readonly string? _p, _s, _d, _w, _i, _f, _fm, _rSm, _rMd, _rLg, _rFull;

    public OverrideSemanticLight(DefaultSemanticLight _, string? primary,
        string? success, string? danger, string? warning, string? info,
        string? font, string? fontMono,
        string? rSm, string? rMd, string? rLg, string? rFull)
    {
        _p = primary; _s = success; _d = danger; _w = warning; _i = info;
        _f = font; _fm = fontMono;
        _rSm = rSm; _rMd = rMd; _rLg = rLg; _rFull = rFull;
    }

    public new string ColorPrimary  => _p ?? base.ColorPrimary;
    public new string ColorSuccess  => _s ?? base.ColorSuccess;
    public new string ColorDanger   => _d ?? base.ColorDanger;
    public new string ColorWarning  => _w ?? base.ColorWarning;
    public new string ColorInfo     => _i ?? base.ColorInfo;
    public new string Font          => _f ?? base.Font;
    public new string FontMono      => _fm ?? base.FontMono;
    public new string RadiusSm      => _rSm  ?? base.RadiusSm;
    public new string RadiusMd      => _rMd  ?? base.RadiusMd;
    public new string RadiusLg      => _rLg  ?? base.RadiusLg;
    public new string RadiusFull    => _rFull ?? base.RadiusFull;
}

internal sealed class OverrideSemanticDark : DefaultSemanticDark
{
    private readonly string? _p, _s, _d, _w, _i, _f, _fm, _rSm, _rMd, _rLg, _rFull;

    public OverrideSemanticDark(DefaultSemanticDark _, string? primary,
        string? success, string? danger, string? warning, string? info,
        string? font, string? fontMono,
        string? rSm, string? rMd, string? rLg, string? rFull)
    {
        _p = primary; _s = success; _d = danger; _w = warning; _i = info;
        _f = font; _fm = fontMono;
        _rSm = rSm; _rMd = rMd; _rLg = rLg; _rFull = rFull;
    }

    public new string ColorPrimary  => _p ?? base.ColorPrimary;
    public new string ColorSuccess  => _s ?? base.ColorSuccess;
    public new string ColorDanger   => _d ?? base.ColorDanger;
    public new string ColorWarning  => _w ?? base.ColorWarning;
    public new string ColorInfo     => _i ?? base.ColorInfo;
    public new string Font          => _f ?? base.Font;
    public new string FontMono      => _fm ?? base.FontMono;
    public new string RadiusSm      => _rSm  ?? base.RadiusSm;
    public new string RadiusMd      => _rMd  ?? base.RadiusMd;
    public new string RadiusLg      => _rLg  ?? base.RadiusLg;
    public new string RadiusFull    => _rFull ?? base.RadiusFull;
}
```

**Файл: `Themes/ThemeBase.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Базовый класс для тем. Реализует GenerateCss() и ленивую инициализацию.
/// </summary>
public abstract class ThemeBase : IThemeDefinition
{
    private IThemePrimitives? _primitives;
    private IThemeSemantic?   _light;
    private IThemeSemantic?   _dark;
    private IThemeComponents? _components;

    public abstract string Id      { get; }
    public abstract string Name    { get; }
    public virtual string? Description => null;
    public virtual string? Author      => null;
    public virtual string Version      => "1.0.0";

    public IThemePrimitives Primitives  => _primitives ??= CreatePrimitives();
    public IThemeSemantic   Light       => _light      ??= CreateLight();
    public IThemeSemantic?  Dark        => _dark       ??= CreateDark();
    public IThemeComponents? Components => _components ??= CreateComponents();
    public virtual string? AdditionalCss => null;

    protected abstract IThemePrimitives CreatePrimitives();
    protected abstract IThemeSemantic   CreateLight();
    protected abstract IThemeSemantic   CreateDark();
    protected abstract IThemeComponents CreateComponents();

    /// <summary>
    /// Генерирует полный CSS со всеми переменными темы.
    /// </summary>
    public string GenerateCss()
    {
        var sb = new System.Text.StringBuilder();

        // Primitive tokens
        sb.AppendLine("/* SuperUI Theme: " + Name + " v" + Version + " */");
        sb.AppendLine(GeneratePrimitivesCss());

        // Light mode
        sb.AppendLine(":root,");
        sb.AppendLine("[data-theme=\"light\"] {");
        sb.AppendLine(GenerateSemanticCss(Light));
        if (Components is not null)
            sb.AppendLine(GenerateComponentsCss(Components));
        sb.AppendLine("}");

        // Dark mode
        if (Dark is not null)
        {
            sb.AppendLine("[data-theme=\"dark\"] {");
            sb.AppendLine(GenerateSemanticCss(Dark));
            sb.AppendLine("}");
        }

        // Additional CSS
        if (!string.IsNullOrEmpty(AdditionalCss))
        {
            sb.AppendLine(AdditionalCss);
        }

        return sb.ToString();
    }

    private string GeneratePrimitivesCss()
    {
        var p = Primitives;
        return $"""
            :root {{
                --sg-p-font-sans: {p.FontSans};
                --sg-p-font-mono: {p.FontMono};
                --sg-p-font-serif: {p.FontSerif};
                --sg-p-radius-none: {p.RadiusNone};
                --sg-p-radius-xs:   {p.RadiusXs};
                --sg-p-radius-sm:   {p.RadiusSm};
                --sg-p-radius-md:   {p.RadiusMd};
                --sg-p-radius-lg:   {p.RadiusLg};
                --sg-p-radius-xl:   {p.RadiusXl};
                --sg-p-radius-2xl:  {p.Radius2Xl};
                --sg-p-radius-full: {p.RadiusFull};
            }}
            """;
    }

    private static string GenerateSemanticCss(IThemeSemantic s) => $"""
        /* Background */
        --sg-bg:              {s.BgDefault};
        --sg-bg-subtle:       {s.BgSubtle};
        --sg-bg-muted:        {s.BgMuted};
        --sg-bg-emphasized:   {s.BgEmphasized};
        --sg-bg-overlay:      {s.BgOverlay};
        /* Surface */
        --sg-surface:         {s.Surface};
        --sg-surface-raised:  {s.SurfaceRaised};
        --sg-surface-overlay: {s.SurfaceOverlay};
        /* Text */
        --sg-fg:              {s.FgDefault};
        --sg-fg-subtle:       {s.FgSubtle};
        --sg-fg-muted:        {s.FgMuted};
        --sg-fg-disabled:     {s.FgDisabled};
        --sg-fg-inverse:      {s.FgInverse};
        --sg-fg-link:         {s.FgLink};
        --sg-fg-link-hover:   {s.FgLinkHover};
        /* Border */
        --sg-border:          {s.BorderDefault};
        --sg-border-subtle:   {s.BorderSubtle};
        --sg-border-strong:   {s.BorderStrong};
        --sg-border-focus:    {s.BorderFocus};
        /* Primary */
        --sg-color-primary:        {s.ColorPrimary};
        --sg-color-primary-subtle: {s.ColorPrimarySubtle};
        --sg-color-primary-muted:  {s.ColorPrimaryMuted};
        --sg-color-primary-hover:  {s.ColorPrimaryHover};
        --sg-color-primary-active: {s.ColorPrimaryActive};
        --sg-color-primary-fg:     {s.ColorPrimaryFg};
        /* Success */
        --sg-color-success:        {s.ColorSuccess};
        --sg-color-success-subtle: {s.ColorSuccessSubtle};
        --sg-color-success-hover:  {s.ColorSuccessHover};
        --sg-color-success-fg:     {s.ColorSuccessFg};
        /* Danger */
        --sg-color-danger:         {s.ColorDanger};
        --sg-color-danger-subtle:  {s.ColorDangerSubtle};
        --sg-color-danger-hover:   {s.ColorDangerHover};
        --sg-color-danger-fg:      {s.ColorDangerFg};
        /* Warning */
        --sg-color-warning:        {s.ColorWarning};
        --sg-color-warning-subtle: {s.ColorWarningSubtle};
        --sg-color-warning-hover:  {s.ColorWarningHover};
        --sg-color-warning-fg:     {s.ColorWarningFg};
        /* Info */
        --sg-color-info:           {s.ColorInfo};
        --sg-color-info-subtle:    {s.ColorInfoSubtle};
        --sg-color-info-hover:     {s.ColorInfoHover};
        --sg-color-info-fg:        {s.ColorInfoFg};
        /* Typography */
        --sg-font:      {s.Font};
        --sg-font-mono: {s.FontMono};
        --sg-text-sm:   {s.TextSm};
        --sg-text-base: {s.TextBase};
        --sg-text-lg:   {s.TextLg};
        /* Shadows */
        --sg-shadow-xs: {s.ShadowXs};
        --sg-shadow-sm: {s.ShadowSm};
        --sg-shadow-md: {s.ShadowMd};
        --sg-shadow-lg: {s.ShadowLg};
        --sg-shadow-xl: {s.ShadowXl};
        /* Radius */
        --sg-radius-sm:   {s.RadiusSm};
        --sg-radius-md:   {s.RadiusMd};
        --sg-radius-lg:   {s.RadiusLg};
        --sg-radius-xl:   {s.RadiusXl};
        --sg-radius-full: {s.RadiusFull};
        /* Transitions */
        --sg-transition-fast: {s.TransitionFast};
        --sg-transition-base: {s.TransitionBase};
        --sg-transition-slow: {s.TransitionSlow};
        /* Focus */
        --sg-focus-ring:        {s.FocusRing};
        --sg-focus-ring-danger: {s.FocusRingDanger};
        /* Z-Index */
        --sg-z-dropdown: {s.ZDropdown};
        --sg-z-sticky:   {s.ZSticky};
        --sg-z-modal:    {s.ZModal};
        --sg-z-toast:    {s.ZToast};
        --sg-z-tooltip:  {s.ZTooltip};
        """;

    private static string GenerateComponentsCss(IThemeComponents c) => $"""
        /* Components */
        --sgc-btn-radius:      {c.BtnRadius};
        --sgc-btn-font-size:   {c.BtnFontSize};
        --sgc-btn-font-weight: {c.BtnFontWeight};
        --sgc-btn-height:      {c.BtnHeight};
        --sgc-btn-height-sm:   {c.BtnHeightSm};
        --sgc-btn-height-lg:   {c.BtnHeightLg};
        --sgc-input-radius:    {c.InputRadius};
        --sgc-input-font-size: {c.InputFontSize};
        --sgc-input-height:    {c.InputHeight};
        --sgc-input-height-sm: {c.InputHeightSm};
        --sgc-input-height-lg: {c.InputHeightLg};
        --sgc-card-radius:     {c.CardRadius};
        --sgc-card-padding:    {c.CardPadding};
        --sgc-modal-radius:    {c.ModalRadius};
        --sgc-table-radius:    {c.TableRadius};
        --sgc-tabs-indicator-height: {c.TabsIndicatorHeight};
        --sgc-tooltip-max-width:     {c.TooltipMaxWidth};
        """;
}
```

**Файл: `Themes/ThemeRegistry.cs`**

```csharp
namespace SuperUI.Themes;

/// <summary>
/// Реестр доступных тем.
/// Позволяет регистрировать и получать темы по ID.
/// </summary>
public sealed class ThemeRegistry
{
    private readonly Dictionary<string, IThemeDefinition> _themes = new();
    private string _defaultId;

    public ThemeRegistry()
    {
        // Встроенные темы
        Register(new DefaultTheme(),  isDefault: true);
        Register(new MaterialTheme());
        Register(new TailwindTheme());
    }

    /// <summary>Зарегистрировать тему.</summary>
    public ThemeRegistry Register(IThemeDefinition theme, bool isDefault = false)
    {
        _themes[theme.Id] = theme;
        if (isDefault || _themes.Count == 1)
            _defaultId = theme.Id;
        return this;
    }

    /// <summary>Получить тему по ID.</summary>
    public bool TryGet(string id, out IThemeDefinition? theme) =>
        _themes.TryGetValue(id, out theme);

    /// <summary>Получить тему по умолчанию.</summary>
    public IThemeDefinition GetDefault() =>
        _themes.TryGetValue(_defaultId, out var t) ? t : _themes.Values.First();

    /// <summary>Список всех тем.</summary>
    public IReadOnlyList<IThemeDefinition> GetAll() =>
        _themes.Values.ToList().AsReadOnly();

    /// <summary>Установить тему по умолчанию.</summary>
    public void SetDefault(string id) => _defaultId = id;
}
```

---

## 12. CSS — полный код файлов

**Файл: `wwwroot/themes/sg-tokens-compat.css`**

```css
/* =============================================================================
   SuperUI — Обратная совместимость
   Алиасы старых переменных → новые токены.
   Включать только если нужна поддержка компонентов, использующих --sui-*
   ============================================================================= */

:root,
[data-theme="light"],
[data-theme="dark"] {

    /* ── Старый --sui-* → новый --sg-* ─────────────────────────────────── */

    /* Backgrounds */
    --sui-bg-primary:   var(--sg-bg);
    --sui-bg-secondary: var(--sg-bg-subtle);
    --sui-bg-tertiary:  var(--sg-bg-muted);
    --sui-bg:           var(--sg-bg);
    --sui-bg-alt:       var(--sg-bg-subtle);
    --sui-bg-hover:     var(--sg-bg-muted);

    /* Text */
    --sui-text-primary:   var(--sg-fg);
    --sui-text-secondary: var(--sg-fg-subtle);
    --sui-text-muted:     var(--sg-fg-muted);
    --sui-text-disabled:  var(--sg-fg-disabled);
    --sui-fg:             var(--sg-fg);
    --sui-text:           var(--sg-fg);
    --sui-muted:          var(--sg-fg-muted);

    /* Borders */
    --sui-border:       var(--sg-border);
    --sui-border-hover: var(--sg-border-strong);
    --sui-border-focus: var(--sg-border-focus);
    --sui-border-soft:  var(--sg-border-subtle);
    --sui-border-strong: var(--sg-border-strong);

    /* Accent / Primary */
    --sui-accent:        var(--sg-color-primary);
    --sui-accent-hover:  var(--sg-color-primary-hover);
    --sui-accent-active: var(--sg-color-primary-active);
    --sui-accent-soft:   var(--sg-color-primary-subtle);
    --sui-primary:       var(--sg-color-primary);
    --sui-primary-hover: var(--sg-color-primary-hover);

    /* States */
    --sui-success:        var(--sg-color-success);
    --sui-success-bg:     var(--sg-color-success-subtle);
    --sui-success-border: var(--sg-color-success-subtle);

    --sui-danger:        var(--sg-color-danger);
    --sui-danger-bg:     var(--sg-color-danger-subtle);
    --sui-danger-border: var(--sg-color-danger-subtle);

    --sui-warn:        var(--sg-color-warning);
    --sui-warn-bg:     var(--sg-color-warning-subtle);
    --sui-warn-border: var(--sg-color-warning-subtle);

    --sui-info:        var(--sg-color-info);
    --sui-info-bg:     var(--sg-color-info-subtle);
    --sui-info-border: var(--sg-color-info-subtle);

    /* Shadows */
    --sui-shadow-sm: var(--sg-shadow-sm);
    --sui-shadow-md: var(--sg-shadow-md);
    --sui-shadow-lg: var(--sg-shadow-lg);

    /* Overlay */
    --sui-overlay-bg: var(--sg-bg-overlay);

    /* Input */
    --sui-input-bg:           var(--sgc-input-bg);
    --sui-input-border:       var(--sgc-input-border);
    --sui-input-focus-border: var(--sgc-input-border-focus);
    --sui-input-disabled-bg:  var(--sgc-input-disabled-bg);

    /* Card */
    --sui-card-bg:     var(--sgc-card-bg);
    --sui-card-border: var(--sgc-card-border);

    /* Interaction */
    --sui-hover-bg:    var(--sg-bg-muted);
    --sui-active-bg:   var(--sg-bg-emphasized);
    --sui-selected-bg: var(--sg-color-primary-subtle);

    /* Typography */
    --sui-font-family:   var(--sg-font);
    --sui-font-size-xs:   var(--sg-text-xs,   0.75rem);
    --sui-font-size-sm:   var(--sg-text-sm);
    --sui-font-size-base: var(--sg-text-base);
    --sui-font-size-lg:   var(--sg-text-lg);

    /* Radius */
    --sui-radius-sm:   var(--sg-radius-sm);
    --sui-radius-md:   var(--sg-radius-md);
    --sui-radius-lg:   var(--sg-radius-lg);
    --sui-radius-full: var(--sg-radius-full);
    --sg-border-radius: var(--sg-radius-md);
    --sg-border-radius-sm: var(--sg-radius-sm);

    /* Spacing */
    --sui-spacing-1:  0.125rem;
    --sui-spacing-2:  0.25rem;
    --sui-spacing-3:  0.375rem;
    --sui-spacing-4:  0.5rem;
    --sui-spacing-5:  0.625rem;
    --sui-spacing-6:  0.75rem;
    --sui-spacing-8:  1rem;
    --sui-spacing-10: 1.25rem;
    --sui-spacing-12: 1.5rem;
    --sui-spacing-16: 2rem;

    /* Z-Index */
    --sui-z-dropdown: var(--sg-z-dropdown);
    --sui-z-sticky:   var(--sg-z-sticky);
    --sui-z-modal:    var(--sg-z-modal);
    --sui-z-toast:    var(--sg-z-toast);
    --sui-z-tooltip:  var(--sg-z-tooltip);

    /* Focus */
    --sui-focus: var(--sg-focus-ring);
    --sg-primary: var(--sg-color-primary);
    --sg-primary-hover: var(--sg-color-primary-hover);
    --sg-danger: var(--sg-color-danger);
    --sg-success: var(--sg-color-success);
    --sg-warn: var(--sg-color-warning);
    --sg-muted: var(--sg-fg-muted);
    --sg-fg: var(--sg-fg);
    --sg-border: var(--sg-border);
    --sg-bg-primary: var(--sg-bg);
    --sg-bg-secondary: var(--sg-bg-subtle);
    --sg-bg-tertiary: var(--sg-bg-muted);
    --sg-text-primary: var(--sg-fg);
    --sg-text-secondary: var(--sg-fg-subtle);
    --sg-border-color: var(--sg-border);
    --sg-font-family: var(--sg-font);
}
```

**Файл: `wwwroot/themes/sg-theme-bundle.css`**

```css
/* =============================================================================
   SuperUI Theme Bundle — подключает все слои в правильном порядке
   Использование в index.html:
   <link rel="stylesheet" href="_content/SuperUI/themes/sg-theme-bundle.css">
   ============================================================================= */

@import url('./sg-tokens-primitives.css');
@import url('./sg-tokens-semantic.css');
@import url('./sg-tokens-semantic-dark.css');
@import url('./sg-tokens-component.css');
@import url('./sg-tokens-compat.css');
```

---

## 13. Blazor-компоненты

### SgThemeEditor (расширенный)

**Файл: `Components/SgThemeEditor.razor`**

```razor
@namespace SuperUI.Components
@using SuperUI.Services
@using SuperUI.Themes
@inject SgThemeService ThemeService

<div class="sg-theme-editor">
    <div class="sg-theme-editor-header">
        <h4 class="sg-theme-editor-title">Тема оформления</h4>
    </div>

    <div class="sg-theme-editor-body">
        <!-- Выбор темы -->
        <div class="sg-theme-field">
            <label>Тема</label>
            <select class="sgc-select" @onchange="OnThemeChanged">
                @foreach (var theme in ThemeService.GetAvailableThemes())
                {
                    <option value="@theme.Id"
                            selected="@(ThemeService.CurrentTheme.Id == theme.Id)">
                        @theme.Name
                    </option>
                }
            </select>
        </div>

        <!-- Режим -->
        <div class="sg-theme-field">
            <label>Режим</label>
            <div class="sg-btn-group-inline">
                @foreach (var mode in new[] { "light", "dark", "auto" })
                {
                    <button class="@GetModeClass(mode)"
                            @onclick="() => SetModeAsync(mode)">
                        @GetModeLabel(mode)
                    </button>
                }
            </div>
        </div>

        <!-- Информация о теме -->
        @if (ThemeService.CurrentTheme.Description is not null)
        {
            <div class="sg-theme-info">
                <span>@ThemeService.CurrentTheme.Description</span>
                @if (ThemeService.CurrentTheme.Author is not null)
                {
                    <span class="sg-theme-author">by @ThemeService.CurrentTheme.Author</span>
                }
            </div>
        }

        <!-- Превью токенов -->
        <div class="sg-theme-preview">
            <div class="sg-theme-preview-title">Цвета</div>
            <div class="sg-theme-swatches">
                @foreach (var (name, token) in ColorTokens)
                {
                    <div class="sg-theme-swatch-item">
                        <div class="sg-theme-swatch"
                             style="background: var(@token);"
                             title="@token"></div>
                        <span>@name</span>
                    </div>
                }
            </div>
        </div>
    </div>

    <div class="sg-theme-editor-footer">
        <span class="sg-theme-version">v@ThemeService.CurrentTheme.Version</span>
    </div>
</div>

@code {
    private static readonly (string Name, string Token)[] ColorTokens =
    {
        ("Primary",  "--sg-color-primary"),
        ("Success",  "--sg-color-success"),
        ("Danger",   "--sg-color-danger"),
        ("Warning",  "--sg-color-warning"),
        ("Info",     "--sg-color-info"),
        ("Bg",       "--sg-bg"),
        ("Surface",  "--sg-surface"),
        ("Border",   "--sg-border"),
    };

    private async Task OnThemeChanged(ChangeEventArgs e)
    {
        var id = e.Value?.ToString();
        if (!string.IsNullOrEmpty(id))
            await ThemeService.SetThemeAsync(id);
        StateHasChanged();
    }

    private async Task SetModeAsync(string mode)
    {
        await ThemeService.SetModeAsync(mode);
        StateHasChanged();
    }

    private string GetModeClass(string mode) =>
        "sgc-btn" + (ThemeService.CurrentMode == mode ? " sgc-btn-primary" : "");

    private static string GetModeLabel(string mode) => mode switch
    {
        "light" => "☀️ Светлая",
        "dark"  => "🌙 Тёмная",
        "auto"  => "🖥️ Авто",
        _ => mode
    };
}
```

### SgThemeProvider (Host-компонент)

**Файл: `Components/SgThemeProvider.razor`**

```razor
@namespace SuperUI.Components
@using SuperUI.Services
@inject SgThemeService ThemeService
@implements IAsyncDisposable

@ChildContent

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string DefaultThemeId { get; set; } = "superui-default";

    [Parameter]
    public string DefaultMode { get; set; } = "light";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ThemeService.InitializeAsync();
            ThemeService.ThemeChanged += OnThemeChanged;
        }
    }

    private void OnThemeChanged(SuperUI.Themes.IThemeDefinition theme, string mode)
    {
        InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        ThemeService.ThemeChanged -= OnThemeChanged;
        await ThemeService.DisposeAsync();
    }
}
```

---

## 14. Регистрация в DI

**Файл: `SuperUIServiceExtensions.cs`**

```csharp
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Services;
using SuperUI.Themes;

namespace SuperUI;

public static class SuperUIServiceExtensions
{
    /// <summary>
    /// Регистрирует все сервисы SuperUI.
    /// </summary>
    public static IServiceCollection AddSuperUI(
        this IServiceCollection services,
        Action<ThemeRegistry>? configureThemes = null)
    {
        // Реестр тем (singleton — создаётся один раз)
        services.AddSingleton<ThemeRegistry>(sp =>
        {
            var registry = new ThemeRegistry();
            configureThemes?.Invoke(registry);
            return registry;
        });

        // ThemeService (scoped — по одному на пользователя в Blazor Server)
        services.AddScoped<SgThemeService>();

        // Остальные сервисы
        services.AddScoped<SgToastService>();
        services.AddScoped<SgConfirmService>();
        services.AddScoped<SgNotificationService>();
        services.AddScoped<SgZIndexService>();

        return services;
    }

    /// <summary>
    /// Добавить кастомную тему.
    /// </summary>
    public static IServiceCollection AddSuperUITheme(
        this IServiceCollection services,
        IThemeDefinition theme,
        bool asDefault = false)
    {
        // Нельзя изменить singleton после регистрации напрямую —
        // используйте configureThemes в AddSuperUI()
        throw new NotSupportedException(
            "Используйте AddSuperUI(registry => registry.Register(theme)) " +
            "для добавления тем до запуска приложения.");
    }
}
```

**Использование в `Program.cs`:**

```csharp
builder.Services.AddSuperUI(themes =>
{
    // Встроенные темы (Default, Material, Tailwind) регистрируются автоматически

    // Добавить кастомную тему через Builder
    var myTheme = ThemeBuilder.Create()
        .WithId("my-brand")
        .WithName("My Brand Theme")
        .WithPrimaryColor("#8B5CF6", dark: "#A78BFA")  // Violet
        .WithSuccessColor("#22C55E")
        .WithDangerColor("#EF4444")
        .AsRounded()
        .Build();

    themes.Register(myTheme);

    // Или зарегистрировать полноценный класс:
    themes.Register(new MaterialTheme(), isDefault: false);

    // Установить тему по умолчанию
    // themes.SetDefault("material-design-3");
});
```

**Использование в `App.razor`:**

```razor
@using SuperUI.Components

<SgThemeProvider DefaultThemeId="superui-default" DefaultMode="light">
    <Router AppAssembly="@typeof(App).Assembly">
        ...
    </Router>
</SgThemeProvider>
```

**Подключение CSS в `index.html` / `_Host.cshtml`:**

```html
<!-- Порядок важен! -->
<link rel="stylesheet" href="_content/SuperUI/themes/sg-theme-bundle.css">
<link rel="stylesheet" href="_content/SuperUI/superui-components.css">

<!-- superui-theme.css теперь только алиас: -->
<!-- <link rel="stylesheet" href="_content/SuperUI/superui-theme.css"> -->
```

---

## 15. Шаги для агента — пошаговая инструкция

### ШАГ 1: Создать папку `Themes/` и интерфейсы

```
Действие: Создать файлы в SuperUI/Themes/
Файлы:
  - IThemePrimitives.cs    ← интерфейс примитивов (см. раздел 8)
  - IThemeSemantic.cs      ← интерфейс семантики
  - IThemeComponents.cs    ← интерфейс компонентов
  - IThemeDefinition.cs    ← главный интерфейс
  - ThemeBase.cs           ← базовый класс с GenerateCss()
  - ThemeRegistry.cs       ← реестр тем
  - ThemeBuilder.cs        ← Fluent API
  - DefaultTheme.cs        ← реализация Default темы
  - MaterialTheme.cs       ← Material Design 3
  - TailwindTheme.cs       ← Tailwind CSS
  - CustomTheme.cs         ← шаблон кастомной темы

Команды:
  mkdir SuperUI/Themes
  # Создать каждый файл с кодом из раздела 8-9
```

### ШАГ 2: Создать CSS-файлы токенов

```
Действие: Создать файлы в SuperUI/wwwroot/themes/
Файлы:
  - sg-tokens-primitives.css      ← сырые значения (раздел 5)
  - sg-tokens-semantic.css        ← семантика light (раздел 6)
  - sg-tokens-semantic-dark.css   ← семантика dark
  - sg-tokens-component.css       ← компонентные токены (раздел 7)
  - sg-tokens-compat.css          ← алиасы --sui-* (раздел 12)
  - sg-theme-bundle.css           ← точка входа

Команды:
  mkdir SuperUI/wwwroot/themes
  # Создать каждый CSS файл с кодом из соответствующих разделов
```

### ШАГ 3: Обновить `SgThemeService.cs`

```
Действие: Заменить содержимое SuperUI/Services/SgThemeService.cs
Код: раздел 10

Важно: Сохранить сигнатуры:
  - public string CurrentTheme      → ПЕРЕИМЕНОВАТЬ в CurrentThemeId (или обернуть)
  - public event Action<string>? ThemeChanged → РАСШИРИТЬ до Action<IThemeDefinition, string>?
  - InitializeAsync() → сохранить
  - SetThemeAsync(string) → теперь принимает ID темы
  - ToggleThemeAsync() → сохранить
```

### ШАГ 4: Зарегистрировать сервисы

```
Действие: Обновить SuperUI/SuperUIServiceExtensions.cs (или ServiceCollectionExtensions.cs)
Код: раздел 14

Добавить в Program.cs проекта-потребителя:
  builder.Services.AddSuperUI(themes => {
      // кастомные темы...
  });
```

### ШАГ 5: Обновить компоненты

```
Действие: Обновить файлы компонентов
- SuperUI/Components/SgThemeEditor.razor     → раздел 13
- SuperUI/Components/SgThemeEditor.razor.cs  → упростить, вся логика в SgThemeService
- SuperUI/Components/SgThemeSwitcher.razor   → использовать SgThemeService.ToggleModeAsync()
- Создать SuperUI/Components/SgThemeProvider.razor  → раздел 13
```

### ШАГ 6: Обновить `superui-theme.css` (обратная совместимость)

```css
/* superui-theme.css — DEPRECATED, используйте sg-theme-bundle.css */
/* Этот файл сохранён для обратной совместимости */

/* Импортируем новую систему */
@import url('./themes/sg-theme-bundle.css');

/* Старые переменные теперь алиасируются через sg-tokens-compat.css */
/* Ничего не нужно добавлять здесь */
```

### ШАГ 7: Проверить обратную совместимость

```
Действие: Убедиться, что все --sui-* переменные доступны через алиасы

Проверить в браузере DevTools:
  getComputedStyle(document.documentElement).getPropertyValue('--sui-accent')
  → должно вернуть значение (через var(--sg-color-primary))

Компоненты, использующие старые переменные, должны работать без изменений:
  - SgButton.razor         → --sui-accent, --sui-accent-hover
  - SgTextBox.razor        → --sui-input-*, --sui-border
  - SgDataGrid.razor.css   → --sui-bg-primary, --sui-border
  - superui-components.css → все --sg-* через aliaes
```

### ШАГ 8: Обновить `index.html`

```html
<!DOCTYPE html>
<html lang="ru">
<head>
    ...
    <!-- БЫЛО -->
    <!-- <link rel="stylesheet" href="_content/SuperUI/superui-theme.css"> -->

    <!-- СТАЛО -->
    <link rel="stylesheet" href="_content/SuperUI/themes/sg-theme-bundle.css">
    <link rel="stylesheet" href="_content/SuperUI/superui-components.css">
    ...
</head>
```

### ШАГ 9: Тестирование

```
1. Переключение тем:
   ThemeService.SetThemeAsync("material-design-3")
   ThemeService.SetThemeAsync("tailwind-v3")
   ThemeService.SetThemeAsync("superui-default")

2. Переключение режимов:
   ThemeService.SetModeAsync("dark")
   ThemeService.SetModeAsync("light")
   ThemeService.SetModeAsync("auto")

3. Создание кастомной темы:
   var theme = ThemeBuilder.Create()
       .WithId("test")
       .WithName("Test Theme")
       .WithPrimaryColor("#8B5CF6")
       .AsRounded()
       .Build();
   await ThemeService.SetThemeAsync(theme);

4. Проверить, что --sui-* алиасы работают
5. Проверить все компоненты библиотеки визуально
```

---

## 16. Тестирование и валидация

### Unit-тесты

```csharp
// Tests/ThemeTests.cs

using SuperUI.Themes;
using Xunit;

public class ThemeTests
{
    [Fact]
    public void DefaultTheme_HasValidId()
    {
        var theme = new DefaultTheme();
        Assert.NotEmpty(theme.Id);
        Assert.NotEmpty(theme.Name);
    }

    [Fact]
    public void DefaultTheme_GeneratesCss_ContainsAllTokens()
    {
        var theme = new DefaultTheme();
        var css = theme.GenerateCss();

        // Проверить наличие ключевых токенов
        Assert.Contains("--sg-bg:", css);
        Assert.Contains("--sg-color-primary:", css);
        Assert.Contains("--sg-color-danger:", css);
        Assert.Contains("--sg-shadow-md:", css);
        Assert.Contains("--sgc-btn-radius:", css);
        Assert.Contains("[data-theme=\"dark\"]", css);
    }

    [Fact]
    public void MaterialTheme_HasDifferentPrimaryColor()
    {
        var defaultTheme  = new DefaultTheme();
        var materialTheme = new MaterialTheme();

        Assert.NotEqual(
            defaultTheme.Light.ColorPrimary,
            materialTheme.Light.ColorPrimary);
    }

    [Fact]
    public void TailwindTheme_HasSlateNeutral()
    {
        var theme = new TailwindTheme();
        // Tailwind Slate-50
        Assert.Equal("#F8FAFC", theme.Primitives.Neutral50);
    }

    [Fact]
    public void ThemeBuilder_BuildsCustomTheme()
    {
        var theme = ThemeBuilder.Create()
            .WithId("test-custom")
            .WithName("Test Custom")
            .WithPrimaryColor("#8B5CF6")
            .AsRounded()
            .Build();

        Assert.Equal("test-custom", theme.Id);
        Assert.Equal("Test Custom", theme.Name);

        var css = theme.GenerateCss();
        Assert.Contains("--sg-bg:", css);
        Assert.Contains("--sg-radius-md: 12px", css); // AsRounded()
    }

    [Fact]
    public void ThemeRegistry_ContainsBuiltinThemes()
    {
        var registry = new ThemeRegistry();
        var all = registry.GetAll();

        Assert.Contains(all, t => t.Id == "superui-default");
        Assert.Contains(all, t => t.Id == "material-design-3");
        Assert.Contains(all, t => t.Id == "tailwind-v3");
    }

    [Fact]
    public void ThemeRegistry_DefaultIsFirst()
    {
        var registry = new ThemeRegistry();
        var def = registry.GetDefault();

        Assert.Equal("superui-default", def.Id);
    }

    [Fact]
    public void ThemeRegistry_CustomTheme_RegisterAndGet()
    {
        var registry = new ThemeRegistry();
        var custom = ThemeBuilder.Create()
            .WithId("my-brand")
            .WithName("My Brand")
            .Build();

        registry.Register(custom);
        var found = registry.TryGet("my-brand", out var t);

        Assert.True(found);
        Assert.NotNull(t);
        Assert.Equal("My Brand", t!.Name);
    }

    [Fact]
    public void DefaultTheme_AdditionalCss_ContainsCompat()
    {
        var theme = new DefaultTheme();
        Assert.Contains("--sui-accent", theme.AdditionalCss);
        Assert.Contains("--sui-border", theme.AdditionalCss);
    }
}
```

### CSS Validation Script (JavaScript)

```javascript
// Вставить в консоль браузера для проверки токенов

const style = getComputedStyle(document.documentElement);

const requiredTokens = [
    // Semantic
    '--sg-bg', '--sg-bg-subtle', '--sg-bg-muted',
    '--sg-fg', '--sg-fg-subtle', '--sg-fg-muted',
    '--sg-border', '--sg-border-strong', '--sg-border-focus',
    '--sg-color-primary', '--sg-color-primary-subtle',
    '--sg-color-success', '--sg-color-danger',
    '--sg-color-warning', '--sg-color-info',
    '--sg-shadow-sm', '--sg-shadow-md',
    '--sg-radius-md', '--sg-radius-lg',
    // Component
    '--sgc-btn-radius', '--sgc-btn-height',
    '--sgc-input-radius', '--sgc-input-height',
    '--sgc-card-radius', '--sgc-modal-radius',
    // Compat
    '--sui-accent', '--sui-border', '--sui-bg-primary',
    '--sui-text-primary', '--sui-shadow-md',
];

const missing = requiredTokens.filter(t =>
    !style.getPropertyValue(t).trim()
);

if (missing.length === 0) {
    console.log('✅ Все токены присутствуют');
} else {
    console.error('❌ Отсутствуют токены:', missing);
}
```

---

## 17. Миграция с текущей версии

### Что изменится

| Было | Стало | Совместимость |
|------|-------|---------------|
| `--sui-bg-primary` | `--sg-bg` (алиас через compat) | ✅ Авто через compat |
| `--sui-accent` | `--sg-color-primary` (алиас) | ✅ Авто через compat |
| `--sui-text-primary` | `--sg-fg` (алиас) | ✅ Авто через compat |
| `SgThemeService.CurrentTheme: string` | `SgThemeService.CurrentTheme: IThemeDefinition` | ⚠️ API изменён |
| `SetThemeAsync(string mode)` | `SetModeAsync(string mode)` | ⚠️ Разделено |
| `ThemeChanged: Action<string>` | `ThemeChanged: Action<IThemeDefinition, string>` | ⚠️ Расширено |
| Нет поддержки тем | `ThemeRegistry` + 3 встроенных темы | 🆕 Новое |

### Пошаговая миграция

```csharp
// БЫЛО
@inject SgThemeService ThemeService

// Применить тёмную тему:
await ThemeService.SetThemeAsync("dark");
// Переключить:
await ThemeService.ToggleThemeAsync();

// ─────────────────────────────────────────────────

// СТАЛО
@inject SgThemeService ThemeService

// Применить тёмную ТЕМУ (superui-default + dark mode):
await ThemeService.SetModeAsync("dark");

// Применить Material Design тему:
await ThemeService.SetThemeAsync("material-design-3");

// Переключить light/dark:
await ThemeService.ToggleModeAsync();

// Подписаться на изменения:
ThemeService.ThemeChanged += (theme, mode) => {
    Console.WriteLine($"Theme: {theme.Name}, Mode: {mode}");
};
```

### Чеклист миграции

- [ ] Заменить `superui-theme.css` → `sg-theme-bundle.css` в index.html
- [ ] Добавить `services.AddSuperUI()` в Program.cs
- [ ] Обернуть App.razor в `<SgThemeProvider>`
- [ ] Обновить вызовы `ThemeService.SetThemeAsync("dark")` → `SetModeAsync("dark")`
- [ ] Проверить подписки на `ThemeChanged` (новая сигнатура)
- [ ] Убедиться, что `sg-tokens-compat.css` подключён (обратная совместимость --sui-*)
- [ ] Проверить компоненты в браузере (все 3 темы, light + dark)

---

## Итог

Система состоит из **4 слоёв**:

```
┌─────────────────────────────────────────────────────────────┐
│  C# Layer (генерация)                                        │
│  IThemeDefinition → ThemeBase → GenerateCss()               │
│  ThemeBuilder (Fluent API) → BuiltTheme                     │
│  ThemeRegistry → SgThemeService                              │
└──────────────────────────┬──────────────────────────────────┘
                           │ генерирует CSS в <style id="sg-theme-vars">
┌──────────────────────────▼──────────────────────────────────┐
│  Primitive Tokens (--sg-p-*)                                 │
│  Сырые значения: цвета, размеры, шрифты                      │
└──────────────────────────┬──────────────────────────────────┘
                           │ ссылаются на
┌──────────────────────────▼──────────────────────────────────┐
│  Semantic Tokens (--sg-*)                                    │
│  Абстракции: bg, fg, border, color-primary, color-danger...  │
│  Light + Dark режимы через [data-theme]                      │
└──────────────────────────┬──────────────────────────────────┘
                           │ ссылаются на
┌──────────────────────────▼──────────────────────────────────┐
│  Component Tokens (--sgc-*)                                  │
│  Специфика: sgc-btn-radius, sgc-input-height...              │
│  + Compat Aliases (--sui-* → --sg-*)                         │
└──────────────────────────┬──────────────────────────────────┘
                           │ потребляют
┌──────────────────────────▼──────────────────────────────────┐
│  Blazor Components (.razor / .razor.css)                     │
│  SgButton, SgTextBox, SgDataGrid, SgModal...                 │
└─────────────────────────────────────────────────────────────┘
```

**Встроенные темы**: Default (текущая) · Material Design 3 · Tailwind CSS v3 · Custom (через Builder)

**Ключевые файлы для создания**:
1. `Themes/*.cs` — 10 файлов интерфейсов и реализаций
2. `wwwroot/themes/*.css` — 6 CSS файлов
3. `Services/SgThemeService.cs` — расширенный сервис
4. `Components/SgThemeProvider.razor` — новый компонент-обёртка
5. `Components/SgThemeEditor.razor` — обновлённый редактор
