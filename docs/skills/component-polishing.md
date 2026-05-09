# SUI Component Polishing — Patterns & Skills

Шпаргалка по тому, как мы «полируем» компоненты SuperUI до библиотечного уровня. Применяется к любому `SgXxx`, на примере `SgRow` и `SgCard`.

> Цель полировки — не «добавить фич», а сделать API целостным, предсказуемым,
> доступным и согласованным со всей библиотекой.

---

## 1. API-паттерны параметров

### 1.1. Variant + Size + Status — три оси оформления

| Ось        | Тип / значения                                                      | Назначение |
|------------|---------------------------------------------------------------------|------------|
| `Variant`  | `Default \| Elevated \| Outlined \| Filled \| Ghost`                | Стиль поверхности |
| `Size`     | `SgSize` (`Sm \| Md \| Lg \| Xl`)                                   | Плотность / шрифт / отступы |
| `Status`   | `None \| Info \| Success \| Warning \| Danger \| Muted`             | Семантический акцент |

**Правило:** оси не должны конфликтовать. `Size` управляет ТОЛЬКО размером,
`Variant` ТОЛЬКО цветом/тенью/границей, `Status` ТОЛЬКО семантическим акцентом.
Никаких «Variant.LargeSuccess». Если приходится комбинировать —
значит, ось названа неправильно.

### 1.2. Состояния — bool с двусторонней привязкой

```razor
[Parameter] public bool Selected { get; set; }
[Parameter] public EventCallback<bool> SelectedChanged { get; set; }
```

Любое поведенческое состояние, которое может изменить пользователь, должно
поддерживать `@bind-` форму. Поэтому всегда добавляйте парный `XxxChanged`.

### 1.3. Слоты как `RenderFragment?` — не строки

```razor
[Parameter] public RenderFragment? HeaderContent  { get; set; }
[Parameter] public RenderFragment? ActionContent  { get; set; }
[Parameter] public RenderFragment? CoverContent   { get; set; }
[Parameter] public RenderFragment? FooterContent  { get; set; }
[Parameter] public RenderFragment? ChildContent   { get; set; }
```

**Правило:** если слот может содержать произвольный UI (иконку + текст +
кнопку) — это `RenderFragment?`, а не `string`. Простые строковые поля
(`Title`, `Subtitle`) дают удобный shortcut, но `HeaderContent` имеет приоритет.

### 1.4. Capture-unmatched всегда

```razor
[Parameter(CaptureUnmatchedValues = true)]
public Dictionary<string, object>? AdditionalAttributes { get; set; }
```

Чтобы консьюмер мог прокинуть `id`, `data-*`, `aria-*`, `role`, `title`
без обходных путей.

### 1.5. CssClass + Style как escape-hatch

```razor
[Parameter] public string? CssClass { get; set; }
[Parameter] public string? Style    { get; set; }
```

Они **дописываются** к computed-значениям, а не заменяют их.

### 1.6. Backwards-compat при добавлении новых параметров

Если расширяете API (например, `NoWrap` → `Wrap` enum), не удаляйте старое:

```csharp
[Parameter] public SgFlexWrap? Wrap { get; set; }   // новый, приоритетный
[Parameter] public bool NoWrap { get; set; }        // legacy, жив
```

Внутри: `if (Wrap is { } w) ... else NoWrap ? "nowrap" : "wrap"`.

---

## 2. Доступность (a11y)

### 2.1. Кликабельный non-button → role + tabindex + key

Если карточка/строка кликабельна (`Selectable`, `OnClick`):

```razor
<div role="button"
     tabindex="0"
     aria-pressed="@(Selected ? "true" : "false")"
     aria-disabled="@(Disabled ? "true" : null)"
     @onkeydown="HandleKeyDownAsync">
```

Поддержать клавиши `Enter` и `Space`:

```csharp
if (e.Key is "Enter" or " " or "Spacebar") { /* toggle + invoke */ }
```

### 2.2. Disabled — pointer-events + aria-disabled

```css
.sgc-card.sgc-card-disabled { opacity: .6; cursor: not-allowed; pointer-events: none; }
```

### 2.3. Focus-visible

```css
.sgc-card.sgc-card-selectable:focus-visible {
    outline: 2px solid var(--sui-accent);
    outline-offset: 2px;
}
```

Используем `:focus-visible`, а не `:focus`, — клавиатурный фокус подсвечен,
мышиный — нет.

### 2.4. stopPropagation для вложенных кнопок

Если в шапке `Selectable`-карточки есть `ActionContent`, оборачиваем:

```razor
<div class="sgc-card-actions" @onclick:stopPropagation="true">
    @ActionContent
</div>
```

