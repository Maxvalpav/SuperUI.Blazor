# SuperUI — Skill для агента

Полная карта проекта, чтобы агент понимал архитектуру, паттерны и конвенции без лишних запросов.

---

## 1. Проект одной строкой

```
SuperUI.slnx
├── SuperUI/                    # NuGet-пакет SuperUI (библиотека)
│   ├── Components/             # Компоненты (90+)
│   ├── Services/               # DI-сервисы
│   ├── Base/                   # Базовые классы, билдеры, утилиты
│   ├── Enums/                  # 137 enum-файлов
│   ├── Resources/locales/      # en/ru JSON-ресурсы
│   └── wwwroot/                # JS-модули, CSS
├── SuperUI.Demo/               # Blazor WASM демо + ручной тест-харнес
│   └── Components/Pages/       # 165+ демо-страниц
└── SuperUI.Tests/              # xUnit + bUnit тесты
```

**Технологии:** .NET 10, C# latest, nullable enabled, implicit usings, Blazor WASM/Server.
**Пакет:** `SuperUI` v1.1.0, MIT, GitHub SourceLink.

---

## 2. Базовые классы (наследование)

```
ComponentBase (Blazor)
  └── SgComponentBase               # SuperUI/Base/ComponentBases/SgComponentBase.cs
        ├── [Parameter] CssClass, Style, Id, AdditionalAttributes
        ├── [Inject] ILoggerFactory, SgThemeService, ISuperUILocalizer
        ├── [CascadingParameter] HostEnvironmentContext
        ├── protected CurrentMode, IsDark
        └── IDisposable + IAsyncDisposable
              └── SgJsComponentBase  # SuperUI/Base/ComponentBases/SgJsComponentBase.cs
                    ├── abstract ModulePath → "./_content/SuperUI/superui-*.js"
                    ├── SafeInvokeAsync<T>(method, args...)
                    ├── SafeInvokeVoidAsync(method, args...)
                    ├── SelfRef (DotNetObjectReference для [JSInvokable])
                    ├── IsInteractive (SSR-безопасность)
                    ├── OnInteractiveAsync() — переопределить для JS-инициализации
                    ├── OnDisposingAsync() — переопределить для JS-очистки
                    └── Module НЕ диспозится (владеет SgJsModuleCache)
```

### Правила наследования:
- **Нет JS** → наследуй `SgComponentBase` + `@implements IDisposable`
- **Нужен JSInterop** → наследуй `SgJsComponentBase` + `@implements IAsyncDisposable`
- **Невизуальный (простой)** → можно без наследования, `@inject` напрямую
- **Не наследовать `SgJsComponentBase` если нет JS** — он тянет лишние инжекции

---

## 3. Паттерны создания компонентов

### 3a. Визуальный компонент (Razor + code-behind)

```
SuperUI/Components/{Domain}/{SgName}/
  ├── Sg{Name}.razor       # Markup — минимум логики
  └── Sg{Name}.razor.cs    # Code-behind — partial class
```

- Разметка в `.razor`, логика в `.razor.cs`
- `@namespace SuperUI.Components`
- Параметры с XML-доками
- Для сложного рендеринга (RenderTreeBuilder) — весь рендер в code-behind, `.razor` только вызывает `_renderContent`

### 3b. Невизуальный компонент (один .razor файл)

```
SuperUI/Components/{Domain}/Sg{Name}.razor
```

- Всё в одном `.razor` файле: разметка (если есть) + `@code`
- `@namespace SuperUI.Components`
- Без JS: `@inherits SgComponentBase` + `@implements IDisposable`
- С JS: `@inherits SgJsComponentBase` + `@implements IAsyncDisposable`
- Примеры: `SgClipboard`, `SgTabSync`, `SgDebounce`, `SgMediaQuery`

### 3c. JS-зависимый невизуальный (шаблон)

```razor
@namespace SuperUI.Components
@inherits SgJsComponentBase
@implements IAsyncDisposable

@code {
    /// <summary>...</summary>
    [Parameter] public ... { get; set; }

    protected override string ModulePath => "./_content/SuperUI/superui-browser-features.js";

    protected override async ValueTask OnInteractiveAsync()
    {
        await SafeInvokeVoidAsync("exportedJsFunction", args, SelfRef);
    }

    [JSInvokable]
    public async Task OnJsCallback(string data)
    {
        // .NET получил вызов из JS
        StateHasChanged();
    }

    protected override async ValueTask OnDisposingAsync()
    {
        await SafeInvokeVoidAsync("cleanupFunction");
    }
}
```

