using System;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

/// <summary>
/// Source telemetry projection. Native XU/device-format telemetry is advisory:
/// it updates live signal labels and auto-retargeting hints without becoming the
/// capture pipeline's authoritative negotiated format.
/// </summary>
public partial class MainViewModel
{
    private void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        HdrRuntimeState = runtime.HdrRuntimeState;
        HdrReadinessReason = runtime.HdrReadinessReason;
        UpdateTargetSummary();
    }

    private void RefreshSourceTelemetrySummaryAge()
    {
        var ageSeconds = ComputeTelemetryAgeSeconds(SourceTelemetryTimestampUtc, DateTimeOffset.UtcNow);
        var ageBucket = ageSeconds.HasValue ? ageSeconds.Value / 5 : (int?)null;
        if (_lastTelemetryAgeBucket.HasValue &&
            ageBucket.HasValue &&
            _lastTelemetryAgeBucket.Value == ageBucket.Value)
        {
            return;
        }

        var refreshedSummary = BuildSourceTelemetrySummaryText(_latestSourceTelemetry, DateTimeOffset.UtcNow);
        if (!string.Equals(SourceTelemetrySummaryText, refreshedSummary, StringComparison.Ordinal))
        {
            SourceTelemetrySummaryText = refreshedSummary;
        }

        _lastTelemetryAgeBucket = ageBucket;
    }


    private void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            ApplySourceTelemetrySnapshot(snapshot, allowAutoRetarget: true);
        }))
        {
            Logger.Log(
                $"SOURCE_TELEMETRY_UI_ENQUEUE_FAILED availability={snapshot.Availability} " +
                $"origin={snapshot.Origin} mode='{snapshot.GetModeKey()}'");
        }
    }

    private void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)
    {
        _latestSourceTelemetry = snapshot;
        SourceWidth = snapshot.Width;
        SourceHeight = snapshot.Height;
        SourceIsHdr = snapshot.IsHdr;
        if (!IsRecording && IsHdrEnabled && snapshot.IsHdr == false)
        {
            IsHdrEnabled = false;
        }
        SourceTelemetryAvailability = snapshot.Availability.ToString();
        SourceTelemetryOriginDetail = snapshot.OriginDetail;
        SourceTelemetryConfidence = snapshot.Confidence.ToString();
        SourceTelemetryDiagnosticSummary = snapshot.DiagnosticSummary;
        SourceTelemetryTimestampUtc = snapshot.TimestampUtc;
        DetectedSourceFrameRate = snapshot.FrameRateExact;
        DetectedSourceFrameRateArg = snapshot.FrameRateArg;
        SourceFrameRateOrigin = snapshot.Origin != SourceTelemetryOrigin.Unknown
            ? snapshot.Origin.ToString()
            : "Unknown";
        _lastTelemetryAgeBucket = null;
        SourceTelemetrySummaryText = BuildSourceTelemetrySummaryText(snapshot, DateTimeOffset.UtcNow);

        var modeKey = snapshot.GetModeKey();
        if (!string.IsNullOrWhiteSpace(modeKey) &&
            !string.Equals(modeKey, _lastSourceModeKey, StringComparison.Ordinal))
        {
            if (allowAutoRetarget)
            {
                var shouldAutoRetargetResolution =
                    IsAutoResolutionValue(SelectedResolution) ||
                    !_hasUserOverriddenResolutionForCurrentMode;
                var shouldAutoRetargetFrameRate =
                    IsAutoFrameRateSelected ||
                    !_hasUserOverriddenFrameRateForCurrentMode;
                _lastSourceModeKey = modeKey;
                _forceSourceAutoRetarget = shouldAutoRetargetResolution || shouldAutoRetargetFrameRate;
                if (shouldAutoRetargetResolution)
                {
                    _hasUserOverriddenResolutionForCurrentMode = false;
                }

                if (shouldAutoRetargetFrameRate)
                {
                    _hasUserOverriddenFrameRateForCurrentMode = false;
                }
            }
        }

        var shouldRebuildModeOptions = allowAutoRetarget &&
                                       (_forceSourceAutoRetarget ||
                                        (snapshot.HasSignalData && AvailableResolutions.Count == 0));
        if (shouldRebuildModeOptions)
        {
            if (IsRecording)
            {
                _pendingModeOptionsRefresh = true;
            }
            else
            {
                RebuildResolutionOptions();
            }
        }
        else
        {
            UpdateTargetSummary();
        }
    }

    private static string BuildSourceTelemetrySummaryText(SourceSignalTelemetrySnapshot snapshot, DateTimeOffset nowUtc)
    {
        if (!snapshot.HasSignalData &&
            snapshot.Availability is Models.SourceTelemetryAvailability.Unavailable or Models.SourceTelemetryAvailability.Unknown)
        {
            return "Source: waiting for signal telemetry";
        }

        var resolution = snapshot.HasDimensions
            ? $"{snapshot.Width}x{snapshot.Height}"
            : "?x?";
        var fps = snapshot.FrameRateArg ??
                  snapshot.FrameRateExact?.ToString("0.###") ??
                  "?";
        var hdr = snapshot.IsHdr.HasValue ? (snapshot.IsHdr.Value ? "HDR" : "SDR") : "HDR?";
        var ageText = BuildTelemetryAgeText(snapshot.TimestampUtc, nowUtc);
        return $"Source: {resolution} @ {fps} | {hdr} | {snapshot.Availability}/{snapshot.Confidence} | {ageText}";
    }

    private static string BuildTelemetryAgeText(DateTimeOffset timestampUtc, DateTimeOffset nowUtc)
    {
        var ageSeconds = ComputeTelemetryAgeSeconds(timestampUtc, nowUtc);
        if (!ageSeconds.HasValue)
        {
            return "updated ?";
        }

        return ageSeconds.Value <= 0
            ? "updated now"
            : $"updated {ageSeconds.Value}s ago";
    }

    private static int? ComputeTelemetryAgeSeconds(DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)
    {
        if (!timestampUtc.HasValue)
        {
            return null;
        }

        var age = nowUtc - timestampUtc.Value;
        if (age < TimeSpan.Zero)
        {
            return 0;
        }

        return (int)Math.Floor(age.TotalSeconds);
    }

    private void UpdateTargetSummary()
    {
        var friendly = SelectedFriendlyFrameRate ?? Math.Round(SelectedFrameRate);
        var exact = SelectedExactFrameRate ?? SelectedFrameRate;
        var exactArg = SelectedExactFrameRateArg;
        var exactText = !string.IsNullOrWhiteSpace(exactArg)
            ? exactArg
            : exact > 0
                ? exact.ToString("0.###")
                : "?";
        var hdrStateText = string.IsNullOrWhiteSpace(HdrRuntimeState) ? "Unknown" : HdrRuntimeState;
        SourceTargetSummaryText = $"Target: {GetSelectedResolutionDisplayText()} @ {friendly:0} (exact {exactText}) | HDR={hdrStateText}";
    }
}
