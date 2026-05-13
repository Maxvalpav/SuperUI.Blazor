// SuperUI/Base/ISgComponent.cs — НОВЫЙ (UX-1)
//
// НОВОЕ:
// ✅ Минимальный публичный контракт компонента SuperUI
// ✅ Используется в юнит-тестах с bUnit без зависимости от ComponentBase
// ✅ Позволяет мокировать компоненты в тестах
// ✅ Улучшает тестируемость и разделение ответственности

using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperUI.Base;

/// <summary>
/// Базовый контракт для всех компонентов SuperUI.
/// </summary>
public interface ISgComponent
{
    /// <summary>Уникальный идентификатор компонента.</summary>
    string ComponentId { get; }

    /// <summary>Компонент видим.</summary>
    bool Visible { get; set; }

    /// <summary>Компонент утилизирован.</summary>
    bool IsDisposed { get; }

    /// <summary>Дополнительные HTML-атрибуты (splatting).</summary>
    IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Пользовательский CSS класс.</summary>
    string? Class { get; set; }

    /// <summary>Пользовательский inline стиль.</summary>
    string? Style { get; set; }

    /// <summary>Компонент находится в фазе prerendering.</summary>
    bool IsPrerendering { get; }

    /// <summary>Компонент интерактивен.</summary>
    bool IsInteractive { get; }

    /// <summary>Запросить перерисовку (без ожидания).</summary>
    void RequestRender();

    /// <summary>Обновить состояние и перерисовать (с ожиданием).</summary>
    Task RefreshAsync();
}
