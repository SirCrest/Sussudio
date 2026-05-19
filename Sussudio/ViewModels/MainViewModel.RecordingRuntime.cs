using System;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

/// <summary>
/// Recording runtime presentation updates driven by the periodic UI timer.
/// </summary>
public partial class MainViewModel
{
    partial void OnIsRecordingChanged(bool value)
    {
        if (!value)
        {
            ResetAudioMeter();
            RecordingSizeInfo = "--";
            RecordingBitrateInfo = "--";
            _recordingBitrateSamples.Clear();

            if (_pendingModeOptionsRefresh)
            {
                _pendingModeOptionsRefresh = false;
                RebuildResolutionOptions();
            }
        }
    }

    private void UpdateRecordingStats()
    {
        var stats = _captureService.GetRecordingStats();
        var totalBytes = stats.TotalBytes;
        RecordingSizeInfo = DisplayFormatters.FormatBytes(totalBytes, "0");

        var now = Environment.TickCount64;
        var smoothed = _recordingBitrateSamples.AddSampleAndCompute(now, totalBytes);
        RecordingBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : "--";
    }

    private void UpdateDiskSpace()
    {
        DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);
    }
}
