# ============================================================
# SuperUI — CDN Library Backup Script
# Downloads all vendor JS/CSS libraries to ./backup/libs/
# Run from the repo root:  .\backup\download-libs.ps1
# ============================================================

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot "libs"
New-Item -ItemType Directory -Force -Path $root | Out-Null

function Get-Lib {
    param(
        [string]$Url,
        [string]$SubDir,
        [string]$FileName
    )
    $dir  = Join-Path $root $SubDir
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $dest = Join-Path $dir $FileName
    if (Test-Path $dest) {
        Write-Host "  [skip] $SubDir/$FileName" -ForegroundColor DarkGray
        return
    }
    try {
        Write-Host "  [dl]   $SubDir/$FileName" -ForegroundColor Cyan
        Invoke-WebRequest -Uri $Url -OutFile $dest -UseBasicParsing
    } catch {
        Write-Warning "  FAILED: $Url`n         $_"
    }
}

Write-Host "`n=== SuperUI CDN Library Backup ===" -ForegroundColor Yellow
Write-Host "Output: $root`n"

# ── Chart.js ─────────────────────────────────────────────────────────────────
Write-Host "[Chart.js]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"                          "chartjs"          "chart.umd.min.js"
Get-Lib "https://cdn.jsdelivr.net/npm/chartjs-plugin-zoom@2.1.0/dist/chartjs-plugin-zoom.min.js"     "chartjs"          "chartjs-plugin-zoom.min.js"
Get-Lib "https://cdn.jsdelivr.net/npm/chartjs-chart-matrix@1.1.1/dist/chartjs-chart-matrix.min.js"   "chartjs"          "chartjs-chart-matrix.min.js"

# ── D3.js ─────────────────────────────────────────────────────────────────────
Write-Host "[D3.js]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/d3@7.9.0/dist/d3.min.js"                                       "d3"               "d3.min.js"

# ── Apache ECharts ────────────────────────────────────────────────────────────
Write-Host "[ECharts]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/echarts@5.5.1/dist/echarts.min.js"                             "echarts"          "echarts.min.js"

# ── Konva.js ──────────────────────────────────────────────────────────────────
Write-Host "[Konva.js]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/konva@9.3.18/konva.min.js"                                     "konva"            "konva.min.js"

# ── Mermaid ───────────────────────────────────────────────────────────────────
Write-Host "[Mermaid]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/mermaid@11.4.1/dist/mermaid.min.js"                            "mermaid"          "mermaid.min.js"

# ── Monaco Editor ─────────────────────────────────────────────────────────────
Write-Host "[Monaco Editor]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/loader.js"                         "monaco/vs"        "loader.js"

# ── OpenLayers ────────────────────────────────────────────────────────────────
Write-Host "[OpenLayers]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/ol@10.3.1/dist/ol.js"                                          "openlayers"       "ol.js"
Get-Lib "https://cdn.jsdelivr.net/npm/ol@10.3.1/ol.css"                                              "openlayers"       "ol.css"

# ── Leaflet ───────────────────────────────────────────────────────────────────
Write-Host "[Leaflet]" -ForegroundColor Green
Get-Lib "https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"                                            "leaflet"          "leaflet.js"
Get-Lib "https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"                                           "leaflet"          "leaflet.css"

# ── Three.js ──────────────────────────────────────────────────────────────────
Write-Host "[Three.js]" -ForegroundColor Green
Get-Lib "https://cdnjs.cloudflare.com/ajax/libs/three.js/r134/three.min.js"                          "three"            "three.min.js"
Get-Lib "https://unpkg.com/three@0.134.0/examples/js/controls/OrbitControls.js"                      "three"            "OrbitControls.js"
Get-Lib "https://unpkg.com/three@0.134.0/examples/js/loaders/GLTFLoader.js"                          "three"            "GLTFLoader.js"

