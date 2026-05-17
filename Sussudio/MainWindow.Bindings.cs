namespace Sussudio;

// Manual binding layer for WinUI controls. The app deliberately avoids x:Bind,
// so this partial maps view-model property changes to concrete UI updates.
public sealed partial class MainWindow
{
    private void SetupBindings()
    {
        AttachAudioMeterActivationBindings();

        ApplyInitialFlashbackSettings();

        // Bind all collections to ComboBoxes
        AttachCaptureSelectionBindings();
        InitializeCaptureOptionCollections();

        // Set initial values
        UpdateOutputPathDisplay();
        ApplyInitialStatusStripPresentation();
        UpdateLiveSignalInfoVisibility();
        ApplyInitialAudioControlBindings();
        ApplyInitialCaptureOptionSelections();
        ApplyInitialAudioMeterPresentation();
        ApplyAudioClipVisibility();
        HandleFfmpegMissingChanged();
        RefreshHdrHintText();
        UpdateFpsTelemetryTooltip();
        EnsureDeviceSelection();
        EnsureAudioControlSelections();
        EnsureInitialCaptureOptionSelections();

        AttachDeviceSelectionChangedBinding();
        AttachAudioSelectionBindings();
        AttachCaptureModeSelectionBindings();

        AttachRecordingOptionBindings();
        AttachAudioRecordPreviewToggleBindings();
        AttachStatsOverlayToggleBindings();
        AttachAudioInputToggleBindings();
        AttachShowAllCaptureOptionsBinding();
        AttachFlashbackSettingsBindings();
        AttachDeviceAudioGainAndMeterBindings();
        SetupResponsiveShellLayoutBindings();
        AttachOutputPathDisplay();
        ApplyStatsVisibility(ViewModel.IsStatsVisible, immediate: true);
    }
}
