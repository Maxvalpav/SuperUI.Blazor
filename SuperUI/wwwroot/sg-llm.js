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
    } else if (att.isPdf || att.isVideo) {
      // Pass as file part (OpenRouter/OpenAI-compatible)
      parts.push({ 
        type: 'file', 
        file: { 
          name: att.name, 
          data: `data:${att.mimeType};base64,${att.base64}` 
        } 
      });
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
  } else if (providerLower === 'openaicompatible' || providerLower === 'openrouter' || providerLower === 'opencode' || providerLower === 'openai') {
    const isOR = providerLower === 'openrouter';
    const isOC = providerLower === 'opencode';
    const extraHeaders = {};
    if (isOR) {
      extraHeaders['HTTP-Referer'] = window.location.origin;
      extraHeaders['X-Title'] = 'SuperUI';
    }
    inst.llmEngine = {
      kind: 'openai',
      baseUrl: opts?.baseUrl || (isOR ? 'https://openrouter.ai/api/v1' : (isOC ? 'https://api.opencode.ai/v1' : 'https://api.openai.com/v1')),
      apiKey: opts?.apiKey || inst.options.apiKey || '',
      model: modelId || (isOR ? 'google/gemini-2.0-flash-001:free' : (isOC ? 'mistralai/mistral-7b-instruct' : 'gpt-4o-mini')),
      extraHeaders,
      temperature: opts?.temperature ?? 0.7,
      topP: opts?.topP ?? 1.0,
      maxTokens: opts?.maxTokens,
      presencePenalty: opts?.presencePenalty ?? 0.0,
      frequencyPenalty: opts?.frequencyPenalty ?? 0.0,
    };
    try {
      inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', {
        stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true,
      });
    } catch (_) {}
  } else if (providerLower === 'anthropic') {
    inst.llmEngine = {
      kind: 'anthropic',
      baseUrl: opts?.baseUrl || 'https://api.anthropic.com/v1',
      apiKey: opts?.apiKey || inst.options.apiKey || '',
      model: modelId || 'claude-3-5-sonnet-20240620',
      temperature: opts?.temperature ?? 0.7,
      topP: opts?.topP ?? 1.0,
      maxTokens: opts?.maxTokens,
    };
    try {
      inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', {
        stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true,
      });
    } catch (_) {}
  } else if (providerLower === 'ollama') {
    inst.llmEngine = {
      kind: 'ollama',
      baseUrl: opts?.baseUrl || 'http://localhost:11434',
      model: modelId || 'llama3',
      temperature: opts?.temperature ?? 0.7,
      topP: opts?.topP ?? 1.0,
      maxTokens: opts?.maxTokens,
    };
    try {
      inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', {
        stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true,
      });
    } catch (_) {}
  }
}

