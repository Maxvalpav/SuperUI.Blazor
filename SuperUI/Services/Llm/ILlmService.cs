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
    Task<List<SgLlmModelInfo>> GetOpenRouterModelsAsync();
    Task<List<SgLlmModelInfo>> GetOpenAiModelsAsync(string? baseUrl = null, string? apiKey = null);
    Task<List<SgLlmModelInfo>> GetAnthropicModelsAsync(string? apiKey = null);
    Task<List<SgOllamaModel>> GetOllamaModelsAsync(string? baseUrl = null);
    Task<float[]> GetEmbeddingsAsync(string text, string? modelId = null);
    
    // Новые возможности OpenAI Compatible API
    Task<string> GenerateImageAsync(string prompt, string? modelId = null, string? size = "1024x1024");
    Task<string> SpeechToTextAsync(Stream audioStream, string fileName, string? modelId = null);
    Task<byte[]> TextToSpeechAsync(string text, string? modelId = null, string? voice = "alloy");
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string purpose = "fine-tune");
}

public class SgLlmModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFree { get; set; }
}
