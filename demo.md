# SuperUI Demo Pages & Component Usage

This document lists all demonstration pages in the `SuperUI.Demo` project and the specific components they showcase.

## 🤖 AI & Agents
| Page | File Path | Components Showcased |
| :--- | :--- | :--- |
| **LangGraph (Workflows)** | `LangGraphDemo.razor` | `LangGraphProvider`, `GraphStreamingChat`, `LangGraphVisualizer`, `SgLlmSettings`, `HumanInTheLoopInterrupter`, `AgentCheckpointManager` |
| **RAG (AI Search)** | `RagDemo.razor` | `SgRagProvider`, `SgRagChat`, `SgRagDocumentUploader`, `SgRagVectorDbPanel`, `SgRagEmbeddingVisualizer` |
| **LLM Studio** | `LlmStudioDemo.razor` | `SgLlmStudio`, `SgLlmSettings`, `SgOllamaDashboard` |
| **Smart Forms** | `SmartFormDemo.razor` | `SgSmartForm`, `SgSmartFormBuilder`, `SgLlmSettings` |
| **Voice Forms** | `VoiceFormDemo.razor` | `SgSmartForm`, `SgVoiceInput`, `SgLlmSettings` |
| **Document Extractor**| `DocumentExtractorDemo.razor` | `SgDocumentExtractor` |
| **Eye Tracking** | `AccessibilityDemo.razor` | `SgEyeTracker` |
| **Whiteboard** | `ExcalidrawDemo.razor` | `SgExcalidraw` |
| **Web Terminal** | `TerminalDemo.razor` | `SgTerminal` |

## 📊 Data Management
| Page | File Path | Components Showcased |
| :--- | :--- | :--- |
| **Data Grid (Main)** | `DataGridDemo.razor` | `SgDataGrid`, `SgDataGridColumn`, `SgSavedViews`, `SgDataGridFilterBar` |
| **Dexie (IndexedDB)** | `DexieDemo.razor` | `SgDexieProvider`, `SgDexieExplorer`, `SgButton`, `SgStack` |
| **DuckDB SQL** | `DuckDbDemo.razor` | `SgDataGrid`, `SgDataGridColumn` |
| **Pivot Table** | `PivotDemo.razor` | `SgPivotTable`, `SgPivotField`, `SgPivotDesigner` |
| **Kanban Board** | `KanbanDemo.razor` | `SgKanban`, `SgKanbanColumn`, `SgKanbanTask` |
| **Gantt Charts** | `GanttDemo.razor` | `SgGantt`, `SgGanttTask` |
| **Canvas Grid** | `CanvasGridDemo.razor` | `SgCanvasGrid` |
| **Tree Data Grid** | `TreeDataGridDemo.razor` | `SgTreeDataGrid` |
| **Property Grid** | `PropertyGridDemo.razor` | `SgPropertyGrid`, `SgDescriptions` |

## 📈 Visualization
| Page | File Path | Components Showcased |
| :--- | :--- | :--- |
| **ECharts Viz** | `EChartsDemo.razor` | `SgECharts`, `SgEChartsDataPoint` |
| **D3 Visualization** | `D3Demo.razor` | `SgD3Chart`, `SgD3Link` |
| **Diagram Editor** | `OrgChartDemo.razor` | `SgDiagram`, `SgDiagramEditor`, `SgOrgChart` |
| **BPMN Viewer** | `BpmnDemo.razor` | `SgBpmn` |
| **3D Digital Twin** | `ThreeDemo.razor` | `SgThree`, `SgWarehouseCell` |
| **3D Pallet Packer** | `PalletPackerDemo.razor` | `SgPalletPacker` |
| **Network Trace** | `NetworkTraceDemo.razor` | `SgNetworkTrace` |
| **Konva 2D Graphics** | `KonvaDemo.razor` | `SgKonva`, `SgFloorRoom` |
| **Mermaid Diagrams** | `MermaidDemo.razor` | `SgMermaid` |

## 🏗️ Layout & Navigation
| Page | File Path | Components Showcased |
| :--- | :--- | :--- |
| **Docking System** | `DockManagerDemo.razor` | `SgDockManager`, `SgDockPane`, `SgDockPanel` |
| **Command Palette** | `CommandPaletteDemo.razor` | `SgCommandPalette`, `SgIcon` |
| **Tour Guide** | `TourDemo.razor` | `SgTour`, `SgTourStep`, `SgButton` |
| **Navigation System**| `NavigationDemo.razor` | `SgNavMenu`, `SgBreadcrumb`, `SgStepper`, `SgPagination` |
| **Spacing System** | `SpaceDemo.razor` | `SgSpace`, `SgButton`, `SgTag` |
| **Layout Blocks** | `LayoutDemo.razor` | `SgStack`, `SgRow`, `SgCol`, `SgSplitter`, `SgHeader`, `SgFooter` |
| **Ribbon UI** | `RibbonDemo.razor` | `SgRibbon`, `SgRibbonTab`, `SgRibbonGroup`, `SgRibbonButton` |

## 📝 Industrial & Hardware
| Page | File Path | Components Showcased |
| :--- | :--- | :--- |
| **Browser APIs** | `BrowserFeatures.razor` | `SgFileSystem`, `SgSerialPort`, `SgUsbManager`, `SgWebRTC`, `SgBluetooth`, `SgWebNFC` |
| **Barcode Scanner** | `BarcodeScannerDemo.razor` | `SgBarcodeScanner`, `SgNativeBarcodeScanner` |
| **OCR (Text Extract)**| `OcrDemo.razor` | `SgOcr` |
| **Interaction Rec** | `RecorderDemo.razor` | `SgRecorder`, `SgComponentRecorder` |
| **Warehouse Viz** | `WarehouseDemo.razor` | `SgWarehouse`, `SgStatusPanel`, `SgKPICard` |

## 🛠️ Common UI Components
| Page | File Path | Components Showcased |
| :--- | :--- | :--- |
| **Inputs & Forms** | `InputsDemo.razor` | `SgTextBox`, `SgSelect`, `SgAutoComplete`, `SgDatePicker`, `SgColorPicker`, `SgRating`, `SgSlider`, `SgSwitch` |
| **Data Forms** | `DataFormDemo.razor` | `SgDataForm`, `SgModal`, `SgAlert` |
| **Display Widgets** | `DataDisplayDemo.razor` | `SgBadge`, `SgTag`, `SgAvatar`, `SgTimeline`, `SgActivityFeed`, `SgStatistic`, `SgEmpty`, `SgSkeleton` |
| **Spinners** | `SpinnerDemo.razor` | `SgSpinner` — 9 spinner types (Ring, Dots, Bars, Pulse, Bounce, SpinCircle, Border, Typing, Morph) with sizes, speeds, variants; Interactive Constructor with native select controls; All Spinner Types CSS grid gallery; Interactive Progress mode with gradients, thickness, real-time slider, and determinate animation |
| **Buttons** | `ButtonDemo.razor` | `SgButton` — all variants, sizes, loading state, progress bar/ring (`ProgressType`, `ProgressSpinnerType`), glow/pulse/glass effects, danger confirm, countdown, live constructor with auto-generated code |
| **Overlays** | `ModalDemo.razor` | `SgModal`, `SgDrawer`, `SgTooltip`, `SgPopover`, `SgDropdown` |
| **PDF Suite** | `PdfSuiteDemo.razor` | `SgPdfViewer`, `SgPdfFormFiller` |
| **Weather** | `WeatherDemo.razor` | `SgWeatherDashboard` |

---
*Total Demo Pages: ~100+ | Organized by Functional Domain*
