/**
 * documentExtractor.js - SuperUI Document AI Extractor JS Bridge (ESM)
 */

// Load external libraries dynamically
const PDF_JS_URL = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js';
const PDF_JS_WORKER_URL = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
const MAMMOTH_JS_URL = 'https://cdnjs.cloudflare.com/ajax/libs/mammoth/1.6.0/mammoth.browser.min.js';
const HTML2PDF_JS_URL = 'https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js';

async function loadScript(url) {
    if (document.querySelector(`script[src="${url}"]`)) return;
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = url;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

export async function exportToPdf(htmlContent, fileName) {
    await loadScript(HTML2PDF_JS_URL);
    const options = {
        margin: 10,
        filename: fileName,
        image: { type: 'jpeg', quality: 0.98 },
        html2canvas: { scale: 2, useCORS: true },
        jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
    };
    
    // Create a temporary container for rendering
    const element = document.createElement('div');
    element.innerHTML = htmlContent;
    element.style.padding = '20px';
    element.style.fontFamily = 'Arial, sans-serif';
    
    return html2pdf().set(options).from(element).save();
}

export async function exportToWord(htmlContent, fileName) {
    // Basic HTML to Word conversion using Blob and data URI
    // For a more advanced version, we could use a library like html-docx-js
    const header = "<html xmlns:o='urn:schemas-microsoft-com:office:office' "+
            "xmlns:w='urn:schemas-microsoft-com:office:word' "+
            "xmlns='http://www.w3.org/TR/REC-html40'>"+
            "<head><meta charset='utf-8'><title>Export</title></head><body>";
    const footer = "</body></html>";
    const sourceHTML = header + htmlContent + footer;
    
    const source = 'data:application/vnd.ms-word;charset=utf-8,' + encodeURIComponent(sourceHTML);
    const fileLink = document.createElement("a");
    document.body.appendChild(fileLink);
    fileLink.href = source;
    fileLink.download = fileName;
    fileLink.click();
    document.body.removeChild(fileLink);
}

export async function parsePdfDocument(base64Pdf) {
    await loadScript(PDF_JS_URL);
    const pdfjsLib = window['pdfjs-dist/build/pdf'];
    pdfjsLib.GlobalWorkerOptions.workerSrc = PDF_JS_WORKER_URL;

    const pdfData = atob(base64Pdf);
    const uint8Array = new Uint8Array(pdfData.length);
    for (let i = 0; i < pdfData.length; i++) {
        uint8Array[i] = pdfData.charCodeAt(i);
    }

    const loadingTask = pdfjsLib.getDocument({ data: uint8Array });
    const pdf = await loadingTask.promise;
    const images = [];
    const textParts = [];

    // Limit to first 10 pages for AI analysis
    const pagesToProcess = Math.min(pdf.numPages, 10);

    for (let i = 1; i <= pagesToProcess; i++) {
        const page = await pdf.getPage(i);
        const textContent = await page.getTextContent();
        const pageText = textContent.items.map(x => x.str).join(' ');
        if (pageText.trim()) {
            textParts.push(`Page ${i}: ${pageText}`);
        }

        const viewport = page.getViewport({ scale: 2.0 }); // Higher scale for better OCR
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d');
        canvas.height = viewport.height;
        canvas.width = viewport.width;

        await page.render({ canvasContext: context, viewport: viewport }).promise;
        images.push({
            pageNumber: i,
            base64: canvas.toDataURL('image/jpeg', 0.8).split(',')[1],
            mimeType: 'image/jpeg'
        });
    }

    return {
        extractedText: textParts.join('\n\n'),
        pages: images,
        metadata: {
            pageCount: String(pdf.numPages),
            processedPages: String(pagesToProcess)
        }
    };
}

export async function extractWordDocument(base64Word) {
    await loadScript(MAMMOTH_JS_URL);
    const mammoth = window.mammoth;
    
    const binaryString = atob(base64Word);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }

    const result = await mammoth.extractRawText({ arrayBuffer: bytes.buffer });
    return {
        extractedText: result.value || '',
        pages: [],
        metadata: {
            parser: 'mammoth'
        }
    };
}

export function downloadFile(fileName, base64Content, mimeType) {
    const byteCharacters = atob(base64Content);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
}

export function saveExtractorSettings(settings) {
    localStorage.setItem('sg_extractor_settings', JSON.stringify(settings));
}

export function loadExtractorSettings() {
    const settings = localStorage.getItem('sg_extractor_settings');
    return settings ? JSON.parse(settings) : null;
}
