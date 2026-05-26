using System;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the late device-format probe controller.
/// </summary>
internal sealed class MainViewModelDeviceFormatProbeControllerContext
{
    public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
    public required Func<long> ReadDeviceScanGeneration { get; init; }
    public required Func<string, CaptureDevice?> FindDeviceById { get; init; }
    public required Action<bool> SetPendingSdrAutoSelectionForDeviceChange { get; init; }
    public required Action<int?> SetPendingSdrAutoFriendlyFrameRateBucket { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Func<double> GetSelectedFrameRate { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
    public required Action<CaptureDevice, bool> RebuildSelectedDeviceCapabilities { get; init; }
    public required Func<MainViewModelDeviceFormatProbeRetargetApplier> CreateRetargetApplier { get; init; }
}

/// <summary>
/// Owns late device-format probe reconciliation for the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelDeviceFormatProbeController
{
    private readonly MainViewModelDeviceFormatProbeControllerContext _context;
    private readonly MainViewModelDeviceFormatProbeRetargetApplier _retargetApplier;

    public MainViewModelDeviceFormatProbeController(MainViewModelDeviceFormatProbeControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _retargetApplier = _context.CreateRetargetApplier();
    }

    public void OnDeviceFormatProbeCompleted(object? sender, DeviceService.DeviceFormatProbeCompletedEventArgs e)
    {
        if (!_context.TryEnqueueOnUiThread(() =>
        {
            if (e.RequestId != _context.ReadDeviceScanGeneration())
            {
                return;
            }

            var target = _context.FindDeviceById(e.DeviceId);
            if (target == null)
            {
                return;
            }

            if (!e.Succeeded)
            {
                _context.SetPendingSdrAutoSelectionForDeviceChange(false);
                _context.SetPendingSdrAutoFriendlyFrameRateBucket(null);
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

            var selectedDevice = _context.GetSelectedDevice();
            if (selectedDevice == null ||
                !string.Equals(selectedDevice.Id, target.Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var isPreviewing = _context.IsPreviewing();
            var isRecording = _context.IsRecording();
            var preserveActiveSelection = isPreviewing || isRecording;
            var allowProbeDrivenRetarget = isPreviewing && _context.IsInitialized() && !isRecording;
            var previousResolution = _context.GetSelectedResolution();
            var previousFrameRate = _context.GetSelectedFrameRate();
            Logger.Log($"Format probe completed for {e.DeviceName}: formats={e.Formats.Count} preserveActive={preserveActiveSelection} allowRetarget={allowProbeDrivenRetarget} prevRes={previousResolution} prevFps={previousFrameRate:0.###}");

            if (preserveActiveSelection)
            {
                Logger.Log($"Refreshing selected-device capabilities during active capture for {e.DeviceName} (preserveSelection={!allowProbeDrivenRetarget}).");
            }

            _context.SetSuppressFormatChangeReinitialize(preserveActiveSelection);
            try
            {
                _context.RebuildSelectedDeviceCapabilities(selectedDevice, false);
            }
            finally
            {
                _context.SetSuppressFormatChangeReinitialize(false);
            }

            var selectedResolution = _context.GetSelectedResolution();
            var selectedFrameRate = _context.GetSelectedFrameRate();
            var selectedFormat = _context.GetSelectedFormat();
            Logger.Log($"Format probe rebuild done: SelectedRes={selectedResolution} SelectedFormat={selectedFormat?.Width}x{selectedFormat?.Height}@{selectedFormat?.FrameRate:0.###} modeChanged={!string.Equals(previousResolution, selectedResolution, StringComparison.OrdinalIgnoreCase) || !FrameRateTimingPolicy.IsFrameRateMatch(previousFrameRate, selectedFrameRate)}");

            var modeChanged = !string.Equals(previousResolution, selectedResolution, StringComparison.OrdinalIgnoreCase) ||
                              !FrameRateTimingPolicy.IsFrameRateMatch(previousFrameRate, selectedFrameRate);

            if (_retargetApplier.TryApplyDeviceFormatProbeRetarget(
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
}

/// <summary>
/// Graph-built ports consumed by late device-format probe retarget application.
/// </summary>
internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext
{
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Action<string?> SetSelectedResolution { get; init; }
    public required Func<double> GetSelectedFrameRate { get; init; }
    public required Action<double> SetSelectedFrameRate { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required Func<string, bool> AvailableResolutionsContains { get; init; }
    public required Action<bool> SetIsRebuildingModeOptions { get; init; }
    public required Action<bool> SetIsApplyingAutomaticResolutionSelection { get; init; }
    public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
    public required Action RebuildFrameRateOptions { get; init; }
    public required Func<string, Task> ReinitializeDeviceAsync { get; init; }
    public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }
    public required Func<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshot { get; init; }
    public required Action UpdateSelectedFormat { get; init; }
    public required Action UpdateTargetSummary { get; init; }
}

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
