// SuperUI/Base/Services/SgComponentFactory.cs

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Services;

/// <summary>
/// Фабрика для динамического создания компонентов SuperUI.
/// Полезна при генерации UI на основе данных (form builders, dashboard builders).
/// </summary>
public interface IComponentFactory
{
    /// <summary>
    /// Создать RenderFragment для компонента заданного типа с параметрами.
    /// </summary>
    RenderFragment Create<TComponent>(Action<Dictionary<string, object?>>? configureParams = null)
        where TComponent : IComponent;

    /// <summary>
    /// Создать RenderFragment с содержимым (ChildContent).
    /// </summary>
    RenderFragment Create<TComponent>(
        RenderFragment childContent,
        Action<Dictionary<string, object?>>? configureParams = null)
        where TComponent : IComponent;
}

/// <summary>
/// Реализация фабрики компонентов.
/// Использует IServiceProvider для разрешения зависимостей (если требуется DI-контейнером).
/// </summary>
public sealed class ComponentFactory : IComponentFactory
{
    public ComponentFactory()
    {
    }

    public RenderFragment Create<TComponent>(
        Action<Dictionary<string, object?>>? configureParams = null)
        where TComponent : IComponent
    {
        return builder =>
        {
            builder.OpenComponent<TComponent>(0);

            if (configureParams is not null)
            {
                var parameters = new Dictionary<string, object?>();
                configureParams(parameters);

                var seq = 1;
                foreach (var (name, value) in parameters)
                    builder.AddAttribute(seq++, name, value);
            }

            builder.CloseComponent();
        };
    }

    public RenderFragment Create<TComponent>(
        RenderFragment childContent,
        Action<Dictionary<string, object?>>? configureParams = null)
        where TComponent : IComponent
    {
        return builder =>
        {
            builder.OpenComponent<TComponent>(0);

            if (configureParams is not null)
            {
                var parameters = new Dictionary<string, object?>();
                configureParams(parameters);

                var seq = 1;
                foreach (var (name, value) in parameters)
                    builder.AddAttribute(seq++, name, value);
            }

            builder.AddAttribute(99, "ChildContent", childContent);
            builder.CloseComponent();
        };
    }
}