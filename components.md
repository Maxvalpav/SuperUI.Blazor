# SuperUI Complete Component & Service Inventory (v5.0)

This document is the absolute source of truth for all components and services in the SuperUI library, including all associated files (`.razor`, `.cs`, `.js`, `.css`).

## 🧱 Base UI & Forms (`SuperUI/Components/Forms`)
*Standard input and interactive elements.*

| Component | Related Files | Function | Status |
| :--- | :--- | :--- | :--- |
| **SgAutoComplete** | `SgAutoComplete.razor` | Suggestion-based input. | `stable` |
| **SgButton** | `SgButton.razor`, `SgButton.razor.cs` | Clickable button with styles, loading spinner, progress bar/ring (`Progress`, `ProgressType`, `ProgressSpinnerType`). Rendering via `RenderTreeBuilder` in code-behind. | `stable` |
| **SgButtonGroup** | `SgButtonGroup.razor`, `.css` | Group of related buttons. | `stable` |
| **SgCascader** | `SgCascader.razor`, `SgCascaderOption.cs` | Cascading selection menu. | `stable` |
| **SgCheckBox** | `SgCheckBox.razor` | Boolean checkbox. | `stable` |
| **SgColorPicker** | `SgColorPicker.razor` | Color selection tool. | `stable` |
| **SgComboBox** | `SgComboBox.razor` | Editable dropdown list. | `stable` |
| **SgComboBoxEx** | `SgComboBoxEx.razor` | Extended combo box features. | `stable` |
| **SgCoordinateInput** | `SgCoordinateInput.razor`, `.css` | GPS/Map coordinate entry. | `stable` |
| **SgCron** | `SgCron.razor`, `SgCronHumanizer.cs`, `SgCronPreset.cs` | Cron expression builder. | `stable` |
| **SgCronPicker** | `SgCronPicker.razor` | Visual cron scheduler. | `stable` |
| **SgDataForm** | `SgDataForm.razor` | Auto-binding form for models. | `stable` |
| **SgDatePicker** | `SgDatePicker.razor` | Date selection calendar. | `stable` |
| **SgDateRangePicker** | `SgDateRangePicker.razor` | Start/End date selection. | `stable` |
| **SgEntityPicker** | `SgEntityPicker.razor`, `.css` | Complex object/entity selector. | `stable` |
| **SgFab** | `SgFab.razor` | Floating action button. | `stable` |
| **SgFileUpload** | `SgFileUpload.razor` | File drag-and-drop/upload. | `stable` |
| **SgJsonSchemaForm** | `SgJsonSchemaForm.razor`, `.css` | Form generation from JSON. | `stable` |
| **SgMaskedInput** | `SgMaskedInput.razor` | Formatted string input. | `experiment beta` |
| **SgMultiSelect** | `SgMultiSelect.razor` | Multiple item dropdown. | `stable` |
| **SgMultiSelectEx** | `SgMultiSelectEx.razor` | Extended multi-selection. | `stable` |
| **SgNumberEdit** | `SgNumberEdit.razor` | Inline numeric editor. | `stable` |
| **SgNumberInput** | `SgNumberInput.razor`, `.css` | Numeric entry field. | `stable` |
| **SgRadioGroup** | `SgRadioGroup.razor`, `.css` | Single selection radio list. | `stable` |
| **SgRating** | `SgRating.razor`, `.css` | Icon-based rating scale. | `stable` |
| **SgSelect** | `SgSelect.razor` | Standard select dropdown. | `stable` |
| **SgSignaturePad** | `SgSignaturePad.razor` | Handwriting capture. | `experiment beta` (Canvas) |
| **SgSlider** | `SgSlider.razor` | Value range slider. | `stable` |
| **SgSmartForm** | `SgSmartForm.razor`, `SgSmartFormBuilder.razor`, `SgSmartFormMetadata.cs`, `SgSmartFormProvider.cs` | AI-assisted form builder. | `experiment beta` |
| **SgSwitch** | `SgSwitch.razor` | Binary toggle switch. | `stable` |
| **SgTextArea** | `SgTextArea.razor` | Multi-line text entry. | `stable` |
| **SgTextBox** | `SgTextBox.razor` | Single-line text entry. | `stable` |
| **SgTimePicker** | `SgTimePicker.razor`, `.css` | Clock-based time selector. | `stable` |
| **SgTransfer** | `SgTransfer.razor`, `SgTransferItem.cs` | Dual-list item mover. | `stable` |
| **SgTreeSelect** | `SgTreeSelect.razor` | Tree-structured dropdown. | `stable` |
| **SgTerminal** | `SgTerminal.razor`, `.js` | Industrial xterm.js wrapper. | `stable` |
| **SgPalletPacker** | `SgPalletPacker.razor`, `.js` | 3D Bin Packing visualization. | `experiment beta` |
| **SgNetworkTrace** | `SgNetworkTrace.razor`, `.js` | Traceroute visualization on map. | `experiment beta` |
| **SgPdfSuite** | `SgPdfViewer.razor`, `SgPdfFormFiller.razor` | PDF viewing and form filling. | `stable` |
| **SgEyeTracker** | `SgEyeTracker.razor`, `.js` | WebGazer.js eye tracking bridge. | `experiment beta` |
| **SgExcalidraw** | `SgExcalidraw.razor`, `.js` | Collaborative whiteboard (React). | `experiment beta` |

