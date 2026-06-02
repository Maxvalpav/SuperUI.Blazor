# Component Library Polish Plan

Цель: пройтись по библиотеке `SuperUI/Components/**`, исправить найденные баги,
выровнять API между однотипными компонентами, починить безопасность и
доступность, не ломая публичный контракт. После каждого пункта — сборка +
коммит.

Каждый шаг помечен приоритетом:

- **P0** — критика (баги, безопасность, поломки UX/доступности)
- **P1** — заметная полировка (API/qulity-of-life, регрессий нет)
- **P2** — косметика (внутренний клин-ап, мини-улучшения)

Сборка после каждого шага: `dotnet build SuperUI/SuperUI.csproj -c Release`.
Каждый шаг — отдельный коммит.

---

## Step 1 — SgPinInput: убрать `JS.invoke('eval', ...)` (P0, безопасность)

`SuperUI/Components/Forms/PinInput/SgPinInput.razor`

Сейчас вся фокусировка/селект/очистка боксов идёт через
`JS.InvokeVoidAsync("eval", $"document.getElementById('{InputId(index)}')?.focus()")`
с интерполяцией строки. Это:

1. Использует запрещённый в проде `eval()` (CSP-несовместимый).
2. Делает 1 JS round-trip на каждый клик/keydown.

**Что сделать:**

- Хранить `ElementReference[]` для каждого `<input>` (через `@ref="_refs[idx]"`).
- `FocusInput(i)` → `_refs[i].FocusAsync()`.
- `SelectInput(i)` → новый JS-модуль `superui-pininput.js` с функцией `selectInput(el)`
  (загружается через `SgJsModuleCache`).
- `ClearDomInput(i)` → достаточно `StateHasChanged()` после обнуления `_chars[i]`
  (Blazor сам пересинхронизирует `value="@GetChar(idx)"`); если останется
  проблема рассинхронизации после фильтрации невалидного символа — добавить
  туда же `clearInput(el)`.
- Удалить метод `SyncDom` (он только делает `StateHasChanged`, имя вводит в
  заблуждение).

Файлы: `SgPinInput.razor`, новый `SgPinInput.razor.js` рядом
(`wwwroot/superui-pininput.js` если уже используется bundling, иначе
co-located JS-модуль; смотрим, как сделано в соседних компонентах).

---

## Step 2 — SgRating: клавиатура и a11y (P0)

`SuperUI/Components/Forms/Rating/SgRating.razor`

- Сейчас `role="slider"` без `tabindex` → клавиатурой не достучаться.
- `aria-label="@(starValue + " stars")"` — захардкоженная английская строка
  в одно из публичных свойств, ломает RU/UA/локализацию.
- Нет обработки `ArrowLeft/Right/Home/End`.
- При `Disabled` всё ещё ловит `@onclick`.

**Что сделать:**

- `tabindex="0"` на корне (или `-1` если `Disabled`).
- `@onkeydown` на корне:
  - `ArrowRight`/`ArrowUp` → `+precision`
  - `ArrowLeft`/`ArrowDown` → `-precision`
  - `Home` → `0`
  - `End` → `Max`
- Локализатор для `"stars"`: ключ `Rating_StarsValueLabel` (формат `{0} of {1}`)
  через `ISuperUILocalizer`.
- Не вешать `@onclick`/`@onmouseenter` когда `Disabled` или `ReadOnly`
  (вернуть `null` делегат).

---

## Step 3 — SgAvatar: культура и keyboard click (P1)

`SuperUI/Components/Display/Avatar/SgAvatar.razor`

- `GetInitials`: `parts[0][0].ToString().ToUpper()` — без культуры,
  делает турецкую `i → İ` неконсистентно. Заменить на
  `ToUpperInvariant()`.
- `font-size:{PixelSize.Value / 2.5}px` — `double.ToString()` без культуры:
  в локалях с запятой получим `font-size:16,8px` → невалидный CSS.
  Использовать `FormattableString.Invariant($"...")` (или `ToString("0.##", CultureInfo.InvariantCulture)`).
- `role="button"` + `tabindex="0"` есть, но нет `@onkeydown` для `Enter/Space` —
  клавиатура не активирует клик. Добавить `HandleKeyDownAsync`.
- На `<img>` нет `onerror` — если URL битый, не упадёт обратно на инициалы.
  Минимум: ловим ошибку через JS-маленький helper (`onerror="this.style.display='none'"`
  через `data-` атрибут) или флаг `_imgFailed`.

