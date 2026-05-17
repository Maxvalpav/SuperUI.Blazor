using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using SuperUI.Components.SgMachineScheduler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperUI.Components.SgMachineScheduler;

public partial class SgMachineScheduler : ComponentBase
{
    // Parameters
    [Parameter] public List<MachineResource> Resources { get; set; } = new();
    [Parameter] public List<MachineReservation> Reservations { get; set; } = new();
    [Parameter] public List<MachineDowntime> Downtimes { get; set; } = new();
    [Parameter] public DateTime VisibleStart { get; set; } = DateTime.Today;
    [Parameter] public DateTime VisibleEnd { get; set; } = DateTime.Today.AddDays(7);

    // Callbacks
    [Parameter] public EventCallback<MachineReservation> OnReservationClick { get; set; }
    [Parameter] public EventCallback<MachineReservation> OnReservationMoved { get; set; }
    [Parameter] public EventCallback<MachineReservation> OnReservationResized { get; set; }
    [Parameter] public EventCallback<(DateTime Start, int MachineId)> OnSlotDblClick { get; set; }

    // Internal state
    private SKCanvasView? _canvasView;
    private ElementReference _containerElement;
    
    private float _pixelsPerHour = 140f;
    private float _rowHeight = 100f;
    private float _headerHeight = 90f;
    private float _labelWidth = 350f;
    
    private float _scrollOffsetX = 0f;
    private float _scrollOffsetY = 0f;
    
    private bool _isPanning;
    private SKPoint _lastPanPoint;
    
    private bool _tooltipVisible;
    private float _tooltipX;
    private float _tooltipY;
    private RenderFragment? _tooltipContent;

    // Caches
    private Dictionary<int, float> _machineRowTopCache = new();
    private Dictionary<int, SKRect> _reservationRectCache = new();
    private float _totalContentHeight;
    private float _totalContentWidth;

    // Scale
    private float _scaleFactor = 1f;
    private const float MinScale = 0.125f;
    private const float MaxScale = 16f;

    // Drag & Drop
    private int? _draggingReservationId;
    private SKPoint _dragOffset;