## 📐 Layout & Containers (`SuperUI/Components/Layout`)
*Structural components and layout grids.*

| Component | Related Files | Function | Status |
| :--- | :--- | :--- | :--- |
| **SgAffix** | `SgAffix.razor` | Sticky element container. | `stable` |
| **SgCol** | `SgCol.razor`, `SgCol.razor.cs` | Grid system column. | `stable` |
| **SgCollapse** | `SgCollapse.razor` | Expandable content area. | `stable` |
| **SgDivider** | `SgDivider.razor` | Visual line separator. | `stable` |
| **SgDockManager** | `SgDockManager.razor`, `SgDockPane.razor`, `SgDockPanel.cs`, `SgDockWindow.razor` | IDE-style pane docking. | `experiment beta` (JS) |
| **SgFooter** | `SgFooter.razor` | Page/Section footer. | `stable` |
| **SgFormActions** | `SgFormActions.razor` | Action buttons for forms. | `stable` |
| **SgFormRow** | `SgFormRow.razor` | Layout row for form fields. | `stable` |
| **SgFormSection** | `SgFormSection.razor` | Titled form grouping. | `stable` |
| **SgHeader** | `SgHeader.razor` | Page/Section header. | `stable` |
| **SgPageTabs** | `SgPageTabs.razor`, `.css` | Tabbed navigation for pages. | `stable` |
| **SgResizable** | `SgResizable.razor` | Resizable box container. | `experiment beta` |
| **SgResponsiveContainer** | `SgResponsiveContainer.razor` | Media-query aware container. | `stable` |
| **SgRow** | `SgRow.razor`, `SgRow.razor.cs` | Grid system row. | `stable` |
| **SgSplitter** | `SgSplitter.razor`, `SgSplitter.razor.cs` | Pane resizing handle. Rendering via `RenderTreeBuilder` in code-behind. | `experiment beta` (JS) |
| **SgSpace** | `SgSpace.razor`, `SgSpace.razor.cs` | Set components spacing. | `stable` |
| **SgStack** | `SgStack.razor`, `SgStack.razor.cs` | Flexbox auto-layout. | `stable` |

## 📊 Data & Grids (`SuperUI/Components/Data`)
*High-performance visualization and local data storage.*

