namespace Sussudio.Tools;

internal readonly partial record struct DiagnosticSessionScenarioPlan
{
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
