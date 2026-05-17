using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio;

// XAML-facing adapter for bottom status-strip text. The controller owns the
// concrete control projection so the root property dispatcher stays declarative.
public sealed partial class MainWindow
{
    private WindowTitleController _windowTitleController = null!;
    private StatusStripPresentationController _statusStripPresentationController = null!;

    private void InitializeWindowTitleController()
        => _windowTitleController = new WindowTitleController();

    private void InitializeStatusStripPresentationController()
    {
        _statusStripPresentationController = new StatusStripPresentationController(new StatusStripPresentationControllerContext
        {
            DiskWarningInfoBar = DiskWarningInfoBar,
            StatusTextBlock = StatusTextBlock,
            RecordingTimeTextBlock = RecordingTimeTextBlock,
            DiskSpaceTextBlock = DiskSpaceTextBlock,
            RecordingSizeTextBlock = RecordingSizeTextBlock,
            RecordingBitrateTextBlock = RecordingBitrateTextBlock,
        });
    }

    private void ApplyInitialStatusStripPresentation()
        => _statusStripPresentationController.ApplyInitial(BuildStatusStripPresentationSnapshot());

    private bool TryHandleStatusStripPropertyChanged(string? propertyName)
        => _statusStripPresentationController.TryHandlePropertyChanged(
            propertyName,
            BuildStatusStripPresentationSnapshot(),
            ApplyWindowTitle);

    private StatusStripPresentationSnapshot BuildStatusStripPresentationSnapshot()
        => new(
            ViewModel.StatusText,
            ViewModel.RecordingTime,
            ViewModel.DiskSpaceInfo,
            ViewModel.RecordingSizeInfo,
            ViewModel.RecordingBitrateInfo,
            ViewModel.FlashbackBitrateInfo,
            ViewModel.IsDiskWarningActive,
            ViewModel.IsRecording,
            ViewModel.IsFlashbackEnabled);

    private void UpdateStatusTextPresentation()
        => _statusStripPresentationController.UpdateStatusText(ViewModel.StatusText);

    private void UpdateRecordingTimePresentation()
        => _statusStripPresentationController.UpdateRecordingTime(ViewModel.RecordingTime);

    private void UpdateDiskSpacePresentation()
        => _statusStripPresentationController.UpdateDiskSpace(ViewModel.DiskSpaceInfo);

    private void UpdateRecordingSizePresentation()
        => _statusStripPresentationController.UpdateRecordingSize(ViewModel.RecordingSizeInfo);

    private void UpdateRecordingBitratePresentation()
        => _statusStripPresentationController.UpdateRecordingBitrate(ViewModel.RecordingBitrateInfo);

    private void UpdateFlashbackBitratePresentation()
        => _statusStripPresentationController.UpdateFlashbackBitrate(
            ViewModel.FlashbackBitrateInfo,
            ViewModel.IsRecording,
            ViewModel.IsFlashbackEnabled);

    private void UpdateDiskWarningPresentation()
        => _statusStripPresentationController.UpdateDiskWarning(ViewModel.IsDiskWarningActive);

    private void ApplyWindowTitle()
        => Title = _windowTitleController.BuildTitle(ViewModel.IsRecording, ViewModel.RecordingTime);
}
