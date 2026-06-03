<# Builds a GitHub-ready portable release zip for Sussudio.

The zip root contains a double-click launcher and release notes. The full app
payload is kept under app\ so users do not have to sort through the publish
output.
#>
param(
    [string]$Version = (Get-Date -Format "yyyy.MM.dd"),
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputRoot = "artifacts\releases",
    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "Sussudio\Sussudio.csproj"
$absoluteOutputRoot = Join-Path $repoRoot $OutputRoot
$packageWorkRoot = Join-Path $repoRoot "artifacts\release-package"
$publishDir = Join-Path $packageWorkRoot "publish"
$releaseName = "Sussudio-$Version-$RuntimeIdentifier"
$stagingDir = Join-Path $packageWorkRoot $releaseName
$appDir = Join-Path $stagingDir "app"
$zipPath = Join-Path $absoluteOutputRoot "$releaseName.zip"
$checksumPath = Join-Path $absoluteOutputRoot "$releaseName.sha256.txt"
$githubNotesPath = Join-Path $absoluteOutputRoot "$releaseName.github-release.md"

function Invoke-RobocopyMirror {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (!(Test-Path $Source)) {
        throw "Source folder does not exist: $Source"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    $null = & robocopy $Source $Destination /MIR /NFL /NDL /NJH /NJS /NP
    $exitCode = $LASTEXITCODE
    if ($exitCode -ge 8) {
        throw "robocopy failed with exit code $exitCode syncing '$Source' -> '$Destination'"
    }
}

function Get-GitValue {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    try {
        $value = & git @Arguments 2>$null
        if ($LASTEXITCODE -eq 0) {
            return ($value -join "`n").Trim()
        }
    }
    catch {
        return ""
    }

    return ""
}

if (!(Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

New-Item -ItemType Directory -Path $absoluteOutputRoot -Force | Out-Null
if (Test-Path $stagingDir) {
    Remove-Item -LiteralPath $stagingDir -Recurse -Force
}
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
if (Test-Path $checksumPath) {
    Remove-Item -LiteralPath $checksumPath -Force
}
if (Test-Path $githubNotesPath) {
    Remove-Item -LiteralPath $githubNotesPath -Force
}

if (!$SkipPublish) {
    if (Test-Path $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }

    Write-Host "Publishing Sussudio $Configuration $RuntimeIdentifier..."
    & dotnet publish $projectPath `
        -c $Configuration `
        -p:Platform=x64 `
        -r $RuntimeIdentifier `
        -p:PublishProfile=win-x64 `
        -p:PublishDir="$publishDir\" `
        -p:WindowsPackageType=None `
        -p:SelfContained=true `
        -p:WindowsAppSDKSelfContained=true

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}

$publishedExe = Join-Path $publishDir "Sussudio.exe"
if (!(Test-Path $publishedExe)) {
    throw "Published app launcher not found: $publishedExe"
}

Invoke-RobocopyMirror -Source $publishDir -Destination $appDir

$requiredFfmpegDlls = @(
    "avcodec-62.dll",
    "avformat-62.dll",
    "avutil-60.dll",
    "swresample-6.dll"
)
foreach ($dll in $requiredFfmpegDlls) {
    $path = Join-Path $appDir "ffmpeg\$dll"
    if (!(Test-Path $path)) {
        throw "Required FFmpeg runtime DLL missing from package: $path"
    }
}

$launcherPath = Join-Path $stagingDir "Start Sussudio.cmd"
@"
@echo off
setlocal
set "ROOT=%~dp0"
start "" "%ROOT%app\Sussudio.exe" %*
"@ | Set-Content -Path $launcherPath -Encoding ASCII

$readmePath = Join-Path $stagingDir "README.txt"
@"
Sussudio $Version ($RuntimeIdentifier)

Run:
  Double-click Start Sussudio.cmd

Layout:
  app\ contains the full Sussudio application payload.
  Start Sussudio.cmd launches app\Sussudio.exe.

Requirements:
  Windows 10/11 x64.
  HDMI capture hardware. The primary target is Elgato 4K X.
  NVIDIA hardware encoder support is expected for the current recording path.

Notes:
  If Windows SmartScreen warns on first launch, choose More info, then Run anyway.
  Logs are written under %LocalAppData%\Sussudio\logs\ for packaged-style runs.
"@ | Set-Content -Path $readmePath -Encoding ASCII

$licenseSource = Join-Path $repoRoot "LICENSE"
if (Test-Path $licenseSource) {
    Copy-Item -LiteralPath $licenseSource -Destination (Join-Path $stagingDir "LICENSE.txt") -Force
}

$commit = Get-GitValue @("rev-parse", "--short", "HEAD")
$dirty = Get-GitValue @("status", "--porcelain")
$dirtyText = if ([string]::IsNullOrWhiteSpace($dirty)) { "No" } else { "Yes" }
$manifestPath = Join-Path $stagingDir "RELEASE.txt"
@"
Sussudio Release Package

Version: $Version
Configuration: $Configuration
Runtime: $RuntimeIdentifier
Source commit: $commit
Uncommitted changes included: $dirtyText
Built at: $((Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz"))

Package layout:
  Start Sussudio.cmd
  README.txt
  LICENSE.txt
  RELEASE.txt
  app\
"@ | Set-Content -Path $manifestPath -Encoding ASCII

Write-Host "Creating $zipPath..."
Compress-Archive -Path (Join-Path $stagingDir "*") -DestinationPath $zipPath -CompressionLevel Optimal -Force

$hash = Get-FileHash -Path $zipPath -Algorithm SHA256
"$($hash.Hash)  $releaseName.zip" | Set-Content -Path $checksumPath -Encoding ASCII

@"
## Sussudio $Version

Portable Windows x64 release package.

### Download

- ``$releaseName.zip``
- SHA256: ``$($hash.Hash)``

### Run

Extract the zip, then double-click ``Start Sussudio.cmd``.

The zip root contains only the launcher, README, license, release manifest, and
an ``app\`` folder with the full Sussudio payload.

### Notes

- Primary capture target: Elgato 4K X.
- Native FFmpeg/libav DLLs are included under ``app\ffmpeg\``.
- The current recording path expects NVIDIA hardware encoder support.
"@ | Set-Content -Path $githubNotesPath -Encoding ASCII

Write-Host ""
Write-Host "Release package created:"
Write-Host "  $zipPath"
Write-Host "  $checksumPath"
Write-Host "  $githubNotesPath"
Write-Host "SHA256:"
Write-Host "  $($hash.Hash)"
