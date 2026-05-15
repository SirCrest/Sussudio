using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio;

// Flashback-specific ViewModel property projections: timeline lockout,
// playback marker movement, export progress, and settings-control sync.
public sealed partial class MainWindow
{
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
    {
        if (FlashbackGpuDecodeToggle.IsOn != ViewModel.FlashbackGpuDecode)
        {
            FlashbackGpuDecodeToggle.IsOn = ViewModel.FlashbackGpuDecode;
        }
    }

    private void HandleFlashbackBufferMinutesChanged()
    {
        if (FlashbackBufferDurationCombo.SelectedItem is not ComboBoxItem current ||
            current.Tag is not string currentTag ||
            currentTag != ViewModel.FlashbackBufferMinutes.ToString())
        {
            foreach (ComboBoxItem item in FlashbackBufferDurationCombo.Items)
            {
                if (item.Tag is string tag && tag == ViewModel.FlashbackBufferMinutes.ToString())
                {
                    FlashbackBufferDurationCombo.SelectedItem = item;
                    break;
                }
            }
        }
    }
}
