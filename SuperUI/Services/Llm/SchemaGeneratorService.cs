using System;
using System.Collections.Generic;
using System.Text.Json;
using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

public class SchemaGeneratorService : ISchemaGeneratorService
{
    public DocumentSchema ParseOpenAiResponse(string jsonResponse)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonDocument.Parse(jsonResponse).RootElement;
            
            var schema = new DocumentSchema
            {
                DocumentType = root.GetProperty("documentType").GetString() ?? "other",
                Title = root.GetProperty("documentTitle").GetString() ?? "Document",
                RawJsonSchema = jsonResponse
            };

            if (root.TryGetProperty("sections", out var sectionsElement))
            {
                foreach (var s in sectionsElement.EnumerateArray())
                {
                    var section = new FormSection
                    {
                        Key = s.GetProperty("key").GetString() ?? Guid.NewGuid().ToString(),
                        Title = s.GetProperty("title").GetString() ?? "Section",
                        Order = s.GetProperty("order").GetInt32()
                    };
                    if (s.TryGetProperty("fieldKeys", out var fieldKeys))
                    {
                        foreach (var k in fieldKeys.EnumerateArray())
                        {
                            section.FieldKeys.Add(k.GetString() ?? "");
                        }
                    }
                    schema.Sections.Add(section);
                }
            }

            if (root.TryGetProperty("fields", out var fieldsElement))
            {
                foreach (var f in fieldsElement.EnumerateArray())
                {
                    var field = ParseField(f);
                    schema.Fields.Add(field);
                }
            }

            return schema;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse OpenAI response: {ex.Message}", ex);
        }
    }

    private FieldDefinition ParseField(JsonElement f)
    {
        var field = new FieldDefinition
        {
            Key = f.GetProperty("key").GetString() ?? Guid.NewGuid().ToString(),
            Label = f.GetProperty("label").GetString() ?? "Field",
            Type = MapType(f.GetProperty("type").GetString() ?? "text"),
            Required = f.TryGetProperty("required", out var req) && req.GetBoolean(),
            Description = f.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Order = f.TryGetProperty("order", out var ord) ? ord.GetInt32() : null,
            Group = f.TryGetProperty("section", out var sec) ? sec.GetString() : null
        };

        if (field.Type == FieldType.Select || field.Type == FieldType.MultiSelect)
        {
            if (f.TryGetProperty("options", out var opts))
            {
                field.Options = new List<SelectOption>();
                foreach (var o in opts.EnumerateArray())
                {
                    field.Options.Add(new SelectOption
                    {
                        Value = o.GetProperty("value").GetString() ?? "",
                        Label = o.GetProperty("label").GetString() ?? ""
                    });
                }
            }
        }

        if (field.Type == FieldType.Table && f.TryGetProperty("columns", out var cols))
        {
            field.Columns = new List<FieldDefinition>();
            foreach (var c in cols.EnumerateArray())
            {
                field.Columns.Add(ParseField(c));
            }
        }

        return field;
    }

    private FieldType MapType(string type) => type.ToLower() switch
    {
        "text" => FieldType.Text,
        "textarea" => FieldType.TextArea,
        "number" => FieldType.Number,
        "integer" => FieldType.Integer,
        "date" => FieldType.Date,
        "datetime" => FieldType.DateTime,
        "boolean" => FieldType.Boolean,
        "select" => FieldType.Select,
        "multiselect" => FieldType.MultiSelect,
        "table" => FieldType.Table,
        "email" => FieldType.Email,
        "phone" => FieldType.Phone,
        "address" => FieldType.Address,
        _ => FieldType.Text
    };
}
