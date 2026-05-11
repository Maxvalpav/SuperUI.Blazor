namespace SuperUI.Utilities;

/// <summary>
/// Фабрика конвертеров.
/// </summary>
public static class SgConverterFactory
{
    public static ISgConverter<T> Get<T>() => typeof(T) switch
    {
        var t when t == typeof(int)     => (ISgConverter<T>)(object)new NumericConverter<int>(),
        var t when t == typeof(double)  => (ISgConverter<T>)(object)new NumericConverter<double>(),
        var t when t == typeof(decimal) => (ISgConverter<T>)(object)new NumericConverter<decimal>(),
        var t when t == typeof(string)  => (ISgConverter<T>)(object)new StringPassthroughConverter(),
        _                               => throw new NotSupportedException($"No converter for {typeof(T).Name}")
    };
}

public sealed class StringPassthroughConverter : SgConverter<string>
{
    public override bool TryConvert(string? text, out string? value, out string? error)
    {
        value = text; error = null; return true;
    }
    public override string? ConvertBack(string? value) => value;
}
