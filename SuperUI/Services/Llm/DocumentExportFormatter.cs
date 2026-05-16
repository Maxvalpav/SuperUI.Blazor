using System.Globalization;
using System.Text;
using System.Text.Json;
using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

internal static class DocumentExportFormatter
{
    public static List<string> BuildLines(ExtractedData data, ExportOptions options)
    {
        var lines = new List<string>
        {
            string.IsNullOrWhiteSpace(data.Schema.Title) ? "Extracted Document" : data.Schema.Title
        };

        if (options.IncludeSchemaSummary)
        {
            lines.Add($"Document type: {data.Schema.DocumentType}");
            lines.Add($"Extracted at: {data.ExtractedAt:yyyy-MM-dd HH:mm:ss} UTC");
            lines.Add(string.Empty);
        }

        foreach (var section in data.Schema.Sections.OrderBy(x => x.Order))
        {
            lines.Add(section.Title);
            lines.Add(new string('-', Math.Max(8, section.Title.Length)));

            foreach (var fieldKey in section.FieldKeys)
            {
                var field = data.Schema.Fields.FirstOrDefault(x => x.Key == fieldKey);
                if (field is null)
                {
                    continue;
                }

                AppendFieldLine(lines, field, data.Values, options.IncludeEmptyFields);
            }

            lines.Add(string.Empty);
        }

        var knownKeys = data.Schema.Sections.SelectMany(x => x.FieldKeys).ToHashSet();
        foreach (var field in data.Schema.Fields.Where(x => !knownKeys.Contains(x.Key)).OrderBy(x => x.Order ?? int.MaxValue))
        {
            AppendFieldLine(lines, field, data.Values, options.IncludeEmptyFields);
        }

        if (options.IncludeSourceMetadata && data.SourceFiles.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Source files");
            lines.Add("------------");

            foreach (var file in data.SourceFiles)
            {
                lines.Add($"{file.FileName} ({file.Category}, {file.Size / 1024} KB)");
            }
        }

        return lines;
    }

    public static string FormatValue(FieldDefinition field, Dictionary<string, object?> values)
    {
        values.TryGetValue(field.Key, out var value);
        return FormatValue(value);
    }

    public static string FormatValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            bool b => b ? "Yes" : "No",
            DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            JsonElement json => json.ToString(),
            IEnumerable<string> strings => string.Join(", ", strings),
            _ => TryFormatStructuredValue(value)
        };
    }

    private static string TryFormatStructuredValue(object value)
    {
        if (value is string s)
        {
            return s;
        }

        if (value is IEnumerable<object?> list)
        {
            return string.Join("; ", list.Select(FormatValue));
        }

        try
        {
            return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }

    private static void AppendFieldLine(List<string> lines, FieldDefinition field, Dictionary<string, object?> values, bool includeEmptyFields)
    {
        var formatted = FormatValue(field, values);
        if (string.IsNullOrWhiteSpace(formatted) && !includeEmptyFields)
        {
            return;
        }

        lines.Add($"{field.Label}: {formatted}");
    }

    public static string EscapePdfText(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    public static string EscapeXml(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(ch switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => ch.ToString()
            });
        }

        return builder.ToString();
    }
}
