using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Components.DocumentExtractor.Models;

namespace SuperUI.Components.DocumentExtractor.Services;

/// <summary>
/// Default <see cref="ILlmExtractionClient"/> for OpenAI-compatible APIs and OpenRouter.
/// Both speak the same /chat/completions and /models shape — the only difference is
/// the base URL, the auth header, and OpenRouter's optional Referer/Title headers.
/// </summary>
public sealed class OpenAiCompatibleLlmExtractionClient : ILlmExtractionClient
{
    private const string DefaultExtractionPrompt =
        "You extract editable form fields from documents. " +
        "Given the document text (and optionally an image), return ONLY a JSON object with this shape: " +
        "{\"fields\":[{\"key\":\"snake_case_id\",\"label\":\"Human label\",\"type\":\"text|multiline|number|date|boolean|select\",\"value\":\"...\",\"options\":[\"a\",\"b\"],\"locator\":\"optional anchor in the doc\"}]}. " +
        "Detect real fields the user might want to edit (names, dates, amounts, addresses, line items). " +
        "Never wrap the JSON in markdown fences. Never add commentary.";

    private readonly HttpClient _http;

    public OpenAiCompatibleLlmExtractionClient(HttpClient http) => _http = http;

    /// <summary>Sends the document to the LLM and parses the JSON fields response.</summary>
    public async Task<List<SgDocumentField>> ExtractFieldsAsync(
        SgLlmEndpointConfig config,
        SgDocumentSource source,
        string? extractedPlainText,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrWhiteSpace(config.Model))
            throw new InvalidOperationException("LLM model id is required.");

        var systemPrompt = string.IsNullOrWhiteSpace(config.SystemPrompt) ? DefaultExtractionPrompt : config.SystemPrompt!;
        var userContent = BuildUserContent(source, extractedPlainText);

        var payload = new
        {
            model = config.Model,
            temperature = 0.0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, JoinUrl(config.BaseUrl, "chat/completions"))
        {
            Content = JsonContent.Create(payload)
        };
        ApplyHeaders(req, config);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return ParseFields(content);
    }

    /// <summary>Fetches the list of available models from the provider's REST API.</summary>
    public async Task<List<SgLlmModelDescriptor>> ListModelsAsync(SgLlmEndpointConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        using var req = new HttpRequestMessage(HttpMethod.Get, JoinUrl(config.BaseUrl, "models"));
        ApplyHeaders(req, config);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return new List<SgLlmModelDescriptor>();

        var result = new List<SgLlmModelDescriptor>(data.GetArrayLength());
        foreach (var item in data.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(id)) continue;
            var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? id : id;
            var desc = item.TryGetProperty("description", out var d) ? d.GetString() : null;

            bool isFree = false;
            if (item.TryGetProperty("pricing", out var pricing) && pricing.ValueKind == JsonValueKind.Object)
            {
                isFree = LooksZero(pricing, "prompt") && LooksZero(pricing, "completion");
            }

            result.Add(new SgLlmModelDescriptor { Id = id, Name = name, Description = desc, IsFree = isFree });
        }
        return result;
    }

    private static bool LooksZero(JsonElement obj, string prop)
    {
        if (!obj.TryGetProperty(prop, out var el)) return false;
        var s = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        return s is "0" or "0.0" or "0.00";
    }

    private static object BuildUserContent(SgDocumentSource source, string? extractedPlainText)
    {
        var hasImage = source.Kind == SgDocumentKind.Image && source.Data.Length > 0;
        var textHeader = $"FILE: {source.FileName}\nKIND: {source.Kind}\n";
        var text = textHeader + (string.IsNullOrWhiteSpace(extractedPlainText)
            ? "(no plain text extracted — rely on the image if present)"
            : extractedPlainText!);

        if (!hasImage)
        {
            return text;
        }

        // OpenAI vision content array; OpenRouter and most OpenAI-compatible vision models accept the same shape.
        var dataUrl = $"data:{(string.IsNullOrEmpty(source.MimeType) ? "image/png" : source.MimeType)};base64,{Convert.ToBase64String(source.Data)}";
        return new object[]
        {
            new { type = "text", text = text },
            new { type = "image_url", image_url = new { url = dataUrl } }
        };
    }

    private static List<SgDocumentField> ParseFields(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new();
        var json = StripFences(content!);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("fields", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return new();

        var fields = new List<SgDocumentField>(arr.GetArrayLength());
        foreach (var f in arr.EnumerateArray())
        {
            var key = f.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(key)) continue;
            var label = f.TryGetProperty("label", out var l) ? l.GetString() ?? key : key;
            var typeStr = f.TryGetProperty("type", out var t) ? t.GetString() : null;
            var value = f.TryGetProperty("value", out var v)
                ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString())
                : null;
            var locator = f.TryGetProperty("locator", out var loc) ? loc.GetString() : null;
            List<string>? options = null;
            if (f.TryGetProperty("options", out var opt) && opt.ValueKind == JsonValueKind.Array)
            {
                options = opt.EnumerateArray()
                    .Select(o => o.ValueKind == JsonValueKind.String ? o.GetString() ?? "" : o.ToString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            fields.Add(new SgDocumentField
            {
                Key = key,
                Label = label,
                Type = MapType(typeStr),
                Value = value,
                Options = options,
                Locator = locator
            });
        }
        return fields;
    }

    private static SgDocumentFieldType MapType(string? s) => s?.ToLowerInvariant() switch
    {
        "multiline" or "multilinetext" or "textarea" => SgDocumentFieldType.MultilineText,
        "number" or "numeric" or "int" or "integer" or "decimal" => SgDocumentFieldType.Number,
        "date" or "datetime" => SgDocumentFieldType.Date,
        "boolean" or "bool" => SgDocumentFieldType.Boolean,
        "select" or "enum" or "choice" => SgDocumentFieldType.Select,
        _ => SgDocumentFieldType.Text
    };

    private static string StripFences(string s)
    {
        var trimmed = s.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = trimmed.IndexOf('\n');
            if (firstNl > 0) trimmed = trimmed[(firstNl + 1)..];
            if (trimmed.EndsWith("```", StringComparison.Ordinal)) trimmed = trimmed[..^3];
        }
        return trimmed.Trim();
    }

    private static void ApplyHeaders(HttpRequestMessage req, SgLlmEndpointConfig config)
    {
        if (!string.IsNullOrEmpty(config.ApiKey))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        if (config.Kind == SgLlmEndpointKind.OpenRouter)
        {
            // OpenRouter recommends these; harmless for OpenAI-compatible endpoints (they ignore unknown headers).
            req.Headers.TryAddWithoutValidation("HTTP-Referer", "https://superui.local");
            req.Headers.TryAddWithoutValidation("X-Title", "SuperUI Document Extractor");
        }

        if (config.ExtraHeaders != null)
        {
            foreach (var (k, v) in config.ExtraHeaders)
                req.Headers.TryAddWithoutValidation(k, v);
        }
    }

    private static string JoinUrl(string baseUrl, string path)
    {
        if (string.IsNullOrEmpty(baseUrl)) return path;
        return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
    }
}
