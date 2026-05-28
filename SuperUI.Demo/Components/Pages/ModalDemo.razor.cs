using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Components;
using SuperUI.Enums;

namespace SuperUI.Demo.Components.Pages;

/// <summary>
/// Demo page for the SgModal component.
/// </summary>
public partial class ModalDemo : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ── Event log ─────────────────────────────────────────────────────
    private record EventEntry(string Type, string Message, string Time);

    private readonly List<EventEntry> _events = new();

    private void AddEvent(string type, string message)
    {
        _events.Add(new EventEntry(type, message, DateTime.Now.ToString("HH:mm:ss.fff")));
        if (_events.Count > 100) _events.RemoveRange(0, _events.Count - 100);
    }

    // ── State flags ───────────────────────────────────────────────────
    private bool _showBasic, _showForm, _showConfirm, _showFullscreen;
    private bool _showSize, _showAnim, _showPos;
    private bool _showDraggable, _showGlass, _showNoPad, _showCombo;
    private bool _showConstructor, _constructorOpened;

    // Advanced feature flags
    private bool _showLoadingTmp;
    private bool _isLoading;
    private bool _showBackdropStatic;
    private bool _showBackdropBlur;
    private bool _showMaximize;
    private bool _showResizable;
    private bool _showShortcut;
    private bool _showStacked1, _showStacked2;
    private bool _showResponsive;
    private bool _showMinimizeDemo;

    private string _lastEvent = "";

    // ── Form state ────────────────────────────────────────────────────
    private string? _formName, _formEmail, _formRole;
    private readonly List<string> _roles = new() { "Admin", "Manager", "User", "Guest" };

    // ── Size modal state ──────────────────────────────────────────────
    private SgModalSize _activeSize;
    private string _activeSizeLabel = "";

    // ── Animation modal state ─────────────────────────────────────────
    private SgModalAnimation _activeAnim;
    private string _activeAnimLabel = "";

    // ── Position modal state ──────────────────────────────────────────
    private SgModalPosition _activePos;
    private string _activePosLabel = "";

    // ── Combo modal state ─────────────────────────────────────────────
    private SgModalAnimation _comboAnim;
    private string _comboAnimLabel = "";
    private SgModalSize _comboSize;
    private string _comboSizeLabel = "";

    // ── Constructor state ─────────────────────────────────────────────
    private SgModalSize _cSize = SgModalSize.Md;
    private SgModalAnimation _cAnim = SgModalAnimation.Scale;
    private SgModalPosition _cPos = SgModalPosition.Center;
    private bool _cDrag, _cFull, _cShowClose = true, _cEsc = true, _cShowMinimize, _cShowMaximize;
    private string[] _generatedCode = Array.Empty<string>();
    private string _copyLabel = "Copy";

    // ── Size definitions ──────────────────────────────────────────────
    private record SizeDef(SgModalSize Value, string Label, string Width, string PreviewW);
    private readonly List<SizeDef> _sizes = new()
    {
        new(SgModalSize.Sm, "Sm", "400px", "60%"),
        new(SgModalSize.Md, "Md", "520px", "80%"),
        new(SgModalSize.Lg, "Lg", "720px", "95%"),
        new(SgModalSize.Xl, "Xl", "960px", "100%"),
    };

    // ── Animation definitions ─────────────────────────────────────────
    private record AnimDef(SgModalAnimation Value, string Label);
    private readonly List<AnimDef> _animations = new()
    {
        new(SgModalAnimation.None, "None"),
        new(SgModalAnimation.Fade, "Fade"),
        new(SgModalAnimation.Scale, "Scale"),
        new(SgModalAnimation.Zoom, "Zoom"),
        new(SgModalAnimation.Slide, "Slide"),
        new(SgModalAnimation.SlideUp, "SlideUp"),
        new(SgModalAnimation.SlideDown, "SlideDown"),
        new(SgModalAnimation.SlideLeft, "SlideLeft"),
        new(SgModalAnimation.SlideRight, "SlideRight"),
    };

    // ── Handler methods ──────────────────────────────────────────────
    private void OpenSizeModal(SgModalSize size)
    {
        _activeSize = size;
        _activeSizeLabel = _sizes.First(s => s.Value == size).Label;
        _showSize = true;
        AddEvent("opened", $"Size modal: {_activeSizeLabel}");
    }

    private void OpenAnimModal(SgModalAnimation anim)
    {
        _activeAnim = anim;
        _activeAnimLabel = _animations.First(a => a.Value == anim).Label;
        _showAnim = true;
        AddEvent("opened", $"Animation modal: {_activeAnimLabel}");
    }

    private void OpenPosModal(SgModalPosition pos)
    {
        _activePos = pos;
        _activePosLabel = pos.ToString();
        _showPos = true;
        AddEvent("opened", $"Position modal: {_activePosLabel}");
    }

    private void OpenComboModal(SgModalAnimation anim, SgModalSize size)
    {
        _comboAnim = anim;
        _comboAnimLabel = _animations.First(a => a.Value == anim).Label;
        _comboSize = size;
        _comboSizeLabel = _sizes.First(s => s.Value == size).Label;
        _showCombo = true;
        AddEvent("opened", $"Combo modal: {_comboAnimLabel} + {_comboSizeLabel}");
    }

    private async Task OpenLoadingModal()
    {
        _isLoading = true;
        _showLoadingTmp = true;
        AddEvent("opened", "Loading modal");
        await Task.Delay(2000);
        _isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private void SaveForm()
    {
        AddEvent("form", $"Saved: {_formName}, {_formEmail}, {_formRole}");
        _showForm = false;
    }

    private void ConfirmDelete()
    {
        AddEvent("delete", "Item deleted");
        _showConfirm = false;
    }

    // ── Segmented options ─────────────────────────────────────────────
    private readonly List<SgSegmentedOption<SgModalSize>> _sizeSegOpts = new()
    {
        new() { Value = SgModalSize.Sm, Label = "Sm" },
        new() { Value = SgModalSize.Md, Label = "Md" },
        new() { Value = SgModalSize.Lg, Label = "Lg" },
        new() { Value = SgModalSize.Xl, Label = "Xl" },
    };

    private readonly List<SgSegmentedOption<SgModalPosition>> _posSegOpts = new()
    {
        new() { Value = SgModalPosition.Center, Label = "Center" },
        new() { Value = SgModalPosition.Top, Label = "Top" },
        new() { Value = SgModalPosition.Bottom, Label = "Bottom" },
    };

    private readonly List<SgSegmentedOption<bool>> _boolSegOpts = new()
    {
        new() { Value = false, Label = "Off" },
        new() { Value = true, Label = "On" },
    };

    private readonly List<SgSegmentedOption<bool>> _boolSegRevOpts = new()
    {
        new() { Value = true, Label = "On" },
        new() { Value = false, Label = "Off" },
    };

    private readonly List<SgModalAnimation> _animItems = Enum.GetValues<SgModalAnimation>().ToList();

    // ── Lifecycle ─────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        UpdateCode();
    }

    private void UpdateCode()
    {
        var lines = new List<string>
        {
            "<SgModal @bind-Visible=\"_visible\"",
            $"         Size=\"SgModalSize.{_cSize}\"",
            $"         Animation=\"SgModalAnimation.{_cAnim}\"",
            $"         Position=\"SgModalPosition.{_cPos}\"",
        };
        if (_cDrag) lines.Add("         Draggable=\"true\"");
        if (_cFull) lines.Add("         FullScreen=\"true\"");
        if (_cShowMinimize) lines.Add("         ShowMinimize=\"true\"");
        if (_cShowMaximize) lines.Add("         ShowMaximize=\"true\"");
        if (!_cShowClose) lines.Add("         ShowClose=\"false\"");
        if (!_cEsc) lines.Add("         CloseOnEscape=\"false\"");
        lines.Add("         Title=\"Modal Title\">");
        lines.Add("    <ChildContent>");
        lines.Add("        <!-- Your content here -->");
        lines.Add("    </ChildContent>");
        lines.Add("    <FooterContent>");
        lines.Add("        <SgButton Text=\"Close\" OnClick=\"@(() => _visible = false)\" />");
        lines.Add("    </FooterContent>");
        lines.Add("</SgModal>");
        _generatedCode = lines.ToArray();
    }

    private async Task CopyCode()
    {
        var code = string.Join("\n", _generatedCode);
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", code);
            _copyLabel = "Copied!";
            StateHasChanged();
            await Task.Delay(2000);
            _copyLabel = "Copy";
            StateHasChanged();
        }
        catch
        {
            _copyLabel = "Failed";
            StateHasChanged();
            await Task.Delay(1500);
            _copyLabel = "Copy";
            StateHasChanged();
        }
    }

    // ── Property tables ──────────────────────────────────────────────
    private List<PropertyPanelItem> _keyFeatures = new()
    {
        new() { Label = "Animations", Value = "9 types", BadgeText = "Scale/Fade/Zoom/Slide", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "Sizes", Value = "4 presets", BadgeText = "Sm/Md/Lg/Xl", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "Positions", Value = "3 modes", BadgeText = "Center/Top/Bottom", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "Draggable", Value = "bool", BadgeText = "Drag header", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "FullScreen", Value = "bool", BadgeText = "Immersive", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "Glass", Value = "bool", BadgeText = "Glassmorphism", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "NoPadding", Value = "bool", BadgeText = "Edge-to-edge", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "Resizable", Value = "bool", BadgeText = "Drag edges", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "ShowMinimize", Value = "bool", BadgeText = "Minimize btn", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "Icon", Value = "string", BadgeText = "Heroicons SVG", BadgeVariant = SgBadgeVariant.Info },
    };

    private List<PropertyPanelItem> _modalProperties = new()
    {
        new() { Label = "Visible", Value = "bool", BadgeText = "Visibility", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "Title", Value = "string?", BadgeText = "Header text", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "ChildContent", Value = "RenderFragment?", BadgeText = "Body", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "FooterContent", Value = "RenderFragment?", BadgeText = "Footer", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "HeaderContent", Value = "RenderFragment?", BadgeText = "Custom header", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "Icon", Value = "string?", BadgeText = "Title icon", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "Size", Value = "SgModalSize", BadgeText = "Sm/Md/Lg/Xl", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "Width/MaxWidth/MinWidth", Value = "string?", BadgeText = "Custom CSS", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "FullScreen", Value = "bool", BadgeText = "Overrides Size", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "Position", Value = "SgModalPosition", BadgeText = "Center/Top/Bottom", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "Animation", Value = "SgModalAnimation", BadgeText = "9 variants", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "ShowClose", Value = "bool", BadgeText = "Close button", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "CloseIcon", Value = "string?", BadgeText = "Custom SVG", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "CloseOnBackdrop", Value = "bool", BadgeText = "Click backdrop", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "CloseOnEscape", Value = "bool", BadgeText = "ESC key", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "Draggable", Value = "bool", BadgeText = "Drag header", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "Resizable", Value = "bool", BadgeText = "Drag edges", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "NoPadding", Value = "bool", BadgeText = "Body padding", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "Glass", Value = "bool", BadgeText = "Glass effect", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "AutoFocus", Value = "bool", BadgeText = "Auto focus first", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "TrapFocus", Value = "bool", BadgeText = "Focus trap", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "ScrollLock", Value = "bool", BadgeText = "Body scroll lock", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "Loading", Value = "bool", BadgeText = "Loading overlay", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "ResponsiveMode", Value = "bool", BadgeText = "Auto fullscreen", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "BackdropBlur", Value = "string?", BadgeText = "CSS blur", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "BackdropDismiss", Value = "bool", BadgeText = "Allow dismiss", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "ShowMinimize", Value = "bool", BadgeText = "Minimize btn", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "ShowMaximize", Value = "bool", BadgeText = "Maximize btn", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "ShortcutSubmit", Value = "string?", BadgeText = "Hotkey", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "Body/Header/FooterClass", Value = "string?", BadgeText = "CSS class", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "CustomZIndex", Value = "int?", BadgeText = "Z-index", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "OnClose", Value = "EventCallback", BadgeText = "After close", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "OnClosing", Value = "EventCallback", BadgeText = "Before close", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "OnOpened", Value = "EventCallback", BadgeText = "After open", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "OnSubmit", Value = "EventCallback", BadgeText = "Shortcut submit", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "OnMaximizedChanged", Value = "EventCallback<bool>", BadgeText = "Maximize toggle", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "OnMinimized", Value = "EventCallback", BadgeText = "Minimize click", BadgeVariant = SgBadgeVariant.Success },
    };

    private List<PropertyPanelItem> _animEnum = new()
    {
        new() { Label = "None", Value = "No animation", BadgeText = "Instant", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "Fade", Value = "Opacity fade", BadgeText = "Smooth", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "Scale", Value = "Scale in/out", BadgeText = "Default", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "Zoom", Value = "Zoom from 0.3", BadgeText = "Dramatic", BadgeVariant = SgBadgeVariant.Warn },
        new() { Label = "Slide", Value = "Slide up (legacy)", BadgeText = "Compat", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "SlideUp", Value = "Slide from bottom", BadgeText = "Upward", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "SlideDown", Value = "Slide from top", BadgeText = "Downward", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "SlideLeft", Value = "Slide from right", BadgeText = "Leftward", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "SlideRight", Value = "Slide from left", BadgeText = "Rightward", BadgeVariant = SgBadgeVariant.Info },
    };

    private List<PropertyPanelItem> _sizeEnum = new()
    {
        new() { Label = "Sm", Value = "400px", BadgeText = "Small", BadgeVariant = SgBadgeVariant.Default },
        new() { Label = "Md", Value = "520px", BadgeText = "Medium (default)", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "Lg", Value = "720px", BadgeText = "Large", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "Xl", Value = "960px", BadgeText = "Extra large", BadgeVariant = SgBadgeVariant.Warn },
    };

    private List<PropertyPanelItem> _posEnum = new()
    {
        new() { Label = "Center", Value = "Middle of screen", BadgeText = "Default", BadgeVariant = SgBadgeVariant.Success },
        new() { Label = "Top", Value = "Top of screen", BadgeText = "Top-aligned", BadgeVariant = SgBadgeVariant.Info },
        new() { Label = "Bottom", Value = "Bottom of screen", BadgeText = "Bottom-aligned", BadgeVariant = SgBadgeVariant.Info },
    };
}
