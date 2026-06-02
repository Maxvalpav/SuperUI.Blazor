// tools/ThemeV2Refactor/Program.cs
// One-shot: brings every theme JSON up to the v2.0 token system.
//
// For each of the 63 themes in SuperUI/Themes/json/ this tool adds:
//   1. light.state + dark.state (FgPlaceholder, SurfaceHover/Active/Selected,
//      BorderHover/Focus, ColorPrimaryDisabled/DisabledBg, ColorSuccess/Danger/
//      Warning/Info ActiveBg/Disabled) - 16 tokens
//   2. light.elevation{1..5} + dark.elevation{1..5} - 5 shadow levels
//   3. light.motion + dark.motion (Instant/Fast/Base/Slow/Slower ms +
//      EasingStandard/Emphasis/Decel) - 8 tokens, theme-agnostic Fibonacci
//   4. light.density + dark.density (Compact/Comfortable/Spacious) - 3 tokens
//   5. light.measure + dark.measure (Narrow/Optimal/Wide in ch) - 3 tokens
//   6. primitives.fonts.display + primitives.fonts.medical (already partially
//      present in some themes; always normalised)
//   7. typography.serifFont + displayFont + medicalFont (from the font
//      customizer's profile, already in JSON but normalised here too)
//   8. components.select + checkbox + switch + dropdown + alert + badge +
//      chip + spinner + progress (9 new component groups, mirror the
//      BaseComponents C# defaults so the JSON is self-contained)
//
// Idempotent: each section is gated on the field's existence. Re-runs are
// no-ops. To re-apply after a defaults change, delete the field first or
// use --force to overwrite.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ThemeV2Refactor;

