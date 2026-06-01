# SuperUI 2.0 — Token & Theming Refactor

## Контекст

Аудит выявил критические проблемы в токен-системе и темизации:

- **95 ссылок** на несуществующий `--sg-color-warn` (тихий фолбэк)
- **~200 ссылок** на отсутствующие токены (`--sg-color-text`, `--sg-bg-glass`, `--sg-easing-out`, ...)
- **Легаси-сирота** `css/superui-tokens.css` (никем не импортируется, но поставляется в NuGet, конфликтует именами)
- **Мёртвые CSS-ветки** `:root.dark` × 3 и `.sui-dark` × 1
- **Странный `:root`-блок** прямо в `superui-components.css:378`
- **Дублирование систем**: статические `sg-tokens-*.css` + C#-генератор `SgThemeGenerator` воссоздаёт похожий набор через `eval()`
- **Гонки** в `SgThemeService` (fire-and-forget 6×JS на каждый клик)
- **44 темы** зашиты в C#-классах — нельзя подключить без перекомпиляции
- **Нет слоёв** motion / state / elevation / a11y — копипаст в каждом компоненте

## Решение: мажор-релиз 2.0 (полный рефактор)

### Скоуп по фазам

| Фаза | Что | PR |
|---|---|---|
| **A** | Чистка дубликатов, мёртвых веток, алиасы для пропавших токенов | #1 |
| **B** | Runtime: debounce, idempotency, prefers-color-scheme | #5 |
| **C** | JSON-темы + Obsolete на ThemeBase | #3 |
| **D** | Новые слои motion/state/elevation/a11y | #2 |
| **E** | color-mix миграция | #6 (опц.) |
| **F** | Hardcode sweep в components.css | #4 (итеративно) |
| **G** | Тесты (audit, JSON, service) | в каждом PR |
| **H** | Конвертер тем C# → JSON | #3 |

## Подробный план

### Фаза A — стабилизация

**A1. Удалить `SuperUI/wwwroot/css/superui-tokens.css`** (легаси-сирота).
- Удалить файл и пустую папку `wwwroot/css/`.

**A2. Добавить недостающие алиасы** в `sg-tokens-compat.css`:
```css
:root, [data-theme="light"], [data-theme="dark"] {
    --sg-color-warn:           var(--sg-color-warning);
    --sg-color-warn-subtle:    var(--sg-color-warning-subtle);
    --sg-color-warn-hover:     var(--sg-color-warning-hover);
    --sg-color-warn-fg:        var(--sg-color-warning-fg);
    --sg-color-error:          var(--sg-color-danger);
    --sg-color-error-subtle:   var(--sg-color-danger-subtle);
    --sg-color-text:           var(--sg-fg);
    --sg-color-text-secondary: var(--sg-fg-subtle);
    --sg-color-surface:        var(--sg-surface);
    --sg-color-input-bg:       var(--sg-bg);
    --sg-color-bg-hover:       var(--sg-bg-muted);
    --sg-color-bg-secondary:   var(--sg-bg-subtle);
    --sg-color-primary-rgb:    37, 99, 235;
    --sg-color-primary-light:  color-mix(in oklch, var(--sg-color-primary) 70%, white);
    --sg-color-primary-dark:   color-mix(in oklch, var(--sg-color-primary) 70%, black);
    --sg-color-primary-10:     color-mix(in oklch, var(--sg-color-primary) 10%, transparent);
    --sg-bg-glass:             color-mix(in oklch, var(--sg-bg) 70%, transparent);
    --sg-bg-rgb:               255, 255, 255;
    --sg-bg-translucent:       color-mix(in oklch, var(--sg-bg) 50%, transparent);
    --sg-bg-hover:             var(--sg-bg-muted);
    --sg-border-hover:         var(--sg-border-strong);
    --sg-border-soft:          var(--sg-border-subtle);
    --sg-border-disabled:      var(--sg-border-subtle);
    --sg-blur-glass:           10px;
    --sg-easing-out:           cubic-bezier(0, 0, 0.2, 1);
    --sg-easing-in-out:        cubic-bezier(0.4, 0, 0.2, 1);
    --sg-easing-emphasized:    cubic-bezier(0.2, 0, 0, 1);
    --sg-radius-3xl:           1.5rem;
    --sg-zindex-modal:         var(--sg-z-modal);
}
```

