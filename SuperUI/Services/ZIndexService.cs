// ─────────────────────────────────────────────────────────────────
// FILE: Services/ZIndexService.cs
// Описание: Глобальный менеджер z-index для оверлеев.
// Thread-safe через Interlocked.
// ─────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.Threading;

namespace SuperUI.Services;

/// <summary>
/// Интерфейс сервиса управления z-index для overlay компонентов.
/// </summary>
public interface IZIndexService
{
    int Acquire(string componentId);
    void Release(string componentId);
    int GetZIndex(string componentId);
    int BaseZIndex { get; }
}

/// <summary>
/// Singleton-сервис управления z-index для оверлеев.
/// Гарантирует уникальный нарастающий z-index без конфликтов.
/// </summary>
public sealed class ZIndexService : IZIndexService
{
    private readonly int _baseZIndex;
    private readonly Dictionary<string, int> _stack = new();
    private int _counter;
    private readonly object _lock = new();

    public ZIndexService(int baseZIndex = 1000)
    {
        _baseZIndex = baseZIndex;
    }

    public int BaseZIndex => _baseZIndex;

    public int Acquire(string componentId)
    {
        lock (_lock)
        {
            _counter++;
            var zIndex = _baseZIndex + _counter;
            _stack[componentId] = zIndex;
            return zIndex;
        }
    }

    public void Release(string componentId)
    {
        lock (_lock)
        {
            _stack.Remove(componentId);
            if (_stack.Count == 0) _counter = 0;
        }
    }

    public int GetZIndex(string componentId)
    {
        lock (_lock)
        {
            return _stack.TryGetValue(componentId, out var z) ? z : _baseZIndex;
        }
    }
}