### 3d. Паттерн двустороннего биндинга

```csharp
[Parameter] public string Value { get; set; } = "";
[Parameter] public EventCallback<string> ValueChanged { get; set; }

// ВНИМАНИЕ: Не использовать @bind-Value если есть ValueChanged + другие EventCallback
// В демо используй: Value="_field" ValueChanged="@(v => { _field = v; StateHasChanged(); })"
```

---

## 4. Создание демо-страницы

```
SuperUI.Demo/Components/Pages/{Name}Demo.razor
```

### Шаблон страницы
```razor
@page "/{name}-demo"
@using SuperUI.Components
@using SuperUI.Enums

<PageTitle>{Name} — SuperUI</PageTitle>

<SgCard Variant="SgCardVariant.Default" HeaderContent="@_headerContent">
    <ChildContent>
        <div class="demo-section">
            <div class="demo-section-header">
                <SgTypography Variant="..." Weight="SgTypographyWeight.Semibold">Title</SgTypography>
                <SgTypography Size="SgTypographySize.Sm" Color="var(--sg-text-muted)">Description</SgTypography>
            </div>
            <!-- компонент -->
        </div>
    </ChildContent>
</SgCard>

@code {
    private RenderFragment _headerContent => __builder => {
        <SgStack Horizontal Align="SgAlignItems.Center" Space="SgSize.Md">
            <div style="width:40px;height:40px;border-radius:10px;background:...">
                <SgIcon Icon="@SgIcons.SomeIcon" Size="20px" />
            </div>
            <div>
                <SgTypography Variant="SgTypographyVariant.Heading4" Weight="SgTypographyWeight.Bold">Title</SgTypography>
                <SgTypography Size="SgTypographySize.Xs" Color="var(--sg-text-muted)">Subtitle</SgTypography>
            </div>
        </SgStack>
    };
}
```

### Правила для демо:
- `<SgTypography>` вместо сырых `<h1>`, `<span>`, `<b>`
- Цвета: `var(--sg-text-muted)`, `var(--sg-color-primary)` — никаких HEX
- Компоненты: только SuperUI (SgButton, SgSwitch, SgSelect, SgChip, SgAlert, SgCard...)
- Секции: `<div class="demo-section">` + `<div class="demo-section-header">`
- Для сложных демо — Live Constructor: слева контролы, справа превью

### Регистрация в меню

```razor
<!-- SuperUI.Demo/Components/Layout/AppNav.razor -->
<SgNavLink Href="{name}-demo" Text="My Component" Icon="@SgIcons.Box" />
```

Добавлять в соответствующую группу:
- `Layout & Foundation` — Row, Col, Splitter, Space, Stack, Typography...
- `Feedback & UI` — Alert, Badge, Chip, Modal, Drawer, Toast, Spinner...
- `Navigation & Structure` — Tabs, Menu, Pagination, Breadcrumb, TreeView...
- `Data Entry` — Button, Select, TextBox, Switch, Slider, DateTimePicker, Form...
- `Enterprise` — DataGrid, Gantt, Kanban, Pivot, Scheduler, OrgChart...
- `Learning & Tools` — Lifecycle, Locale Editor, Non-Visual Components
- `AI & Utils` — Chat, SmartForm, LLM Studio, Document Extractor...
- `Experimental` — Canvas Grid, Warehouse, Mermaid...

---

## 5. Локализация

### Интерфейс
```
SuperUI/Services/ISuperUILocalizer.cs  → namespace SuperUI.Localization
```

Методы:
- `this[string key]` — получение строки по ключу
- `GetString(key, args...)` — форматированная строка
- `SetLanguage(lang)` — смена языка
- `OnLocaleChanged` — событие для ререндера
- `CurrentLanguage`, `SupportedLanguages`

### Инжекция в компонент
```razor
@inject ISuperUILocalizer Localizer
// или в C#: [Inject] private ISuperUILocalizer Localizer { get; set; }
```

### Файлы ресурсов
```
SuperUI/Resources/locales/en/*.json   # Английский
SuperUI/Resources/locales/ru/*.json   # Русский
```

Каждый JSON — словарь `{ "Key": "Value" }`. Ключи группируются по доменам (`DataGrid.json`, `Settings.json`, `Common.json`).

### Добавление нового ключа
1. Добавить в `en/{Domain}.json`
2. Добавить перевод в `ru/{Domain}.json`
3. Если домена нет — создать файл (он авто-подхватится как EmbeddedResource)

---

## 6. Перечисления (Enums)