---

## Step 4 — SgButton: enum для LoadingPosition + a11y + link disabled (P1)

`SuperUI/Components/Forms/Button/SgButton.razor` + `.razor.cs`

1. `LoadingPosition` сейчас `string` ("left"/"right"/"center"). Это «магические строки».
   Завести `enum SgButtonLoadingPosition { Left, Center, Right }` в `SuperUI/Enums/`,
   оставить старый `string` параметр `[Obsolete]` с конвертацией (back-compat).
2. Link-вариант (`<a href>`) игнорирует `Disabled` и `Loading`: пользователь
   уйдёт по ссылке. Добавить:
   - `aria-disabled` уже есть, но нужно ещё `tabindex="-1"` и
     `@onclick:preventDefault="true"` когда `Disabled || Loading`, а сам `OnClickAsync`
     должен сразу `return` (он уже это делает).
   - Не рендерить `href`-навигацию при disabled — установить `href="javascript:void(0)"`
     ИЛИ убрать `href` и переключиться на `<button>`. Возьмём вариант 2:
     если disabled и есть Href — всё равно рендерим `<button>` с тем же видом.
3. `aria-label="@(string.IsNullOrEmpty(Text) ? Title : Text)"` — если задан
   `ChildContent` (например иконка) и нет ни `Text`, ни `Title`, кнопка станет
   безымянной. Добавить фолбэк через `Localizer["Common_Button"]`.

---

## Step 5 — SgTextBox: убрать `Task.Run` из debounce + OnFocus/OnBlur (P1)

`SuperUI/Components/Forms/Text/SgTextBox.razor`

- В Blazor WASM (single-thread) `Task.Run` бесполезен и создаёт лишний
  ContinueWith. Заменить на чистый `Task.Delay` + `InvokeAsync`:
  ```csharp
  _debounceCts = new();
  var token = _debounceCts.Token;
  var raw = e.Value?.ToString();
  _ = DebouncedCommitAsync(raw, token);
  async Task DebouncedCommitAsync(string? r, CancellationToken ct){
      try { await Task.Delay(Debounce, ct); await CommitAsync(r); }
      catch (TaskCanceledException) { }
  }
  ```
- Добавить параметры `OnFocus`/`OnBlur` (часто запрашиваемые) и
  `OnInput` (raw текст до commit/conversion).
- Длинная цепочка тернарок в `class` line 11 — вынести в `private string WrapClasses =>`.

---

## Step 6 — SgCheckBox: IAsyncDisposable + индетерминатность только при изменении (P1)

`SuperUI/Components/Forms/Check/SgCheckBox.razor`

- Сейчас `Dispose()` делает `_ = _module.DisposeAsync()` (fire-and-forget),
  при быстрой перерендеризации это может оставить «зависший» модуль.
  Реализовать `IAsyncDisposable`-цепочку: вызывать `DisposeAsync` корректно
  (наследоваться от `SgComponentBase`, у которого уже есть `DisposeAsync`).
- `SetIndeterminateAsync` ловит только `JSException`/`TaskCanceledException`,
  но не `ObjectDisposedException` (фейл при отмене во время отписки) — добавить.
- Кэшировать JS-модуль через `SgJsModuleCache` (singleton кэш импорта на тип).

---

## Step 7 — SgSwitch: убрать хардкод цветов (P1)

`SuperUI/Components/Forms/Switch/SgSwitch.razor`

`ComputedOnColor` для `SgColor.Danger` возвращает `"#e53935"`, для `Warning`
— `"#fb8c00"`. Не темо-зависимо. Заменить на тематические токены
(`var(--sg-color-danger)`, `var(--sg-color-warning)`).

---

## Step 8 — SgSlider: исправить keyboard на marks + вертикальный tooltip (P1)

`SuperUI/Components/Forms/Slider/SgSlider.razor`

- `@onkeydown="MarksClickable ? (() => ClickMark(mark)) : null"` срабатывает
  на ЛЮБУЮ клавишу — фактически набор символов на focused mark двигает значение.
  Должен реагировать только на `Enter`/`Space`.
- При `Vertical=true` tooltip позиционируется через `left: @FillPercent%`,
  что в вертикальной ориентации неправильно — должен быть `bottom:`.
- `CultureInfo.InvariantCulture` уже используется хорошо ✅.

---

## Step 9 — SgPagination: вынести RU-фолбэки в локалайзер (P1)

