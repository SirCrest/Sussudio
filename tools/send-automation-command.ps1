param(
    [Parameter(Mandatory = $true)]
    [string]$Command,

    [string]$PipeName = "ElgatoCaptureAutomation",
    [string]$AuthToken = "",
    [string]$PayloadJson = "{}",
    [int]$ConnectTimeoutMs = 5000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-AutomationClientPath {
    $projectPath = Join-Path $PSScriptRoot "AutomationClient\AutomationClient.csproj"
    if (-not (Test-Path $projectPath)) {
        throw "AutomationClient project not found: $projectPath"
    }

    $buildOutput = Join-Path $PSScriptRoot "AutomationClient\bin\Debug\net8.0\AutomationClient.dll"
    if (Test-Path $buildOutput) {
        return $buildOutput
    }

    & dotnet build $projectPath -nologo | Out-Null
    if (Test-Path $buildOutput) {
        return $buildOutput
    }

    throw "AutomationClient build output not found: $buildOutput"
}

if ([string]::IsNullOrWhiteSpace($PayloadJson)) {
    $PayloadJson = "{}"
}

$automationClientPath = Resolve-AutomationClientPath
$arguments = @(
    $automationClientPath
    "--command", $Command
    "--pipe", $PipeName
    "--connect-timeout-ms", $ConnectTimeoutMs
    "--payload", $PayloadJson
)

if (-not [string]::IsNullOrWhiteSpace($AuthToken)) {
    $arguments += @("--token", $AuthToken)
}

& dotnet @arguments
