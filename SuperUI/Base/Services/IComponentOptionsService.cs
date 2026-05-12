// SuperUI/Base/Services/IComponentOptionsService.cs

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис получения параметров компонента из глобальной конфигурации.
/// Singleton: readonly → thread-safe.
/// </summary>
public interface IComponentOptionsService
{
    /// <summary>Получить конфигурацию для компонента по его типу.</summary>
    TOptions GetOptions<TComponent, TOptions>()
        where TOptions : class, new();

    /// <summary>Конфигурация библиотеки.</summary>
    SgLibraryOptions LibraryOptions { get; }
}
