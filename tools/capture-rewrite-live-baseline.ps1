param(
    [string]$PipeName = "ElgatoCaptureAutomation",
    [string]$AuthToken = $env:ELGATOCAPTURE_AUTOMATION_TOKEN,
    [int]$PreviewSeconds = 60,
    [int]$FlashbackSeconds = 300,
    [int]$RecordingSeconds = 60,
    [int]$PresentMonSeconds = 30,
    [int]$WaitTimeoutMs = 60000,
    [int]$WaitPollMs = 500,
    [int]$SnapshotIntervalMs = 1000,
    [string]$OutputRoot = "",
    [string]$PresentMonPath = "",
    [switch]$NoLaunch,
    [switch]$NoClose,
    [switch]$SkipPresentMon,
    [switch]$SkipPreviewOnly,
    [switch]$SkipFlashbackRetention,
    [switch]$SkipFlashbackRecording,
    [switch]$SkipDedicatedLibAv,
    [switch]$AppendImplementationLog,
    [switch]$ValidateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:Root = Split-Path -Parent $PSScriptRoot
$script:SendCommandPath = Join-Path $PSScriptRoot "send-automation-command.ps1"
$script:VerifyDedicatedLibAvPath = Join-Path $PSScriptRoot "verify-dedicated-libav-recording.ps1"
$script:EcCtlProjectPath = Join-Path $PSScriptRoot "ecctl\ecctl.csproj"
$script:EcCtlPath = Join-Path $PSScriptRoot "ecctl\bin\Debug\net8.0\ecctl.dll"
$script:AppExePath = Join-Path $script:Root "ElgatoCapture\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\ElgatoCapture.exe"
$script:ImplementationLogPath = Join-Path $script:Root "docs\realtime-capture-engine-implementation-log.md"
$script:LaunchedApp = $false
$script:InitialSnapshot = $null

function Assert-PositiveInt {
    param(
        [int]$Value,
        [string]$Name
    )

    if ($Value -lt 1) {
        throw "$Name must be at least 1."
    }
}

function Get-IsoTimestamp {
    return (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", [Globalization.CultureInfo]::InvariantCulture)
}

function Get-PropertyValue {
    param(
        [AllowNull()]
        [object]$Object,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [AllowNull()]
        [object]$Fallback = $null
    )

    if ($null -eq $Object) {
        return $Fallback
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Fallback
    }

    return $property.Value
}

function Convert-ToBool {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return $false
    }

    if ($Value -is [bool]) {
        return [bool]$Value
    }

    return [bool]::Parse([string]$Value)
}

function Convert-ToInt64 {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return 0L
    }

    return [long]$Value
}

function New-RunDirectory {
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        $base = Join-Path $script:Root "temp\capture-rewrite-baselines"
    }
    else {
        $base = $OutputRoot
    }

    $runId = Get-Date -Format "yyyyMMdd_HHmmss"
    $runDir = Join-Path $base $runId
    New-Item -ItemType Directory -Force -Path $runDir | Out-Null
    return $runDir
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [AllowNull()]
        [object]$Value,
        [int]$Depth = 80
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $Value | ConvertTo-Json -Depth $Depth | Set-Content -Path $Path -Encoding UTF8
}

function Invoke-Automation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [hashtable]$Payload = @{},
        [int]$ConnectTimeoutMs = 5000,
        [int]$ResponseTimeoutMs = 0,
        [switch]$AllowFailure
    )

    $payloadJson = "{}"
    if ($Payload.Count -gt 0) {
        $payloadJson = $Payload | ConvertTo-Json -Depth 30 -Compress
    }

    $raw = & $script:SendCommandPath `
        -Command $Command `
        -PipeName $PipeName `
        -AuthToken $AuthToken `
        -ConnectTimeoutMs $ConnectTimeoutMs `
        -ResponseTimeoutMs $ResponseTimeoutMs `
        -PayloadJson $payloadJson

    $exitCode = $LASTEXITCODE
    $rawText = ($raw | Out-String).Trim()
    $response = $null
    if (-not [string]::IsNullOrWhiteSpace($rawText)) {
        try {
            $response = $rawText | ConvertFrom-Json
        }
        catch {
            if (-not $AllowFailure) {
                throw
            }
        }
    }

    if ($exitCode -ne 0) {
        if ($AllowFailure) {
            return $response
        }

        if ($null -ne $response) {
            $message = Get-PropertyValue $response "Message" "unknown error"
            $errorCode = Get-PropertyValue $response "ErrorCode" ""
            throw "Automation command '$Command' failed: $message [ErrorCode=$errorCode ExitCode=$exitCode]"
        }

        throw "Automation command '$Command' transport failed with exit code $exitCode."
    }

    if ($null -eq $response) {
        if ($AllowFailure) {
            return $null
        }

        throw "Automation command '$Command' returned no JSON response."
    }

    if (-not $AllowFailure -and -not (Convert-ToBool (Get-PropertyValue $response "Success" $false))) {
        $message = Get-PropertyValue $response "Message" "unknown error"
        $errorCode = Get-PropertyValue $response "ErrorCode" ""
        throw "Automation command '$Command' failed: $message [ErrorCode=$errorCode]"
    }

    return $response
}

function Get-Snapshot {
    $response = Invoke-Automation -Command "GetSnapshot"
    $snapshot = Get-PropertyValue $response "Snapshot" $null
    if ($null -eq $snapshot) {
        throw "GetSnapshot returned no snapshot payload."
    }

    return $snapshot
}

