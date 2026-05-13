// SgFormNameGenerator.cs — Генерация имен форм для .NET 8 SSR

namespace SuperUI.Base;

/// <summary>
/// Интерфейс для генерации уникальных имен форм.
/// </summary>
public interface IFormNameGenerator
{
    string GenerateName(Type modelType, string? suffix = null);
}

/// <summary>
/// Реализация по умолчанию для генерации имен форм.
/// </summary>
public class DefaultFormNameGenerator : IFormNameGenerator
{
    public string GenerateName(Type modelType, string? suffix = null)
    {
        var name = modelType.Name;
        if (!string.IsNullOrEmpty(suffix))
            name += $"_{suffix}";
            
        return name;
    }
}
