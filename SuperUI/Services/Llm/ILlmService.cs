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
    Task ChatAsync(string message, SgLlmPromptOptions? options = null);
    Task<bool> IsReadyAsync();

    /// <summary>
    /// Lightweight connectivity check against the provider's REST endpoint —
    /// usually GET /models with the supplied API key. Returns the raw HTTP status,
    /// a short body snippet (truncated), and whether it looked successful.
    /// </summary>
    Task<SgLlmConnectionTest> TestConnectionAsync(SgLlmConfig config);

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
}

public record SgLlmConnectionTest(bool Ok, int Status, string Message);
