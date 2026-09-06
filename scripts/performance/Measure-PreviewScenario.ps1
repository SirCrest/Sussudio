<#
.SYNOPSIS
Records one preview A/B observation using the existing diagnostic-session tool.
.DESCRIPTION
Configure the source, display, dock/graph visibility, and recording/Flashback state
before running. Run once for each variant with the same moving source and duration.
This script observes the running app; it does not start recording, change app
settings, restart the app, or alter environment variables.

Renderer environment settings apply when the renderer is constructed. For an
environment A/B, launch the app separately with the selected process environment,
then check it with -ExpectedWaitable, -ExpectedMaxFrameLatency, and
-ExpectedBufferCount. Restore the original launch environment after the pair.
.EXAMPLE
./scripts/performance/Measure-PreviewScenario.ps1 -Label baseline-graph-off `
    -ScenarioDescription '1080p59.94 on 60 Hz, SDR, graph/dock hidden, Flashback off' `
    -ExpectedWaitable 0 -ExpectedMaxFrameLatency 1 -ExpectedBufferCount 2 -PresentMon
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$')]
    [string] $Label,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $ScenarioDescription,

    [ValidateRange(5, 300)]
    [int] $Seconds = 30,

    [ValidateRange(0, 60)]
    [int] $WarmupSeconds = 10,

    [ValidateRange(100, 5000)]
    [int] $SampleMilliseconds = 500,

    [ValidateRange(0, 1)]
    [Nullable[int]] $ExpectedWaitable,

    [ValidateRange(1, 3)]
    [Nullable[int]] $ExpectedMaxFrameLatency,

    [ValidateRange(2, 4)]
    [Nullable[int]] $ExpectedBufferCount,

    [switch] $PresentMon,
    [string] $PresentMonPath,
    [string] $SsctlPath,
    [string] $OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ([string]::IsNullOrWhiteSpace($SsctlPath)) {
    $SsctlPath = Join-Path $repoRoot 'tools/ssctl/bin/Debug/net8.0/ssctl.exe'
}
$SsctlPath = (Resolve-Path -LiteralPath $SsctlPath).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $runName = '{0}-{1}' -f [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'), $Label
    $OutputDirectory = Join-Path $repoRoot "artifacts/preview-performance/$runName"
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "Output directory already exists: $OutputDirectory"
}
if ($PresentMonPath -and !$PresentMon) {
    throw '-PresentMonPath requires -PresentMon.'
}
if ($PresentMonPath) {
    $PresentMonPath = (Resolve-Path -LiteralPath $PresentMonPath).Path
}

function Invoke-SsctlJson {
    param([string[]] $Arguments)
    $lines = & $SsctlPath --json @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "ssctl failed with exit code $LASTEXITCODE during $($Arguments[0])."
    }
    return (($lines -join [Environment]::NewLine) | ConvertFrom-Json)
}

function Assert-ExpectedSetting {
    param($Snapshot, [string] $Field, $Expected)
    if ($null -eq $Expected) { return }
    $property = $Snapshot.PSObject.Properties[$Field]
    if ($null -eq $property -or [int] $property.Value -ne [int] $Expected) {
        $actual = if ($null -eq $property) { 'unavailable' } else { $property.Value }
        throw "Expected $Field=$Expected; actual value is '$actual'. Check the app's launch environment."
    }
}

Write-Host "Warming up '$Label' for $WarmupSeconds seconds; leave the source and UI state unchanged."
if ($WarmupSeconds -gt 0) { Start-Sleep -Seconds $WarmupSeconds }
$before = Invoke-SsctlJson -Arguments @('state')
if (!$before.Success -or !$before.Snapshot.IsPreviewing) {
    throw 'An active preview is required. Configure and start the intended scenario before collecting it.'
}
Assert-ExpectedSetting $before.Snapshot 'PreviewD3DFrameLatencyWaitEnabled' $ExpectedWaitable
Assert-ExpectedSetting $before.Snapshot 'PreviewD3DMaxFrameLatency' $ExpectedMaxFrameLatency
Assert-ExpectedSetting $before.Snapshot 'PreviewD3DSwapChainBufferCount' $ExpectedBufferCount

$null = New-Item -ItemType Directory -Path $OutputDirectory
$utf8 = [Text.UTF8Encoding]::new($false)
$manifest = [ordered]@{
    Label = $Label
    ScenarioDescription = $ScenarioDescription
    StartedUtc = [DateTimeOffset]::UtcNow.ToString('O')
    DurationSeconds = $Seconds
    WarmupSeconds = $WarmupSeconds
    SampleMilliseconds = $SampleMilliseconds
    IncludePresentMon = [bool] $PresentMon
    PresentMonPath = $PresentMonPath
    SsctlPath = $SsctlPath
    SsctlSha256 = (Get-FileHash -LiteralPath $SsctlPath -Algorithm SHA256).Hash
    ExpectedWaitable = $ExpectedWaitable
    ExpectedMaxFrameLatency = $ExpectedMaxFrameLatency
    ExpectedBufferCount = $ExpectedBufferCount
    SettingsChangedByScript = @()
    InitialSnapshot = $before.Snapshot
    Status = 'Running'
}
$manifestPath = Join-Path $OutputDirectory 'scenario.json'
[IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 12), $utf8)
$arguments = @('diagnostic-session', '--scenario', 'observe', '--seconds', "$Seconds",
    '--sample-ms', "$SampleMilliseconds", '--output', (Join-Path $OutputDirectory 'diagnostics'))
if ($PresentMon) { $arguments += '--presentmon' }
if ($PresentMonPath) { $arguments += @('--presentmon-path', $PresentMonPath) }
try {
    Write-Host "Observing '$Label' for $Seconds seconds."
    $result = Invoke-SsctlJson -Arguments $arguments
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'result.json'), ($result | ConvertTo-Json -Depth 30), $utf8)
    $manifest.Status = 'Completed'
}
catch {
    $manifest.Status = 'Failed'
    $manifest['Failure'] = $_.Exception.Message
    throw
}
finally {
    $manifest['CompletedUtc'] = [DateTimeOffset]::UtcNow.ToString('O')
    [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 12), $utf8)
}
Write-Host "Saved observation: $OutputDirectory"
Write-Host 'Compare equal-duration variants and inspect PresentMon coverage; present-call cadence alone does not establish scan-out improvement.'
