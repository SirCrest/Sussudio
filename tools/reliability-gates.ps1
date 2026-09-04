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
            Stop-Job -Job $job -ErrorAction SilentlyContinue | Out-Null
            throw "Command timed out after $TimeoutSeconds seconds: $Exe $argumentString"
        }

        $result = Receive-Job -Job $job -ErrorAction Stop
        $output = @($result.Output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
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

function Assert-BuildWarningsAllowed {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Output,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if ($Output -match "MVVMTK0045") {
        throw "MVVMTK0045 warning detected while building $Label."
    }

    if ($FailOnAnyWarning -and ($Output -match ": warning ")) {
        throw "Warnings detected while building $Label."
    }
}

function Read-TrxCounters {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "xUnit TRX result was not created: $Path"
    }

    [xml]$trx = Get-Content -LiteralPath $Path -Raw
    $counters = $trx.SelectSingleNode(
        "/*[local-name()='TestRun']/*[local-name()='ResultSummary']/*[local-name()='Counters']")
    if ($null -eq $counters) {
        throw "xUnit TRX result does not contain counters: $Path"
    }

    return [pscustomobject]@{
        Total = [int]$counters.GetAttribute("total")
        Failed = [int]$counters.GetAttribute("failed")
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnetCliHome = Join-Path $repoRoot ".tmp_dotnet_home"
if (-not (Test-Path $dotnetCliHome)) {
    New-Item -Path $dotnetCliHome -ItemType Directory | Out-Null
}
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_HOME = $dotnetCliHome

$solutionPath = Join-Path $repoRoot "Sussudio.slnx"
$testProjectPath = Join-Path $repoRoot "tests\Sussudio.Tests\Sussudio.Tests.csproj"
$ssctlProjectPath = Join-Path $repoRoot "tools\ssctl\ssctl.csproj"
$mcpServerProjectPath = Join-Path $repoRoot "tools\McpServer\McpServer.csproj"
$automationClientProjectPath = Join-Path $repoRoot "tools\AutomationClient\AutomationClient.csproj"
$nativeXuProbeProjectPath = Join-Path $repoRoot "tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj"
$releaseHelperTestsPath = Join-Path $repoRoot "tools\release\test-release-helpers.ps1"
if (-not (Test-Path $solutionPath)) {
    throw "Solution file not found: $solutionPath"
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
if (-not (Test-Path $automationClientProjectPath)) {
    throw "AutomationClient project file not found: $automationClientProjectPath"
}
if (-not (Test-Path $nativeXuProbeProjectPath)) {
    throw "NativeXuAudioProbe project file not found: $nativeXuProbeProjectPath"
}
if (-not (Test-Path $releaseHelperTestsPath)) {
    throw "Release helper test script not found: $releaseHelperTestsPath"
}

$buildOutput = Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $solutionPath,
        "-c", $Configuration,
        "-m:1",
        "--no-restore",
        "--nologo",
        "-v", "minimal",
        "-p:Platform=$Platform"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot
Assert-BuildWarningsAllowed -Output $buildOutput -Label "Sussudio.slnx"

$ssctlBuildOutput = Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $ssctlProjectPath,
        "-c", $Configuration,
        "-t:Rebuild",
        "--no-restore",
        "--nologo",
        "-v", "minimal"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot
Assert-BuildWarningsAllowed -Output $ssctlBuildOutput -Label "ssctl"

$mcpBuildOutput = Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $mcpServerProjectPath,
        "-c", $Configuration,
        "-t:Rebuild",
        "--no-restore",
        "--nologo",
        "-v", "minimal"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot
Assert-BuildWarningsAllowed -Output $mcpBuildOutput -Label "McpServer"

$automationClientBuildOutput = Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $automationClientProjectPath,
        "-c", $Configuration,
        "-t:Rebuild",
        "--no-restore",
        "--nologo",
        "-v", "minimal"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot
Assert-BuildWarningsAllowed -Output $automationClientBuildOutput -Label "AutomationClient"

$nativeXuBuildOutput = Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $nativeXuProbeProjectPath,
        "-c", $Configuration,
        "-t:Rebuild",
        "--no-restore",
        "--nologo",
        "-v", "minimal"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot
Assert-BuildWarningsAllowed -Output $nativeXuBuildOutput -Label "NativeXuAudioProbe"

# The repository's test/tool reflection harness intentionally resolves the AnyCPU
# tool outputs under bin\<Configuration>. Build the test host explicitly in the
# same layout after the full x64 solution build so --no-build cannot select a
# stale artifact from a previous run.
$testBuildOutput = Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "build",
        $testProjectPath,
        "-c", $Configuration,
        "--no-restore",
        "--nologo",
        "-v", "minimal"
    ) `
    -TimeoutSeconds $BuildTimeoutSeconds `
    -WorkingDirectory $repoRoot
Assert-BuildWarningsAllowed -Output $testBuildOutput -Label "Sussudio.Tests"

$appAssemblyPath = Join-Path $repoRoot "Sussudio\bin\$Platform\$Configuration\net8.0-windows10.0.19041.0\win-x64\Sussudio.dll"
if (-not (Test-Path $appAssemblyPath)) {
    throw "Built app assembly not found: $appAssemblyPath"
}

$testAssemblyPath = Join-Path $repoRoot "tests\Sussudio.Tests\bin\$Configuration\net8.0\Sussudio.Tests.dll"
if (-not (Test-Path $testAssemblyPath)) {
    throw "Built offline harness assembly not found: $testAssemblyPath"
}

$testResultsDirectory = Join-Path $repoRoot "artifacts\test-results\reliability-gate"
if (Test-Path -LiteralPath $testResultsDirectory) {
    Remove-Item -LiteralPath $testResultsDirectory -Recurse -Force
}
New-Item -Path $testResultsDirectory -ItemType Directory -Force | Out-Null
$trxPath = Join-Path $testResultsDirectory "reliability-gate.trx"

Write-Host "Running the real xUnit suite..."
Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "test",
        $testProjectPath,
        "-c", $Configuration,
        "--no-build",
        "--no-restore",
        "--nologo",
        "--logger", "trx;LogFileName=reliability-gate.trx",
        "--results-directory", $testResultsDirectory,
        "-v", "minimal"
    ) `
    -TimeoutSeconds $TestTimeoutSeconds `
    -WorkingDirectory $repoRoot

$testCounters = Read-TrxCounters -Path $trxPath
if ($testCounters.Total -le 0) {
    throw "Reliability gate discovered zero xUnit tests."
}
if ($testCounters.Failed -ne 0) {
    throw "Reliability gate TRX reports $($testCounters.Failed) failed xUnit tests."
}
Write-Host "xUnit result: total=$($testCounters.Total) failed=$($testCounters.Failed)"

Write-Host "Running the separate offline assembly/freshness harness..."
Invoke-ToolWithTimeout `
    -Exe "dotnet" `
    -Arguments @(
        "exec",
        $testAssemblyPath,
        $appAssemblyPath
    ) `
    -TimeoutSeconds $TestTimeoutSeconds `
    -WorkingDirectory $repoRoot

Write-Host "Running package-contract helper tests..."
Invoke-ToolWithTimeout `
    -Exe "powershell.exe" `
    -Arguments @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $releaseHelperTestsPath
    ) `
    -TimeoutSeconds 120 `
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

Write-Host "Checking the working diff for whitespace errors..."
Invoke-ToolWithTimeout `
    -Exe "git" `
    -Arguments @("diff", "--check") `
    -TimeoutSeconds 120 `
    -WorkingDirectory $repoRoot

Write-Host ""
Write-Host "Gate result: PASS"
Write-Host "Solution/tool builds, nonzero xUnit suite, separate offline harness, package-contract checks, and diff checks passed. Optional HDR validation is controlled by the ValidateHdr parameters."
