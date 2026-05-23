namespace SuperUI.Services.Llm;

/// <summary>
/// Contract for optional backend proxy mode. A host app can expose an endpoint that
/// accepts this request, injects provider secrets server-side, forwards to the LLM
/// provider and streams or returns an OpenAI-compatible response.
/// </summary>
public sealed class SgLlmProxyRequest
{
    public SgLlmProvider Provider { get; set; }
    public string? ModelId { get; set; }
    public string? BaseUrl { get; set; }
    public string? SystemPrompt { get; set; }
    public List<SgLlmMessage> Messages { get; set; } = new();
    public SgLlmPromptOptions Options { get; set; } = new();
    public SgLlmConfig ClientConfig { get; set; } = new();
}

public sealed class SgLlmProxyResponse
{
    public string Content { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public string? RawJson { get; set; }
    public string? Error { get; set; }
}
