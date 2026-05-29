using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Components.SgGanttCanvas.Models;
using SuperUI.Components.SgGanttCanvas.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperUI.Components.SgGanttCanvas;

/// <summary>Interactive Gantt chart canvas component with dependency mapping, critical path, zoom, undo/redo, and JS-based rendering.</summary>
public partial class SgGanttCanvas : SgJsComponentBase
{
    /// <summary>Collection of tasks displayed in the Gantt chart.</summary>
    [Parameter] public List<GanttTask> Tasks { get; set; } = new();
    /// <summary>Fired when tasks are modified (moved, resized, progress changed).</summary>
    [Parameter] public EventCallback<List<GanttTask>> TasksChanged { get; set; }

    /// <summary>Dependencies (links) between tasks.</summary>
    [Parameter] public List<GanttDependency> Dependencies { get; set; } = new();
    /// <summary>Fired when dependencies are added or removed.</summary>
    [Parameter] public EventCallback<List<GanttDependency>> DependenciesChanged { get; set; }
    /// <summary>Fired when a new dependency link is created by the user.</summary>
    [Parameter] public EventCallback<GanttDependency> OnDependencyCreated { get; set; }
    /// <summary>Resource assignments shown on tasks.</summary>
    [Parameter] public List<GanttResource> Resources { get; set; } = new();
    /// <summary>Milestone markers displayed on the timeline.</summary>
    [Parameter] public List<GanttMilestone> Milestones { get; set; } = new();
    /// <summary>Column definitions for the left-side task grid.</summary>
    [Parameter] public List<GanttColumn> Columns { get; set; } = new();

