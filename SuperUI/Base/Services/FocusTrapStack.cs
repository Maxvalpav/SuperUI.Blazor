// SuperUI/Base/Services/FocusTrapStack.cs
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Services;

/// <summary>Thread-safe stack for managing nested focus traps.</summary>
public class FocusTrapStack
{
    private readonly Stack<FocusTrapEntry> _stack = new();
    private readonly object _lock = new();

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { lock (_lock) return _stack.Count; }
    }

    public void Push(ElementReference element, FocusTrapOptions options)
    {
        lock (_lock)
        {
            _stack.Push(new FocusTrapEntry(element, options));
        }
    }

    public bool TryPop(out FocusTrapEntry entry)
    {
        lock (_lock)
        {
            if (_stack.Count > 0)
            {
                entry = _stack.Pop();
                return true;
            }
            entry = default;
            return false;
        }
    }

    public bool TryPeek(out FocusTrapEntry entry)
    {
        lock (_lock)
        {
            if (_stack.Count > 0)
            {
                entry = _stack.Peek();
                return true;
            }
            entry = default;
            return false;
        }
    }
}
