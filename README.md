# SuperUI

<p align="center">
  <img src="icon.png" alt="SuperUI" width="128">
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/SuperUI"><img src="https://img.shields.io/nuget/v/SuperUI.svg?logo=nuget&label=NuGet&color=blue" alt="NuGet"></a>
  <a href="https://www.nuget.org/packages/SuperUI"><img src="https://img.shields.io/nuget/dt/SuperUI.svg?logo=nuget&color=purple" alt="Downloads"></a>
  <a href="https://github.com/Maxvalpav/SuperUI.Blazor/actions/workflows/build-and-publish.yml"><img src="https://img.shields.io/github/actions/workflow/status/Maxvalpav/SuperUI.Blazor/build-and-publish.yml?logo=github&label=CI" alt="CI"></a>
  <a href="https://maxvalpav.github.io/SuperUI.Blazor/"><img src="https://img.shields.io/badge/demo-live-success?logo=github" alt="Demo"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green" alt="License"></a>
</p>

<p align="center">
  <b>90+ Blazor components</b> — data grid, charts, gantt, kanban, scheduler, pivot, org chart, diagram editor, forms, overlays, layout, maps, AI. Dark mode, 32 themes, localization, full IntelliSense.
</p>

<p align="center">
  <a href="https://maxvalpav.github.io/SuperUI.Blazor/"><b>Live demo</b></a> ·
  <a href="https://www.nuget.org/packages/SuperUI"><b>NuGet</b></a> ·
  <code>dotnet add package SuperUI</code>
</p>

---

## Screenshots

| Home — Natura UI | DataGrid — Natura Dark | Dashboard — Calyx |
|:---:|:---:|:---:|
| ![Home](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/home-natura-ui-light.png) | ![DataGrid](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/datagrid-natura-dark.png) | ![Dashboard](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/dashboard-calyx-light.png) |

| Scheduler — Neon Dark | Charts — Solaris Dark |
|:---:|:---:|
| ![Scheduler](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/scheduler-neon-dark.png) | ![Charts](https://raw.githubusercontent.com/Maxvalpav/SuperUI.Blazor/main/docs/screenshots/charts-solaris-dark.png) |



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
<link rel="stylesheet" href="_content/SuperUI/superui-theme.css" />
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
@* Theming — light/dark toggle, 32 built-in themes *@
<SgThemeSwitcher />
```

32 built-in themes included: Natura UI, Solaris, Royal, Graphite, Forest, Neon, Glass, Chrono, Calyx, Apex, Zen, Neo, Oasis, Flux, Prism, Cosmos, Fractalis, Wave, Aurea, Sylvan, Medici, Aether, Clarity, Element, Radius, Muse, Forge, Gordian, Inclus, Reader, Signature, Cantus. Switch via `SgThemeService.SetThemeAsync("theme-id")`.

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

---

**License:** MIT &copy; 2026 SuperUI Contributors  
**Contacts:** Telegram @maksimov8val · Email maksimov.val@rambler.ru  
**Repo:** [github.com/Maxvalpav/SuperUI.Blazor](https://github.com/Maxvalpav/SuperUI.Blazor)
