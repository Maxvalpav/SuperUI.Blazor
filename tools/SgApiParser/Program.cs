using System.Text.Json;
using System.Text.Json.Serialization;

namespace SgApiParser;

public class Program
{
    private static readonly string s_solutionRoot = FindSolutionRoot();
    private static readonly string s_componentsDir = Path.Combine(s_solutionRoot, "SuperUI", "Components");
    private static readonly string s_enumsDir = Path.Combine(s_solutionRoot, "SuperUI", "Enums");
    private static readonly string s_demoDir = Path.Combine(s_solutionRoot, "SuperUI.Demo", "Components", "Pages");
    private static readonly string s_testsDir = Path.Combine(s_solutionRoot, "SuperUI.Tests");

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            Console.WriteLine("""
                SgApiParser — SuperUI component API analyzer

                Usage:
                  --component <name>     Analyze one component (e.g. SgMention, SgModal)
                  --path <path>          Analyze component by file path
                  --all                  Analyze all components
                  --out <file>           Write output to file (default: stdout)
                  --json                 Format output as JSON (default)
                  --help / -h            Show this help
                """);
            return 0;
        }

        var components = new List<ComponentInfo>();
        var enumNames = LoadEnumNames();
        int exitCode = 0;

        if (TryGetArg(args, "--component") is string compName)
        {
            var info = AnalyzeComponent(compName, enumNames);
            if (info is not null)
                components.Add(info);
            else
            {
                Console.Error.WriteLine($"Component '{compName}' not found");
                exitCode = 1;
            }
        }
        else if (TryGetArg(args, "--path") is string path)
        {
            var info = AnalyzeFile(path, enumNames);
            if (info is not null)
                components.Add(info);
            else
            {
                Console.Error.WriteLine($"File '{path}' not found or not parseable");
                exitCode = 1;
            }
        }
        else if (args.Contains("--all"))
        {
            foreach (var comp in DiscoverComponents())
            {
                var info = AnalyzeComponent(comp, enumNames);
                if (info is not null)
                    components.Add(info);
            }
            Console.Error.WriteLine($"Parsed {components.Count} components");
        }
        else
        {
            Console.Error.WriteLine("Specify --component, --path, or --all. Use --help for details.");
            return 1;
        }

        string json;
        if (components.Count == 1)
            json = JsonSerializer.Serialize(components[0], s_jsonOpts);
        else
            json = JsonSerializer.Serialize(components, s_jsonOpts);

        if (TryGetArg(args, "--out") is string outFile)
        {
            var fullPath = Path.GetFullPath(outFile, s_solutionRoot);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(fullPath, json);
            Console.Error.WriteLine($"Written to {fullPath}");
        }
        else
        {
            Console.WriteLine(json);
        }

        return exitCode;
    }

    // ── Component discovery ──────────────────────────────────────────────────

    private static List<string> DiscoverComponents()
    {
        var names = new List<string>();
        foreach (var dir in Directory.EnumerateDirectories(s_componentsDir, "*", SearchOption.AllDirectories))
        {
            var razorFiles = Directory.GetFiles(dir, "*.razor")
                .Where(f => Path.GetFileName(f) != "_Imports.razor")
                .ToList();

            foreach (var rf in razorFiles)
            {
                var name = Path.GetFileNameWithoutExtension(rf);
                if (name.StartsWith("Sg") && !names.Contains(name))
                    names.Add(name);
            }
        }
        return names;
    }

    // ── Single component analysis ────────────────────────────────────────────

    private static ComponentInfo? AnalyzeComponent(string name, List<string> enumNames)
    {
        // Find razor file
        var razorFile = FindRazorFile(name);
        if (razorFile is null) return null;

        return AnalyzeFile(razorFile, enumNames);
    }

    private static ComponentInfo? AnalyzeFile(string filePath, List<string> enumNames)
    {
        if (!File.Exists(filePath)) return null;

        var info = new ComponentInfo();

        // Component name from file name
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (fileName.EndsWith(".razor")) fileName = fileName[..^6];
        info.Name = fileName;

        // Determine file kind
        var codeBehind = Path.ChangeExtension(filePath, ".razor.cs");
        bool hasCodeBehind = File.Exists(codeBehind);

        // Parse .razor directives
        var razorContent = File.ReadAllText(filePath);
        var razorParser = new RazorFileParser();
        razorParser.Parse(razorContent);

        info.Namespace = razorParser.Namespace ?? "";
        info.Inherits = razorParser.Inherits;
        info.Implements = razorParser.Implements;
        info.FileKind = hasCodeBehind ? "code-behind" : "single-file";
        info.FilePath = filePath;
        info.LineCount = razorContent.Split('\n').Length;
        info.HasCss = File.Exists(Path.ChangeExtension(filePath, ".razor.css"));
        info.HasDemo = FindDemoPage(info.Name) is not null;
        info.HasTests = FindTestFile(info.Name) is not null;

        // Check for JS interop signals
        var jsModulePattern = @"ModulePath\s*=>\s*""([^""]+)""";
        var jsModulePath = System.Text.RegularExpressions.Regex.Match(razorContent, jsModulePattern);
        info.ModulePath = jsModulePath.Success ? jsModulePath.Groups[1].Value : null;
        info.UsesJsInterop = info.ModulePath is not null
                             || razorContent.Contains("SafeInvoke")
                             || razorContent.Contains("JS.Invoke");

        if (hasCodeBehind)
        {
            var csContent = File.ReadAllText(codeBehind);
            var walker = new ComponentWalker { EnumNames = enumNames };
            walker.Parse(CombineWithImports(csContent), codeBehind);
            MergeWalkerInfo(info, walker);
        }

        if (razorParser.CodeBlock is not null)
        {
            var walker = new ComponentWalker { EnumNames = enumNames };
            var code = razorParser.CodeBlock;

            // Wrap in a class for Roslyn to parse
            var ns = razorParser.Namespace ?? "SuperUI.Components";
            var inherits = razorParser.Inherits is not null ? $" : {razorParser.Inherits}" : "";
            var wrapper = $@"
namespace {ns};
public partial class {info.Name}{inherits}
{{
    {code}
}}";
            walker.Parse(wrapper, filePath);
            MergeWalkerInfo(info, walker);
        }

        // Inherits checks
        if (info.Inherits is not null)
        {
            if (info.Inherits.Contains("SgJsComponentBase") || info.Inherits.Contains("SgOverlayComponentBase"))
                info.UsesJsInterop = true;
        }

        return info;
    }

    private static void MergeWalkerInfo(ComponentInfo info, ComponentWalker walker)
    {
        foreach (var p in walker.Info.Parameters)
        {
            if (!info.Parameters.Any(ex => ex.Name == p.Name))
                info.Parameters.Add(p);
        }
        foreach (var e in walker.Info.Events)
        {
            if (!info.Events.Any(ex => ex.Name == e.Name))
                info.Events.Add(e);
        }
        if (walker.Info.HasJsInvokable) info.HasJsInvokable = true;
        foreach (var m in walker.Info.JsInvokableMethods)
            if (!info.JsInvokableMethods.Contains(m))
                info.JsInvokableMethods.Add(m);
        foreach (var en in walker.Info.EnumsUsed)
            if (!info.EnumsUsed.Contains(en))
                info.EnumsUsed.Add(en);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? FindRazorFile(string name)
    {
        // Check both naming patterns: SgName/SgName.razor and SgName.razor
        var candidates = new[]
        {
            Path.Combine(s_componentsDir, "**", name, $"{name}.razor"),
            Path.Combine(s_componentsDir, "**", $"{name}.razor"),
        };

        // Use manual search since glob is not available
        foreach (var pattern in new[] { $"{name}.razor", $"{name}\\{name}.razor" })
        {
            var files = Directory.GetFiles(s_componentsDir, "*.razor", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (files.Count > 0) return files[0];
        }

        return null;
    }

    private static string? FindDemoPage(string componentName)
    {
        var demoName = componentName.Replace("Sg", "") + "Demo.razor";
        var file = Path.Combine(s_demoDir, demoName);
        return File.Exists(file) ? file : null;
    }

    private static string? FindTestFile(string componentName)
    {
        var files = Directory.GetFiles(s_testsDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"));
        return files.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f).Contains(componentName, StringComparison.OrdinalIgnoreCase)
            && !Path.GetFileNameWithoutExtension(f).Contains("GlobalUsings"));
    }

    private static List<string> LoadEnumNames()
    {
        if (!Directory.Exists(s_enumsDir))
        {
            Console.Error.WriteLine($"Warning: Enums directory not found at {s_enumsDir}");
            return new();
        }
        return Directory.GetFiles(s_enumsDir, "*.cs")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList()!;
    }

    private static string CombineWithImports(string code)
    {
        var usings = new[]
        {
            "using System;",
            "using System.Collections.Generic;",
            "using System.Threading.Tasks;",
            "using Microsoft.AspNetCore.Components;",
            "using Microsoft.JSInterop;",
            "using SuperUI.Components;",
            "using SuperUI.Enums;",
            "using SuperUI.Localization;",
            "using SuperUI.Services;",
        };
        return string.Join("\n", usings) + "\n" + code;
    }

    private static string? TryGetArg(string[] args, string key)
    {
        var idx = Array.IndexOf(args, key);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }

    private static string FindSolutionRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "SuperUI.slnx")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        return Directory.GetCurrentDirectory();
    }
}
