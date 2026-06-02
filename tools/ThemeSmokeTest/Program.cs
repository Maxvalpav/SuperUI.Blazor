using System.Text.Json;
using SuperUI.Themes;

namespace ThemeSmokeTest;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "dump" && args.Length > 1)
        {
            DumpCssFor(args[1]);
            return 0;
        }

        var dir = @"C:\Users\SuperComp\Documents\Blazor\SuperUI.Blazor\SuperUI\Themes\json";
        var files = Directory.GetFiles(dir, "*.json")
            .Where(f => File.ReadAllText(f).Contains("\"category\": \"Modern\""))
            .OrderBy(f => Path.GetFileName(f))
            .ToArray();

        var ok = 0; var fail = 0;
        foreach (var f in files)
        {
            try
            {
                var text = File.ReadAllText(f);
                var t = JsonThemeDefinition.FromJson(text);
                if (t.Dark == null)
                {
                    Console.WriteLine($"  [FAIL] {Path.GetFileName(f),-25} no Dark block");
                    fail++;
                    continue;
                }
                var lightH = t.Light.ColorPrimary;
                var darkH = t.Dark.ColorPrimary;
                IThemeSemantic lightI = t.Light;
                IThemeSemantic darkI = t.Dark;
                var lBg = lightI.BgDefault;
                var dBg = darkI.BgDefault;
                Console.WriteLine($"  [OK]   {t.Id,-25}  light.bg={lBg}  dark.bg={dBg}");

                // Verify CSS generation works for both modes.
                var css = t.GenerateCss();
                if (!css.Contains("[data-theme=\"dark\"]"))
                {
                    Console.WriteLine($"  [WARN] {t.Id,-25} CSS missing dark selector");
                }
                if (css.Length < 1000)
                {
                    Console.WriteLine($"  [WARN] {t.Id,-25} CSS too short ({css.Length} chars)");
                }

                ok++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERR]  {Path.GetFileName(f),-25} {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"OK: {ok}/{files.Length}   Failed: {fail}");
        return fail == 0 ? 0 : 1;
    }

    private static void DumpCssFor(string themeId)
    {
        var path = $@"C:\Users\SuperComp\Documents\Blazor\SuperUI.Blazor\SuperUI\Themes\json\{themeId}.json";
        if (!File.Exists(path)) { Console.WriteLine($"Theme {themeId} not found"); return; }
        var t = JsonThemeDefinition.FromJson(File.ReadAllText(path));
        var css = t.GenerateCss();
        var darkStart = css.IndexOf("[data-theme=\"dark\"]");
        var darkBlock = darkStart >= 0 ? css.Substring(darkStart) : "(none)";
        Console.WriteLine($"=== {themeId} (total CSS: {css.Length} chars, dark block: {darkBlock.Length} chars) ===");
        Console.WriteLine("--- dark block (key sections) ---");
        // Print only the var names (line-starting with --) from the dark block.
        foreach (var line in darkBlock.Split('\n').Take(4000))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("--"))
            {
                Console.WriteLine("  " + trimmed);
            }
        }
    }
}
