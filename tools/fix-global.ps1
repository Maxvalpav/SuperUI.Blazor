# Заменяет невалидный для Blazor :global(...) на ::deep / [data-theme="dark"].
# Regex допускает один уровень вложенных скобок: :global(tr:nth-child(even) td)
param([string]$Root = (Split-Path $PSScriptRoot -Parent), [switch]$Apply)

$rx = ':global\(((?:[^()]|\([^()]*\))*)\)'

$files = Get-ChildItem -Path $Root -Recurse -Filter *.razor.css |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Where-Object { [IO.File]::ReadAllText($_.FullName) -match ':global\(' }

$total = 0
foreach ($f in $files) {
    $lines = [IO.File]::ReadAllLines($f.FullName)
    $n = 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -notmatch ':global\(') { continue }

        $new = [regex]::Replace($line, $rx, {
            param($m)
            $inner = $m.Groups[1].Value
            # префикс слева от :global в этой строке
            $prefix = $m.Result('$`')
            if ($prefix.Trim().Length -eq 0) {
                # селектор-предок в начале строки
                if ($inner -eq '.dark') { return '[data-theme="dark"]' }
                return $inner
            }
            # потомок -> ::deep
            return '::deep ' + $inner
        })

        # схлопнуть возможные двойные пробелы, не трогая отступ
        $indent = [regex]::Match($new, '^\s*').Value
        $new = $indent + ($new.Substring($indent.Length) -replace '  +', ' ')

        if ($new -ne $line) {
            $n += ([regex]::Matches($line, $rx)).Count
            $lines[$i] = $new
        }
    }
    $total += $n
    Write-Host ("{0,-32} {1} замен" -f $f.Name, $n)
    if ($Apply) {
        [IO.File]::WriteAllLines($f.FullName, $lines, [Text.UTF8Encoding]::new($false))
    }
}
Write-Host "`nИТОГО: $total (Apply=$Apply)"
