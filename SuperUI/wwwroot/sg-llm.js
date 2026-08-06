// sg-llm.js — SuperUI LLM JS Bridge (ESM)
// Supports: WebLLM, OpenAI-compatible (OpenAI/OpenRouter/LM Studio/HuggingFace/GigaGPT and legacy presets),
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
  openaicompatible:{ url: 'https://api.openai.com/v1',               model: 'gpt-5.5' },
  openrouter:    { url: 'https://openrouter.ai/api/v1',              model: 'openai/gpt-5.5' },
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
  huggingface:   { url: 'https://router.huggingface.co/v1',          model: 'deepseek-ai/DeepSeek-V4-Pro' },
  lmstudio:      { url: 'http://localhost:1234/v1',                  model: 'local-model' },
  gigagpt:       { url: 'https://gigachat.devices.sberbank.ru/api/v1',model: 'GigaChat-2-Max' },
};

function _isOpenAiKind(p) {
  // Keys must be `SgLlmProvider.<Member>.ToString().toLowerCase()` — the C# side
  // sends the enum name verbatim. A typo here silently drops the provider into
  // the "Unknown provider" branch below, which resets baseUrl to api.openai.com.
  return ['openai','openaicompatible','openaicompatiblecustom','openrouter','opencode','mistral','groq','deepseek',
          'xai','cohere','perplexity','togetherai','fireworks','cerebras','huggingface','lmstudio','gigagpt','azureopenai',
          'llamacpp','yandexgpt','cloudflareworkersai','githubmodels','sambanova','pollinations','glhfchat','targon',
          'replicate','novita','aimlapi','lepton','deepinfra','vllm','jan','gpt4all','koboldcpp'].includes(p);
}

