# SuperUI Polish Plan — Round 2 (Row/Col/Stack/Space + Library-wide)

## Цель

Пройтись по библиотеке, исправить и улучшить компоненты `SgRow`, `SgCol`,
`SgStack`, `SgSpace` (семейство layout-примитивов), дополнить CSS
(отсутствуют `sg-col-xs-*`, `sg-col-*-hidden`, hover/clickable) и
переполировать `RowDemo.razor` так, чтобы он демонстрировал всю
мощь компонентов с максимальным использованием самой библиотеки.

Конкурентный анализ:
- **MudBlazor** `MudGrid/MudItem` — простой Gutter, xs/sm/md/lg/xl
- **Radzen** `RadzenRow/RadzenColumn` — Gap (string), Justify, Align
- **Telerik** `TelerikGrid/Column` — это уже про данные, для layout
  используют обычные div
- **DevExpress** `DxGrid` — данные; для layout — `Row`/`Col` через
  Bootstrap 5 utility classes
- **Element Plus** `el-row/el-col` — `gutter` (число, не string), 24-col,
  `xs/sm/md/lg/xl`, `tag`, `justify`, `align`
- **Ant Design** `Row/Col` — `gutter={[16,16]}` (row + col), flex, span, order
- **Material UI** `Grid/Grid2` — `spacing`, `xs..xl`, `justifyContent`
- **Bootstrap 5** `row/col` — gutter через CSS classes (g-0..g-5), 12-col

Что у нас есть лучше всех:
- ✅ 24-колонка
- ✅ `Push`/`Pull` (уникально)
- ✅ `Auto`/`Fill`/`Flex` (богаче MudBlazor)
- ✅ `Offset` (как у AntD)
- ✅ `SgRow.Tag` (semantic HTML — есть у AntD/ElementPlus)
- ✅ `SgSize` enum-токены для gap (есть у ElementPlus через числа)

Чего не хватает (что добавим):
- ❌ `Density` preset (Compact/Default/Comfortable) — единая ручка
  плотности (как у Carbon/Material Density)
- ❌ `SgRow.ItemWidth` — фиксированный minmax-режим (auto-fit grid)
- ❌ `SgRow.Align`/`Justify` per-breakpoint (responsive)
- ❌ `SgCol.Order{Xs,Sm,Md,Lg,Xl}` — responsive order (есть у AntD)
- ❌ `SgCol.Align{Xs,...}` — responsive align-self
- ❌ `SgCol.Offset{Xs,...}` уже есть
- ❌ `SgRow.Tag` — перевести с string на enum `SgRowTag`
- ❌ `SgRow.Align`/`Justify` — улучшить API
- ❌ `SgSpace` — миграция с magic-string `"small"/"middle"/"large"`
  на SgSize enum, починить Split, добавить Density
- ❌ CSS — добавить `sg-col-xs-{1..12}`, `sg-col-{xs..xl}-hidden`,
  `sg-row-hoverable`/`sg-row-clickable`/`sg-col-hoverable`/`sg-col-clickable`

Сборка: `dotnet build SuperUI/SuperUI.csproj -c Release`
Демо: `dotnet build SuperUI.Demo/SuperUI.Demo.csproj -c Release`
Каждый шаг — отдельный коммит.

---

## Step 0 — Оформить план (этот файл)

Просто коммит с обновлённым планом.

---

## Step 1 — SgRow: `SgRowTag` enum, `Density`, `FullHeight`, `ItemWidth` (P1)

`SuperUI/Components/Layout/Row/SgRow.razor.cs`

1. **Tag как enum.** `string Tag` → `SgRowTag Tag` (enum:
   `Div, Section, Header, Footer, Nav, Main, Ul, Article, Aside`).
   Старое `string` помечаем `[Obsolete]`, конвертируем в enum (back-compat).
2. **`Density`** (`SgDensity` enum уже есть: `Compact, Default, Comfortable`).
   Влияет на:
   - `--sg-row-density-gap-scale` (CSS-переменная) = `0.5 / 1.0 / 1.5`
   - Применяется ко всем гаттерам.
3. **`FullHeight`** (по аналогии с `FullWidth`) → `height: 100%`.
4. **`ItemWidth`** (`string?`, e.g. `"240px"` или `"minmax(240px, 1fr)"`) —
   включает **auto-fit режим**: `flex-wrap: wrap`, колонки получают
   `flex: 0 1 <width>; min-width: <width>` → `SgRow` сам пересчитывает
   кол-во колонок от ширины контейнера (как CSS Grid auto-fit).
5. **`ResponsiveDirection`** / **`ResponsiveWrap`** — dictionary-based
   `IReadOnlyDictionary<SgBreakpoint, SgFlexDirection?>` (опционально).
   Чтобы не усложнять — добавим простые `DirectionXs/DirectionSm/.../DirectionXl`
   с тем же приоритетом, что и `Direction` (последний выигрывает).
