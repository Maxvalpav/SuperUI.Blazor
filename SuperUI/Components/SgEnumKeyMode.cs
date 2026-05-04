namespace SuperUI.Components;

/// <summary>
/// Determines what value is used as the key when binding an enum to
/// <see cref="SgSelect{TValue}"/>, <see cref="SgMultiSelect{TItem,TKey}"/>, or
/// <see cref="SgComboBox{TValue}"/> via <c>EnumType</c>.
/// </summary>
public enum SgEnumKeyMode
{
    /// <summary>
    /// Use the enum member name (e.g. <c>"Active"</c>). Default.
    /// </summary>
    Name,

    /// <summary>
    /// Use the underlying integer value (e.g. <c>"1"</c>).
    /// </summary>
    IntValue
}
