# План миграции компонентов SuperUI на новую базовую иерархию

> Базовые классы готовы и отшлифованы: `SgComponentBase`, `SgJsComponentBase`,
> `SgOverlayComponentBase`. Утилиты: `CssBuilder`, `StyleBuilder`,
> `SgJsModuleCache`, `SgDebouncer`, `AsyncThrottler`, `SgRenderMode`,
> `SgIdGenerator`, `SgPersistentState`, `SgMarkerInterfaces`.
>
> Этот документ описывает, как мигрировать оставшиеся компоненты по эшелонам,
> с готовыми кодовыми шаблонами для каждого случая.

---

## Эшелон 0. Уже мигрировано (ничего не делать)

`SgPortal`, `SgStack`, `SgButton`, `SgModal`, `SgTooltip`, `SgDrawer`,
`SgContextMenu`, `SgDockWindow`, `SgECharts`, `SgPopover`, `SgMermaid`,
`SgMonaco`, `SgAnchor`, `SgBarcodeScanner`, `SgMap`, `SgDashboard`,
`SgChart`, `SgDockManager`, `SgGoogleMap`, `SgKonva`, `SgLeaflet`, `SgOcr`,
`SgRecorder`, `SgResizable`, `SgSplitter`.

Проверь у каждого, что:
* В шаблоне `@attributes="AttributesWithoutClassAndStyle"`, а не `@attributes="AdditionalAttributes"`.
* Корневой класс собирается через `Css("sg-...")`, не StringBuilder.
* Авто-ID используется через `ResolvedId`, а не `Guid.NewGuid()`.
* Дублей `[Parameter] public string? CssClass` / `Style` / `Id` / `AdditionalAttributes` в самом компоненте нет — они унаследованы.

---

## Эшелон 1. «Простые контейнеры» — `SgComponentBase` без JS

> ~40 компонентов. Только CSS + ChildContent.

Кандидаты:
`SgRow`, `SgCol`, `SgVerticalGrid`, `SgCard`, `SgDivider`, `SgChip`,
`SgAvatar`, `SgAvatarGroup`, `SgEmpty`, `SgResult`, `SgSkeleton`, `SgFooter`,
`SgHeader`, `SgFormActions`, `SgFormRow`, `SgFormSection`, `SgToolbar`,
`SgStatusPanel`, `SgPropertyPanel`, `SgDescriptions`, `SgTimeline`,
`SgBreadcrumb`, `SgMenu`, `SgMenuItem`, `SgMenuSeparator`, `SgNavGroup`,
`SgNavLink`, `SgNavMenu`, `SgDropdownItem`, `SgRibbon*`, `SgTabs`, `SgTabPanel`,
`SgAccordionItem`, `SgCollapse`, `SgSegmented`, `SgBackTop`, `SgAffix`,
`SgFab`, `SgIconButton`, `SgProgress`, `SgSpinner`.

### Готовый шаблон миграции

**До:**
```razor
@namespace SuperUI.Components

<div class="@_class" style="@Style" @attributes="AdditionalAttributes">
    @ChildContent
</div>

@code {
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public string? Id { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string _class => $"sg-card {CssClass}";
}
```

**После:**
```razor
@namespace SuperUI.Components
@inherits SgComponentBase

<div class="@Css("sg-card").Build()"
     style="@Styles().Build()"
     id="@(Id is null ? null : ResolvedId)"
     @attributes="AttributesWithoutClassAndStyle">
    @ChildContent
</div>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

### Скрипт замены (regex)

В каждом файле:
1. Удалить блоки:
   * `[Parameter] public string? CssClass { get; set; }`
   * `[Parameter] public string? Style { get; set; }`
   * `[Parameter] public string? Id { get; set; }`
   * `[Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }`
2. Добавить `@inherits SgComponentBase` после `@namespace`.
3. Заменить `@attributes="AdditionalAttributes"` → `@attributes="AttributesWithoutClassAndStyle"`.
4. Сборки `$"sg-card {CssClass} {ConditionalClass}"` заменить на `Css("sg-card").AddClass(ConditionalClass).Build()`.

---

## Эшелон 2. «Input-компоненты» — `SgComponentBase` + value binding

Кандидаты: `SgCheckBox`, `SgNumberInput`, `SgNumberEdit`, `SgMaskedInput`,
`SgRadioGroup`, `SgSelect`, `SgComboBox`, `SgComboBoxEx`, `SgAutoComplete`,
`SgSlider`, `SgColorPicker`, `SgFileUpload`, `SgCascader`, `SgTreeSelect`,
`SgSegmented`, `SgEntityPicker`, `SgPagination`, `SgTimePicker`,
`SgDateRangePicker`.

### Готовый шаблон

```razor
@namespace SuperUI.Components
@typeparam TValue
@inherits SgComponentBase

