using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteRefreshDevicesCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        await _deviceSelectionPort.RefreshDevicesForAutomationAsync(cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, "Device list refresh requested.");
    }

    private async Task<AutomationCommandResponse> ExecuteSelectDeviceCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var deviceId = GetString(payload, "deviceId");
        var deviceName = GetString(payload, "deviceName");
        await _deviceSelectionPort.SelectDeviceAsync(deviceId, deviceName, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, "Capture device selection requested.");
    }

    private async Task<AutomationCommandResponse> ExecuteSelectAudioInputDeviceCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var deviceId = GetString(payload, "deviceId");
        var deviceName = GetString(payload, "deviceName");
        await _deviceSelectionPort.SelectAudioInputDeviceAsync(deviceId, deviceName, cancellationToken).ConfigureAwait(false);
        return CreateAcknowledgedResponse(correlationId, "Audio input device selection requested.");
    }

    private async Task<AutomationCommandResponse> ExecuteGetCaptureOptionsCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var options = await _snapshotQueryPort.GetAutomationOptionsSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, "Capture options retrieved.", data: options);
    }
}
