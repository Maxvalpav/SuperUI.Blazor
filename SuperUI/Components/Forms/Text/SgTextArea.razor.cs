using SuperUI.Enums;
using SuperUI.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace SuperUI.Components;

/// <summary>Counter display mode for SgTextArea.</summary>
public enum SgTextAreaCounterMode
{
    /// <summary>Show current / max (e.g., 45/50).</summary>
    CharCount,
    /// <summary>Show remaining characters (e.g., 5 remaining).</summary>
    Remaining,
    /// <summary>Show both (e.g., 45/50, 5 remaining).</summary>
    Both
}

/// <summary>
/// A feature-rich multi-line text input with auto-resize, character counter,
/// clear button, validation, emoji picker, floating label, progress bar,
/// word/line counts, and extensive customization.
/// </summary>
public partial class SgTextArea : IDisposable
{
    private readonly string _id = "sg-ta-" + Guid.NewGuid().ToString("N")[..8];
    private string _hintId => _id + "-hint";
    private string _errorId => _id + "-err";
    private ElementReference _textareaRef;
    private IJSObjectReference? _module;
    private bool _focused;
    private string? _pasteValue;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    // ── Basic parameters ─────────────────────────────────────────────────

    /// <summary>Label displayed above or beside the textarea.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Position of the label relative to the textarea.</summary>
    [Parameter] public SgLabelPosition LabelPosition { get; set; } = SgLabelPosition.Top;

    /// <summary>Placeholder text when the textarea is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Hint text displayed below the textarea.</summary>
    [Parameter] public string? Hint { get; set; }

    /// <summary>Error text displayed below the textarea. Overrides validation messages.</summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>Marks the field as required (adds asterisk and native required attribute).</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Disables the textarea so it cannot be interacted with.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Makes the textarea read-only (content visible but not editable).</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Expands the field to fill its container width.</summary>
    [Parameter] public bool Block { get; set; } = true;

    /// <summary>Additional CSS class for the outer field wrapper.</summary>
    [Parameter] public string? CssClass { get; set; }

    // ── Behavior ─────────────────────────────────────────────────────────

    /// <summary>If true, updates the value on every keystroke. If false, waits for blur/change.</summary>
    [Parameter] public bool Immediate { get; set; } = true;

    /// <summary>Debounce delay in milliseconds before committing the value.</summary>
    [Parameter] public int Debounce { get; set; }

    /// <summary>Maximum character length allowed.</summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>Show a clear button when the textarea has a value.</summary>
    [Parameter] public bool AllowClear { get; set; } = true;

    /// <summary>Alias for <see cref="AllowClear"/>. Use AllowClear instead.</summary>
    [Parameter] public bool ShowClearButton { get => AllowClear; set => AllowClear = value; }

    /// <summary>Automatically focus the textarea on mount.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>Select all text when the textarea receives focus.</summary>
    [Parameter] public bool SelectAllOnFocus { get; set; }

    /// <summary>Auto-trim whitespace from the value when the textarea loses focus.</summary>
    [Parameter] public bool TrimOnBlur { get; set; }

    /// <summary>When true, MaxLength shows a warning but doesn't block further input.</summary>
    [Parameter] public bool SoftMaxLength { get; set; }

    /// <summary>When true, strips HTML and truncates to MaxLength on paste.</summary>
    [Parameter] public bool PasteSanitize { get; set; }

    // ── Visual rows & resize ─────────────────────────────────────────────

    /// <summary>Number of visible text rows.</summary>
    [Parameter] public int Rows { get; set; } = 3;

    /// <summary>Maximum number of visible rows when <see cref="AutoResize"/> is true. 0 = unlimited.</summary>
    [Parameter] public int MaxRows { get; set; }

    /// <summary>Automatically grow the textarea height to fit content.</summary>
    [Parameter] public bool AutoResize { get; set; }

    /// <summary>Minimum height in CSS units (e.g., "80px", "5em"). Used only when <see cref="AutoResize"/> is true.</summary>
    [Parameter] public string? AutoHeightMin { get; set; }

    /// <summary>Maximum height in CSS units (e.g., "300px", "20em"). Used only when <see cref="AutoResize"/> is true.</summary>
    [Parameter] public string? AutoHeightMax { get; set; }

    /// <summary>Controls the resize handle direction.</summary>
    [Parameter] public SgTextResize Resize { get; set; } = SgTextResize.Vertical;

    /// <summary>Show a custom styled resize grip in the bottom-right corner.</summary>
    [Parameter] public bool ShowResizeGrip { get; set; }

    // ── Text behavior ────────────────────────────────────────────────────

