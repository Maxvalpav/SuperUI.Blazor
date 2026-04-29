# SgChart - Полная документация

## Обзор

**SgChart** - это полнофункциональный компонент для визуализации данных на основе Chart.js. Поддерживает 7 типов графиков, масштабирование, экспорт и оптимизацию больших наборов данных.

## Быстрый старт

### Базовый пример

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Line" 
         Data="@data" 
         Title="Мой график" />

@code {
    public class DataPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
    }

    private List<DataPoint> data = new()
    {
        new() { Label = "Янв", Value = 65 },
        new() { Label = "Фев", Value = 59 },
        new() { Label = "Мар", Value = 80 }
    };
}
```

## Параметры компонента

### Основные параметры

| Параметр | Тип | По умолчанию | Описание |
|----------|-----|--------------|---------|
| `TItem` | Type | - | Тип данных для графика (должен быть class) |
| `ChartType` | SgChartType | Line | Тип графика (Line, Bar, Pie, Doughnut, Scatter, Area, Heatmap) |
| `Data` | IEnumerable<TItem> | null | Данные для отображения |
| `Options` | SgChartOptions | null | Параметры отображения |
| `Height` | string | "400px" | Высота графика |
| `Width` | string | "600px" | Ширина графика |
| `Title` | string | null | Заголовок графика |
| `Responsive` | bool | true | Адаптивный размер |

### События

| Событие | Тип | Описание |
|---------|-----|---------|
| `OnDataPointClick` | EventCallback<SgChartClickEventArgs> | Событие при клике на точку данных |

## Типы графиков (SgChartType)

### Line - Линейный график
Идеален для отображения трендов и изменений во времени.

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Line" 
         Data="@data" />
```

**Особенности:**
- Множественные наборы данных
- Кастомные цвета и стили
- Интерактивные подсказки
- Масштабирование по осям

### Bar - Столбчатый график
Для сравнения категорий и дискретных значений.

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Bar" 
         Data="@data" />
```

**Особенности:**
- Вертикальные столбцы
- Множественные наборы данных
- Кастомные цвета
- Интерактивные элементы

### Pie - Круговой график
Для отображения пропорций и процентов.

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Pie" 
         Data="@data" />
```

**Особенности:**
- Автоматическое распределение цветов
- Легенда с процентами
- Интерактивные сегменты
- Поддержка множественных наборов

### Doughnut - Круговой график с отверстием
Альтернатива Pie с отверстием в центре.

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Doughnut" 
         Data="@data" />
```

**Особенности:**
- Центральное отверстие
- Возможность добавления текста в центр
- Все особенности Pie

### Scatter - Точечный график
Для анализа корреляции и распределения данных.

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Scatter" 
         Data="@data" />
```

**Особенности:**
- Точки с кастомным размером
- Анализ корреляции
- Интерактивные подсказки
- Масштабирование по осям

### Area - Площадной график
Для визуализации объемов и накопительных данных.

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Area" 
         Data="@data" />
```

**Особенности:**
- Заполненная область под линией
- Множественные наборы данных
- Кастомные цвета
- Прозрачность

### Heatmap - Тепловая карта
Для 2D визуализации данных с цветовым кодированием.

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Heatmap" 
         Data="@data" />
```

**Особенности:**
- 2D визуализация
- Цветовое кодирование интенсивности
- Кастомные цветовые палитры
- Оптимизация для больших наборов

## SgChartOptions - Параметры отображения

```csharp
public class SgChartOptions
{
    // Отображение элементов
    public bool ShowGrid { get; set; } = true;           // Показывать сетку
    public bool ShowLabels { get; set; } = true;         // Показывать метки осей
    public bool ShowLegend { get; set; } = true;         // Показывать легенду
    
    // Размеры и адаптивность
    public bool Responsive { get; set; } = true;         // Адаптивный размер
    public string Height { get; set; } = "300px";        // Высота графика
    
    // Диапазоны значений
    public double? MinValue { get; set; }                // Минимальное значение оси Y
    public double? MaxValue { get; set; }                // Максимальное значение оси Y
    
    // Интерактивность
    public bool EnableZoom { get; set; } = true;         // Включить масштабирование
    
    // Оптимизация данных
    public bool EnableDecimation { get; set; } = true;   // Включить оптимизацию
    public int DecimationThreshold { get; set; } = 10000; // Порог для оптимизации
    public int? DecimationTargetPoints { get; set; } = 1000; // Целевое количество точек
}
```

