# Behavioral regression coverage for corpus directory pruning.
# Failure modes covered:
# 1. Descending into .claude/worktrees lets stale worktree references hide a live member finding.
# 2. Excluding all of .claude loses operator knobs documented in .claude/napkin.md.
# 3. Scanning generated bin/obj content lets generated references hide a live member finding.

param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path,
    [string]$ArtifactPath = 'artifacts/dead-surface-regression.json'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) "dead-surface-pruning-$([guid]::NewGuid().ToString('N'))"
$reportPath = Join-Path $fixtureRoot 'report.json'
$scannerPath = Join-Path $RepositoryRoot 'scripts/architecture/Report-DeadSurface.ps1'

try {
    $directories = @(
        'Sussudio',
        'tests',
        '.claude/worktrees/old/Sussudio',
        'tests/bin/Generated',
        'tests/obj/Generated'
    )
    foreach ($directory in $directories) {
        New-Item -ItemType Directory -Force -Path (Join-Path $fixtureRoot $directory) | Out-Null
    }
    & git -C $fixtureRoot init --quiet

    Set-Content -LiteralPath (Join-Path $fixtureRoot 'Sussudio/Fixture.cs') -Encoding utf8 -Value @'
using System;
public sealed class Fixture
{
    public static void Entry() => Environment.GetEnvironmentVariable("FIXTURE_KNOB");
    public void WorktreeOnlyReference() { }
    public void GeneratedOnlyReference() { }
}
'@
    Set-Content -LiteralPath (Join-Path $fixtureRoot '.claude/napkin.md') -Encoding utf8 -Value 'The FIXTURE_KNOB setting controls fixture behavior.'
    Set-Content -LiteralPath (Join-Path $fixtureRoot '.claude/worktrees/old/Sussudio/Old.cs') -Encoding utf8 -Value 'class Old { void Use() { new Fixture().WorktreeOnlyReference(); } }'
    Set-Content -LiteralPath (Join-Path $fixtureRoot 'tests/bin/Generated/Generated.cs') -Encoding utf8 -Value 'class Generated { void Use() { new Fixture().GeneratedOnlyReference(); } }'
    Set-Content -LiteralPath (Join-Path $fixtureRoot 'tests/obj/Generated/Generated.cs') -Encoding utf8 -Value 'class GeneratedAgain { void Use() { new Fixture().GeneratedOnlyReference(); } }'
    & git -C $fixtureRoot -c user.name=Fixture -c user.email=fixture@example.invalid add .
    & git -C $fixtureRoot -c user.name=Fixture -c user.email=fixture@example.invalid commit --quiet -m fixture

    & $scannerPath -Root $fixtureRoot -OutputJson 'report.json' | Out-Null
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    $findingNames = @($report.findings | ForEach-Object { $_.name })

    $expectedFindings = @(
        'Sussudio/Fixture.cs::WorktreeOnlyReference',
        'Sussudio/Fixture.cs::GeneratedOnlyReference'
    )
    $missing = @($expectedFindings | Where-Object { $findingNames -notcontains $_ })
    $unexpectedKnob = @($report.findings | Where-Object { $_.category -eq 'env-unset' -and $_.name -eq 'FIXTURE_KNOB' })
    $passed = ($missing.Count -eq 0 -and $unexpectedKnob.Count -eq 0)

    $artifact = [pscustomobject]@{
        generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
        passed = $passed
        missingExpectedFindings = $missing
        documentedKnobReportedUnset = ($unexpectedKnob.Count -gt 0)
        findings = $findingNames
    }
    $artifactPathFull = Join-Path $RepositoryRoot $ArtifactPath
    $artifactDirectory = Split-Path -Parent $artifactPathFull
    if ($artifactDirectory -and -not (Test-Path -LiteralPath $artifactDirectory)) {
        New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null
    }
    $artifact | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $artifactPathFull -Encoding utf8

    if (-not $passed) {
        throw "Dead-surface pruning regression failed; see $artifactPathFull"
    }
    "Dead-surface pruning regression passed; wrote $artifactPathFull"
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        $resolvedFixture = (Resolve-Path -LiteralPath $fixtureRoot).Path
        $resolvedTempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char]'\') + [System.IO.Path]::DirectorySeparatorChar
        $fixtureName = Split-Path -Leaf $resolvedFixture
        if (-not $resolvedFixture.StartsWith($resolvedTempRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
            $fixtureName -notlike 'dead-surface-pruning-*') {
            throw "Refusing to remove unexpected fixture path: $resolvedFixture"
        }
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
