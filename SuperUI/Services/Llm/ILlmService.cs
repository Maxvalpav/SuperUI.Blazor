using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperUI.Services.Llm;

public interface ILlmService
{
    bool IsInitialized { get; }
    SgLlmConfig? CurrentConfig { get; }

    event Action<string>? OnTokenReceived;
    event Action<string>? OnChatComplete;
    event Action<string>? OnError;

    Task InitializeAsync(SgLlmConfig config);
    SgLlmConfig ResolveConfigForTask(string purpose, SgLlmConfig? baseConfig = null);
    Task ChatAsync(string message, SgLlmPromptOptions? options = null);
    Task<bool> IsReadyAsync();

    /// <summary>
    /// Lightweight connectivity check against the provider's REST endpoint —
    /// usually GET /models with the supplied API key. Returns the raw HTTP status,
    /// a short body snippet (truncated), and whether it looked successful.
    /// </summary>
    Task<SgLlmConnectionTest> TestConnectionAsync(SgLlmConfig config);
    Task<SgLlmDiagnosticsResult> TestFullConnectionAsync(SgLlmConfig config);

    Task<List<SgLlmProfile>> GetProfilesAsync();
    Task SaveProfileAsync(SgLlmProfile profile);
    Task DeleteProfileAsync(string profileId);
    Task<string> ExportProfilesJsonAsync();
    Task ImportProfilesJsonAsync(string json);

    Task<List<SgLlmUsageRecord>> GetUsageRecordsAsync();
    Task ClearUsageRecordsAsync();
    Task<List<SgLlmHealthStatus>> CheckProvidersHealthAsync(SgLlmConfig? baseConfig = null);

