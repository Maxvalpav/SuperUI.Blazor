const scanners = new Map();

export async function initBarcodeScanner(dotNetRef, instanceId, sources) {
    if (scanners.has(instanceId)) {
        // re-init: refresh dotNetRef but keep codeReader
        const existing = scanners.get(instanceId);
        existing.dotNetRef = dotNetRef;
    } else {
        try {
            if (sources?.ZxingScript) {
                await loadScript(sources.ZxingScript);
            } else {
                await loadScript('https://unpkg.com/@zxing/library@0.21.3/umd/index.min.js');
            }

            scanners.set(instanceId, {
                dotNetRef,
                instanceId,
                codeReader: null,
                lastDeviceId: null,
                devices: [],
                capturePicture: false
            });
        } catch (error) {
            console.error('Failed to initialize barcode scanner:', error);
            await dotNetRef.invokeMethodAsync('OnErrorAsync', error.message || 'Initialization failed');
            return;
        }
    }

    // Try to enumerate without permission first; labels may be empty until granted.
    try { await updateDeviceList(instanceId); } catch (e) { /* ignore */ }
    // Signal that the JS module is ready so Blazor can enable Start.
    try { await dotNetRef.invokeMethodAsync('OnReadyAsync'); } catch (e) { /* ignore */ }
}

/**
 * Explicitly request camera permission. Surfaces the browser prompt and
 * returns the up-to-date device list with labels populated.
 */
export async function requestCameraPermission(instanceId) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return false;

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        await scanner.dotNetRef.invokeMethodAsync('OnErrorAsync', 'Camera API is not available in this browser.');
        return false;
    }

    let stream = null;
    try {
        stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
        return true;
    } catch (err) {
        const name = err && err.name ? err.name : '';
        let msg = err && err.message ? err.message : 'Camera permission denied';
        if (name === 'NotAllowedError' || name === 'SecurityError') {
            msg = 'Доступ к камере отклонён. Разрешите доступ в настройках браузера.';
        } else if (name === 'NotFoundError' || name === 'OverconstrainedError') {
            msg = 'Камера не найдена.';
        } else if (name === 'NotReadableError') {
            msg = 'Камера используется другим приложением.';
        }
        await scanner.dotNetRef.invokeMethodAsync('OnErrorAsync', msg);
        return false;
    } finally {
        if (stream) {
            try { stream.getTracks().forEach(t => t.stop()); } catch (e) { /* ignore */ }
        }
        try { await updateDeviceList(instanceId); } catch (e) { /* ignore */ }
    }
}

async function loadScript(url) {
    return new Promise((resolve, reject) => {
        if (window.ZXing) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = url;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

export async function startScanner(instanceId, videoElementId, deviceId, decodeFormats, capturePicture, videoWidth, videoHeight) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;

    scanner.capturePicture = capturePicture;
    scanner.decodeFormats = decodeFormats;

    try {
        if (!scanner.codeReader) {
            const hints = new Map();
            const possibleFormats = buildDecodeFormats(decodeFormats);
            if (possibleFormats.length > 0) {
                hints.set(ZXing.DecodeHintType.POSSIBLE_FORMATS, possibleFormats);
            }
            hints.set(ZXing.DecodeHintType.TRY_HARDER, true);
            scanner.codeReader = new ZXing.BrowserMultiFormatReader(hints);
        }

        const constraints = {
            video: {
                width: { ideal: videoWidth },
                height: { ideal: videoHeight }
            }
        };

        if (deviceId) {
            constraints.video.deviceId = { exact: deviceId };
        }

        await scanner.codeReader.decodeFromVideoDevice(
            deviceId || undefined,
            videoElementId,
            (result, error) => {
                if (result) {
                    handleDecodeResult(instanceId, result, capturePicture, null);
                } else if (error && !(error instanceof ZXing.NotFoundException)) {
                    handleDecodeError(instanceId, error);
                }
            }
        );

        scanner.lastDeviceId = deviceId;
        await updateDeviceList(instanceId);
    } catch (error) {
        console.error('Failed to start scanner:', error);
        await scanner.dotNetRef.invokeMethodAsync('OnErrorAsync', error.message || 'Failed to start scanner');
    }
}

function handleDecodeResult(instanceId, result, capturePicture, imageElement) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;

    let picture = null;
    if (capturePicture) {
        try {
            if (imageElement) {
                const canvas = document.createElement('canvas');
                canvas.width = imageElement.width;
                canvas.height = imageElement.height;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(imageElement, 0, 0);
                picture = canvas.toDataURL('image/jpeg', 0.9);
            } else {
                const video = document.getElementById(scanner.instanceId + '-video');
                if (video) {
                    const canvas = document.createElement('canvas');
                    canvas.width = video.videoWidth;
                    canvas.height = video.videoHeight;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(video, 0, 0);
                    picture = canvas.toDataURL('image/jpeg', 0.9);
                }
            }
        } catch (e) {
            console.warn('Failed to capture picture:', e);
        }
    }

    scanner.dotNetRef.invokeMethodAsync('OnBarcodeReceivedAsync', {
        text: result.getText(),
        format: result.getBarcodeFormat().toString(),
        picture: picture
    });
}

function handleDecodeError(instanceId, error) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;
    scanner.dotNetRef.invokeMethodAsync('OnDecodeErrorAsync', error.message || 'Decode error');
}

