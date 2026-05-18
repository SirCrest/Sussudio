using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Selected capture-device reaction flow: capability projection, source
/// telemetry reset, and device-native audio-control refresh handoff.
/// </summary>
public partial class MainViewModel
{
    partial void OnSelectedDeviceChanged(CaptureDevice? value)
    {
        CancelPendingAudioControlWork();
        RebuildSelectedDeviceCapabilities(value, resetTelemetryState: true);
        RequestDeviceAudioControlsRefresh(value);
        SaveSettings();
    }

    private void RebuildSelectedDeviceCapabilities(CaptureDevice? device, bool resetTelemetryState)
    {
        _isChangingDevice = true;
        try
        {
            ResetFrameRateSelectionState();
            HdrResolutionSupportHint = string.Empty;

            AvailableFormats.Clear();
            AvailableFrameRates.Clear();
            _resolutionToFormats.Clear();
            if (resetTelemetryState)
            {
                _pendingSdrAutoSelectionForDeviceChange = device != null && !IsHdrEnabled;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
                _sourceTelemetryController.ApplySourceTelemetrySnapshot(
                    SourceSignalTelemetrySnapshot.CreateUnavailable("awaiting-source-telemetry"),
                    allowAutoRetarget: false);
            }

            if (device != null)
            {
                foreach (var format in device.SupportedFormats)
                {
                    AvailableFormats.Add(format);

                    var resolutionKey = GetResolutionKey(format.Width, format.Height);
                    if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
                    {
                        formats = new List<MediaFormat>();
                        _resolutionToFormats[resolutionKey] = formats;
                    }

                    formats.Add(format);
                }

                IsHdrAvailable = device.IsHdrCapable;
                if (!IsHdrAvailable)
                {
                    IsHdrEnabled = false;
                }
            }

            if (IsRecording)
            {
                _pendingModeOptionsRefresh = true;
            }
            else
            {
                RebuildResolutionOptions();
            }
        }
        finally
        {
            _isChangingDevice = false;
        }
    }
}
