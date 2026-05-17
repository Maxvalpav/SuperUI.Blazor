# 🏭 Blazor Canvas Component: Планировщик Занятости Производственных Станков

## 🎯 Архитектурное Видение

Создание **самого лучшего в мире Blazor-компонента** для визуализации загрузки станков/ресурсов по временной шкале (Timeline + Gantt-подход). Компонент рисуется через **SkiaSharp Canvas** (НЕ JS Interop для рисования!), что даёт 60fps даже на 1000+ резервах.

---

## 🔧 Технологический Стек

| Слой | Технология | Почему |
|---|---|---|
| **Рендеринг** | `SkiaSharp.Views.Blazor` + `SKCanvasView` | Прямой доступ к WebGL-бэкенду SkiaSharp, обходит узкое горло JS Interop. Отрисовка 70+ fps в WASM |
| **Фреймворк** | Blazor (.NET 8/9) | C# на всём стеке, компонентная модель |
| **Ввод** | Blazor Touch/Mouse Events (нативные) | Не нужен JS для обработки касаний/мыши |
| **Данные** | `IAsyncEnumerable<T>` + SignalR | Real-time обновления из MES/ERP |
| **Состояние** | `StateContainer` pattern (Scoped) | Централизованный стейт для переключения view без перерисовок |

### NuGet Пакеты:
```xml
<PackageReference Include="SkiaSharp" Version="3.116.1" />
<PackageReference Include="SkiaSharp.Views.Blazor" Version="3.116.1" />
<PackageReference Include="SkiaSharp.HarfBuzz" Version="3.116.1" /> <!-- Для текста -->
```

---

## 🧬 Модель Данных (Domain Model)

```csharp
// === РЕСУРС (Станок / Рабочий Центр) ===
public record MachineResource
{
    public int Id { get; init; }
    public string Name { get; init; }              // "ЧПУ Haas VF-2 #3"
    public string Group { get; init; }             // "Токарная группа"
    public string Cell { get; init; }              // "Цех №2, Линия А"
    public MachineStatus Status { get; set; }       // Online/Offline/Maintenance/Fault
    public int MaxCapacityUnits { get; init; } = 1; // 1 = один станок, N = группа станков
    public string Color { get; init; }              // Цвет на диаграмме
    public ShiftPattern Shift { get; init; }        // График смен (доступные часы)
    public double HourlyRate { get; init; }         // €/час для расчёта стоимости
}

// === РЕЗЕРВ (Бронирование / Задание) ===
public record MachineReservation
{
    public int Id { get; init; }
    public int MachineId { get; init; }
    public string OrderNumber { get; init; }        // "ORD-2026-04582"
    public string OperationName { get; init; }       // "Фрезерование корпуса"
    public string PartNumber { get; init; }          // "PN-7742-A"
    public string CustomerName { get; init; }
    public DateTime StartTime { get; set; }          // Начало операции
    public DateTime EndTime { get; set; }            // Конец операции
    public DateTime? ActualStart { get; set; }       // Фактическое начало
    public DateTime? ActualEnd { get; set; }         // Фактический конец
    public ReservationStatus Status { get; set; }    // Planned/InProgress/Completed/Delayed/Cancelled
    public int Priority { get; init; } = 50;         // Приоритет (1-100)
    public string Color { get; set; }                // Динамический цвет по статусу
    public string[] Tags { get; init; }              // ["Горячий заказ", "VIP клиент"]
    public double SetupTimeMinutes { get; init; }    // Время наладки
    public double CycleTimeMinutes { get; init; }    // Время цикла
    public int PartsCount { get; init; }             // Количество деталей
}

// === ПРОСТОЙ (Downtime) ===
public record MachineDowntime
{
    public int Id { get; init; }
    public int MachineId { get; init; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public DowntimeReason Reason { get; init; }      // Setup/Breakdown/MaterialWait/Maintenance/NoOperator
    public string Note { get; init; }
}

// === СМЕНА ===
public record ShiftPattern
{
    public DayOfWeek DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }         // "06:00"
    public TimeSpan EndTime { get; init; }           // "14:00"
    public bool IsWorking { get; init; }
}
```

---

## 🎨 1. Базовая Архитектура SKCanvasView Компонента

### `MachineTimeline.razor`

```csharp
// Файл: Components/MachineTimeline.razor

@using SkiaSharp.Views.Blazor
@using SkiaSharp

<div class="machine-timeline-container"
     @ref="_containerElement"
     @onwheel="OnWheel"
     @onpointerdown="OnPointerDown"
     @onpointermove="OnPointerMove"
     @onpointerup="OnPointerUp">

    <SKCanvasView @ref="_canvasView"
                  OnPaintSurface="OnPaintSurfaceAsync"
                  IgnorePixelScaling="true" />

    <!-- Оверлей для drag-and-drop (отдельный div поверх Canvas) -->
    <div class="tooltip-overlay" style="display:@(_tooltipVisible ? "block" : "none")"
         style="left:@(_tooltipX)px; top:@(_tooltipY)px;">
        @_tooltipContent
    </div>
</div>

@code {
    // Параметры компонента
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

    // Внутреннее состояние
    private SKCanvasView? _canvasView;
    private ElementReference _containerElement;
    private float _pixelsPerHour = 80f;
    private float _rowHeight = 48f;
    private float _headerHeight = 56f;
    private float _labelWidth = 200f;
    private float _scrollOffsetX = 0f;
    private float _scrollOffsetY = 0f;
    private SKMatrix _transform = SKMatrix.Identity; // Матрица для зума/панорамы
    private bool _isPanning;
    private SKPoint _lastPanPoint;

    // Кэш layout-данных (вычисляется один раз при изменении данных)
    private Dictionary<int, float> _machineRowTopCache = new();
    private Dictionary<int, int> _machineRowIndexCache = new();
    private Dictionary<int, SKRect> _reservationRectCache = new();
    private float _totalContentHeight;
    private float _totalContentWidth;
}
```

