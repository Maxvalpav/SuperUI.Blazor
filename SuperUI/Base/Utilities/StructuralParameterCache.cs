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
public sealed class ParameterChangeDetector
{
    private object?[]? _previous;
    private int _version;

    /// <summary>
    /// Проверить изменились ли параметры. Если да — обновляет кэш и возвращает true.
    /// </summary>
    public bool HasChanged(params object?[] currentValues)
    {
        if (_previous is null || _previous.Length != currentValues.Length)
        {
            _previous = (object?[])currentValues.Clone();
            _version++;
            return true;
        }

        for (int i = 0; i < currentValues.Length; i++)
        {
            if (!Equals(_previous[i], currentValues[i]))
            {
                currentValues.CopyTo(_previous, 0);
                _version++;
                return true;
            }
        }

        return false;
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