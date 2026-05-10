<# Runs the build, tool, test, and optional HDR validation gates used before treating a change as
reliability-ready. #>
param(
    [switch]$FailOnAnyWarning,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [ValidateSet("x64")]
    [string]$Platform = "x64",
    [int]$BuildTimeoutSeconds = 900,
    [int]$TestTimeoutSeconds = 900,
    [string]$ValidateHdrFile,
    [switch]$ValidateHdrExpectHdr,
    [ValidateSet("hevc", "av1", "either")]
    [string]$ValidateHdrCodec = "either",
    [switch]$ValidateHdrRequireHdr10StaticMetadata,
    [double]$ValidateHdrExpectedFps = 0
)

$ErrorActionPreference = "Stop"

function Invoke-ToolWithTimeout {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Exe,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds,
        [string]$WorkingDirectory = (Get-Location).Path
    )

    $argumentString = [string]::Join(" ", $Arguments)
    Write-Host "> $Exe $argumentString"

    $job = Start-Job -ScriptBlock {
        param($ToolExe, $ToolArgs, $ToolWorkingDirectory)
        if (-not [string]::IsNullOrWhiteSpace($ToolWorkingDirectory)) {
            Set-Location -Path $ToolWorkingDirectory
        }
        $allOutput = & $ToolExe @ToolArgs 2>&1 | ForEach-Object { $_.ToString() }
        $code = if ($null -eq $LASTEXITCODE) { 0 } else { [int]$LASTEXITCODE }
        [pscustomobject]@{
            ExitCode = $code
            Output = @($allOutput)
        }
    } -ArgumentList @($Exe, $Arguments, $WorkingDirectory)

    try {
        $completed = Wait-Job -Job $job -Timeout $TimeoutSeconds
        if (-not $completed) {
            Stop-Job -Job $job -Force -ErrorAction SilentlyContinue | Out-Null
            throw "Command timed out after $TimeoutSeconds seconds: $Exe $argumentString"
        }

        $result = Receive-Job -Job $job -ErrorAction Stop
        $output = @($result.Output)
        $output | ForEach-Object { Write-Host $_ }

        $exitCode = [int]$result.ExitCode
        if ($exitCode -ne 0) {
            throw "Command failed (exit code $exitCode): $Exe $argumentString"
        }

        return $output
    }
    finally {
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue | Out-Null
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnetCliHome = Join-Path $repoRoot ".tmp_dotnet_home"
if (-not (Test-Path $dotnetCliHome)) {
    New-Item -Path $dotnetCliHome -ItemType Directory | Out-Null
}
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_HOME = $dotnetCliHome

$projectPath = Join-Path $repoRoot "Sussudio\Sussudio.csproj"
$testProjectPath = Join-Path $repoRoot "tests\Sussudio.Tests\Sussudio.Tests.csproj"
$ssctlProjectPath = Join-Path $repoRoot "tools\ssctl\ssctl.csproj"
$mcpServerProjectPath = Join-Path $repoRoot "tools\McpServer\McpServer.csproj"
$nativeXuProbeProjectPath = Join-Path $repoRoot "tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj"
if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}
if (-not (Test-Path $testProjectPath)) {
    throw "Test project file not found: $testProjectPath"
}
if (-not (Test-Path $ssctlProjectPath)) {
    throw "ssctl project file not found: $ssctlProjectPath"
}
if (-not (Test-Path $mcpServerProjectPath)) {
    throw "McpServer project file not found: $mcpServerProjectPath"
}
if (-not (Test-Path $nativeXuProbeProjectPath)) {
    throw "NativeXuAudioProbe project file not found: $nativeXuProbeProjectPath"
}

$buildOutput = Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $projectPath,
        "-c", $Configuration,
        "-m:1",
        "--nologo",
        "-v", "minimal",
        "-p:Platform=$Platform"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot

if ($buildOutput -match "MVVMTK0045") {
    throw "MVVMTK0045 warning detected in build output."
}

if ($FailOnAnyWarning -and ($buildOutput -match ": warning ")) {
    throw "Warnings detected in build output."
}

Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $ssctlProjectPath,
        "-c", $Configuration,
        "-t:Rebuild",
        "--nologo",
        "-v", "minimal"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot

Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $mcpServerProjectPath,
        "-c", $Configuration,
        "-t:Rebuild",
        "--nologo",
        "-v", "minimal"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot

Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $nativeXuProbeProjectPath,
        "-c", $Configuration,
        "-t:Rebuild",
        "--nologo",
        "-v", "minimal"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot

Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $testProjectPath,
        "-c", $Configuration,
        "--nologo",
        "-v", "minimal"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot

$appAssemblyPath = Join-Path $repoRoot "Sussudio\bin\$Platform\$Configuration\net8.0-windows10.0.19041.0\win-x64\Sussudio.dll"
if (-not (Test-Path $appAssemblyPath)) {
    throw "Built app assembly not found: $appAssemblyPath"
}

Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "run",
        "--project",
        $testProjectPath,
        "-c", $Configuration,
        "--no-build",
        "--",
        $appAssemblyPath
    ) `
    -TimeoutSeconds $TestTimeoutSeconds `
    -WorkingDirectory $repoRoot

if (-not [string]::IsNullOrWhiteSpace($ValidateHdrFile)) {
    $validatorPath = Join-Path $repoRoot "tools\\validate_hdr.ps1"
    if (-not (Test-Path $validatorPath)) {
        throw "HDR validator script not found: $validatorPath"
    }

    $validatorArgs = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $validatorPath,
        "-File", $ValidateHdrFile,
        "-Codec", $ValidateHdrCodec
    )

    if ($ValidateHdrExpectHdr) {
        $validatorArgs += "-ExpectHdr"
    }
    if ($ValidateHdrRequireHdr10StaticMetadata) {
        $validatorArgs += "-RequireHdr10StaticMetadata"
    }
    if ($ValidateHdrExpectedFps -gt 0) {
        $validatorArgs += @("-ExpectedFps", $ValidateHdrExpectedFps.ToString([Globalization.CultureInfo]::InvariantCulture))
    }

    Invoke-ToolWithTimeout `
        -Exe "powershell" `
        -Arguments $validatorArgs `
        -TimeoutSeconds 120 `
        -WorkingDirectory $repoRoot
}

Write-Host ""
Write-Host "Gate result: PASS"
Write-Host "Build, tool, and offline regression gates passed. Optional HDR validation is controlled by the ValidateHdr parameters."
