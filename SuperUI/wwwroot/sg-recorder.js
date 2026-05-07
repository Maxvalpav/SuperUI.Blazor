const recorders = new Map();

export async function initRecorder(dotNetRef, instanceId, sources) {
    if (recorders.has(instanceId)) return;

    try {
        recorders.set(instanceId, {
            dotNetRef,
            instanceId,
            stream: null,
            recorder: null,
            startTime: null,
            videoDeviceId: null,
            audioDeviceId: null,
            isPaused: false,
            chunks: []
        });
    } catch (error) {
        console.error('Failed to initialize recorder:', error);
        await dotNetRef.invokeMethodAsync('OnErrorAsync', error.message || 'Initialization failed', 'initialization');
    }
}

export async function requestPermissions(instanceId, video, audio, videoConstraints, audioConstraints, videoDeviceId, audioDeviceId) {
    const recorder = recorders.get(instanceId);
    if (!recorder) return;

    try {
        const constraints = {};

        if (video) {
            constraints.video = {};
            if (videoDeviceId) {
                constraints.video.deviceId = { exact: videoDeviceId };
            }
            if (videoConstraints) {
                if (videoConstraints.width) constraints.video.width = { ideal: videoConstraints.width };
                if (videoConstraints.height) constraints.video.height = { ideal: videoConstraints.height };
                if (videoConstraints.frameRate) constraints.video.frameRate = { ideal: videoConstraints.frameRate };
                if (videoConstraints.aspectRatio) constraints.video.aspectRatio = videoConstraints.aspectRatio;
                if (videoConstraints.facingMode) constraints.video.facingMode = videoConstraints.facingMode;
            }
        } else {
            constraints.video = false;
        }

        if (audio) {
            constraints.audio = {};
            if (audioDeviceId) {
                constraints.audio.deviceId = { exact: audioDeviceId };
            }
            if (audioConstraints) {
                if (audioConstraints.echoCancellation !== undefined) constraints.audio.echoCancellation = audioConstraints.echoCancellation;
                if (audioConstraints.noiseSuppression !== undefined) constraints.audio.noiseSuppression = audioConstraints.noiseSuppression;
                if (audioConstraints.autoGainControl !== undefined) constraints.audio.autoGainControl = audioConstraints.autoGainControl;
                if (audioConstraints.sampleRate) constraints.audio.sampleRate = audioConstraints.sampleRate;
                if (audioConstraints.channelCount) constraints.audio.channelCount = audioConstraints.channelCount;
            }
        } else {
            constraints.audio = false;
        }

        recorder.stream = await navigator.mediaDevices.getUserMedia(constraints);
        recorder.videoDeviceId = videoDeviceId;
        recorder.audioDeviceId = audioDeviceId;

        const videoElement = document.getElementById(instanceId + '-preview');
        if (videoElement && recorder.stream) {
            videoElement.srcObject = recorder.stream;
        }

        const devices = await navigator.mediaDevices.enumerateDevices();
        const videoDevices = devices
            .filter(d => d.kind === 'videoinput')
            .map(d => ({ deviceId: d.deviceId, label: d.label || `Camera ${d.deviceId.slice(0, 8)}...` }));
        const audioDevices = devices
            .filter(d => d.kind === 'audioinput')
            .map(d => ({ deviceId: d.deviceId, label: d.label || `Microphone ${d.deviceId.slice(0, 8)}...` }));

        await recorder.dotNetRef.invokeMethodAsync('OnPermissionGrantedAsync', videoDevices, audioDevices);

    } catch (error) {
        console.error('Failed to get permissions:', error);
        await recorder.dotNetRef.invokeMethodAsync('OnErrorAsync',
            error.message || 'Permission denied',
            error.name === 'NotAllowedError' ? 'permission_denied' : 'permission_error');
        await recorder.dotNetRef.invokeMethodAsync('OnPermissionDeniedAsync');
    }
}

function getSupportedMimeType(preferredType) {
    const types = [
        preferredType,
        'video/webm',
        'video/webm;codecs=vp8',
        'video/webm;codecs=vp9',
        'video/webm;codecs=h264',
        'audio/webm',
        'audio/webm;codecs=opus',
        'audio/ogg'
    ];

    for (const type of types) {
        if (MediaRecorder.isTypeSupported(type)) {
            return type;
        }
    }
    return '';
}

