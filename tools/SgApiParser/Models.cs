using System.Text.Json.Serialization;

namespace SgApiParser;

public class ComponentInfo
{
    [JsonPropertyName("component")] public string Name { get; set; } = "";
    [JsonPropertyName("namespace")] public string Namespace { get; set; } = "";
    [JsonPropertyName("inherits")] public string? Inherits { get; set; }
    [JsonPropertyName("implements")] public List<string> Implements { get; set; } = new();
    [JsonPropertyName("usesJsInterop")] public bool UsesJsInterop { get; set; }
    [JsonPropertyName("modulePath")] public string? ModulePath { get; set; }
    [JsonPropertyName("hasJsInvokable")] public bool HasJsInvokable { get; set; }
    [JsonPropertyName("jsInvokableMethods")] public List<string> JsInvokableMethods { get; set; } = new();
    [JsonPropertyName("fileKind")] public string FileKind { get; set; } = ""; // "single-file" | "code-behind"
    [JsonPropertyName("hasCss")] public bool HasCss { get; set; }
    [JsonPropertyName("hasDemo")] public bool HasDemo { get; set; }
    [JsonPropertyName("hasTests")] public bool HasTests { get; set; }
    [JsonPropertyName("parameters")] public List<ParameterInfo> Parameters { get; set; } = new();
    [JsonPropertyName("events")] public List<ParameterInfo> Events { get; set; } = new();
    [JsonPropertyName("enumsUsed")] public List<string> EnumsUsed { get; set; } = new();
    [JsonPropertyName("filePath")] public string? FilePath { get; set; }
    [JsonPropertyName("lineCount")] public int LineCount { get; set; }
}

public class ParameterInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("default")] public string? DefaultValue { get; set; }
    [JsonPropertyName("doc")] public string? DocComment { get; set; }
    [JsonPropertyName("required")] public bool Required { get; set; }

    [JsonIgnore]
    public bool IsEventCallback => Type.StartsWith("EventCallback");
}
