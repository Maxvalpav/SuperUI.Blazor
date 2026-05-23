using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SuperUI.Services.Llm;

/// <summary>
/// Server-side helper for applications that want to expose a SuperUI LLM proxy
/// without leaking provider secrets to the browser. It is framework-agnostic: wire it
/// to Minimal API, MVC, Carter, FastEndpoints, etc. in the host app.
/// </summary>
public sealed class SgLlmProxyForwarder
{
    private readonly HttpClient _http;

    public SgLlmProxyForwarder(HttpClient http) => _http = http;

    public async Task<HttpResponseMessage> ForwardRawAsync(
        SgLlmProvider provider,
        string providerUrl,
        string rawJson,
        string? apiKey,
        SgLlmConfig? config = null,
        CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, providerUrl)
        {
            Content = new StringContent(rawJson, Encoding.UTF8, "application/json")
        };
        await ApplyAuthAsync(req, provider, apiKey, config, ct);
        return await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public async Task<SgLlmProxyResponse> CompleteAsync(
        SgLlmProxyRequest request,
        string? apiKey,
        CancellationToken ct = default)
    {
        var config = request.ClientConfig ?? new SgLlmConfig();
        config.Provider = request.Provider;
        config.ModelId = request.ModelId ?? config.ModelId;
        config.BaseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? config.BaseUrl : request.BaseUrl;
        config.SystemPrompt = request.SystemPrompt ?? config.SystemPrompt;

        var url = BuildChatUrl(config);
        var messages = BuildMessages(request, config);
        var body = new Dictionary<string, object?>
        {
            ["model"] = config.ModelId,
            ["messages"] = messages,
            ["stream"] = false
        };
        if (config.UseAdvanced)
        {
            body["temperature"] = config.Temperature;
            body["top_p"] = config.TopP;
            if (config.MaxTokens.HasValue) body["max_tokens"] = config.MaxTokens.Value;
            if (!string.IsNullOrWhiteSpace(config.ResponseFormat))
                body["response_format"] = config.ResponseFormat == "json_object"
                    ? new { type = "json_object" }
                    : new { type = "text" };
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        await ApplyAuthAsync(req, config.Provider, apiKey, config, ct);

        var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            return new SgLlmProxyResponse { Error = $"HTTP {(int)resp.StatusCode}: {Truncate(raw, 500)}", RawJson = raw };
        }

        return ParseOpenAiCompatibleResponse(raw);
    }

    public async Task<string?> ResolveGigaAccessTokenAsync(
        string? authorizationKey,
        string? scope = null,
        string? oauthUrl = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationKey)) return null;
        using var req = new HttpRequestMessage(HttpMethod.Post,
            string.IsNullOrWhiteSpace(oauthUrl) ? "https://ngw.devices.sberbank.ru:9443/api/v2/oauth" : oauthUrl);
        var basic = authorizationKey.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)
            ? authorizationKey[6..].Trim()
            : authorizationKey.Trim();
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        req.Headers.TryAddWithoutValidation("RqUID", Guid.NewGuid().ToString());
        req.Headers.TryAddWithoutValidation("Accept", "application/json");
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["scope"] = string.IsNullOrWhiteSpace(scope) ? "GIGACHAT_API_PERS" : scope!
        });

        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.TryGetProperty("access_token", out var token) ? token.GetString() : null;
    }

    private async Task ApplyAuthAsync(HttpRequestMessage req, SgLlmProvider provider, string? apiKey, SgLlmConfig? config, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        if (provider == SgLlmProvider.Anthropic)
        {
            req.Headers.TryAddWithoutValidation("x-api-key", apiKey);
            req.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            return;
        }

        if (provider == SgLlmProvider.GigaGpt && string.Equals(config?.GigaAuthMode, "OAuth", StringComparison.OrdinalIgnoreCase))
        {
            var token = await ResolveGigaAccessTokenAsync(apiKey, config?.GigaScope, config?.GigaOAuthUrl, ct);
            if (!string.IsNullOrWhiteSpace(token)) apiKey = token;
        }

        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        if (provider == SgLlmProvider.OpenRouter)
        {
            req.Headers.TryAddWithoutValidation("HTTP-Referer", "https://superui.local");
            req.Headers.TryAddWithoutValidation("X-Title", "SuperUI Proxy");
        }
    }

    private static string BuildChatUrl(SgLlmConfig config)
    {
        var baseUrl = SgLlmProviderRegistry.NormalizeBaseUrl(config.Provider, config.BaseUrl);
        if (config.Provider == SgLlmProvider.Anthropic) return baseUrl.TrimEnd('/') + "/messages";
        if (config.Provider == SgLlmProvider.Ollama) return baseUrl.TrimEnd('/') + "/api/chat";
        if (config.Provider == SgLlmProvider.OpenAiCompatible && config.UseResponsesApi == true) return baseUrl.TrimEnd('/') + "/responses";
        return baseUrl.TrimEnd('/') + "/chat/completions";
    }

    private static List<object> BuildMessages(SgLlmProxyRequest request, SgLlmConfig config)
    {
        var list = new List<object>();
        if (!string.IsNullOrWhiteSpace(config.SystemPrompt)) list.Add(new { role = "system", content = config.SystemPrompt });
        foreach (var m in request.Messages)
            list.Add(new { role = m.Role, content = m.Content });
        return list;
    }

    private static SgLlmProxyResponse ParseOpenAiCompatibleResponse(string raw)
    {
        var result = new SgLlmProxyResponse { RawJson = raw };
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
            {
                var msg = choices[0].GetProperty("message");
                if (msg.TryGetProperty("content", out var content)) result.Content = content.GetString() ?? string.Empty;
            }
            else if (root.TryGetProperty("output_text", out var outputText))
            {
                result.Content = outputText.GetString() ?? string.Empty;
            }
            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.ValueKind == JsonValueKind.Number) result.PromptTokens = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ct) && ct.ValueKind == JsonValueKind.Number) result.CompletionTokens = ct.GetInt32();
                if (usage.TryGetProperty("input_tokens", out var it) && it.ValueKind == JsonValueKind.Number) result.PromptTokens = it.GetInt32();
                if (usage.TryGetProperty("output_tokens", out var ot) && ot.ValueKind == JsonValueKind.Number) result.CompletionTokens = ot.GetInt32();
            }
        }
        catch (Exception ex) { result.Error = ex.Message; }
        return result;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
