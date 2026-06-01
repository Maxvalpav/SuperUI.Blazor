param(
    [string]$InputFile = "SuperUI/wwwroot/superui-components.css",
    [string]$OutputFile = "SuperUI/wwwroot/superui-components.css"
)

$excluded = @(
    '.sgc-', '.sg-col-', '.sg-space-', '.sg-zindex-', '.sg-font-',
    '.sg-lh-', '.sg-fw-', '.sg-color-', '.sg-spacing-', '.sg-theme-',
    '.sg-animate-', '.sg-badge-'
)

$global:ModifiedCount = 0

function NeedsAlias {
    param([string]$selector)
    if (-not $selector.StartsWith('.sg-')) { return $false }
    if ($selector.StartsWith('.sgc-')) { return $false }
    foreach ($ex in $excluded) {
        if ($selector.StartsWith($ex)) { return $false }
    }
    return $true
}

function Process-SelectorString {
    param([string]$text)
    if ([string]::IsNullOrWhiteSpace($text)) { return $text }

    # Find comma positions at proper depth (not inside parens/brackets OR comments)
    $commaPositions = [System.Collections.Generic.List[int]]::new()
    $parenDepth = 0
    $bracketDepth = 0
    $j = 0
    while ($j -lt $text.Length) {
        $ch = $text[$j]
        # Skip CSS comments — commas inside comments don't separate selectors
        if ($ch -eq '/' -and $j -lt $text.Length - 1 -and $text[$j+1] -eq '*') {
            $cEnd = $text.IndexOf('*/', $j + 2)
            if ($cEnd -eq -1) { break }
            $j = $cEnd + 2
            continue
        }
        if ($ch -eq '(') { $parenDepth++ }
        elseif ($ch -eq ')') { $parenDepth-- }
        elseif ($ch -eq '[') { $bracketDepth++ }
        elseif ($ch -eq ']') { $bracketDepth-- }
        elseif ($ch -eq ',' -and $parenDepth -eq 0 -and $bracketDepth -eq 0) {
            $commaPositions.Add($j)
        }
        $j++
    }

    # Split into segments at comma positions
    $segments = [System.Collections.Generic.List[string]]::new()
    $prev = 0
    foreach ($pos in $commaPositions) {
        $segments.Add($text.Substring($prev, $pos - $prev))
        $prev = $pos + 1
    }
    $segments.Add($text.Substring($prev))

    # Process each segment — preserve exact formatting
    $resultSegments = [System.Collections.Generic.List[string]]::new()
    $anyChanged = $false
    foreach ($seg in $segments) {
        # Scan past leading whitespace and CSS comments to find actual selector
        $scanPos = 0
        $segLen = $seg.Length
        # Skip leading whitespace
        $wsMatch = [regex]::Match($seg.Substring($scanPos), '^\s*')
        $scanPos += $wsMatch.Length
        # Skip leading CSS comments and any following whitespace
        while ($scanPos -lt $segLen) {
            $cm = [regex]::Match($seg.Substring($scanPos), '^/\*.*?\*/')
            if (-not $cm.Success) { break }
            $scanPos += $cm.Length
            $wsMatch2 = [regex]::Match($seg.Substring($scanPos), '^\s*')
            $scanPos += $wsMatch2.Length
        }

        $actualRaw = $seg.Substring($scanPos)
        $trimmed = $actualRaw.Trim()

        if ($trimmed.Length -gt 0 -and (NeedsAlias $trimmed)) {
            $alias = '.sgc-' + $trimmed.Substring(4)
            $leading  = $seg.Substring(0, $scanPos)
            $trailIdx = $scanPos + $actualRaw.IndexOf($trimmed) + $trimmed.Length
            $trailing = if ($trailIdx -lt $segLen) { $seg.Substring($trailIdx) } else { '' }
            $resultSegments.Add("$leading$alias, $trimmed$trailing")
            $anyChanged = $true
        } else {
            $resultSegments.Add($seg)
        }
    }

    if (-not $anyChanged) { return $text }
    $global:ModifiedCount++
    return ($resultSegments -join ',')
}