| Component | Related Files | Function | Status |
| :--- | :--- | :--- | :--- |
| **SgCanvasGrid** | `SgCanvasGrid.razor` | Canvas-based high-speed grid. | `experiment beta` (Canvas) |
| **SgDataMatrix** | `SgDataMatrix.razor` | Matrix/Spreadsheet display. | `stable` |
| **SgDataGrid** | `SgDataGrid.razor`, `SgDataGridFilterBar.razor`, `SgDataGridRowHighlighter.cs` | Advanced paged data table. | `stable` |
| **SgDexieExplorer** | `SgDexieExplorer.razor` | IndexedDB data manager. | `experiment beta` |
| **SgDexieProvider** | `SgDexieProvider.razor` | IndexedDB context provider. | `experiment beta` |
| **SgFilterBuilder** | `SgFilterBuilder.razor` | Visual query condition UI. | `stable` |
| **SgGantt** | `SgGantt.razor` | Scheduling Gantt chart. | `stable` |
| **SgKanban** | `SgKanban.razor` | Task workflow board. | `stable` |
| **SgPivotDesigner** | `SgPivotDesigner.razor` | Drag-drop pivot config. | `stable` |
| **SgPivotTable** | `SgPivotTable.razor` | Pivot data aggregation. | `stable` |
| **SgPropertyGrid** | `SgPropertyGrid.razor` | Visual object inspector. | `stable` |
| **SgQueryBuilder** | `SgQueryBuilder.razor` | Advanced logic builder. | `stable` |
| **SgTable** | `SgTable.razor`, `SgTableColumn.cs`, `SgTableHeaderGroup.razor` | Semantic HTML table wrapper. | `stable` |
| **SgTransposeGrid** | `SgTransposeGrid.razor`, `SgTransposeColumn.cs` | Rows-to-columns grid. | `stable` |
| **SgTreeDataGrid** | `SgTreeDataGrid.razor` | Nested hierarchy grid. | `stable` |
| **SgTreeView** | `SgTreeView.razor` | Classic tree navigation. | `stable` |
| **SgVerticalGrid** | `SgVerticalGrid.razor`, `SgVerticalGridRow.cs` | Name-value vertical list. | `stable` |
| **SgVirtualList** | `SgVirtualList.razor`, `.cs` | Large list virtualization. | `stable` |

## 🤖 AI & LLM (`SuperUI/Components/AI`)
*Agentic workflows, RAG, and LLM interfaces.*

| Component | Related Files | Function | Status |
| :--- | :--- | :--- | :--- |
| **AgentCheckpointManager** | `AgentCheckpointManager.razor` | Agent memory persistence. | `experiment beta` |
| **BlazorToolExecutor** | `BlazorToolExecutor.razor` | C# tool bridge for agents. | `experiment beta` |
| **GraphStreamingChat** | `GraphStreamingChat.razor` | Node-step agent chat. | `experiment beta` |
| **HumanInTheLoopInterrupter** | `HumanInTheLoopInterrupter.razor` | Action approval modal. | `experiment beta` |
| **LangGraphProvider** | `LangGraphProvider.razor` | Workflow engine context. | `experiment beta` |
| **LangGraphVisualizer** | `LangGraphVisualizer.razor` | Workflow flowchart. | `experiment beta` (Canvas) |
| **StateInspector** | `StateInspector.razor` | JSON state debugger. | `experiment beta` |
| **SgChat** | `SgChat.razor` | Standard LLM chat. | `experiment beta` |
| **SgLlmSettings** | `SgLlmSettings.razor` | Provider configuration. | `experiment beta` |
| **SgRagChat** | `SgRagChat.razor` | Search-aware AI chat. | `experiment beta` |
| **SgRagProvider** | `SgRagProvider.razor` | Local vector DB context. | `experiment beta` |

## 📈 Charts & Graphics (`SuperUI/Components/Charts` & `Other`)
*Complex visualizations and process modeling.*

