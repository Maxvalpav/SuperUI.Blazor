using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Splitter component that divides space between two or more panes with a draggable resize bar.
/// Supports horizontal and vertical orientation, collapsible panes, keyboard resizing, and snap-to-grid.
/// </summary>
public partial class SgSplitter
{
    protected override string ModulePath => "./_content/SuperUI/superui-splitter.js";
    protected override string IdPrefix => "sg-splitter";

    private readonly List<SgSplitterPane> _panes = new();
    private readonly List<ElementReference> _paneRefs = new();
    private readonly List<ElementReference> _barRefs = new();
    private readonly Dictionary<SgSplitterPane, double> _paneSizes = new();
    private bool _isDragging;
    private double _currentSize;
    private double _initialSize;
    private bool _needsAttach = true;
    private bool _interactiveReady;

    /// <summary>Content for the first (primary) pane.</summary>
    [Parameter] public RenderFragment? First { get; set; }
    /// <summary>Content for the second (secondary) pane.</summary>
    [Parameter] public RenderFragment? Second { get; set; }
    /// <summary>Orientation of the splitter (Horizontal or Vertical).</summary>
    [Parameter] public SgOrientation Orientation { get; set; } = SgOrientation.Horizontal;
    /// <summary>If true, sets the splitter to vertical orientation (overrides Orientation).</summary>
    [Parameter] public bool Vertical { get; set; }
    /// <summary>Initial size of the first pane in pixels.</summary>
    [Parameter] public double Size { get; set; } = 240;
    /// <summary>Callback invoked when the splitter size changes.</summary>
    [Parameter] public EventCallback<double> SizeChanged { get; set; }
    /// <summary>Minimum size of the first pane in pixels.</summary>
    [Parameter] public double Min { get; set; } = 80;
    /// <summary>Maximum size of the first pane in pixels.</summary>
    [Parameter] public double Max { get; set; } = 1200;
    /// <summary>Whether to show the visible drag handle on the resize bar.</summary>
    [Parameter] public bool ShowHandle { get; set; } = true;
    /// <summary>Disables the splitter, preventing resizing.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Allows collapsing and expanding the primary pane via buttons on the resize bar.</summary>
    [Parameter] public bool Collapsible { get; set; }
    /// <summary>Content for multi-pane mode. Overrides First/Second when used with SgSplitterPane children.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Enables keyboard-based resizing with arrow keys.</summary>
    [Parameter] public bool KeyboardResize { get; set; } = true;
    /// <summary>Step size in pixels for keyboard and programmatic resizing.</summary>
    [Parameter] public double Step { get; set; } = 10;
    /// <summary>If set, snaps the pane size to the nearest multiple of this value.</summary>
    [Parameter] public double? SnapToGrid { get; set; }
    /// <summary>Callback invoked when resize starts.</summary>
    [Parameter] public EventCallback OnResizeStart { get; set; }
    /// <summary>Callback invoked when resize ends, providing the final pane sizes.</summary>
    [Parameter] public EventCallback<double[]> OnResizeEnd { get; set; }
    /// <summary>Tooltip text shown when hovering the drag handle.</summary>
    [Parameter] public string? HandleTooltip { get; set; }

    private bool IsVertical => Orientation == SgOrientation.Vertical || Vertical;
    private bool IsMultiPane => _panes.Count > 0 || ChildContent != null;

    private string FirstStyle => IsVertical
        ? $"height:{_currentSize.ToString(CultureInfo.InvariantCulture)}px;"
        : $"width:{_currentSize.ToString(CultureInfo.InvariantCulture)}px;";

    private RenderFragment _renderContent = null!;

    private string GetContainerClass()
    {
        var cls = "sgc-split";
        cls += IsVertical ? " sgc-v" : " sgc-h";
        if (_isDragging) cls += " is-dragging";
        return cls;
    }

    internal void RegisterPane(SgSplitterPane pane)
    {
        if (!_panes.Contains(pane))
        {
            _panes.Add(pane);
            _paneSizes[pane] = pane.Size ?? 200;
            _needsAttach = true;
            BuildRenderContent();
            StateHasChanged();
        }
    }

    internal void UnregisterPane(SgSplitterPane pane)
    {
        if (_panes.Remove(pane))
        {
            _paneSizes.Remove(pane);
            _needsAttach = true;
            BuildRenderContent();
            StateHasChanged();
        }
    }