function Process-CSS {
    param([string]$text, [int]$start, [int]$length)
    $sb = [System.Text.StringBuilder]::new()
    $i = $start
    $end = $start + $length
    while ($i -lt $end) {
        # Skip block comments
        if ($i -lt $end - 1 -and $text[$i] -eq '/' -and $text[$i+1] -eq '*') {
            $cEnd = $text.IndexOf('*/', $i + 2)
            if ($cEnd -eq -1 -or $cEnd -ge $end) { $cEnd = $end - 2 }
            [void]$sb.Append($text.Substring($i, $cEnd - $i + 2))
            $i = $cEnd + 2
            continue
        }
        # Skip strings
        if ($text[$i] -eq "'" -or $text[$i] -eq '"') {
            $quote = $text[$i]; $s = $i; $i++
            while ($i -lt $end) {
                if ($text[$i] -eq $quote -and $text[$i-1] -ne '\') { break }
                $i++
            }
            if ($i -lt $end) { $i++ }
            [void]$sb.Append($text.Substring($s, $i - $s))
            continue
        }
        # Find next '{' at brace-depth 0 to locate a rule
        $selStart = $i
        $depth = 0
        $found = $false
        while ($i -lt $end) {
            # Skip comments inside selectors
            if ($i -lt $end - 1 -and $text[$i] -eq '/' -and $text[$i+1] -eq '*') {
                $cEnd = $text.IndexOf('*/', $i + 2)
                if ($cEnd -eq -1 -or $cEnd -ge $end) { $cEnd = $end - 2 }
                $i = $cEnd + 2
                continue
            }
            if ($text[$i] -eq "'" -or $text[$i] -eq '"') {
                $quote = $text[$i]; $i++
                while ($i -lt $end) {
                    if ($text[$i] -eq $quote -and $text[$i-1] -ne '\') { break }
                    $i++
                }
                if ($i -lt $end) { $i++ }
                continue
            }
            if ($text[$i] -eq '{') {
                if ($depth -eq 0) {
                    $selText = $text.Substring($selStart, $i - $selStart)
                    $bodyStart = $i
                    $depth = 1; $i++
                    while ($depth -gt 0 -and $i -lt $end) {
                        if ($i -lt $end - 1 -and $text[$i] -eq '/' -and $text[$i+1] -eq '*') {
                            $cEnd = $text.IndexOf('*/', $i + 2)
                            if ($cEnd -eq -1 -or $cEnd -ge $end) { $cEnd = $end - 2 }
                            $i = $cEnd + 2
                            continue
                        }
                        if ($text[$i] -eq "'" -or $text[$i] -eq '"') {
                            $quote = $text[$i]; $i++
                            while ($i -lt $end) {
                                if ($text[$i] -eq $quote -and $text[$i-1] -ne '\') { break }
                                $i++
                            }
                            if ($i -lt $end) { $i++ }
                            continue
                        }
                        if ($text[$i] -eq '{') { $depth++ }
                        elseif ($text[$i] -eq '}') { $depth-- }
                        if ($depth -gt 0) { $i++ }
                    }
                    $bodyFull = $text.Substring($bodyStart, $i - $bodyStart + 1)
                    $trimmedSel = $selText.Trim()
                    $isAtRule = ($trimmedSel -match '^@')
                    if ($isAtRule) {
                        $inner = $bodyFull.Substring(1, $bodyFull.Length - 2)
                        $processedInner = Process-CSS $inner 0 $inner.Length
                        if ($processedInner -ne $inner) { $global:ModifiedCount++ }
                        [void]$sb.Append($selText).Append('{').Append($processedInner).Append('}')
                    } else {
                        $newSel = Process-SelectorString $selText
                        [void]$sb.Append($newSel).Append($bodyFull)
                    }
                    $i++; $found = $true; break
                } else { $depth++ }
            } elseif ($text[$i] -eq '}') {
                [void]$sb.Append($text.Substring($selStart, $i - $selStart + 1))
                $found = $true; $i++; break
            }
            $i++
        }
        if (-not $found) {
            if ($selStart -lt $end) { [void]$sb.Append($text.Substring($selStart, $end - $selStart)) }
            break
        }
    }
    return $sb.ToString()
}

# ---- Main ----
$InputFile = Resolve-Path $InputFile
Write-Host "Reading $InputFile ..." -ForegroundColor Cyan
$content = Get-Content -Path $InputFile -Raw -Encoding UTF8
if (-not $content) { Write-Error "Empty file"; exit 1 }
$result = Process-CSS $content 0 $content.Length
Write-Host "Writing $OutputFile ($($result.Length) bytes)..." -ForegroundColor Cyan
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($InputFile, $result, $Utf8NoBom)
Write-Host "Done. Modified $global:ModifiedCount rules." -ForegroundColor Green

# Validate brace balance
$ob = 0; $cb = 0; $bi = 0; $blen = $result.Length
while ($bi -lt $blen) {
    $bc = $result[$bi]
    if ($bc -eq "'" -or $bc -eq '"') { $bq = $bc; $bi++; while ($bi -lt $blen -and -not ($result[$bi] -eq $bq -and $result[$bi-1] -ne '\')) { $bi++ }; $bi++; continue }
    if ($bc -eq '/' -and $bi -lt $blen - 1 -and $result[$bi+1] -eq '*') { $be = $result.IndexOf('*/', $bi+2); if ($be -eq -1) { break }; $bi = $be+1; continue }
    if ($bc -eq '{') { $ob++ } elseif ($bc -eq '}') { $cb++ }
    $bi++
}
if ($ob -eq $cb) { Write-Host "Brace balance: $ob = $cb (OK)" -ForegroundColor Green }
else { Write-Host "BRACE MISMATCH: $ob vs $cb" -ForegroundColor Red; exit 1 }
