using System;
using System.Linq;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Background device-format probe reconciliation. This keeps late-arriving
/// capability data from polluting the general device discovery flow.
/// </summary>
public partial class MainViewModel
{
    private void OnDeviceFormatProbeCompleted(object? sender, DeviceService.DeviceFormatProbeCompletedEventArgs e)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            if (e.RequestId != Interlocked.Read(ref _deviceScanGeneration))
            {
                return;
            }

            var target = Devices.FirstOrDefault(d => string.Equals(d.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                return;
            }

            if (!e.Succeeded)
            {
                _pendingSdrAutoSelectionForDeviceChange = false;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
                Logger.Log($"Format probe failed for {e.DeviceName}: {e.Error}");
                return;
            }

            target.SupportedFormats.Clear();
            foreach (var format in e.Formats)
            {
                target.SupportedFormats.Add(new MediaFormat
                {
                    Width = format.Width,
                    Height = format.Height,
                    FrameRate = format.FrameRate,
                    FrameRateNumerator = format.FrameRateNumerator,
                    FrameRateDenominator = format.FrameRateDenominator,
                    PixelFormat = format.PixelFormat,
                    IsHdr = format.IsHdr
                });
            }

            target.IsHdrCapable = e.IsHdrCapable;

            if (SelectedDevice == null ||
                !string.Equals(SelectedDevice.Id, target.Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var preserveActiveSelection = IsPreviewing || IsRecording;
            var allowProbeDrivenRetarget = IsPreviewing && IsInitialized && !IsRecording;
            var previousResolution = SelectedResolution;
            var previousFrameRate = SelectedFrameRate;
            Logger.Log($"Format probe completed for {e.DeviceName}: formats={e.Formats.Count} preserveActive={preserveActiveSelection} allowRetarget={allowProbeDrivenRetarget} prevRes={previousResolution} prevFps={previousFrameRate:0.###}");

            if (preserveActiveSelection)
            {
                Logger.Log($"Refreshing selected-device capabilities during active capture for {e.DeviceName} (preserveSelection={!allowProbeDrivenRetarget}).");
            }

            _suppressFormatChangeReinitialize = preserveActiveSelection;
            try
            {
                RebuildSelectedDeviceCapabilities(SelectedDevice, resetTelemetryState: false);
            }
            finally
            {
                _suppressFormatChangeReinitialize = false;
            }
            Logger.Log($"Format probe rebuild done: SelectedRes={SelectedResolution} SelectedFormat={SelectedFormat?.Width}x{SelectedFormat?.Height}@{SelectedFormat?.FrameRate:0.###} modeChanged={!string.Equals(previousResolution, SelectedResolution, StringComparison.OrdinalIgnoreCase) || !IsFrameRateMatch(previousFrameRate, SelectedFrameRate)}");

            var modeChanged = !string.Equals(previousResolution, SelectedResolution, StringComparison.OrdinalIgnoreCase) ||
                              !IsFrameRateMatch(previousFrameRate, SelectedFrameRate);

            var retargetDecision = DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(
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
                IncludeSessionMismatchCheck: false,
                SessionActualWidth: null,
                SessionActualHeight: null));

            if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.HdrRetarget)
            {
                Logger.Log($"Format probe updated HDR mode set; applying new mode {SelectedResolution}@{SelectedFrameRate:0.###} via device renegotiation.");
                EnqueueUiOperation(
                    () => ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                    retargetDecision.UiOperationName!);
                return;
            }

            if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.PreserveMjpegHighFrameRate)
            {
                Logger.Log(
                    $"Format probe preserved special MJPG HFR mode at {SelectedResolution}@{SelectedFrameRate:0.###}; " +
                    "skipping SDR NV12 retarget.");
                return;
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
                return;
            }

            // After probes complete, compare the live session negotiated resolution against
            // the now-resolved SelectedFormat. This catches the startup case where preview began
            // with an incomplete format list (probes not yet done) and therefore initialized at
            // a lower resolution than the user saved selection.
            if (allowProbeDrivenRetarget && SelectedFormat != null)
            {
                var runtime = GetCaptureRuntimeSnapshot();
                Logger.Log($"Format probe session check: actual={runtime.ActualWidth}x{runtime.ActualHeight} selected={SelectedFormat.Width}x{SelectedFormat.Height}");
                retargetDecision = DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(
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
                    IncludeSessionMismatchCheck: true,
                    SessionActualWidth: runtime.ActualWidth,
                    SessionActualHeight: runtime.ActualHeight));

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
                    return;
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
        }))
        {
            Logger.Log($"FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
        }
    }
}
