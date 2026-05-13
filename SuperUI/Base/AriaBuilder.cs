// AriaBuilder.cs — Fluent Builder для ARIA атрибутов 
// Обеспечивает корректные ARIA атрибуты и их валидацию 
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices; 
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Fluent API для построения ARIA атрибутов. 
/// Помогает избежать ошибок accessibility. 
/// 
/// Пример: 
/// <code> 
/// var aria = new AriaBuilder() 
///     .Label("Close dialog") 
///     .DescribedBy("dialog-desc") 
///     .Expanded(isOpen) 
///     .Disabled(isDisabled) 
///     .Build(); 
/// </code> 
/// </summary> 
public sealed class AriaBuilder 
{ 
    private readonly Dictionary<string, object?> _attributes = new(StringComparer.OrdinalIgnoreCase); 
 
    /// <summary>aria-label</summary> 
    public AriaBuilder Label(string? label, bool condition = true) 
    { 
        if (condition && !string.IsNullOrWhiteSpace(label)) 
            _attributes["aria-label"] = label; 
        return this; 
    } 
 
    /// <summary>aria-labelledby</summary> 
    public AriaBuilder LabelledBy(string? id, bool condition = true) 
    { 
        if (condition && !string.IsNullOrWhiteSpace(id)) 
            _attributes["aria-labelledby"] = id; 
        return this; 
    } 
 
    /// <summary>aria-describedby</summary> 
    public AriaBuilder DescribedBy(string? id, bool condition = true) 
    { 
        if (condition && !string.IsNullOrWhiteSpace(id)) 
            _attributes["aria-describedby"] = id; 
        return this; 
    } 
 
    /// <summary>aria-expanded</summary> 
    public AriaBuilder Expanded(bool expanded) 
    { 
        _attributes["aria-expanded"] = expanded.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    /// <summary>aria-hidden</summary> 
    public AriaBuilder Hidden(bool hidden = true) 
    { 
        _attributes["aria-hidden"] = hidden.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    /// <summary>aria-disabled</summary> 
    public AriaBuilder Disabled(bool disabled = true) 
    { 
        _attributes["aria-disabled"] = disabled.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    /// <summary>aria-selected</summary> 
    public AriaBuilder Selected(bool selected = true) 
    { 
        _attributes["aria-selected"] = selected.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    /// <summary>aria-checked</summary> 
    public AriaBuilder Checked(bool? isChecked) 
    { 
        _attributes["aria-checked"] = isChecked switch 
        { 
            true => "true", 
            false => "false", 
            null => "mixed" 
        }; 
        return this; 
    } 
 
    /// <summary>aria-haspopup</summary> 
    public AriaBuilder HasPopup(string? popupType = "true") 
    { 
        _attributes["aria-haspopup"] = popupType ?? "true"; 
        return this; 
    } 
 
    /// <summary>aria-controls</summary> 
    public AriaBuilder Controls(string? id, bool condition = true) 
    { 
        if (condition && !string.IsNullOrWhiteSpace(id)) 
            _attributes["aria-controls"] = id; 
        return this; 
    } 
 
    /// <summary>aria-live</summary> 
    public AriaBuilder Live(string? politeness = "polite") 
    { 
        _attributes["aria-live"] = politeness ?? "off"; 
        return this; 
    } 
 
    /// <summary>aria-atomic</summary> 
    public AriaBuilder Atomic(bool atomic = true) 
    { 
        _attributes["aria-atomic"] = atomic.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    /// <summary>aria-busy</summary> 
    public AriaBuilder Busy(bool busy = true) 
    { 
        _attributes["aria-busy"] = busy.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    /// <summary>aria-current</summary> 
    public AriaBuilder Current(string? value = "true") 
    { 
        _attributes["aria-current"] = value ?? "true"; 
        return this; 
    } 
 
    /// <summary>aria-required</summary> 
    public AriaBuilder Required(bool required = true) 
    { 
        _attributes["aria-required"] = required.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    /// <summary>aria-invalid</summary> 
    public AriaBuilder Invalid(bool invalid = true) 
    { 
        _attributes["aria-invalid"] = invalid.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    /// <summary>aria-errormessage</summary> 
    public AriaBuilder ErrorMessage(string? id, bool condition = true) 
    { 
        if (condition && !string.IsNullOrWhiteSpace(id)) 
            _attributes["aria-errormessage"] = id; 
        return this; 
    } 
 
    /// <summary>role</summary> 
    public AriaBuilder Role(string? role, bool condition = true) 
    { 
        if (condition && !string.IsNullOrWhiteSpace(role)) 
            _attributes["role"] = role; 
        return this; 
    } 
 
    /// <summary>role (сокращение для alert)</summary> 
    public AriaBuilder Alert() => Role("alert"); 
 
    /// <summary>role (сокращение для status)</summary> 
    public AriaBuilder Status() => Role("status"); 
 
    /// <summary>role (сокращение для dialog)</summary> 
    public AriaBuilder Dialog() => Role("dialog"); 
 
    /// <summary>role (сокращение для button)</summary> 
    public AriaBuilder ButtonRole() => Role("button"); 
 
    /// <summary>Произвольный aria-* атрибут.</summary> 
    public AriaBuilder Add(string attribute, string? value, bool condition = true) 
    { 
        if (condition && !string.IsNullOrWhiteSpace(value)) 
        { 
            var key = attribute.StartsWith("aria-", StringComparison.OrdinalIgnoreCase) 
                ? attribute 
                : $"aria-{attribute}"; 
            _attributes[key] = value; 
        } 
        return this; 
    } 
 
    /// <summary> 
    /// Построить словарь атрибутов. 
    /// </summary> 
    public IReadOnlyDictionary<string, object?> Build() 
    { 
        return new Dictionary<string, object?>(_attributes); 
    } 
 
    /// <summary> 
    /// Получить значение атрибута. 
    /// </summary> 
    public string? Get(string attribute) 
    { 
        return _attributes.TryGetValue(attribute, out var value) ? value?.ToString() : null; 
    } 
 
    /// <summary> 
    /// Пустой builder. 
    /// </summary> 
    public static AriaBuilder Empty => new(); 
} 
