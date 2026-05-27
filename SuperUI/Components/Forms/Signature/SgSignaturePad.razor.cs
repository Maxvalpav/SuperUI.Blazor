using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;

namespace SuperUI.Components;

public sealed partial class SgSignaturePad : SgJsComponentBase
{
    private ElementReference _canvasRef;
    private bool _hasContent;
    private bool _focused;

    protected override string ModulePath => "./_content/SuperUI/superui-signature-pad.js";

    // ── Core Parameters ─────────────────────────────────────────────────

    /// <summary>Canvas width (CSS value).</summary>
    [Parameter] public string Width { get; set; } = "100%";
    /// <summary>Canvas height (CSS value).</summary>
    [Parameter] public string Height { get; set; } = "280px";
    /// <summary>Pen/stroke color (hex).</summary>
    [Parameter] public string PenColor { get; set; } = "#1e293b";
    /// <summary>Pen/stroke width in pixels.</summary>
    [Parameter] public double PenWidth { get; set; } = 2;
    /// <summary>Background color (hex or transparent).</summary>
    [Parameter] public string BackgroundColor { get; set; } = "#ffffff";
    /// <summary>Prevents drawing when true.</summary>
    [Parameter] public bool ReadOnly { get; set; }
    /// <summary>Shows the floating label and action toolbar.</summary>
    [Parameter] public bool ShowActions { get; set; } = true;
    /// <summary>Maximum undo steps stored.</summary>
    [Parameter] public int MaxUndoSteps { get; set; } = 100;

    // ── Signature Value (data URL binding) ──────────────────────────────

    /// <summary>Current signature as a data URL (PNG/JPEG). Bindable.</summary>
    [Parameter] public string? Value { get; set; }
    /// <summary>Fired when the signature changes.</summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    // ── Export ─────────────────────────────────────────────────────────

    /// <summary>Image format for export ("image/png" or "image/jpeg").</summary>
    [Parameter] public string ImageFormat { get; set; } = "image/png";
    /// <summary>JPEG quality (0-1).</summary>
    [Parameter] public double ImageQuality { get; set; } = 0.92;

    // ── Guide Line ─────────────────────────────────────────────────────

    /// <summary>Shows a guide/baseline line on the canvas.</summary>
    [Parameter] public bool ShowGuideLine { get; set; }
    /// <summary>Guide line color.</summary>
    [Parameter] public string GuideLineColor { get; set; } = "#d1d5db";
    /// <summary>Optional label text at the end of the guide line.</summary>
    [Parameter] public string? GuideLineText { get; set; }
    /// <summary>Guide line style.</summary>
    [Parameter] public string GuideLineStyle { get; set; } = "Dashed";

    // ── Variants ───────────────────────────────────────────────────────

    /// <summary>Visual variant of the pad.</summary>
    [Parameter] public SgSignatureVariant Variant { get; set; } = SgSignatureVariant.Default;
    /// <summary>When true, reduces the pad for initials-only input.</summary>
    [Parameter] public bool InitialsOnly { get; set; }

    // ── Validation ─────────────────────────────────────────────────────

    /// <summary>Minimum number of strokes required.</summary>
    [Parameter] public int MinStrokes { get; set; }
    /// <summary>Maximum number of strokes allowed (0 = unlimited).</summary>
    [Parameter] public int MaxStrokes { get; set; }
    /// <summary>Marks the field as required (adds asterisk to label).</summary>
    [Parameter] public bool Required { get; set; }

    // ── Per-button Visibility ──────────────────────────────────────────

    /// <summary>Shows the Undo button.</summary>
    [Parameter] public bool ShowUndo { get; set; } = true;
    /// <summary>Shows the Redo button.</summary>
    [Parameter] public bool ShowRedo { get; set; } = true;
    /// <summary>Shows the Clear button.</summary>
    [Parameter] public bool ShowClear { get; set; } = true;
    /// <summary>Shows the Copy button.</summary>
    [Parameter] public bool ShowCopy { get; set; } = true;
    /// <summary>Shows the Download button.</summary>
    [Parameter] public bool ShowDownload { get; set; } = true;

