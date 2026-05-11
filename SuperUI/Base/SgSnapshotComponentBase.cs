// SuperUI/Base/SgSnapshotComponentBase.cs
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>Помечает свойство для включения в snapshot.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SnapshotAttribute : Attribute { }

/// <summary>Контракт для компонентов с поддержкой снэпшота.</summary>
public interface ISnapshotable
{
    /// <summary>Захватить текущее состояние помеченных свойств.</summary>
    Dictionary<string, object?> CaptureSnapshot();

    /// <summary>Восстановить состояние из ранее захваченного снэпшота.</summary>
    void RestoreSnapshot(Dictionary<string, object?> snapshot);
}

/// <summary>
/// Базовый класс для компонентов с автоматическим сохранением/восстановлением
/// состояния через SessionStorage.
/// </summary>
public abstract class SgSnapshotComponentBase : SgInteractiveBase, ISnapshotable
{
    private static readonly ConcurrentDictionary<Type, SnapshotPropertyInfo[]> _cache = new();

    [Inject] private ISessionStorage SessionStorage { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var snapshot = await SessionStorage
                .GetItemAsync<Dictionary<string, object?>>(ComponentId);
            if (snapshot is not null)
                RestoreSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Id}] Failed to restore snapshot", ComponentId);
        }
        await base.OnInitializedAsync();
    }

    public Dictionary<string, object?> CaptureSnapshot()
    {
        var props = GetSnapshotProperties();
        var dict = new Dictionary<string, object?>(props.Length);
        foreach (var info in props)
            dict[info.Property.Name] = info.Property.GetValue(this);
        return dict;
    }

    public void RestoreSnapshot(Dictionary<string, object?> snapshot)
    {
        var props = GetSnapshotProperties();
        foreach (var info in props)
        {
            if (!snapshot.TryGetValue(info.Property.Name, out var value)) continue;
            try
            {
                if (value is null)
                {
                    if (info.IsValueType)
                        info.Property.SetValue(this, info.DefaultValue);
                    else
                        info.Property.SetValue(this, null);
                }
                else
                {
                    info.Property.SetValue(this, value);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "[{Id}] Snapshot restore skipped for {Prop}",
                    ComponentId, info.Property.Name);
            }
        }
    }

    private SnapshotPropertyInfo[] GetSnapshotProperties()
        => _cache.GetOrAdd(GetType(), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(p => p.GetCustomAttribute<SnapshotAttribute>() is not null
                      && p.CanRead && p.CanWrite
                      // Проверяем что setter не private (допускаем public/internal/protected)
                      && p.SetMethod is { IsPrivate: false })
             .Select(p => new SnapshotPropertyInfo(p))
             .ToArray());

    private sealed class SnapshotPropertyInfo
    {
        public readonly PropertyInfo Property;
        public readonly bool IsValueType;
        public readonly object? DefaultValue;

        public SnapshotPropertyInfo(PropertyInfo property)
        {
            Property = property;
            var propType = property.PropertyType;
            IsValueType = propType.IsValueType && Nullable.GetUnderlyingType(propType) is null;
            DefaultValue = IsValueType ? Activator.CreateInstance(propType) : null;
        }
    }
}