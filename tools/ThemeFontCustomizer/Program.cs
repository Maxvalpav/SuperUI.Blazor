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

        // ── 43 original themes (kept for backward compat) ───────────────
        // Each gets a distinctive body/serif/display/mono identity that
        // matches its category and character. Google Fonts only.

        ["aether"] = new(
            sans:   "Manrope, system-ui, -apple-system, sans-serif",
            serif:  "Newsreader, Georgia, serif",
            display:"'Space Grotesk', Manrope, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Manrope:wght@400;500;600;700|Newsreader:opsz,wght@6..72,400;6..72,500;6..72,600;6..72,700|Space+Grotesk:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["apex"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "Lora, Georgia, serif",
            display:"'Space Grotesk', Inter, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Lora:ital,wght@0,400;0,500;0,600;0,700;1,400|Space+Grotesk:wght@500;600;700|JetBrains+Mono:wght@400;500"),

        ["aurea"] = new(
            // Gold-tinged elegant theme. All-serif for a refined look.
            sans:   "'Playfair Display', Georgia, serif",
            serif:  "'Playfair Display', Georgia, serif",
            display:"'Playfair Display', Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Playfair+Display:ital,wght@0,400;0,500;0,600;0,700;1,400|JetBrains+Mono:wght@400;500"),

        ["biofilia"] = new(
            // Organic nature theme. Nunito for soft, rounded body.
            sans:   "Nunito, system-ui, -apple-system, sans-serif",
            serif:  "Lora, Georgia, serif",
            display:"Nunito, system-ui, sans-serif",
            mono:   "'Source Code Pro', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Nunito:wght@400;500;600;700|Lora:wght@400;500;600;700|Source+Code+Pro:wght@400;500"),

        ["calyx"] = new(
            // Book serif, literary.
            sans:   "Literata, Georgia, serif",
            serif:  "Literata, Georgia, serif",
            display:"Literata, Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Literata:opsz,wght@7..72,400;7..72,500;7..72,600;7..72,700|JetBrains+Mono:wght@400;500"),

        ["cantus"] = new(
            // Minimalist song-like.
            sans:   "'DM Sans', system-ui, -apple-system, sans-serif",
            serif:  "'Playfair Display', Georgia, serif",
            display:"'DM Sans', system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "DM+Sans:wght@400;500;600;700|Playfair+Display:ital,wght@0,400;0,500;0,600;0,700;1,400|JetBrains+Mono:wght@400;500"),

        ["chrono"] = new(
            // Time/futuristic.
            sans:   "Rajdhani, system-ui, -apple-system, sans-serif",
            serif:  "'Cormorant Garamond', Georgia, serif",
            display:"Rajdhani, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Share Tech Mono', ui-monospace, monospace",
            gfonts: "Rajdhani:wght@400;500;600;700|Cormorant+Garamond:wght@400;500;600;700|Share+Tech+Mono|JetBrains+Mono:wght@400;500"),

        ["circadian"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "'Source Serif 4', Georgia, serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500"),

        ["clarity"] = new(
            // Minimal — one font, no serif.
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "Inter, system-ui, -apple-system, sans-serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["clarity-clinical"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "Newsreader, Georgia, serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Newsreader:opsz,wght@6..72,400;6..72,500;6..72,600;6..72,700|JetBrains+Mono:wght@400;500"),

        ["cosmos"] = new(
            sans:   "'Space Grotesk', system-ui, -apple-system, sans-serif",
            serif:  "'Cormorant Garamond', Georgia, serif",
            display:"'Space Grotesk', system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Space+Grotesk:wght@400;500;600;700|Cormorant+Garamond:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["element"] = new(
            // Space Grotesk for sans + Fraunces for warm serif.
            sans:   "'Space Grotesk', system-ui, -apple-system, sans-serif",
            serif:  "Fraunces, Georgia, serif",
            display:"Fraunces, Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Space+Grotesk:wght@400;500;600;700|Fraunces:ital,opsz,wght,SOFT@0,9..144,300..800,50;1,9..144,300..800,50|JetBrains+Mono:wght@400;500"),

        ["ergo"] = new(
            sans:   "'IBM Plex Sans', system-ui, -apple-system, sans-serif",
            serif:  "'IBM Plex Serif', Georgia, serif",
            display:"'IBM Plex Sans', system-ui, sans-serif",
            mono:   "'IBM Plex Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "IBM+Plex+Sans:wght@400;500;600;700|IBM+Plex+Serif:wght@400;600;700|IBM+Plex+Mono:wght@400;500"),

        ["flux"] = new(
            // Outfit + Clash Display (variable display font).
            sans:   "Outfit, system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"'Clash Display', Outfit, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Outfit:wght@400;500;600;700|Inter:wght@400;500;600;700|Clash+Display:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["forest"] = new(
            sans:   "'Source Sans 3', system-ui, -apple-system, sans-serif",
            serif:  "'Source Serif 4', Georgia, serif",
            display:"'Source Serif 4', Georgia, serif",
            mono:   "'Source Code Pro', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Source+Sans+3:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|Source+Code+Pro:wght@400;500"),

        ["forge"] = new(
            // Mono-heavy forge theme.
            sans:   "'IBM Plex Mono', 'IBM Plex Sans', monospace, sans-serif",
            serif:  "'IBM Plex Serif', Georgia, serif",
            display:"'IBM Plex Sans', system-ui, sans-serif",
            mono:   "'IBM Plex Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "IBM+Plex+Mono:wght@400;500;600|IBM+Plex+Sans:wght@400;500;600;700|IBM+Plex+Serif:wght@400;600"),

        ["fractalis"] = new(
            // Geometric futuristic.
            sans:   "Orbitron, system-ui, -apple-system, sans-serif",
            serif:  "'Space Grotesk', sans-serif",
            display:"Orbitron, system-ui, sans-serif",
            mono:   "'Space Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Orbitron:wght@400;500;600;700;800|Space+Grotesk:wght@500;600;700|Space+Mono:wght@400;700"),

        ["glass"] = new(
            sans:   "'Plus Jakarta Sans', system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"'Plus Jakarta Sans', system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Plus+Jakarta+Sans:ital,wght@0,400;0,500;0,600;0,700;1,400|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["glass-dark"] = new(
            sans:   "'Space Grotesk', system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"'Space Grotesk', system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Space+Grotesk:wght@300;400;500;600;700|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["glass-light"] = new(
            sans:   "'Plus Jakarta Sans', system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"'Plus Jakarta Sans', system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Plus+Jakarta+Sans:ital,wght@0,300;0,400;0,500;0,600;0,700;1,400|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["glass-neumorphic"] = new(
            sans:   "Nunito, system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"Nunito, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Nunito:wght@300;400;500;600;700|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["glass-tinted"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@300;400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["gordian"] = new(
            // Knot, elegant.
            sans:   "'Cormorant Garamond', Georgia, serif",
            serif:  "'Cormorant Garamond', Georgia, serif",
            display:"'Cormorant Garamond', Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Cormorant+Garamond:ital,wght@0,400;0,500;0,600;0,700;1,400|JetBrains+Mono:wght@400;500"),

        ["graphite"] = new(
            sans:   "'IBM Plex Sans', system-ui, -apple-system, sans-serif",
            serif:  "'IBM Plex Serif', Georgia, serif",
            display:"'IBM Plex Serif', Georgia, serif",
            mono:   "'IBM Plex Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "IBM+Plex+Sans:wght@400;500;600;700|IBM+Plex+Serif:wght@400;600;700|IBM+Plex+Mono:wght@400;500"),

        ["inclus"] = new(
            // Accessibility-first.
            sans:   "'Atkinson Hyperlegible', system-ui, -apple-system, sans-serif",
            serif:  "'Atkinson Hyperlegible', system-ui, sans-serif",
            display:"'Atkinson Hyperlegible', system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Atkinson+Hyperlegible:ital,wght@0,400;0,700;1,400;1,700|JetBrains+Mono:wght@400;500"),

        ["lumina"] = new(
            sans:   "'Atkinson Hyperlegible', Inter, system-ui, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"'Atkinson Hyperlegible', system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Atkinson+Hyperlegible:ital,wght@0,400;0,700;1,400;1,700|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["medici"] = new(
            sans:   "'Atkinson Hyperlegible', Inter, system-ui, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"'Atkinson Hyperlegible', system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Atkinson+Hyperlegible:ital,wght@0,400;0,700;1,400;1,700|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["muse"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "'Playfair Display', Georgia, serif",
            display:"'Playfair Display', Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Playfair+Display:ital,wght@0,400;0,500;0,600;0,700;1,400|JetBrains+Mono:wght@400;500"),

        ["natura-ui"] = new(
            // Default theme - Inter + Source Serif 4.
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "'Source Serif 4', Georgia, serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500"),

        ["neo"] = new(
            sans:   "Manrope, system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"Manrope, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Manrope:wght@400;500;600;700|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["neon"] = new(
            sans:   "Orbitron, system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"Orbitron, system-ui, sans-serif",
            mono:   "'Share Tech Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Orbitron:wght@400;500;600;700;800|Inter:wght@400;500;600;700|Share+Tech+Mono|JetBrains+Mono:wght@400;500"),

        ["oasis"] = new(
            sans:   "Lora, Georgia, serif",
            serif:  "Lora, Georgia, serif",
            display:"Lora, Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Lora:ital,wght@0,400;0,500;0,600;0,700;1,400|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["prism"] = new(
            sans:   "Outfit, system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"Outfit, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Outfit:wght@400;500;600;700|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["radius"] = new(
            sans:   "Manrope, system-ui, -apple-system, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"Manrope, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Manrope:wght@400;500;600;700|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["reader"] = new(
            sans:   "Literata, Georgia, serif",
            serif:  "'Source Serif 4', Georgia, serif",
            display:"Literata, Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Literata:opsz,wght@7..72,400;7..72,500;7..72,600;7..72,700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500"),

        ["royal"] = new(
            sans:   "'Cormorant Garamond', Georgia, serif",
            serif:  "'Cormorant Garamond', Georgia, serif",
            display:"'Cormorant Garamond', Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Cormorant+Garamond:ital,wght@0,400;0,500;0,600;0,700;1,400|JetBrains+Mono:wght@400;500"),

        ["signature"] = new(
            // Hand-written modern serif.
            sans:   "Fraunces, Georgia, serif",
            serif:  "'Playfair Display', Georgia, serif",
            display:"Fraunces, Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Fraunces:ital,opsz,wght,SOFT@0,9..144,300..800,50;1,9..144,300..800,50|Playfair+Display:ital,wght@0,400;0,500;0,600;0,700;1,400|Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["solaris"] = new(
            sans:   "'DM Sans', system-ui, -apple-system, sans-serif",
            serif:  "'DM Serif Display', Georgia, serif",
            display:"'DM Serif Display', Georgia, serif",
            mono:   "'DM Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "DM+Sans:wght@400;500;600;700|DM+Serif+Display:ital@0;1|DM+Mono:wght@400;500|JetBrains+Mono:wght@400;500"),

        ["sylvan"] = new(
            sans:   "Fraunces, Georgia, serif",
            serif:  "Lora, Georgia, serif",
            display:"Fraunces, Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Fraunces:ital,opsz,wght,SOFT@0,9..144,300..800,50;1,9..144,300..800,50|Lora:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["veiled"] = new(
            // Mystery serif. Was system-only, now using Google Fonts.
            sans:   "Newsreader, Georgia, serif",
            serif:  "Lora, Georgia, serif",
            display:"Newsreader, Georgia, serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Newsreader:opsz,wght@6..72,400;6..72,500;6..72,600;6..72,700|Lora:wght@400;500;600;700|JetBrains+Mono:wght@400;500"),

        ["wave"] = new(
            sans:   "Inter, system-ui, -apple-system, sans-serif",
            serif:  "'Source Serif 4', Georgia, serif",
            display:"Inter, system-ui, sans-serif",
            mono:   "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
            gfonts: "Inter:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500"),

        ["window"] = new(
            // Was Windows system-only. Source Sans 3 is closest free Segoe.
            sans:   "'Source Sans 3', 'Segoe UI', system-ui, sans-serif",
            serif:  "Inter, Georgia, serif",
            display:"'Source Sans 3', 'Segoe UI', system-ui, sans-serif",
            mono:   "'Fira Code', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Source+Sans+3:wght@400;500;600;700|Inter:wght@400;500;600;700|Fira+Code:wght@400;500"),

        ["zen"] = new(
            sans:   "'Noto Sans JP', 'Hiragino Sans', system-ui, sans-serif",
            serif:  "'Noto Serif JP', Georgia, serif",
            display:"'Noto Sans JP', 'Hiragino Sans', sans-serif",
            mono:   "'Noto Sans Mono', 'JetBrains Mono', ui-monospace, monospace",
            gfonts: "Noto+Sans+JP:wght@400;500;600;700|Noto+Serif+JP:wght@400;500;700|Noto+Sans+Mono:wght@400;500;600|JetBrains+Mono:wght@400;500"),
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