`SuperUI/Components/Data/Pagination/SgPagination.razor`

- `Localizer["Table_Of"] ?? "из"`, `Localizer["Selected"] ?? "Выбрано"` —
  захардкодженный русский фолбэк противоречит подходу остального проекта
  (для EN-пользователей с битым словарём покажется кириллица). Использовать
  `Localizer["Pagination_Of"]`, `Localizer["Pagination_Selected"]` БЕЗ
  hardcoded fallback (`ISuperUILocalizer` сам возвращает имя ключа при отсутствии).
- Добавить ключи в `Localization/SuperUiResources.*.resx`.

---

## Step 10 — SgModal: max-height + small polish (P1)

`SuperUI/Components/Overlays/Modal/SgModal.razor.cs`

- Высокий контент сейчас может вырасти за viewport (есть `Width`/`MaxWidth`/`MinWidth`,
  но нет `Height`/`MaxHeight`/`MinHeight`). Добавить параметры
  `MaxHeight`, `MinHeight`, `Height`.
- В CSS уже скорее всего есть `max-height: calc(100vh - 64px)`,
  но управление через параметры удобно.

---

## Step 11 — SgTextArea: вынести commonness в локализатор (P2)

`SuperUI/Components/Forms/Text/SgTextArea.razor`

- `@_wordCount words`, `@_lineCount lines`, `@rem remaining` — текст
  не локализован. Использовать `Localizer["TextArea_Words"]`,
  `Localizer["TextArea_Lines"]`, `Localizer["TextArea_Remaining"]`.
- `aria-label="@(string.IsNullOrEmpty(Label) ? Placeholder ?? "Text input" : null)"`
  — английский фолбэк "Text input" локализовать (`Common_TextInput`).

---

## Step 12 — Удалить пустые папки `AI/`, `Feedback/` (нет, они не пустые но почистить мусор) (P2)

В корне `SuperUI/Components/` есть файлы `_Imports.razor`, `A` (file?!), `NotificationItem.cs`.
Файл с именем `A` (без расширения) — мусор. Удалить.

```
Components/
├── _Imports.razor   ← OK
├── A                ← мусор, удалить
└── NotificationItem.cs ← переместить в Display/Notification/
```

---

## Step 13 — Финальная сборка + smoke-тест (P0 проверка)

- `dotnet build -c Release` без ошибок и **без новых предупреждений**.
- `dotnet test` (если есть тесты для затронутых компонентов).
- `dotnet build SuperUI.Demo` чтобы убедиться, что Demo-проект собирается
  с новыми сигнатурами.

---

## Что НЕ трогаем в этом раунде

- Большие компоненты `SgDataGrid` (140 KB), `SgChart`, `SgKanban`, `SgGantt`,
  `SgPivotTable`, `SgKonva`, `SgThree`, `SgBpmn`, `SgMermaid` — у них
  отдельный жизненный цикл и тесты, ломать их одним «полирующим» проходом
  опасно.
- Темо-генератор и `SgThemeService` — отдельный мажор-релиз 2.0
  (см. `plans/theme-2.0.md`).
- Experimental beta-компоненты из AI, Browser, Network, Industrial —
  по статусу из `components.md` они beta, политика «полировки» к ним
  применяется во вторую очередь.

---

## Порядок коммитов

1. `polish: add planminimax.md`
2. `fix(pininput): replace eval() with ElementReference focus` *(Step 1)*
3. `fix(rating): keyboard nav + localized aria + disabled guards` *(Step 2)*
4. `fix(avatar): invariant culture + keyboard activation + img fallback` *(Step 3)*
5. `refactor(button): enum for LoadingPosition + disabled link guard` *(Step 4)*
6. `refactor(textbox): drop Task.Run debounce + OnFocus/OnBlur` *(Step 5)*
7. `fix(checkbox): proper async disposal + cached JS module` *(Step 6)*
8. `fix(switch): use theme tokens instead of hardcoded colors` *(Step 7)*
9. `fix(slider): keyboard on marks + vertical tooltip placement` *(Step 8)*
10. `i18n(pagination): replace hardcoded RU fallbacks with resource keys` *(Step 9)*
11. `feat(modal): add MaxHeight/MinHeight/Height parameters` *(Step 10)*
12. `i18n(textarea): localize words/lines/remaining strings` *(Step 11)*
13. `chore(components): remove stray 'A' file, move NotificationItem.cs` *(Step 12)*
14. `chore: final build verification` *(Step 13, только если что-то правится)*
