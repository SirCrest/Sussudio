using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Capture-mode transactions that coordinate option rebuilds, HDR/SDR changes,
/// and active-preview reinitialization without duplicate property-change cascades.
/// </summary>
public partial class MainViewModel
{
    private bool _pendingModeOptionsRefresh;
    private bool _suppressFormatChangeReinitialize;
    private bool _isRevertingHdrToggle;

    private void RebuildResolutionOptions()
        => _captureModeOptionRebuildController.RebuildResolutionOptions();

    private void RebuildFrameRateOptions()
        => _captureModeOptionRebuildController.RebuildFrameRateOptions();

    private void UpdateSelectedFormat()
        => _captureModeOptionRebuildController.UpdateSelectedFormat();

    private void RebuildVideoFormatOptions()
        => _captureModeOptionRebuildController.RebuildVideoFormatOptions();

    public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);
            }

            if (enabled && !IsHdrAvailable)
            {
                throw new InvalidOperationException("HDR is not available on the selected device.");
            }

            IsHdrEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("True HDR preview cannot be changed while recording.");
            }

            IsTrueHdrPreviewEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    partial void OnIsHdrEnabledChanged(bool value)
    {
        if (_isRevertingHdrToggle)
        {
            return;
        }

        if (value)
        {
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        if (IsRecording)
        {
            _isRevertingHdrToggle = true;
            try
            {
                IsHdrEnabled = !value;
            }
            finally
            {
                _isRevertingHdrToggle = false;
            }

            StatusText = HdrToggleBlockedWhileRecordingMessage;
            return;
        }

        if (!_isChangingDevice)
        {
            _suppressFormatChangeReinitialize = true;
            try
            {
                ResetModeSelectionState();
                RebuildResolutionOptions();
                RebuildRecordingFormatOptions();
            }
            finally
            {
                _suppressFormatChangeReinitialize = false;
            }

            if (IsInitialized && !IsRecording && SelectedDevice != null && SelectedFormat != null)
            {
                Logger.Log($"HDR toggle changed to {(value ? "On" : "Off")} - forcing immediate device renegotiation");
                EnqueueUiOperation(() => ReinitializeDeviceAsync("HDR toggle"), "hdr toggle reinitialize");
            }
        }

        SaveSettings();
    }

}