export async function chatDirectStream(instanceId, message, systemPrompt, attachments, streamId, tools, toolChoice) {
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
  let toolCalls = [];

  const _sendToken = (token) => {
    fullAnswer += token;
    try { inst.dotnetRef.invokeMethodAsync('OnStreamTokenCallback', token); } catch (_) {}
  };

  const _sendComplete = () => {
    inst._directHistory.push(userTurn);
    inst._directHistory.push({ role: 'assistant', content: fullAnswer, tool_calls: toolCalls.length ? toolCalls : undefined });
    if (inst._directHistory.length > 20) inst._directHistory = inst._directHistory.slice(-20);
    const answer = {
      question: message,
      answer: fullAnswer,
      tool_calls: toolCalls,
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
      const body = { 
        model: inst.llmEngine.model, 
        messages, 
        stream: true,
        tools: tools || undefined,
        tool_choice: toolChoice || undefined,
        temperature: inst.llmEngine.temperature,
        top_p: inst.llmEngine.topP,
        max_tokens: inst.llmEngine.maxTokens || undefined,
        presence_penalty: inst.llmEngine.presencePenalty,
        frequency_penalty: inst.llmEngine.frequencyPenalty
      };

      const response = await fetch(`${inst.llmEngine.baseUrl.replace(/\/$/, '')}/chat/completions`, {
        method: 'POST',
        signal: abortCtrl.signal,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${inst.llmEngine.apiKey}`,
          ...(inst.llmEngine.extraHeaders || {}),
        },
        body: JSON.stringify(body),
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
            const delta = parsed.choices?.[0]?.delta;
            if (delta?.content) _sendToken(delta.content);
            if (delta?.tool_calls) {
              for (const tc of delta.tool_calls) {
                if (!toolCalls[tc.index]) {
                  toolCalls[tc.index] = { id: tc.id, type: 'function', function: { name: '', arguments: '' } };
                }
                if (tc.function?.name) toolCalls[tc.index].function.name += tc.function.name;
                if (tc.function?.arguments) toolCalls[tc.index].function.arguments += tc.function.arguments;
              }
            }
          } catch (_) {}
        }
      }
    } catch (err) {
      if (err?.name !== 'AbortError') throw err;
    } finally {
      inst._activeAbortCtrl = null;
      _sendComplete();
    }
  } else if (inst.llmEngine.kind === 'ollama') {
     const abortCtrl = new AbortController();
     inst._activeAbortCtrl = abortCtrl;
     try {
       // Ollama API supports chat completions at /api/chat
       const response = await fetch(`${inst.llmEngine.baseUrl.replace(/\/$/, '')}/api/chat`, {
         method: 'POST',
         signal: abortCtrl.signal,
         headers: { 'Content-Type': 'application/json' },
         body: JSON.stringify({ 
           model: inst.llmEngine.model, 
           messages: messages, // Ollama uses the same messages format
           stream: true,
           tools: tools || undefined,
           options: {
             temperature: inst.llmEngine.temperature,
             top_p: inst.llmEngine.topP,
             num_predict: inst.llmEngine.maxTokens || undefined
           }
         }),
       });
       if (!response.ok) throw new Error(`Ollama API error ${response.status}`);
       const reader = response.body.getReader();
       const decoder = new TextDecoder('utf-8');
       let buffer = '';
       while (true) {
         const { done, value } = await reader.read();
         if (done || abortCtrl.signal.aborted) break;
         
         buffer += decoder.decode(value, { stream: true });
         
         // Ollama might send multiple JSON objects in one chunk or split one object across chunks
         let boundary = buffer.indexOf('\n');
         while (boundary !== -1) {
           const line = buffer.slice(0, boundary).trim();
           buffer = buffer.slice(boundary + 1);
           
           if (line) {
             try {
               const parsed = JSON.parse(line);
               if (parsed.message?.content) {
                 _sendToken(parsed.message.content);
               }
               if (parsed.message?.tool_calls) {
                 for (const tc of parsed.message.tool_calls) {
                   // Ollama tool calls are usually complete in one chunk or a few
                   toolCalls.push(tc);
                 }
               }
               if (parsed.done) break;
             } catch (e) {
               console.error('[sg-llm] Ollama parse error:', e, line);
             }
           }
           boundary = buffer.indexOf('\n');
         }
       }
     } catch (err) {
       if (err?.name !== 'AbortError') throw err;
     } finally {
       inst._activeAbortCtrl = null;
       _sendComplete();
     }
    } else if (inst.llmEngine.kind === 'anthropic') {
      const abortCtrl = new AbortController();
      inst._activeAbortCtrl = abortCtrl;
      try {
        const body = {
          model: inst.llmEngine.model,
          messages: messages.filter(m => m.role !== 'system'),
          system: messages.find(m => m.role === 'system')?.content || undefined,
          max_tokens: inst.llmEngine.maxTokens || 4096,
          stream: true,
          tools: tools || undefined,
          temperature: inst.llmEngine.temperature,
          top_p: inst.llmEngine.topP,
        };

        const response = await fetch(`${inst.llmEngine.baseUrl.replace(/\/$/, '')}/messages`, {
          method: 'POST',
          signal: abortCtrl.signal,
          headers: {
            'Content-Type': 'application/json',
            'x-api-key': inst.llmEngine.apiKey,
            'anthropic-version': '2023-06-01',
            'dangerously-allow-browser': 'true'
          },
          body: JSON.stringify(body),
        });
        if (!response.ok) throw new Error(`Anthropic API error ${response.status}`);
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
            try {
              const parsed = JSON.parse(payload);
              if (parsed.type === 'content_block_delta' && parsed.delta?.text) {
                _sendToken(parsed.delta.text);
              }
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
