// SuperUI/Base/Configuration/SgComponentBuilder.cs
//
// Fluent API для конфигурации компонентов через C# код.
// Позволяет динамически создавать компоненты без Razor-разметки.
//
// Использование:
//   RenderFragment fragment = new SgComponentBuilder<SgButton>()
//       .WithId("my-btn")
//       .WithClass("btn-primary")
//       .WithParameter("Text", "Click me")
//       .Build();
//
//   // Или через implicit conversion:
//   RenderFragment fragment = new SgComponentBuilder<SgButton>()
//       .WithId("my-btn")
//       .WithClass("btn-primary");

using Microsoft.AspNetCore.Components;
using SuperUI.Base;

namespace SuperUI.Base.Configuration;

/// <summary>
/// Fluent builder для настройки компонента SuperUI через код (C# вместо Razor-атрибутов).
/// Значительно улучшает читаемость при динамическом создании компонентов.
/// </summary>
/// <typeparam name="TComponent">Тип компонента (должен наследовать SgComponentBase).</typeparam>
public class SgComponentBuilder<TComponent> where TComponent : SgComponentBase
{
    private readonly Dictionary<string, object?> _parameters = new();
    private readonly List<Action<TComponent>> _configurators = new();

    /// <summary>Установить параметр Id.</summary>
    public SgComponentBuilder<TComponent> WithId(string id)
    {
        _parameters["Id"] = id;
        return this;
    }

    /// <summary>Установить CSS класс.</summary>
    public SgComponentBuilder<TComponent> WithClass(string cssClass)
    {
        _parameters["Class"] = cssClass;
        return this;
    }

    /// <summary>Установить inline-стиль.</summary>
    public SgComponentBuilder<TComponent> WithStyle(string style)
    {
        _parameters["Style"] = style;
        return this;
    }

    /// <summary>Установить видимость компонента.</summary>
    public SgComponentBuilder<TComponent> WithVisible(bool visible)
    {
        _parameters["Visible"] = visible;
        return this;
    }

    /// <summary>Установить произвольный параметр компонента.</summary>
    public SgComponentBuilder<TComponent> WithParameter(string name, object? value)
    {
        _parameters[name] = value;
        return this;
    }

    /// <summary>
    /// Зарегистрировать конфигуратор, который будет вызван на экземпляре компонента
    /// после его создания. Полезно для настройки complex properties или вызова методов.
    /// </summary>
    public SgComponentBuilder<TComponent> Configure(Action<TComponent> configurator)
    {
        _configurators.Add(configurator);
        return this;
    }

    /// <summary>
    /// Получить словарь установленных параметров.
    /// </summary>
    public Dictionary<string, object?> BuildParameters()
    {
        return new Dictionary<string, object?>(_parameters);
    }

    /// <summary>
    /// Неявное преобразование в RenderFragment для использования в @(builder) контексте.
    /// </summary>
    public static implicit operator RenderFragment(SgComponentBuilder<TComponent> builder)
        => builder.Build();

    /// <summary>
    /// Построить RenderFragment для внедрения в дерево компонентов.
    /// </summary>
    public RenderFragment Build() => renderTreeBuilder =>
    {
        renderTreeBuilder.OpenComponent<TComponent>(0);
        foreach (var (name, value) in _parameters)
        {
            renderTreeBuilder.AddAttribute(1, name, value);
        }
        renderTreeBuilder.CloseComponent();
    };
}