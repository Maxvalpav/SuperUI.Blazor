using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Components.DocumentExtractor.Models;

namespace SuperUI.Components.DocumentExtractor.Services;

/// <summary>
/// Thin OpenAI-compatible client used by <see cref="LlmDocumentExtractor"/>.
/// Lets the extractor stay LLM-agnostic — swap implementations to point at OpenAI,
/// OpenRouter, Azure OpenAI, a local proxy, etc.
/// </summary>
public interface ILlmExtractionClient
{
    /// <summary>Sends the document text/image to the configured model and asks for a JSON list of fields.</summary>
    Task<List<SgDocumentField>> ExtractFieldsAsync(
        SgLlmEndpointConfig config,
        SgDocumentSource source,
        string? extractedPlainText,
        CancellationToken ct = default);

    /// <summary>Fetches the catalog of models exposed by the provider's REST API (OpenAI /v1/models or OpenRouter).</summary>
    Task<List<SgLlmModelDescriptor>> ListModelsAsync(SgLlmEndpointConfig config, CancellationToken ct = default);
}

public enum SgLlmEndpointKind
{
    /// <summary>OpenAI-compatible Chat Completions endpoint (OpenAI, vLLM, LM Studio, Ollama-OpenAI, …).</summary>
    OpenAiCompatible,
    OpenRouter
}

public sealed class SgLlmEndpointConfig
{
    public SgLlmEndpointKind Kind { get; set; } = SgLlmEndpointKind.OpenAiCompatible;
    /// <summary>Base URL (e.g. https://api.openai.com/v1, https://openrouter.ai/api/v1).</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string? ApiKey { get; set; }
    /// <summary>Model id. Either typed in by hand or chosen from the provider's /models response.</summary>
    public string Model { get; set; } = string.Empty;
    public Dictionary<string, string>? ExtraHeaders { get; set; }
    /// <summary>Optional override system prompt for the extraction call.</summary>
    public string? SystemPrompt { get; set; }
}

public sealed class SgLlmModelDescriptor
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFree { get; set; }
}
