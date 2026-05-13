// ComponentIdGenerator.cs — Генератор уникальных ID для компонентов 
// Компактный (для WASM), уникальный, human-readable 
 
using System;
using System.Runtime.CompilerServices; 
using System.Security.Cryptography; 
using System.Threading;

namespace SuperUI.Base.Utilities; 
 
/// <summary> 
/// Генерирует уникальные идентификаторы для компонентов SuperUI. 
/// Формат: "sg-[тип]-[short-guid]" 
/// Пример: "sg-button-a3f2c1" 
/// </summary> 
public static class ComponentIdGenerator 
{ 
    private static readonly char[] s_base32Chars = "abcdefghijklmnopqrstuvwxyz012345".ToCharArray(); 
    private static int s_counter; 
 
    /// <summary> 
    /// Генерирует ID на основе типа компонента. 
    /// </summary> 
    public static string Generate(Type componentType) 
    { 
        var typeName = GetShortTypeName(componentType); 
        var uniquePart = GenerateShortId(); 
        return $"sg-{typeName}-{uniquePart}"; 
    } 
 
    /// <summary> 
    /// Генерирует ID с указанным префиксом. 
    /// </summary> 
    public static string Generate(string prefix) 
    { 
        var uniquePart = GenerateShortId(); 
        return $"{prefix}-{uniquePart}"; 
    } 
 
    /// <summary> 
    /// Генерирует ID с указанным префиксом (алиас для Generate). 
    /// </summary> 
    public static string Next(string prefix) => Generate(prefix);

    /// <summary> 
    /// Генерирует ID для ARIA атрибутов. 
    /// </summary> 
    public static string GenerateAriaId(string prefix) 
    { 
        return $"{prefix}-{GenerateShortId()}"; 
    } 
 
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    private static string GenerateShortId() 
    { 
        // Используем комбинацию счётчика и случайного числа для компактности 
        var counter = Interlocked.Increment(ref s_counter); 
        var random = RandomNumberGenerator.GetInt32(0, 32 * 32 * 32); 
 
        // 6-символьный base32 ID (32^6 = ~1 млрд комбинаций) 
        Span<char> id = stackalloc char[6]; 
        id[0] = s_base32Chars[(counter >> 0) & 31]; 
        id[1] = s_base32Chars[(random >> 5) & 31]; 
        id[2] = s_base32Chars[(random >> 10) & 31]; 
        id[3] = s_base32Chars[(random >> 15) & 31]; 
        id[4] = s_base32Chars[(counter >> 5) & 31]; 
        id[5] = s_base32Chars[(random >> 20) & 31]; 
 
        return new string(id); 
    } 
 
    private static string GetShortTypeName(Type type) 
    { 
        var name = type.Name; 
 
        // Убираем суффиксы компонентов 
        if (name.EndsWith("Component", StringComparison.Ordinal)) 
            name = name[..^9]; 
        else if (name.EndsWith("Base", StringComparison.Ordinal)) 
            name = name[..^4]; 
 
        // Убираем префиксы 
        if (name.StartsWith("Sg", StringComparison.Ordinal)) 
            name = name[2..]; 
 
        // CamelCase → kebab-case 
        return ToKebabCase(name); 
    } 
 
    private static string ToKebabCase(string name) 
    { 
        if (string.IsNullOrEmpty(name)) return name; 
 
        Span<char> result = stackalloc char[name.Length * 2]; 
        var index = 0; 
 
        for (var i = 0; i < name.Length; i++) 
        { 
            if (i > 0 && char.IsUpper(name[i])) 
            { 
                result[index++] = '-'; 
            } 
            result[index++] = char.ToLowerInvariant(name[i]); 
        } 
 
        return new string(result[..index]); 
    } 
} 
