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
}
