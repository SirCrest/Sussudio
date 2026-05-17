using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteSetDeviceAudioModeCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var mode = RequireString(payload, "mode");
        await _viewModel.SetDeviceAudioModeAsync(mode, cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, $"Device audio mode changed: {mode}.");
    }

    private async Task<AutomationCommandResponse> ExecuteSetAnalogAudioGainCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var gain = RequireDouble(payload, "gain");
        await _viewModel.SetAnalogAudioGainAsync(gain, cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, $"Analog audio gain set to {gain:0.###}%.");
    }

    private async Task<AutomationCommandResponse> ExecuteSetMicrophoneEnabledCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var enabled = GetBool(payload, "enabled") ?? throw new InvalidOperationException("Missing 'enabled' parameter.");
        await _viewModel.SetMicrophoneEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, $"Microphone {(enabled ? "enabled" : "disabled")}.");
    }
}
