static partial class Program
{
    private static string ReadDiagnosticSessionBackgroundTasksSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionBackgroundTasks.cs",
            "tools/Common/DiagnosticSessionBackgroundTasks.PresentMon.cs",
            "tools/Common/DiagnosticSessionBackgroundTasks.RecordingSettingsDeferred.cs",
            "tools/Common/DiagnosticSessionBackgroundTasks.FaultDrain.cs");

    private static string ReadDiagnosticSessionCleanupActionsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionCleanupActions.cs",
            "tools/Common/DiagnosticSessionCleanupActions.Recording.cs",
            "tools/Common/DiagnosticSessionCleanupActions.FlashbackPlayback.cs",
            "tools/Common/DiagnosticSessionCleanupActions.Preview.cs",
            "tools/Common/DiagnosticSessionCleanupActions.FlashbackState.cs");

    private static string ReadDiagnosticSessionScenarioSetupSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionScenarioSetup.cs",
            "tools/Common/DiagnosticSessionScenarioSetup.Flashback.cs",
            "tools/Common/DiagnosticSessionScenarioSetup.Preview.cs",
            "tools/Common/DiagnosticSessionScenarioSetup.Recording.cs",
            "tools/Common/DiagnosticSessionScenarioSetup.Results.cs");

    private static string ReadDiagnosticSessionFlashbackCycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.Restart.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.RestartValidation.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.RestartExport.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.Encoder.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.EncoderValidation.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.EncoderExport.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.EncoderRestore.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.Registrations.cs");

    private static string ReadDiagnosticSessionFlashbackExportScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Concurrent.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExportValidation.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.Range.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.Coordination.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Rotated.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackPreExport.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackPostExport.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackFinalState.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeSelection.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeSelection.Markers.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeValidation.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeCleanup.cs");

    private static string ReadDiagnosticSessionFlashbackLifecycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs",
            "tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.Validation.cs");

    private static string ReadDiagnosticSessionFlashbackMetricsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackMetrics.Recording.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.Model.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs");

    private static string ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackPreStop.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackStopped.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackRestartValidation.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackExport.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackPreStop.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackStopped.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackRestart.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackExport.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.RecordingCounters.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.RecordingValidation.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.RecordingRestartValidation.cs");

    private static string ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DeferredPresetState.cs",
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs",
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecordingRejections.cs",
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecordingValidation.cs",
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs",
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStopRestore.cs");

    private static string ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs",
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Target.cs",
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.LiveRestore.cs",
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Validation.cs",
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs");

    private static string ReadDiagnosticSessionFlashbackSegmentsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackSegments.CompletedWaits.cs",
            "tools/Common/DiagnosticSessionFlashbackSegments.PlaybackTargetWaits.cs",
            "tools/Common/DiagnosticSessionFlashbackSegments.PlaybackHeadroomWaits.cs",
            "tools/Common/DiagnosticSessionFlashbackSegments.Parsing.cs");

    private static string ReadDiagnosticSessionFlashbackStressScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackStressScenario.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.Stress.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.StressExport.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.WarmPlayback.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.WarmPlaybackAudio.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.CommandDrainWait.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.CommandDrain.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.Scrub.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.ScrubUpdates.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.ScrubDrain.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.AudioMaster.cs");

    private static string ReadDiagnosticSessionFlashbackWaitsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackWaits.cs",
            "tools/Common/DiagnosticSessionFlashbackWaits.RecordingReady.cs",
            "tools/Common/DiagnosticSessionFlashbackWaits.BufferReady.cs",
            "tools/Common/DiagnosticSessionFlashbackWaits.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackWaits.PlaybackBoundary.cs",
            "tools/Common/DiagnosticSessionFlashbackWaits.PlaybackWarmSample.cs",
            "tools/Common/DiagnosticSessionFlashbackWaits.PlaybackPosition.cs");

    private static string ReadDiagnosticSessionMetricsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionMetrics.Cadence.cs",
            "tools/Common/DiagnosticSessionMetrics.PreviewD3D.cs",
            "tools/Common/DiagnosticSessionMetrics.PlaybackCommands.cs",
            "tools/Common/DiagnosticSessionMetrics.Counters.cs");

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
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackWarnings.cs",
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
            "tools/Common/DiagnosticSessionRunContext.InitialSnapshot.cs",
            "tools/Common/DiagnosticSessionRunContext.LiveState.cs",
            "tools/Common/DiagnosticSessionRunContext.Lifetime.cs",
            "tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs",
            "tools/Common/DiagnosticSessionRunExecution.Completion.cs",
            "tools/Common/DiagnosticSessionRunExecution.CompletionContext.cs",
            "tools/Common/DiagnosticSessionRunExecution.ResultBuildRequest.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseContext.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseResult.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseState.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.Sampling.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs");

    private static string ReadDiagnosticSessionRunExecutionRootSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.cs");

    private static string ReadDiagnosticSessionRunContextSource()
        => ReadDiagnosticSessionRunContextRootSource()
            + "\n" + ReadDiagnosticSessionRunContextInitialSnapshotSource()
            + "\n" + ReadDiagnosticSessionRunContextLiveStateSource()
            + "\n" + ReadDiagnosticSessionRunContextLifetimeSource()
            + "\n" + ReadDiagnosticSessionRunContextPhaseContextsSource();

    private static string ReadDiagnosticSessionRunContextRootSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs");

    private static string ReadDiagnosticSessionRunContextInitialSnapshotSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.InitialSnapshot.cs");

    private static string ReadDiagnosticSessionRunContextLiveStateSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.LiveState.cs");

    private static string ReadDiagnosticSessionRunContextLifetimeSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.Lifetime.cs");

    private static string ReadDiagnosticSessionRunContextPhaseContextsSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs");

    private static string ReadDiagnosticSessionRunExecutionScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseContext.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseResult.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseState.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.Sampling.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunExecution.Completion.cs",
            "tools/Common/DiagnosticSessionRunExecution.CompletionContext.cs",
            "tools/Common/DiagnosticSessionRunExecution.ResultBuildRequest.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionRootSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.Completion.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionContextSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.CompletionContext.cs");

    private static string ReadDiagnosticSessionRunExecutionResultBuildRequestSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.ResultBuildRequest.cs");

    private static string ReadDiagnosticSessionScenarioStartupSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionScenarioStartup.cs",
            "tools/Common/DiagnosticSessionScenarioStartup.Registrations.cs",
            "tools/Common/DiagnosticSessionScenarioStartup.Playback.cs");

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
