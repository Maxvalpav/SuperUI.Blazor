using System.Text;
using SuperUI.Themes;

namespace ThemeCssExporter;

internal static class Program
{
    private static int Main(string[] args)
    {
        var outDir = args.Length > 0
            ? args[0]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "SuperUI", "wwwroot", "themes", "css"));

        Directory.CreateDirectory(outDir);
        Console.WriteLine($"Output directory: {outDir}");

        var registry = new ThemeRegistry();
        var themes = registry.GetAll();
        Console.WriteLine($"Loaded {themes.Count} themes from embedded JSON.");

        var written = 0;
        var totalBytes = 0L;
        foreach (var theme in themes)
        {
            var css = SgThemeGenerator.GenerateFullThemeCss(theme);
            var path = Path.Combine(outDir, theme.Id + ".css");

            // Stable, LF-only line endings so the .css files don't churn
            // every time someone runs the tool on a different OS.
            var normalized = css.Replace("\r\n", "\n").Replace("\r", "\n");
            File.WriteAllText(path, normalized, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            var size = new FileInfo(path).Length;
            totalBytes += size;
            Console.WriteLine($"  {theme.Id,-24} -> {Path.GetFileName(path),-32}  {size,6} bytes");
            written++;
        }

        Console.WriteLine();
        Console.WriteLine($"Done. {written} themes exported, {totalBytes:N0} bytes total.");
        Console.WriteLine($"Path: {outDir}");
        return 0;
    }
}
