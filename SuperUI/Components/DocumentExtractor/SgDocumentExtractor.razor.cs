using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SuperUI.Components.DocumentExtractor.Models;
using SuperUI.Components.DocumentExtractor.Services;
using SuperUI.Services.Llm;

namespace SuperUI.Components.DocumentExtractor;

/// <summary>
/// Upload → extract → edit → save pipeline component.
///
/// Plug your own <see cref="IDocumentExtractor"/> and <see cref="IDocumentSaver"/> sets via
/// DI (see <c>AddSgDocumentExtractor</c>). The default registration includes the LLM
/// extractor (OpenAI-compatible + OpenRouter), a managed DOCX text extractor, and savers
/// for DOCX, plain text, and a passthrough for PDF/images.
/// </summary>
public partial class SgDocumentExtractor : ComponentBase, IAsyncDisposable
{
    private static readonly long DefaultMaxBytes = 25L * 1024 * 1024;

    [Inject] public IEnumerable<IDocumentExtractor> Extractors { get; set; } = Array.Empty<IDocumentExtractor>();
    [Inject] public IEnumerable<IDocumentSaver> Savers { get; set; } = Array.Empty<IDocumentSaver>();
    [Inject] public ILlmExtractionClient LlmClient { get; set; } = default!;
    [Inject] public SgLlmEndpointConfigStore EndpointStore { get; set; } = default!;

    /// <summary>Maximum upload size in bytes. Defaults to 25 MiB.</summary>
    [Parameter] public long? MaxFileSizeBytes { get; set; } = DefaultMaxBytes;

    [Parameter] public string UploadLabel { get; set; } = "Upload document";

    /// <summary>Initial extractor id. Defaults to <c>"llm"</c> when present.</summary>
    [Parameter] public string? InitialExtractorId { get; set; }

    [Parameter] public string? CssClass { get; set; }

    /// <summary>Fires after a successful save with the resulting <see cref="SgDocumentSource"/>.</summary>
    [Parameter] public EventCallback<SgDocumentSource> OnSaved { get; set; }

    /// <summary>Fires after a successful extraction with the produced <see cref="SgDocumentExtractionResult"/>.</summary>
    [Parameter] public EventCallback<SgDocumentExtractionResult> OnExtracted { get; set; }

    private readonly List<string> _providerOptions = new() { nameof(SgLlmEndpointKind.OpenAiCompatible), nameof(SgLlmEndpointKind.OpenRouter) };

    private SgLlmConfig _llmConfig = new();
    private SgDocumentSource? _source;
    private SgDocumentExtractionResult? _result;
    private List<SgLlmModelDescriptor> _models = new();
    private string? _selectedExtractorId;
    private string? _status;
    private bool _isError;
    private bool _busy;

