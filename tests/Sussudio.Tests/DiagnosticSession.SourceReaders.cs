static partial class Program
{
    private static string ReadDiagnosticSessionBackgroundTasksSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionBackgroundTasks.cs",
            "tools/Common/DiagnosticSessionBackgroundTasks.FaultDrain.cs");

    private static string ReadDiagnosticSessionCleanupActionsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionCleanupActions.cs",
            "tools/Common/DiagnosticSessionCleanupActions.Recording.cs",
            "tools/Common/DiagnosticSessionCleanupActions.StateRestore.cs");

    private static string ReadDiagnosticSessionFlashbackCycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.Restart.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.Encoder.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.Registrations.cs");

    private static string ReadDiagnosticSessionFlashbackExportScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Concurrent.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Rotated.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeSelection.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeValidation.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeCleanup.cs");

    private static string ReadDiagnosticSessionFlashbackMetricsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackMetrics.Models.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.Recording.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.OnePercentLow.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.FrameDecode.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.AudioMaster.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs");

    private static string ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackExport.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackExport.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs");

    private static string ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs",
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs");

    private static string ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs",
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Validation.cs",
            "tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs");

    private static string ReadDiagnosticSessionFlashbackStressScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackStressScenario.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.Stress.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.WarmPlayback.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.CommandDrain.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.Scrub.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.ScrubDrain.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.AudioMaster.cs");

    private static string ReadDiagnosticSessionFlashbackWaitsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackWaits.cs",
            "tools/Common/DiagnosticSessionFlashbackWaits.Playback.cs");

    private static string ReadDiagnosticSessionMetricsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionMetrics.Models.cs",
            "tools/Common/DiagnosticSessionMetrics.Cadence.cs",
            "tools/Common/DiagnosticSessionMetrics.PreviewD3D.cs",
            "tools/Common/DiagnosticSessionMetrics.PlaybackCommands.cs",
            "tools/Common/DiagnosticSessionMetrics.Counters.cs");

    private static string ReadDiagnosticSessionModelsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionOptions.cs",
            "tools/Common/DiagnosticSessionResult.cs",
            "tools/Common/DiagnosticSessionResult.Overview.cs",
            "tools/Common/DiagnosticSessionResult.CaptureSource.cs",
            "tools/Common/DiagnosticSessionResult.PreviewCadence.cs",
            "tools/Common/DiagnosticSessionResult.PreviewScheduler.cs",
            "tools/Common/DiagnosticSessionResult.PreviewD3D.cs",
            "tools/Common/DiagnosticSessionResult.PreviewVisualCadence.cs",
            "tools/Common/DiagnosticSessionResult.FlashbackPlayback.Commands.cs",
            "tools/Common/DiagnosticSessionResult.FlashbackPlayback.Cadence.cs",
            "tools/Common/DiagnosticSessionResult.FlashbackPlayback.Decode.cs",
            "tools/Common/DiagnosticSessionResult.FlashbackPlayback.AudioMaster.cs",
            "tools/Common/DiagnosticSessionResult.FlashbackPlayback.Stage.cs",
            "tools/Common/DiagnosticSessionResult.FlashbackRecording.cs",
            "tools/Common/DiagnosticSessionResult.FlashbackExport.cs",
            "tools/Common/DiagnosticSessionSample.cs");

    private static string ReadDiagnosticSessionResultBuilderSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionResultBuilder.cs",
            "tools/Common/DiagnosticSessionResultBuilder.Flattening.cs",
            "tools/Common/DiagnosticSessionResultBuilder.OverviewResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.Analysis.cs",
            "tools/Common/DiagnosticSessionResultBuilder.AnalysisValidation.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackWarnings.cs",
            "tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs",
            "tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs",
            "tools/Common/DiagnosticSessionResultBuilder.PreviewSchedulerValidation.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCommandsResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCadenceResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackDecodeResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackAudioMasterResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackStagesResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackProjectionModels.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackRecordingResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.FlashbackExportResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.CaptureResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.PreviewResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.PreviewD3DResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs",
            "tools/Common/DiagnosticSessionResultBuilder.Models.cs");

    private static string ReadDiagnosticSessionResultFormatterSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionResultFormatter.cs",
            "tools/Common/DiagnosticSessionResultFormatter.Overview.cs",
            "tools/Common/DiagnosticSessionResultFormatter.Flashback.cs",
            "tools/Common/DiagnosticSessionResultFormatter.FlashbackRecording.cs",
            "tools/Common/DiagnosticSessionResultFormatter.FlashbackExport.cs",
            "tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Performance.cs",
            "tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Decode.cs",
            "tools/Common/DiagnosticSessionResultFormatter.Preview.cs",
            "tools/Common/DiagnosticSessionResultFormatter.PreviewD3D.cs",
            "tools/Common/DiagnosticSessionResultFormatter.PreviewVisualCadence.cs",
            "tools/Common/DiagnosticSessionResultFormatter.Artifacts.cs");

    private static string ReadDiagnosticSessionRunnerSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs",
            "tools/Common/DiagnosticSessionRunExecution.cs",
            "tools/Common/DiagnosticSessionRunContext.cs",
            "tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs",
            "tools/Common/DiagnosticSessionRunExecution.Completion.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.Models.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.Sampling.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs");

    private static string ReadDiagnosticSessionRunExecutionRootSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.cs");

    private static string ReadDiagnosticSessionRunContextSource()
        => ReadDiagnosticSessionRunContextRootSource()
            + "\n" + ReadDiagnosticSessionRunContextPhaseContextsSource();

    private static string ReadDiagnosticSessionRunContextRootSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs");

    private static string ReadDiagnosticSessionRunContextPhaseContextsSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs");

    private static string ReadDiagnosticSessionRunExecutionScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.Models.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseRunner.Sampling.cs",
            "tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunExecution.Completion.cs");

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
