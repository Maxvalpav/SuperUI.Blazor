namespace SuperUI.Services.Llm;

public enum SgLlmProvider
{
    WebLlm,
    OpenAiCompatible,
    OpenRouter,
    Ollama,
    OpenCode,
    Anthropic,
    None
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

public class SgLlmConfig
{
    public SgLlmProvider Provider { get; set; } = SgLlmProvider.OpenAiCompatible;
    public string? ModelId { get; set; }
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? SystemPrompt { get; set; }
    public Dictionary<string, string>? ExtraHeaders { get; set; }

    // Advanced settings
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 1.0;
    public int? MaxTokens { get; set; }
    public double PresencePenalty { get; set; } = 0.0;
    public double FrequencyPenalty { get; set; } = 0.0;
}

public class SgLlmPromptOptions
{
    public string? SystemPrompt { get; set; }
    public bool Stream { get; set; } = true;
    public List<SgLlmAttachment>? Attachments { get; set; }
    public List<object>? Tools { get; set; }
    public object? ToolChoice { get; set; }
}
