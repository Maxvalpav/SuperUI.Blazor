// SuperUI/Components/Forms/SgFieldDescriptor.cs
// ИСПРАВЛЕНО: вынесен из SgVirtualForm.razor в отдельный .cs файл
// Причина: типы объявленные внутри .razor недоступны из других .razor файлов
namespace SuperUI.Components.Forms;

/// <summary>
/// Дескриптор поля формы для виртуализации.
/// Описывает метаданные поля: имя, тип, метка, порядок.
/// </summary>
public sealed class SgFieldDescriptor
{
    public string Name { get; init; } = "";
    public string? Label { get; init; }
    public Type FieldType { get; init; } = typeof(string);
    public bool Required { get; init; }
    public int Order { get; init; }
    public int? MaxLength { get; init; }
    public string? Placeholder { get; init; }
    public object? DefaultValue { get; init; }
}

/// <summary>
/// Строитель дескрипторов на основе Reflection + атрибутов.
/// </summary>
public static class SgFieldDescriptorBuilder
{
    public static List<SgFieldDescriptor> Build<TModel>()
    {
        var props = typeof(TModel)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        var result = new List<SgFieldDescriptor>();
        int order = 0;

        foreach (var prop in props)
        {
            result.Add(new SgFieldDescriptor
            {
                Name      = prop.Name,
                Label     = prop.Name, // можно заменить на DisplayAttribute
                FieldType = prop.PropertyType,
                Order     = order++,
            });
        }

        return result.OrderBy(f => f.Order).ToList();
    }
}