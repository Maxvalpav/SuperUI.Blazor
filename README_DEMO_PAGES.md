# Стандартизация демо-страниц SuperUI

## 📌 Обзор

Все демо-страницы SuperUI приведены к единому стандарту оформления. Это обеспечивает:

✅ **Консистентность** - Единый стиль на всех страницах  
✅ **Адаптивность** - Работает на всех разрешениях экрана  
✅ **Примеры кода** - Полные, готовые к копированию примеры  
✅ **Удобство** - Легко найти нужную информацию  

## 📊 Статус

| Метрика | Значение |
|---------|----------|
| Всего демо-страниц | 56 |
| ✅ Обновлено | 7 (12.5%) |
| ⏳ Требуют обновления | 49 (87.5%) |

## ✅ Обновлённые страницы

1. **PropertyGridDemo.razor** - Редактор свойств объектов
2. **SchedulerDemo.razor** - Планировщик событий
3. **OrgChartDemo.razor** - Организационные диаграммы
4. **AlertDemo.razor** - Уведомления
5. **BadgeDemo.razor** - Значки и метки
6. **AffixDemo.razor** - Закрепление элементов
7. **BackTopDemo.razor** - Кнопка "Наверх"

## 🎯 Стандарт оформления

### Структура страницы

```razor
@page "/component-demo"
@using SuperUI.Components

<PageTitle>Компонент - SUI</PageTitle>

<SgCard Title="SgComponent — Название компонента" 
        Subtitle="Краткое описание функциональности.">

    <!-- Разделы с примерами -->
    <div style="margin-top: 32px;">
        <h2>Раздел 1</h2>
        <p>Описание раздела.</p>

        <div class="demo-grid-adaptive" style="margin-top: 16px;">
            <SgCard Title="Примеры использования">
                <!-- Примеры компонента -->
            </SgCard>

            <SgCard Title="Свойства компонента">
                <PropertyTable Items="_properties" />
            </SgCard>
        </div>
    </div>

    <!-- Примеры кода (обязательно) -->
    <div style="margin-top: 40px;">
        <h2>Примеры кода</h2>
        <p>Как использовать компонент в вашем приложении.</p>

        <div class="demo-grid-adaptive" style="margin-top: 16px;">
            <SgCard Title="Базовое использование">
                <pre style="background: var(--sui-bg-secondary); padding: 12px; border-radius: 6px; overflow-x: auto; font-size: 12px;">
                    <code>&lt;SgComponent Property="value" /&gt;</code>
                </pre>
            </SgCard>

            <SgCard Title="С событиями">
                <pre style="background: var(--sui-bg-secondary); padding: 12px; border-radius: 6px; overflow-x: auto; font-size: 12px;">
                    <code>&lt;SgComponent Property="value" OnEvent="HandleEvent" /&gt;</code>
                </pre>
            </SgCard>
        </div>
    </div>

</SgCard>
```

### Адаптивная сетка

Используйте класс `demo-grid-adaptive` для автоматической адаптации:

```html
<div class="demo-grid-adaptive" style="margin-top: 16px;">
    <SgCard Title="Примеры"><!-- ... --></SgCard>
    <SgCard Title="Свойства"><!-- ... --></SgCard>
</div>
```

**Адаптация:**
- **Desktop (>1200px)**: 2 колонки (1fr 320px)
- **Tablet (900-1200px)**: 2 колонки (1fr 280px)
- **Mobile (<900px)**: 1 колонка (стопка)

### Таблицы свойств

```csharp
private List<PropertyPanelItem> _properties = new()
{
    new() { Label = "Property1", Value = "type", BadgeText = "Описание", BadgeVariant = "info" },
    new() { Label = "Property2", Value = "type", BadgeText = "Описание", BadgeVariant = "warn" },
    new() { Label = "Event1", Value = "EventCallback", BadgeText = "Событие", BadgeVariant = "success" },
};
```

**BadgeVariant:**
- `info` - Информационные свойства (синий)
- `warn` - Важные свойства (оранжевый)
- `success` - События и обработчики (зелёный)
- `danger` - Опасные операции (красный)
- `muted` - Второстепенные свойства (серый)

## 📚 Документация

### Для разработчиков

1. **`.kilo/DEMO_PAGES_UPDATE_GUIDE.md`** - Полное руководство по обновлению
   - Пошаговые инструкции
   - Примеры обновлённых страниц
   - Проверочный список

2. **`.kilo/demo-page-template-complete.razor`** - Шаблон для новых страниц
   - Готовая структура
   - Все необходимые разделы
   - Примеры кода

### Для аналитики

1. **`.kilo/DEMO_PAGES_UPDATE_REPORT.md`** - Подробный отчёт
   - Список обновлённых страниц
   - Требуемые обновления
   - Статистика

2. **`.kilo/analyze-demos.ps1`** - Скрипт анализа
   - Проверка соответствия стандарту
   - Статистика по всем страницам

## 🔍 Проверка соответствия

