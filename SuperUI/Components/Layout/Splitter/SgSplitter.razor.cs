using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;
using SuperUI.Services;

namespace SuperUI.Components;

/// <summary>
/// Resizable splitter that divides space between two or more panes with draggable bars.
/// </summary>
/// <remarks>
/// <para>Two usage shapes:</para>
/// <list type="bullet">
///   <item><b>Two-pane</b> — supply <see cref="First"/> and <see cref="Second"/>. The first pane is
///   sized by <see cref="Size"/>; the second fills the remaining space.</item>
///   <item><b>Multi-pane</b> — supply any number of <see cref="SgSplitterPane"/> children. Every pane
///   except the last is sized; the last always flexes to fill remaining space.</item>
/// </list>
/// <para>Resizing works with mouse, touch and pen (drag a bar), and with the keyboard
/// (focus a bar, then arrow keys / Home / End). Double-clicking any bar restores the
/// initial layout. When <see cref="PersistKey"/> is set, sizes are saved to browser
/// storage and restored on the next render.</para>
/// <para>Fully SSR-safe: markup renders statically and interactivity attaches only once
/// the component becomes interactive (Server or WebAssembly).</para>
/// <example>
/// <code>
/// &lt;SgSplitter Size="240" Min="120" Collapsible="true"&gt;
///     &lt;First&gt;Sidebar&lt;/First&gt;
///     &lt;Second&gt;Content&lt;/Second&gt;
/// &lt;/SgSplitter&gt;
///
/// &lt;SgSplitter Orientation="SgOrientation.Horizontal" PersistKey="editor"&gt;
///     &lt;SgSplitterPane Size="200" Min="120"&gt;Files&lt;/SgSplitterPane&gt;
///     &lt;SgSplitterPane Size="400"&gt;Editor&lt;/SgSplitterPane&gt;
///     &lt;SgSplitterPane&gt;Preview&lt;/SgSplitterPane&gt;
/// &lt;/SgSplitter&gt;
/// </code>
/// </example>
/// </remarks>
public partial class SgSplitter
{
    protected override string ModulePath => "./_content/SuperUI/superui-splitter.js";
    protected override string IdPrefix => "sg-splitter";

    [Inject] private SgStorageService Storage { get; set; } = default!;

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>Content of the first (sized) pane in two-pane mode.</summary>
    [Parameter] public RenderFragment? First { get; set; }

    /// <summary>Content of the second (flexible) pane in two-pane mode.</summary>
    [Parameter] public RenderFragment? Second { get; set; }

    /// <summary>Container for <see cref="SgSplitterPane"/> children in multi-pane mode.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Layout direction. <see cref="SgOrientation.Horizontal"/> places panes side by side.</summary>
    [Parameter] public SgOrientation Orientation { get; set; } = SgOrientation.Horizontal;

    /// <summary>Shorthand that forces vertical orientation. Overrides <see cref="Orientation"/> when <c>true</c>.</summary>
    [Parameter] public bool Vertical { get; set; }

    /// <summary>Initial size of the first pane in pixels (two-pane mode). Supports two-way binding.</summary>
    [Parameter] public double Size { get; set; } = 240;

    /// <summary>Raised when the first pane size changes (enables <c>@bind-Size</c>).</summary>
    [Parameter] public EventCallback<double> SizeChanged { get; set; }

    /// <summary>Minimum size of the first pane in pixels (two-pane mode).</summary>
    [Parameter] public double Min { get; set; } = 80;

    /// <summary>Maximum size of the first pane in pixels (two-pane mode).</summary>
    [Parameter] public double Max { get; set; } = 1200;

    /// <summary>Shows the centred grip on each resize bar.</summary>
    [Parameter] public bool ShowHandle { get; set; } = true;

    /// <summary>Disables all resizing while keeping the layout rendered.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Shows a collapse / expand toggle on each bar that hides the preceding pane and restores it on click.</summary>
    [Parameter] public bool Collapsible { get; set; }

    /// <summary>Enables arrow-key / Home / End resizing when a bar is focused.</summary>
    [Parameter] public bool KeyboardResize { get; set; } = true;

    /// <summary>Pixels moved per arrow-key press, and the snap step for <see cref="SnapToGrid"/>.</summary>
    [Parameter] public double Step { get; set; } = 10;

