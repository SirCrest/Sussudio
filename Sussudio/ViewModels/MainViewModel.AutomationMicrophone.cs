using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutator for microphone monitor enablement and recording-time guards.
/// </summary>
public partial class MainViewModel
{
    public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return SetMicrophoneEnabledAutomationAsync(enabled, cancellationToken);
    }

    private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)
    {
        var request = await InvokeOnUiThreadAsync(
            () => (
                IsRecording,
                CurrentMicEnabled: IsMicrophoneEnabled,
                DeviceId: SelectedMicrophoneDevice?.Id,
                DeviceName: SelectedMicrophoneDevice?.Name),
            cancellationToken).ConfigureAwait(false);

        if (request.IsRecording)
        {
            if (enabled == request.CurrentMicEnabled)
            {
                // Idempotent reassertion during recording: automation clients often
                // re-issue desired state. The mic wiring is already where the caller
                // wants it, so succeed as a no-op rather than throwing.
                Logger.Log($"MIC_TOGGLE_NOOP reason=recording_active_idempotent requested={enabled}");
                return;
            }

            // Real state transition while recording: refuse. UpdateMicrophoneMonitorAsync
            // cannot rewire the device mid-recording, so setting IsMicrophoneEnabled
            // here would leave UI state lying about the actual device wiring.
            Logger.Log($"MIC_TOGGLE_REFUSED reason=recording_active requested={enabled} current={request.CurrentMicEnabled}");
            throw new InvalidOperationException(
                "Cannot change microphone enable state while recording. Stop the recording first.");
        }

        await _sessionCoordinator.UpdateMicrophoneMonitorAsync(
            enabled,
            request.DeviceId,
            request.DeviceName,
            cancellationToken).ConfigureAwait(false);

        await InvokeOnUiThreadAsync(
            () =>
            {
                _suppressMicrophoneMonitorUpdate = true;
                try
                {
                    IsMicrophoneEnabled = enabled;
                }
                finally
                {
                    _suppressMicrophoneMonitorUpdate = false;
                }

                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }
}