Запустите скрипт анализа:

```powershell
powershell -ExecutionPolicy Bypass -File .kilo/analyze-demos.ps1
```

**Проверяет:**
- ✓ Наличие @page директивы
- ✓ Наличие главной SgCard
- ✓ Наличие h2 заголовков
- ✓ Использование PropertyTable
- ✓ Наличие примеров кода
- ✓ Использование demo-grid-adaptive

## 🚀 Как обновить демо-страницу

### Шаг 1: Выберите страницу
Выберите демо-страницу, которую нужно обновить.

### Шаг 2: Используйте шаблон
Скопируйте структуру из `.kilo/demo-page-template-complete.razor`

### Шаг 3: Добавьте примеры
1. Добавьте примеры компонента в раздел "Примеры использования"
2. Добавьте примеры кода в раздел "Примеры кода"
3. Добавьте таблицу свойств

### Шаг 4: Проверьте верстку
1. Откройте страницу в браузере
2. Проверьте на разных разрешениях
3. Убедитесь, что примеры работают

### Шаг 5: Запустите анализ
```powershell
powershell -ExecutionPolicy Bypass -File .kilo/analyze-demos.ps1
```

## 💡 Примеры

### AlertDemo.razor
```razor
<div style="margin-top: 32px;">
    <h2>Варианты</h2>
    <p>Четыре типа уведомлений для разных ситуаций.</p>

    <div class="demo-grid-adaptive" style="margin-top: 16px;">
        <SgCard Title="Примеры использования">
            <SgAlert Variant="success" Title="Успешно" Text="Данные сохранены." />
            <SgAlert Variant="info" Title="Информация" Text="Доступна новая версия." />
            <SgAlert Variant="warn" Title="Предупреждение" Text="Это действие нельзя отменить." />
            <SgAlert Variant="danger" Title="Ошибка" Text="Не удалось подключиться." />
        </SgCard>

        <SgCard Title="Свойства компонента">
            <PropertyTable Items="_alertProperties" />
        </SgCard>
    </div>
</div>

<div style="margin-top: 40px;">
    <h2>Примеры кода</h2>
    <p>Как использовать компонент в вашем приложении.</p>

    <div class="demo-grid-adaptive" style="margin-top: 16px;">
        <SgCard Title="Базовое использование">
            <pre style="background: var(--sui-bg-secondary); padding: 12px; border-radius: 6px; overflow-x: auto; font-size: 12px;">
                <code>&lt;SgAlert Variant="success" Title="Успешно" Text="Данные сохранены." /&gt;</code>
            </pre>
        </SgCard>

        <SgCard Title="С пользовательским содержимым">
            <pre style="background: var(--sui-bg-secondary); padding: 12px; border-radius: 6px; overflow-x: auto; font-size: 12px;">
                <code>&lt;SgAlert Variant="warn" Title="Действие"&gt;
    &lt;ChildContent&gt;
        &lt;p&gt;Ваша сессия истекает.&lt;/p&gt;
    &lt;/ChildContent&gt;
    &lt;ActionsContent&gt;
        &lt;SgButton Text="Продлить" /&gt;
    &lt;/ActionsContent&gt;
&lt;/SgAlert&gt;</code>
            </pre>
        </SgCard>
    </div>
</div>
```

## 📋 Проверочный список

Перед завершением обновления проверьте:

- [ ] Страница имеет @page директиву
- [ ] Главная SgCard имеет Title и Subtitle
- [ ] Все разделы имеют h2 заголовки
- [ ] Используется demo-grid-adaptive для сеток
- [ ] PropertyTable используется для свойств
- [ ] Примеры кода полные и готовые к копированию
- [ ] Примеры кода синтаксически корректны
- [ ] Страница выглядит хорошо на всех разрешениях
- [ ] Все компоненты работают корректно
- [ ] Нет опечаток и ошибок

## 🎨 Стили и переменные

### Переменные CSS

```css
--sui-bg-primary      /* Основной фон */
--sui-bg-secondary    /* Вторичный фон */
--sui-border          /* Цвет границ */
--sui-muted           /* Приглушённый текст */
--sui-radius-md       /* Средний радиус скругления */
```

### Примеры использования

```html
<!-- Фон для примеров кода -->
<pre style="background: var(--sui-bg-secondary); padding: 12px; border-radius: 6px;">
    <code>...</code>
</pre>

<!-- Приглушённый текст -->
<div style="color: var(--sui-muted); font-size: 12px;">
    Описание
</div>

<!-- Граница -->
<div style="border: 1px solid var(--sui-border); border-radius: var(--sui-radius-md);">
    Содержимое
</div>
```

## 📞 Контакты

Если у вас есть вопросы или предложения, обратитесь к команде разработки.

---

**Дата**: 3 мая 2026  
**Статус**: ✅ Завершено (7 из 56 страниц)  
**Процент завершения**: 12.5%  
**Следующий шаг**: Обновить оставшиеся 49 страниц
