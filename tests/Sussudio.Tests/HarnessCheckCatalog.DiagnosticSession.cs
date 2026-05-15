using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddDiagnosticSessionChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Diagnostic session runner writes terminal artifacts on final snapshot failure",
            DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts);
        await AddCheckAsync(results,
            "Diagnostic session model ownership is split from runner behavior",
            DiagnosticSessionModels_AreSplitFromRunnerBehavior);
        await AddCheckAsync(results,
            "Diagnostic session initial snapshot has a named owner",
            DiagnosticSessionInitialSnapshot_OwnsBaselineCapture);
        await AddCheckAsync(results,
            "Diagnostic session result formatting has a named owner",
            DiagnosticSessionResultFormatter_OwnsFormattedSummaryText);
        await AddCheckAsync(results,
            "Diagnostic session result construction has a named owner",
            DiagnosticSessionResultBuilder_OwnsSummaryConstruction);
        await AddCheckAsync(results,
            "Diagnostic session result health verdict has a named owner",
            DiagnosticSessionResultBuilder_DiagnosticHealthVerdictLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Diagnostic session summary writer has a named owner",
            DiagnosticSessionSummaryWriter_OwnsSummaryWriteFailures);
        await AddCheckAsync(results,
            "Diagnostic session result artifacts have a named owner",
            DiagnosticSessionResultArtifacts_OwnPreSummaryWrites);
        await AddCheckAsync(results,
            "Diagnostic session shared text helpers have a named owner",
            DiagnosticSessionText_OwnsSharedFormattingHelpers);
        await AddCheckAsync(results,
            "Diagnostic session pipe retry policy has a named owner",
            DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification);
        await AddCheckAsync(results,
            "Diagnostic session command channel has a named owner",
            DiagnosticSessionCommandChannel_OwnsSerializedCommandSending);
        await AddCheckAsync(results,
            "Diagnostic session JSON artifacts have a named owner",
            DiagnosticSessionJsonArtifacts_OwnsArtifactsAndResponseExtraction);
        await AddCheckAsync(results,
            "Diagnostic session run state has a named owner",
            DiagnosticSessionRunState_OwnsTerminalAndLiveState);
        await AddCheckAsync(results,
            "Diagnostic session run bootstrap has a named owner",
            DiagnosticSessionRunBootstrap_OwnsNormalizedSessionIdentity);
        await AddCheckAsync(results,
            "Diagnostic session output lock has a named owner",
            DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock);
        await AddCheckAsync(results,
            "Diagnostic session scenario plan has a named owner",
            DiagnosticSessionScenarioPlan_OwnsScenarioFlags);
        await AddCheckAsync(results,
            "Diagnostic session scenario setup has a named owner",
            DiagnosticSessionScenarioSetup_OwnsInitialMutations);
        await AddCheckAsync(results,
            "Diagnostic session background tasks have a named owner",
            DiagnosticSessionBackgroundTasks_OwnTaskDraining);
        await AddCheckAsync(results,
            "Diagnostic session PresentMon startup has a named owner",
            DiagnosticSessionPresentMonStartup_OwnsPresentMonLaunch);
        await AddCheckAsync(results,
            "Diagnostic session cleanup policy has a named owner",
            DiagnosticSessionCleanupPolicy_OwnsRestoreWarnings);
        await AddCheckAsync(results,
            "Diagnostic session recording checks have a named owner",
            DiagnosticSessionRecordingChecks_OwnPostRunRecordingVerification);
        await AddCheckAsync(results,
            "Diagnostic session post-run snapshots have a named owner",
            DiagnosticSessionPostRunSnapshots_OwnTimelineAndFinalSnapshot);
        await AddCheckAsync(results,
            "Diagnostic session Flashback cycle scenarios have a named owner",
            DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows);
        await AddCheckAsync(results,
            "Diagnostic session sampler has a named owner",
            DiagnosticSessionSampler_OwnsSampleLoopOrdering);
        await AddCheckAsync(results,
            "Diagnostic session metrics have a named owner",
            DiagnosticSessionMetrics_OwnsSessionMetricProjection);
        await AddCheckAsync(results,
            "Diagnostic session Flashback metrics have a named owner",
            DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection);
        await AddCheckAsync(results,
            "Diagnostic session Flashback export force-rotate counters ignore export relevance gate",
            DiagnosticSessionFlashbackMetrics_ExportForceRotateCountersIgnoreRelevanceGate);
        await AddCheckAsync(results,
            "Diagnostic session Flashback preview cycle scenarios have a named owner",
            DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows);
        await AddCheckAsync(results,
            "Diagnostic session Flashback rejected exports have a named owner",
            DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows);
        await AddCheckAsync(results,
            "Diagnostic session Flashback recording settings scenarios have a named owner",
            DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow);
        await AddCheckAsync(results,
            "Diagnostic session Flashback lifecycle scenarios have a named owner",
            DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow);
        await AddCheckAsync(results,
            "Diagnostic session Flashback segment playback scenarios have a named owner",
            DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow);
        await AddCheckAsync(results,
            "Diagnostic session Flashback export scenarios have a named owner",
            DiagnosticSessionFlashbackExportScenarios_OwnExportFlows);
        await AddCheckAsync(results,
            "Diagnostic session Flashback export helpers have a named owner",
            DiagnosticSessionFlashbackExports_OwnsExportHelpers);
        await AddCheckAsync(results,
            "Diagnostic session Flashback segment waits have a named owner",
            DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing);
        await AddCheckAsync(results,
            "Diagnostic session Flashback stress scenario has a named owner",
            DiagnosticSessionFlashbackStressScenario_OwnsStressFlow);
        await AddCheckAsync(results,
            "Diagnostic session Flashback snapshot waits have a named owner",
            DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits);
        await AddCheckAsync(results,
            "Diagnostic session Flashback validation has a named owner",
            DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy);
        await AddCheckAsync(results,
            "Diagnostic session health policy has a named owner",
            DiagnosticSessionHealthPolicy_OwnsHealthTolerances);
        await AddCheckAsync(results,
            "Diagnostic session runner verifies flashback export during playback",
            DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow);
        await AddCheckAsync(results,
            "Diagnostic session runner ignores transient flashback warmup warnings",
            DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings);
        await AddCheckAsync(results,
            "Diagnostic session runner tolerates sparse source cadence warnings only without source drops",
            DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops);
        await AddCheckAsync(results,
            "Diagnostic session runner fails unknown initial snapshot without mutating state",
            DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState);
        await AddCheckAsync(results,
            "Diagnostic session runner retries synthetic pipe connect failures",
            DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures);
        await AddCheckAsync(results,
            "Diagnostic session runner rejects concurrent invocation on same output directory",
            DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory);
        await AddCheckAsync(results,
            "Diagnostic session Flashback stress scenario classifies audio-master fallbacks",
            DiagnosticSessionFlashbackStressScenario_ClassifiesAudioMasterFallbacks);
    }
}