    Task<List<SgLlmModelInfo>> GetOpenRouterModelsAsync();
    Task<List<SgLlmModelInfo>> GetOpenAiModelsAsync(string? baseUrl = null, string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetAnthropicModelsAsync(string? apiKey = null);
    Task<List<SgOllamaModel>> GetOllamaModelsAsync(string? baseUrl = null);

    Task<List<SgLlmModelInfo>> GetGoogleModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetMistralModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetGroqModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetDeepSeekModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetXAiModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetCohereModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetPerplexityModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetTogetherModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetFireworksModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetCerebrasModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetHuggingFaceModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetLmStudioModelsAsync(string? baseUrl = null);
    Task<List<SgLlmModelInfo>> GetGigaGptModelsAsync(string? baseUrl = null, string? apiKey = null, string? authMode = null, string? scope = null, string? oauthUrl = null);

    Task<float[]> GetEmbeddingsAsync(string text, string? modelId = null);
    Task<List<float[]>> GetEmbeddingsBatchAsync(IEnumerable<string> texts, string? modelId = null, int? dimensions = null);

    Task<string> GenerateImageAsync(string prompt, string? modelId = null, string? size = "1024x1024");
    Task<SgLlmImageResult> GenerateImageRichAsync(SgLlmImageRequest request);
    Task<SgLlmImageResult> EditImageAsync(byte[] imageBytes, string imageMime, string prompt,
        byte[]? maskBytes = null, string? maskMime = null, string? modelId = null, string? size = "1024x1024");

    Task<string> SpeechToTextAsync(Stream audioStream, string fileName, string? modelId = null);
    Task<SgLlmTranscription> TranscribeAsync(SgLlmTranscribeRequest request);
    Task<byte[]> TextToSpeechAsync(string text, string? modelId = null, string? voice = "alloy");
    Task<SgLlmTtsResult> SynthesizeAsync(SgLlmTtsRequest request);

    Task<SgLlmModerationResult> ModerateAsync(string text, string? modelId = null);
    Task<string> AnalyzeVisionAsync(SgLlmVisionRequest request);
    Task<string> AnalyzeVideoAsync(SgLlmVideoRequest request);

    Task<string> UploadFileAsync(Stream fileStream, string fileName, string purpose = "fine-tune");

    // --- Free / additional providers ---
    Task<List<SgLlmModelInfo>> GetCloudflareWorkersAiModelsAsync(string? accountId = null, string? apiToken = null);
    Task<List<SgLlmModelInfo>> GetGitHubModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetSambaNovaModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetGlhfModelsAsync(string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetPollinationsModelsAsync();

    // --- Provider presets registry ---
    IReadOnlyList<SgLlmProviderPreset> GetProviderPresets();
    SgLlmProviderPreset? GetPreset(SgLlmProvider provider);

    // --- Structured outputs / tools / advanced ---
    Task<SgLlmStructuredResult<T>> CompleteStructuredAsync<T>(SgLlmStructuredRequest request);
    Task<SgLlmChatResult> CompleteAsync(SgLlmChatRequest request);
    Task<List<SgLlmRerankResult>> RerankAsync(SgLlmRerankRequest request);
    Task<SgLlmImageResult> GenerateImageVariationsAsync(byte[] imageBytes, string imageMime, int count = 1,
        string? size = "1024x1024", string? modelId = null);

    // --- Files / fine-tuning / batch ---
    Task<List<SgLlmFileInfo>> ListFilesAsync(string? purpose = null);
    Task<bool> DeleteFileAsync(string fileId);
    Task<List<SgLlmFineTuneJob>> ListFineTuneJobsAsync();
    Task<SgLlmBatchJob> CreateBatchAsync(SgLlmBatchRequest request);
    Task<SgLlmBatchJob?> GetBatchAsync(string batchId);
    Task<List<SgLlmBatchJob>> ListBatchesAsync();
}

public class SgLlmImageRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Size { get; set; } = "1024x1024";
    public int Count { get; set; } = 1;
    public string? Quality { get; set; }
    public string? Style { get; set; }
    public string? Background { get; set; }
    public string? ResponseFormat { get; set; } = "b64_json";
}

public class SgLlmImageResult
{
    public List<SgLlmGeneratedImage> Images { get; set; } = new();
    public string? RevisedPrompt { get; set; }
    public string? Error { get; set; }
}

public class SgLlmGeneratedImage
{
    public string? Url { get; set; }
    public string? B64Json { get; set; }
    public string? MimeType { get; set; } = "image/png";
}

public class SgLlmTranscribeRequest
{
    public byte[] Audio { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "audio.webm";
    public string? Model { get; set; }
    public string? Language { get; set; }
    public string? Prompt { get; set; }
    public string? ResponseFormat { get; set; } = "json";
    public double? Temperature { get; set; }
    public bool Translate { get; set; }
}

public class SgLlmTranscription
{
    public string Text { get; set; } = string.Empty;
    public string? Language { get; set; }
    public double? Duration { get; set; }
    public string? Error { get; set; }
}

public class SgLlmTtsRequest
{
    public string Input { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Voice { get; set; } = "alloy";
    public string? Format { get; set; } = "mp3";
    public double? Speed { get; set; }
    public string? Instructions { get; set; }
}

public class SgLlmTtsResult
{
    public byte[] Audio { get; set; } = Array.Empty<byte>();
    public string MimeType { get; set; } = "audio/mpeg";
    public string? Error { get; set; }
}

public class SgLlmModerationResult
{
    public bool Flagged { get; set; }
    public Dictionary<string, double> CategoryScores { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string? Error { get; set; }
}

public class SgLlmVisionRequest
{
    public string Prompt { get; set; } = "Describe this image.";
    public List<SgLlmAttachment> Images { get; set; } = new();
    public string? Model { get; set; }
    public int? MaxTokens { get; set; }
    public double Temperature { get; set; } = 0.7;
}

public class SgLlmVideoRequest
{
    public string Prompt { get; set; } = "Summarize this video.";
    public byte[] Video { get; set; } = Array.Empty<byte>();
    public string MimeType { get; set; } = "video/mp4";
    public string? Model { get; set; }
    public int? MaxTokens { get; set; }
}

public class SgLlmModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public bool IsRecommended { get; set; }
    public SgLlmProvider Provider { get; set; }
    public string? ProviderLabel { get; set; }
    public int? ContextWindow { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsTools { get; set; }
    public bool SupportsJsonSchema { get; set; }
    public bool SupportsReasoning { get; set; }
}

public record SgLlmConnectionTest(bool Ok, int Status, string Message);

public class SgLlmChatRequest
{
    public List<SgLlmChatMsg> Messages { get; set; } = new();
    public string? Model { get; set; }
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int? MaxTokens { get; set; }
    public List<string>? Stop { get; set; }
    public string? ResponseFormat { get; set; } // "text" | "json_object" | "json_schema"
    public string? JsonSchema { get; set; }
    public List<SgLlmTool>? Tools { get; set; }
    public object? ToolChoice { get; set; }
    public bool Stream { get; set; }
    public int? Seed { get; set; }
}

public class SgLlmChatMsg
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public List<SgLlmAttachment>? Attachments { get; set; }
    public List<SgLlmToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
    public string? Name { get; set; }
}

public class SgLlmTool
{
    public string Type { get; set; } = "function";
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParametersJsonSchema { get; set; }
}

public class SgLlmToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
}

public class SgLlmChatResult
{
    public string Content { get; set; } = string.Empty;
    public string? FinishReason { get; set; }
    public List<SgLlmToolCall> ToolCalls { get; set; } = new();
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
    public string? Error { get; set; }
    public string? RawJson { get; set; }
}

public class SgLlmStructuredRequest
{
    public List<SgLlmChatMsg> Messages { get; set; } = new();
    public string? Model { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string SchemaName { get; set; } = "Result";
    public string? JsonSchema { get; set; }
}

public class SgLlmStructuredResult<T>
{
    public T? Data { get; set; }
    public string? RawJson { get; set; }
    public string? Error { get; set; }
}

public class SgLlmRerankRequest
{
    public string Query { get; set; } = string.Empty;
    public List<string> Documents { get; set; } = new();
    public string? Model { get; set; }
    public int? TopN { get; set; }
}

public class SgLlmRerankResult
{
    public int Index { get; set; }
    public double Score { get; set; }
    public string Document { get; set; } = string.Empty;
}

public class SgLlmFileInfo
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Bytes { get; set; }
    public string? Purpose { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class SgLlmFineTuneJob
{
    public string Id { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Status { get; set; }
    public string? FineTunedModel { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class SgLlmBatchRequest
{
    public string InputFileId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "/v1/chat/completions";
    public string CompletionWindow { get; set; } = "24h";
    public Dictionary<string, string>? Metadata { get; set; }
}

public class SgLlmBatchJob
{
    public string Id { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Endpoint { get; set; }
    public string? InputFileId { get; set; }
    public string? OutputFileId { get; set; }
    public string? ErrorFileId { get; set; }
    public int? RequestCounts_Completed { get; set; }
    public int? RequestCounts_Failed { get; set; }
    public int? RequestCounts_Total { get; set; }
    public DateTime? CreatedAt { get; set; }
}