---

## 🧮 2. Layout Engine — Сердце Компонента

### Вычисление позиций (вызывается при изменении данных/зума/скролла)

```csharp
private void InvalidateLayout()
{
    _machineRowIndexCache.Clear();
    _machineRowTopCache.Clear();
    _reservationRectCache.Clear();

    float currentY = _headerHeight;

    // Группировка ресурсов: сначала по Group, затем по Cell
    var grouped = Resources
        .GroupBy(r => r.Group)
        .OrderBy(g => g.Key);

    foreach (var group in grouped)
    {
        // Отрисовка заголовка группы (+40px высоты)
        // currentY += GroupHeaderHeight;

        foreach (var machine in group.OrderBy(m => m.Name))
        {
            _machineRowIndexCache[machine.Id] = (int)(currentY / _rowHeight);
            _machineRowTopCache[machine.Id] = currentY;

            // Вычисляем прямоугольники резервов для этой машины
            var machineReservations = Reservations
                .Where(r => r.MachineId == machine.Id)
                .OrderBy(r => r.StartTime);

            foreach (var res in machineReservations)
            {
                float x = DateTimeToX(res.StartTime);
                float width = DateTimeToX(res.EndTime) - x;
                float y = currentY + 4;
                float height = _rowHeight - 8;

                _reservationRectCache[res.Id] = new SKRect(x, y, x + width, y + height);
            }

            // Также вычисляем прямоугольники простоев
            // ...

            currentY += _rowHeight;
        }
    }

    _totalContentHeight = currentY;
    _totalContentWidth = DateTimeToX(VisibleEnd);
}
```

### Конвертация дат в координаты:

```csharp
private float DateTimeToX(DateTime dt)
{
    var totalHours = (dt - VisibleStart).TotalHours;
    return (float)(totalHours * _pixelsPerHour) - _scrollOffsetX;
    // + учёт transform matrix для зума
}

private DateTime XToDateTime(float x)
{
    var totalHours = (x + _scrollOffsetX) / _pixelsPerHour;
    return VisibleStart.AddHours(totalHours);
}
```

---

## 🖌️ 3. Рендеринг — 7 Слоёв Отрисовки

Метод `OnPaintSurfaceAsync` должен рисовать **строго по слоям**, чтобы минимизировать перерисовки:

```csharp
private async Task OnPaintSurfaceAsync(SKPaintSurfaceEventArgs e)
{
    var canvas = e.Surface.Canvas;
    var info = e.Info;

    canvas.Clear(SKColors.White);

    // Применяем матрицу трансформации (зум + панорама)
    canvas.SetMatrix(_transform);

    // --- СЛОЙ 1: Фон + Сетка ---
    DrawGrid(canvas, info);

    // --- СЛОЙ 2: Неактивные зоны (выходные, нерабочие часы) ---
    DrawNonWorkingZones(canvas, info);

    // --- СЛОЙ 3: Текущее время (красная вертикальная линия "СЕЙЧАС") ---
    DrawNowLine(canvas, info);

    // --- СЛОЙ 4: Простои (Downtime) — полупрозрачный красный ---
    DrawDowntimes(canvas, info);

    // --- СЛОЙ 5: Резервы (задания) — основной контент ---
    DrawReservations(canvas, info);

    // --- СЛОЙ 6: Заголовки машин (левая панель) — фиксированная ---
    DrawMachineHeaders(canvas, info);

    // --- СЛОЙ 7: Временная шкала (верхняя панель) — фиксированная ---
    DrawTimelineHeader(canvas, info);
}
```

---

## 🧩 4. Детали Отрисовки Каждого Слоя

### 4.1 Сетка (Grid)

```csharp
private void DrawGrid(SKCanvas canvas, SKImageInfo info)
{
    using var gridPaint = new SKPaint
    {
        Color = SKColor.Parse("#E8ECF0"),
        StrokeWidth = 1f,
        IsAntialias = false
    };

    // Вертикальные линии — каждый час
    // + жирные линии на начало смены
    // + жирные линии на полночь
    for (var dt = VisibleStart.Date; dt <= VisibleEnd; dt = dt.AddHours(1))
    {
        float x = DateTimeToX(dt);
        bool isMajor = dt.Hour == 0 || dt.Hour == 6 || dt.Hour == 14 || dt.Hour == 22;

        using var paint = isMajor
            ? new SKPaint { Color = SKColor.Parse("#C0C8D0"), StrokeWidth = 1.5f, IsAntialias = false }
            : gridPaint;

        canvas.DrawLine(x, 0, x, _totalContentHeight, paint);
    }

    // Горизонтальные линии — каждая машина
    foreach (var kvp in _machineRowTopCache)
    {
        float y = kvp.Value;
        canvas.DrawLine(0, y, _totalContentWidth, y, gridPaint);
        canvas.DrawLine(0, y + _rowHeight, _totalContentWidth, y + _rowHeight, gridPaint);
    }
}
```

