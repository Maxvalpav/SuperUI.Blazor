# 🔍 Аудит библиотеки SuperUI.Blazor


---


## 2. README.md — ошибки и недочёты



---


---

#### 2.3. Описание `SgAlertVariant.Warn` — несоответствие

В таблице компонентов:
```
SgAlert | Inline alert (success/info/warn/danger)
```

В примере enum-параметров:
```razor
<SgAlert Variant="SgAlertVariant.Warn" />
```

Но в CSS классы имеют имя `.sg-badge-warn`, `.sgc-btn` — нет единообразия. В одном месте `Warn`, в другом коде нет подтверждения этого значения enum.  
**Рекомендация:** добавить явный пример всех вариантов `SgAlertVariant` в README.

---

### 🟡 Предупреждения

#### 2.4. Устаревшая версия Chart.js

В README указана зависимость:
```html
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/..."></script>
```

Актуальная версия Chart.js — **4.4.7+** (2026). Версия `4.4.0` содержит известные баги с легендой и tooltip-ами.

**Исправление:**
```html
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.7/dist/chart.umd.min.js"></script>
```

---


---




---

## 3. CSS — ошибки и улучшения

### superui-theme.css

#### 🔴 3.1. Глобальный `transition` на все элементы — проблема производительности

```css
/* Текущий код */
* {
    transition: background-color 0.2s ease, color 0.2s ease, border-color 0.2s ease;
}
```

**Проблемы:**
- Применяется ко ВСЕМ элементам на странице, включая `canvas`, `svg`, `video`, `img`
- Значительно замедляет перерисовку при смене темы на страницах с большим количеством элементов
- Canvas-элементы с `transition` на `background-color` вызывают мерцание
- Нарушает правило `prefers-reduced-motion`

**Исправление:**
```css
/* Только на элементы, которые реально переключают тему */
.sg-theme-transition,
.sg-theme-transition *:not(canvas):not(img):not(video):not(svg) {
    transition: background-color 0.2s ease, color 0.2s ease, border-color 0.2s ease;
}

@media (prefers-reduced-motion: reduce) {
    * {
        transition: none !important;
        animation-duration: 0.01ms !important;
    }
}
```

---

#### 🔴 3.2. Отсутствует CSS-переменная `--sui-spacing-5`

Ряд spacing-переменных:
```css
--sui-spacing-1: 0.125rem; /* 2px */
--sui-spacing-2: 0.25rem;  /* 4px */
--sui-spacing-3: 0.375rem; /* 6px */
--sui-spacing-4: 0.5rem;   /* 8px */
--sui-spacing-6: 0.75rem;  /* 12px */
/* ❌ --sui-spacing-5 отсутствует! (10px) */
```

Пропущен шаг `5` (10px). Это нарушает консистентность шкалы отступов.

**Исправление:**
```css
--sui-spacing-5: 0.625rem; /* 10px */
```

---

#### 🔴 3.3. Отсутствует переменная `--sui-spacing-8` и выше

Нет отступов для `16px`, `20px`, `24px`, `32px` — стандартных значений для padding карточек и секций. В компонентах используются хардкодные значения `padding: 24px`, `gap: 16px` вместо переменных.

**Добавить:**
```css
--sui-spacing-8:  1rem;    /* 16px */
--sui-spacing-10: 1.25rem; /* 20px */
--sui-spacing-12: 1.5rem;  /* 24px */
--sui-spacing-16: 2rem;    /* 32px */
```

---

#### 🟡 3.4. Закомментированный блок `auto` темы не работает правильно

```css
/* Automatic Dark Mode based on system preference — disabled, light is default */
/* Uncomment to re-enable auto system theme detection:
@media (prefers-color-scheme: dark) {
    :root:not([data-theme="light"]) { ... }
}
*/
```

Блок закомментирован, но в `AddSuperUI()` есть опция `DefaultTheme = "auto"`. При использовании `"auto"` ничего не произойдёт — JS не применяет медиа-запрос, CSS заблокирован.

**Исправление:** либо реализовать `auto`-тему через JS при инициализации (читать `window.matchMedia`), либо удалить опцию `"auto"` из конфигурации до реализации.

---


