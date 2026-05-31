using System.Text.RegularExpressions;

namespace SgApiParser;

public partial class RazorFileParser
{
    public string? Namespace { get; private set; }
    public string? Inherits { get; private set; }
    public List<string> Implements { get; } = new();
    public string? CodeBlock { get; private set; }

    public void Parse(string razorContent)
    {
        Namespace = ExtractDirective(razorContent, "namespace");
        Inherits = ExtractDirective(razorContent, "inherits");

        Implements.Clear();
        foreach (Match m in ImplementsRegex().Matches(razorContent))
            Implements.Add(m.Groups[1].Value.Trim());

        CodeBlock = ExtractCodeBlock(razorContent);
    }

    private static string? ExtractDirective(string content, string keyword)
    {
        var match = Regex.Match(content,
            $@"@{keyword}\s+([\w.]+)",
            RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractCodeBlock(string content)
    {
        foreach (var keyword in new[] { "code", "functions" })
        {
            var startMatch = Regex.Match(content,
                $@"@{keyword}\s*{{",
                RegexOptions.Singleline);
            if (!startMatch.Success) continue;

            int start = startMatch.Index + startMatch.Length;
            int depth = 1;
            int pos = start;

            while (pos < content.Length && depth > 0)
            {
                if (content[pos] == '{') depth++;
                else if (content[pos] == '}') depth--;
                if (depth > 0) pos++;
            }

            if (depth == 0)
                return content[start..pos];
        }

        return null;
    }

    [GeneratedRegex(@"@implements\s+([\w.]+)")]
    private static partial Regex ImplementsRegex();
}
