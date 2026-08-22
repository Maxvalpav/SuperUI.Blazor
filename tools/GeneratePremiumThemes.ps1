# Generate 23 Premium theme JSON files from natura-ui.json template
param([string]$TemplatePath = "SuperUI/Themes/json/natura-ui.json",
      [string]$OutputDir = "SuperUI/Themes/json",
      [switch]$WhatIf)

$ErrorActionPreference = "Stop"
$root = "C:\Users\SuperComp\Documents\Blazor\SuperUI.Blazor"
$templateFile = Join-Path $root $TemplatePath
$outDir = Join-Path $root $OutputDir
if (-not (Test-Path $templateFile)) { throw "Template not found: $templateFile" }

$templateRaw = Get-Content $templateFile -Raw -Encoding UTF8

# Theme definitions
$themes = @(
    # Gold Series (5)
    ,@{ id="gold-standard";    name="Gold Standard";     hue=45;  desc="Classic gold: warm, rich hue for premium brands, luxury, finance."; font="Inter" }
    ,@{ id="old-money";        name="Old Money";         hue=38;  desc="Patina gold: subdued elegance for heritage brands, conservative luxury."; font="Inter" }
    ,@{ id="honey-amber";      name="Honey Amber";       hue=42;  desc="Rich sweet amber for premium F&B, cozy luxury, handmade goods."; font="Inter" }
    ,@{ id="brass-instrument"; name="Brass Instrument";  hue=50;  desc="Yellow-gold metallic: industrial aesthetic, hi-end audio, menswear."; font="Inter" }
    ,@{ id="copper-glow";      name="Copper Glow";       hue=28;  desc="Rosy-orange metallic: warm premium, autumn collections, crafts."; font="Inter" }
    # Warm Series (5)
    ,@{ id="sun-baked";        name="Sun Baked";         hue=25;  desc="Warm terracotta: Mediterranean sun for travel, leisure, outdoor."; font="Inter" }
    ,@{ id="chili-pepper";     name="Chili Pepper";      hue=15;  desc="Energetic red for sports, street food, active lifestyle."; font="Inter" }
    ,@{ id="persimmon";        name="Persimmon";         hue=18;  desc="Juicy orange-red for autumn, creativity, inspiration."; font="Inter" }
    ,@{ id="turmeric";         name="Turmeric";          hue=48;  desc="Bright yellow-orange: sunny mood, wellness, healthy eating."; font="Inter" }
    ,@{ id="papaya-whip";      name="Papaya Whip";       hue=32;  desc="Soft rosy-orange for tropics, skincare, spa."; font="Inter" }
    # Cold / Nature Series (5)
    ,@{ id="sage-leaf";        name="Sage Leaf";         hue=135; desc="Wise green for ecology, organic, sustainable development."; font="Inter" }
    ,@{ id="sea-foam";         name="Sea Foam";          hue=175; desc="Turquoise-blue: ocean, resorts, aqua therapy."; font="Inter" }
    ,@{ id="glacier-stream";   name="Glacier Stream";    hue=200; desc="Pure blue for science, technology, healthcare."; font="Inter" }
    ,@{ id="lavender-field";   name="Lavender Field";    hue=265; desc="Soft purple for beauty, relaxation, premium skincare."; font="Inter" }
    ,@{ id="thunder-cloud";    name="Thunder Cloud";     hue=275; desc="Deep violet-blue: drama, premium fashion, nocturnal aesthetic."; font="Inter" }
    # Dark Series (4)
    ,@{ id="midnight-onyx";    name="Midnight Onyx";     hue=260; desc="Deep blue-black for luxury, minimalism, hi-tech."; font="Outfit" }
    ,@{ id="shadow-iris";      name="Shadow Iris";       hue=270; desc="Violet-black: mystique, creativity, nightclub."; font="Outfit" }
    ,@{ id="charcoal-velvet";  name="Charcoal Velvet";   hue=0;   desc="Achromatic black for absolute minimalism, industrial design."; font="Sora" }
    ,@{ id="obsidian-glow";    name="Obsidian Glow";     hue=285; desc="Violet-black sheen for gaming, cyberpunk, premium tech."; font="Outfit" }
    # Neutral Series (4)
    ,@{ id="warm-oat";         name="Warm Oat";          hue=55;  desc="Soft creamy: cozy warmth for spa, home textile."; font="Cormorant" }
    ,@{ id="cool-stone";       name="Cool Stone";        hue=240; desc="Gray-blue: architecture, minimalism, corporate."; font="Sora" }
    ,@{ id="greige";           name="Greige";            hue=40;  desc="Gray-beige for interiors, fashion, unisex."; font="Inter" }
    ,@{ id="mushroom-spore";   name="Mushroom Spore";    hue=30;  desc="Earthy gray: organic produce, forest, artisanal."; font="Cormorant" }
)