| Component | Related Files | Function | Status |
| :--- | :--- | :--- | :--- |
| **SgECharts** | `SgECharts.razor` | Apache ECharts wrapper. | `experiment beta` (JS) |
| **SgChart** | `SgChart.razor` | SVG custom chart engine. | `experiment beta` |
| **SgD3Chart** | `SgD3Chart.razor` | D3.js visualization. | `experiment beta` (JS) |
| **SgDiagram** | `SgDiagram.razor`, `SgDiagramEditor.razor`, `SgDiagramNode.cs`, `SgDiagramEdge.cs` | Relationship engine. | `experiment beta` (Canvas) |
| **SgThree** | `SgThree.razor`, `.css`, `SgThreeModels.cs`, `SgThreeSources.cs` | 3D rendering (Three.js). | `experiment beta` (Canvas) |
| **SgMermaid** | `SgMermaid.razor`, `.css`, `SgMermaidModels.cs`, `SgMermaidSources.cs` | Markdown flowcharts. | `experiment beta` (JS) |
| **SgBpmn** | `SgBpmn.razor`, `.css`, `SgBpmnElementInfo.cs`, `SgBpmnSources.cs` | Process model editor. | `experiment beta` (JS) |
| **SgKonva** | `SgKonva.razor`, `.css`, `SgKonvaModels.cs`, `SgKonvaOptions.cs`, `SgKonvaSources.cs` | Canvas 2D graphics. | `experiment beta` (JS) |

## 🧭 Navigation & Overlays (`SuperUI/Components/Navigation` & `Overlays`)
*Menus, breadcrumbs, and floating UI.*

| Component | Related Files | Function | Status |
| :--- | :--- | :--- | :--- |
| **SgAnchor** | `SgAnchor.razor`, `.css`, `.js`, `AnchorItem.cs` | Scroll-to navigation. | `stable` |
| **SgBackTop** | `SgBackTop.razor` | Scroll-to-top button. | `stable` |
| **SgBreadcrumb** | `SgBreadcrumb.razor`, `BreadcrumbItem.cs` | Path trail indicator. | `stable` |
| **SgCommandBar** | `SgCommandBar.razor`, `CommandBarItem.cs` | Top action toolbar. | `stable` |
| **SgCommandPalette** | `SgCommandPalette.razor` | Launcher (Ctrl+K). | `experiment beta` (JS) |
| **SgContextMenu** | `SgContextMenu.razor`, `SgContextMenuItem.razor` | Right-click menu. | `experiment beta` (JS) |
| **SgNavMenu** | `SgNavMenu.razor`, `SgNavGroup.razor`, `SgNavLink.razor` | Sidebar tree menu. | `stable` |
| **SgRibbon** | `SgRibbon.razor`, `SgRibbonButton.razor`, `SgRibbonGroup.razor`, `SgRibbonTab.razor` | Microsoft-style ribbon UI. | `stable` |
| **SgStepper** | `SgStepper.razor`, `StepperItem.cs` | Multi-step progress UI. | `stable` |
| **SgTabs** | `SgTabs.razor`, `SgTabPanel.razor`, `SgTabItem.cs` | Dynamic tab container. | `stable` |
| **SgTour** | `SgTour.razor`, `SgTourStep.razor`, `SgTourModels.cs` | Guided app tour component. | `experiment beta` |
| **SgModal** | `SgModal.razor` | Dialog overlay. | `stable` |
| **SgDrawer** | `SgDrawer.razor` | Side sliding panel. | `stable` |
| **SgTooltip** | `SgTooltip.razor` | Hover info popup. | `stable` |
| **SgPopover** | `SgPopover.razor` | Triggered popup box. | `stable` |

## 🛠️ Industrial & Browser APIs (`SuperUI/Components/Other/Browser`)
*Advanced browser features and hardware bridges.*