<div class="@Css("sg-input").AddClass("sg-disabled", Disabled).Build()"
     style="@Styles().Build()"
     @attributes="AttributesWithoutClassAndStyle">

    @if (!string.IsNullOrEmpty(Label))
    {
        <label for="@ResolvedId" class="sg-input-label">@Label</label>
    }
    <input id="@ResolvedId"
           type="text"
           class="sg-input-control"
           value="@Value"
           disabled="@Disabled"
           @oninput="OnInputAsync" />
</div>

@code {
    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public bool Disabled { get; set; }

    protected override string IdPrefix => "sg-input";

    private Task OnInputAsync(ChangeEventArgs e)
    {
        var parsed = (TValue?)Convert.ChangeType(e.Value, typeof(TValue));
        return ValueChanged.HasDelegate ? ValueChanged.InvokeAsync(parsed) : Task.CompletedTask;
    }
}
```

> Замечание: для full-fledged input-компонентов с валидацией лучше унаследовать
> `InputBase<TValue>` из Microsoft.AspNetCore.Components.Forms — это даёт
> EditContext-интеграцию. Но дополнительные параметры (CssClass/Style/Id) можно
> вытащить отдельно или сделать `SgInputComponentBase<TValue> : InputBase<TValue>`
> по тому же шаблону.

---

## Эшелон 3. «JS-компоненты» — `SgJsComponentBase`

Кандидаты: `SgRichTextEditor`, `SgQrCode`, `SgKanban`, `SgGantt`, `SgD3Chart`,
`SgDiagram`, `SgDiagramEditor`, `SgOrgChart`, `SgSpreadsheet`,
`SgJsonSchemaForm`, `SgGraphHopper`, `SgYandexMap`, `SgYandexMap21`,
`SgVirtualList`, `SgTreeView`, `SgCalendar`, `SgWarehouse`, `SgDataMatrix`.

### Готовый шаблон

```razor
@namespace SuperUI.Components
@inherits SgJsComponentBase
@using Microsoft.Extensions.Logging

<div @ref="RootRef"
     class="@Css("sg-qr").Build()"
     style="@Styles().AddStyle("width", Size, !string.IsNullOrEmpty(Size)).Build()"
     id="@ResolvedId"
     @attributes="AttributesWithoutClassAndStyle">
</div>

@code {
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public string? Size { get; set; } = "200px";

    protected override string ModulePath => "./_content/SuperUI/superui-qrcode.js";
    protected override string IdPrefix   => "sg-qr";

    // Капчуем последнее переданное значение, чтобы детектить изменения параметра.
    private string? _lastValue;

    protected override async ValueTask OnInteractiveAsync()
    {
        await TryInvokeVoidAsync("init", RootRef, SelfRef, Value);
        _lastValue = Value;
    }

    protected override async Task OnAfterRenderSafeAsync(bool firstRender)
    {
        if (!firstRender && Module is not null && !string.Equals(_lastValue, Value, StringComparison.Ordinal))
        {
            _lastValue = Value;
            await SafeInvokeVoidAsync("update", RootRef, Value);
        }
    }

    protected override async ValueTask OnDisposingAsync()
    {
        await SafeInvokeVoidAsync("dispose", RootRef);
    }

    protected override ValueTask OnJsInitializationFailedAsync(Exception ex)
    {
        Logger.LogError(ex, "SgQrCode: init failed");
        return default; // или показать локализованную ошибку
    }
}
```

**Ключевые моменты:**

* `TryInvokeVoidAsync` для **первой** init — чтобы реальная ошибка JS долетела до
  `OnJsInitializationFailedAsync` и попала в лог.
* `SafeInvokeVoidAsync` для **последующих** вызовов (update / dispose) — это
  нормально, что они тихо отказывают, если circuit умер.
* `Module` доступен только **после** `OnInteractiveAsync` — не вызывай JS в
  `OnInitialized*` или `OnParametersSet*`.
* Если у компонента есть собственная функция «снять JS-listener» — кладите её в
  `OnDisposingAsync`, **НЕ** в Dispose модуля (модуль владеется `SgJsModuleCache`).

---

## Эшелон 4. «Overlay-компоненты» — `SgOverlayComponentBase`

Кандидаты: `SgDropdown`, `SgNotificationBell`, `SgNotificationPanel`,
`SgPortalHost`, `SgConfirmHost`, `SgPropertyGrid` (если выходит как
overlay).

### Готовый шаблон

```razor
@namespace SuperUI.Components
@using SuperUI.Services
@inherits SgOverlayComponentBase

