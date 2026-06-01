
const streams = new Map();

export async function startCamera(videoElement, options = {}) {
    try {
        stopCamera(videoElement);

        const constraints = {
            video: {
                width: { ideal: options.width || 1280 },
                height: { ideal: options.height || 720 },
                facingMode: options.facingMode || 'user'
            },
            audio: false
        };

        const newStream = await navigator.mediaDevices.getUserMedia(constraints);
        streams.set(videoElement, newStream);
        videoElement.srcObject = newStream;
        await videoElement.play();
        return true;
    } catch (err) {
        console.error("Error accessing camera:", err);
        return false;
    }
}

export function stopCamera(videoElement) {
    const stream = streams.get(videoElement);
    if (stream) {
        stream.getTracks().forEach(track => track.stop());
        streams.delete(videoElement);
    }
    if (videoElement) {
        videoElement.srcObject = null;
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
