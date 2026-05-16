using System;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private bool TryApplyDeviceFormatProbeRetarget(
        CaptureDevice target,
        bool preserveActiveSelection,
        bool allowProbeDrivenRetarget,
        string? previousResolution,
        double previousFrameRate,
        bool modeChanged)
    {
        var retargetDecision = DecideDeviceFormatProbeRetarget(
            target,
            preserveActiveSelection,
            allowProbeDrivenRetarget,
            previousResolution,
            previousFrameRate,
            modeChanged,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.HdrRetarget)
        {
            Logger.Log($"Format probe updated HDR mode set; applying new mode {SelectedResolution}@{SelectedFrameRate:0.###} via device renegotiation.");
            EnqueueUiOperation(
                () => ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                retargetDecision.UiOperationName!);
            return true;
        }

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.PreserveMjpegHighFrameRate)
        {
            Logger.Log(
                $"Format probe preserved special MJPG HFR mode at {SelectedResolution}@{SelectedFrameRate:0.###}; " +
                "skipping SDR NV12 retarget.");
            return true;
        }

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.SdrNv12Retarget)
        {
            Logger.Log(
                $"Format probe detected MJPG-only mode at {SelectedResolution}@{SelectedFrameRate:0.###}; " +
                $"retargeting SDR to NV12-capable mode {retargetDecision.TargetResolution}@{retargetDecision.TargetFrameRate:0.###}.");

            _isRebuildingModeOptions = true;
            _isApplyingAutomaticResolutionSelection = true;
            try
            {
                SelectedResolution = retargetDecision.TargetResolution;
            }
            finally
            {
                _isApplyingAutomaticResolutionSelection = false;
                _isRebuildingModeOptions = false;
            }

            _suppressFormatChangeReinitialize = true;
            try
            {
                RebuildFrameRateOptions();
            }
            finally
            {
                _suppressFormatChangeReinitialize = false;
            }

            EnqueueUiOperation(
                () => ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                retargetDecision.UiOperationName!);
            return true;
        }

        // After probes complete, compare the live session negotiated resolution against
        // the now-resolved SelectedFormat. This catches the startup case where preview began
        // with an incomplete format list (probes not yet done) and therefore initialized at
        // a lower resolution than the user saved selection.
        if (allowProbeDrivenRetarget && SelectedFormat != null)
        {
            var runtime = GetCaptureRuntimeSnapshot();
            Logger.Log($"Format probe session check: actual={runtime.ActualWidth}x{runtime.ActualHeight} selected={SelectedFormat.Width}x{SelectedFormat.Height}");
            retargetDecision = DecideDeviceFormatProbeRetarget(
                target,
                preserveActiveSelection,
                allowProbeDrivenRetarget,
                previousResolution,
                previousFrameRate,
                modeChanged,
                includeSessionMismatchCheck: true,
                sessionActualWidth: runtime.ActualWidth,
                sessionActualHeight: runtime.ActualHeight);

            if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.SessionRuntimeUnavailable)
            {
                Logger.Log("Format probe session mismatch check skipped: runtime width/height not yet available.");
            }
            else if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.SessionMismatch)
            {
                Logger.Log(
                    $"Format probe detected session/format mismatch: " +
                    $"session={runtime.ActualWidth}x{runtime.ActualHeight} " +
                    $"selected={SelectedFormat.Width}x{SelectedFormat.Height}; reinitializing.");
                EnqueueUiOperation(
                    () => ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                    retargetDecision.UiOperationName!);
                return true;
            }
        }

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.RestoreActiveSelection)
        {
            _isRebuildingModeOptions = true;
            _isApplyingAutomaticResolutionSelection = true;
            try
            {
                SelectedResolution = previousResolution;
                SelectedFrameRate = previousFrameRate;
                UpdateSelectedFormat();
                UpdateTargetSummary();
            }
            finally
            {
                _isApplyingAutomaticResolutionSelection = false;
                _isRebuildingModeOptions = false;
            }
        }

        return false;
    }

    private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(
        CaptureDevice target,
        bool preserveActiveSelection,
        bool allowProbeDrivenRetarget,
        string? previousResolution,
        double previousFrameRate,
        bool modeChanged,
        bool includeSessionMismatchCheck,
        uint? sessionActualWidth,
        uint? sessionActualHeight)
        => DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(
            preserveActiveSelection,
            allowProbeDrivenRetarget,
            IsHdrEnabled,
            modeChanged,
            previousResolution,
            previousFrameRate,
            SelectedResolution,
            SelectedFrameRate,
            SelectedFormat,
            target.SupportedFormats,
            !string.IsNullOrWhiteSpace(previousResolution) &&
                AvailableResolutions.Any(option => string.Equals(option.Value, previousResolution, StringComparison.OrdinalIgnoreCase)),
            includeSessionMismatchCheck,
            sessionActualWidth,
            sessionActualHeight));
}
