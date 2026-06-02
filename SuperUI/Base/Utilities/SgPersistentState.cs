// SuperUI/Base/Utilities/SgPersistentState.cs
// Вспомогательные методы для PersistentComponentState (.NET 8+).
// Позволяет Streaming SSR сохранять данные, которые Interactive-клиент
// подхватывает без повторного запроса к серверу.

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Утилиты для работы с <see cref="PersistentComponentState"/> в SuperUI.
/// </summary>
/// <remarks>
/// <para>Сценарий использования — Streaming Rendering + InteractiveAuto:</para>
/// <code>
/// // В OnInitializedAsync:
/// var data = await SgPersistentState.TakeOrCreateAsync(
///     PersistentState, "my-data",
///     ct => FetchDataAsync(ct),
///     cancellationToken);
/// </code>
/// </remarks>
public static class SgPersistentState
{
    /// <summary>
    /// Регистрирует функцию сохранения состояния перед pausing компонента.
    /// </summary>
    public static IDisposable Register<T>(
        PersistentComponentState pcs,
        string key,
        Func<T?> getter)
    {
        ArgumentNullException.ThrowIfNull(pcs);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(getter);

        return pcs.RegisterOnPersisting(() =>
        {
            var value = getter();
            if (value is not null)
            {
                pcs.PersistAsJson(key, value);
            }
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Пытается восстановить значение из персистентного состояния.
    /// </summary>
    public static bool TryTake<T>(
        PersistentComponentState pcs,
        string key,
        out T? value)
    {
        ArgumentNullException.ThrowIfNull(pcs);
        return pcs.TryTakeFromJson<T>(key, out value);
    }

    /// <summary>
    /// Восстанавливает значение из персистентного состояния или создаёт его заново.
    /// </summary>
    public static async ValueTask<T?> TakeOrCreateAsync<T>(
        PersistentComponentState pcs,
        string key,
        Func<CancellationToken, ValueTask<T?>> compute,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pcs);
        ArgumentNullException.ThrowIfNull(compute);

        if (pcs.TryTakeFromJson<T>(key, out var persisted))
        {
            return persisted;
        }

        return await compute(ct);
    }

    /// <summary>
    /// Регистрирует callback, который вызывается после restore из PersistentState.
    /// Используйте, чтобы инициализировать C# state на основе восстановленных данных.
    /// </summary>
    public static void OnRestored<T>(
        PersistentComponentState pcs,
        string key,
        Action<T> onRestored)
    {
        ArgumentNullException.ThrowIfNull(pcs);
        ArgumentNullException.ThrowIfNull(onRestored);
        if (pcs.TryTakeFromJson<T>(key, out var value) && value is not null)
        {
            onRestored(value);
        }
    }
}
