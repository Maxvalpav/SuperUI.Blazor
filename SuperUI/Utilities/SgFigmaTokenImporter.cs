using System.Text.Json;
using System.Text;

namespace SuperUI.Utilities;

public class SgFigmaTokenImporter
{
    public string GenerateCssFromFigmaTokens(string json)
    {
        var sb = new StringBuilder(":root {\n");
        using var doc = JsonDocument.Parse(json);
        
        foreach (var category in doc.RootElement.EnumerateObject())
        {
            ProcessTokenCategory(category.Name, category.Value, sb);
        }
        
        sb.Append("}");
        return sb.ToString();
    }

    private void ProcessTokenCategory(string prefix, JsonElement element, StringBuilder sb)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("value", out var value))
            {
                sb.AppendLine($"  --sg-{prefix.Replace("/", "-")}: {value};");
            }
            else
            {
                foreach (var child in element.EnumerateObject())
                {
                    ProcessTokenCategory($"{prefix}-{child.Name}", child.Value, sb);
                }
            }
        }
    }
}
