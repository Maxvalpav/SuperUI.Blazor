namespace SuperUI.Services;

/// <summary>
/// Сервис для управления дополнительными атрибутами.
/// Позволяет задать глобальные атрибуты для всех компонентов типа.
/// Пример: добавить data-testid ко всем кнопкам в тестовом окружении.
/// </summary>
public interface IAdditionalAttributesService
{
    void RegisterGlobal(string componentType, string attributeName, object value);
    IReadOnlyDictionary<string, object> GetGlobalAttributes(string componentType);

    IReadOnlyDictionary<string, object> Merge(
        string componentType,
        IReadOnlyDictionary<string, object>? userAttributes);
}

public sealed class AdditionalAttributesService : IAdditionalAttributesService
{
    private readonly Dictionary<string, Dictionary<string, object>> _global = new();

    public void RegisterGlobal(string componentType, string attributeName, object value)
    {
        if (!_global.TryGetValue(componentType, out var attrs))
            _global[componentType] = attrs = new();
        attrs[attributeName] = value;
    }

    public IReadOnlyDictionary<string, object> GetGlobalAttributes(string componentType)
        => _global.TryGetValue(componentType, out var attrs) ? attrs : new Dictionary<string, object>();

    public IReadOnlyDictionary<string, object> Merge(
        string componentType,
        IReadOnlyDictionary<string, object>? userAttributes)
    {
        var result = new Dictionary<string, object>(GetGlobalAttributes(componentType));
        if (userAttributes != null)
            foreach (var kvp in userAttributes)
                result[kvp.Key] = kvp.Value;
        return result;
    }
}