    /// <summary>When set, sizes snap to the nearest multiple of this many pixels while dragging.</summary>
    [Parameter] public double? SnapToGrid { get; set; }

    /// <summary>
    /// When <c>true</c> (default) drag and keyboard resizing are clamped to <see cref="Min"/>/<see cref="Max"/>.
    /// Set to <c>false</c> to allow dragging past those limits — a pane can then be shrunk to nothing
    /// (overlapping its content, which scrolls) or grown up to the container edge. The limits are still
    /// reported to assistive tech as the recommended range.
    /// </summary>
    [Parameter] public bool Constrained { get; set; } = true;

    /// <summary>Tooltip and accessible label shown for each resize bar.</summary>
    [Parameter] public string? HandleTooltip { get; set; }

    /// <summary>
    /// When set, pane sizes are saved under <c>sg-splitter:{PersistKey}</c> and restored on load.
    /// Storage location is controlled by <see cref="PersistKind"/>.
    /// </summary>
    [Parameter] public string? PersistKey { get; set; }

    /// <summary>Where persisted sizes are stored. Defaults to the session (cleared when the tab closes).</summary>
    [Parameter] public SgStorageKind PersistKind { get; set; } = SgStorageKind.Session;

    /// <summary>Raised when a resize interaction begins (pointer down on a bar).</summary>
    [Parameter] public EventCallback OnResizeStart { get; set; }

    /// <summary>Raised when a resize interaction ends, with the final size of every sized pane.</summary>
    [Parameter] public EventCallback<double[]> OnResizeEnd { get; set; }

    // ── Internal state ──────────────────────────────────────────────────────────

    private readonly List<SgSplitterPane> _panes = new();
    private readonly List<ElementReference> _paneRefs = new();
    private readonly List<ElementReference> _barRefs = new();

    // Current size of each *sized* pane (every pane except the last). Index-aligned to bars.
    private readonly List<double> _sizes = new();
    // Size remembered before a collapse, so expand restores it. Keyed by bar index.
    private readonly Dictionary<int, double> _preCollapse = new();

    private bool _isDragging;
    private bool _needsAttach = true;
    private bool _interactiveReady;
    private bool _persistenceLoaded;

    // The last value the consumer passed via the Size parameter. Used to tell an
    // *intentional* new Size (which should move the pane) apart from the same old
    // Size arriving again on an unrelated parent re-render (which must NOT clobber
    // a size the user just dragged). Sentinel NaN = "not seen yet".
    private double _lastInboundSize = double.NaN;

    // Snapshot of options that change the JS engine's behaviour. When any of these
    // differ on a re-render we re-attach the engine in place — so consumers can flip
    // them live without recreating the component (which would lose the dragged size).
    private (bool Vertical, bool Disabled, bool Keyboard, bool Constrained, double Step, double? Snap) _attachOpts;

    private RenderFragment _renderContent = null!;

    private bool IsVertical => Vertical || Orientation == SgOrientation.Vertical;

    // Mode is decided by the *presence of ChildContent*, which is known at first
    // render and never changes — NOT by _panes.Count, which starts at 0 and only
    // fills in after children register. Keying the rendered structure to a value
    // that flips between renders would swap ElementReferenceCapture frames and
    // crash the diff engine.
    private bool IsMultiPane => ChildContent is not null;
    private int SizedCount => IsMultiPane ? Math.Max(0, _panes.Count - 1) : 1;

    private string ContainerClass => Css("sgc-split")
        .AddClass(IsVertical ? "sgc-v" : "sgc-h")
        .AddClass("is-dragging", _isDragging)
        .AddClass("is-disabled", Disabled)
        .Build();

    private string? StorageKey => string.IsNullOrWhiteSpace(PersistKey) ? null : $"sg-splitter:{PersistKey}";

    // ── Pane registration (multi-pane mode) ─────────────────────────────────────

    internal void RegisterPane(SgSplitterPane pane)
    {
        if (_panes.Contains(pane)) return;
        _panes.Add(pane);
        _needsAttach = true;
        SyncSizesToPanes();
        BuildRenderContent();
        StateHasChanged();
    }

    internal void UnregisterPane(SgSplitterPane pane)
    {
        if (!_panes.Remove(pane)) return;
        _needsAttach = true;
        SyncSizesToPanes();
        BuildRenderContent();
        StateHasChanged();
    }

