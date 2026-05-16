using System.Threading.Tasks;

static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderAnalysisWarningsOwnership()
    {
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var flashbackWarningsText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackWarnings.cs")
            .Replace("\r\n", "\n");

        AssertContains(analysisText, "AddFlashbackPlaybackAnalysisWarnings(playbackResultMetrics, warnings);");
        AssertContains(analysisText, "AddFlashbackExportAnalysisWarnings(");
        AssertContains(analysisText, "ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "exportMetrics.ForceRotateFallbacksAtEnd,");
        AssertContains(analysisText, "exportMetrics.ForceRotateFallbacksDelta,");
        AssertContains(analysisText, "exportMetrics.LastForceRotateFallbackSegmentsAtEnd,");
        AssertDoesNotContain(analysisText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertDoesNotContain(analysisText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertDoesNotContain(analysisText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertDoesNotContain(analysisText, "var flashbackExportForceRotateFallbacksAtEnd =");
        AssertDoesNotContain(analysisText, "FlashbackExportForceRotateFallbacksAtEnd,");
        AssertDoesNotContain(analysisText, "flashback playback seek forward-decode cap hit during session");
        AssertDoesNotContain(analysisText, "flashback export used force-rotate partial fallback");
        AssertContains(flashbackWarningsText, "private static void AddFlashbackPlaybackAnalysisWarnings(");
        AssertContains(flashbackWarningsText, "private static void AddFlashbackExportAnalysisWarnings(");
        AssertContains(flashbackWarningsText, "flashback playback seek forward-decode cap hit during session");
        AssertContains(flashbackWarningsText, "flashback export used force-rotate partial fallback");
    }

    private static Task DiagnosticSessionResultBuilder_DiagnosticHealthVerdictLivesInFocusedPartial()
    {
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var healthText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs")
            .Replace("\r\n", "\n");

        AssertContains(analysisText, "var diagnosticHealthSucceeded = AnalyzeDiagnosticHealth(");
        AssertContains(healthText, "private static bool AnalyzeDiagnosticHealth(");
        AssertContains(healthText, "BuildSessionDiagnosticHealthObservation(");
        AssertContains(healthText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(healthText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(healthText, "IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(healthText, "diagnostic health degraded during session");
        AssertContains(healthText, "diagnostic health {toleratedReason}:");
        AssertContains(healthText, "flashback force-rotate drain warning tolerated for flashback scenario");
        AssertDoesNotContain(analysisText, "BuildSessionDiagnosticHealthObservation(");
        AssertDoesNotContain(analysisText, "diagnostic health degraded during session");
        AssertDoesNotContain(analysisText, "diagnostic health {toleratedReason}:");

        return Task.CompletedTask;
    }
}
