<# Builds, validates, signs, and packages the supported Sussudio portable prerelease ZIP.

The script deliberately has no unsigned or stale-publish mode. A package is produced only
from a clean commit with an exact SemVer prerelease tag after the canonical reliability gate
passes for that same commit.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [Parameter(Mandatory = $true)]
    [string]$SignToolPath,
    [Parameter(Mandatory = $true)]
    [string]$AzureCodeSigningDlibPath,
    [Parameter(Mandatory = $true)]
    [string]$ArtifactSigningMetadataPath,
    [string]$AzureCliPath = 'az'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$releaseHelperPath = Join-Path $PSScriptRoot 'release\release-helpers.ps1'
$reliabilityGatePath = Join-Path $PSScriptRoot 'reliability-gates.ps1'
$solutionPath = Join-Path $repoRoot 'Sussudio.slnx'
$appProjectPath = Join-Path $repoRoot 'Sussudio\Sussudio.csproj'
$ffmpegManifestPath = Join-Path $repoRoot 'Sussudio\ffmpeg\manifest.json'
$sourceFfmpegDirectory = Join-Path $repoRoot 'Sussudio\ffmpeg'
$releaseOutputRoot = Join-Path $repoRoot 'artifacts\releases'
$packageWorkRoot = Join-Path $repoRoot 'artifacts\release-package'
$publishRoot = Join-Path $packageWorkRoot 'publish'
$releaseName = "Sussudio-$Version-win-x64"
$stagingDir = Join-Path $packageWorkRoot $releaseName
$appDir = Join-Path $stagingDir 'app'
$toolsDir = Join-Path $stagingDir 'tools'
$zipPath = Join-Path $releaseOutputRoot "$releaseName.zip"
$checksumPath = Join-Path $releaseOutputRoot "$releaseName.sha256.txt"
$githubNotesPath = Join-Path $releaseOutputRoot "$releaseName.github-release.md"

foreach ($requiredPath in @($releaseHelperPath, $reliabilityGatePath, $solutionPath, $appProjectPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required release input is missing: $requiredPath"
    }
}

. $releaseHelperPath

function Invoke-RobocopyMirror {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        throw "Publish directory does not exist: $Source"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    $null = & robocopy $Source $Destination /MIR /NFL /NDL /NJH /NJS /NP
    $exitCode = $LASTEXITCODE
    if ($exitCode -ge 8) {
        throw "robocopy failed with exit code $exitCode syncing '$Source' to '$Destination'."
    }
}

$sourceCommit = Assert-CleanTaggedPrerelease -RepoRoot $repoRoot -Version $Version
$signingPrerequisites = Resolve-ArtifactSigningPrerequisites `
    -RepoRoot $repoRoot `
    -SignToolPath $SignToolPath `
    -AzureCodeSigningDlibPath $AzureCodeSigningDlibPath `
    -ArtifactSigningMetadataPath $ArtifactSigningMetadataPath `
    -AzureCliPath $AzureCliPath
$ffmpegManifest = Get-ValidatedFfmpegManifest -ManifestPath $ffmpegManifestPath
Assert-FfmpegRuntimeMatchesManifest -RuntimeDirectory $sourceFfmpegDirectory -Manifest $ffmpegManifest

Write-Host 'Running the canonical reliability gate before publish, staging, or signing...'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $reliabilityGatePath `
    -Configuration Release `
    -Platform x64 `
    -FailOnAnyWarning
if ($LASTEXITCODE -ne 0) {
    throw "Reliability gate failed with exit code $LASTEXITCODE. No package was staged or signed."
}

