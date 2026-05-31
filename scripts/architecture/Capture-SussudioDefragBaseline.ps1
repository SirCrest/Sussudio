<#
.SYNOPSIS
Captures a lightweight Sussudio defragmentation baseline.

.DESCRIPTION
Run from the repository root. The report is intended for docs/architecture/Sussudio-Defragmentation-Goal.md.
It focuses on behavioral locality signals: file counts, tiny files, large files, and partial-class clusters.
#>

param(
    [string]$Root = (Get-Location).Path,
    [string]$Output = "docs/architecture/Sussudio-Defragmentation-Baseline.generated.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$excludeDirs = @(
    ".git", ".vs", ".vscode", "bin", "obj", "packages", "TestResults", "artifacts", "node_modules"
)

function Test-ExcludedPath {
    param([string]$Path)
    $parts = $Path -split '[\\/]'
    foreach ($dir in $excludeDirs) {
        if ($parts -contains $dir) { return $true }
    }
    return $false
}

function Get-LineCount {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return 0 }
    $content = Get-Content -LiteralPath $Path -ErrorAction Stop
    return @($content).Count
}

function Get-NonBlankLineCount {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return 0 }
    $content = Get-Content -LiteralPath $Path -ErrorAction Stop
    return @($content | Where-Object { $_.Trim().Length -gt 0 }).Count
}

function Convert-ToRepoPath {
    param([string]$Path)
    $rootPath = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $rootUri = [Uri]::new($rootPath)
    $pathUri = [Uri]::new((Resolve-Path -LiteralPath $Path).Path)
    $relative = [Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString())
    return $relative -replace '\\','/'
}

$allCs = Get-ChildItem -LiteralPath $Root -Recurse -Filter *.cs -File |
    Where-Object { -not (Test-ExcludedPath $_.FullName) }

$entries = foreach ($file in $allCs) {
    $repoPath = Convert-ToRepoPath $file.FullName
    $isTest = $repoPath -match '(^|/)(test|tests|.*\.Tests?)(/|$)' -or $repoPath -match '(Test|Tests)\.cs$'
    $isGenerated = $repoPath -match '(\.g\.cs$|\.Designer\.cs$|\.generated\.cs$|/Generated/|/obj/)'
    $raw = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop
    $partialMatches = [regex]::Matches($raw, '\bpartial\s+(?:class|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)')
    [pscustomobject]@{
        Path = $repoPath
        Lines = Get-LineCount $file.FullName
        NonBlankLines = Get-NonBlankLineCount $file.FullName
        IsTest = $isTest
        IsGenerated = $isGenerated
        PartialTypes = @($partialMatches | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    }
}

$production = @($entries | Where-Object { -not $_.IsTest -and -not $_.IsGenerated })
$tests = @($entries | Where-Object { $_.IsTest })
$coreApp = @($entries | Where-Object { -not $_.IsGenerated -and $_.Path -like 'Sussudio/*' })
$sussudioTests = @($entries | Where-Object { -not $_.IsGenerated -and $_.Path -like 'tests/Sussudio.Tests/*' })
$coreAppNonBlank = ($coreApp | Measure-Object NonBlankLines -Sum).Sum
$sussudioTestsNonBlank = ($sussudioTests | Measure-Object NonBlankLines -Sum).Sum
if ($null -eq $coreAppNonBlank) { $coreAppNonBlank = 0 }
if ($null -eq $sussudioTestsNonBlank) { $sussudioTestsNonBlank = 0 }
$under60 = @($production | Where-Object { $_.Lines -lt 60 })
$under80 = @($production | Where-Object { $_.Lines -lt 80 })

$partialRows = foreach ($entry in $production) {
    foreach ($type in $entry.PartialTypes) {
        [pscustomobject]@{ Type = $type; Path = $entry.Path; Lines = $entry.Lines }
    }
}

$partialClusters = @($partialRows |
    Group-Object Type |
    Sort-Object Count -Descending |
    Select-Object -First 30 |
    ForEach-Object {
        $paths = @($_.Group | Sort-Object Path | Select-Object -ExpandProperty Path)
        [pscustomobject]@{
            Type = $_.Name
            Files = $_.Count
            TotalLines = ($_.Group | Measure-Object Lines -Sum).Sum
            SamplePaths = ($paths | Select-Object -First 8) -join ', '
        }
    })

$largestFiles = @($production | Sort-Object Lines -Descending | Select-Object -First 30)
$tinyFiles = @($under60 | Sort-Object Lines, Path | Select-Object -First 60)

function Format-Percent {
    param([int]$Count, [int]$Total)
    if ($Total -eq 0) { return "0.0%" }
    return ("{0:N1}%" -f (($Count / $Total) * 100.0))
}

$timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
$outPath = Join-Path $Root $Output
$outDir = Split-Path $outPath -Parent
if ($outDir -and -not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Sussudio Defragmentation Baseline - Generated")
$lines.Add("")
$lines.Add("Generated UTC: $timestamp")
$lines.Add("Root: $Root")
$lines.Add("")
$lines.Add("## Summary")
$lines.Add("")
$lines.Add("| Metric | Value |")
$lines.Add("| --- | ---: |")
$lines.Add("| Production .cs files | $($production.Count) |")
$lines.Add("| Test .cs files | $($tests.Count) |")
$lines.Add("| Core app .cs files (Sussudio/) | $($coreApp.Count) |")
$lines.Add("| Core app nonblank LoC (Sussudio/) | $coreAppNonBlank |")
$lines.Add("| Sussudio.Tests .cs files | $($sussudioTests.Count) |")
$lines.Add("| Sussudio.Tests nonblank LoC | $sussudioTestsNonBlank |")
$lines.Add("| Production .cs files under 60 lines | $($under60.Count) ($(Format-Percent $under60.Count $production.Count)) |")
$lines.Add("| Production .cs files under 80 lines | $($under80.Count) ($(Format-Percent $under80.Count $production.Count)) |")
$lines.Add("")
$lines.Add("## Largest partial-type clusters")
$lines.Add("")
$lines.Add("| Type | Files | Total lines | Sample paths |")
$lines.Add("| --- | ---: | ---: | --- |")
foreach ($cluster in $partialClusters) {
    $sample = $cluster.SamplePaths.Replace('|','\\|')
    $lines.Add("| $($cluster.Type) | $($cluster.Files) | $($cluster.TotalLines) | $sample |")
}
$lines.Add("")
$lines.Add("## Largest production files")
$lines.Add("")
$lines.Add("| Lines | Path |")
$lines.Add("| ---: | --- |")
foreach ($entry in $largestFiles) {
    $lines.Add("| $($entry.Lines) | $($entry.Path) |")
}
$lines.Add("")
$lines.Add("## Sample production files under 60 lines")
$lines.Add("")
$lines.Add("| Lines | Path |")
$lines.Add("| ---: | --- |")
foreach ($entry in $tinyFiles) {
    $lines.Add("| $($entry.Lines) | $($entry.Path) |")
}
$lines.Add("")
$lines.Add("## Notes")
$lines.Add("")
$lines.Add("Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.")

$lines | Set-Content -LiteralPath $outPath -Encoding UTF8
Write-Host "Wrote $outPath"
