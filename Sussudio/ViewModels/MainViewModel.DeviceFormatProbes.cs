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

            if (allowProbeDrivenRetarget &&
                IsHdrEnabled &&
                modeChanged)
            {
                Logger.Log($"Format probe updated HDR mode set; applying new mode {SelectedResolution}@{SelectedFrameRate:0.###} via device renegotiation.");
                EnqueueUiOperation(() => ReinitializeDeviceAsync("format probe (HDR retarget)"), "format probe hdr retarget");
                return;
            }

            if (allowProbeDrivenRetarget &&
                !IsHdrEnabled &&
                SelectedFormat?.PixelFormat.Equals("MJPG", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (ShouldPreserveMjpegHighFrameRateMode(SelectedFormat))
                {
                    Logger.Log(
                        $"Format probe preserved special MJPG HFR mode at {SelectedResolution}@{SelectedFrameRate:0.###}; " +
                        "skipping SDR NV12 retarget.");
                    return;
                }

                var preferredRate = previousFrameRate > 0 ? previousFrameRate : SelectedFrameRate;
                var preferredBucket = GetFriendlyFrameRateBucket(preferredRate);
                var nv12Candidates = target.SupportedFormats
                    .Where(format => format.PixelFormat.Equals("NV12", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                MediaFormat? selectedNv12 = nv12Candidates
                    .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredBucket)
                    .OrderByDescending(format => (long)format.Width * format.Height)
                    .FirstOrDefault();

                selectedNv12 ??= nv12Candidates
                    .OrderBy(format => Math.Abs(format.FrameRateExact - preferredRate))
                    .ThenByDescending(format => (long)format.Width * format.Height)
                    .FirstOrDefault();

                if (selectedNv12 != null)
                {
                    var targetResolution = GetResolutionKey(selectedNv12.Width, selectedNv12.Height);
                    if (!string.Equals(targetResolution, SelectedResolution, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Log(
                            $"Format probe detected MJPG-only mode at {SelectedResolution}@{SelectedFrameRate:0.###}; " +
                            $"retargeting SDR to NV12-capable mode {targetResolution}@{selectedNv12.FrameRateExact:0.###}.");

                        _isRebuildingModeOptions = true;
                        _isApplyingAutomaticResolutionSelection = true;
                        try
                        {
                            SelectedResolution = targetResolution;
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
                        EnqueueUiOperation(() => ReinitializeDeviceAsync("format probe (SDR nv12 retarget)"), "format probe sdr retarget");
                        return;
                    }
                }
            }

            // After probes complete, compare the live session negotiated resolution against
            // the now-resolved SelectedFormat. This catches the startup case where preview began
            // with an incomplete format list (probes not yet done) and therefore initialized at
            // a lower resolution than the user saved selection.
            if (allowProbeDrivenRetarget && SelectedFormat != null)
            {
                var runtime = GetCaptureRuntimeSnapshot();
                Logger.Log($"Format probe session check: actual={runtime.ActualWidth}x{runtime.ActualHeight} selected={SelectedFormat.Width}x{SelectedFormat.Height}");
                if (runtime.ActualWidth == null || runtime.ActualHeight == null)
                {
                    Logger.Log("Format probe session mismatch check skipped: runtime width/height not yet available.");
                }
                else if (runtime.ActualWidth != SelectedFormat.Width || runtime.ActualHeight != SelectedFormat.Height)
                {
                    Logger.Log(
                        $"Format probe detected session/format mismatch: " +
                        $"session={runtime.ActualWidth}x{runtime.ActualHeight} " +
                        $"selected={SelectedFormat.Width}x{SelectedFormat.Height}; reinitializing.");
                    EnqueueUiOperation(
                        () => ReinitializeDeviceAsync("format probe (session mismatch)"),
                        "format probe session mismatch");
                    return;
                }
            }

            if (preserveActiveSelection &&
                !allowProbeDrivenRetarget &&
                modeChanged &&
                !string.IsNullOrWhiteSpace(previousResolution) &&
                AvailableResolutions.Any(option => string.Equals(option.Value, previousResolution, StringComparison.OrdinalIgnoreCase)))
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