Иначе клик по «×» внутри карточки внезапно её ещё и выбирает.

---

## 3. Стилевые паттерны

### 3.1. Только CSS-переменные `--sui-*` — никаких хардкод-цветов

```css
.sgc-card-icon {
    background: var(--sui-bg-secondary);
    color: var(--sui-accent, #1890ff);
}
```

Резервное значение в `var(--x, fallback)` ставим только если переменная
может отсутствовать в старых темах.

### 3.2. Логические свойства для RTL

```css
.sgc-card[class*="sgc-card-status-"]::before {
    inset-inline-start: 0;          /* не left */
    border-start-start-radius: 8px; /* не border-top-left-radius */
}
```

### 3.3. Скоупная CSS-файл рядом с компонентом

`SgCard.razor.css` — Blazor-scoped, попадает только на этот компонент. Глобальные
правила (`.sgc-card`-базовые) — в `wwwroot/superui-components.css`.
**Правило:** всё новое идёт в scoped-файл, если не нужно повлиять глобально.

### 3.4. `:deep()` для дочернего HTML

```css
.sgc-card-icon :deep(svg) { width: 18px; height: 18px; }
```

Иконку рисует консьюмер, но размер мы навязываем — через `:deep()`,
поскольку scoped-CSS иначе не достанет.

### 3.5. Два уровня плотности — Size и Compact

`Size` — глобальная шкала (Sm/Md/Lg/Xl). `Compact` — поджимает Md ещё чуть-чуть
для дашбордов. Не путать: `Compact` ≠ `Size.Sm` (другой шрифт).

---

## 4. Семантика и SEO — параметр `Tag`

Контейнерные компоненты (`SgRow`, `SgStack`, `SgCol`) должны уметь рендериться
как нужный HTML-элемент:

```razor
<SgRow Tag="section">…</SgRow>
<SgRow Tag="ul" Direction="SgFlexDirection.Column">…</SgRow>
```

Реализация — `if`/`else` по `Tag` в `.razor`, дефолт — `div`. Для `ul` —
сбрасываем `list-style/padding/margin` в `style`.

---

## 5. Лоадинг и пустые состояния

### 5.1. Loading заменяет тело скелетоном

```razor
@if (Loading) {
    <SgSkeleton Width="100%" Height="14px" />
    <SgSkeleton Width="60%"  Height="14px" />
} else {
    @ChildContent
}
```

Не показываем «спиннер поверх контента» — пользователь не знает, успело
старое отрендериться или нет.

### 5.2. Пустое состояние — отдельный слот, не magic

Если у компонента есть данные → empty UI: добавляйте `EmptyContent` слот,
но не примешивайте его в `ChildContent`. Так консьюмер сам решает.

---

## 6. События

| Паттерн                       | Тип                                   | Когда              |
|-------------------------------|----------------------------------------|--------------------|
| `OnClick`                     | `EventCallback<MouseEventArgs>`        | Любой клик-handler |
| `OnXxxChanged` + `Xxx`        | `EventCallback<T>` + `[Parameter] T`   | Двусторонний bind  |
| `OnBeforeXxx` + ret `bool`    | `Func<…, Task<bool>>`                  | Отмена действия    |

Не возвращаем `void` из callback'ов — всегда `Task` или `EventCallback<T>`.

---

## 7. Демо-страницы — единый шаблон

```razor
@page "/xxx-demo"
@using SuperUI.Components

<PageTitle>Заголовок - SUI</PageTitle>

<SgCard Title="…" Subtitle="…">

    <div style="margin-top: 32px;">
        <h2>Имя секции</h2>
        <p>Что показываем и зачем.</p>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-top: 16px;">
            <SgCard Title="Примеры использования"> … </SgCard>
            <SgCard Title="Свойства компонента">
                <PropertyTable Items="_props" />
            </SgCard>
        </div>
    </div>

</SgCard>
```

**Чек-лист для демо:**
- [ ] `<PageTitle>` с суффиксом `- SUI`
- [ ] Корневой `SgCard` с `Title` + `Subtitle`
- [ ] Секция = `<h2>` + `<p>` + grid `1fr 1fr` (примеры | свойства)
- [ ] `PropertyTable` со всеми параметрами и `BadgeVariant`
- [ ] Запись в `AppNav.razor` в подходящей `SgNavGroup`

---

## 8. Чек-лист полировки компонента

Перед тем как сказать «готово», прогоняем по чек-листу:

