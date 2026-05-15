using Sussudio.ViewModels;

namespace Sussudio;

// Flashback-specific ViewModel property projections: timeline lockout,
// playback marker movement, export progress, and settings-control sync.
public sealed partial class MainWindow
{
    private bool TryHandleFlashbackPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsFlashbackTimelineVisible):
                HandleFlashbackTimelineVisibleChanged();
                return true;

            case nameof(MainViewModel.IsFlashbackEnabled):
                HandleFlashbackEnabledChanged();
                return true;

            case nameof(MainViewModel.FlashbackState):
                HandleFlashbackStateChanged();
                return true;

            case nameof(MainViewModel.FlashbackBufferFillPercent):
            case nameof(MainViewModel.FlashbackBufferDiskBytes):
                HandleFlashbackBufferChanged();
                return true;

            case nameof(MainViewModel.FlashbackBitrateInfo):
                HandleFlashbackBitrateChanged();
                return true;

            case nameof(MainViewModel.FlashbackPlaybackPosition):
                HandleFlashbackPlaybackPositionChanged();
                return true;

            case nameof(MainViewModel.FlashbackInPoint):
            case nameof(MainViewModel.FlashbackOutPoint):
                HandleFlashbackRangeChanged();
                return true;

            case nameof(MainViewModel.FlashbackExportProgress):
                HandleFlashbackExportProgressChanged();
                return true;

            case nameof(MainViewModel.IsFlashbackExporting):
                HandleFlashbackExportingChanged();
                return true;

            case nameof(MainViewModel.FlashbackGpuDecode):
                HandleFlashbackGpuDecodeChanged();
                return true;

            case nameof(MainViewModel.FlashbackBufferMinutes):
                HandleFlashbackBufferMinutesChanged();
                return true;

            default:
                return false;
        }
    }

    private void HandleFlashbackTimelineVisibleChanged()
    {
        ApplyFlashbackTimelineVisibility(ViewModel.IsFlashbackTimelineVisible);
    }

    private void HandleFlashbackEnabledChanged()
    {
        ApplyFlashbackTimelineLockout();
    }

    private void HandleFlashbackStateChanged()
    {
        UpdateFlashbackStateUI();
    }

    private void HandleFlashbackBufferChanged()
    {
        UpdateFlashbackBufferFill();
        UpdateFlashbackPositionUI(); // Recalculate playhead fraction as buffer grows.
        UpdateFlashbackMarkers();    // Recalculate in/out positions too.
    }

    private void HandleFlashbackBitrateChanged()
    {
        UpdateFlashbackBitratePresentation();
    }

    private void HandleFlashbackPlaybackPositionChanged()
    {
        UpdateFlashbackPositionUI();
    }

    private void HandleFlashbackRangeChanged()
    {
        UpdateFlashbackMarkers();
    }

    private void HandleFlashbackExportProgressChanged()
    {
        UpdateFlashbackExportProgress(ViewModel.FlashbackExportProgress);
    }

    private void HandleFlashbackExportingChanged()
    {
        UpdateFlashbackExportingPresentation(ViewModel.IsFlashbackExporting);
    }

    private void HandleFlashbackGpuDecodeChanged()
        => SyncFlashbackGpuDecodeSetting();

    private void HandleFlashbackBufferMinutesChanged()
        => SyncFlashbackBufferDurationSetting();
}
