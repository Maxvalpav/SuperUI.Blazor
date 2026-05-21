using Microsoft.AspNetCore.Components;
using SuperUI.Localization;
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

    private bool _manualModel;
    private bool _showLogs;
    private string _lastStatus = "None";
    private string _statusText = "Готов к подключению";
    private readonly List<LogEntry> _logs = new();

    private bool _loadingModels;
    private readonly List<SgLlmProvider> _providers = Enum.GetValues<SgLlmProvider>().ToList();
    private List<string> _orModelIds = new() { "openrouter/free" };

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
    private readonly List<string> _orSortOptions = new() { "price", "throughput", "latency" };
    private readonly List<string> _orTransforms = new() { "middle-out" };
    private readonly List<string> _serviceTiers = new() { "auto", "default", "flex", "priority", "scale" };

    private static bool NeedsBaseUrl(SgLlmProvider p) =>
        p is SgLlmProvider.OpenAiCompatible
            or SgLlmProvider.OpenRouter
            or SgLlmProvider.OpenCode
            or SgLlmProvider.Ollama
            or SgLlmProvider.Mistral
            or SgLlmProvider.Groq
            or SgLlmProvider.DeepSeek
            or SgLlmProvider.XAi
            or SgLlmProvider.Cohere
            or SgLlmProvider.Perplexity
            or SgLlmProvider.TogetherAi
            or SgLlmProvider.Fireworks
            or SgLlmProvider.Cerebras
            or SgLlmProvider.HuggingFace
            or SgLlmProvider.AzureOpenAi
            or SgLlmProvider.Anthropic
            or SgLlmProvider.Google;

    private static bool SupportsPenalties(SgLlmProvider p) =>
        p is SgLlmProvider.OpenAiCompatible
            or SgLlmProvider.OpenRouter
            or SgLlmProvider.OpenCode
            or SgLlmProvider.Mistral
            or SgLlmProvider.Groq
            or SgLlmProvider.DeepSeek
            or SgLlmProvider.XAi
            or SgLlmProvider.TogetherAi
            or SgLlmProvider.Fireworks
            or SgLlmProvider.Ollama
            or SgLlmProvider.AzureOpenAi;

    private static bool SupportsTopKMinP(SgLlmProvider p) =>
        p is SgLlmProvider.OpenRouter
            or SgLlmProvider.Ollama
            or SgLlmProvider.Anthropic
            or SgLlmProvider.Google
            or SgLlmProvider.TogetherAi
            or SgLlmProvider.Fireworks
            or SgLlmProvider.Cohere;

    private static bool SupportsReasoning(SgLlmProvider p) =>
        p is SgLlmProvider.OpenAiCompatible
            or SgLlmProvider.AzureOpenAi
            or SgLlmProvider.OpenRouter
            or SgLlmProvider.DeepSeek
            or SgLlmProvider.XAi;

    private static bool SupportsServiceTier(SgLlmProvider p) =>
        p is SgLlmProvider.OpenAiCompatible
            or SgLlmProvider.AzureOpenAi
            or SgLlmProvider.Anthropic;

    private static string DefaultBaseUrl(SgLlmProvider p) => p switch
    {
        SgLlmProvider.OpenRouter => "https://openrouter.ai/api/v1",
        SgLlmProvider.OpenCode => "https://api.opencode.ai/v1",
        SgLlmProvider.Ollama => "http://localhost:11434",
        SgLlmProvider.OpenAiCompatible => "https://api.openai.com/v1",
        SgLlmProvider.Anthropic => "https://api.anthropic.com/v1",
        SgLlmProvider.Google => "https://generativelanguage.googleapis.com/v1beta",
        SgLlmProvider.Mistral => "https://api.mistral.ai/v1",
        SgLlmProvider.Groq => "https://api.groq.com/openai/v1",
        SgLlmProvider.DeepSeek => "https://api.deepseek.com",
        SgLlmProvider.XAi => "https://api.x.ai/v1",
        SgLlmProvider.Cohere => "https://api.cohere.ai/compatibility/v1",
        SgLlmProvider.Perplexity => "https://api.perplexity.ai",
        SgLlmProvider.TogetherAi => "https://api.together.xyz/v1",
        SgLlmProvider.Fireworks => "https://api.fireworks.ai/inference/v1",
        SgLlmProvider.Cerebras => "https://api.cerebras.ai/v1",
        SgLlmProvider.HuggingFace => "https://router.huggingface.co/v1",
        SgLlmProvider.AzureOpenAi => "https://YOUR_RESOURCE.openai.azure.com",
        _ => ""
    };

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

            _lastStatus = "Success";
            _statusText = Localizer["Llm_LoadedFromService"];
        }

        await LoadModelsAsync();
    }

    private async Task OnProviderChanged(SgLlmProvider provider)
    {
        Config.Provider = provider;
        Config.BaseUrl = DefaultBaseUrl(provider);
        AddLog($"Switch provider → {provider}");
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
                SgLlmProvider.OpenCode => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.AzureOpenAi => await LlmService.GetOpenAiModelsAsync(Config.BaseUrl, Config.ApiKey),
                SgLlmProvider.Anthropic => await LlmService.GetAnthropicModelsAsync(Config.ApiKey),
                SgLlmProvider.Google => await LlmService.GetGoogleModelsAsync(Config.ApiKey),
                SgLlmProvider.Mistral => await LlmService.GetMistralModelsAsync(Config.ApiKey),
                SgLlmProvider.Groq => await LlmService.GetGroqModelsAsync(Config.ApiKey),
                SgLlmProvider.DeepSeek => await LlmService.GetDeepSeekModelsAsync(Config.ApiKey),
                SgLlmProvider.XAi => await LlmService.GetXAiModelsAsync(Config.ApiKey),
                SgLlmProvider.Cohere => await LlmService.GetCohereModelsAsync(Config.ApiKey),
                SgLlmProvider.Perplexity => await LlmService.GetPerplexityModelsAsync(Config.ApiKey),
                SgLlmProvider.TogetherAi => await LlmService.GetTogetherModelsAsync(Config.ApiKey),
                SgLlmProvider.Fireworks => await LlmService.GetFireworksModelsAsync(Config.ApiKey),
                SgLlmProvider.Cerebras => await LlmService.GetCerebrasModelsAsync(Config.ApiKey),
                SgLlmProvider.HuggingFace => await LlmService.GetHuggingFaceModelsAsync(Config.ApiKey),
                SgLlmProvider.Ollama => null,
                _ => null
            };

            if (Config.Provider == SgLlmProvider.Ollama)
            {
                var ollamaModels = await LlmService.GetOllamaModelsAsync(Config.BaseUrl);
                _orModelIds = ollamaModels.Select(m => m.Name).OrderBy(id => id).ToList();
                AddLog($"Loaded {ollamaModels.Count} models (Ollama)", "Success");
            }
            else if (models != null)
            {
                _orModelIds = models.Select(m => m.Id).OrderBy(id => id).ToList();
                AddLog($"Loaded {models.Count} models ({Config.Provider})", "Success");
            }

            if (_orModelIds.Any())
            {
                _lastStatus = "Success";
                _statusText = Localizer["Llm_ModelsUpdated"];
            }
        }
        catch (Exception ex)
        {
            AddLog($"Error loading models: {ex.Message}", "Error");
            _lastStatus = "Error";
            _statusText = Localizer["Llm_LoadError"];
        }
        finally
        {
            _loadingModels = false;
            StateHasChanged();
        }
    }

    private async Task ApplySettingsAsync()
    {
        _lastStatus = "Pending";
        _statusText = Localizer["Llm_Checking"];
        AddLog($"Apply: {Config.Provider} / {Config.ModelId} (advanced={Config.UseAdvanced})");

        try
        {
            // 1) Probe the provider endpoint with the supplied key. This catches
            //    401/403/etc. up-front rather than waiting for the first message.
            var probe = await LlmService.TestConnectionAsync(Config);
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
            await LlmService.InitializeAsync(Config);
            await ConfigChanged.InvokeAsync(Config);

            _lastStatus = "Success";
            _statusText = Localizer["Llm_Connected"];
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
