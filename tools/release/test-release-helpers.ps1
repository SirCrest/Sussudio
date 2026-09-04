param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot 'release-helpers.ps1'
. $helperPath

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

function Assert-ThrowsContaining {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$ExpectedText
    )

    try {
        & $Action
    }
    catch {
        if ($_.Exception.Message -notlike "*$ExpectedText*") {
            throw "Expected failure containing '$ExpectedText', received '$($_.Exception.Message)'."
        }
        return
    }

    throw "Expected failure containing '$ExpectedText', but the action succeeded."
}

function Invoke-TestGit {
    param([string]$Repo, [string[]]$Arguments)
    $output = @(& git -C $Repo @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "Test git command failed: git $($Arguments -join ' ') :: $($output -join ' ')"
    }
}

Assert-True (Test-SemVerPrerelease -Version '1.2.3-beta.1') 'Canonical SemVer prerelease was rejected.'
Assert-True (-not (Test-SemVerPrerelease -Version '1.2.3')) 'Stable SemVer must not pass the prerelease gate.'
Assert-True (-not (Test-SemVerPrerelease -Version '1.2.3-beta.01')) 'Numeric prerelease identifiers with leading zero must fail.'
Assert-True (-not (Test-SemVerPrerelease -Version '..\escape-beta.1')) 'Path-like version must fail.'

$tempParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/')
$tempRoot = Join-Path $tempParent ("SussudioReleaseHelpers-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null

try {
    $gitRepo = Join-Path $tempRoot 'repo'
    New-Item -ItemType Directory -Path $gitRepo | Out-Null
    Invoke-TestGit -Repo $gitRepo -Arguments @('init', '--quiet')
    Invoke-TestGit -Repo $gitRepo -Arguments @('config', 'user.name', 'Sussudio Release Test')
    Invoke-TestGit -Repo $gitRepo -Arguments @('config', 'user.email', 'release-test@example.invalid')
    Invoke-TestGit -Repo $gitRepo -Arguments @('config', 'commit.gpgsign', 'false')
    'fixture' | Set-Content -LiteralPath (Join-Path $gitRepo 'fixture.txt') -Encoding ASCII
    Invoke-TestGit -Repo $gitRepo -Arguments @('add', 'fixture.txt')
    Invoke-TestGit -Repo $gitRepo -Arguments @('commit', '--quiet', '-m', 'fixture')
    Invoke-TestGit -Repo $gitRepo -Arguments @('tag', 'v1.2.3-beta.1')

    $head = Assert-CleanTaggedPrerelease -RepoRoot $gitRepo -Version '1.2.3-beta.1'
    Assert-True ($head -match '^[0-9a-f]{40}$') 'Clean tagged prerelease did not return a full commit ID.'
    'dirty' | Set-Content -LiteralPath (Join-Path $gitRepo 'untracked.txt') -Encoding ASCII
    Assert-ThrowsContaining {
        $null = Assert-CleanTaggedPrerelease -RepoRoot $gitRepo -Version '1.2.3-beta.1'
    } 'clean worktree'
    Remove-Item -LiteralPath (Join-Path $gitRepo 'untracked.txt') -Force

    $manifestPath = Join-Path $tempRoot 'manifest.json'
    @'
{
  "schemaVersion": 1,
  "buildVersion": "fixture-build",
  "files": [
    { "fileName": "avcodec-62.dll", "productVersion": "fixture-build", "fileVersion": "1", "sha256": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" },
    { "fileName": "avformat-62.dll", "productVersion": "fixture-build", "fileVersion": "1", "sha256": "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB" },
    { "fileName": "avutil-60.dll", "productVersion": "fixture-build", "fileVersion": "1", "sha256": "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC" },
    { "fileName": "swresample-6.dll", "productVersion": "fixture-build", "fileVersion": "1", "sha256": "DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD" }
  ]
}
'@ | Set-Content -LiteralPath $manifestPath -Encoding ASCII
    $manifest = Get-ValidatedFfmpegManifest -ManifestPath $manifestPath
    Assert-True (@($manifest.files).Count -eq 4) 'Valid FFmpeg manifest did not retain four entries.'

    $badManifestPath = Join-Path $tempRoot 'bad-manifest.json'
    (Get-Content -LiteralPath $manifestPath -Raw).Replace(
        '"productVersion": "fixture-build"',
        '"productVersion": "contradictory-build"') | Set-Content -LiteralPath $badManifestPath -Encoding ASCII
    Assert-ThrowsContaining {
        $null = Get-ValidatedFfmpegManifest -ManifestPath $badManifestPath
    } 'must equal buildVersion'

    $staging = Join-Path $tempRoot 'staging'
    $nested = Join-Path $staging 'tools\ssctl'
    New-Item -ItemType Directory -Path $nested -Force | Out-Null
    'app' | Set-Content -LiteralPath (Join-Path $staging 'README.txt') -Encoding ASCII
    'tool' | Set-Content -LiteralPath (Join-Path $nested 'ssctl.exe') -Encoding ASCII
    $zipPath = Join-Path $tempRoot 'valid.zip'
    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zipPath
    Assert-ZipMatchesDirectory -ZipPath $zipPath -StagingDirectory $staging

    'late' | Set-Content -LiteralPath (Join-Path $staging 'unexpected.txt') -Encoding ASCII
    Assert-ThrowsContaining {
        Assert-ZipMatchesDirectory -ZipPath $zipPath -StagingDirectory $staging
    } 'inventory differs'
    Remove-Item -LiteralPath (Join-Path $staging 'unexpected.txt') -Force

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $duplicateZip = Join-Path $tempRoot 'duplicate.zip'
    $stream = [IO.File]::Open($duplicateZip, [IO.FileMode]::CreateNew)
    try {
        $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            $null = $archive.CreateEntry('README.txt')
            $null = $archive.CreateEntry('README.txt')
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
    Assert-ThrowsContaining {
        Assert-ZipMatchesDirectory -ZipPath $duplicateZip -StagingDirectory $staging
    } 'duplicate file entry'

    $unsafeZip = Join-Path $tempRoot 'unsafe.zip'
    $stream = [IO.File]::Open($unsafeZip, [IO.FileMode]::CreateNew)
    try {
        $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            $null = $archive.CreateEntry('../escape.txt')
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
    Assert-ThrowsContaining {
        Assert-ZipMatchesDirectory -ZipPath $unsafeZip -StagingDirectory $staging
    } 'unsafe entry path'

    Write-Output 'PASS: release helper behavior is fail-closed.'
}
finally {
    $resolvedTempRoot = [IO.Path]::GetFullPath($tempRoot)
    $tempPrefix = $tempParent + [IO.Path]::DirectorySeparatorChar
    if ($resolvedTempRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTempRoot)) {
        Remove-Item -LiteralPath $resolvedTempRoot -Recurse -Force
    }
}
