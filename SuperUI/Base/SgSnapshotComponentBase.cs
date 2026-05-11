using System.Reflection;
using Microsoft.AspNetCore.Components;
using SuperUI.Services;

namespace SuperUI.Base;

/// <summary>
/// Интерфейс для компонентов, поддерживающих сохранение/восстановление состояния.
/// Позволяет реализовать hot reload безопасное сохранение состояния.
/// </summary>
public interface ISnapshotable
{
    object? CaptureSnapshot();
    void RestoreSnapshot(object? snapshot);
}

/// <summary>
/// Атрибут для пометки полей/свойств, которые нужно сохранять в snapshot.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SnapshotAttribute : Attribute { }

/// <summary>
/// Базовый класс для компонентов с поддержкой snapshot state.
/// При hot reload / reconnect состояние не теряется.
/// </summary>
public abstract class SgSnapshotComponentBase : SgInteractiveBase, ISnapshotable
{
    [Inject] private ISessionStorage SessionStorage { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var snapshot = await SessionStorage.GetItemAsync<Dictionary<string, object?>>(ComponentId);
        if (snapshot != null) RestoreSnapshot(snapshot);
        await base.OnInitializedAsync();
    }

    public object? CaptureSnapshot()
    {
        return GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<SnapshotAttribute>() != null && p.CanRead)
            .ToDictionary(p => p.Name, p => p.GetValue(this));
    }

    public void RestoreSnapshot(object? snapshot)
    {
        if (snapshot is not Dictionary<string, object?> dict) return;
        foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<SnapshotAttribute>() != null && p.CanWrite))
        {
            if (dict.TryGetValue(prop.Name, out var value))
                prop.SetValue(this, value);
        }
    }
}