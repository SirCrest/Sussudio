param(
    [switch]$FailOnAnyWarning,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [ValidateSet("x64")]
    [string]$Platform = "x64",
    [int]$BuildTimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"

function Invoke-ToolWithTimeout {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Exe,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds
    )

    $argumentString = [string]::Join(" ", $Arguments)
    Write-Host "> $Exe $argumentString"

    $job = Start-Job -ScriptBlock {
        param($ToolExe, $ToolArgs)
        $allOutput = & $ToolExe @ToolArgs 2>&1 | ForEach-Object { $_.ToString() }
        $code = if ($null -eq $LASTEXITCODE) { 0 } else { [int]$LASTEXITCODE }
        [pscustomobject]@{
            ExitCode = $code
            Output = @($allOutput)
        }
    } -ArgumentList @($Exe, $Arguments)

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

$projectPath = Join-Path $repoRoot "ElgatoCapture\ElgatoCapture.csproj"
if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
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
    -TimeoutSeconds $BuildTimeoutSeconds

if ($buildOutput -match "MVVMTK0045") {
    throw "MVVMTK0045 warning detected in build output."
}

if ($FailOnAnyWarning -and ($buildOutput -match ": warning ")) {
    throw "Warnings detected in build output."
}

Write-Host ""
Write-Host "Gate result: PASS"
Write-Host "Automation stack reset is active. Add new tests incrementally under docs/testing/README.md."
