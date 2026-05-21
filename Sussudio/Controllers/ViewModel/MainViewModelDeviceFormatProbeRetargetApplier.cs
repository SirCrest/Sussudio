using System;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

/// <summary>
/// Applies late device-format probe retarget decisions to the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelDeviceFormatProbeRetargetApplier
{
    private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;

    public MainViewModelDeviceFormatProbeRetargetApplier(MainViewModelDeviceFormatProbeRetargetApplierContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
            Logger.Log($"Format probe updated HDR mode set; applying new mode {_context.GetSelectedResolution()}@{_context.GetSelectedFrameRate():0.###} via device renegotiation.");
            _context.EnqueueUiOperation(
                () => _context.ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                retargetDecision.UiOperationName!);
            return true;
        }

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.PreserveMjpegHighFrameRate)
        {
            Logger.Log(
                $"Format probe preserved special MJPG HFR mode at {_context.GetSelectedResolution()}@{_context.GetSelectedFrameRate():0.###}; " +
                "skipping SDR NV12 retarget.");
            return true;
        }

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.SdrNv12Retarget)
        {
            Logger.Log(
                $"Format probe detected MJPG-only mode at {_context.GetSelectedResolution()}@{_context.GetSelectedFrameRate():0.###}; " +
                $"retargeting SDR to NV12-capable mode {retargetDecision.TargetResolution}@{retargetDecision.TargetFrameRate:0.###}.");

            _context.SetIsRebuildingModeOptions(true);
            _context.SetIsApplyingAutomaticResolutionSelection(true);
            try
            {
                _context.SetSelectedResolution(retargetDecision.TargetResolution);
            }
            finally
            {
                _context.SetIsApplyingAutomaticResolutionSelection(false);
                _context.SetIsRebuildingModeOptions(false);
            }

            _context.SetSuppressFormatChangeReinitialize(true);
            try
            {
                _context.RebuildFrameRateOptions();
            }
            finally
            {
                _context.SetSuppressFormatChangeReinitialize(false);
            }

            _context.EnqueueUiOperation(
                () => _context.ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                retargetDecision.UiOperationName!);
            return true;
        }

        if (allowProbeDrivenRetarget && _context.GetSelectedFormat() != null)
        {
            var runtime = _context.GetCaptureRuntimeSnapshot();
            var selectedFormat = _context.GetSelectedFormat();
            Logger.Log($"Format probe session check: actual={runtime.ActualWidth}x{runtime.ActualHeight} selected={selectedFormat!.Width}x{selectedFormat.Height}");
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
                    $"selected={selectedFormat.Width}x{selectedFormat.Height}; reinitializing.");
                _context.EnqueueUiOperation(
                    () => _context.ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                    retargetDecision.UiOperationName!);
                return true;
            }
        }

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.RestoreActiveSelection)
        {
            _context.SetIsRebuildingModeOptions(true);
            _context.SetIsApplyingAutomaticResolutionSelection(true);
            try
            {
                _context.SetSelectedResolution(previousResolution);
                _context.SetSelectedFrameRate(previousFrameRate);
                _context.UpdateSelectedFormat();
                _context.UpdateTargetSummary();
            }
            finally
            {
                _context.SetIsApplyingAutomaticResolutionSelection(false);
                _context.SetIsRebuildingModeOptions(false);
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
            _context.IsHdrEnabled(),
            modeChanged,
            previousResolution,
            previousFrameRate,
            _context.GetSelectedResolution(),
            _context.GetSelectedFrameRate(),
            _context.GetSelectedFormat(),
            target.SupportedFormats,
            !string.IsNullOrWhiteSpace(previousResolution) &&
                _context.AvailableResolutionsContains(previousResolution),
            includeSessionMismatchCheck,
            sessionActualWidth,
            sessionActualHeight));
}