async function _resolveGigaAccessToken(opts) {
  const key = opts?.apiKey || '';
  if (!key || opts?.useBackendProxy || (opts?.gigaAuthMode || 'Bearer').toLowerCase() !== 'oauth') return key;
  const oauthUrl = opts?.gigaOAuthUrl || 'https://ngw.devices.sberbank.ru:9443/api/v2/oauth';
  const scope = opts?.gigaScope || 'GIGACHAT_API_PERS';
  const basic = key.toLowerCase().startsWith('basic ') ? key.slice(6).trim() : key.trim();
  try {
    const resp = await fetch(oauthUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'Accept': 'application/json',
        'RqUID': crypto?.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random()}`,
        'Authorization': `Basic ${basic}`,
      },
      body: new URLSearchParams({ scope }).toString(),
    });
    if (!resp.ok) throw new Error(`GigaChat OAuth error ${resp.status}`);
    const data = await resp.json();
    return data?.access_token || key;
  } catch (err) {
    console.warn('[sg-llm] GigaChat OAuth failed; using provided key as bearer token:', err);
    return key;
  }
}

export async function loadLlm(instanceId, provider, modelId, opts) {
  const inst = _instances.get(instanceId);
  if (!inst) throw new Error(`Instance ${instanceId} not found`);
  opts = opts || {};
  const providerLower = (provider || '').toLowerCase();
  console.log('[sg-llm] loadLlm called:', { instanceId, provider, providerLower, modelId });

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
    let baseUrl = (opts?.useBackendProxy && opts?.proxyUrl) ? opts.proxyUrl : (opts?.baseUrl || defaults.url);
    if (providerLower === 'azureopenai' && opts.azureDeployment && opts.azureApiVersion) {
      // Azure uses deployment-scoped URL; we'll build the chat URL at request time.
    }
    const resolvedApiKey = providerLower === 'gigagpt'
      ? await _resolveGigaAccessToken(opts)
      : (opts?.apiKey || inst.options.apiKey || '');
    inst.llmEngine = {
      kind: 'openai',
      sub: providerLower,
      baseUrl,
      apiKey: opts?.useBackendProxy ? '' : resolvedApiKey,
      model: modelId || defaults.model,
      extraHeaders,
      opts,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', { stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true }); } catch (_) {}
  } else if (providerLower === 'anthropic') {
    inst.llmEngine = {
      kind: 'anthropic',
      baseUrl: opts?.baseUrl || 'https://api.anthropic.com/v1',
      apiKey: opts?.useBackendProxy ? '' : (opts?.apiKey || inst.options.apiKey || ''),
      model: modelId || 'claude-opus-4-7',
      opts,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', { stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true }); } catch (_) {}
  } else if (providerLower === 'google') {
    inst.llmEngine = {
      kind: 'google',
      baseUrl: opts?.baseUrl || 'https://generativelanguage.googleapis.com/v1beta',
      apiKey: opts?.useBackendProxy ? '' : (opts?.apiKey || inst.options.apiKey || ''),
      model: modelId || 'gemini-2.5-flash',
      opts,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', { stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true }); } catch (_) {}
  } else if (providerLower === 'ollama') {
    inst.llmEngine = {
      kind: 'ollama',
      baseUrl: opts?.baseUrl || 'http://localhost:11434',
      model: modelId || 'qwen3.6',
      opts,
    };
    try { inst.dotnetRef.invokeMethodAsync('OnLlmProgressCallback', { stage: 'ready', loaded: 1, total: 1, percent: 100, file: null, isComplete: true }); } catch (_) {}
  } else {
    console.warn('[sg-llm] Unknown provider "' + provider + '", defaulting to OpenAI-compatible');
    inst.llmEngine = {
      kind: 'openai',
      sub: providerLower,
      baseUrl: opts?.baseUrl || 'https://api.openai.com/v1',
      apiKey: opts?.useBackendProxy ? '' : (opts?.apiKey || inst.options.apiKey || ''),
      model: modelId || 'gpt-4o-mini',
      extraHeaders: { ...(opts?.extraHeaders || {}) },
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
  _put(body, 'max_tokens', o.requestTokenLimit || o.maxTokens);
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

function _shouldUseResponsesApi(engine) {
  const base = String(engine.baseUrl || '').toLowerCase();
  const model = String(engine.model || '').toLowerCase();
  return engine.sub === 'openaicompatible'
    && (engine.opts?.useResponsesApi === true || (base.includes('api.openai.com') && model.startsWith('gpt-5')));
}

function _buildOpenAiUrl(engine) {
  if (_shouldUseResponsesApi(engine)) {
    return `${engine.baseUrl.replace(/\/$/, '')}/responses`;
  }
  if (engine.sub === 'azureopenai') {
    const o = engine.opts || {};
    const base = engine.baseUrl.replace(/\/$/, '');
    const deployment = o.azureDeployment || engine.model;
    const api = o.azureApiVersion || '2024-10-21';
    return `${base}/openai/deployments/${encodeURIComponent(deployment)}/chat/completions?api-version=${encodeURIComponent(api)}`;
  }
  return `${engine.baseUrl.replace(/\/$/, '')}/chat/completions`;
}

function _contentToResponseText(content) {
  if (typeof content === 'string') return content;
  if (Array.isArray(content)) {
    return content.map(p => {
      if (p.type === 'text') return p.text || '';
      if (p.type === 'image_url') return `[image: ${p.image_url?.url ? 'attached' : 'url missing'}]`;
      return '';
    }).filter(Boolean).join('\n');
  }
  return String(content || '');
}

function _buildResponsesBody(engine, messages, tools, toolChoice) {
  const o = engine.opts || {};
  const sys = messages.find(m => m.role === 'system')?.content;
  const input = messages
    .filter(m => m.role !== 'system')
    .map(m => ({ role: m.role, content: _contentToResponseText(m.content) }));
  const body = { model: engine.model, input, stream: true };
  if (sys) body.instructions = _contentToResponseText(sys);
  _put(body, 'tools', tools);
  _put(body, 'tool_choice', toolChoice);
  _put(body, 'temperature', o.temperature);
  _put(body, 'top_p', o.topP);
  _put(body, 'max_output_tokens', o.requestTokenLimit || o.maxTokens);
  _put(body, 'parallel_tool_calls', o.parallelToolCalls);
  _put(body, 'reasoning', o.reasoningEffort ? { effort: o.reasoningEffort } : undefined);
  if (o.responseFormat === 'json_object') body.text = { format: { type: 'json_object' } };
  if (o.responseFormat === 'json_schema' && o.jsonSchema) {
    try { body.text = { format: { type: 'json_schema', ...JSON.parse(o.jsonSchema) } }; } catch {}
  }
  return body;
}

function _buildOpenAiHeaders(engine) {
  const headers = { 'Content-Type': 'application/json', ...(engine.extraHeaders || {}) };
  if (engine.sub === 'azureopenai') {
    if (engine.apiKey) headers['api-key'] = engine.apiKey;
  } else if (engine.apiKey) {
    headers['Authorization'] = `Bearer ${engine.apiKey}`;
  }
  return headers;
}

async function _fetchWithRetry(url, init, opts) {
  const retryCount = Number(opts?.retryCount || 0);
  const retryDelayMs = Number(opts?.retryDelayMs || 500);
  const timeoutSeconds = Number(opts?.timeoutSeconds || 0);
  let lastError;
  for (let attempt = 0; attempt <= retryCount; attempt++) {
    const ctrl = new AbortController();
    const external = init.signal;
    const onAbort = () => ctrl.abort();
    if (external) external.addEventListener('abort', onAbort, { once: true });
    const timer = timeoutSeconds > 0 ? setTimeout(() => ctrl.abort(), timeoutSeconds * 1000) : null;
    try {
      const response = await fetch(url, { ...init, signal: ctrl.signal });
      if (response.ok || response.status < 500 || attempt === retryCount) return response;
      lastError = new Error(`HTTP ${response.status}`);
    } catch (err) {
      lastError = err;
      if (attempt === retryCount || err?.name === 'AbortError') throw err;
    } finally {
      if (timer) clearTimeout(timer);
      if (external) external.removeEventListener('abort', onAbort);
    }
    if (retryDelayMs > 0) await new Promise(r => setTimeout(r, retryDelayMs));
  }
  throw lastError || new Error('fetch failed');
}

function _todayTokenUsage() {
  try {
    const raw = localStorage.getItem('sui-llm-usage');
    const rows = raw ? JSON.parse(raw) : [];
    const today = new Date().toDateString();
    return rows.filter(r => new Date(r.Timestamp || r.timestamp).toDateString() === today)
      .reduce((s, r) => s + Number(r.TotalTokens || r.totalTokens || ((r.PromptTokens || r.promptTokens || 0) + (r.CompletionTokens || r.completionTokens || 0)) || 0), 0);
  } catch { return 0; }
}

function _checkUsageGuard(engine, messages) {
  const o = engine.opts || {};
  if (o.onlyFreeModels && engine.sub === 'openrouter' && !String(engine.model || '').includes(':free')) {
    return `Cost guard: модель ${engine.model} не помечена как :free.`;
  }
  if (o.dailyTokenLimit && _todayTokenUsage() >= Number(o.dailyTokenLimit)) {
    return `Cost guard: дневной лимит токенов исчерпан (${o.dailyTokenLimit}).`;
  }
  const promptEstimate = Math.ceil(messages.reduce((s, m) => s + (typeof m.content === 'string' ? m.content.length : 1000), 0) / 4);
  if (o.requestTokenLimit && promptEstimate > Number(o.requestTokenLimit) * 4) {
    return `Cost guard: слишком большой запрос (${promptEstimate} input tokens estimate).`;
  }
  return null;
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

  const startedAt = performance.now();
  let fullAnswer = '';
  let toolCalls = [];

  // Batch tokens so each network chunk doesn't bounce through .NET interop —
  // every JSInvokable call is a synchronisation hop that floods the Blazor
  // renderer on long answers. We flush at most once per animation frame
  // (~16 ms), which keeps streaming visibly smooth without freezing the UI.
  let _pendingBuf = '';
  let _flushScheduled = false;
  const _flush = () => {
    _flushScheduled = false;
    if (!_pendingBuf) return;
    const chunk = _pendingBuf;
    _pendingBuf = '';
    try { inst.dotnetRef.invokeMethodAsync('OnStreamTokenCallback', chunk); } catch (_) {}
  };
  const _sendToken = (token) => {
    if (!token) return;
    fullAnswer += token;
    _pendingBuf += token;
    if (!_flushScheduled) {
      _flushScheduled = true;
      const raf = (typeof requestAnimationFrame === 'function') ? requestAnimationFrame : (cb) => setTimeout(cb, 16);
      raf(_flush);
    }
  };

  const _sendComplete = () => {
    // Drain any tokens still waiting in the rAF buffer so the final chunk
    // isn't lost between the last delta and the completion event.
    if (_pendingBuf) {
      const chunk = _pendingBuf;
      _pendingBuf = '';
      _flushScheduled = false;
      try { inst.dotnetRef.invokeMethodAsync('OnStreamTokenCallback', chunk); } catch (_) {}
    }
    inst._directHistory.push(userTurn);
    inst._directHistory.push({ role: 'assistant', content: fullAnswer, tool_calls: toolCalls.length ? toolCalls : undefined });
    if (inst._directHistory.length > 20) inst._directHistory = inst._directHistory.slice(-20);
    const answer = {
      question: message, answer: fullAnswer, tool_calls: toolCalls, sources: [],
      promptTokens: Math.ceil(messages.reduce((s, m) => s + (typeof m.content === 'string' ? m.content.length : 1000), 0) / 4),
      completionTokens: Math.ceil(fullAnswer.length / 4), durationMs: Math.round(performance.now() - startedAt),
    };
    try { inst.dotnetRef.invokeMethodAsync('OnStreamCompleteCallback', answer); } catch (_) {}
  };

  const guardError = _checkUsageGuard(inst.llmEngine, messages);
  if (guardError) {
    try { inst.dotnetRef.invokeMethodAsync('OnErrorCallback', guardError); } catch (_) {}
    return;
  }

  if (inst.llmEngine.kind === 'openai') {
    const abortCtrl = new AbortController();
    inst._activeAbortCtrl = abortCtrl;
    try {
      const useResponses = _shouldUseResponsesApi(inst.llmEngine);
      const body = useResponses
        ? _buildResponsesBody(inst.llmEngine, messages, tools, toolChoice)
        : _buildOpenAiBody(inst.llmEngine, messages, tools, toolChoice);
      const url = _buildOpenAiUrl(inst.llmEngine);
      const headers = _buildOpenAiHeaders(inst.llmEngine);
      // Diagnostic log — shows in DevTools console so users can confirm the key,
      // base URL and model actually reaching the provider.
      console.info('[sg-llm] →', { url, model: inst.llmEngine.model, sub: inst.llmEngine.sub });
      const response = await _fetchWithRetry(url, { method: 'POST', signal: abortCtrl.signal, headers, body: JSON.stringify(body) }, inst.llmEngine.opts || {});
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
            if (parsed.type === 'response.output_text.delta' && parsed.delta) _sendToken(parsed.delta);
            if (parsed.type === 'response.reasoning_summary_text.delta' && parsed.delta) _sendToken(parsed.delta);
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
      _put(options, 'num_predict', o.requestTokenLimit || o.maxTokens);
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
        max_tokens: o.requestTokenLimit || o.maxTokens || 4096,
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

      const url = `${inst.llmEngine.baseUrl.replace(/\/$/, '')}/models/${encodeURIComponent(inst.llmEngine.model)}:streamGenerateContent?alt=sse`;
      const response = await fetch(url, {
        method: 'POST', signal: abortCtrl.signal,
        headers: {
          'Content-Type': 'application/json',
          'x-goog-api-key': inst.llmEngine.apiKey,
        },
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

// Cheap, non-smooth scroll-to-bottom for streaming chat. The previous
// implementation used document.querySelector + smooth behavior + eval on every
// token, which thrashed the layout engine and froze the UI on long answers.
export function scrollChatToBottom(selector) {
  try {
    const el = document.querySelector(selector || '.sg-chat-messages');
    if (!el) return;
    el.scrollTop = el.scrollHeight;
  } catch (_) {}
}

function _escapeHtml(v) {
  return String(v)
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

export async function renderMarkdown(text, markedSrc) {
  const src = markedSrc || 'https://cdn.jsdelivr.net/npm/marked@12.0.0/marked.min.js';
  await _loadScript(src);
  const marked = window.marked;
  if (!marked) return _escapeHtml(text);
  marked.setOptions({ gfm: true, breaks: true });
  const html = marked.parse(text || '');
  const div = document.createElement('div');
  div.innerHTML = html;
  const scripts = div.querySelectorAll('script, iframe, object, embed');
  for (const el of scripts) el.remove();
  const all = div.querySelectorAll('*');
  for (const el of all) {
    const attrs = el.attributes;
    for (let i = attrs.length - 1; i >= 0; i--) {
      const name = attrs[i].name;
      if (name.startsWith('on') || name === 'href' && attrs[i].value.trim().toLowerCase().startsWith('javascript:')) {
        el.removeAttribute(name);
      }
    }
  }
  return div.innerHTML;
}
