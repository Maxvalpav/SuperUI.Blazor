using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SuperUI.Services.Llm;

public class SgOllamaModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("modified_at")]
    public DateTime ModifiedAt { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    public string Digest { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public SgOllamaModelDetails? Details { get; set; }
}

public class SgOllamaModelDetails
{
    [JsonPropertyName("parent_model")]
    public string ParentModel { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("family")]
    public string Family { get; set; } = string.Empty;

    [JsonPropertyName("families")]
    public List<string>? Families { get; set; }

    [JsonPropertyName("parameter_size")]
    public string ParameterSize { get; set; } = string.Empty;

    [JsonPropertyName("quantization_level")]
    public string QuantizationLevel { get; set; } = string.Empty;
}

public class SgOllamaListResponse
{
    [JsonPropertyName("models")]
    public List<SgOllamaModel> Models { get; set; } = new();
}

public class SgOllamaShowResponse
{
    [JsonPropertyName("modelfile")]
    public string ModelFile { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public string Parameters { get; set; } = string.Empty;

    [JsonPropertyName("template")]
    public string Template { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public SgOllamaModelDetails? Details { get; set; }
}