    // ── Events ─────────────────────────────────────────────────────────

    /// <summary>Fired when drawing starts.</summary>
    [Parameter] public EventCallback OnDrawStart { get; set; }
    /// <summary>Fired when drawing ends.</summary>
    [Parameter] public EventCallback OnDrawEnd { get; set; }
    /// <summary>Fired when signature content changes.</summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    // ── Label ──────────────────────────────────────────────────────────

    /// <summary>Label text displayed above or floating on the pad.</summary>
    [Parameter] public string? Label { get; set; }
    /// <summary>Placeholder text shown when canvas is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    protected override async ValueTask OnInteractiveAsync()
    {
        await SafeInvokeVoidAsync("init", _canvasRef, SelfRef, PenColor, PenWidth,
            BackgroundColor, ReadOnly, ShowGuideLine, GuideLineColor,
            GuideLineText, GuideLineStyle, InitialsOnly, Label);

        if (!string.IsNullOrEmpty(Value))
            await SafeInvokeVoidAsync("loadImage", _canvasRef, Value);
    }

    protected override async ValueTask OnDisposingAsync()
    {
        await SafeInvokeVoidAsync("dispose", _canvasRef);
    }

    // ── Button Click Wrappers ──────────────────────────────────────────

    private Task OnUndoClick(MouseEventArgs e) => UndoAsync();
    private Task OnRedoClick(MouseEventArgs e) => RedoAsync();
    private Task OnCopyClick(MouseEventArgs e) => CopyToClipboardAsync();
    private Task OnDownloadClick(MouseEventArgs e) => DownloadAsync();
    private Task OnClearClick(MouseEventArgs e) => ClearAsync();

    // ── Public Methods ─────────────────────────────────────────────────

    /// <summary>Clears the signature pad.</summary>
    public async Task ClearAsync()
    {
        await SafeInvokeVoidAsync("clear", _canvasRef);
        _hasContent = false;
        Value = null;
        await ValueChanged.InvokeAsync(null);
        await OnChange.InvokeAsync(false);
    }

    /// <summary>Gets the signature as a data URL.</summary>
    public async Task<string> GetImageAsync(string? format = null, double? quality = null)
    {
        return await SafeInvokeAsync<string>("getDataUrl", _canvasRef,
            format ?? ImageFormat, quality ?? ImageQuality) ?? "";
    }

    /// <summary>Gets the signature auto-trimmed to remove whitespace margins.</summary>
    public async Task<string> GetTrimmedImageAsync(string? format = null, double? quality = null)
    {
        return await SafeInvokeAsync<string>("getTrimmedDataUrl", _canvasRef,
            format ?? ImageFormat, quality ?? ImageQuality) ?? "";
    }

    /// <summary>Checks if the signature pad is empty.</summary>
    public async Task<bool> IsEmptyAsync()
    {
        return await SafeInvokeAsync<bool>("isEmpty", _canvasRef);
    }

    /// <summary>Gets the number of strokes drawn.</summary>
    public async Task<int> GetStrokeCountAsync()
    {
        return await SafeInvokeAsync<int>("getStrokeCount", _canvasRef);
    }

    /// <summary>Undoes the last stroke.</summary>
    public async Task<bool> UndoAsync()
    {
        var result = await SafeInvokeAsync<bool>("undo", _canvasRef);
        var empty = await IsEmptyAsync();
        _hasContent = !empty;
        await SyncValueAsync();
        await OnChange.InvokeAsync(!empty);
        return result;
    }

    /// <summary>Redoes the last undone stroke.</summary>
    public async Task<bool> RedoAsync()
    {
        var result = await SafeInvokeAsync<bool>("redo", _canvasRef);
        _hasContent = true;
        await SyncValueAsync();
        await OnChange.InvokeAsync(true);
        return result;
    }

    /// <summary>Downloads the signature as an image file.</summary>
    public async Task DownloadAsync(string? filename = null, string? format = null, double? quality = null)
    {
        await SafeInvokeVoidAsync("download", _canvasRef, filename ?? "signature.png",
            format ?? ImageFormat, quality ?? ImageQuality);
    }

