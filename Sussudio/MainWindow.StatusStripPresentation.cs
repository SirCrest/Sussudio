using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio;

// XAML-facing adapter for shell status presentation. The controllers own concrete
// control projection so the root property dispatcher stays declarative.
public sealed partial class MainWindow
{
    private LiveSignalInfoController _liveSignalInfoController = null!;
    private WindowTitleController _windowTitleController = null!;
    private StatusStripPresentationController _statusStripPresentationController = null!;

    private void InitializeLiveSignalInfoController()
    {
        _liveSignalInfoController = new LiveSignalInfoController(new LiveSignalInfoControllerContext
        {
            DispatcherQueue = DispatcherQueue,
            LiveSignalInfoPanel = LiveSignalInfoPanel,
            LiveSignalInfoScale = LiveSignalInfoScale,
            LiveResolutionTextBlock = LiveResolutionTextBlock,
            LiveFrameRateTextBlock = LiveFrameRateTextBlock,
            LivePixelFormatTextBlock = LivePixelFormatTextBlock,
        });
    }

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

    private void UpdateLiveSignalInfoVisibility()
        => _liveSignalInfoController.Update(
            ViewModel.LiveResolution,
            ViewModel.LiveFrameRate,
            ViewModel.LivePixelFormat);

    private void StopLiveSignalInfoTimers()
        => _liveSignalInfoController.StopTimers();

    private bool TryHandleLiveSignalPropertyChanged(string propertyName)
        => _liveSignalInfoController.TryHandlePropertyChanged(
            propertyName,
            ViewModel.LiveResolution,
            ViewModel.LiveFrameRate,
            ViewModel.LivePixelFormat);

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

    private void UpdateDiskWarningPresentation()
        => _statusStripPresentationController.UpdateDiskWarning(ViewModel.IsDiskWarningActive);

    private void ApplyWindowTitle()
        => Title = _windowTitleController.BuildTitle(ViewModel.IsRecording, ViewModel.RecordingTime);
}
