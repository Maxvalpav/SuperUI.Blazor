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
