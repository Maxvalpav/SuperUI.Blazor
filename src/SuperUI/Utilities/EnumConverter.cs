using SuperUI.Utilities;

namespace SuperUI.Utilities;

/// <summary>
/// Конвертер для Enum с регистронезависимым парсингом.
/// </summary>
public sealed class EnumConverter<TEnum> : SgConverter<TEnum> where TEnum : struct, Enum
{
    public override bool TryConvert(string? text, out TEnum value, out string? error)
    {
        if (Enum.TryParse<TEnum>(text, true, out value)) 
        { 
            error = null; 
            return true; 
        }
        
        error = $"'{text}' не является допустимым значением {typeof(TEnum).Name}";
        return false;
    }

    public override string? ConvertBack(TEnum value) => value.ToString();
}
