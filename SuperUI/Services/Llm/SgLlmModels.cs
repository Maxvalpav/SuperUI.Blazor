namespace SuperUI.Services.Llm;

public enum SgLlmProvider
{
    WebLlm,
    OpenAiCompatible,
    OpenRouter,
    None
}

public class SgLlmAttachment
{
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Base64 { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public bool IsPdf { get; set; }
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
}

public class SgLlmPromptOptions
{
    public string? SystemPrompt { get; set; }
    public bool Stream { get; set; } = true;
    public List<SgLlmAttachment>? Attachments { get; set; }
}
