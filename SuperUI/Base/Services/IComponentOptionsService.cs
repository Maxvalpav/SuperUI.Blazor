// SuperUI/Base/Services/IComponentOptionsService.cs

using SuperUI.Components;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис получения параметров компонента из глобальной конфигурации.
/// Singleton: readonly → thread-safe.
/// </summary>
public interface IComponentOptionsService
{
    /// <summary>Размер компонентов по умолчанию.</summary>
    SgSize DefaultSize { get; }

    /// <summary>Анимации включены.</summary>
    bool EnableAnimations { get; }

    /// <summary>ARIA атрибуты включены.</summary>
    bool EnableAria { get; }

    /// <summary>Локаль по умолчанию (BCP 47).</summary>
    string Locale { get; }

    /// <summary>Базовый z-index для overlay.</summary>
    int BaseZIndex { get; }

    /// <summary>Шаг z-index между слоями.</summary>
    int ZIndexStep { get; }

    /// <summary>CSS-префикс для всех компонентов.</summary>
    string CssPrefix { get; }

    /// <summary>Получить конфигурацию для компонента по его типу.</summary>
    TOptions GetOptions<TComponent, TOptions>()
        where TOptions : class, new();

    /// <summary>Конфигурация библиотеки.</summary>
    SgLibraryOptions LibraryOptions { get; }
}
