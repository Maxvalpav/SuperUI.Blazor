using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SuperUI.Services.Llm;
using SuperUI.Localization;
using System.Text;

namespace SuperUI.Components.Llm;

public partial class SgChat : ComponentBase, IAsyncDisposable
{
    [Inject] private ILlmService LlmService { get; set; } = default!;
    [Inject] private SgChatHistoryService HistoryService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

    [Parameter] public string? Title { get; set; } = "AI Assistant";
    [Parameter] public string? CssClass { get; set; }
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
        ModelId = "google/gemini-2.0-flash-001:free",
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

    private async Task SendMessage()
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

    private void HandleToken(string token)
    {
        InvokeAsync(async () =>
        {
            if (_streamingMsg == null)
            {
                _streamingMsg = new SgLlmMessage { Role = "assistant", Content = "" };
                // Don't add to session yet, wait for complete
                _isThinking = false;
            }
            _streamingMsg.Content += token;
            _tokenCount = _streamingMsg.Content.Length / 4;
            _renderCounter++;
            if (_renderCounter % RenderEvery == 0)
                await RenderMarkdownAsync(_streamingMsg);
            StateHasChanged();
            await ScrollToBottomAsync();
        });
    }

    private void HandleComplete(string fullAnswer)
    {
        InvokeAsync(async () =>
        {
            if (_streamingMsg != null)
                await RenderMarkdownAsync(_streamingMsg);
            
            if (_currentSession != null && _streamingMsg != null)
            {
                _currentSession.Messages.Add(new SgLlmMessage 
                { 
                    Role = "assistant", 
                    Content = _streamingMsg.Content,
                    Timestamp = DateTime.UtcNow
                });
                _currentSession.UpdatedAt = DateTime.UtcNow;
                await HistoryService.SaveSessionAsync(_currentSession);
            }

            _isThinking = false;
            _streaming = false;
            _streamingMsg = null;
            StateHasChanged();
            await ScrollToBottomAsync();
        });
    }

    private void HandleError(string error)
    {
        InvokeAsync(() =>
        {
            if (error.Contains("401"))
            {
                _error = "Unauthorized (401). Please check your API Key in settings.";
            }
            else
            {
                _error = error;
            }
            _isThinking = false;
            _streaming = false;
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
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessage();
        }
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("eval", @"
                const el = document.querySelector('.sg-chat-messages');
                if (el) {
                    el.scrollTo({
                        top: el.scrollHeight,
                        behavior: 'smooth'
                    });
                }
            ");
        }
        catch { }
    }

    // ── Markdown rendering ────────────────────────────────────────────────────

    private IJSObjectReference? _llmModule;
    private readonly Dictionary<string, string> _htmlCache = new();
    private int _renderCounter;
    private const int RenderEvery = 6;

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

        var bytes = Encoding.UTF8.GetBytes(content);
        var b64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("eval", $"(()=>{{const a=document.createElement('a');a.href='data:{mime};base64,{b64}';a.download='chat-{ts}.{ext}';document.body.appendChild(a);a.click();document.body.removeChild(a);}})()");
    }

    public async ValueTask DisposeAsync()
    {
        LlmService.OnTokenReceived -= HandleToken;
        LlmService.OnChatComplete -= HandleComplete;
        LlmService.OnError -= HandleError;

        if (_llmModule is not null)
        {
            try { await _llmModule.DisposeAsync(); } catch { }
        }
    }
}