function Wait-Condition {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConditionName,
        [int]$TimeoutMs = $WaitTimeoutMs
    )

    $responseTimeoutMs = [Math]::Max($TimeoutMs + 5000, 60000)
    $response = Invoke-Automation -Command "WaitForCondition" -Payload @{
        condition = $ConditionName
        timeoutMs = $TimeoutMs
        pollMs = $WaitPollMs
    } -ResponseTimeoutMs $responseTimeoutMs -AllowFailure

    if ($null -eq $response -or -not (Convert-ToBool (Get-PropertyValue $response "Success" $false))) {
        $message = Get-PropertyValue $response "Message" "timeout"
        throw "Timed out waiting for condition '$ConditionName': $message"
    }
}

function Wait-FlashbackState {
    param(
        [bool]$ExpectedActive,
        [int]$TimeoutMs = $WaitTimeoutMs
    )

    $started = Get-Date
    while (((Get-Date) - $started).TotalMilliseconds -lt $TimeoutMs) {
        $snapshot = Get-Snapshot
        if ((Convert-ToBool (Get-PropertyValue $snapshot "FlashbackActive" $false)) -eq $ExpectedActive) {
            return $snapshot
        }

        Start-Sleep -Milliseconds $WaitPollMs
    }

    throw "Timed out waiting for FlashbackActive=$ExpectedActive."
}

function Wait-AutomationAvailable {
    param(
        [int]$TimeoutMs = $WaitTimeoutMs
    )

    $started = Get-Date
    while (((Get-Date) - $started).TotalMilliseconds -lt $TimeoutMs) {
        try {
            $response = Invoke-Automation -Command "GetSnapshot" -ConnectTimeoutMs 1000 -AllowFailure
            if ($null -ne $response -and (Convert-ToBool (Get-PropertyValue $response "Success" $false))) {
                return $true
            }
        }
        catch {
        }

        Start-Sleep -Milliseconds $WaitPollMs
    }

    return $false
}

function Start-AppIfNeeded {
    $running = Get-Process -Name "ElgatoCapture" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $running) {
        return [pscustomobject]@{
            Started = $false
            ProcessId = $running.Id
            Path = $running.Path
        }
    }

    if ($NoLaunch) {
        throw "ElgatoCapture is not running. Re-run without -NoLaunch or start the app before running the harness."
    }

    if (-not (Test-Path $script:AppExePath)) {
        throw "App executable not found. Build ElgatoCapture first: $script:AppExePath"
    }

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $script:AppExePath
    $psi.WorkingDirectory = Split-Path -Parent $script:AppExePath
    $psi.UseShellExecute = $false
    if (-not [string]::IsNullOrWhiteSpace($AuthToken)) {
        $psi.Environment["ELGATOCAPTURE_AUTOMATION_TOKEN"] = $AuthToken
    }

    $process = [System.Diagnostics.Process]::Start($psi)
    $script:LaunchedApp = $true

    if (-not (Wait-AutomationAvailable -TimeoutMs $WaitTimeoutMs)) {
        throw "Timed out waiting for automation after launching ElgatoCapture."
    }

    return [pscustomobject]@{
        Started = $true
        ProcessId = $process.Id
        Path = $script:AppExePath
    }
}

function Ensure-EcCtl {
    if (Test-Path $script:EcCtlPath) {
        return
    }

    if (-not (Test-Path $script:EcCtlProjectPath)) {
        throw "ecctl project not found: $script:EcCtlProjectPath"
    }

    & dotnet build $script:EcCtlProjectPath -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "ecctl build failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path $script:EcCtlPath)) {
        throw "ecctl build output not found: $script:EcCtlPath"
    }
}

