using Microsoft.AspNetCore.Components;
using SuperUI.Localization;
using SuperUI.Enums;
using SuperUI.Services.Llm;

namespace SuperUI.Components.Llm;

/// <summary>Settings panel for LLM configuration.</summary>
public partial class SgLlmSettings : ComponentBase, IDisposable
{
    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

    [Parameter] public SgLlmConfig Config { get; set; } = new();
    /// <summary>Callback invoked when the configuration changes.</summary>
    [Parameter] public EventCallback<SgLlmConfig> ConfigChanged { get; set; }
    /// <summary>Custom inline styles applied to the settings panel.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// When true, render the one-row inline editor (provider chip + model + key)
    /// with a popover holding the rest. Designed to live in a chat toolbar.
    /// </summary>
    [Parameter] public bool Compact { get; set; }
    [Parameter] public string TaskPurpose { get; set; } = SgLlmTaskPurpose.Chat;

    private bool _manualModel;
    private bool _showLogs;
    private string _lastStatus = "None";
    private string _statusText = "";
    private readonly List<LogEntry> _logs = new();
    private SgBadgeVariant StatusBadgeVariant => _lastStatus == "Success" ? SgBadgeVariant.Success : SgBadgeVariant.Danger;
    private string StatusBadgeText => _lastStatus == "Success" ? Localizer["Llm_Connected"] : Localizer["Llm_NotConnected"];

    private bool _loadingModels;
    private readonly List<SgLlmProvider> _providers = SgLlmProviderRegistry.AllowedProviders.ToList();

    private List<SgLlmModelInfo> _models = new();
    private List<string> _modelIds = new();
    private bool _filterFreeOnly;
    private bool _filterVisionOnly;
    private bool _filterToolsOnly;
    private bool _filterJsonOnly;
    private bool _filterReasoningOnly;

    // Stage 5 — model catalog UX
    private string _modelSearchText = string.Empty;
    private bool _catalogExpanded;
    private int _catalogTop = 25;
    private string _modelSort = "recommended"; // recommended | context | name
    private string? _selectedFamily;
    private List<SgLlmProfile> _profiles = new();
    private string? _selectedProfileId;
    private string _profileName = "";
    private bool _profileDefault;
    private SgLlmDiagnosticsResult? _diagnostics;
    private bool _checkingConnection;
    private string? _profilesJson;
    private List<SgLlmUsageRecord> _usageRecords = new();
    private List<SgLlmHealthStatus> _healthStatuses = new();
    private bool _checkingHealth;
    private int TodayTokens => _usageRecords.Where(u => u.Timestamp.ToLocalTime().Date == DateTime.Now.Date).Sum(u => u.TotalTokens);
    private string DailyLimitText => Config.DailyTokenLimit is > 0
        ? $"{TodayTokens:N0} / {Config.DailyTokenLimit:N0} today"
        : $"{TodayTokens:N0} today";
    private string _lastAppliedFingerprint = "";

    private IEnumerable<SgLlmModelInfo> RecommendedModels =>
        (_models.Count > 0 ? _models : SgLlmProviderRegistry.FallbackModels(Config.Provider))
        .Where(m => (!Config.OnlyFreeModels || IsModelFree(m)) && (m.IsRecommended || m.IsFree))
        .Take(6);

