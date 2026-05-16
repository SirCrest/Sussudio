using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Sussudio.ViewModels;

/// <summary>
/// Runtime timer updates, disk-space projection, and power-resume handling.
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

    partial void OnIsPreviewingChanged(bool value)
    {
        if (!value && !IsRecording)
        {
            ResetLiveCaptureInfo();
        }
    }
}
