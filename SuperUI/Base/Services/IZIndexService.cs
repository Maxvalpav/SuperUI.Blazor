// SuperUI/Base/Services/IZIndexService.cs
//
// Централизованный менеджер z-index для overlay-компонентов.
// Гарантирует правильный порядок наложения (dialog поверх tooltip и т.д.).
//
// Thread safety:
// - WASM: однопоточный → Interlocked лишний, но корректен.
// - Server: каждый circuit — Scoped DI → отдельный экземпляр → нет конкуренции.
//   Если Singleton — Interlocked необходим.

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления z-index для overlay-компонентов.
/// </summary>
public interface IZIndexService
{
    /// <summary>Получить следующий z-index (выше всех текущих).</summary>
    int GetNext();

    /// <summary>Освободить z-index (позволяет повторное использование).</summary>
    void Release(int zIndex);

    /// <summary>Текущий максимальный z-index.</summary>
    int Current { get; }
}

/// <summary>
/// Реализация <see cref="IZIndexService"/> для Scoped/Singleton DI.
/// </summary>
public sealed class ZIndexService : IZIndexService
{
    // Базовое значение — выше типичных CSS z-index (nav, header итд.)
    private const int Base = 1000;
    private int _current = Base;
    private readonly SortedSet<int> _released = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public int Current => Volatile.Read(ref _current);

    /// <inheritdoc />
    public int GetNext()
    {
        lock (_lock)
        {
            // Повторно используем освобождённые значения
            if (_released.Count > 0)
            {
                var reused = _released.Max;
                _released.Remove(reused);
                if (reused > _current) _current = reused;
                return reused;
            }

            return ++_current;
        }
    }

    /// <inheritdoc />
    public void Release(int zIndex)
    {
        if (zIndex <= Base) return;
        lock (_lock)
        {
            _released.Add(zIndex);
            // Если это максимальный — уменьшаем _current
            while (_current > Base && _released.Contains(_current))
            {
                _released.Remove(_current);
                _current--;
            }
        }
    }
}
