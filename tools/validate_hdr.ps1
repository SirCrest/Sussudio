param(
    [Parameter(Mandatory = $true)]
    [string]$File,
    [switch]$ExpectHdr,
    [ValidateSet("hevc", "av1", "either")]
    [string]$Codec = "either",
    [switch]$RequireHdr10StaticMetadata,
    [double]$ExpectedFps = 0
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    return Split-Path -Parent $PSScriptRoot
}

function Resolve-FfprobePath {
    $ffprobeCommand = Get-Command ffprobe.exe -ErrorAction SilentlyContinue
    $ffprobeFromPath = $null
    if ($ffprobeCommand) {
        $ffprobeFromPath = $ffprobeCommand.Source
    }
    if ($ffprobeFromPath) {
        return $ffprobeFromPath
    }

    $repoRoot = Get-RepoRoot
    $candidate = Join-Path $repoRoot "latest-build\\ffmpeg\\ffprobe.exe"
    if (Test-Path $candidate) {
        return $candidate
    }

    $candidate = Join-Path $repoRoot "Sussudio\\bin\\x64\\Debug\\net8.0-windows10.0.19041.0\\win-x64\\ffmpeg\\ffprobe.exe"
    if (Test-Path $candidate) {
        return $candidate
    }

    throw "ffprobe.exe not found on PATH or known repo locations."
}

function Invoke-FfprobeJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FfprobePath,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $allOutput = & $FfprobePath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        $joined = [string]::Join([Environment]::NewLine, @($allOutput))
        throw "ffprobe failed ($LASTEXITCODE): $joined"
    }

    $jsonText = [string]::Join([Environment]::NewLine, @($allOutput))
    if ([string]::IsNullOrWhiteSpace($jsonText)) {
        throw "ffprobe returned empty output."
    }

    return $jsonText | ConvertFrom-Json
}

function Convert-RationalToDouble {
    param([string]$Raw)

    if ([string]::IsNullOrWhiteSpace($Raw)) {
        return $null
    }

    $parts = $Raw.Split("/")
    if ($parts.Count -eq 2) {
        $num = 0.0
        $den = 0.0
        if ([double]::TryParse($parts[0], [ref]$num) -and
            [double]::TryParse($parts[1], [ref]$den) -and
            [Math]::Abs($den) -gt [double]::Epsilon) {
            return $num / $den
        }
    }

    $direct = 0.0
    if ([double]::TryParse($Raw, [ref]$direct)) {
        return $direct
    }

    return $null
}

function Get-FrameTimestamp {
    param($Frame)

    $candidates = @(
        $Frame.best_effort_timestamp_time,
        $Frame.pkt_dts_time,
        $Frame.pkt_pts_time
    )

    foreach ($candidate in $candidates) {
        if ($null -eq $candidate) {
            continue
        }

        $value = 0.0
        if ([double]::TryParse([string]$candidate, [ref]$value)) {
            return $value
        }
    }

    return $null
}

