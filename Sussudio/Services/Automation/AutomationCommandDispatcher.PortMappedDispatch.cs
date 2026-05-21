using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse?> TryExecutePortMappedCommandAsync(
        AutomationCommandKind command,
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var uiSettingsResponse = await TryExecuteUiSettingsCommandAsync(command, payload, correlationId, cancellationToken)
            .ConfigureAwait(false);
        if (uiSettingsResponse != null)
        {
            return uiSettingsResponse;
        }

        if (TrivialDeviceSelectionHandlers.TryGetValue(command, out var deviceSelectionHandler))
        {
            await deviceSelectionHandler.InvokeAsync(_deviceSelectionPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, deviceSelectionHandler.AcknowledgeMessage(command, payload));
        }

        if (TrivialCaptureSettingsHandlers.TryGetValue(command, out var captureSettingsHandler))
        {
            await captureSettingsHandler.InvokeAsync(_captureSettingsPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, captureSettingsHandler.AcknowledgeMessage(command, payload));
        }

        if (TrivialAudioHandlers.TryGetValue(command, out var audioHandler))
        {
            await audioHandler.InvokeAsync(_audioPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, audioHandler.AcknowledgeMessage(command, payload));
        }

        if (TrivialPreviewRecordingHandlers.TryGetValue(command, out var previewRecordingHandler))
        {
            await previewRecordingHandler.InvokeAsync(_previewRecordingPort, payload, cancellationToken).ConfigureAwait(false);
            return CreateAcknowledgedResponse(correlationId, previewRecordingHandler.AcknowledgeMessage(command, payload));
        }

        return null;
    }
}
