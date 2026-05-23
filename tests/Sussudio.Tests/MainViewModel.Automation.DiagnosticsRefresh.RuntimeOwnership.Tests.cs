static partial class Program
{
    private static void AssertDiagnosticsRefreshRuntimeOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.VerificationText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.VerificationText, "public async Task<RecordingVerificationResult> VerifyFileAsync");
        AssertContains(diagnostics.VerificationText, "private bool ShouldAutoVerifySnapshot(");
        AssertContains(diagnostics.VerificationText, "private RecordingVerificationResult? CaptureLastVerificationForSnapshot(");
        AssertContains(diagnostics.VerificationText, "private void ScheduleAutoVerificationIfNeeded(");
        AssertContains(diagnostics.VerificationText, "Automatic recording verification started.");
        AssertContains(diagnostics.VerificationText, "private static CaptureRuntimeSnapshot ApplyVerificationProfile(");
        AssertContains(diagnostics.VerificationText, "string.Equals(verificationProfile, \"flashback-export\"");
        AssertDoesNotContain(diagnostics.HubText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.HubText, "private readonly IAutomationSnapshotQueryPort _snapshotQueryPort;");
        AssertContains(diagnostics.HubText, "IAutomationSnapshotQueryPort snapshotQueryPort,");
        AssertContains(diagnostics.HubText, "_snapshotQueryPort = snapshotQueryPort ?? throw new ArgumentNullException(nameof(snapshotQueryPort));");
        AssertDoesNotContain(diagnostics.HubText, "IAutomationViewModel viewModel,");
        AssertDoesNotContain(diagnostics.HubText, "private readonly IAutomationViewModel _viewModel;");
        AssertContains(diagnostics.SnapshotsText, "await _snapshotQueryPort\n            .GetViewModelRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.SnapshotsText, "await _snapshotQueryPort\n            .GetCaptureRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.VerificationText, "await _snapshotQueryPort\n                .GetCaptureRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.SnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnostics.SnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertContains(diagnostics.SnapshotsText, "private static PreviewPacingClassification ClassifyPreviewPacing(");
        AssertContains(diagnostics.SnapshotsText, "ClassifyPreviewPacing(");
        AssertContains(diagnostics.LifecycleText, "public void Start()");
        AssertContains(diagnostics.LifecycleText, "private async Task RunLoopAsync(CancellationToken cancellationToken)");
        AssertDoesNotContain(diagnostics.HubText, "public void Start()");
        AssertContains(diagnostics.HdrText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.HdrText, "private static PreviewHdrState BuildPreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private readonly record struct PreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private static bool IsHdrSubtype(string? subtype)");
        AssertContains(diagnostics.HdrText, "static string NormalizeFormatToken(string? text)");
        AssertDoesNotContain(diagnostics.HubText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.SnapshotsText, "var previewHdrState = BuildPreviewHdrState(captureRuntime, viewModelSnapshot, previewRuntime);");
        AssertDoesNotContain(diagnostics.SnapshotsText, "var previewHdrInputDetected =");
        AssertContains(diagnostics.SnapshotsText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
    }
}
