// sg-llm.js — SuperUI LLM JS Bridge (ESM)
// Independent of RAG logic, focused on chat and model interaction.

const _instances = new Map();
const _loaded = new Set();
const _moduleCache = new Map();

function _loadScript(url) {
  if (_loaded.has(url)) return Promise.resolve();
  return new Promise((resolve, reject) => {
    const el = document.createElement('script');
    el.src = url;
    el.onload = () => { _loaded.add(url); resolve(); };
    el.onerror = () => reject(new Error(`Failed to load script: ${url}`));
    document.head.appendChild(el);
  });
}

async function _importModule(url) {
  if (_moduleCache.has(url)) return _moduleCache.get(url);
  const mod = await import(/* @vite-ignore */ url);
  _moduleCache.set(url, mod);
  return mod;
}

async function _safeUnloadLlm(engine) {
  if (!engine || typeof engine.unload !== 'function') return;
  try {
    await engine.unload();
  } catch (err) {
    const name = err?.name || '';
    if (name === 'AbortError' || name === 'DOMException' || err instanceof DOMException) return;
    const msg = String(err?.message || '');
    if (msg.includes('mapAsync') || msg.includes('unmapped') || msg.includes('GPUBuffer')) return;
    console.warn('[sg-llm] LLM unload warning:', err);
  }
}

function _buildMultimodalContent(text, attachments) {
  if (!attachments || attachments.length === 0) return text || '';
  const parts = [];
  if (text && text.trim()) parts.push({ type: 'text', text: text.trim() });
  for (const att of attachments) {
    if (att.isImage) {
      parts.push({ type: 'image_url', image_url: { url: `data:${att.mimeType};base64,${att.base64}` } });
    } else if (att.isPdf) {
      parts.push({ type: 'file', file: { filename: att.name, file_data: `data:${att.mimeType};base64,${att.base64}` } });
    } else {
      try {
        const decoded = atob(att.base64);
        const bytes = new Uint8Array(decoded.length);
        for (let i = 0; i < decoded.length; i++) bytes[i] = decoded.charCodeAt(i);
        const fileText = new TextDecoder('utf-8').decode(bytes);
        const truncated = fileText.length > 8000 ? fileText.slice(0, 8000) + '\n...[truncated]' : fileText;
        parts.push({ type: 'text', text: `\n\n--- File: ${att.name} ---\n${truncated}\n--- End of file ---` });
      } catch (_) {
        parts.push({ type: 'text', text: `[Could not read file: ${att.name}]` });
      }
    }
  }
  return (parts.length === 1 && parts[0].type === 'text') ? parts[0].text : parts;
}

export async function init(dotnetRef, instanceId, options) {
  if (_instances.has(instanceId)) return;
  
  if (!window.__sgLlmGpuErrorHandlerInstalled) {
    window.__sgLlmGpuErrorHandlerInstalled = true;
    window.addEventListener('unhandledrejection', (event) => {
      const err = event.reason;
      if (!err) return;
      const msg = String(err?.message || err || '');
      if (msg.includes('mapAsync') || msg.includes('unmapped') || msg.includes('GPUBuffer') || err?.name === 'AbortError') {
        event.preventDefault();
      }
    });
  }

  _instances.set(instanceId, {
    dotnetRef,
    options: options || {},
    llmEngine: null,
    _directHistory: []
  });
}

export async function dispose(instanceId) {
  const inst = _instances.get(instanceId);
  if (!inst) return;
  if (inst.llmEngine) await _safeUnloadLlm(inst.llmEngine);
  _instances.delete(instanceId);
}

export function checkWebGpu() {
  const available = !!(navigator.gpu);
  return { available, adapter: available ? 'gpu' : null };
}

export async function loadLlm(instanceId, provider, modelId, opts) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);

  const providerLower = (provider || '').toLowerCase();

  if (providerLower === 'webllm') {
    const src = opts?.webLlmScript || 'https://cdn.jsdelivr.net/npm/@mlc-ai/web-llm@0.2.83/lib/index.js';
    const webllm = await _importModule(src);
    const progressCallback = (report) => {
      try {
        inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', {
          stage: report.text || '',
          loaded: report.progress || 0,
          total: 1,
          percent: (report.progress || 0) * 100,
          file: null,
          isComplete: report.progress >= 1,
        });
      } catch (_) {}
    };
    inst.llmEngine = await webllm.CreateMLCEngine(modelId, { initProgressCallback: progressCallback });
  } else if (providerLower === 'openaicompatible' || providerLower === 'openrouter') {
    const isOR = providerLower === 'openrouter';
    const extraHeaders = {};
    if (isOR) {
      extraHeaders['HTTP-Referer'] = window.location.origin;
      extraHeaders['X-Title'] = 'SuperUI';
    }
    inst.llmEngine = {
      kind: 'openai',
      baseUrl: opts?.baseUrl || (isOR ? 'https://openrouter.ai/api/v1' : 'https://api.openai.com/v1'),
      apiKey: opts?.apiKey || inst.options.apiKey || '',
      model: modelId || (isOR ? 'google/gemini-2.0-flash-001:free' : 'gpt-4o-mini'),
      extraHeaders
    };
    try {
      inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', {
        stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true,
      });
    } catch (_) {}
  }
}

