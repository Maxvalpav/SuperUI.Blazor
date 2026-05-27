# SuperUI LLM backend proxy

Use backend proxy mode when API keys must not be exposed to the browser or when a provider blocks browser CORS.

## UI settings

In `SgLlmSettings` enable:

- `Backend proxy`
- `Proxy URL`, for example `/api/llm/chat`

When proxy mode is enabled, `sg-llm.js` sends chat traffic to `Proxy URL` and does **not** attach the provider API key as `Authorization`.

## Current browser bridge behavior

The current JS bridge keeps the provider payload shape for compatibility:

- OpenAI/OpenRouter/HuggingFace/LM Studio/GigaChat compatible providers: request body is chat-completions or responses-style JSON.
- Streaming response should be Server-Sent Events with `data: ...` chunks and optional `[DONE]`.

That means the simplest backend proxy can accept the incoming body as raw JSON, inject the server-side secret, forward it to the real provider, and stream the provider response back unchanged.

## Minimal endpoint sketch

```csharp
app.MapPost("/api/llm/chat", async (HttpRequest request, IConfiguration cfg, HttpClient http) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync();

    // Resolve the real provider endpoint and secret from route, tenant or headers.
    var providerUrl = cfg["LLM:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/chat/completions";
    var apiKey = cfg["LLM:OpenAI:ApiKey"];

    using var upstream = new HttpRequestMessage(HttpMethod.Post, providerUrl)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    upstream.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

    var response = await http.SendAsync(upstream, HttpCompletionOption.ResponseHeadersRead);
    return Results.Stream(await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentType?.ToString() ?? "text/event-stream");
});
```

## Typed proxy contract and helper

`SgLlmProxyContracts.cs` contains DTOs for applications that prefer a typed proxy protocol:

- `SgLlmProxyRequest`
- `SgLlmProxyResponse`

`SgLlmProxyForwarder` is a framework-agnostic server helper registered by `AddSuperUI()`.
You can inject it into a server endpoint:

```csharp
app.MapPost("/api/superui/llm/complete", async (
    SgLlmProxyRequest req,
    SgLlmProxyForwarder forwarder,
    IConfiguration cfg,
    CancellationToken ct) =>
{
    var apiKey = req.Provider switch
    {
        SgLlmProvider.OpenRouter => cfg["LLM:OpenRouter:ApiKey"],
        SgLlmProvider.OpenAiCompatible => cfg["LLM:OpenAI:ApiKey"],
        SgLlmProvider.Anthropic => cfg["LLM:Anthropic:ApiKey"],
        SgLlmProvider.HuggingFace => cfg["LLM:HuggingFace:ApiKey"],
        SgLlmProvider.GigaGpt => cfg["LLM:GigaChat:AuthorizationKey"],
        _ => null
    };

    return await forwarder.CompleteAsync(req, apiKey, ct);
});
```

If you use the browser bridge without adaptation, prefer the raw streaming endpoint shown above. If you use the typed contract, expose a separate endpoint or adapt the JS bridge to post `SgLlmProxyRequest`.

## Recommended server responsibilities

- Store provider secrets server-side.
- Normalize provider errors to clear messages.
- Enforce tenant/user token limits.
- Log usage with prompt/completion tokens.
- Cache `/models` responses.
- Implement provider fallback routing.
- Exchange GigaChat authorization keys for access tokens server-side.

---

## Provider catalog

Built-in `SgLlmProviderRegistry` presets. Endpoints are defaults — every preset
allows overriding the `BaseUrl` from the UI or `SgLlmConfig`. Auth styles
correspond to `SgLlmAuthStyle`; API styles correspond to `SgLlmApiStyle`.

### Frontier

| Provider | Endpoint | API style | Auth |
|---|---|---|---|
| OpenAI | `https://api.openai.com/v1` | openai-chat | bearer |
| Anthropic | `https://api.anthropic.com/v1` | anthropic-messages | x-api-key |
| Google Gemini | `https://generativelanguage.googleapis.com/v1beta` | google-gemini | bearer |
| xAI Grok | `https://api.x.ai/v1` | openai-chat | bearer |

### Open routing

