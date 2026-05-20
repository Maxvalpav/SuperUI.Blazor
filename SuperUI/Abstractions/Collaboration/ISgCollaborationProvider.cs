using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperUI.Abstractions.Collaboration;

public interface ISgCollaborationProvider : IAsyncDisposable
{
    string RoomId { get; }
    Task ConnectAsync(string roomId, SgCollaborativeUser localUser);
    Task SendCursorUpdateAsync(SgCursorPosition cursor);
    Task SendComponentActivityAsync(SgComponentActivity activity);
    
    event Action<SgCursorPosition>? OnCursorMoved;
    event Action<SgComponentActivity>? OnActivityReceived;
    event Action<List<SgCollaborativeUser>>? OnPresenceChanged;
}

public class SgCollaborativeUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Anonymous";
    public string Color { get; set; } = "#1890ff";
    public string? AvatarUrl { get; set; }
}

public class SgCursorPosition
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Color { get; set; } = "#1890ff";
    public double X { get; set; }
    public double Y { get; set; }
    public string? ElementSelector { get; set; }
}

public class SgComponentActivity
{
    public string UserId { get; set; } = string.Empty;
    public string ComponentId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // e.g. "editing", "selecting"
    public string? TargetKey { get; set; } // e.g. column key, row id
    public object? Data { get; set; }
}
