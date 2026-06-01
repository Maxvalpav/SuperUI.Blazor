# SuperUI

[![NuGet](https://img.shields.io/nuget/v/SuperUI.svg?logo=nuget&label=NuGet&color=blue)](https://www.nuget.org/packages/SuperUI)
[![Downloads](https://img.shields.io/nuget/dt/SuperUI.svg?logo=nuget&color=purple)](https://www.nuget.org/packages/SuperUI)
[![CI](https://img.shields.io/github/actions/workflow/status/Maxvalpav/SuperUI.Blazor/build-and-publish.yml?logo=github&label=CI)](https://github.com/Maxvalpav/SuperUI.Blazor/actions/workflows/build-and-publish.yml)
[![Demo](https://img.shields.io/badge/demo-live-success?logo=github)](https://maxvalpav.github.io/SuperUI.Blazor/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

**90+ Blazor components** — data grid, charts, gantt, kanban, scheduler, pivot, org chart, diagram editor, forms, overlays, layout, maps, AI. Dark mode, **43 themes** (JSON-defined), localization, full IntelliSense.

[Live demo](https://maxvalpav.github.io/SuperUI.Blazor/) · [NuGet](https://www.nuget.org/packages/SuperUI) · `dotnet add package SuperUI`

---

## Screenshots

[![Home](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/1.png)](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/1.png) [![DataGrid](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/2.png)](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/2.png)

[![Dashboard](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/3.png)](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/3.png) [![OrgChart](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/4.png)](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/4.png)



---

## Get started

```razor
@* _Imports.razor *@
@using SuperUI
@using SuperUI.Components
```

```csharp
// Program.cs
builder.Services.AddSuperUI();
```

```html
<!-- wwwroot/index.html -->
<link rel="stylesheet" href="_content/SuperUI/themes/sg-theme-bundle.css" />
<link rel="stylesheet" href="_content/SuperUI/superui-components.css" />
```

```razor
@* MainLayout.razor *@
<SgThemeProvider>
    <SgToastHost />
    <SgConfirmHost />
    <SgPortalHost />
    @Body
</SgThemeProvider>
```

Targets `net10.0` — works with Blazor WebAssembly, Server, Web App, Hybrid.

> **Upgrading from 1.x?** See [`docs/MIGRATION-2.0.md`](docs/MIGRATION-2.0.md) for the full checklist.

---

## In action

```razor
@* Button with loading and toast *@
<SgButton Variant="SgButtonVariant.Primary" 
          IsLoading="@_saving"
          ProgressType="SgButtonProgressType.Ring"
          OnClick="Save">Save</SgButton>

<SgButton Variant="SgButtonVariant.Danger" 
          Type="SgButtonType.Link"
          Href="https://example.com">Delete</SgButton>
```

```razor
@inject SgToastService Toast

<button @onclick="() => Toast.Success('Done!')">Notify me</button>
```

```razor
@* Data grid with sorting, filtering, paging *@
<SgDataGrid TItem="Employee" Items="@employees"
            ShowSearch="true" PageSize="20">
    <SgDataGridColumn TItem="Employee" Title="Name" Value="@(e => e.Name)" Sortable="true" />
    <SgDataGridColumn TItem="Employee" Title="Department" Value="@(e => e.Dept)" Filterable="true" />
    <SgDataGridColumn TItem="Employee" Title="Salary" Value="@(e => e.Salary)" Format="C0" />
</SgDataGrid>
```

```razor
@* Confirm dialog *@
@inject SgConfirmService Confirm

@if (await Confirm.ConfirmAsync("Delete this record?", variant: SgAlertVariant.Danger))
{
    // user confirmed
}
```

```razor
@* Theming — light/dark toggle, 43 built-in themes *@
<SgThemeSwitcher />
```

43 built-in themes included: Natura UI, Solaris, Royal, Graphite, Forest, Neon, Glass, Chrono, Calyx, Apex, Zen, Neo, Oasis, Flux, Prism, Cosmos, Fractalis, Wave, Aurea, Sylvan, Medici, Aether, Clarity, Element, Radius, Muse, Forge, Gordian, Inclus, Reader, Signature, Cantus, **Aether, Biofilia, Calyx, Cantus, Chrono, Circadian, Clarity, Clarity Clinical, Element, Ergo, Fractalis, Gordian, Inclus, Lumina, Muse, Oasis, Prism, Reader, Signature, Sylvan, Veiled, Wave, Window**. Switch via `SgThemeService.SetThemeAsync("theme-id")`. Themes are JSON-defined (see [`docs/MIGRATION-2.0.md`](docs/MIGRATION-2.0.md#2-themes-are-now-json)) and the runtime swaps a single `<link>` element — no per-click CSS push, browser cache does the work.

---

## Components

| Category | Components |
|----------|-----------|
| **Data display** | `SgDataGrid`, `SgCanvasGrid`, `SgPivotTable`, `SgKanban`, `SgGantt`, `SgScheduler`, `SgOrgChart`, `SgDiagram`/`SgDiagramEditor`, `SgTreeView`, `SgTreeSelect`, `SgTransfer`, `SgVirtualList`, `SgDashboard`, `SgTimeline`, `SgDataMatrix`, `SgSpreadsheet` |
| **Forms & inputs** | `SgTextBox`, `SgTextArea`, `SgNumberEdit`, `SgSelect`, `SgMultiSelect`, `SgComboBox`, `SgAutoComplete`, `SgCascader`, `SgCheckBox`, `SgSwitch`, `SgRadioGroup`, `SgSlider`, `SgDatePicker`, `SgDateRangePicker`, `SgTimePicker`, `SgColorPicker`, `SgMaskedInput`, `SgFileUpload`, `SgRichTextEditor`, `SgDataForm`, `SgFilterBuilder`, `SgQueryBuilder`, `SgEntityPicker`, `SgButton`, `SgButtonGroup`, `SgCron`, `SgCronPicker`, `SgSignaturePad` |
| **Overlays & feedback** | `SgModal`, `SgDrawer`, `SgPopover`, `SgTooltip`, `SgContextMenu`, `SgDropdown`, `SgAlert`, `SgResult`, `SgProgress`, `SgSpinner` (9 types), `SgSkeleton`, `SgEmpty`, `SgDockWindow` |
| **Navigation** | `SgTabs`, `SgMenu`, `SgNavMenu`, `SgBreadcrumb`, `SgStepper`, `SgPagination`, `SgCommandBar`, `SgToolbar`, `SgSegmented`, `SgBackTop`, `SgAffix`, `SgAnchor`, `SgRibbon`, `SgCommandPalette` |
| **Layout** | `SgCard`, `SgRow`/`SgCol`, `SgStack`, `SgSplitter`, `SgAccordion`, `SgCollapse`, `SgDivider`, `SgResizable`, `SgHeader`/`SgFooter`, `SgDescriptions`, `SgPropertyGrid`, `SgSpace`, `SgResponsiveContainer` |
| **Charts & graphics** | `SgChart` (Chart.js), `SgECharts`, `SgD3Chart`, `SgThree` (3D), `SgKonva`, `SgMermaid`, `SgBpmn`, `SgExcalidraw`, `SgHeatmap` |
| **AI & LLM** | `SgChat`, `SgLlmSettings`, `SgRagProvider`, `SgRagChat`, `LangGraphProvider`, `SgSmartForm`, `BlazorToolExecutor`, `HumanInTheLoopInterrupter`, `StateInspector`, `SgTypingIndicator` |
| **Industrial & API** | `SgFileSystem`, `SgSerialPort`, `SgUsbManager`, `SgWebRTC`, `SgBluetooth`, `SgBarcodeScanner`, `SgOcr`, `SgRecorder`, `SgMidiController`, `SgComputePressure`, `SgNetworkTrace`, `SgPalletPacker`, `SgTerminal`, `SgEyeTracker` |
| **Display & misc** | `SgBadge`, `SgChip`, `SgAvatar`, `SgStatistic`, `SgCalendar`, `SgCode`, `SgQrCode`, `SgNotificationBell`, `SgPermissionGate`, `SgLanguageSwitcher`, `SgThemeSwitcher`, `SgCountdown`, `SgWeatherDashboard` |

---

## Services

| Service | What it does |
|---------|-------------|
| `SgToastService` | Show toast notifications from C# code |
| `SgConfirmService` | Async confirm dialogs |
| `SgNotificationService` | Notification feed |
| `SgThemeService` | Switch themes, light/dark mode, custom themes |
| `SgLlmService` | LLM connector |
| `SgLangGraphService` | Agentic workflows |
| `SgRagService` | RAG pipeline |
| `SgDexieService` | IndexedDB bridge |
| `SgMqttService` | MQTT / IIoT |
| `SgFeatureFlagService` | Feature toggles |
| `SgCalendarService` | Calendar events |
| `SgPdfService` | PDF generation |
| `SgWeatherService` | Weather data |
| `SgHeatmapService` | Heatmap data |

---

## Build & test

```bash
dotnet restore
dotnet build -c Release
dotnet test
dotnet run --project SuperUI.Demo
```

Requires .NET 10 SDK. Tests use bUnit + xUnit.

To regenerate the per-theme static CSS files (after adding a JSON
theme or changing `SgThemeGenerator`):

```bash
dotnet run --project tools/ThemeCssExporter/ThemeCssExporter.csproj -- "SuperUI/wwwroot/themes/css"
```

---

## What's new in 2.0

* **JSON-defined themes** (43 in alpha.1) — every theme is a JSON
  file in `Themes/json/`, validated against `Themes/schemas/theme.schema.json`.
  Adding a new theme no longer requires recompiling SuperUI.
* **New token layers** — motion, state, elevation, a11y — published
  as `_content/SuperUI/themes/sg-tokens-{motion,state,elevation,a11y}.css`
  and bundled together by `sg-theme-bundle.css`.
* **`color-mix()` everywhere** — 302 `rgba()` calls in
  `superui-components.css` are now `color-mix(in srgb, ...)` so
  tints track the active theme.
* **Link-swap runtime** — the theme switcher now mutates a single
  `<link rel="stylesheet">` in `<head>` instead of regenerating and
  re-injecting a 15-33 KB CSS string. Pre-built `.css` files ship
  with `Cache-Control: max-age=31536000, immutable`.
* **Debounced, batched, idempotent `SgThemeService`** — 150 ms debounce,
  single `applyThemeState({...})` JS round-trip, race-safe
  `DisposeAsync`, single `matchMedia` subscription that only mounts
  while the user is in `auto` mode.
* **`SgThemeGenerator.GenerateFullThemeCss(IThemeDefinition)`** is now
  public — used by the build-time exporter and by anyone who needs
  to ship a pre-built CSS for a custom theme.

See [`CHANGELOG.md`](CHANGELOG.md) for the full list and
[`docs/MIGRATION-2.0.md`](docs/MIGRATION-2.0.md) for the upgrade
checklist.

---

**License:** MIT &copy; 2026 SuperUI Contributors  
**Contacts:** Telegram @maksimov8val · Email maksimov.val@rambler.ru  
**Repo:** [github.com/Maxvalpav/SuperUI.Blazor](https://github.com/Maxvalpav/SuperUI.Blazor)
