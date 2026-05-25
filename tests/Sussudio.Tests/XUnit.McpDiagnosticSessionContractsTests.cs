using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class McpDiagnosticSessionCommandRunContextContractsTests
{
    [Fact]
    public Task PipeRetryPolicyOwnsConnectRetryClassification()
        => global::Program.DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification();

    [Fact]
    public Task CommandChannelOwnsSerializedCommandSending()
        => global::Program.DiagnosticSessionCommandChannel_OwnsSerializedCommandSending();

    [Fact]
    public Task JsonArtifactsOwnJsonWritingAndResponseExtractionSplit()
        => global::Program.DiagnosticSessionJsonArtifacts_OwnsJsonWritingAndResponseExtractionSplit();

    [Fact]
    public Task RunStateOwnsTerminalState()
        => global::Program.DiagnosticSessionRunState_OwnsTerminalState();

    [Fact]
    public Task LiveStateWriterOwnsBreadcrumbFile()
        => global::Program.DiagnosticSessionLiveStateWriter_OwnsBreadcrumbFile();

    [Fact]
    public Task RunContextOwnsMutableRunInfrastructure()
        => global::Program.DiagnosticSessionRunContext_OwnsMutableRunInfrastructure();

    [Fact]
    public Task RunBootstrapOwnsNormalizedSessionIdentity()
        => global::Program.DiagnosticSessionRunBootstrap_OwnsNormalizedSessionIdentity();

    [Fact]
    public Task OutputLockOwnsExclusiveOutputDirectoryLock()
        => global::Program.DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock();
}

public sealed class McpDiagnosticSessionCoreContractsTests
{
    [Fact]
    public Task SamplerOwnsSampleLoopOrdering()
        => global::Program.DiagnosticSessionSampler_OwnsSampleLoopOrdering();

    [Fact]
    public Task MetricsOwnSessionMetricProjection()
        => global::Program.DiagnosticSessionMetrics_OwnsSessionMetricProjection();

    [Fact]
    public Task HealthPolicyOwnsHealthTolerances()
        => global::Program.DiagnosticSessionHealthPolicy_OwnsHealthTolerances();
}

public sealed class McpDiagnosticSessionFlashbackContractsTests
{
    [Fact]
    public Task FlashbackCycleScenariosOwnCycleFlows()
        => global::Program.DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows();

    [Fact]
    public Task FlashbackMetricsOwnSessionMetricProjection()
        => global::Program.DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection();

    [Fact]
    public Task FlashbackMetricsExportForceRotateCountersIgnoreRelevanceGate()
        => global::Program.DiagnosticSessionFlashbackMetrics_ExportForceRotateCountersIgnoreRelevanceGate();

    [Fact]
    public Task FlashbackPreviewCycleScenariosOwnPreviewCycleFlows()
        => global::Program.DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows();

    [Fact]
    public Task FlashbackRejectedExportsOwnRejectionFlows()
        => global::Program.DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows();

    [Fact]
    public Task FlashbackRecordingSettingsScenariosOwnDeferredSettingsFlow()
        => global::Program.DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow();

    [Fact]
    public Task FlashbackLifecycleScenariosOwnLifecycleFlow()
        => global::Program.DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow();

    [Fact]
    public Task FlashbackSegmentPlaybackScenariosOwnSegmentPlaybackFlow()
        => global::Program.DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow();

    [Fact]
    public Task FlashbackExportScenariosOwnExportFlows()
        => global::Program.DiagnosticSessionFlashbackExportScenarios_OwnExportFlows();

    [Fact]
    public Task FlashbackExportsOwnExportHelpers()
        => global::Program.DiagnosticSessionFlashbackExports_OwnsExportHelpers();

    [Fact]
    public Task FlashbackSegmentsOwnSegmentWaitsAndParsing()
        => global::Program.DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing();

    [Fact]
    public Task FlashbackStressScenarioOwnsStressFlow()
        => global::Program.DiagnosticSessionFlashbackStressScenario_OwnsStressFlow();

    [Fact]
    public Task FlashbackWaitsOwnSnapshotPollingWaits()
        => global::Program.DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits();

    [Fact]
    public Task FlashbackValidationOwnWarningPolicy()
        => global::Program.DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy();

    [Fact]
    public Task FlashbackStressScenarioClassifiesAudioMasterFallbacks()
        => global::Program.DiagnosticSessionFlashbackStressScenario_ClassifiesAudioMasterFallbacks();
}