    protected override void OnInitialized()
    {
        _llmConfig = ToLlmConfig(EndpointStore.Current);
        _selectedExtractorId = InitialExtractorId ?? Extractors.FirstOrDefault(e => e.Id == "llm")?.Id ?? Extractors.FirstOrDefault()?.Id;
    }

    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/js/documentExtractor.js");
            var savedSettings = await _module.InvokeAsync<SgLlmEndpointConfig?>("loadExtractorSettings");
            if (savedSettings != null)
            {
                _llmConfig = ToLlmConfig(savedSettings);
                UpdateEndpointStore();
                StateHasChanged();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            try { await _module.DisposeAsync(); } catch { /* Ignore */ }
        }
    }

    private async Task SaveSettingsAsync()
    {
        if (_module != null)
        {
            await _module.InvokeVoidAsync("saveExtractorSettings", FromLlmConfig(_llmConfig));
        }
    }

    private void OnLlmConfigChanged(SgLlmConfig config)
    {
        _llmConfig = config;
        UpdateEndpointStore();
        _ = SaveSettingsAsync();
    }

    private void UpdateEndpointStore()
    {
        EndpointStore.Current = FromLlmConfig(_llmConfig);
    }

    private SgLlmConfig ToLlmConfig(SgLlmEndpointConfig endpoint)
    {
        return new SgLlmConfig
        {
            Provider = endpoint.Kind == SgLlmEndpointKind.OpenRouter ? SgLlmProvider.OpenRouter : SgLlmProvider.OpenAiCompatible,
            ModelId = endpoint.Model,
            ApiKey = endpoint.ApiKey,
            BaseUrl = endpoint.BaseUrl,
            SystemPrompt = endpoint.SystemPrompt,
            ExtraHeaders = endpoint.ExtraHeaders
        };
    }

    private SgLlmEndpointConfig FromLlmConfig(SgLlmConfig config)
    {
        return new SgLlmEndpointConfig
        {
            Kind = config.Provider == SgLlmProvider.OpenRouter ? SgLlmEndpointKind.OpenRouter : SgLlmEndpointKind.OpenAiCompatible,
            Model = config.ModelId ?? "",
            ApiKey = config.ApiKey,
            BaseUrl = config.BaseUrl ?? "",
            SystemPrompt = config.SystemPrompt,
            ExtraHeaders = config.ExtraHeaders
        };
    }

    private async Task DownloadAsync(SgDocumentSource saved)
    {
        if (_module != null)
        {
            var b64 = Convert.ToBase64String(saved.Data);
            await _module.InvokeVoidAsync("downloadFile", saved.FileName, b64, saved.MimeType);
        }
    }

    private void OnExtractorPicked(string? id) => _selectedExtractorId = id;

    private async Task OnFilesPicked(IReadOnlyList<IBrowserFile>? files)
    {
        _result = null;
        _source = null;
        if (files is null || files.Count == 0) return;

        var file = files[0];
        var max = MaxFileSizeBytes ?? DefaultMaxBytes;
        if (file.Size > max)
        {
            Status($"File exceeds {max / (1024 * 1024)} MiB.", true);
            return;
        }

        await using var stream = file.OpenReadStream(max);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        _source = new SgDocumentSource
        {
            FileName = file.Name,
            MimeType = file.ContentType ?? "application/octet-stream",
            Kind = DetectKind(file.Name, file.ContentType),
            Data = ms.ToArray()
        };
        Status($"Loaded {_source.FileName} ({_source.Kind}, {_source.Data.Length:N0} bytes).", false);
    }

    private async Task ExtractAsync()
    {
        if (_source is null) return;

        var extractor = Extractors.FirstOrDefault(e => e.Id == _selectedExtractorId)
                        ?? Extractors.FirstOrDefault(e => e.CanHandle(_source));
        if (extractor is null) { Status("No extractor available for this document.", true); return; }

        // Make sure the LLM extractor sees the latest endpoint settings.
        UpdateEndpointStore();

        _busy = true; _isError = false; _status = "Extracting…";
        try
        {
            _result = await extractor.ExtractAsync(_source, CancellationToken.None);
            Status($"Extracted {_result.Fields.Count} field(s) via {extractor.DisplayName}.", false);
            if (OnExtracted.HasDelegate) await OnExtracted.InvokeAsync(_result);
        }
        catch (Exception ex) { Status($"Extraction failed: {ex.Message}", true); }
        finally { _busy = false; }
    }

    private async Task SaveAsync()
    {
        if (_result is null || _source is null) return;

        var saver = Savers.FirstOrDefault(s => s.CanHandle(_result));
        if (saver is null) { Status("No saver available for this document kind.", true); return; }

        _busy = true; _isError = false; _status = "Saving…";
        try
        {
            var saved = await saver.SaveAsync(_result, _result.Fields, CancellationToken.None);
            await DownloadAsync(saved);
            Status($"Saved {saved.FileName} via {saver.DisplayName}.", false);
            if (OnSaved.HasDelegate) await OnSaved.InvokeAsync(saved);
        }
        catch (Exception ex) { Status($"Save failed: {ex.Message}", true); }
        finally { _busy = false; }
    }

    private void Status(string message, bool isError)
    {
        _status = message;
        _isError = isError;
        StateHasChanged();
    }

    private static SgDocumentKind DetectKind(string name, string? mime)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        if (ext == ".pdf" || mime == "application/pdf") return SgDocumentKind.Pdf;
        if (ext == ".docx" || mime == "application/vnd.openxmlformats-officedocument.wordprocessingml.document") return SgDocumentKind.Docx;
        if (ext == ".txt" || mime == "text/plain") return SgDocumentKind.PlainText;
        if (mime?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true) return SgDocumentKind.Image;
        if (ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp") return SgDocumentKind.Image;
        return SgDocumentKind.Unknown;
    }

    private static double? AsNullableDouble(object? value) => value switch
    {
        null => null,
        double d => d,
        int i => i,
        long l => l,
        decimal m => (double)m,
        string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
        string s when double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var d) => d,
        _ => null
    };

    private static DateTime? AsNullableDate(object? value) => value switch
    {
        null => null,
        DateTime dt => dt,
        DateTimeOffset dto => dto.DateTime,
        string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var d) => d,
        string s when DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var d) => d,
        _ => null
    };

    private static bool AsBool(object? value) => value switch
    {
        bool b => b,
        string s when bool.TryParse(s, out var b) => b,
        string s => s.Equals("yes", StringComparison.OrdinalIgnoreCase) || s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase),
        _ => false
    };
}

/// <summary>
/// Scoped holder for the LLM endpoint config so the same settings flow into both the UI
/// and the <see cref="LlmDocumentExtractor"/> without circular DI.
/// </summary>
public sealed class SgLlmEndpointConfigStore
{
    public SgLlmEndpointConfig Current { get; set; } = new();
}
