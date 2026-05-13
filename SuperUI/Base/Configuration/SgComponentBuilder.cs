// SuperUI/Base/Configuration/SgComponentBuilder.cs
// УЛУЧШЕНИЯ:
// ✅ NEW: WithRenderMode — установить render mode (.NET 8+)
// ✅ NEW: WithEventCallback — типизированный helper
// ✅ FIX: BuildParameters возвращает IReadOnlyDictionary (immutable)

using Microsoft.AspNetCore.Components;
using SuperUI.Base;

namespace SuperUI.Base.Configuration;

/// <summary>
/// Fluent builder для динамического создания компонентов SuperUI.
/// </summary>
public class SgComponentBuilder<TComponent> where TComponent : SgComponentBase
{
    private readonly Dictionary<string, object?> _parameters = new();
    private readonly List<Action<TComponent>> _configurators = new();

    public SgComponentBuilder<TComponent> WithId(string id)
    {
        _parameters["Id"] = id;
        return this;
    }

    public SgComponentBuilder<TComponent> WithClass(string cssClass)
    {
        _parameters["Class"] = cssClass;
        return this;
    }

    public SgComponentBuilder<TComponent> WithStyle(string style)
    {
        _parameters["Style"] = style;
        return this;
    }

    public SgComponentBuilder<TComponent> WithVisible(bool visible)
    {
        _parameters["Visible"] = visible;
        return this;
    }

    public SgComponentBuilder<TComponent> WithParameter(string name, object? value)
    {
        _parameters[name] = value;
        return this;
    }

    /// <summary>NEW: Установить render mode (.NET 8+).</summary>
    public SgComponentBuilder<TComponent> WithRenderMode(IComponentRenderMode renderMode)
    {
        _parameters["@rendermode"] = renderMode;
        return this;
    }

    /// <summary>NEW: Типизированный helper для EventCallback.</summary>
    public SgComponentBuilder<TComponent> WithEventCallback(
        string name, Func<Task> callback)
    {
        _parameters[name] = EventCallback.Factory.Create(this, callback);
        return this;
    }

    /// <summary>NEW: Типизированный helper для EventCallback<T>.</summary>
    public SgComponentBuilder<TComponent> WithEventCallback<TArg>(
        string name, Func<TArg, Task> callback)
    {
        _parameters[name] = EventCallback.Factory.Create<TArg>(this, callback);
        return this;
    }

    public SgComponentBuilder<TComponent> Configure(Action<TComponent> configurator)
    {
        _configurators.Add(configurator);
        return this;
    }

    /// <summary>FIX: возвращает IReadOnlyDictionary — immutable снаружи.</summary>
    public IReadOnlyDictionary<string, object?> BuildParameters()
        => new Dictionary<string, object?>(_parameters);

    public static implicit operator RenderFragment(SgComponentBuilder<TComponent> builder)
        => builder.Build();

    public RenderFragment Build() => renderTreeBuilder =>
    {
        renderTreeBuilder.OpenComponent<TComponent>(0);
        foreach (var (name, value) in _parameters)
        {
            if (value is not null)
                renderTreeBuilder.AddAttribute(1, name, value);
        }
        renderTreeBuilder.CloseComponent();
    };
}
