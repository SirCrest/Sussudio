using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendStateSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Sussudio State ==");
        builder.AppendLine($"Status: {Get(snapshot, "SessionState")} | {Get(snapshot, "StatusText")}");
        builder.AppendLine($"Capture Commands: pending={Get(snapshot, "CaptureCommandPendingCommands")} maxPending={Get(snapshot, "CaptureCommandMaxPendingCommands")} oldestAge={Get(snapshot, "CaptureCommandOldestPendingCommandAgeMs")}ms lastLatency={Get(snapshot, "CaptureCommandLastQueueLatencyMs")}ms maxLatency={Get(snapshot, "CaptureCommandMaxQueueLatencyMs")}ms enq={Get(snapshot, "CaptureCommandCommandsEnqueued")} done={Get(snapshot, "CaptureCommandCommandsCompleted")} fail={Get(snapshot, "CaptureCommandCommandsFailed")} cancel={Get(snapshot, "CaptureCommandCommandsCanceled")} coalesced={Get(snapshot, "CaptureCommandCommandsCoalesced")} last={Get(snapshot, "CaptureCommandLastCommand", "None")} outcome={Get(snapshot, "CaptureCommandLastOutcome", "None")} corr={Get(snapshot, "CaptureCommandLastCorrelationId", "")} error={Get(snapshot, "CaptureCommandLastError", "")}");
        builder.AppendLine($"Device: {Get(snapshot, "SelectedDeviceName")} ({Get(snapshot, "SelectedDeviceId")})");
        builder.AppendLine($"Initialized: {Get(snapshot, "IsInitialized")} | Previewing: {Get(snapshot, "IsPreviewing")} | Recording: {Get(snapshot, "IsRecording")}");
        builder.AppendLine();
    }

}
