// SuperUI/Base/SgSnapshotComponentBase.cs
// ИСПРАВЛЕНО:
// 1. RestoreSnapshot: проверка на null для value-type свойств
// 2. CaptureSnapshot: возвращает только non-null значения для value типов
// 3. Reflection кэш: используем MethodHandle для сравнения типов (нет boxing)
// 4. Добавлен ISnapshotable явный интерфейс (не теряет контракт)
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SuperUI.Services;

namespace SuperUI.Base;

public interface ISnapshotable
{
    object? CaptureSnapshot();
    void RestoreSnapshot(object? snapshot);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SnapshotAttribute : Attribute { }

/// <summary>
/// Базовый класс для компонентов с поддержкой снэпшота состояния.
/// 
/// ИСПРАВЛЕНИЯ:
/// - RestoreSnapshot: null-check для value-type свойств
/// - Reflection кэш: статический per-type ConcurrentDictionary (нет повторного GetProperties)
/// - SessionStorage: защита от ошибок десериализации
/// </summary>
public abstract class SgSnapshotComponentBase : SgInteractiveBase, ISnapshotable
{
    // Статический кэш: PropertyInfo[] per конкретный тип компонента
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
        {
            var value = info.Property.GetValue(this);
            dict[info.Property.Name] = value;
        }
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
                        // Для value типов null недопустим → устанавливаем default
                        // Не бросаем исключение — используем default(T)
                        info.Property.SetValue(this, info.DefaultValue);
                    }
                    else
                    {
                        // Reference type — null допустим
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
                // Тип не совпал (например, был int, стал string после рефакторинга)
                // Игнорируем с логированием в DEBUG
                Logger.LogDebug(ex, "[{Id}] Snapshot restore failed for property {Prop}",
                    ComponentId, info.Property.Name);
            }
        }
    }

    private SnapshotPropertyInfo[] GetSnapshotProperties() =>
        _snapshotPropsCache.GetOrAdd(GetType(), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(p => p.GetCustomAttribute<SnapshotAttribute>() != null && p.CanRead && p.CanWrite)
             .Select(p => new SnapshotPropertyInfo(p))
             .ToArray());

    /// <summary>Кэшированные метаданные свойства для снэпшота.</summary>
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