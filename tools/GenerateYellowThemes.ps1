# Generate 2 REAL Yellow Premium Themes — with proper OKLCH lightness for yellow
param([switch]$WhatIf)

$ErrorActionPreference = "Stop"
$root = "C:\Users\SuperComp\Documents\Blazor\SuperUI.Blazor"
$templateFile = Join-Path $root "SuperUI/Themes/json/natura-ui.json"
$outDir = Join-Path $root "SuperUI/Themes/json"
if (-not (Test-Path $templateFile)) { throw "Template not found: $templateFile" }

$themes = @(
    @{ id="banana-zest";  name="Banana Zest";   hue=105; glass=$true;
       desc="Banana Zest: bright yellow with glassmorphism. Bold, playful."
       fontName="Sora" }
    @{ id="golden-hour";  name="Golden Hour";   hue=100; glass=$false;
       desc="Golden Hour: warm honey, refined luxury."
       fontName="Playfair" }
)

$DQ = [char]0x22

$inter = "Inter"
$interComma = "Inter, "

$f = @{}
$f["Sora"] = @{
    sans = "'Sora', system-ui, -apple-system, sans-serif"
    serif = "'Source Serif 4', Georgia, serif"
    display = "'Sora', system-ui, sans-serif"
    heading = "'Sora', system-ui, -apple-system, sans-serif"
    headingFam = "'Sora', system-ui, -apple-system, 'Segoe UI', sans-serif"
    google = "Sora:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500"
    lightFont = "'Sora', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    darkFont = "'Sora', system-ui, -apple-system, 'Segoe UI', sans-serif"
    fontSansPrim = "'Sora', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    medical = "'JetBrains Mono', 'Fira Code', ui-monospace, monospace"
    geoTimes = "Georgia, 'Times New Roman', serif"
}
$f["Playfair"] = @{
    sans = "'Playfair Display', Georgia, serif"
    serif = "'Playfair Display', Georgia, serif"
    display = "'Playfair Display', Georgia, serif"
    heading = "'Playfair Display', Georgia, serif"
    headingFam = "'Playfair Display', Georgia, serif"
    google = "Playfair+Display:wght@400;500;600;700|JetBrains+Mono:wght@400;500"
    lightFont = "'Playfair Display', Georgia, serif"
    darkFont = "'Playfair Display', Georgia, serif"
    fontSansPrim = "'Playfair Display', Georgia, serif"
    medical = "'JetBrains Mono', 'Fira Code', ui-monospace, monospace"
    geoTimes = "Georgia, 'Times New Roman', serif"
}

# Yellow-lightened primary values
$yellowL = @{}
foreach ($n in @("50","100","200","300","400","500","600","700","800","900")) {
    $yellowL[$n] = $true
}

