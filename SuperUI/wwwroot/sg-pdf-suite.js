let pdfjsLib = null;
let fabric = null;
let pdfLib = null;

async function ensureDependencies() {
    if (window.pdfjsLib && window.fabric && window.PDFLib) {
        pdfjsLib = window.pdfjsLib;
        fabric = window.fabric;
        pdfLib = window.PDFLib;
        return;
    }

    const scripts = [
        'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js',
        'https://cdnjs.cloudflare.com/ajax/libs/fabric.js/5.3.1/fabric.min.js',
        'https://unpkg.com/pdf-lib@1.17.1/dist/pdf-lib.min.js',
        'https://cdn.jsdelivr.net/npm/sortablejs@1.15.0/Sortable.min.js'
    ];

    await Promise.all(scripts.map(src => new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = src;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    })));

    window.pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
    pdfjsLib = window.pdfjsLib;
    fabric = window.fabric;
    pdfLib = window.PDFLib;
}

let annotatorInstances = new Map();
const sortableInstances = new WeakMap();
const objectUrls = new Set();

export async function initAnnotator(container, dotNetHelper, fileUrl) {
    await ensureDependencies();
    
    const loadingTask = pdfjsLib.getDocument(fileUrl);
    const pdf = await loadingTask.promise;
    
    // Создаем внутренний контейнер, который JS будет полностью контролировать
    // Это предотвращает ошибки Blazor "removeChild" при обновлении DOM
    const jsContainer = document.createElement('div');
    jsContainer.className = 'sg-pdf-js-managed';
    jsContainer.style.width = '100%';
    jsContainer.style.height = '100%';
    container.appendChild(jsContainer);

    const instanceId = Math.random().toString(36).substr(2, 9);
    const instance = {
        pdf,
        canvases: new Map(), // pageNum -> { fabricCanvas, originalWidth, originalHeight }
        container: jsContainer,
        dotNetHelper
    };
    
    annotatorInstances.set(instanceId, instance);
    
    // Render first page by default
    await renderPage(instanceId, 1);
    
    return {
        instanceId,
        totalPages: pdf.numPages
    };
}

export async function renderPage(instanceId, pageNum) {
    const instance = annotatorInstances.get(instanceId);
    if (!instance) return;

    const page = await instance.pdf.getPage(pageNum);
    const viewport = page.getViewport({ scale: 1.5 });
    
    // Dispose old fabric canvases before clearing
    instance.canvases.forEach(c => { try { c.fabricCanvas?.dispose(); } catch {} });
    instance.canvases.clear();
    // Clear container
    instance.container.innerHTML = '';
    
    const wrapper = document.createElement('div');
    wrapper.style.position = 'relative';
    wrapper.style.width = `${viewport.width}px`;
    wrapper.style.height = `${viewport.height}px`;
    wrapper.style.margin = '0 auto';
    instance.container.appendChild(wrapper);

    // 1. PDF Layer (Canvas)
    const pdfCanvas = document.createElement('canvas');
    pdfCanvas.width = viewport.width;
    pdfCanvas.height = viewport.height;
    pdfCanvas.style.position = 'absolute';
    pdfCanvas.style.top = '0';
    pdfCanvas.style.left = '0';
    wrapper.appendChild(pdfCanvas);

    const renderContext = {
        canvasContext: pdfCanvas.getContext('2d'),
        viewport: viewport
    };
    await page.render(renderContext).promise;

    // 2. Fabric Layer (Annotation)
    const fabricCanvasEl = document.createElement('canvas');
    fabricCanvasEl.id = `fabric-${instanceId}-${pageNum}`;
    wrapper.appendChild(fabricCanvasEl);

    const fCanvas = new fabric.Canvas(fabricCanvasEl, {
        width: viewport.width,
        height: viewport.height,
        isDrawingMode: false
    });

    instance.canvases.set(pageNum, {
        fabricCanvas: fCanvas,
        width: viewport.width,
        height: viewport.height
    });

    fCanvas.on('object:added', () => syncAnnotations(instanceId, pageNum));
    fCanvas.on('object:modified', () => syncAnnotations(instanceId, pageNum));
    fCanvas.on('object:removed', () => syncAnnotations(instanceId, pageNum));
}

function syncAnnotations(instanceId, pageNum) {
    const instance = annotatorInstances.get(instanceId);
    const state = instance.canvases.get(pageNum);
    if (state) {
        const json = JSON.stringify(state.fabricCanvas.toJSON());
        try { instance.dotNetHelper?.invokeMethodAsync('OnAnnotationsChanged', pageNum, json)?.catch(() => {}); } catch {}
    }
}

export function setDrawingMode(instanceId, pageNum, mode) {
    const instance = annotatorInstances.get(instanceId);
    const state = instance.canvases.get(pageNum);
    if (state) {
        state.fabricCanvas.isDrawingMode = mode === 'pencil';
        if (mode === 'pencil') {
            state.fabricCanvas.freeDrawingBrush = new fabric.PencilBrush(state.fabricCanvas);
            state.fabricCanvas.freeDrawingBrush.width = 3;
            state.fabricCanvas.freeDrawingBrush.color = '#ff0000';
        }
    }
}

