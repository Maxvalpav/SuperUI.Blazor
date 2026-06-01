const instances = new Map();

export function initVoiceRecognition(instanceId, dotNetRef, lang = 'ru-RU') {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

    if (!SpeechRecognition) {
        console.error('Speech recognition not supported in this browser.');
        return false;
    }

    const recognition = new SpeechRecognition();
    recognition.lang = lang;
    recognition.interimResults = true;
    recognition.continuous = true;

    recognition.onresult = (event) => {
        let interimTranscript = '';
        let finalTranscript = '';

        for (let i = event.resultIndex; i < event.results.length; ++i) {
            if (event.results[i].isFinal) {
                finalTranscript += event.results[i][0].transcript;
            } else {
                interimTranscript += event.results[i][0].transcript;
            }
        }

        try { dotNetRef?.invokeMethodAsync('OnSpeechResult', finalTranscript, interimTranscript)?.catch(() => {}); } catch {}
    };

    recognition.onerror = (event) => {
        console.error('Speech recognition error', event.error);
        try { dotNetRef?.invokeMethodAsync('OnSpeechError', event.error)?.catch(() => {}); } catch {}
    };

    recognition.onend = () => {
        try { dotNetRef?.invokeMethodAsync('OnSpeechEnd')?.catch(() => {}); } catch {}
    };

    instances.set(instanceId, { recognition, dotNetRef });
    return true;
}

export function startRecognition(instanceId) {
    const inst = instances.get(instanceId);
    if (inst?.recognition) {
        try {
            inst.recognition.start();
            return true;
        } catch (e) {
            console.warn('Recognition already started or failed', e);
        }
    }
    return false;
}

export function stopRecognition(instanceId) {
    const inst = instances.get(instanceId);
    if (inst?.recognition) {
        try { inst.recognition.stop(); } catch {}
    }
}

export function disposeVoiceRecognition(instanceId) {
    const inst = instances.get(instanceId);
    if (inst?.recognition) {
        try { inst.recognition.stop(); } catch {}
        try { inst.recognition.abort(); } catch {}
        inst.recognition.onresult = null;
        inst.recognition.onerror  = null;
        inst.recognition.onend    = null;
    }
    instances.delete(instanceId);
}
