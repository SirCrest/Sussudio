using Sussudio.ViewModels;

namespace Sussudio;

// Capture-option and source-signal ViewModel property projections: HDR toggles,
// source overlay updates, telemetry tooltips, bitrate visibility, and option
// shelf synchronization.
public sealed partial class MainWindow
{
    private bool TryHandleCaptureOptionPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.AudioClipping):
                ApplyAudioClipVisibility();
                return true;

            case nameof(MainViewModel.IsHdrAvailable):
            case nameof(MainViewModel.SourceIsHdr):
                ApplyHdrToggleEnabledState();
                return true;

            case nameof(MainViewModel.IsHdrEnabled):
                HandleHdrEnabledChanged();
                return true;

            case nameof(MainViewModel.IsTrueHdrPreviewEnabled):
                HandleTrueHdrPreviewEnabledChanged();
                return true;

            case nameof(MainViewModel.HdrResolutionSupportHint):
            case nameof(MainViewModel.HdrReadinessReason):
            case nameof(MainViewModel.HdrRuntimeState):
                RefreshHdrHintText();
                return true;

            case nameof(MainViewModel.SourceTelemetrySummaryText):
            case nameof(MainViewModel.SourceTargetSummaryText):
                UpdateFpsTelemetryTooltip();
                return true;

            case nameof(MainViewModel.SourceWidth):
            case nameof(MainViewModel.SourceHeight):
                UpdateVideoContentOverlays();
                return true;

            case nameof(MainViewModel.IsCustomBitrateVisible):
                ApplyBitrateVisibility();
                return true;

            case nameof(MainViewModel.CustomBitrateMbps):
                HandleCustomBitratePropertyChanged();
                return true;

            case nameof(MainViewModel.ShowAllCaptureOptions):
                HandleShowAllCaptureOptionsChanged();
                return true;

            default:
                return false;
        }
    }

    private void HandleHdrEnabledChanged()
    {
        if (HdrToggle.IsChecked != ViewModel.IsHdrEnabled)
        {
            HdrToggle.IsChecked = ViewModel.IsHdrEnabled;
        }

        ApplyHdrToggleEnabledState();
    }

    private void HandleTrueHdrPreviewEnabledChanged()
    {
        if (TrueHdrPreviewToggle.IsChecked != ViewModel.IsTrueHdrPreviewEnabled)
        {
            TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;
        }

        _d3dRenderer?.SetHdrPassthroughEnabled(ViewModel.IsTrueHdrPreviewEnabled);
    }

    private void HandleShowAllCaptureOptionsChanged()
    {
        if ((ShowAllCaptureOptionsToggle.IsChecked == true) != ViewModel.ShowAllCaptureOptions)
        {
            ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;
        }
    }
}