| Provider | Endpoint | API style | Auth |
|---|---|---|---|
| OpenRouter | `https://openrouter.ai/api/v1` | openai-chat | bearer |
| Together AI | `https://api.together.xyz/v1` | openai-chat | bearer |
| Fireworks AI | `https://api.fireworks.ai/inference/v1` | openai-chat | bearer |
| Hugging Face | `https://router.huggingface.co/v1` | openai-chat | bearer |
| Replicate | `https://api.replicate.com/v1` | openai-chat | bearer |
| AI/ML API | `https://api.aimlapi.com/v1` | openai-chat | bearer |
| Novita AI | `https://api.novita.ai/v3/openai` | openai-chat | bearer |

### Fast inference

| Provider | Endpoint | API style | Auth |
|---|---|---|---|
| Groq | `https://api.groq.com/openai/v1` | openai-chat | bearer |
| Cerebras | `https://api.cerebras.ai/v1` | openai-chat | bearer |
| SambaNova | `https://api.sambanova.ai/v1` | openai-chat | bearer |
| DeepSeek | `https://api.deepseek.com/v1` | openai-chat | bearer |
| Lepton AI | `https://api.lepton.ai/api/v1` | openai-chat | bearer |
| DeepInfra | `https://api.deepinfra.com/v1/openai` | openai-chat | bearer |

### Local (no key required)

| Provider | Default URL | API style | CORS note |
|---|---|---|---|
| Ollama | `http://localhost:11434` | ollama-native | `OLLAMA_ORIGINS` env var |
| LM Studio | `http://localhost:1234/v1` | openai-chat | Enable CORS in Local Server settings |
| vLLM | `http://localhost:8000/v1` | openai-chat | Run with `--allow-cors` |
| llama.cpp | `http://localhost:8080/v1` | openai-chat | Run with `--api-cors-allow *` |
| Jan | `http://localhost:1337/v1` | openai-chat | Enable API in Settings → Advanced |
| GPT4All | `http://localhost:4891/v1` | openai-chat | Enable API server in settings |
| KoboldCpp | `http://localhost:5001/v1` | openai-chat | Requires `--openai-compatibility` |
| Oobabooga TGW | `http://localhost:5000/v1` | openai-chat | Enable OpenAI extension |
| TabbyAPI | `http://localhost:5000/v1` | openai-chat | exl2 inference, OpenAI-compatible |
| llamafile | `http://localhost:8080/v1` | openai-chat | CORS on by default |
| WebLLM | (in-browser) | openai-chat | Runs in the page via WebGPU |

### Free / community

| Provider | Endpoint | API style | Auth |
|---|---|---|---|
| Cloudflare Workers AI | `https://api.cloudflare.com/client/v4/accounts/{account_id}/ai` | cloudflare-workers-ai | bearer |
| GitHub Models | `https://models.inference.ai.azure.com` | openai-chat | bearer (PAT) |
| Pollinations | `https://text.pollinations.ai/openai` | openai-chat | none |
| glhf.chat | `https://glhf.chat/api/openai/v1` | openai-chat | bearer |
| Targon | `https://api.targon.com/v1` | openai-chat | none |
| Chutes | `https://chutes-api.chutes.ai/v1` | openai-chat | bearer |

### Russian

| Provider | Endpoint | API style | Auth |
|---|---|---|---|
| GigaGPT / GigaChat | `https://gigachat.devices.sberbank.ru/api/v1` | gigachat | oauth |
| YandexGPT | `https://llm.api.cloud.yandex.net/foundationModels/v1` | openai-chat | bearer |

### Specialty

| Provider | Endpoint | API style | Auth |
|---|---|---|---|
| Cohere | `https://api.cohere.com/v2` | cohere-v2 | bearer |
| Mistral AI | `https://api.mistral.ai/v1` | openai-chat | bearer |
| Perplexity | `https://api.perplexity.ai` | openai-chat | bearer |
| Voyage AI | `https://api.voyageai.com/v1` | openai-chat | bearer |
| Jina AI | `https://api.jina.ai/v1` | openai-chat | bearer |
| Nomic Atlas | `https://api-atlas.nomic.ai/v1` | openai-chat | bearer |
| AssemblyAI | `https://api.assemblyai.com/v2` | openai-chat | bearer |
| Deepgram | `https://api.deepgram.com/v1` | openai-chat | bearer |
| ElevenLabs | `https://api.elevenlabs.io/v1` | openai-chat | x-api-key |
| Custom OpenAI-compatible | _(user-supplied)_ | openai-chat | bearer |

### Azure

| Provider | Endpoint | API style | Auth |
|---|---|---|---|
| Azure OpenAI | `https://{resource}.openai.azure.com` | openai-chat | azure-api-key |
