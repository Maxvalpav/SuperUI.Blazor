// SuperUI/Base/ISgComponent.cs — НОВЫЙ (UX-1)
//
// НОВОЕ:
// ✅ Минимальный публичный контракт компонента SuperUI
// ✅ Используется в юнит-тестах с bUnit без зависимости от ComponentBase
// ✅ Позволяет мокировать компоненты в тестах
// ✅ Улучшает тестируемость и разделение ответственности

namespace SuperUI.Base;

/// <summary>
/// Минимальный публичный контракт компонента SuperUI.
/// Используется в юнит-тестах с bUnit без зависимости от ComponentBase.
/// </summary>
/// <remarks>
/// Реализуется SgComponentBase и всеми его наследниками.
/// Позволяет тестировать компоненты через интерфейс вместо конкретного типа.
/// </remarks>
public interface ISgComponent
{
    /// <summary>Уникальный идентификатор компонента.</summary>
    string ComponentId { get; }

    /// <summary>Компонент видим.</summary>
    bool Visible { get; set; }

    /// <summary>Компонент утилизирован.</summary>
    bool IsDisposed { get; }

    /// <summary>Запросить перерисовку (без ожидания).</summary>
    void RequestRender();

    /// <summary>Обновить состояние и перерисовать (с ожиданием).</summary>
    Task RefreshAsync();
}
