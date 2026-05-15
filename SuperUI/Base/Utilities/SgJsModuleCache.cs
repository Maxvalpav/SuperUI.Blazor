// SuperUI/Base/Utilities/SgJsModuleCache.cs
// Scoped-кеш JS-модулей: один IJSObjectReference на (цикл, путь к модулю).
// Решает проблему 10 модалов = 10 import() на один файл.
// Регистрируется в DI через AddSuperUI().

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
    private readonly Dictionary<string, Task<IJSObjectReference>> _cache = new();
    private readonly SemaphoreSlim _sem = new(1, 1);
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

        // Fast path без блокировки (кеш уже содержит успешный Task).
        if (_cache.TryGetValue(path, out var existing) && existing.IsCompletedSuccessfully)
        {
            return await existing;
        }

        // Slow path с блокировкой для coalescing параллельных вызовов.
        await _sem.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(path, out existing))
            {
                return await existing;
            }

            var importTask = js.InvokeAsync<IJSObjectReference>("import", path)
                               .AsTask();
            _cache[path] = importTask;
            return await importTask;
        }
        catch
        {
            // При ошибке — удаляем из кеша, чтобы следующий вызов попробовал снова.
            _cache.Remove(path);
            throw;
        }
        finally
        {
            _sem.Release();
        }
    }

    /// <summary>
    /// Удаляет модуль из кеша (например, при hotreload).
    /// </summary>
    /// <param name="path">Путь к модулю.</param>
    public async ValueTask InvalidateAsync(string path)
    {
        await _sem.WaitAsync();
        try
        {
            if (_cache.Remove(path, out var task) && task.IsCompletedSuccessfully)
            {
                try { await (await task).DisposeAsync(); }
                catch (JSDisconnectedException) { }
                catch (TaskCanceledException) { }
            }
        }
        finally
        {
            _sem.Release();
        }
    }

    /// <summary>
    /// Освобождает все кешированные JS-модули.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _sem.WaitAsync();
        try
        {
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
        finally
        {
            _sem.Release();
            _sem.Dispose();
        }
    }
}