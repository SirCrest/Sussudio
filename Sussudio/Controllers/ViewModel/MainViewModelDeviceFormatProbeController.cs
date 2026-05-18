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
        private readonly MainViewModelDeviceFormatProbeRetargetApplier _retargetApplier;

        public MainViewModelDeviceFormatProbeController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _retargetApplier = new MainViewModelDeviceFormatProbeRetargetApplier(_viewModel);
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
}
