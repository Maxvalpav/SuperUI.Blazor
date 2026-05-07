# SuperUI — CDN Library Backup

Все внешние JS/CSS библиотеки, используемые компонентами SuperUI.

## Скачать все библиотеки

```powershell
# Из корня репозитория:
.\backup\download-libs.ps1
```

Файлы сохраняются в `backup/libs/<library>/`.

---

## Полный список библиотек

| Библиотека | Версия | Файл | Компонент | Sources-класс |
|---|---|---|---|---|
| **Chart.js** | 4.4.0 | `chart.umd.min.js` | `SgChart` | `SgChartSources.ChartScript` |
| chartjs-plugin-zoom | 2.1.0 | `chartjs-plugin-zoom.min.js` | `SgChart` | `SgChartSources.ZoomScript` |
| chartjs-chart-matrix | 1.1.1 | `chartjs-chart-matrix.min.js` | `SgChart` | `SgChartSources.MatrixScript` |
| **D3.js** | 7.9.0 | `d3.min.js` | `SgD3` | `SgD3Sources.D3Script` |
| **Apache ECharts** | 5.5.1 | `echarts.min.js` | `SgECharts` | `SgEChartsSources.EChartsScript` |
| **Konva.js** | 9.3.18 | `konva.min.js` | `SgKonva` | `SgKonvaSources.KonvaScript` |
| **Mermaid** | 11.4.1 | `mermaid.min.js` | `SgMermaid` | `SgMermaidSources.MermaidScript` |
| **Monaco Editor** | 0.45.0 | `vs/loader.js` | `SgMonaco` | `SgMonacoSources.LoaderScript` |
| **OpenLayers** | 10.3.1 | `ol.js`, `ol.css` | `SgMap` | `SgMapSources.OlScript/OlCss` |
| **Leaflet** | 1.9.4 | `leaflet.js`, `leaflet.css` | `SgLeaflet` | `SgLeafletSources.LeafletScript/Css` |
| **Three.js** | r134 | `three.min.js` | `SgThree` | `SgThreeSources.ThreeScript` |
| Three OrbitControls | 0.134.0 | `OrbitControls.js` | `SgThree` | `SgThreeSources.OrbitControls` |
| Three GLTFLoader | 0.134.0 | `GLTFLoader.js` | `SgThree` | `SgThreeSources.GltfLoader` |
| **BPMN.js** | 17.11.1 | `bpmn-modeler.development.js` | `SgBpmn` | `SgBpmnSources.ModelerScript` |
| BPMN viewer | 17.11.1 | `bpmn-viewer.development.js` | `SgBpmn` | `SgBpmnSources.ViewerScript` |
| BPMN navigated viewer | 17.11.1 | `bpmn-navigated-viewer.development.js` | `SgBpmn` | `SgBpmnSources.NavigatedViewerScript` |
| BPMN diagram-js CSS | 17.11.1 | `diagram-js.css` | `SgBpmn` | `SgBpmnSources.DiagramCss` |
| BPMN bpmn-js CSS | 17.11.1 | `bpmn-js.css` | `SgBpmn` | `SgBpmnSources.BpmnFontCss` |
| BPMN embedded CSS | 17.11.1 | `bpmn-embedded.css` | `SgBpmn` | `SgBpmnSources.BpmnEmbeddedCss` |
| **Tesseract.js** | 5.1.1 | `tesseract.min.js` | `SgOcr` | `SgOcrSources.TesseractScript` |
| Tesseract worker | 5.1.1 | `worker.min.js` | `SgOcr` | `SgOcrSources.WorkerPath` |
| Tesseract WASM core | 5.1.1 | `tesseract-core.wasm.js` | `SgOcr` | `SgOcrSources.CorePath` |
| **Transformers.js** | 2.17.2 | `transformers.min.js` | `SgRag` | `SgRagSources.TransformersScript` |
| **WebLLM** | 0.2.83 | `index.js` | `SgRag` | `SgRagSources.WebLlmScript` |
| **PDF.js** | 4.4.168 | `pdf.min.mjs` | `SgRag` | `SgRagSources.PdfJsScript` |
| PDF.js worker | 4.4.168 | `pdf.worker.min.mjs` | `SgRag` | `SgRagSources.PdfJsWorker` |
| **mammoth.js** | 1.8.0 | `mammoth.browser.min.js` | `SgRag` | `SgRagSources.MammothScript` |
| **marked.js** | 12.0.0 | `marked.min.js` | `SgRag` | `SgRagSources.MarkedScript` |
| **idb** | 8.0.0 | `umd.js` | `SgRag` | `SgRagSources.IdbScript` |
| **DuckDB-Wasm** | 1.29.0 | `duckdb-browser-*.wasm/js` | Demo | `sg-duckdb.js` |

---

## Использование локальных копий

После скачивания скопируйте нужные файлы в `wwwroot/lib/` и переопределите Sources:

```csharp
// Program.cs / Startup
builder.Services.AddSuperUI(opts => { });

// В компоненте или провайдере:
<SgECharts Sources="@(new SgEChartsSources {
    EChartsScript = "/lib/echarts/echarts.min.js"
})" ... />

<SgRagProvider Options="@(new SgRagOptions {
    Sources = new SgRagSources {
        TransformersScript = "/lib/transformers/transformers.min.js",
        PdfJsScript        = "/lib/pdfjs/pdf.min.mjs",
        PdfJsWorker        = "/lib/pdfjs/pdf.worker.min.mjs",
        MammothScript      = "/lib/mammoth/mammoth.browser.min.js",
        MarkedScript       = "/lib/marked/marked.min.js",
        IdbScript          = "/lib/idb/umd.js",
        WebLlmScript       = null, // отключить WebLLM
    }
})" ...>

<SgOcr Sources="@(new SgOcrSources {
    TesseractScript = "/lib/tesseract/tesseract.min.js",
    WorkerPath      = "/lib/tesseract/worker.min.js",
    CorePath        = "/lib/tesseract/tesseract-core.wasm.js",
    LangPath        = "/lib/tesseract/lang-data/"
})" ... />
```

---

## Примечания

- **Yandex Maps** и **Google Maps** — API-ключи, скачать нельзя. Загружаются динамически.
- **GraphHopper** — REST API, не требует JS-библиотеки.
- **DuckDB-Wasm** — ESM-модуль, скачивается через `+esm` endpoint. WASM-файлы нужны отдельно.
- **Monaco Editor** — только `loader.js` скачивается; остальные файлы (`vs/`) загружаются через `require.config`. Для полного оффлайна нужно скопировать всю папку `vs/` (~15 МБ).
- **WebLLM** — модели (~700 МБ+) кэшируются браузером в Cache API автоматически.
- **Transformers.js** — модели (~23–130 МБ) кэшируются в IndexedDB автоматически.
