using System;

namespace Sussudio.ViewModels;

/// <summary>
/// Runtime timer updates for periodic UI refreshes.
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
}