### 4.2 Нерабочие Зоны

```csharp
private void DrawNonWorkingZones(SKCanvas canvas, SKImageInfo info)
{
    using var nonWorkingPaint = new SKPaint
    {
        Color = SKColor.Parse("#F5F5F5"), // Светло-серый
        Style = SKPaintStyle.Fill
    };

    // Для каждого дня: закрашиваем часы вне смен
    for (var day = VisibleStart.Date; day <= VisibleEnd.Date; day = day.AddDays(1))
    {
        if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
        {
            // Выходной — полностью серый
            float x = DateTimeToX(day);
            float w = DateTimeToX(day.AddDays(1)) - x;
            canvas.DrawRect(x, 0, w, _totalContentHeight, nonWorkingPaint);
        }
        else
        {
            // Рабочий день — закрашиваем только ночные часы
            // 00:00-06:00 и 22:00-24:00
        }
    }
}
```

### 4.3 Линия «Сейчас»

```csharp
private void DrawNowLine(SKCanvas canvas, SKImageInfo info)
{
    var now = DateTime.Now;
    if (now < VisibleStart || now > VisibleEnd) return;

    float x = DateTimeToX(now);

    using var linePaint = new SKPaint
    {
        Color = SKColor.Parse("#E53935"),
        StrokeWidth = 2f,
        IsAntialias = true,
        PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0)
    };

    canvas.DrawLine(x, 0, x, _totalContentHeight, linePaint);

    // Красный кружок на линии
    using var circlePaint = new SKPaint
    {
        Color = SKColor.Parse("#E53935"),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };
    canvas.DrawCircle(x, _headerHeight / 2, 6, circlePaint);
}
```

### 4.4 Резервы (Основной контент) — **Самый важный слой**

```csharp
private void DrawReservations(SKCanvas canvas, SKImageInfo info)
{
    foreach (var reservation in Reservations)
    {
        if (!_reservationRectCache.TryGetValue(reservation.Id, out var rect))
            continue;

        // Пропускаем, если за пределами viewport
        if (rect.Right < 0 || rect.Left > info.Width) continue;

        var color = GetReservationColor(reservation);
        var statusIcon = GetStatusIcon(reservation.Status);

        // --- 1. Тень блока ---
        using var shadowPaint = new SKPaint { Color = SKColors.Black.WithAlpha(25), IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(rect with { Left = rect.Left + 1, Top = rect.Top + 1 }, 4), shadowPaint);

        // --- 2. Основной прямоугольник с градиентом ---
        using var gradientShader = SKShader.CreateLinearGradient(
            new SKPoint(rect.Left, rect.Top),
            new SKPoint(rect.Left, rect.Bottom),
            new[] { color, color.WithAlpha(180) },
            null,
            SKShaderTileMode.Clamp);
        using var fillPaint = new SKPaint { Shader = gradientShader, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawRoundRect(new SKRoundRect(rect, 4), fillPaint);

        // --- 3. Полоса прогресса (если выполняется) ---
        if (reservation.Status == ReservationStatus.InProgress && reservation.ActualStart.HasValue)
        {
            var now = DateTime.Now;
            var progress = (now - reservation.ActualStart.Value).TotalMinutes /
                           (reservation.EndTime - reservation.StartTime).TotalMinutes;
            progress = Math.Clamp(progress, 0, 1);

            var progressRect = new SKRect(rect.Left, rect.Bottom - 4,
                                          rect.Left + rect.Width * (float)progress, rect.Bottom);
            using var progressPaint = new SKPaint { Color = SKColors.White.WithAlpha(180), IsAntialias = true };
            canvas.DrawRect(progressRect, progressPaint);
        }

        // --- 4. Текст заказа ---
        DrawTextInRect(canvas, $"{reservation.OrderNumber} | {reservation.OperationName}",
                       rect, SKColors.White, 12f);

        // --- 5. Флаг приоритета (красный треугольник в углу для горячих заказов) ---
        if (reservation.Priority >= 80)
            DrawPriorityFlag(canvas, rect);

        // --- 6. Индикатор наладки (Setup) ---
        if (reservation.SetupTimeMinutes > 0)
            DrawSetupIndicator(canvas, rect, reservation);
    }
}
```

### 4.5 Цветовая Кодировка Резервов

```csharp
private SKColor GetReservationColor(MachineReservation r) => r.Status switch
{
    ReservationStatus.Planned      => SKColor.Parse("#4FC3F7"), // Голубой
    ReservationStatus.InProgress   => SKColor.Parse("#66BB6A"), // Зелёный
    ReservationStatus.Completed    => SKColor.Parse("#78909C"), // Серый
    ReservationStatus.Delayed      => SKColor.Parse("#FF7043"), // Оранжевый
    ReservationStatus.Overdue      => SKColor.Parse("#E53935"), // Красный
    ReservationStatus.Cancelled    => SKColor.Parse("#BDBDBD"), // Светло-серый с перечёркиванием
    _                              => SKColor.Parse("#90A4AE")
};
```

