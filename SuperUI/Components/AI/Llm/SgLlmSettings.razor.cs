using Microsoft.AspNetCore.Components;
using SuperUI.Localization;
using SuperUI.Enums;
using SuperUI.Services.Llm;

namespace SuperUI.Components;

public partial class SgLlmSettings : ComponentBase, IDisposable
{
    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

    [Parameter] public SgLlmConfig Config { get; set; } = new();
    [Parameter] public EventCallback<SgLlmConfig> ConfigChanged { get; set; }
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
    private string _statusText = "Готов к подключению";
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
    private string DailyLimitText => Config.DailyTokenLimit is > 0 ? $"{TodayTokens:N0} / {Config.DailyTokenLimit:N0} токенов сегодня" : $"{TodayTokens:N0} токенов сегодня";
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
            return _models
                .Where(m => (!Config.OnlyFreeModels && !_filterFreeOnly) || IsModelFree(m))
                .Where(m => !_filterVisionOnly || m.SupportsVision)
                .Where(m => !_filterToolsOnly || m.SupportsTools)
                .Where(m => !_filterJsonOnly || m.SupportsJsonSchema)
                .Where(m => !_filterReasoningOnly || m.SupportsReasoning)
                .Select(m => m.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id)
                .ToList();
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
    private static string ProviderLabel(SgLlmProvider p) => p == SgLlmProvider.None ? "Не выбран" : SgLlmProviderRegistry.Label(p);
    private static string ProviderShortHint(SgLlmProvider p) => SgLlmProviderRegistry.ShortHint(p);
    private static string ProviderConnectionHint(SgLlmProvider p) => SgLlmProviderRegistry.ConnectionHint(p);

    private static string Fingerprint(SgLlmConfig config)
    {
        var copy = CopyConfig(config);
        copy.ApiKey = string.IsNullOrWhiteSpace(copy.ApiKey) ? "" : "***";
        return System.Text.Json.JsonSerializer.Serialize(copy);
    }

    private static SgLlmConfig CopyConfig(SgLlmConfig source)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<SgLlmConfig>(json) ?? new();
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
        LlmService.OnError += OnServiceError;

        if (LlmService.CurrentConfig != null && string.IsNullOrEmpty(Config.ModelId))
        {
            var c = LlmService.CurrentConfig;
            Config.Provider = c.Provider;
            Config.ModelId = c.ModelId;
            Config.ApiKey = c.ApiKey;
            Config.BaseUrl = c.BaseUrl;
            Config.SystemPrompt = c.SystemPrompt;
            Config.Temperature = c.Temperature;
            Config.TopP = c.TopP;
            Config.MaxTokens = c.MaxTokens;
            Config.PresencePenalty = c.PresencePenalty;
            Config.FrequencyPenalty = c.FrequencyPenalty;
            Config.UseAdvanced = c.UseAdvanced;
            Config.Seed = c.Seed;
            Config.Stop = c.Stop;
            Config.TopK = c.TopK;
            Config.MinP = c.MinP;
            Config.RepetitionPenalty = c.RepetitionPenalty;
            Config.ResponseFormat = c.ResponseFormat;
            Config.JsonSchema = c.JsonSchema;
            Config.LogProbs = c.LogProbs;
            Config.TopLogProbs = c.TopLogProbs;
            Config.ParallelToolCalls = c.ParallelToolCalls;
            Config.StreamUsage = c.StreamUsage;
            Config.ReasoningEffort = c.ReasoningEffort;
            Config.Verbosity = c.Verbosity;
            Config.AnthropicThinking = c.AnthropicThinking;
            Config.AnthropicThinkingBudgetTokens = c.AnthropicThinkingBudgetTokens;
            Config.GeminiSafetyThreshold = c.GeminiSafetyThreshold;
            Config.GeminiThinkingBudget = c.GeminiThinkingBudget;
            Config.GeminiIncludeThoughts = c.GeminiIncludeThoughts;
            Config.OrFallbackModels = c.OrFallbackModels;
            Config.OrProviderSort = c.OrProviderSort;
            Config.OrAllowedProviders = c.OrAllowedProviders;
            Config.OrIgnoredProviders = c.OrIgnoredProviders;
            Config.OrRequireParameters = c.OrRequireParameters;
            Config.OrAllowDataCollection = c.OrAllowDataCollection;
            Config.OrTransforms = c.OrTransforms;
            Config.ServiceTier = c.ServiceTier;
            Config.AzureDeployment = c.AzureDeployment;
            Config.AzureApiVersion = c.AzureApiVersion;
            Config.UserIdentifier = c.UserIdentifier;
            Config.PersistApiKey = c.PersistApiKey;
            Config.UseBackendProxy = c.UseBackendProxy;
            Config.ProxyUrl = c.ProxyUrl;
            Config.TimeoutSeconds = c.TimeoutSeconds;
            Config.RetryCount = c.RetryCount;
            Config.RetryDelayMs = c.RetryDelayMs;
            Config.FallbackProvider = c.FallbackProvider;
            Config.FallbackBaseUrl = c.FallbackBaseUrl;
            Config.GigaAuthMode = c.GigaAuthMode;
            Config.GigaScope = c.GigaScope;
            Config.GigaOAuthUrl = c.GigaOAuthUrl;
            Config.UseResponsesApi = c.UseResponsesApi;
            Config.OnlyFreeModels = c.OnlyFreeModels;
            Config.WarnOnPaidModels = c.WarnOnPaidModels;
            Config.DailyTokenLimit = c.DailyTokenLimit;
            Config.RequestTokenLimit = c.RequestTokenLimit;

            _lastStatus = "Success";
            _statusText = Localizer["Llm_LoadedFromService"];
        }

        EnsureBaseUrl();
        await LoadProfilesAsync();
        await LoadModelsAsync();
        await LoadUsageAsync();
        _lastAppliedFingerprint = Fingerprint(Config);
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
                SgLlmProvider.OpenRouter => await LlmService.GetOpenRouterModelsAsync(),
                SgLlmProvider.OpenAiCompatible => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Anthropic => await LlmService.GetAnthropicModelsAsync(Config.ApiKey),
                SgLlmProvider.LmStudio => await LlmService.GetLmStudioModelsAsync(Config.BaseUrl),
                SgLlmProvider.HuggingFace => await LlmService.GetHuggingFaceModelsAsync(Config.ApiKey),
                SgLlmProvider.GigaGpt => await LlmService.GetGigaGptModelsAsync(Config.BaseUrl, Config.ApiKey, Config.GigaAuthMode, Config.GigaScope, Config.GigaOAuthUrl),
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
            _statusText = _modelIds.Count > 0 ? "Показаны встроенные модели" : Localizer["Llm_LoadError"];
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
        _statusText = "Проверка подключения…";
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
            _statusText = "Выберите модель";
            AddLog("Apply stopped: model is empty", "Error");
            return;
        }

        if (Config.OnlyFreeModels && SelectedModelLooksPaid)
        {
            _lastStatus = "Error";
            _statusText = "Выбрана paid-модель, а включён режим only-free";
            AddLog("Apply stopped: paid model blocked by cost guard", "Error");
            return;
        }
        if (Config.DailyTokenLimit is > 0 && TodayTokens >= Config.DailyTokenLimit.Value)
        {
            _lastStatus = "Error";
            _statusText = "Дневной лимит токенов исчерпан";
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