6. **Привести ComputedClass к CssBuilder** (консистентность с
   SgStack/SgSpace после рефакторинга).
7. **`@onclick` → `@onclick="OnClick.HasDelegate ? HandleClick : null"`**
   (не вешаем слушатель, если делегата нет — копеечная, но приятная
   оптимизация).

---

## Step 2 — SgCol: Responsive Order/Align, рефактор ComputedStyle, AutoFit (P1)

`SuperUI/Components/Layout/Col/SgCol.razor.cs`

1. **Responsive Order** — добавить `OrderXs/Sm/Md/Lg/Xl` (int).
   В CSS (см. Step 5) добавим `.sg-col-order-xs-N` и т.д.
2. **Responsive AlignSelf** — `AlignSelfXs/.../Xl` (SgAlignItems?).
3. **Починить `ComputedStyle`** — заменить конволюцию
   `cols != 12 || (span >= 1 && span <= cols && Span is null is false)`
   на простую `if (span >= 1 && span <= cols)`.
4. **`AutoFit`** (bool) — генерирует `width: auto; flex: 0 0 auto; min-width: <ItemWidth или 200px>`.
   Работает в паре с `SgRow.ItemWidth`.
5. **`Grow` / `Basis`** (`int?`, `string?`) — современный flex-примитив
   для колонки. `Grow=1` ≡ `Fill=true`, но с произвольным значением.
6. **CSS class helpers** — заменить портянку `if (Xs is >= 1 and <= 12)` на
   CssBuilder.

---

## Step 3 — SgStack: `Density`, `Stretch`, рефактор (P1)

`SuperUI/Components/Layout/Stack/SgStack.razor.cs`

1. **`Density`** — как у SgRow.
2. **`Stretch`** (bool) — `align-items: stretch` shortcut, по умолчанию true
   (совпадает с default). Доп. смысл: делает child items `flex: 1 1 auto`
   чтобы они занимали равное место (если установлен).
3. **`ItemBasis`** (string?) — передаётся на child через CSS-var
   `--sg-stack-item-basis`.
4. **Привести Inline-к switch-выражения** (если ещё не).
5. **OnClick guard** — `@onclick` только если есть делегат.

---

## Step 4 — SgSpace: magic-strings → SgSize, починить Split, Density, CssBuilder (P1)

`SuperUI/Components/Layout/Space/SgSpace.razor.cs`

1. **`Size` (string) → `SgSize? Size` (enum).** Старый `string Size`
   помечаем `[Obsolete]`, конвертируем:
   - `"small"` → `SgSize.Sm` (8px)
   - `"middle"` → `SgSize.Md` (16px)
   - `"large"` → `SgSize.Lg` (24px)
   - `"16px"` → SgSpace превращает в `var(--sg-space-N)` или
     оставляет как есть (custom CSS).
2. **`Density`** — как у Row/Stack.
3. **Починить `Split`.** Текущий код делает `_items.Count > 0` всегда `false`,
   разделитель рендерится только как `<span class="sgc-space-split">@Split</span>`
   один раз, без повторения. Исправить: использовать
   `ChildContent` (все дочерние фрагменты) и автоматически
   вставлять Split между ними через `RenderTreeBuilder`. В качестве
   простого решения — обернуть `@ChildContent` в рендер-фрагмент,
   который разделяет `Split` (без отдельного API SpaceItem). Самый
   простой способ: использовать `@: {child}` в `for` по `RenderTreeBuilder`.
4. **CssBuilder / StyleBuilder** — привести в соответствие с SgStack.
5. **CSS class** — оставляем `sgc-space` (исторически сложилось), но
   добавляем `sg-space` алиасы для совместимости.

---

## Step 5 — CSS: добавить недостающие правила (P1)

`SuperUI/wwwroot/superui-components.css` — около строки 5780 (после `.sg-col`).

1. **`.sg-col-xs-1` … `.sg-col-xs-12`** в `@media (max-width: 575.98px)`.
   Текущий Xs игнорируется потому что CSS-классы не существуют.
2. **`.sg-col-xs-hidden`** в `@media (max-width: 575.98px) { display: none !important; }`
3. **`.sg-col-sm-hidden`** в `@media (min-width: 576px) and (max-width: 767.98px) { display: none !important; }`
4. **Аналогично `md`/`lg`/`xl-hidden`.**
5. **`.sg-col-order-{xs,sm,md,lg,xl}-N`** — `order: N` в нужном медиа.
   (N=0..12).
6. **`.sg-col-align-self-{xs,sm,md,lg,xl}-{start,center,end,stretch,baseline}`**
   в нужных медиа — `align-self: …`.
