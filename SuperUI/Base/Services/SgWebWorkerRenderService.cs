// SuperUI/Base/Services/SgWebWorkerRenderService.cs
// 🆕 Вынос тяжёлых вычислений в Web Worker (.NET 8+ WASM).
// Использует dedicated Web Worker для вычислений вне UI потока.
// Ни у кого нет.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Services;

/// <summary>
/// Result from a Web Worker computation.
/// </summary>
public sealed class WorkerResult<T>
{
    public T? Value { get; init; }
    public double DurationMs { get; init; }
    public bool IsCancelled { get; init; }
    public Exception? Error { get; init; }
    public bool Success => Error == null && !IsCancelled;
}

/// <summary>
/// Service for offloading heavy computations to a Web Worker.
/// Prevents UI thread blocking in WASM.
///
/// Usage:
/// var result = await _workerService.ExecuteInWorkerAsync("computeSomething", data);
/// </summary>
public sealed class SgWebWorkerRenderService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly ILogger<SgWebWorkerRenderService> _logger;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingCalls = new();
    private IJSObjectReference? _workerModule;
    private DotNetObjectReference<SgWebWorkerRenderService>? _dotNetRef;
    private int _callCounter;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public SgWebWorkerRenderService(IJSRuntime js, ILogger<SgWebWorkerRenderService> logger)
    {
        _js = js;
        _logger = logger;
    }

    /// <summary>
    /// Initialize the Web Worker.
    /// </summary>
    public async Task InitializeAsync(string workerScriptPath = "/_content/SuperUI/js/worker.js")
    {
        if (_isInitialized) return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _workerModule = await _js.InvokeAsync<IJSObjectReference>("import", workerScriptPath);
            await _workerModule.InvokeVoidAsync("initWorker", _dotNetRef);
            _isInitialized = true;
            _logger.LogInformation("[WebWorker] Initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WebWorker] Initialization failed");
            throw;
        }
    }

    /// <summary>
    /// Execute a computation in the Web Worker.
    /// </summary>
    public async Task<WorkerResult<T>> ExecuteInWorkerAsync<T>(string operationName,
        object? data = null,
        CancellationToken ct = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("WebWorker not initialized. Call InitializeAsync first.");

        var callId = Interlocked.Increment(ref _callCounter).ToString();
        var tcs = new TaskCompletionSource<string>();
        _pendingCalls[callId] = tcs;

        try
        {
            var payload = new
            {
                callId,
                operation = operationName,
                data
            };

            await _workerModule!.InvokeVoidAsync("executeInWorker", ct, payload);

            // Wait for result with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

            var resultJson = await tcs.Task.WaitAsync(timeoutCts.Token);
            var value = System.Text.Json.JsonSerializer.Deserialize<T>(resultJson);

            return new WorkerResult<T> { Value = value, DurationMs = 0, IsCancelled = false };
        }
        catch (OperationCanceledException)
        {
            return new WorkerResult<T> { IsCancelled = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WebWorker] ExecuteInWorkerAsync failed for {Operation}", operationName);
            return new WorkerResult<T> { Error = ex };
        }
        finally
        {
            _pendingCalls.TryRemove(callId, out _);
        }
    }

    /// <summary>
    /// Callback from JavaScript when worker completes.
    /// </summary>
    [JSInvokable("WorkerCallback")]
    public void OnWorkerCallback(string callId, string resultJson, string? errorJson)
    {
        if (_pendingCalls.TryGetValue(callId, out var tcs))
        {
            if (errorJson != null)
            {
                tcs.TrySetException(new InvalidOperationException(errorJson));
            }
            else
            {
                tcs.TrySetResult(resultJson);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_workerModule != null)
        {
            try
            {
                await _workerModule.InvokeVoidAsync("terminateWorker");
                await _workerModule.DisposeAsync();
            }
            catch { }

            _workerModule = null;
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;

        foreach (var tcs in _pendingCalls.Values)
        {
            tcs.TrySetCanceled();
        }

        _pendingCalls.Clear();
    }
}
