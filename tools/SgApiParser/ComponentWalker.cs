using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SgApiParser;

public class ComponentWalker
{
    public ComponentInfo Info { get; } = new();
    public List<string> Errors { get; } = new();
    public List<string> EnumNames { get; set; } = new();

    public void Parse(string csharpCode, string filePath)
    {
        var tree = CSharpSyntaxTree.ParseText(csharpCode, new CSharpParseOptions(LanguageVersion.Latest));
        var root = tree.GetRoot();

        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDecl is null)
        {
            Errors.Add($"No class found in '{filePath}'");
            return;
        }

        Info.Name = classDecl.Identifier.Text;
        Info.FilePath = filePath;
        Info.LineCount = csharpCode.Split('\n').Length;

        // Class modifiers
        foreach (var mod in classDecl.Modifiers)
            Info.Name = $"{mod.Text} {Info.Name}"; // not ideal, but works

        // Base type
        if (classDecl.BaseList?.Types.Count > 0)
        {
            var firstBase = classDecl.BaseList.Types[0].Type;
            Info.Inherits = firstBase.ToString();
        }

        // Walk members
        foreach (var member in classDecl.Members)
        {
            if (member is PropertyDeclarationSyntax prop)
                VisitProperty(prop);
            else if (member is MethodDeclarationSyntax method)
                VisitMethod(method);
        }
    }

    private void VisitProperty(PropertyDeclarationSyntax prop)
    {
        var hasParam = prop.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() is "Parameter" or "ParameterAttribute" or "CascadingParameter" or "CascadingParameterAttribute");

        if (!hasParam && !IsEventCallbackType(prop.Type))
            return;

        var typeStr = prop.Type.ToString();
        var isEvent = typeStr.StartsWith("EventCallback");

        var param = new ParameterInfo
        {
            Name = prop.Identifier.Text,
            Type = typeStr,
            DefaultValue = prop.Initializer?.Value?.ToString(),
            DocComment = GetDocComment(prop),
            Required = !isEvent && prop.Initializer is null && !typeStr.EndsWith("?") && typeStr is not "string" and not "RenderFragment" and not "RenderFragment<T>",
        };

        // Track enum usage
        foreach (var enumName in EnumNames)
        {
            if (typeStr.Contains(enumName) && !Info.EnumsUsed.Contains(enumName))
                Info.EnumsUsed.Add(enumName);
        }

        if (isEvent)
            Info.Events.Add(param);
        else
            Info.Parameters.Add(param);
    }

    private void VisitMethod(MethodDeclarationSyntax method)
    {
        var hasJsInvokable = method.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString() is "JSInvokable" or "JSInvokableAttribute");

        if (!hasJsInvokable) return;

        Info.HasJsInvokable = true;
        Info.JsInvokableMethods.Add(method.Identifier.Text);

        // Any [JSInvokable] method means JS interop is used
        Info.UsesJsInterop = true;
    }

    private static bool IsEventCallbackType(TypeSyntax type)
    {
        var text = type.ToString();
        return text.StartsWith("EventCallback");
    }

    private static string? GetDocComment(MemberDeclarationSyntax member)
    {
        var trivia = member.GetLeadingTrivia()
            .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

        if (trivia.IsKind(SyntaxKind.None))
            return null;

        var content = trivia.ToFullString();
        return StripXmlMarkup(content);
    }

    private static string? StripXmlTags(string xml)
    {
        if (string.IsNullOrEmpty(xml)) return null;
        // Extract <summary> content
        var match = System.Text.RegularExpressions.Regex.Match(xml,
            @"<summary>\s*(.*?)\s*</summary>",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        var text = match.Success ? match.Groups[1].Value : xml;
        return CleanDocText(text);
    }

    private static string? StripXmlMarkup(string xml)
    {
        if (string.IsNullOrEmpty(xml)) return null;
        // Remove XML doc comment markers: ///, /** */, etc.
        var text = System.Text.RegularExpressions.Regex.Replace(xml,
            @"^\s*///\s?|^\s*/\*\*\s?|\s*\*/\s*$|^\s*\*\s?", "",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        // Extract <summary> content
        var match = System.Text.RegularExpressions.Regex.Match(text,
            @"<summary>\s*(.*?)\s*</summary>",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        text = match.Success ? match.Groups[1].Value : text;
        return CleanDocText(text);
    }

    private static string CleanDocText(string text)
    {
        text = text.Replace("\r", "").Replace("\n", " ").Trim();
        // Collapse multiple spaces
        while (text.Contains("  ")) text = text.Replace("  ", " ");
        return text;
    }
}
