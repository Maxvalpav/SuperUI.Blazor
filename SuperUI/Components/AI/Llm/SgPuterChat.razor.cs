using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SuperUI.Services.Llm;
using SuperUI.Localization;
using System.Text;

namespace SuperUI.Components.Llm;

public partial class SgPuterChat : ComponentBase, IAsyncDisposable
{
    [Inject] private SgPuterService PuterService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

    [Parameter] public string? CssClass { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private List<SgLlmMessage> _messages = new();
    private string _userInput = "";
    private string _selectedModel = "openai/gpt-4o-mini";
    private bool _isThinking;
    private bool _isSignedIn;
    private string? _error;
    private ElementReference _messagesRef;
    private SgLlmMessage? _streamingMsg;
    private bool _showExportMenu;
    private bool _streaming;
    private int _tokenCount;

    private List<SgLlmModelInfo> _puterModels = new()
    {
        new SgLlmModelInfo { Id = "openai/gpt-4o-mini", Name = "GPT-4o mini", Description = "OpenAI", IsFree = true },
        new SgLlmModelInfo { Id = "openai/gpt-4o", Name = "GPT-4o", Description = "OpenAI", IsFree = true },
        new SgLlmModelInfo { Id = "anthropic/claude-3-5-sonnet", Name = "Claude 3.5 Sonnet", Description = "Anthropic", IsFree = true },
        new SgLlmModelInfo { Id = "anthropic/claude-3-5-haiku", Name = "Claude 3.5 Haiku", Description = "Anthropic", IsFree = true },
        new SgLlmModelInfo { Id = "google/gemini-2.0-flash", Name = "Gemini 2.0 Flash", Description = "Google", IsFree = true },
        new SgLlmModelInfo { Id = "meta-llama/llama-3.1-70b-instruct", Name = "Llama 3.1 70B", Description = "Meta", IsFree = true },
        new SgLlmModelInfo { Id = "mistralai/mistral-large", Name = "Mistral Large", Description = "Mistral AI", IsFree = true },
        new SgLlmModelInfo { Id = "deepseek/deepseek-chat", Name = "DeepSeek Chat", Description = "DeepSeek", IsFree = true }
    };

    private IJSObjectReference? _llmModule;
    private IJSObjectReference? _blobModule;

    private async Task<IJSObjectReference> EnsureBlobModuleAsync()
        => _blobModule ??= await JS.InvokeAsync<IJSObjectReference>(
            "import", "./_content/SuperUI/sg-blob.js");
    private readonly Dictionary<string, string> _htmlCache = new();
    private int _renderCounter;
    private const int RenderEvery = 6;

    protected override async Task OnInitializedAsync()
    {
        PuterService.OnTokenReceived += HandleToken;
        PuterService.OnChatComplete += HandleComplete;
        PuterService.OnError += HandleError;

        try
        {
            if (await PuterService.IsAvailableAsync())
            {
                _isSignedIn = await PuterService.IsSignedInAsync();
            }
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private void HandleToken(string token)
    {
        if (_disposed) return;
        InvokeAsync(async () =>
        {
            if (_disposed) return;
            if (_streamingMsg == null)
            {
                _streamingMsg = new SgLlmMessage { Role = "assistant", Content = "" };
                _isThinking = false;
                _streaming = true;
            }
            _streamingMsg.Content += token;
            _tokenCount = _streamingMsg.Content.Length / 4;

            _renderCounter++;
            if (_renderCounter % RenderEvery == 0)
                await RenderMarkdownAsync(_streamingMsg);

            if (_disposed) return;
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
            {
                await RenderMarkdownAsync(_streamingMsg);
                _messages.Add(new SgLlmMessage
                {
                    Role = "assistant",
                    Content = _streamingMsg.Content,
                    Timestamp = DateTime.UtcNow
                });
            }

            if (_disposed) return;
            _isThinking = false;
            _streaming = false;
            _streamingMsg = null;
            _tokenCount = 0;
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
            _error = error;
            _isThinking = false;
            _streaming = false;
            _streamingMsg = null;
            StateHasChanged();
        });
    }

    private async Task SignIn()
    {
        try
        {
            _error = null;
            await PuterService.SignInAsync();
            _isSignedIn = await PuterService.IsSignedInAsync();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        StateHasChanged();
    }

    private async Task SignOut()
    {
        try
        {
            _error = null;
            await PuterService.SignOutAsync();
            _isSignedIn = false;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        StateHasChanged();
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_userInput) || _isThinking) return;

        var input = _userInput.Trim();
        var userMsg = new SgLlmMessage { Role = "user", Content = input };
        _messages.Add(userMsg);
        
        _userInput = "";
        _isThinking = true;
        _streaming = false;
        _tokenCount = 0;
        _error = null;
        _streamingMsg = null;
        _renderCounter = 0;
        
        StateHasChanged();
        await ScrollToBottomAsync();

        try
        {
            if (!await PuterService.IsAvailableAsync())
            {
                _error = "Puter.js is not loaded.";
                _isThinking = false;
                return;
            }

            if (input.StartsWith("/img "))
            {
                var prompt = input.Substring(5);
                var imgSrc = await PuterService.Txt2ImgAsync(prompt);
                _messages.Add(new SgLlmMessage { Role = "assistant", Content = imgSrc });
                _isThinking = false;
            }
            else if (input.StartsWith("/kv_set "))
            {
                var parts = input.Substring(8).Split(' ', 2);
                if (parts.Length == 2)
                {
                    await PuterService.KvSetAsync(parts[0], parts[1]);
                    _messages.Add(new SgLlmMessage { Role = "assistant", Content = $"✅ Saved '{parts[0]}' to cloud." });
                }
                _isThinking = false;
            }
            else if (input.StartsWith("/kv_get "))
            {
                var key = input.Substring(8).Trim();
                var val = await PuterService.KvGetAsync(key);
                _messages.Add(new SgLlmMessage { Role = "assistant", Content = val != null ? $"📁 Value for '{key}': {val}" : "❌ Key not found." });
                _isThinking = false;
            }
            else
            {
                await PuterService.ChatAsync(input, _selectedModel);
            }
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _isThinking = false;
        }
        
        StateHasChanged();
        await ScrollToBottomAsync();
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
            var blob = await EnsureBlobModuleAsync();
            await blob.InvokeVoidAsync("scrollSelectorToBottom", ".sg-chat-messages", "smooth");
        }
        catch (JSDisconnectedException) { /* circuit gone — page navigated away */ }
        catch (ObjectDisposedException) { /* component is disposing */ }
    }

    private string GetHtml(SgLlmMessage msg)
    {
        if (_htmlCache.TryGetValue(msg.Id, out var cached)) return cached;
        // Basic fallback for non-markdown content (e.g. image URLs or simple status messages)
        if (msg.Content.StartsWith("http") && (msg.Content.EndsWith(".png") || msg.Content.EndsWith(".jpg") || msg.Content.Contains("puter.ai/ai/txt2img")))
        {
            // Only render http(s) image URLs; attribute-encode to prevent breaking out of src/onclick.
            if (Uri.TryCreate(msg.Content, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                var safeUrl = System.Web.HttpUtility.HtmlAttributeEncode(uri.AbsoluteUri);
                return $"<a href=\"{safeUrl}\" target=\"_blank\" rel=\"noopener\"><img src=\"{safeUrl}\" class=\"sg-chat-att-img\" style=\"max-width: 100%; cursor: pointer;\" /></a>";
            }
        }
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
        if (msg.Content.StartsWith("http") && (msg.Content.EndsWith(".png") || msg.Content.EndsWith(".jpg") || msg.Content.Contains("puter.ai/ai/txt2img"))) return;

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
            sb.AppendLine($"# Puter Chat Export - {DateTime.Now}");
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
            await blob.InvokeVoidAsync("downloadText", content, $"puter_chat_{ts}.{ext}", mime);
        }
        catch (Exception ex)
        {
            _error = $"Не удалось экспортировать чат: {ex.Message}";
        }
    }

    private void ClearHistory()
    {
        _messages.Clear();
        _htmlCache.Clear();
        _error = null;
        StateHasChanged();
    }

    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        PuterService.OnTokenReceived -= HandleToken;
        PuterService.OnChatComplete -= HandleComplete;
        PuterService.OnError -= HandleError;

        if (_llmModule is not null)
        {
            try { await _llmModule.DisposeAsync(); }
            catch (Microsoft.JSInterop.JSDisconnectedException) { /* circuit gone */ }
            catch (TaskCanceledException) { /* shutdown in flight */ }
            catch (ObjectDisposedException) { /* already disposed */ }
            _llmModule = null;
        }
        if (_blobModule is not null)
        {
            try { await _blobModule.DisposeAsync(); }
            catch (Microsoft.JSInterop.JSDisconnectedException) { /* circuit gone */ }
            catch (TaskCanceledException) { /* shutdown in flight */ }
            catch (ObjectDisposedException) { /* already disposed */ }
            _blobModule = null;
        }
        GC.SuppressFinalize(this);
    }
}
