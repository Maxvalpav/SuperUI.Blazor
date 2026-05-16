using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

public class SchemaExtractionResult
{
    public DocumentSchema Schema { get; set; } = new();
    public Dictionary<string, object?> ExtractedValues { get; set; } = new();
    public string RawResponse { get; set; } = string.Empty;
}

public interface IOpenAiService
{
    Task<SchemaExtractionResult> ExtractSchemaAsync(
        List<UploadedFile> files,
        OpenAiSettings settings,
        IProgress<string>? progress = null);
    
    Task<bool> ValidateApiKeyAsync(OpenAiSettings settings);
    Task<List<string>> GetAvailableModelsAsync(OpenAiSettings settings);
}
