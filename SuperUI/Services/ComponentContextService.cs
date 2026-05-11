// ─────────────────────────────────────────────────────────────────
// FILE: Services/ComponentContextService.cs
// ИННОВАЦИЯ: Контекстный сервис компонента — передаёт мета-информацию
// вниз по дереву без CascadingParameter overhead.
// ─────────────────────────────────────────────────────────────────
using System.Collections.Generic;

namespace SuperUI.Services;

/// <summary>
/// Интерфейс сервиса контекста компонента.
/// </summary>
public interface IComponentContext
{
    string Size { get; set; }
    string Density { get; set; }
    string Variant { get; set; }
    bool Rtl { get; set; }
    IDictionary<string, object?> Extra { get; }
}

/// <summary>
/// Контекст компонента — легковесная альтернатива CascadingValue
/// для передачи мета-информации (theme variant, size, density...).
/// </summary>
public sealed class ComponentContext : IComponentContext
{
    /// <summary>Вариант размера компонентов в контексте.</summary>
    public string Size { get; set; } = "md"; // xs|sm|md|lg|xl

    /// <summary>Плотность контента.</summary>
    public string Density { get; set; } = "normal"; // compact|normal|comfortable

    /// <summary>Вариант внешнего вида.</summary>
    public string Variant { get; set; } = "default";

    /// <summary>RTL в контексте.</summary>
    public bool Rtl { get; set; }

    /// <summary>Дополнительные мета-данные.</summary>
    public IDictionary<string, object?> Extra { get; } = new Dictionary<string, object?>();
}
