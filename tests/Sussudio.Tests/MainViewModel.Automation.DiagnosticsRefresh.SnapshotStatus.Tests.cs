static partial class Program
{
    private static void AssertDiagnosticsSnapshotStatusProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionSnapshotStatusText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(diagnostics.SnapshotProjectionSnapshotStatusText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(diagnostics.SnapshotProjectionSnapshotStatusText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "StatusText = viewModelSnapshot.StatusText,");
    }
}