| Component | Related Files | Function | Status |
| :--- | :--- | :--- | :--- |
| **SgBarcodeScanner** | `SgBarcodeScanner.razor`, `.css`, `SgBarcodeScannerModels.cs`, `SgBarcodeScannerSources.cs` | Camera-based recognition. | `experiment beta` (JS) |
| **SgOcr** | `SgOcr.razor`, `.css`, `SgOcrModels.cs`, `SgOcrSources.cs` | Image-to-text extractor. | `experiment beta` (JS) |
| **SgFileSystem** | `SgFileSystem.razor` | Local file access API. | `experiment beta` (JS) |
| **SgSerialPort** | `SgSerialPort.razor` | COM/Serial hardware access. | `experiment beta` (JS) |
| **SgUsbManager** | `SgUsbManager.razor` | WebUSB device manager. | `experiment beta` (JS) |
| **SgWebRTC** | `SgWebRTC.razor` | P2P video/audio/data. | `experiment beta` (JS) |
| **SgBluetooth** | `SgBluetooth.razor` | WebBluetooth connection. | `experiment beta` (JS) |
| **SgRecorder** | `SgRecorder.razor`, `.css`, `SgRecorderModels.cs`, `SgRecorderSources.cs` | Session/Canvas recorder. | `experiment beta` (JS) |
| **SgMidiController** | `SgMidiController.razor` | MIDI hardware access. | `experiment beta` (JS) |
| **SgComputePressure** | `SgComputePressure.razor`, `SgComputePressureState.cs` | CPU load monitoring API. | `experiment beta` |

## 🏢 Display & Misc (`SuperUI/Components/Display`)
*Visual presentation and specialized widgets.*

| Component | Related Files | Function | Status |
| :--- | :--- | :--- | :--- |
| **SgAccordion** | `SgAccordion.razor`, `SgAccordionItem.razor` | Vertical collapse list. | `stable` |
| **SgActivityFeed** | `SgActivityFeed.razor`, `.css`, `ActivityFeedItem.cs` | Timeline of events. | `stable` |
| **SgAvatar** | `SgAvatar.razor`, `SgAvatarGroup.razor` | User profile circles. | `stable` |
| **SgBadge** | `SgBadge.razor` | Status pill indicator. | `stable` |
| **SgCard** | `SgCard.razor`, `.css` | Content block with shadow. | `stable` |
| **SgChatBubble** | `SgChatBubble.razor`, `.css` | Single chat message box. | `stable` |
| **SgChip** | `SgChip.razor` | Small removable tag. | `stable` |
| **SgCountdown** | `SgCountdown.razor`, `.css` | Time remaining timer. | `stable` |
| **SgDescriptions** | `SgDescriptions.razor`, `DescriptionItem.cs` | Term-definition list. | `stable` |
| **SgEmpty** | `SgEmpty.razor` | Data-not-found placeholder. | `stable` |
| **SgNotificationBell** | `SgNotificationBell.razor`, `.css`, `SgNotificationPanel.razor` | Alert counter and popup. | `stable` |
| **SgProgress** | `SgProgress.razor`, `.css` | Circular/Linear progress with gradient support. | `stable` |
| **SgQrCode** | `SgQrCode.razor` | QR generator component. | `stable` |
| **SgSpinner** | `SgSpinner.razor` | Loading spinner: 9 types (`Ring`, `SpinCircle`, `Dots`, `Bars`, `Pulse`, `Bounce`, `Border`, `Typing`, `Morph`), determinate progress mode with gradient, completion animation, delay, easing, overlay mode. | `stable` |
| **SgStatistic** | `SgStatistic.razor` | Large KPI number display. | `stable` |
| **SgStatusPanel** | `SgStatusPanel.razor`, `.css`, `StatusPanelItem.cs` | Industrial health panel. | `stable` |
| **SgTag** | `SgTag.razor`, `.css`, `SgTagInput.razor` | Color-coded metadata. | `stable` |
| **SgTimeline** | `SgTimeline.razor`, `.cs`, `.css`, `TimelineItem.cs` | Vertical event sequence. | `stable` |
| **SgWeatherDashboard** | `SgWeatherDashboard.razor` | Weather forecast widget. | `stable` |

