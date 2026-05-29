namespace SuperUI.Components;

/// <summary>Manages conversation memory for RAG.</summary>
public class SgRagConversationMemory
{
    private readonly List<SgRagChatMessage> _messages = new();
    private string? _summary;
    private readonly int _summaryInterval;

    public SgRagConversationMemory(int summaryInterval = 10)
    {
        _summaryInterval = summaryInterval;
    }

    /// <summary>Adds a message to the conversation history.</summary>
    /// <param name="message">The message to add.</param>
    public void AddMessage(SgRagChatMessage message)
    {
        _messages.Add(message);
    }

    public IReadOnlyList<SgRagChatMessage> GetMessages() => _messages.AsReadOnly();

    public string? GetSummary() => _summary;

    /// <summary>Generates a summary of the conversation.</summary>
    /// <param name="llmSummarize">A function that takes a prompt and returns a summary.</param>
    /// <returns>The generated summary.</returns>
    public async Task<string> GenerateSummaryAsync(Func<string, Task<string>> llmSummarize)
    {
        if (_messages.Count < _summaryInterval)
        {
            return _summary ?? string.Empty;
        }

        var context = string.Join("\n", _messages.Select(m => $"{(m.IsUser ? "User" : "Assistant")}: {m.Content}"));
        var prompt = $"Summarize the following conversation concisely:\n{context}";
        _summary = await llmSummarize(prompt);
        return _summary;
    }

    /// <summary>Clears the conversation memory.</summary>
    public void Clear()
    {
        _messages.Clear();
        _summary = null;
    }
}
