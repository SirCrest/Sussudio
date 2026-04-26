param(
    [Parameter(Mandatory = $true)]
    [string]$Command,

    [string]$PipeName = "ElgatoCaptureAutomation",
    [string]$AuthToken = "",
    [string]$PayloadJson = "{}",
    [int]$ConnectTimeoutMs = 5000,
    [int]$ResponseTimeoutMs = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-AutomationClientPath {
    $projectPath = Join-Path $PSScriptRoot "AutomationClient\AutomationClient.csproj"
    if (-not (Test-Path $projectPath)) {
        throw "AutomationClient project not found: $projectPath"
    }

    $buildOutput = Join-Path $PSScriptRoot "AutomationClient\bin\Debug\net8.0\AutomationClient.dll"
    if (Test-AutomationClientBuildFresh -BuildOutput $buildOutput) {
        return $buildOutput
    }

    & dotnet build $projectPath -nologo | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "AutomationClient build failed with exit code $LASTEXITCODE."
    }

    if (Test-AutomationClientBuildFresh -BuildOutput $buildOutput) {
        return $buildOutput
    }

    if (Test-Path $buildOutput) {
        throw "AutomationClient build output is stale after rebuild: $buildOutput"
    }

    throw "AutomationClient build output not found: $buildOutput"
}

function Test-AutomationClientBuildFresh {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BuildOutput
    )

    return (Test-Path $BuildOutput) -and ((Get-Item $BuildOutput).LastWriteTimeUtc -ge (Get-AutomationClientInputWriteTimeUtc))
}

function Get-AutomationClientInputWriteTimeUtc {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $inputPaths = @(
        (Join-Path $PSScriptRoot "AutomationClient"),
        (Join-Path $PSScriptRoot "Common")
    )
    $inputFiles = @()
    foreach ($inputPath in $inputPaths) {
        if (Test-Path $inputPath) {
            $inputFiles += Get-ChildItem -LiteralPath $inputPath -Recurse -File |
                Where-Object {
                    $_.Extension -in @(".cs", ".csproj", ".props", ".targets") -and
                    $_.FullName -notmatch "\\(bin|obj)\\"
                }
        }
    }

    $commandKindPath = Join-Path $repoRoot "ElgatoCapture\Models\AutomationCommandKind.cs"
    if (Test-Path $commandKindPath) {
        $inputFiles += Get-Item -LiteralPath $commandKindPath
    }

    $newest = [DateTime]::MinValue
    foreach ($inputFile in $inputFiles) {
        if ($inputFile.LastWriteTimeUtc -gt $newest) {
            $newest = $inputFile.LastWriteTimeUtc
        }
    }

    return $newest
}

if ([string]::IsNullOrWhiteSpace($PayloadJson)) {
    $PayloadJson = "{}"
}

$automationClientPath = Resolve-AutomationClientPath
$payloadBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($PayloadJson))
$arguments = @(
    $automationClientPath
    "--command", $Command
    "--pipe", $PipeName
    "--connect-timeout-ms", $ConnectTimeoutMs
    "--payload-base64", $payloadBase64
)

if (-not [string]::IsNullOrWhiteSpace($AuthToken)) {
    $arguments += @("--token", $AuthToken)
}

if ($ResponseTimeoutMs -gt 0) {
    $arguments += @("--response-timeout-ms", $ResponseTimeoutMs)
}

& dotnet @arguments
