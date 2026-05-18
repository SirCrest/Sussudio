using System;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Applies late device-format probe retarget decisions to the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelDeviceFormatProbeRetargetApplier
    {
        private readonly MainViewModel _viewModel;

        public MainViewModelDeviceFormatProbeRetargetApplier(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public bool TryApplyDeviceFormatProbeRetarget(
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
