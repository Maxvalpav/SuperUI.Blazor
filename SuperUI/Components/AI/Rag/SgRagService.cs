using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace SuperUI.Components;

/// <summary>
/// Scoped service that owns the JS bridge for the RAG stack.
/// Injected automatically by <see cref="SgRagProvider"/> and consumed by child components.
/// </summary>
public sealed class SgRagService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SgRagService>? _selfRef;
    private string? _instanceId;
    private bool _isDisposed;

    // Per-stream routing for AskStreamAsync/ChatDirectStreamAsync.
    // Replaces the legacy service-wide multicast (OnStreamToken/OnStreamComplete/OnError)
    // which mixed tokens across concurrent streams.
    private readonly ConcurrentDictionary<string, StreamRouter> _streamRouters = new();

    private sealed class StreamRouter
    {
        public Channel<string> Tokens { get; } = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        public SgRagAnswer? Result { get; set; }
        public Exception? Error { get; set; }
    }

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>Current ready state of the RAG stack.</summary>
    public SgRagReadyState State { get; private set; } = new();

    // ── Events ────────────────────────────────────────────────────────────────

    public event Action<SgRagModelProgress>? OnEmbeddingProgress;
    public event Action<SgRagModelProgress>? OnLlmProgress;
    public event Action<SgRagIndexProgress>? OnIndexProgress;
    public event Action<string>? OnStreamToken;
    public event Action<SgRagAnswer>? OnStreamComplete;
    public event Action<string, string>? OnError;
    public event Action<SgRagReadyState>? OnStateChanged;

    public SgRagService(IJSRuntime js)
    {
        _js = js;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the JS bridge. Called by <see cref="SgRagProvider"/> on first render.
    /// </summary>
    public async Task InitAsync(SgRagOptions options, CancellationToken ct = default)
    {
        if (_module is not null) return;

        _instanceId = $"sg-rag-{Guid.NewGuid():N}";
        _module = await _js.InvokeAsync<IJSObjectReference>(
            "import", "./_content/SuperUI/sg-rag.js");

        _selfRef = DotNetObjectReference.Create(this);

        var jsOpts = BuildJsOptions(options);
        await _module.InvokeVoidAsync("init", ct, _selfRef, _instanceId, jsOpts);

        var webGpu = await CheckWebGpuAsync(ct);
        State = State with { WebGpuAvailable = webGpu, DbReady = options.PersistToIndexedDb };
        NotifyStateChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_module is not null && _instanceId is not null)
        {
            try { await _module.InvokeVoidAsync("dispose", _instanceId); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch { }
        }

        var selfRef = _selfRef;
        _selfRef = null;
        selfRef?.Dispose();

        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch { }
            _module = null;
        }
    }

    // ── Models ────────────────────────────────────────────────────────────────

    /// <summary>Checks whether WebGPU is available in the current browser.</summary>
    public async Task<bool> CheckWebGpuAsync(CancellationToken ct = default)
    {
        if (_module is null) return false;
        try
        {
            var result = await _module.InvokeAsync<System.Text.Json.JsonElement>("checkWebGpu", ct);
            return result.TryGetProperty("available", out var av) && av.GetBoolean();
        }
        catch { return false; }
    }

    /// <summary>Loads the embedding model.</summary>
    public async Task LoadEmbeddingModelAsync(
        SgRagEmbeddingModelKind kind,
        SgRagQuantization quantization,
        IProgress<SgRagModelProgress>? progress = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        var modelId = ResolveEmbeddingModelId(kind, null);
        var dtype = quantization.ToString().ToLowerInvariant();

        await _module!.InvokeVoidAsync("loadEmbeddingModel", ct,
            _instanceId, kind.ToString(), modelId, dtype);

        State = State with { EmbeddingReady = true, EmbeddingModel = modelId };
        NotifyStateChanged();
    }

    /// <summary>Loads the LLM.</summary>
    public async Task LoadLlmAsync(
        SgRagLlmProviderKind provider,
        string? modelId,
        string? apiKey = null,
        string? baseUrl = null,
        IProgress<SgRagModelProgress>? progress = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        // Pass credentials directly so JS has them at call time,
        // overriding whatever was set in init() options.
        var overrides = new
        {
            apiKey  = apiKey,
            baseUrl = baseUrl,
        };
        await _module!.InvokeVoidAsync("loadLlm", ct, _instanceId, provider.ToString(), modelId, overrides);
        State = State with { LlmReady = true, LlmModel = modelId, LlmProvider = provider };
        NotifyStateChanged();
    }

    /// <summary>Unloads the embedding model to free memory.</summary>
    public async Task UnloadEmbeddingAsync(CancellationToken ct = default)
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("unloadEmbedding", ct, _instanceId);
        State = State with { EmbeddingReady = false, EmbeddingModel = null };
        NotifyStateChanged();
    }

    /// <summary>Unloads the LLM to free memory.</summary>
    public async Task UnloadLlmAsync(CancellationToken ct = default)
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("unloadLlm", ct, _instanceId);
        State = State with { LlmReady = false, LlmModel = null };
        NotifyStateChanged();
    }

    /// <summary>Loads the cross-encoder reranker model.</summary>
    public async Task LoadRerankerAsync(
        string modelId = "Xenova/ms-marco-MiniLM-L-6-v2",
        IProgress<SgRagModelProgress>? progress = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        await _module!.InvokeVoidAsync("loadReranker", ct, _instanceId, modelId);
    }

    /// <summary>Unloads the reranker model to free memory.</summary>
    public async Task UnloadRerankerAsync(CancellationToken ct = default)
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("unloadReranker", ct, _instanceId);
    }

    /// <summary>Reranks search hits using the cross-encoder.</summary>
    public async Task<IReadOnlyList<SgRagSearchHit>> RerankAsync(
        string query,
        IReadOnlyList<SgRagSearchHit> hits,
        int topN = 5,
        CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "rerank", ct, _instanceId, query, hits, topN);
        return ParseSearchHits(raw);
    }

    /// <summary>Exports chat history to the specified format.</summary>
    public async Task<SgRagChatExportResult> ExportChatAsync(
        string format,
        IReadOnlyList<SgRagChatMessage> messages,
        CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "exportChat", ct, _instanceId, format, messages);
        return new SgRagChatExportResult
        {
            Content = raw.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "",
            ContentType = raw.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
            Extension = raw.TryGetProperty("extension", out var e) ? e.GetString() ?? "" : ""
        };
    }

    // ── Documents / Chunking ──────────────────────────────────────────────────

    /// <summary>Ingests a browser file into the specified collection.</summary>
    public async Task<SgRagDocumentIngestResult> IngestFileAsync(
        IBrowserFile file,
        string collection = "default",
        SgRagChunkingOptions? chunking = null,
        IProgress<SgRagIndexProgress>? progress = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        var docId = Guid.NewGuid().ToString("N");
        var opts = chunking ?? new SgRagChunkingOptions();

        // Read file bytes and pass as base64
        using var stream = file.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024);
        using var ms = new System.IO.MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var base64 = Convert.ToBase64String(ms.ToArray());

        var result = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "ingestFile", ct,
            _instanceId, base64, file.Name, file.ContentType, collection, BuildChunkOpts(opts), docId);

        return ParseIngestResult(result);
    }

    /// <summary>Ingests plain text into the specified collection.</summary>
    public async Task<SgRagDocumentIngestResult> IngestTextAsync(
        string title,
        string text,
        SgRagDocumentFormat format = SgRagDocumentFormat.Txt,
        string collection = "default",
        SgRagChunkingOptions? chunking = null,
        IProgress<SgRagIndexProgress>? progress = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        var docId = Guid.NewGuid().ToString("N");
        var opts = chunking ?? new SgRagChunkingOptions();

        var result = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "ingestText", ct,
            _instanceId, title, text, format.ToString(), collection, BuildChunkOpts(opts), docId);

        return ParseIngestResult(result);
    }

    /// <summary>Returns a preview of how text would be chunked with the given options.</summary>
    public async Task<IReadOnlyList<SgRagChunk>> PreviewChunksAsync(
        string text,
        SgRagChunkingOptions chunking,
        CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "previewChunks", ct, _instanceId, text, BuildChunkOpts(chunking));
        return ParseChunks(raw);
    }

    /// <summary>Removes a document and its chunks/vectors from the index.</summary>
    public async Task RemoveDocumentAsync(string documentId, CancellationToken ct = default)
    {
        EnsureReady();
        await _module!.InvokeVoidAsync("removeDocument", ct, _instanceId, documentId);
    }

    /// <summary>Re-embeds a document with new chunking options.</summary>
    public async Task ReindexDocumentAsync(
        string documentId,
        SgRagChunkingOptions? chunking = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        await _module!.InvokeVoidAsync("reindexDocument", ct,
            _instanceId, documentId, BuildChunkOpts(chunking ?? new()));
    }

    // ── Collections / DB ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SgRagCollectionInfo>> ListCollectionsAsync(CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>("listCollections", ct, _instanceId);
        return ParseCollections(raw);
    }

    public async Task CreateCollectionAsync(string name, CancellationToken ct = default)
    {
        EnsureReady();
        await _module!.InvokeVoidAsync("createCollection", ct, _instanceId, name);
    }

    public async Task DeleteCollectionAsync(string name, CancellationToken ct = default)
    {
        EnsureReady();
        await _module!.InvokeVoidAsync("deleteCollection", ct, _instanceId, name);
    }

    public async Task<IReadOnlyList<SgRagDocument>> ListDocumentsAsync(
        string collection = "default",
        CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "listDocuments", ct, _instanceId, collection);
        return ParseDocuments(raw);
    }

    public async Task<SgRagDocument?> GetDocumentAsync(string documentId, CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "getDocument", ct, _instanceId, documentId);
        return ParseDocument(raw);
    }

    public async Task ClearCollectionAsync(string collection = "default", CancellationToken ct = default)
    {
        EnsureReady();
        await _module!.InvokeVoidAsync("clearCollection", ct, _instanceId, collection);
    }

    // ── Search / RAG ──────────────────────────────────────────────────────────

    /// <summary>Performs semantic search without LLM generation.</summary>
    public async Task<IReadOnlyList<SgRagSearchHit>> SearchAsync(
        string query,
        string collection = "default",
        int topK = 10,
        double minScore = 0.0,
        CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "search", ct, _instanceId, query, collection, topK, minScore);
        return ParseSearchHits(raw);
    }

    /// <summary>Asks a question and returns a complete answer (non-streaming).</summary>
    public async Task<SgRagAnswer> AskAsync(
        string question,
        string collection = "default",
        int topK = 5,
        string? systemPrompt = null,
        SgRagAnswerMode mode = SgRagAnswerMode.Strict,
        CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "ask", ct, _instanceId, question, collection, topK, systemPrompt, mode.ToString());
        return ParseAnswer(raw);
    }

    /// <summary>Asks a question and streams the answer token by token.</summary>
    public IAsyncEnumerable<string> AskStreamAsync(
        string question,
        string collection = "default",
        int topK = 5,
        string? systemPrompt = null,
        SgRagAnswerMode mode = SgRagAnswerMode.Strict,
        CancellationToken ct = default)
    {
        EnsureReady();
        return StreamCoreAsync(
            streamId => _module!.InvokeVoidAsync(
                "askStream", CancellationToken.None,
                _instanceId, question, collection, topK, systemPrompt, mode.ToString(), streamId).AsTask(),
            ct);
    }

    // ── Direct chat (no RAG) ──────────────────────────────────────────────────

    /// <summary>
    /// Streams a direct LLM response without any document retrieval.
    /// Maintains conversation history for multi-turn context.
    /// </summary>
    public IAsyncEnumerable<string> ChatDirectStreamAsync(
        string message,
        string? systemPrompt = null,
        IEnumerable<object>? attachments = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        var attachmentList = attachments?.ToList();
        return StreamCoreAsync(
            streamId => _module!.InvokeVoidAsync(
                "chatDirectStream", CancellationToken.None,
                _instanceId, message, systemPrompt, attachmentList, streamId).AsTask(),
            ct);
    }

    // Shared core for both streaming endpoints. JS pushes tokens into a per-stream
    // Channel via OnStreamTokenForCallback(streamId, token) — no service-wide multicast,
    // no busy-wait polling. Cancellation aborts only this stream's JS run.
    private async IAsyncEnumerable<string> StreamCoreAsync(
        Func<string, Task> startJsCall,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var streamId = Guid.NewGuid().ToString("N");
        var router = new StreamRouter();
        _streamRouters[streamId] = router;

        Task? jsTask = null;
        try
        {
            jsTask = startJsCall(streamId).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    CompleteStream(streamId, error: t.Exception?.InnerException ?? t.Exception);
                // Successful JS task end is signalled by OnStreamCompleteCallback.
            }, TaskScheduler.Default);

            using var ctReg = ct.Register(() =>
            {
                // Tell JS to abort this specific stream.
                try { _ = _module?.InvokeVoidAsync("cancelStream", CancellationToken.None, _instanceId, streamId); }
                catch { }
                CompleteStream(streamId, cancelled: true);
            });

            await foreach (var token in router.Tokens.Reader.ReadAllAsync(CancellationToken.None))
            {
                yield return token;
            }

            ct.ThrowIfCancellationRequested();
            if (router.Error is not null) throw router.Error;
        }
        finally
        {
            _streamRouters.TryRemove(streamId, out _);
            if (jsTask is not null)
            {
                try { await jsTask.ConfigureAwait(false); } catch { }
            }
        }
    }

    private void CompleteStream(string streamId, SgRagAnswer? result = null, Exception? error = null, bool cancelled = false)
    {
        if (!_streamRouters.TryGetValue(streamId, out var router)) return;
        if (result is not null) router.Result = result;
        if (error is not null) router.Error = error;
        if (cancelled && router.Error is null)
            router.Error = new OperationCanceledException("Stream was cancelled.");
        router.Tokens.Writer.TryComplete();
    }

    /// <summary>Clears the direct chat conversation history.</summary>
    public async Task ClearDirectHistoryAsync(CancellationToken ct = default)
    {
        if (_module is null) return;
        try { await _module.InvokeVoidAsync("clearDirectHistory", ct, _instanceId); }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    /// <summary>Exports the database (or a single collection) as a JSON blob.</summary>
    public async Task<string> ExportAsync(string? collection = null, CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<string>("exportDb", ct, _instanceId, collection);
        return raw;
    }

    /// <summary>Imports a previously exported database blob.</summary>
    public async Task ImportAsync(string data, bool merge = false, CancellationToken ct = default)
    {
        EnsureReady();
        await _module!.InvokeVoidAsync("importDb", ct, _instanceId, data, merge);
    }

    /// <summary>Creates a named snapshot in IndexedDB.</summary>
    public async Task<SgRagSnapshotInfo> CreateSnapshotAsync(
        SgRagSnapshotKind kind = SgRagSnapshotKind.Manual,
        string? note = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "createSnapshot", ct, _instanceId, kind.ToString(), note);
        return ParseSnapshot(raw);
    }

    /// <summary>Lists all snapshots.</summary>
    public async Task<IReadOnlyList<SgRagSnapshotInfo>> ListSnapshotsAsync(CancellationToken ct = default)
    {
        EnsureReady();
        var raw = await _module!.InvokeAsync<System.Text.Json.JsonElement>(
            "listSnapshots", ct, _instanceId);
        return ParseSnapshots(raw);
    }

    /// <summary>Restores a snapshot by ID.</summary>
    public async Task RestoreSnapshotAsync(string snapshotId, CancellationToken ct = default)
    {
        EnsureReady();
        await _module!.InvokeVoidAsync("restoreSnapshot", ct, _instanceId, snapshotId);
    }

    // ── JS Callbacks ──────────────────────────────────────────────────────────

    [JSInvokable]
    public void OnEmbeddingProgressCallback(SgRagModelProgress progress)
    {
        OnEmbeddingProgress?.Invoke(progress);
    }

    [JSInvokable]
    public void OnLlmProgressCallback(SgRagModelProgress progress)
    {
        OnLlmProgress?.Invoke(progress);
        if (progress.IsComplete)
        {
            State = State with { LlmReady = true };
            NotifyStateChanged();
        }
    }

    [JSInvokable]
    public void OnIndexProgressCallback(SgRagIndexProgress progress)
    {
        OnIndexProgress?.Invoke(progress);
    }

    [JSInvokable]
    public void OnStreamTokenCallback(string token)
    {
        OnStreamToken?.Invoke(token);
    }

    [JSInvokable]
    public void OnStreamCompleteCallback(System.Text.Json.JsonElement answerJson)
    {
        var answer = ParseAnswer(answerJson);
        OnStreamComplete?.Invoke(answer);
    }

    [JSInvokable]
    public void OnErrorCallback(string code, string message)
    {
        OnError?.Invoke(code, message);
    }

    // ── Per-stream callbacks (used by AskStreamAsync / ChatDirectStreamAsync) ────
    // JS passes the streamId so tokens from concurrent streams never mix.

    [JSInvokable]
    public void OnStreamTokenForCallback(string streamId, string token)
    {
        if (_streamRouters.TryGetValue(streamId, out var router))
            router.Tokens.Writer.TryWrite(token);
    }

    [JSInvokable]
    public void OnStreamCompleteForCallback(string streamId, System.Text.Json.JsonElement answerJson)
    {
        var answer = ParseAnswer(answerJson);
        CompleteStream(streamId, result: answer);
    }

    [JSInvokable]
    public void OnStreamErrorForCallback(string streamId, string code, string message)
    {
        CompleteStream(streamId, error: new InvalidOperationException($"{code}: {message}"));
    }

    [JSInvokable]
    public void OnEmbeddingReadyCallback(string modelId)
    {
        State = State with { EmbeddingReady = true, EmbeddingModel = modelId };
        NotifyStateChanged();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsureReady()
    {
        if (_module is null || _instanceId is null)
            throw new InvalidOperationException("SgRagService is not initialized. Ensure SgRagProvider is in the component tree.");
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke(State);

    private static string ResolveEmbeddingModelId(SgRagEmbeddingModelKind kind, string? custom) => kind switch
    {
        SgRagEmbeddingModelKind.MiniLmL6V2 => "Xenova/all-MiniLM-L6-v2",
        SgRagEmbeddingModelKind.JinaBaseEn => "Xenova/jina-embeddings-v2-base-en",
        SgRagEmbeddingModelKind.BgeSmallEn => "Xenova/bge-small-en-v1.5",
        SgRagEmbeddingModelKind.Custom     => custom ?? throw new ArgumentException("CustomEmbeddingModel must be set"),
        _                                  => "Xenova/all-MiniLM-L6-v2",
    };

    private static object BuildJsOptions(SgRagOptions opts) => new
    {
        persistToIndexedDb  = opts.PersistToIndexedDb,
        indexedDbName       = opts.IndexedDbName,
        defaultCollection   = opts.DefaultCollection,
        similarityThreshold = opts.SimilarityThreshold,
        maxContextTokens    = opts.MaxContextTokens,
        openAiBaseUrl       = opts.OpenAiBaseUrl,
        openAiApiKey        = opts.OpenAiApiKey,
        openAiModel         = opts.OpenAiModel,
        openRouterReferer   = opts.OpenRouterReferer,
        openRouterTitle     = opts.OpenRouterTitle,
        sources = new
        {
            transformersScript = opts.Sources.TransformersScript,
            webLlmScript       = opts.Sources.WebLlmScript,
            pdfJsScript        = opts.Sources.PdfJsScript,
            pdfJsWorker        = opts.Sources.PdfJsWorker,
            mammothScript      = opts.Sources.MammothScript,
            markedScript       = opts.Sources.MarkedScript,
            idbScript          = opts.Sources.IdbScript,
        },
    };

    private static object BuildChunkOpts(SgRagChunkingOptions opts) => new
    {
        strategy   = opts.Strategy.ToString(),
        chunkSize  = opts.ChunkSize,
        overlap    = opts.Overlap,
        separators = opts.Separators,
        semanticSimilarityWindow = opts.SemanticSimilarityWindow,
    };

    // ── JSON Parsers ──────────────────────────────────────────────────────────

    private static SgRagDocumentIngestResult ParseIngestResult(System.Text.Json.JsonElement j) => new()
    {
        DocumentId = j.TryGetProperty("documentId", out var d) ? d.GetString() ?? "" : "",
        Title      = j.TryGetProperty("title",      out var t) ? t.GetString() ?? "" : "",
        ChunkCount = j.TryGetProperty("chunkCount", out var c) ? c.GetInt32()       : 0,
        Success    = j.TryGetProperty("success",    out var s) && s.GetBoolean(),
        Error      = j.TryGetProperty("error",      out var e) ? e.GetString()      : null,
    };

    private static IReadOnlyList<SgRagChunk> ParseChunks(System.Text.Json.JsonElement j)
    {
        var list = new List<SgRagChunk>();
        if (j.ValueKind != System.Text.Json.JsonValueKind.Array) return list;
        foreach (var item in j.EnumerateArray())
            list.Add(new SgRagChunk
            {
                Id         = item.TryGetProperty("id",    out var id)  ? id.GetString()  ?? "" : "",
                Index      = item.TryGetProperty("index", out var idx) ? idx.GetInt32()       : 0,
                Text       = item.TryGetProperty("text",  out var tx)  ? tx.GetString()  ?? "" : "",
                TokenCount = item.TryGetProperty("tokenCount", out var tc) ? tc.GetInt32() : 0,
            });
        return list;
    }

    private static IReadOnlyList<SgRagSearchHit> ParseSearchHits(System.Text.Json.JsonElement j)
    {
        var list = new List<SgRagSearchHit>();
        if (j.ValueKind != System.Text.Json.JsonValueKind.Array) return list;
        foreach (var item in j.EnumerateArray())
        {
            var chunk = item.TryGetProperty("chunk", out var ch) ? ParseChunkObj(ch) : new SgRagChunk();
            var doc   = item.TryGetProperty("document", out var dc) ? ParseDocumentObj(dc) : new SgRagDocument();
            var score = item.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0.0;
            list.Add(new SgRagSearchHit { Chunk = chunk, Document = doc, Score = score });
        }
        return list;
    }

    private static SgRagChunk ParseChunkObj(System.Text.Json.JsonElement j) => new()
    {
        Id         = j.TryGetProperty("id",         out var id)  ? id.GetString()  ?? "" : "",
        DocumentId = j.TryGetProperty("documentId", out var did) ? did.GetString() ?? "" : "",
        Index      = j.TryGetProperty("index",      out var idx) ? idx.GetInt32()       : 0,
        Text       = j.TryGetProperty("text",       out var tx)  ? tx.GetString()  ?? "" : "",
        TokenCount = j.TryGetProperty("tokenCount", out var tc)  ? tc.GetInt32()        : 0,
    };

    private static SgRagDocument ParseDocumentObj(System.Text.Json.JsonElement j) => new()
    {
        Id         = j.TryGetProperty("id",         out var id)  ? id.GetString()  ?? "" : "",
        Collection = j.TryGetProperty("collection", out var col) ? col.GetString() ?? "" : "",
        Title      = j.TryGetProperty("title",      out var t)   ? t.GetString()   ?? "" : "",
        Source     = j.TryGetProperty("source",     out var src) ? src.GetString() ?? "" : "",
        ChunkCount = j.TryGetProperty("chunkCount", out var cc)  ? cc.GetInt32()        : 0,
    };

    private static SgRagAnswer ParseAnswer(System.Text.Json.JsonElement j) => new()
    {
        Question         = j.TryGetProperty("question",         out var q)  ? q.GetString()  ?? "" : "",
        Answer           = j.TryGetProperty("answer",           out var a)  ? a.GetString()  ?? "" : "",
        PromptTokens     = j.TryGetProperty("promptTokens",     out var pt) ? pt.GetInt32()       : 0,
        CompletionTokens = j.TryGetProperty("completionTokens", out var ct2)? ct2.GetInt32()      : 0,
        DurationMs       = j.TryGetProperty("durationMs",       out var dm) ? dm.GetInt64()       : 0,
        Sources          = j.TryGetProperty("sources",          out var src) ? ParseSearchHits(src) : [],
    };

    private static SgRagSnapshotInfo ParseSnapshot(System.Text.Json.JsonElement j) => new()
    {
        Id        = j.TryGetProperty("id",        out var id)  ? id.GetString()  ?? "" : "",
        Note      = j.TryGetProperty("note",      out var n)   ? n.GetString()        : null,
        SizeBytes = j.TryGetProperty("sizeBytes", out var sb)  ? sb.GetInt64()        : 0,
    };

    private static IReadOnlyList<SgRagSnapshotInfo> ParseSnapshots(System.Text.Json.JsonElement j)
    {
        var list = new List<SgRagSnapshotInfo>();
        if (j.ValueKind != System.Text.Json.JsonValueKind.Array) return list;
        foreach (var item in j.EnumerateArray())
            list.Add(ParseSnapshot(item));
        return list;
    }

    private static IReadOnlyList<SgRagDocument> ParseDocuments(System.Text.Json.JsonElement j)
    {
        var list = new List<SgRagDocument>();
        if (j.ValueKind != System.Text.Json.JsonValueKind.Array) return list;
        foreach (var item in j.EnumerateArray())
            list.Add(ParseDocumentObj(item));
        return list;
    }

    private static SgRagDocument? ParseDocument(System.Text.Json.JsonElement j)
    {
        if (j.ValueKind == System.Text.Json.JsonValueKind.Null) return null;
        return ParseDocumentObj(j);
    }

    private static IReadOnlyList<SgRagCollectionInfo> ParseCollections(System.Text.Json.JsonElement j)
    {
        var list = new List<SgRagCollectionInfo>();
        if (j.ValueKind != System.Text.Json.JsonValueKind.Array) return list;
        foreach (var item in j.EnumerateArray())
            list.Add(new SgRagCollectionInfo
            {
                Name           = item.TryGetProperty("name",           out var n)  ? n.GetString()  ?? "" : "",
                VectorDim      = item.TryGetProperty("vectorDim",      out var vd) ? vd.GetInt32()       : 0,
                EmbeddingModel = item.TryGetProperty("embeddingModel", out var em) ? em.GetString()      : null,
                DocCount       = item.TryGetProperty("docCount",       out var dc) ? dc.GetInt32()       : 0,
                ChunkCount     = item.TryGetProperty("chunkCount",     out var cc) ? cc.GetInt32()       : 0,
            });
        return list;
    }
}
