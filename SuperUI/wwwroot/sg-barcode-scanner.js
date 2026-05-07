const scanners = new Map();

export async function initBarcodeScanner(dotNetRef, instanceId, sources) {
    if (scanners.has(instanceId)) return;

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

    try {
        if (!scanner.codeReader) {
            scanner.codeReader = new ZXing.BrowserMultiFormatReader();
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
                    handleDecodeResult(instanceId, result, capturePicture);
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

function handleDecodeResult(instanceId, result, capturePicture) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;

    let picture = null;
    if (capturePicture) {
        try {
            const video = document.getElementById(scanner.instanceId + '-video');
            if (video) {
                const canvas = document.createElement('canvas');
                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(video, 0, 0);
                picture = canvas.toDataURL('image/jpeg', 0.9);
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

export async function decodeFromFile(instanceId, fileDataUrl) {
    const scanner = scanners.get(instanceId);
    if (!scanner) return;

    try {
        if (!scanner.codeReader) {
            scanner.codeReader = new ZXing.BrowserMultiFormatReader();
        }

        const result = await scanner.codeReader.decodeFromImageElement(
            await createImageElement(fileDataUrl)
        );

        handleDecodeResult(instanceId, result, false);
    } catch (error) {
        console.error('Failed to decode from file:', error);
        await scanner.dotNetRef.invokeMethodAsync('OnDecodeErrorAsync', error.message || 'Failed to decode from file');
    }
}

function createImageElement(dataUrl) {
    return new Promise((resolve, reject) => {
        const img = new Image();
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
