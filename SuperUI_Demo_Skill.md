# Skill: SuperUI Demo Page Creation Guide

Этот документ является эталонным руководством для агентов по созданию и обновлению демонстрационных страниц (Demo Pages) в проекте SuperUI.Blazor. 

## 1. Архитектура страницы (по мотивам RowDemo)
Каждая страница должна иметь четкую структуру, основанную на системных компонентах.

### Базовый макет
- **Контейнер:** Вся страница оборачивается в один `SgCard` с `Variant="SgCardVariant.Default"`.
- **Шапка:** Используйте параметр `HeaderContent` у карточки. Внутри должен быть `SgStack` с иконкой раздела и `SgTypography` (H4 для заголовка).
- **Секции:** Разделяйте примеры на блоки с классом `demo-section`. 
- **Заголовки секций:** Используйте класс `demo-section-header`, внутри которого `SgTypography` (H5 для заголовка и Sm для описания).

```razor
<SgCard Variant="SgCardVariant.Default" HeaderContent="@_mainHeader">
    <div class="demo-section">
        <div class="demo-section-header">
            <SgTypography Variant="SgTypographyVariant.Heading5">Basic Usage</SgTypography>
            <SgTypography Size="SgTypographySize.Sm">Description...</SgTypography>
        </div>
        <!-- Примеры здесь -->
    </div>
</SgCard>
```

## 2. Интерактивность и Конструкторы
Для сложных компонентов обязательно добавляйте раздел **Live Constructor**.
- **Слева (Control Panel):** Используйте `SgCol` с элементами управления (`SgSegmented` для Enums, `SgSwitch` для bool, `SgSelect` для чисел).
- **Справа (Preview):** Отображение компонента в реальном времени с примененными параметрами.
- **Стилизация:** Используйте `SgStack` и `SgRow` для плотной компоновки контролов.
- **Компоненты:** максимально используй компонеты библиотеки.

## 3. Типографика и Темы
- **Никаких сырых тегов:** Вместо `<h1>`, `<span>`, `<b>` всегда используйте `<SgTypography>`.
- **Семантика веса:** 
  - `Weight="SgTypographyWeight.Bold"` — для главных заголовков.
  - `Weight="SgTypographyWeight.Semibold"` — для заголовков секций.
  - `Weight="SgTypographyWeight.Medium"` — для акцентов в тексте.
- **Цвета:** Используйте только системные переменные:
  - `Color="var(--sg-text-muted)"` для второстепенных описаний.
  - `Color="var(--sg-color-primary)"` для брендовых акцентов.

## 4. Описание API (PropertyTable)
В конце каждой страницы или секции добавляйте таблицу параметров.
- Используйте компонент `<PropertyTable Items="_properties" />`.
- Данные готовьте в `List<PropertyPanelItem>`.
- Используйте `BadgeText` и `BadgeVariant` (Success для событий, Danger для обязательных полей).

## 5. Работа с Enums (Важно!)
Всегда используйте системные перечисления для параметров:
- **Размеры:** `SgSize.Sm`, `SgSize.Md`, `SgSize.Lg`.
- **Тень:** `SgShadow.Sm`, `SgShadow.Md` и т.д.
- **Выравнивание:** `SgAlignItems`, `SgJustifyContent`.
- **Варианты:** `SgButtonVariant`, `SgCardVariant`.

## 6. Иконки
- **Системные:** `@SgIcons.Home`, `@SgIcons.Settings` через компонент `<SgIcon>`.
- **Heroicons:** Используйте `<SgHeroicon Name="user" Variant="Outline" />`.

## 7. Регистрация в меню
После создания страницы добавьте её в [AppNav.razor](file:///c:/Users/SuperComp/Documents/Blazor/SuperUI.Blazor/SuperUI.Demo/Components/Layout/AppNav.razor) в соответствующую группу:
```razor
<SgNavLink Href="my-component-demo" Text="My Component" Icon="@SgIcons.Component" />
```

## 8. Чек-лист перед завершением
1. [ ] Страница занимает всю ширину.
2. [ ] Использована типографика SuperUI.
3. [ ] Нет "ядовитых" цветов или HEX-кодов.
4. [ ] Добавлен Live Constructor (если применимо).
5. [ ] Добавлена PropertyTable с описанием API.
6. [ ] Страница добавлена в боковое меню.
