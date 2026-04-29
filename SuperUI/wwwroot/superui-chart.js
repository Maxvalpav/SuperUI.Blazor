// superui-chart.js - Chart.js wrapper for SgChart

let chartJsLoaded = false;

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

export async function init(canvas, config) {
    await ensureChartJs();
    
    if (canvas._sgChart) {
        canvas._sgChart.destroy();
    }

    const chartConfig = transformConfig(config);
    canvas._sgChart = new Chart(canvas, chartConfig);
}

export function update(canvas, config) {
    if (!canvas?._sgChart) return;
    
    const chart = canvas._sgChart;
    const newConfig = transformConfig(config);
    
    chart.data = newConfig.data;
    chart.options = newConfig.options;
    chart.update();
}

export function downloadImage(canvas, fileName) {
    if (!canvas) return;
    const url = canvas.toDataURL('image/png');
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}

export function downloadSvg(canvas, fileName) {
    if (!canvas) return;
    
    // Simplest way to get SVG from Chart.js canvas is to use a plugin or just 
    // export the current state as image and wrap in SVG if needed, 
    // but here we will just provide a stub or use toDataURL('image/svg+xml') if supported
    // Chart.js doesn't natively export SVG. A common workaround is using 'chartjs-to-svg' 
    // or just providing the image. For now, we will do a data URL approach.
    
    const url = canvas.toDataURL('image/png'); // Fallback to PNG inside SVG
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
}

export function destroy(canvas) {
    if (canvas?._sgChart) {
        canvas._sgChart.destroy();
        canvas._sgChart = null;
    }
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