$lightBgM = @{
    default    = "oklch(0.97 0.01 {H})"
    subtle     = "oklch(0.95 0.02 {H})"
    muted      = "oklch(0.92 0.025 {H})"
    emphasized = "oklch(0.88 0.03 {H})"
    overlay    = "oklch(0.15 0.03 {H} / 0.40)"
    glass      = "oklch(0.97 0.01 {H} / 0.7)"
}
$darkBgM = @{
    default    = "oklch(0.14 0.015 {H})"
    subtle     = "oklch(0.18 0.018 {H})"
    muted      = "oklch(0.22 0.02 {H})"
    emphasized = "oklch(0.26 0.022 {H})"
    overlay    = "oklch(0 0 0 / 0.72)"
    glass      = "oklch(0.14 0.015 {H} / 0.7)"
}
$ltFg = @{
    default  = "oklch(0.14 0.02 {H})"
    subtle   = "oklch(0.36 0.015 {H})"
    muted    = "oklch(0.52 0.012 {H})"
    disabled = "oklch(0.68 0.008 {H})"
    inverse  = "oklch(0.97 0.01 {H})"
    link     = "oklch(0.70 0.20 {H})"
    linkHover = "oklch(0.65 0.20 {H})"
}
$dkFg = @{
    default   = "oklch(0.93 0.01 {H})"
    subtle    = "oklch(0.80 0.012 {H})"
    muted     = "oklch(0.63 0.015 {H})"
    disabled  = "oklch(0.53 0.015 {H})"
    inverse   = "oklch(0.14 0.015 {H})"
    link      = "oklch(0.75 0.18 {H})"
    linkHover = "oklch(0.80 0.16 {H})"
}
$ltBorder = @{
    default = "oklch(0.88 0.015 {H})"
    subtle  = "oklch(0.92 0.01 {H})"
    strong  = "oklch(0.80 0.02 {H})"
    focus   = "oklch(0.70 0.20 {H})"
}
$dkBorder = @{
    default = "oklch(0.28 0.02 {H})"
    subtle  = "oklch(0.22 0.018 {H})"
    strong  = "oklch(0.35 0.022 {H})"
    focus   = "oklch(0.75 0.18 {H})"
}
$ltSt = @{
    fgPlaceholder       = "oklch(0.52 0.012 {H})"
    surfaceHover        = "oklch(0.95 0.02 {H})"
    surfaceActive       = "oklch(0.92 0.025 {H})"
    surfaceSelected     = "oklch(0.95 0.04 {H})"
    borderHover         = "oklch(0.80 0.02 {H})"
    borderFocus         = "oklch(0.70 0.20 {H})"
    colorPrimaryDisabled   = "oklch(0.68 0.008 {H})"
    colorPrimaryDisabledBg = "oklch(0.92 0.025 {H})"
    colorPrimaryActiveBg   = "oklch(0.68 0.20 {H})"
    colorWarningActiveBg   = "oklch(0.68 0.20 {H})"
    colorWarningDisabled   = "oklch(0.68 0.008 {H})"
}
$dkSt = @{
    fgPlaceholder       = "oklch(0.63 0.015 {H})"
    surfaceHover        = "oklch(0.18 0.018 {H})"
    surfaceActive       = "oklch(0.22 0.02 {H})"
    surfaceSelected     = "oklch(0.30 0.06 {H})"
    borderHover         = "oklch(0.35 0.022 {H})"
    borderFocus         = "oklch(0.75 0.18 {H})"
    colorPrimaryDisabled   = "oklch(0.53 0.015 {H})"
    colorPrimaryDisabledBg = "oklch(0.22 0.02 {H})"
    colorPrimaryActiveBg   = "oklch(0.83 0.16 {H})"
    colorWarningActiveBg   = "oklch(0.80 0.14 {H})"
    colorWarningDisabled   = "oklch(0.53 0.015 {H})"
}

$ltPrim = @{
    default = "oklch(0.75 0.20 {H})"
    subtle  = "oklch(0.95 0.04 {H})"
    hover   = "oklch(0.68 0.20 {H})"
    fg      = "oklch(0.14 0.02 {H})"
}
$dkPrim = @{
    default = "oklch(0.78 0.18 {H})"
    subtle  = "oklch(0.30 0.06 {H})"
    hover   = "oklch(0.83 0.16 {H})"
    fg      = "oklch(0.12 0.015 {H})"
}
$ltWarn = @{
    default = "oklch(0.75 0.18 {H})"
    subtle  = "oklch(0.95 0.04 {H})"
    hover   = "oklch(0.68 0.20 {H})"
    fg      = "oklch(0.14 0.02 {H})"
}
$dkWarn = @{
    default = "oklch(0.78 0.16 {H})"
    subtle  = "oklch(0.30 0.06 105)"
    hover   = "oklch(0.83 0.14 {H})"
}

