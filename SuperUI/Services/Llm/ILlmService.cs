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
    Task<List<SgOllamaModel>> GetOllamaModelsAsync(string? baseUrl = null);
}

public class SgLlmModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFree { get; set; }
}
