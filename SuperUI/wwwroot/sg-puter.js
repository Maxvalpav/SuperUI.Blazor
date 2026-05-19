// sg-puter.js — SuperUI Puter.js Bridge
// Provides access to Puter.js cloud features (AI, KV, FS, Auth, etc.)

const PUTER_SDK_URL = 'https://js.puter.com/v2/';
let _puterLoadPromise = null;

function _getPuter() {
    if (typeof window !== 'undefined' && window.puter) return window.puter;
    if (typeof globalThis !== 'undefined' && globalThis.puter) return globalThis.puter;
    return null;
}

async function _loadScript(url) {
    await new Promise((resolve, reject) => {
        const existing = document.querySelector(`script[src="${url}"]`);
        if (existing) {
            existing.addEventListener('load', () => resolve(), { once: true });
            existing.addEventListener('error', () => reject(new Error('Failed to load Puter.js SDK.')), { once: true });
            if (_getPuter()) resolve();
            return;
        }

        const script = document.createElement('script');
        script.src = url;
        script.async = true;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Failed to load Puter.js SDK.'));
        document.head.appendChild(script);
    });
}

async function ensurePuter() {
    const existing = _getPuter();
    if (existing) return existing;

    if (!_puterLoadPromise) {
        _puterLoadPromise = _loadScript(PUTER_SDK_URL)
            .catch((err) => {
                _puterLoadPromise = null;
                throw err;
            });
    }

    await _puterLoadPromise;

    const puter = _getPuter();
    if (!puter) {
        throw new Error('Puter.js SDK loaded, but global "puter" is unavailable.');
    }

    return puter;
}

export async function chat(message, model, stream, dotnetRef) {
    try {
        const puter = await ensurePuter();
        const response = await puter.ai.chat(message, { 
            model: model || 'gpt-4o-mini',
            stream: !!stream
        });

        if (stream) {
            for await (const part of response) {
                if (part?.text) {
                    await dotnetRef.invokeMethodAsync('OnTokenReceivedCallback', part.text);
                }
            }
            await dotnetRef.invokeMethodAsync('OnChatCompleteCallback', '');
        } else {
            return response.message.content;
        }
    } catch (err) {
        console.error('[sg-puter] Chat error:', err);
        if (dotnetRef) {
            await dotnetRef.invokeMethodAsync('OnErrorCallback', err.message || err.toString());
        }
        throw err;
    }
}

// AI Features
export async function txt2img(prompt) {
    const puter = await ensurePuter();
    const result = await puter.ai.txt2img(prompt);
    return result?.src || '';
}

export async function txt2speech(text) {
    const puter = await ensurePuter();
    return await puter.ai.txt2speech(text);
}

export async function img2txt(image) {
    const puter = await ensurePuter();
    return await puter.ai.img2txt(image);
}

// Auth
export async function signIn() {
    const puter = await ensurePuter();
    return await puter.auth.signIn();
}

export async function signOut() {
    const puter = await ensurePuter();
    return await puter.auth.signOut();
}

export async function isSignedIn() {
    const puter = await ensurePuter();
    return await puter.auth.isSignedIn();
}

export async function getUser() {
    const puter = await ensurePuter();
    return await puter.auth.getUser();
}

// Key-Value Store
export async function kvSet(key, value) {
    const puter = await ensurePuter();
    return await puter.kv.set(key, value);
}

export async function kvGet(key) {
    const puter = await ensurePuter();
    return await puter.kv.get(key);
}

export async function kvList() {
    const puter = await ensurePuter();
    return await puter.kv.list();
}

export async function kvDel(key) {
    const puter = await ensurePuter();
    return await puter.kv.del(key);
}

// Cloud Storage (FS)
export async function fsWrite(path, content) {
    const puter = await ensurePuter();
    return await puter.fs.write(path, content);
}

export async function fsRead(path) {
    const puter = await ensurePuter();
    return await puter.fs.read(path);
}

export async function fsReaddir(path) {
    const puter = await ensurePuter();
    return await puter.fs.readdir(path);
}

export async function fsMkdir(path) {
    const puter = await ensurePuter();
    return await puter.fs.mkdir(path);
}

export async function fsDelete(path) {
    const puter = await ensurePuter();
    return await puter.fs.delete(path);
}

// UI Utilities
export async function alert(message) {
    const puter = await ensurePuter();
    return await puter.ui.alert(message);
}

export async function notify(message, title) {
    const puter = await ensurePuter();
    return await puter.ui.notify(title, message);
}

export async function isPuterAvailable() {
    try {
        await ensurePuter();
        return true;
    } catch (_) {
        return false;
    }
}
