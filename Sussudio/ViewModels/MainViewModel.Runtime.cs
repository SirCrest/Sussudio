using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

/// <summary>
/// Runtime status projection, timer updates, and capture-service event handling.
/// </summary>
public partial class MainViewModel
{
    private void SetupTimer()
    {
        _timer = _dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            var runtimeSnapshot = _captureService.GetRuntimeSnapshot();

            if (IsRecording)
            {
                RecordingTime = _recordingStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                UpdateRecordingStats();
            }

            if (!IsRecording && _captureService.IsFlashbackActive)
            {
                UpdateFlashbackBitrate();
            }

            if (IsPreviewing || IsRecording)
            {
                UpdateLiveCaptureInfo(runtimeSnapshot);
            }
            else
            {
                ResetLiveCaptureInfo();
            }

            UpdateDiskSpace();
            RefreshSourceTelemetrySummaryAge();
            UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        };
        _timer.Start();
    }

    private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        IsAudioPreviewActive = runtime.IsAudioPreviewActive;

        var width = runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth;
        var height = runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight;
        LiveResolution = width.HasValue && height.HasValue
            ? $"{width.Value}x{height.Value}"
            : LiveInfoUnavailable;

        var frameRateValue = runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate;
        if (frameRateValue.HasValue && frameRateValue.Value > 0)
        {
            LiveFrameRate = frameRateValue.Value.ToString("0.00");
        }
        else
        {
            LiveFrameRate = LiveInfoUnavailable;
        }

        var pixelFormat =
            runtime.ReaderSourceSubtype ??
            runtime.VideoNegotiatedSubtype ??
            runtime.NegotiatedPixelFormat ??
            runtime.LatestObservedFramePixelFormat ??
            runtime.RequestedReaderSubtype ??
            runtime.RequestedPixelFormat;
        var codecSuffix = _captureService.EncoderCodecName switch
        {
            "hevc_nvenc" => " / HEVC",
            "h264_nvenc" => " / H264",
            "av1_nvenc" => " / AV1",
            _ => ""
        };
        LivePixelFormat = string.IsNullOrWhiteSpace(pixelFormat) ? LiveInfoUnavailable : pixelFormat + codecSuffix;
    }

    private void ResetLiveCaptureInfo()
    {
        IsAudioPreviewActive = false;
        LiveResolution = LiveInfoUnavailable;
        LiveFrameRate = LiveInfoUnavailable;
        LivePixelFormat = LiveInfoUnavailable;
    }

    private void UpdateDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(OutputPath) ?? "C:");
            var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            DiskSpaceInfo = $"Free: {freeGb:F1} GB";
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Suppressed exception in MainViewModel.RefreshDiskSpace: {ex.Message}");
            DiskSpaceInfo = "";
        }
    }

    private void OnCaptureStatusChanged(object? sender, string status)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            var runtimeSnapshot = _captureService.GetRuntimeSnapshot();
            StatusText = status;
            UpdateLiveCaptureInfo(runtimeSnapshot);
            UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        }))
        {
            Logger.Log($"CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        }
    }

    private void OnCaptureError(object? sender, Exception ex)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            var runtimeSnapshot = _captureService.GetRuntimeSnapshot();
            StatusText = $"Error: {ex.Message}";
            IsInitialized = _captureService.IsInitialized;
            IsPreviewing = _captureService.IsVideoPreviewActive;
            IsRecording = _captureService.IsRecording;
            if (!IsPreviewing && !IsRecording)
            {
                ResetAudioMeter();
            }

            UpdateLiveCaptureInfo(runtimeSnapshot);
            UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);

            // AUDCLNT_E_DEVICE_INVALIDATED (0x88890004) arrives when the audio
            // engine is reset independently of a full system suspend — e.g. monitor
            // power-off, USB hot-unplug, or wake events that don't trigger
            // PowerManager.SystemResuming. Trigger a full rebind so the user does
            // not have to manually re-pick the device. The IsRecording guard inside
            // ReinitializeDeviceAsync (fix #1) prevents this path from running
            // mid-recording; EnqueueUiOperation serializes with any in-flight
            // PowerManager-triggered reinit from OnSystemResuming.
            unchecked
            {
                const int AudclntDeviceInvalidated = (int)0x88890004;
                if (ex is COMException comEx &&
                    comEx.HResult == AudclntDeviceInvalidated &&
                    IsPreviewing &&
                    !IsRecording)
                {
                    Logger.Log("AUDCLNT_E_DEVICE_INVALIDATED received — scheduling audio rebind.");
                    EnqueueUiOperation(
                        () => ReinitializeDeviceAsync("audio device invalidated"),
                        "audio device invalidated reinit");
                }
            }
        }))
        {
            Logger.Log($"CAPTURE_ERROR_UI_ENQUEUE_FAILED type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    // PowerModeChanged fires on the system thread pool — must not touch UI properties
    // directly. We act only on PowerModes.Resume; Suspend/StatusChange are ignored
    // (Suspend arrives just before the OS freezes the process so there's nothing
    // useful to do, and StatusChange fires on AC/battery transitions which don't
    // affect capture). All UI-state reads happen inside the EnqueueUiOperation
    // lambda, which executes on the DispatcherQueue thread. ReinitializeDeviceAsync's
    // IsRecording guard (fix #1) keeps this safe to call regardless of state.
    private void OnSystemPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        Logger.Log("SYSTEM_RESUMING_EVENT received — scheduling capture rebind if previewing.");
        EnqueueUiOperation(() =>
        {
            if (!IsPreviewing || !IsInitialized || IsRecording)
            {
                Logger.Log(
                    $"SYSTEM_RESUMING_REINIT_SKIP previewing={IsPreviewing} " +
                    $"initialized={IsInitialized} recording={IsRecording}");
                return Task.CompletedTask;
            }

            Logger.Log("SYSTEM_RESUMING_REINIT_SCHEDULED");
            return ReinitializeDeviceAsync("system resume");
        }, "system resume reinit");
    }

    private void OnCapturePreCleanupRequested()
    {
        // Fires on a background thread before CaptureService.CleanupAsync disposes
        // the shared D3D11 device. Stop the renderer first to prevent the same race
        // as the reinit crash (renderer calling native D3D on a dying device).
        var handlers = PreviewRendererStopRequested;
        if (handlers != null)
        {
            foreach (Func<Task> handler in handlers.GetInvocationList())
            {
                try { handler().GetAwaiter().GetResult(); }
                catch (Exception ex) { Logger.Log($"PreCleanup renderer stop warning: {ex.Message}"); }
            }
        }
    }

    private void OnFrameCaptured(object? sender, ulong frameCount)
    {
        // Could update frame count display if needed
    }

    partial void OnIsRecordingChanged(bool value)
    {
        if (!value)
        {
            ResetAudioMeter();
            RecordingSizeInfo = "--";
            RecordingBitrateInfo = "--";
            _bitrateSamples.Clear();

            if (_pendingModeOptionsRefresh)
            {
                _pendingModeOptionsRefresh = false;
                RebuildResolutionOptions();
            }
        }
    }

    partial void OnIsPreviewingChanged(bool value)
    {
        if (!value && !IsRecording)
        {
            ResetLiveCaptureInfo();
        }
    }

    private void UpdateRecordingStats()
    {
        var stats = _captureService.GetRecordingStats();
        var totalBytes = stats.TotalBytes;
        RecordingSizeInfo = DisplayFormatters.FormatBytes(totalBytes, "0");

        var now = Environment.TickCount64;
        _bitrateSamples.Enqueue((now, totalBytes));
        while (_bitrateSamples.Count > 0 && now - _bitrateSamples.Peek().Tick > BitrateWindowMs)
        {
            _bitrateSamples.Dequeue();
        }

        var smoothed = ComputeAverageBitrate(_bitrateSamples);
        RecordingBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : "--";
    }

    private static double? ComputeAverageBitrate(Queue<(long Tick, long Bytes)> samples)
    {
        if (samples.Count < 2)
        {
            return null;
        }

        var first = samples.Peek();
        var last = samples.Last();
        var deltaMs = last.Tick - first.Tick;
        if (deltaMs <= 0)
        {
            return null;
        }

        var deltaBytes = Math.Max(0, last.Bytes - first.Bytes);
        return (deltaBytes * 8.0) / (deltaMs / 1000.0);
    }
}