Все enums в `SuperUI/Enums/` (137 файлов). Именование: `Sg{Component}{Aspect}.cs`.

Самые частые:
- `SgSize` — Sm, Md, Lg
- `SgShadow` — Sm, Md, Lg, Xl
- `SgTypographyVariant` — Heading1-6, Body, Small, Caption
- `SgTypographyWeight` — Light, Normal, Medium, Semibold, Bold
- `SgTypographySize` — Xs, Sm, Base, Lg, Xl
- `SgOrientation` — Horizontal, Vertical
- `SgButtonVariant` — Primary, Default, Outline, Ghost, Danger, Text
- `SgCardVariant` — Default, Outlined, Elevation, Flat, Ghost
- `SgAlertVariant` — Info, Success, Warn, Danger
- `SgAlignItems` — Start, Center, End, Stretch, Baseline
- `SgJustifyContent` — Start, Center, End, Between, Around, Evenly
- `SgPlacement` — Top, Bottom, Left, Right, TopLeft, TopRight...

---

## 7. JS модули — карта

| Модуль | Путь | Назначение |
|--------|------|-----------|
| browser-features | `superui-browser-features.js` | 50+ browser API (clipboard, notification, fullscreen, media query, etc.) |
| core | `superui.js` | Общая логика |
| theme | `superui-theme.js` | Смена темы |
| components | `superui-components.js` | CSS + JS компонентов |
| datagrid | `superui-datagrid.js` | DataGrid |
| modal | `superui-modal.js` | Modal |
| portal | `superui-portal.js` | Portal |
| tooltip | `superui-tooltip.js` | Tooltip |
| heatmap | `sg-heatmap.js` | Click heatmap |

### Правила работы с JS:
- **Всегда используй `SgJsComponentBase`** вместо прямого `@inject IJSRuntime`
- `SafeInvoke*` ловит `JSDisconnectedException`, `TaskCanceledException`, `ObjectDisposedException`
- `SelfRef` — для `.invokeMethodAsync` из JS в .NET
- Функции JS — `export function` (ES module)
- Модуль кешируется `SgJsModuleCache` — один `import()` на модуль на весь circuit
- `OnInteractiveAsync()` выполняется ТОЛЬКО в интерактивном режиме (SSR-safe)

---

## 8. Сервисы DI

| Сервис | Интерфейс | Регистрация | Назначение |
|--------|-----------|-------------|-----------|
| `LocalizationService` | `ISuperUILocalizer` | Singleton | Локализация |
| `SgThemeService` | — | Scoped | Тема (light/dark/auto) |
| `SgSettingsService` | — | Scoped | Настройки (localStorage) |
| `SgToastService` | — | Scoped | Тосты |
| `SgConfirmService` | — | Scoped | Confirm-диалоги |
| `SgModalService` | — | Scoped | Модальные окна |
| `SgPageTabsService` | — | Scoped | Табы страниц |
| `SgHeatmapService` | — | Scoped | Click heatmap |
| `SgJsModuleCache` | — | Scoped | Кеш JS-модулей |

Все регистрируются через `services.AddSuperUI()`.

---

## 9. Локации важных файлов

| Что | Путь |
|-----|------|
| **Главные imports** | `SuperUI/_Imports.razor`, `SuperUI.Demo/Components/_Imports.razor` |
| **Глобальные иконки** | `SuperUI/SgIcons.cs` (90+ const SVG) |
| **Nav-меню демо** | `SuperUI.Demo/Components/Layout/AppNav.razor` |
| **Главный лейаут** | `SuperUI.Demo/Components/Layout/MainLayout.razor` |
| **Страницы демо** | `SuperUI.Demo/Components/Pages/` (165+ .razor) |
| **CSS компонентов** | `SuperUI/wwwroot/superui-components.css` |
| **CSS темы** | `SuperUI/wwwroot/superui-theme.css` |
| **JS browser features** | `SuperUI/wwwroot/superui-browser-features.js` |
| **Опции** | `SuperUI/SuperUiOptions.cs` |
| **Конфиг сборки** | `SuperUI/SuperUI.csproj` |
| **Тесты** | `SuperUI.Tests/` (xUnit + bUnit) |
| **Enums** | `SuperUI/Enums/` (137 файлов) |
| **Locale EN** | `SuperUI/Resources/locales/en/` |
| **Locale RU** | `SuperUI/Resources/locales/ru/` |
| **SgSettings (настройки)** | `SuperUI/Components/Other/SgSettings.razor` |

---

## 10. Конвенции кода

