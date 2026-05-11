// SuperUI/Base/SgSnapshotComponentBase.cs
// ИСПРАВЛЕНО:
// 1. RestoreSnapshot: null-check для value-type свойств (ArgumentException → graceful fallback)
// 2. Reflection кэш: статический per-type ConcurrentDictionary
// 3. SnapshotPropertyInfo кэширует IsValueType и DefaultValue (нет повторного Activator.CreateInstance)
// 4. Добавлен ISnapshotable явный интерфейс (чёткий контракт)
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SuperUI.Services;

namespace SuperUI.Base;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SnapshotAttribute : Attribute { }

public interface ISnapshotable
{
    object? CaptureSnapshot();
    void RestoreSnapshot(object? snapshot);
}

/// <summary>
/// Базовый класс для компонентов с поддержкой снэпшота состояния.
/// Автоматически сохраняет/восстанавливает свойства, помеченные [Snapshot].
/// </summary>
public abstract class SgSnapshotComponentBase : SgInteractiveBase, ISnapshotable
{
    // Статический кэш: PropertyInfo[] per конкретный тип компонента
    // Инициализируется один раз на тип, не на экземпляр
    private static readonly ConcurrentDictionary<Type, SnapshotPropertyInfo[]> _snapshotPropsCache = new();

    [Inject] private ISessionStorage SessionStorage { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var snapshot = await SessionStorage.GetItemAsync<Dictionary<string, object?>>(ComponentId);
            if (snapshot != null) RestoreSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Id}] Failed to restore snapshot from session storage", ComponentId);
        }
        await base.OnInitializedAsync();
    }

    public object? CaptureSnapshot()
    {
        var props = GetSnapshotProperties();
        var dict = new Dictionary<string, object?>(props.Length);
        foreach (var info in props)
            dict[info.Property.Name] = info.Property.GetValue(this);
        return dict;
    }

    public void RestoreSnapshot(object? snapshot)
    {
        if (snapshot is not Dictionary<string, object?> dict) return;

        var props = GetSnapshotProperties();
        foreach (var info in props)
        {
            if (!dict.TryGetValue(info.Property.Name, out var value)) continue;

            try
            {
                // ИСПРАВЛЕНО: проверка на null для value-type свойств
                if (value is null)
                {
                    if (info.IsValueType)
                    {
                        // Для value типов null недопустим → устанавливаем default(T)
                        info.Property.SetValue(this, info.DefaultValue);
                    }
                    else
                    {
                        info.Property.SetValue(this, null);
                    }
                }
                else
                {
                    info.Property.SetValue(this, value);
                }
            }
            catch (Exception ex)
            {
                // Тип не совпал (рефакторинг изменил тип свойства) — graceful fallback
                Logger.LogDebug(ex,
                    "[{Id}] Snapshot restore failed for property {Prop}",
                    ComponentId, info.Property.Name);
            }
        }
    }

    private SnapshotPropertyInfo[] GetSnapshotProperties()
        => _snapshotPropsCache.GetOrAdd(GetType(), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(p => p.GetCustomAttribute<SnapshotAttribute>() != null
                      && p.CanRead && p.CanWrite)
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
            // Nullable<T> — это value type, но допускает null
            IsValueType = propType.IsValueType && Nullable.GetUnderlyingType(propType) is null;
            DefaultValue = IsValueType ? Activator.CreateInstance(propType) : null;
        }
    }
}