---

## 📐 5. Функциональность Компонента

### 5.1 Зум (Pinch-to-Zoom + Колёсико мыши)

```csharp
// Минимальный/максимальный масштаб: 30 мин = ширина экрана / 1 год = ширина экрана
private float _scaleFactor = 1f;
private const float MinScale = 0.125f; // 8 пикселей на час
private const float MaxScale = 16f;    // 1024 пикселей на час

private void OnWheel(WheelEventArgs e)
{
    // Определить точку под курсором (для зума к точке, а не к центру)
    var mousePoint = new SKPoint((float)e.OffsetX, (float)e.OffsetY);

    float zoomDelta = e.DeltaY > 0 ? 0.9f : 1.1f;
    float newScale = _scaleFactor * zoomDelta;
    newScale = Math.Clamp(newScale, MinScale, MaxScale);

    // Корректируем скролл, чтобы точка под курсором осталась на месте
    _scrollOffsetX = mousePoint.X - (mousePoint.X - _scrollOffsetX) * (newScale / _scaleFactor);
    _scrollOffsetY = mousePoint.Y - (mousePoint.Y - _scrollOffsetY) * (newScale / _scaleFactor);

    _scaleFactor = newScale;
    InvalidateLayout();
    _canvasView?.InvalidateSurface();
}
```

### 5.2 Drag & Drop перемещение резервов

```csharp
private int? _draggingReservationId;
private SKPoint _dragOffset;
private int _targetMachineId;
private DateTime _snappedStartTime;

private void OnPointerDown(PointerEventArgs e)
{
    var point = ScreenToWorld(new SKPoint((float)e.OffsetX, (float)e.OffsetY));

    // Hit-test: найти резерв под курсором
    foreach (var kvp in _reservationRectCache)
    {
        if (kvp.Value.Contains(point))
        {
            _draggingReservationId = kvp.Key;
            _dragOffset = point - new SKPoint(kvp.Value.Left, kvp.Value.Top);
            break;
        }
    }
}

private void OnPointerMove(PointerEventArgs e)
{
    if (_draggingReservationId == null) return;

    var point = ScreenToWorld(new SKPoint((float)e.OffsetX, (float)e.OffsetY));

    // Найти ближайшую машину (по Y)
    _targetMachineId = FindMachineAtY(point.Y);

    // Snapping к началу часа / 15-минуткам
    var exactTime = XToDateTime(point.X - _dragOffset.X);
    _snappedStartTime = SnapToGrid(exactTime, TimeSpan.FromMinutes(15));

    // Обновить кэш прямоугольника (временно)
    // ...
    _canvasView?.InvalidateSurface();
}

private async void OnPointerUp(PointerEventArgs e)
{
    if (_draggingReservationId == null) return;

    var reservation = Reservations.Find(r => r.Id == _draggingReservationId);
    if (reservation != null)
    {
        var duration = reservation.EndTime - reservation.StartTime;
        reservation.StartTime = _snappedStartTime;
        reservation.EndTime = _snappedStartTime + duration;
        reservation.MachineId = _targetMachineId;

        await OnReservationMoved.InvokeAsync(reservation);
    }

    _draggingReservationId = null;
    InvalidateLayout();
    _canvasView?.InvalidateSurface();
}

private DateTime SnapToGrid(DateTime dt, TimeSpan grid)
{
    long ticks = dt.Ticks / grid.Ticks;
    return new DateTime(ticks * grid.Ticks, dt.Kind);
}
```

### 5.3 Resize резервов (изменение длительности)

```csharp
// Правый край резерва — зона ресайза (±6px от правого края)
// При захвате правого края — изменение EndTime
// Аналогично левый край — изменение StartTime
// Snapping к 15-минутным интервалам
```

### 5.4 Tooltip при наведении

```csharp
// При движении мыши находим резерв под курсором
// Показываем HTML-тултип:
// - Номер заказа и операция
// - Станок
// - Время начала/конца
// - Длительность
// - Статус
// - Номер детали + количество
// - Клиент
// - Примечания
```

### 5.5 Контекстное меню (правый клик)

```csharp
// Правая кнопка мыши на резерве → меню:
// - «Редактировать» → модальное окно
// - «Разделить» → разделить резерв на 2 части (если станок сломался посередине)
// - «Сдвинуть вправо» → сдвинуть все последующие резервы этого станка
// - «Поменять станок» → быстрый выбор другого станка
// - «Повысить приоритет» / «Понизить приоритет»
// - «Отменить» → пометить как Cancelled
// - «Удалить»
```

### 5.6 Создание нового резерва (двойной клик / drag по пустому месту)

```csharp
// Двойной клик по пустому слоту → открытие формы создания
// Или: зажать левую кнопку на пустом месте → тянуть → создать резерв нужной длины
```

### 5.7 Подсветка конфликтов (Overbooking)

