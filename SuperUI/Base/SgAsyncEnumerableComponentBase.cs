// SuperUI/Base/SgAsyncEnumerableComponentBase.cs
// УЛУЧШЕНИЕ: добавлена поддержка IAsyncEnumerable<T> streaming
// Дополняет существующий SgStreamingComponentBase

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

/// <summary>
/// Расширение SgStreamingComponentBase с поддержкой IAsyncEnumerable&lt;T&gt;.
/// Позволяет постепенно выводить данные по мере их поступления.
/// </summary>
public abstract class SgAsyncEnumerableComponentBase<T> : SgInteractiveBase
{
    [Parameter] public RenderFragment<T>? ItemTemplate { get; set; }
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    protected List<T> StreamedItems { get; } = new();
    protected bool IsStreaming { get; private set; }
    protected bool IsStreamDone { get; private set; }
    protected Exception? StreamException { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await StreamDataAsync();
    }

    private async Task StreamDataAsync()
    {
        IsStreaming = true;
        StreamedItems.Clear();
        StreamException = null;

        try
        {
            await foreach (var item in GetDataStreamAsync(ComponentToken))
            {
                if (IsDisposed || ComponentToken.IsCancellationRequested) break;
                StreamedItems.Add(item);

                // В режиме Streaming SSR — каждый await позволяет серверу отправить
                // следующую часть HTML клиенту
                await InvokeAsync(StateHasChanged);
            }
            IsStreamDone = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StreamException = ex;
            Logger.LogError(ex, "[{Id}] AsyncEnumerable stream error", ComponentId);
        }
        finally
        {
            IsStreaming = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Предоставить IAsyncEnumerable источник данных.</summary>
    protected abstract IAsyncEnumerable<T> GetDataStreamAsync(CancellationToken ct);

    /// <summary>Перезапустить стриминг.</summary>
    public Task RefreshStreamAsync() => StreamDataAsync();
}
