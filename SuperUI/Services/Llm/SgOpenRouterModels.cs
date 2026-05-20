using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SuperUI.Services.Llm;

public class SgOpenRouterModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("context_length")]
    public int ContextLength { get; set; }

    [JsonPropertyName("pricing")]
    public SgOpenRouterPricing Pricing { get; set; } = new();

    [JsonPropertyName("architecture")]
    public SgOpenRouterArchitecture? Architecture { get; set; }

    [JsonPropertyName("top_provider")]
    public SgOpenRouterTopProvider? TopProvider { get; set; }

    public bool IsFree => Pricing.Prompt == "0" && Pricing.Completion == "0";
}

public class SgOpenRouterPricing
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = "0";

    [JsonPropertyName("completion")]
    public string Completion { get; set; } = "0";

    [JsonPropertyName("request")]
    public string Request { get; set; } = "0";

    [JsonPropertyName("image")]
    public string Image { get; set; } = "0";
}

public class SgOpenRouterArchitecture
{
    [JsonPropertyName("modality")]
    public string Modality { get; set; } = string.Empty;

    [JsonPropertyName("tokenizer")]
    public string Tokenizer { get; set; } = string.Empty;

    [JsonPropertyName("instruct_type")]
    public string? InstructType { get; set; }
}

public class SgOpenRouterTopProvider
{
    [JsonPropertyName("context_length")]
    public int? ContextLength { get; set; }

    [JsonPropertyName("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }

    [JsonPropertyName("is_moderated")]
    public bool IsModerated { get; set; }
}

public class SgOpenRouterModelsResponse
{
    [JsonPropertyName("data")]
    public List<SgOpenRouterModel> Data { get; set; } = new();
}

public class SgOpenRouterKeyResponse
{
    [JsonPropertyName("data")]
    public SgOpenRouterKeyData Data { get; set; } = new();
}

public class SgOpenRouterKeyData
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public double Usage { get; set; }

    [JsonPropertyName("limit")]
    public double? Limit { get; set; }

    [JsonPropertyName("is_free_tier")]
    public bool IsFreeTier { get; set; }

    [JsonPropertyName("rate_limit")]
    public SgOpenRouterRateLimit RateLimit { get; set; } = new();
}

public class SgOpenRouterRateLimit
{
    [JsonPropertyName("requests")]
    public int Requests { get; set; }

    [JsonPropertyName("interval")]
    public string Interval { get; set; } = string.Empty;
}
