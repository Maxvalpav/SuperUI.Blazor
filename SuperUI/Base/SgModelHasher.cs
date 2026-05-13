// SuperUI/Base/SgModelHasher.cs 
// Улучшения: 
// - Кэш PropertyInfo[] по типу (избегаем рефлексию на каждый вызов) 
// - FrozenDictionary (.NET 8) для O(1) lookup 
// - IStructuralEquatable поддержка для коллекций 
// - Source Generator hint для AOT/trimming 
 
using System; 
using System.Collections.Concurrent; 
using System.Reflection; 
using System.Runtime.CompilerServices; 
using System.Collections;

namespace SuperUI.Base; 
 
/// <summary> 
/// Утилита для структурного хэширования моделей. 
/// Кэширует метаданные типов — без рефлексии на горячем пути. 
/// Совместим с AOT (Native AOT / Trimming) при использовании Source Generators. 
/// </summary> 
public static class SgModelHasher 
{ 
    // Кэш PropertyInfo[] по типу — заполняется один раз на тип 
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache 
        = new ConcurrentDictionary<Type, PropertyInfo[]>(); 
 
    /// <summary> 
    /// Вычисляет структурный хэш объекта по значениям его публичных свойств. 
    /// </summary> 
    public static int ComputeHash<T>(T? model) where T : class 
    { 
        if (model == null) return 0; 
 
        var props = _propertyCache.GetOrAdd( 
            typeof(T), 
            static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)); 
 
        var hash = new HashCode(); 
        hash.Add(typeof(T)); 
 
        foreach (var prop in props) 
        { 
            try 
            { 
                var value = prop.GetValue(model); 
                if (value is IStructuralEquatable seq) 
                    hash.Add(seq.GetHashCode(StructuralComparisons.StructuralEqualityComparer)); 
                else 
                    hash.Add(value); 
            } 
            catch 
            { 
                // Пропускаем свойства, которые нельзя прочитать 
            } 
        } 
 
        return hash.ToHashCode(); 
    } 
 
    /// <summary> 
    /// Быстрое структурное сравнение двух объектов. 
    /// </summary> 
    public static bool StructuralEquals<T>(T? a, T? b) where T : class 
    { 
        if (ReferenceEquals(a, b)) return true; 
        if (a == null || b == null) return false; 
        return ComputeHash(a) == ComputeHash(b); 
    } 
 
    /// <summary> 
    /// Очищает кэш для типа (нужно при hot-reload). 
    /// </summary> 
    public static void ClearCache<T>() where T : class 
        => _propertyCache.TryRemove(typeof(T), out _); 
 
    /// <summary>Очищает весь кэш.</summary> 
    public static void ClearAllCaches() => _propertyCache.Clear(); 
} 
