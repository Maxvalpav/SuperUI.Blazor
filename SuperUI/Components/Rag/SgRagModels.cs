namespace SuperUI.Components;

// ── Enums ─────────────────────────────────────────────────────────────────────

/// <summary>Embedding model kind for RAG.</summary>
public enum SgRagEmbeddingModelKind
{
    /// <summary>all-MiniLM-L6-v2 (384-dim, ~23 MB Q8). Best balance of speed and quality.</summary>
    MiniLmL6V2,
    /// <summary>jina-embeddings-v2-base-en (768-dim, ~130 MB). Higher quality.</summary>
    JinaBaseEn,
    /// <summary>bge-small-en-v1.5 (384-dim, ~33 MB). Fast, good quality.</summary>
    BgeSmallEn,
    /// <summary>Custom model — specify via <see cref="SgRagOptions.CustomEmbeddingModel"/>.</summary>
    Custom,
}

/// <summary>Quantization level for model weights.</summary>
public enum SgRagQuantization
{
    /// <summary>Full 32-bit float. Highest quality, largest size.</summary>
    Fp32,
    /// <summary>16-bit float. Good quality, half the size of Fp32.</summary>
    Fp16,
    /// <summary>8-bit quantization. Recommended default.</summary>
    Q8,
    /// <summary>4-bit quantization. Smallest size, some quality loss.</summary>
    Q4,
}

/// <summary>LLM provider for RAG answer generation.</summary>
public enum SgRagLlmProviderKind
{
    /// <summary>WebLLM — runs the model locally in the browser via WebGPU.</summary>
    WebLlm,
    /// <summary>OpenAI-compatible cloud API (OpenAI, Azure, Ollama, etc.).</summary>
    OpenAiCompatible,
    /// <summary>OpenRouter — aggregator with 200+ models, OpenAI-compatible API.</summary>
    OpenRouter,
    /// <summary>No LLM — search-only mode.</summary>
    None,
}

/// <summary>Text chunking strategy.</summary>
public enum SgRagChunkStrategy
{
    /// <summary>Sliding window over characters. Fast, language-agnostic fallback.</summary>
    Characters,
    /// <summary>Split on sentence boundaries, pack into chunks.</summary>
    Sentences,
    /// <summary>LangChain-style recursive splitting on configurable separators.</summary>
    Recursive,
    /// <summary>Semantic coherence-based splitting (requires embedding model).</summary>
    Semantic,
    /// <summary>Code-aware splitting: splits on top-level functions/classes/blocks.</summary>
    Code,
}

/// <summary>RAG answer generation mode.</summary>
public enum SgRagAnswerMode
{
    /// <summary>Answer ONLY from retrieved context. Returns "No information found" if insufficient.</summary>
    Strict,
    /// <summary>Prefer context, supplement with general knowledge. Cite when context is used.</summary>
    Hybrid,
    /// <summary>Context as supplement only. LLM may answer freely.</summary>
    FreeForm,
    /// <summary>Direct chat — no document search, pure LLM conversation.</summary>
    Direct,
}

/// <summary>Supported document formats for ingestion.</summary>
public enum SgRagDocumentFormat
{
    Pdf,
    Txt,
    Md,
    Docx,
    Html,
    Json,
    /// <summary>Source code file (language detected from extension).</summary>
    Code,
    /// <summary>Auto-detect from MIME type or file extension.</summary>
    Auto,
}

/// <summary>Snapshot kind.</summary>
public enum SgRagSnapshotKind
{
    Manual,
    Auto,
}

// ── Options ───────────────────────────────────────────────────────────────────

/// <summary>Chunking configuration for document ingestion.</summary>
public class SgRagChunkingOptions
{
    /// <summary>Chunking strategy. Default <see cref="SgRagChunkStrategy.Recursive"/>.</summary>
    public SgRagChunkStrategy Strategy { get; set; } = SgRagChunkStrategy.Recursive;

    /// <summary>Target chunk size in characters. Default 512.</summary>
    public int ChunkSize { get; set; } = 512;

    /// <summary>Overlap between consecutive chunks in characters. Default 64.</summary>
    public int Overlap { get; set; } = 64;

    /// <summary>Separators for Recursive strategy. Default: paragraph, newline, sentence, space, empty.</summary>
    public string[] Separators { get; set; } = ["\n\n", "\n", ". ", " ", ""];

