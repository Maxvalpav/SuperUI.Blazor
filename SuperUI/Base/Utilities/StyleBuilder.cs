// SuperUI/Base/Utilities/StyleBuilder.cs 
// Улучшения: 
// - Inline string building без промежуточных аллокаций 
// - Fluent API аналогичный SgCssBuilder 
// - Кэширование результата 
// - Поддержка CSS переменных 
 
using System; 
using System.Text; 
 
namespace SuperUI.Base.Utilities; 
 
/// <summary> 
/// Fluent builder для inline CSS стилей. 
/// Кэширует результат между рендерами. 
/// </summary> 
public sealed class StyleBuilder 
{ 
    private readonly StringBuilder _sb = new(); 
    private string? _cached; 
    private bool _isDirty; 
 
    public static StyleBuilder Default() => new(); 
 
    /// <summary>Добавляет CSS свойство.</summary> 
    public StyleBuilder Add(string property, string value) 
    { 
        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(value)) 
            return this; 
 
        if (_sb.Length > 0) _sb.Append(';'); 
        _sb.Append(property.Trim()); 
        _sb.Append(':'); 
        _sb.Append(value.Trim()); 
        _isDirty = true; 
        _cached = null; 
        return this; 
    } 
 
    /// <summary>Добавляет CSS свойство при условии.</summary> 
    public StyleBuilder Add(string property, string value, bool condition) 
        => condition ? Add(property, value) : this; 
 
    /// <summary>Добавляет CSS свойство при условии (lazy).</summary> 
    public StyleBuilder Add(string property, Func<string> valueFactory, bool condition) 
        => condition ? Add(property, valueFactory()) : this; 
 
    /// <summary>Добавляет CSS переменную.</summary> 
    public StyleBuilder AddVar(string varName, string value) 
        => Add($"--{varName}", value); 
 
    /// <summary>Добавляет пользовательский стиль (последним, для override).</summary> 
    public StyleBuilder AddUserStyle(string? userStyle) 
    { 
        if (string.IsNullOrWhiteSpace(userStyle)) return this; 
        if (_sb.Length > 0) _sb.Append(';'); 
        _sb.Append(userStyle.Trim()); 
        _isDirty = true; 
        _cached = null; 
        return this; 
    } 
 
    /// <summary>Возвращает итоговую строку стилей.</summary> 
    public string Build() 
    { 
        if (_cached != null) return _cached; 
        _cached = _sb.Length == 0 ? string.Empty : _sb.ToString(); 
        return _cached; 
    } 
 
    public static implicit operator string(StyleBuilder builder) => builder.Build(); 
    public override string ToString() => Build(); 
} 