```csharp
private List<(MachineReservation A, MachineReservation B, SKRect Overlap)> FindConflicts()
{
    // Для каждого станка находим пересекающиеся по времени резервы
    // Если ActualStart/ActualEnd не заданы — используем StartTime/EndTime
    // Overbooking = два задания на одном станке в одно время
    // Отображаем красную полупрозрачную зону пересечения
    // + иконка предупреждения
}
```

### 5.8 Фильтры и поиск

```csharp
[Parameter] public string SearchQuery { get; set; } = "";
[Parameter] public HashSet<int> VisibleMachineIds { get; set; }
[Parameter] public HashSet<ReservationStatus> VisibleStatuses { get; set; }
[Parameter] public HashSet<string> VisibleGroups { get; set; }
[Parameter] public int MinPriority { get; set; } = 0;

// Фильтрация в реальном времени — подсветка найденных резервов
// Остальные — полупрозрачные
```

---

## 📊 6. Виды Отображения (Views)

### ViewType Enum:
```csharp
public enum TimelineViewType
{
    ResourceTimeline,  // Машины по вертикали, время по горизонтали
    OrderTimeline,     // Заказы по вертикали (один заказ = одна строка с операциями по машинам)
    CompactDay,        // День с 24 часовыми слотами
    CompactWeek,       // Неделя (7 колонок × 24 строки)
    MonthHeatmap,      // Месяц — HeatMap загрузки (% загрузки каждого станка по дням)
    UtilizationChart   // График утилизации (OEE) по времени
}
```

### Быстрое переключение видов:

```csharp
// Кнопки в тулбаре: «День», «Неделя», «Месяц», «Timeline», «Heatmap», «OEE»
// При переключении пересчитываются VisibleStart/VisibleEnd и перерисовывается
```

---

## 🎮 7. Горячие Клавиши и Навигация

| Клавиша | Действие |
|---|---|
| `Ctrl + колёсико` | Зум |
| `Стрелки ← →` | Панорама влево/вправо на 1 день |
| `Стрелки ↑ ↓` | Скролл по машинам |
| `T` | Перейти к «Сегодня» |
| `F` | Fit-to-screen (показать всё) |
| `Ctrl+F` | Фокус на поиск |
| `Delete` | Удалить выбранный резерв |
| `Escape` | Снять выделение |
| `Ctrl+Z` | Undo последнего перемещения |
| `Ctrl+D` | Дублировать выбранный резерв |
| `1-5` | Быстрое переключение видов |
| `G` | Показать/скрыть сетку |

---

## 🧠 8. Умные Функции для Производства

### 8.1 «Что-если» (What-If Scenario)

```csharp
// Виртуальный режим — изменения не сохраняются в базу
// Можно подвигать резервы и посмотреть, как изменится:
// - Загрузка станков (%)
// - Дата завершения заказа
// - Простои
// Кнопка «Apply» — применить изменения
// Кнопка «Discard» — отменить
```

### 8.2 Автоматическое планирование (Suggest Fit)

```csharp
// Для неподвешенного заказа:
// 1. Определяем все возможные станки (по типу операции)
// 2. Находим ближайший свободный слот нужной длительности
// 3. Показываем полупрозрачный «призрак» резерва при перетаскивании
// 4. При отпускании — автоматически вставляется в лучший слот
```

### 8.3 Связи между операциями (Dependency Lines)

```csharp
// Если у заказа несколько операций на разных станках:
// Рисуем стрелку от конца операции N к началу операции N+1
// Стрелка может быть:
// - FS (Finish-to-Start): стандартная
// - SS (Start-to-Start): параллельно
// - FF (Finish-to-Finish): синхронное завершение
// Стрелки рисуются в отдельном слое поверх резервов:
// cubic bezier от правого края блока к левому краю следующего
```

### 8.4 Оповещения и Alerts

```csharp
// Индикаторы на timeline:
// ⚠️ Оранжевый: заказ рискует опоздать (осталось < 20% буфера)
// 🔴 Красный: заказ опаздывает (EndTime в прошлом, статус не Completed)
// 🔵 Синий: машина скоро освободится (< 15 минут)
// 🟡 Жёлтый: простой > 30 минут без причины
// 🟢 Зелёный пульсирующий: станок только что освободился
```

### 8.5 Быстрый просмотр очереди станка

```csharp
// Клик по имени станка в левой панели → раскрывается список:
// - Все резервы на сегодня
// - Загрузка сегодня: X%
// - Следующий свободный слот: HH:mm
// - Ожидающих заказов: N
```

### 8.6 Интеграция с OEE

```csharp
// Для каждого станка показываем мини-график OEE:
// - Availability (доступность): зелёный
// - Performance (производительность): синий
// - Quality (качество): жёлтый
// Прямо в левой панели, маленькая спарклайн-диаграмма
```

---

## 🏗️ 9. Структура Blazor Проекта