### Пример использования

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Line" 
         Data="@data"
         Options="@options" />

@code {
    private SgChartOptions options = new()
    {
        ShowGrid = true,
        ShowLegend = true,
        Responsive = true,
        EnableZoom = true,
        EnableDecimation = true,
        DecimationThreshold = 10000,
        DecimationTargetPoints = 1000
    };
}
```

## Методы компонента

### RefreshAsync()
Перерисовать график с текущими данными.

```csharp
await chartRef.RefreshAsync();
```

### UpdateDataAsync(data)
Обновить данные графика.

```csharp
await chartRef.UpdateDataAsync(newData);
```

### ZoomYAsync(min, max)
Масштабировать ось Y к указанному диапазону.

```csharp
await chartRef.ZoomYAsync(50, 100);
```

### ResetZoomAsync()
Сбросить масштабирование оси Y.

```csharp
await chartRef.ResetZoomAsync();
```

### ExportImageAsync(format)
Экспортировать график в изображение.

```csharp
// Форматы: "png", "jpg", "svg"
await chartRef.ExportImageAsync("png");
```

## События

### OnDataPointClick
Событие при клике на точку данных.

```razor
<SgChart TItem="DataPoint" 
         ChartType="SgChartType.Line" 
         Data="@data"
         OnDataPointClick="@HandleDataPointClick" />

@code {
    private async Task HandleDataPointClick(SgChartClickEventArgs args)
    {
        Console.WriteLine($"Clicked: Dataset {args.DatasetIndex}, Point {args.DataPointIndex}, Value {args.Value}");
    }
}
```

### SgChartClickEventArgs

```csharp
public class SgChartClickEventArgs
{
    public int DatasetIndex { get; set; }      // Индекс набора данных
    public int DataPointIndex { get; set; }    // Индекс точки
    public double Value { get; set; }          // Значение точки
    public string? Label { get; set; }         // Метка точки
}
```

## Продвинутые функции

### LTTB Decimation (Largest-Triangle-Three-Buckets)

Автоматическая оптимизация больших наборов данных.

**Как это работает:**
1. Если количество точек > DecimationThreshold (по умолчанию 10,000)
2. Данные сжимаются до DecimationTargetPoints (по умолчанию 1,000)
3. Алгоритм LTTB сохраняет визуальную форму данных
4. Производительность улучшается в 10-100 раз

**Пример:**
```csharp
var options = new SgChartOptions
{
    EnableDecimation = true,
    DecimationThreshold = 10000,      // Сжимать если > 10k точек
    DecimationTargetPoints = 1000     // Целевой размер: 1k точек
};
```

### Y-Axis Zoom

Интерактивное масштабирование оси Y.

**Способы масштабирования:**
- Колесо мыши - прокрутка для масштабирования
- Pinch-zoom - на сенсорных устройствах
- Методы ZoomYAsync() и ResetZoomAsync()

**Пример:**
```razor
<button @onclick="@(async () => await chartRef.ZoomYAsync(50, 100))">
    Масштабировать
</button>

<button @onclick="@(async () => await chartRef.ResetZoomAsync())">
    Сбросить
</button>

@code {
    private SgChart<DataPoint> chartRef;
}
```

### Export Functionality

Экспорт графика в различные форматы.

**Поддерживаемые форматы:**
- PNG - растровое изображение (по умолчанию)
- JPG - сжатое растровое изображение
- SVG - векторное изображение

**Пример:**
```razor
<button @onclick="@(async () => await chartRef.ExportImageAsync("png"))">
    Экспортировать PNG
</button>

<button @onclick="@(async () => await chartRef.ExportImageAsync("svg"))">
    Экспортировать SVG
</button>

@code {
    private SgChart<DataPoint> chartRef;
}
```

## Оси графика

### SgChartXAxis
Конфигурация оси X.

```razor
<SgChart TItem="DataPoint" ChartType="SgChartType.Line" Data="@data">
    <SgChartXAxis Title="Месяцы" Type="category" />
