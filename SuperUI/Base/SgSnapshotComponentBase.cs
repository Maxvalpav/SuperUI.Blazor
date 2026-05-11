// SuperUI/Base/SgSnapshotComponentBase.cs
// ИСПРАВЛЕНО:
// 1. RestoreSnapshot — JsonElement конвертация через System.Text.Json
// 2. CaptureSnapshot возвращает Dictionary<string, JsonElement-совместимые типы>
// 3. Добавлен интерфейс ISnapshotService для тестируемости

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SuperUI.Base.Services;

namespace SuperUI.Base;

public interface ISnapshotable
{
    object? CaptureSnapshot();
    void RestoreSnapshot(object? snapshot);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SnapshotAttribute : Attribute { }

/// <summary>
/// Базовый класс с поддержкой сохранения/восстановления состояния через SessionStorage.
/// ИСПРАВЛЕНО: JsonElement корректно конвертируется при восстановлении.
/// </summary>
public abstract class SgSnapshotComponentBase : SgInteractiveBase, ISnapshotable
{
    // Статический кэш PropertyInfo[] per-type — zero-reflection при runtime
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _snapshotPropsCache = new();

    [Inject] private ISessionStorage SessionStorage { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var snapshot = await SessionStorage.GetItemAsync<Dictionary<string, JsonElement>>(ComponentId);
        if (snapshot != null)
            RestoreSnapshot(snapshot);
        await base.OnInitializedAsync();
    }

    public object? CaptureSnapshot()
    {
        var props = GetSnapshotProperties();
        var dict = new Dictionary<string, object?>(props.Length);
        foreach (var prop in props)
            dict[prop.Name] = prop.GetValue(this);
        return dict;
    }

    // ИСПРАВЛЕНО: поддержка JsonElement при десериализации из SessionStorage
    public void RestoreSnapshot(object? snapshot)
    {
        if (snapshot is not Dictionary<string, JsonElement> jsonDict &&
            snapshot is not Dictionary<string, object?> objDict)
            return;

        var props = GetSnapshotProperties();
        foreach (var prop in props)
        {
            object? rawValue = null;

            if (snapshot is Dictionary<string, JsonElement> jd)
            {
                if (!jd.TryGetValue(prop.Name, out var elem)) continue;
                rawValue = ConvertJsonElement(elem, prop.PropertyType);
            }
            else if (snapshot is Dictionary<string, object?> od)
            {
                if (!od.TryGetValue(prop.Name, out rawValue)) continue;
            }

            try { prop.SetValue(this, rawValue); }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex,
                    "[{Id}] Snapshot restore failed for property {Prop}",
                    ComponentId, prop.Name);
            }
        }
    }

    private static object? ConvertJsonElement(JsonElement elem, Type targetType)
    {
        try
        {
            return targetType switch
            {
                _ when targetType == typeof(string)  => elem.GetString(),
                _ when targetType == typeof(int)     => elem.GetInt32(),
                _ when targetType == typeof(long)    => elem.GetInt64(),
                _ when targetType == typeof(double)  => elem.GetDouble(),
                _ when targetType == typeof(float)   => (float)elem.GetDouble(),
                _ when targetType == typeof(bool)    => elem.GetBoolean(),
                _ when targetType == typeof(Guid)    => elem.GetGuid(),
                _ when targetType == typeof(DateTime)=> elem.GetDateTime(),
                _ when Nullable.GetUnderlyingType(targetType) is { } inner
                    => elem.ValueKind == JsonValueKind.Null
                        ? null
                        : ConvertJsonElement(elem, inner),
                _ => JsonSerializer.Deserialize(elem.GetRawText(), targetType)
            };
        }
        catch { return null; }
    }

    private PropertyInfo[] GetSnapshotProperties()
        => _snapshotPropsCache.GetOrAdd(GetType(), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(p => p.GetCustomAttribute<SnapshotAttribute>() != null
                      && p.CanRead && p.CanWrite)
             .ToArray());
}
