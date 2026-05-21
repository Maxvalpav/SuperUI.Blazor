using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SuperUI.Services.Llm;

/// <summary>
/// Service for interacting with LLMs (chat, images, etc.).
/// Extracted from RAG components for general use.
/// </summary>
public class SgLlmService : ILlmService, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly HttpClient _http;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SgLlmService>? _selfRef;
    private string? _instanceId;
    private bool _isDisposed;

    public bool IsInitialized => _module != null;
    public SgLlmConfig? CurrentConfig { get; private set; }

    public async Task SaveGlobalConfigAsync(SgLlmConfig config)
    {
        CurrentConfig = config;
        var json = System.Text.Json.JsonSerializer.Serialize(config);
        await _js.InvokeVoidAsync("localStorage.setItem", "sui-global-llm-config", json);
    }

    public async Task<SgLlmConfig?> GetGlobalConfigAsync()
    {
        if (CurrentConfig != null) return CurrentConfig;

        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", "sui-global-llm-config");
            if (!string.IsNullOrEmpty(json))
            {
                CurrentConfig = System.Text.Json.JsonSerializer.Deserialize<SgLlmConfig>(json);
            }
        }
        catch { }

        return CurrentConfig;
    }

    public event Action<string>? OnTokenReceived;
    public event Action<string>? OnChatComplete;
    public event Action<string>? OnError;
    public event Action<double>? OnLoadingProgress;

    public SgLlmService(IJSRuntime js, HttpClient http)
    {
        _js = js;
        _http = http;
    }

    public async Task InitializeAsync(SgLlmConfig config)
    {
        if (_module is null)
        {
            _instanceId = $"sg-llm-{Guid.NewGuid():N}";
            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/sg-llm.js");
            _selfRef = DotNetObjectReference.Create(this);

            await _module.InvokeVoidAsync("init", _selfRef, _instanceId, new { });
        }

        CurrentConfig = config;

        // Build overrides. Base is always sent; advanced fields are only included when
        // UseAdvanced is true. JS side ignores undefined/null params.
        object overrides = BuildOverrides(config);

        await _module.InvokeVoidAsync("loadLlm", _instanceId, config.Provider.ToString(), config.ModelId, overrides);
    }

    private static object BuildOverrides(SgLlmConfig c)
    {
        var dict = new Dictionary<string, object?>
        {
            ["apiKey"] = c.ApiKey,
            ["baseUrl"] = c.BaseUrl,
            ["extraHeaders"] = c.ExtraHeaders,
            ["stream"] = c.Stream,
        };

        if (!c.UseAdvanced)
        {
            return dict;
        }

        // Sampling
        dict["temperature"] = c.Temperature;
        dict["topP"] = c.TopP;
        if (c.MaxTokens.HasValue) dict["maxTokens"] = c.MaxTokens;
        dict["presencePenalty"] = c.PresencePenalty;
        dict["frequencyPenalty"] = c.FrequencyPenalty;
        if (c.Seed.HasValue) dict["seed"] = c.Seed;
        if (c.Stop is { Count: > 0 }) dict["stop"] = c.Stop;
        if (c.TopK.HasValue) dict["topK"] = c.TopK;
        if (c.MinP.HasValue) dict["minP"] = c.MinP;
        if (c.RepetitionPenalty.HasValue) dict["repetitionPenalty"] = c.RepetitionPenalty;

        // Output
        if (!string.IsNullOrEmpty(c.ResponseFormat)) dict["responseFormat"] = c.ResponseFormat;
        if (!string.IsNullOrEmpty(c.JsonSchema)) dict["jsonSchema"] = c.JsonSchema;
        if (c.LogProbs.HasValue) dict["logProbs"] = c.LogProbs;
        if (c.TopLogProbs.HasValue) dict["topLogProbs"] = c.TopLogProbs;
        if (c.ParallelToolCalls.HasValue) dict["parallelToolCalls"] = c.ParallelToolCalls;
        if (c.StreamUsage.HasValue) dict["streamUsage"] = c.StreamUsage;

        // Reasoning / verbosity
        if (!string.IsNullOrEmpty(c.ReasoningEffort)) dict["reasoningEffort"] = c.ReasoningEffort;
        if (!string.IsNullOrEmpty(c.Verbosity)) dict["verbosity"] = c.Verbosity;

        // Anthropic thinking
        if (c.AnthropicThinking == true)
        {
            dict["anthropicThinking"] = true;
            if (c.AnthropicThinkingBudgetTokens.HasValue)
                dict["anthropicThinkingBudgetTokens"] = c.AnthropicThinkingBudgetTokens;
        }

        // Gemini
        if (!string.IsNullOrEmpty(c.GeminiSafetyThreshold)) dict["geminiSafetyThreshold"] = c.GeminiSafetyThreshold;
        if (c.GeminiThinkingBudget.HasValue) dict["geminiThinkingBudget"] = c.GeminiThinkingBudget;
        if (c.GeminiIncludeThoughts.HasValue) dict["geminiIncludeThoughts"] = c.GeminiIncludeThoughts;

        // OpenRouter
        if (c.OrFallbackModels is { Count: > 0 }) dict["orFallbackModels"] = c.OrFallbackModels;
        if (!string.IsNullOrEmpty(c.OrProviderSort)) dict["orProviderSort"] = c.OrProviderSort;
        if (c.OrAllowedProviders is { Count: > 0 }) dict["orAllowedProviders"] = c.OrAllowedProviders;
        if (c.OrIgnoredProviders is { Count: > 0 }) dict["orIgnoredProviders"] = c.OrIgnoredProviders;
        if (c.OrRequireParameters.HasValue) dict["orRequireParameters"] = c.OrRequireParameters;
        if (c.OrAllowDataCollection.HasValue) dict["orAllowDataCollection"] = c.OrAllowDataCollection;
        if (!string.IsNullOrEmpty(c.OrTransforms)) dict["orTransforms"] = c.OrTransforms;

        // Service tier
        if (!string.IsNullOrEmpty(c.ServiceTier)) dict["serviceTier"] = c.ServiceTier;

        // Azure
        if (!string.IsNullOrEmpty(c.AzureDeployment)) dict["azureDeployment"] = c.AzureDeployment;
        if (!string.IsNullOrEmpty(c.AzureApiVersion)) dict["azureApiVersion"] = c.AzureApiVersion;

        if (!string.IsNullOrEmpty(c.UserIdentifier)) dict["user"] = c.UserIdentifier;

        return dict;
    }

    public async Task ChatAsync(string message, SgLlmPromptOptions? options = null)
    {
        if (_module is null || _instanceId is null) throw new InvalidOperationException("LLM Service not initialized");

        options ??= new SgLlmPromptOptions();
        var sysPrompt = options.SystemPrompt ?? CurrentConfig?.SystemPrompt ?? "You are a helpful assistant.";

        await _module.InvokeVoidAsync("chatDirectStream", _instanceId, message, sysPrompt, options.Attachments, "default-stream", options.Tools, options.ToolChoice);
    }

    public async Task<bool> IsReadyAsync()
    {
        return IsInitialized;
    }

    public async Task<SgLlmConnectionTest> TestConnectionAsync(SgLlmConfig config)
    {
        // Pick a cheap GET that the provider exposes for the given config. For the
        // OpenAI-compatible family that's /models. For Anthropic we hit /v1/models.
        // For Google we hit /v1beta/models?key=... For Ollama we hit /api/tags.
        try
        {
            string url;
            HttpRequestMessage req;

            switch (config.Provider)
            {
                case SgLlmProvider.Google:
                    if (string.IsNullOrEmpty(config.ApiKey))
                        return new(false, 0, "API key is empty");
                    url = $"{(config.BaseUrl?.TrimEnd('/') ?? "https://generativelanguage.googleapis.com/v1beta")}/models?key={config.ApiKey}";
                    req = new HttpRequestMessage(HttpMethod.Get, url);
                    break;
                case SgLlmProvider.Ollama:
                    url = $"{(config.BaseUrl?.TrimEnd('/') ?? "http://localhost:11434")}/api/tags";
                    req = new HttpRequestMessage(HttpMethod.Get, url);
                    break;
                case SgLlmProvider.Anthropic:
                    url = $"{(config.BaseUrl?.TrimEnd('/') ?? "https://api.anthropic.com/v1")}/models";
                    req = new HttpRequestMessage(HttpMethod.Get, url);
                    if (!string.IsNullOrEmpty(config.ApiKey)) req.Headers.Add("x-api-key", config.ApiKey);
                    req.Headers.Add("anthropic-version", "2023-06-01");
                    break;
                case SgLlmProvider.AzureOpenAi:
                    url = $"{(config.BaseUrl?.TrimEnd('/') ?? "")}/openai/models?api-version={config.AzureApiVersion ?? "2024-10-21"}";
                    req = new HttpRequestMessage(HttpMethod.Get, url);
                    if (!string.IsNullOrEmpty(config.ApiKey)) req.Headers.Add("api-key", config.ApiKey);
                    break;
                default:
                    var baseUrl = (config.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1");
                    // OpenRouter's /models is public (200 with any/no key) — that defeats the
                    // whole purpose of an "is my key valid" probe. Use /auth/key which is
                    // explicitly auth-gated.
                    url = config.Provider == SgLlmProvider.OpenRouter
                        ? $"{baseUrl}/auth/key"
                        : $"{baseUrl}/models";
                    req = new HttpRequestMessage(HttpMethod.Get, url);
                    if (!string.IsNullOrEmpty(config.ApiKey))
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
                    if (config.Provider == SgLlmProvider.OpenRouter)
                    {
                        req.Headers.TryAddWithoutValidation("HTTP-Referer", "https://superui.local");
                        req.Headers.TryAddWithoutValidation("X-Title", "SuperUI");
                    }
                    break;
            }

            var resp = await _http.SendAsync(req);
            var status = (int)resp.StatusCode;
            if (resp.IsSuccessStatusCode)
            {
                // OpenRouter exposes whether the key is a provisioning/management key — those
                // CANNOT do chat completions and will fail with "User not found" at runtime.
                if (config.Provider == SgLlmProvider.OpenRouter)
                {
                    try
                    {
                        var body = await resp.Content.ReadAsStringAsync();
                        if (body.Contains("\"is_provisioning_key\":true") || body.Contains("\"is_management_key\":true"))
                        {
                            return new(false, status,
                                "Key is a Provisioning/Management key — it cannot run chat completions. " +
                                "Create a regular inference key at https://openrouter.ai/settings/keys");
                        }
                    }
                    catch { }
                }
                return new(true, status, $"OK {status}");
            }

            var errBody = "";
            try { errBody = await resp.Content.ReadAsStringAsync(); } catch { }
            if (errBody.Length > 200) errBody = errBody.Substring(0, 200) + "…";
            return new(false, status, $"HTTP {status}: {errBody}");
        }
        catch (Exception ex)
        {
            return new(false, 0, ex.Message);
        }
    }

    public async Task<List<SgLlmModelInfo>> GetOpenRouterModelsAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<OpenRouterModelsResponse>("https://openrouter.ai/api/v1/models");
            if (response?.Data == null) return new();

            var result = new List<SgLlmModelInfo>();
            foreach (var m in response.Data)
            {
                var pricing = m.Pricing;
                bool isFree = pricing != null &&
                              (pricing.Prompt?.ToString() == "0" || pricing.Prompt?.ToString() == "0.0") &&
                              (pricing.Completion?.ToString() == "0" || pricing.Completion?.ToString() == "0.0");

                result.Add(new SgLlmModelInfo
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    IsFree = isFree
                });
            }
            return result;
        }
        catch { return new(); }
    }

    public async Task<List<SgLlmModelInfo>> GetOpenAiModelsAsync(string? baseUrl = null, string? apiKey = null)
    {
        var url = (baseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/models";
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();

            var data = await response.Content.ReadFromJsonAsync<OpenAiModelsResponse>();
            if (data?.Data == null) return new();

            return data.Data.Select(m => new SgLlmModelInfo
            {
                Id = m.Id,
                Name = m.Id,
                Description = $"Owned by: {m.OwnedBy}"
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SgLlmService] OpenAI Models Error: {ex.Message}");
            return new();
        }
    }

    public async Task<List<SgLlmModelInfo>> GetAnthropicModelsAsync(string? apiKey = null)
    {
        // Anthropic does have a public /v1/models endpoint, but it needs auth headers and is often
        // blocked by CORS from browsers. Provide a current built-in list as fallback.
        return new List<SgLlmModelInfo>
        {
            new() { Id = "claude-opus-4-7", Name = "Claude Opus 4.7", Description = "Most capable, frontier reasoning" },
            new() { Id = "claude-sonnet-4-6", Name = "Claude Sonnet 4.6", Description = "Balanced speed/intelligence" },
            new() { Id = "claude-haiku-4-5", Name = "Claude Haiku 4.5", Description = "Fastest and cheapest" },
            new() { Id = "claude-3-7-sonnet-latest", Name = "Claude 3.7 Sonnet", Description = "Previous-gen sonnet" },
            new() { Id = "claude-3-5-sonnet-latest", Name = "Claude 3.5 Sonnet", Description = "Stable older model" },
            new() { Id = "claude-3-5-haiku-latest", Name = "Claude 3.5 Haiku", Description = "Stable older haiku" }
        };
    }

    public async Task<List<SgOllamaModel>> GetOllamaModelsAsync(string? baseUrl = null)
    {
        var url = (baseUrl?.TrimEnd('/') ?? "http://localhost:11434") + "/api/tags";
        try
        {
            var response = await _http.GetFromJsonAsync<SgOllamaListResponse>(url);
            return response?.Models ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SgLlmService] Ollama API Error: {ex.Message}");
            return new();
        }
    }

    public async Task<List<SgLlmModelInfo>> GetGoogleModelsAsync(string? apiKey = null)
    {
        // Gemini API: GET https://generativelanguage.googleapis.com/v1beta/models?key=API_KEY
        if (string.IsNullOrEmpty(apiKey))
        {
            return BuiltinGoogleModels();
        }
        try
        {
            var resp = await _http.GetFromJsonAsync<GeminiModelsResponse>(
                $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}");
            if (resp?.Models == null) return BuiltinGoogleModels();

            return resp.Models
                .Where(m => m.SupportedGenerationMethods?.Contains("generateContent") == true)
                .Select(m => new SgLlmModelInfo
                {
                    Id = m.Name?.Replace("models/", "") ?? "",
                    Name = m.DisplayName ?? m.Name ?? "",
                    Description = m.Description ?? ""
                })
                .Where(m => !string.IsNullOrEmpty(m.Id))
                .ToList();
        }
        catch
        {
            return BuiltinGoogleModels();
        }
    }

    private static List<SgLlmModelInfo> BuiltinGoogleModels() => new()
    {
        new() { Id = "gemini-2.5-pro", Name = "Gemini 2.5 Pro", Description = "Most capable, with thinking" },
        new() { Id = "gemini-2.5-flash", Name = "Gemini 2.5 Flash", Description = "Fast multimodal" },
        new() { Id = "gemini-2.5-flash-lite", Name = "Gemini 2.5 Flash Lite", Description = "Cheapest" },
        new() { Id = "gemini-2.0-flash", Name = "Gemini 2.0 Flash", Description = "Previous flagship flash" },
    };

    public async Task<List<SgLlmModelInfo>> GetMistralModelsAsync(string? apiKey = null)
    {
        return await GetOpenAiCompatibleModelsAsync("https://api.mistral.ai/v1", apiKey, BuiltinMistralModels);
    }

    public async Task<List<SgLlmModelInfo>> GetGroqModelsAsync(string? apiKey = null)
    {
        return await GetOpenAiCompatibleModelsAsync("https://api.groq.com/openai/v1", apiKey, BuiltinGroqModels);
    }

    public async Task<List<SgLlmModelInfo>> GetDeepSeekModelsAsync(string? apiKey = null)
    {
        return await GetOpenAiCompatibleModelsAsync("https://api.deepseek.com", apiKey, BuiltinDeepSeekModels);
    }

    public async Task<List<SgLlmModelInfo>> GetXAiModelsAsync(string? apiKey = null)
    {
        return await GetOpenAiCompatibleModelsAsync("https://api.x.ai/v1", apiKey, BuiltinXAiModels);
    }

    public async Task<List<SgLlmModelInfo>> GetCohereModelsAsync(string? apiKey = null)
    {
        return await GetOpenAiCompatibleModelsAsync("https://api.cohere.ai/compatibility/v1", apiKey, BuiltinCohereModels);
    }

    public async Task<List<SgLlmModelInfo>> GetPerplexityModelsAsync(string? apiKey = null) => BuiltinPerplexityModels();

    public async Task<List<SgLlmModelInfo>> GetTogetherModelsAsync(string? apiKey = null)
    {
        return await GetOpenAiCompatibleModelsAsync("https://api.together.xyz/v1", apiKey, () => new());
    }

    public async Task<List<SgLlmModelInfo>> GetFireworksModelsAsync(string? apiKey = null)
    {
        return await GetOpenAiCompatibleModelsAsync("https://api.fireworks.ai/inference/v1", apiKey, () => new());
    }

    public async Task<List<SgLlmModelInfo>> GetCerebrasModelsAsync(string? apiKey = null)
    {
        return await GetOpenAiCompatibleModelsAsync("https://api.cerebras.ai/v1", apiKey, BuiltinCerebrasModels);
    }

    public async Task<List<SgLlmModelInfo>> GetHuggingFaceModelsAsync(string? apiKey = null) => BuiltinHuggingFaceModels();

    private async Task<List<SgLlmModelInfo>> GetOpenAiCompatibleModelsAsync(string baseUrl, string? apiKey, Func<List<SgLlmModelInfo>> fallback)
    {
        try
        {
            var res = await GetOpenAiModelsAsync(baseUrl, apiKey);
            return res.Count > 0 ? res : fallback();
        }
        catch
        {
            return fallback();
        }
    }

    private static List<SgLlmModelInfo> BuiltinMistralModels() => new()
    {
        new() { Id = "mistral-large-latest", Name = "Mistral Large", Description = "Top-tier reasoning" },
        new() { Id = "mistral-medium-latest", Name = "Mistral Medium", Description = "Balanced" },
        new() { Id = "mistral-small-latest", Name = "Mistral Small", Description = "Fast & cheap" },
        new() { Id = "open-mistral-nemo", Name = "Mistral Nemo", Description = "Open weights" },
        new() { Id = "codestral-latest", Name = "Codestral", Description = "Code generation" },
        new() { Id = "pixtral-large-latest", Name = "Pixtral Large", Description = "Vision-language" }
    };

    private static List<SgLlmModelInfo> BuiltinGroqModels() => new()
    {
        new() { Id = "llama-3.3-70b-versatile", Name = "Llama 3.3 70B", Description = "Versatile" },
        new() { Id = "llama-3.1-8b-instant", Name = "Llama 3.1 8B Instant", Description = "Fastest" },
        new() { Id = "mixtral-8x7b-32768", Name = "Mixtral 8x7B", Description = "MoE 32k ctx" },
        new() { Id = "gemma2-9b-it", Name = "Gemma 2 9B", Description = "Google open model" },
        new() { Id = "deepseek-r1-distill-llama-70b", Name = "DeepSeek R1 Distill 70B", Description = "Reasoning" }
    };

    private static List<SgLlmModelInfo> BuiltinDeepSeekModels() => new()
    {
        new() { Id = "deepseek-chat", Name = "DeepSeek-V3", Description = "Latest chat model" },
        new() { Id = "deepseek-reasoner", Name = "DeepSeek-R1", Description = "Reasoning model" }
    };

    private static List<SgLlmModelInfo> BuiltinXAiModels() => new()
    {
        new() { Id = "grok-4", Name = "Grok 4", Description = "Latest flagship" },
        new() { Id = "grok-4-fast-reasoning", Name = "Grok 4 Fast (Reasoning)", Description = "Fast w/ reasoning" },
        new() { Id = "grok-3", Name = "Grok 3", Description = "Stable" },
        new() { Id = "grok-3-mini", Name = "Grok 3 Mini", Description = "Smaller/cheaper" }
    };

    private static List<SgLlmModelInfo> BuiltinCohereModels() => new()
    {
        new() { Id = "command-a-03-2025", Name = "Command A", Description = "Top Cohere" },
        new() { Id = "command-r-plus-08-2024", Name = "Command R+", Description = "RAG-optimized" },
        new() { Id = "command-r-08-2024", Name = "Command R", Description = "Balanced" }
    };

    private static List<SgLlmModelInfo> BuiltinPerplexityModels() => new()
    {
        new() { Id = "sonar", Name = "Sonar", Description = "Web search" },
        new() { Id = "sonar-pro", Name = "Sonar Pro", Description = "Better web search" },
        new() { Id = "sonar-reasoning", Name = "Sonar Reasoning", Description = "Web + reasoning" },
        new() { Id = "sonar-reasoning-pro", Name = "Sonar Reasoning Pro", Description = "Best web + reasoning" },
        new() { Id = "sonar-deep-research", Name = "Sonar Deep Research", Description = "Long-form research" }
    };

    private static List<SgLlmModelInfo> BuiltinCerebrasModels() => new()
    {
        new() { Id = "llama-3.3-70b", Name = "Llama 3.3 70B", Description = "Fast inference" },
        new() { Id = "llama3.1-8b", Name = "Llama 3.1 8B", Description = "Ultra-fast" },
        new() { Id = "qwen-3-32b", Name = "Qwen 3 32B", Description = "Strong reasoning" }
    };

    private static List<SgLlmModelInfo> BuiltinHuggingFaceModels() => new()
    {
        new() { Id = "meta-llama/Meta-Llama-3.1-70B-Instruct", Name = "Llama 3.1 70B Instruct", Description = "Via HF Router" },
        new() { Id = "Qwen/Qwen2.5-72B-Instruct", Name = "Qwen 2.5 72B", Description = "Via HF Router" }
    };

    public async Task<float[]> GetEmbeddingsAsync(string text, string? modelId = null)
    {
        if (CurrentConfig == null) return Array.Empty<float>();

        var mId = modelId ?? "text-embedding-3-small";
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/embeddings";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);
            }

            request.Content = JsonContent.Create(new { model = mId, input = text });
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Array.Empty<float>();

            var data = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingsResponse>();
            return data?.Data?[0]?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SgLlmService] Embeddings Error: {ex.Message}");
            return Array.Empty<float>();
        }
    }

    public async Task<string> GenerateImageAsync(string prompt, string? modelId = null, string? size = "1024x1024")
    {
        if (CurrentConfig == null) return string.Empty;
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/images/generations";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);

            request.Content = JsonContent.Create(new { model = modelId ?? "gpt-image-1", prompt, size, n = 1 });
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return string.Empty;

            var data = await response.Content.ReadFromJsonAsync<OpenAiImageResponse>();
            return data?.Data?[0]?.Url ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    public async Task<string> SpeechToTextAsync(Stream audioStream, string fileName, string? modelId = null)
    {
        if (CurrentConfig == null) return string.Empty;
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/audio/transcriptions";

        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(audioStream);
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent(modelId ?? "whisper-1"), "model");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return string.Empty;

            var data = await response.Content.ReadFromJsonAsync<OpenAiAudioResponse>();
            return data?.Text ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    public async Task<byte[]> TextToSpeechAsync(string text, string? modelId = null, string? voice = "alloy")
    {
        if (CurrentConfig == null) return Array.Empty<byte>();
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/audio/speech";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);

            request.Content = JsonContent.Create(new { model = modelId ?? "tts-1", input = text, voice });
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return Array.Empty<byte>();

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch { return Array.Empty<byte>(); }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string purpose = "fine-tune")
    {
        if (CurrentConfig == null) return string.Empty;
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/files";

        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent(purpose), "purpose");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return string.Empty;

            var data = await response.Content.ReadFromJsonAsync<OpenAiFileResponse>();
            return data?.Id ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    // ---- Internal response DTOs ----
    private class OpenAiModelsResponse { public List<OpenAiModel>? Data { get; set; } }
    private class OpenAiModel
    {
        public string Id { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; } = string.Empty;
    }

    private class GeminiModelsResponse { public List<GeminiModel>? Models { get; set; } }
    private class GeminiModel
    {
        public string? Name { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("supportedGenerationMethods")]
        public List<string>? SupportedGenerationMethods { get; set; }
    }

    private class OpenAiImageResponse { public List<OpenAiImageData>? Data { get; set; } }
    private class OpenAiImageData { public string? Url { get; set; } }
    private class OpenAiAudioResponse { public string? Text { get; set; } }
    private class OpenAiFileResponse { public string? Id { get; set; } }
    private class OpenAiEmbeddingsResponse { public List<OpenAiEmbeddingData>? Data { get; set; } }
    private class OpenAiEmbeddingData { public float[]? Embedding { get; set; } }

    private class OpenRouterModelsResponse { public List<OpenRouterModel>? Data { get; set; } }
    private class OpenRouterModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OpenRouterPricing? Pricing { get; set; }
    }
    private class OpenRouterPricing
    {
        public object? Prompt { get; set; }
        public object? Completion { get; set; }
    }

    [JSInvokable]
    public void OnStreamTokenCallback(string token) => OnTokenReceived?.Invoke(token);

    [JSInvokable]
    public void OnStreamCompleteCallback(System.Text.Json.JsonElement result)
    {
        string answer = "";
        if (result.ValueKind == System.Text.Json.JsonValueKind.Object && result.TryGetProperty("answer", out var prop))
        {
            answer = prop.GetString() ?? "";
        }
        else
        {
            answer = result.ToString();
        }
        OnChatComplete?.Invoke(answer);
    }

    [JSInvokable]
    public void OnLlmProgressCallback(System.Text.Json.JsonElement progress)
    {
        if (progress.TryGetProperty("percent", out var p))
        {
            OnLoadingProgress?.Invoke(p.GetDouble());
        }
    }

    [JSInvokable]
    public void OnErrorCallback(string message) => OnError?.Invoke(message);

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_module is not null && _instanceId is not null)
        {
            try { await _module.InvokeVoidAsync("dispose", _instanceId); } catch { }
        }

        _selfRef?.Dispose();
        if (_module is not null) await _module.DisposeAsync();
    }
}
