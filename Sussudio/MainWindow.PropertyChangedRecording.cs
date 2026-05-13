using System;
using Microsoft.UI.Xaml;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio;

// Recording-specific ViewModel property projections: record-button state,
// recording glow, and controls that must lock while recording transitions run.
public sealed partial class MainWindow
{
    private void HandleRecordingChanged()
    {
        if (ViewModel.IsRecording)
        {
            RecordingGlowBorder.Opacity = 1.0;
            RecordingGlowPulseStoryboard.Begin();
        }
        else
        {
            RecordingGlowPulseStoryboard.Stop();
            RecordingGlowBorder.Opacity = 0;
            ResetAudioMeterVisuals();
        }

        // Three-state button: hide spinner, show correct content, animated morph.
        RecordButtonStartingContent.IsActive = false;
        RecordButtonStartingContent.Visibility = Visibility.Collapsed;
        if (ViewModel.IsRecording)
        {
            // Circle -> pill: show recording content, measure target, animate.
            RecordButtonNormalContent.Visibility = Visibility.Collapsed;
            RecordButtonRecordingContent.Visibility = Visibility.Visible;
            RecordButton.Padding = new Thickness(12, 0, 12, 0);
            RecordButton.Width = double.NaN;
            RecordButton.UpdateLayout();
            var targetWidth = RecordButton.ActualWidth;
            RecordButton.Width = 36;
            AnimateRecordButtonWidth(36, targetWidth);
        }
        else
        {
            // Pill -> circle: capture current width, animate to 36, swap content on completion.
            var currentWidth = RecordButton.ActualWidth;
            RecordButton.Width = currentWidth;
            AnimateRecordButtonWidth(currentWidth, 36, () =>
            {
                RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
                RecordButtonNormalContent.Visibility = Visibility.Visible;
                RecordButton.Padding = new Thickness(0);
            });
        }

        AudioRecordToggle.IsEnabled = !ViewModel.IsRecording;
        CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
        MicrophoneToggle.IsEnabled = !ViewModel.IsRecording;
        AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
        MicrophoneComboBox.IsEnabled = ViewModel.IsMicrophoneEnabled && !ViewModel.IsRecording;
        DeviceAudioModeToggle.IsEnabled = ViewModel.IsDeviceAudioControlSupported && !ViewModel.IsRecording;
        AnalogAudioGainSlider.IsEnabled = ViewModel.IsDeviceAudioControlSupported &&
                                          string.Equals(ViewModel.SelectedDeviceAudioMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase) &&
                                          !ViewModel.IsRecording;
        ApplyHdrToggleEnabledState();
        // Stats panel always visible - shows "--" when not recording.
        RefreshHdrHintText();
        UpdateDeviceApplyButtonState();
        if (ViewModel.IsRecording)
        {
            RecPulseStoryboard.Begin();
        }
        else
        {
            RecPulseStoryboard.Stop();
        }

        ApplyWindowTitle();
    }

    private void HandleRecordingTransitioningChanged()
    {
        RecordButton.IsEnabled = !ViewModel.IsRecordingTransitioning;
        if (ViewModel.IsRecordingTransitioning)
        {
            if (ViewModel.IsRecording)
            {
                // Stopping: freeze pill width so it doesn't collapse when content hides.
                RecordButton.Width = RecordButton.ActualWidth;
                RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Starting: hide idle dot, show spinner in circle.
                RecordButtonNormalContent.Visibility = Visibility.Collapsed;
            }

            RecordButtonStartingContent.IsActive = true;
            RecordButtonStartingContent.Visibility = Visibility.Visible;
        }
        else
        {
            RecordButtonStartingContent.IsActive = false;
            RecordButtonStartingContent.Visibility = Visibility.Collapsed;
            RecordButtonNormalContent.Visibility = ViewModel.IsRecording ? Visibility.Collapsed : Visibility.Visible;
            RecordButtonRecordingContent.Visibility = ViewModel.IsRecording ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void HandleFfmpegMissingChanged()
    {
        RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing && !ViewModel.IsRecordingTransitioning;
    }
}