    private void SyncSizesToPanes()
    {
        _sizes.Clear();
        for (var i = 0; i < SizedCount; i++)
            _sizes.Add(IsMultiPane ? (_panes[i].Size ?? 200) : Size);
    }

    private double MinAt(int i) => IsMultiPane ? _panes[i].Min : Min;
    private double MaxAt(int i) => IsMultiPane ? _panes[i].Max : Max;

    // ── Lifecycle ───────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _lastInboundSize = Size;
        SyncSizesToPanes();
        BuildRenderContent();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // Two-pane mode: only adopt Size when the *parameter itself* changed since we
        // last saw it. A parent re-render that re-passes the same literal Size must NOT
        // overwrite a size the user dragged — otherwise the bar snaps back. Skip while
        // dragging so an in-flight gesture is never interrupted.
        if (!IsMultiPane && !_isDragging && Math.Abs(Size - _lastInboundSize) > 0.5)
        {
            _lastInboundSize = Size;
            if (_sizes.Count > 0) _sizes[0] = Size; else _sizes.Add(Size);
        }

        // If a behaviour-affecting option changed, schedule a re-attach so the live
        // JS engine picks it up — without recreating the component or resetting sizes.
        var opts = (IsVertical, Disabled, KeyboardResize, Constrained, Step, SnapToGrid);
        if (_interactiveReady && !opts.Equals(_attachOpts))
            _needsAttach = true;
        _attachOpts = opts;

