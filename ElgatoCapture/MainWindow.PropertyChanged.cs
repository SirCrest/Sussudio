using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using ElgatoCapture.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;

namespace ElgatoCapture;

public sealed partial class MainWindow
{
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(
            () => HandleViewModelPropertyChangedAsync(e),
            $"ViewModel_PropertyChanged:{e.PropertyName}");
    }
    private void ViewModel_PreviewStartRequested(object? sender, EventArgs e)
    {
        _previewStopRequestedByUser = false;
    }
    private async Task ViewModel_PreviewReinitRequested(string reason)
    {
        if (!ViewModel.IsPreviewing)
        {
            return;
        }

        _isPreviewReinitAnimating = true;
        Logger.Log($"PREVIEW_REINIT_ANIMATE_OUT reason={reason}");
        await AnimatePreviewOutAsync();
    }
    private void ViewModel_PreviewStopRequested(object? sender, EventArgs e)
    {
        _previewStopRequestedByUser = _previewStopRequestedByUser || !ViewModel.IsPreviewReinitializing;
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
    }
    private async Task HandleViewModelPropertyChangedAsync(System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsPreviewing):
                if (ViewModel.IsPreviewing)
                {
                    _previewStopRequestedByUser = false;
                    if (string.IsNullOrWhiteSpace(_previewStartupAttemptId) ||
                        IsPreviewStartupFailedState(_previewStartupState) ||
                        _previewStartupState == PreviewStartupState.Idle)
                    {
                        BeginPreviewStartupAttempt();
                    }

                    SetPreviewStartupState(PreviewStartupState.StartingSession);
                    Logger.Log($"PREVIEW_SESSION_STARTED attempt={_previewStartupAttemptId ?? "none"}");
                    if (!ViewModel.IsPreviewReinitializing && !_isPreviewReinitAnimating)
                    {
                        FadeOutElement(NoDevicePlaceholder);
                        StartPreviewStartupOverlay();
                        PreviewContentGrid.Opacity = 0.0;
                        PreviewContentScale.ScaleX = 0.97;
                        PreviewContentScale.ScaleY = 0.97;
                    }
                    SetPreviewStartupState(PreviewStartupState.RendererAttaching);
                    try
                    {
                        await StartPreviewRendererAsync();
                    }
                    catch (Exception ex)
                    {
                        var attachFailureReason = $"renderer-attach-failed:{ex.Message}";
                        SetPreviewStartupState(PreviewStartupState.Failed, attachFailureReason);
                        StopPreviewStartupWatchdog();
                        StopPreviewStartupOverlay();
                        ResetPreviewContentTransform();
                        FadeInElement(NoDevicePlaceholder);
                        Logger.Log($"PREVIEW_RENDERER_ATTACH_FAILED attempt={_previewStartupAttemptId ?? "none"} reason={attachFailureReason}");
                        SchedulePreviewStartupFailureStop(attachFailureReason);
                        throw;
                    }
                    if (!_previewFirstVisualConfirmed)
                    {
                        SetPreviewStartupState(PreviewStartupState.WaitingForFirstVisual);
                        StartPreviewStartupWatchdog();
                    }
                    PreviewButtonIcon.Glyph = "\uE71A";
                    ToolTipService.SetToolTip(PreviewButton, "Stop Preview");
                    TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
                }
                else
                {
                    StopPreviewStartupWatchdog();
                    StopPreviewStartupOverlay();
                    await StopPreviewRendererAsync();
                    if (!ViewModel.IsPreviewReinitializing && !_isPreviewReinitAnimating)
                    {
                        ResetPreviewContentTransform();
                        FadeInElement(NoDevicePlaceholder);
                    }
                    if (ViewModel.IsPreviewReinitializing)
                    {
                        PreviewButtonIcon.Glyph = "\uE71A";
                        ToolTipService.SetToolTip(PreviewButton, "Stop Preview");
                    }
                    else
                    {
                        PreviewButtonIcon.Glyph = "\uE768";
                        ToolTipService.SetToolTip(PreviewButton, "Start Preview");
                    }
                    TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
                    ResetPreviewStartupTracking(preserveReinitAnimation: ViewModel.IsPreviewReinitializing || _isPreviewReinitAnimating);
                }
                break;

            case nameof(MainViewModel.IsPreviewReinitializing):
                if (!ViewModel.IsPreviewReinitializing && _isPreviewReinitAnimating)
                {
                    if (!ViewModel.IsPreviewing)
                    {
                        _isPreviewReinitAnimating = false;
                        StopPreviewStartupOverlay();
                        ResetPreviewContentTransform();
                        FadeInElement(NoDevicePlaceholder);
                    }
                    else if (_previewFirstVisualConfirmed)
                    {
                        Logger.Log($"PREVIEW_REINIT_ANIMATE_RESET attempt={_previewStartupAttemptId ?? "none"} reason=reinit-stop-failed");
                        _isPreviewReinitAnimating = false;
                        StopPreviewStartupOverlay();
                        ResetPreviewContentTransform();
                    }
                }
                else if (!ViewModel.IsPreviewReinitializing && !ViewModel.IsPreviewing)
                {
                    PreviewButtonIcon.Glyph = "\uE768";
                    ToolTipService.SetToolTip(PreviewButton, "Start Preview");
                }
                break;

            case nameof(MainViewModel.IsRecording):
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

                // Three-state button: hide spinner, show correct content, animated morph
                RecordButtonStartingContent.IsActive = false;
                RecordButtonStartingContent.Visibility = Visibility.Collapsed;
                if (ViewModel.IsRecording)
                {
                    // Circle → pill: show recording content, measure target, animate
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
                    // Pill → circle: capture current width, animate to 36, swap content on completion
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
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable &&
                                      !ViewModel.IsRecording &&
                                      ViewModel.SourceIsHdr != false;
                TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
                // Stats panel always visible — shows "--" when not recording
                RefreshHdrHintText();
                if (ViewModel.IsRecording)
                    RecPulseStoryboard.Begin();
                else
                    RecPulseStoryboard.Stop();
                ApplyWindowTitle();
                break;

            case nameof(MainViewModel.StatusText):
                StatusTextBlock.Text = ViewModel.StatusText;
                break;

            case nameof(MainViewModel.RecordingTime):
                RecordingTimeTextBlock.Text = ViewModel.RecordingTime;
                if (ViewModel.IsRecording)
                    ApplyWindowTitle();
                break;

            case nameof(MainViewModel.DiskSpaceInfo):
                DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;
                break;
            case nameof(MainViewModel.RecordingSizeInfo):
                RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;
                break;
            case nameof(MainViewModel.RecordingBitrateInfo):
                RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;
                break;

            case nameof(MainViewModel.OutputPath):
                UpdateOutputPathDisplay();
                break;

            case nameof(MainViewModel.AudioClipping):
                AudioClipText.Visibility = ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
                break;

            case nameof(MainViewModel.SelectedDevice):
                Logger.Log($"=== SelectedDevice PropertyChanged ===");
                Logger.Log($"  ViewModel.SelectedDevice: {ViewModel.SelectedDevice?.Name ?? "NULL"}");
                Logger.Log($"  ViewModel.Devices count: {ViewModel.Devices.Count}");
                Logger.Log($"  DeviceComboBox.Items count: {DeviceComboBox.Items.Count}");
                Logger.Log($"  DeviceComboBox.SelectedItem: {((ElgatoCapture.Models.CaptureDevice?)DeviceComboBox.SelectedItem)?.Name ?? "NULL"}");
                EnsureDeviceSelection();
                break;

            case nameof(MainViewModel.SelectedResolution):
                EnsureResolutionSelection();
                break;

            case nameof(MainViewModel.SelectedFrameRate):
                EnsureFrameRateSelection();
                break;

            case nameof(MainViewModel.IsAutoFrameRateSelected):
                EnsureFrameRateSelection();
                break;

            case nameof(MainViewModel.AvailableResolutions):
                ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
                EnsureResolutionSelection();
                break;

            case nameof(MainViewModel.AvailableFrameRates):
                FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;
                EnsureFrameRateSelection();
                break;

            case nameof(MainViewModel.IsHdrAvailable):
            case nameof(MainViewModel.SourceIsHdr):
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable &&
                                      !ViewModel.IsRecording &&
                                      ViewModel.SourceIsHdr != false;
                break;

            case nameof(MainViewModel.IsHdrEnabled):
                if (HdrToggle.IsChecked != ViewModel.IsHdrEnabled)
                {
                    HdrToggle.IsChecked = ViewModel.IsHdrEnabled;
                }

                TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
                break;

            case nameof(MainViewModel.IsTrueHdrPreviewEnabled):
                if (TrueHdrPreviewToggle.IsChecked != ViewModel.IsTrueHdrPreviewEnabled)
                {
                    TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;
                }

                _d3dRenderer?.SetHdrPassthroughEnabled(ViewModel.IsTrueHdrPreviewEnabled);
                break;

            case nameof(MainViewModel.HdrResolutionSupportHint):
            case nameof(MainViewModel.HdrReadinessReason):
            case nameof(MainViewModel.HdrRuntimeState):
                RefreshHdrHintText();
                break;

            case nameof(MainViewModel.SourceTelemetrySummaryText):
            case nameof(MainViewModel.SourceTargetSummaryText):
                UpdateFpsTelemetryTooltip();
                break;

            case nameof(MainViewModel.SourceWidth):
            case nameof(MainViewModel.SourceHeight):
                UpdateVideoContentOverlays();
                break;

            case nameof(MainViewModel.IsCustomBitrateVisible):
                CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
                PresetPanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Collapsed : Visibility.Visible;
                break;

            case nameof(MainViewModel.CustomBitrateMbps):
                if (double.IsNaN(CustomBitrateNumberBox.Value) ||
                    Math.Abs(CustomBitrateNumberBox.Value - ViewModel.CustomBitrateMbps) > 0.01)
                {
                    CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;
                }
                break;

            case nameof(MainViewModel.IsCustomAudioInputEnabled):
                if ((CustomAudioToggle.IsChecked == true) != ViewModel.IsCustomAudioInputEnabled)
                {
                    CustomAudioToggle.IsChecked = ViewModel.IsCustomAudioInputEnabled;
                }
                AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
                break;

            case nameof(MainViewModel.IsMicrophoneEnabled):
                if ((MicrophoneToggle.IsChecked == true) != ViewModel.IsMicrophoneEnabled)
                {
                    MicrophoneToggle.IsChecked = ViewModel.IsMicrophoneEnabled;
                }
                MicrophoneComboBox.IsEnabled = ViewModel.IsMicrophoneEnabled && !ViewModel.IsRecording;
                UpdateMicrophoneControlsVisibility();
                break;

            case nameof(MainViewModel.IsDeviceAudioControlSupported):
            case nameof(MainViewModel.SelectedDeviceAudioMode):
            case nameof(MainViewModel.AnalogAudioGainPercent):
            case nameof(MainViewModel.AvailableDeviceAudioModes):
                ApplyDeviceAudioControlState();
                break;

            case nameof(MainViewModel.ShowAllCaptureOptions):
                if ((ShowAllCaptureOptionsToggle.IsChecked == true) != ViewModel.ShowAllCaptureOptions)
                {
                    ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;
                }
                break;

            case nameof(MainViewModel.IsStatsVisible):
                if (StatsToggle.IsChecked != ViewModel.IsStatsVisible)
                {
                    StatsToggle.IsChecked = ViewModel.IsStatsVisible;
                }
                ApplyStatsVisibility(ViewModel.IsStatsVisible);
                break;

            case nameof(MainViewModel.IsSettingsVisible):
                ApplySettingsVisibility(ViewModel.IsSettingsVisible);
                break;

            case nameof(MainViewModel.SelectedAudioInputDevice):
                EnsureAudioInputSelection();
                break;

            case nameof(MainViewModel.SelectedMicrophoneDevice):
                EnsureMicrophoneSelection();
                break;

            case nameof(MainViewModel.SelectedRecordingFormat):
                EnsureFormatSelection();
                break;

            case nameof(MainViewModel.SelectedQuality):
                EnsureQualitySelection();
                break;

            case nameof(MainViewModel.AvailablePresets):
                PresetComboBox.ItemsSource = ViewModel.AvailablePresets;
                EnsurePresetSelection();
                break;

            case nameof(MainViewModel.SelectedPreset):
                EnsurePresetSelection();
                break;

            case nameof(MainViewModel.AvailableSplitEncodeModes):
                SplitEncodeComboBox.ItemsSource = ViewModel.AvailableSplitEncodeModes;
                EnsureSplitEncodeModeSelection();
                break;

            case nameof(MainViewModel.SelectedSplitEncodeMode):
                EnsureSplitEncodeModeSelection();
                break;

            case nameof(MainViewModel.LiveResolution):
                LiveResolutionTextBlock.Text = ViewModel.LiveResolution;
                UpdateLiveSignalInfoVisibility();
                break;

            case nameof(MainViewModel.LiveFrameRate):
                LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;
                UpdateLiveSignalInfoVisibility();
                break;

            case nameof(MainViewModel.LivePixelFormat):
                LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;
                UpdateLiveSignalInfoVisibility();
                break;

            case nameof(MainViewModel.IsAudioEnabled):
                if (AudioRecordToggle.IsChecked != ViewModel.IsAudioEnabled)
                {
                    AudioRecordToggle.IsChecked = ViewModel.IsAudioEnabled;
                }
                AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled;
                if (!ViewModel.IsAudioEnabled && AudioPreviewToggle.IsChecked == true)
                {
                    AudioPreviewToggle.IsChecked = false;
                }
                AnimateAudioMeterDisabled(!ViewModel.IsAudioEnabled);
                break;

            case nameof(MainViewModel.IsAudioPreviewEnabled):
                if (AudioPreviewToggle.IsChecked != ViewModel.IsAudioPreviewEnabled)
                {
                    AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
                }
                break;

            case nameof(MainViewModel.IsAudioPreviewActive):
                SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);
                break;

            case nameof(MainViewModel.PreviewVolume):
                if (!_isVolumeFadingIn)
                {
                    var volumePct = ViewModel.PreviewVolume * 100;
                    if (PreviewVolumeSlider.Value != volumePct)
                    {
                        PreviewVolumeSlider.Value = volumePct;
                    }

                    PreviewVolumeLabel.Text = $"{(int)volumePct}%";
                }
                break;

            case nameof(MainViewModel.MicrophoneVolume):
                SyncMicrophoneVolumeControls(ViewModel.MicrophoneVolume);
                break;

            case nameof(MainViewModel.IsRecordingTransitioning):
                RecordButton.IsEnabled = !ViewModel.IsRecordingTransitioning;
                if (ViewModel.IsRecordingTransitioning)
                {
                    if (ViewModel.IsRecording)
                    {
                        // Stopping: freeze pill width so it doesn't collapse when content hides
                        RecordButton.Width = RecordButton.ActualWidth;
                        RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        // Starting: hide idle dot, show spinner in circle
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
                break;

            case nameof(MainViewModel.IsFfmpegMissing):
                RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing && !ViewModel.IsRecordingTransitioning;
                break;

            case nameof(MainViewModel.IsFlashbackTimelineVisible):
                if (ViewModel.IsFlashbackTimelineVisible)
                    AnimateFlashbackTimeline(show: true);
                else
                    AnimateFlashbackTimeline(show: false);
                break;

            case nameof(MainViewModel.FlashbackState):
                UpdateFlashbackStateUI();
                break;

            case nameof(MainViewModel.FlashbackBufferFillPercent):
            case nameof(MainViewModel.FlashbackBufferDiskBytes):
                UpdateFlashbackBufferFill();
                UpdateFlashbackPositionUI(); // recalc playhead fraction as buffer grows
                UpdateFlashbackMarkers();    // recalc in/out positions too
                break;

            case nameof(MainViewModel.FlashbackBitrateInfo):
                if (!ViewModel.IsRecording && ViewModel.IsFlashbackEnabled)
                    RecordingBitrateTextBlock.Text = ViewModel.FlashbackBitrateInfo;
                break;

            case nameof(MainViewModel.FlashbackPlaybackPosition):
                UpdateFlashbackPositionUI();
                break;

            case nameof(MainViewModel.FlashbackInPoint):
            case nameof(MainViewModel.FlashbackOutPoint):
                UpdateFlashbackMarkers();
                break;

            case nameof(MainViewModel.FlashbackExportProgress):
                FlashbackExportProgressBar.Value = ViewModel.FlashbackExportProgress;
                break;

            case nameof(MainViewModel.IsFlashbackExporting):
                FlashbackExportProgressBar.Visibility = ViewModel.IsFlashbackExporting
                    ? Microsoft.UI.Xaml.Visibility.Visible
                    : Microsoft.UI.Xaml.Visibility.Collapsed;
                if (!ViewModel.IsFlashbackExporting)
                    FlashbackExportProgressBar.Value = 0;
                break;

            case nameof(MainViewModel.IsDiskWarningActive):
                DiskWarningInfoBar.IsOpen = ViewModel.IsDiskWarningActive;
                break;
        }
    }
}
