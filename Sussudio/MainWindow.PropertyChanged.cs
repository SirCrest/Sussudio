using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;
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
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// PropertyChanged dispatcher for view-model updates. Keep this as UI projection
// logic: command execution belongs in MainViewModel and capture state belongs in
// CaptureService.
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
        if (string.IsNullOrWhiteSpace(_previewStartupAttemptId) ||
            IsPreviewStartupFailedState(_previewStartupState) ||
            _previewStartupState == PreviewStartupState.Idle)
        {
            BeginPreviewStartupAttempt();
        }

        PrimePreviewAudioFadeIn();
        if (!ViewModel.IsPreviewReinitializing && !_isPreviewReinitAnimating)
        {
            PreparePreviewStartupPresentation();
        }
    }
    private async Task ViewModel_PreviewReinitRequested(string reason)
    {
        if (!ViewModel.IsPreviewing)
        {
            return;
        }

        _isPreviewReinitAnimating = true;
        Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=true caller={nameof(ViewModel_PreviewReinitRequested)}");
        Logger.Log($"PREVIEW_REINIT_ANIMATE_OUT reason={reason}");
        await AnimatePreviewOutAsync();
    }
    private Task ViewModel_PreviewRendererStopRequested()
    {
        // Stop the render thread before the capture pipeline teardown. This ensures
        // no native D3D calls (VideoProcessorBlt/Present) are in flight when
        // UnifiedVideoCapture disposes the shared D3D11 device and DXGI manager.
        //
        // IMPORTANT: this only drains and detaches the active renderer. The later
        // attach step may replace the SwapChainPanel surface for HDR/SDR or mode
        // changes because WinUI can keep native DXGI state behind a panel even
        // after SetSwapChain(null). Replacing the surface happens after capture
        // teardown so the old renderer is no longer receiving frames.
        var renderer = _d3dRenderer;
        if (renderer != null)
        {
            Logger.Log("PREVIEW_REINIT_RENDERER_STOP: stopping render thread before pipeline teardown");
            try
            {
                DisposeD3DPreviewRendererForReinit();
            }
            catch (TimeoutException ex)
            {
                // Render thread did not exit before its stop timeout. The renderer's
                // stop path has already logged details and the fresh attach path will
                // replace the panel surface if needed. Swallow the exception so reinit
                // can continue rather than crashing the UI thread mid-resolution-change.
                Logger.Log($"PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; continuing reinit with orphan render thread expected to exit shortly.");
            }
        }

        return Task.CompletedTask;
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
                        PreparePreviewStartupPresentation();
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
                        RevealPreviewUnavailablePlaceholder();
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
                    ApplyHdrToggleEnabledState();
                }
                else
                {
                    StopPreviewStartupWatchdog();
                    StopPreviewStartupOverlay();
                    // During reinit, the renderer is kept alive (render thread stopped
                    // by ViewModel_PreviewRendererStopRequested, instance preserved).
                    // StartPreviewRendererAsync will reuse it via Start().
                    if (!ViewModel.IsPreviewReinitializing)
                    {
                        await StopPreviewRendererAsync();
                    }
                    if (!ViewModel.IsPreviewReinitializing && !_isPreviewReinitAnimating)
                    {
                        RevealPreviewUnavailablePlaceholder();
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
                    ApplyHdrToggleEnabledState();
                    ResetPreviewStartupTracking(preserveReinitAnimation: ViewModel.IsPreviewReinitializing || _isPreviewReinitAnimating);
                }
                break;

            case nameof(MainViewModel.IsPreviewReinitializing):
                UpdateDeviceApplyButtonState();
                if (!ViewModel.IsPreviewReinitializing && _isPreviewReinitAnimating)
                {
                    if (!ViewModel.IsPreviewing)
                    {
                        _isPreviewReinitAnimating = false;
                        Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(HandleViewModelPropertyChangedAsync)}");
                        RevealPreviewUnavailablePlaceholder();
                    }
                    else if (_previewFirstVisualConfirmed)
                    {
                        Logger.Log($"PREVIEW_REINIT_ANIMATE_RESET attempt={_previewStartupAttemptId ?? "none"} reason=reinit-stop-failed");
                        _isPreviewReinitAnimating = false;
                        Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(HandleViewModelPropertyChangedAsync)}");
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
                ApplyHdrToggleEnabledState();
                // Stats panel always visible — shows "--" when not recording
                RefreshHdrHintText();
                UpdateDeviceApplyButtonState();
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
                ApplyAudioClipVisibility();
                break;

            case nameof(MainViewModel.SelectedDevice):
                var selectedDevice = (CaptureDevice?)DeviceComboBox.SelectedItem;
                if (!string.Equals(selectedDevice?.Id, ViewModel.SelectedDevice?.Id, StringComparison.Ordinal))
                {
                    Logger.Log(
                        $"DEVICE_SELECTION_SYNC viewModel='{ViewModel.SelectedDevice?.Name ?? "NULL"}' combo='{selectedDevice?.Name ?? "NULL"}' devices={ViewModel.Devices.Count} comboItems={DeviceComboBox.Items.Count}");
                }
                EnsureDeviceSelection();
                UpdateDeviceApplyButtonState();
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
                ApplyHdrToggleEnabledState();
                break;

            case nameof(MainViewModel.IsHdrEnabled):
                if (HdrToggle.IsChecked != ViewModel.IsHdrEnabled)
                {
                    HdrToggle.IsChecked = ViewModel.IsHdrEnabled;
                }

                ApplyHdrToggleEnabledState();
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
                ApplyBitrateVisibility();
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
                if (!IsPreviewAudioFadeInActive)
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
                ApplyFlashbackTimelineVisibility(ViewModel.IsFlashbackTimelineVisible);
                break;

            case nameof(MainViewModel.IsFlashbackEnabled):
                ApplyFlashbackTimelineLockout();
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

            case nameof(MainViewModel.FlashbackGpuDecode):
                if (FlashbackGpuDecodeToggle.IsOn != ViewModel.FlashbackGpuDecode)
                    FlashbackGpuDecodeToggle.IsOn = ViewModel.FlashbackGpuDecode;
                break;

            case nameof(MainViewModel.FlashbackBufferMinutes):
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
                break;
        }
    }
}
