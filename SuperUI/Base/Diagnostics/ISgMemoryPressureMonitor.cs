// SuperUI/Base/Diagnostics/ISgMemoryPressureMonitor.cs
// Интерфейс монитора давления памяти для Blazor Server.
namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Мониторинг использования памяти на Blazor Server.
/// Помогает предотвратить OutOfMemoryException в долгоживущих circuits.
/// </summary>
public interface ISgMemoryPressureMonitor : IDisposable
{
    /// <summary>Текущий объём использованной managed-памяти (байт).</summary>
    long CurrentMemoryBytes { get; }

    /// <summary>Текущий объём памяти в мегабайтах.</summary>
    double CurrentMemoryMB { get; }

    /// <summary>Текущий уровень давления памяти.</summary>
    MemoryPressureLevel CurrentLevel { get; }

    /// <summary>Событие, возникающее при изменении уровня давления памяти.</summary>
    event Action<MemoryPressureLevel>? MemoryPressureChanged;
}
