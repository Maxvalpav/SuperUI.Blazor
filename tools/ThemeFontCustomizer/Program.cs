// tools/ThemeFontCustomizer/Program.cs
// One-shot: gives each Modern theme a distinctive font identity matching its
// character. Currently all 20 Modern themes share an identical body font
// (Inter) and identical Google Fonts import URL, so the picker only changes
// colors. After running this tool + re-running ThemeCssExporter, switching
// between Modern themes also changes the typography.
//
// Idempotent: it unconditionally rewrites the font fields each run, so it is
// safe to re-execute after adjusting the profile table below.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ThemeFontCustomizer;

internal static class Program
{
    // Per-theme font profile. Each entry maps a Modern theme ID to its
    // typographic identity: which font family anchors the body (Sans), the
    // serif voice, the display voice, the mono/medical voice, and which
    // Google Fonts families (with weights) need to be loaded.
    //
    // Non-Google fonts (e.g. -apple-system, Segoe UI, Georgia) are listed in
    // the CSS stack but excluded from googleFontsImportUrl. The Google URL
    // builder below only emits families whose Name appears in the Google
    // catalog.
    private static readonly Dictionary<string, FontProfile> Profiles = new()
    {
        ["bio-signal"] = new(
            sans:   "'IBM Plex Sans', system-ui, -apple-system, sans-serif",
            serif:  "'IBM Plex Serif', Georgia, serif",
            display:"'IBM Plex Sans', system-ui, sans-serif",
            mono:   "'IBM Plex Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "IBM+Plex+Sans:wght@400;500;600;700|IBM+Plex+Serif:wght@400;500;600;700|IBM+Plex+Mono:wght@400;500"),

        ["clinical-calm"] = new(
            sans:   "Manrope, system-ui, -apple-system, sans-serif",
            serif:  "Lora, Georgia, serif",
            display:"Manrope, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Manrope:wght@400;500;600;700|Lora:ital,wght@0,400;0,500;0,600;0,700;1,400|JetBrains+Mono:wght@400;500"),

        ["precision-lab"] = new(
            sans:   "'IBM Plex Sans', system-ui, -apple-system, sans-serif",
            serif:  "'IBM Plex Serif', Georgia, serif",
            display:"'IBM Plex Sans', system-ui, sans-serif",
            mono:   "'IBM Plex Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "IBM+Plex+Sans:wght@400;500;600;700|IBM+Plex+Serif:wght@400;500;600;700|IBM+Plex+Mono:wght@400;500"),

        ["human-care"] = new(
            sans:   "'Source Sans 3', system-ui, -apple-system, sans-serif",
            serif:  "'Source Serif 4', Georgia, serif",
            display:"'Source Sans 3', system-ui, sans-serif",
            mono:   "'Source Code Pro', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Source+Sans+3:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|Source+Code+Pro:wght@400;500"),

        ["sentry-pulse"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "'Crimson Pro', Georgia, serif",
            display:"'Space Grotesk', Inter, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Crimson+Pro:wght@400;500;600;700|Space+Grotesk:wght@500;600;700|JetBrains+Mono:wght@400;500"),

        ["ios-aurora"] = new(
            // Free Google Fonts analogues of Apple's SF Pro / New York stack.
            // Inter was designed by Rasmus Andersson (ex-Apple) and is the
            // closest free match to SF Pro Text in metrics and feel.
            // Newsreader (by Production Type for Google) is the closest
            // free match to New York - same slightly-geometric book serif
            // character. JetBrains Mono approximates SF Mono's metrics.
            sans:   "Inter, system-ui, -apple-system, BlinkMacSystemFont, sans-serif",
            serif:  "Newsreader, 'New York', Georgia, serif",
            display:"Inter, system-ui, -apple-system, BlinkMacSystemFont, sans-serif",
            mono:   "'JetBrains Mono', 'SF Mono', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Newsreader:opsz,wght@6..72,400;6..72,500;6..72,600;6..72,700|JetBrains+Mono:wght@400;500"),

        ["fluent-breeze"] = new(
            // Free Google Fonts analogues of Windows Segoe UI / Constantia /
            // Cascadia Code. Source Sans 3 (Adobe's open humanist sans) is
            // the closest match to Segoe UI in x-height and stroke contrast.
            // Source Serif 4 matches Constantia's bookish warmth. Fira Code
            // (with ligatures) is the closest free match to Cascadia Code.
            sans:   "'Source Sans 3', 'Segoe UI', system-ui, sans-serif",
            serif:  "'Source Serif 4', Constantia, Georgia, serif",
            display:"'Source Sans 3', 'Segoe UI', system-ui, sans-serif",
            mono:   "'Fira Code', 'Cascadia Code', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Source+Sans+3:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|Fira+Code:wght@400;500"),

        ["sonoma-sky"] = new(
            // Free Google Fonts analogues of macOS user-installed Charter +
            // Menlo. Charter is Bitstream's book serif (shipped on macOS
            // installs) - Source Serif 4 is the closest free book serif in
            // similar weight. Inter covers the SF Pro body. JetBrains Mono
            // approximates Menlo's metrics.
            sans:   "Inter, -apple-system, BlinkMacSystemFont, system-ui, sans-serif",
            serif:  "'Source Serif 4', Charter, 'Iowan Old Style', Georgia, serif",
            display:"Inter, -apple-system, BlinkMacSystemFont, system-ui, sans-serif",
            mono:   "'JetBrains Mono', Menlo, ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500"),

        ["material-bloom"] = new(
            sans:   "Roboto, system-ui, -apple-system, sans-serif",
            serif:  "'Roboto Slab', Georgia, serif",
            display:"Roboto, system-ui, sans-serif",
            mono:   "'Roboto Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Roboto:wght@400;500;700|Roboto+Slab:wght@400;500;600;700|Roboto+Mono:wght@400;500"),

        ["nord-frost"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "Lora, Georgia, serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Lora:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["tokyo-night"] = new(
            sans:   "'Noto Sans JP', 'Hiragino Sans', system-ui, sans-serif",
            serif:  "'Noto Serif JP', Georgia, serif",
            display:"'Noto Sans JP', 'Hiragino Sans', sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Noto+Sans+JP:wght@400;500;700|Noto+Serif+JP:wght@400;500;700|JetBrains+Mono:wght@400;500"),

        ["catppuccin-mocha"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "Fraunces, Georgia, serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Fraunces:opsz,wght@9..144,400;9..144,500;9..144,600;9..144,700|JetBrains+Mono:wght@400;500"),

        ["boreal-forest"] = new(
            sans:   "'Source Sans 3', system-ui, -apple-system, sans-serif",
            serif:  "'Source Serif 4', Georgia, serif",
            display:"'Source Sans 3', system-ui, sans-serif",
            mono:   "'Source Code Pro', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Source+Sans+3:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|Source+Code+Pro:wght@400;500"),

        ["lichen-stone"] = new(
            sans:   "Lora, Georgia, serif",
            serif:  "Lora, Georgia, serif",
            display:"Lora, Georgia, serif",
            mono:   "Inconsolata, 'JetBrains Mono', monospace",
            gfonts: "Lora:ital,wght@0,400;0,500;0,600;0,700;1,400|Inconsolata:wght@400;500;600;700"),

        ["coral-reef"] = new(
            sans:   "Outfit, system-ui, -apple-system, sans-serif",
            serif:  "'Playfair Display', Georgia, serif",
            display:"'Playfair Display', Georgia, serif",
            mono:   "'Space Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Outfit:wght@400;500;600;700|Playfair+Display:ital,wght@0,400;0,500;0,600;0,700;1,400|Space+Mono:wght@400;700"),

        ["fern-canopy"] = new(
            sans:   "Manrope, system-ui, -apple-system, sans-serif",
            serif:  "Lora, Georgia, serif",
            display:"Manrope, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Manrope:wght@400;500;600;700|Lora:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["dune-warmth"] = new(
            sans:   "'DM Sans', system-ui, -apple-system, sans-serif",
            serif:  "Lora, Georgia, serif",
            display:"'DM Serif Display', Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "DM+Sans:wght@400;500;700|DM+Serif+Display:wght@400|Lora:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["dracula-dusk"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "Fraunces, Georgia, serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'Fira Code', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Fraunces:opsz,wght@9..144,400;9..144,500;9..144,600;9..144,700|Fira+Code:wght@400;500"),

        ["gruvbox-amber"] = new(
            sans:   "Iosevka, 'JetBrains Mono', ui-monospace, monospace",
            serif:  "'EB Garamond', Georgia, serif",
            display:"Iosevka, 'JetBrains Mono', ui-monospace, monospace",
            mono:   "Iosevka, 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Iosevka:wght@400;500;600;700|EB+Garamond:ital,wght@0,400;0,500;0,600;0,700;1,400"),

        ["solarized-tide"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "Bitter, Georgia, serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Bitter:ital,wght@0,400;0,500;0,600;0,700;1,400|JetBrains+Mono:wght@400;500"),
    };

    private static int Main(string[] args)
    {
        var jsonDir = args.Length > 0
            ? args[0]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "SuperUI", "Themes", "json"));

        if (!Directory.Exists(jsonDir))
        {
            Console.Error.WriteLine($"Theme JSON directory not found: {jsonDir}");
            return 1;
        }

        Console.WriteLine($"Source: {jsonDir}");
        Console.WriteLine($"Profiles: {Profiles.Count} Modern themes");
        Console.WriteLine();

        var updated = 0;
        var skipped = 0;
        var missing = new List<string>();

        foreach (var (themeId, profile) in Profiles)
        {
            var path = Path.Combine(jsonDir, themeId + ".json");
            if (!File.Exists(path))
            {
                missing.Add(themeId);
                continue;
            }

            // Parse, mutate in-place, write back.
            var json = File.ReadAllText(path);
            var node = JsonNode.Parse(json)!;

            // 1) primitives.fonts.*
            var primitives = node["primitives"]!;
            var fonts = primitives["fonts"]!;
            fonts["sans"]    = profile.sans;
            fonts["serif"]   = profile.serif;
            fonts["display"] = profile.display;
            fonts["mono"]    = profile.mono;
            fonts["medical"] = profile.mono;  // medical = mono in our model

            // 2) typography.* - same families as primitives but as plain strings
            //    (not CSS stacks). Used by SgTypography enums.
            var typography = node["typography"]!;
            typography["headingFont"] = profile.sans;        // body font doubles as heading default
            typography["serifFont"]   = profile.serif;
            typography["displayFont"] = profile.display;
            typography["medicalFont"] = profile.mono;

            // 3) Google Fonts URL - only emit if the profile references any
            //    Google families (system-only profiles get an empty string and
            //    no @import in the generated CSS).
            typography["googleFontsImportUrl"]   = profile.gfonts;
            typography["embedGoogleFontsImport"] = !string.IsNullOrEmpty(profile.gfonts);

            // 4) Write back with the same formatting the rest of the project uses.
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            var updated2 = node.ToJsonString(options);

            // Stable EOL: LF only.
            updated2 = updated2.Replace("\r\n", "\n").Replace("\r", "\n");

            File.WriteAllText(path, updated2, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            updated++;
            var gfontsLabel = string.IsNullOrEmpty(profile.gfonts) ? "(system fonts)" : profile.gfonts.Split('|').Length + " families";
            Console.WriteLine($"  {themeId,-20} -> {profile.sans.Split(',')[0],-32}  {gfontsLabel}");
        }

        Console.WriteLine();
        Console.WriteLine($"Updated: {updated} themes");
        if (skipped > 0) Console.WriteLine($"Skipped: {skipped} themes");
        if (missing.Count > 0)
        {
            Console.WriteLine($"MISSING: {string.Join(", ", missing)}");
            return 2;
        }

        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("  1) dotnet run --project tools/ThemeCssExporter");
        Console.WriteLine("  2) dotnet build SuperUI.Demo");
        return 0;
    }

    private sealed record FontProfile(
        string sans,
        string serif,
        string display,
        string mono,
        string gfonts);
}
