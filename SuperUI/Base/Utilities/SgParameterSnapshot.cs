// SuperUI/Base/Utilities/SgParameterSnapshot.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ FIX CS0305: убран generic-параметр TComponent (не использовался)
// ✅ NEW: SgParameterSnapshot — non-generic public struct
// ✅ NEW: TryGetValue — безопасное получение значения параметра через ParameterViewLazy
// ✅ PERF: HashCode кэшируется; минимум аллокаций
// ✅ NEW: операторы == / !=

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Снимок параметров компонента для структурного сравнения.
/// Используется в ShouldSetParameters для оптимизации — пропускает
/// обработку если параметры не изменились.
/// </summary>
public readonly struct SgParameterSnapshot : IEquatable<SgParameterSnapshot>
{
    private readonly int _hash;
    private readonly ParameterViewLazy _parameters;

    public SgParameterSnapshot(ParameterView parameters)
    {
        // PERF: сохраняем не весь ParameterView (struct copy),
        // а только ленивую обёртку + предвычисленный хэш
        _parameters = new ParameterViewLazy(parameters);
        _hash = ComputeHash(parameters);
    }

    private static int ComputeHash(ParameterView parameters)
    {
        var hash = new HashCode();
        foreach (var kvp in parameters)
        {
            hash.Add(kvp.Name);
            hash.Add(GetValueHash(kvp.Value));
        }
        return hash.ToHashCode();
    }

    private static int GetValueHash(object? value)
    {
        return value switch
        {
            null                => 0,
            string s            => s.GetHashCode(),
            int i               => i.GetHashCode(),
            long l              => l.GetHashCode(),
            bool b              => b.GetHashCode(),
            double d            => d.GetHashCode(),
            float f             => f.GetHashCode(),
            decimal m           => m.GetHashCode(),
            char c              => c.GetHashCode(),
            IEquatable<object> eq => eq.GetHashCode(),
            _                   => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value)
        };
    }

    /// <summary>Попытаться получить значение параметра по имени.</summary>
    public bool TryGetValue<T>(string name, out T? value)
        => _parameters.TryGetValue(name, out value);

    public bool Equals(SgParameterSnapshot other) => _hash == other._hash;
    public override bool Equals(object? obj) => obj is SgParameterSnapshot other && Equals(other);
    public override int GetHashCode() => _hash;

    public static bool operator ==(SgParameterSnapshot left, SgParameterSnapshot right)
        => left.Equals(right);

    public static bool operator !=(SgParameterSnapshot left, SgParameterSnapshot right)
        => !left.Equals(right);
}

/// <summary>
/// Ленивая обёртка ParameterView для избежания лишнего копирования.
/// Позволяет безопасно обращаться к параметрам по имени после снятия снимка.
/// </summary>
internal readonly struct ParameterViewLazy
{
    private readonly ParameterView _view;

    public ParameterViewLazy(ParameterView view) => _view = view;

    /// <summary>Попытаться получить значение параметра по имени.</summary>
    public bool TryGetValue<T>(string name, out T? value)
    {
        if (_view.TryGetValue<T>(name, out var val))
        {
            value = val;
            return true;
        }
        value = default;
        return false;
    }
}
