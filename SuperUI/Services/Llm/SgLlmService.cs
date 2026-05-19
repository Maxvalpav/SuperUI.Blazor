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
        };

        await _module.InvokeVoidAsync("loadLlm", _instanceId, config.Provider.ToString(), config.ModelId, overrides);
    }

    public async Task ChatAsync(string message, SgLlmPromptOptions? options = null)
    {
        if (_module is null || _instanceId is null) throw new InvalidOperationException("LLM Service not initialized");

        options ??= new SgLlmPromptOptions();
        var sysPrompt = options.SystemPrompt ?? CurrentConfig?.SystemPrompt ?? "You are a helpful assistant.";

        await _module.InvokeVoidAsync("chatDirectStream", _instanceId, message, sysPrompt, options.Attachments, "default-stream");
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