    /// <summary>Enables browser spellcheck. Null = browser default.</summary>
    [Parameter] public bool? Spellcheck { get; set; }

    /// <summary>Text wrapping: "soft" (default), "hard", or "off".</summary>
    [Parameter] public string? Wrap { get; set; }

    /// <summary>Tab index for keyboard navigation.</summary>
    [Parameter] public int? TabIndex { get; set; }

    /// <summary>Line-height multiplier for the textarea content. Default = 1.5.</summary>
    [Parameter] public double LineHeight { get; set; } = 1.5;

    // ── Label ────────────────────────────────────────────────────────────

    /// <summary>When true, the label floats up on focus or when the textarea has a value.</summary>
    [Parameter] public bool FloatingLabel { get; set; }

    // ── Icons & affixes ──────────────────────────────────────────────────

    /// <summary>Optional SVG/HTML string rendered as a prefix icon inside the textarea.</summary>
    [Parameter] public string? IconPrefix { get; set; }

    /// <summary>Optional SVG/HTML string rendered as a suffix icon inside the textarea.</summary>
    [Parameter] public string? IconSuffix { get; set; }

    // ── Counter ──────────────────────────────────────────────────────────

    /// <summary>Shows a character counter (requires <see cref="MaxLength"/>).</summary>
    [Parameter] public bool ShowCounter { get; set; }

    /// <summary>Counter display mode.</summary>
    [Parameter] public SgTextAreaCounterMode CounterMode { get; set; } = SgTextAreaCounterMode.CharCount;

    /// <summary>Color threshold percentage (0-100) for the counter warning. Default 80.</summary>
    [Parameter] public double CharCountPctThreshold { get; set; } = 80.0;

    /// <summary>Render a completely custom counter. When set, built-in counter logic is skipped.</summary>
    [Parameter] public RenderFragment? CounterTemplate { get; set; }

    // ── Additional counters ──────────────────────────────────────────────

    /// <summary>Show word count below the textarea.</summary>
    [Parameter] public bool ShowWordCount { get; set; }

    /// <summary>Show line count below the textarea.</summary>
    [Parameter] public bool ShowLineCount { get; set; }

    // ── Progress bar ─────────────────────────────────────────────────────

    /// <summary>Show a thin progress bar below the textarea indicating fill level relative to MaxLength.</summary>
    [Parameter] public bool ShowCharProgressBar { get; set; }

    // ── Emoji ────────────────────────────────────────────────────────────

    /// <summary>Show an emoji picker button next to the textarea.</summary>
    [Parameter] public bool ShowEmojiPicker { get; set; }

    /// <summary>Fires when an emoji is selected from the emoji picker.</summary>
    [Parameter] public EventCallback<string> OnEmojiSelected { get; set; }

    // ── Toolbar ──────────────────────────────────────────────────────────

    /// <summary>Show a bottom toolbar with clear, counters, and emoji button.</summary>
    [Parameter] public bool ShowToolbar { get; set; }

    /// <summary>Custom content rendered inside the bottom toolbar.</summary>
    [Parameter] public RenderFragment? ToolbarContent { get; set; }

    // ── Events ───────────────────────────────────────────────────────────

    /// <summary>Event raised when the textarea gains focus.</summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>Event raised when the textarea loses focus.</summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Computed state ───────────────────────────────────────────────────

    private CancellationTokenSource? _debounceCts;

    private bool IsInvalid => !string.IsNullOrEmpty(ErrorText) || HasValidationErrors;
    private string? DisplayedError => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationMessages.FirstOrDefault();
    private string? DescribedBy => IsInvalid ? _errorId : (!string.IsNullOrEmpty(Hint) ? _hintId : null);

    private string? _spellCheckAttr => Spellcheck.HasValue ? (Spellcheck.Value ? "true" : "false") : null;
    private string? _wrapAttr => string.IsNullOrEmpty(Wrap) ? null : Wrap;
    private string? _lineHeightStyle => LineHeight != 1.5 ? $"line-height:{LineHeight}" : null;
    private string? _autoHeightStyle => BuildAutoHeightStyle();
    private string? _combinedStyle => CombineStyles(_lineHeightStyle, _autoHeightStyle);

    private bool _hasValue => !string.IsNullOrEmpty(Value);
    private bool _showFloatingLabel => FloatingLabel && !string.IsNullOrEmpty(Label);

