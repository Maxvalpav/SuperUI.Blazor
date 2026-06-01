let activeDotNetRef = null;
let mediaRecorder = null;
let recordedChunks = [];
let displayStream = null;
let recordedBlobUrl = null;

export async function startRecording(root, dotNetRef, options = {}) {
    if (!root || !dotNetRef) return;
    activeDotNetRef = dotNetRef;

    // 1. Interaction Recording (Existing logic)
    const handler = (e) => {
        if (!activeDotNetRef) return;
        const ev = {
            Type: e.type,
            Selector: e.target.tagName + (e.target.id ? '#' + e.target.id : '') + (e.target.className ? '.' + e.target.className.split(' ').join('.') : ''),
            TagName: e.target.tagName,
            Value: e.target.value,
            Timestamp: new Date().toISOString(),
            ClientX: e.clientX || null,
            ClientY: e.clientY || null
        };
        try {
            activeDotNetRef.invokeMethodAsync('OnInteractionCaptured', ev);
        } catch (err) {
            console.warn("Recorder: failed to send event", err);
        }
    };

    root._recorderHandler = handler;
    root.addEventListener('click', handler, true);
    root.addEventListener('input', handler, true);
    root.addEventListener('change', handler, true);

    // 2. Video Recording (New logic using html2canvas or getDisplayMedia)
    if (options.recordVideo) {
        try {
            displayStream = await navigator.mediaDevices.getDisplayMedia({
                video: { cursor: "always" },
                audio: options.recordAudio // This captures system/tab audio
            });
            let stream = displayStream;

            // If we also want microphone audio, we need to merge tracks
            if (options.recordAudio) {
                try {
                    const audioStream = await navigator.mediaDevices.getUserMedia({ audio: true });
                    const audioTracks = audioStream.getAudioTracks();
                    if (audioTracks.length > 0) {
                        // If getDisplayMedia also has audio, we might want to mix them, 
                        // but for simplicity we'll just add the microphone track if available.
                        // Or replace it if the user only wants microphone.
                        stream.addTrack(audioTracks[0]);
                    }
                } catch (audioErr) {
                    console.warn("Microphone access denied or failed, continuing with system audio only if available.", audioErr);
                }
            }

            recordedChunks = [];
            mediaRecorder = new MediaRecorder(stream, { mimeType: 'video/webm; codecs=vp9' });

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    recordedChunks.push(event.data);
                }
            };

            mediaRecorder.onstop = () => {
                if (recordedChunks.length > 0) {
                    const blob = new Blob(recordedChunks, { type: 'video/webm' });
                    const url = URL.createObjectURL(blob);
                    recordedBlobUrl = url;
                    if (dotNetRef) {
                        dotNetRef.invokeMethodAsync('HandleVideoCaptured', url);
                    }
                }
                if (displayStream) {
                    displayStream.getTracks().forEach(track => track.stop());
                    displayStream = null;
                }
            };

            mediaRecorder.start();
            return true;
        } catch (err) {
            console.error("Video recording failed:", err);
            return false;
        }
    }
    return true;
}

export function stopRecording(root) {
    if (root && root._recorderHandler) {
        root.removeEventListener('click', root._recorderHandler, true);
        root.removeEventListener('input', root._recorderHandler, true);
        root.removeEventListener('change', root._recorderHandler, true);
        delete root._recorderHandler;
    }

    if (mediaRecorder && mediaRecorder.state !== 'inactive') {
        mediaRecorder.stop();
    }

    if (displayStream) {
        displayStream.getTracks().forEach(track => track.stop());
        displayStream = null;
    }

    if (recordedBlobUrl) {
        URL.revokeObjectURL(recordedBlobUrl);
        recordedBlobUrl = null;
    }

    recordedChunks = [];
    activeDotNetRef = null;
}

function getSelector(el) {
    if (el.id) return '#' + el.id;
    if (el === document.body) return 'body';
    let path = [];
    while (el.parentElement) {
        let index = Array.from(el.parentElement.children).indexOf(el) + 1;
        path.unshift(`${el.tagName.toLowerCase()}:nth-child(${index})`);
        el = el.parentElement;
    }
    return path.join(' > ');
}

export function downloadUrl(url, fileName) {
     const a = document.createElement('a');
     a.href = url;
     a.download = fileName;
     document.body.appendChild(a);
     a.click();
     document.body.removeChild(a);
 }

export async function getBlobBase64(url) {
    const response = await fetch(url);
    const blob = await response.blob();
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result.split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}
