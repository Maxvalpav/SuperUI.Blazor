using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;

namespace SuperUI.Components;

public sealed partial class SgImageEditor : SgJsComponentBase
{
    private ElementReference _canvasRef;
    private ElementReference _overlayRef;
    private ElementReference _canvasStageRef;
    private ElementReference _fileInputRef;

    private string _filter = "";
    private int _zoomPercent = 100;
    private bool _imageLoaded;
    private string? _error;
    private bool _canUndo;
    private bool _canRedo;
    private bool _cropActive;
    private EditorTool _activeTool = EditorTool.Select;

    private static readonly string[] SvgIconSelect = ["M3 3l7.07 16.97 2.51-7.39 7.39-2.51L3 3z", "M13 13l6 6"];
    private static readonly string[] SvgIconCrop = ["M6.13 1L6 16a2 2 0 0 0 2 2h15", "M1 6.13L16 6a2 2 0 0 1 2 2v15"];

    protected override string ModulePath => "./_content/SuperUI/superui-image-editor.js";

    /// <summary>Image source URL. Loaded on parameter set.</summary>
    [Parameter, EditorRequired] public string Src { get; set; } = "";

    /// <summary>Alternative text for the image.</summary>
    [Parameter] public string? Alt { get; set; }

    /// <summary>Canvas width (CSS value, e.g. "100%", "800px").</summary>
    [Parameter] public string Width { get; set; } = "100%";

    /// <summary>Canvas height (CSS value, e.g. "600px").</summary>
    [Parameter] public string Height { get; set; } = "600px";

    /// <summary>Maximum width (CSS value).</summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>Pen/stroke color for drawing tool (CSS color).</summary>
    [Parameter] public string PenColor { get; set; } = "#1e293b";

    /// <summary>Pen/stroke width in pixels.</summary>
    [Parameter] public double PenWidth { get; set; } = 2;

    /// <summary>Fired when the image finishes loading.</summary>
    [Parameter] public EventCallback<int> OnImageWidthChanged { get; set; }

    /// <summary>Fired when the image finishes loading.</summary>
    [Parameter] public EventCallback<int> OnImageHeightChanged { get; set; }

    /// <summary>Fired when the active tool changes.</summary>
    [Parameter] public EventCallback<EditorTool> ActiveToolChanged { get; set; }

    protected override async ValueTask OnInteractiveAsync()
    {
        await SafeInvokeVoidAsync("init", _canvasRef, _overlayRef, SelfRef,
            new { PenColor, PenWidth });

        if (!string.IsNullOrEmpty(Src))
            await LoadImageAsync(Src);
    }

    protected override async ValueTask OnDisposingAsync()
    {
        await SafeInvokeVoidAsync("dispose", _canvasRef);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(Src) && !_imageLoaded && Module is not null)
        {
            await LoadImageAsync(Src);
        }
    }

    private async Task LoadImageAsync(string src)
    {
        _error = null;
        var ok = await SafeInvokeAsync<bool>("loadImage", _canvasRef, src);
        if (!ok)
            _error = "Failed to load image. The URL may be inaccessible or the format is unsupported.";
        StateHasChanged();
    }

    // ── Tool switching ──

    private async Task SetTool(EditorTool tool)
    {
        _activeTool = tool;
        if (tool != EditorTool.Crop) _cropActive = false;
        await SafeInvokeVoidAsync("setTool", _canvasRef, tool.ToString().ToLowerInvariant());
        await ActiveToolChanged.InvokeAsync(tool);
    }

    // ── Toolbar actions ──

    private async Task RotateCwAsync()
    {
        await SafeInvokeVoidAsync("rotate", _canvasRef, 90);
    }

    private async Task RotateCcwAsync()
    {
        await SafeInvokeVoidAsync("rotate", _canvasRef, -90);
    }

    private async Task FlipHAsync()
    {
        await SafeInvokeVoidAsync("flip", _canvasRef, true, false);
    }

    private async Task FlipVAsync()
    {
        await SafeInvokeVoidAsync("flip", _canvasRef, false, true);
    }

    private async Task ApplyFilterAsync()
    {
        await SafeInvokeVoidAsync("applyFilter", _canvasRef, _filter);
    }

    private async Task UndoAsync()
    {
        await SafeInvokeVoidAsync("undo", _canvasRef);
    }

    private async Task RedoAsync()
    {
        await SafeInvokeVoidAsync("redo", _canvasRef);
    }

    private async Task ResetAsync()
    {
        _filter = "";
        await SafeInvokeVoidAsync("reset", _canvasRef);
    }

    private async Task DownloadAsync()
    {
        await SafeInvokeVoidAsync("download", _canvasRef, "image.png", "image/png", 0.92);
    }

    private async Task ApplyCropAsync()
    {
        await SafeInvokeVoidAsync("applyCrop", _canvasRef);
        _cropActive = false;
    }

    private void CancelCrop()
    {
        _cropActive = false;
        _ = SafeInvokeVoidAsync("clearCrop", _canvasRef);
    }

    private void OnZoomInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var val))
            _zoomPercent = Math.Clamp(val, 10, 400);
    }

    private async Task OnFileSelected(ChangeEventArgs e)
    {
        _error = null;
        // The file input is handled via JS — get the file and convert to data URL
        // We'll use eval to read the file
        try
        {
            var dataUrl = await JS.InvokeAsync<string>("eval", @"
                (function() {
                    const i = document.querySelector('.sgc-img-editor input[type=file]');
                    if (!i || !i.files || !i.files[0]) return '';
                    return new Promise((res) => {
                        const r = new FileReader();
                        r.onload = () => res(r.result);
                        r.onerror = () => res('');
                        r.readAsDataURL(i.files[0]);
                    });
                })()
            ");
            if (!string.IsNullOrEmpty(dataUrl))
            {
                var ok = await SafeInvokeAsync<bool>("loadImage", _canvasRef, dataUrl);
                if (ok)
                {
                    _imageLoaded = true;
                    StateHasChanged();
                }
                else
                {
                    _error = "Failed to load the selected image.";
                    StateHasChanged();
                }
            }
        }
        catch
        {
            _error = "Failed to read the selected file.";
            StateHasChanged();
        }
    }

    private void TriggerFileUpload()
    {
        _ = JS.InvokeVoidAsync("eval", "document.querySelector('.sgc-img-editor input[type=file]')?.click()");
    }

    // ── JSInvokable ──

    [JSInvokable]
    public void OnUndoRedoChangedJs(bool canUndo, bool canRedo)
    {
        _canUndo = canUndo;
        _canRedo = canRedo;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnImageLoadedJs(int naturalWidth, int naturalHeight)
    {
        _imageLoaded = true;
        _error = null;
        _ = OnImageWidthChanged.InvokeAsync(naturalWidth);
        _ = OnImageHeightChanged.InvokeAsync(naturalHeight);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnCropActiveChangedJs(bool active)
    {
        _cropActive = active;
        StateHasChanged();
    }

    // ── CSS ──

    private string GetRootClasses()
    {
        var cls = new List<string>(3) { "sgc-img-editor" };
        if (!string.IsNullOrWhiteSpace(CssClass)) cls.Add(CssClass!);
        if (_activeTool == EditorTool.Draw) cls.Add("sgc-img-editor-draw-active");
        if (_activeTool == EditorTool.Crop) cls.Add("sgc-img-editor-crop-active");
        return string.Join(' ', cls);
    }
}
