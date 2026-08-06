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
    // Lazy<Task<IJSObjectReference>> — coalescing: GetOrAdd может дважды вызвать фабрику при
    // гонке, но Lazy откладывает выполнение до первого .Value, поэтому проигравший экземпляр
    // так и не импортирует модуль (нет утечки IJSObjectReference) — победитель импортирует один раз.
    private readonly ConcurrentDictionary<string, Lazy<Task<IJSObjectReference>>> _cache = new();
    private int _disposed;

    /// <summary>
    /// Возвращает <see cref="IJSObjectReference"/> для указанного пути к модулю.
    /// При первом вызове — импортирует модуль; при повторных — возвращает кешированный.
    /// </summary>
    public async ValueTask<IJSObjectReference> GetAsync(
        IJSRuntime js,
        string path,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
        ArgumentNullException.ThrowIfNull(js);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Lazy откладывает import до .Value. При гонке GetOrAdd может создать два Lazy, но
        // проигравший не будет выполнен — только победитель импортирует, без утечки ресурсов.
        var lazy = _cache.GetOrAdd(path, p =>
            new Lazy<Task<IJSObjectReference>>(
                () => js.InvokeAsync<IJSObjectReference>("import", p).AsTask(),
                isThreadSafe: true));

        try
        {
            return await lazy.Value.WaitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // При ошибке — удаляем из кеша только если в нём всё ещё лежит наш упавший
            // Lazy (другой поток мог уже подложить рабочий).
            ((ICollection<KeyValuePair<string, Lazy<Task<IJSObjectReference>>>>)_cache)
                .Remove(new KeyValuePair<string, Lazy<Task<IJSObjectReference>>>(path, lazy));
            throw;
        }
    }

    /// <summary>
    /// Удаляет модуль из кеша (например, при hotreload).
    /// </summary>
    public async ValueTask InvalidateAsync(string path)
    {
        if (_cache.TryRemove(path, out var lazy) &&
            lazy.Value is { IsCompletedSuccessfully: true } task)
        {
            try { await (await task).DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
        }
    }

    /// <summary>
    /// Возвращает количество кешированных модулей (для тестов и диагностики).
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Освобождает все кешированные JS-модули.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        foreach (var lazy in _cache.Values)
        {
            if (lazy.Value is not { IsCompletedSuccessfully: true } task) continue;
            try { await (await task).DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
        }
        _cache.Clear();
    }
}
