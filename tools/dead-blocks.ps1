# Удаляет из глобального superui-components.css только те правила, которые:
#   1) не используются нигде в репозитории, И
#   2) принадлежат явному списку семейств, вытесненных scoped .razor.css
#      (компоненты мигрировали с префикса sgc- на sg-).
# Утилиты пакета (.sg-badge, .sg-spinner, .sg-overlay, fib-шкалы) НЕ трогаются:
# это публичный API NuGet-пакета SuperUI.
#
# Без -Apply — только отчёт.
param(
    [string]$Root = (Split-Path $PSScriptRoot -Parent),
    [switch]$Apply
)

$cssPath = Join-Path $Root 'SuperUI\wwwroot\superui-components.css'
$skip = '\\(bin|obj|node_modules|\.git)\\'

# Семейства-дубликаты: глобальный sgc-* -> живая scoped-замена
$families = @(
    'sgc-progress'      # -> .sg-progress*        (SgProgress.razor.css)
    'sgc-circular-'     # -> .sg-progress-circular-* (SgProgress.razor.css)
    'sgc-pager'         # -> .sg-pager* / .sg-page-btn (SgPagination.razor.css)
    'sgc-chart-'        # -> .sg-chart-*          (SgChart.razor.css)
    'sgc-acc'           # -> .sgc-accordion*      (SgAccordion.razor)
    'sgc-splitter'      # -> .sgc-split*          (SgSplitter.razor.css)
    'sgc-master-detail' # компонента MasterDetail в репо нет
)

function Test-InFamily([string]$cls) {
    foreach ($f in $families) {
        if ($cls -eq $f.TrimEnd('-') -or $cls.StartsWith($f)) { return $true }
    }
    return $false
}

$src = Get-ChildItem -Path $Root -Recurse -Include *.razor, *.cs, *.js, *.html -File |
    Where-Object { $_.FullName -notmatch $skip }
$blob = ($src | ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"

function Test-Alive([string]$cls) {
    if ([regex]::IsMatch($blob, '(?<![A-Za-z0-9_-])' + [regex]::Escape($cls) + '(?![A-Za-z0-9_-])')) { return $true }
    for ($i = $cls.Length - 1; $i -gt 3; $i--) {
        if ($cls[$i] -eq '-' -and $blob.Contains($cls.Substring(0, $i + 1))) { return $true }
    }
    return $false
}

$text = [IO.File]::ReadAllText($cssPath)
$comments = [regex]::Matches($text, '/\*.*?\*/', 'Singleline')

$hits = [System.Collections.Generic.List[object]]::new()
$cache = @{}

foreach ($m in [regex]::Matches($text, '(?m)^([A-Za-z.#\[:][^\{\}@]*?)\s*\{')) {
    $inComment = $false
    foreach ($c in $comments) {
        if ($m.Index -ge $c.Index -and $m.Index -lt ($c.Index + $c.Length)) { $inComment = $true; break }
    }
    if ($inComment) { continue }

    $sel = $m.Groups[1].Value.Trim()
    $classes = [regex]::Matches($sel, '\.(-?[A-Za-z_][A-Za-z0-9_-]*)') | ForEach-Object { $_.Groups[1].Value }
    if (-not $classes) { continue }

    # все классы правила: мертвы И принадлежат целевым семействам
    $ok = $true
    foreach ($c in $classes) {
        if (-not $cache.ContainsKey($c)) { $cache[$c] = Test-Alive $c }
        if ($cache[$c] -or -not (Test-InFamily $c)) { $ok = $false; break }
    }
    if (-not $ok) { continue }

    $i = $m.Index + $m.Length - 1
    $depth = 0
    while ($i -lt $text.Length) {
        if ($text[$i] -eq '{') { $depth++ }
        elseif ($text[$i] -eq '}') { $depth--; if ($depth -eq 0) { break } }
        $i++
    }
    $hits.Add([pscustomobject]@{
        Line  = ($text.Substring(0, $m.Index) -split "`n").Count
        Sel   = $sel; Start = $m.Index; End = $i
        Lines = ($text.Substring($m.Index, $i - $m.Index + 1) -split "`n").Count
    })
}

Write-Host "К удалению: $($hits.Count) правил, $(($hits|Measure-Object Lines -Sum).Sum) строк`n"
$hits | Sort-Object Line | ForEach-Object { "  L{0,-6} ({1,2} стр) {2}" -f $_.Line, $_.Lines, $_.Sel }

if ($Apply) {
    $out = $text
    foreach ($h in ($hits | Sort-Object Start -Descending)) {
        $out = $out.Remove($h.Start, $h.End - $h.Start + 1)
    }
    $out = [regex]::Replace($out, '(\r?\n[ \t]*){3,}', "`n`n")

    # контроль баланса скобок
    $ob = ([regex]::Matches($out, '\{')).Count
    $cb = ([regex]::Matches($out, '\}')).Count
    if ($ob -ne $cb) { Write-Error "Дисбаланс скобок: $ob vs $cb — запись отменена"; exit 1 }

    [IO.File]::WriteAllText($cssPath, $out, [Text.UTF8Encoding]::new($false))
    Write-Host "`nГотово. $(($text -split "`n").Count) -> $(($out -split "`n").Count) строк. Скобки: $ob = $cb."
}