```

---

#### 🟡 3.6. Планировщик (SgScheduler) стили в файле темы

Стили `.sg-scheduler-*` находятся в `superui-theme.css`, хотя логически это стили компонента, а не темы. Файл темы должен содержать только CSS-переменные и сбросы.

**Рекомендация:** перенести `.sg-scheduler-*` в `SgScheduler.razor.css` или в `superui-components.css`.

---

#### 🟡 3.7. Нет CSS-переменной для z-index

Дропдауны, модалки, тултипы — все используют захардкоженные значения (`z-index: 200`, `z-index: 10`, `z-index: 1050`). При наложении компонентов могут возникать конфликты.

**Добавить:**
```css
--sui-z-dropdown:    100;
--sui-z-sticky:      200;
--sui-z-modal:       300;
--sui-z-toast:       400;
--sui-z-tooltip:     500;
```

---

### superui-components.css

#### 🔴 3.8. Дублирование переменных — двойное объявление `:root`

В файле два блока `:root {}` — сначала алиасы `--sg-*`, потом алиасы `--sui-*`:

```css
/* Блок 1 */
:root {
    --sg-bg-primary: var(--sui-bg-primary);
    --sg-text-primary: var(--sui-text-primary);
    /* ... */
}

/* Блок 2 (позже в том же файле) */
:root {
    --sui-bg: var(--sui-bg-primary);
    --sui-fg: var(--sui-text-primary);
    --sui-text: var(--sui-text-primary);         /* ❌ дубль --sui-text-primary */
    --sui-text-secondary: var(--sui-text-secondary); /* ❌ самоссылка! */
    /* ... */
}
```

**Критическая ошибка:** `--sui-text-secondary: var(--sui-text-secondary)` — переменная ссылается сама на себя! Это создаёт циклическую зависимость и браузер вернёт `initial` значение.

**Исправление:** объединить в один блок `:root`, убрать самоссылку.

---

#### 🔴 3.9. Самоссылка CSS-переменной

```css
:root {
    --sui-text-secondary: var(--sui-text-secondary); /* ❌ ЦИКЛИЧЕСКАЯ ЗАВИСИМОСТЬ */
}
```

Это прямая ошибка — `--sui-text-secondary` объявлена в `superui-theme.css` со значением `#4b5563`, но затем переопределяется на саму себя в `superui-components.css`. В результате браузер не может разрешить значение и возвращает пустое значение (fallback).

**Исправление:** удалить эту строку полностью.

---

#### 🔴 3.10. Глобальный scrollbar для `*` перекрывает кастомные стили

```css
/* Firefox */
* {
    scrollbar-width: thin;
    scrollbar-color: var(--sui-border-hover, #b0b0b0) transparent;
}
```

Применение `scrollbar-width: thin` ко всем элементам (`*`) ломает нативный скроллбар в некоторых браузерах и конфликтует с `.sg-thin-scroll`, который объявлен отдельно в том же файле.

**Исправление:** применять только к контейнерам-оверфлоу:
```css
.sg-scroll, [data-scroll], .sg-panel-container, body {
    scrollbar-width: thin;
    scrollbar-color: var(--sui-border-hover, #b0b0b0) transparent;
}
```

---

#### 🔴 3.11. Кнопка: дебаунс-логика блокирует UI без визуальной индикации

В CSS нет стиля для состояния `_isDebouncing`. В JavaScript/Razor логика есть, а CSS-класс для "кнопка временно заблокирована" — нет. Пользователь не понимает, почему кнопка не реагирует.

**Добавить:**
```css
.sgc-btn.sgc-debouncing {
    opacity: 0.7;
    cursor: wait;
    pointer-events: none;
}
```

---

#### 🟡 3.12. `.sgc-btn` — использование `translate` как shorthand

```css
.sgc-btn:hover:not(:disabled) {
    translate: 0 -1px; /* ⚠️ CSS Transforms Level 2, не поддерживается в Safari < 14.1 */
}
```

Свойство `translate` как самостоятельное (не `transform: translate(...)`) — относительно новый синтаксис. Если нужна поддержка Safari 14 и ниже, использовать `transform`.

**Исправление:**
```css
.sgc-btn:hover:not(:disabled) {
    transform: translateY(-1px);
}
```

---

#### 🟡 3.13. Использование `color-mix()` без fallback

```css
background: color-mix(in srgb, var(--sui-danger) 4%, var(--sui-bg-primary));
```

`color-mix()` не поддерживается в Firefox < 113, Chrome < 111. Нет fallback-значения.

**Исправление:**
```css
background: rgba(244, 63, 94, 0.04); /* fallback */
background: color-mix(in srgb, var(--sui-danger) 4%, var(--sui-bg-primary));
```

---

#### 🟡 3.14. `.sgc-btn` — отсутствует `aria`-стиль для `[aria-busy="true"]`

При состоянии Loading нет CSS-поддержки атрибута `aria-busy`:
```css
/* Добавить */
.sgc-btn[aria-busy="true"] {
    cursor: wait;
}
```

