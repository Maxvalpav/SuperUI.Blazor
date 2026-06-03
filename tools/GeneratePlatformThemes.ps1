# Generate 3 Platform Theme JSON files from fluent-breeze.json template
param([string]$TemplatePath = "SuperUI/Themes/json/fluent-breeze.json",
      [string]$OutputDir = "SuperUI/Themes/json",
      [switch]$WhatIf)

$ErrorActionPreference = "Stop"
$root = "C:\Users\SuperComp\Documents\Blazor\SuperUI.Blazor"
$templateFile = Join-Path $root $TemplatePath
$outDir = Join-Path $root $OutputDir
if (-not (Test-Path $templateFile)) { throw "Template not found: $templateFile" }

$templateRaw = Get-Content $templateFile -Raw -Encoding UTF8

$themes = @(
    ,@{ id="fluent-blue";   name="Fluent Blue";      hue=240; cat="Windows 11"; 
        desc="Windows 11 Fluent Blue: crisp, professional, Mica-inspired.";
        fontSans="'Segoe UI', system-ui, sans-serif";
        googleUrl=""; embedGoogle=$false;
        isWin11=$true }
    ,@{ id="apple-sonoma";  name="Apple Sonoma";     hue=248; cat="macOS";
        desc="macOS Sonoma: vibrant blue accent, frosted-glass elegance.";
        fontSans="Inter, -apple-system, 'Helvetica Neue', sans-serif";
        googleUrl="Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500";
        embedGoogle=$true; isWin11=$false }
    ,@{ id="apple-sequoia"; name="Apple Sequoia";    hue=280; cat="macOS";
        desc="macOS Sequoia: muted violet accent, earthy warmth.";
        fontSans="Inter, -apple-system, 'Helvetica Neue', sans-serif";
        googleUrl="Inter:wght@400;500;600;700|JetBrains+Mono:wght@400;500";
        embedGoogle=$true; isWin11=$false }
)

$mono = "'JetBrains Mono', 'Fira Code', ui-monospace, monospace"
$serif = "'Source Serif 4', Georgia, serif"

$DQ = [char]0x22

$srcSans      = "'Source Sans 3', 'Segoe UI', system-ui, sans-serif"
$srcDisplay   = "'Source Sans 3', 'Segoe UI', system-ui, sans-serif"
$srcMono      = "'Fira Code', 'Cascadia Code', 'JetBrains Mono', ui-monospace, monospace"
$srcSerif     = "'Source Serif 4', Constantia, Georgia, serif"
$srcMedical   = "'Fira Code', 'Cascadia Code', 'JetBrains Mono', ui-monospace, monospace"
$srcLightFont = "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
$srcDarkFont  = "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif"
$srcFontSansPrim = "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
$srcGoogle    = "Source+Sans+3:wght@400;500;600;700|Source+Serif+4:opsz,wght@8..60,400;8..60,500;8..60,600;8..60,700|Fira+Code:wght@400;500"
$srcHeading   = "'Source Sans 3', 'Segoe UI', system-ui, sans-serif"