function Patch($json, $H) {
    # 1. Remove additionalCss line
    $json = $json -replace '(?m)^\s*"additionalCss".*\r?\n', ''

    # 2. Hue rotation: 262 -> H (inside oklch)
    $json = $json -replace '(?<=oklch\([^)]*?)\b262(?=\s*\)|\s*/)', $H

    # 3. Fix primitives.primary lightness (only inside primary block)
    # Match the primary object and replace lightness/chroma in its 50-900 entries
    # After hue rotation, values are like oklch(L C H). We want to replace L and C for primary.
    $json = $json -replace '(?<="primary": \{\s*)(.*?)(?=\s*\},\s*"(?:success|danger|warning|info|neutral)")', {
        $block = $_.Value
        # Map: old (lightness chroma) -> new for each level
        $map = @{
            "0.95 0.03" = "0.97 0.03"
            "0.90 0.06" = "0.94 0.06"
            "0.82 0.10" = "0.90 0.10"
            "0.719 0.14" = "0.85 0.14"
            "0.63 0.18"  = "0.80 0.18"
            "0.55 0.20"  = "0.75 0.20"
            "0.48 0.20"  = "0.68 0.20"
            "0.40 0.18"  = "0.58 0.18"
            "0.30 0.16"  = "0.45 0.16"
            "0.20 0.14"  = "0.30 0.14"
        }
        foreach ($pair in $map.GetEnumerator()) {
            $block = $block.Replace("oklch($($pair.Key) $H)", "oklch($($pair.Value) $H)")
        }
        $block
    }

    # 4. Theme light section — background, foreground, border, divider
    foreach ($kv in $lightBgM.GetEnumerator()) {
        $json = $json -replace ('"divider": "oklch\(0\.93 0\.01 ' + $H + '\)"'), ('"divider": "oklch(0.92 0.02 ' + $H + ')"')
    }
    foreach ($kv in $lightBgM.GetEnumerator()) {
        $v = $kv.Value -replace '{H}', $H
        $oldPattern = '"{0}": "oklch\([^)]*?{1}\)"' -f $kv.Key, $H
        $json = $json -replace $oldPattern, ('"{0}": "{1}"' -f $kv.Key, $v)
    }
    foreach ($kv in $ltFg.GetEnumerator()) {
        $v = $kv.Value -replace '{H}', $H
        $oldPattern = '"{0}": "oklch\([^)]*?{1}\)"' -f $kv.Key, $H
        $json = $json -replace $oldPattern, ('"{0}": "{1}"' -f $kv.Key, $v)
    }
    foreach ($kv in $ltBorder.GetEnumerator()) {
        $v = $kv.Value -replace '{H}', $H
        $oldPattern = '"{0}": "oklch\([^)]*?{1}\)"' -f $kv.Key, $H
        $json = $json -replace $oldPattern, ('"{0}": "{1}"' -f $kv.Key, $v)
    }

    # 5. Theme dark section
    foreach ($kv in $darkBgM.GetEnumerator()) {
        $v = $kv.Value -replace '{H}', $H
        $oldPattern = '"{0}": "oklch\([^)]*?{1}\)"' -f $kv.Key, $H
        $json = $json -replace $oldPattern, ('"{0}": "{1}"' -f $kv.Key, $v)
    }
    foreach ($kv in $dkFg.GetEnumerator()) {
        $v = $kv.Value -replace '{H}', $H
        $oldPattern = '"{0}": "oklch\([^)]*?{1}\)"' -f $kv.Key, $H
        $json = $json -replace $oldPattern, ('"{0}": "{1}"' -f $kv.Key, $v)
    }
    foreach ($kv in $dkBorder.GetEnumerator()) {
        $v = $kv.Value -replace '{H}', $H
        $oldPattern = '"{0}": "oklch\([^)]*?{1}\)"' -f $kv.Key, $H
        $json = $json -replace $oldPattern, ('"{0}": "{1}"' -f $kv.Key, $v)
    }

    # 6. colorPrimary light
    foreach ($kv in $ltPrim.GetEnumerator()) {
        $v = $kv.Value -replace '{H}', $H
        $json = $json -replace ('"colorPrimary": \{') , '"colorPrimary___TMP___": {'
        # Find the colorPrimary block and replace key by key
    }

    # Better: just do targeted string replacements for primary/warning
    # Light colorPrimary
    $json = $json -replace ('"default": "oklch\(0\.56 0\.22 ' + $H + '\)",'), ('"default": "oklch(0.75 0.20 ' + $H + ')",')
    $json = $json -replace ('"hover": "oklch\(0\.50 0\.23 ' + $H + '\)",'), ('"hover": "oklch(0.68 0.20 ' + $H + ')",')
    $json = $json -replace ('"fg": "oklch\(0\.99 0 0\)"'), ('"fg": "oklch(0.14 0.02 ' + $H + ')"')

    # Dark colorPrimary
    $json = $json -replace ('"default": "oklch\(0\.62 0\.22 ' + $H + '\)",'), ('"default": "oklch(0.78 0.18 ' + $H + ')",')
    $json = $json -replace ('"hover": "oklch\(0\.70 0\.22 ' + $H + '\)",'), ('"hover": "oklch(0.83 0.16 ' + $H + ')",')

    # Light colorWarning
    $json = $json -replace ('"default": "oklch\(0\.767 0\.181 83\.1\)"'), ('"default": "oklch(0.75 0.18 ' + $H + ')"')
    $json = $json -replace ('"hover": "oklch\(0\.70 0\.18 83\)"'), ('"hover": "oklch(0.68 0.20 ' + $H + ')"')

    # Dark colorWarning
    $json = $json -replace ('"default": "oklch\(0\.74 0\.16 75\)"'), ('"default": "oklch(0.78 0.16 ' + $H + ')"')
    $json = $json -replace ('"hover": "oklch\(0\.80 0\.14 75\)"'), ('"hover": "oklch(0.83 0.14 ' + $H + ')"')

    # 7. State overrides
    foreach ($kv in $ltSt.GetEnumerator()) {
        $v = $kv.Value -replace '{H}', $H
        $json = $json -replace ('"' + $kv.Key + '": "oklch\([^)]*? ' + $H + '\)"'), ('"' + $kv.Key + '": "' + $v + '"')
    }
    foreach ($kv in $dkSt.GetEnumerator()) {
        $v = $kv.Value -replace '{H}', $H
        $json = $json -replace ('"' + $kv.Key + '": "oklch\([^)]*? ' + $H + '\)"'), ('"' + $kv.Key + '": "' + $v + '"')
    }

    # Warning active bg/disbaled already in state maps, but also fix alternate patterns
    $json = $json -replace ('"colorWarningActiveBg": "oklch\(0\.70 0\.18 83\)"'), ('"colorWarningActiveBg": "oklch(0.68 0.20 ' + $H + ')"')
    $json = $json -replace ('"colorWarningActiveBg": "oklch\(0\.80 0\.14 75\)"'), ('"colorWarningActiveBg": "oklch(0.80 0.14 ' + $H + ')"')

    # 8. Surface fg for all color modes
    # Already handled in state overrides for surfaceHover/Active/Selected

    # 9. Fix light divider
    $json = $json -replace ('"divider": "oklch\(0\.93 0\.01 ' + $H + '\)"'), ('"divider": "oklch(0.92 0.02 ' + $H + ')"')
    $json = $json -replace ('"divider": "oklch\(0\.18 0\.012 ' + $H + '\)"'), ('"divider": "oklch(0.22 0.018 ' + $H + ')"')

    return $json
}

