// SuperUI/Base/SgSnapshotComponentBase.cs
// ИСПРАВЛЕНО:
// 1. ISessionStorage — используется JsSessionStorage (CS0246 устранён)
// 2. SaveSnapshotAsync() — добавлен метод сохранения
// 3. OnInitializedAsync — base.OnInitializedAsync() вызывается ПЕРВЫМ
// 4. RestoreSnapshot — защита от JsonElement при JSON-десериализации
// 5. AutoSave — опциональное автосохранение при изменении параметров
// 6. JSDisconnectedException в GetItemAsync/SetItemAsync — уже обработан в JsSessionStorage
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
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
/// <remarks>
/// Работает на Blazor WASM и Blazor Server.
/// При prerendering — пропускает операции JS interop.
/// </remarks>
public abstract class SgSnapshotComponentBase : SgInteractiveBase, ISnapshotable
{
    // Кэш reflection — per-Type, статический (hot-reload безопасен через .NET 9 MetadataUpdateHandler)
    private static readonly ConcurrentDictionary<Type, SnapshotPropertyInfo[]> _cache = new();

    [Inject] private ISessionStorage SessionStorage { get; set; } = null!;

    /// <summary>
    /// Автоматически сохранять snapshot при каждом OnParametersSetAsync.
    /// По умолчанию false — вызывайте SaveSnapshotAsync() вручную.
    /// </summary>
    [Parameter] public bool AutoSave { get; set; } = false;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        // ИСПРАВЛЕНО: сначала базовая инициализация (устанавливает параметры, инжекции)
        await base.OnInitializedAsync();

        // Затем восстанавливаем snapshot
        try
        {
            var snapshot = await SessionStorage.GetItemAsync<Dictionary<string, object?>>(ComponentId);
            if (snapshot is not null)
                RestoreSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Id}] Failed to restore snapshot", ComponentId);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (AutoSave)
        {
            try { await SaveSnapshotAsync(); }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[{Id}] AutoSave snapshot failed", ComponentId);
            }
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Сохранить текущий snapshot в SessionStorage.
    /// Вызывайте вручную или используйте AutoSave=true.
    /// </summary>
    public async Task SaveSnapshotAsync()
    {
        if (IsDisposed) return;
        try
        {
            var snapshot = CaptureSnapshot();
            await SessionStorage.SetItemAsync(ComponentId, snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Id}] Failed to save snapshot", ComponentId);
        }
    }

    /// <summary>Удалить snapshot из SessionStorage.</summary>
    public async Task ClearSnapshotAsync()
    {
        if (IsDisposed) return;
        try { await SessionStorage.RemoveItemAsync(ComponentId); }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Id}] Failed to clear snapshot", ComponentId);
        }
    }

    // ── ISnapshotable ──────────────────────────────────────────────────────────

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
                // ИСПРАВЛЕНО: JsonElement конвертация (при десериализации из sessionStorage
                // через System.Text.Json числа/строки приходят как JsonElement)
                var converted = ConvertFromJson(value, info.Property.PropertyType, info);
                info.Property.SetValue(this, converted);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "[{Id}] Snapshot restore skipped for {Prop}", ComponentId, info.Property.Name);
            }
        }
    }

    // ── Internals ──────────────────────────────────────────────────────────────

    private static object? ConvertFromJson(object? value, Type targetType, SnapshotPropertyInfo info)
    {
        if (value is null)
            return info.IsValueType ? info.DefaultValue : null;

        // Если уже нужного типа — возвращаем
        if (targetType.IsInstanceOfType(value))
            return value;

        // JsonElement — конвертируем через System.Text.Json
        if (value is JsonElement element)
        {
            try { return element.Deserialize(targetType); }
            catch { return info.IsValueType ? info.DefaultValue : null; }
        }

        // Примитивные конвертации
        try { return Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType); }
        catch { return info.IsValueType ? info.DefaultValue : null; }
    }

    private SnapshotPropertyInfo[] GetSnapshotProperties()
        => _cache.GetOrAdd(GetType(), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(p => p.GetCustomAttribute<SnapshotAttribute>() is not null
                         && p.CanRead && p.CanWrite
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