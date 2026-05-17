static partial class Program
{
    private static void AssertDiagnosticsSnapshotStatusProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionSnapshotStatusText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(diagnostics.SnapshotProjectionSnapshotStatusText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(diagnostics.SnapshotProjectionSnapshotStatusText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "StatusText = viewModelSnapshot.StatusText,");
    }
}
