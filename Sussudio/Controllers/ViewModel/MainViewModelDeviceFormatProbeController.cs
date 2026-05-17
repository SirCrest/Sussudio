using System;
using System.Linq;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns late device-format probe reconciliation for the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelDeviceFormatProbeController
    {
        private readonly MainViewModel _viewModel;

        public MainViewModelDeviceFormatProbeController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void OnDeviceFormatProbeCompleted(object? sender, DeviceService.DeviceFormatProbeCompletedEventArgs e)
        {
            if (!_viewModel._dispatcherQueue.TryEnqueue(() =>
            {
                if (e.RequestId != Interlocked.Read(ref _viewModel._deviceScanGeneration))
                {
                    return;
                }

                var target = _viewModel.Devices.FirstOrDefault(
                    d => string.Equals(d.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase));
                if (target == null)
                {
                    return;
                }

                if (!e.Succeeded)
                {
                    _viewModel._pendingSdrAutoSelectionForDeviceChange = false;
                    _viewModel._pendingSdrAutoFriendlyFrameRateBucket = null;
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

                if (_viewModel.SelectedDevice == null ||
                    !string.Equals(_viewModel.SelectedDevice.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var preserveActiveSelection = _viewModel.IsPreviewing || _viewModel.IsRecording;
                var allowProbeDrivenRetarget = _viewModel.IsPreviewing && _viewModel.IsInitialized && !_viewModel.IsRecording;
                var previousResolution = _viewModel.SelectedResolution;
                var previousFrameRate = _viewModel.SelectedFrameRate;
                Logger.Log($"Format probe completed for {e.DeviceName}: formats={e.Formats.Count} preserveActive={preserveActiveSelection} allowRetarget={allowProbeDrivenRetarget} prevRes={previousResolution} prevFps={previousFrameRate:0.###}");

                if (preserveActiveSelection)
                {
                    Logger.Log($"Refreshing selected-device capabilities during active capture for {e.DeviceName} (preserveSelection={!allowProbeDrivenRetarget}).");
                }

                _viewModel._suppressFormatChangeReinitialize = preserveActiveSelection;
                try
                {
                    _viewModel.RebuildSelectedDeviceCapabilities(_viewModel.SelectedDevice, resetTelemetryState: false);
                }
                finally
                {
                    _viewModel._suppressFormatChangeReinitialize = false;
                }

                Logger.Log($"Format probe rebuild done: SelectedRes={_viewModel.SelectedResolution} SelectedFormat={_viewModel.SelectedFormat?.Width}x{_viewModel.SelectedFormat?.Height}@{_viewModel.SelectedFormat?.FrameRate:0.###} modeChanged={!string.Equals(previousResolution, _viewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase) || !FrameRateTimingPolicy.IsFrameRateMatch(previousFrameRate, _viewModel.SelectedFrameRate)}");

                var modeChanged = !string.Equals(previousResolution, _viewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase) ||
                                  !FrameRateTimingPolicy.IsFrameRateMatch(previousFrameRate, _viewModel.SelectedFrameRate);

                if (TryApplyDeviceFormatProbeRetarget(
                    target,
                    preserveActiveSelection,
                    allowProbeDrivenRetarget,
                    previousResolution,
                    previousFrameRate,
                    modeChanged))
                {
                    return;
                }
            }))
            {
                Logger.Log($"FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
            }
        }

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
                Logger.Log($"Format probe updated HDR mode set; applying new mode {_viewModel.SelectedResolution}@{_viewModel.SelectedFrameRate:0.###} via device renegotiation.");
                _viewModel.EnqueueUiOperation(
                    () => _viewModel.ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                    retargetDecision.UiOperationName!);
                return true;
            }

            if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.PreserveMjpegHighFrameRate)
            {
                Logger.Log(
                    $"Format probe preserved special MJPG HFR mode at {_viewModel.SelectedResolution}@{_viewModel.SelectedFrameRate:0.###}; " +
                    "skipping SDR NV12 retarget.");
                return true;
            }

            if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.SdrNv12Retarget)
            {
                Logger.Log(
                    $"Format probe detected MJPG-only mode at {_viewModel.SelectedResolution}@{_viewModel.SelectedFrameRate:0.###}; " +
                    $"retargeting SDR to NV12-capable mode {retargetDecision.TargetResolution}@{retargetDecision.TargetFrameRate:0.###}.");

                _viewModel._isRebuildingModeOptions = true;
                _viewModel._isApplyingAutomaticResolutionSelection = true;
                try
                {
                    _viewModel.SelectedResolution = retargetDecision.TargetResolution;
                }
                finally
                {
                    _viewModel._isApplyingAutomaticResolutionSelection = false;
                    _viewModel._isRebuildingModeOptions = false;
                }

                _viewModel._suppressFormatChangeReinitialize = true;
                try
                {
                    _viewModel.RebuildFrameRateOptions();
                }
                finally
                {
                    _viewModel._suppressFormatChangeReinitialize = false;
                }

                _viewModel.EnqueueUiOperation(
                    () => _viewModel.ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                    retargetDecision.UiOperationName!);
                return true;
            }

            if (allowProbeDrivenRetarget && _viewModel.SelectedFormat != null)
            {
                var runtime = _viewModel.GetCaptureRuntimeSnapshot();
                Logger.Log($"Format probe session check: actual={runtime.ActualWidth}x{runtime.ActualHeight} selected={_viewModel.SelectedFormat.Width}x{_viewModel.SelectedFormat.Height}");
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
                        $"selected={_viewModel.SelectedFormat.Width}x{_viewModel.SelectedFormat.Height}; reinitializing.");
                    _viewModel.EnqueueUiOperation(
                        () => _viewModel.ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                        retargetDecision.UiOperationName!);
                    return true;
                }
            }

            if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.RestoreActiveSelection)
            {
                _viewModel._isRebuildingModeOptions = true;
                _viewModel._isApplyingAutomaticResolutionSelection = true;
                try
                {
                    _viewModel.SelectedResolution = previousResolution;
                    _viewModel.SelectedFrameRate = previousFrameRate;
                    _viewModel.UpdateSelectedFormat();
                    _viewModel.UpdateTargetSummary();
                }
                finally
                {
                    _viewModel._isApplyingAutomaticResolutionSelection = false;
                    _viewModel._isRebuildingModeOptions = false;
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
                _viewModel.IsHdrEnabled,
                modeChanged,
                previousResolution,
                previousFrameRate,
                _viewModel.SelectedResolution,
                _viewModel.SelectedFrameRate,
                _viewModel.SelectedFormat,
                target.SupportedFormats,
                !string.IsNullOrWhiteSpace(previousResolution) &&
                    _viewModel.AvailableResolutions.Any(option => string.Equals(option.Value, previousResolution, StringComparison.OrdinalIgnoreCase)),
                includeSessionMismatchCheck,
                sessionActualWidth,
                sessionActualHeight));
    }
}
