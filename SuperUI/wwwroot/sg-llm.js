// sg-llm.js — SuperUI LLM JS Bridge (ESM)
// Supports: WebLLM, OpenAI-compatible (OpenAI/OpenRouter/OpenCode/Mistral/Groq/DeepSeek/xAI/Cohere/Perplexity/Together/Fireworks/Cerebras/HuggingFace),
// Anthropic, Google Gemini (v1beta), Ollama, Azure OpenAI.

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
  try { await engine.unload(); }
  catch (err) {
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
      parts.push({ type: 'file', file: { name: att.name, data: `data:${att.mimeType};base64,${att.base64}` } });
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
  _instances.set(instanceId, { dotnetRef, options: options || {}, llmEngine: null, _directHistory: [] });
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

// ── Provider default base URLs ─────────────────────────────────────────────
const _providerDefaults = {
  openai:        { url: 'https://api.openai.com/v1',                 model: 'gpt-4o-mini' },
  openaicompatible:{ url: 'https://api.openai.com/v1',               model: 'gpt-4o-mini' },
  openrouter:    { url: 'https://openrouter.ai/api/v1',              model: 'openrouter/free' },
  opencode:      { url: 'https://api.opencode.ai/v1',                model: 'mistralai/mistral-7b-instruct' },
  mistral:       { url: 'https://api.mistral.ai/v1',                 model: 'mistral-large-latest' },
  groq:          { url: 'https://api.groq.com/openai/v1',            model: 'llama-3.3-70b-versatile' },
  deepseek:      { url: 'https://api.deepseek.com',                  model: 'deepseek-chat' },
  xai:           { url: 'https://api.x.ai/v1',                       model: 'grok-4' },
  cohere:        { url: 'https://api.cohere.ai/compatibility/v1',    model: 'command-a-03-2025' },
  perplexity:    { url: 'https://api.perplexity.ai',                 model: 'sonar' },
  togetherai:    { url: 'https://api.together.xyz/v1',               model: 'meta-llama/Llama-3.3-70B-Instruct-Turbo' },
  fireworks:     { url: 'https://api.fireworks.ai/inference/v1',     model: 'accounts/fireworks/models/llama-v3p3-70b-instruct' },
  cerebras:      { url: 'https://api.cerebras.ai/v1',                model: 'llama-3.3-70b' },
  huggingface:   { url: 'https://router.huggingface.co/v1',          model: 'meta-llama/Meta-Llama-3.1-70B-Instruct' },
};

function _isOpenAiKind(p) {
  return ['openai','openaicompatible','openrouter','opencode','mistral','groq','deepseek',
          'xai','cohere','perplexity','togetherai','fireworks','cerebras','huggingface','azureopenai'].includes(p);
}

export async function loadLlm(instanceId, provider, modelId, opts) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);
  opts = opts || {};
  const providerLower = (provider || '').toLowerCase();

  if (providerLower === 'webllm') {
    const src = opts?.webLlmScript || 'https://cdn.jsdelivr.net/npm/@mlc-ai/web-llm@0.2.83/lib/index.js';
    const webllm = await _importModule(src);
    const progressCallback = (report) => {
      try {
        inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', {
          stage: report.text || '', loaded: report.progress || 0, total: 1,
          percent: (report.progress || 0) * 100, file: null, isComplete: report.progress >= 1,
        });
      } catch (_) {}
    };
    inst.llmEngine = await webllm.CreateMLCEngine(modelId, { initProgressCallback: progressCallback });
  } else if (_isOpenAiKind(providerLower)) {
    const defaults = _providerDefaults[providerLower] || _providerDefaults.openaicompatible;
    const extraHeaders = { ...(opts.extraHeaders || {}) };
    if (providerLower === 'openrouter') {
      extraHeaders['HTTP-Referer'] = window.location.origin;
      extraHeaders['X-Title'] = 'SuperUI';
    }
    let baseUrl = opts?.baseUrl || defaults.url;
    if (providerLower === 'azureopenai' && opts.azureDeployment && opts.azureApiVersion) {
      // Azure uses deployment-scoped URL; we'll build the chat URL at request time.
    }
    inst.llmEngine = {
      kind: 'openai',
      sub: providerLower,
      baseUrl,
      apiKey: opts?.apiKey || inst.options.apiKey || '',
      model: modelId || defaults.model,
      extraHeaders,
      opts,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', { stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true }); } catch (_) {}
  } else if (providerLower === 'anthropic') {
    inst.llmEngine = {
      kind: 'anthropic',
      baseUrl: opts?.baseUrl || 'https://api.anthropic.com/v1',
      apiKey: opts?.apiKey || inst.options.apiKey || '',
      model: modelId || 'claude-sonnet-4-6',
      opts,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', { stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true }); } catch (_) {}
  } else if (providerLower === 'google') {
    inst.llmEngine = {
      kind: 'google',
      baseUrl: opts?.baseUrl || 'https://generativelanguage.googleapis.com/v1beta',
      apiKey: opts?.apiKey || inst.options.apiKey || '',
      model: modelId || 'gemini-2.5-flash',
      opts,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', { stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true }); } catch (_) {}
  } else if (providerLower === 'ollama') {
    inst.llmEngine = {
      kind: 'ollama',
      baseUrl: opts?.baseUrl || 'http://localhost:11434',
      model: modelId || 'llama3',
      opts,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', { stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true }); } catch (_) {}
  }
}

// Add only defined properties to a body object.
function _put(body, key, val) {
  if (val === undefined || val === null) return;
  if (Array.isArray(val) && val.length === 0) return;
  if (typeof val === 'string' && val.length === 0) return;
  body[key] = val;
}

function _buildOpenAiBody(engine, messages, tools, toolChoice) {
  const o = engine.opts || {};
  const body = { model: engine.model, messages, stream: true };
  _put(body, 'tools', tools);
  _put(body, 'tool_choice', toolChoice);
  // Sampling (only present when advanced flag set on .NET side passed them through)
  _put(body, 'temperature', o.temperature);
  _put(body, 'top_p', o.topP);
  _put(body, 'max_tokens', o.maxTokens);
  _put(body, 'presence_penalty', o.presencePenalty);
  _put(body, 'frequency_penalty', o.frequencyPenalty);
  _put(body, 'seed', o.seed);
  _put(body, 'stop', o.stop);
  _put(body, 'top_k', o.topK);
  _put(body, 'min_p', o.minP);
  _put(body, 'repetition_penalty', o.repetitionPenalty);
  _put(body, 'logprobs', o.logProbs);
  _put(body, 'top_logprobs', o.topLogProbs);
  _put(body, 'parallel_tool_calls', o.parallelToolCalls);
  _put(body, 'user', o.user);
  // Reasoning / verbosity (OpenAI o-series, GPT-5)
  _put(body, 'reasoning_effort', o.reasoningEffort);
  _put(body, 'verbosity', o.verbosity);
  // Service tier (OpenAI)
  _put(body, 'service_tier', o.serviceTier);
  // Response format
  if (o.responseFormat) {
    if (o.responseFormat === 'json_object') body.response_format = { type: 'json_object' };
    else if (o.responseFormat === 'json_schema' && o.jsonSchema) {
      try { body.response_format = { type: 'json_schema', json_schema: JSON.parse(o.jsonSchema) }; }
      catch { body.response_format = { type: 'json_object' }; }
    } else if (o.responseFormat === 'text') body.response_format = { type: 'text' };
  }
  // Stream usage (OpenAI)
  if (o.streamUsage) body.stream_options = { include_usage: true };

  // OpenRouter-specific routing
  if (engine.sub === 'openrouter') {
    if (o.orFallbackModels && o.orFallbackModels.length > 0) body.models = o.orFallbackModels;
    if (o.orTransforms) body.transforms = [o.orTransforms];
    const provider = {};
    if (o.orProviderSort) provider.sort = o.orProviderSort;
    if (o.orAllowedProviders?.length) provider.order = o.orAllowedProviders;
    if (o.orIgnoredProviders?.length) provider.ignore = o.orIgnoredProviders;
    if (o.orRequireParameters !== undefined) provider.require_parameters = !!o.orRequireParameters;
    if (o.orAllowDataCollection !== undefined) {
      provider.data_collection = o.orAllowDataCollection ? 'allow' : 'deny';
    }
    if (Object.keys(provider).length) body.provider = provider;
  }
  return body;
}

function _buildOpenAiUrl(engine) {
  if (engine.sub === 'azureopenai') {
    const o = engine.opts || {};
    const base = engine.baseUrl.replace(/\/$/, '');
    const deployment = o.azureDeployment || engine.model;
    const api = o.azureApiVersion || '2024-10-21';
    return `${base}/openai/deployments/${encodeURIComponent(deployment)}/chat/completions?api-version=${encodeURIComponent(api)}`;
  }
  return `${engine.baseUrl.replace(/\/$/, '')}/chat/completions`;
}

function _buildOpenAiHeaders(engine) {
  const headers = { 'Content-Type': 'application/json', ...(engine.extraHeaders || {}) };
  if (engine.sub === 'azureopenai') {
    headers['api-key'] = engine.apiKey;
  } else {
    headers['Authorization'] = `Bearer ${engine.apiKey}`;
  }
  return headers;
}

export async function chatDirectStream(instanceId, message, systemPrompt, attachments, streamId, tools, toolChoice) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);
  if (!inst.llmEngine) throw new Error('LLM not loaded');

  if (!inst._directHistory) inst._directHistory = [];
  const sysMsg = systemPrompt || 'You are a helpful assistant.';
  const userContent = _buildMultimodalContent(message, attachments);

  const messages = [
    { role: 'system', content: sysMsg },
    ...inst._directHistory,
    { role: 'user', content: userContent },
  ];

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
      question: message, answer: fullAnswer, tool_calls: toolCalls, sources: [],
      promptTokens: Math.ceil(messages.reduce((s, m) => s + (typeof m.content === 'string' ? m.content.length : 1000), 0) / 4),
      completionTokens: Math.ceil(fullAnswer.length / 4), durationMs: 0,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnStreamCompleteCallback', answer); } catch (_) {}
  };

  if (inst.llmEngine.kind === 'openai') {
    const abortCtrl = new AbortController();
    inst._activeAbortCtrl = abortCtrl;
    try {
      const body = _buildOpenAiBody(inst.llmEngine, messages, tools, toolChoice);
      const url = _buildOpenAiUrl(inst.llmEngine);
      const headers = _buildOpenAiHeaders(inst.llmEngine);
      // Diagnostic log — shows in DevTools console so users can confirm the key,
      // base URL and model actually reaching the provider.
      const keyMasked = (inst.llmEngine.apiKey || '').length > 8
        ? `${inst.llmEngine.apiKey.slice(0,6)}…${inst.llmEngine.apiKey.slice(-4)} (len=${inst.llmEngine.apiKey.length})`
        : `(empty, len=${(inst.llmEngine.apiKey || '').length})`;
      console.info('[sg-llm] →', { url, model: inst.llmEngine.model, sub: inst.llmEngine.sub, apiKey: keyMasked });
      const response = await fetch(url, { method: 'POST', signal: abortCtrl.signal, headers, body: JSON.stringify(body) });
      if (!response.ok) {
        const t = await response.text().catch(() => '');
        throw new Error(`LLM API error ${response.status} ${t.slice(0,200)}`);
      }
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
            if (delta?.reasoning_content) _sendToken(delta.reasoning_content);
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
      if (err?.name !== 'AbortError') {
        try { inst.dotnetRef.invokeMethodAsync('OnErrorCallback', String(err?.message || err)); } catch (_) {}
      }
    } finally {
      inst._activeAbortCtrl = null;
      _sendComplete();
    }
  } else if (inst.llmEngine.kind === 'ollama') {
    const abortCtrl = new AbortController();
    inst._activeAbortCtrl = abortCtrl;
    try {
      const o = inst.llmEngine.opts || {};
      const options = {};
      _put(options, 'temperature', o.temperature);
      _put(options, 'top_p', o.topP);
      _put(options, 'top_k', o.topK);
      _put(options, 'min_p', o.minP);
      _put(options, 'repeat_penalty', o.repetitionPenalty);
      _put(options, 'presence_penalty', o.presencePenalty);
      _put(options, 'frequency_penalty', o.frequencyPenalty);
      _put(options, 'num_predict', o.maxTokens);
      _put(options, 'seed', o.seed);
      _put(options, 'stop', o.stop);
      const body = { model: inst.llmEngine.model, messages, stream: true };
      if (Object.keys(options).length) body.options = options;
      if (tools) body.tools = tools;
      if (o.responseFormat === 'json_object') body.format = 'json';
      else if (o.responseFormat === 'json_schema' && o.jsonSchema) {
        try { body.format = JSON.parse(o.jsonSchema); } catch {}
      }

      const response = await fetch(`${inst.llmEngine.baseUrl.replace(/\/$/, '')}/api/chat`, {
        method: 'POST', signal: abortCtrl.signal,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!response.ok) throw new Error(`Ollama API error ${response.status}`);
      const reader = response.body.getReader();
      const decoder = new TextDecoder('utf-8');
      let buffer = '';
      while (true) {
        const { done, value } = await reader.read();
        if (done || abortCtrl.signal.aborted) break;
        buffer += decoder.decode(value, { stream: true });
        let boundary = buffer.indexOf('\n');
        while (boundary !== -1) {
          const line = buffer.slice(0, boundary).trim();
          buffer = buffer.slice(boundary + 1);
          if (line) {
            try {
              const parsed = JSON.parse(line);
              if (parsed.message?.content) _sendToken(parsed.message.content);
              if (parsed.message?.tool_calls) {
                for (const tc of parsed.message.tool_calls) toolCalls.push(tc);
              }
              if (parsed.done) break;
            } catch (e) { console.error('[sg-llm] Ollama parse error:', e, line); }
          }
          boundary = buffer.indexOf('\n');
        }
      }
    } catch (err) {
      if (err?.name !== 'AbortError') {
        try { inst.dotnetRef.invokeMethodAsync('OnErrorCallback', String(err?.message || err)); } catch (_) {}
      }
    } finally {
      inst._activeAbortCtrl = null;
      _sendComplete();
    }
  } else if (inst.llmEngine.kind === 'anthropic') {
    const abortCtrl = new AbortController();
    inst._activeAbortCtrl = abortCtrl;
    try {
      const o = inst.llmEngine.opts || {};
      const body = {
        model: inst.llmEngine.model,
        messages: messages.filter(m => m.role !== 'system').map(m => ({ role: m.role, content: m.content })),
        max_tokens: o.maxTokens || 4096,
        stream: true,
      };
      const sys = messages.find(m => m.role === 'system')?.content;
      if (sys) body.system = sys;
      if (tools) body.tools = tools;
      _put(body, 'temperature', o.temperature);
      _put(body, 'top_p', o.topP);
      _put(body, 'top_k', o.topK);
      _put(body, 'stop_sequences', o.stop);
      if (o.user) body.metadata = { user_id: o.user };
      if (o.anthropicThinking) {
        body.thinking = { type: 'enabled', budget_tokens: o.anthropicThinkingBudgetTokens || 4096 };
      }
      if (o.serviceTier) body.service_tier = o.serviceTier;

      const response = await fetch(`${inst.llmEngine.baseUrl.replace(/\/$/, '')}/messages`, {
        method: 'POST', signal: abortCtrl.signal,
        headers: {
          'Content-Type': 'application/json',
          'x-api-key': inst.llmEngine.apiKey,
          'anthropic-version': '2023-06-01',
          'anthropic-dangerous-direct-browser-access': 'true',
        },
        body: JSON.stringify(body),
      });
      if (!response.ok) {
        const t = await response.text().catch(()=>'');
        throw new Error(`Anthropic API error ${response.status} ${t.slice(0,200)}`);
      }
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
            if (parsed.type === 'content_block_delta') {
              if (parsed.delta?.type === 'text_delta' && parsed.delta?.text) _sendToken(parsed.delta.text);
              else if (parsed.delta?.type === 'thinking_delta' && parsed.delta?.thinking) _sendToken(parsed.delta.thinking);
            }
          } catch (_) {}
        }
      }
    } catch (err) {
      if (err?.name !== 'AbortError') {
        try { inst.dotnetRef.invokeMethodAsync('OnErrorCallback', String(err?.message || err)); } catch (_) {}
      }
    } finally {
      inst._activeAbortCtrl = null;
      _sendComplete();
    }
  } else if (inst.llmEngine.kind === 'google') {
    const abortCtrl = new AbortController();
    inst._activeAbortCtrl = abortCtrl;
    try {
      const o = inst.llmEngine.opts || {};
      const sys = messages.find(m => m.role === 'system')?.content;
      const contents = [];
      for (const m of messages) {
        if (m.role === 'system') continue;
        const role = m.role === 'assistant' ? 'model' : 'user';
        let parts;
        if (typeof m.content === 'string') parts = [{ text: m.content }];
        else if (Array.isArray(m.content)) {
          parts = m.content.map(p => {
            if (p.type === 'text') return { text: p.text };
            if (p.type === 'image_url') {
              const data = (p.image_url?.url || '').split(',')[1] || '';
              const mime = (p.image_url?.url || '').match(/^data:([^;]+);/)?.[1] || 'image/png';
              return { inline_data: { mime_type: mime, data } };
            }
            return { text: '' };
          });
        } else parts = [{ text: String(m.content || '') }];
        contents.push({ role, parts });
      }
      const generationConfig = {};
      _put(generationConfig, 'temperature', o.temperature);
      _put(generationConfig, 'topP', o.topP);
      _put(generationConfig, 'topK', o.topK);
      _put(generationConfig, 'maxOutputTokens', o.maxTokens);
      _put(generationConfig, 'stopSequences', o.stop);
      _put(generationConfig, 'seed', o.seed);
      if (o.responseFormat === 'json_object') generationConfig.responseMimeType = 'application/json';
      if (o.responseFormat === 'json_schema' && o.jsonSchema) {
        generationConfig.responseMimeType = 'application/json';
        try { generationConfig.responseSchema = JSON.parse(o.jsonSchema); } catch {}
      }
      if (o.geminiThinkingBudget !== undefined || o.geminiIncludeThoughts !== undefined) {
        generationConfig.thinkingConfig = {};
        if (o.geminiThinkingBudget !== undefined) generationConfig.thinkingConfig.thinkingBudget = o.geminiThinkingBudget;
        if (o.geminiIncludeThoughts !== undefined) generationConfig.thinkingConfig.includeThoughts = !!o.geminiIncludeThoughts;
      }
      const body = { contents };
      if (sys) body.systemInstruction = { parts: [{ text: sys }] };
      if (Object.keys(generationConfig).length) body.generationConfig = generationConfig;
      if (o.geminiSafetyThreshold) {
        body.safetySettings = ['HARM_CATEGORY_HARASSMENT','HARM_CATEGORY_HATE_SPEECH','HARM_CATEGORY_SEXUALLY_EXPLICIT','HARM_CATEGORY_DANGEROUS_CONTENT']
          .map(c => ({ category: c, threshold: o.geminiSafetyThreshold }));
      }
      if (tools) body.tools = tools;

      const url = `${inst.llmEngine.baseUrl.replace(/\/$/, '')}/models/${encodeURIComponent(inst.llmEngine.model)}:streamGenerateContent?alt=sse&key=${encodeURIComponent(inst.llmEngine.apiKey)}`;
      const response = await fetch(url, {
        method: 'POST', signal: abortCtrl.signal,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!response.ok) {
        const t = await response.text().catch(()=>'');
        throw new Error(`Gemini API error ${response.status} ${t.slice(0,200)}`);
      }
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
            const parts = parsed.candidates?.[0]?.content?.parts || [];
            for (const p of parts) if (p.text) _sendToken(p.text);
          } catch (_) {}
        }
      }
    } catch (err) {
      if (err?.name !== 'AbortError') {
        try { inst.dotnetRef.invokeMethodAsync('OnErrorCallback', String(err?.message || err)); } catch (_) {}
      }
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
      if (err?.name !== 'AbortError') {
        try { inst.dotnetRef.invokeMethodAsync('OnErrorCallback', String(err?.message || err)); } catch (_) {}
      }
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
