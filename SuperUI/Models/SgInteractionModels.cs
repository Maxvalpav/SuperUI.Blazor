using System;
using System.Collections.Generic;

namespace SuperUI.Models;

public class SgInteractionSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public List<SgInteractionEvent> Events { get; set; } = new();
    public string UserAgent { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class SgInteractionEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = string.Empty; // click, input, keydown, change
    public string Selector { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? TagName { get; set; }
    public double? ClientX { get; set; }
    public double? ClientY { get; set; }
}

public class SgEmailSendRequest
{
    public string Email { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Base64Data { get; set; } = string.Empty;
    public string MimeType { get; set; } = "video/webm";
    public SgInteractionSession? Session { get; set; }
}