function Analyze-Cadence {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Frames,
        [Parameter(Mandatory = $true)]
        [double]$ExpectedFps
    )

    $intervals = New-Object System.Collections.Generic.List[double]
    $previous = $null
    foreach ($frame in $Frames) {
        $timestamp = Get-FrameTimestamp -Frame $frame
        if ($null -eq $timestamp) {
            continue
        }

        if ($null -ne $previous) {
            $delta = ($timestamp - $previous) * 1000.0
            if ($delta -gt 0 -and $delta -lt 5000) {
                $intervals.Add($delta)
            }
        }

        $previous = $timestamp
    }

    if ($intervals.Count -lt 1) {
        return $null
    }

    $sampleCount = $intervals.Count
    $expectedInterval = 1000.0 / $ExpectedFps
    $average = ($intervals | Measure-Object -Average).Average
    $sorted = $intervals.ToArray()
    [Array]::Sort($sorted)
    $p95Index = [int][Math]::Ceiling(($sorted.Length - 1) * 0.95)
    if ($p95Index -lt 0) {
        $p95Index = 0
    }
    if ($p95Index -ge $sorted.Length) {
        $p95Index = $sorted.Length - 1
    }
    $p95 = $sorted[$p95Index]

    $severeThreshold = $expectedInterval * 2.25
    $severeGaps = 0
    $estimatedDropped = 0L
    foreach ($interval in $intervals) {
        if ($interval -ge $severeThreshold) {
            $severeGaps++
        }

        $missing = [Math]::Floor(($interval + $expectedInterval * 0.20) / $expectedInterval) - 1
        if ($missing -gt 0) {
            $estimatedDropped += [int64]$missing
        }
    }

    $severePercent = if ($sampleCount -gt 0) { ($severeGaps / $sampleCount) * 100.0 } else { 0.0 }
    $dropPercent = if (($sampleCount + $estimatedDropped) -gt 0) {
        ($estimatedDropped / ($sampleCount + $estimatedDropped)) * 100.0
    }
    else {
        0.0
    }

    return [pscustomobject]@{
        SampleCount = $sampleCount
        ExpectedIntervalMs = $expectedInterval
        AverageIntervalMs = $average
        P95IntervalMs = $p95
        SevereGapCount = $severeGaps
        SevereGapPercent = $severePercent
        EstimatedDroppedFrames = $estimatedDropped
        EstimatedDropPercent = $dropPercent
    }
}