export async function stopScanner(instanceId) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;

    try {
        if (scanner.codeReader) {
            scanner.codeReader.reset();
        }
    } catch (e) {
        console.warn('Error stopping scanner:', e);
    }
}

async function updateDeviceList(instanceId) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;

    try {
        const devices = await navigator.mediaDevices.enumerateDevices();
        const videoDevices = devices
            .filter(d => d.kind === 'videoinput')
            .map(d => ({ deviceId: d.deviceId, label: d.label || `Camera ${d.deviceId.slice(0, 8)}...` }));

        scanner.devices = videoDevices;
        await scanner.dotNetRef.invokeMethodAsync('OnDeviceListChangedAsync', videoDevices);
    } catch (e) {
        console.warn('Failed to get device list:', e);
    }
}

export async function toggleTorch(instanceId, enabled) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;
    try {
        const video = document.getElementById(scanner.instanceId + '-video');
        if (video?.srcObject) {
            const track = video.srcObject.getVideoTracks()[0];
            if (track) {
                const capabilities = track.getCapabilities();
                if (capabilities.torch) {
                    await track.applyConstraints({ advanced: [{ torch: enabled }] });
                }
            }
        }
    } catch (e) {
        console.warn('Failed to toggle torch:', e);
    }
}

function buildDecodeFormats(decodeFormats) {
    const formats = [];
    if (!decodeFormats || decodeFormats.length === 0 || decodeFormats.includes('All')) {
        return []; // empty means all
    }
    
    const formatMap = {
        'QRCode': ZXing.BarcodeFormat.QR_CODE,
        'Code128': ZXing.BarcodeFormat.CODE_128,
        'Code39': ZXing.BarcodeFormat.CODE_39,
        'EAN13': ZXing.BarcodeFormat.EAN_13,
        'EAN8': ZXing.BarcodeFormat.EAN_8,
        'UPCA': ZXing.BarcodeFormat.UPC_A,
        'UPCE': ZXing.BarcodeFormat.UPC_E,
        'ITF': ZXing.BarcodeFormat.ITF,
        'PDF417': ZXing.BarcodeFormat.PDF_417,
        'DataMatrix': ZXing.BarcodeFormat.DATA_MATRIX,
        'Aztec': ZXing.BarcodeFormat.AZTEC,
        'Codabar': ZXing.BarcodeFormat.CODABAR
    };
    
    for (const format of decodeFormats) {
        if (formatMap[format]) {
            formats.push(formatMap[format]);
        }
    }
    
    return formats;
}

export async function decodeFromFile(instanceId, fileDataUrl, decodeFormats, capturePicture) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;
    
    console.log('[sg-barcode-scanner] decodeFromFile called', { instanceId, decodeFormats, capturePicture });

    try {
        // Create code reader with decode formats
        const hints = new Map();
        const possibleFormats = buildDecodeFormats(decodeFormats || scanner.decodeFormats);
        console.log('[sg-barcode-scanner] possibleFormats', possibleFormats);
        
        if (possibleFormats.length > 0) {
            hints.set(ZXing.DecodeHintType.POSSIBLE_FORMATS, possibleFormats);
        }
        hints.set(ZXing.DecodeHintType.TRY_HARDER, true);
        const reader = new ZXing.BrowserMultiFormatReader(hints);
        
        // Create image element and wait for it to load
        const img = await createImageElement(fileDataUrl);
        console.log('[sg-barcode-scanner] Image loaded:', img.width, 'x', img.height);
        
        // Try decodeFromImage
        console.log('[sg-barcode-scanner] Trying decodeFromImage');
        const result = await reader.decodeFromImage(img);
        console.log('[sg-barcode-scanner] decodeFromImage success:', result);
        
        if (result) {
            console.log('[sg-barcode-scanner] Decode complete, calling handleDecodeResult');
            handleDecodeResult(instanceId, result, capturePicture, img);
        }
    } catch (error) {
        console.error('[sg-barcode-scanner] Failed to decode from file:', error);
        await scanner.dotNetRef.invokeMethodAsync('OnDecodeErrorAsync', error.message || 'Failed to decode from file');
    }
}

function createImageElement(dataUrl) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = "anonymous";
        img.onload = () => resolve(img);
        img.onerror = reject;
        img.src = dataUrl;
    });
}

export async function disposeBarcodeScanner(instanceId) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;

    try {
        if (scanner.codeReader) {
            scanner.codeReader.reset();
        }
    } catch (e) {
        console.warn('Error disposing scanner:', e);
    }

    scanners.delete(instanceId);
}
