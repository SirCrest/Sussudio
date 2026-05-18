using System;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed partial class CaptureOptionBindingController
{
    public bool TryHandlePropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.AudioClipping):
                _context.ApplyAudioClipVisibility();
                return true;

            case nameof(MainViewModel.IsHdrAvailable):
            case nameof(MainViewModel.SourceIsHdr):
                _context.ApplyHdrToggleEnabledState();
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
                _context.RefreshHdrHintText();
                return true;

            case nameof(MainViewModel.SourceTelemetrySummaryText):
            case nameof(MainViewModel.SourceTargetSummaryText):
                _context.UpdateFpsTelemetryTooltip();
                return true;

            case nameof(MainViewModel.SourceWidth):
            case nameof(MainViewModel.SourceHeight):
                _context.UpdateVideoContentOverlays();
                return true;

            case nameof(MainViewModel.IsCustomBitrateVisible):
                _context.ApplyBitrateVisibility();
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

    public void HandleCustomBitratePropertyChanged()
    {
        if (double.IsNaN(_context.CustomBitrateNumberBox.Value) ||
            Math.Abs(_context.CustomBitrateNumberBox.Value - _context.ViewModel.CustomBitrateMbps) > 0.01)
        {
            _context.CustomBitrateNumberBox.Value = _context.ViewModel.CustomBitrateMbps;
        }
    }

    public void HandleHdrEnabledChanged()
    {
        if (_context.HdrToggle.IsChecked != _context.ViewModel.IsHdrEnabled)
        {
            _context.HdrToggle.IsChecked = _context.ViewModel.IsHdrEnabled;
        }

        _context.ApplyHdrToggleEnabledState();
    }

    public void HandleTrueHdrPreviewEnabledChanged()
    {
        if (_context.TrueHdrPreviewToggle.IsChecked != _context.ViewModel.IsTrueHdrPreviewEnabled)
        {
            _context.TrueHdrPreviewToggle.IsChecked = _context.ViewModel.IsTrueHdrPreviewEnabled;
        }

        _context.SetHdrPassthroughEnabled(_context.ViewModel.IsTrueHdrPreviewEnabled);
    }

    public void HandleShowAllCaptureOptionsChanged()
    {
        if ((_context.ShowAllCaptureOptionsToggle.IsChecked == true) != _context.ViewModel.ShowAllCaptureOptions)
        {
            _context.ShowAllCaptureOptionsToggle.IsChecked = _context.ViewModel.ShowAllCaptureOptions;
        }
    }
}
