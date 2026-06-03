# Replace `transition: all` with explicit properties in all source files
$root = "C:\Users\SuperComp\Documents\Blazor\SuperUI.Blazor"
$files = @()
$files += Get-ChildItem "$root/SuperUI/Components" -Recurse -Filter *.razor
$files += Get-ChildItem "$root/SuperUI/Components" -Recurse -Filter *.razor.css

$propList = "color, background-color, border-color, opacity, box-shadow, transform"

$totalReplaced = 0
foreach ($file in $files) {
    $content = Get-Content $file -Raw -Encoding UTF8
    $original = $content
    # Match: `transition: all <anything>;`
    $content = $content -replace 'transition: all\s+(.+?);', ("transition: color `$1, background-color `$1, border-color `$1, opacity `$1, box-shadow `$1, transform `$1;")
    if ($content -ne $original) {
        $matches = [regex]::Matches($original, 'transition: all')
        $count = $matches.Count
        $totalReplaced += $count
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($file.FullName, $content, $utf8NoBom)
        Write-Host "  $($file.Name) -> $count replacements" -ForegroundColor Green
    }
}
Write-Host "Done! $totalReplaced total replacements across $($files.Count) files." -ForegroundColor Green