export async function chatDirectStream(instanceId, message, systemPrompt, attachments, streamId) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);
  if (!inst.llmEngine) throw new Error('LLM not loaded');

  if (!inst._directHistory) inst._directHistory = [];
  const sysMsg = systemPrompt || 'You are a helpful assistant.';
  // Build multimodal user content (text + optional images/PDFs)
  const userContent = _buildMultimodalContent(message, attachments);

  // Build messages array: system + full history + new user message
  const messages = [
    { role: 'system', content: sysMsg },
    ...inst._directHistory,
    { role: 'user', content: userContent },
  ];

  // Store text-only version in history for NEXT turns (base64 would bloat it)
  const historyText = message || (attachments?.length ? `[${attachments.length} file(s)]` : '');
  const userTurn = { role: 'user', content: historyText };

  let fullAnswer = '';
  const _sendToken = (token) => {
    fullAnswer += token;
    try { inst.dotnetRef.invokeMethodAsync('OnStreamTokenCallback', token); } catch (_) {}
  };

  const _sendComplete = () => {
    inst._directHistory.push(userTurn);
    inst._directHistory.push({ role: 'assistant', content: fullAnswer });
    if (inst._directHistory.length > 20) inst._directHistory = inst._directHistory.slice(-20);
    const answer = {
      question: message,
      answer: fullAnswer,
      sources: [],
      promptTokens: Math.ceil(messages.reduce((s, m) => s + (typeof m.content === 'string' ? m.content.length : 1000), 0) / 4),
      completionTokens: Math.ceil(fullAnswer.length / 4),
      durationMs: 0,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnStreamCompleteCallback', answer); } catch (_) {}
  };

  if (inst.llmEngine.kind === 'openai') {
    const abortCtrl = new AbortController();
    inst._activeAbortCtrl = abortCtrl;
    try {
      const response = await fetch(`${inst.llmEngine.baseUrl}/chat/completions`, {
        method: 'POST',
        signal: abortCtrl.signal,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${inst.llmEngine.apiKey}`,
          ...(inst.llmEngine.extraHeaders || {}),
        },
        body: JSON.stringify({ model: inst.llmEngine.model, messages, stream: true }),
      });
      if (!response.ok) throw new Error(`LLM API error ${response.status}`);
      const reader = response.body.getReader();
      const decoder = new TextDecoder('utf-8');
      let buffer = '';
      while (true) {
        const { done, value } = await reader.read();
        if (done || abortCtrl.signal.aborted) break;
        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop();
        for (const line of lines) {
          const trimmed = line.trim();
          if (!trimmed.startsWith('data:')) continue;
          const payload = trimmed.slice(5).trim();
          if (payload === '[DONE]') break;
          try {
            const parsed = JSON.parse(payload);
            const token = parsed.choices?.[0]?.delta?.content;
            if (token) _sendToken(token);
          } catch (_) {}
        }
      }
    } catch (err) {
      if (err?.name !== 'AbortError') throw err;
    } finally {
      inst._activeAbortCtrl = null;
      _sendComplete();
    }
  } else {
    // WebLLM
    try {
      const stream = await inst.llmEngine.chat.completions.create({ messages, stream: true });
      for await (const chunk of stream) {
        const token = chunk.choices?.[0]?.delta?.content;
        if (token) _sendToken(token);
      }
    } catch (err) {
      if (err?.name !== 'AbortError') throw err;
    } finally {
      _sendComplete();
    }
  }
}

export function clearDirectHistory(instanceId) {
  const inst = _instances.get(instanceId);
  if (inst) inst._directHistory = [];
}

export async function renderMarkdown(text, markedSrc) {
  const src = markedSrc || 'https://cdn.jsdelivr.net/npm/marked@12.0.0/marked.min.js';
  await _loadScript(src);
  const marked = window.marked;
  if (!marked) return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  marked.setOptions({ gfm: true, breaks: true });
  const html = marked.parse(text || '');
  return html.replace(/<script[\s\S]*?<\/script>/gi, '')
             .replace(/\son\w+\s*=\s*["'][^"']*["']/gi, '')
             .replace(/javascript\s*:/gi, 'nojs:');
}
