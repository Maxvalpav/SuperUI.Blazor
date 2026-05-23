
let recognition = null;
let activeDotNetRef = null;

export function initVoiceRecognition(dotNetRef, lang = 'ru-RU') {
    activeDotNetRef = dotNetRef;
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    
    if (!SpeechRecognition) {
        console.error('Speech recognition not supported in this browser.');
        return false;
    }

    recognition = new SpeechRecognition();
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

        if (activeDotNetRef) {
            activeDotNetRef.invokeMethodAsync('OnSpeechResult', finalTranscript, interimTranscript);
        }
    };

    recognition.onerror = (event) => {
        console.error('Speech recognition error', event.error);
        if (activeDotNetRef) {
            activeDotNetRef.invokeMethodAsync('OnSpeechError', event.error);
        }
    };

    recognition.onend = () => {
        if (activeDotNetRef) {
            activeDotNetRef.invokeMethodAsync('OnSpeechEnd');
        }
    };

    return true;
}

export function startRecognition() {
    if (recognition) {
        try {
            recognition.start();
            return true;
        } catch (e) {
            console.warn('Recognition already started or failed', e);
        }
    }
    return false;
}

export function stopRecognition() {
    if (recognition) {
        try { recognition.stop(); } catch {}
    }
}

export function disposeVoiceRecognition() {
    if (recognition) {
        try { recognition.stop(); } catch {}
        try { recognition.abort(); } catch {}
        recognition.onresult = null;
        recognition.onerror  = null;
        recognition.onend    = null;
        recognition = null;
    }
    activeDotNetRef = null;
}