$validatedCommit = Assert-CleanTaggedPrerelease -RepoRoot $repoRoot -Version $Version -ExpectedCommit $sourceCommit
if (-not [string]::Equals($validatedCommit, $sourceCommit, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Validated commit changed unexpectedly from $sourceCommit to $validatedCommit."
}

New-Item -ItemType Directory -Path $releaseOutputRoot -Force | Out-Null
foreach ($path in @($stagingDir, $publishRoot)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}
foreach ($path in @($zipPath, $checksumPath, $githubNotesPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

$appPublishDir = Join-Path $publishRoot 'app'
Write-Host 'Publishing Sussudio Release win-x64...'
$null = Invoke-ReleaseNativeCommand `
    -FilePath 'dotnet' `
    -Arguments @(
        'publish', $appProjectPath,
        '-c', 'Release',
        '-p:Platform=x64',
        '-r', 'win-x64',
        '-p:PublishProfile=win-x64',
        "-p:PublishDir=$appPublishDir\",
        '-p:WindowsPackageType=None',
        '-p:SelfContained=true',
        '-p:WindowsAppSDKSelfContained=true'
    ) `
    -Description 'Sussudio publish' `
    -WorkingDirectory $repoRoot

$toolProjects = @(
    [pscustomobject]@{ Name = 'ssctl'; Project = (Join-Path $repoRoot 'tools\ssctl\ssctl.csproj'); Executable = 'ssctl.exe' },
    [pscustomobject]@{ Name = 'McpServer'; Project = (Join-Path $repoRoot 'tools\McpServer\McpServer.csproj'); Executable = 'McpServer.exe' },
    [pscustomobject]@{ Name = 'AutomationClient'; Project = (Join-Path $repoRoot 'tools\AutomationClient\AutomationClient.csproj'); Executable = 'AutomationClient.exe' }
)

foreach ($tool in $toolProjects) {
    if (-not (Test-Path -LiteralPath $tool.Project -PathType Leaf)) {
        throw "Tool project is missing: $($tool.Project)"
    }

    $toolPublishDir = Join-Path $publishRoot $tool.Name
    Write-Host "Publishing $($tool.Name) Release win-x64 self-contained..."
    $null = Invoke-ReleaseNativeCommand `
        -FilePath 'dotnet' `
        -Arguments @(
            'publish', $tool.Project,
            '-c', 'Release',
            '-r', 'win-x64',
            '--self-contained', 'true',
            '-p:PublishSingleFile=false',
            "-p:PublishDir=$toolPublishDir\"
        ) `
        -Description "$($tool.Name) publish" `
        -WorkingDirectory $repoRoot
}

Invoke-RobocopyMirror -Source $appPublishDir -Destination $appDir
foreach ($tool in $toolProjects) {
    Invoke-RobocopyMirror -Source (Join-Path $publishRoot $tool.Name) -Destination (Join-Path $toolsDir $tool.Name)
}

$packagedFfmpegDirectory = Join-Path $appDir 'ffmpeg'
$packagedFfmpegManifest = Get-ValidatedFfmpegManifest -ManifestPath (Join-Path $packagedFfmpegDirectory 'manifest.json')
Assert-FfmpegRuntimeMatchesManifest -RuntimeDirectory $packagedFfmpegDirectory -Manifest $packagedFfmpegManifest

$launcherPath = Join-Path $stagingDir 'Start Sussudio.cmd'
@"
@echo off
setlocal
set "ROOT=%~dp0"
start "" "%ROOT%app\Sussudio.exe" %*
"@ | Set-Content -LiteralPath $launcherPath -Encoding ASCII

$readmePath = Join-Path $stagingDir 'README.txt'
@"
Sussudio $Version (Windows x64 prerelease)

Run the app:
  Double-click Start Sussudio.cmd

Included automation tools:
  tools\ssctl\ssctl.exe
  tools\McpServer\McpServer.exe
  tools\AutomationClient\AutomationClient.exe

Requirements:
  Windows 10/11 x64.
  HDMI capture hardware. The primary target is Elgato 4K X.
  NVIDIA hardware encoder support is expected for recording.

Authenticity:
  First-party Sussudio executables and assemblies are Authenticode-signed and
  timestamped with Microsoft Artifact Signing. The FFmpeg DLLs are third-party
  inputs pinned by app\ffmpeg\manifest.json and are intentionally not signed with
  the Sussudio publisher identity. Verify the ZIP SHA-256 published with the release.

This ZIP is the current supported prerelease channel. MSIX distribution and the
Stream Deck plugin remain future roadmap work and are not included.
"@ | Set-Content -LiteralPath $readmePath -Encoding ASCII

$licenseSource = Join-Path $repoRoot 'LICENSE'
if (Test-Path -LiteralPath $licenseSource -PathType Leaf) {
    Copy-Item -LiteralPath $licenseSource -Destination (Join-Path $stagingDir 'LICENSE.txt') -Force
}

$manifestPath = Join-Path $stagingDir 'RELEASE.txt'
@"
Sussudio Portable Prerelease

Version: $Version
Git tag: v$Version
Source commit: $sourceCommit
Source tree clean: Yes
Configuration: Release
Runtime: win-x64
GitHub release classification: Prerelease
FFmpeg build: $($ffmpegManifest.buildVersion)
FFmpeg identity manifest: app\ffmpeg\manifest.json
First-party signing: Azure Artifact Signing, SHA-256, Microsoft timestamp
Built at UTC: $([DateTime]::UtcNow.ToString('o'))

Package layout:
  Start Sussudio.cmd
  README.txt
  LICENSE.txt
  RELEASE.txt
  app\
  tools\ssctl\
  tools\McpServer\
  tools\AutomationClient\
"@ | Set-Content -LiteralPath $manifestPath -Encoding ASCII

$firstPartyRelativePaths = @(
    'app\Sussudio.exe',
    'app\Sussudio.dll',
    'app\Sussudio.Automation.Contracts.dll'
)
foreach ($tool in $toolProjects) {
    $toolExecutableDll = [IO.Path]::ChangeExtension($tool.Executable, '.dll')
    $firstPartyRelativePaths += @(
        "tools\$($tool.Name)\$($tool.Executable)",
        "tools\$($tool.Name)\$toolExecutableDll",
        "tools\$($tool.Name)\Sussudio.Automation.Contracts.dll"
    )
}
if ($firstPartyRelativePaths -match '(^|\\)ffmpeg(\\|$)') {
    throw 'Internal release policy error: third-party FFmpeg files must not be signed with the Sussudio publisher identity.'
}
Assert-RequiredPackageFiles -Root $stagingDir -RelativePaths $firstPartyRelativePaths
$firstPartyFiles = @($firstPartyRelativePaths | ForEach-Object { Join-Path $stagingDir $_ })

# Re-check immediately before the first externally authenticated mutation so the
# signed payload is tied to the same clean commit that passed the reliability gate.
$null = Assert-CleanTaggedPrerelease -RepoRoot $repoRoot -Version $Version -ExpectedCommit $sourceCommit
Invoke-ArtifactSigning -Prerequisites $signingPrerequisites -Files $firstPartyFiles
Assert-AuthenticodeSignatures -Prerequisites $signingPrerequisites -Files $firstPartyFiles

$requiredPackageRelativePaths = @(
    'Start Sussudio.cmd',
    'README.txt',
    'RELEASE.txt',
    'app\Sussudio.exe',
    'app\ffmpeg\manifest.json'
)
foreach ($tool in $toolProjects) {
    $requiredPackageRelativePaths += "tools\$($tool.Name)\$($tool.Executable)"
}
foreach ($entry in @($packagedFfmpegManifest.files)) {
    $requiredPackageRelativePaths += "app\ffmpeg\$($entry.fileName)"
}
if (Test-Path -LiteralPath (Join-Path $stagingDir 'LICENSE.txt')) {
    $requiredPackageRelativePaths += 'LICENSE.txt'
}
Assert-RequiredPackageFiles -Root $stagingDir -RelativePaths $requiredPackageRelativePaths

Write-Host "Creating $zipPath..."
Compress-Archive -Path (Join-Path $stagingDir '*') -DestinationPath $zipPath -CompressionLevel Optimal -Force
Assert-ZipContainsFiles -ZipPath $zipPath -RelativePaths $requiredPackageRelativePaths
Assert-ZipMatchesDirectory -ZipPath $zipPath -StagingDirectory $stagingDir

$zipHash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
"$($zipHash.Hash)  $releaseName.zip" | Set-Content -LiteralPath $checksumPath -Encoding ASCII

@"
## Sussudio $Version (prerelease)

Portable Windows x64 prerelease package. In GitHub, publish this with **Set as a
pre-release** enabled; it is not a stable release.

### Download

- ``$releaseName.zip``
- SHA-256: ``$($zipHash.Hash)``

### Authenticity and contents

- First-party Sussudio executables and assemblies are Authenticode-signed and
  Microsoft-timestamped through Azure Artifact Signing.
- Native FFmpeg DLLs are third-party inputs verified against the included
  ``app\ffmpeg\manifest.json``; they do not carry the Sussudio publisher signature.
- The ZIP includes the app, ``ssctl``, MCP server, and ``AutomationClient``.
- MSIX and Stream Deck integration remain future roadmap items.

Extract the ZIP, verify its SHA-256, then double-click ``Start Sussudio.cmd``.
"@ | Set-Content -LiteralPath $githubNotesPath -Encoding ASCII

Write-Host ''
Write-Host 'Signed prerelease package created:'
Write-Host "  $zipPath"
Write-Host "  $checksumPath"
Write-Host "  $githubNotesPath"
Write-Host 'SHA256:'
Write-Host "  $($zipHash.Hash)"
