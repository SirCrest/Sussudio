static partial class Program
{
    private static string ReadDiagnosticSessionBackgroundTasksSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionBackgroundTasks.cs");

    private static string ReadDiagnosticSessionCleanupActionsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionCleanupActions.cs");

    private static string ReadDiagnosticSessionScenarioSetupSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs");

    private static string ReadDiagnosticSessionFlashbackCycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs");

    private static string ReadDiagnosticSessionFlashbackExportScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Concurrent.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExportValidation.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Rotated.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs");

    private static string ReadDiagnosticSessionFlashbackLifecycleScenariosSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs");

    private static string ReadDiagnosticSessionFlashbackMetricsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackMetrics.Recording.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs");

    private static string ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs");

    private static string ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs");

    private static string ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs");

    private static string ReadDiagnosticSessionFlashbackSegmentsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackSegments.cs");

    private static string ReadDiagnosticSessionFlashbackStressScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackStressScenario.cs");

    private static string ReadDiagnosticSessionFlashbackWaitsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackWaits.cs");

    private static string ReadDiagnosticSessionMetricsSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionMetrics.cs");

    private static string ReadDiagnosticSessionModelsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionOptions.cs",
            "tools/Common/DiagnosticSessionResult.cs",
            "tools/Common/DiagnosticSessionSample.cs");

    private static string ReadDiagnosticSessionResultBuilderSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionResultBuildRequest.cs",
            "tools/Common/DiagnosticSessionResultBuilder.cs",
            "tools/Common/DiagnosticSessionResultBuilder.Projections.cs",
            "tools/Common/DiagnosticSessionResultBuilder.Flattening.cs",
            "tools/Common/DiagnosticSessionResultBuilder.Analysis.cs",
            "tools/Common/DiagnosticSessionResultBuilder.AnalysisValidation.cs",
            "tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs",
            "tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs");

    private static string ReadDiagnosticSessionResultFormatterSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionResultFormatter.cs");

    private static string ReadDiagnosticSessionRunnerSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs",
            "tools/Common/DiagnosticSessionRunExecution.cs",
            "tools/Common/DiagnosticSessionRunContext.cs",
            "tools/Common/DiagnosticSessionRunExecution.CompletionContext.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseModels.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs");

    private static string ReadDiagnosticSessionRunExecutionRootSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.cs");

    private static string ReadDiagnosticSessionRunContextSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs");

    private static string ReadDiagnosticSessionRunContextRootSource()
        => ReadDiagnosticSessionRunContextSource();

    private static string ReadDiagnosticSessionRunExecutionScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseModels.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunExecution.cs",
            "tools/Common/DiagnosticSessionRunExecution.CompletionContext.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionRootSource()
        => ReadDiagnosticSessionRunExecutionRootSource();

    private static string ReadDiagnosticSessionRunExecutionCompletionContextSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.CompletionContext.cs");

    private static string ReadDiagnosticSessionScenarioStartupSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionScenarioStartup.cs");

    private static string ReadNormalizedSourceFiles(params string[] paths)
    {
        var parts = new string[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            parts[i] = ReadNormalizedRepoFile(paths[i]);
        }

        return string.Join("\n", parts);
    }
}
