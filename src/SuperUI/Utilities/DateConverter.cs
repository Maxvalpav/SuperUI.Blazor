using SuperUI.Utilities;

namespace SuperUI.Utilities;

/// <summary>
/// Конвертер для DateTime с форматом.
/// </summary>
public sealed class DateConverter : SgConverter<DateTime?>
{
    private readonly string _format;

    public DateConverter(string format = "d") => _format = format;

    public override bool TryConvert(string? text, out DateTime? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) 
        { 
            value = null; 
            error = null; 
            return true; 
        }
        
        if (DateTime.TryParse(text, Culture as CultureInfo, DateTimeStyles.None, out var dt))
        { 
            value = dt; 
            error = null; 
            return true; 
        }
        
        value = null; 
        error = $"Некорректная дата: '{text}'"; 
        return false;
    }

    public override string? ConvertBack(DateTime? value)
        => value?.ToString(_format, Culture as CultureInfo);
}
