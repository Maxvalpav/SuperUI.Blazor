// superui-chart.js - Chart.js wrapper for SgChart

let chartJsLoaded = false;
const charts = new Map(); // Store chart instances with isDisposed flag

async function ensureChartJs() {
    if (chartJsLoaded || window.Chart) return;
    
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/chart.js';
        script.onload = () => {
            chartJsLoaded = true;
            resolve();
        };
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

export async function initChart(dotnet, canvas, config) {
    await ensureChartJs();
    
    if (!canvas) return;
    
    // Cleanup old chart if exists
    if (canvas._sgChart) {
        canvas._sgChart.destroy();
    }

    let isDisposed = false;
    const chartConfig = transformConfig(config);
    const chart = new Chart(canvas, chartConfig);
    
    // Store chart with disposal tracking
    canvas._sgChart = {
        chart,
        isDisposed,
        dispose: function() {
            this.isDisposed = true;
            dotnet = null;
        }
    };
    
    charts.set(canvas.id, canvas._sgChart);
}

export function updateChart(chartId, config) {
    const canvas = document.getElementById(chartId);
    if (!canvas?._sgChart || canvas._sgChart.isDisposed) return;
    
    try {
        const chart = canvas._sgChart.chart;
        const newConfig = transformConfig(config);
        
        chart.data = newConfig.data;
        chart.options = newConfig.options;
        chart.update();
    } catch { }
}

export function downloadImage(canvas, fileName) {
    if (!canvas?._sgChart || canvas._sgChart.isDisposed) return;
    
    try {
        const url = canvas.toDataURL('image/png');
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    } catch { }
}

export function downloadSvg(canvas, fileName) {
    if (!canvas?._sgChart || canvas._sgChart.isDisposed) return;
    
    try {
        const url = canvas.toDataURL('image/png');
        const svg = `
            <svg xmlns="http://www.w3.org/2000/svg" width="${canvas.width}" height="${canvas.height}">
                <image href="${url}" width="${canvas.width}" height="${canvas.height}" />
            </svg>
        `;
        const blob = new Blob([svg], { type: 'image/svg+xml' });
        const blobUrl = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = blobUrl;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(() => URL.revokeObjectURL(blobUrl), 0);
    } catch { }
}

export function dispose(chartId) {
    const canvas = document.getElementById(chartId);
    if (canvas?._sgChart) {
        if (canvas._sgChart.dispose) {
            canvas._sgChart.dispose();
        }
        if (canvas._sgChart.chart) {
            canvas._sgChart.chart.destroy();
        }
        canvas._sgChart = null;
    }
    charts.delete(chartId);
}

function transformConfig(config) {
    const { type, data, options } = config;
    
    const chartData = {
        labels: data.labels,
        datasets: data.datasets.map(ds => ({
            label: ds.label,
            data: ds.scatterData || ds.data,
            backgroundColor: ds.colors || ds.fillColor || ds.color,
            borderColor: ds.color,
            borderWidth: ds.borderWidth,
            fill: type === 'Area',
            pointRadius: ds.showPoints ? 3 : 0,
            stack: ds.stack
        }))
    };

    const chartOptions = {
        responsive: options.responsive,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                display: options.showLegend
            },
            decimation: {
                enabled: options.enableDecimation,
                threshold: options.decimationThreshold
            }
        },
        scales: {
            y: {
                display: options.showLabels,
                beginAtZero: options.minValue === 0,
                min: options.minValue,
                max: options.maxValue,
                grid: {
                    display: options.showGrid
                }
            },
            x: {
                display: options.showLabels,
                grid: {
                    display: options.showGrid
                }
            }
        }
    };

    // Special handling for chart types
    let chartType = type.toLowerCase();
    if (chartType === 'area') chartType = 'line';
    if (chartType === 'heatmap') chartType = 'matrix'; // matrix plugin needed for real heatmap, using bubble/bar as fallback or assuming user might add plugin

    return {
        type: chartType,
        data: chartData,
        options: chartOptions
    };
}
