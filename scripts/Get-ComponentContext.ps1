#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds a compact AI context document for a SuperUI component.
.PARAMETER Component
    Component name (e.g. SgMention, SgModal).
.PARAMETER OutputJson
    If set, outputs raw JSON instead of markdown context.
.PARAMETER ApiParser
    Path to SgApiParser project. Default: tools/SgApiParser/SgApiParser.csproj
#>
param(
    [Parameter(Mandatory)]
    [string]$Component,
    [switch]$OutputJson,
    [string]$ApiParser = "tools/SgApiParser/SgApiParser.csproj"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "Parsing $Component..." -ForegroundColor Cyan

$json = & dotnet run --project "$root/$ApiParser" -- --component $Component 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to parse component $Component"
    exit 1
}

$info = $json | ConvertFrom-Json

if ($OutputJson) {
    $json
    return
}

$sb = [System.Text.StringBuilder]::new()

# Header
$null = $sb.AppendLine("## $($info.component)")

# Domain folder
$folder = if ($info.filePath) {
    $rel = [System.IO.Path]::GetRelativePath($root, $info.filePath)
    [System.IO.Path]::GetDirectoryName($rel)
} else { "unknown" }
$null = $sb.AppendLine("- **Location:** $folder")
$null = $sb.AppendLine("- **File kind:** $($info.fileKind)")
$null = $sb.AppendLine("- **Lines:** $($info.lineCount)")

# Inheritance
$chain = @()
if ($info.inherits) { $chain += $info.inherits }
foreach ($impl in $info.implements) { $chain += $impl }
$null = $sb.AppendLine("- **Type chain:** $($chain -join ' -> ')")

# JS interop
if ($info.usesJsInterop) {
    $null = $sb.AppendLine("- **JS interop:** YES $($info.modulePath)")
    if ($info.hasJsInvokable) {
        $null = $sb.AppendLine("- **JS invokable:** $($info.jsInvokableMethods -join ', ')")
    }
} else {
    $null = $sb.AppendLine("- **JS interop:** NO")
}

# Demo + tests
$null = $sb.AppendLine("- **Demo page:** $(if ($info.hasDemo) { 'YES' } else { 'NO' })")
$null = $sb.AppendLine("- **Tests:** $(if ($info.hasTests) { 'YES' } else { 'NO' })")
$null = $sb.AppendLine()

# Parameters
$params = $info.parameters
if ($params.Count -gt 0) {
    $null = $sb.AppendLine("### Parameters ($($params.Count))")
    $null = $sb.AppendLine('| Name | Type | Default |')
    $null = $sb.AppendLine('|------|------|---------|')
    foreach ($p in $params) {
        $def = if ($p.default) { "'$($p.default)'" } elseif ($p.required) { '*required*' } else { '-' }
        $null = $sb.AppendLine("| $($p.name) | $($p.type) | $def |")
    }
    $null = $sb.AppendLine()
}

# Events
if ($info.events.Count -gt 0) {
    $null = $sb.AppendLine("### Events ($($info.events.Count))")
    $null = $sb.AppendLine('| Name | Type |')
    $null = $sb.AppendLine('|------|------|')
    foreach ($e in $info.events) {
        $null = $sb.AppendLine("| $($e.name) | $($e.type) |")
    }
    $null = $sb.AppendLine()
}

# Enums used
if ($info.enumsUsed.Count -gt 0) {
    $null = $sb.AppendLine("### Enums used ($($info.enumsUsed.Count))")
    foreach ($enumName in $info.enumsUsed) {
        $enumFile = Join-Path $root "SuperUI" "Enums" "$enumName.cs"
        if (Test-Path $enumFile) {
            $content = Get-Content $enumFile -Raw
            $values = if ($content -match 'enum\s+\w+\s*\{([^}]+)\}') {
                $matches[1].Trim() -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
            } else { @() }
            $null = $sb.AppendLine("- **${enumName}:** $($values -join ', ')")
        }
    }
    $null = $sb.AppendLine()
}

# Similar components (same folder)
$folderPath = if ($info.filePath) { [System.IO.Path]::GetDirectoryName($info.filePath) } else { $null }
if ($folderPath -and (Test-Path $folderPath)) {
    $siblings = Get-ChildItem $folderPath -Filter '*.razor' | Where-Object {
        $_.Name -ne '_Imports.razor' -and $_.BaseName -ne $info.component
    }
    if ($siblings) {
        $null = $sb.AppendLine("### Sibling components")
        foreach ($s in $siblings) {
            $null = $sb.AppendLine("- $($s.BaseName)")
        }
        $null = $sb.AppendLine()
    }
}

# Raw JSON appendix
$null = $sb.AppendLine('---')
$null = $sb.AppendLine('_API JSON:_')
$null = $sb.AppendLine()
$null = $sb.AppendLine('```')
$null = $sb.AppendLine($json)
$null = $sb.AppendLine('```')

Write-Host $sb.ToString()