function Contains-CaseInsensitive {
    param(
        [string]$Value,
        [string]$Token
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return $Value.IndexOf($Token, [StringComparison]::OrdinalIgnoreCase) -ge 0
}

$resolvedFile = Resolve-Path -Path $File -ErrorAction Stop
$ffprobePath = Resolve-FfprobePath

$streamArgs = @(
    "-v", "error",
    "-select_streams", "v:0",
    "-show_entries", "format=format_name",
    "-show_entries", "stream=codec_name,profile,width,height,avg_frame_rate,r_frame_rate,pix_fmt,color_primaries,color_transfer,color_space,color_range,side_data_list",
    "-of", "json",
    $resolvedFile.Path
)
$probe = Invoke-FfprobeJson -FfprobePath $ffprobePath -Arguments $streamArgs

if ($null -eq $probe.streams -or $probe.streams.Count -lt 1) {
    throw "No video stream found in file: $($resolvedFile.Path)"
}

$stream = $probe.streams[0]
$codecName = [string]$stream.codec_name
$pixFmt = [string]$stream.pix_fmt
$colorPrimaries = [string]$stream.color_primaries
$colorTransfer = [string]$stream.color_transfer
$colorSpace = [string]$stream.color_space
$avgFrameRateRaw = [string]$stream.avg_frame_rate
$rFrameRateRaw = [string]$stream.r_frame_rate
$detectedFps = Convert-RationalToDouble $avgFrameRateRaw
if ($null -eq $detectedFps) {
    $detectedFps = Convert-RationalToDouble $rFrameRateRaw
}

$mismatches = New-Object System.Collections.Generic.List[string]

if ($ExpectHdr -or $Codec -ne "either") {
    $codecAllowed = switch ($Codec) {
        "hevc" { @("hevc") }
        "av1" { @("av1") }
        default { @("hevc", "av1") }
    }
    if (-not ($codecAllowed -contains $codecName.ToLowerInvariant())) {
        $expectedCodec = [string]::Join("|", $codecAllowed)
        $mismatches.Add("codec-mismatch(expected=$expectedCodec,actual=$codecName)")
    }
}

if ($ExpectHdr) {
    $allowedPixFmts = @("p010le", "yuv420p10le", "yuv422p10le", "yuv444p10le")
    if (-not ($allowedPixFmts -contains $pixFmt.ToLowerInvariant())) {
        $mismatches.Add("pixfmt-not-10bit(actual=$pixFmt)")
    }

    if (-not (Contains-CaseInsensitive -Value $colorPrimaries -Token "bt2020")) {
        $mismatches.Add("colorimetry-mismatch(primaries=$colorPrimaries)")
    }
    if (-not (Contains-CaseInsensitive -Value $colorTransfer -Token "smpte2084")) {
        $mismatches.Add("colorimetry-mismatch(transfer=$colorTransfer)")
    }

    $colorSpaceNormalized = $colorSpace.ToLowerInvariant()
    if ($colorSpaceNormalized -ne "bt2020nc" -and $colorSpaceNormalized -ne "bt2020c") {
        $mismatches.Add("colorimetry-mismatch(space=$colorSpace)")
    }

    if ($RequireHdr10StaticMetadata) {
        $hasMastering = $false
        $hasContentLight = $false
        if ($stream.side_data_list) {
            foreach ($entry in $stream.side_data_list) {
                $sideDataType = [string]$entry.side_data_type
                if (Contains-CaseInsensitive -Value $sideDataType -Token "Mastering display metadata") {
                    $hasMastering = $true
                }
                if (Contains-CaseInsensitive -Value $sideDataType -Token "Content light level metadata") {
                    $hasContentLight = $true
                }
            }
        }

        if (-not ($hasMastering -or $hasContentLight)) {
            $mismatches.Add("hdr-metadata-missing")
        }
    }
}

$cadenceMetrics = $null
if ($ExpectedFps -gt 0) {
    $frameArgs = @(
        "-v", "error",
        "-select_streams", "v:0",
        "-show_frames",
        "-show_entries", "frame=best_effort_timestamp_time,pkt_dts_time,pkt_pts_time",
        "-of", "json",
        $resolvedFile.Path
    )
    $frameProbe = Invoke-FfprobeJson -FfprobePath $ffprobePath -Arguments $frameArgs
    $frameArray = @($frameProbe.frames)
    $cadenceMetrics = Analyze-Cadence -Frames $frameArray -ExpectedFps $ExpectedFps
    if ($null -eq $cadenceMetrics) {
        $mismatches.Add("cadence-unavailable")
    }
    else {
        if ($cadenceMetrics.EstimatedDropPercent -ge 5.0) {
            $mismatches.Add("cadence-drop-high(percent={0:0.###},estimated={1})" -f $cadenceMetrics.EstimatedDropPercent, $cadenceMetrics.EstimatedDroppedFrames)
        }
        if ($cadenceMetrics.SevereGapPercent -ge 3.0) {
            $mismatches.Add("cadence-gaps-high(percent={0:0.###},count={1})" -f $cadenceMetrics.SevereGapPercent, $cadenceMetrics.SevereGapCount)
        }
        if ($cadenceMetrics.P95IntervalMs -ge ($cadenceMetrics.ExpectedIntervalMs * 2.5)) {
            $mismatches.Add("cadence-p95-high(expectedMs={0:0.###},p95Ms={1:0.###})" -f $cadenceMetrics.ExpectedIntervalMs, $cadenceMetrics.P95IntervalMs)
        }
    }
}

$repoRoot = Get-RepoRoot
$artifactRoot = Join-Path $repoRoot "artifacts\\hdr-validator\\$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -Path $artifactRoot -ItemType Directory -Force | Out-Null
$streamProbePath = Join-Path $artifactRoot "ffprobe.stream.json"
($probe | ConvertTo-Json -Depth 12) | Set-Content -Path $streamProbePath -Encoding UTF8

if ($cadenceMetrics) {
    $cadencePath = Join-Path $artifactRoot "ffprobe.cadence.metrics.json"
    ($cadenceMetrics | ConvertTo-Json -Depth 6) | Set-Content -Path $cadencePath -Encoding UTF8
}

$summary = "codec_name=$codecName pix_fmt=$pixFmt color_primaries=$colorPrimaries color_transfer=$colorTransfer color_space=$colorSpace avg_frame_rate=$avgFrameRateRaw detected_fps=$detectedFps"
Write-Host "HDR_VALIDATE_FIELDS $summary"
Write-Host "HDR_VALIDATE_ARTIFACTS $artifactRoot"

if ($mismatches.Count -eq 0) {
    Write-Host "HDR_VALIDATE_RESULT PASS file='$($resolvedFile.Path)' codec='$codecName'"
    exit 0
}

$joined = [string]::Join(", ", $mismatches)
Write-Error "HDR_VALIDATE_RESULT FAIL file='$($resolvedFile.Path)' mismatches=[$joined]"
exit 1
