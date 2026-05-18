using Sussudio.Controllers;

namespace Sussudio;

// XAML/MainWindow adapter for Flashback property-change routing. The route
// table lives in FlashbackPropertyChangedController.
public sealed partial class MainWindow
{
    private FlashbackPropertyChangedController _flashbackPropertyChangedController = null!;

    private void InitializeFlashbackPropertyChangedController()
    {
        _flashbackPropertyChangedController = new FlashbackPropertyChangedController(new FlashbackPropertyChangedControllerContext
        {
            IsTimelineVisible = () => ViewModel.IsFlashbackTimelineVisible,
            GetExportProgress = () => ViewModel.FlashbackExportProgress,
            IsExporting = () => ViewModel.IsFlashbackExporting,
            ApplyTimelineVisibility = ApplyFlashbackTimelineVisibility,
            ApplyTimelineLockout = ApplyFlashbackTimelineLockout,
            UpdateState = UpdateFlashbackStateUI,
            UpdateBuffer = UpdateFlashbackBufferPresentation,
            UpdatePlaybackPosition = UpdateFlashbackPositionUI,
            UpdateRangeMarkers = UpdateFlashbackMarkers,
            UpdateExportProgress = UpdateFlashbackExportProgress,
            UpdateExportingPresentation = UpdateFlashbackExportingPresentation,
            SyncGpuDecodeSetting = SyncFlashbackGpuDecodeSetting,
            SyncBufferDurationSetting = SyncFlashbackBufferDurationSetting
        });
    }

    private bool TryHandleFlashbackPropertyChanged(string propertyName)
        => _flashbackPropertyChangedController.TryHandlePropertyChanged(propertyName);
}
