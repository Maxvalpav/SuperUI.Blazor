
let stream = null;

export async function startCamera(videoElement, options = {}) {
    try {
        const constraints = {
            video: {
                width: { ideal: options.width || 1280 },
                height: { ideal: options.height || 720 },
                facingMode: options.facingMode || 'user'
            },
            audio: false
        };

        stream = await navigator.mediaDevices.getUserMedia(constraints);
        videoElement.srcObject = stream;
        await videoElement.play();
        return true;
    } catch (err) {
        console.error("Error accessing camera:", err);
        return false;
    }
}

export function stopCamera(videoElement) {
    if (stream) {
        stream.getTracks().forEach(track => track.stop());
        videoElement.srcObject = null;
        stream = null;
    }
}

export function takeSnapshot(videoElement) {
    const canvas = document.createElement('canvas');
    canvas.width = videoElement.videoWidth;
    canvas.height = videoElement.videoHeight;
    const ctx = canvas.getContext('2d');
    
    // Apply filters from video element to canvas if needed
    ctx.filter = getComputedStyle(videoElement).filter;
    
    ctx.drawImage(videoElement, 0, 0, canvas.width, canvas.height);
    return canvas.toDataURL('image/png');
}

export function speak(text, options = {}) {
    if (!window.speechSynthesis) return false;
    
    // Cancel any ongoing speech
    window.speechSynthesis.cancel();
    
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = options.lang || 'ru-RU';
    utterance.pitch = options.pitch || 1.0;
    utterance.rate = options.rate || 1.0;
    utterance.volume = options.volume || 1.0;
    
    window.speechSynthesis.speak(utterance);
    return true;
}