```
📁 Components/
  📁 MachineScheduler/
    ├── MachineTimeline.razor          // Основной Canvas-компонент
    ├── MachineTimeline.razor.cs       // Code-behind с логикой
    ├── MachineTimeline.razor.css      // Изолированные стили
    ├── TimelineToolbar.razor          // Панель инструментов (зум, виды, фильтры)
    ├── ReservationTooltip.razor       // Всплывающая подсказка
    ├── ReservationEditor.razor        // Модальное окно редактирования
    ├── ConflictPanel.razor            // Панель конфликтов
    ├── MachineFilterPanel.razor       // Фильтр по станкам/группам
    │
    📁 Services/
    │   ├── TimelineLayoutEngine.cs    // Вычисление позиций
    │   ├── TimelineRenderEngine.cs    // Отрисовка слоёв
    │   ├── TimelineHitTestService.cs  // Hit-testing
    │   ├── ConflictDetectionService.cs// Поиск конфликтов
    │   └── SnapService.cs            // Привязка к сетке
    │
    📁 Models/
    │   ├── MachineResource.cs
    │   ├── MachineReservation.cs
    │   ├── MachineDowntime.cs
    │   ├── ShiftPattern.cs
    │   ├── Enums.cs                   // Все enum'ы
    │   └── TimelineViewState.cs      // DTO для состояния view
    │
    📁 Extensions/
        ├── DateTimeExtensions.cs      // Round, Snap и т.п.
        ├── SkiaExtensions.cs          // DrawTextInRect и т.п.
        └── ColorExtensions.cs         // Генерация палитры
```

---

## ⚡ 10. Производительность — Ключевые Принципы

### 10.1 Кэширование всего, что можно

```csharp
// Layout пересчитывается ТОЛЬКО при изменении:
// - Данных (резервы, станки)
// - Масштаба (_scaleFactor)
// - Скролла (_scrollOffsetX/Y)
// - Размеров контейнера

// Результаты Layout кэшируются в словарях
// При каждом OnPaintSurface используется готовый кэш
```

### 10.2 Viewport Culling

```csharp
// Рисуем ТОЛЬКО то, что видно в текущем viewport
// Для 1000 резервов, но при видимости 20 — рисуем 20

private bool IsVisible(SKRect rect, SKImageInfo viewport)
{
    return rect.Right >= -_labelWidth &&
           rect.Left <= viewport.Width &&
           rect.Bottom >= 0 &&
           rect.Top <= viewport.Height;
}
```

### 10.3 Уровень детализации (LOD)

```csharp
// При мелком масштабе (месяц на экране):
// - Не рисуем текст внутри блоков (не поместится)
// - Не рисуем полосу прогресса
// - Только цветной прямоугольник + tooltip при наведении

// При крупном масштабе (день на экране):
// - Полный текст, прогресс, иконки, тени

private int GetLodLevel()
{
    if (_pixelsPerHour < 30) return 0;  // Год/Месяц — минимум деталей
    if (_pixelsPerHour < 80) return 1;  // Неделя — средний уровень
    return 2;                             // День — максимальная детализация
}
```

### 10.4 Dirty Flag Pattern

```csharp
private bool _isLayoutDirty = true;
private bool _isRenderDirty = true;

public void SetData(IReadOnlyList<MachineReservation> reservations)
{
    _reservations = reservations;
    _isLayoutDirty = true;
    _isRenderDirty = true;
}

// InvalidateSurface вызывается ТОЛЬКО если _isRenderDirty = true
// На OnPaintSurface — сбрасываем флаг
```

### 10.5 SkiaSharp Object Pooling

```csharp
// SKPaint, SKPath — дорогие объекты
// Используем using или пул:
private readonly ObjectPool<SKPaint> _paintPool = new DefaultObjectPool<SKPaint>(
    new SKPaintPooledObjectPolicy());

// Для частых операций:
private static readonly SKPaint _gridPaint = new()
{
    Color = SKColor.Parse("#E8ECF0"),
    StrokeWidth = 1f,
    IsAntialias = false
};
// Не создаём новый на каждый кадр!
```

### 10.6 Двойная буферизация для сложных сцен

```csharp
// Если рисуем много элементов:
// 1. Рисуем статический фон (сетка + нерабочие зоны) в offscreen bitmap
// 2. Кэшируем bitmap
// 3. Каждый кадр: рисуем кэшированный фон + динамические элементы (резервы)
// Это сокращает время отрисовки сетки в 10x
```

---

## 📐 11. Адаптивный Дизайн (Responsive)

```css
/* machine-timeline.css */
.machine-timeline-container {
    width: 100%;
    height: calc(100vh - 64px); /* Минус высота шапки приложения */
    position: relative;
    overflow: hidden;
    background: #FFFFFF;
    border: 1px solid #E0E0E0;
    border-radius: 8px;
}

/* Desktop: метка машин 200px */
@media (min-width: 1200px) {
    .machine-timeline-container { --label-width: 240px; }
}

/* Tablet: метка машин 140px */
@media (max-width: 1199px) {
    .machine-timeline-container { --label-width: 160px; }
}

/* Mobile: метка машин 80px, упрощённый вид */
@media (max-width: 768px) {
    .machine-timeline-container {
        --label-width: 80px;
        height: calc(100vh - 56px);
    }
    /* На мобильном показываем только CompactDay view */
    /* Скрываем tooltip, используем bottom sheet */
}
```

---

## 🔌 12. Интеграция с Бэкендом и MES

### 12.1 Получение данных (API Contract)

