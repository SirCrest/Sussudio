using System;
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

    private sealed class MainViewModelDeviceFormatProbeControllerContext
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
}
