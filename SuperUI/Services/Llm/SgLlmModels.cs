namespace SuperUI.Services.Llm;

public enum SgLlmProvider
{
    WebLlm,
    OpenAiCompatible,
    OpenRouter,
    Ollama,
    OpenCode,
    Anthropic,
    Google,        // Google Gemini (native v1beta)
    Mistral,       // Mistral AI (la Plateforme)
    Groq,          // Groq Cloud
    DeepSeek,      // DeepSeek Platform
    XAi,           // xAI (Grok)
    Cohere,        // Cohere v2
    Perplexity,    // Perplexity AI
    TogetherAi,    // Together AI
    Fireworks,     // Fireworks AI
    Cerebras,      // Cerebras Cloud
    AzureOpenAi,   // Azure OpenAI
    HuggingFace,   // HuggingFace Router (OpenAI-compatible)
    CloudflareWorkersAi, // Cloudflare Workers AI — free tier (Neurons)
    GitHubModels,        // GitHub Marketplace Models — free for devs
    SambaNova,           // SambaNova Cloud — free tier
    Pollinations,        // pollinations.ai — no key needed, free
    GlhfChat,            // glhf.chat — free OSS models, OpenAI-compatible
    Targon,              // targon.ai — free routing
    OpenAiCompatibleCustom, // explicit alias for arbitrary base URL
    None
}

/// <summary>Provider preset metadata used by the UI to pre-fill BaseUrl, hint URLs and labels.</summary>
public class SgLlmProviderPreset
{
    public SgLlmProvider Provider { get; set; }
    public string Label { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKeyUrl { get; set; }
    public string? DocsUrl { get; set; }
    public bool IsFree { get; set; }
    public bool RequiresKey { get; set; } = true;
    public string? Notes { get; set; }
}

public class SgLlmAttachment
{
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Base64 { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public bool IsPdf { get; set; }
    public bool IsVideo { get; set; }
    public bool IsText { get; set; }
}

public class SgLlmMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public List<SgLlmAttachment>? Attachments { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// LLM configuration spanning all supported providers and the union of their parameters.
/// Only "base" properties (Provider, ModelId, ApiKey, BaseUrl, SystemPrompt, Stream) are always sent.
/// Advanced properties are only forwarded to the provider when <see cref="UseAdvanced"/> is true.
/// </summary>
public class SgLlmConfig
{
    // --- Base / connection ---
    public SgLlmProvider Provider { get; set; } = SgLlmProvider.OpenRouter;
    public string? ModelId { get; set; }
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? SystemPrompt { get; set; }
    public bool Stream { get; set; } = true;
    public Dictionary<string, string>? ExtraHeaders { get; set; }

    /// <summary>
    /// Master switch. When false, only Provider/ModelId/ApiKey/BaseUrl/SystemPrompt
    /// reach the provider — every advanced field below is omitted from the request.
    /// </summary>
    public bool UseAdvanced { get; set; }

    // --- Sampling (most OpenAI-compatible providers) ---
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 1.0;
    public int? MaxTokens { get; set; }
    public double PresencePenalty { get; set; } = 0.0;
    public double FrequencyPenalty { get; set; } = 0.0;
    public int? Seed { get; set; }
    public List<string>? Stop { get; set; }

    // --- Extended sampling (Anthropic / Gemini / OpenRouter / vLLM-style) ---
    public int? TopK { get; set; }
    public double? MinP { get; set; }
    public double? RepetitionPenalty { get; set; }

    // --- Output controls ---
    /// <summary>"text" | "json_object" | "json_schema"</summary>
    public string? ResponseFormat { get; set; }
    public string? JsonSchema { get; set; }
    public bool? LogProbs { get; set; }
    public int? TopLogProbs { get; set; }
    public bool? ParallelToolCalls { get; set; }
    public bool? StreamUsage { get; set; }

    // --- Reasoning models (OpenAI o-series, GPT-5, DeepSeek-R1, Grok-think) ---
    /// <summary>"minimal" | "low" | "medium" | "high"</summary>
    public string? ReasoningEffort { get; set; }
    /// <summary>"low" | "medium" | "high" — GPT-5 verbosity control</summary>
    public string? Verbosity { get; set; }

    // --- Anthropic extended thinking ---
    public bool? AnthropicThinking { get; set; }
    public int? AnthropicThinkingBudgetTokens { get; set; }

    // --- Google Gemini ---
    /// <summary>"BLOCK_NONE" | "BLOCK_ONLY_HIGH" | "BLOCK_MEDIUM_AND_ABOVE" | "BLOCK_LOW_AND_ABOVE"</summary>
    public string? GeminiSafetyThreshold { get; set; }
    public int? GeminiThinkingBudget { get; set; }
    public bool? GeminiIncludeThoughts { get; set; }

    // --- OpenRouter-specific ---
    /// <summary>Fallback model list (OR routes through them in order if primary fails).</summary>
    public List<string>? OrFallbackModels { get; set; }
    /// <summary>"fallback" | "lowest-price" | "highest-throughput" | "fastest" | null</summary>
    public string? OrProviderSort { get; set; }
    /// <summary>Allowed provider slugs (e.g. ["anthropic","openai"]). Empty = all.</summary>
    public List<string>? OrAllowedProviders { get; set; }
    /// <summary>Ignored provider slugs.</summary>
    public List<string>? OrIgnoredProviders { get; set; }
    /// <summary>If true, only use providers that don't log prompts (data:policy).</summary>
    public bool? OrRequireParameters { get; set; }
    public bool? OrAllowDataCollection { get; set; }
    /// <summary>"middle-out" — OR auto-compresses long contexts.</summary>
    public string? OrTransforms { get; set; }

    // --- Service tier (OpenAI/Anthropic priority queues) ---
    /// <summary>"auto" | "default" | "flex" | "priority" | "scale"</summary>
    public string? ServiceTier { get; set; }

    // --- Azure-specific ---
    public string? AzureDeployment { get; set; }
    public string? AzureApiVersion { get; set; } = "2024-10-21";

    // --- User identification (abuse tracking on OpenAI/Anthropic) ---
    public string? UserIdentifier { get; set; }
}

public class SgLlmPromptOptions
{
    public string? SystemPrompt { get; set; }
    public bool Stream { get; set; } = true;
    public List<SgLlmAttachment>? Attachments { get; set; }
    public List<object>? Tools { get; set; }
    public object? ToolChoice { get; set; }
}