```csharp
public interface IMachineSchedulerApi
{
    Task<IReadOnlyList<MachineResource>> GetResourcesAsync(CancellationToken ct);
    Task<IReadOnlyList<MachineReservation>> GetReservationsAsync(
        DateTime from, DateTime to, int[]? machineIds, CancellationToken ct);
    Task<IReadOnlyList<MachineDowntime>> GetDowntimesAsync(
        DateTime from, DateTime to, int[]? machineIds, CancellationToken ct);

    Task<MachineReservation> CreateReservationAsync(MachineReservation reservation, CancellationToken ct);
    Task<MachineReservation> UpdateReservationAsync(MachineReservation reservation, CancellationToken ct);
    Task DeleteReservationAsync(int id, CancellationToken ct);

    // Реал-тайм через SignalR
    IAsyncEnumerable<SchedulerEvent> StreamEventsAsync(CancellationToken ct);
}
```

### 12.2 SignalR Real-time Hub

```csharp
// События с производственной линии:
public abstract record SchedulerEvent;
public record ReservationStarted(int ReservationId, DateTime ActualStart) : SchedulerEvent;
public record ReservationCompleted(int ReservationId, DateTime ActualEnd, int GoodParts, int ScrapParts) : SchedulerEvent;
public record MachineStatusChanged(int MachineId, MachineStatus NewStatus) : SchedulerEvent;
public record DowntimeStarted(int MachineId, DateTime Start, DowntimeReason Reason) : SchedulerEvent;
public record DowntimeEnded(int MachineId, DateTime End) : SchedulerEvent;
public record ReservationMoved(int ReservationId, int NewMachineId, DateTime NewStart, DateTime NewEnd, string MovedBy) : SchedulerEvent;

// Компонент подписывается на Hub:
protected override async Task OnInitializedAsync()
{
    _connection = new HubConnectionBuilder()
        .WithUrl("/hubs/machine-scheduler")
        .Build();

    _connection.On<ReservationStarted>(e => OnReservationStarted(e));
    _connection.On<ReservationCompleted>(e => OnReservationCompleted(e));
    // ...

    await _connection.StartAsync();
}
```

### 12.3 Оптимистичные обновления (Optimistic UI)

```csharp
// При перемещении резерва:
// 1. Сразу обновляем UI (оптимистично)
// 2. Отправляем запрос на сервер
// 3. Если сервер вернул ошибку → откатываем и показываем toast
// 4. Если пришло подтверждение от SignalR — игнорируем (уже отрисовано)
```

---

## 🎯 13. Что Нужно для Производства (Must-Have Checklist)

### Данные, которые ДОЛЖНЫ быть в системе:

- [x] **Список станков** (Machine Resources) с группами, ячейками, статусами
- [x] **График смен** (Shift Pattern) — рабочие/нерабочие часы
- [x] **Резервы/Задания** (Reservations) с привязкой к станку и заказу
- [x] **Номер заказа** (Order Number) — сквозной идентификатор
- [x] **Номер детали** (Part Number) — что производим
- [x] **Время наладки** (Setup Time) — подготовка станка
- [x] **Время цикла** (Cycle Time) — расчётное время обработки
- [x] **Количество деталей** (Parts Count) — объём партии
- [x] **Простои** (Downtime) с причинами
- [x] **Приоритет заказа** (Priority)
- [x] **Статус выполнения** (Planned → InProgress → Completed)
- [x] **Фактическое время** (Actual Start/End) — для сравнения план/факт
- [x] **Клиент** (Customer) — для цвета/группировки
- [x] **Связи между операциями** (Dependencies) заказа

### Интеграции:

- [x] **ERP** (SAP, 1C, Oracle) — заказы, номенклатура, клиенты
- [x] **MES** — фактическое выполнение, простои
- [x] **PLC/SCADA** — статусы станков в реальном времени
- [x] **APS** (Advanced Planning & Scheduling) — алгоритмы оптимизации
- [x] **OEE** — показатели эффективности

---

## 🧪 14. Тестирование и Quality Assurance

### Юнит-тесты:

```csharp
// TimelineLayoutEngineTests.cs
[Test]
public void ComputesCorrectXPosition_ForKnownDateTime()
{
    var engine = new TimelineLayoutEngine(
        visibleStart: new DateTime(2026, 1, 1, 6, 0, 0),
        pixelsPerHour: 80f);

    float x = engine.DateTimeToX(new DateTime(2026, 1, 1, 10, 0, 0));
    Assert.AreEqual(320f, x, 0.01f); // 4 часа × 80px
}

[Test]
public void DetectsOverlappingReservations()
{
    var conflicts = ConflictDetectionService.FindConflicts(new[]
    {
        new MachineReservation { MachineId = 1, Start = 10h, End = 12h },
        new MachineReservation { MachineId = 1, Start = 11h, End = 14h },
        new MachineReservation { MachineId = 2, Start = 10h, End = 12h },
    });

    Assert.AreEqual(1, conflicts.Count);
    Assert.AreEqual(TimeSpan.FromHours(1), conflicts[0].OverlapDuration);
}
```

### BUnit тесты:

```csharp
// Рендерим компонент в тестовом контексте
// Проверяем, что canvas присутствует
// Проверяем реакции на клики и драг
```

### Производительность:

- **1000 резервов** → отрисовка < 16 мс (60fps)
- **10000 резервов** → отрисовка < 50 мс (20fps) с viewport culling
- **Память** → < 50 MB на 10K резервов
- **Инкрементальное обновление** → только изменённый резерв

