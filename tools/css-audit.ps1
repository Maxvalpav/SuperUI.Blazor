# CSS audit: находит классы в CSS, не встречающиеся ни в .razor/.cs/.js
# Read-only: ничего не меняет, только печатает отчёт.
param(
    [string]$Root = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = 'Stop'
$skip = '\\(bin|obj|node_modules|\.git)\\'

# ---------- 1. Собираем ВСЕ идентификаторы из исходников ----------
$srcFiles = Get-ChildItem -Path $Root -Recurse -Include *.razor, *.cs, *.js, *.html, *.cshtml -File |
    Where-Object { $_.FullName -notmatch $skip }

$usedExact = [System.Collections.Generic.HashSet[string]]::new()
$usedText  = [System.Text.StringBuilder]::new()

foreach ($f in $srcFiles) {
    $text = [IO.File]::ReadAllText($f.FullName)
    [void]$usedText.Append($text).Append("`n")
    foreach ($m in [regex]::Matches($text, '[A-Za-z_][A-Za-z0-9_-]*')) {
        [void]$usedExact.Add($m.Value)
    }
}
$allSource = $usedText.ToString()

Write-Host "Источников просканировано: $($srcFiles.Count), уникальных идентификаторов: $($usedExact.Count)"

# ---------- 2. Классы из CSS ----------
function Get-CssClasses([string]$path) {
    $css = [IO.File]::ReadAllText($path)
    # убираем комментарии, чтобы закомментированный CSS не считался
    $css = [regex]::Replace($css, '/\*.*?\*/', '', 'Singleline')
    $set = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($m in [regex]::Matches($css, '\.(-?[A-Za-z_][A-Za-z0-9_-]*)')) {
        [void]$set.Add($m.Groups[1].Value)
    }
    return $set
}

$cssFiles = Get-ChildItem -Path $Root -Recurse -Filter *.css -File |
    Where-Object { $_.FullName -notmatch $skip -and $_.FullName -notmatch '\\themes\\' }

$report = @()
foreach ($cf in $cssFiles) {
    $classes = Get-CssClasses $cf.FullName
    $unused = @()
    foreach ($c in $classes) {
        if ($usedExact.Contains($c)) { continue }
        # эвристика для динамических классов: sgc-size-md <- "sgc-size-" в исходнике
        $isDynamic = $false
        for ($i = $c.Length - 1; $i -gt 3; $i--) {
            if ($c[$i] -eq '-') {
                $prefix = $c.Substring(0, $i + 1)
                if ($allSource.Contains($prefix)) { $isDynamic = $true; break }
            }
        }
        if (-not $isDynamic) { $unused += $c }
    }
    if ($unused.Count -gt 0) {
        $report += [pscustomobject]@{
            File        = $cf.FullName.Replace("$Root\", '')
            Total       = $classes.Count
            Unused      = $unused.Count
            Pct         = [math]::Round(100.0 * $unused.Count / [math]::Max($classes.Count, 1), 1)
            UnusedNames = ($unused | Sort-Object) -join ' '
        }
    }
}

$report | Sort-Object Unused -Descending | Select-Object File, Total, Unused, Pct | Format-Table -AutoSize
Write-Host "`n=== ДЕТАЛИ ===`n"
foreach ($r in ($report | Sort-Object Unused -Descending)) {
    Write-Host "--- $($r.File)  ($($r.Unused)/$($r.Total))"
    Write-Host $r.UnusedNames
    Write-Host ''
}
