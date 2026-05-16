using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Text.Json;

namespace SuperUI.Services.Llm;

public class SgChatHistoryService
{
    private readonly IJSRuntime _js;
    private const string StorageKey = "sg_chat_history";

    public SgChatHistoryService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<List<SgChatSession>> GetSessionsAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json)) return new List<SgChatSession>();
            return JsonSerializer.Deserialize<List<SgChatSession>>(json) ?? new List<SgChatSession>();
        }
        catch { return new List<SgChatSession>(); }
    }

    public async Task SaveSessionsAsync(List<SgChatSession> sessions)
    {
        try
        {
            var json = JsonSerializer.Serialize(sessions);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch { }
    }

    public async Task SaveSessionAsync(SgChatSession session)
    {
        var sessions = await GetSessionsAsync();
        var index = sessions.FindIndex(s => s.Id == session.Id);
        if (index >= 0)
        {
            sessions[index] = session;
        }
        else
        {
            sessions.Insert(0, session);
        }
        await SaveSessionsAsync(sessions);
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        var sessions = await GetSessionsAsync();
        sessions.RemoveAll(s => s.Id == sessionId);
        await SaveSessionsAsync(sessions);
    }
}