foreach ($t in $themes) {
    Write-Host "Generating $($t.id)..." -ForegroundColor Cyan
    $json = $templateRaw

    # === Metadata ===
    $json = $json.Replace('"id": "fluent-breeze"', ($DQ+"id"+$DQ+": "+$DQ+$t.id+$DQ))
    $json = $json.Replace('"name": "Fluent Breeze"', ($DQ+"name"+$DQ+": "+$DQ+$t.name+$DQ))
    $json = $json.Replace('"category": "Modern"', ($DQ+"category"+$DQ+": "+$DQ+$t.cat+$DQ))
    $json = $json -replace '(?<="description": ")[^"]*', ($t.desc -replace '"', '\"')
    $json = $json.Replace('"author": "SuperUI Modern Library"', ($DQ+"author"+$DQ+": "+$DQ+"SuperUI Platform Themes"+$DQ))

    # === Hue: 218 (light) and 338 (dark) → theme hue ===
    $json = $json -replace '(?<=oklch\([^)]*?)\b218\b(?=\s*\)|\s*/|[,\s])', [string]$t.hue
    $json = $json -replace '(?<=oklch\([^)]*?)\b338\b(?=\s*\)|\s*/|[,\s])', [string]$t.hue

    # === Fonts ===
    $json = $json.Replace($DQ+$srcSans+$DQ, $DQ+$t.fontSans+$DQ)
    $json = $json.Replace($DQ+$srcDisplay+$DQ, $DQ+$t.fontSans+$DQ)
    $json = $json.Replace($DQ+$srcMono+$DQ, $DQ+$mono+$DQ)
    $json = $json.Replace($DQ+$srcSerif+$DQ, $DQ+$serif+$DQ)
    $json = $json.Replace($DQ+$srcMedical+$DQ, $DQ+$mono+$DQ)
    $json = $json.Replace($DQ+$srcLightFont+$DQ, $DQ+$t.fontSans+$DQ)
    $json = $json.Replace($DQ+$srcDarkFont+$DQ, $DQ+$t.fontSans+$DQ)
    $json = $json.Replace($DQ+$srcFontSansPrim+$DQ, $DQ+$t.fontSans+$DQ)
    $json = $json.Replace($DQ+$srcHeading+$DQ, $DQ+$t.fontSans+$DQ)

    # === Google Fonts ===
    $json = $json.Replace($DQ+$srcGoogle+$DQ, $DQ+$t.googleUrl+$DQ)
    $json = $json.Replace('"embedGoogleFontsImport": true', ($DQ+"embedGoogleFontsImport"+$DQ+": "+($t.embedGoogle).ToString().ToLower()))

    # === Radius (primitives + light + dark) ===
    $rSm = if ($t.isWin11) { "8px" } else { "10px" }
    $rMd = if ($t.isWin11) { "8px" } else { "10px" }
    $rLg = if ($t.isWin11) { "8px" } else { "10px" }

    # primitives radius (unique context)
    $json = $json.Replace('"sm": "5px",', ($DQ+"sm"+$DQ+": "+$DQ+$rSm+$DQ+","))
    $json = $json.Replace('"md": "8px",', ($DQ+"md"+$DQ+": "+$DQ+$rMd+$DQ+","))
    $json = $json.Replace('"lg": "13px",', ($DQ+"lg"+$DQ+": "+$DQ+$rLg+$DQ+","))

    # Transition
    if ($t.isWin11) {
        $json = $json.Replace('"fast": "120ms', '"fast": "150ms')
        # base 200ms stays 200ms
        $json = $json.Replace('"slow": "350ms', '"slow": "300ms')
    } else {
        $json = $json.Replace('"fast": "120ms', '"fast": "200ms')
        $json = $json.Replace('"base": "200ms', '"base": "300ms')
        $json = $json.Replace('"slow": "350ms', '"slow": "400ms')
    }

    # === Component heights ===
    $h = if ($t.isWin11) { "32px" } else { "28px" }
    $json = $json.Replace('"height": "30px"', ($DQ+"height"+$DQ+": "+$DQ+$h+$DQ))

    # === Component radii (with context to be precise) ===
    # button.radius: "5px" → Win11:8px, macOS:8px
    $br = "8px"
    # input.radius & select.radius: "3px" → 8px
    $ir = "8px"
    # card.radius: "5px" → Win11:8px, macOS:10px
    $cr = if ($t.isWin11) { "8px" } else { "10px" }

    $json = $json.Replace('"radius": "5px",', ($DQ+"radius"+$DQ+": "+$DQ+$br+$DQ+","))
    $json = $json.Replace('"radius": "3px",', ($DQ+"radius"+$DQ+": "+$DQ+$ir+$DQ+","))

    # === additionalCss ===
    if ($t.isWin11) {
        $acss = "/* Windows 11 Mica background */`r`n[data-theme=`"light`"] { --sg-bg: #FCFCFC; --sg-surface: #FFFFFF; }`r`n[data-theme=`"dark`"]  { --sg-bg: #2C2C2C; --sg-surface: #363636; }"
    } else {
        $acss = "/* macOS frosted background */`r`n[data-theme=`"light`"] { --sg-bg: #ECECEC; --sg-surface: #F5F5F5; }`r`n[data-theme=`"dark`"]  { --sg-bg: #323232; --sg-surface: #3A3A3A; }"
    }
    $json = $json -replace '"version": "2.0.0"', ('"version": "2.0.0",' + "`r`n  " + $DQ + "additionalCss" + $DQ + ": " + $DQ + $acss + $DQ)

    # === Write ===
    $outFile = Join-Path $outDir "$($t.id).json"
    if ($WhatIf) {
        Write-Host ("[WHATIF] $($t.id).json ($($json.Length) chars)") -ForegroundColor Cyan
    } else {
        Remove-Item $outFile -ErrorAction SilentlyContinue
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($outFile, $json, $utf8NoBom)
        Write-Host ("Created: $($t.id).json") -ForegroundColor Green
    }
}
Write-Host "Done! 3 platform themes generated." -ForegroundColor Green
