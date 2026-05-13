using Microsoft.UI.Xaml;
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
        if (!ViewModel.IsRecording && ViewModel.IsFlashbackEnabled)
        {
            RecordingBitrateTextBlock.Text = ViewModel.FlashbackBitrateInfo;
        }
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
        FlashbackExportProgressBar.Value = ViewModel.FlashbackExportProgress;
    }

    private void HandleFlashbackExportingChanged()
    {
        FlashbackExportProgressBar.Visibility = ViewModel.IsFlashbackExporting
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (!ViewModel.IsFlashbackExporting)
        {
            FlashbackExportProgressBar.Value = 0;
        }
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