# Font profiles
$SQ = [char]0x27
function q { $SQ + $args[0] + $SQ }
$fp = @{}
$fp["Inter"] = @{
    sans    = "Inter, system-ui, -apple-system, sans-serif"
    serif   = (q 'Source Serif 4') + ", Georgia, serif"
    display = "Inter, system-ui, sans-serif"
    heading = "Inter, system-ui, -apple-system, sans-serif"
    headingFam = (q 'Inter') + ", system-ui, -apple-system, " + (q 'Segoe UI') + ", sans-serif"
    google  = "Inter:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500"
    lightFont = (q 'Inter') + ", system-ui, -apple-system, " + (q 'Segoe UI') + ", Roboto, Helvetica, Arial, sans-serif"
    darkFont  = (q 'Inter') + ", system-ui, -apple-system, " + (q 'Segoe UI') + ", sans-serif"
    fontSansPrim = (q 'Inter') + ", -apple-system, BlinkMacSystemFont, " + (q 'Segoe UI') + ", Roboto, Helvetica, Arial, sans-serif"
    medical = (q 'JetBrains Mono') + ", " + (q 'Fira Code') + ", ui-monospace, monospace"
    geoTimes = "Georgia, " + (q 'Times New Roman') + ", serif"
}
$fp["Outfit"] = @{
    sans    = (q 'Outfit') + ", system-ui, -apple-system, sans-serif"
    serif   = (q 'Source Serif 4') + ", Georgia, serif"
    display = (q 'Outfit') + ", system-ui, sans-serif"
    heading = (q 'Outfit') + ", system-ui, -apple-system, sans-serif"
    headingFam = (q 'Outfit') + ", system-ui, -apple-system, " + (q 'Segoe UI') + ", sans-serif"
    google  = "Outfit:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500"
    lightFont = (q 'Outfit') + ", system-ui, -apple-system, " + (q 'Segoe UI') + ", Roboto, Helvetica, Arial, sans-serif"
    darkFont  = (q 'Outfit') + ", system-ui, -apple-system, " + (q 'Segoe UI') + ", sans-serif"
    fontSansPrim = (q 'Outfit') + ", -apple-system, BlinkMacSystemFont, " + (q 'Segoe UI') + ", Roboto, Helvetica, Arial, sans-serif"
    medical = (q 'JetBrains Mono') + ", " + (q 'Fira Code') + ", ui-monospace, monospace"
    geoTimes = "Georgia, " + (q 'Times New Roman') + ", serif"
}
$fp["Sora"] = @{
    sans    = (q 'Sora') + ", system-ui, -apple-system, sans-serif"
    serif   = (q 'Source Serif 4') + ", Georgia, serif"
    display = (q 'Sora') + ", system-ui, sans-serif"
    heading = (q 'Sora') + ", system-ui, -apple-system, sans-serif"
    headingFam = (q 'Sora') + ", system-ui, -apple-system, " + (q 'Segoe UI') + ", sans-serif"
    google  = "Sora:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500"
    lightFont = (q 'Sora') + ", system-ui, -apple-system, " + (q 'Segoe UI') + ", Roboto, Helvetica, Arial, sans-serif"
    darkFont  = (q 'Sora') + ", system-ui, -apple-system, " + (q 'Segoe UI') + ", sans-serif"
    fontSansPrim = (q 'Sora') + ", -apple-system, BlinkMacSystemFont, " + (q 'Segoe UI') + ", Roboto, Helvetica, Arial, sans-serif"
    medical = (q 'JetBrains Mono') + ", " + (q 'Fira Code') + ", ui-monospace, monospace"
    geoTimes = "Georgia, " + (q 'Times New Roman') + ", serif"
}
$fp["Cormorant"] = @{
    sans    = (q 'Cormorant Garamond') + ", Georgia, serif"
    serif   = (q 'Cormorant Garamond') + ", Georgia, serif"
    display = (q 'Cormorant Garamond') + ", Georgia, serif"
    heading = (q 'Playfair Display') + ", Georgia, serif"
    headingFam = (q 'Playfair Display') + ", Georgia, serif"
    google  = "Playfair+Display:wght@400;500;600;700|Cormorant+Garamond:wght@400;500;600;700|JetBrains+Mono:wght@400;500"
    lightFont = (q 'Cormorant Garamond') + ", Georgia, serif"
    darkFont  = (q 'Cormorant Garamond') + ", Georgia, serif"
    fontSansPrim = (q 'Cormorant Garamond') + ", Georgia, serif"
    medical = (q 'JetBrains Mono') + ", " + (q 'Fira Code') + ", ui-monospace, monospace"
    geoTimes = "Georgia, " + (q 'Times New Roman') + ", serif"
}

