// StyleBuilder.cs — Fluent Builder for Inline CSS styles 
 
using System; 
using System.Collections.Generic;
using System.Runtime.CompilerServices; 
using System.Text; 
 
namespace SuperUI.Base.Utilities; 
 
/// <summary> 
/// Fluent API for building inline CSS styles. 
/// 
/// Example: 
/// <code> 
/// var style = new StyleBuilder() 
///     .Add("display", "flex") 
///     .Add("width", width + "px", width > 0) 
///     .Add("color", "var(--sg-primary)") 
///     .AddFromAttributes(additionalAttributes) 
///     .Build(); 
/// </code> 
/// </summary> 
public sealed class StyleBuilder 
{ 
    private readonly StringBuilder _builder = new(); 
 
    /// <summary> 
    /// Create a new builder instance. 
    /// </summary> 
    public static StyleBuilder Default() => new();

    /// <summary> 
    /// Add a CSS property. 
    /// </summary> 
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public StyleBuilder Add(string property, string? value, bool condition = true) 
    { 
        if (!condition || string.IsNullOrWhiteSpace(value)) 
            return this; 
 
        if (_builder.Length > 0) 
            _builder.Append(' '); 
 
        _builder.Append(property); 
        _builder.Append(':'); 
        _builder.Append(value.Trim()); 
        _builder.Append(';'); 
 
        return this; 
    } 
 
    /// <summary> 
    /// Add a CSS property with a numeric value and unit. 
    /// </summary> 
    public StyleBuilder Add(string property, double value, string unit = "px", bool condition = true) 
    { 
        return Add(property, $"{value}{unit}", condition); 
    } 
 
    /// <summary> 
    /// Add multiple CSS properties as a single string (format: "prop: value; prop2: value2"). 
    /// </summary> 
    public StyleBuilder AddRaw(string? rawStyles, bool condition = true) 
    { 
        if (!condition || string.IsNullOrWhiteSpace(rawStyles)) 
            return this; 
 
        if (_builder.Length > 0) 
            _builder.Append(' '); 
 
        _builder.Append(rawStyles.Trim()); 
        if (!_builder.ToString().EndsWith(';'))
            _builder.Append(';');

        return this; 
    } 

    /// <summary>
    /// Alias for AddRaw for backward compatibility.
    /// </summary>
    public StyleBuilder AddUserStyle(string? userStyle) => AddRaw(userStyle);
 
    /// <summary> 
    /// Add styles from AdditionalAttributes (style). 
    /// </summary> 
    public StyleBuilder AddFromAttributes(IReadOnlyDictionary<string, object>? attributes) 
    { 
        if (attributes == null) return this; 
 
        if (attributes.TryGetValue("style", out var styleValue) && styleValue is string styleString) 
        { 
            AddRaw(styleString); 
        } 
 
        return this; 
    } 
 
    /// <summary> 
    /// Add a CSS custom property (variable). 
    /// </summary> 
    public StyleBuilder AddCustomProperty(string name, string? value, bool condition = true) 
    { 
        return Add($"--{name}", value, condition); 
    } 
 
    /// <summary> 
    /// Build the resulting style string. 
    /// </summary> 
    public string Build() 
    { 
        return _builder.ToString(); 
    } 
 
    /// <summary> 
    /// Build string or return null if no styles. 
    /// </summary> 
    public string? NullIfEmpty() 
    { 
        var result = Build(); 
        return string.IsNullOrEmpty(result) ? null : result; 
    } 
 
    public override string ToString() => Build(); 
 
    public static implicit operator string(StyleBuilder builder) => builder.Build(); 
} 