# ── BPMN.js ───────────────────────────────────────────────────────────────────
Write-Host "[BPMN.js]" -ForegroundColor Green
Get-Lib "https://unpkg.com/bpmn-js@17.11.1/dist/bpmn-modeler.development.js"                         "bpmn"             "bpmn-modeler.development.js"
Get-Lib "https://unpkg.com/bpmn-js@17.11.1/dist/bpmn-viewer.development.js"                          "bpmn"             "bpmn-viewer.development.js"
Get-Lib "https://unpkg.com/bpmn-js@17.11.1/dist/bpmn-navigated-viewer.development.js"                "bpmn"             "bpmn-navigated-viewer.development.js"
Get-Lib "https://unpkg.com/bpmn-js@17.11.1/dist/assets/diagram-js.css"                               "bpmn/assets"      "diagram-js.css"
Get-Lib "https://unpkg.com/bpmn-js@17.11.1/dist/assets/bpmn-js.css"                                  "bpmn/assets"      "bpmn-js.css"
Get-Lib "https://unpkg.com/bpmn-js@17.11.1/dist/assets/bpmn-font/css/bpmn-embedded.css"              "bpmn/assets"      "bpmn-embedded.css"

# ── Tesseract.js (OCR) ────────────────────────────────────────────────────────
Write-Host "[Tesseract.js]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/tesseract.js@5.1.1/dist/tesseract.min.js"                      "tesseract"        "tesseract.min.js"
Get-Lib "https://cdn.jsdelivr.net/npm/tesseract.js@5.1.1/dist/worker.min.js"                         "tesseract"        "worker.min.js"
Get-Lib "https://cdn.jsdelivr.net/npm/tesseract.js-core@5.1.1/tesseract-core.wasm.js"                "tesseract"        "tesseract-core.wasm.js"

# ── RAG: Transformers.js ──────────────────────────────────────────────────────
Write-Host "[Transformers.js]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/@xenova/transformers@2.17.2/dist/transformers.min.js"          "transformers"     "transformers.min.js"

# ── RAG: WebLLM ───────────────────────────────────────────────────────────────
Write-Host "[WebLLM]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/@mlc-ai/web-llm@0.2.83/lib/index.js"                           "web-llm"          "index.js"

# ── RAG: PDF.js ───────────────────────────────────────────────────────────────
Write-Host "[PDF.js]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/pdfjs-dist@4.4.168/build/pdf.min.mjs"                          "pdfjs"            "pdf.min.mjs"
Get-Lib "https://cdn.jsdelivr.net/npm/pdfjs-dist@4.4.168/build/pdf.worker.min.mjs"                   "pdfjs"            "pdf.worker.min.mjs"

# ── RAG: mammoth.js ───────────────────────────────────────────────────────────
Write-Host "[mammoth.js]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/mammoth@1.8.0/mammoth.browser.min.js"                          "mammoth"          "mammoth.browser.min.js"

# ── RAG: marked.js ───────────────────────────────────────────────────────────
Write-Host "[marked.js]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/marked@12.0.0/marked.min.js"                                   "marked"           "marked.min.js"

# ── RAG: idb ─────────────────────────────────────────────────────────────────
Write-Host "[idb]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/idb@8.0.0/build/umd.js"                                        "idb"              "umd.js"

# ── DuckDB-Wasm ───────────────────────────────────────────────────────────────
Write-Host "[DuckDB-Wasm]" -ForegroundColor Green
Get-Lib "https://cdn.jsdelivr.net/npm/@duckdb/duckdb-wasm@1.29.0/dist/duckdb-mvp.wasm"                "duckdb"           "duckdb-mvp.wasm"
Get-Lib "https://cdn.jsdelivr.net/npm/@duckdb/duckdb-wasm@1.29.0/dist/duckdb-browser-mvp.worker.js"   "duckdb"           "duckdb-browser-mvp.worker.js"
Get-Lib "https://cdn.jsdelivr.net/npm/@duckdb/duckdb-wasm@1.29.0/dist/duckdb-eh.wasm"                 "duckdb"           "duckdb-eh.wasm"
Get-Lib "https://cdn.jsdelivr.net/npm/@duckdb/duckdb-wasm@1.29.0/dist/duckdb-browser-eh.worker.js"    "duckdb"           "duckdb-browser-eh.worker.js"
Get-Lib "https://cdn.jsdelivr.net/npm/@duckdb/duckdb-wasm@1.29.0/dist/duckdb-browser.mjs"             "duckdb"           "duckdb-browser.mjs"

Write-Host "`n=== Done ===" -ForegroundColor Yellow
Write-Host "Files saved to: $root"

# Print summary
$files = Get-ChildItem $root -Recurse -File
$totalMb = [math]::Round(($files | Measure-Object -Property Length -Sum).Sum / 1MB, 1)
Write-Host "Total: $($files.Count) files, $totalMb MB`n"