7. **`.sg-row-hoverable`** — лёгкий shadow + bg shift на hover.
8. **`.sg-row-clickable`** — `cursor: pointer; user-select: none;` + плавный transition.
9. **`.sg-col-hoverable`** / **`.sg-col-clickable`** — аналогично.
10. **Density tokens** — определить на `.sg-row[data-density="compact"]`,
    `.sg-stack[data-density="compact"]`, и т.д.:
    ```css
    .sg-row[data-density="compact"] { --sg-row-density-scale: 0.5; }
    .sg-row[data-density="comfortable"] { --sg-row-density-scale: 1.5; }
    .sg-row[data-density] { gap: calc(var(--sg-row-density-scale, 1) * var(--sg-row-gap, 16px)); }
    ```
    (Row в SgRow-razor.cs применяет через `gap: calc(ResolvedGutter * scale)`).

---

## Step 6 — RowDemo: переписать showcase (P1)

`SuperUI.Demo/Components/Pages/RowDemo.razor`

Цели:
- Показать всю мощь семейства SgRow/SgCol/SgStack/SgSpace.
- Использовать **максимально компоненты библиотеки**:
  `SgCard`, `SgTypography`, `SgSegmented`, `SgBadge`, `SgTag`, `SgButton`,
  `SgDivider`, `SgHeroicon`, `SgSwitch`, `SgSlider`, `SgAlert`, `SgPropertyGrid`,
  `SgCodeBlock` (если есть), `SgSplitter`, `SgTooltip`.
- Компактно, аккуратно, гармонично.

Новая структура:
1. **Hero / intro** — `SgCard` + `DemoPageHeader` (как сейчас).
2. **SgRow — Quick Start** — несколько мини-карточек, каждая
   демонстрирует один аспект. Кнопка "Copy" с кодом через `SgCodeBlock`.
3. **SgRow — Density** — три мини-примера (Compact / Default / Comfortable)
   с одинаковым контентом — наглядно.
4. **SgRow — Span & Auto** — 12-колонка, 24-колонка, Auto/Fill.
5. **SgCol — Offset / Order / Push / Pull** — текущая секция, но
   аккуратнее.
6. **SgCol — Responsive** — интерактивный превью (XS / SM / MD / LG / XL)
   с текущим viewport-indicator (уже есть) + ResponsiveOrder/Align demo.
7. **SgRow — Auto-fit / ItemWidth** — новый раздел.
8. **SgRow — Alignment / Direction / Wrap** — текущая, но компактнее.
9. **SgRow — Semantic HTML** — текущая, но компактнее.
10. **SgSpace — gap, separator, density, wrap** — компактнее, SgSize.
11. **SgStack — orientations, dividers, grow, fill, real layout** — компактнее.
12. **Live Constructor** — улучшить: добавить Density переключатель,
    переключатель Columns (12/24), выбор ItemWidth для Auto-fit,
    Compact Toggle. Шапка более информативная.
13. **API Reference** — все три PropertyTable заменить на `SgPropertyGrid`
    (если он доступен и удобен) или оставить PropertyTable.

CSS: вынести все demo-стили из Razor в `RowDemo.razor.css` (Blazor
поддерживает co-located CSS, см. `DocumentExtractorDemo.razor.css`).

---

## Step 7 — Final build verification

- `dotnet build SuperUI/SuperUI.csproj -c Release` — без новых warnings
  (допускаются pre-existing).
- `dotnet build SuperUI.Demo/SuperUI.Demo.csproj -c Release` — Demo
  собирается.
- Коммит только если что-то реально правилось (например,
  выпилил случайный warning).

---

## Что НЕ трогаем в этом раунде

- Большие компоненты `SgDataGrid` (140 KB), `SgChart`, `SgKanban`, `SgGantt`,
  `SgPivotTable`, `SgKonva`, `SgThree`, `SgBpmn`, `SgMermaid` — у них
  отдельный жизненный цикл.
- `SgSplitter` — отдельный JS-компонент, ломать нельзя.
- Themes / Theme service — major release 2.0 (см. `plans/theme-2.0.md`).
- Experimental beta-компоненты.

---

## Порядок коммитов

1. `polish: update planminimax.md (round 2: row/col/stack/space family)`
2. `feat(row): SgRowTag enum + Density + FullHeight + ItemWidth + onClick guard` *(Step 1)*
3. `feat(col): responsive order/align + AutoFit + Basis/Grow + refactor` *(Step 2)*
4. `feat(stack): Density + Stretch + ItemBasis + onClick guard` *(Step 3)*
5. `refactor(space): SgSize enum + Density + fix Split + builders` *(Step 4)*
6. `css(layout): add xs breakpoint, hide, order, align, hover/clickable, density` *(Step 5)*
7. `demo(row): rewrite showcase with library components + new features` *(Step 6)*
8. `chore: final build verification` *(Step 7, если что-то правилось)*
