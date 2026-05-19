// SuperUI/Base/Utilities/SgJsModuleCache.cs
// Scoped-кеш JS-модулей: один IJSObjectReference на (цикл, путь к модулю).
// Решает проблему 10 модалов = 10 import() на один файл.
// Регистрируется в DI через AddSuperUI().

using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Scoped-кеш <see cref="IJSObjectReference"/> для JS ES-модулей.
/// </summary>
/// <remarks>
/// <para>Гарантирует, что каждый путь к модулю загружается один раз
/// на жизненный цикл Blazor-цикла (SignalR circuit или WASM session).</para>
/// <para>Параллельные вызовы <see cref="GetAsync"/> с одним путём коалесцируют:
/// только один <c>import()</c> уходит в браузер, остальные ждут его результата.</para>
/// <para><b>Владение:</b> кеш владеет <see cref="IJSObjectReference"/>-объектами
/// и освобождает их в <see cref="DisposeAsync"/>. Компоненты НЕ должны
/// вызывать <c>DisposeAsync</c> на полученном модуле.</para>
/// </remarks>
public sealed class SgJsModuleCache : IAsyncDisposable
{
    // Task<IJSObjectReference> — coalescing: параллельные GetAsync получат один Task.
    // ConcurrentDictionary: lock-free чтение в fast-path, GetOrAdd с фабрикой для slow-path.
    private readonly ConcurrentDictionary<string, Task<IJSObjectReference>> _cache = new();
    private bool _disposed;

    /// <summary>
    /// Возвращает <see cref="IJSObjectReference"/> для указанного пути к модулю.
    /// При первом вызове — импортирует модуль; при повторных — возвращает кешированный.
    /// </summary>
    /// <param name="js">Экземпляр <see cref="IJSRuntime"/>.</param>
    /// <param name="path">Путь к модулю (e.g. <c>"./_content/SuperUI/superui-modal.js"</c>).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ObjectDisposedException">Кеш уже освобождён.</exception>
    public async ValueTask<IJSObjectReference> GetAsync(
        IJSRuntime js,
        string path,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(js);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // GetOrAdd с lambda гарантирует, что параллельные вызовы получат один и тот же
        // Task<IJSObjectReference> (хотя сама фабрика теоретически может быть вызвана
        // дважды — для import() это безопасно, потому что лишний import просто пропадёт
        // вместе с проигравшим Task'ом).
        var task = _cache.GetOrAdd(path, p =>
            js.InvokeAsync<IJSObjectReference>("import", p).AsTask());

        try
        {
            return await task.WaitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // При ошибке — удаляем из кеша только если в нём всё ещё лежит наш упавший
            // Task (другой поток мог уже подложить рабочий).
            ((ICollection<KeyValuePair<string, Task<IJSObjectReference>>>)_cache)
                .Remove(new KeyValuePair<string, Task<IJSObjectReference>>(path, task));
            throw;
        }
    }

    /// <summary>
    /// Удаляет модуль из кеша (например, при hotreload).
    /// </summary>
    /// <param name="path">Путь к модулю.</param>
    public async ValueTask InvalidateAsync(string path)
    {
        if (_cache.TryRemove(path, out var task) && task.IsCompletedSuccessfully)
        {
            try { await (await task).DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
        }
    }

    /// <summary>
    /// Освобождает все кешированные JS-модули.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var task in _cache.Values)
        {
            if (!task.IsCompletedSuccessfully) continue;
            try { await (await task).DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
        }
        _cache.Clear();
    }
}