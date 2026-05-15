using System;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
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
    // Cached enum values for the telemetry projection. ToString() on enums goes
    // through Enum.GetName which allocates per call; telemetry snapshots fire
    // every device-cycle (often ~500ms) and these enums rarely change. Fully
    // qualified to disambiguate from the partial-property string accessors with
    // the same names on this class.
    private Models.SourceTelemetryAvailability _lastAppliedTelemetryAvailability = Models.SourceTelemetryAvailability.Unknown;
    private Models.SourceTelemetryConfidence _lastAppliedTelemetryConfidence = Models.SourceTelemetryConfidence.Unknown;
    private SourceTelemetryOrigin _lastAppliedFrameRateOrigin = SourceTelemetryOrigin.Unknown;
    private bool _hasAppliedTelemetryEnums;

    private void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        HdrRuntimeState = runtime.HdrRuntimeState;
        HdrReadinessReason = runtime.HdrReadinessReason;
        UpdateTargetSummary();
    }

    private void RefreshSourceTelemetrySummaryAge()
    {
        var ageSeconds = TelemetryAgeHelper.ComputeAgeSeconds(SourceTelemetryTimestampUtc, DateTimeOffset.UtcNow);
        var ageBucket = ageSeconds.HasValue ? ageSeconds.Value / 5 : (int?)null;
        if (_lastTelemetryAgeBucket.HasValue &&
            ageBucket.HasValue &&
            _lastTelemetryAgeBucket.Value == ageBucket.Value)
        {
            return;
        }

        var refreshedSummary = SourceTelemetryPresentationBuilder.BuildSourceSummary(_latestSourceTelemetry, DateTimeOffset.UtcNow);
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
        if (!_hasAppliedTelemetryEnums || _lastAppliedTelemetryAvailability != snapshot.Availability)
        {
            SourceTelemetryAvailability = snapshot.Availability.ToString();
            _lastAppliedTelemetryAvailability = snapshot.Availability;
        }
        SourceTelemetryOriginDetail = snapshot.OriginDetail;
        if (!_hasAppliedTelemetryEnums || _lastAppliedTelemetryConfidence != snapshot.Confidence)
        {
            SourceTelemetryConfidence = snapshot.Confidence.ToString();
            _lastAppliedTelemetryConfidence = snapshot.Confidence;
        }
        SourceTelemetryDiagnosticSummary = snapshot.DiagnosticSummary;
        SourceTelemetryTimestampUtc = snapshot.TimestampUtc;
        DetectedSourceFrameRate = snapshot.FrameRateExact;
        DetectedSourceFrameRateArg = snapshot.FrameRateArg;
        if (!_hasAppliedTelemetryEnums || _lastAppliedFrameRateOrigin != snapshot.Origin)
        {
            SourceFrameRateOrigin = snapshot.Origin != SourceTelemetryOrigin.Unknown
                ? snapshot.Origin.ToString()
                : "Unknown";
            _lastAppliedFrameRateOrigin = snapshot.Origin;
        }
        _hasAppliedTelemetryEnums = true;
        _lastTelemetryAgeBucket = null;
        SourceTelemetrySummaryText = SourceTelemetryPresentationBuilder.BuildSourceSummary(snapshot, DateTimeOffset.UtcNow);

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

    private void UpdateTargetSummary()
    {
        SourceTargetSummaryText = SourceTelemetryPresentationBuilder.BuildTargetSummary(
            GetSelectedResolutionDisplayText(),
            SelectedFrameRate,
            SelectedFriendlyFrameRate,
            SelectedExactFrameRate,
            SelectedExactFrameRateArg,
            HdrRuntimeState);
    }
}
