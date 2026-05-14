using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureSessionCoordinator_QueueWorkerLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var queueText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Queue.cs")
            .Replace("\r\n", "\n");

        AssertContains(queueText, "private sealed class CoordinatorWorkItem");
        AssertContains(queueText, "private Task EnqueueAsync(");
        AssertContains(queueText, "private async Task ProcessQueueAsync()");
        AssertContains(queueText, "private void FailPendingCommands(Exception ex)");
        AssertContains(queueText, "private void DecrementPendingCommands(string operation)");
        AssertContains(queueText, "Logger.LogEvent(\"CAP-COORD-START\"");
        AssertContains(queueText, "Logger.LogEvent(\"CAP-COORD-DONE\"");
        AssertDoesNotContain(rootText, "private sealed class CoordinatorWorkItem");
        AssertDoesNotContain(rootText, "private async Task ProcessQueueAsync()");
        AssertDoesNotContain(rootText, "private void FailPendingCommands(Exception ex)");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionCoordinator_SnapshotProjectionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var snapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Snapshot.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotText, "public CaptureSessionSnapshot Snapshot");
        AssertContains(snapshotText, "private void UpdateSnapshot(CaptureCommand command, CaptureCommandOutcome outcome, string? error)");
        AssertContains(snapshotText, "private void TrackPendingCommandEnqueued(DateTimeOffset enqueuedAtUtc)");
        AssertContains(snapshotText, "private void RemoveOldestPendingCommand()");
        AssertContains(snapshotText, "private void RecordCommandQueueLatency(DateTimeOffset enqueuedAtUtc)");
        AssertContains(snapshotText, "OldestPendingCommandAgeMs = oldestPendingCommandAgeMs,");
        AssertDoesNotContain(rootText, "public CaptureSessionSnapshot Snapshot");
        AssertDoesNotContain(rootText, "private void UpdateSnapshot(CaptureCommand command, CaptureCommandOutcome outcome, string? error)");
        AssertDoesNotContain(rootText, "private void RecordCommandQueueLatency(DateTimeOffset enqueuedAtUtc)");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionCoordinator_DisposalLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var disposalText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Disposal.cs")
            .Replace("\r\n", "\n");

        AssertContains(disposalText, "private const int DefaultDisposeDrainTimeoutMs = 15_000;");
        AssertContains(disposalText, "public void Dispose()");
        AssertContains(disposalText, "public async ValueTask DisposeAsync()");
        AssertContains(disposalText, "private async ValueTask CoreDisposeAsync()");
        AssertContains(disposalText, "private async Task WaitForWorkerCancellationAsync()");
        AssertContains(disposalText, "private void DisposeWorkerCancellationWhenSafe()");
        AssertContains(disposalText, "private void CancelWorkerBestEffort()");
        AssertDoesNotContain(rootText, "private async ValueTask CoreDisposeAsync()");
        AssertDoesNotContain(rootText, "SUSSUDIO_COORDINATOR_DISPOSE_TIMEOUT_MS");
        AssertDoesNotContain(rootText, "private void DisposeWorkerCancellationWhenSafe()");

        return Task.CompletedTask;
    }
}