export function loadAnnotations(instanceId, pageNum, json) {
    const instance = annotatorInstances.get(instanceId);
    const state = instance.canvases.get(pageNum);
    if (state && json) {
        state.fabricCanvas.loadFromJSON(json, () => {
            state.fabricCanvas.renderAll();
        });
    }
}

// --- Form Filler Logic ---

export async function getFormFields(fileUrl) {
    await ensureDependencies();
    const existingPdfBytes = await fetch(fileUrl).then(res => res.arrayBuffer());
    const pdfDoc = await pdfLib.PDFDocument.load(existingPdfBytes);
    const form = pdfDoc.getForm();
    const fields = form.getFields();
    
    return fields.map(f => ({
        name: f.getName(),
        type: f.constructor.name,
        value: f.getText ? f.getText() : (f.isChecked ? f.isChecked() : '')
    }));
}

export async function fillForm(fileUrl, fieldData) {
    const existingPdfBytes = await fetch(fileUrl).then(res => res.arrayBuffer());
    const pdfDoc = await pdfLib.PDFDocument.load(existingPdfBytes);
    const form = pdfDoc.getForm();

    for (const [name, value] of Object.entries(fieldData)) {
        const field = form.getField(name);
        if (field.setText) field.setText(value);
        else if (field.check && value === 'true') field.check();
        else if (field.uncheck && value === 'false') field.uncheck();
    }

    const pdfBytes = await pdfDoc.save();
    const blob = new Blob([pdfBytes], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);
    objectUrls.add(url);
    return url;
}

export async function mergePdfs(fileUrls) {
    await ensureDependencies();
    const mergedPdf = await pdfLib.PDFDocument.create();
    
    for (const url of fileUrls) {
        const bytes = await fetch(url).then(res => res.arrayBuffer());
        const pdf = await pdfLib.PDFDocument.load(bytes);
        const copiedPages = await mergedPdf.copyPages(pdf, pdf.getPageIndices());
        copiedPages.forEach((page) => mergedPdf.addPage(page));
    }

    const pdfBytes = await mergedPdf.save();
    const blob = new Blob([pdfBytes], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);
    objectUrls.add(url);
    return url;
}

export function initSortable(el, dotNetHelper) {
    const s = Sortable.create(el, {
        animation: 150,
        onEnd: (evt) => {
            try { dotNetHelper?.invokeMethodAsync('OnReorder', evt.oldIndex, evt.newIndex)?.catch(() => {}); } catch {}
        }
    });
    sortableInstances.set(el, s);
}

export async function extractText(fileUrl) {
    await ensureDependencies();
    const loadingTask = pdfjsLib.getDocument(fileUrl);
    const pdf = await loadingTask.promise;
    let fullText = [];

    for (let i = 1; i <= pdf.numPages; i++) {
        const page = await pdf.getPage(i);
        const textContent = await page.getTextContent();
        const text = textContent.items.map(item => item.str).join(' ');
        fullText.push({ pageNum: i, text });
    }
    return fullText;
}

export async function highlightText(instanceId, pageNum, searchTerm) {
    const instance = annotatorInstances.get(instanceId);
    if (!instance) return;

    // Ensure page is rendered
    await renderPage(instanceId, pageNum);
    const state = instance.canvases.get(pageNum);
    if (!state) return;

    const page = await instance.pdf.getPage(pageNum);
    const textContent = await page.getTextContent();
    const viewport = page.getViewport({ scale: 1.5 });

    textContent.items.forEach(item => {
        if (item.str.toLowerCase().includes(searchTerm.toLowerCase())) {
            const tx = pdfjsLib.Util.transform(viewport.transform, item.transform);
            
            // Draw highlight rectangle in Fabric
            const rect = new fabric.Rect({
                left: tx[4],
                top: tx[5] - (item.height * 1.5), // Approximate top
                width: item.width * 1.5,
                height: item.height * 1.5,
                fill: 'yellow',
                opacity: 0.4,
                selectable: false,
                evented: false
            });
            state.fabricCanvas.add(rect);
        }
    });
    state.fabricCanvas.renderAll();
}

export function dispose(instanceId) {
    const instance = annotatorInstances.get(instanceId);
    if (instance) {
        instance.canvases.forEach(c => { try { c.fabricCanvas?.dispose(); } catch {} });
        instance.canvases.clear();
        // Destroy Sortable instances in container
        const sortables = instance.container.querySelectorAll('[data-sortable]');
        sortables.forEach(el => {
            const s = sortableInstances.get(el);
            if (s) { try { s.destroy(); } catch {}; sortableInstances.delete(el); }
        });
        annotatorInstances.delete(instanceId);
    }
}

export function destroySortable(el) {
    const s = sortableInstances.get(el);
    if (s) { try { s.destroy(); } catch {}; sortableInstances.delete(el); }
}

export function revokeObjectUrl(url) {
    if (url && objectUrls.has(url)) {
        try { URL.revokeObjectURL(url); } catch {}
        objectUrls.delete(url);
    }
}

export function revokeAllObjectUrls() {
    objectUrls.forEach(url => { try { URL.revokeObjectURL(url); } catch {} });
    objectUrls.clear();
}
