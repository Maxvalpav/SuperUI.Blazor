// SuperUI/Base/SgSnapshotComponentBase.cs
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
/// ИСПРАВЛЕНО: Reflection кэшируется статически per-type.
/// Нет повторного вызова GetProperties при каждом CaptureSnapshot/RestoreSnapshot.
/// </summary>
public abstract class SgSnapshotComponentBase : SgInteractiveBase, ISnapshotable
{
    // ИСПРАВЛЕНО: статический кэш PropertyInfo[] per Type — zero-reflection при runtime
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _snapshotPropsCache = new();

    [Inject] private ISessionStorage SessionStorage { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var snapshot = await SessionStorage.GetItemAsync<Dictionary<string, object>>(ComponentId);
        if (snapshot != null) RestoreSnapshot(snapshot);
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

    public void RestoreSnapshot(object? snapshot)
    {
        if (snapshot is not Dictionary<string, object?> dict) return;
        var props = GetSnapshotProperties();
        foreach (var prop in props)
        {
            if (dict.TryGetValue(prop.Name, out var value))
            {
                try { prop.SetValue(this, value); }
                catch (Exception) { /* тип не совпал — игнорируем */ }
            }
        }
    }

    private PropertyInfo[] GetSnapshotProperties()
        => _snapshotPropsCache.GetOrAdd(GetType(), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(p => p.GetCustomAttribute<SnapshotAttribute>() != null && p.CanRead && p.CanWrite)
             .ToArray());
}
