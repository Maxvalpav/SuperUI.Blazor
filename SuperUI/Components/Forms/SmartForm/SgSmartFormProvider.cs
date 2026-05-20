using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SuperUI.Services.Llm;

namespace SuperUI.Components;

public class SgSmartFormProvider
{
    private readonly ILlmService? _llm;

    public SgSmartFormProvider(ILlmService? llm = null)
    {
        _llm = llm;
    }

    /// <summary>
    /// Generates metadata from a C# class using reflection.
    /// </summary>
    public SgSmartFormMetadata FromModel<T>()
    {
        var type = typeof(T);
        var meta = new SgSmartFormMetadata
        {
            Title = type.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? type.Name
        };

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false);

        foreach (var p in props)
        {
            var display = p.GetCustomAttribute<DisplayAttribute>();
            var required = p.GetCustomAttribute<RequiredAttribute>() != null;
            
            meta.Fields.Add(new SgSmartFieldMetadata
            {
                Key = p.Name,
                Label = display?.GetName() ?? p.Name,
                Description = display?.GetDescription(),
                Placeholder = display?.GetPrompt(),
                Required = required,
                Type = MapType(p.PropertyType),
                FullWidth = p.PropertyType == typeof(string) && (display?.GetDescription()?.Length > 50)
            });
        }

        return meta;
    }

    /// <summary>
    /// Generates metadata from an AI prompt (Innovation 7.1).
    /// </summary>
    public async Task<SgSmartFormMetadata> FromPromptAsync(string prompt)
    {
        if (_llm == null) throw new InvalidOperationException("LLM Service not configured");

        var systemPrompt = @"You are a Blazor Form Generator. Convert natural language into JSON metadata.
Return ONLY valid JSON in this format:
{
  ""Title"": ""Form Title"",
  ""Columns"": 1,
  ""Fields"": [
    { 
      ""Key"": ""fieldName"", 
      ""Label"": ""Display Label"", 
      ""Type"": 0, 
      ""Required"": true, 
      ""Placeholder"": ""Optional hint"",
      ""FullWidth"": false,
      ""Options"": [] 
    }
  ]
}
Field Types (integer): 0=Text, 1=Multiline, 2=Number, 3=Boolean, 4=Date, 5=DateTime, 6=Select, 7=Password, 8=Email.
For Select (Type 6), provide Options as [{""Label"":""L"", ""Value"":""V""}].";

        string responseJson = "";
        var tcs = new TaskCompletionSource<string>();

        void OnComplete(string content) => tcs.TrySetResult(content);
        void OnError(string error) => tcs.TrySetException(new Exception(error));

        _llm.OnChatComplete += OnComplete;
        _llm.OnError += OnError;

        try
        {
            await _llm.ChatAsync($"{systemPrompt}\n\nUser Request: {prompt}");
            responseJson = await tcs.Task;
        }
        finally
        {
            _llm.OnChatComplete -= OnComplete;
            _llm.OnError -= OnError;
        }

        // Clean JSON if LLM added markdown blocks
        if (responseJson.Contains("```json"))
        {
            responseJson = responseJson.Split("```json")[1].Split("```")[0];
        }
        else if (responseJson.Contains("```"))
        {
            responseJson = responseJson.Split("```")[1].Split("```")[0];
        }

        return System.Text.Json.JsonSerializer.Deserialize<SgSmartFormMetadata>(responseJson, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        }) ?? new SgSmartFormMetadata { Title = "AI Error" };
    }

    private SgSmartFieldType MapType(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        if (t == typeof(bool)) return SgSmartFieldType.Boolean;
        if (t == typeof(int) || t == typeof(decimal) || t == typeof(double)) return SgSmartFieldType.Number;
        if (t == typeof(DateTime)) return SgSmartFieldType.Date;
        if (t.IsEnum) return SgSmartFieldType.Select;
        return SgSmartFieldType.Text;
    }
}
