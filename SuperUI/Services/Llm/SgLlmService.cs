using Microsoft.JSInterop;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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
    private readonly ILogger<SgLlmService> _logger;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SgLlmService>? _selfRef;
    private string? _instanceId;
    private bool _isDisposed;
    private readonly Dictionary<string, (DateTimeOffset At, List<SgLlmModelInfo> Models)> _modelCache = new();
    private static readonly TimeSpan ModelCacheTtl = TimeSpan.FromMinutes(15);
    private const string GlobalConfigStorageKey = "sui-global-llm-config";
    private const string ProfilesStorageKey = "sui-llm-profiles";
    private const string UsageStorageKey = "sui-llm-usage";

    public bool IsInitialized => _module != null;
    public SgLlmConfig? CurrentConfig { get; private set; }

    public async Task SaveGlobalConfigAsync(SgLlmConfig config)
    {
        CurrentConfig = config;
        var json = System.Text.Json.JsonSerializer.Serialize(SanitizeForStorage(config));
        await _js.InvokeVoidAsync("localStorage.setItem", GlobalConfigStorageKey, json);
        RaiseConfigChanged(config);
    }

    public async Task<SgLlmConfig?> GetGlobalConfigAsync()
    {
        if (CurrentConfig != null) return CurrentConfig;

        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", GlobalConfigStorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                CurrentConfig = MigrateConfig(System.Text.Json.JsonSerializer.Deserialize<SgLlmConfig>(json) ?? new SgLlmConfig());
                await _js.InvokeVoidAsync("localStorage.setItem", GlobalConfigStorageKey, System.Text.Json.JsonSerializer.Serialize(SanitizeForStorage(CurrentConfig)));
                RaiseConfigChanged(CurrentConfig);
            }
        }
        catch { }

        return CurrentConfig;
    }

    public async Task<List<SgLlmProfile>> GetProfilesAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", ProfilesStorageKey);
            if (string.IsNullOrWhiteSpace(json)) return new();
            var profiles = System.Text.Json.JsonSerializer.Deserialize<List<SgLlmProfile>>(json) ?? new();
            var changed = false;
            foreach (var p in profiles)
            {
                var migrated = MigrateConfig(p.Config);
                if (migrated.SchemaVersion != p.Config.SchemaVersion || migrated.ModelId != p.Config.ModelId || migrated.BaseUrl != p.Config.BaseUrl || migrated.Provider != p.Config.Provider)
                    changed = true;
                p.Config = migrated;
            }
            if (changed)
                await _js.InvokeVoidAsync("localStorage.setItem", ProfilesStorageKey, System.Text.Json.JsonSerializer.Serialize(profiles));
            return profiles;
        }
        catch
        {
            return new();
        }
    }

    public async Task SaveProfileAsync(SgLlmProfile profile)
    {
        var profiles = await GetProfilesAsync();
        profile.UpdatedAt = DateTime.UtcNow;
        profile.Config = SanitizeForStorage(profile.Config);

        if (profile.IsDefault)
        {
            foreach (var p in profiles) p.IsDefault = false;
        }

        var idx = profiles.FindIndex(p => p.Id == profile.Id);
        if (idx >= 0) profiles[idx] = profile;
        else profiles.Add(profile);

        await _js.InvokeVoidAsync("localStorage.setItem", ProfilesStorageKey, System.Text.Json.JsonSerializer.Serialize(profiles));
    }

    public async Task DeleteProfileAsync(string profileId)
    {
        var profiles = await GetProfilesAsync();
        profiles.RemoveAll(p => p.Id == profileId);
        await _js.InvokeVoidAsync("localStorage.setItem", ProfilesStorageKey, System.Text.Json.JsonSerializer.Serialize(profiles));
    }

    public async Task<string> ExportProfilesJsonAsync()
    {
        var profiles = await GetProfilesAsync();
        return System.Text.Json.JsonSerializer.Serialize(profiles, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public async Task ImportProfilesJsonAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        var imported = System.Text.Json.JsonSerializer.Deserialize<List<SgLlmProfile>>(json) ?? new();
        foreach (var p in imported)
        {
            if (string.IsNullOrWhiteSpace(p.Id)) p.Id = Guid.NewGuid().ToString("N");
            p.Config = MigrateConfig(p.Config);
            p.UpdatedAt = DateTime.UtcNow;
        }
        await _js.InvokeVoidAsync("localStorage.setItem", ProfilesStorageKey, System.Text.Json.JsonSerializer.Serialize(imported));
    }

    public async Task<List<SgLlmUsageRecord>> GetUsageRecordsAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", UsageStorageKey);
            if (string.IsNullOrWhiteSpace(json)) return new();
            return System.Text.Json.JsonSerializer.Deserialize<List<SgLlmUsageRecord>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task ClearUsageRecordsAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", UsageStorageKey);
    }

    public async Task<List<SgLlmHealthStatus>> CheckProvidersHealthAsync(SgLlmConfig? baseConfig = null)
    {
        var result = new List<SgLlmHealthStatus>();
        foreach (var provider in SgLlmProviderRegistry.AllowedProviders)
        {
            var cfg = baseConfig is null ? new SgLlmConfig() : CopyConfig(baseConfig);
            var activeProvider = baseConfig?.Provider;
            cfg.Provider = provider;
            cfg.BaseUrl = activeProvider == provider && !string.IsNullOrWhiteSpace(baseConfig?.BaseUrl)
                ? SgLlmProviderRegistry.NormalizeBaseUrl(provider, baseConfig!.BaseUrl)
                : SgLlmProviderRegistry.DefaultBaseUrl(provider);
            cfg.ModelId = activeProvider == provider && !string.IsNullOrWhiteSpace(baseConfig?.ModelId)
                ? baseConfig!.ModelId
                : SgLlmProviderRegistry.FallbackModels(provider).FirstOrDefault()?.Id;

            // Reuse the entered key only for the active provider. Local providers do not need a key.
            if (activeProvider != provider && SgLlmProviderRegistry.RequiresKey(provider))
            {
                cfg.ApiKey = string.Empty;
            }

            var diag = await TestFullConnectionAsync(cfg);
            result.Add(new SgLlmHealthStatus
            {
                Provider = provider,
                ProviderLabel = SgLlmProviderRegistry.Label(provider),
                BaseUrl = cfg.BaseUrl,
                Ok = diag.Ok,
                Status = diag.Summary,
                Checks = diag.Checks
            });
        }
        return result;
    }

    private static SgLlmConfig CopyConfig(SgLlmConfig source)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<SgLlmConfig>(json) ?? new();
    }

    private async Task SaveUsageRecordAsync(SgLlmUsageRecord record)
    {
        try
        {
            var records = await GetUsageRecordsAsync();
            records.Insert(0, record);
            if (records.Count > 200) records = records.Take(200).ToList();
            await _js.InvokeVoidAsync("localStorage.setItem", UsageStorageKey, System.Text.Json.JsonSerializer.Serialize(records));
        }
        catch { }
    }

    private static SgLlmConfig MigrateConfig(SgLlmConfig? source)
    {
        var config = source ?? new SgLlmConfig();

        if (!SgLlmProviderRegistry.IsAllowed(config.Provider))
        {
            config.Provider = SgLlmProvider.OpenRouter;
            config.BaseUrl = SgLlmProviderRegistry.DefaultBaseUrl(config.Provider);
            config.ModelId = null;
        }

        if (string.Equals(config.ModelId, "google/gemini-2.0-flash-001:free", StringComparison.OrdinalIgnoreCase)
            || string.Equals(config.ModelId, "openrouter/free", StringComparison.OrdinalIgnoreCase))
        {
            config.ModelId = null;
        }

        config.BaseUrl = SgLlmProviderRegistry.NormalizeBaseUrl(config.Provider, config.BaseUrl);

        config.Routes ??= new Dictionary<string, SgLlmRouteConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var purpose in new[]
        {
            SgLlmTaskPurpose.Chat,
            SgLlmTaskPurpose.Documents,
            SgLlmTaskPurpose.Vision,
            SgLlmTaskPurpose.Structured,
            SgLlmTaskPurpose.Embeddings,
            SgLlmTaskPurpose.Rerank,
            SgLlmTaskPurpose.Images,
            SgLlmTaskPurpose.Moderation,
            SgLlmTaskPurpose.Speech,
            SgLlmTaskPurpose.Video
        })
        {
            if (!config.Routes.ContainsKey(purpose)) config.Routes[purpose] = new SgLlmRouteConfig { Purpose = purpose };
        }

        foreach (var route in config.Routes.Values)
        {
            if (string.IsNullOrWhiteSpace(route.Purpose)) route.Purpose = SgLlmTaskPurpose.Chat;
            if (route.Provider.HasValue && !SgLlmProviderRegistry.IsAllowed(route.Provider.Value)) route.Provider = null;
            if (route.Provider.HasValue && !string.IsNullOrWhiteSpace(route.BaseUrl))
                route.BaseUrl = SgLlmProviderRegistry.NormalizeBaseUrl(route.Provider.Value, route.BaseUrl);
            if (string.Equals(route.ModelId, "google/gemini-2.0-flash-001:free", StringComparison.OrdinalIgnoreCase)
                || string.Equals(route.ModelId, "openrouter/free", StringComparison.OrdinalIgnoreCase))
                route.ModelId = null;
        }

        config.SchemaVersion = SgLlmConfig.CurrentSchemaVersion;
        config.GigaAuthMode ??= "Bearer";
        config.GigaScope ??= "GIGACHAT_API_PERS";
        config.GigaOAuthUrl ??= "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
        config.TimeoutSeconds ??= 120;
        config.RetryCount ??= 0;
        config.RetryDelayMs ??= 500;
        return config;
    }

    private static SgLlmConfig SanitizeForStorage(SgLlmConfig source)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        var clone = MigrateConfig(System.Text.Json.JsonSerializer.Deserialize<SgLlmConfig>(json) ?? new());
        if (!clone.PersistApiKey) clone.ApiKey = string.Empty;
        return clone;
    }

    public event Action<string>? OnTokenReceived;
    public event Action<string>? OnChatComplete;
    public event Action<string>? OnError;
    public event Action<double>? OnLoadingProgress;

    /// <inheritdoc />
    public event Action<SgLlmConfig>? OnConfigChanged;

    /// <inheritdoc />
    public event Action<bool>? OnReadyChanged;

    private bool _lastReady;

    /// <summary>
    /// Snapshot of the most recently broadcast readiness state.
    /// Use this to seed UI without awaiting <see cref="IsReadyAsync"/>.
    /// </summary>
    public bool IsReady => _lastReady;

    private void RaiseConfigChanged(SgLlmConfig? config)
    {
        if (config == null) return;
        OnConfigChanged?.Invoke(config);

        // Recompute readiness inline — synchronous form for the event listeners.
        var ready = !string.IsNullOrEmpty(config.ModelId)
            && (!SgLlmProviderRegistry.RequiresKey(config.Provider) || !string.IsNullOrEmpty(config.ApiKey));
        if (ready != _lastReady)
        {
            _lastReady = ready;
            OnReadyChanged?.Invoke(ready);
        }
    }

    /// <summary>
    /// Constructs the LLM service. The <paramref name="logger"/> parameter is optional —
    /// callers that haven't registered logging will get a silent <see cref="NullLogger{T}"/>.
    /// </summary>
    public SgLlmService(IJSRuntime js, HttpClient http, ILogger<SgLlmService>? logger = null)
    {
        _js = js;
        _http = http;
        _logger = logger ?? NullLogger<SgLlmService>.Instance;
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

        RaiseConfigChanged(config);
    }

    public SgLlmConfig ResolveConfigForTask(string purpose, SgLlmConfig? baseConfig = null)
    {
        var resolved = CopyConfig(baseConfig ?? CurrentConfig ?? new SgLlmConfig());
        if (string.IsNullOrWhiteSpace(purpose) || resolved.Routes is null) return resolved;
        if (!resolved.Routes.TryGetValue(purpose, out var route) || route is null || !route.Enabled) return resolved;

        if (route.Provider.HasValue) resolved.Provider = route.Provider.Value;
        if (!string.IsNullOrWhiteSpace(route.ModelId)) resolved.ModelId = route.ModelId;
        if (!string.IsNullOrWhiteSpace(route.BaseUrl)) resolved.BaseUrl = route.BaseUrl;
        if (!string.IsNullOrWhiteSpace(route.SystemPrompt)) resolved.SystemPrompt = route.SystemPrompt;
        if (!string.IsNullOrWhiteSpace(resolved.BaseUrl))
            resolved.BaseUrl = SgLlmProviderRegistry.NormalizeBaseUrl(resolved.Provider, resolved.BaseUrl);
        return resolved;
    }

    private static bool UsesMicrosoftExtensionsAiOptions(SgLlmProvider provider) =>
        provider is SgLlmProvider.OpenAiCompatible
            or SgLlmProvider.LmStudio
            or SgLlmProvider.HuggingFace
            or SgLlmProvider.GigaGpt;

    /// <summary>
    /// Normalizes OpenAI-compatible provider settings through Microsoft.Extensions.AI.
    /// The JS bridge still performs browser streaming, but the option surface is kept
    /// aligned with <see cref="ChatOptions"/> so the same config can be reused by
    /// server-side IChatClient adapters.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> BuildMicrosoftAiChatOptions(SgLlmConfig c)
    {
        var options = new ChatOptions
        {
            ModelId = c.ModelId,
            Instructions = c.SystemPrompt,
            Temperature = (float)c.Temperature,
            TopP = (float)c.TopP,
            MaxOutputTokens = c.MaxTokens,
            PresencePenalty = (float)c.PresencePenalty,
            FrequencyPenalty = (float)c.FrequencyPenalty,
            Seed = c.Seed,
            TopK = c.TopK
        };

        if (c.Stop is { Count: > 0 }) options.StopSequences = [.. c.Stop];

        // Keep the JS payload serializable; ChatOptions itself may contain non-JSON
        // members such as RawRepresentationFactory.
        return new Dictionary<string, object?>
        {
            ["modelId"] = options.ModelId,
            ["instructions"] = options.Instructions,
            ["temperature"] = options.Temperature,
            ["topP"] = options.TopP,
            ["topK"] = options.TopK,
            ["maxOutputTokens"] = options.MaxOutputTokens,
            ["presencePenalty"] = options.PresencePenalty,
            ["frequencyPenalty"] = options.FrequencyPenalty,
            ["seed"] = options.Seed,
            ["stopSequences"] = options.StopSequences
        };
    }

    private static object BuildOverrides(SgLlmConfig c)
    {
        var dict = new Dictionary<string, object?>
        {
            ["apiKey"] = c.ApiKey,
            ["baseUrl"] = c.BaseUrl,
            ["extraHeaders"] = c.ExtraHeaders,
            ["routes"] = c.Routes,
            ["stream"] = c.Stream,
            ["useBackendProxy"] = c.UseBackendProxy,
            ["proxyUrl"] = c.ProxyUrl,
            ["timeoutSeconds"] = c.TimeoutSeconds,
            ["retryCount"] = c.RetryCount,
            ["retryDelayMs"] = c.RetryDelayMs,
            ["gigaAuthMode"] = c.GigaAuthMode,
            ["gigaScope"] = c.GigaScope,
            ["gigaOAuthUrl"] = c.GigaOAuthUrl,
            ["useResponsesApi"] = c.UseResponsesApi,
            ["onlyFreeModels"] = c.OnlyFreeModels,
            ["dailyTokenLimit"] = c.DailyTokenLimit,
            ["requestTokenLimit"] = c.RequestTokenLimit,
        };

        if (UsesMicrosoftExtensionsAiOptions(c.Provider))
        {
            dict["microsoftExtensionsAI"] = BuildMicrosoftAiChatOptions(c);
        }

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
                case SgLlmProvider.GigaGpt:
                    url = $"{(config.BaseUrl?.TrimEnd('/') ?? DefaultBaseUrl(config.Provider))}/models";
                    req = new HttpRequestMessage(HttpMethod.Get, url);
                    var gigaToken = await ResolveGigaAccessTokenAsync(config.ApiKey, config.GigaAuthMode, config.GigaScope, config.GigaOAuthUrl);
                    if (!string.IsNullOrEmpty(gigaToken))
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", gigaToken);
                    break;
                default:
                    var baseUrl = (config.BaseUrl?.TrimEnd('/') ?? DefaultBaseUrl(config.Provider));
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

    public async Task<SgLlmDiagnosticsResult> TestFullConnectionAsync(SgLlmConfig config)
    {
        var result = new SgLlmDiagnosticsResult();
        void Add(string name, bool ok, string message) => result.Checks.Add(new SgLlmDiagnosticCheck { Name = name, Ok = ok, Message = message });

        if (!SgLlmProviderRegistry.IsAllowed(config.Provider))
        {
            Add("Провайдер", false, "Провайдер не входит в поддерживаемый список SuperUI.");
            return result;
        }

        Add("Провайдер", true, SgLlmProviderRegistry.Label(config.Provider));

        var baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl)
            ? SgLlmProviderRegistry.DefaultBaseUrl(config.Provider)
            : config.BaseUrl!.Trim();
        Add("Base URL", !string.IsNullOrWhiteSpace(baseUrl), baseUrl);

        var requiresKey = SgLlmProviderRegistry.RequiresKey(config.Provider);
        Add("API key", !requiresKey || !string.IsNullOrWhiteSpace(config.ApiKey), !requiresKey ? "Ключ не требуется" : (string.IsNullOrWhiteSpace(config.ApiKey) ? "Ключ не указан" : "Ключ указан"));

        Add("Модель", !string.IsNullOrWhiteSpace(config.ModelId), string.IsNullOrWhiteSpace(config.ModelId) ? "Выберите модель перед применением" : config.ModelId!);

        var probe = await TestConnectionAsync(config);
        Add("Endpoint", probe.Ok, probe.Message);

        try
        {
            List<SgLlmModelInfo> models = config.Provider switch
            {
                SgLlmProvider.OpenRouter => await GetOpenRouterModelsAsync(),
                SgLlmProvider.OpenAiCompatible => await GetOpenAiModelsAsync(config.BaseUrl, config.ApiKey),
                SgLlmProvider.Anthropic => await GetAnthropicModelsAsync(config.ApiKey),
                SgLlmProvider.Ollama => (await GetOllamaModelsAsync(config.BaseUrl)).Select(m => new SgLlmModelInfo { Id = m.Name, Name = m.Name, Provider = config.Provider }).ToList(),
                SgLlmProvider.LmStudio => await GetLmStudioModelsAsync(config.BaseUrl),
                SgLlmProvider.HuggingFace => await GetHuggingFaceModelsAsync(config.ApiKey),
                SgLlmProvider.GigaGpt => await GetGigaGptModelsAsync(config.BaseUrl, config.ApiKey, config.GigaAuthMode, config.GigaScope, config.GigaOAuthUrl),
                _ => new()
            };

            if (string.IsNullOrWhiteSpace(config.ModelId))
            {
                Add("Каталог моделей", models.Count > 0, models.Count > 0 ? $"Найдено моделей: {models.Count}" : "Модели не получены");
            }
            else
            {
                var exists = models.Count == 0 || models.Any(m => string.Equals(m.Id, config.ModelId, StringComparison.OrdinalIgnoreCase));
                Add("Модель в каталоге", exists, exists ? "OK" : "Модель не найдена в /models; можно оставить вручную, если endpoint её поддерживает");
            }
        }
        catch (Exception ex)
        {
            Add("Каталог моделей", false, ex.Message);
        }

        if (config.UseBackendProxy)
        {
            Add("Backend proxy", !string.IsNullOrWhiteSpace(config.ProxyUrl), string.IsNullOrWhiteSpace(config.ProxyUrl) ? "Укажите Proxy URL" : config.ProxyUrl!);
        }

        return result;
    }

    private bool TryGetCachedModels(string key, out List<SgLlmModelInfo> models)
    {
        if (_modelCache.TryGetValue(key, out var entry) && DateTimeOffset.UtcNow - entry.At < ModelCacheTtl)
        {
            models = entry.Models.Select(CloneModel).ToList();
            return true;
        }
        models = new();
        return false;
    }

    private void SetCachedModels(string key, List<SgLlmModelInfo> models)
    {
        _modelCache[key] = (DateTimeOffset.UtcNow, models.Select(CloneModel).ToList());
    }

    private static SgLlmModelInfo CloneModel(SgLlmModelInfo m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Description = m.Description,
        IsFree = m.IsFree,
        IsRecommended = m.IsRecommended,
        Provider = m.Provider,
        ProviderLabel = m.ProviderLabel,
        ContextWindow = m.ContextWindow,
        SupportsVision = m.SupportsVision,
        SupportsTools = m.SupportsTools,
        SupportsJsonSchema = m.SupportsJsonSchema,
        SupportsReasoning = m.SupportsReasoning
    };

    public async Task<List<SgLlmModelInfo>> GetOpenRouterModelsAsync()
    {
        const string cacheKey = "openrouter:/models";
        if (TryGetCachedModels(cacheKey, out var cached)) return cached;
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

                var inputModalities = m.Architecture?.InputModalities ?? new();
                var isVision = inputModalities.Any(x => string.Equals(x, "image", StringComparison.OrdinalIgnoreCase))
                    || (m.Description?.Contains("vision", StringComparison.OrdinalIgnoreCase) ?? false)
                    || (m.Description?.Contains("image", StringComparison.OrdinalIgnoreCase) ?? false);

                result.Add(new SgLlmModelInfo
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    IsFree = isFree,
                    Provider = SgLlmProvider.OpenRouter,
                    ProviderLabel = SgLlmProviderRegistry.Label(SgLlmProvider.OpenRouter),
                    ContextWindow = m.ContextLength,
                    SupportsJsonSchema = true,
                    SupportsTools = true,
                    SupportsVision = isVision,
                    SupportsReasoning = m.Id.Contains("reason", StringComparison.OrdinalIgnoreCase)
                        || m.Id.Contains("gpt-5", StringComparison.OrdinalIgnoreCase)
                        || m.Id.Contains("claude", StringComparison.OrdinalIgnoreCase)
                        || m.Id.Contains("deepseek", StringComparison.OrdinalIgnoreCase)
                });
            }
            SetCachedModels(cacheKey, result);
            return result;
        }
        catch { return SgLlmProviderRegistry.FallbackModels(SgLlmProvider.OpenRouter); }
    }

    public async Task<List<SgLlmModelInfo>> GetOpenAiModelsAsync(string? baseUrl = null, string? apiKey = null)
    {
        try
        {
            var models = await FetchOpenAiModelsAsync(baseUrl, apiKey);
            return models.Count > 0 ? models : BuiltinOpenAiModels();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI /models request failed; returning built-in fallback list.");
            return BuiltinOpenAiModels();
        }
    }

    private async Task<List<SgLlmModelInfo>> FetchOpenAiModelsAsync(string? baseUrl = null, string? apiKey = null)
    {
        var normalizedBaseUrl = baseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1";
        var cacheKey = $"openai-compatible:{normalizedBaseUrl}:{(!string.IsNullOrWhiteSpace(apiKey))}";
        if (TryGetCachedModels(cacheKey, out var cached)) return cached;
        var url = normalizedBaseUrl + "/models";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new();

        var data = await response.Content.ReadFromJsonAsync<OpenAiModelsResponse>();
        if (data?.Data == null) return new();

        var result = data.Data.Select(m =>
        {
            var context = m.Providers?.Select(p => p.ContextLength ?? 0).DefaultIfEmpty(0).Max() ?? 0;
            var supportsTools = m.Providers?.Any(p => p.SupportsTools == true) ?? true;
            var supportsStructured = m.Providers?.Any(p => p.SupportsStructuredOutput == true) ?? true;
            var inputModalities = m.Architecture?.InputModalities ?? new();
            var isVision = inputModalities.Any(x => string.Equals(x, "image", StringComparison.OrdinalIgnoreCase));
            return new SgLlmModelInfo
            {
                Id = m.Id,
                Name = m.Id,
                Description = context > 0 ? $"Owned by: {m.OwnedBy} · ctx {context:n0}" : $"Owned by: {m.OwnedBy}",
                Provider = SgLlmProvider.OpenAiCompatible,
                ProviderLabel = SgLlmProviderRegistry.Label(SgLlmProvider.OpenAiCompatible),
                ContextWindow = context > 0 ? context : null,
                SupportsVision = isVision,
                SupportsTools = supportsTools,
                SupportsJsonSchema = supportsStructured,
                SupportsReasoning = m.Id.Contains("reason", StringComparison.OrdinalIgnoreCase) || m.Id.Contains("gpt-5", StringComparison.OrdinalIgnoreCase) || m.Id.Contains("deepseek", StringComparison.OrdinalIgnoreCase)
            };
        }).ToList();
        SetCachedModels(cacheKey, result);
        return result;
    }

    private static string DefaultBaseUrl(SgLlmProvider provider) =>
        SgLlmProviderRegistry.DefaultBaseUrl(provider) is { Length: > 0 } url ? url : "https://api.openai.com/v1";

    private static List<SgLlmModelInfo> BuiltinOpenAiModels() => SgLlmProviderRegistry.FallbackModels(SgLlmProvider.OpenAiCompatible);

    public async Task<List<SgLlmModelInfo>> GetAnthropicModelsAsync(string? apiKey = null)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
                req.Headers.TryAddWithoutValidation("x-api-key", apiKey);
                req.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
                var resp = await _http.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    var data = await resp.Content.ReadFromJsonAsync<AnthropicModelsResponse>();
                    if (data?.Data is { Count: > 0 })
                    {
                        return data.Data.Select(m => new SgLlmModelInfo
                        {
                            Id = m.Id,
                            Name = string.IsNullOrWhiteSpace(m.DisplayName) ? m.Id : m.DisplayName!,
                            Description = m.CreatedAt is null ? "Anthropic model" : $"Created: {m.CreatedAt}",
                            Provider = SgLlmProvider.Anthropic,
                            ProviderLabel = SgLlmProviderRegistry.Label(SgLlmProvider.Anthropic),
                            ContextWindow = m.Id.Contains("haiku-4-5", StringComparison.OrdinalIgnoreCase) ? 200_000 : 1_000_000,
                            SupportsVision = true,
                            SupportsTools = true,
                            SupportsJsonSchema = true,
                            SupportsReasoning = true,
                            IsRecommended = m.Id.Contains("opus-4-7", StringComparison.OrdinalIgnoreCase) || m.Id.Contains("sonnet-4-6", StringComparison.OrdinalIgnoreCase)
                        }).ToList();
                    }
                }
            }
            catch { }
        }

        return SgLlmProviderRegistry.FallbackModels(SgLlmProvider.Anthropic);
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
            _logger.LogWarning(ex, "Ollama /api/tags request failed at {Url}.", url);
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

    public async Task<List<SgLlmModelInfo>> GetHuggingFaceModelsAsync(string? apiKey = null)
    {
        return await GetOpenAiCompatibleModelsAsync("https://router.huggingface.co/v1", apiKey, BuiltinHuggingFaceModels);
    }

    public async Task<List<SgLlmModelInfo>> GetLmStudioModelsAsync(string? baseUrl = null)
    {
        var url = string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:1234/v1" : baseUrl;
        return await GetOpenAiCompatibleModelsAsync(url, null, BuiltinLmStudioModels);
    }

    public async Task<List<SgLlmModelInfo>> GetGigaGptModelsAsync(string? baseUrl = null, string? apiKey = null, string? authMode = null, string? scope = null, string? oauthUrl = null)
    {
        var url = string.IsNullOrWhiteSpace(baseUrl) ? "https://gigachat.devices.sberbank.ru/api/v1" : baseUrl;
        var token = await ResolveGigaAccessTokenAsync(apiKey, authMode, scope, oauthUrl);
        return await GetOpenAiCompatibleModelsAsync(url, token, BuiltinGigaGptModels);
    }

    private async Task<string?> ResolveGigaAccessTokenAsync(string? apiKey, string? authMode, string? scope, string? oauthUrl)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return apiKey;
        if (!string.Equals(authMode, "OAuth", StringComparison.OrdinalIgnoreCase)) return apiKey;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, string.IsNullOrWhiteSpace(oauthUrl) ? "https://ngw.devices.sberbank.ru:9443/api/v2/oauth" : oauthUrl);
            var authValue = apiKey.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase) ? apiKey[6..].Trim() : apiKey.Trim();
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
            req.Headers.TryAddWithoutValidation("RqUID", Guid.NewGuid().ToString());
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["scope"] = string.IsNullOrWhiteSpace(scope) ? "GIGACHAT_API_PERS" : scope!
            });

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return apiKey;
            var token = await resp.Content.ReadFromJsonAsync<GigaOAuthResponse>();
            return string.IsNullOrWhiteSpace(token?.AccessToken) ? apiKey : token.AccessToken;
        }
        catch
        {
            return apiKey;
        }
    }

    private async Task<List<SgLlmModelInfo>> GetOpenAiCompatibleModelsAsync(string baseUrl, string? apiKey, Func<List<SgLlmModelInfo>> fallback)
    {
        try
        {
            var res = await FetchOpenAiModelsAsync(baseUrl, apiKey);
            if (res.Count > 0)
            {
                var provider = SgLlmProviderRegistry.DetectProvider(baseUrl);
                foreach (var m in res)
                {
                    m.Provider = provider == SgLlmProvider.None ? SgLlmProvider.OpenAiCompatible : provider;
                    m.ProviderLabel = SgLlmProviderRegistry.Label(m.Provider);
                }
                return res;
            }
            return fallback();
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

    private static List<SgLlmModelInfo> BuiltinHuggingFaceModels() => SgLlmProviderRegistry.FallbackModels(SgLlmProvider.HuggingFace);

    private static List<SgLlmModelInfo> BuiltinLmStudioModels() => SgLlmProviderRegistry.FallbackModels(SgLlmProvider.LmStudio);

    private static List<SgLlmModelInfo> BuiltinGigaGptModels() => SgLlmProviderRegistry.FallbackModels(SgLlmProvider.GigaGpt);

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

    public async Task<List<float[]>> GetEmbeddingsBatchAsync(IEnumerable<string> texts, string? modelId = null, int? dimensions = null)
    {
        if (CurrentConfig == null) return new();
        var inputs = texts.ToList();
        if (inputs.Count == 0) return new();

        var mId = modelId ?? "text-embedding-3-small";
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/embeddings";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);

            object body = dimensions.HasValue
                ? new { model = mId, input = inputs, dimensions = dimensions.Value }
                : new { model = mId, input = inputs };
            request.Content = JsonContent.Create(body);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();

            var data = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingsResponse>();
            return data?.Data?.Select(d => d.Embedding ?? Array.Empty<float>()).ToList() ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SgLlmService] Embeddings batch error: {ex.Message}");
            return new();
        }
    }

    public async Task<SgLlmImageResult> GenerateImageRichAsync(SgLlmImageRequest req)
    {
        var result = new SgLlmImageResult();
        if (CurrentConfig == null) { result.Error = "Service not initialized"; return result; }
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/images/generations";

        try
        {
            var body = new Dictionary<string, object?>
            {
                ["model"] = req.Model ?? "gpt-image-1",
                ["prompt"] = req.Prompt,
                ["n"] = Math.Max(1, req.Count),
                ["size"] = req.Size ?? "1024x1024",
                ["response_format"] = req.ResponseFormat ?? "b64_json"
            };
            if (!string.IsNullOrEmpty(req.Quality)) body["quality"] = req.Quality;
            if (!string.IsNullOrEmpty(req.Style)) body["style"] = req.Style;
            if (!string.IsNullOrEmpty(req.Background)) body["background"] = req.Background;

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);

            var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                result.Error = $"HTTP {(int)response.StatusCode}: {Truncate(raw, 300)}";
                return result;
            }

            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("data", out var dataArr))
            {
                foreach (var item in dataArr.EnumerateArray())
                {
                    var img = new SgLlmGeneratedImage();
                    if (item.TryGetProperty("url", out var u)) img.Url = u.GetString();
                    if (item.TryGetProperty("b64_json", out var b)) img.B64Json = b.GetString();
                    if (item.TryGetProperty("revised_prompt", out var rp)) result.RevisedPrompt = rp.GetString();
                    result.Images.Add(img);
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            return result;
        }
    }

    public async Task<SgLlmImageResult> EditImageAsync(byte[] imageBytes, string imageMime, string prompt,
        byte[]? maskBytes = null, string? maskMime = null, string? modelId = null, string? size = "1024x1024")
    {
        var result = new SgLlmImageResult();
        if (CurrentConfig == null) { result.Error = "Service not initialized"; return result; }
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/images/edits";

        try
        {
            using var content = new MultipartFormDataContent();
            var imgContent = new ByteArrayContent(imageBytes);
            imgContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageMime);
            content.Add(imgContent, "image", "image" + GuessExt(imageMime));
            if (maskBytes != null && maskBytes.Length > 0)
            {
                var maskContent = new ByteArrayContent(maskBytes);
                maskContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(maskMime ?? "image/png");
                content.Add(maskContent, "mask", "mask.png");
            }
            content.Add(new StringContent(prompt), "prompt");
            content.Add(new StringContent(modelId ?? "gpt-image-1"), "model");
            content.Add(new StringContent(size ?? "1024x1024"), "size");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);

            var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                result.Error = $"HTTP {(int)response.StatusCode}: {Truncate(raw, 300)}";
                return result;
            }
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("data", out var dataArr))
            {
                foreach (var item in dataArr.EnumerateArray())
                {
                    var img = new SgLlmGeneratedImage();
                    if (item.TryGetProperty("url", out var u)) img.Url = u.GetString();
                    if (item.TryGetProperty("b64_json", out var b)) img.B64Json = b.GetString();
                    result.Images.Add(img);
                }
            }
            return result;
        }
        catch (Exception ex) { result.Error = ex.Message; return result; }
    }

    public async Task<SgLlmTranscription> TranscribeAsync(SgLlmTranscribeRequest req)
    {
        var result = new SgLlmTranscription();
        if (CurrentConfig == null) { result.Error = "Service not initialized"; return result; }
        var endpoint = req.Translate ? "/audio/translations" : "/audio/transcriptions";
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + endpoint;

        try
        {
            using var content = new MultipartFormDataContent();
            var audio = new ByteArrayContent(req.Audio);
            content.Add(audio, "file", req.FileName);
            content.Add(new StringContent(req.Model ?? "whisper-1"), "model");
            if (!string.IsNullOrEmpty(req.Language)) content.Add(new StringContent(req.Language), "language");
            if (!string.IsNullOrEmpty(req.Prompt)) content.Add(new StringContent(req.Prompt), "prompt");
            if (!string.IsNullOrEmpty(req.ResponseFormat)) content.Add(new StringContent(req.ResponseFormat), "response_format");
            if (req.Temperature.HasValue) content.Add(new StringContent(req.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "temperature");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);

            var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                result.Error = $"HTTP {(int)response.StatusCode}: {Truncate(raw, 300)}";
                return result;
            }
            // verbose_json contains language + duration, plain text returns the text itself
            if (req.ResponseFormat == "text" || req.ResponseFormat == "srt" || req.ResponseFormat == "vtt")
            {
                result.Text = raw;
            }
            else
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("text", out var t)) result.Text = t.GetString() ?? "";
                if (doc.RootElement.TryGetProperty("language", out var l)) result.Language = l.GetString();
                if (doc.RootElement.TryGetProperty("duration", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.Number)
                    result.Duration = d.GetDouble();
            }
            return result;
        }
        catch (Exception ex) { result.Error = ex.Message; return result; }
    }

    public async Task<SgLlmTtsResult> SynthesizeAsync(SgLlmTtsRequest req)
    {
        var result = new SgLlmTtsResult();
        if (CurrentConfig == null) { result.Error = "Service not initialized"; return result; }
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/audio/speech";

        try
        {
            var body = new Dictionary<string, object?>
            {
                ["model"] = req.Model ?? "tts-1",
                ["input"] = req.Input,
                ["voice"] = req.Voice ?? "alloy",
                ["response_format"] = req.Format ?? "mp3"
            };
            if (req.Speed.HasValue) body["speed"] = req.Speed.Value;
            if (!string.IsNullOrEmpty(req.Instructions)) body["instructions"] = req.Instructions;

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync();
                result.Error = $"HTTP {(int)response.StatusCode}: {Truncate(errBody, 300)}";
                return result;
            }
            result.Audio = await response.Content.ReadAsByteArrayAsync();
            result.MimeType = (req.Format ?? "mp3") switch
            {
                "wav" => "audio/wav",
                "opus" => "audio/opus",
                "aac" => "audio/aac",
                "flac" => "audio/flac",
                "pcm" => "audio/pcm",
                _ => "audio/mpeg"
            };
            return result;
        }
        catch (Exception ex) { result.Error = ex.Message; return result; }
    }

    public async Task<SgLlmModerationResult> ModerateAsync(string text, string? modelId = null)
    {
        var result = new SgLlmModerationResult();
        if (CurrentConfig == null) { result.Error = "Service not initialized"; return result; }
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/moderations";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);
            request.Content = JsonContent.Create(new { input = text, model = modelId ?? "omni-moderation-latest" });

            var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                result.Error = $"HTTP {(int)response.StatusCode}: {Truncate(raw, 300)}";
                return result;
            }
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("results", out var arr) && arr.GetArrayLength() > 0)
            {
                var first = arr[0];
                if (first.TryGetProperty("flagged", out var f)) result.Flagged = f.GetBoolean();
                if (first.TryGetProperty("categories", out var cats))
                {
                    foreach (var c in cats.EnumerateObject())
                        if (c.Value.ValueKind == System.Text.Json.JsonValueKind.True) result.Categories.Add(c.Name);
                }
                if (first.TryGetProperty("category_scores", out var scores))
                {
                    foreach (var s in scores.EnumerateObject())
                        if (s.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                            result.CategoryScores[s.Name] = s.Value.GetDouble();
                }
            }
            return result;
        }
        catch (Exception ex) { result.Error = ex.Message; return result; }
    }

    public async Task<string> AnalyzeVisionAsync(SgLlmVisionRequest req)
    {
        if (CurrentConfig == null) return "(service not initialized)";
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/chat/completions";

        var parts = new List<object> { new { type = "text", text = req.Prompt } };
        foreach (var img in req.Images)
        {
            parts.Add(new { type = "image_url", image_url = new { url = $"data:{img.MimeType};base64,{img.Base64}" } });
        }

        var body = new Dictionary<string, object?>
        {
            ["model"] = req.Model ?? CurrentConfig.ModelId ?? "gpt-4o-mini",
            ["messages"] = new[] { new { role = "user", content = parts } },
            ["temperature"] = req.Temperature
        };
        if (req.MaxTokens.HasValue) body["max_tokens"] = req.MaxTokens.Value;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);
            if (CurrentConfig.Provider == SgLlmProvider.OpenRouter)
            {
                request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://superui.local");
                request.Headers.TryAddWithoutValidation("X-Title", "SuperUI");
            }
            var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) return $"HTTP {(int)response.StatusCode}: {Truncate(raw, 300)}";

            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        catch (Exception ex) { return ex.Message; }
    }

    public async Task<string> AnalyzeVideoAsync(SgLlmVideoRequest req)
    {
        // Gemini accepts inline video data via generateContent. For everyone else we
        // fall back to chat/completions multipart (some providers accept video frames
        // as image attachments, but proper video support is provider-specific).
        if (CurrentConfig == null) return "(service not initialized)";

        if (CurrentConfig.Provider == SgLlmProvider.Google)
        {
            var baseUrl = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://generativelanguage.googleapis.com/v1beta");
            var model = req.Model ?? CurrentConfig.ModelId ?? "gemini-2.5-flash";
            var url = $"{baseUrl}/models/{model}:generateContent?key={CurrentConfig.ApiKey}";

            var b64 = Convert.ToBase64String(req.Video);
            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = req.Prompt },
                            new { inline_data = new { mime_type = req.MimeType, data = b64 } }
                        }
                    }
                },
                generationConfig = req.MaxTokens.HasValue
                    ? (object)new { maxOutputTokens = req.MaxTokens.Value }
                    : new { }
            };
            try
            {
                var resp = await _http.PostAsJsonAsync(url, body);
                var raw = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return $"HTTP {(int)resp.StatusCode}: {Truncate(raw, 300)}";
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var sb = new System.Text.StringBuilder();
                foreach (var p in doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts").EnumerateArray())
                    if (p.TryGetProperty("text", out var t)) sb.Append(t.GetString());
                return sb.ToString();
            }
            catch (Exception ex) { return ex.Message; }
        }

        return "Video analysis is only implemented for Google Gemini in this build.";
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";
    private static string GuessExt(string mime) => mime switch
    {
        "image/png" => ".png",
        "image/jpeg" or "image/jpg" => ".jpg",
        "image/webp" => ".webp",
        _ => ""
    };

    // ============== FREE / ADDITIONAL PROVIDERS ==============

    private static readonly IReadOnlyList<SgLlmProviderPreset> _presets = new List<SgLlmProviderPreset>
    {
        new() { Provider = SgLlmProvider.OpenAiCompatible, Label = "OpenAI", BaseUrl = "https://api.openai.com/v1",
                ApiKeyUrl = "https://platform.openai.com/api-keys", DocsUrl = "https://platform.openai.com/docs", IsFree = false },
        new() { Provider = SgLlmProvider.OpenRouter, Label = "OpenRouter", BaseUrl = "https://openrouter.ai/api/v1",
                ApiKeyUrl = "https://openrouter.ai/keys", DocsUrl = "https://openrouter.ai/docs", IsFree = true,
                Notes = "Free models available (suffix :free)." },
        new() { Provider = SgLlmProvider.Ollama, Label = "Ollama (local)", BaseUrl = "http://localhost:11434",
                DocsUrl = "https://ollama.com", IsFree = true, RequiresKey = false,
                Notes = "Локальные модели, ключ не нужен." },
        new() { Provider = SgLlmProvider.Anthropic, Label = "Anthropic", BaseUrl = "https://api.anthropic.com/v1",
                ApiKeyUrl = "https://console.anthropic.com/settings/keys" },
        new() { Provider = SgLlmProvider.LmStudio, Label = "LM Studio", BaseUrl = "http://localhost:1234/v1",
                DocsUrl = "https://lmstudio.ai/docs", IsFree = true, RequiresKey = false,
                Notes = "Локальный OpenAI-compatible сервер." },
        new() { Provider = SgLlmProvider.GigaGpt, Label = "GigaGPT / GigaChat", BaseUrl = "https://gigachat.devices.sberbank.ru/api/v1",
                ApiKeyUrl = "https://developers.sber.ru/portal/products/gigachat-api", IsFree = false,
                Notes = "Совместимый chat/completions endpoint, требуется bearer token." },
        new() { Provider = SgLlmProvider.Google, Label = "Google Gemini",
                BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
                ApiKeyUrl = "https://aistudio.google.com/app/apikey", IsFree = true,
                Notes = "AI Studio даёт бесплатную квоту." },
        new() { Provider = SgLlmProvider.Mistral, Label = "Mistral AI", BaseUrl = "https://api.mistral.ai/v1",
                ApiKeyUrl = "https://console.mistral.ai/api-keys", IsFree = true,
                Notes = "На «Experiment» тарифе бесплатно." },
        new() { Provider = SgLlmProvider.Groq, Label = "Groq", BaseUrl = "https://api.groq.com/openai/v1",
                ApiKeyUrl = "https://console.groq.com/keys", IsFree = true,
                Notes = "Очень быстрый инференс, free tier." },
        new() { Provider = SgLlmProvider.DeepSeek, Label = "DeepSeek", BaseUrl = "https://api.deepseek.com",
                ApiKeyUrl = "https://platform.deepseek.com/api_keys" },
        new() { Provider = SgLlmProvider.XAi, Label = "xAI Grok", BaseUrl = "https://api.x.ai/v1",
                ApiKeyUrl = "https://console.x.ai/" },
        new() { Provider = SgLlmProvider.Cohere, Label = "Cohere",
                BaseUrl = "https://api.cohere.ai/compatibility/v1",
                ApiKeyUrl = "https://dashboard.cohere.com/api-keys", IsFree = true,
                Notes = "Trial-ключи бесплатны." },
        new() { Provider = SgLlmProvider.Perplexity, Label = "Perplexity", BaseUrl = "https://api.perplexity.ai",
                ApiKeyUrl = "https://www.perplexity.ai/settings/api" },
        new() { Provider = SgLlmProvider.TogetherAi, Label = "Together AI",
                BaseUrl = "https://api.together.xyz/v1",
                ApiKeyUrl = "https://api.together.ai/settings/api-keys", IsFree = true,
                Notes = "$1 free credits + бесплатные модели." },
        new() { Provider = SgLlmProvider.Fireworks, Label = "Fireworks AI",
                BaseUrl = "https://api.fireworks.ai/inference/v1",
                ApiKeyUrl = "https://fireworks.ai/account/api-keys" },
        new() { Provider = SgLlmProvider.Cerebras, Label = "Cerebras",
                BaseUrl = "https://api.cerebras.ai/v1",
                ApiKeyUrl = "https://cloud.cerebras.ai/", IsFree = true,
                Notes = "Free tier для разработчиков." },
        new() { Provider = SgLlmProvider.AzureOpenAi, Label = "Azure OpenAI", BaseUrl = "" },
        new() { Provider = SgLlmProvider.HuggingFace, Label = "HuggingFace",
                BaseUrl = "https://router.huggingface.co/v1",
                ApiKeyUrl = "https://huggingface.co/settings/tokens", IsFree = true,
                Notes = "Бесплатный inference router." },
        new() { Provider = SgLlmProvider.CloudflareWorkersAi, Label = "Cloudflare Workers AI",
                BaseUrl = "https://api.cloudflare.com/client/v4/accounts/{ACCOUNT_ID}/ai/v1",
                ApiKeyUrl = "https://dash.cloudflare.com/profile/api-tokens",
                DocsUrl = "https://developers.cloudflare.com/workers-ai/", IsFree = true,
                Notes = "10 000 Neurons/день бесплатно. Замените {ACCOUNT_ID}." },
        new() { Provider = SgLlmProvider.GitHubModels, Label = "GitHub Models",
                BaseUrl = "https://models.inference.ai.azure.com",
                ApiKeyUrl = "https://github.com/settings/tokens",
                DocsUrl = "https://github.com/marketplace/models", IsFree = true,
                Notes = "Бесплатно по GitHub Personal Access Token." },
        new() { Provider = SgLlmProvider.SambaNova, Label = "SambaNova Cloud",
                BaseUrl = "https://api.sambanova.ai/v1",
                ApiKeyUrl = "https://cloud.sambanova.ai/apis", IsFree = true,
                Notes = "Free tier, OpenAI-совместимый." },
        new() { Provider = SgLlmProvider.GlhfChat, Label = "GLHF.chat",
                BaseUrl = "https://glhf.chat/api/openai/v1",
                ApiKeyUrl = "https://glhf.chat/users/settings/api", IsFree = true,
                Notes = "Бесплатный hosted OSS-инференс." },
        new() { Provider = SgLlmProvider.Targon, Label = "Targon",
                BaseUrl = "https://api.targon.com/v1",
                ApiKeyUrl = "https://targon.com/sign-in", IsFree = true,
                Notes = "Бесплатный routing, требуется логин." },
        new() { Provider = SgLlmProvider.Pollinations, Label = "Pollinations",
                BaseUrl = "https://text.pollinations.ai/openai",
                DocsUrl = "https://pollinations.ai/", IsFree = true, RequiresKey = false,
                Notes = "Не требует ключа, есть текст и картинки." },
    };

    public IReadOnlyList<SgLlmProviderPreset> GetProviderPresets() => SgLlmProviderRegistry.Presets;

    public SgLlmProviderPreset? GetPreset(SgLlmProvider provider) => SgLlmProviderRegistry.GetPreset(provider);

    public async Task<List<SgLlmModelInfo>> GetCloudflareWorkersAiModelsAsync(string? accountId = null, string? apiToken = null)
    {
        // Cloudflare doesn't expose /models on the openai-compat path; return a curated list.
        return new List<SgLlmModelInfo>
        {
            new() { Id = "@cf/meta/llama-3.3-70b-instruct-fp8-fast", Name = "Llama 3.3 70B (CF)", IsFree = true },
            new() { Id = "@cf/meta/llama-3.1-8b-instruct", Name = "Llama 3.1 8B (CF)", IsFree = true },
            new() { Id = "@cf/qwen/qwen2.5-coder-32b-instruct", Name = "Qwen2.5 Coder 32B", IsFree = true },
            new() { Id = "@cf/deepseek-ai/deepseek-r1-distill-qwen-32b", Name = "DeepSeek R1 Distill 32B", IsFree = true },
            new() { Id = "@cf/google/gemma-3-12b-it", Name = "Gemma 3 12B", IsFree = true },
            new() { Id = "@cf/mistralai/mistral-small-3.1-24b-instruct", Name = "Mistral Small 3.1 24B", IsFree = true },
        };
    }

    public async Task<List<SgLlmModelInfo>> GetGitHubModelsAsync(string? apiKey = null)
    {
        // GitHub Models catalog endpoint is azure-flavoured; use a curated list as fallback.
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://models.inference.ai.azure.com/models");
            if (!string.IsNullOrEmpty(apiKey)) req.Headers.Authorization = new("Bearer", apiKey);
            var resp = await _http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                var list = new List<SgLlmModelInfo>();
                var root = doc.RootElement;
                IEnumerable<System.Text.Json.JsonElement> items =
                    root.ValueKind == System.Text.Json.JsonValueKind.Array
                        ? root.EnumerateArray()
                        : (root.TryGetProperty("data", out var d) ? d.EnumerateArray() : Array.Empty<System.Text.Json.JsonElement>());
                foreach (var m in items)
                {
                    var id = m.TryGetProperty("name", out var n) ? n.GetString()
                          : (m.TryGetProperty("id", out var idEl) ? idEl.GetString() : null);
                    if (!string.IsNullOrEmpty(id)) list.Add(new() { Id = id!, Name = id!, IsFree = true });
                }
                if (list.Count > 0) return list;
            }
        }
        catch { }
        return new()
        {
            new() { Id = "gpt-4o-mini", Name = "GPT-4o mini (GitHub)", IsFree = true },
            new() { Id = "gpt-4o", Name = "GPT-4o (GitHub)", IsFree = true },
            new() { Id = "Phi-3.5-MoE-instruct", Name = "Phi-3.5 MoE (GitHub)", IsFree = true },
            new() { Id = "Mistral-large-2407", Name = "Mistral Large (GitHub)", IsFree = true },
            new() { Id = "Meta-Llama-3.1-70B-Instruct", Name = "Llama 3.1 70B (GitHub)", IsFree = true },
        };
    }

    public Task<List<SgLlmModelInfo>> GetSambaNovaModelsAsync(string? apiKey = null)
        => GetOpenAiCompatibleModelsAsync("https://api.sambanova.ai/v1", apiKey, () => new()
        {
            new() { Id = "Meta-Llama-3.3-70B-Instruct", Name = "Llama 3.3 70B", IsFree = true },
            new() { Id = "DeepSeek-R1-Distill-Llama-70B", Name = "DeepSeek R1 Distill 70B", IsFree = true },
            new() { Id = "Qwen2.5-72B-Instruct", Name = "Qwen2.5 72B", IsFree = true },
        });

    public Task<List<SgLlmModelInfo>> GetGlhfModelsAsync(string? apiKey = null) =>
        Task.FromResult(new List<SgLlmModelInfo>
        {
            new() { Id = "hf:meta-llama/Llama-3.3-70B-Instruct", Name = "Llama 3.3 70B", IsFree = true },
            new() { Id = "hf:Qwen/Qwen2.5-72B-Instruct", Name = "Qwen2.5 72B", IsFree = true },
            new() { Id = "hf:deepseek-ai/DeepSeek-V3", Name = "DeepSeek V3", IsFree = true },
            new() { Id = "hf:mistralai/Mistral-Small-Instruct-2409", Name = "Mistral Small", IsFree = true },
        });

    public Task<List<SgLlmModelInfo>> GetPollinationsModelsAsync() =>
        Task.FromResult(new List<SgLlmModelInfo>
        {
            new() { Id = "openai", Name = "OpenAI (Pollinations proxy)", IsFree = true },
            new() { Id = "mistral", Name = "Mistral (Pollinations)", IsFree = true },
            new() { Id = "qwen-coder", Name = "Qwen2.5 Coder", IsFree = true },
            new() { Id = "llama", Name = "Llama 3.x", IsFree = true },
            new() { Id = "deepseek", Name = "DeepSeek", IsFree = true },
        });

    // ============== ADDITIONAL API CAPABILITIES ==============

    public async Task<SgLlmChatResult> CompleteAsync(SgLlmChatRequest req)
    {
        var result = new SgLlmChatResult();
        if (CurrentConfig == null) { result.Error = "Service not initialized"; return result; }
        var baseUrl = CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1";
        var url = baseUrl + "/chat/completions";

        var messages = req.Messages.Select(m => BuildOpenAiMessage(m)).ToList();
        var body = new Dictionary<string, object?>
        {
            ["model"] = req.Model ?? CurrentConfig.ModelId ?? "gpt-4o-mini",
            ["messages"] = messages,
            ["stream"] = false
        };
        if (req.Temperature.HasValue) body["temperature"] = req.Temperature.Value;
        if (req.TopP.HasValue) body["top_p"] = req.TopP.Value;
        if (req.MaxTokens.HasValue) body["max_tokens"] = req.MaxTokens.Value;
        if (req.Seed.HasValue) body["seed"] = req.Seed.Value;
        if (req.Stop is { Count: > 0 }) body["stop"] = req.Stop;
        if (req.Tools is { Count: > 0 })
        {
            body["tools"] = req.Tools.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = string.IsNullOrEmpty(t.ParametersJsonSchema)
                        ? (object)new { type = "object", properties = new { } }
                        : System.Text.Json.JsonDocument.Parse(t.ParametersJsonSchema).RootElement
                }
            }).ToList();
        }
        if (req.ToolChoice != null) body["tool_choice"] = req.ToolChoice;
        if (req.ResponseFormat == "json_object")
            body["response_format"] = new { type = "json_object" };
        else if (req.ResponseFormat == "json_schema" && !string.IsNullOrEmpty(req.JsonSchema))
        {
            var schema = System.Text.Json.JsonDocument.Parse(req.JsonSchema).RootElement;
            body["response_format"] = new { type = "json_schema", json_schema = new { name = "schema", schema, strict = true } };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);
            if (CurrentConfig.Provider == SgLlmProvider.OpenRouter)
            {
                request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://superui.local");
                request.Headers.TryAddWithoutValidation("X-Title", "SuperUI");
            }

            var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            result.RawJson = raw;
            if (!response.IsSuccessStatusCode) { result.Error = $"HTTP {(int)response.StatusCode}: {Truncate(raw, 300)}"; return result; }

            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            var choice = doc.RootElement.GetProperty("choices")[0];
            if (choice.TryGetProperty("finish_reason", out var fr)) result.FinishReason = fr.GetString();
            var msg = choice.GetProperty("message");
            if (msg.TryGetProperty("content", out var c) && c.ValueKind == System.Text.Json.JsonValueKind.String)
                result.Content = c.GetString() ?? "";
            if (msg.TryGetProperty("tool_calls", out var tcs) && tcs.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var tc in tcs.EnumerateArray())
                {
                    var call = new SgLlmToolCall();
                    if (tc.TryGetProperty("id", out var idEl)) call.Id = idEl.GetString() ?? "";
                    if (tc.TryGetProperty("function", out var fn))
                    {
                        if (fn.TryGetProperty("name", out var nm)) call.Name = nm.GetString() ?? "";
                        if (fn.TryGetProperty("arguments", out var ar)) call.ArgumentsJson = ar.GetString() ?? "";
                    }
                    result.ToolCalls.Add(call);
                }
            }
            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.ValueKind == System.Text.Json.JsonValueKind.Number) result.PromptTokens = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ct) && ct.ValueKind == System.Text.Json.JsonValueKind.Number) result.CompletionTokens = ct.GetInt32();
                if (usage.TryGetProperty("total_tokens", out var tt) && tt.ValueKind == System.Text.Json.JsonValueKind.Number) result.TotalTokens = tt.GetInt32();
            }
            return result;
        }
        catch (Exception ex) { result.Error = ex.Message; return result; }
    }

    private static object BuildOpenAiMessage(SgLlmChatMsg m)
    {
        if (m.Attachments is { Count: > 0 })
        {
            var parts = new List<object> { new { type = "text", text = m.Content } };
            foreach (var a in m.Attachments.Where(x => x.IsImage))
                parts.Add(new { type = "image_url", image_url = new { url = $"data:{a.MimeType};base64,{a.Base64}" } });
            return new { role = m.Role, content = parts };
        }
        if (m.ToolCalls is { Count: > 0 })
        {
            return new
            {
                role = m.Role,
                content = m.Content,
                tool_calls = m.ToolCalls.Select(t => new
                {
                    id = t.Id,
                    type = "function",
                    function = new { name = t.Name, arguments = t.ArgumentsJson }
                }).ToList()
            };
        }
        if (!string.IsNullOrEmpty(m.ToolCallId))
            return new { role = "tool", content = m.Content, tool_call_id = m.ToolCallId, name = m.Name };
        return new { role = m.Role, content = m.Content };
    }

    public async Task<SgLlmStructuredResult<T>> CompleteStructuredAsync<T>(SgLlmStructuredRequest req)
    {
        var schema = req.JsonSchema ?? BuildJsonSchemaForType(typeof(T));
        var chatRes = await CompleteAsync(new SgLlmChatRequest
        {
            Messages = req.Messages,
            Model = req.Model,
            Temperature = req.Temperature,
            MaxTokens = req.MaxTokens,
            ResponseFormat = "json_schema",
            JsonSchema = schema
        });
        var sr = new SgLlmStructuredResult<T> { RawJson = chatRes.Content, Error = chatRes.Error };
        if (!string.IsNullOrEmpty(chatRes.Content))
        {
            try { sr.Data = System.Text.Json.JsonSerializer.Deserialize<T>(chatRes.Content); }
            catch (Exception ex) { sr.Error = $"Parse failed: {ex.Message}"; }
        }
        return sr;
    }

    private static string BuildJsonSchemaForType(Type t)
    {
        // Minimal helper for common cases — non-recursive; users can pass a custom schema for complex types.
        if (t == typeof(string))
            return "{\"type\":\"object\",\"properties\":{\"value\":{\"type\":\"string\"}},\"required\":[\"value\"],\"additionalProperties\":false}";

        var props = new Dictionary<string, object>();
        var required = new List<string>();
        foreach (var p in t.GetProperties())
        {
            var name = char.ToLowerInvariant(p.Name[0]) + p.Name[1..];
            props[name] = MapJsonType(p.PropertyType);
            required.Add(name);
        }
        var schema = new { type = "object", properties = props, required, additionalProperties = false };
        return System.Text.Json.JsonSerializer.Serialize(schema);
    }

    private static object MapJsonType(Type t)
    {
        var nt = Nullable.GetUnderlyingType(t) ?? t;
        if (nt == typeof(string)) return new { type = "string" };
        if (nt == typeof(bool)) return new { type = "boolean" };
        if (nt == typeof(int) || nt == typeof(long)) return new { type = "integer" };
        if (nt == typeof(double) || nt == typeof(float) || nt == typeof(decimal)) return new { type = "number" };
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(nt) && nt != typeof(string))
            return new { type = "array", items = new { type = "string" } };
        return new { type = "object" };
    }

    public async Task<List<SgLlmRerankResult>> RerankAsync(SgLlmRerankRequest req)
    {
        if (CurrentConfig == null) return new();
        if (req.Documents.Count == 0) return new();

        // Cohere v2 rerank endpoint
        if (CurrentConfig.Provider == SgLlmProvider.Cohere)
        {
            var url = "https://api.cohere.com/v2/rerank";
            var body = new
            {
                model = req.Model ?? "rerank-v3.5",
                query = req.Query,
                documents = req.Documents,
                top_n = req.TopN ?? req.Documents.Count
            };
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
                if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentConfig.ApiKey);
                var resp = await _http.SendAsync(request);
                if (!resp.IsSuccessStatusCode) return new();
                using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var list = new List<SgLlmRerankResult>();
                if (doc.RootElement.TryGetProperty("results", out var arr))
                    foreach (var r in arr.EnumerateArray())
                    {
                        var idx = r.GetProperty("index").GetInt32();
                        var score = r.GetProperty("relevance_score").GetDouble();
                        list.Add(new() { Index = idx, Score = score, Document = req.Documents[idx] });
                    }
                return list;
            }
            catch { return new(); }
        }

        // Fallback: embedding-based cosine rerank
        var qVec = await GetEmbeddingsAsync(req.Query);
        var docVecs = await GetEmbeddingsBatchAsync(req.Documents);
        var fallback = new List<SgLlmRerankResult>();
        for (var i = 0; i < docVecs.Count; i++)
        {
            double dot = 0, na = 0, nb = 0;
            var a = qVec; var b = docVecs[i];
            var n = Math.Min(a.Length, b.Length);
            for (var k = 0; k < n; k++) { dot += a[k] * b[k]; na += a[k] * a[k]; nb += b[k] * b[k]; }
            var denom = Math.Sqrt(na) * Math.Sqrt(nb);
            fallback.Add(new() { Index = i, Score = denom == 0 ? 0 : dot / denom, Document = req.Documents[i] });
        }
        return fallback.OrderByDescending(r => r.Score).Take(req.TopN ?? fallback.Count).ToList();
    }

    public async Task<SgLlmImageResult> GenerateImageVariationsAsync(byte[] imageBytes, string imageMime, int count = 1,
        string? size = "1024x1024", string? modelId = null)
    {
        var result = new SgLlmImageResult();
        if (CurrentConfig == null) { result.Error = "Service not initialized"; return result; }
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/images/variations";
        try
        {
            using var content = new MultipartFormDataContent();
            var img = new ByteArrayContent(imageBytes);
            img.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageMime);
            content.Add(img, "image", "image" + GuessExt(imageMime));
            content.Add(new StringContent(Math.Max(1, count).ToString()), "n");
            content.Add(new StringContent(size ?? "1024x1024"), "size");
            content.Add(new StringContent(modelId ?? "dall-e-2"), "model");
            content.Add(new StringContent("b64_json"), "response_format");

            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                req.Headers.Authorization = new("Bearer", CurrentConfig.ApiKey);
            var resp = await _http.SendAsync(req);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { result.Error = $"HTTP {(int)resp.StatusCode}: {Truncate(raw, 300)}"; return result; }
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("data", out var arr))
                foreach (var item in arr.EnumerateArray())
                {
                    var gi = new SgLlmGeneratedImage();
                    if (item.TryGetProperty("url", out var u)) gi.Url = u.GetString();
                    if (item.TryGetProperty("b64_json", out var b)) gi.B64Json = b.GetString();
                    result.Images.Add(gi);
                }
            return result;
        }
        catch (Exception ex) { result.Error = ex.Message; return result; }
    }

    public async Task<List<SgLlmFileInfo>> ListFilesAsync(string? purpose = null)
    {
        if (CurrentConfig == null) return new();
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/files";
        if (!string.IsNullOrEmpty(purpose)) url += $"?purpose={Uri.EscapeDataString(purpose)}";
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                req.Headers.Authorization = new("Bearer", CurrentConfig.ApiKey);
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return new();
            using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var list = new List<SgLlmFileInfo>();
            if (doc.RootElement.TryGetProperty("data", out var arr))
                foreach (var f in arr.EnumerateArray())
                {
                    var fi = new SgLlmFileInfo();
                    if (f.TryGetProperty("id", out var i)) fi.Id = i.GetString() ?? "";
                    if (f.TryGetProperty("filename", out var n)) fi.FileName = n.GetString() ?? "";
                    if (f.TryGetProperty("bytes", out var b) && b.ValueKind == System.Text.Json.JsonValueKind.Number) fi.Bytes = b.GetInt64();
                    if (f.TryGetProperty("purpose", out var p)) fi.Purpose = p.GetString();
                    if (f.TryGetProperty("created_at", out var ca) && ca.ValueKind == System.Text.Json.JsonValueKind.Number)
                        fi.CreatedAt = DateTimeOffset.FromUnixTimeSeconds(ca.GetInt64()).UtcDateTime;
                    list.Add(fi);
                }
            return list;
        }
        catch { return new(); }
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        if (CurrentConfig == null) return false;
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/files/" + Uri.EscapeDataString(fileId);
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                req.Headers.Authorization = new("Bearer", CurrentConfig.ApiKey);
            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<SgLlmFineTuneJob>> ListFineTuneJobsAsync()
    {
        if (CurrentConfig == null) return new();
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/fine_tuning/jobs";
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                req.Headers.Authorization = new("Bearer", CurrentConfig.ApiKey);
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return new();
            using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var list = new List<SgLlmFineTuneJob>();
            if (doc.RootElement.TryGetProperty("data", out var arr))
                foreach (var j in arr.EnumerateArray())
                {
                    var ft = new SgLlmFineTuneJob();
                    if (j.TryGetProperty("id", out var i)) ft.Id = i.GetString() ?? "";
                    if (j.TryGetProperty("model", out var m)) ft.Model = m.GetString();
                    if (j.TryGetProperty("status", out var s)) ft.Status = s.GetString();
                    if (j.TryGetProperty("fine_tuned_model", out var fm)) ft.FineTunedModel = fm.GetString();
                    if (j.TryGetProperty("created_at", out var ca) && ca.ValueKind == System.Text.Json.JsonValueKind.Number)
                        ft.CreatedAt = DateTimeOffset.FromUnixTimeSeconds(ca.GetInt64()).UtcDateTime;
                    list.Add(ft);
                }
            return list;
        }
        catch { return new(); }
    }

    public async Task<SgLlmBatchJob> CreateBatchAsync(SgLlmBatchRequest req)
    {
        var job = new SgLlmBatchJob();
        if (CurrentConfig == null) return job;
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/batches";
        try
        {
            var body = new Dictionary<string, object?>
            {
                ["input_file_id"] = req.InputFileId,
                ["endpoint"] = req.Endpoint,
                ["completion_window"] = req.CompletionWindow
            };
            if (req.Metadata != null) body["metadata"] = req.Metadata;
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                request.Headers.Authorization = new("Bearer", CurrentConfig.ApiKey);
            var resp = await _http.SendAsync(request);
            if (!resp.IsSuccessStatusCode) return job;
            using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            ParseBatch(doc.RootElement, job);
            return job;
        }
        catch { return job; }
    }

    public async Task<SgLlmBatchJob?> GetBatchAsync(string batchId)
    {
        if (CurrentConfig == null) return null;
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/batches/" + Uri.EscapeDataString(batchId);
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                req.Headers.Authorization = new("Bearer", CurrentConfig.ApiKey);
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var job = new SgLlmBatchJob();
            ParseBatch(doc.RootElement, job);
            return job;
        }
        catch { return null; }
    }

    public async Task<List<SgLlmBatchJob>> ListBatchesAsync()
    {
        if (CurrentConfig == null) return new();
        var url = (CurrentConfig.BaseUrl?.TrimEnd('/') ?? "https://api.openai.com/v1") + "/batches";
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(CurrentConfig.ApiKey))
                req.Headers.Authorization = new("Bearer", CurrentConfig.ApiKey);
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return new();
            using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var list = new List<SgLlmBatchJob>();
            if (doc.RootElement.TryGetProperty("data", out var arr))
                foreach (var j in arr.EnumerateArray())
                {
                    var job = new SgLlmBatchJob();
                    ParseBatch(j, job);
                    list.Add(job);
                }
            return list;
        }
        catch { return new(); }
    }

    private static void ParseBatch(System.Text.Json.JsonElement root, SgLlmBatchJob job)
    {
        if (root.TryGetProperty("id", out var i)) job.Id = i.GetString() ?? "";
        if (root.TryGetProperty("status", out var s)) job.Status = s.GetString();
        if (root.TryGetProperty("endpoint", out var e)) job.Endpoint = e.GetString();
        if (root.TryGetProperty("input_file_id", out var ifid)) job.InputFileId = ifid.GetString();
        if (root.TryGetProperty("output_file_id", out var ofid)) job.OutputFileId = ofid.GetString();
        if (root.TryGetProperty("error_file_id", out var efid)) job.ErrorFileId = efid.GetString();
        if (root.TryGetProperty("request_counts", out var rc))
        {
            if (rc.TryGetProperty("completed", out var c) && c.ValueKind == System.Text.Json.JsonValueKind.Number) job.RequestCounts_Completed = c.GetInt32();
            if (rc.TryGetProperty("failed", out var f) && f.ValueKind == System.Text.Json.JsonValueKind.Number) job.RequestCounts_Failed = f.GetInt32();
            if (rc.TryGetProperty("total", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.Number) job.RequestCounts_Total = t.GetInt32();
        }
        if (root.TryGetProperty("created_at", out var ca) && ca.ValueKind == System.Text.Json.JsonValueKind.Number)
            job.CreatedAt = DateTimeOffset.FromUnixTimeSeconds(ca.GetInt64()).UtcDateTime;
    }

    // ---- Internal response DTOs ----
    private class GigaOAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        [JsonPropertyName("expires_at")]
        public long? ExpiresAt { get; set; }
    }

    private class OpenAiModelsResponse { public List<OpenAiModel>? Data { get; set; } }
    private class OpenAiModel
    {
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; } = string.Empty;
        public OpenAiModelArchitecture? Architecture { get; set; }
        public List<OpenAiProviderInfo>? Providers { get; set; }
    }

    private class OpenAiModelArchitecture
    {
        [JsonPropertyName("input_modalities")]
        public List<string> InputModalities { get; set; } = new();
        [JsonPropertyName("output_modalities")]
        public List<string> OutputModalities { get; set; } = new();
    }

    private class OpenAiProviderInfo
    {
        [JsonPropertyName("context_length")]
        public int? ContextLength { get; set; }
        [JsonPropertyName("supports_tools")]
        public bool? SupportsTools { get; set; }
        [JsonPropertyName("supports_structured_output")]
        public bool? SupportsStructuredOutput { get; set; }
    }

    private class AnthropicModelsResponse { public List<AnthropicModel>? Data { get; set; } }
    private class AnthropicModel
    {
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
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
        [JsonPropertyName("context_length")]
        public int? ContextLength { get; set; }
        public OpenRouterArchitecture? Architecture { get; set; }
        public OpenRouterPricing? Pricing { get; set; }
    }
    private class OpenRouterArchitecture
    {
        [JsonPropertyName("input_modalities")]
        public List<string> InputModalities { get; set; } = new();
        [JsonPropertyName("output_modalities")]
        public List<string> OutputModalities { get; set; } = new();
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
        if (CurrentConfig is not null)
        {
            var usage = new SgLlmUsageRecord
            {
                Provider = CurrentConfig.Provider,
                ProviderLabel = SgLlmProviderRegistry.Label(CurrentConfig.Provider),
                ModelId = CurrentConfig.ModelId,
                Ok = true
            };
            if (result.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (result.TryGetProperty("promptTokens", out var pt) && pt.ValueKind == System.Text.Json.JsonValueKind.Number) usage.PromptTokens = pt.GetInt32();
                if (result.TryGetProperty("completionTokens", out var ct) && ct.ValueKind == System.Text.Json.JsonValueKind.Number) usage.CompletionTokens = ct.GetInt32();
                if (result.TryGetProperty("durationMs", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.Number) usage.DurationMs = d.GetDouble();
            }
            _ = SaveUsageRecordAsync(usage);
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
    public void OnErrorCallback(string message)
    {
        OnError?.Invoke(message);
        if (CurrentConfig is not null)
        {
            _ = SaveUsageRecordAsync(new SgLlmUsageRecord
            {
                Provider = CurrentConfig.Provider,
                ProviderLabel = SgLlmProviderRegistry.Label(CurrentConfig.Provider),
                ModelId = CurrentConfig.ModelId,
                Ok = false,
                Error = message
            });
        }
    }

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
