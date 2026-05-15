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
    /// Регистрирует функцию сохранения состояния перед pausin компонента.
    /// </summary>
    /// <typeparam name="T">Тип данных.</typeparam>
    /// <param name="pcs">Экземпляр <see cref="PersistentComponentState"/>.</param>
    /// <param name="key">Уникальный ключ (рекомендуется: тип + discriminator).</param>
    /// <param name="getter">Функция, возвращающая текущее значение для сохранения.</param>
    /// <returns>
    /// <see cref="IDisposable"/> — subscription. Dispose вызывается в <c>DisposeAsync</c> компонента.
    /// </returns>
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
    /// <typeparam name="T">Тип данных.</typeparam>
    /// <param name="pcs">Экземпляр <see cref="PersistentComponentState"/>.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="value">Восстановленное значение или <c>default</c>.</param>
    /// <returns><c>true</c>, если значение найдено и восстановлено.</returns>
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
    /// <typeparam name="T">Тип данных.</typeparam>
    /// <param name="pcs">Экземпляр <see cref="PersistentComponentState"/>.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="compute">Функция создания значения (вызывается только если нет персистентного).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Восстановленное или вновь созданное значение.</returns>
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
}