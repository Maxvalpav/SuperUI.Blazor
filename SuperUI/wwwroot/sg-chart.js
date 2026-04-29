// SgChart - Chart.js Integration Module
// Provides JavaScript interop for Blazor SgChart component

let chartInstances = new Map();

/**
 * Initialize a new chart
 * @param {DotNetObjectReference} dotnetRef - Reference to Blazor component
 * @param {ElementReference} canvasRef - Canvas element reference
 * @param {Object} config - Chart.js configuration
 */
export async function initChart(dotnetRef, canvasRef, config) {
    try {
        console.log('SgChart: Initializing chart with config:', config);
        
        // Wait for Chart.js to be available
        let Chart = window.Chart;
        let attempts = 0;
        while (!Chart && attempts < 50) {
            await new Promise(resolve => setTimeout(resolve, 100));
            Chart = window.Chart;
            attempts++;
        }
        
        if (!Chart) {
            throw new Error('Chart.js library not loaded');
        }
        
        console.log('SgChart: Chart.js loaded successfully');
        
        // Register zoom plugin if available
        if (window.ChartZoom) {
            Chart.register(window.ChartZoom);
            console.log('SgChart: Zoom plugin registered');
            
            // Add zoom configuration to options
            if (!config.options) config.options = {};
            if (!config.options.plugins) config.options.plugins = {};
            
            config.options.plugins.zoom = {
                zoom: {
                    wheel: {
                        enabled: true,
                        speed: 0.1
                    },
                    pinch: {
                        enabled: true
                    },
                    mode: 'y'
                },
                pan: {
                    enabled: true,
                    mode: 'y'
                }
            };
        }
        
        // Register matrix chart plugin if available (for heatmap)
        if (window.MatrixController) {
            Chart.register(window.MatrixController);
            console.log('SgChart: Matrix chart plugin registered');
        }
        
        // Get canvas context
        const ctx = canvasRef.getContext('2d');
        if (!ctx) {
            throw new Error('Failed to get canvas context');
        }
        
        console.log('SgChart: Canvas context obtained');
        
        // Create chart instance
        const chart = new Chart(ctx, config);
        
        console.log('SgChart: Chart instance created');
        
        // Store chart instance
        const chartId = canvasRef.id;
        chartInstances.set(chartId, {
            instance: chart,
            dotnetRef: dotnetRef,
            canvasRef: canvasRef
        });
        
        console.log('SgChart: Chart stored with ID:', chartId);
        
        // Attach click event handler
        canvasRef.addEventListener('click', (e) => {
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, true);
            if (points.length > 0) {
                const point = points[0];
                const datasetIndex = point.datasetIndex;
                const index = point.index;
                const value = chart.data.datasets[datasetIndex].data[index];
                const label = chart.data.labels?.[index] || '';
                
                dotnetRef.invokeMethodAsync('OnDataPointClickedAsync', {
                    datasetIndex: datasetIndex,
                    dataPointIndex: index,
                    value: value,
                    label: label
                });
            }
        });
        
    } catch (error) {
        console.error('Failed to initialize chart:', error);
        throw error;
    }
}

/**
 * Update chart data
 * @param {string} chartId - Chart canvas ID
 * @param {Object} config - New chart configuration
 */
export function updateChart(chartId, config) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) {
        console.warn(`Chart ${chartId} not found`);
        return;
    }
    
    const chart = chartData.instance;
    chart.data = config.data;
    chart.options = config.options;
    chart.update();
}

/**
 * Zoom Y-axis to specified range
 * @param {string} chartId - Chart canvas ID
 * @param {number} min - Minimum Y value
 * @param {number} max - Maximum Y value
 */
export function zoomY(chartId, min, max) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) {
        console.warn(`Chart ${chartId} not found`);
        return;
    }
    
    const chart = chartData.instance;
    if (chart.options.scales && chart.options.scales.y) {
        chart.options.scales.y.min = min;
        chart.options.scales.y.max = max;
        chart.update();
    }
}

/**
 * Reset Y-axis zoom to original range
 * @param {string} chartId - Chart canvas ID
 */
export function resetZoom(chartId) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) {
        console.warn(`Chart ${chartId} not found`);
        return;
    }
    
    const chart = chartData.instance;
    if (chart.options.scales && chart.options.scales.y) {
        delete chart.options.scales.y.min;
        delete chart.options.scales.y.max;
        chart.update();
    }
}

/**
 * Export chart as image
 * @param {string} chartId - Chart canvas ID
 * @param {string} format - Image format (png, jpg, svg)
 */
export function exportImage(chartId, format = 'png') {
    const chartData = chartInstances.get(chartId);
    if (!chartData) {
        console.warn(`Chart ${chartId} not found`);
        return;
    }
    
    const canvas = chartData.canvasRef;
    const link = document.createElement('a');
    
    if (format === 'svg') {
        // For SVG, we need to use a library or convert canvas to SVG
        // For now, we'll just export as PNG
        link.href = canvas.toDataURL('image/png');
        link.download = `chart-${new Date().getTime()}.png`;
    } else {
        const mimeType = format === 'jpg' ? 'image/jpeg' : 'image/png';
        link.href = canvas.toDataURL(mimeType);
        link.download = `chart-${new Date().getTime()}.${format}`;
    }
    
    link.click();
}

/**
 * Dispose chart and clean up resources
 * @param {string} chartId - Chart canvas ID
 */
export function dispose(chartId) {
    const chartData = chartInstances.get(chartId);
    if (!chartData) {
        return;
    }
    
    const chart = chartData.instance;
    chart.destroy();
    
    // Remove event listeners
    const canvas = chartData.canvasRef;
    canvas.removeEventListener('click', null);
    
    // Remove from map
    chartInstances.delete(chartId);
}

/**
 * Get chart instance (for advanced usage)
 * @param {string} chartId - Chart canvas ID
 * @returns {Object} Chart.js instance or null
 */
export function getChartInstance(chartId) {
    const chartData = chartInstances.get(chartId);
    return chartData ? chartData.instance : null;
}