function Start-ToolProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$StdoutPath,
        [Parameter(Mandatory = $true)]
        [string]$StderrPath
    )

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $StdoutPath) | Out-Null
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $StderrPath) | Out-Null

    return Start-Process `
        -FilePath $FilePath `
        -ArgumentList $Arguments `
        -WorkingDirectory $script:Root `
        -RedirectStandardOutput $StdoutPath `
        -RedirectStandardError $StderrPath `
        -WindowStyle Hidden `
        -PassThru
}

function Wait-ToolProcessExitCode {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process
    )

    $Process.WaitForExit()
    $Process.Refresh()
    if ($Process.HasExited) {
        return $Process.ExitCode
    }

    return $null
}

function Start-PresentMonCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScenarioDir,
        [AllowNull()]
        [object]$Snapshot
    )

    if ($SkipPresentMon) {
        return $null
    }

    Ensure-EcCtl
    $seconds = [Math]::Max([Math]::Min($PresentMonSeconds, $PreviewSeconds), 1)
    $stdoutPath = Join-Path $ScenarioDir "presentmon.stdout.json"
    $stderrPath = Join-Path $ScenarioDir "presentmon.stderr.txt"
    $csvPath = Join-Path $ScenarioDir "presentmon.csv"
    $swapChain = [string](Get-PropertyValue $Snapshot "PreviewD3DSwapChainAddress" "")
    $arguments = @(
        $script:EcCtlPath,
        "--json",
        "presentmon",
        "--seconds", ([string]$seconds),
        "--process", "ElgatoCapture",
        "--output", $csvPath,
        "--keep-csv"
    )

    if (-not [string]::IsNullOrWhiteSpace($swapChain)) {
        $arguments += @("--swapchain", $swapChain)
    }

    if (-not [string]::IsNullOrWhiteSpace($PresentMonPath)) {
        $arguments += @("--presentmon", $PresentMonPath)
    }

    $process = Start-ToolProcess -FilePath "dotnet" -Arguments $arguments -StdoutPath $stdoutPath -StderrPath $stderrPath
    return [pscustomobject]@{
        Process = $process
        StdoutPath = $stdoutPath
        StderrPath = $stderrPath
        CsvPath = $csvPath
        ExpectedSwapChainAddress = $swapChain
        StartedAt = Get-IsoTimestamp
    }
}

function Complete-PresentMonCapture {
    param(
        [AllowNull()]
        [object]$Capture
    )

    if ($null -eq $Capture) {
        return [pscustomobject]@{
            Skipped = $true
            Reason = "SkipPresentMon"
        }
    }

    $process = $Capture.Process
    $exitCode = Wait-ToolProcessExitCode -Process $process
    $stdoutText = ""
    $stderrText = ""
    if (Test-Path $Capture.StdoutPath) {
        $rawStdout = Get-Content -Raw $Capture.StdoutPath
        if ($null -ne $rawStdout) {
            $stdoutText = $rawStdout.Trim()
        }
    }

    if (Test-Path $Capture.StderrPath) {
        $rawStderr = Get-Content -Raw $Capture.StderrPath
        if ($null -ne $rawStderr) {
            $stderrText = $rawStderr.Trim()
        }
    }

    $parsed = $null
    if (-not [string]::IsNullOrWhiteSpace($stdoutText)) {
        try {
            $parsed = $stdoutText | ConvertFrom-Json
        }
        catch {
        }
    }

    if ($null -eq $exitCode -and $null -ne $parsed) {
        $exitCode = Get-PropertyValue $parsed "ExitCode" $null
    }

    return [pscustomobject]@{
        Skipped = $false
        ExitCode = $exitCode
        Success = ($exitCode -eq 0 -or (Convert-ToBool (Get-PropertyValue $parsed "Success" $false)))
        StdoutPath = $Capture.StdoutPath
        StderrPath = $Capture.StderrPath
        CsvPath = $Capture.CsvPath
        ExpectedSwapChainAddress = $Capture.ExpectedSwapChainAddress
        Parsed = $parsed
        ErrorText = $stderrText
    }
}

function Normalize-SwapChainAddress {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return ""
    }

    $text = ([string]$Value).Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return ""
    }

    if ($text.StartsWith("0x", [StringComparison]::OrdinalIgnoreCase)) {
        return "0x" + $text.Substring(2).TrimStart("0").PadLeft(1, "0").ToUpperInvariant()
    }

    return "0x" + $text.TrimStart("0").PadLeft(1, "0").ToUpperInvariant()
}

function Convert-ToNullableDouble {
    param(
        [AllowNull()]
        [object]$Value
    )

    if ($null -eq $Value) {
        return $null
    }

    $text = ([string]$Value).Trim()
    if ([string]::IsNullOrWhiteSpace($text) -or
        [string]::Equals($text, "NA", [StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }

    $parsed = 0.0
    if ([double]::TryParse($text, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
        return $parsed
    }

    return $null
}

function Get-PresentMonOutcome {
    param(
        [AllowNull()]
        [object]$DisplayedTime,
        [AllowNull()]
        [object]$UntilDisplayed
    )

    $displayed = Convert-ToNullableDouble $DisplayedTime
    if ($null -eq $displayed) {
        return "SupersededOrNotDisplayed"
    }

    $until = Convert-ToNullableDouble $UntilDisplayed
    if ($null -ne $until -and $until -ge 16.0) {
        return "DisplayedLate"
    }

    return "Displayed"
}

function Add-PresentMonSnapshotCorrelation {
    param(
        [AllowNull()]
        [object]$Capture,
        [AllowNull()]
        [object]$Summary,
        [AllowNull()]
        [string]$SnapshotSamplePath
    )

    if ($null -eq $Summary -or (Convert-ToBool (Get-PropertyValue $Summary "Skipped" $false))) {
        return $Summary
    }

    if ([string]::IsNullOrWhiteSpace($SnapshotSamplePath) -or
        -not (Test-Path $SnapshotSamplePath) -or
        $null -eq $Capture -or
        [string]::IsNullOrWhiteSpace($Capture.CsvPath) -or
        -not (Test-Path $Capture.CsvPath)) {
        $Summary | Add-Member -NotePropertyName "Correlation" -Force -NotePropertyValue ([pscustomobject]@{
            Status = "Unavailable"
            Reason = "Missing snapshot samples or PresentMon CSV."
        })
        return $Summary
    }

    $expectedSwapChain = Normalize-SwapChainAddress $Capture.ExpectedSwapChainAddress
    $parsed = Get-PropertyValue $Summary "Parsed" $null
    if ([string]::IsNullOrWhiteSpace($expectedSwapChain) -and $null -ne $parsed -and $null -ne $parsed.Summary) {
        $expectedSwapChain = Normalize-SwapChainAddress (Get-PropertyValue $parsed.Summary "SelectedSwapChainAddress" "")
    }

    if ([string]::IsNullOrWhiteSpace($expectedSwapChain)) {
        $Summary | Add-Member -NotePropertyName "Correlation" -Force -NotePropertyValue ([pscustomobject]@{
            Status = "Unavailable"
            Reason = "No expected or selected swap-chain address."
        })
        return $Summary
    }

    $rows = Import-Csv -Path $Capture.CsvPath
    $selectedRows = @($rows | Where-Object { (Normalize-SwapChainAddress $_.SwapChainAddress) -eq $expectedSwapChain })
    if ($selectedRows.Count -eq 0) {
        $Summary | Add-Member -NotePropertyName "Correlation" -Force -NotePropertyValue ([pscustomobject]@{
            Status = "SwapChainMismatch"
            ExpectedSwapChainAddress = $expectedSwapChain
            Reason = "No PresentMon row matched the app swap-chain address."
        })
        return $Summary
    }

    $sampleData = Get-Content -Raw $SnapshotSamplePath | ConvertFrom-Json
    $samples = @($sampleData)
    if ($samples.Count -eq 1 -and $samples[0] -is [Array]) {
        $samples = @($samples[0])
    }
    $captureStart = [DateTimeOffset]::Parse(
        [string]$Capture.StartedAt,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal)
    $captureStartUnixMs = $captureStart.ToUnixTimeMilliseconds()
    $seenPresents = New-Object "System.Collections.Generic.HashSet[Int64]"
    $correlations = New-Object "System.Collections.Generic.List[object]"

    foreach ($sample in $samples) {
        $snapshot = $sample.Snapshot
        if ($null -eq $snapshot) {
            continue
        }

        $appPresentId = Convert-ToInt64 (Get-PropertyValue $snapshot "PreviewD3DLastRenderedPreviewPresentId" 0)
        $appPresentUtcMs = Convert-ToInt64 (Get-PropertyValue $snapshot "PreviewD3DLastRenderedUtcUnixMs" 0)
        if ($appPresentId -le 0 -or $appPresentUtcMs -le 0) {
            continue
        }

        if (-not $seenPresents.Add($appPresentId)) {
            continue
        }

        $appOffsetMs = [double]($appPresentUtcMs - $captureStartUnixMs)
        $bestRow = $null
        $bestDelta = [double]::PositiveInfinity
        $bestCpuStart = $null
        foreach ($row in $selectedRows) {
            $cpuStart = Convert-ToNullableDouble (Get-PropertyValue $row "CPUStartTime" $null)
            if ($null -eq $cpuStart) {
                continue
            }

            $delta = [Math]::Abs([double]$cpuStart - $appOffsetMs)
            if ($delta -lt $bestDelta) {
                $bestDelta = $delta
                $bestRow = $row
                $bestCpuStart = [double]$cpuStart
            }
        }

        if ($null -eq $bestRow) {
            continue
        }

        if ($bestDelta -gt 50.0) {
            continue
        }

        $correlations.Add([pscustomobject]@{
            AppPresentId = $appPresentId
            AppSourceSequenceNumber = Convert-ToInt64 (Get-PropertyValue $snapshot "PreviewD3DLastRenderedSourceSequenceNumber" -1)
            AppPresentUtcUnixMs = $appPresentUtcMs
            AppOffsetMs = [Math]::Round($appOffsetMs, 3)
            PresentMonCpuStartTimeMs = [Math]::Round($bestCpuStart, 3)
            DeltaMs = [Math]::Round($bestDelta, 3)
            Outcome = Get-PresentMonOutcome -DisplayedTime (Get-PropertyValue $bestRow "DisplayedTime" $null) -UntilDisplayed (Get-PropertyValue $bestRow "MsUntilDisplayed" $null)
            PresentMode = [string](Get-PropertyValue $bestRow "PresentMode" "")
            UntilDisplayedMs = Convert-ToNullableDouble (Get-PropertyValue $bestRow "MsUntilDisplayed" $null)
            DisplayLatencyMs = Convert-ToNullableDouble (Get-PropertyValue $bestRow "DisplayLatency" $null)
        })
    }

    $ordered = @($correlations | Sort-Object DeltaMs)
    $best = if ($ordered.Count -gt 0) { $ordered[0] } else { $null }
    $Summary | Add-Member -NotePropertyName "Correlation" -Force -NotePropertyValue ([pscustomobject]@{
        Status = if ($ordered.Count -gt 0) { "Available" } else { "Unavailable" }
        Reason = if ($ordered.Count -gt 0) { "Matched app render presents to nearest exact-swap-chain PresentMon CPUStartTime rows." } else { "No app render presents in snapshot samples had matching PresentMon CPUStartTime rows." }
        ExpectedSwapChainAddress = $expectedSwapChain
        CaptureStartUtcUnixMs = $captureStartUnixMs
        CorrelatedPresentCount = $ordered.Count
        Best = $best
        Recent = @($ordered | Select-Object -First 12)
    })

    return $Summary
}

function Save-Snapshot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScenarioDir,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $snapshot = Get-Snapshot
    Write-JsonFile -Path (Join-Path $ScenarioDir "$Name.snapshot.json") -Value $snapshot
    return $snapshot
}

function Collect-SnapshotSamples {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScenarioDir,
        [Parameter(Mandatory = $true)]
        [string]$ScenarioName,
        [int]$Seconds
    )

    $samples = [System.Collections.Generic.List[object]]::new()
    $started = Get-Date
    while (((Get-Date) - $started).TotalSeconds -lt $Seconds) {
        $snapshot = Get-Snapshot
        $samples.Add([pscustomobject]@{
            CapturedAt = Get-IsoTimestamp
            Scenario = $ScenarioName
            Snapshot = $snapshot
        })
        Start-Sleep -Milliseconds $SnapshotIntervalMs
    }

    $path = Join-Path $ScenarioDir "snapshot-samples.json"
    Write-JsonFile -Path $path -Value $samples
    return [pscustomobject]@{
        Count = $samples.Count
        Path = $path
        LastSnapshot = $(if ($samples.Count -gt 0) { $samples[$samples.Count - 1].Snapshot } else { $null })
    }
}

function New-SnapshotSummary {
    param(
        [AllowNull()]
        [object]$Snapshot
    )

    return [ordered]@{
        SessionState = Get-PropertyValue $Snapshot "SessionState" ""
        IsPreviewing = Get-PropertyValue $Snapshot "IsPreviewing" $null
        IsRecording = Get-PropertyValue $Snapshot "IsRecording" $null
        RecordingBackend = Get-PropertyValue $Snapshot "RecordingBackend" ""
        FlashbackActive = Get-PropertyValue $Snapshot "FlashbackActive" $null
        NegotiatedWidth = Get-PropertyValue $Snapshot "NegotiatedWidth" $null
        NegotiatedHeight = Get-PropertyValue $Snapshot "NegotiatedHeight" $null
        NegotiatedFrameRate = Get-PropertyValue $Snapshot "NegotiatedFrameRate" $null
        IngestVideoFramesArrived = Get-PropertyValue $Snapshot "IngestVideoFramesArrived" $null
        VideoFramesEnqueued = Get-PropertyValue $Snapshot "VideoFramesEnqueued" $null
        EncoderVideoFramesEncoded = Get-PropertyValue $Snapshot "EncoderVideoFramesEncoded" $null
        VideoDropsQueueSaturated = Get-PropertyValue $Snapshot "VideoDropsQueueSaturated" $null
        CaptureCadenceEstimatedDroppedFrames = Get-PropertyValue $Snapshot "CaptureCadenceEstimatedDroppedFrames" $null
        PreviewFramesDisplayed = Get-PropertyValue $Snapshot "PreviewFramesDisplayed" $null
        PreviewD3DSwapChainAddress = Get-PropertyValue $Snapshot "PreviewD3DSwapChainAddress" ""
        RecordingEncodingFailed = Get-PropertyValue $Snapshot "RecordingEncodingFailed" $null
        RecordingEncodingFailureMessage = Get-PropertyValue $Snapshot "RecordingEncodingFailureMessage" ""
        RecordingIntegrityStatus = Get-PropertyValue $Snapshot "RecordingIntegrityStatus" ""
        RecordingIntegrityComplete = Get-PropertyValue $Snapshot "RecordingIntegrityComplete" $null
        RecordingIntegrityBackend = Get-PropertyValue $Snapshot "RecordingIntegrityBackend" ""
        RecordingIntegritySourceFrames = Get-PropertyValue $Snapshot "RecordingIntegritySourceFrames" $null
        RecordingIntegrityAcceptedFrames = Get-PropertyValue $Snapshot "RecordingIntegrityAcceptedFrames" $null
        RecordingIntegrityPipelineDroppedFrames = Get-PropertyValue $Snapshot "RecordingIntegrityPipelineDroppedFrames" $null
        RecordingIntegrityQueueDroppedFrames = Get-PropertyValue $Snapshot "RecordingIntegrityQueueDroppedFrames" $null
        RecordingIntegritySubmittedFrames = Get-PropertyValue $Snapshot "RecordingIntegritySubmittedFrames" $null
        RecordingIntegrityEncodedFrames = Get-PropertyValue $Snapshot "RecordingIntegrityEncodedFrames" $null
        RecordingIntegrityPacketsWritten = Get-PropertyValue $Snapshot "RecordingIntegrityPacketsWritten" $null
        RecordingIntegrityEncoderDroppedFrames = Get-PropertyValue $Snapshot "RecordingIntegrityEncoderDroppedFrames" $null
        RecordingIntegritySequenceGaps = Get-PropertyValue $Snapshot "RecordingIntegritySequenceGaps" $null
        RecordingIntegrityQueueMaxDepth = Get-PropertyValue $Snapshot "RecordingIntegrityQueueMaxDepth" $null
        RecordingIntegrityQueueOldestFrameAgeMs = Get-PropertyValue $Snapshot "RecordingIntegrityQueueOldestFrameAgeMs" $null
        RecordingIntegrityBackpressureWaitMs = Get-PropertyValue $Snapshot "RecordingIntegrityBackpressureWaitMs" $null
        RecordingIntegrityBackpressureEvents = Get-PropertyValue $Snapshot "RecordingIntegrityBackpressureEvents" $null
        RecordingIntegrityBackpressureMaxWaitMs = Get-PropertyValue $Snapshot "RecordingIntegrityBackpressureMaxWaitMs" $null
        RecordingIntegrityAudioStatus = Get-PropertyValue $Snapshot "RecordingIntegrityAudioStatus" ""
        RecordingIntegrityAudioEnabled = Get-PropertyValue $Snapshot "RecordingIntegrityAudioEnabled" $null
        RecordingIntegrityAudioCaptureActive = Get-PropertyValue $Snapshot "RecordingIntegrityAudioCaptureActive" $null
        RecordingIntegrityAudioFramesArrived = Get-PropertyValue $Snapshot "RecordingIntegrityAudioFramesArrived" $null
        RecordingIntegrityAudioFramesWrittenToSink = Get-PropertyValue $Snapshot "RecordingIntegrityAudioFramesWrittenToSink" $null
        RecordingIntegrityAudioSamplesEncoded = Get-PropertyValue $Snapshot "RecordingIntegrityAudioSamplesEncoded" $null
        RecordingIntegrityAudioDropEvents = Get-PropertyValue $Snapshot "RecordingIntegrityAudioDropEvents" $null
        RecordingIntegrityAudioDiscontinuities = Get-PropertyValue $Snapshot "RecordingIntegrityAudioDiscontinuities" $null
        RecordingIntegrityAudioTimestampErrors = Get-PropertyValue $Snapshot "RecordingIntegrityAudioTimestampErrors" $null
        RecordingIntegrityAudioCallbackGaps = Get-PropertyValue $Snapshot "RecordingIntegrityAudioCallbackGaps" $null
        RecordingIntegrityAvSyncDriftMs = Get-PropertyValue $Snapshot "RecordingIntegrityAvSyncDriftMs" $null
        RecordingIntegrityEncoderAvSyncDriftMs = Get-PropertyValue $Snapshot "RecordingIntegrityEncoderAvSyncDriftMs" $null
        RecordingIntegrityReason = Get-PropertyValue $Snapshot "RecordingIntegrityReason" ""
        LastFinalizeStatus = Get-PropertyValue $Snapshot "LastFinalizeStatus" ""
        MuxResult = Get-PropertyValue $Snapshot "MuxResult" ""
    }
}

function Invoke-PreviewScenario {
    param([string]$RunDir)

    $scenarioDir = Join-Path $RunDir "preview-only"
    New-Item -ItemType Directory -Force -Path $scenarioDir | Out-Null
    Invoke-Automation -Command "SetFlashbackEnabled" -Payload @{ enabled = $false } | Out-Null
    Wait-FlashbackState -ExpectedActive $false | Out-Null
    Invoke-Automation -Command "SetPreviewEnabled" -Payload @{ enabled = $true } | Out-Null
    Wait-Condition -ConditionName "VideoFramesFlowing"
    Wait-Condition -ConditionName "PreviewRendererHealthy"

    $before = Save-Snapshot -ScenarioDir $scenarioDir -Name "before"
    $presentMon = Start-PresentMonCapture -ScenarioDir $scenarioDir -Snapshot $before
    $samples = Collect-SnapshotSamples -ScenarioDir $scenarioDir -ScenarioName "preview-only" -Seconds $PreviewSeconds
    $presentMonSummary = Complete-PresentMonCapture -Capture $presentMon
    $presentMonSummary = Add-PresentMonSnapshotCorrelation -Capture $presentMon -Summary $presentMonSummary -SnapshotSamplePath $samples.Path
    $after = Save-Snapshot -ScenarioDir $scenarioDir -Name "after"

    return [pscustomobject]@{
        Name = "preview-only"
        StartedAt = Get-IsoTimestamp
        DurationSeconds = $PreviewSeconds
        SnapshotSamples = $samples.Count
        SnapshotSamplePath = $samples.Path
        Before = New-SnapshotSummary -Snapshot $before
        After = New-SnapshotSummary -Snapshot $after
        PresentMon = $presentMonSummary
    }
}

function Invoke-FlashbackRetentionScenario {
    param([string]$RunDir)

    $scenarioDir = Join-Path $RunDir "flashback-retention"
    New-Item -ItemType Directory -Force -Path $scenarioDir | Out-Null
    Invoke-Automation -Command "SetPreviewEnabled" -Payload @{ enabled = $true } | Out-Null
    Wait-Condition -ConditionName "VideoFramesFlowing"
    Invoke-Automation -Command "SetFlashbackEnabled" -Payload @{ enabled = $true } | Out-Null
    $active = Wait-FlashbackState -ExpectedActive $true
    Write-JsonFile -Path (Join-Path $scenarioDir "active.snapshot.json") -Value $active

    $samples = Collect-SnapshotSamples -ScenarioDir $scenarioDir -ScenarioName "flashback-retention" -Seconds $FlashbackSeconds
    $segmentsResponse = Invoke-Automation -Command "FlashbackGetSegments"
    Write-JsonFile -Path (Join-Path $scenarioDir "segments.response.json") -Value $segmentsResponse
    $after = Save-Snapshot -ScenarioDir $scenarioDir -Name "after"

    return [pscustomobject]@{
        Name = "flashback-retention"
        StartedAt = Get-IsoTimestamp
        DurationSeconds = $FlashbackSeconds
        SnapshotSamples = $samples.Count
        SnapshotSamplePath = $samples.Path
        Before = New-SnapshotSummary -Snapshot $active
        After = New-SnapshotSummary -Snapshot $after
        SegmentsPath = Join-Path $scenarioDir "segments.response.json"
    }
}

function Invoke-FlashbackRecordingScenario {
    param([string]$RunDir)

    $scenarioDir = Join-Path $RunDir "flashback-recording"
    New-Item -ItemType Directory -Force -Path $scenarioDir | Out-Null
    Invoke-Automation -Command "SetPreviewEnabled" -Payload @{ enabled = $true } | Out-Null
    Wait-Condition -ConditionName "VideoFramesFlowing"
    Invoke-Automation -Command "SetFlashbackEnabled" -Payload @{ enabled = $true } | Out-Null
    Wait-FlashbackState -ExpectedActive $true | Out-Null

    Invoke-Automation -Command "SetRecordingEnabled" -Payload @{ enabled = $true } | Out-Null
    Wait-Condition -ConditionName "RecordingFileGrowing"
    $recordingStart = Save-Snapshot -ScenarioDir $scenarioDir -Name "recording-start"
    $samples = Collect-SnapshotSamples -ScenarioDir $scenarioDir -ScenarioName "flashback-recording" -Seconds $RecordingSeconds

    Invoke-Automation -Command "SetRecordingEnabled" -Payload @{ enabled = $false } | Out-Null
    Wait-Condition -ConditionName "RecordingStopped" -TimeoutMs ([Math]::Max($WaitTimeoutMs, 150000))
    $verifyResponse = Invoke-Automation -Command "VerifyLastRecording" -ResponseTimeoutMs 60000
    Write-JsonFile -Path (Join-Path $scenarioDir "verify.response.json") -Value $verifyResponse
    $after = Save-Snapshot -ScenarioDir $scenarioDir -Name "after"

    return [pscustomobject]@{
        Name = "flashback-recording"
        StartedAt = Get-IsoTimestamp
        DurationSeconds = $RecordingSeconds
        SnapshotSamples = $samples.Count
        SnapshotSamplePath = $samples.Path
        RecordingStart = New-SnapshotSummary -Snapshot $recordingStart
        After = New-SnapshotSummary -Snapshot $after
        VerifyResponsePath = Join-Path $scenarioDir "verify.response.json"
        VerifySucceeded = Get-PropertyValue $verifyResponse "Success" $null
    }
}

function Invoke-DedicatedLibAvScenario {
    param([string]$RunDir)

    if (-not (Test-Path $script:VerifyDedicatedLibAvPath)) {
        throw "Dedicated LibAv verifier not found: $script:VerifyDedicatedLibAvPath"
    }

    $scenarioDir = Join-Path $RunDir "dedicated-libav-recording"
    $outputDir = Join-Path $scenarioDir "recordings"
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    $stdoutPath = Join-Path $scenarioDir "verify.stdout.txt"
    $stderrPath = Join-Path $scenarioDir "verify.stderr.txt"

    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $script:VerifyDedicatedLibAvPath,
        "-PipeName", $PipeName,
        "-DurationSeconds", ([string]$RecordingSeconds),
        "-OutputDirectory", $outputDir,
        "-WaitTimeoutMs", ([string][Math]::Max($WaitTimeoutMs, 150000))
    )

    if (-not [string]::IsNullOrWhiteSpace($AuthToken)) {
        $arguments += @("-AuthToken", $AuthToken)
    }

    $process = Start-ToolProcess -FilePath "powershell" -Arguments $arguments -StdoutPath $stdoutPath -StderrPath $stderrPath
    $exitCode = Wait-ToolProcessExitCode -Process $process
    $stdoutText = ""
    if (Test-Path $stdoutPath) {
        $rawStdout = Get-Content -Raw $stdoutPath
        if ($null -ne $rawStdout) {
            $stdoutText = $rawStdout.Trim()
        }
    }

    $parsed = $null
    $jsonStart = $stdoutText.IndexOf("{")
    if ($jsonStart -ge 0) {
        $jsonText = $stdoutText.Substring($jsonStart).Trim()
        try {
            $parsed = $jsonText | ConvertFrom-Json
        }
        catch {
            $parsed = $null
        }
    }

    $resultText = ""
    if ($null -ne $parsed) {
        $resultText = [string](Get-PropertyValue $parsed "Result" "")
    }

    if ($null -eq $exitCode -and [string]::Equals($resultText, "PASS", [StringComparison]::OrdinalIgnoreCase)) {
        $exitCode = 0
    }

    return [pscustomobject]@{
        Name = "dedicated-libav-recording"
        StartedAt = Get-IsoTimestamp
        DurationSeconds = $RecordingSeconds
        ExitCode = $exitCode
        Success = ($exitCode -eq 0 -or [string]::Equals($resultText, "PASS", [StringComparison]::OrdinalIgnoreCase))
        StdoutPath = $stdoutPath
        StderrPath = $stderrPath
        OutputDirectory = $outputDir
    }
}

function Restore-AppState {
    if ($null -eq $script:InitialSnapshot) {
        return
    }

    try {
        $current = Get-Snapshot
        if (Convert-ToBool (Get-PropertyValue $current "IsRecording" $false)) {
            Invoke-Automation -Command "SetRecordingEnabled" -Payload @{ enabled = $false } -AllowFailure | Out-Null
            Wait-Condition -ConditionName "RecordingStopped" -TimeoutMs ([Math]::Max($WaitTimeoutMs, 150000))
        }
    }
    catch {
        Write-Warning "Recording cleanup failed: $($_.Exception.Message)"
    }

    if ($script:LaunchedApp -and -not $NoClose) {
        try {
            Invoke-Automation -Command "ArmClose" -Payload @{ armed = $true } -AllowFailure | Out-Null
            Invoke-Automation -Command "WindowAction" -Payload @{ action = "Close" } -AllowFailure | Out-Null
        }
        catch {
            Write-Warning "Launched app close failed: $($_.Exception.Message)"
        }

        return
    }

    try {
        $initialFlashback = Convert-ToBool (Get-PropertyValue $script:InitialSnapshot "FlashbackActive" $false)
        Invoke-Automation -Command "SetFlashbackEnabled" -Payload @{ enabled = $initialFlashback } -AllowFailure | Out-Null
    }
    catch {
        Write-Warning "Flashback restore failed: $($_.Exception.Message)"
    }

    try {
        $initialPreview = Convert-ToBool (Get-PropertyValue $script:InitialSnapshot "IsPreviewing" $false)
        Invoke-Automation -Command "SetPreviewEnabled" -Payload @{ enabled = $initialPreview } -AllowFailure | Out-Null
    }
    catch {
        Write-Warning "Preview restore failed: $($_.Exception.Message)"
    }
}

function Write-MarkdownReport {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RunDir,
        [Parameter(Mandatory = $true)]
        [object]$Summary
    )

    $reportPath = Join-Path $RunDir "summary.md"
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Capture Rewrite Live Baseline")
    $lines.Add("")
    $lines.Add("- Run started: $($Summary.StartedAt)")
    $lines.Add("- Output directory: $RunDir")
    $lines.Add("- App process: $($Summary.App.ProcessId)")
    $lines.Add("- Preview seconds: $PreviewSeconds")
    $lines.Add("- Flashback seconds: $FlashbackSeconds")
    $lines.Add("- Recording seconds: $RecordingSeconds")
    $lines.Add("")
    $lines.Add("## Scenarios")
    foreach ($scenario in $Summary.Scenarios) {
        $status = "captured"
        if ($scenario.PSObject.Properties["Skipped"]) {
            $status = "skipped"
        }
        elseif ($scenario.PSObject.Properties["Success"] -and -not [bool]$scenario.Success) {
            $status = "failed"
        }

        $lines.Add("- $($scenario.Name): $status")
    }

    $lines | Set-Content -Path $reportPath -Encoding UTF8
    return $reportPath
}

function Append-ImplementationLogEntry {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Summary,
        [Parameter(Mandatory = $true)]
        [string]$ReportPath
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("")
    $lines.Add("## $(Get-Date -Format 'yyyy-MM-dd HH:mm') - Phase 0.5 live baseline run")
    $lines.Add("")
    $lines.Add("- Output directory: $($Summary.OutputDirectory)")
    $lines.Add("- Summary report: $ReportPath")
    $lines.Add("- App process: $($Summary.App.ProcessId)")
    foreach ($scenario in $Summary.Scenarios) {
        $lines.Add("- Scenario $($scenario.Name): captured")
    }

    Add-Content -Path $script:ImplementationLogPath -Value $lines
}

Assert-PositiveInt -Value $PreviewSeconds -Name "PreviewSeconds"
Assert-PositiveInt -Value $FlashbackSeconds -Name "FlashbackSeconds"
Assert-PositiveInt -Value $RecordingSeconds -Name "RecordingSeconds"
Assert-PositiveInt -Value $PresentMonSeconds -Name "PresentMonSeconds"
Assert-PositiveInt -Value $WaitTimeoutMs -Name "WaitTimeoutMs"
Assert-PositiveInt -Value $WaitPollMs -Name "WaitPollMs"
Assert-PositiveInt -Value $SnapshotIntervalMs -Name "SnapshotIntervalMs"

if (-not (Test-Path $script:SendCommandPath)) {
    throw "Missing automation sender script: $script:SendCommandPath"
}

if ($ValidateOnly) {
    $validation = [ordered]@{
        Root = $script:Root
        SendCommandPath = $script:SendCommandPath
        VerifyDedicatedLibAvPath = $script:VerifyDedicatedLibAvPath
        EcCtlPath = $script:EcCtlPath
        AppExePath = $script:AppExePath
        AppExeExists = Test-Path $script:AppExePath
        NoLaunch = [bool]$NoLaunch
        SkipPresentMon = [bool]$SkipPresentMon
        Scenarios = @(
            if (-not $SkipPreviewOnly) { "preview-only" }
            if (-not $SkipFlashbackRetention) { "flashback-retention" }
            if (-not $SkipFlashbackRecording) { "flashback-recording" }
            if (-not $SkipDedicatedLibAv) { "dedicated-libav-recording" }
        )
    }

    $validation | ConvertTo-Json -Depth 20
    return
}

$runDir = New-RunDirectory
$scenarioResults = [System.Collections.Generic.List[object]]::new()
$appInfo = $null

try {
    $appInfo = Start-AppIfNeeded
    $script:InitialSnapshot = Get-Snapshot
    Write-JsonFile -Path (Join-Path $runDir "initial.snapshot.json") -Value $script:InitialSnapshot

    if (-not $SkipPreviewOnly) {
        $scenarioResults.Add((Invoke-PreviewScenario -RunDir $runDir))
    }

    if (-not $SkipFlashbackRetention) {
        $scenarioResults.Add((Invoke-FlashbackRetentionScenario -RunDir $runDir))
    }

    if (-not $SkipFlashbackRecording) {
        $scenarioResults.Add((Invoke-FlashbackRecordingScenario -RunDir $runDir))
    }

    if (-not $SkipDedicatedLibAv) {
        $scenarioResults.Add((Invoke-DedicatedLibAvScenario -RunDir $runDir))
    }

    $finalSnapshot = Get-Snapshot
    Write-JsonFile -Path (Join-Path $runDir "final.snapshot.json") -Value $finalSnapshot

    $summary = [pscustomobject]@{
        StartedAt = Get-IsoTimestamp
        OutputDirectory = $runDir
        PipeName = $PipeName
        App = $appInfo
        Initial = New-SnapshotSummary -Snapshot $script:InitialSnapshot
        Final = New-SnapshotSummary -Snapshot $finalSnapshot
        Scenarios = $scenarioResults
    }

    $summaryPath = Join-Path $runDir "summary.json"
    Write-JsonFile -Path $summaryPath -Value $summary
    $reportPath = Write-MarkdownReport -RunDir $runDir -Summary $summary
    if ($AppendImplementationLog) {
        Append-ImplementationLogEntry -Summary $summary -ReportPath $reportPath
    }

    Write-Host "Capture rewrite live baseline: captured $($scenarioResults.Count) scenario(s)."
    Write-Host "Summary: $summaryPath"
    $summary | ConvertTo-Json -Depth 80
}
finally {
    Restore-AppState
}
