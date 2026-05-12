namespace Sussudio.Tools;

internal readonly record struct DiagnosticSessionScenarioPlan(
    bool RunFlashbackPlayback,
    bool RunFlashbackStress,
    bool RunFlashbackScrubStress,
    bool RunFlashbackRestartCycle,
    bool RunFlashbackEncoderCycle,
    bool RunFlashbackExportPlayback,
    bool RunFlashbackSegmentPlayback,
    bool RunFlashbackRangeExport,
    bool RunFlashbackRangeExportAudioSwitch,
    bool RunFlashbackLifecycle,
    bool RunFlashbackExportConcurrent,
    bool RunFlashbackDisableDuringExport,
    bool RunFlashbackRotatedExport,
    bool RunFlashbackPreviewCycle,
    bool RunFlashbackPlaybackPreviewCycle,
    bool RunFlashbackRecording,
    bool RunFlashbackRecordingPreviewCycle,
    bool RunFlashbackRecordingSettingsDeferred,
    bool RunFlashbackRecordingExportRejected,
    bool RunFlashbackExportRejected,
    bool RunCombined)
{
    internal static DiagnosticSessionScenarioPlan From(string scenario)
        => new(
            scenario == DiagnosticSessionScenarios.FlashbackPlayback,
            scenario == DiagnosticSessionScenarios.FlashbackStress,
            scenario == DiagnosticSessionScenarios.FlashbackScrubStress,
            scenario == DiagnosticSessionScenarios.FlashbackRestartCycle,
            scenario == DiagnosticSessionScenarios.FlashbackEncoderCycle,
            scenario == DiagnosticSessionScenarios.FlashbackExportPlayback,
            scenario == DiagnosticSessionScenarios.FlashbackSegmentPlayback,
            scenario == DiagnosticSessionScenarios.FlashbackRangeExport,
            scenario == DiagnosticSessionScenarios.FlashbackRangeExportAudioSwitch,
            scenario == DiagnosticSessionScenarios.FlashbackLifecycle,
            scenario == DiagnosticSessionScenarios.FlashbackExportConcurrent,
            scenario == DiagnosticSessionScenarios.FlashbackDisableDuringExport,
            scenario == DiagnosticSessionScenarios.FlashbackRotatedExport,
            scenario == DiagnosticSessionScenarios.FlashbackPreviewCycle,
            scenario == DiagnosticSessionScenarios.FlashbackPlaybackPreviewCycle,
            scenario == DiagnosticSessionScenarios.FlashbackRecording,
            scenario == DiagnosticSessionScenarios.FlashbackRecordingPreviewCycle,
            scenario == DiagnosticSessionScenarios.FlashbackRecordingSettingsDeferred,
            scenario == DiagnosticSessionScenarios.FlashbackRecordingExportRejected,
            scenario == DiagnosticSessionScenarios.FlashbackExportRejected,
            scenario == DiagnosticSessionScenarios.Combined);

    internal bool RequiresFlashbackRecordingReadiness
        => RunFlashbackRecording ||
           RunFlashbackRecordingPreviewCycle ||
           RunFlashbackRecordingSettingsDeferred ||
           RunFlashbackRecordingExportRejected;

    internal bool RequiresFlashbackRecordingValidation
        => RequiresFlashbackRecordingReadiness;

    internal bool UsesFlashbackScenarioWarningPolicy
        => RunFlashbackPlayback ||
           RunFlashbackStress ||
           RunFlashbackScrubStress ||
           RunFlashbackRestartCycle ||
           RunFlashbackEncoderCycle ||
           RunFlashbackExportPlayback ||
           RunFlashbackSegmentPlayback ||
           RunFlashbackRangeExport ||
           RunFlashbackRangeExportAudioSwitch ||
           RunFlashbackLifecycle ||
           RunFlashbackExportConcurrent ||
           RunFlashbackDisableDuringExport ||
           RunFlashbackRotatedExport ||
           RunFlashbackPreviewCycle ||
           RunFlashbackPlaybackPreviewCycle ||
           RunFlashbackRecording ||
           RunFlashbackRecordingPreviewCycle ||
           RunFlashbackRecordingSettingsDeferred ||
           RunFlashbackRecordingExportRejected ||
           RunFlashbackExportRejected ||
           RunCombined;

    internal bool ToleratesSourceSignalHealthWarning
        => RunFlashbackRangeExport ||
           RunFlashbackRangeExportAudioSwitch ||
           RunFlashbackExportConcurrent ||
           RunFlashbackDisableDuringExport ||
           RunFlashbackRotatedExport ||
           RunFlashbackPreviewCycle ||
           RunFlashbackPlaybackPreviewCycle;

    internal bool ToleratesFlashbackForceRotateDrainWarning
        => RunFlashbackExportPlayback ||
           RunFlashbackScrubStress ||
           RunFlashbackRangeExport ||
           RunFlashbackRangeExportAudioSwitch ||
           RunFlashbackExportConcurrent ||
           RunFlashbackDisableDuringExport ||
           RunFlashbackRotatedExport;

    internal bool IsPreviewCycleScenario
        => RunFlashbackPreviewCycle ||
           RunFlashbackPlaybackPreviewCycle ||
           RunFlashbackRecordingPreviewCycle;

    internal bool ToleratesSparsePreviewSchedulerStressTransitions
        => RunFlashbackScrubStress || RunFlashbackSegmentPlayback;
}
