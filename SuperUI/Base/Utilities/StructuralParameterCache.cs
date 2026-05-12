// SuperUI/Base/Utilities/StructuralParameterCache.cs
// НОВОЕ: Структурное сравнение параметров Blazor компонента.
// Позволяет пропускать рендер если параметры реально не изменились.
//
// Аналог React.memo() / PureComponent в React.
// В Blazor нет встроенного механизма — это инновация.
//
// Использование в компоненте:
//   [Parameter] public string Title { get; set; } = "";
//   [Parameter] public int Count { get; set; }
//
//   protected override bool ShouldRender()
//       => _paramCache.HasChanged(Title, Count) || base.ShouldRender();
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Кэш параметров для структурного сравнения.
/// Позволяет определить изменились ли параметры между рендерами.
/// </summary>
/// <remarks>
/// Per-instance — безопасно на WASM и Server (per-circuit изоляция).
/// Не thread-safe; предполагается вызов из Blazor lifecycle/dispatcher.
/// Внимание: <c>params</c>-перегрузка аллоцирует массив на каждый вызов;
/// для hot-path используйте <see cref="SingleParameterDetector{T}"/>.
/// </remarks>
public sealed class ParameterChangeDetector
{
    private object?[]? _previous;
    private int _version;

    /// <summary>
    /// Проверить изменились ли параметры. Если да — обновляет кэш и возвращает true.
    /// </summary>
    public bool HasChanged(params object?[] currentValues)
        => HasChanged((ReadOnlySpan<object?>)currentValues);

    /// <summary>
    /// Allocation-free перегрузка для hot-path.
    /// </summary>
    public bool HasChanged(ReadOnlySpan<object?> currentValues)
    {
        if (_previous is null || _previous.Length != currentValues.Length)
        {
            _previous = currentValues.ToArray();
            _version++;
            return true;
        }

        var changed = false;
        for (int i = 0; i < currentValues.Length; i++)
        {
            if (!Equals(_previous[i], currentValues[i])) { changed = true; break; }
        }

        if (!changed) return false;

        currentValues.CopyTo(_previous);
        _version++;
        return true;
    }

    /// <summary>Текущая версия параметров (для отладки).</summary>
    public int Version => _version;

    /// <summary>Сбросить кэш (принудительный рендер).</summary>
    public void Invalidate() { _previous = null; _version++; }
}

/// <summary>
/// Быстрый детектор изменений для одного параметра.
/// Zero-allocation при отсутствии изменений.
/// </summary>
public sealed class SingleParameterDetector<T>
{
    private T? _previous;
    private bool _initialized;
    private readonly IEqualityComparer<T> _comparer;

    public SingleParameterDetector(IEqualityComparer<T>? comparer = null)
        => _comparer = comparer ?? EqualityComparer<T>.Default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasChanged(T current)
    {
        if (!_initialized)
        {
            _initialized = true;
            _previous = current;
            return true;
        }
        if (_comparer.Equals(_previous!, current)) return false;
        _previous = current;
        return true;
    }

    public T? Previous => _previous;
}