## 🧭 Global Services (`SuperUI/Services`)
*Infrastructure services registered in the DI container.*

| Service | File Path | Function |
| :--- | :--- | :--- |
| **SgNotificationService** | `SuperUI/Services/SgNotificationService.cs` | Global toast and alert manager. |
| **SgConfirmService** | `SuperUI/Services/SgConfirmService.cs` | Async confirmation dialogs. |
| **SgThemeService** | `SuperUI/Services/SgThemeService.cs` | CSS theme and token management. |
| **SgDexieService** | `SuperUI/Services/Data/SgDexieService.cs` | IndexedDB (Dexie.js) bridge. |
| **SgLlmService** | `SuperUI/Services/Llm/SgLlmService.cs` | Universal LLM connector. |
| **SgLangGraphService** | `SuperUI/Services/AI/SgLangGraphService.cs` | Agentic workflow orchestrator. |
| **SgRagService** | `SuperUI/Services/AI/SgRagService.cs` | Local-first RAG pipeline. |
| **SgFeatureFlagService** | `SuperUI/Services/FeatureFlags/SgFeatureFlagService.cs` | Runtime feature toggling. |
| **SgCalendarService** | `SuperUI/Services/Data/SgCalendarService.cs` | Industrial calendar and holidays. |
| **SgMqttService** | `SuperUI/Services/IoT/SgMqttService.cs` | MQTT/IIoT messaging bridge. |
| **SgPdfService** | `SuperUI/Services/Other/SgPdfService.cs` | PDF generation and processing. |
| **SgWeatherService** | `SuperUI/Services/SgWeatherService.cs` | Weather API connector. |
| **SgHeatmapService** | `SuperUI/Services/Analytics/SgHeatmapService.cs` | Click and scroll heatmap tracker. |

## 🧩 Key Enums (`SuperUI/Enums`)

| Enum | Values | Used By |
| :--- | :--- | :--- |
| **SgButtonProgressType** | `Bar`, `Ring` | `SgButton.ProgressType` |
| **SgButtonType** | `Button`, `Submit`, `Reset` | `SgButton.Type` |
| **SgButtonVariant** | `Default`, `Primary`, `Danger`, `Success`, `Ghost`, `Outlined`, `Dashed` | `SgButton.Variant` |
| **SgSize** | `Sm`, `Md`, `Lg`, `Xl` | Multiple components |
| **SgSpinnerType** | `Ring`, `Dots`, `Bars`, `Pulse`, `Bounce`, `SpinCircle`, `Border`, `Typing`, `Morph` | `SgSpinner.Type`, `SgButton.ProgressSpinnerType` |
| **SgSpinnerVariant** | `Primary`, `Default`, `Success`, `Danger`, `Warn`, `Info` | `SgSpinner.Variant` |
| **SgSpinnerSpeed** | `Slow`, `Normal`, `Fast` | `SgSpinner.Speed` |
| **SgSpinnerLabelPosition** | `Top`, `Bottom`, `Left`, `Right` | `SgSpinner.LabelPosition` |

## 📦 Bundled Assets (`SuperUI/wwwroot`)

| File | Description |
| :--- | :--- |
| `superui-components.css` | Single bundled stylesheet shipped via `_content/SuperUI/`. Contains all component styles, spinner keyframes (`sgc-spin`, `sgc-dot-bounce`, `sgc-bar-wave`, etc.), button progress styles (`sgc-btn-progress-fill`, `sgc-btn-progress-text`), and theme variables. |
| `superui-theme.css` | CSS variables for light/dark mode theming. |

---
*Inventory Audit v5.0 | Total: ~225 Components | ~35 Services | Generated: 2026-05-28*