---

## 🚀 15. Roadmap Разработки (Фазы)

### Фаза 1 — MVP (2-3 недели)
- SKCanvasView базовый рендеринг
- Layout engine (позиции резервов)
- Статическая отрисовка (сетка, резервы, машины)
- Базовый зум колёсиком мыши

### Фаза 2 — Интерактивность (2-3 недели)
- Drag & Drop перемещение
- Resize резервов
- Tooltip
- Hit-testing
- Выделение резервов

### Фаза 3 — Производственные фичи (2-3 недели)
- Простои (Downtime)
- Смены (нерабочие зоны)
- Конфликты (Overbooking detection)
- Приоритеты + цветокодирование
- Линия «Сейчас»

### Фаза 4 — Виды и фильтры (1-2 недели)
- Переключение видов (день/неделя/месяц)
- Фильтры по станкам, статусам
- Heatmap загрузки

### Фаза 5 — Интеграции (2-3 недели)
- SignalR real-time
- API клиент
- Оптимистичные обновления
- Undo/Redo

### Фаза 6 — Оптимизация (1-2 недели)
- Viewport culling
- Object pooling
- LOD (уровень детализации)
- Двойная буферизация для статических слоёв
- Бенчмарки

---

## 💡 16. Дифференциаторы — «Вишенки на торте»

### 16.1 Анимированные переходы

```csharp
// При изменении масштаба — плавная анимация (EaseInOutCubic)
// При перемещении резерва — плавный slide (200ms)
// При появлении нового резерва — fade-in + scale (300ms)
// Используем requestAnimationFrame через JS Interop (один раз)
```

### 16.2 Экспорт в PNG/PDF

```csharp
public async Task<byte[]> ExportToPngAsync()
{
    using var surface = SKSurface.Create(new SKImageInfo(
        (int)_totalContentWidth, (int)_totalContentHeight));
    var canvas = surface.Canvas;

    // Рендерим все слои на offscreen surface
    DrawAllLayers(canvas, new SKImageInfo((int)_totalContentWidth, (int)_totalContentHeight));

    using var image = surface.Snapshot();
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    return data.ToArray();
}
```

### 16.3 Тёмная тема

```csharp
[Parameter] public bool IsDarkMode { get; set; }

// При IsDarkMode: тёмный фон (#1E1E2E), светлая сетка, другие цвета резервов
// Все цвета адаптируются через словарь тем
```

### 16.4 Мини-карта (Overview)

```csharp
// В правом нижнем углу — маленькая мини-карта всей шкалы (200×80px)
// Прямоугольник показывает текущий viewport
// Можно перетаскивать прямоугольник для быстрой навигации
```

### 16.5 Быстрый поиск (Command Palette)

```csharp
// Ctrl+K → открывается command palette
// Печатаем номер заказа / детали → мгновенный скролл к нужному резерву
// Подсветка пульсирующей анимацией
```

---

## 📋 17. Резюме для Агента-Разработчика

### Что нужно сделать по порядку:

1. **Создать Blazor проект** (.NET 8 WASM или Server)
2. **Установить SkiaSharp.Views.Blazor** NuGet пакет
3. **Создать модели** (MachineResource, MachineReservation, etc.)
4. **Создать `TimelineLayoutEngine.cs`** — расчёт позиций
5. **Создать базовый `MachineTimeline.razor`** с SKCanvasView
6. **Реализовать слой отрисовки** — рисование сетки и резервов
7. **Добавить зум и панораму** (matrix transform)
8. **Добавить Drag&Drop** для резервов
9. **Добавить слои**: простои, нерабочие зоны, линия «сейчас»
10. **Добавить тулбар** с переключением видов
11. **Добавить tooltip**
12. **Добавить фильтры**
13. **Добавить редактирование резервов** (модальное окно)
14. **Добавить обнаружение конфликтов**
15. **Подключить SignalR** для real-time
16. **Оптимизировать** (viewport culling, object pooling)

### Ключевые принципы:

- **SkiaSharp рисует всё.** HTML используется только для тултипов и модалок
- **Layout Engine** — отдельный класс, результат кэшируется
- **Viewport culling** — рисуем только видимое
- **Dirty flag** — перерисовка только при изменении данных
- **Слои рендеринга** — строгий порядок отрисовки
- **Object pooling** для SKPaint/SKPath
- **LOD** — меньше деталей при мелком масштабе

---

## 🔗 Полезные Ссылки

- [SkiaSharp GitHub](https://github.com/mono/SkiaSharp)
- [SkiaSharp.Views.Blazor Docs](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.views.blazor)
- [SkiaSharp Documentation](https://learn.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/)
- [Blazor + SkiaSharp Performance](https://www.meziantou.net/optimizing-js-interop-in-a-blazor-webassembly-application.htm)
- [Syncfusion Blazor Gantt (референс)](https://blazor.syncfusion.com/documentation/gantt-chart/getting-started)
- [Производственное планирование — фичи](https://www.jitbase.com/blog/12-essential-manufacturing-scheduling-features)

---

> **Автор:** AI Agent Architecture Guide
> **Дата:** 2026
> **Версия:** 1.0 — Полное руководство для реализации
