// SuperUI/Base/Services/FocusTrapStack.cs
//
// Вспомогательный класс для управления стеком активных focus trap-ов.
// Используется в SgOverlayBase для корректного восстановления фокуса
// при наличии нескольких одновременно открытых overlay.

using System;
using System.Collections.Generic;

namespace SuperUI.Base.Services;

/// <summary>
/// Вспомогательный класс для управления стеком активных focus trap-ов.
/// Используется в <see cref="SgOverlayBase"/> для корректного восстановления фокуса
/// при наличии нескольких одновременно открытых overlay.
/// </summary>
public sealed class FocusTrapStack
{
    private readonly Stack<string> _stack = new();

    /// <summary>Количество активных trap-ов.</summary>
    public int Count => _stack.Count;

    /// <summary>Верхний (текущий активный) trap ID.</summary>
    public string? Current => _stack.TryPeek(out var id) ? id : null;

    /// <summary>Добавить trap в стек.</summary>
    public void Push(string elementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        _stack.Push(elementId);
    }

    /// <summary>Удалить верхний trap из стека. Возвращает предыдущий активный ID или null.</summary>
    public string? Pop()
    {
        if (_stack.Count == 0) return null;
        _stack.Pop();
        return _stack.TryPeek(out var prev) ? prev : null;
    }

    /// <summary>Очистить стек.</summary>
    public void Clear() => _stack.Clear();
}
