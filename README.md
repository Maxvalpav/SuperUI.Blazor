# SuperUI

<p align="center">
  <img src="icon.png" alt="SuperUI logo" width="128">
</p>

[![NuGet](https://img.shields.io/nuget/v/SuperUI.svg?logo=nuget)](https://www.nuget.org/packages/SuperUI)
[![Downloads](https://img.shields.io/nuget/dt/SuperUI.svg?logo=nuget)](https://www.nuget.org/packages/SuperUI)
[![Build](https://github.com/Maxvalpav/SuperUI.Blazor/actions/workflows/build-and-publish.yml/badge.svg)](https://github.com/Maxvalpav/SuperUI.Blazor/actions/workflows/build-and-publish.yml)
[![Demo](https://img.shields.io/badge/demo-GitHub%20Pages-success?logo=github)](https://Maxvalpav.github.io/SuperUI.Blazor/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Blazor component library targeting .NET 10. 90+ components across data grid, canvas grid, forms, overlays, navigation, layout, charts, kanban, gantt, pivot table, org chart, scheduler, diagram editor, and more. Dark mode, localization (en-US, ru-RU), full IntelliSense.

Most stable on **WebAssembly** — Server-side hosting is still in progress.

**Demo:** <https://maxvalpav.github.io/SuperUI.Blazor/>  
**NuGet:** <https://www.nuget.org/packages/SuperUI>

![SuperUI overview](docs/screenshots/grid2.png)

## Install

```bash
dotnet add package SuperUI
```

Targets `net10.0`. Supports Blazor WASM, Server, Web App, and Hybrid hosting models.

## Setup

`Program.cs`:
```csharp
using SuperUI;
builder.Services.AddSuperUI();
```

`_Imports.razor`:
```razor
@using SuperUI
@using SuperUI.Components
```

Host page CSS:
```html
<link rel="stylesheet" href="_content/SuperUI/superui-theme.css" />
<link rel="stylesheet" href="_content/SuperUI/superui-components.css" />
```

`MainLayout.razor` (required for toasts, confirm dialogs, portals):
```razor
<SgThemeProvider>
    <SgToastHost />
    <SgConfirmHost />
    <SgPortalHost />
    @Body
</SgThemeProvider>
```

For Blazor Web App, call `AddSuperUI()` in both Server and Client projects.

## Components

### Data
`SgDataGrid` (sorting, filtering, grouping, paging, virtualization, inline edit, CSV/Excel export, column chooser, master-detail), `SgCanvasGrid`, `SgDataMatrix`, `SgPivotTable`, `SgKanban`, `SgGantt`, `SgScheduler`, `SgTimeline`, `SgOrgChart`, `SgDiagram`/`SgDiagramEditor`, `SgTreeView`, `SgTreeSelect`, `SgTransfer`, `SgVirtualList`, `SgDashboard`

### Forms & Inputs
`SgTextBox`, `SgTextArea`, `SgNumberEdit`, `SgSelect`, `SgMultiSelect`/`SgMultiSelectEx`, `SgComboBox`/`SgComboBoxEx`, `SgAutoComplete`, `SgCascader`, `SgCheckBox`, `SgSwitch`, `SgRadioGroup`, `SgSlider`, `SgDatePicker`, `SgDateRangePicker`, `SgTimePicker`, `SgColorPicker`, `SgMaskedInput`, `SgFileUpload`, `SgRichTextEditor`, `SgDataForm`, `SgFilterBuilder`, `SgQueryBuilder`, `SgEntityPicker`, `SgButton` (variants, sizes, loading, progress bar/ring), `SgButtonGroup`, `SgCron`, `SgCronPicker`, `SgSignaturePad`

### Overlays & Feedback
`SgModal`, `SgDrawer` (resizable, all sides), `SgPopover`, `SgTooltip`, `SgContextMenu`, `SgDropdown`, `SgAlert`, `SgResult`, `SgProgress` (circular/linear, gradient), `SgSpinner` (9 types, determinate progress), `SgSkeleton`, `SgEmpty`, `SgDockWindow`

### Navigation
`SgTabs`/`SgTabPanel`, `SgMenu`, `SgNavMenu`/`SgNavGroup`/`SgNavLink`, `SgBreadcrumb`, `SgStepper`, `SgPagination`, `SgCommandBar`, `SgToolbar`, `SgSegmented`, `SgBackTop`, `SgAffix`, `SgAnchor`, `SgRibbon`, `SgCommandPalette`

### Layout
`SgCard`, `SgRow`/`SgCol` (24-column grid), `SgStack` (flex), `SgSplitter`, `SgAccordion`/`SgAccordionItem`, `SgCollapse`, `SgDivider`, `SgResizable`, `SgHeader`/`SgFooter`, `SgDescriptions`, `SgPropertyGrid`, `SgSpace`, `SgResponsiveContainer`

### Charts & Graphics
`SgChart` (Chart.js wrapper — line, bar, area, pie, doughnut, scatter, heatmap, matrix), `SgECharts`, `SgD3Chart`, `SgThree` (3D), `SgKonva` (canvas 2D), `SgMermaid` (diagrams), `SgBpmn` (process models)

### Display & Misc
`SgBadge`, `SgChip`, `SgAvatar`/`SgAvatarGroup`, `SgStatistic`, `SgCalendar`, `SgCode`, `SgQrCode`, `SgNotificationBell`/`SgNotificationPanel`, `SgPermissionGate`, `SgLanguageSwitcher`, `SgThemeSwitcher`/`SgThemeToggle`/`SgThemeEditor`, `SgCountdown`, `SgWeatherDashboard`

### Industrial & Browser APIs
`SgFileSystem`, `SgSerialPort`, `SgUsbManager`, `SgWebRTC`, `SgBluetooth`, `SgBarcodeScanner`, `SgOcr`, `SgRecorder`, `SgMidiController`, `SgComputePressure`, `SgNetworkTrace`, `SgPalletPacker`, `SgTerminal`, `SgEyeTracker`

### AI & LLM
`SgChat`, `SgLlmSettings`, `SgRagProvider`/`SgRagChat`, `LangGraphProvider`/`GraphStreamingChat`, `SgSmartForm`, `BlazorToolExecutor`, `HumanInTheLoopInterrupter`, `StateInspector`

## Services

`SgToastService` (notifications from code), `SgConfirmService` (async confirm dialogs), `SgNotificationService` (notification feed), `SgThemeService` (theme management), `SgLlmService` (LLM connector), `SgLangGraphService` (agentic workflows), `SgRagService` (RAG pipeline), `SgDexieService` (IndexedDB bridge), `SgMqttService` (MQTT/IIoT), `SgFeatureFlagService`, `SgCalendarService`, `SgPdfService`, `SgWeatherService`, `SgHeatmapService`

## Build

```bash
git clone https://github.com/Maxvalpav/SuperUI.Blazor.git
cd SuperUI
dotnet restore
dotnet build -c Release
dotnet run --project SuperUI.Demo
```

Requires .NET 10 SDK.

## Tests

```bash
dotnet test
```

Uses bUnit + xUnit.

## Screenshots

<table>
  <tr>
    <td><img src="docs/screenshots/grid2.png" alt="Data Grid" width="380" /></td>
    <td><img src="docs/screenshots/input.png" alt="Inputs"    width="380" /></td>
    <td><img src="docs/screenshots/orgchart.png" alt="Org"    width="380" /></td>
  </tr>
</table>

## License

MIT &copy; 2026 SuperUI Contributors

## Contacts

Telegram: @maksimov8val · Email: maksimov.val@rambler.ru