    /// <summary>Window size for smoothing in Semantic strategy. Default 3.</summary>
    public int SemanticSimilarityWindow { get; set; } = 3;
}

// ── Data models ───────────────────────────────────────────────────────────────

/// <summary>Progress event for model loading.</summary>
public class SgRagModelProgress
{
    public string Stage { get; set; } = string.Empty;
    public double Loaded { get; set; }
    public double Total { get; set; }
    public double Percent { get; set; }
    public string? File { get; set; }
    public bool IsComplete { get; set; }
}

/// <summary>Progress event for document indexing.</summary>
public class SgRagIndexProgress
{
    public string DocumentId { get; set; } = string.Empty;
    public int ChunksDone { get; set; }
    public int Total { get; set; }
    /// <summary>Current phase: "parsing", "chunking", "embedding", "persisting".</summary>
    public string Phase { get; set; } = string.Empty;
}

/// <summary>A document stored in the RAG index.</summary>
public class SgRagDocument
{
    public string Id { get; set; } = string.Empty;
    public string Collection { get; set; } = "default";
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public SgRagDocumentFormat Format { get; set; } = SgRagDocumentFormat.Auto;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public int ChunkCount { get; set; }
}

/// <summary>A text chunk from a document.</summary>
public class SgRagChunk
{
    public string Id { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public int Index { get; set; }
    public string Text { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>An embedding vector for a chunk.</summary>
public class SgRagEmbedding
{
    public string ChunkId { get; set; } = string.Empty;
    public float[] Vector { get; set; } = [];
    public int Dim { get; set; }
    public string Model { get; set; } = string.Empty;
}

/// <summary>A semantic search result.</summary>
public class SgRagSearchHit
{
    public SgRagChunk Chunk { get; set; } = new();
    public SgRagDocument Document { get; set; } = new();
    public double Score { get; set; }
    public IReadOnlyList<(int Start, int End)> HighlightSpans { get; set; } = [];
}

/// <summary>Result of ingesting a document.</summary>
public class SgRagDocumentIngestResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>A chat message in the RAG conversation.</summary>
public class SgRagChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsUser { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<SgRagSearchHit>? Sources { get; set; }
}

/// <summary>A complete RAG answer with sources.</summary>
public class SgRagAnswer
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public IReadOnlyList<SgRagSearchHit> Sources { get; set; } = [];
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>Snapshot metadata.</summary>
public class SgRagSnapshotInfo
{
    public string Id { get; set; } = string.Empty;
    public SgRagSnapshotKind Kind { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long SizeBytes { get; set; }
}

/// <summary>Ready state of the RAG provider.</summary>
public record SgRagReadyState
{
    public bool EmbeddingReady { get; init; }
    public bool LlmReady { get; init; }
    public bool DbReady { get; init; }
    public bool WebGpuAvailable { get; init; }
    public string? EmbeddingModel { get; init; }
    public string? LlmModel { get; init; }
    public SgRagLlmProviderKind LlmProvider { get; init; }
}

/// <summary>Collection metadata.</summary>
public class SgRagCollectionInfo
{
    public string Name { get; set; } = string.Empty;
    public int VectorDim { get; set; }
    public string? EmbeddingModel { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DocCount { get; set; }
    public int ChunkCount { get; set; }
}

// ── Analytics models ───────────────────────────────────────────────────────────

/// <summary>Query log entry for analytics.</summary>
public class SgRagQueryLogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Query { get; set; } = string.Empty;
    public string Collection { get; set; } = "default";
    public long LatencyMs { get; set; }
    public int HitCount { get; set; }
    public bool HasAnswer { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>Analytics summary for display.</summary>
public class SgRagAnalyticsSummary
{
    public int TotalQueries { get; set; }
    public double AverageLatencyMs { get; set; }
    public double NoAnswerRate { get; set; }
    public int TotalHits { get; set; }
    public IReadOnlyList<SgRagQueryLogEntry> RecentQueries { get; set; } = [];
}

/// <summary>Result of exporting a chat history.</summary>
public class SgRagChatExportResult
{
    public string Content { get; set; } = "";
    public string ContentType { get; set; } = "";
    public string Extension { get; set; } = "";
}
