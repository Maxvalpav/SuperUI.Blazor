// SuperUI/Base/Utilities/SgParameterSnapshot.cs — НОВЫЙ
// ✅ Структурное сравнение параметров компонента
// ✅ Кэширование хэша параметров для ShouldSetParameters

using System;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Снимок параметров компонента для структурного сравнения.
/// Используется в ShouldSetParameters для оптимизации — пропускает
/// обработку если параметры не изменились.
/// </summary>
/// <typeparam name="TComponent">Тип компонента</typeparam>
internal readonly struct SgParameterSnapshot<TComponent> where TComponent : ComponentBase
{
    private readonly int _hash;
    private readonly ParameterView _parameters;

    public SgParameterSnapshot(ParameterView parameters)
    {
        _parameters = parameters;
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
            null => 0,
            string s => s.GetHashCode(),
            int i => i.GetHashCode(),
            long l => l.GetHashCode(),
            bool b => b.GetHashCode(),
            double d => d.GetHashCode(),
            float f => f.GetHashCode(),
            decimal m => m.GetHashCode(),
            char c => c.GetHashCode(),
            IEquatable<object> eq => eq.GetHashCode(),
            _ => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value)
        };
    }

    public bool Equals(SgParameterSnapshot<TComponent> other) => _hash == other._hash;

    public override bool Equals(object? obj) => obj is SgParameterSnapshot<TComponent> other && Equals(other);

    public override int GetHashCode() => _hash;
}
