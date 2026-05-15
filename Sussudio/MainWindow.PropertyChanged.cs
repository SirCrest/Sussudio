using System;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;

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

    private async Task HandleViewModelPropertyChangedAsync(System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsPreviewing):
                await HandlePreviewingChangedAsync();
                break;

            case nameof(MainViewModel.IsPreviewReinitializing):
                HandlePreviewReinitializingChanged();
                break;

            case nameof(MainViewModel.IsRecording):
                HandleRecordingChanged();
                break;

            case nameof(MainViewModel.StatusText):
                UpdateStatusTextPresentation();
                break;

            case nameof(MainViewModel.RecordingTime):
                UpdateRecordingTimePresentation();
                if (ViewModel.IsRecording)
                    ApplyWindowTitle();
                break;

            case nameof(MainViewModel.DiskSpaceInfo):
                UpdateDiskSpacePresentation();
                break;
            case nameof(MainViewModel.RecordingSizeInfo):
                UpdateRecordingSizePresentation();
                break;
            case nameof(MainViewModel.RecordingBitrateInfo):
                UpdateRecordingBitratePresentation();
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
                HandleCustomAudioInputEnabledChanged();
                break;

            case nameof(MainViewModel.IsMicrophoneEnabled):
                HandleMicrophoneEnabledChanged();
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
                UpdateLiveSignalInfoVisibility();
                break;

            case nameof(MainViewModel.LiveFrameRate):
                UpdateLiveSignalInfoVisibility();
                break;

            case nameof(MainViewModel.LivePixelFormat):
                UpdateLiveSignalInfoVisibility();
                break;

            case nameof(MainViewModel.IsAudioEnabled):
                HandleAudioEnabledChanged();
                break;

            case nameof(MainViewModel.IsAudioPreviewEnabled):
                HandleAudioPreviewEnabledChanged();
                break;

            case nameof(MainViewModel.IsAudioPreviewActive):
                HandleAudioPreviewActiveChanged();
                break;

            case nameof(MainViewModel.PreviewVolume):
                HandlePreviewVolumeChanged();
                break;

            case nameof(MainViewModel.MicrophoneVolume):
                HandleMicrophoneVolumeChanged();
                break;

            case nameof(MainViewModel.IsRecordingTransitioning):
                HandleRecordingTransitioningChanged();
                break;

            case nameof(MainViewModel.IsFfmpegMissing):
                HandleFfmpegMissingChanged();
                break;

            case nameof(MainViewModel.IsFlashbackTimelineVisible):
                HandleFlashbackTimelineVisibleChanged();
                break;

            case nameof(MainViewModel.IsFlashbackEnabled):
                HandleFlashbackEnabledChanged();
                break;

            case nameof(MainViewModel.FlashbackState):
                HandleFlashbackStateChanged();
                break;

            case nameof(MainViewModel.FlashbackBufferFillPercent):
            case nameof(MainViewModel.FlashbackBufferDiskBytes):
                HandleFlashbackBufferChanged();
                break;

            case nameof(MainViewModel.FlashbackBitrateInfo):
                HandleFlashbackBitrateChanged();
                break;

            case nameof(MainViewModel.FlashbackPlaybackPosition):
                HandleFlashbackPlaybackPositionChanged();
                break;

            case nameof(MainViewModel.FlashbackInPoint):
            case nameof(MainViewModel.FlashbackOutPoint):
                HandleFlashbackRangeChanged();
                break;

            case nameof(MainViewModel.FlashbackExportProgress):
                HandleFlashbackExportProgressChanged();
                break;

            case nameof(MainViewModel.IsFlashbackExporting):
                HandleFlashbackExportingChanged();
                break;

            case nameof(MainViewModel.IsDiskWarningActive):
                UpdateDiskWarningPresentation();
                break;

            case nameof(MainViewModel.FlashbackGpuDecode):
                HandleFlashbackGpuDecodeChanged();
                break;

            case nameof(MainViewModel.FlashbackBufferMinutes):
                HandleFlashbackBufferMinutesChanged();
                break;
        }
    }
}
