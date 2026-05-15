using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class StatusStripPresentationControllerContext
{
    public required InfoBar DiskWarningInfoBar { get; init; }
    public required TextBlock StatusTextBlock { get; init; }
    public required TextBlock RecordingTimeTextBlock { get; init; }
    public required TextBlock DiskSpaceTextBlock { get; init; }
    public required TextBlock RecordingSizeTextBlock { get; init; }
    public required TextBlock RecordingBitrateTextBlock { get; init; }
}

internal readonly record struct StatusStripPresentationSnapshot(
    string StatusText,
    string RecordingTime,
    string DiskSpaceInfo,
    string RecordingSizeInfo,
    string RecordingBitrateInfo,
    bool IsDiskWarningActive);

internal sealed class StatusStripPresentationController
{
    private readonly StatusStripPresentationControllerContext _context;

    public StatusStripPresentationController(StatusStripPresentationControllerContext context)
    {
        _context = context;
    }

    public void ApplyInitial(StatusStripPresentationSnapshot snapshot)
    {
        UpdateStatusText(snapshot.StatusText);
        UpdateRecordingTime(snapshot.RecordingTime);
        UpdateDiskSpace(snapshot.DiskSpaceInfo);
        UpdateRecordingSize(snapshot.RecordingSizeInfo);
        UpdateRecordingBitrate(snapshot.RecordingBitrateInfo);
        UpdateDiskWarning(snapshot.IsDiskWarningActive);
    }

    public void UpdateStatusText(string statusText)
    {
        _context.StatusTextBlock.Text = statusText;
    }

    public void UpdateRecordingTime(string recordingTime)
    {
        _context.RecordingTimeTextBlock.Text = recordingTime;
    }

    public void UpdateDiskSpace(string diskSpaceInfo)
    {
        _context.DiskSpaceTextBlock.Text = diskSpaceInfo;
    }

    public void UpdateRecordingSize(string recordingSizeInfo)
    {
        _context.RecordingSizeTextBlock.Text = recordingSizeInfo;
    }

    public void UpdateRecordingBitrate(string recordingBitrateInfo)
    {
        _context.RecordingBitrateTextBlock.Text = recordingBitrateInfo;
    }

    public void UpdateFlashbackBitrate(string flashbackBitrateInfo, bool isRecording, bool isFlashbackEnabled)
    {
        if (!isRecording && isFlashbackEnabled)
        {
            _context.RecordingBitrateTextBlock.Text = flashbackBitrateInfo;
        }
    }

    public void UpdateDiskWarning(bool isDiskWarningActive)
    {
        _context.DiskWarningInfoBar.IsOpen = isDiskWarningActive;
    }
}