    private int _charLen => Value?.Length ?? 0;
    private int _wordCount => string.IsNullOrWhiteSpace(Value) ? 0 : Value!.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
    private int _lineCount => string.IsNullOrEmpty(Value) ? 0 : Value!.Split('\n').Length;
    private double _charPct => MaxLength.HasValue && MaxLength.Value > 0 ? (double)_charLen / MaxLength.Value * 100.0 : 0.0;
    private bool _counterWarn => MaxLength.HasValue && _charPct >= CharCountPctThreshold;

    private string ResizeClass => AutoResize ? "none" : Resize switch
    {
        SgTextResize.None       => "none",
        SgTextResize.Horizontal => "horizontal",
        SgTextResize.Both       => "both",
        _                       => "vertical"
    };

    private string? BuildAutoHeightStyle()
    {
        if (!AutoResize) return null;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(AutoHeightMin)) parts.Add($"min-height:{AutoHeightMin}");
        if (!string.IsNullOrEmpty(AutoHeightMax)) parts.Add($"max-height:{AutoHeightMax}");
        return parts.Count > 0 ? string.Join(";", parts) : null;
    }

    private static string? CombineStyles(string? a, string? b)
    {
        if (a == null && b == null) return null;
        if (a == null) return b;
        if (b == null) return a;
        return a + ";" + b;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (AutoResize) await CallAutoResizeAsync();
            if (AutoFocus) await FocusAsync();
        }
    }

    // ── Focus management ─────────────────────────────────────────────────

    private async Task FocusAsync()
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui.js");
            await _module.InvokeVoidAsync("focusElement", _textareaRef);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
    }

    private async Task OnFocusAsync(FocusEventArgs e)
    {
        _focused = true;
        if (SelectAllOnFocus)
        {
            try
            {
                _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui.js");
                await _module.InvokeVoidAsync("selectText", _textareaRef);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
        }
        await OnFocus.InvokeAsync(e);
    }

    private async Task OnBlurAsync(FocusEventArgs e)
    {
        _focused = false;
        if (TrimOnBlur && Value != null)
        {
            var trimmed = Value.Trim();
            if (trimmed != Value)
                await SetValueAsync(trimmed);
        }
        await OnBlur.InvokeAsync(e);
    }

    // ── Input handling ───────────────────────────────────────────────────

    private void OnInputAsync(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString() ?? string.Empty;

        if (SoftMaxLength && MaxLength.HasValue && raw.Length > MaxLength.Value)
        {
            // Soft: allow but counter shows warning
        }
        else if (!SoftMaxLength && MaxLength.HasValue && raw.Length > MaxLength.Value)
        {
            raw = raw[..MaxLength.Value];
        }

        if (Debounce > 0)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            var val = raw;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(Debounce, token);
                    await InvokeAsync(async () =>
                    {
                        await CommitAsync(val);
                        if (AutoResize) await CallAutoResizeAsync();
                    });
                }
                catch (TaskCanceledException) { }
            });
        }
        else if (Immediate)
        {
            _ = SafeCommitAsync(raw);
            if (AutoResize) _ = CallAutoResizeAsync();
        }
    }

    private Task OnChangeAsync(ChangeEventArgs e) => !Immediate && Debounce <= 0 ? CommitAsync(e.Value?.ToString()) : Task.CompletedTask;

    private async Task OnPasteAsync(ClipboardEventArgs e)
    {
        if (!PasteSanitize) return;
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui.js");
            var text = await _module.InvokeAsync<string?>("getPastedText");
            if (text == null) return;

            // Strip HTML tags
            var plain = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", "");

            // Truncate to MaxLength
            if (!SoftMaxLength && MaxLength.HasValue && plain.Length > MaxLength.Value)
                plain = plain[..MaxLength.Value];

            _pasteValue = plain;
            StateHasChanged();
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
    }

    // ── Emoji handling ───────────────────────────────────────────────────

    private bool _emojiPickerOpen;

    private void ToggleEmojiPicker()
    {
        if (Disabled || ReadOnly) return;
        _emojiPickerOpen = !_emojiPickerOpen;
    }

    private async Task SelectEmojiAsync(string emoji)
    {
        _emojiPickerOpen = false;
        await OnEmojiSelected.InvokeAsync(emoji);
        // Append emoji to current value
        var current = Value ?? string.Empty;
        var next = current + emoji;
        if (MaxLength.HasValue && next.Length > MaxLength.Value && !SoftMaxLength)
            next = next[..MaxLength.Value];
        await SetValueAsync(next);
        if (AutoResize) await CallAutoResizeAsync();
        StateHasChanged();
    }

    // ── Auto-resize ──────────────────────────────────────────────────────

    private async Task CallAutoResizeAsync()
    {
        if (_disposed) return;
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui.js");
            if (_disposed) return;
            await _module.InvokeVoidAsync("autoResizeTextarea", _textareaRef, Rows, MaxRows);
        }
        catch (JSException) { }
        catch (TaskCanceledException) { }
    }

    // ── Value commit helpers ─────────────────────────────────────────────

    private async Task SafeCommitAsync(string? raw)
    {
        try { await CommitAsync(raw); }
        catch (Exception ex) { Console.Error.WriteLine($"[SgTextArea.CommitAsync] {ex}"); }
    }

    private Task ClearAsync() => SetValueAsync(string.Empty);
    private Task CommitAsync(string? raw) => SetValueAsync(raw ?? string.Empty);

    // ── Emoji data ───────────────────────────────────────────────────────

    private static readonly Dictionary<string, List<string>> EmojiCategories = new()
    {
        ["Smileys"] = new() { "😀", "😃", "😄", "😁", "😅", "😂", "🤣", "😊", "😇", "🙂", "😉", "😌", "😍", "🥰", "😘", "😗", "😋", "😛", "😜", "🤪", "😝", "🤑", "🤗", "🤭", "🤫", "🤔", "😐", "😑", "😶", "😏", "😒", "😞", "😔", "😟", "😕", "🙁", "😣", "😖", "😫", "😩", "🥺", "😢", "😭", "😤", "😠", "😡", "🤬", "🤯", "😳", "🥵", "🥶", "😱", "😨", "😰", "😥", "😓", "🤗", "🤔", "🤭", "🤫", "🤥", "😶", "😐", "😑", "😬", "🙄", "😯", "😦", "😧", "😮", "😲", "😴", "🤤", "😪", "😵", "🤐", "🥴", "🤢", "🤮", "🤧", "😷", "🤒", "🤕", "🤑", "🤠", "😈", "👿", "👹", "👺", "💀", "☠️", "💩", "🤡", "👻", "💀" },
        ["Gestures"] = new() { "👋", "🤚", "🖐", "✋", "🖖", "👌", "🤌", "🤏", "✌️", "🤞", "🤟", "🤘", "🤙", "👈", "👉", "👆", "🖕", "👇", "☝️", "👍", "👎", "✊", "👊", "🤛", "🤜", "👏", "🙌", "👐", "🤲", "🤝", "🙏", "✍️", "💅", "🤳", "💪", "🦵", "🦶", "👂", "🦻", "👃", "🧠", "🦷", "🦴", "👀", "👁", "👅", "👄" },
        ["Hearts"] = new() { "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍", "🤎", "💕", "💞", "💓", "💗", "💖", "💘", "💝", "💟", "❣️", "💔", "❤️‍🔥", "❤️‍🩹", "💌" },
        ["Objects"] = new() { "📝", "✏️", "📖", "📕", "📗", "📘", "📙", "📚", "📓", "📒", "📃", "📜", "📄", "📰", "🗞️", "🔖", "🏷️", "💰", "💵", "💴", "💶", "💷", "💸", "💳", "🧾", "✉️", "📧", "📨", "📩", "📤", "📥", "📦", "📫", "📪", "📬", "📭", "📮", "🗳️", "✂️", "🔒", "🔓", "🔏", "🔐", "🔑", "🗝️", "🔨", "🪓", "⛏️", "⚒️", "🛠️", "🗡️", "⚔️", "🔫", "🛡️", "🔧", "🔩", "⚙️", "🗜️", "⚖️", "🦯", "🔗", "⛓️", "🧰", "🧲" },
        ["Symbols"] = new() { "✅", "❌", "❓", "❔", "❕", "❗", "➕", "➖", "➗", "✖️", "♻️", "💯", "🔥", "⭐", "🌟", "✨", "💡", "🔔", "📌", "📍", "🎯", "🏆", "🥇", "🥈", "🥉", "🎖️", "🏅", "🎁", "🎀", "🎉", "🎊", "🎈", "🔮", "🪄" },
        ["Nature"] = new() { "☀️", "🌤", "⛅", "🌥", "☁️", "🌦", "🌧", "⛈", "🌩", "🌨", "❄️", "☃️", "⛄", "🔥", "💧", "🌊", "🌈", "☔", "⚡", "🌪", "🌫", "🍀", "🌿", "🌱", "🌴", "🌵", "🌻", "🌺", "🌸", "🌷", "🌹", "💐", "🍄", "🐚", "🐌", "🐛", "🦋", "🐝", "🐞", "🦗", "🐜" },
    };

    // ── Dispose ──────────────────────────────────────────────────────────

    public override void Dispose()
    {
        if (_disposed) return;
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        if (_module is not null)
            _ = _module.DisposeAsync();
        base.Dispose();
    }
}
