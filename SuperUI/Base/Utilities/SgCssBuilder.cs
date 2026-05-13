// SgCssBuilder.cs — Fluent Builder for CSS classes 
// Support for conditional classes, arrays, enum-based classes 
 
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices; 
using System.Text; 
 
namespace SuperUI.Base.Utilities; 
 
/// <summary> 
/// Fluent API for building CSS class strings. 
/// Analogous to classnames (JS), clsx, etc. 
/// 
/// Example: 
/// <code> 
/// var classes = new SgCssBuilder("base-class") 
///     .Add("active", isActive) 
///     .Add("disabled", isDisabled) 
///     .Add("size-" + size.ToString().ToLower()) 
///     .AddFromAttributes(additionalAttributes) 
///     .Build(); 
/// </code> 
/// </summary> 
public sealed class SgCssBuilder 
{ 
    private readonly StringBuilder _builder = new(); 
    private bool _hasClasses; 
 
    /// <summary> 
    /// Create builder with an initial class. 
    /// </summary> 
    public SgCssBuilder(string? baseClass = null) 
    { 
        if (!string.IsNullOrWhiteSpace(baseClass)) 
        { 
            _builder.Append(baseClass.Trim()); 
            _hasClasses = true; 
        } 
    } 
 
    /// <summary> 
    /// Add class if condition is true. 
    /// </summary> 
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public SgCssBuilder Add(string? cssClass, bool condition = true) 
    { 
        if (!condition || string.IsNullOrWhiteSpace(cssClass)) 
            return this; 
 
        if (_hasClasses) 
            _builder.Append(' '); 
 
        _builder.Append(cssClass.Trim()); 
        _hasClasses = true; 
        return this; 
    } 
 
    /// <summary> 
    /// Add multiple classes (space-separated) if condition is true. 
    /// </summary> 
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public SgCssBuilder AddMultiple(string? cssClasses, bool condition = true) 
    { 
        if (!condition || string.IsNullOrWhiteSpace(cssClasses)) 
            return this; 
 
        foreach (var cls in cssClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries)) 
        { 
            Add(cls); 
        } 
 
        return this; 
    } 
 
    /// <summary> 
    /// Add classes from a collection. 
    /// </summary> 
    public SgCssBuilder AddRange(IEnumerable<string>? classes) 
    { 
        if (classes == null) return this; 
 
        foreach (var cls in classes) 
        { 
            Add(cls); 
        } 
 
        return this; 
    } 
 
    /// <summary> 
    /// Add class if condition is true, otherwise another class. 
    /// </summary> 
    public SgCssBuilder AddOrElse(string trueClass, string falseClass, bool condition) 
    { 
        return condition ? Add(trueClass) : Add(falseClass); 
    } 
 
    /// <summary> 
    /// Add class based on enum value. 
    /// </summary> 
    public SgCssBuilder AddEnum<T>(T value, string prefix = "") where T : Enum 
    { 
        var className = $"{prefix}{value.ToString().ToLowerInvariant()}"; 
        return Add(className); 
    } 
 
    /// <summary> 
    /// Add classes from AdditionalAttributes (class/key). 
    /// </summary> 
    public SgCssBuilder AddFromAttributes(IReadOnlyDictionary<string, object>? attributes) 
    { 
        if (attributes == null) return this; 
 
        if (attributes.TryGetValue("class", out var classValue) && classValue is string classString) 
        { 
            AddMultiple(classString); 
        } 
 
        return this; 
    } 
 
    /// <summary> 
    /// Add classes based on size/variant. 
    /// </summary> 
    public SgCssBuilder AddSize(string component, string? size, string defaultSize = "md") 
    { 
        var effectiveSize = string.IsNullOrWhiteSpace(size) ? defaultSize : size; 
        return Add($"{component}--{effectiveSize.ToLowerInvariant()}"); 
    } 
 
    /// <summary> 
    /// Add modifiers like block--modifier. 
    /// </summary> 
    public SgCssBuilder AddModifier(string block, string modifier, bool condition = true) 
    { 
        return Add($"{block}--{modifier}", condition); 
    } 
 
    /// <summary> 
    /// Build the resulting CSS class string. 
    /// </summary> 
    public string Build() 
    { 
        return _builder.ToString(); 
    } 
 
    /// <summary> 
    /// Build string or return null if no classes. 
    /// </summary> 
    public string? NullIfEmpty() 
    { 
        var result = Build(); 
        return string.IsNullOrEmpty(result) ? null : result; 
    } 
 
    public override string ToString() => Build(); 
 
    /// <summary> 
    /// Implicit conversion to string. 
    /// </summary> 
    public static implicit operator string(SgCssBuilder builder) => builder.Build(); 
} 