public sealed class McpDiagnosticSessionInfrastructureContractsTests
{
    [Fact]
    public Task RunnerWritesTerminalArtifactsOnFinalSnapshotFailure()
        => global::Program.DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts();

    [Fact]
    public Task ModelsAreSplitFromRunnerBehavior()
        => global::Program.DiagnosticSessionModels_AreSplitFromRunnerBehavior();

    [Fact]
    public Task InitialSnapshotOwnsBaselineCapture()
        => global::Program.DiagnosticSessionInitialSnapshot_OwnsBaselineCapture();

    [Fact]
    public Task RunnerOwnsCompatibilitySurface()
        => global::Program.DiagnosticSessionRunner_OwnsCompatibilitySurface();
}

public sealed class McpDiagnosticSessionResultSurfaceContractsTests
{
    [Fact]
    public Task ResultFormatterOwnsFormattedSummaryText()
        => global::Program.DiagnosticSessionResultFormatter_OwnsFormattedSummaryText();

    [Fact]
    public Task ResultBuilderOwnsSummaryConstruction()
        => global::Program.DiagnosticSessionResultBuilder_OwnsSummaryConstruction();

    [Fact]
    public Task ResultBuilderDiagnosticHealthVerdictLivesInFocusedPartial()
        => global::Program.DiagnosticSessionResultBuilder_DiagnosticHealthVerdictLivesInFocusedPartial();

    [Fact]
    public Task ResultBuilderOwnsSummaryWriteFailures()
        => global::Program.DiagnosticSessionResultBuilder_OwnsSummaryWriteFailures();

    [Fact]
    public Task ResultArtifactsOwnPreSummaryWrites()
        => global::Program.DiagnosticSessionResultArtifacts_OwnPreSummaryWrites();

    [Fact]
    public Task OptionalTextFormatterOwnsSharedFormattingHelpers()
        => global::Program.DiagnosticSessionOptionalTextFormatter_OwnsSharedFormattingHelpers();
}

public sealed class McpDiagnosticSessionRunnerBehaviorContractsTests
{
    [Fact]
    public Task VerifiesFlashbackExportPlaybackCommandFlow()
        => global::Program.DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow();

    [Fact]
    public Task IgnoresTransientFlashbackWarmupWarnings()
        => global::Program.DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings();

    [Fact]
    public Task ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops()
        => global::Program.DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops();

    [Fact]
    public Task UnknownInitialSnapshotFailsWithoutMutatingState()
        => global::Program.DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState();

    [Fact]
    public Task RetriesSyntheticPipeConnectFailures()
        => global::Program.DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures();

    [Fact]
    public Task RejectsConcurrentInvocationOnSameOutputDirectory()
        => global::Program.DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory();
}

public sealed class McpDiagnosticSessionScenarioExecutionContractsTests
{
    [Fact]
    public Task RunExecutionScenarioOwnsScenarioPhase()
        => global::Program.DiagnosticSessionRunExecutionScenario_OwnsScenarioPhase();

    [Fact]
    public Task RunExecutionCompletionOwnsPostCleanupEvidenceAndResult()
        => global::Program.DiagnosticSessionRunExecutionCompletion_OwnsPostCleanupEvidenceAndResult();

    [Fact]
    public Task ScenarioPlanOwnsScenarioFlags()
        => global::Program.DiagnosticSessionScenarioPlan_OwnsScenarioFlags();

    [Fact]
    public Task ScenarioSetupOwnsInitialMutations()
        => global::Program.DiagnosticSessionScenarioSetup_OwnsInitialMutations();

    [Fact]
    public Task BackgroundTasksOwnTaskDraining()
        => global::Program.DiagnosticSessionBackgroundTasks_OwnTaskDraining();

    [Fact]
    public Task PresentMonStartupOwnsPresentMonLaunch()
        => global::Program.DiagnosticSessionPresentMonStartup_OwnsPresentMonLaunch();

    [Fact]
    public Task CleanupPolicyOwnsRestoreWarnings()
        => global::Program.DiagnosticSessionAnalysisValidation_OwnsCleanupRestoreWarnings();

    [Fact]
    public Task RecordingChecksOwnPostRunRecordingVerification()
        => global::Program.DiagnosticSessionRecordingChecks_OwnPostRunRecordingVerification();

    [Fact]
    public Task PostRunSnapshotsOwnTimelineAndFinalSnapshot()
        => global::Program.DiagnosticSessionPostRunSnapshots_OwnTimelineAndFinalSnapshot();
}