### API
- [ ] Title / Subtitle / Icon — короткие shortcut-параметры
- [ ] `HeaderContent` / `FooterContent` / `ChildContent` — слоты
- [ ] `Variant`, `Size`, `Status` — где осмысленно
- [ ] `Disabled` — отключает поведение и обновляет ARIA
- [ ] `Loading` — заменяет тело скелетоном
- [ ] `OnClick` (если кликабелен) + двусторонние bind'ы
- [ ] `CssClass`, `Style`, `AdditionalAttributes` — escape-hatches
- [ ] XML-док-комментарии на КАЖДОМ `[Parameter]`

### Доступность
- [ ] `role` + `tabindex` + key-handler на кликабельных не-кнопках
- [ ] `aria-pressed` / `aria-expanded` / `aria-disabled`
- [ ] `:focus-visible` стиль
- [ ] Контраст текста ≥ 4.5:1 в обеих темах

### Стиль
- [ ] Только `--sui-*` переменные
- [ ] Логические свойства (`inset-inline-start`)
- [ ] Scoped CSS — для специфики компонента
- [ ] Поведение в светлой и тёмной теме
- [ ] Поведение на `prefers-reduced-motion: reduce` (если есть transitions)

### API back-compat
- [ ] Старые параметры живы и работают
- [ ] Новые имеют приоритет над старыми
- [ ] Дефолты не меняют визуал в существующих демках

### Демо
- [ ] Создан/обновлён `XxxDemo.razor` по шаблону
- [ ] Покрыты ВСЕ варианты, размеры, статусы, состояния
- [ ] Показан минимум 1 «паттерн использования» (composition)
- [ ] Зарегистрирован в `AppNav.razor`
- [ ] `dotnet build SuperUI.Demo` без ошибок

### Документация
- [ ] Если появилось общее правило — добавить сюда

---

## 9. Mini-skills (быстрые техники)

### `Skill: extract-status-stripe`
Когда у компонента есть «семантические оттенки» (info/success/warn/danger),
не плодим `Variant`-ы — выносим в отдельную ось `Status` и рисуем
3-px полоску слева через `::before`. Один параметр работает с любым
`Variant`.

### `Skill: collapsible-via-bind`
Сворачиваемость = `Collapsible` (показ кнопки) + `@bind-Collapsed`. НЕ
делайте `DefaultCollapsed` — пусть управление полностью внешнее, тогда
состояние можно сохранить в localStorage / URL / store.

### `Skill: selectable-card`
Чтобы карточка стала «радио»-плиткой:
1. `Selectable=true` → role=button, tabindex, keys.
2. `@bind-Selected` → выбор без флага в коде.
3. Подсветка через `box-shadow: 0 0 0 1px accent` — чище чем `border-color`,
   не «прыгает» layout.
4. `ActionContent` оборачиваем `@onclick:stopPropagation`.

### `Skill: cover-slot`
Для медиа-карточек (картинка/видео сверху) добавляем `CoverContent`,
ставим `OverflowHidden=true` и в scoped-CSS:
```css
.sgc-card-cover > img, .sgc-card-cover > video { width:100%; height:auto; display:block; }
```

### `Skill: legacy-bool-to-enum`
Если параметр-bool оказался узок (`NoWrap`), добавляем enum
(`SgFlexWrap`) как **nullable**, и приоритет — у nullable. Старый bool
живёт, демки не падают.

```csharp
[Parameter] public SgFlexWrap? Wrap   { get; set; }
[Parameter] public bool        NoWrap { get; set; }
private string WrapCss => Wrap switch
{
    SgFlexWrap.NoWrap      => "nowrap",
    SgFlexWrap.WrapReverse => "wrap-reverse",
    SgFlexWrap.Wrap        => "wrap",
    _ => NoWrap ? "nowrap" : "wrap"
};
```

### `Skill: tagged-container`
Универсальные контейнеры (`SgRow`, `SgStack`) принимают `Tag` —
рендерят правильный семантический элемент (`section/header/nav/ul`).
В `.razor` switch по строке. Для `ul` сбрасываем list-style.

### `Skill: keyboard-toggle`
Для toggle-поведения: `Enter`, `Space`, `Spacebar` — все три ключа
надо ловить (старые браузеры используют `Spacebar`).

```csharp
if (e.Key is "Enter" or " " or "Spacebar") { … }
```

### `Skill: slot-priority-order`
В `.razor`:
```razor
@if (HeaderContent is not null) { @HeaderContent }
else { /* fallback из Title/Subtitle/Icon */ }
```
Слот всегда **выигрывает** у простых параметров. Никогда не объединяем
их (это конфликт интересов).

---

## 10. Когда сказать «готово»

`dotnet build` — зелёный, демка показывает все варианты, в `AppNav`
есть ссылка, `:focus-visible` подсвечен, переключение темы не ломает
цвета, и чек-лист выше — весь зачёркнут. Тогда — готово.