**A3. Переместить `:root` блок** из `superui-components.css:378-381` в `sg-tokens-component.css`:
```css
--sgc-progress-glow:  color-mix(in oklch, var(--sg-color-primary) 30%, transparent);
--sgc-progress-track: color-mix(in oklch, var(--sg-fg) 8%, transparent);
```

**A4. Удалить мёртвые CSS-ветки**:
- `:root.dark .sgc-num-icon` (10121)
- `:root.dark .sgc-num-range-track` (10145)
- `:root.dark .sgc-input-icon` (16173)
- `.sui-dark .sgc-spinner-backdrop` → заменить на `[data-theme="dark"] .sgc-spinner-backdrop`

**A5. `sgt-*` → переименовать в `sgc-table-*`** или завести в `sg-tokens-component.css`:
- `--sgt-anim-duration`, `--sgt-empty-color`, `--sgt-header-bg`, `--sgt-item-hover`, `--sgt-item-radius`, `--sgt-item-selected`, `--sgt-radius`

**A6. Hardcode sweep** в `superui-components.css` (итеративно, ~150-200 правок):
- `rgba(0,0,0,0.0X)` → color-mix / elevation-токены
- `rgba(255,255,255,0.0X)` → color-mix
- `box-shadow: 0 4px 16px var(--sg-color-primary-subtle)` → `--sg-elev-2`

### Фаза B — runtime

**B1. `SgThemeService` — debounce + батч**:
- Один JS-вызов `SuperUI.applyThemeState(state)` вместо 6 параллельных `localStorage.setItem` + `eval`
- Debounce 150мс через `CancellationTokenSource`
- `InitializeAsync` идемпотентен (`_module != null` guard)
- `DisposeAsync` — обнуление `ThemeChanged` через сохранённый делегат

**B2. Подписка на `prefers-color-scheme`** в `superui-theme.js`:
```js
const mq = window.matchMedia('(prefers-color-scheme: dark)');
mq.addEventListener('change', () => {
    if (localStorage.getItem(MODE_KEY) === 'auto') {
        document.documentElement.setAttribute('data-theme', mq.matches ? 'dark' : 'light');
    }
});
```

**B3. Убрать C#-CSS-генерацию** для «только-компонентных» тем → `<link>` swap.

**B4. `data-*` атрибуты** на `<html>` вместо inline-style для font/density.

### Фаза C — JSON-темы

**C1. JSON-схема** `SuperUI/Themes/schemas/theme.schema.json`.

**C2. Загрузчик** `SuperUI/Themes/JsonThemeDefinition.cs` + 4 companion-класса для Primitives/Semantic/Components/Typography.

**C3. Конвертация 44 тем** через `tools/ThemeConverter/Program.cs` (dotnet-script) → `SuperUI/Themes/json/{id}.json` (embedded resources).

**C4. `ThemeBase` → `[Obsolete]`**:
```csharp
[Obsolete("Use JsonThemeDefinition + SuperUI/Themes/json/{id}.json. " +
          "Will be removed in 3.0. See migration guide.")]
public abstract class ThemeBase : IThemeDefinition { ... }
```