# === Generate ===
$DQ = [char]0x22
foreach ($t in $themes) {
    $f = $fp[$t.font]
    $json = $templateRaw

    # Metadata (use .Replace() with simple concat)
    $json = $json.Replace('"id": "natura-ui"', ($DQ + "id" + $DQ + ": " + $DQ + $t.id + $DQ))
    $json = $json.Replace('"name": "Natura UI"', ($DQ + "name" + $DQ + ": " + $DQ + $t.name + $DQ))
    $json = $json.Replace('"category": "Core"', ($DQ + "category" + $DQ + ": " + $DQ + "Premium" + $DQ))
    $json = $json.Replace("SuperUI + Natura", "SuperUI Premium")

    # Description via regex (safe)
    $json = $json -replace '(?<="description": ")[^"]*', ($t.desc -replace '"', '\"')

    # OKLCH hue 262 -> theme hue (regex, safe)
    $json = $json -replace '(?<=oklch\([^)]*?)\b262(?=\s*\)|\s*/)', [string]$t.hue

    # Font replacements (use .Replace())
    $inter = $fp["Inter"]
    $json = $json.Replace($DQ + "sans" + $DQ + ": " + $DQ + $inter.sans + $DQ, ($DQ + "sans" + $DQ + ": " + $DQ + $f.sans + $DQ))
    $json = $json.Replace($DQ + "serif" + $DQ + ": " + $DQ + $inter.serif + $DQ, ($DQ + "serif" + $DQ + ": " + $DQ + $f.serif + $DQ))
    $json = $json.Replace($DQ + "display" + $DQ + ": " + $DQ + $inter.display + $DQ, ($DQ + "display" + $DQ + ": " + $DQ + $f.display + $DQ))
    $json = $json.Replace($inter.fontSansPrim, $f.fontSansPrim)
    $json = $json.Replace($inter.medical, $f.medical)
    $json = $json.Replace($inter.geoTimes, $f.geoTimes)
    $json = $json.Replace($DQ + "googleFontsImportUrl" + $DQ + ": " + $DQ + $inter.google + $DQ, ($DQ + "googleFontsImportUrl" + $DQ + ": " + $DQ + $f.google + $DQ))
    $json = $json.Replace($DQ + "headingFont" + $DQ + ": " + $DQ + $inter.heading + $DQ, ($DQ + "headingFont" + $DQ + ": " + $DQ + $f.heading + $DQ))
    $json = $json.Replace($inter.headingFam, $f.headingFam)
    $json = $json.Replace($DQ + "serifFont" + $DQ + ": " + $DQ + $inter.serif + $DQ, ($DQ + "serifFont" + $DQ + ": " + $DQ + $f.serif + $DQ))
    $json = $json.Replace($DQ + "displayFont" + $DQ + ": " + $DQ + $inter.display + $DQ, ($DQ + "displayFont" + $DQ + ": " + $DQ + $f.display + $DQ))
    $json = $json.Replace($DQ + "default" + $DQ + ": " + $DQ + $inter.lightFont + $DQ, ($DQ + "default" + $DQ + ": " + $DQ + $f.lightFont + $DQ))
    $json = $json.Replace($DQ + "default" + $DQ + ": " + $DQ + $inter.darkFont + $DQ, ($DQ + "default" + $DQ + ": " + $DQ + $f.darkFont + $DQ))

    # Fix additionalCss selector: natura-ui -> actual theme id (escaped JSON)
    $json = $json.Replace('[data-theme-id=\"natura-ui\"]', '[data-theme-id=\"' + $t.id + '\"]')

    # Write
    $outFile = Join-Path $outDir "$($t.id).json"
    if ($WhatIf) {
        Write-Host ("[WHATIF] " + $t.id + ".json (" + $json.Length + " chars)") -ForegroundColor Cyan
    } else {
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($outFile, $json, $utf8NoBom)
        Write-Host ("Created: " + $t.id + ".json") -ForegroundColor Green
    }
}
Write-Host ("Done! " + $themes.Count + " themes.")