    protected override void OnParametersSet()
    {
        InvalidateLayout();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ScrollToTime(DateTime.Now.AddHours(-2));
            StateHasChanged();
        }
    }

    public void ScrollToTime(DateTime dt)
    {
        var x = (float)(dt - VisibleStart).TotalHours * _pixelsPerHour * _scaleFactor;
        _scrollOffsetX = Math.Max(0, x - 100); // Leave some margin
        InvalidateLayout();
        _canvasView?.Invalidate();
    }

    private void InvalidateLayout()
    {
        _machineRowTopCache.Clear();
        _reservationRectCache.Clear();

        float currentY = _headerHeight;

        var grouped = Resources
            .GroupBy(r => r.Group)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            foreach (var machine in group.OrderBy(m => m.Name))
            {
                _machineRowTopCache[machine.Id] = currentY;

                var machineReservations = Reservations
                    .Where(r => r.MachineId == machine.Id);

                foreach (var res in machineReservations)
                {
                    float x = DateTimeToX(res.StartTime);
                    float width = DateTimeToX(res.EndTime) - x;
                    float y = currentY + 15;
                    float height = _rowHeight - 30;

                    _reservationRectCache[res.Id] = new SKRect(x, y, x + width, y + height);
                }

                currentY += _rowHeight;
            }
        }

        _totalContentHeight = currentY;
        _totalContentWidth = DateTimeToX(VisibleEnd);
    }

    private float DateTimeToX(DateTime dt)
    {
        var totalHours = (dt - VisibleStart).TotalHours;
        return (float)(totalHours * _pixelsPerHour * _scaleFactor) + _labelWidth;
    }

    private DateTime XToDateTime(float x)
    {
        var totalHours = (x - _labelWidth) / (_pixelsPerHour * _scaleFactor);
        return VisibleStart.AddHours(totalHours);
    }

    private void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;

        canvas.Clear(SKColors.White);

        // --- LAYER 1-5: Content (Scrolled) ---
        canvas.Save();
        canvas.ClipRect(new SKRect(_labelWidth, _headerHeight, info.Width, info.Height));
        canvas.Translate(-_scrollOffsetX, -_scrollOffsetY);
        
        DrawGrid(canvas, info);
        DrawNonWorkingZones(canvas, info);
        DrawNowLine(canvas, info);
        DrawDowntimes(canvas, info);
        DrawReservations(canvas, info);
        canvas.Restore();

        // --- LAYER 6-7: Headers (Fixed) ---
        DrawMachineHeaders(canvas, info);
        DrawTimelineHeader(canvas, info);
    }

    private void DrawGrid(SKCanvas canvas, SKImageInfo info)
    {
        using var gridPaint = new SKPaint { Color = SKColor.Parse("#F1F5F9"), StrokeWidth = 1f, IsAntialias = false };
        using var majorPaint = new SKPaint { Color = SKColor.Parse("#CBD5E1"), StrokeWidth = 2f, IsAntialias = false };

        var start = VisibleStart.Date;
        var end = VisibleEnd.Date.AddDays(1);
        for (var dt = start; dt <= end; dt = dt.AddHours(1))
        {
            float x = DateTimeToX(dt);
            if (x < _scrollOffsetX + _labelWidth - 200 || x > _scrollOffsetX + info.Width + 200) continue;

            var paint = dt.Hour == 0 ? majorPaint : gridPaint;
            canvas.DrawLine(x, _headerHeight + _scrollOffsetY, x, Math.Max(info.Height + _scrollOffsetY, _totalContentHeight), paint);
        }

        foreach (var kvp in _machineRowTopCache)
        {
            float y = kvp.Value;
            canvas.DrawLine(_labelWidth + _scrollOffsetX, y, _totalContentWidth + 2000, y, gridPaint);
        }
    }

    private void DrawNonWorkingZones(SKCanvas canvas, SKImageInfo info)
    {
        using var nonWorkingPaint = new SKPaint { Color = SKColor.Parse("#F8FAFC"), Style = SKPaintStyle.Fill };

        for (var day = VisibleStart.Date; day <= VisibleEnd.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
            {
                float x = DateTimeToX(day);
                float w = DateTimeToX(day.AddDays(1)) - x;
                var rect = new SKRect(x, _headerHeight + _scrollOffsetY, x + w, Math.Max(info.Height + _scrollOffsetY, _totalContentHeight));
                canvas.DrawRect(rect, nonWorkingPaint);
            }
        }
    }

    private void DrawNowLine(SKCanvas canvas, SKImageInfo info)
    {
        var now = DateTime.Now;
        if (now < VisibleStart || now > VisibleEnd) return;

        float x = DateTimeToX(now);
        using var linePaint = new SKPaint { Color = SKColor.Parse("#EF4444"), StrokeWidth = 3f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 10f, 5f }, 0) };
        canvas.DrawLine(x, _headerHeight + _scrollOffsetY, x, Math.Max(info.Height + _scrollOffsetY, _totalContentHeight), linePaint);
        
        using var headPaint = new SKPaint { Color = SKColor.Parse("#EF4444"), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(x, _headerHeight + _scrollOffsetY, 8, headPaint);
    }

    private void DrawDowntimes(SKCanvas canvas, SKImageInfo info)
    {
        using var paint = new SKPaint { Color = SKColors.Red.WithAlpha(45), Style = SKPaintStyle.Fill };
        foreach (var downtime in Downtimes)
        {
            if (!_machineRowTopCache.TryGetValue(downtime.MachineId, out var top)) continue;
            float x = DateTimeToX(downtime.Start);
            float endX = downtime.End.HasValue ? DateTimeToX(downtime.End.Value) : DateTimeToX(VisibleEnd);
            canvas.DrawRect(new SKRect(x, top + 10, endX, top + _rowHeight - 10), paint);
        }
    }

    private void DrawReservations(SKCanvas canvas, SKImageInfo info)
    {
        using var textPaint = new SKPaint { Color = SKColors.White, TextSize = 17, IsAntialias = true, FakeBoldText = true };
        using var subTextPaint = new SKPaint { Color = SKColors.White.WithAlpha(230), TextSize = 14, IsAntialias = true };

        foreach (var reservation in Reservations)
        {
            if (!_reservationRectCache.TryGetValue(reservation.Id, out var rect)) continue;

            var color = GetReservationColor(reservation);
            
            using var shadowPaint = new SKPaint { Color = SKColors.Black.WithAlpha(60), IsAntialias = true, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4) };
            canvas.DrawRoundRect(new SKRect(rect.Left + 2, rect.Top + 2, rect.Right + 2, rect.Bottom + 2), 10, 10, shadowPaint);

            using var fillPaint = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawRoundRect(rect, 10, 10, fillPaint);

            if (rect.Width > 60)
            {
                canvas.Save();
                canvas.ClipRect(rect);
                canvas.DrawText(reservation.OrderNumber, rect.Left + 15, rect.Top + 32, textPaint);
                if (rect.Height > 50) canvas.DrawText(reservation.OperationName, rect.Left + 15, rect.Top + 55, subTextPaint);
                canvas.Restore();
            }
        }
    }

    private void DrawMachineHeaders(SKCanvas canvas, SKImageInfo info)
    {
        using var bgPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
        canvas.DrawRect(0, 0, _labelWidth, info.Height, bgPaint);
        
        using var borderPaint = new SKPaint { Color = SKColor.Parse("#E2E8F0"), StrokeWidth = 1f };
        canvas.DrawLine(_labelWidth, 0, _labelWidth, info.Height, borderPaint);

        using var textPaint = new SKPaint { Color = SKColor.Parse("#0F172A"), TextSize = 18, IsAntialias = true, FakeBoldText = true };
        using var groupTextPaint = new SKPaint { Color = SKColor.Parse("#64748B"), TextSize = 13, IsAntialias = true, FakeBoldText = true };

        foreach (var machine in Resources)
        {
            if (!_machineRowTopCache.TryGetValue(machine.Id, out var top)) continue;
            float y = top - _scrollOffsetY;
            if (y + _rowHeight < _headerHeight || y > info.Height) continue;

            var statusColor = GetMachineStatusColor(machine.Status);
            using var statusPaint = new SKPaint { Color = statusColor.WithAlpha(30), Style = SKPaintStyle.Fill };
            canvas.DrawRect(0, y, _labelWidth, _rowHeight, statusPaint);
            
            using var dotPaint = new SKPaint { Color = statusColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawCircle(25, y + _rowHeight / 2, 8, dotPaint);

            canvas.DrawText(machine.Name, 50, y + 42, textPaint);
            canvas.DrawText(machine.Group.ToUpper(), 50, y + 68, groupTextPaint);
            canvas.DrawLine(0, y + _rowHeight, _labelWidth, y + _rowHeight, borderPaint);
        }
        
        canvas.DrawRect(0, 0, _labelWidth, _headerHeight, bgPaint);
        canvas.DrawLine(_labelWidth, 0, _labelWidth, _headerHeight, borderPaint);
        canvas.DrawLine(0, _headerHeight, _labelWidth, _headerHeight, borderPaint);
    }

    private void DrawTimelineHeader(SKCanvas canvas, SKImageInfo info)
    {
        using var bgPaint = new SKPaint { Color = SKColor.Parse("#F8FAFC"), Style = SKPaintStyle.Fill };
        canvas.DrawRect(_labelWidth, 0, info.Width, _headerHeight, bgPaint);
        using var borderPaint = new SKPaint { Color = SKColor.Parse("#E2E8F0"), StrokeWidth = 1f };
        canvas.DrawLine(_labelWidth, _headerHeight, info.Width, _headerHeight, borderPaint);

        using var hourTextPaint = new SKPaint { Color = SKColor.Parse("#475569"), TextSize = 14, IsAntialias = true };
        using var dayTextPaint = new SKPaint { Color = SKColor.Parse("#0F172A"), TextSize = 17, IsAntialias = true, FakeBoldText = true };

        canvas.Save();
        canvas.ClipRect(new SKRect(_labelWidth, 0, info.Width, _headerHeight));
        canvas.Translate(-_scrollOffsetX, 0);

        for (var dt = VisibleStart.Date; dt <= VisibleEnd.Date.AddDays(1); dt = dt.AddHours(1))
        {
            float x = DateTimeToX(dt);
            if (x < _scrollOffsetX + _labelWidth - 200 || x > _scrollOffsetX + info.Width + 200) continue;

            if (dt.Hour == 0) canvas.DrawText(dt.ToString("dd MMM, dddd"), x + 15, 35, dayTextPaint);
            canvas.DrawText(dt.ToString("HH:mm"), x + 15, 65, hourTextPaint);
            canvas.DrawLine(x, _headerHeight - 15, x, _headerHeight, borderPaint);
        }
        canvas.Restore();
    }

    public void ZoomIn()
    {
        _scaleFactor = Math.Min(MaxScale, _scaleFactor * 1.2f);
        InvalidateLayout();
        _canvasView?.Invalidate();
    }

    public void ZoomOut()
    {
        _scaleFactor = Math.Max(MinScale, _scaleFactor / 1.2f);
        InvalidateLayout();
        _canvasView?.Invalidate();
    }

    private SKColor GetMachineStatusColor(MachineStatus status) => status switch
    {
        MachineStatus.Online => SKColor.Parse("#10B981"),
        MachineStatus.Offline => SKColor.Parse("#6B7280"),
        MachineStatus.Maintenance => SKColor.Parse("#F59E0B"),
        MachineStatus.Fault => SKColor.Parse("#EF4444"),
        _ => SKColor.Parse("#9CA3AF")
    };

    private SKColor GetReservationColor(MachineReservation r) => r.Status switch
    {
        ReservationStatus.Planned => SKColor.Parse("#3B82F6"),
        ReservationStatus.InProgress => SKColor.Parse("#10B981"),
        ReservationStatus.Completed => SKColor.Parse("#6B7280"),
        ReservationStatus.Delayed => SKColor.Parse("#F59E0B"),
        ReservationStatus.Overdue => SKColor.Parse("#EF4444"),
        ReservationStatus.Cancelled => SKColor.Parse("#D1D5DB"),
        _ => SKColor.Parse("#9CA3AF")
    };

    private void OnWheel(WheelEventArgs e)
    {
        if (e.CtrlKey)
        {
            var mouseX = (float)e.OffsetX;
            float zoomDelta = e.DeltaY > 0 ? 0.9f : 1.1f;
            float newScale = _scaleFactor * zoomDelta;
            newScale = Math.Clamp(newScale, MinScale, MaxScale);
            _scrollOffsetX = mouseX - (mouseX - _scrollOffsetX + _labelWidth) * (newScale / _scaleFactor) - _labelWidth;
            _scaleFactor = newScale;
        }
        else
        {
            if (e.ShiftKey) _scrollOffsetX += (float)e.DeltaY;
            else _scrollOffsetY += (float)e.DeltaY;
        }
        _scrollOffsetY = Math.Max(0, _scrollOffsetY);
        _scrollOffsetX = Math.Max(0, _scrollOffsetX);
        InvalidateLayout();
        _canvasView?.Invalidate();
    }

    private void OnPointerDown(PointerEventArgs e)
    {
        if (e.Button == 0)
        {
            var point = new SKPoint((float)e.OffsetX, (float)e.OffsetY);
            foreach (var kvp in _reservationRectCache)
            {
                var rect = kvp.Value;
                var drawRect = new SKRect(rect.Left - _scrollOffsetX, rect.Top - _scrollOffsetY, rect.Right - _scrollOffsetX, rect.Bottom - _scrollOffsetY);
                if (drawRect.Contains(point))
                {
                    _draggingReservationId = kvp.Key;
                    _dragOffset = new SKPoint(point.X - (rect.Left - _scrollOffsetX), point.Y - (rect.Top - _scrollOffsetY));
                    return;
                }
            }
            _isPanning = true;
            _lastPanPoint = point;
        }
    }

    private void OnPointerMove(PointerEventArgs e)
    {
        var point = new SKPoint((float)e.OffsetX, (float)e.OffsetY);
        if (_draggingReservationId != null)
        {
            var res = Reservations.FirstOrDefault(r => r.Id == _draggingReservationId);
            if (res != null)
            {
                float x = point.X - _dragOffset.X + _scrollOffsetX;
                float width = DateTimeToX(res.EndTime) - DateTimeToX(res.StartTime);
                float y = point.Y - _dragOffset.Y + _scrollOffsetY;
                _reservationRectCache[res.Id] = new SKRect(x, y, x + width, y + _rowHeight - 24);
            }
            _canvasView?.Invalidate();
        }
        else if (_isPanning)
        {
            var delta = _lastPanPoint - point;
            _scrollOffsetX += delta.X;
            _scrollOffsetY += delta.Y;
            _scrollOffsetX = Math.Max(0, _scrollOffsetX);
            _scrollOffsetY = Math.Max(0, _scrollOffsetY);
            _lastPanPoint = point;
            InvalidateLayout();
            _canvasView?.Invalidate();
        }
        else UpdateTooltip(point);
    }

    private DateTime SnapToGrid(DateTime dt, TimeSpan grid)
    {
        long ticks = dt.Ticks / grid.Ticks;
        return new DateTime(ticks * grid.Ticks, dt.Kind);
    }

    private int FindMachineAtY(float y)
    {
        foreach (var kvp in _machineRowTopCache)
        {
            if (y >= kvp.Value && y < kvp.Value + _rowHeight) return kvp.Key;
        }
        return -1;
    }

    private async Task OnPointerUp(PointerEventArgs e)
    {
        if (_draggingReservationId != null)
        {
            var res = Reservations.FirstOrDefault(r => r.Id == _draggingReservationId);
            if (res != null)
            {
                var point = new SKPoint((float)e.OffsetX, (float)e.OffsetY);
                int newMachineId = FindMachineAtY(point.Y + _scrollOffsetY);
                if (newMachineId != -1) res.MachineId = newMachineId;
                var newStartTime = XToDateTime(point.X - _dragOffset.X + _scrollOffsetX);
                var duration = res.EndTime - res.StartTime;
                res.StartTime = SnapToGrid(newStartTime, TimeSpan.FromMinutes(15));
                res.EndTime = res.StartTime + duration;
                await OnReservationMoved.InvokeAsync(res);
            }
            _draggingReservationId = null;
        }
        _isPanning = false;
        InvalidateLayout();
        _canvasView?.Invalidate();
    }

    private void OnPointerLeave(PointerEventArgs e)
    {
        _isPanning = false;
        _draggingReservationId = null;
        _tooltipVisible = false;
        StateHasChanged();
    }

    private void UpdateTooltip(SKPoint point)
    {
        bool found = false;
        foreach (var kvp in _reservationRectCache)
        {
            var rect = kvp.Value;
            var drawRect = new SKRect(rect.Left - _scrollOffsetX, rect.Top - _scrollOffsetY, rect.Right - _scrollOffsetX, rect.Bottom - _scrollOffsetY);
            if (drawRect.Contains(point))
            {
                var res = Reservations.FirstOrDefault(r => r.Id == kvp.Key);
                if (res != null)
                {
                    _tooltipContent = (builder) =>
                    {
                        builder.OpenElement(0, "div");
                        builder.AddAttribute(1, "style", "font-weight:bold;margin-bottom:4px;font-size:14px;");
                        builder.AddContent(2, $"{res.OrderNumber} | {res.OperationName}");
                        builder.CloseElement();
                        builder.OpenElement(3, "div");
                        builder.AddContent(4, $"Деталь: {res.PartNumber}");
                        builder.CloseElement();
                        builder.OpenElement(5, "div");
                        builder.AddContent(6, $"Время: {res.StartTime:HH:mm} - {res.EndTime:HH:mm} ({(res.EndTime - res.StartTime).TotalHours:F1}ч)");
                        builder.CloseElement();
                        builder.OpenElement(7, "div");
                        builder.AddAttribute(8, "style", $"color:{GetReservationColor(res).ToString()};font-weight:bold;text-transform:uppercase;margin-top:6px;font-size:11px;");
                        builder.AddContent(9, res.Status.ToString());
                        builder.CloseElement();
                    };
                    _tooltipX = point.X + 20;
                    _tooltipY = point.Y + 20;
                    _tooltipVisible = true;
                    found = true;
                    break;
                }
            }
        }
        if (!found && _tooltipVisible) _tooltipVisible = false;
        StateHasChanged();
    }
}
