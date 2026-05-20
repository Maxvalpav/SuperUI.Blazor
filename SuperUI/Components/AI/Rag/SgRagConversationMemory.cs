namespace SuperUI.Components;

public class SgRagConversationMemory
{
    private readonly List<SgRagChatMessage> _messages = new();
    private string? _summary;
    private readonly int _summaryInterval;

    public SgRagConversationMemory(int summaryInterval = 10)
    {
        _summaryInterval = summaryInterval;
    }

    public void AddMessage(SgRagChatMessage message)
    {
        _messages.Add(message);
    }

    public IReadOnlyList<SgRagChatMessage> GetMessages() => _messages.AsReadOnly();

    public string? GetSummary() => _summary;

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

    public void Clear()
    {
        _messages.Clear();
        _summary = null;
    }
}
