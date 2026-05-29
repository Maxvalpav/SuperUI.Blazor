using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SuperUI.Services.Llm;
using SuperUI.Localization;
using System.Text;

namespace SuperUI.Components.Llm;

/// <summary>Represents a chat component for LLM interaction.</summary>
public partial class SgChat : ComponentBase, IAsyncDisposable
{
    [Inject] private ILlmService LlmService { get; set; } = default!;
    [Inject] private SgChatHistoryService HistoryService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

    [Parameter] public string? Title { get; set; } = "AI Assistant";
    /// <summary>Custom CSS class applied to the chat container.</summary>
    [Parameter] public string? CssClass { get; set; }
    /// <summary>Additional HTML attributes applied to the chat container.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private List<SgChatSession> _sessions = new();
    private SgChatSession? _currentSession;
    private List<SgLlmMessage> _messages => _currentSession?.Messages ?? new();
    private List<SgLlmAttachment> _attachments = new();
    private string _userInput = "";
    private bool _isThinking;
    private bool _isReady;
    private bool _showSettings;
    private bool _showSidebar = true;
    private string? _error;
    private ElementReference _messagesRef;
    private SgLlmMessage? _streamingMsg;
    private int _tokenCount;
    private bool _streaming;

    private SgLlmConfig _config = new()
    {
        Provider = SgLlmProvider.OpenRouter,
        ModelId = null,
        BaseUrl = "https://openrouter.ai/api/v1",
        ApiKey = ""
    };

    protected override async Task OnInitializedAsync()
    {
        LlmService.OnTokenReceived += HandleToken;
        LlmService.OnChatComplete += HandleComplete;
        LlmService.OnError += HandleError;

        if (LlmService.IsInitialized && LlmService.CurrentConfig != null)
        {
            _config = LlmService.CurrentConfig;
            _isReady = true;
        }

        await LoadSessionsAsync();
    }

    private void OnConfigChanged(SgLlmConfig config)
    {
        _config = config;
        _isReady = true;
        _showSettings = false;
        StateHasChanged();
    }

    private async Task LoadSessionsAsync()
    {
        _sessions = await HistoryService.GetSessionsAsync();
        if (_sessions.Count > 0)
        {
            _currentSession = _sessions[0];
        }
        else
        {
            await NewSessionAsync();
        }
    }

    private async Task NewSessionAsync()
    {
        _currentSession = new SgChatSession { Title = "New Chat" };
        _sessions.Insert(0, _currentSession);
        await HistoryService.SaveSessionAsync(_currentSession);
        StateHasChanged();
    }

    private async Task SelectSessionAsync(SgChatSession session)
    {
        _currentSession = session;
        _error = null;
        StateHasChanged();
        await ScrollToBottomAsync();
    }

    private async Task DeleteSessionAsync(SgChatSession session)
    {
        _sessions.Remove(session);
        await HistoryService.DeleteSessionAsync(session.Id);
        if (_currentSession?.Id == session.Id)
        {
            if (_sessions.Count > 0)
                _currentSession = _sessions[0];
            else
                await NewSessionAsync();
        }
        StateHasChanged();
    }

    private void ToggleSidebar() => _showSidebar = !_showSidebar;

