using System.Text.Json;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionRunContext
{
    internal JsonElement InitialSnapshot { get; private set; }

    internal bool InitialSnapshotKnown { get; private set; }

    private void InitializeUnknownSnapshotState()
    {
        var unknownSnapshot = DiagnosticSessionInitialSnapshot.CreateUnknown();
        InitialSnapshot = unknownSnapshot.Snapshot;
        InitialSnapshotKnown = unknownSnapshot.Known;
    }

    internal async Task CaptureInitialSnapshotAsync()
    {
        await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        var initialSnapshotResult = await DiagnosticSessionInitialSnapshot.CaptureAsync(
                CommandChannel,
                SetStage,
                RecordTerminalException,
                () => WriteLiveStateBestEffortAsync())
            .ConfigureAwait(false);
        InitialSnapshot = initialSnapshotResult.Snapshot;
        InitialSnapshotKnown = initialSnapshotResult.Known;
    }
}
