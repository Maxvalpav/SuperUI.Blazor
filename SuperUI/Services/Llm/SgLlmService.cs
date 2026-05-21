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

        var overrides = new
        {
            apiKey = config.ApiKey,
            baseUrl = config.BaseUrl,
            temperature = config.Temperature,
            topP = config.TopP,
            maxTokens = config.MaxTokens,
            presencePenalty = config.PresencePenalty,
            frequencyPenalty = config.FrequencyPenalty
        };

        await _module.InvokeVoidAsync("loadLlm", _instanceId, config.Provider.ToString(), config.ModelId, overrides);
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
                Name = m.Id, // OpenAI models usually only have ID as name
                Description = $"Owned by: {m.OwnedBy}"
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SgLlmService] OpenAI Models Error: {ex.Message}");
            return new();
        }
    }

    private class OpenAiModelsResponse
    {
        public List<OpenAiModel>? Data { get; set; }
    }

    private class OpenAiModel
    {
        public string Id { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; } = string.Empty;
    }

    public async Task<List<SgLlmModelInfo>> GetAnthropicModelsAsync(string? apiKey = null)
    {
        // Anthropic doesn't have a public models list endpoint like OpenAI
        // Returning current state-of-the-art models as fallback, but ideally from a managed list
        return new List<SgLlmModelInfo>
        {
            new() { Id = "claude-3-5-sonnet-20240620", Name = "Claude 3.5 Sonnet", Description = "Most intelligent model" },
            new() { Id = "claude-3-opus-20240229", Name = "Claude 3 Opus", Description = "Powerful for complex tasks" },
            new() { Id = "claude-3-sonnet-20240229", Name = "Claude 3 Sonnet", Description = "Balanced speed and intelligence" },
            new() { Id = "claude-3-haiku-20240307", Name = "Claude 3 Haiku", Description = "Fastest and most compact" }
        };
    }

    public async Task<List<SgOllamaModel>> GetOllamaModelsAsync(string? baseUrl = null)
    {
        var url = (baseUrl?.TrimEnd('/') ?? "http://localhost:11434") + "/api/tags";
        try
        {
            var response = await _http.GetFromJsonAsync<SgOllamaListResponse>(url);
            
            // Если Ollama возвращает детализированный список, преобразуем его
            if (response?.Models != null)
            {
                foreach (var model in response.Models)
                {
                    // Можно добавить доп. обработку если нужно
                }
                return response.Models;
            }
            return new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SgLlmService] Ollama API Error: {ex.Message}");
            return new();
        }
    }

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

            request.Content = JsonContent.Create(new { model = modelId ?? "dall-e-3", prompt, size, n = 1 });
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

    private class OpenAiImageResponse { public List<OpenAiImageData>? Data { get; set; } }
    private class OpenAiImageData { public string? Url { get; set; } }
    private class OpenAiAudioResponse { public string? Text { get; set; } }
    private class OpenAiFileResponse { public string? Id { get; set; } }

    private class OpenAiEmbeddingsResponse
    {
        public List<OpenAiEmbeddingData>? Data { get; set; }
    }

    private class OpenAiEmbeddingData
    {
        public float[]? Embedding { get; set; }
    }

    private class OpenRouterModelsResponse
    {
        public List<OpenRouterModel>? Data { get; set; }
    }

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