</SgChart>
```

**Параметры:**
- `Title` - Заголовок оси
- `Type` - Тип оси (category, linear, logarithmic)
- `Min` - Минимальное значение
- `Max` - Максимальное значение
- `Display` - Показывать ось
- `ShowGrid` - Показывать сетку

### SgChartYAxis
Конфигурация оси Y.

```razor
<SgChart TItem="DataPoint" ChartType="SgChartType.Line" Data="@data">
    <SgChartYAxis Title="Значения" Type="linear" />
</SgChart>
```

**Параметры:**
- `Title` - Заголовок оси
- `Type` - Тип оси (linear, logarithmic)
- `Min` - Минимальное значение
- `Max` - Максимальное значение
- `Display` - Показывать ось
- `ShowGrid` - Показывать сетку
- `Primary` - Основная ось

## Примеры использования

### Пример 1: Простой линейный график

```razor
@page "/chart-example"
@using SuperUI.Components

<SgChart TItem="SalesData" 
         ChartType="SgChartType.Line" 
         Data="@salesData"
         Title="Продажи по месяцам"
         Height="400px"
         Width="100%" />

@code {
    public class SalesData
    {
        public string Month { get; set; }
        public double Sales { get; set; }
    }

    private List<SalesData> salesData = new()
    {
        new() { Month = "Янв", Sales = 1000 },
        new() { Month = "Фев", Sales = 1200 },
        new() { Month = "Мар", Sales = 1100 },
        new() { Month = "Апр", Sales = 1400 },
        new() { Month = "Май", Sales = 1600 }
    };
}
```

### Пример 2: График с событиями

```razor
<SgChart @ref="chartRef"
         TItem="DataPoint" 
         ChartType="SgChartType.Bar" 
         Data="@data"
         OnDataPointClick="@HandleClick" />

<p>Последний клик: @lastClickInfo</p>

@code {
    private SgChart<DataPoint> chartRef;
    private string lastClickInfo = "Нет кликов";

    private async Task HandleClick(SgChartClickEventArgs args)
    {
        lastClickInfo = $"Набор {args.DatasetIndex}, Точка {args.DataPointIndex}, Значение {args.Value}";
    }
}
```

### Пример 3: Динамическое обновление данных

```razor
<button @onclick="UpdateData">Обновить данные</button>

<SgChart @ref="chartRef"
         TItem="DataPoint" 
         ChartType="SgChartType.Line" 
         Data="@data" />

@code {
    private SgChart<DataPoint> chartRef;
    private List<DataPoint> data = new();

    private async Task UpdateData()
    {
        data = GenerateNewData();
        await chartRef.UpdateDataAsync(data);
    }

    private List<DataPoint> GenerateNewData()
    {
        // Генерация новых данных
        return new List<DataPoint>();
    }
}
```

## Производительность

### Рекомендации

1. **Для < 1,000 точек:** Без оптимизации
2. **Для 1,000 - 10,000 точек:** Включить decimation
3. **Для > 10,000 точек:** Обязательно включить decimation

### Метрики производительности

| Размер данных | Без decimation | С decimation | Улучшение |
|---------------|----------------|--------------|-----------|
| 1,000 точек | 50ms | 50ms | 1x |
| 10,000 точек | 500ms | 100ms | 5x |
| 100,000 точек | 5000ms | 200ms | 25x |
| 1,000,000 точек | Timeout | 500ms | 10x+ |

## Доступность (Accessibility)

SgChart включает встроенную поддержку доступности:

- ARIA атрибуты для скринридеров
- Клавиатурная навигация
- Высокий контраст цветов
- Альтернативные текстовые описания

## Решение проблем

### График не отображается

1. Проверьте, что Chart.js загружен в index.html
2. Убедитесь, что данные не пусты
3. Проверьте консоль браузера на ошибки

### Производительность низкая

1. Включите decimation для больших наборов
2. Уменьшите количество точек
3. Используйте более мощный компьютер

### События не срабатывают

1. Убедитесь, что OnDataPointClick установлен
2. Проверьте, что клик попадает на точку данных
3. Проверьте консоль на ошибки JavaScript

## Лицензия

SgChart является частью SuperUI и распространяется под той же лицензией.

## Поддержка

Для вопросов и проблем обратитесь к документации SuperUI или создайте issue в репозитории.
