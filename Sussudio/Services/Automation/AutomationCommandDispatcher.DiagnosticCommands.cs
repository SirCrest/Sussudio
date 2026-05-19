using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private AutomationCommandResponse ExecuteGetDiagnosticsCommand(
        JsonElement payload,
        string correlationId)
    {
        var maxEvents = GetInt(payload, "maxEvents") ?? 100;
        var events = _diagnosticsHub.GetRecentEvents(maxEvents);
        return CreateResponse(correlationId, "Diagnostics retrieved.", data: events);
    }

    private AutomationCommandResponse ExecuteGetPerformanceTimelineCommand(
        JsonElement payload,
        string correlationId)
    {
        var maxEntries = GetInt(payload, "maxEntries") ?? 240;
        var timeline = _diagnosticsHub.GetPerformanceTimeline(maxEntries);
        return CreateResponse(correlationId, "Performance timeline retrieved.", data: timeline);
    }

    private async Task<AutomationCommandResponse> ExecuteGetAudioRampTraceCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var maxEntries = GetInt(payload, "maxEntries") ?? 512;
        var trace = await _snapshotQueryPort.GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken).ConfigureAwait(false);
        return CreateResponse(correlationId, "Audio ramp trace retrieved.", data: trace);
    }
}
