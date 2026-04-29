# SuperUI - Полная документация компонентов

## Содержание

1. [Компоненты данных](#компоненты-данных)
2. [Компоненты ввода](#компоненты-ввода)
3. [Компоненты отображения](#компоненты-отображения)
4. [Компоненты навигации](#компоненты-навигации)
5. [Компоненты макета](#компоненты-макета)
6. [Компоненты обратной связи](#компоненты-обратной-связи)
7. [Компоненты визуализации](#компоненты-визуализации)

---

## Компоненты данных

### SgDataGrid - Таблица данных

Мощный компонент для отображения и управления табличными данными.

#### Основные параметры

```csharp
<SgDataGrid TItem="Employee" 
            Items="@employees"
            ShowSearch="true"
            ShowQuickFilters="true"
            ShowColumnChooser="true"
            ShowStatusBar="true"
            AllowMultiSelect="true"
            AllowEdit="true"
            AllowDelete="true"
            PageSize="50"
            EnablePaging="true" />
```

#### Параметры

| Параметр | Тип | По умолчанию | Описание |
|----------|-----|--------------|---------|
| `Items` | IEnumerable<T> | - | Данные для отображения |
| `ShowSearch` | bool | true | Показывать поле поиска |
| `ShowQuickFilters` | bool | true | Показывать быстрые фильтры |
| `ShowColumnChooser` | bool | true | Показывать выбор колонок |
| `ShowStatusBar` | bool | true | Показывать статус-бар |
| `AllowMultiSelect` | bool | true | Множественный выбор строк |
| `AllowEdit` | bool | true | Редактирование строк |
| `AllowDelete` | bool | true | Удаление строк |
| `PageSize` | int | 50 | Количество строк на странице |
| `EnablePaging` | bool | true | Включить пагинацию |
| `AutoGenerateColumns` | bool | false | Автогенерация колонок |
| `CssClass` | string | - | CSS класс |

#### Колонки

```razor
<SgDataGridColumn TItem="Employee" 
                  ColumnKey="FirstName" 
                  Title="Имя" 
                  Value="@(e => e.FirstName)" 
                  Width="120" 
                  Sortable="true" 
                  Filterable="true" />
```

#### Функции

- ✅ Сортировка по колонкам
- ✅ Фильтрация данных
- ✅ Быстрый поиск
- ✅ Пагинация
- ✅ Множественный выбор
- ✅ Редактирование строк (модальное окно)
- ✅ Удаление строк
- ✅ Выбор видимых колонок
- ✅ Форматирование данных
- ✅ Виртуальная прокрутка для больших наборов

---

### SgPivotTable - Сводная таблица

Компонент для анализа данных с помощью сводных таблиц.

#### Основные параметры

```csharp
<SgPivotTable TItem="SalesData"
              Items="@salesData"
              RowFields="@rowFields"
              ColumnFields="@columnFields"
              ValueFields="@valueFields" />
```

#### Функции

- ✅ Группировка по строкам и колонкам
- ✅ Агрегация данных (Sum, Avg, Count, Min, Max)
- ✅ Перетаскивание полей
- ✅ Экспорт результатов

---

### SgKanban - Доска Канбан

Компонент для управления задачами в стиле Канбан.

#### Основные параметры

```csharp
<SgKanban TItem="Task"
          Items="@tasks"
          GroupBy="@(t => t.Status)"
          AllowDragDrop="true" />
```

#### Функции

- ✅ Группировка задач по статусам
- ✅ Перетаскивание между колонками
- ✅ Добавление новых задач
- ✅ Редактирование задач
- ✅ Удаление задач

---

### SgGantt - Диаграмма Ганта

Компонент для визуализации проектов и временных шкал.

#### Основные параметры

```csharp
<SgGantt TItem="GanttTask"
         Items="@tasks"
         StartDate="@startDate"
         EndDate="@endDate" />
```

#### Функции

- ✅ Визуализация временных шкал
- ✅ Зависимости между задачами
- ✅ Редактирование сроков
- ✅ Прогресс задач

---

## Компоненты ввода

### SgTextBox - Текстовое поле

Базовое текстовое поле для ввода текста.

```razor
<SgTextBox @bind-Value="@text" 
           Placeholder="Введите текст"
           Label="Имя" />
```

#### Параметры

| Параметр | Тип | Описание |
|----------|-----|---------|
| `Value` | string | Значение поля |
| `Placeholder` | string | Подсказка |
| `Label` | string | Метка поля |
| `Disabled` | bool | Отключить поле |
| `ReadOnly` | bool | Только для чтения |

---

### SgNumberEdit - Числовое поле

Поле для ввода чисел с валидацией.

```razor
<SgNumberEdit @bind-Value="@number" 
              Label="Количество"
              Min="0"
              Max="100" />
```

#### Параметры

| Параметр | Тип | Описание |
|----------|-----|---------|
| `Value` | double | Значение |
| `Min` | double? | Минимальное значение |
| `Max` | double? | Максимальное значение |
| `Step` | double | Шаг изменения |
| `Label` | string | Метка поля |

---

### SgDatePicker - Выбор даты

Компонент для выбора одной даты.

```razor
<SgDatePicker @bind-Value="@date" 
              Label="Дата рождения" />
```

#### Функции

- ✅ Выбор даты из календаря
- ✅ Ввод даты вручную
- ✅ Форматирование даты
- ✅ Диапазон доступных дат

---

### SgDateRangePicker - Выбор диапазона дат

Компонент для выбора диапазона дат.

```razor
<SgDateRangePicker @bind-StartDate="@startDate"
                   @bind-EndDate="@endDate"
                   Label="Период" />
```

#### Функции

- ✅ Выбор начальной и конечной даты
- ✅ Предустановленные диапазоны
- ✅ Валидация диапазона

---

### SgSelect - Выпадающий список

Компонент для выбора одного значения из списка.

```razor
<SgSelect @bind-Value="@selectedValue"
          Items="@items"
          Label="Выберите опцию" />
```

#### Параметры

| Параметр | Тип | Описание |
|----------|-----|---------|
| `Value` | T | Выбранное значение |
| `Items` | IEnumerable<T> | Список опций |
| `Label` | string | Метка поля |
| `Placeholder` | string | Подсказка |

---

### SgMultiSelect - Множественный выбор

Компонент для выбора нескольких значений.

```razor
<SgMultiSelect @bind-Value="@selectedValues"
               Items="@items"
               Label="Выберите опции" />
```

#### Функции

- ✅ Выбор нескольких значений
- ✅ Поиск по значениям
- ✅ Удаление выбранных значений
- ✅ Все/Ничего

---

### SgCheckBox - Флажок

Компонент для выбора булева значения.

```razor
<SgCheckBox @bind-Value="@isChecked" 
            Label="Согласен с условиями" />
```

---

### SgSwitch - Переключатель

Компонент для переключения между двумя состояниями.

```razor
<SgSwitch @bind-Value="@isEnabled" 
          Label="Включить" />
```

---

### SgSlider - Ползунок

Компонент для выбора значения из диапазона.

```razor
<SgSlider @bind-Value="@value"
          Min="0"
          Max="100"
          Label="Громкость" />
```

---

### SgColorPicker - Выбор цвета

Компонент для выбора цвета.

```razor
<SgColorPicker @bind-Value="@color" 
               Label="Выберите цвет" />
```

---

### SgAutoComplete - Автодополнение

Компонент для ввода с автодополнением.

```razor
<SgAutoComplete @bind-Value="@value"
                Items="@suggestions"
                Label="Поиск" />
```

---

### SgFileUpload - Загрузка файлов

Компонент для загрузки файлов.

```razor
<SgFileUpload OnFilesSelected="@HandleFilesSelected"
              AcceptedFileTypes=".pdf,.doc,.docx"
              MaxFileSize="5242880" />
```

#### Параметры

| Параметр | Тип | Описание |
|----------|-----|---------|
| `AcceptedFileTypes` | string | Допустимые типы файлов |
| `MaxFileSize` | long | Максимальный размер файла |
| `Multiple` | bool | Множественная загрузка |

---

## Компоненты отображения

### SgCard - Карточка

Компонент для отображения содержимого в карточке.

```razor
<SgCard Title="Заголовок" 
        Subtitle="Подзаголовок">
    Содержимое карточки
</SgCard>
```

#### Параметры

| Параметр | Тип | Описание |
|----------|-----|---------|
| `Title` | string | Заголовок |
| `Subtitle` | string | Подзаголовок |
| `Bordered` | bool | Граница |
| `Hoverable` | bool | Эффект при наведении |

---

### SgBadge - Значок

Компонент для отображения значков и меток.

```razor
<SgBadge Text="Новое" 
         Variant="success" />
```

#### Варианты

- `default` - Серый
- `primary` - Синий
- `success` - Зеленый
- `warning` - Оранжевый
- `danger` - Красный
- `info` - Голубой

---

### SgAlert - Уведомление

Компонент для отображения уведомлений.

```razor
<SgAlert Type="success" 
         Title="Успешно"
         Message="Операция выполнена" />
```

#### Типы

- `success` - Успех
- `error` - Ошибка
- `warning` - Предупреждение
- `info` - Информация

---

### SgAvatar - Аватар

Компонент для отображения аватара пользователя.

```razor
<SgAvatar Name="John Doe" 
          ImageUrl="@imageUrl"
          Size="large" />
```

#### Размеры

- `small` - 32px
- `medium` - 48px
- `large` - 64px

---

### SgStatistic - Статистика

Компонент для отображения статистических данных.

```razor
<SgStatistic Title="Продажи" 
             Value="1,234"
             Prefix="$"
             Trend="up" />
```

---

### SgEmpty - Пустое состояние

Компонент для отображения пустого состояния.

```razor
<SgEmpty Title="Нет данных"
         Description="Попробуйте позже" />
```

---

### SgSkeleton - Скелет загрузки

Компонент для отображения скелета загрузки.

```razor
<SgSkeleton Count="5" 
            Height="20px" />
```

---

### SgTimeline - Временная шкала

Компонент для отображения событий на временной шкале.

```razor
<SgTimeline>
    <TimelineItem Title="Событие 1" 
                  Date="2024-01-01" />
    <TimelineItem Title="Событие 2" 
                  Date="2024-01-02" />
</SgTimeline>
```

---

### SgActivityFeed - Лента активности

Компонент для отображения ленты активности.

```razor
<SgActivityFeed Items="@activities" />
```

---

## Компоненты навигации

### SgMenu - Меню

Компонент для отображения меню.

```razor
<SgMenu>
    <SgMenuItem Text="Главная" Icon="home" />
    <SgMenuItem Text="О нас" Icon="info" />
    <SgMenuSeparator />
    <SgMenuItem Text="Выход" Icon="logout" />
</SgMenu>
```

---

### SgNavMenu - Навигационное меню

Компонент для боковой навигации.

```razor
<SgNavMenu>
    <SgNavLink Text="Главная" Href="/" Icon="home" />
    <SgNavGroup Text="Данные" Icon="database">
        <SgNavLink Text="Таблица" Href="/datagrid" />
        <SgNavLink Text="Графики" Href="/charts" />
    </SgNavGroup>
</SgNavMenu>
```

---

### SgBreadcrumb - Хлебные крошки

Компонент для отображения пути навигации.

```razor
<SgBreadcrumb>
    <BreadcrumbItem Text="Главная" Href="/" />
    <BreadcrumbItem Text="Данные" Href="/data" />
    <BreadcrumbItem Text="Таблица" />
</SgBreadcrumb>
```

---

### SgTabs - Вкладки

Компонент для отображения содержимого во вкладках.

```razor
<SgTabs>
    <SgTabPanel Title="Вкладка 1">
        Содержимое 1
    </SgTabPanel>
    <SgTabPanel Title="Вкладка 2">
        Содержимое 2
    </SgTabPanel>
</SgTabs>
```

---

### SgStepper - Пошаговый процесс

Компонент для отображения пошагового процесса.

```razor
<SgStepper>
    <StepperItem Title="Шаг 1" />
    <StepperItem Title="Шаг 2" />
    <StepperItem Title="Шаг 3" />
</SgStepper>
```

---

### SgPagination - Пагинация

Компонент для навигации по страницам.

```razor
<SgPagination Total="100"
              PageSize="10"
              OnPageChanged="@HandlePageChange" />
```

---

## Компоненты макета

### SgRow и SgCol - Сетка

Компоненты для создания адаптивной сетки.

```razor
<SgRow>
    <SgCol Span="12" Md="6" Lg="4">
        Содержимое
    </SgCol>
</SgRow>
```

#### Параметры

| Параметр | Тип | Описание |
|----------|-----|---------|
| `Span` | int | Ширина на мобильных (1-24) |
| `Sm` | int | Ширина на планшетах |
| `Md` | int | Ширина на десктопах |
| `Lg` | int | Ширина на больших экранах |

---

### SgStack - Стек

Компонент для расположения элементов в стек.

```razor
<SgStack Direction="vertical" Gap="16px">
    <div>Элемент 1</div>
    <div>Элемент 2</div>
</SgStack>
```

---

### SgDivider - Разделитель

Компонент для разделения содержимого.

```razor
<SgDivider Text="или" />
```

---

### SgHeader и SgFooter - Заголовок и подвал

Компоненты для заголовка и подвала страницы.

```razor
<SgHeader>
    Заголовок
</SgHeader>

<SgFooter>
    Подвал
</SgFooter>
```

---

## Компоненты обратной связи

### SgModal - Модальное окно

Компонент для отображения модального окна.

```razor
<SgModal Title="Подтверждение"
         Visible="@isVisible"
         OnOk="@HandleOk"
         OnCancel="@HandleCancel">
    Содержимое модального окна
</SgModal>
```

---

### SgDrawer - Выдвижная панель

Компонент для отображения выдвижной панели.

```razor
<SgDrawer Title="Меню"
          Visible="@isVisible"
          OnClose="@HandleClose">
    Содержимое панели
</SgDrawer>
```

---

### SgPopover - Всплывающее окно

Компонент для отображения всплывающего окна.

```razor
<SgPopover Title="Информация"
           Content="Текст информации">
    <button>Наведите мышь</button>
</SgPopover>
```

---

### SgTooltip - Подсказка

Компонент для отображения подсказки.

```razor
<SgTooltip Title="Это подсказка">
    <button>Наведите мышь</button>
</SgTooltip>
```

---

### SgProgress - Прогресс

Компонент для отображения прогресса.

```razor
<SgProgress Value="65" 
            ShowLabel="true" />
```

---

### SgSpinner - Спиннер загрузки

Компонент для отображения спиннера загрузки.

```razor
<SgSpinner Size="large" />
```

---

### SgToastService - Уведомления

Сервис для отображения уведомлений.

```csharp
await toastService.ShowAsync("Успешно!", "success");
```

---

## Компоненты визуализации

### SgChart - Графики

Полнофункциональный компонент для визуализации данных.

```razor
<SgChart TItem="DataPoint"
         ChartType="SgChartType.Line"
         Data="@data"
         Title="Продажи" />
```

#### Типы графиков

- `Line` - Линейный
- `Bar` - Столбчатый
- `Pie` - Круговой
- `Doughnut` - Круговой с отверстием
- `Scatter` - Точечный
- `Area` - Площадной
- `Heatmap` - Тепловая карта

#### Функции

- ✅ 7 типов графиков
- ✅ LTTB decimation для больших наборов
- ✅ Y-axis zoom
- ✅ Экспорт в PNG/JPG/SVG
- ✅ События клика на точки
- ✅ Адаптивный дизайн

---

### SgCalendar - Календарь

Компонент для отображения календаря.

```razor
<SgCalendar @bind-SelectedDate="@date"
            Events="@events" />
```

---

### SgTreeView - Древовидный список

Компонент для отображения иерархических данных.

```razor
<SgTreeView Items="@treeItems"
            AllowDragDrop="true" />
```

---

## Лучшие практики

### 1. Производительность

- Используйте виртуальную прокрутку для больших списков
- Включайте decimation для графиков с > 10k точек
- Используйте пагинацию вместо загрузки всех данных

### 2. Доступность

- Всегда добавляйте метки (Label) к полям ввода
- Используйте ARIA атрибуты
- Тестируйте с клавиатурой

### 3. Производительность сети

- Загружайте данные постепенно
- Используйте кэширование
- Минимизируйте размер передаваемых данных

### 4. Пользовательский опыт

- Показывайте состояние загрузки
- Предоставляйте обратную связь при действиях
- Используйте интуитивные иконки

---

## Поддержка

Для вопросов и проблем обратитесь к документации или создайте issue в репозитории.
