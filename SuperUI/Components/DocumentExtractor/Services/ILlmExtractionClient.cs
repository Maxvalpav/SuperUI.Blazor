using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Components.DocumentExtractor.Models;

using SuperUI.Services.Llm;

namespace SuperUI.Components.DocumentExtractor.Services;

public sealed class SgLlmEndpointConfig : SgLlmConfig
{
    // Keeping this for backward compatibility and specialized extraction needs
    public SgLlmEndpointKind Kind { get; set; } = SgLlmEndpointKind.OpenAiCompatible;
    public string Model { get => ModelId ?? ""; set => ModelId = value; }
}

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
    OpenAiCompatible,
    OpenRouter
}

public sealed class SgLlmModelDescriptor
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFree { get; set; }
}