    /// <summary>Time scale configuration (zoom level, units, working hours).</summary>
    [Parameter] public GanttTimeScale TimeScale { get; set; } = new();
    /// <summary>View options controlling visibility of dependencies, baselines, grid, etc.</summary>
    [Parameter] public GanttViewOptions ViewOptions { get; set; } = new();
    /// <summary>Theme colors for the Gantt chart rendering.</summary>
    [Parameter] public GanttTheme Theme { get; set; } = new();
    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (IsInteractive && _jsInstance != null)
        {
            await SafeInvokeVoidAsync("setOptions", _jsInstance, new { 
                theme = Theme, 
                viewOptions = ViewOptions,
                columnWidth = _layoutEngine.GetZoomSettings(TimeScale.ZoomLevel).ColumnWidth
            });
            await RefreshDataAsync();
            StateHasChanged();
        }
    }

    /// <summary>Display mode (Gantt, Resource, etc.).</summary>
    [Parameter] public string ViewMode { get; set; } = "Gantt";
    /// <summary>Optional project start date used to calculate the timeline origin.</summary>
    [Parameter] public DateTime? ProjectStart { get; set; }
    /// <summary>Optional project end date.</summary>
    [Parameter] public DateTime? ProjectEnd { get; set; }
    /// <summary>Height of the component container.</summary>
    [Parameter] public string Height { get; set; } = "100%";
    /// <summary>Width of the component container.</summary>
    [Parameter] public string Width { get; set; } = "100%";
    /// <summary>Disables user interaction (drag, resize, dependency creation).</summary>
    [Parameter] public bool ReadOnly { get; set; }
    /// <summary>Shows the top toolbar with zoom and action buttons.</summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;
    /// <summary>Shows the status bar at the bottom.</summary>
    [Parameter] public bool ShowStatusBar { get; set; } = true;

    /// <summary>Custom toolbar content replacing the default toolbar.</summary>
    [Parameter] public RenderFragment? ToolbarTemplate { get; set; }
    /// <summary>Template for task tooltips on hover.</summary>
    [Parameter] public RenderFragment<GanttTask>? TooltipTemplate { get; set; }

    /// <summary>Fired when a task bar is clicked.</summary>
    [Parameter] public EventCallback<GanttTask> OnTaskClick { get; set; }
    /// <summary>Fired when a task bar is double-clicked.</summary>
    [Parameter] public EventCallback<GanttTask> OnTaskDoubleClick { get; set; }
    /// <summary>Fired when the selection changes.</summary>
    [Parameter] public EventCallback<List<GanttTask>> OnSelectionChanged { get; set; }
    /// <summary>Fired when the zoom level changes.</summary>
    [Parameter] public EventCallback<double> OnZoomChanged { get; set; }
    /// <summary>Fired when the undo command is triggered.</summary>
    [Parameter] public EventCallback OnUndo { get; set; }
    /// <summary>Fired when the redo command is triggered.</summary>
    [Parameter] public EventCallback OnRedo { get; set; }
    /// <summary>Fired when selected tasks should be deleted.</summary>
    [Parameter] public EventCallback<List<string>> OnDeleteSelected { get; set; }
    /// <summary>Fired when demo data generation is requested.</summary>
    [Parameter] public EventCallback<int> OnGenerateData { get; set; }
    /// <summary>Fired when the theme is changed.</summary>
    [Parameter] public EventCallback<string> OnThemeChanged { get; set; }

    private ElementReference _canvasWrapperRef;
    private ElementReference _leftPanelRef;
    private bool _isLoading = true;
    private double _zoomLevel = 1.0;
    private string _statusText = string.Empty;
    private GanttLayoutEngine _layoutEngine = new();
    private GanttCriticalPathEngine _cpmEngine = new();
    private IJSObjectReference? _jsInstance;

    private int _scrollRowIndex = 0;
    private int _visibleRowCount = 20;
    private bool _isSyncingScroll = false;
    private bool _isInitializing = true;

    private Stack<List<GanttTask>> _undoStack = new();
    private Stack<List<GanttTask>> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    protected override string ModulePath => "./_content/SuperUI/superui-gantt-canvas.js";

    protected override async ValueTask OnInteractiveAsync()
    {
        _isLoading = true;
        StateHasChanged();
        await InitializeGanttAsync();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task InitializeGanttAsync()
    {
        var zoomSettings = _layoutEngine.GetZoomSettings(TimeScale.ZoomLevel);
        var options = new
        {
            headerHeight = 60,
            rowHeight = ViewOptions.RowHeight,
            barHeight = ViewOptions.BarHeight,
            columnWidth = zoomSettings.ColumnWidth,
            leftPanelWidth = ViewOptions.FlatMode ? 300 : 400,
            theme = Theme,
            initialZoom = _zoomLevel,
            snapToGrid = true,
            viewOptions = ViewOptions
        };

        _jsInstance = await TryInvokeAsync<IJSObjectReference>("init", _canvasWrapperRef, SelfRef, options);
        _isInitializing = false;
        await RefreshDataAsync();
    }

    private void SaveState()
    {
        // Deep copy tasks
        var state = Tasks.Select(t => new GanttTask
        {
            Id = t.Id,
            Name = t.Name,
            Start = t.Start,
            End = t.End,
            Progress = t.Progress,
            RowIndex = t.RowIndex,
            Color = t.Color,
            ParentId = t.ParentId,
            IsMilestone = t.IsMilestone,
            IsSummary = t.IsSummary
        }).ToList();
        
        _undoStack.Push(state);
        _redoStack.Clear();
    }

    private List<GanttTask> GetVisibleTasks()
    {
        var visibleTasks = new List<GanttTask>();
        var collapsedParentIds = new HashSet<string>();

        foreach (var task in Tasks.OrderBy(t => t.RowIndex))
        {
            bool isHidden = false;
            if (task.ParentId != null && collapsedParentIds.Contains(task.ParentId))
            {
                isHidden = true;
                if (task.IsSummary) collapsedParentIds.Add(task.Id);
            }
            else if (task.IsSummary && task.IsCollapsed)
            {
                collapsedParentIds.Add(task.Id);
            }

            if (!isHidden)
            {
                visibleTasks.Add(task);
            }
        }
        
        // Re-assign row indices for rendering
        for (int i = 0; i < visibleTasks.Count; i++)
        {
            visibleTasks[i].RowIndex = i;
        }
        
        return visibleTasks;
    }

    private List<GanttTask> _visibleTasks = new();
    
    private async Task RefreshDataAsync()
    {
        if (_jsInstance == null) return;
        
        _visibleTasks = GetVisibleTasks();

        // Calculate Critical Path
        if (ViewOptions.ShowCriticalPath)
        {
            var criticalIds = _cpmEngine.CalculateCriticalPath(_visibleTasks, Dependencies);
            foreach (var task in _visibleTasks)
            {
                task.IsCritical = criticalIds.Contains(task.Id);
            }
        }

        var minDate = ProjectStart ?? (_visibleTasks.Any() ? _visibleTasks.Min(t => t.Start) : DateTime.Today).AddDays(-7);
        var zoomSettings = _layoutEngine.GetZoomSettings(TimeScale.ZoomLevel);
        
        var renderData = new
        {
            columnWidth = zoomSettings.ColumnWidth,
            bottomUnit = zoomSettings.BottomUnit.ToString(),
            projectStart = minDate.ToString("yyyy-MM-ddTHH:mm:ss"),
            viewOptions = ViewOptions,
            tasks = _visibleTasks.Select(t => new
            {
                t.Id,
                t.Name,
                t.Progress,
                t.Color,
                t.TextColor,
                t.RowIndex,
                t.IsCritical,
                t.IsMilestone,
                t.IsSummary,
                baselineX = t.BaselineStart.HasValue ? _layoutEngine.GetX(t.BaselineStart.Value, minDate, TimeScale.ZoomLevel) : (double?)null,
                baselineWidth = (t.BaselineStart.HasValue && t.BaselineEnd.HasValue) ? _layoutEngine.GetWidth(t.BaselineEnd.Value - t.BaselineStart.Value, TimeScale.ZoomLevel) : (double?)null,
                x = _layoutEngine.GetX(t.Start, minDate, TimeScale.ZoomLevel),
                width = _layoutEngine.GetWidth(t.End - t.Start, TimeScale.ZoomLevel)
            }).ToList(),
            dependencies = Dependencies.Select(d => new
            {
                d.Id,
                d.FromTaskId,
                d.ToTaskId,
                d.Type,
                d.Color
            }).ToList()
        };

        await SafeInvokeVoidAsync("setData", _jsInstance, renderData);
    }

    [JSInvokable]
    public async Task OnTaskClickInternal(string taskId)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            await OnTaskClick.InvokeAsync(task);
        }
    }

    [JSInvokable]
    public async Task OnTaskDoubleClickInternal(string taskId)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            await HandleTaskDoubleClick(task);
        }
    }

    private async Task HandleTaskDoubleClick(GanttTask task)
    {
        await OnTaskDoubleClick.InvokeAsync(task);
    }

    [JSInvokable]
    public async Task OnZoomChangedInternal(double zoom)
    {
        _zoomLevel = zoom;
        await OnZoomChanged.InvokeAsync(zoom);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnZoomFromWheel(int delta)
    {
        if (delta > 0) await ZoomIn();
        else await ZoomOut();
    }

    [JSInvokable]
    public async Task OnScrollInternal(double x, double y)
    {
        if (_isSyncingScroll) return;
        _isSyncingScroll = true;

        _scrollRowIndex = (int)(y / ViewOptions.RowHeight);
        
        // No need to eval here anymore, JS handles sync directly
        // but we update state to trigger re-render of left panel items (virtualization)
        StateHasChanged();
        
        _isSyncingScroll = false;
    }

    private async Task OnLeftPanelScroll(EventArgs e)
    {
        if (_isSyncingScroll || _jsInstance == null) return;
        _isSyncingScroll = true;

        var scrollTop = await JS.InvokeAsync<double>("eval", "document.querySelector('.sg-gantt-left-body').scrollTop");
        
        _scrollRowIndex = (int)(scrollTop / ViewOptions.RowHeight);
        
        // Sync to Canvas
        await SafeInvokeVoidAsync("scrollTo", _jsInstance, null, scrollTop);
        
        StateHasChanged();
        _isSyncingScroll = false;
    }

    [JSInvokable]
    public async Task OnSelectionChangedInternal(List<string> taskIds)
    {
        var selectedTasks = Tasks.Where(t => taskIds.Contains(t.Id)).ToList();
        await OnSelectionChanged.InvokeAsync(selectedTasks);
    }

    [JSInvokable]
    public async Task OnUndoInternal()
    {
        if (_undoStack.Count > 0)
        {
            var currentState = Tasks.Select(t => new GanttTask { Id = t.Id, Name = t.Name, Start = t.Start, End = t.End, Progress = t.Progress, RowIndex = t.RowIndex }).ToList();
            _redoStack.Push(currentState);
            
            Tasks = _undoStack.Pop();
            await RefreshDataAsync();
            await TasksChanged.InvokeAsync(Tasks);
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task OnRedoInternal()
    {
        if (_redoStack.Count > 0)
        {
            var currentState = Tasks.Select(t => new GanttTask { Id = t.Id, Name = t.Name, Start = t.Start, End = t.End, Progress = t.Progress, RowIndex = t.RowIndex }).ToList();
            _undoStack.Push(currentState);
            
            Tasks = _redoStack.Pop();
            await RefreshDataAsync();
            await TasksChanged.InvokeAsync(Tasks);
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task OnDeleteSelectedInternal(List<string> taskIds) => await OnDeleteSelected.InvokeAsync(taskIds);

    [JSInvokable]
    public async Task OnDependencyCreatedInternal(string fromId, string toId)
    {
        if (ReadOnly) return;
        
        var dep = new GanttDependency
        {
            FromTaskId = fromId,
            ToTaskId = toId,
            Type = DependencyType.FinishToStart
        };
        
        Dependencies.Add(dep);
        await OnDependencyCreated.InvokeAsync(dep);
        await DependenciesChanged.InvokeAsync(Dependencies);
        await RefreshDataAsync();
    }

    [JSInvokable]
    public void OnClearSelectionInternal()
    {
        OnSelectionChanged.InvokeAsync(new List<GanttTask>());
    }

    [JSInvokable]
    public async Task OnTaskInteractionEndInternal(string taskId, string mode, double dx, double dy)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null || ReadOnly) return;

        SaveState();

        var zoomSettings = _layoutEngine.GetZoomSettings(TimeScale.ZoomLevel);
        var columnWidth = zoomSettings.ColumnWidth;

        // Apply changes based on interaction
        // This is a simplified logic
        if (mode == "move")
        {
            var days = dx / columnWidth;
            if (zoomSettings.BottomUnit == TimeUnit.Hour) days /= 24;
            else if (zoomSettings.BottomUnit == TimeUnit.Minute15) days /= (24 * 4);
            
            task.Start = task.Start.AddDays(days);
            task.End = task.End.AddDays(days);
        }
        else if (mode == "resize-start")
        {
            var days = dx / columnWidth;
            if (zoomSettings.BottomUnit == TimeUnit.Hour) days /= 24;
            else if (zoomSettings.BottomUnit == TimeUnit.Minute15) days /= (24 * 4);
            
            task.Start = task.Start.AddDays(days);
        }
        else if (mode == "resize-end")
        {
            var days = dx / columnWidth;
            if (zoomSettings.BottomUnit == TimeUnit.Hour) days /= 24;
            else if (zoomSettings.BottomUnit == TimeUnit.Minute15) days /= (24 * 4);
            
            task.End = task.End.AddDays(days);
        }
        else if (mode == "progress")
        {
            var width = _layoutEngine.GetWidth(task.End - task.Start, TimeScale.ZoomLevel);
            var dProgress = dx / width;
            task.Progress = Math.Clamp(task.Progress + dProgress, 0, 1);
        }

        await RefreshDataAsync();
        await TasksChanged.InvokeAsync(Tasks);
    }

    // Public API
    /// <summary>Increases the zoom level of the timeline.</summary>
    public async Task ZoomIn()
    {
        if (TimeScale.ZoomLevel < 7)
        {
            TimeScale.ZoomLevel++;
            await OnZoomChanged.InvokeAsync(TimeScale.ZoomLevel);
            await RefreshDataAsync();
        }
    }

    /// <summary>Decreases the zoom level of the timeline.</summary>
    public async Task ZoomOut()
    {
        if (TimeScale.ZoomLevel > 1)
        {
            TimeScale.ZoomLevel--;
            await OnZoomChanged.InvokeAsync(TimeScale.ZoomLevel);
            await RefreshDataAsync();
        }
    }
    /// <summary>Scrolls the timeline to center on today's date.</summary>
    public async Task ScrollToToday()
    {
        if (_jsInstance == null) return;
        var minDate = ProjectStart ?? (Tasks.Any() ? Tasks.Min(t => t.Start) : DateTime.Today).AddDays(-7);
        var x = _layoutEngine.GetX(DateTime.Today, minDate, TimeScale.ZoomLevel);
        
        // Center today in the viewport if possible
        var rect = await JS.InvokeAsync<double[]>("eval", "const r = document.querySelector('.sg-gantt-canvas-element').getBoundingClientRect(); [r.width, r.height]");
        var viewportWidth = rect[0];
        
        await SafeInvokeVoidAsync("scrollTo", _jsInstance, Math.Max(0, x - viewportWidth / 2), null);
    }

    /// <summary>Adjusts the zoom level so all tasks fit within the viewport width.</summary>
    public async Task FitToScreen()
    {
        if (_jsInstance == null || !Tasks.Any()) return;

        var minDate = Tasks.Min(t => t.Start);
        var maxDate = Tasks.Max(t => t.End);
        var totalDuration = maxDate - minDate;

        var rect = await JS.InvokeAsync<double[]>("eval", "const r = document.querySelector('.sg-gantt-canvas-element').getBoundingClientRect(); [r.width, r.height]");
        var viewportWidth = rect[0];

        // Try to find a zoom level where totalWidth <= viewportWidth
        int bestZoom = 1;
        for (int z = 7; z >= 1; z--)
        {
            var width = _layoutEngine.GetWidth(totalDuration, z);
            if (width <= viewportWidth)
            {
                bestZoom = z;
                break;
            }
        }

        TimeScale.ZoomLevel = bestZoom;
        ProjectStart = minDate.AddDays(-1);
        
        await RefreshDataAsync();
        await SafeInvokeVoidAsync("scrollTo", _jsInstance, 0, 0);
    }

    /// <summary>Restores the previous task state from the undo stack.</summary>
    public async Task Undo() => await OnUndoInternal();
    /// <summary>Reapplies the last undone task state from the redo stack.</summary>
    public async Task Redo() => await OnRedoInternal();

    /// <summary>Toggles the collapsed state of a summary task.</summary>
    public async Task ToggleCollapse(GanttTask task)
    {
        task.IsCollapsed = !task.IsCollapsed;
        await RefreshDataAsync();
    }

    /// <summary>Expands all collapsed summary tasks.</summary>
    public async Task ExpandAll()
    {
        foreach (var task in Tasks.Where(t => t.IsSummary))
        {
            task.IsCollapsed = false;
        }
        await RefreshDataAsync();
    }

    /// <summary>Collapses all summary tasks.</summary>
    public async Task CollapseAll()
    {
        foreach (var task in Tasks.Where(t => t.IsSummary))
        {
            task.IsCollapsed = true;
        }
        await RefreshDataAsync();
    }
}
