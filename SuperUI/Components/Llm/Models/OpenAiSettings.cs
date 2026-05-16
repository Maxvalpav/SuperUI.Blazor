namespace SuperUI.Components.Llm.Models;

public enum LlmProvider { OpenAI, OpenRouter }

public class OpenAiSettings
{
    public LlmProvider Provider { get; set; } = LlmProvider.OpenAI;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o";
    public double Temperature { get; set; } = 0.1;
    public int MaxTokens { get; set; } = 4096;
    public string? SystemPrompt { get; set; }
    public bool UseFileApi { get; set; } = false;
    
    public static readonly string[] OpenAIModels = 
    {
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4-turbo",
        "o1",
        "o1-mini"
    };
}
