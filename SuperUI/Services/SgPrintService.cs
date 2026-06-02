// SuperUI/Services/SgPrintService.cs
// Утилита для window.print() с возможностью скрыть/показать элементы перед печатью.

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис печати: window.print() с пред-/пост-обработкой DOM.
/// </summary>
/// <remarks>
/// <para>Метод <see cref="PrintAsync(string?, string?)"/> позволяет временно
/// пометить элементы по CSS-селектору как видимые/скрытые и сбросить состояние
/// после печати.</para>
/// </remarks>
public sealed class SgPrintService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;

    public SgPrintService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>Вызывает window.print().</summary>
    public async ValueTask PrintAsync(CancellationToken ct = default)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.print").ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    /// <summary>
    /// Печатает, временно скрывая элементы по <paramref name="hideSelector"/>
    /// и показывая элементы по <paramref name="showSelector"/>.
    /// </summary>
    public async ValueTask PrintAsync(string? hideSelector, string? showSelector, CancellationToken ct = default)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.printWithToggles", hideSelector, showSelector).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Volatile.Write(ref _disposed, 1);
        return ValueTask.CompletedTask;
    }
}