@if (Visible || IsClosing)
{
    <div class="@Css("sg-dropdown").AddClass("sg-closing", IsClosing).Build()"
         style="@Styles().AddStyle("z-index", ZIndexValue.ToString(), ZIndexValue > 0).Build()"
         @ref="RootRef"
         id="@ResolvedId"
         role="menu"
         @attributes="AttributesWithoutClassAndStyle">
        @ChildContent
    </div>
}

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override string ModulePath  => "./_content/SuperUI/superui-dropdown.js";
    protected override int    ZIndexBase  => SgZIndexService.DropdownBase;
    protected override string IdPrefix    => "sg-dropdown";
    protected override int    ClosingAnimationMs => 150;

    protected override async ValueTask OnOpeningAsync()
        => await TryInvokeVoidAsync("attach", RootRef, SelfRef);

    protected override async ValueTask OnClosingAsync()
        => await SafeInvokeVoidAsync("detach");

    [JSInvokable]
    public override Task RequestCloseAsync() => CloseAsync();
}
```

**Что даёт `SgOverlayComponentBase` бесплатно:**

* `[Parameter] public bool Visible` + `EventCallback<bool> VisibleChanged`.
* Z-index Allocate/Release через `SgZIndexService` — гарантированно поверх всего.
* `OnOpeningAsync` / `OnOpenedAsync` / `OnClosingAsync` / `OnClosedAsync` хуки.
* `CloseAsync()` — программное закрытие с анимацией.
* `RequestCloseAsync` `[JSInvokable]` — стандартный канал ESC / backdrop из JS.
* `BringToFront()` — для перевыделения z-index (используется в `SgDockWindow`).

---

## Эшелон 5. Тяжёлые legacy-компоненты — рефакторить отдельно

Кандидаты: `SgDataGrid`, `SgPivotTable`, `SgTable`, `SgTreeView`, `SgGantt`,
`SgKanban`, `SgRichTextEditor`, `SgSpreadsheet`, `SgDiagramEditor`, весь
`SgHttpApiTester*`.

Эти компоненты обычно:
* Имеют свои `OnInitialized*` с тяжёлой логикой.
* Управляют десятками подсостояний (sorting, filtering, virtualization).
* Иногда переопределяют `ShouldRender` для производительности.

### План для тяжёлых

1. **Аддитивная миграция**: только наследование от `SgComponentBase` / `SgJsComponentBase`,
   удаление дублей параметров. **Не трогать** внутреннюю логику.
2. **Css/Styles** — переводить на builder инкрементально, в местах где StringBuilder.
3. **JS-интероп** оставить «как есть», но прогнать через `TryInvokeVoidAsync` для первого вызова — это сразу выявит скрытые баги (которых много в legacy).
4. Любой `setTimeout`-debounce в JS дублировать `SgDebouncer` на .NET-стороне.

### Анти-паттерны в тяжёлых компонентах (проверить)

* `private string _id = Guid.NewGuid().ToString();` — заменить на
  `protected string ResolvedId` (база уже даёт стабильный ID).
* `await Task.Delay(1);` в `OnAfterRenderAsync` для «ждём DOM» — заменить
  на `await Task.Yield();` или вынести в JS.
* `IJSObjectReference? _module = null;` + ручной `import()` — заменить на
  `SgJsModuleCache.GetAsync(JS, ModulePath)`.
* `_isDisposed = true; module?.DisposeAsync()` — НЕ диспозить модуль вручную,
  он принадлежит `SgJsModuleCache`.
* `CancellationTokenSource _cts = new()` — заменить на
  `ComponentLifetime` (база даёт токен).
* `[Inject] ILoggerFactory` дублирующийся — убрать, использовать `Logger`
  из базы.

---

## Эшелон 6. Тесты и санитарные правила

После каждого эшелона добавить юнит-тест на:

1. **CSS-сборка**: компонент с `CssClass="x"` и `class="y"` в splat → итоговый
   корневой `class` содержит обе строки и НЕ имеет дублей.
2. **ID-стабильность**: два рендера одного `this` дают одинаковый `ResolvedId`.
3. **Dispose**: после `DisposeAsync` повторные вызовы любых публичных методов
   не бросают.
4. **JS-failure**: если `Module = null` (имитация JSDisconnected),
   `SafeInvokeVoidAsync` молча возвращает, а `TryInvokeVoidAsync` — бросает.
5. **Hydration**: `ResolvedId` идентичен между prerender (server) и
   handoff (client) при использовании `StableIdFor`.

Простейший тестовый набор (`bunit`):

```csharp
[Fact]
public void Css_Mergers_AddsCssClassAndSplatClass()
{
    using var ctx = new TestContext();
    var cut = ctx.RenderComponent<SgCard>(p => p
        .Add(c => c.CssClass, "extra")
        .AddUnmatched("class", "from-splat"));

    cut.Find("div").GetAttribute("class")
       .Should().Contain("sg-card")
       .And.Contain("extra")
       .And.Contain("from-splat");
}
```

---

## Эшелон 7. Производительность

* **CSS/Style Builder**: для горячих компонентов (списки, виртуализация)
  заменить интерполяцию `$"{existing} {toAdd}"` на `string.Concat` или
  `ValueStringBuilder` (это уже в TODO для CssBuilder).
* **Reflection-кеш**: для всех generic-компонентов выносить `PropertyInfo` в
  `static class TypeAccessors` внутри типа (`SgChart` — пример).
* **Идемпотентный JS init**: каждый `init` в JS должен либо делать noop, если
  уже инициализирован для данного `id`, либо честно destroy предыдущий
  (как сделано в `sg-chart.js → initChart`).
* **ShouldRender**: для тяжёлых компонентов добавить override, который
  игнорирует «холостые» циклы Blazor:
  ```csharp
  protected override bool ShouldRender()
  {
      if (_internalRequest) { _internalRequest = false; return true; }
      var sig = ComputeSignature(); // хэш параметров
      if (sig == _lastSig) return false;
      _lastSig = sig;
      return true;
  }
  ```

---

## Эшелон 8. Cleanup

После завершения всех эшелонов:

1. Удалить `SuperUI.Base.OldComponentBase` (если такой ещё есть).
2. Удалить старые ручные `Guid.NewGuid().ToString()` для ID.
3. Проверить, что **все** razor-файлы имеют `@inherits` (нет «голых» компонентов
   без базы).
4. Прогнать `dotnet build -warnaserror` — если предупреждений нет, значит
   миграция чистая.

---

## Последовательность работы (рекомендация)

1. **Сегодня**: Эшелон 1 (простые контейнеры) — pure mechanical, не ломает поведения.
   ~40 компонентов, ~2-3 часа на копипасту.
2. **Завтра**: Эшелон 2 (input-компоненты) + Эшелон 4 (overlays). 
   Здесь чуть аккуратнее — два-way binding и Z-index.
3. **На неделе**: Эшелон 3 (JS-компоненты). Каждый — отдельный коммит, чтобы
   видно было причину регрессии.
4. **Эшелон 5** делать только когда базы устаканились.
5. **Параллельно**: писать `bunit`-тесты из Эшелона 6 на свежемигрированные
   компоненты.

---

## Чеклист для каждого PR миграции

- [ ] `@inherits` указан.
- [ ] Удалены дублирующие `[Parameter]` для CssClass / Style / Id / AdditionalAttributes.
- [ ] `@attributes="AttributesWithoutClassAndStyle"`, а не `AdditionalAttributes`.
- [ ] Корневой `class` через `Css(...)`, не StringBuilder / interpolation.
- [ ] `style` через `Styles(...)`, не интерполяция.
- [ ] Авто-ID через `ResolvedId`, не `Guid.NewGuid()`.
- [ ] Первый JS-вызов через `TryInvokeVoidAsync` (диагностика ошибок).
- [ ] Последующие — `SafeInvokeVoidAsync`.
- [ ] Если overlay — `OnOpeningAsync`/`OnClosingAsync`, не свои boolean'ы.
- [ ] `Logger` используется, а не `Console.WriteLine` / `Debug.WriteLine`.
- [ ] `Logger.LogError(ex, ...)` в каждой `catch (Exception)`.
- [ ] `dotnet build` зелёный.
- [ ] Если есть демо — ручная проверка по основному сценарию.
