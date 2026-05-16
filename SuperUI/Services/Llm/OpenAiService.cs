using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _http;
    private readonly ISchemaGeneratorService _schemaGenerator;
    private readonly IDocumentParserService _parser;

    public OpenAiService(HttpClient http, ISchemaGeneratorService schemaGenerator, IDocumentParserService parser)
    {
        _http = http;
        _schemaGenerator = schemaGenerator;
        _parser = parser;
    }

    public async Task<SchemaExtractionResult> ExtractSchemaAsync(
        List<UploadedFile> files,
        OpenAiSettings settings,
        IProgress<string>? progress = null)
    {
        progress?.Report("Parsing documents and preparing data...");

        var messages = new List<object>();
        
        // System message
        messages.Add(new { role = "system", content = settings.SystemPrompt ?? GetDefaultSystemPrompt() });

        // User message with images
        var contentParts = new List<object>();
        var userPrompt = new StringBuilder();
        userPrompt.AppendLine("Analyze the attached document content and extract all data according to the requested format.");

        foreach (var file in files)
        {
            progress?.Report($"Processing {file.FileName}...");
            var parsed = await _parser.ParseAsync(file);

            if (parsed.Pages.Any())
            {
                foreach (var page in parsed.Pages)
                {
                    contentParts.Add(new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = $"data:{page.MimeType};base64,{page.Base64}",
                            detail = "high"
                        }
                    });
                }
            }
            
            if (!string.IsNullOrEmpty(parsed.ExtractedText))
            {
                userPrompt.AppendLine($"\nContent from {file.FileName}:\n{parsed.ExtractedText}");
            }

            if (parsed.Metadata.Count > 0)
            {
                userPrompt.AppendLine($"Metadata for {file.FileName}: {string.Join(", ", parsed.Metadata.Select(x => $"{x.Key}={x.Value}"))}");
            }
        }

        contentParts.Insert(0, new { type = "text", text = userPrompt.ToString() });
        messages.Add(new { role = "user", content = contentParts });

        var requestBody = new
        {
            model = settings.Model,
            messages = messages,
            response_format = new { type = "json_object" },
            temperature = settings.Temperature,
            max_tokens = settings.MaxTokens
        };

        progress?.Report($"Sending request to {settings.Provider} ({settings.Model})...");

        var baseUrl = settings.BaseUrl;
        if (settings.Provider == LlmProvider.OpenRouter && (string.IsNullOrEmpty(baseUrl) || baseUrl == "https://api.openai.com/v1"))
        {
            baseUrl = "https://openrouter.ai/api/v1";
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        
        if (settings.Provider == LlmProvider.OpenRouter)
        {
            request.Headers.Add("HTTP-Referer", "https://superui.blazor");
            request.Headers.Add("X-Title", "SuperUI Document Extractor");
        }

        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API error: {response.StatusCode} - {error}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(jsonResponse);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        if (string.IsNullOrEmpty(content))
            throw new Exception("Empty response from OpenAI");

        progress?.Report("Parsing extracted data...");
        
        var schema = _schemaGenerator.ParseOpenAiResponse(content);
        
        // Extract values from the same JSON
        var values = new Dictionary<string, object?>();
        var contentJson = JsonDocument.Parse(content).RootElement;
        if (contentJson.TryGetProperty("extractedData", out var dataEl))
        {
            foreach (var prop in dataEl.EnumerateObject())
            {
                values[prop.Name] = GetValue(prop.Value);
            }
        }

        return new SchemaExtractionResult
        {
            Schema = schema,
            ExtractedValues = values,
            RawResponse = content
        };
    }

    private object? GetValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => el.GetRawText(), // Handled by specific field components
        JsonValueKind.Object => el.GetRawText(),
        _ => null
    };

    public async Task<bool> ValidateApiKeyAsync(OpenAiSettings settings)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.BaseUrl.TrimEnd('/')}/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<string>> GetAvailableModelsAsync(OpenAiSettings settings)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.BaseUrl.TrimEnd('/')}/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();
            
            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            var list = new List<string>();
            foreach (var m in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                list.Add(m.GetProperty("id").GetString() ?? "");
            }
            return list;
        }
        catch { return new(); }
    }

    private string GetDefaultSystemPrompt()
    {
        return @"You are a document analysis expert. Your task is to:
1. Analyze the provided document (PDF pages as images or extracted text)
2. Identify ALL data fields present in the document
3. Return a structured JSON response with two parts:
   a) A JSON Schema describing all fields (fields and sections)
   b) The actual data extracted from the document (extractedData)

CRITICAL RULES:
- Return ONLY valid JSON, no markdown, no explanations
- Detect the document language and use it for field labels
- For tables: create a 'table' type field with column definitions
- Infer field types: text, number, date, boolean, select, table
- If a field has a fixed set of values, make it 'select' type";
    }
}
