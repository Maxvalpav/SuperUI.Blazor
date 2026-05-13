// SuperUI/Base/AriaBuilder.cs 
// Улучшения: 
// - Поддержка ARIA 1.2: aria-description, aria-details, aria-errormessage 
// - Fluent API 
// - Возвращает IReadOnlyDictionary для splatting 
// - Валидация значений в Debug режиме 
 
using System.Collections.Generic; 
using System.Diagnostics; 
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Fluent builder для ARIA атрибутов (WAI-ARIA 1.2). 
/// Использование: var aria = new AriaBuilder().Role("dialog").Label("Окно").Build(); 
/// </summary> 
public sealed class AriaBuilder 
{ 
    private readonly Dictionary<string, object> _attributes = new(); 
 
    /// <summary>Создать новый builder.</summary> 
    public static AriaBuilder Create() => new();

    // ────────────────────────────────────────────────────────────────────── 
    // ARIA 1.1 — основные атрибуты 
    // ────────────────────────────────────────────────────────────────────── 
 
    public AriaBuilder Role(string role) 
    { 
        _attributes["role"] = role; 
        return this; 
    } 
 
    public AriaBuilder Label(string label) 
    { 
        _attributes["aria-label"] = label; 
        return this; 
    } 
 
    public AriaBuilder LabelledBy(string id) 
    { 
        _attributes["aria-labelledby"] = id; 
        return this; 
    } 
 
    public AriaBuilder DescribedBy(string id) 
    { 
        _attributes["aria-describedby"] = id; 
        return this; 
    } 
 
    public AriaBuilder Hidden(bool hidden = true) 
    { 
        _attributes["aria-hidden"] = hidden.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    public AriaBuilder Expanded(bool? expanded) 
    { 
        _attributes["aria-expanded"] = expanded.HasValue 
            ? expanded.Value.ToString().ToLowerInvariant() 
            : "undefined"; 
        return this; 
    } 
 
    public AriaBuilder Selected(bool selected) 
    { 
        _attributes["aria-selected"] = selected.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    public AriaBuilder Checked(bool? @checked) 
    { 
        _attributes["aria-checked"] = @checked.HasValue 
            ? @checked.Value.ToString().ToLowerInvariant() 
            : "mixed"; 
        return this; 
    } 
 
    public AriaBuilder Disabled(bool disabled = true) 
    { 
        _attributes["aria-disabled"] = disabled.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    public AriaBuilder Required(bool required = true) 
    { 
        _attributes["aria-required"] = required.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    public AriaBuilder Invalid(string? value = "true") 
    { 
        _attributes["aria-invalid"] = value ?? "true"; 
        return this; 
    } 
 
    public AriaBuilder Live(string politeness = "polite") 
    { 
        Debug.Assert(politeness is "polite" or "assertive" or "off", 
            "aria-live must be 'polite', 'assertive', or 'off'"); 
        _attributes["aria-live"] = politeness; 
        return this; 
    } 
 
    public AriaBuilder Atomic(bool atomic = true) 
    { 
        _attributes["aria-atomic"] = atomic.ToString().ToLowerInvariant(); 
        return this; 
    } 
 
    public AriaBuilder HasPopup(string type = "true") 
    { 
        _attributes["aria-haspopup"] = type; 
        return this; 
    } 
 
    public AriaBuilder Controls(string id) 
    { 
        _attributes["aria-controls"] = id; 
        return this; 
    } 
 
    public AriaBuilder Owns(string id) 
    { 
        _attributes["aria-owns"] = id; 
        return this; 
    } 
 
    public AriaBuilder SetSize(int size) 
    { 
        _attributes["aria-setsize"] = size; 
        return this; 
    } 
 
    public AriaBuilder PosInSet(int position) 
    { 
        _attributes["aria-posinset"] = position; 
        return this; 
    } 
 
    public AriaBuilder Level(int level) 
    { 
        _attributes["aria-level"] = level; 
        return this; 
    } 
 
    public AriaBuilder ValueMin(double min) 
    { 
        _attributes["aria-valuemin"] = min; 
        return this; 
    } 
 
    public AriaBuilder ValueMax(double max) 
    { 
        _attributes["aria-valuemax"] = max; 
        return this; 
    } 
 
    public AriaBuilder ValueNow(double now) 
    { 
        _attributes["aria-valuenow"] = now; 
        return this; 
    } 
 
    public AriaBuilder ValueText(string text) 
    { 
        _attributes["aria-valuetext"] = text; 
        return this; 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // ARIA 1.2 — новые атрибуты 
    // ────────────────────────────────────────────────────────────────────── 
 
    /// <summary> 
    /// [ARIA 1.2] Прямое текстовое описание элемента. 
    /// Используйте вместо aria-describedby когда нет отдельного элемента. 
    /// </summary> 
    public AriaBuilder Description(string description) 
    { 
        _attributes["aria-description"] = description; 
        return this; 
    } 
 
    /// <summary> 
    /// [ARIA 1.2] Ссылка на детальное описание (расширение aria-describedby). 
    /// </summary> 
    public AriaBuilder Details(string id) 
    { 
        _attributes["aria-details"] = id; 
        return this; 
    } 
 
    /// <summary> 
    /// [ARIA 1.2] Ссылка на элемент с сообщением об ошибке. 
    /// Используется вместе с aria-invalid. 
    /// </summary> 
    public AriaBuilder ErrorMessage(string id) 
    { 
        _attributes["aria-errormessage"] = id; 
        return this; 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Кастомный атрибут 
    // ────────────────────────────────────────────────────────────────────── 
 
    public AriaBuilder Set(string attribute, object value) 
    { 
        _attributes[attribute] = value; 
        return this; 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Построение 
    // ────────────────────────────────────────────────────────────────────── 
 
    public IReadOnlyDictionary<string, object> Build() => _attributes; 
 
    /// <summary>Слияние с дополнительными атрибутами (для @attributes splatting).</summary> 
    public IReadOnlyDictionary<string, object> BuildWith( 
        IReadOnlyDictionary<string, object>? additional) 
    { 
        if (additional == null || additional.Count == 0) return _attributes; 
 
        var merged = new Dictionary<string, object>(_attributes); 
        foreach (var kv in additional) 
            merged[kv.Key] = kv.Value; 
        return merged; 
    } 
} 
