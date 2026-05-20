using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SuperUI.Abstractions.Collaboration;

namespace SuperUI.Services.Collaboration;

/// <summary>
/// Reference implementation for SignalR-based collaboration.
/// Requires Microsoft.AspNetCore.SignalR.Client NuGet package.
/// </summary>
public class SgSignalRCollaborationProvider : ISgCollaborationProvider
{
    public string RoomId { get; private set; } = string.Empty;
    
    public event Action<SgCursorPosition>? OnCursorMoved;
    public event Action<SgComponentActivity>? OnActivityReceived;
    public event Action<List<SgCollaborativeUser>>? OnPresenceChanged;

    // This would wrap HubConnection in a real app
    public async Task ConnectAsync(string roomId, SgCollaborativeUser localUser)
    {
        this.RoomId = roomId;
        // In real impl: connection.StartAsync(), connection.InvokeAsync("JoinRoom", roomId, localUser)
        await Task.CompletedTask;
    }

    public async Task SendCursorUpdateAsync(SgCursorPosition cursor)
    {
        // In real impl: hubConnection.SendAsync("UpdateCursor", RoomId, cursor)
        OnCursorMoved?.Invoke(cursor); // For testing: echo back
        await Task.CompletedTask;
    }

    public async Task SendComponentActivityAsync(SgComponentActivity activity)
    {
        // In real impl: hubConnection.SendAsync("ReportActivity", RoomId, activity)
        OnActivityReceived?.Invoke(activity);
        await Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