- **XML docs** на каждом `[Parameter]`, `EventCallback`, public методе
- `<see cref="..."/>` для ссылок на связанные типы
- `[Parameter] public T Prop { get; set; }` — авто-свойства
- `private` поля с `_префиксом`
- `CamelCase` для public, `_camelCase` для private
- Никаких комментариев внутри методов — только XML доки
- `@namespace SuperUI.Components` для всех компонентов
- `namespace SuperUI.Enums;` для всех enum-файлов (file-scoped)

---

## 11. Частые ошибки и их решения

| Ошибка | Причина | Решение |
|--------|---------|---------|
| `RZ9991: bind-Value` | Компонент имеет ValueChanged + другой EventCallback | В демо: вместо `@bind-Value` пиши `Value="@x" ValueChanged="@(v => { x = v; })"` |
| `CS1525: ??` | `??` в Razor-разметке | Razor не поддерживает `??` — используй `@if (x is not null) { @x }` |
| `CS0103: не найдено в контексте` | Не хватает `@using` или `@inject` | Добавь неймспейс/инжекцию |
| `SgAlertSeverity` не найден | Неверное имя | Правильно: `SgAlertVariant` (Info, Success, Warn, Danger) |
| Компонент не ререндерится | OnLocaleChanged не подписан | Подпишись в OnInitialized: `Localizer.OnLocaleChanged += StateHasChanged` |
| JS не вызывается в SSR | OnAfterRender без проверки | Используй `SgJsComponentBase` — он сам проверяет `IsInteractive` |

---

## 12. Команды

```powershell
# Сборка библиотеки
dotnet build SuperUI/SuperUI.csproj

# Сборка демо
dotnet build SuperUI.Demo/SuperUI.Demo.csproj

# Запуск демо
dotnet run --project SuperUI.Demo/SuperUI.Demo.csproj

# Тесты
dotnet test SuperUI.Tests/SuperUI.Tests.csproj

# Упаковка NuGet
dotnet pack SuperUI/SuperUI.csproj -c Release -o ./artifacts
```

---

## 13. Компоненты One-Shot Reference

### Основные UI-компоненты (для демо):
```razor
<SgButton>Click</SgButton>
<SgButton Variant="SgButtonVariant.Primary" Size="SgSize.Sm" />

<SgSwitch @bind-Value="_flag" Label="Enable" />
<SgSelect TValue="string" Items="_items" @bind-Value="_selected" />
<SgSegmented TValue="string" @bind-Value="_mode" Options="_opts" />
<SgTextBox TValue="string" @bind-Value="_text" Placeholder="..." />
<SgChip>Status</SgChip>
<SgBadge Count="5" />
<SgAlert Variant="@SgAlertVariant.Info" Closable>Message</SgAlert>
<SgCard Title="Card" Variant="SgCardVariant.Default">Body</SgCard>
<SgIcon Icon="@SgIcons.Box" Size="20px" />
<SgStack Horizontal Space="SgSize.Md" Align="SgAlignItems.Center">...</SgStack>
<SgRow Space="SgSize.Lg"><SgCol Md="6">...</SgCol><SgCol Md="6">...</SgCol></SgRow>
<SgSpinner Type="SgSpinnerType.Dots" />
<SgTooltip Text="Tooltip text">Hover me</SgTooltip>
```

### Невизуальные (behavioral):
```razor
<SgDebounce TValue="string" Value="_x" ValueChanged="..." Delay="300" OnDebouncedValue="..." />
<SgInterval @ref="_int" IntervalMs="1000" OnTick="..." AutoStart="true" />
<SgUrlParam Name="tab" Value="_t" ValueChanged="..." />
<SgMediaQuery Query="(max-width:768px)" Matching="@_mobile" NotMatching="@_desktop" />
<SgKeyboardShortcut Keys="Ctrl+S" OnExecute="..." />
<SgBeforeUnload Prevent="_dirty" Message="Unsaved changes" />
<SgVisibilitySensor Once OnVisible="..." @ref="_sensor">@Content</SgVisibilitySensor>
<SgLocalStorage Key="key" Value="_v" ValueChanged="..." />
<SgFullscreen @ref="_fs" Target="@_el" @bind-IsFullscreen="_isFs" />
<SgFocusTracker OnFocusIn="selector => ..." OnFocusOut="..." />
```

---

## 14. Структура enum-файла

```csharp
// SuperUI/Enums/Sg{Name}.cs
namespace SuperUI.Enums;
public enum Sg{Name} { Value1, Value2, Value3 }
```

Без space перед `public`, без излишних доков — название файла и enum говорит само за себя.
