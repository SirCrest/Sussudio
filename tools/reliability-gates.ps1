param(
    [switch]$FailOnAnyWarning
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "ElgatoCapture\ElgatoCapture.csproj"
$platforms = @("x64", "x86", "ARM64")

if (!(Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

foreach ($platform in $platforms) {
    Write-Host "=== Build Gate: $platform ==="
    $output = & dotnet build $projectPath -c Debug -p:Platform=$platform 2>&1
    $output | ForEach-Object { Write-Host $_ }

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for platform $platform"
    }

    if ($output -match "MVVMTK0045") {
        throw "MVVMTK0045 warning detected in $platform build output"
    }

    if ($FailOnAnyWarning -and ($output -match ": warning ")) {
        throw "Warnings detected in $platform build output"
    }
}

Write-Host "Build matrix passed: x64, x86, ARM64"

$logPath = Join-Path ([Environment]::GetFolderPath("MyDocuments")) "ElgatoCapture_Debug.log"
if (Test-Path $logPath) {
    $latestDiagnostics = Select-String -Path $logPath -Pattern "CaptureDiagnostics:" | Select-Object -Last 1
    if ($latestDiagnostics) {
        Write-Host "Latest diagnostics snapshot:"
        Write-Host $latestDiagnostics.Line
    }
    else {
        Write-Warning "No CaptureDiagnostics snapshot found yet. Complete one record start/stop cycle first."
    }
}
else {
    Write-Warning "Log file not found yet: $logPath"
}

Write-Host ""
Write-Host "Manual stress checklist:"
Write-Host "1. Preview start/stop loop x200 (no deadlocks/crashes)."
Write-Host "2. Record start/stop loop x200 (no orphan ffmpeg process)."
Write-Host "3. Close app during active recording (clean shutdown)."
Write-Host "4. 20-minute recording under storage pressure (bounded queue/memory behavior)."