    private double GetPaneSize(int index)
    {
        if (index < 0 || index >= _panes.Count) return 200;
        if (_paneSizes.TryGetValue(_panes[index], out var s)) return s;
        return _panes[index].Size ?? 200;
    }

    private double _lastSize;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _currentSize = Size;
        _initialSize = Size;
        _lastSize = Size;
        BuildRenderContent();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!_isDragging && Math.Abs(Size - _lastSize) > 0.5)
        {
            _currentSize = Size;
            _lastSize = Size;
        }
        BuildRenderContent();
    }

    private void BuildRenderContent()
    {
        _renderContent = IsMultiPane ? BuildMultiPaneContent() : BuildSimpleContent();
    }

    private RenderFragment BuildSimpleContent()
    {
        return builder =>
        {
            _paneRefs.Clear();
            _barRefs.Clear();
            var seq = 0;

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-split-pane");
            builder.AddAttribute(seq++, "style", FirstStyle);
            builder.AddElementReferenceCapture(seq++, r => _paneRefs.Add(r));
            builder.AddContent(seq++, First);
            builder.CloseElement();

            RenderBar(builder, ref seq, 0, simple: true);

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-split-pane sgc-split-flex");
            builder.AddContent(seq++, Second);
            builder.CloseElement();
        };
    }

    private RenderFragment BuildMultiPaneContent()
    {
        return builder =>
        {
            if (_panes.Count == 0)
            {
                if (ChildContent != null)
                    builder.AddContent(0, ChildContent);
                return;
            }

            _paneRefs.Clear();
            _barRefs.Clear();
            var seq = 0;
            var count = _panes.Count;

            builder.AddContent(seq++, ChildContent);

            for (var i = 0; i < count; i++)
            {
                var pane = _panes[i];
                var isLast = i == count - 1;

                builder.OpenElement(seq++, "div");
                var cls = "sgc-split-pane";
                if (isLast) cls += " sgc-split-flex";
                builder.AddAttribute(seq++, "class", cls);
                if (!isLast)
                {
                    var size = GetPaneSize(i);
                    var style = IsVertical
                        ? $"height:{size.ToString(CultureInfo.InvariantCulture)}px;"
                        : $"width:{size.ToString(CultureInfo.InvariantCulture)}px;";
                    builder.AddAttribute(seq++, "style", style);
                }
                var paneIdx = i;
                builder.AddElementReferenceCapture(seq++, r => _paneRefs.Add(r));
                builder.AddContent(seq++, pane.ChildContent);
                builder.CloseElement();

                if (!isLast)
                {
                    RenderBar(builder, ref seq, i, simple: false);
                }
            }
        };
    }

    private void RenderBar(RenderTreeBuilder builder, ref int seq, int paneIndex, bool simple)
    {
        var barIdx = paneIndex;
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "sgc-split-bar");
        builder.AddAttribute(seq++, "tabindex", "0");
        builder.AddAttribute(seq++, "role", "separator");
        builder.AddElementReferenceCapture(seq++, r => _barRefs.Add(r));

        if (Collapsible)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-split-collapse-btn sgc-btn-prev");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () =>
            {
                if (simple) _ = CollapseFirstPane();
                else _ = CollapsePane(paneIndex);
            }));
            builder.AddAttribute(seq++, "onclick:stopPropagation", true);
            builder.AddAttribute(seq++, "title", CollapseTooltip);
            builder.AddMarkupContent(seq++,
                """<svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" stroke-width="3"><path d="M18 15l-6-6-6 6"/></svg>""");
            builder.CloseElement();
        }

        if (ShowHandle)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-split-handle");
            if (HandleTooltip != null)
                builder.AddAttribute(seq++, "title", HandleTooltip);
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-split-handle-bar");
            builder.CloseElement();
            builder.CloseElement();
        }

        if (Collapsible && simple)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-split-collapse-btn sgc-btn-next");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => _ = CollapseSecondPane()));
            builder.AddAttribute(seq++, "onclick:stopPropagation", true);
            builder.AddAttribute(seq++, "title", "Reset size");
            builder.AddMarkupContent(seq++,
                """<svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" stroke-width="3"><path d="M6 9l6 6 6-6"/></svg>""");
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private string CollapseTooltip => IsVertical ? "Collapse pane" : "Collapse pane";

    protected override async ValueTask OnInteractiveAsync()
    {
        _interactiveReady = true;

        if (IsMultiPane && _paneRefs.Count > 0 && _barRefs.Count > 0)
        {
            _needsAttach = false;
            await AttachMultiPaneAsync();
        }
        else if (!IsMultiPane && _paneRefs.Count > 0 && _barRefs.Count > 0)
        {
            _needsAttach = false;
            await AttachSimpleAsync();
        }
    }

    protected override async Task OnAfterRenderSafeAsync(bool firstRender)
    {
        if (!_interactiveReady || !IsInteractive) return;
        if (!_needsAttach) return;
        if (_paneRefs.Count == 0 || _barRefs.Count == 0) return;

        _needsAttach = false;
        await DetachAsync();

        if (IsMultiPane)
            await AttachMultiPaneAsync();
        else
            await AttachSimpleAsync();
    }

    private async Task AttachSimpleAsync()
    {
        if (_barRefs.Count == 0 || _paneRefs.Count == 0) return;
        await SafeInvokeVoidAsync("attach",
            _barRefs[0], _paneRefs[0], IsVertical, Min, Max, SelfRef, Disabled);
    }

    private async Task AttachMultiPaneAsync()
    {
        if (_barRefs.Count == 0 || _paneRefs.Count == 0) return;

        var minSizes = new double[_panes.Count];
        var maxSizes = new double[_panes.Count];
        var initialSizes = new double[_panes.Count];

        for (var i = 0; i < _panes.Count; i++)
        {
            minSizes[i] = _panes[i].Min;
            maxSizes[i] = _panes[i].Max;
            initialSizes[i] = GetPaneSize(i);
        }

        var options = new
        {
            step = Step,
            snapToGrid = SnapToGrid,
            keyboardResize = KeyboardResize
        };

        await SafeInvokeVoidAsync("attachBars",
            _barRefs.ToArray(),
            _paneRefs.ToArray(),
            IsVertical,
            minSizes, maxSizes, initialSizes,
            SelfRef,
            Disabled,
            options);
    }

    private async Task DetachAsync()
    {
        if (_barRefs.Count == 0) return;

        if (IsMultiPane)
            await SafeInvokeVoidAsync("detachBars", _barRefs.ToArray());
        else
            await SafeInvokeVoidAsync("detach", _barRefs[0]);
    }

    private async Task CollapseFirstPane()
    {
        var target = _currentSize > Min ? Min : 0;
        await SetSize(target);
    }

    private async Task CollapseSecondPane()
    {
        await OnReset();
    }

    private async Task CollapsePane(int index)
    {
        if (index < 0 || index >= _panes.Count) return;
        var cur = GetPaneSize(index);
        var target = cur > _panes[index].Min ? _panes[index].Min : 0;

        var sizes = new double[_panes.Count];
        for (var i = 0; i < _panes.Count; i++)
            sizes[i] = GetPaneSize(i);
        sizes[index] = target;

        await SetSizes(sizes);
    }

    [JSInvokable]
    public async Task SetSize(double size)
    {
        if (Math.Abs(size - _currentSize) < 0.5) return;
        _currentSize = size;
        if (SizeChanged.HasDelegate) await SizeChanged.InvokeAsync(size);
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task SetSizes(double[] sizes)
    {
        if (sizes == null || sizes.Length == 0) return;
        var changed = false;

        for (var i = 0; i < sizes.Length && i < _panes.Count; i++)
        {
            var pane = _panes[i];
            var newSize = Math.Max(0, sizes[i]);
            if (Math.Abs(newSize - GetPaneSize(i)) > 0.5)
            {
                _paneSizes[pane] = newSize;
                pane.SetSize(newSize);
                changed = true;
            }
        }

        if (changed)
        {
            if (OnResizeEnd.HasDelegate)
                await OnResizeEnd.InvokeAsync(sizes);
            await InvokeAsync(StateHasChanged);
        }
    }

    [JSInvokable]
    public void SetDragging(bool dragging)
    {
        _isDragging = dragging;
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnReset()
    {
        if (IsMultiPane)
        {
            var sizes = new double[_panes.Count];
            for (var i = 0; i < _panes.Count; i++)
                sizes[i] = _panes[i].Size ?? 200;
            await SetSizes(sizes);
        }
        else
        {
            _currentSize = _initialSize;
            if (SizeChanged.HasDelegate) await SizeChanged.InvokeAsync(_currentSize);
            await InvokeAsync(StateHasChanged);
        }
    }

    protected override async ValueTask OnDisposingAsync()
    {
        await DetachAsync();
    }
}
