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
$Root = (Resolve-Path -LiteralPath $Root).Path

$excludeDirs = @(
    ".git", ".vs", ".vscode", "bin", "obj", "packages", "TestResults", "artifacts", "node_modules"
)

function Get-RepositoryStatusSnapshot {
    param([string]$RepoRoot)
    $status = & git -C $RepoRoot status --porcelain
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to capture repository status for baseline generation."
    }

    return (@($status) -join "`n")
}

function Get-InputFingerprint {
    param([object[]]$Inputs)
    return (@($Inputs | Sort-Object FullName | ForEach-Object {
        "{0}|{1}|{2}|{3}" -f $_.FullName, $_.Length, $_.LastWriteTimeUtc.Ticks, $_.Hash
    }) -join "`n")
}

function Test-ExcludedPath {
    param([string]$Path)
    $parts = $Path -split '[\\/]'
    foreach ($dir in $excludeDirs) {
        if ($parts -contains $dir) { return $true }
    }
    return $false
}

function Get-CapturedSourceInput {
    param([System.IO.FileInfo]$File)
    $raw = Get-Content -LiteralPath $File.FullName -Raw -ErrorAction Stop
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($raw))
    }
    finally {
        $sha256.Dispose()
    }
    [pscustomobject]@{
        FullName = $File.FullName
        Length = $File.Length
        LastWriteTimeUtc = $File.LastWriteTimeUtc
        Hash = [System.BitConverter]::ToString($hashBytes).Replace('-', '')
        Raw = $raw
    }
}

function Split-CapturedSourceLines {
    param([string]$Raw)
    if ($Raw.Length -eq 0) { return @() }
    $lines = @($Raw -split "`r`n|`n|`r")
    if ($lines.Count -gt 0 -and $lines[$lines.Count - 1].Length -eq 0) {
        return @($lines | Select-Object -First ($lines.Count - 1))
    }

    return $lines
}

function Sum-NonBlankLines {
    param([object[]]$Entries)
    $sum = 0
    foreach ($entry in $Entries) {
        $sum += [int]$entry.NonBlankLines
    }

    return $sum
}

function Convert-ToRepoPath {
    param([string]$Path)
    $rootPath = $Root.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $rootUri = [Uri]::new($rootPath)
    $pathUri = [Uri]::new((Resolve-Path -LiteralPath $Path).Path)
    $relative = [Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString())
    return $relative -replace '\\','/'
}

$beforeStatus = Get-RepositoryStatusSnapshot $Root

$allCs = Get-ChildItem -LiteralPath $Root -Recurse -Filter *.cs -File |
    Where-Object { -not (Test-ExcludedPath $_.FullName) }
$capturedInputs = @($allCs | ForEach-Object { Get-CapturedSourceInput $_ })
$beforeFingerprint = Get-InputFingerprint $capturedInputs

$entries = foreach ($input in $capturedInputs) {
    $repoPath = Convert-ToRepoPath $input.FullName
    $isTest = $repoPath -match '(^|/)(test|tests|.*\.Tests?)(/|$)' -or $repoPath -match '(Test|Tests)\.cs$'
    $isGenerated = $repoPath -match '(\.g\.cs$|\.Designer\.cs$|\.generated\.cs$|/Generated/|/obj/)'
    $partialMatches = [regex]::Matches($input.Raw, '\bpartial\s+(?:class|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)')
    $lines = @(Split-CapturedSourceLines $input.Raw)
    [pscustomobject]@{
        Path = $repoPath
        Lines = $lines.Count
        NonBlankLines = @($lines | Where-Object { $_.Trim().Length -gt 0 }).Count
        IsTest = $isTest
        IsGenerated = $isGenerated
        PartialTypes = @($partialMatches | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    }
}

$production = @($entries | Where-Object { -not $_.IsTest -and -not $_.IsGenerated })
$tests = @($entries | Where-Object { $_.IsTest })
$coreApp = @($entries | Where-Object { -not $_.IsGenerated -and $_.Path -like 'Sussudio/*' })
$sussudioTests = @($entries | Where-Object { -not $_.IsGenerated -and $_.Path -like 'tests/Sussudio.Tests/*' })
$coreAppNonBlank = Sum-NonBlankLines $coreApp
$sussudioTestsNonBlank = Sum-NonBlankLines $sussudioTests
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
$outPath = if ([System.IO.Path]::IsPathRooted($Output)) { $Output } else { Join-Path $Root $Output }
$outDir = Split-Path $outPath -Parent
if ([string]::IsNullOrWhiteSpace($outDir)) { $outDir = $Root }
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

$afterStatus = Get-RepositoryStatusSnapshot $Root
if ($beforeStatus -cne $afterStatus) {
    throw "Repository changed while capturing baseline; discard this run and retry from a stable tree."
}

$afterCs = Get-ChildItem -LiteralPath $Root -Recurse -Filter *.cs -File |
    Where-Object { -not (Test-ExcludedPath $_.FullName) }
$afterInputs = @($afterCs | ForEach-Object { Get-CapturedSourceInput $_ })
$afterFingerprint = Get-InputFingerprint $afterInputs
if ($beforeFingerprint -cne $afterFingerprint) {
    throw "Baseline input files changed while capturing baseline; discard this run and retry from a stable tree."
}

$tmpOutPath = Join-Path $outDir (".{0}.{1}.tmp" -f ([System.IO.Path]::GetFileName($outPath)), ([Guid]::NewGuid().ToString("N")))
try {
    $lines | Set-Content -LiteralPath $tmpOutPath -Encoding UTF8
    Move-Item -LiteralPath $tmpOutPath -Destination $outPath -Force
}
finally {
    if (Test-Path -LiteralPath $tmpOutPath) {
        Remove-Item -LiteralPath $tmpOutPath -Force
    }
}
Write-Host "Wrote $outPath"