**C5. `ThemeRegistry` — гибридный**:
- `Register(IThemeDefinition)` (C#) → Obsolete-warning
- `Register(string path)` (JSON) → новое API

### Фаза D — новые слои токенов

**D1. `motion`** (`sg-tokens-motion.css`):
```css
:root {
    --sg-duration-instant: 60ms;
    --sg-duration-fast:    120ms;
    --sg-duration-base:    200ms;
    --sg-duration-slow:    320ms;
    --sg-easing-standard:    cubic-bezier(0.2, 0, 0, 1);
    --sg-easing-emphasized:  cubic-bezier(0.2, 0, 0, 1);
    --sg-easing-out:         cubic-bezier(0, 0, 0.2, 1);
    --sg-easing-in:          cubic-bezier(0.4, 0, 1, 1);
    --sg-easing-in-out:      cubic-bezier(0.4, 0, 0.2, 1);
}
@media (prefers-reduced-motion: reduce) {
    :root {
        --sg-duration-instant: 0ms;
        --sg-duration-fast:    0ms;
        --sg-duration-base:    0ms;
        --sg-duration-slow:    0ms;
    }
}
```

**D2. `state`** (`sg-tokens-state.css`):
```css
:root {
    --sg-state-hover-bg:         color-mix(in oklch, var(--sg-color-primary) 8%,  transparent);
    --sg-state-active-bg:        color-mix(in oklch, var(--sg-color-primary) 14%, transparent);
    --sg-state-selected-bg:      color-mix(in oklch, var(--sg-color-primary) 12%, transparent);
    --sg-state-disabled-opacity: 0.45;
    --sg-state-focus-ring:       0 0 0 var(--sg-focus-ring-width, 2px) color-mix(in oklch, var(--sg-color-primary) 30%, transparent);
}
[data-theme="dark"] {
    --sg-state-hover-bg:   color-mix(in oklch, var(--sg-color-primary) 18%, transparent);
    --sg-state-active-bg:  color-mix(in oklch, var(--sg-color-primary) 28%, transparent);
}
```

**D3. `elevation`** (`sg-tokens-elevation.css`):
```css
:root {
    --sg-elev-0: none;
    --sg-elev-1: var(--sg-shadow-sm);
    --sg-elev-2: var(--sg-shadow-md);
    --sg-elev-3: var(--sg-shadow-lg);
    --sg-elev-4: var(--sg-shadow-xl);
    --sg-elev-overlay: 0 8px 24px color-mix(in oklch, black 12%, transparent);
}
```

**D4. `a11y`** (`sg-tokens-a11y.css`):
```css
:root {
    --sg-focus-ring-width:  2px;
    --sg-focus-ring-offset: 2px;
    --sg-touch-target-min:  44px;
    --sg-contrast-on-light: var(--sg-fg);
    --sg-contrast-on-dark:  var(--sg-bg);
}
```

**D5. `sg-theme-bundle.css` — финальный порядок**:
```css
@import url('./sg-tokens-primitives.css');
@import url('./sg-tokens-semantic.css');
@import url('./sg-tokens-semantic-dark.css');
@import url('./sg-tokens-motion.css');
@import url('./sg-tokens-elevation.css');
@import url('./sg-tokens-state.css');
@import url('./sg-tokens-a11y.css');
@import url('./sg-tokens-component.css');
@import url('./sg-tokens-compat.css');
```

### Фаза E — color-mix рефактор (опционально)

Поэтапно мигрировать «сырые» subtle-токены на `color-mix(in oklch, var(--sg-color-*) 12%, transparent)`.

- 2.0: ввести обёртки в compat, не трогать существующие.
- 2.1: заменить 30+ subtle в `sg-tokens-component.css`.
- 3.0: удалить обёртки.

## Тесты (фаза G)

### G1. TokenAuditTests (xUnit, в CI)
Парсит все 9 токен-файлов + `superui-components.css`:
- Все `var(--sg-*)` в components.css должны быть определены.
- `:root`-блоков в `superui-components.css` должно быть 0.
- Селекторов `:root.dark` и `.sui-dark` должно быть 0.

### G2. JsonThemeTests
Каждый встроенный `Themes/json/*.json` парсится → golden-file сравнение CSS-выхода.

### G3. SgThemeServiceTests (bUnit + mock IJSRuntime)
- Debounce батчит 5 кликов в 1 JS-вызов.
- `InitializeAsync` идемпотентен.
- `DisposeAsync` отписывает.

### G4. PrefersColorSchemeTests
Fake-JS инжектит `matchMedia` listener, симулирует смену → проверяет `data-theme`.

## Файлы

| Файл | Действие |
|---|---|
| `SuperUI/SuperUI.csproj` | bump 2.0.0, EmbeddedResource для json |
| `wwwroot/css/superui-tokens.css` | **удалить** |
| `wwwroot/themes/sg-tokens-compat.css` | расширить (A2) |
| `wwwroot/themes/sg-tokens-component.css` | +PROGRESS, +TABLE (A3, A5) |
| `wwwroot/themes/sg-tokens-semantic-dark.css` | +dark-override (A2) |
| `wwwroot/themes/sg-tokens-motion.css` | **новый** |
| `wwwroot/themes/sg-tokens-state.css` | **новый** |
| `wwwroot/themes/sg-tokens-elevation.css` | **новый** |
| `wwwroot/themes/sg-tokens-a11y.css` | **новый** |
| `wwwroot/themes/sg-theme-bundle.css` | пересмотреть порядок (D5) |
| `wwwroot/superui-components.css` | чистка (A1-A6) |
| `wwwroot/superui-theme.js` | matchMedia, батч-apply (B1, B2) |
| `Services/SgThemeService.cs` | debounce, idempotency, data-attr (B1, B3, B4) |
| `Themes/ThemeBase.cs` | `[Obsolete]` (C4) |
| `Themes/JsonThemeDefinition.cs` | **новый** (C2) |
| `Themes/JsonTheme*.cs` | **новые** (C2) |
| `Themes/ThemeRegistry.cs` | гибридный (C5) |
| `Themes/json/*.json` | **44 новых** (C3) |
| `tools/ThemeConverter/Program.cs` | **новый** (H) |
| `SuperUI.Tests/Theming/TokenAuditTests.cs` | **новый** (G1) |
| `SuperUI.Tests/Theming/JsonThemeTests.cs` | **новый** (G2) |
| `SuperUI.Tests/Theming/SgThemeServiceTests.cs` | **новый** (G3) |
| `README.md` | раздел миграции |

## Порядок PR

1. **PR #1 «Theme cleanup 2.0-alpha»** — A1-A5, G1, [Obsolete] на ThemeBase, bump 2.0.0-alpha.1
2. **PR #2 «Token layers 2.0-beta»** — D1-D5, B4
3. **PR #3 «JSON themes 2.0-rc1»** — C1-C5, H, G2, G3
4. **PR #4 «Hardcode sweep 2.0-rc2»** — A6 (итеративно)
5. **PR #5 «Runtime polish 2.0-rc3»** — B1, B2, B3, G4
6. **PR #6 «Color-mix 2.0-final»** — E (опц.)
7. **PR #7 «Docs & release 2.0.0»** — README, changelog

## Что НЕ делаем

- ❌ Бандлер (msbuild target для склейки CSS) → 2.1
- ❌ Token Inspector / Storybook-страница → 2.1
- ❌ Полный color-mix-рефактор существующих subtle-токенов → опц.

## Метрика «готово»

- ✅ `TokenAuditTests.TokenUsage_AllDefined_Pass`
- ✅ `TokenAuditTests.NoRootBlockInComponentsCss_Pass`
- ✅ `TokenAuditTests.NoDeadDarkSelectors_Pass`
- ✅ `JsonThemeTests.AllBuiltInThemes_ParseAndMatchGoldenCss`
- ✅ `SgThemeServiceTests.Debounce_BatchesMultipleChanges`
- ✅ `SgThemeServiceTests.PrefersColorScheme_AppliesOnChange`
- ✅ Демо рендерится в Light + Dark + 44 темы (визуально)

## Не делать без отдельного RFC

- Ломать публичный API `IThemeDefinition` без `Obsolete` на 1 минор-релиз
- Удалять `ThemeBase` (до 3.0)
- Менять `[data-theme="dark"]` селектор
- Удалять `sui-*` алиасы
