namespace SuperUI.Components;

/// <summary>
/// Top-level configuration options for <see cref="SgRagProvider"/>.
/// Pass via the <c>Options</c> parameter of the provider component.
/// </summary>
public sealed class SgRagOptions
{
    // ── Embedding ─────────────────────────────────────────────────────────────

    /// <summary>Embedding model to use. Default <see cref="SgRagEmbeddingModelKind.MiniLmL6V2"/>.</summary>
    public SgRagEmbeddingModelKind EmbeddingModelKind { get; set; } = SgRagEmbeddingModelKind.MiniLmL6V2;

    /// <summary>
    /// HuggingFace model ID when <see cref="EmbeddingModelKind"/> is <see cref="SgRagEmbeddingModelKind.Custom"/>.
    /// Example: <c>"Xenova/all-mpnet-base-v2"</c>.
    /// </summary>
    public string? CustomEmbeddingModel { get; set; }

    /// <summary>Quantization for the embedding model. Default <see cref="SgRagQuantization.Q8"/>.</summary>
    public SgRagQuantization Quantization { get; set; } = SgRagQuantization.Q8;

    // ── LLM ──────────────────────────────────────────────────────────────────

    /// <summary>LLM provider. Default <see cref="SgRagLlmProviderKind.None"/> (search-only).</summary>
    public SgRagLlmProviderKind LlmProviderKind { get; set; } = SgRagLlmProviderKind.None;

    /// <summary>
    /// WebLLM model ID. Example: <c>"Llama-3.2-1B-Instruct-q4f16_1-MLC"</c>.
    /// Required when <see cref="LlmProviderKind"/> is <see cref="SgRagLlmProviderKind.WebLlm"/>.
    /// </summary>
    public string? LlmModelId { get; set; }

    /// <summary>OpenAI-compatible API base URL. Default: <c>https://api.openai.com/v1</c>.</summary>
    public string OpenAiBaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// OpenAI API key. Stored in browser memory only; never persisted unless
    /// the user explicitly opts in via <see cref="SgRagSaveLoadDb"/>.
    /// </summary>
    public string? OpenAiApiKey { get; set; }

    /// <summary>OpenAI model name. Default: <c>gpt-4o-mini</c>.</summary>
    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Optional HTTP-Referer header sent with OpenRouter requests.
    /// OpenRouter uses this to attribute usage on their dashboard.
    /// Example: <c>"https://myapp.example.com"</c>.
    /// </summary>
    public string? OpenRouterReferer { get; set; }

    /// <summary>
    /// Optional X-Title header sent with OpenRouter requests.
    /// Shown in the OpenRouter activity log.
    /// </summary>
    public string? OpenRouterTitle { get; set; }

    // ── Chunking ──────────────────────────────────────────────────────────────

    /// <summary>Default chunking options applied to all ingested documents.</summary>
    public SgRagChunkingOptions DefaultChunking { get; set; } = new();

    // ── Storage ───────────────────────────────────────────────────────────────

    /// <summary>Persist vectors and documents to IndexedDB. Default <c>true</c>.</summary>
    public bool PersistToIndexedDb { get; set; } = true;

    /// <summary>IndexedDB database name. Default <c>"sg-rag"</c>.</summary>
    public string IndexedDbName { get; set; } = "sg-rag";

    /// <summary>Default collection name. Default <c>"default"</c>.</summary>
    public string DefaultCollection { get; set; } = "default";

    // ── Search / RAG ──────────────────────────────────────────────────────────

    /// <summary>Minimum cosine similarity score for search results (0–1). Default 0.0 (no filter).</summary>
    public double SimilarityThreshold { get; set; } = 0.0;

    /// <summary>Maximum context tokens to include in the LLM prompt. Default 3000.</summary>
    public int MaxContextTokens { get; set; } = 3000;

    // ── Sources ───────────────────────────────────────────────────────────────

    /// <summary>CDN source URL overrides for vendor libraries.</summary>
    public SgRagSources Sources { get; set; } = new();
}