export async function startRecording(instanceId, mimeType, bitsPerSecond, timeSlice, maxDuration) {
    const recorder = recorders.get(instanceId);
    if (!recorder || !recorder.stream) return;

    try {
        recorder.chunks = [];
        const options = {};

        const supportedMimeType = getSupportedMimeType(mimeType);
        if (supportedMimeType) {
            options.mimeType = supportedMimeType;
        }

        if (bitsPerSecond) {
            options.bitsPerSecond = bitsPerSecond;
        }

        recorder.recorder = new MediaRecorder(recorder.stream, options);
        recorder.startTime = Date.now();
        recorder.isPaused = false;

        recorder.recorder.ondataavailable = (event) => {
            if (event.data && event.data.size > 0) {
                recorder.chunks.push(event.data);
            }
        };

        recorder.recorder.onstop = async () => {
            const blob = new Blob(recorder.chunks, { type: supportedMimeType || mimeType });
            const duration = recorder.startTime ? (Date.now() - recorder.startTime) / 1000 : 0;

            const reader = new FileReader();
            reader.onload = async () => {
                await recorder.dotNetRef.invokeMethodAsync('OnStopAsync',
                    reader.result, duration, blob.size, blob.type);
            };
            reader.readAsDataURL(blob);
        };

        if (maxDuration && maxDuration > 0) {
            setTimeout(async () => {
                if (recorder.recorder && recorder.recorder.state === 'recording') {
                    await stopRecording(instanceId);
                }
            }, maxDuration * 1000);
        }

        recorder.recorder.start(timeSlice || 100);
        await recorder.dotNetRef.invokeMethodAsync('OnStartAsync', new Date().toISOString());

    } catch (error) {
        console.error('Failed to start recording:', error);
        await recorder.dotNetRef.invokeMethodAsync('OnErrorAsync',
            error.message || 'Failed to start recording', 'start_error');
    }
}

export async function pauseRecording(instanceId) {
    const recorder = recorders.get(instanceId);
    if (!recorder || !recorder.recorder || recorder.recorder.state !== 'recording') return;

    try {
        recorder.recorder.pause();
        recorder.isPaused = true;
        await recorder.dotNetRef.invokeMethodAsync('OnPauseAsync');
    } catch (error) {
        console.error('Failed to pause recording:', error);
    }
}

export async function resumeRecording(instanceId) {
    const recorder = recorders.get(instanceId);
    if (!recorder || !recorder.recorder || recorder.recorder.state !== 'paused') return;

    try {
        recorder.recorder.resume();
        recorder.isPaused = false;
        await recorder.dotNetRef.invokeMethodAsync('OnResumeAsync');
    } catch (error) {
        console.error('Failed to resume recording:', error);
    }
}

export async function stopRecording(instanceId) {
    const recorder = recorders.get(instanceId);
    if (!recorder || !recorder.recorder) return;

    try {
        if (recorder.recorder.state === 'recording' || recorder.recorder.state === 'paused') {
            recorder.recorder.stop();
        }
    } catch (error) {
        console.error('Failed to stop recording:', error);
        await recorder.dotNetRef.invokeMethodAsync('OnErrorAsync',
            error.message || 'Failed to stop recording', 'stop_error');
    }
}

export async function resetRecorder(instanceId) {
    const recorder = recorders.get(instanceId);
    if (!recorder) return;

    try {
        if (recorder.recorder) {
            if (recorder.recorder.state === 'recording' || recorder.recorder.state === 'paused') {
                recorder.recorder.stop();
            }
            recorder.recorder = null;
        }
        if (recorder.stream) {
            recorder.stream.getTracks().forEach(track => track.stop());
        }
        recorder.stream = null;
        recorder.chunks = [];
        recorder.startTime = null;
        recorder.isPaused = false;

        const videoElement = document.getElementById(instanceId + '-preview');
        if (videoElement) {
            videoElement.srcObject = null;
        }

        await recorder.dotNetRef.invokeMethodAsync('OnResetAsync');
    } catch (error) {
        console.error('Failed to reset recorder:', error);
    }
}

export async function switchCamera(instanceId, deviceId) {
    const recorder = recorders.get(instanceId);
    if (!recorder) return;

    try {
        recorder.videoDeviceId = deviceId;
        await resetRecorder(instanceId);
    } catch (error) {
        console.error('Failed to switch camera:', error);
    }
}

export async function switchMicrophone(instanceId, deviceId) {
    const recorder = recorders.get(instanceId);
    if (!recorder) return;

    try {
        recorder.audioDeviceId = deviceId;
        await resetRecorder(instanceId);
    } catch (error) {
        console.error('Failed to switch microphone:', error);
    }
}

export async function disposeRecorder(instanceId) {
    const recorder = recorders.get(instanceId);
    if (!recorder) return;

    try {
        if (recorder.recorder) {
            if (recorder.recorder.state === 'recording' || recorder.recorder.state === 'paused') {
                recorder.recorder.stop();
            }
        }
        if (recorder.stream) {
            recorder.stream.getTracks().forEach(track => track.stop());
        }
    } catch (error) {
        console.warn('Error disposing recorder:', error);
    }

    recorders.delete(instanceId);
}
