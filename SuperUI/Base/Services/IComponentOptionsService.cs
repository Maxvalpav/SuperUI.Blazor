namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для доступа к глобальным опциям компонентов SuperUI.
/// Инжектируется в <see cref="SgComponentBase"/>.
/// </summary>
public interface IComponentOptionsService
{
    /// <summary>
    /// Получить опцию для конкретного типа компонента.
    /// Возвращает <c>null</c> если опция не задана.
    /// </summary>
    TOptions? GetOptions<TComponent, TOptions>()
        where TOptions : class;

    /// <summary>
    /// Получить опцию или создать дефолтную через фабрику.
    /// </summary>
    TOptions GetOrDefault<TComponent, TOptions>(Func<TOptions> defaultFactory)
        where TOptions : class;
}