foreach ($t in $themes) {
    Write-Host "Generating $($t.id) (hue=$($t.hue))..." -ForegroundColor Cyan
    $H = [string]$t.hue

    $jsonRaw = Get-Content $templateFile -Raw -Encoding UTF8

    # === Metadata ===
    $jsonRaw = $jsonRaw -replace '"id": "natura-ui"', ('"id": "' + $t.id + '"')
    $jsonRaw = $jsonRaw -replace '"name": "Natura UI"', ('"name": "' + $t.name + '"')
    $jsonRaw = $jsonRaw -replace '"category": "Core"', '"category": "Premium"'
    $jsonRaw = $jsonRaw -replace '"author": "SuperUI \+ Natura"', '"author": "SuperUI Premium"'
    $jsonRaw = $jsonRaw -replace '(?<="description": ")[^"]*', ($t.desc -replace '"', '\"')

    # === Font replacements ===
    $fc = $f[$t.fontName]
    $jsonRaw = $jsonRaw.Replace($DQ+$inter+sans_suffix, $DQ+$fc.sans+$DQ)
    # Actually, just do simpler replacements
    $interPatterns = @(
        @("Inter, system-ui, -apple-system, sans-serif", $fc.sans),
        @("'Source Serif 4', Georgia, serif", $fc.serif),
        @("'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif", $fc.fontSansPrim),
        @("'JetBrains Mono', 'Fira Code', ui-monospace, monospace", $fc.medical),
        @("Georgia, 'Times New Roman', serif", $fc.geoTimes),
        @("Inter:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|JetBrains+Mono:wght@400;500", $fc.google),
        @("'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif", $fc.lightFont),
        @("'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif", $fc.darkFont)
    )
    foreach ($pair in $interPatterns) {
        $jsonRaw = $jsonRaw.Replace($DQ+$pair[0]+$DQ, $DQ+$pair[1]+$DQ)
    }
    # Also handle unquoted ones (heading, etc.)
    $jsonRaw = $jsonRaw.Replace("Inter, system-ui, -apple-system, sans-serif", $fc.sans)
    $jsonRaw = $jsonRaw.Replace("'Source Serif 4', Georgia, serif", $fc.serif)
    $jsonRaw = $jsonRaw.Replace("Inter, system-ui, sans-serif", $fc.display)
    $jsonRaw = $jsonRaw.Replace('"headingFont": "Inter, system-ui, -apple-system, sans-serif"', ('"headingFont": ' + $DQ + $fc.heading + $DQ))
    # headingFam
    $jsonRaw = $jsonRaw.Replace("'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif", $fc.headingFam)

    # === Apply oklch patches ===
    $jsonRaw = Patch $jsonRaw $H

    # === Add additionalCss ===
    if ($t.glass) {
        $css = '/* Banana Zest glassmorphism */\n[data-theme="light"] { --sg-bg: oklch(0.97 0.015 105); --sg-surface: oklch(1 0 0 / 0.6); --sg-bg-glass: oklch(1 0 0 / 0.5); --sg-blur-glass: 20px; }\n[data-theme="dark"] { --sg-bg: oklch(0.14 0.015 105); --sg-surface: oklch(0.22 0.02 105 / 0.5); --sg-bg-glass: oklch(0.18 0.018 105 / 0.4); --sg-blur-glass: 20px; }\n.sgc-glass { backdrop-filter: blur(20px); -webkit-backdrop-filter: blur(20px); border: 1px solid oklch(1 0 0 / 0.15); }'
    } else {
        $css = '/* Golden Hour premium */\n[data-theme="light"] { --sg-bg: oklch(0.97 0.015 100); }\n[data-theme="dark"] { --sg-bg: oklch(0.14 0.015 100); }'
    }
    $cssEscaped = $css.Replace('\', '\\').Replace('"', '\"')
    $jsonRaw = $jsonRaw -replace '"version": "1.0.0"', ('"version": "1.0.0",' + "`r`n  " + $DQ + "additionalCss" + $DQ + ": " + $DQ + $cssEscaped + $DQ)

    # Write
    $outFile = Join-Path $outDir "$($t.id).json"
    if ($WhatIf) {
        Write-Host ("[WHATIF] $($t.id).json ($($jsonRaw.Length) chars)") -ForegroundColor Cyan
    } else {
        Remove-Item $outFile -ErrorAction SilentlyContinue
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($outFile, $jsonRaw, $utf8NoBom)
        Write-Host ("Created: $($t.id).json") -ForegroundColor Green
    }
}
Write-Host "Done!" -ForegroundColor Green
