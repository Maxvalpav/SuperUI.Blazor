using SuperUI.Components;
using SuperUI.Demo.Models;
using Microsoft.AspNetCore.Components;

namespace SuperUI.Demo.Components.Pages;

public partial class NavigationDemo
{
    [Inject] private SgToastService Toasts { get; set; } = default!;
    
    private int _currentStep = 1;
    private int _currentPage = 3;
    private string _tabPosition = "top";
    private string _activeTabTitle = "Обзор";

    private List<RadioOption<string>> _tabPositions = new()
    {
        new() { Value = "top", Label = "Сверху" },
        new() { Value = "bottom", Label = "Снизу" },
        new() { Value = "left", Label = "Слева" },
        new() { Value = "right", Label = "Справа" }
    };

    private List<ComponentParameter> _tabsParams = new()
    {
        new() { Name = "Position", Type = "string", Default = "top", Description = "Расположение вкладок (top, bottom, left, right)" },
        new() { Name = "ActiveTitle", Type = "string", Description = "Заголовок активной вкладки" },
        new() { Name = "ActiveTitleChanged", Type = "EventCallback<string>", Description = "Событие при изменении активной вкладки" },
        new() { Name = "ChildContent", Type = "RenderFragment", Description = "Содержимое вкладок (SgTabPanel)" },
        new() { Name = "CssClass", Type = "string", Description = "Дополнительный CSS класс" }
    };

    private List<ComponentEvent> _tabsEvents = new()
    {
        new() { Name = "ActiveTitleChanged", Type = "EventCallback<string>", Description = "Вызывается при смене вкладки" }
    };

    private List<ComponentParameter> _stepperParams = new()
    {
        new() { Name = "Steps", Type = "IEnumerable<StepperItem>", Description = "Список шагов" },
        new() { Name = "Active", Type = "int", Description = "Индекс активного шага" },
        new() { Name = "Vertical", Type = "bool", Default = "false", Description = "Вертикальное отображение" },
        new() { Name = "Clickable", Type = "bool", Default = "true", Description = "Возможность клика по шагам" },
        new() { Name = "CssClass", Type = "string", Description = "Дополнительный CSS класс" }
    };

    private List<ComponentEvent> _stepperEvents = new()
    {
        new() { Name = "ActiveChanged", Type = "EventCallback<int>", Description = "Вызывается при переходе на другой шаг" }
    };

    private List<ComponentParameter> _menuParams = new()
    {
        new() { Name = "Text", Type = "string", Description = "Текст пункта меню (SgMenuItem)" },
        new() { Name = "Icon", Type = "RenderFragment", Description = "Иконка пункта меню" },
        new() { Name = "Shortcut", Type = "string", Description = "Текст горячей клавиши" },
        new() { Name = "Variant", Type = "string", Default = "default", Description = "Вариант отображения (default, danger)" },
        new() { Name = "Disabled", Type = "bool", Default = "false", Description = "Отключить пункт меню" },
        new() { Name = "OnClick", Type = "EventCallback", Description = "Событие при клике" }
    };

    private List<ComponentEvent> _menuEvents = new()
    {
        new() { Name = "OnShow", Type = "EventCallback", Description = "Вызывается при открытии меню" },
        new() { Name = "OnClose", Type = "EventCallback", Description = "Вызывается при закрытии меню" }
    };

    private List<ComponentParameter> _paginationParams = new()
    {
        new() { Name = "TotalItems", Type = "int", Description = "Общее количество элементов" },
        new() { Name = "PageSize", Type = "int", Default = "25", Description = "Количество элементов на странице" },
        new() { Name = "Page", Type = "int", Default = "1", Description = "Текущая страница" },
        new() { Name = "Siblings", Type = "int", Default = "1", Description = "Количество соседних страниц в пагинации" },
        new() { Name = "ShowInfo", Type = "bool", Default = "true", Description = "Показывать информацию о текущей странице" }
    };

    private List<ComponentEvent> _paginationEvents = new()
    {
        new() { Name = "PageChanged", Type = "EventCallback<int>", Description = "Вызывается при смене страницы" }
    };

    private List<ComponentParameter> _navMenuParams = new()
    {
        new() { Name = "Title", Type = "string", Description = "Заголовок/бренд в шапке меню" },
        new() { Name = "LogoContent", Type = "RenderFragment", Description = "Кастомный логотип в шапке" },
        new() { Name = "FooterContent", Type = "RenderFragment", Description = "Содержимое в нижней части меню" },
        new() { Name = "CssClass", Type = "string", Description = "Дополнительный CSS класс" }
    };

    private List<ComponentEvent> _navMenuEvents = new()
    {
        new() { Name = "OnToggle", Type = "EventCallback<bool>", Description = "Вызывается при сворачивании/разворачивании меню (мини-режим)" }
    };

    private List<BreadcrumbItem> _breadcrumbs = new()
    {
        new BreadcrumbItem { Text = "Главная", Href = "/" },
        new BreadcrumbItem { Text = "Демо-страницы", Href = "#" },
        new BreadcrumbItem { Text = "Навигация" }
    };

    private List<StepperItem> _steps = new()
    {
        new StepperItem { Title = "Авторизация", Description = "Вход в учетную запись" },
        new StepperItem { Title = "Настройка", Description = "Выбор параметров" },
        new StepperItem { Title = "Завершение", Description = "Подтверждение данных" }
    };

    private List<TimelineItem> _auditItems = new()
    {
        new TimelineItem { Title = "Вход в систему", Time = "10:24", Description = "Пользователь admin" },
        new TimelineItem { Title = "Изменение настроек", Time = "11:05", Description = "Обновлен профиль безопасности", Color = "#faad14" },
        new TimelineItem { Title = "Экспорт отчета", Time = "12:30", Description = "Отчет 'Продажи Q1' сформирован", Color = "#52c41a" }
    };

    private void OnPageChanged(int page)
    {
        _currentPage = page;
        Toasts.Show($"Переход на страницу {page}", "Навигация", "info");
    }

    private void HandleTabTitleChanged(string value)
    {
        _activeTabTitle = value;
    }

    private void HandleTabPositionChanged(string value)
    {
        _tabPosition = value;
    }

    private void HandleStepChanged(int value)
    {
        _currentStep = value;
    }

    private void HandlePageChanged(int value)
    {
        _currentPage = value;
    }
}