    private bool HasDirtyChanges => Fingerprint(Config) != _lastAppliedFingerprint;
    private SgLlmModelInfo? SelectedModelInfo => _models.FirstOrDefault(m => string.Equals(m.Id, Config.ModelId, StringComparison.OrdinalIgnoreCase));
    private static bool IsModelFree(SgLlmModelInfo m) => m.IsFree || m.Id.Contains(":free", StringComparison.OrdinalIgnoreCase) || m.Provider is SgLlmProvider.Ollama or SgLlmProvider.LmStudio;
    private bool SelectedModelLooksPaid => SelectedModelInfo is { } m && !IsModelFree(m) && Config.Provider is not (SgLlmProvider.Ollama or SgLlmProvider.LmStudio);
    private List<string> FilteredModelIds
    {
        get
        {
            if (_models.Count == 0) return _modelIds;
            return FilteredModels
                .Select(m => m.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    /// <summary>Models after applying free/vision/tools/json/reasoning filters, family filter, free-text search and sort.</summary>
    private IEnumerable<SgLlmModelInfo> FilteredModels
    {
        get
        {
            if (_models.Count == 0) return Enumerable.Empty<SgLlmModelInfo>();
            var q = _models.AsEnumerable()
                .Where(m => (!Config.OnlyFreeModels && !_filterFreeOnly) || IsModelFree(m))
                .Where(m => !_filterVisionOnly || m.SupportsVision)
                .Where(m => !_filterToolsOnly || m.SupportsTools)
                .Where(m => !_filterJsonOnly || m.SupportsJsonSchema)
                .Where(m => !_filterReasoningOnly || m.SupportsReasoning);

            if (_selectedFamily != null)
                q = q.Where(m => ExtractFamily(m.Id) == _selectedFamily);

            if (!string.IsNullOrWhiteSpace(_modelSearchText))
            {
                var s = _modelSearchText.Trim();
                q = q.Where(m =>
                    m.Id.Contains(s, StringComparison.OrdinalIgnoreCase)
                    || m.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
                    || (m.Description?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return _modelSort switch
            {
                "context" => q.OrderByDescending(m => m.ContextWindow ?? 0).ThenBy(m => m.Name),
                "name" => q.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase),
                _ => q
                    .OrderByDescending(m => m.IsRecommended)
                    .ThenByDescending(m => m.IsFree)
                    .ThenByDescending(m => m.ContextWindow ?? 0)
                    .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            };
        }
    }

    /// <summary>Distinct families present in the loaded model list, with counts (for the family chip bar).</summary>
    private IReadOnlyList<(string Family, int Count)> ModelFamilies =>
        _models
            .Select(m => ExtractFamily(m.Id))
            .Where(f => !string.IsNullOrEmpty(f))
            .GroupBy(f => f!, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(t => t.Item2)
            .ToList();

    /// <summary>Best-effort family extraction from a model id (gpt, claude, gemini, llama, qwen, deepseek, mistral, gemma, kimi, phi, grok, command).</summary>
    private static string? ExtractFamily(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var lower = id.ToLowerInvariant();
        // Strip namespace prefix (openai/, anthropic/, meta-llama/...)
        var slash = lower.LastIndexOf('/');
        if (slash >= 0 && slash < lower.Length - 1) lower = lower[(slash + 1)..];

        static string? Find(string s, params string[] families)
        {
            foreach (var f in families)
                if (s.Contains(f, StringComparison.Ordinal)) return f;
            return null;
        }

        return Find(lower,
            "gpt-oss", "gpt", "claude", "gemini", "gemma",
            "llama", "qwen", "deepseek", "mistral", "mixtral",
            "kimi", "phi", "grok", "command", "yi", "nemotron",
            "minimax", "glm", "sonar", "voyage", "jina", "nomic", "whisper", "nova", "aura");
    }

    /// <summary>Reset Top-N counter when filters or search change so the user sees the first page.</summary>
    private void OnCatalogFilterChanged()
    {
        _catalogTop = 25;
    }

    /// <summary>Show 25 more rows in the catalog.</summary>
    private void LoadMoreCatalogRows()
    {
        _catalogTop += 25;
    }

    /// <summary>Toggle between Top-N and "show all".</summary>
    private void ToggleCatalogExpanded()
    {
        _catalogExpanded = !_catalogExpanded;
        if (!_catalogExpanded) _catalogTop = 25;
    }

    private void ToggleFamily(string family)
    {
        _selectedFamily = _selectedFamily == family ? null : family;
        OnCatalogFilterChanged();
    }

    private void ClearModelFilters()
    {
        _modelSearchText = string.Empty;
        _filterFreeOnly = _filterVisionOnly = _filterToolsOnly = _filterJsonOnly = _filterReasoningOnly = false;
        _selectedFamily = null;
        OnCatalogFilterChanged();
    }

    /// <summary>True when any model filter narrows down the visible list.</summary>
    private bool HasModelFilters =>
        !string.IsNullOrWhiteSpace(_modelSearchText)
        || _filterFreeOnly || _filterVisionOnly || _filterToolsOnly
        || _filterJsonOnly || _filterReasoningOnly
        || _selectedFamily != null;

    /// <summary>True when the connection is set but no models are loaded — surface a CTA.</summary>
    private bool NeedsConnectionForModels =>
        _models.Count == 0
        && Config.Provider != SgLlmProvider.None
        && (Config.Provider != SgLlmProvider.Ollama || string.IsNullOrWhiteSpace(Config.BaseUrl) || !_loadingModels);

    /// <summary>Human-readable hint for why the catalog might be empty.</summary>
    private string EmptyCatalogHint
    {
        get
        {
            var p = Config.Provider;
            if (p == SgLlmProvider.None) return Localizer["Llm_SelectProviderHint"];
            if (p == SgLlmProvider.Ollama)
                return Localizer["Llm_OllamaHint"];
            if (SgLlmProviderRegistry.GetPreset(p) is { Category: SgLlmProviderCategory.Local })
                return Localizer["Llm_LocalFilter"];
            if (SgLlmProviderRegistry.RequiresKey(p) && string.IsNullOrWhiteSpace(Config.ApiKey))
                return Localizer["Llm_NoApiKey"];
            return Localizer["Llm_CorsHint"];
        }
    }

    private readonly List<string> _routePurposes = new()
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
    };

    // Helpers for boolean nullable bindings
    private bool _logProbsBool
    {
        get => Config.LogProbs ?? false;
        set => Config.LogProbs = value ? true : null;
    }
    private bool _parallelToolCallsBool
    {
        get => Config.ParallelToolCalls ?? false;
        set => Config.ParallelToolCalls = value ? true : null;
    }
    private bool _streamUsageBool
    {
        get => Config.StreamUsage ?? false;
        set => Config.StreamUsage = value ? true : null;
    }
    private bool _anthropicThinkingBool
    {
        get => Config.AnthropicThinking ?? false;
        set => Config.AnthropicThinking = value ? true : null;
    }
    private bool _geminiIncludeThoughtsBool
    {
        get => Config.GeminiIncludeThoughts ?? false;
        set => Config.GeminiIncludeThoughts = value ? true : null;
    }
    private bool _orRequireParametersBool
    {
        get => Config.OrRequireParameters ?? false;
        set => Config.OrRequireParameters = value ? true : null;
    }
    private bool _orAllowDataCollectionBool
    {
        get => Config.OrAllowDataCollection ?? false;
        set => Config.OrAllowDataCollection = value ? true : null;
    }
    private bool _useResponsesApiBool
    {
        get => Config.UseResponsesApi ?? false;
        set => Config.UseResponsesApi = value ? true : null;
    }

    // String <-> List<string> bindings
    private string _stopText => Config.Stop is null ? "" : string.Join(", ", Config.Stop);
    private void OnStopChanged(string? v) => Config.Stop = SplitCsv(v);

    private string _orFallbackText => Config.OrFallbackModels is null ? "" : string.Join(", ", Config.OrFallbackModels);
    private void OnOrFallbackChanged(string? v) => Config.OrFallbackModels = SplitCsv(v);

    private string _orAllowedText => Config.OrAllowedProviders is null ? "" : string.Join(", ", Config.OrAllowedProviders);
    private void OnOrAllowedChanged(string? v) => Config.OrAllowedProviders = SplitCsv(v);

    private string _orIgnoredText => Config.OrIgnoredProviders is null ? "" : string.Join(", ", Config.OrIgnoredProviders);
    private void OnOrIgnoredChanged(string? v) => Config.OrIgnoredProviders = SplitCsv(v);

    private static List<string>? SplitCsv(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        var list = v.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        return list.Count == 0 ? null : list;
    }

    private readonly List<string> _responseFormats = new() { "text", "json_object", "json_schema" };
    private readonly List<string> _effortLevels = new() { "minimal", "low", "medium", "high" };
    private readonly List<string> _verbosityLevels = new() { "low", "medium", "high" };
    private readonly List<string> _geminiSafetyLevels = new() { "BLOCK_NONE", "BLOCK_ONLY_HIGH", "BLOCK_MEDIUM_AND_ABOVE", "BLOCK_LOW_AND_ABOVE" };
    private readonly List<string> _orSortOptions = new() { "fallback", "lowest-price", "highest-throughput", "fastest", "price", "throughput", "latency" };
    private readonly List<string> _orTransforms = new() { "middle-out" };
    private readonly List<string> _serviceTiers = new() { "auto", "default", "flex", "priority", "scale" };
    private readonly List<string> _gigaAuthModes = new() { "Bearer", "OAuth" };
    private readonly List<string> _gigaScopes = new() { "GIGACHAT_API_PERS", "GIGACHAT_API_B2B", "GIGACHAT_API_CORP" };

    private static bool NeedsBaseUrl(SgLlmProvider p) => SgLlmProviderRegistry.NeedsBaseUrl(p);
    private static bool ProviderRequiresKey(SgLlmProvider p) => SgLlmProviderRegistry.RequiresKey(p);
    private static bool SupportsPenalties(SgLlmProvider p) => SgLlmProviderRegistry.SupportsPenalties(p);
    private static bool SupportsTopKMinP(SgLlmProvider p) => SgLlmProviderRegistry.SupportsTopKMinP(p);
    private static bool SupportsReasoning(SgLlmProvider p) => SgLlmProviderRegistry.SupportsReasoning(p);
    private static bool SupportsServiceTier(SgLlmProvider p) => SgLlmProviderRegistry.SupportsServiceTier(p);
    private static string DefaultBaseUrl(SgLlmProvider p) => SgLlmProviderRegistry.DefaultBaseUrl(p);
    private static string ProviderLabel(SgLlmProvider p) => p == SgLlmProvider.None ? "None" : SgLlmProviderRegistry.Label(p);
    private static string ProviderShortHint(SgLlmProvider p) => SgLlmProviderRegistry.ShortHint(p);
    private static string ProviderConnectionHint(SgLlmProvider p) => SgLlmProviderRegistry.ConnectionHint(p);

    private static bool IsLocalProvider(SgLlmProvider p) =>
        SgLlmProviderRegistry.GetPreset(p)?.Category == SgLlmProviderCategory.Local
        && p != SgLlmProvider.WebLlm; // WebLlm runs in-browser, no port to test.

    private static string LocalCorsHint(SgLlmProvider p) => p switch
    {
        SgLlmProvider.Ollama => "Ollama: set OLLAMA_ORIGINS to include your origin (e.g. http://localhost:5000).",
        SgLlmProvider.LmStudio => "LM Studio: enable CORS in Local Server → Settings.",
        SgLlmProvider.Vllm => "vLLM: start with --allow-cors, otherwise the browser blocks requests.",
        SgLlmProvider.LlamaCpp => "llama.cpp server: use --api-cors-allow * (or specify your origin).",
        SgLlmProvider.KoboldCpp => "KoboldCpp: start with --openai-compatibility, otherwise chat routes won't work.",
        SgLlmProvider.Jan => "Jan: enable API server in Settings → Advanced → API.",
        _ => "If CORS blocks requests, check your local server CORS configuration."
    };

    private Action? _localeChangedHandler;

    private bool _testingLocalPort;
    private bool _localPortOk;
    private string? _localPortStatus;

    private async Task TestLocalPortAsync()
    {
        _testingLocalPort = true;
        _localPortStatus = null;
        try
        {
            var result = await LlmService.TestConnectionAsync(Config);
            _localPortOk = result.Ok;
            _localPortStatus = result.Ok
                ? $"OK · HTTP {result.Status}"
                : $"Unreachable · {(result.Status > 0 ? "HTTP " + result.Status + " · " : "")}{result.Message}";
        }
        catch (Exception ex)
        {
            _localPortOk = false;
            _localPortStatus = $"Error: {ex.Message}";
        }
        finally
        {
            _testingLocalPort = false;
        }
    }

    private static string Fingerprint(SgLlmConfig config)
    {
        var copy = config.Clone();
        copy.ApiKey = string.IsNullOrWhiteSpace(copy.ApiKey) ? "" : "***";
        return System.Text.Json.JsonSerializer.Serialize(copy);
    }

    private static SgLlmConfig CopyConfig(SgLlmConfig source)
    {
        return source.Clone();
    }

    private string RawRequestPreview
    {
        get
        {
            var payload = new Dictionary<string, object?>
            {
                ["taskPurpose"] = TaskPurpose,
                ["provider"] = ProviderLabel(Config.Provider),
                ["url"] = Config.Provider == SgLlmProvider.Anthropic
                    ? $"{Config.BaseUrl?.TrimEnd('/')}/messages"
                    : Config.Provider == SgLlmProvider.Ollama
                        ? $"{Config.BaseUrl?.TrimEnd('/')}/api/chat"
                        : Config.Provider == SgLlmProvider.OpenAiCompatible && Config.UseResponsesApi == true
                            ? $"{Config.BaseUrl?.TrimEnd('/')}/responses"
                            : $"{Config.BaseUrl?.TrimEnd('/')}/chat/completions",
                ["model"] = Config.ModelId,
                ["stream"] = Config.Stream,
                ["temperature"] = Config.UseAdvanced ? Config.Temperature : null,
                ["top_p"] = Config.UseAdvanced ? Config.TopP : null,
                ["max_tokens"] = Config.UseAdvanced ? Config.MaxTokens : null,
                ["response_format"] = Config.UseAdvanced ? Config.ResponseFormat : null,
                ["timeoutSeconds"] = Config.TimeoutSeconds,
                ["retryCount"] = Config.RetryCount,
                ["useBackendProxy"] = Config.UseBackendProxy,
                ["proxyUrl"] = Config.ProxyUrl,
                ["gigaAuthMode"] = Config.Provider == SgLlmProvider.GigaGpt ? Config.GigaAuthMode : null,
                ["gigaScope"] = Config.Provider == SgLlmProvider.GigaGpt ? Config.GigaScope : null,
                ["useResponsesApi"] = Config.Provider == SgLlmProvider.OpenAiCompatible ? Config.UseResponsesApi : null
            };
            return System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
    }

    private void AddLog(string message, string type = "Info")
    {
        _logs.Insert(0, new LogEntry(DateTime.Now, message, type));
        if (_logs.Count > 50) _logs.RemoveAt(_logs.Count - 1);
        StateHasChanged();
    }

    private void OnServiceError(string msg)
    {
        AddLog($"LLM error: {msg}", "Error");
        _lastStatus = "Error";
        _statusText = Localizer["Llm_Error"];
        if (!_showLogs) _showLogs = true;
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_localeChangedHandler is not null)
            Localizer.OnLocaleChanged -= _localeChangedHandler;
        LlmService.OnError -= OnServiceError;
    }

    private void EnsureBaseUrl()
    {
        if (!_providers.Contains(Config.Provider))
        {
            Config.Provider = SgLlmProvider.OpenRouter;
            Config.ModelId = null;
            Config.BaseUrl = DefaultBaseUrl(Config.Provider);
        }

        if (string.Equals(Config.ModelId, "google/gemini-2.0-flash-001:free", StringComparison.OrdinalIgnoreCase))
        {
            Config.ModelId = null;
        }

        if (Config.Provider != SgLlmProvider.None)
        {
            Config.BaseUrl = SgLlmProviderRegistry.NormalizeBaseUrl(Config.Provider, Config.BaseUrl);
        }
    }

    private async Task OnBaseUrlChanged(string? value)
    {
        var detected = SgLlmProviderRegistry.DetectProvider(value);
        if (detected != SgLlmProvider.None && detected != Config.Provider && _providers.Contains(detected))
        {
            Config.Provider = detected;
            Config.ModelId = null;
        }

        Config.BaseUrl = SgLlmProviderRegistry.NormalizeBaseUrl(Config.Provider, value);
        await LoadModelsAsync();
    }

    private async Task SelectRecommendedModel(string modelId)
    {
        Config.ModelId = modelId;
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnInitializedAsync()
    {
        _localeChangedHandler = () => { try { InvokeAsync(StateHasChanged); } catch { } };
        Localizer.OnLocaleChanged += _localeChangedHandler;

        LlmService.OnError += OnServiceError;

        if (LlmService.CurrentConfig != null && string.IsNullOrEmpty(Config.ModelId))
        {
            Config.UpdateFrom(LlmService.CurrentConfig);
            _lastStatus = "Success";
            _statusText = Localizer["Llm_LoadedFromService"];
        }
        else
        {
            _statusText = Localizer["Llm_Ready"];
        }

        EnsureBaseUrl();
        await LoadProfilesAsync();
        await LoadModelsAsync();
        await LoadUsageAsync();
        _lastAppliedFingerprint = Fingerprint(Config);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Config != null)
        {
            EnsureBaseUrl();
            _lastAppliedFingerprint = Fingerprint(Config);
        }
    }

    private async Task OnProviderChanged(SgLlmProvider provider)
    {
        Config.Provider = provider;
        Config.BaseUrl = DefaultBaseUrl(provider);
        Config.ModelId = null;
        _models = SgLlmProviderRegistry.FallbackModels(provider);
        _modelIds = _models.Select(m => m.Id).ToList();
        if (!ProviderRequiresKey(provider)) Config.ApiKey = string.Empty;
        AddLog($"Switch provider → {ProviderLabel(provider)}");
        await LoadModelsAsync();
    }

    private void OnModelSelected(string modelId)
    {
        Config.ModelId = modelId;
        StateHasChanged();
    }

    private async Task LoadModelsAsync()
    {
        if (Config.Provider == SgLlmProvider.None) return;
        EnsureBaseUrl();

        _loadingModels = true;
        _lastStatus = "Pending";
        _statusText = Localizer["Llm_LoadingModels"];
        AddLog($"Fetch models for {Config.Provider}…");

        try
        {
            List<SgLlmModelInfo>? models = Config.Provider switch
            {
                // --- Frontier ---
                SgLlmProvider.OpenAiCompatible => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Anthropic => await LlmService.GetAnthropicModelsAsync(Config.ApiKey),
                SgLlmProvider.Google => await LlmService.GetGoogleModelsAsync(Config.ApiKey),
                SgLlmProvider.XAi => await LlmService.GetXAiModelsAsync(Config.ApiKey),

                // --- Open routing ---
                SgLlmProvider.OpenRouter => await LlmService.GetOpenRouterModelsAsync(),
                SgLlmProvider.TogetherAi => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Fireworks => await LlmService.GetFireworksModelsAsync(Config.ApiKey),
                SgLlmProvider.HuggingFace => await LlmService.GetHuggingFaceModelsAsync(Config.ApiKey),
                SgLlmProvider.Replicate => SgLlmProviderRegistry.FallbackModels(Config.Provider),
                SgLlmProvider.AiMlApi => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Novita => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),

                // --- Fast inference ---
                SgLlmProvider.Groq => await LlmService.GetGroqModelsAsync(Config.ApiKey),
                SgLlmProvider.Cerebras => await LlmService.GetCerebrasModelsAsync(Config.ApiKey),
                SgLlmProvider.SambaNova => await LlmService.GetSambaNovaModelsAsync(Config.ApiKey),
                SgLlmProvider.DeepSeek => await LlmService.GetDeepSeekModelsAsync(Config.ApiKey),
                SgLlmProvider.Lepton => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.DeepInfra => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),

                // --- Local (all OpenAI-compatible) ---
                SgLlmProvider.LmStudio => await LlmService.GetLmStudioModelsAsync(Config.BaseUrl),
                SgLlmProvider.Vllm => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.LlamaCpp => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Jan => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Gpt4All => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.KoboldCpp => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.OobaboogaTgWebUi => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.TabbyApi => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Llamafile => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.WebLlm => SgLlmProviderRegistry.FallbackModels(Config.Provider),

                // --- Free ---
                SgLlmProvider.CloudflareWorkersAi => await LlmService.GetCloudflareWorkersAiModelsAsync(null, Config.ApiKey),
                SgLlmProvider.GitHubModels => await LlmService.GetGitHubModelsAsync(Config.ApiKey),
                SgLlmProvider.Pollinations => await LlmService.GetPollinationsModelsAsync(),
                SgLlmProvider.GlhfChat => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Targon => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Chutes => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),

                // --- Russian ---
                SgLlmProvider.GigaGpt => await LlmService.GetGigaGptModelsAsync(Config.BaseUrl, Config.ApiKey, Config.GigaAuthMode, Config.GigaScope, Config.GigaOAuthUrl),
                SgLlmProvider.YandexGpt => SgLlmProviderRegistry.FallbackModels(Config.Provider),

                // --- Specialty ---
                SgLlmProvider.Cohere => await LlmService.GetCohereModelsAsync(Config.ApiKey),
                SgLlmProvider.Mistral => await LlmService.GetMistralModelsAsync(Config.ApiKey),
                SgLlmProvider.Perplexity => await LlmService.GetPerplexityModelsAsync(Config.ApiKey),
                SgLlmProvider.VoyageAi => SgLlmProviderRegistry.FallbackModels(Config.Provider),
                SgLlmProvider.JinaAi => SgLlmProviderRegistry.FallbackModels(Config.Provider),
                SgLlmProvider.Nomic => SgLlmProviderRegistry.FallbackModels(Config.Provider),
                SgLlmProvider.AssemblyAi => SgLlmProviderRegistry.FallbackModels(Config.Provider),
                SgLlmProvider.Deepgram => SgLlmProviderRegistry.FallbackModels(Config.Provider),
                SgLlmProvider.ElevenLabs => SgLlmProviderRegistry.FallbackModels(Config.Provider),
                SgLlmProvider.OpenAiCompatibleCustom => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),

                // --- Azure ---
                SgLlmProvider.AzureOpenAi => SgLlmProviderRegistry.FallbackModels(Config.Provider),

                // Ollama is handled below via the native /api/tags endpoint
                SgLlmProvider.Ollama => null,
                _ => null
            };

            if (Config.Provider == SgLlmProvider.Ollama)
            {
                var ollamaModels = await LlmService.GetOllamaModelsAsync(Config.BaseUrl);
                _models = ollamaModels.Select(m => new SgLlmModelInfo { Id = m.Name, Name = m.Name, Provider = Config.Provider, ProviderLabel = ProviderLabel(Config.Provider), IsFree = true }).Where(m => !string.IsNullOrWhiteSpace(m.Id)).OrderBy(m => m.Id).ToList();
                if (_models.Count == 0) _models = SgLlmProviderRegistry.FallbackModels(Config.Provider);
                _modelIds = _models.Select(m => m.Id).ToList();
                AddLog($"Loaded {_modelIds.Count} models (Ollama)", "Success");
            }
            else if (models != null)
            {
                _models = models.Where(m => !string.IsNullOrWhiteSpace(m.Id)).ToList();
                foreach (var m in _models)
                {
                    if (m.Provider == default) m.Provider = Config.Provider;
                    m.ProviderLabel ??= ProviderLabel(Config.Provider);
                }
                if (_models.Count == 0) _models = SgLlmProviderRegistry.FallbackModels(Config.Provider);
                _modelIds = _models.Select(m => m.Id).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id).ToList();
                AddLog($"Loaded {_modelIds.Count} models ({ProviderLabel(Config.Provider)})", "Success");
            }
            else
            {
                _models = SgLlmProviderRegistry.FallbackModels(Config.Provider);
                _modelIds = _models.Select(m => m.Id).ToList();
            }

            if (Config.OnlyFreeModels)
            {
                _models = _models.Where(IsModelFree).ToList();
                _modelIds = _models.Select(m => m.Id).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id).ToList();
            }

            if (_modelIds.Any())
            {
                if (string.IsNullOrWhiteSpace(Config.ModelId) || !_modelIds.Contains(Config.ModelId, StringComparer.OrdinalIgnoreCase))
                {
                    Config.ModelId = PickPreferredModelId();
                }

                _lastStatus = "Success";
                _statusText = Localizer["Llm_ModelsUpdated"];
            }
        }
        catch (Exception ex)
        {
            AddLog($"Error loading models: {ex.Message}", "Error");
            _models = SgLlmProviderRegistry.FallbackModels(Config.Provider);
            _modelIds = _models.Select(m => m.Id).ToList();
            _lastStatus = _modelIds.Count > 0 ? "Success" : "Error";
            _statusText = _modelIds.Count > 0 ? Localizer["Llm_ShowBuiltInModels"] : Localizer["Llm_LoadError"];
        }
        finally
        {
            _loadingModels = false;
            StateHasChanged();
        }
    }

    private static List<string> FallbackModelIds(SgLlmProvider provider) => SgLlmProviderRegistry.FallbackModels(provider).Select(m => m.Id).ToList();

    /// <summary>
    /// Returns the best default model for the current provider. Prefers a recommended
    /// model that survived the active filters, then any free model, then the first
    /// id from the registry fallback list — never auto-picks a hard-coded "gemini".
    /// </summary>
    private string? PickPreferredModelId()
    {
        var ids = FilteredModelIds;
        var fromApi = _models.FirstOrDefault(m => ids.Contains(m.Id) && m.IsRecommended)
                      ?? _models.FirstOrDefault(m => ids.Contains(m.Id) && IsModelFree(m))
                      ?? _models.FirstOrDefault(m => ids.Contains(m.Id));
        if (fromApi is not null) return fromApi.Id;

        var registry = SgLlmProviderRegistry.FallbackModels(Config.Provider);
        return registry.FirstOrDefault(m => m.IsRecommended)?.Id
               ?? registry.FirstOrDefault()?.Id;
    }

    private SgLlmRouteConfig GetRoute(string purpose)
    {
        Config.Routes ??= new Dictionary<string, SgLlmRouteConfig>(StringComparer.OrdinalIgnoreCase);
        if (!Config.Routes.TryGetValue(purpose, out var route) || route is null)
        {
            route = new SgLlmRouteConfig { Purpose = purpose };
            Config.Routes[purpose] = route;
        }
        return route;
    }

    private bool IsRouteEnabled(string purpose) => GetRoute(purpose).Enabled;
    private void SetRouteEnabled(string purpose, bool value) => GetRoute(purpose).Enabled = value;

    private string? GetRouteModel(string purpose) => GetRoute(purpose).ModelId;
    private void SetRouteModel(string purpose, string? value) => GetRoute(purpose).ModelId = value;
    private string? GetRouteBaseUrl(string purpose) => GetRoute(purpose).BaseUrl;
    private void SetRouteBaseUrl(string purpose, string? value) => GetRoute(purpose).BaseUrl = value;
    private SgLlmProvider GetRouteProvider(string purpose) => GetRoute(purpose).Provider ?? Config.Provider;
    private void SetRouteProvider(string purpose, SgLlmProvider value)
    {
        var route = GetRoute(purpose);
        route.Provider = value;
        if (string.IsNullOrWhiteSpace(route.BaseUrl)) route.BaseUrl = DefaultBaseUrl(value);
    }

    private string ProfileLabel(string id) => _profiles.FirstOrDefault(p => p.Id == id)?.Name ?? id;

    private async Task LoadProfilesAsync()
    {
        _profiles = await LlmService.GetProfilesAsync();
        var def = _profiles.FirstOrDefault(p => p.IsDefault);
        _selectedProfileId = def?.Id ?? _profiles.FirstOrDefault()?.Id;
        _profileName = ProviderLabel(Config.Provider) + " profile";
    }

    private async Task SaveProfileAsync()
    {
        var name = string.IsNullOrWhiteSpace(_profileName) ? $"{ProviderLabel(Config.Provider)} / {Config.ModelId}" : _profileName.Trim();
        var profile = new SgLlmProfile
        {
            Id = string.IsNullOrWhiteSpace(_selectedProfileId) ? Guid.NewGuid().ToString("N") : _selectedProfileId!,
            Name = name,
            Config = CopyConfig(Config),
            IsDefault = _profileDefault
        };
        await LlmService.SaveProfileAsync(profile);
        await LoadProfilesAsync();
        _selectedProfileId = profile.Id;
        AddLog($"Profile saved: {name}", "Success");
    }

    private async Task ApplyProfileAsync(string? profileId)
    {
        _selectedProfileId = profileId;
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return;
        Config = CopyConfig(profile.Config);
        EnsureBaseUrl();
        await ConfigChanged.InvokeAsync(Config);
        await LoadModelsAsync();
        _lastAppliedFingerprint = Fingerprint(Config);
        _profileName = profile.Name;
        _profileDefault = profile.IsDefault;
        AddLog($"Profile loaded: {profile.Name}", "Success");
    }

    private async Task DeleteProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedProfileId)) return;
        await LlmService.DeleteProfileAsync(_selectedProfileId);
        _selectedProfileId = null;
        await LoadProfilesAsync();
        AddLog("Profile deleted", "Success");
    }

    private async Task ExportProfilesAsync()
    {
        _profilesJson = await LlmService.ExportProfilesJsonAsync();
    }

    private async Task ImportProfilesAsync()
    {
        if (string.IsNullOrWhiteSpace(_profilesJson)) return;
        await LlmService.ImportProfilesJsonAsync(_profilesJson);
        await LoadProfilesAsync();
        AddLog("Profiles imported", "Success");
    }

    private async Task LoadUsageAsync()
    {
        _usageRecords = await LlmService.GetUsageRecordsAsync();
    }

    private async Task ClearUsageAsync()
    {
        await LlmService.ClearUsageRecordsAsync();
        _usageRecords.Clear();
        AddLog("Usage log cleared", "Success");
    }

    private async Task CheckAllProvidersHealthAsync()
    {
        _checkingHealth = true;
        try
        {
            _healthStatuses = await LlmService.CheckProvidersHealthAsync(Config);
            AddLog("Provider health checked", _healthStatuses.All(h => h.Ok) ? "Success" : "Info");
        }
        catch (Exception ex)
        {
            AddLog($"Health check error: {ex.Message}", "Error");
        }
        finally
        {
            _checkingHealth = false;
        }
    }

    private async Task CheckConnectionAsync()
    {
        _checkingConnection = true;
        _lastStatus = "Pending";
        _statusText = Localizer["Llm_Checking"];
        try
        {
            EnsureBaseUrl();
            _diagnostics = await LlmService.TestFullConnectionAsync(Config);
            _lastStatus = _diagnostics.Ok ? "Success" : "Error";
            _statusText = _diagnostics.Summary;
            AddLog($"Diagnostics: {_diagnostics.Summary}", _diagnostics.Ok ? "Success" : "Error");
        }
        catch (Exception ex)
        {
            _lastStatus = "Error";
            _statusText = ex.Message;
            AddLog($"Diagnostics error: {ex.Message}", "Error");
        }
        finally
        {
            _checkingConnection = false;
        }
    }

    private async Task ApplySettingsAsync()
    {
        _lastStatus = "Pending";
        _statusText = Localizer["Llm_Checking"];
        EnsureBaseUrl();
        if (string.IsNullOrWhiteSpace(Config.ModelId))
        {
            _lastStatus = "Error";
            _statusText = Localizer["Llm_ModelNotSelected"];
            AddLog("Apply stopped: model is empty", "Error");
            return;
        }

        if (Config.OnlyFreeModels && SelectedModelLooksPaid)
        {
            _lastStatus = "Error";
            _statusText = Localizer["Llm_WarnPaidModels"];
            AddLog("Apply stopped: paid model blocked by cost guard", "Error");
            return;
        }
        if (Config.DailyTokenLimit is > 0 && TodayTokens >= Config.DailyTokenLimit.Value)
        {
            _lastStatus = "Error";
            _statusText = Localizer["Llm_DailyTokenLimit"];
            AddLog("Apply stopped: daily token limit reached", "Error");
            return;
        }
        if (Config.RequestTokenLimit is > 0 && (Config.MaxTokens is null || Config.MaxTokens > Config.RequestTokenLimit))
        {
            Config.MaxTokens = Config.RequestTokenLimit;
        }

        AddLog($"Apply: {ProviderLabel(Config.Provider)} / {Config.ModelId} (advanced={Config.UseAdvanced})");

        try
        {
            // 1) Probe the provider endpoint with the supplied key. This catches
            //    401/403/etc. up-front rather than waiting for the first message.
            var effectiveConfig = LlmService.ResolveConfigForTask(TaskPurpose, Config);
            var probe = await LlmService.TestConnectionAsync(effectiveConfig);
            if (probe.Ok)
            {
                AddLog($"Connection check: {probe.Message}", "Success");
            }
            else
            {
                AddLog($"Connection check failed: {probe.Message}", "Error");
                if (probe.Status == 401 || probe.Status == 403)
                {
                    _lastStatus = "Error";
                    _statusText = $"{probe.Status}: API key rejected";
                    if (!_showLogs) _showLogs = true;
                    return;
                }
                // Non-auth failures (e.g. CORS, network) we surface but still try to
                // initialize — some providers don't expose /models from the browser.
            }

            // 2) Initialize the JS-side engine and notify parent.
            await LlmService.InitializeAsync(effectiveConfig);
            await ConfigChanged.InvokeAsync(Config);
            _lastAppliedFingerprint = Fingerprint(Config);

            _lastStatus = "Success";
            _statusText = Localizer["Llm_Connected"];
            await LoadUsageAsync();
            AddLog("Settings applied", "Success");
        }
        catch (Exception ex)
        {
            _lastStatus = "Error";
            _statusText = Localizer["Llm_Error"];
            AddLog($"Apply error: {ex.Message}", "Error");
            if (!_showLogs) _showLogs = true;
        }
    }

    private record LogEntry(DateTime Time, string Message, string Type);
}
