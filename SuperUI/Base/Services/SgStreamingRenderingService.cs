// SuperUI/Base/Services/SgStreamingRenderingService.cs
// НОВЫЙ: поддержка Streaming Rendering (.NET 8+)
//
// Позволяет компонентам использовать механизм streaming-рендеринга:
//   1. Сразу рендерится placeholder (LoadingContent)
//   2. После завершения загрузки данных — полный контент

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для работы со Streaming Rendering (.NET 8+).
/// Позволяет компонентам постепенно отдавать контент по мере загрузки данных.
/// </summary>
public interface IStreamingRenderingService
{
    /// <summary>
    /// Зарегистрировать промис для streaming-рендеринга.
    /// Компонент отрендерится дважды:
    ///   1. Сразу с placeholder (LoadingContent)
    ///   2. После завершения Task — с полными данными
    /// </summary>
    Task StreamAsync<T>(Task<T> dataTask, Action<T> onComplete, Action? onPlaceholder = null);

    /// <summary>Включён ли streaming rendering для текущего запроса.</summary>
    bool IsStreamingEnabled { get; }
}

public sealed class SgStreamingRenderingService : IStreamingRenderingService, IDisposable
{
    private readonly List<Task> _pendingStreams = new();
    private readonly CancellationTokenSource _cts = new();

    public bool IsStreamingEnabled { get; set; } = true;

    public async Task StreamAsync<T>(
        Task<T> dataTask,
        Action<T> onComplete,
        Action? onPlaceholder = null)
    {
        ArgumentNullException.ThrowIfNull(dataTask);
        ArgumentNullException.ThrowIfNull(onComplete);

        if (!IsStreamingEnabled)
        {
            var data = await dataTask;
            onComplete(data);
            return;
        }

        // 1. Сразу показываем placeholder
        onPlaceholder?.Invoke();

        // 2. Регистрируем продолжение
        var continuation = dataTask.ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully && !_cts.IsCancellationRequested)
                onComplete(t.Result);
        }, _cts.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);

        _pendingStreams.Add(continuation);
    }

    public void Dispose() => _cts.Dispose();
}