    private void ToggleSettings()
    {
        _showSettings = !_showSettings;
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles(10))
        {
            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                using var ms = new System.IO.MemoryStream();
                await stream.CopyToAsync(ms);
                
                var bytes = ms.ToArray();
                var attachment = new SgLlmAttachment
                {
                    Name = file.Name,
                    MimeType = file.ContentType,
                    Base64 = Convert.ToBase64String(bytes),
                    IsImage = file.ContentType.StartsWith("image/"),
                    IsPdf = file.ContentType == "application/pdf",
                    IsVideo = file.ContentType.StartsWith("video/"),
                    IsText = file.ContentType.StartsWith("text/") || file.Name.EndsWith(".cs") || file.Name.EndsWith(".js") || file.Name.EndsWith(".md")
                };
                _attachments.Add(attachment);
            }
            catch (Exception ex)
            {
                _error = $"Failed to load file {file.Name}: {ex.Message}";
            }
        }
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(_userInput) && _attachments.Count == 0) return;
        if (!_isReady || _streaming) return;

        var question = _userInput.Trim();
        var attachments = _attachments.Count > 0 ? new List<SgLlmAttachment>(_attachments) : null;

        var userMsg = new SgLlmMessage
        {
            Role = "user",
            Content = string.IsNullOrWhiteSpace(question) ? $"[{_attachments.Count} file(s)]" : question,
            Attachments = attachments
        };

        if (_currentSession != null)
        {
            _currentSession.Messages.Add(userMsg);
            if (_currentSession.Messages.Count == 1 && !string.IsNullOrWhiteSpace(question))
            {
                _currentSession.Title = question.Length > 30 ? question.Substring(0, 30) + "..." : question;
            }
            _currentSession.UpdatedAt = DateTime.UtcNow;
            await HistoryService.SaveSessionAsync(_currentSession);
        }

        _userInput = "";
        _attachments.Clear();
        _isThinking = true;
        _streaming = true;
        _error = null;
        _streamingMsg = null;
        _tokenCount = 0;

        StateHasChanged();
        await ScrollToBottomAsync();

        try
        {
            await LlmService.ChatAsync(question, new SgLlmPromptOptions { Attachments = attachments });
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _isThinking = false;
            _streaming = false;
            StateHasChanged();
        }
    }

    // Streaming throttles. Each token used to trigger StateHasChanged + a
    // smooth-scroll JS interop, which on a long answer pegged the renderer.
    // We now coalesce updates by wall-clock time so visible UI advances at
    // ~60 fps regardless of token arrival rate.
    private DateTime _lastUiTickUtc = DateTime.MinValue;
    private DateTime _lastMarkdownUtc = DateTime.MinValue;
    private static readonly TimeSpan UiTickInterval = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan MarkdownInterval = TimeSpan.FromMilliseconds(180);

    private void HandleToken(string token)
    {
        if (_disposed) return;
        InvokeAsync(async () =>
        {
            if (_disposed) return;
            if (_streamingMsg == null)
            {
                _streamingMsg = new SgLlmMessage { Role = "assistant", Content = "" };
                // Add to the session immediately so the assistant bubble (and
                // the "thinking" dots) is part of the foreach _messages render
                // loop. HandleComplete will persist the session, not duplicate
                // the message.
                _currentSession?.Messages.Add(_streamingMsg);
                _isThinking = false;
                _lastUiTickUtc = DateTime.MinValue;
                _lastMarkdownUtc = DateTime.MinValue;
            }
            _streamingMsg.Content += token;
            _tokenCount = _streamingMsg.Content.Length / 4;

            var now = DateTime.UtcNow;
            if (now - _lastMarkdownUtc >= MarkdownInterval)
            {
                _lastMarkdownUtc = now;
                await RenderMarkdownAsync(_streamingMsg);
                if (_disposed) return;
            }

            if (now - _lastUiTickUtc < UiTickInterval) return;
            _lastUiTickUtc = now;

            StateHasChanged();
            await ScrollToBottomAsync();
        });
    }

    private void HandleComplete(string fullAnswer)
    {
        if (_disposed) return;
        InvokeAsync(async () =>
        {
            if (_disposed) return;
            if (_streamingMsg != null)
                await RenderMarkdownAsync(_streamingMsg);

            if (_currentSession != null && _streamingMsg != null)
            {
                // The streaming message is already part of the session (added
                // in HandleToken). Stamp it and persist the whole session so
                // history reflects the assistant's final answer.
                _streamingMsg.Timestamp = DateTime.UtcNow;
                _currentSession.UpdatedAt = DateTime.UtcNow;
                await HistoryService.SaveSessionAsync(_currentSession);
            }

            if (_disposed) return;
            _isThinking = false;
            _streaming = false;
            _streamingMsg = null;
            StateHasChanged();
            await ScrollToBottomAsync();
        });
    }

    private void HandleError(string error)
    {
        if (_disposed) return;
        InvokeAsync(() =>
        {
            if (_disposed) return;
            // Show the provider's actual response — generic "check API key" hides the
            // real cause (e.g. unknown model on OpenRouter also returns 401).
            _error = error;
            _isThinking = false;
            _streaming = false;
            // Drop the half-filled assistant bubble so the user can retry
            // without an empty/orphaned message stuck in the transcript.
            if (_streamingMsg is not null && _currentSession is not null)
            {
                _currentSession.Messages.Remove(_streamingMsg);
            }
            _streamingMsg = null;
            StateHasChanged();
        });
    }

    private async Task ClearHistory()
    {
        if (_currentSession != null)
        {
            _currentSession.Messages.Clear();
            _currentSession.UpdatedAt = DateTime.UtcNow;
            await HistoryService.SaveSessionAsync(_currentSession);
        }
        _error = null;
        StateHasChanged();
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        // Ignore Enter while an IME composition is active (e.IsComposing) so CJK
        // users committing a candidate don't submit a half-typed message.
        if (e.Key == "Enter" && !e.ShiftKey && !e.IsComposing)
        {
            await SendAsync();
        }
    }

    private async Task ScrollToBottomAsync()
    {
        // The module call is cheaper than eval and avoids a smooth-scroll
        // animation per token, which previously stalled the layout engine.
        try
        {
            await EnsureModuleAsync();
            if (_llmModule is not null)
                await _llmModule.InvokeVoidAsync("scrollChatToBottom", ".sg-chat-messages");
        }
        catch { }
    }

    // ── Markdown rendering ────────────────────────────────────────────────────

    private IJSObjectReference? _llmModule;
    private IJSObjectReference? _blobModule;

    private async Task<IJSObjectReference> EnsureBlobModuleAsync()
        => _blobModule ??= await JS.InvokeAsync<IJSObjectReference>(
            "import", "./_content/SuperUI/sg-blob.js");
    private readonly Dictionary<string, string> _htmlCache = new();

    private string GetHtml(SgLlmMessage msg)
    {
        if (_htmlCache.TryGetValue(msg.Id, out var cached)) return cached;
        return System.Web.HttpUtility.HtmlEncode(msg.Content).Replace("\n", "<br>");
    }

    private async Task EnsureModuleAsync()
    {
        if (_llmModule is not null) return;
        try { _llmModule = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/sg-llm.js"); }
        catch { }
    }

    private async Task RenderMarkdownAsync(SgLlmMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Content)) return;
        await EnsureModuleAsync();
        try
        {
            var html = await _llmModule!.InvokeAsync<string>("renderMarkdown",
                msg.Content,
                "https://cdn.jsdelivr.net/npm/marked@12.0.0/marked.min.js");
            _htmlCache[msg.Id] = html;
        }
        catch
        {
            _htmlCache[msg.Id] = System.Web.HttpUtility.HtmlEncode(msg.Content)
                .Replace("\n", "<br>");
        }
    }

    private bool _showExportMenu;
    private void ToggleExportMenu() => _showExportMenu = !_showExportMenu;

    private async Task ExportAsync(string format)
    {
        _showExportMenu = false;
        var ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        string content = "";
        string mime = "text/plain";
        string ext = "txt";

        if (format == "md")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Chat Export - {DateTime.Now}");
            foreach (var m in _messages)
            {
                sb.AppendLine($"**{(m.Role == "user" ? "You" : "Assistant")}:** {m.Content}");
                sb.AppendLine();
            }
            content = sb.ToString();
            mime = "text/markdown";
            ext = "md";
        }
        else
        {
            var sb = new StringBuilder();
            foreach (var m in _messages)
            {
                sb.AppendLine($"{(m.Role == "user" ? "YOU" : "ASSISTANT")}: {m.Content}");
                sb.AppendLine();
            }
            content = sb.ToString();
        }

        try
        {
            var blob = await EnsureBlobModuleAsync();
            await blob.InvokeVoidAsync("downloadText", content, $"chat-{ts}.{ext}", mime);
        }
        catch (Exception ex)
        {
            _error = $"Не удалось экспортировать чат: {ex.Message}";
        }
    }

    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        LlmService.OnTokenReceived -= HandleToken;
        LlmService.OnChatComplete -= HandleComplete;
        LlmService.OnError -= HandleError;

        if (_llmModule is not null)
        {
            try { await _llmModule.DisposeAsync(); }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (TaskCanceledException) { /* shutdown in flight */ }
            catch (ObjectDisposedException) { /* already disposed */ }
            _llmModule = null;
        }
        if (_blobModule is not null)
        {
            try { await _blobModule.DisposeAsync(); }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (TaskCanceledException) { /* shutdown in flight */ }
            catch (ObjectDisposedException) { /* already disposed */ }
            _blobModule = null;
        }
        GC.SuppressFinalize(this);
    }
}