        BuildRenderContent();
    }

    protected override async ValueTask OnInteractiveAsync()
    {
        _interactiveReady = true;
        await LoadPersistedAsync();
        await AttachAsync();
    }

    protected override async Task OnAfterRenderSafeAsync(bool firstRender)
    {
        if (!_interactiveReady || !IsInteractive || !_needsAttach) return;
        // Panes that registered after the first interactive pass: retry persistence
        // (no-op once loaded) before (re)attaching the JS engine to the new layout.
        await LoadPersistedAsync();
        await AttachAsync();
    }

    protected override async ValueTask OnDisposingAsync()
    {
        await SafeInvokeVoidAsync("detach", RootRef);
    }

    // ── JS attach / sync ─────────────────────────────────────────────────────────

    private async Task AttachAsync()
    {
        if (_paneRefs.Count < 2 || _barRefs.Count == 0) return;
        _needsAttach = false;

        var mins = new double[SizedCount];
        var maxs = new double[SizedCount];
        var sizes = new double[SizedCount];
        for (var i = 0; i < SizedCount; i++)
        {
            mins[i] = MinAt(i);
            maxs[i] = MaxAt(i);
            sizes[i] = i < _sizes.Count ? _sizes[i] : 200;
        }

        var options = new
        {
            vertical = IsVertical,
            step = Step,
            snapToGrid = SnapToGrid,
            keyboardResize = KeyboardResize,
            disabled = Disabled,
            constrained = Constrained,
            mins,
            maxs,
            sizes,
        };

        await SafeInvokeVoidAsync("attach", RootRef, _barRefs.ToArray(), _paneRefs.ToArray(), SelfRef, options);
    }

    private async Task PushSizesToJsAsync()
    {
        await SafeInvokeVoidAsync("setSizes", RootRef, _sizes.ToArray());
    }

    // ── JS callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked by JS once per drag end or per keyboard step with the live pane sizes.</summary>
    [JSInvokable]
    public async Task SetSizes(double[] sizes)
    {
        if (sizes is null || sizes.Length == 0) return;
        var changed = false;

        for (var i = 0; i < sizes.Length && i < SizedCount; i++)
        {
            var v = Math.Max(0, sizes[i]);
            if (i < _sizes.Count && Math.Abs(_sizes[i] - v) <= 0.5) continue;
            if (i < _sizes.Count) _sizes[i] = v; else _sizes.Add(v);
            if (IsMultiPane) _panes[i].SetSize(v);
            changed = true;
        }

        if (!changed) return;

        if (!IsMultiPane && SizeChanged.HasDelegate)
            await SizeChanged.InvokeAsync(_sizes[0]);
        if (OnResizeEnd.HasDelegate)
            await OnResizeEnd.InvokeAsync(_sizes.ToArray());

        await PersistAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Invoked by JS when a drag interaction starts.</summary>
    [JSInvokable]
    public async Task ResizeStart()
    {
        if (OnResizeStart.HasDelegate) await OnResizeStart.InvokeAsync();
    }

    /// <summary>Toggles the dragging CSS state (disables pane transitions during drag).</summary>
    [JSInvokable]
    public void SetDragging(bool dragging)
    {
        _isDragging = dragging;
        StateHasChanged();
    }

    /// <summary>Invoked by JS on double-click — restores the declared initial sizes.</summary>
    [JSInvokable]
    public async Task Reset()
    {
        _preCollapse.Clear();
        SyncSizesToPanes();
        await PushSizesToJsAsync();
        if (!IsMultiPane && SizeChanged.HasDelegate)
            await SizeChanged.InvokeAsync(_sizes.Count > 0 ? _sizes[0] : Size);
        if (OnResizeEnd.HasDelegate)
            await OnResizeEnd.InvokeAsync(_sizes.ToArray());
        await PersistAsync();
        await InvokeAsync(StateHasChanged);
    }

    // ── Collapse / expand ───────────────────────────────────────────────────────

    private async Task ToggleCollapse(int barIndex)
    {
        if (barIndex < 0 || barIndex >= SizedCount) return;
        var current = barIndex < _sizes.Count ? _sizes[barIndex] : MinAt(barIndex);

        double target;
        if (current <= 1)
        {
            // Expand: restore the remembered size (or the declared default).
            target = _preCollapse.TryGetValue(barIndex, out var prev) && prev > 1
                ? prev
                : (IsMultiPane ? (_panes[barIndex].Size ?? 200) : Size);
            _preCollapse.Remove(barIndex);
        }
        else
        {
            _preCollapse[barIndex] = current;
            target = 0;
        }

        if (barIndex < _sizes.Count) _sizes[barIndex] = target; else _sizes.Add(target);
        if (IsMultiPane) _panes[barIndex].SetSize(target);

        await PushSizesToJsAsync();
        if (!IsMultiPane && SizeChanged.HasDelegate) await SizeChanged.InvokeAsync(target);
        if (OnResizeEnd.HasDelegate) await OnResizeEnd.InvokeAsync(_sizes.ToArray());
        await PersistAsync();
        StateHasChanged();
    }

    private bool IsCollapsed(int barIndex) =>
        barIndex < _sizes.Count && _sizes[barIndex] <= 1;

    // ── Persistence ───────────────────────────────────────────────────────────────

    private async Task LoadPersistedAsync()
    {
        if (_persistenceLoaded || StorageKey is null) return;
        // In multi-pane mode the panes may not have registered on the very first
        // interactive pass — wait for them so SizedCount is meaningful.
        if (IsMultiPane && _panes.Count == 0) return;
        _persistenceLoaded = true;

        var saved = await Storage.GetAsync<double[]>(StorageKey, PersistKind);
        if (saved is null || saved.Length != SizedCount) return;

        for (var i = 0; i < SizedCount; i++)
            _sizes[i] = Math.Clamp(saved[i], 0, MaxAt(i));

        if (IsMultiPane)
            for (var i = 0; i < SizedCount; i++) _panes[i].SetSize(_sizes[i]);
        else if (SizeChanged.HasDelegate)
            await SizeChanged.InvokeAsync(_sizes[0]);

        BuildRenderContent();
        StateHasChanged();
    }

    private async Task PersistAsync()
    {
        if (StorageKey is null) return;
        await Storage.SetAsync(StorageKey, _sizes.ToArray(), PersistKind);
    }

    // ── Render tree ───────────────────────────────────────────────────────────────

    private void BuildRenderContent() => _renderContent = Build;

    private void Build(RenderTreeBuilder builder)
    {
        _paneRefs.Clear();
        _barRefs.Clear();

        // ChildContent hosts the SgSplitterPane components (they register themselves
        // and render no markup of their own). It is ALWAYS emitted first at a fixed
        // sequence so the frame layout never changes between renders.
        builder.AddContent(0, ChildContent);

        // Every pane/bar is rendered through a keyed RenderFragment so Blazor tracks
        // element identity by key (not by position). This keeps ElementReferenceCapture
        // frames stable even as panes register over several renders or collapse toggles
        // flip conditional attributes — the diff engine never sees a frame change type.
        if (IsMultiPane)
        {
            for (var i = 0; i < _panes.Count; i++)
            {
                var pane = _panes[i];
                var isLast = i == _panes.Count - 1;
                RenderKeyedPane(builder, pane.Key, pane.ChildContent, i, isLast, pane.Class, pane.Style);
                if (!isLast) RenderKeyedBar(builder, pane.Key, i);
            }
        }
        else
        {
            RenderKeyedPane(builder, "first", First, 0, isLast: false, null, null);
            RenderKeyedBar(builder, "first", 0);
            RenderKeyedPane(builder, "second", Second, 1, isLast: true, null, null);
        }
    }

    private void RenderKeyedPane(RenderTreeBuilder builder, object key, RenderFragment? content,
        int index, bool isLast, string? extraClass, string? extraStyle)
    {
        builder.OpenElement(1, "div");
        builder.SetKey(key);

        var cls = "sgc-split-pane";
        if (isLast) cls += " sgc-split-flex";
        if (!isLast && IsCollapsed(index)) cls += " is-collapsed";
        if (!string.IsNullOrEmpty(extraClass)) cls += " " + extraClass;
        builder.AddAttribute(2, "class", cls);
        builder.AddAttribute(3, "id", $"{ResolvedId}-p{index}");

        var style = extraStyle;
        if (!isLast)
        {
            var size = index < _sizes.Count ? _sizes[index] : 200;
            var px = size.ToString(CultureInfo.InvariantCulture) + "px";
            style = (IsVertical ? $"height:{px};" : $"width:{px};") + style;
        }
        builder.AddAttribute(4, "style", style);

        builder.AddElementReferenceCapture(5, r => _paneRefs.Add(r));
        builder.AddContent(6, content);
        builder.CloseElement();
    }

    private void RenderKeyedBar(RenderTreeBuilder builder, object paneKey, int barIndex)
    {
        var size = barIndex < _sizes.Count ? _sizes[barIndex] : 0;

        builder.OpenElement(10, "div");
        builder.SetKey(("bar", paneKey));
        builder.AddAttribute(11, "class", "sgc-split-bar");
        builder.AddAttribute(12, "tabindex", Disabled ? "-1" : "0");
        builder.AddAttribute(13, "role", "separator");
        builder.AddAttribute(14, "aria-orientation", IsVertical ? "horizontal" : "vertical");
        builder.AddAttribute(15, "aria-controls", $"{ResolvedId}-p{barIndex}");
        builder.AddAttribute(16, "aria-valuemin", MinAt(barIndex).ToString(CultureInfo.InvariantCulture));
        builder.AddAttribute(17, "aria-valuemax", MaxAt(barIndex).ToString(CultureInfo.InvariantCulture));
        builder.AddAttribute(18, "aria-valuenow", Math.Round(size).ToString(CultureInfo.InvariantCulture));
        builder.AddAttribute(19, "aria-label", HandleTooltip);
        builder.AddAttribute(20, "title", HandleTooltip);
        builder.AddElementReferenceCapture(21, r => _barRefs.Add(r));

        if (Collapsible)
        {
            var collapsed = IsCollapsed(barIndex);
            var icon = (IsVertical, collapsed) switch
            {
                (false, false) => SgHeroicons.Outline.ChevronLeft,
                (false, true) => SgHeroicons.Outline.ChevronRight,
                (true, false) => SgHeroicons.Outline.ChevronUp,
                (true, true) => SgHeroicons.Outline.ChevronDown,
            };
            var label = collapsed ? Localizer["Common_Expand"] : Localizer["Common_Collapse"];
            var idx = barIndex;

            builder.OpenElement(22, "button");
            builder.AddAttribute(23, "type", "button");
            builder.AddAttribute(24, "class", "sgc-split-collapse-btn");
            builder.AddAttribute(25, "title", label);
            builder.AddAttribute(26, "aria-label", label);
            builder.AddAttribute(27, "onclick", EventCallback.Factory.Create(this, () => _ = ToggleCollapse(idx)));
            builder.AddAttribute(28, "onclick:stopPropagation", true);
            builder.OpenComponent<SgIcon>(29);
            builder.AddComponentParameter(30, nameof(SgIcon.Icon), icon);
            builder.AddComponentParameter(31, nameof(SgIcon.Size), "12px");
            builder.CloseComponent();
            builder.CloseElement();
        }

        if (ShowHandle)
        {
            builder.OpenElement(32, "div");
            builder.AddAttribute(33, "class", "sgc-split-handle");
            builder.OpenElement(34, "div");
            builder.AddAttribute(35, "class", "sgc-split-handle-bar");
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