internal static class Program
{
    private static int Main(string[] args)
    {
        var jsonDir = args.Length > 0 && !args[0].StartsWith("--")
            ? args[0]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "…", "..", "..", "SuperUI", "Themes", "json"));
        // resolve properly:
        jsonDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "…", "..", "SuperUI", "Themes", "json")
            .Replace("…", ".."));

        if (!Directory.Exists(jsonDir))
        {
            Console.Error.WriteLine($"Theme JSON directory not found: {jsonDir}");
            return 1;
        }

        Console.WriteLine($"Source: {jsonDir}");
        Console.WriteLine();

        var files = Directory.GetFiles(jsonDir, "*.json").OrderBy(f => f).ToArray();
        var updated = 0;

        var jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        foreach (var file in files)
        {
            var id = Path.GetFileNameWithoutExtension(file);
            var text = File.ReadAllText(file);
            var node = JsonNode.Parse(text)!;
            var anyChange = false;

            // ── 1-2. state (light + dark) ─────────────────────────────
            anyChange |= AddV2State(node);

            // ── 3-4. elevation (light + dark) ─────────────────────────
            anyChange |= AddV2Elevation(node);

            // ── 5-6. motion (light + dark) ────────────────────────────
            anyChange |= AddV2Motion(node);

            // ── 7-8. density (light + dark) ───────────────────────────
            anyChange |= AddV2Density(node);

            // ── 9-10. measure (light + dark) ──────────────────────────
            anyChange |= AddV2Measure(node);

            // ── 11. primitives.fonts.display + medical ────────────────
            anyChange |= AddFontSlots(node);

            // ── 12-14. typography.serifFont + displayFont + medicalFont
            //            (already written by ThemeFontCustomizer; no-op
            //            here unless missing - defensive guard)
            anyChange |= AddTypographyFontSlots(node);

            // ── 15. components: 9 new groups ───────────────────────────
            anyChange |= AddV2Components(node);

            if (anyChange)
            {
                var updated2 = node.ToJsonString(jsonOpts);
                updated2 = updated2.Replace("\r\n", "\n").Replace("\r", "\n");
                File.WriteAllText(file, updated2, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                Console.WriteLine($"  [updated] {id}");
                updated++;
            }
            else
            {
                Console.WriteLine($"  [skip]    {id} (v2.0 already present)");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Done. Updated: {updated}  Skipped: {files.Length - updated}  Total: {files.Length}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("  1) dotnet build SuperUI");
        Console.WriteLine("  2) dotnet build tools/ThemeCssExporter/ThemeCssExporter.csproj");
        Console.WriteLine("  3) dotnet tools/ThemeCssExporter/bin/Debug/net10.0/ThemeCssExporter.dll");
        Console.WriteLine("  4) dotnet build SuperUI.Demo");
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────
    // 1-2. State tokens
    // ─────────────────────────────────────────────────────────────────

    private static bool AddV2State(JsonNode node)
    {
        var changed = false;
        foreach (var mode in new[] { "light", "dark" })
        {
            var block = node[mode] as JsonObject;
            if (block is null) continue;
            if (block.ContainsKey("state")) continue;

            // Derive from existing tokens where possible so the values
            // match the theme's actual color identity.
            block["state"] = new JsonObject
            {
                ["fgPlaceholder"]        = block["fg"]?["muted"]?.GetValue<string>() ?? "var(--sg-fg-muted)",
                ["surfaceHover"]         = block["bg"]?["subtle"]?.GetValue<string>() ?? "var(--sg-bg-subtle)",
                ["surfaceActive"]        = block["bg"]?["muted"]?.GetValue<string>() ?? "var(--sg-bg-muted)",
                ["surfaceSelected"]      = block["colorPrimary"]?["subtle"]?.GetValue<string>() ?? "var(--sg-color-primary-subtle)",
                ["borderHover"]          = block["border"]?["strong"]?.GetValue<string>() ?? "var(--sg-border-strong)",
                ["borderFocus"]          = block["border"]?["focus"]?.GetValue<string>() ?? "var(--sg-border-focus)",
                ["colorPrimaryDisabled"]   = block["fg"]?["disabled"]?.GetValue<string>() ?? "var(--sg-fg-disabled)",
                ["colorPrimaryDisabledBg"] = block["bg"]?["muted"]?.GetValue<string>() ?? "var(--sg-bg-muted)",
                ["colorPrimaryActiveBg"]   = block["colorPrimary"]?["active"]?.GetValue<string>() ?? block["colorPrimary"]?["hover"]?.GetValue<string>() ?? "var(--sg-color-primary-active)",
                ["colorSuccessActiveBg"]   = block["colorSuccess"]?["hover"]?.GetValue<string>() ?? "var(--sg-color-success-hover)",
                ["colorSuccessDisabled"]   = block["fg"]?["disabled"]?.GetValue<string>() ?? "var(--sg-fg-disabled)",
                ["colorDangerActiveBg"]    = block["colorDanger"]?["hover"]?.GetValue<string>() ?? "var(--sg-color-danger-hover)",
                ["colorDangerDisabled"]    = block["fg"]?["disabled"]?.GetValue<string>() ?? "var(--sg-fg-disabled)",
                ["colorWarningActiveBg"]   = block["colorWarning"]?["hover"]?.GetValue<string>() ?? "var(--sg-color-warning-hover)",
                ["colorWarningDisabled"]   = block["fg"]?["disabled"]?.GetValue<string>() ?? "var(--sg-fg-disabled)",
                ["colorInfoActiveBg"]      = block["colorInfo"]?["hover"]?.GetValue<string>() ?? "var(--sg-color-info-hover)",
                ["colorInfoDisabled"]      = block["fg"]?["disabled"]?.GetValue<string>() ?? "var(--sg-fg-disabled)",
            };
            changed = true;
        }
        return changed;
    }

    // ─────────────────────────────────────────────────────────────────
    // 3-4. Elevation (5 levels, theme-agnostic shadow formulas)
    // ─────────────────────────────────────────────────────────────────

    private static bool AddV2Elevation(JsonNode node)
    {
        var changed = false;
        foreach (var mode in new[] { "light", "dark" })
        {
            var block = node[mode] as JsonObject;
            if (block is null) continue;
            if (block.ContainsKey("elevation")) continue;

            var isDark = mode == "dark";
            // Dark uses pure black with higher alpha; light uses brand-tinted.
            var shadowColor = isDark ? "oklch(0 0 0 / " : "oklch(0.14 0.02 240 / ";
            var alphaClose = isDark ? "0.40)" : "0.04)";
            var alphaFar   = isDark ? "0.65)" : "0.14)";

            block["elevation"] = new JsonObject
            {
                ["1"] = $"0 1px 2px 0 {shadowColor}{alphaClose}",
                ["2"] = $"0 1px 2px 0 {shadowColor}{alphaClose.Replace(")", ", 0 1px 1px -1px ").Replace(isDark ? "0.40)" : "0.04)", isDark ? "0.50)" : "0.06)")}",
                ["3"] = $"0 2px 4px -1px {shadowColor}{(isDark ? "0.50)" : "0.08)")}, 0 1px 2px -1px {shadowColor}{(isDark ? "0.50)" : "0.06)")}",
                ["4"] = $"0 8px 16px -4px {shadowColor}{(isDark ? "0.55)" : "0.10)")}, 0 2px 4px -2px {shadowColor}{(isDark ? "0.50)" : "0.06)")}",
                ["5"] = $"0 16px 32px -8px {shadowColor}{alphaFar}, 0 4px 8px -4px {shadowColor}{(isDark ? "0.55)" : "0.08)")}",
            };
            changed = true;
        }
        return changed;
    }

    // ─────────────────────────────────────────────────────────────────
    // 5-6. Motion (Fibonacci ms + easings) - theme-agnostic
    // ─────────────────────────────────────────────────────────────────

    private static bool AddV2Motion(JsonNode node)
    {
        var changed = false;
        foreach (var mode in new[] { "light", "dark" })
        {
            var block = node[mode] as JsonObject;
            if (block is null) continue;
            if (block.ContainsKey("motion")) continue;

            block["motion"] = new JsonObject
            {
                ["instant"]        = "89ms",
                ["fast"]           = "144ms",
                ["base"]           = "233ms",
                ["slow"]           = "377ms",
                ["slower"]         = "610ms",
                ["easingStandard"] = "cubic-bezier(0.4, 0, 0.2, 1)",
                ["easingEmphasis"] = "cubic-bezier(0.2, 0, 0, 1)",
                ["easingDecel"]    = "cubic-bezier(0, 0, 0.2, 1)",
            };
            changed = true;
        }
        return changed;
    }

    // ─────────────────────────────────────────────────────────────────
    // 7-8. Density (offset multipliers) - theme-agnostic
    // ─────────────────────────────────────────────────────────────────

    private static bool AddV2Density(JsonNode node)
    {
        var changed = false;
        foreach (var mode in new[] { "light", "dark" })
        {
            var block = node[mode] as JsonObject;
            if (block is null) continue;
            if (block.ContainsKey("density")) continue;

            block["density"] = new JsonObject
            {
                ["compact"]     = "-2px",
                ["comfortable"] = "0px",
                ["spacious"]    = "+2px",
            };
            changed = true;
        }
        return changed;
    }

    // ─────────────────────────────────────────────────────────────────
    // 9-10. Measure (reading width in ch) - theme-agnostic
    // ─────────────────────────────────────────────────────────────────

    private static bool AddV2Measure(JsonNode node)
    {
        var changed = false;
        foreach (var mode in new[] { "light", "dark" })
        {
            var block = node[mode] as JsonObject;
            if (block is null) continue;
            if (block.ContainsKey("measure")) continue;

            block["measure"] = new JsonObject
            {
                ["narrow"]  = "45ch",
                ["optimal"] = "66ch",
                ["wide"]    = "75ch",
            };
            changed = true;
        }
        return changed;
    }

    // ─────────────────────────────────────────────────────────────────
    // 11. primitives.fonts.display + medical
    //     (sans/serif/mono already exist; add the v2.0 slots if missing)
    // ─────────────────────────────────────────────────────────────────

    private static bool AddFontSlots(JsonNode node)
    {
        var fonts = node["primitives"]?["fonts"] as JsonObject;
        if (fonts is null) return false;
        var changed = false;

        if (!fonts.ContainsKey("display"))
        {
            fonts["display"] = fonts["sans"]?.GetValue<string>() ?? "Inter, system-ui, sans-serif";
            changed = true;
        }
        if (!fonts.ContainsKey("medical"))
        {
            fonts["medical"] = fonts["mono"]?.GetValue<string>() ?? "'JetBrains Mono', monospace";
            changed = true;
        }
        return changed;
    }

    // ─────────────────────────────────────────────────────────────────
    // 12-14. typography font slots - defensive
    //     (ThemeFontCustomizer writes these; ensure they exist)
    // ─────────────────────────────────────────────────────────────────

    private static bool AddTypographyFontSlots(JsonNode node)
    {
        var typography = node["typography"] as JsonObject;
        if (typography is null) return false;
        var changed = false;
        var fonts = node["primitives"]?["fonts"] as JsonObject;
        if (fonts is null) return false;

        if (!typography.ContainsKey("serifFont"))
        {
            typography["serifFont"] = fonts["serif"]?.GetValue<string>() ?? "Georgia, serif";
            changed = true;
        }
        if (!typography.ContainsKey("displayFont"))
        {
            typography["displayFont"] = fonts["display"]?.GetValue<string>() ?? "Inter, system-ui, sans-serif";
            changed = true;
        }
        if (!typography.ContainsKey("medicalFont"))
        {
            typography["medicalFont"] = fonts["medical"]?.GetValue<string>() ?? "'JetBrains Mono', monospace";
            changed = true;
        }
        return changed;
    }

    // ─────────────────────────────────────────────────────────────────
    // 15. v2.0 components: 9 new groups
    //     (mirror BaseComponents C# defaults so the JSON is self-contained)
    // ─────────────────────────────────────────────────────────────────

    private static bool AddV2Components(JsonNode node)
    {
        var components = node["components"] as JsonObject;
        if (components is null) return false;
        var changed = false;

        if (!components.ContainsKey("select"))
        {
            components["select"] = new JsonObject
            {
                ["radius"]   = "3px",
                ["fontSize"] = "0.8125rem",
                ["height"]   = "30px",
                ["heightSm"] = "24px",
                ["heightLg"] = "36px",
                ["paddingX"] = "5px",
                ["iconSize"] = "12px",
            };
            changed = true;
        }
        if (!components.ContainsKey("checkbox"))
        {
            components["checkbox"] = new JsonObject
            {
                ["size"]        = "13px",
                ["sizeSm"]      = "8px",
                ["sizeLg"]      = "21px",
                ["radius"]      = "2px",
                ["iconSize"]    = "8px",
                ["borderWidth"] = "1px",
            };
            changed = true;
        }
        if (!components.ContainsKey("switch"))
        {
            components["switch"] = new JsonObject
            {
                ["width"]     = "34px",
                ["height"]    = "21px",
                ["thumbSize"] = "13px",
                ["radius"]    = "9999px",
                ["padding"]   = "2px",
            };
            changed = true;
        }
        if (!components.ContainsKey("dropdown"))
        {
            components["dropdown"] = new JsonObject
            {
                ["radius"]       = "5px",
                ["padding"]      = "3px",
                ["itemHeight"]   = "21px",
                ["itemPaddingX"] = "8px",
                ["itemPaddingY"] = "0",
                ["gap"]          = "2px",
            };
            changed = true;
        }
        if (!components.ContainsKey("alert"))
        {
            components["alert"] = new JsonObject
            {
                ["radius"]    = "5px",
                ["padding"]   = "8px 13px",
                ["paddingSm"] = "5px 8px",
                ["iconSize"]  = "13px",
                ["gap"]       = "8px",
            };
            changed = true;
        }
        if (!components.ContainsKey("badge"))
        {
            components["badge"] = new JsonObject
            {
                ["radius"]     = "9999px",
                ["height"]     = "13px",
                ["heightSm"]   = "8px",
                ["heightLg"]   = "21px",
                ["paddingX"]   = "5px",
                ["fontSize"]   = "0.625rem",
                ["fontWeight"] = "600",
            };
            changed = true;
        }
        if (!components.ContainsKey("chip"))
        {
            components["chip"] = new JsonObject
            {
                ["radius"]   = "9999px",
                ["height"]   = "21px",
                ["heightSm"] = "13px",
                ["heightLg"] = "34px",
                ["paddingX"] = "8px",
                ["gap"]      = "3px",
                ["iconSize"] = "8px",
            };
            changed = true;
        }
        if (!components.ContainsKey("spinner"))
        {
            components["spinner"] = new JsonObject
            {
                ["size"]         = "13px",
                ["sizeSm"]       = "8px",
                ["sizeLg"]       = "21px",
                ["borderWidth"]  = "1px",
                ["trackOpacity"] = "0.2",
            };
            changed = true;
        }
        if (!components.ContainsKey("progress"))
        {
            components["progress"] = new JsonObject
            {
                ["height"]          = "5px",
                ["heightSm"]        = "2px",
                ["heightLg"]        = "8px",
                ["radius"]          = "9999px",
                ["indicatorRadius"] = "9999px",
            };
            changed = true;
        }
        return changed;
    }
}
