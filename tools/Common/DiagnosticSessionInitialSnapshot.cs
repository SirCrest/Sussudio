using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static class DiagnosticSessionInitialSnapshot
{
    internal static DiagnosticSessionInitialSnapshotResult CreateUnknown()
    {
        return new DiagnosticSessionInitialSnapshotResult(CreateEmptyJsonObject(), false);
    }

    internal static async Task<DiagnosticSessionInitialSnapshotResult> CaptureAsync(
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException,
        Func<Task> writeLiveStateAsync)
    {
        var unknownSnapshot = CreateUnknown();
        var initialSnapshot = unknownSnapshot.Snapshot;
        var initialSnapshotKnown = unknownSnapshot.Known;

        try
        {
            setStage("initial-snapshot");
            var initialResponse = await commandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null).ConfigureAwait(false);
            if (TryGetSnapshot(initialResponse, out var initial))
            {
                initialSnapshot = initial;
                initialSnapshotKnown = true;
            }
            else
            {
                commandChannel.RecordFailure("initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped");
            }
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "initial-snapshot");
            await writeLiveStateAsync().ConfigureAwait(false);
        }

        return new DiagnosticSessionInitialSnapshotResult(initialSnapshot, initialSnapshotKnown);
    }
}

internal sealed class DiagnosticSessionInitialSnapshotResult
{
    internal DiagnosticSessionInitialSnapshotResult(JsonElement snapshot, bool known)
    {
        Snapshot = snapshot;
        Known = known;
    }

    internal JsonElement Snapshot { get; }
    internal bool Known { get; }
}