---

#### 🟡 3.15. `.sg-accent-bar::before` — неправильный escape строки

```css
.sg-accent-bar::before {
    content: \"\";  /* ❌ Лишнее экранирование в исходнике */
}
```

В исходном CSS-файле это записано как `content: \"\"` — если это сырой CSS (не внутри C#-строки), лишние обратные слеши некорректны.

**Исправление:**
```css
.sg-accent-bar::before {
    content: "";
}
```

---

#### 🟡 3.16. Анимация `sg-indeterminate` — некорректный конечный процент

```css
@keyframes sg-indeterminate {
    0%   { transform: translateX(-100%); }
    100% { transform: translateX(350%); }  /* ⚠️ магическое число */
}
```

Значение `350%` — захардкоженное "магическое число", которое зависит от ширины элемента (40%). Правильнее использовать `250%` или реализовать через `translateX(calc(100% / 0.4))`.

---

#### 🟡 3.17. `.sg-scheduler-week-header-cell` — неверный селектор

```css
/* В CSS написано: */
.sg-scheduler-week-header-cell:last-child {
    border-right: none;
}

/* Но элементы называются: */
.sg-scheduler-day-header-cell  /* ← в разметке */
```

Селектор `.sg-scheduler-week-header-cell` не соответствует ни одному HTML-классу. Правило никогда не применяется.

---

## 4. JavaScript — ошибки и улучшения

#### 🔴 4.1. `superui.js` — файл является HTML-заглушкой

Файл `wwwroot/superui.js` содержит:
```js
${htmlContent}  // ← это шаблонная строка, не JavaScript!
```

Это явная ошибка: файл — HTML-шаблон (очевидно, генерируется), но в репозитории лежит с нерезолвленной переменной `${htmlContent}`. Если браузер загрузит этот файл, возникнет синтаксическая ошибка.

**Исправление:** либо удалить файл (JS загружается через ES-модули из `.razor.js`), либо заменить корректным содержимым.

---

#### 🔴 4.2. Нет обработки ошибок в JS-интеропе

Компоненты вызывают JS через `IJSRuntime`. Если JS-модуль не загружен (например, при SSR или медленном соединении), Blazor выбросит необработанное исключение.

**Рекомендация:** обернуть все JS-вызовы в `try/catch`:
```csharp
try
{
    await JS.InvokeVoidAsync("superui.someMethod", args);
}
catch (JSException ex)
{
    Logger.LogError(ex, "JS interop failed");
}
catch (TaskCanceledException)
{
    // Component disposed — ignore
}
```

---

#### 🔴 4.3. Отсутствие проверки `DotNetObjectReference` на disposal

В компонентах, передающих `DotNetObjectReference` в JS (например, `SgCanvasGrid`, `SgAnchor`), не видно явного `Dispose()` этих ссылок. Это приводит к утечкам памяти.

**Исправление:**
```csharp
private DotNetObjectReference<MyComponent>? _dotNetRef;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("module.init", _dotNetRef);
    }
}

public async ValueTask DisposeAsync()
{
    _dotNetRef?.Dispose(); // ✅ обязательно
    await JS.InvokeVoidAsync("module.dispose", /* id */);
}
```

---

#### 🟡 4.4. Отсутствие `ResizeObserver` cleanup

Если компоненты используют `ResizeObserver` для адаптивного ресайза (что вероятно для `SgCanvasGrid`, `SgSplitter`, `SgDockWindow`), необходимо отключать observer в `dispose`:
```js
// В JS-модуле
export function dispose(id) {
    const state = _instances.get(id);
    if (state?.resizeObserver) {
        state.resizeObserver.disconnect(); // ✅
    }
    _instances.delete(id);
}
```

---

#### 🟡 4.5. `SgAnchor.razor.js` — вероятная утечка событийных слушателей

Если в `SgAnchor.razor.js` добавляются `scroll`/`resize` listeners без последующего удаления через `removeEventListener`, это приводит к утечкам памяти при частом монтировании/размонтировании компонента.

---

## 5. C# / Razor — ошибки и улучшения

### SgButton.razor

#### 🔴 5.1. Неправильный порядок дебаунса — действие выполняется ДО задержки

```csharp
// Текущий код:
await OnClick.InvokeAsync(e);        // ← сначала выполняется действие
if (DebounceInterval > 0) {
    _isDebouncing = true;            // ← потом блокируется
    // ...
    await Task.Delay(DebounceInterval);
    _isDebouncing = false;
}
```

Логика дебаунсинга **некорректна**: `_isDebouncing = true` устанавливается после вызова `OnClick`, а не до. Это значит:
- При двойном клике оба события выполнятся
- `_isDebouncing = true` / `StateHasChanged()` вызывается внутри `if (DebounceInterval > 0)`, но **после** `await OnClick.InvokeAsync`

**Исправление — правильный дебаунс:**
```csharp
private async Task OnClickAsync(MouseEventArgs e)
{
    if (Disabled || Loading || _isDebouncing) return;

    if (DebounceInterval > 0)
    {
        _isDebouncing = true;
        StateHasChanged();
    }

    if (IsToggle)
    {
        Pressed = !Pressed;
        if (PressedChanged.HasDelegate)
            await PressedChanged.InvokeAsync(Pressed);
    }

    if (OnClick.HasDelegate)
        await OnClick.InvokeAsync(e);

    if (DebounceInterval > 0)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(DebounceInterval, _debounceCts.Token);
        }
        catch (OperationCanceledException) { return; }

        if (!_disposed)
        {
            _isDebouncing = false;
            StateHasChanged();
        }
    }
}
```

---

#### 🔴 5.2. `_pressed` поле объявлено, но не используется

```csharp
private bool _pressed;  // ← объявлено

// В коде:
_pressed = newPressed;  // ← присваивается
// Но нигде не читается — везде используется Pressed (параметр)
```

Поле `_pressed` — мёртвый код. Следует либо удалить, либо использовать.

---

#### 🔴 5.3. Отсутствует `@rendermode` в примерах компонентов

В README не указан обязательный `@rendermode InteractiveServer` для Server-проектов. Пользователи получат компоненты без интерактивности и не поймут, почему.

---

#### 🟡 5.4. `SgButton` — не передаётся `aria-disabled` при `Disabled=true`

Если кнопка имеет `Disabled=true`, она рендерит нативный `disabled` атрибут (хорошо), но для кастомных элементов и скринридеров также нужен `aria-disabled`:
```razor
aria-disabled="@(Disabled ? "true" : null)"
```

---

#### 🟡 5.5. Конструкция `@implements IDisposable` — предпочесть `IAsyncDisposable`

Если компонент делает async JS-вызовы при уничтожении (остановка таймеров, отписка от JS-событий), `IDisposable` (синхронный) не подходит. Нужен `IAsyncDisposable`:

```csharp
// Было:
@implements IDisposable

// Стало:
@implements IAsyncDisposable

public async ValueTask DisposeAsync()
{
    _debounceCts?.Cancel();
    _debounceCts?.Dispose();
    _disposed = true;
    // await JS.InvokeVoidAsync("cleanup", ...); — безопасно
}
```

---

#### 🟡 5.6. `SgDataGrid` — фрагменты разметки без ключей

В `SgDataGrid.razor` цикл `@for (var ri = 0; ri < PendingRules.Count; ri++)` не использует `@key`:

```razor
@for (var ri = 0; ri < PendingRules.Count; ri++)
{
    var rule = PendingRules[ri];
    <!-- ❌ нет @key="rule.Id" или @key="ri" -->
    <div>...</div>
}
```

Без `@key` Blazor не может правильно определить изменения в списке, что приводит к лишним перерисовкам или некорректному состоянию компонентов.

**Исправление:**
```razor
@foreach (var rule in PendingRules)
{
    <div @key="rule">...</div>  <!-- ✅ -->
}
```

---

#### 🟡 5.7. Отсутствие `CancellationToken` в async операциях фильтрации

Операции фильтрации/поиска в DataGrid (судя по структуре) — потенциально дорогостоящие. Нет отмены предыдущего запроса при быстром вводе пользователя.

---

#### 🟡 5.8. Enum `SgButtonVariant.Danger` → CSS-класс `sgc-danger` — несоответствие

```csharp
SgButtonVariant.Danger => "sgc-danger",  // ← без префикса "sgc-btn-"
SgButtonVariant.Primary => "sgc-btn-primary",  // ← с префиксом
```

Несоответствие именования: `Primary` → `sgc-btn-primary`, но `Danger` → `sgc-danger` (без `sgc-btn-`). Это нарушает консистентность.

**Исправление:**
```csharp
SgButtonVariant.Danger  => "sgc-btn-danger",
SgButtonVariant.Success => "sgc-btn-success",
SgButtonVariant.Ghost   => "sgc-btn-ghost",
```

И соответствующие CSS-классы.

---

## 6. Архитектурные замечания

#### 🟡 6.1. Два дублирующих набора CSS-переменных

Существуют параллельные системы переменных:
- `--sui-*` (основная тема, `superui-theme.css`)
- `--sg-*` (алиасы компонентов)
- `--sgc-*` (неявные, используются внутри компонентов)

Это создаёт путаницу и увеличивает размер CSS. Рекомендуется оставить одну систему `--sg-*` и убрать избыточные алиасы.

---

#### 🟡 6.2. Отсутствует `ISuperUILocalizer` документация

В README упоминается расширяемый `ISuperUILocalizer`, но нет примера реализации:
```csharp
// Что нужно реализовать — не показано
public class MyLocalizer : ISuperUILocalizer
{
    public string this[string key] => /* ??? */;
}
```

---


---

#### 🟡 6.4. Зависимость от `.NET 10` — слишком свежая

`.NET 10` на момент публикации — Preview/RC. Это ограничивает аудиторию. Рекомендуется поддержать `.NET 8` (LTS) и `.NET 9`.

```xml
<!-- Рекомендуется: -->
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

---

## 7. Доступность (a11y)

| Проблема | Компонент | Серьёзность |
|---|---|---|
| Нет `role="button"` у кастомных кнопок | SgButton | 🔴 Высокая |
| Нет `aria-expanded` у Accordion | SgAccordion | 🔴 Высокая |
| Нет `aria-label` у иконок-кнопок (toolbar) | sg-tool-btn | 🔴 Высокая |
| Нет `aria-live` у toast-уведомлений | SgToastHost | 🔴 Высокая |
| Нет управления фокусом в модальных окнах | SgModal | 🔴 Высокая |
| Нет `aria-busy` при Loading | SgButton, SgCard | 🟡 Средняя |
| Нет `aria-invalid` при ошибке валидации | Все inputs | 🟡 Средняя |
| Нет `prefers-reduced-motion` | Анимации | 🟡 Средняя |
| Контраст текста не проверен | Все компоненты | 🟡 Средняя |

**Минимально необходимые исправления:**

```razor
<!-- SgModal — focus trap -->
<div role="dialog" aria-modal="true" aria-labelledby="modal-title" @ref="_modalRef">
    ...
</div>

<!-- SgToast -->
<div aria-live="assertive" aria-atomic="true">
    ...toast...
</div>

<!-- Иконка-кнопка -->
<button class="sg-tool-btn" aria-label="Скопировать">
    <svg>...</svg>
</button>
```

---

## 8. Итоговые рекомендации по приоритету

### 🔴 Критично (исправить немедленно)

| # | Проблема | Файл |
|---|---|---|
| 1 | Самоссылка `--sui-text-secondary: var(--sui-text-secondary)` | superui-components.css |
| 2 | `superui.js` содержит `${htmlContent}` вместо кода | superui.js |
| 3 | Дебаунс в SgButton — действие не блокируется до задержки | SgButton.razor |
| 4 | Глобальный `* { transition }` замедляет производительность | superui-theme.css |
| 5 | Утечки `DotNetObjectReference` | Все async компоненты |
| 6 | Неверный CSS-класс для `Danger` кнопки (`sgc-danger` вместо `sgc-btn-danger`) | SgButton.razor + CSS |

### 🟡 Важно (исправить в ближайшем релизе)

| # | Проблема | Файл |
|---|---|---|
| 7 | Неверный путь в `cd SuperUI` в README | README.md |
| 8 | Пропущен `--sui-spacing-5` в шкале | superui-theme.css |
| 9 | Отсутствуют `@key` в циклах DataGrid | SgDataGrid.razor |
| 10 | `_pressed` поле не используется | SgButton.razor |
| 11 | `color-mix()` без fallback | superui-components.css |
| 12 | `translate: 0 -1px` вместо `transform` | superui-components.css |
| 13 | Пустая секция Screenshots | README.md |
| 14 | Устаревшая Chart.js 4.4.0 | README.md |
| 15 | Отсутствие `aria-live` у toasts, `aria-expanded` у accordion | Компоненты |

### 🟢 Желательно (технический долг)

| # | Проблема |
|---|---|
| 16 | Добавить `CHANGELOG.md` |
| 17 | Поддержать `.NET 8` (LTS) |
| 18 | Добавить `prefers-reduced-motion` |
| 19 | Документировать `ISuperUILocalizer` |
| 20 | Объединить дублирующие CSS-переменные `--sui-*`/`--sg-*` |
| 21 | Добавить `--sui-z-*` переменные для z-index |
| 22 | Добавить параметры компонентов в README |
| 23 | Увеличить `--sui-radius-*` до визуально заметных значений |
| 24 | Добавить `coverage` badge в README |

---