    /// <summary>Copies the signature to clipboard.</summary>
    public async Task<bool> CopyToClipboardAsync()
    {
        return await SafeInvokeAsync<bool>("copyToClipboard", _canvasRef);
    }

    /// <summary>Changes pen color dynamically.</summary>
    public async Task SetPenColorAsync(string color)
    {
        await SafeInvokeVoidAsync("setPenColor", _canvasRef, color);
    }

    /// <summary>Changes pen width dynamically.</summary>
    public async Task SetPenWidthAsync(double width)
    {
        await SafeInvokeVoidAsync("setPenWidth", _canvasRef, width);
    }

    /// <summary>Sets read-only mode dynamically.</summary>
    public async Task SetReadOnlyAsync(bool readOnly)
    {
        await SafeInvokeVoidAsync("setReadOnly", _canvasRef, readOnly);
    }

    /// <summary>Sets background color dynamically.</summary>
    public async Task SetBackgroundColorAsync(string color)
    {
        await SafeInvokeVoidAsync("setBgColor", _canvasRef, color);
    }

    /// <summary>Loads a signature image onto the canvas from a data URL.</summary>
    public async Task<bool> LoadImageAsync(string dataUrl)
    {
        var result = await SafeInvokeAsync<bool>("loadImage", _canvasRef, dataUrl);
        if (result)
        {
            _hasContent = true;
            Value = dataUrl;
            await ValueChanged.InvokeAsync(dataUrl);
            await OnChange.InvokeAsync(true);
        }
        return result;
    }

    /// <summary>Replays the signature drawing animation.</summary>
    public async Task<bool> ReplayAsync(int? strokeIndex = null)
    {
        return await SafeInvokeAsync<bool>("replay", _canvasRef, strokeIndex ?? -1);
    }

    /// <summary>Gets the trimmed data URL (auto-cropped). Alias for <see cref="GetTrimmedImageAsync"/>.</summary>
    public Task<string> ExportTrimmedAsync(string? format = null, double? quality = null) =>
        GetTrimmedImageAsync(format, quality);

    // ── JSInvokable ────────────────────────────────────────────────────

    [JSInvokable]
    public async Task OnDrawStartJs(double x, double y)
    {
        await OnDrawStart.InvokeAsync();
    }

    [JSInvokable]
    public async Task OnDrawEndJs()
    {
        _hasContent = true;
        await OnDrawEnd.InvokeAsync();
    }

    [JSInvokable]
    public async Task OnChangeJs(bool hasContent, int strokeCount)
    {
        _hasContent = hasContent;
        await OnChange.InvokeAsync(hasContent);
        await SyncValueAsync();
    }

    private async Task SyncValueAsync()
    {
        var dataUrl = await SafeInvokeAsync<string>("getDataUrl", _canvasRef, ImageFormat, ImageQuality);
        if (dataUrl != Value)
        {
            Value = dataUrl;
            await ValueChanged.InvokeAsync(dataUrl);
        }
    }

    // ── CSS Classes ────────────────────────────────────────────────────

    private string GetRootClasses()
    {
        var sb = new List<string> { "sgc-signature-pad" };
        if (_hasContent) sb.Add("sgc-signature-has-content");
        if (ReadOnly) sb.Add("sgc-signature-readonly");
        if (InitialsOnly) sb.Add("sgc-signature-initials");
        if (Variant == SgSignatureVariant.Frameless) sb.Add("sgc-signature-frameless");
        if (!string.IsNullOrEmpty(Label)) sb.Add("sgc-field");
        if (!string.IsNullOrEmpty(Label) && _focused) sb.Add("sgc-field-active");
        if (!string.IsNullOrEmpty(CssClass)) sb.Add(CssClass);
        var style = Styles().AddStyle("width", Width).AddStyle("height", Height).Build();
        return string.Join(" ", sb);
    }

    private bool ShowAnyAction => ShowActions && (ShowUndo || ShowRedo || ShowClear || ShowCopy || ShowDownload);
}
