using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SuperUI.Components;

/// <summary>
/// Represents a single enum member as a selectable item.
/// </summary>
public sealed class SgEnumItem
{
    /// <summary>Enum member name (e.g. "Active").</summary>
    public string Name { get; init; } = "";

    /// <summary>Underlying integer value (e.g. 1).</summary>
    public int IntValue { get; init; }

    /// <summary>
    /// Display label — taken from <c>[Display(Name=...)]</c>, then
    /// <c>[Description(...)]</c>, then the member name itself.
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// Optional description from <c>[Display(Description=...)]</c> or
    /// <c>[Description(...)]</c>.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional group name from <c>[Display(GroupName=...)]</c>.
    /// </summary>
    public string? GroupName { get; init; }

    /// <summary>
    /// Optional display order from <c>[Display(Order=...)]</c>.
    /// </summary>
    public int Order { get; init; }

    /// <summary>Returns the key string for the given <see cref="SgEnumKeyMode"/>.</summary>
    public string GetKey(SgEnumKeyMode mode) =>
        mode == SgEnumKeyMode.IntValue ? IntValue.ToString() : Name;
}

/// <summary>
/// Utility for reflecting enum members into <see cref="SgEnumItem"/> lists.
/// Results are cached per enum type.
/// </summary>
public static class SgEnumHelper
{
    private static readonly Dictionary<Type, List<SgEnumItem>> _cache = new();

    /// <summary>
    /// Returns all members of <typeparamref name="TEnum"/> as <see cref="SgEnumItem"/> list.
    /// </summary>
    public static List<SgEnumItem> GetItems<TEnum>() where TEnum : struct, Enum
        => GetItems(typeof(TEnum));

    /// <summary>
    /// Returns all members of the given enum <paramref name="enumType"/> as
    /// <see cref="SgEnumItem"/> list.
    /// </summary>
    public static List<SgEnumItem> GetItems(Type enumType)
    {
        if (!enumType.IsEnum)
            throw new ArgumentException($"{enumType.Name} is not an enum.", nameof(enumType));

        if (_cache.TryGetValue(enumType, out var cached))
            return cached;

        var items = new List<SgEnumItem>();

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var intVal = Convert.ToInt32(field.GetValue(null));

            // [Display] attribute (DataAnnotations)
            var display = field.GetCustomAttribute<DisplayAttribute>();
            // [Description] attribute (ComponentModel)
            var desc = field.GetCustomAttribute<DescriptionAttribute>();

            var label = display?.GetName()
                     ?? desc?.Description
                     ?? field.Name;

            var description = display?.GetDescription()
                           ?? (desc is not null && desc.Description != label ? desc.Description : null);

            var groupName = display?.GetGroupName();
            var order     = display?.GetOrder() ?? 0;

            items.Add(new SgEnumItem
            {
                Name        = field.Name,
                IntValue    = intVal,
                Label       = label,
                Description = description,
                GroupName   = groupName,
                Order       = order
            });
        }

        // Sort by Order if any Display(Order) was set, otherwise keep declaration order
        if (items.Any(i => i.Order != 0))
            items = items.OrderBy(i => i.Order).ToList();

        _cache[enumType] = items;
        return items;
    }

    /// <summary>
    /// Clears the internal cache. Useful in tests or hot-reload scenarios.
    /// </summary>
    public static void ClearCache() => _cache.Clear();
}
