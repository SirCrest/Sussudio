using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsCaptureAndFlashbackRoutingChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Flashback mutations route through capture coordinator",
            MainViewModelCapture_RoutesFlashbackMutationsThroughCoordinator);
        await AddCheckAsync(results,
            "Flashback exports release backend lease before native export",
            CaptureService_FlashbackExportsReleaseBackendLeaseBeforeNativeExport);
        await AddCheckAsync(results,
            "MainViewModel Flashback export routes through coordinator and owns CTS lifecycle",
            MainViewModelFlashbackExport_RoutesThroughCoordinatorAndOwnsCtsLifecycle);
        await AddCheckAsync(results,
            "Retained flashback preview pipeline recycles on settings changes",
            CaptureService_RecyclesRetainedFlashbackPreviewPipeline_WhenSettingsChange);
        await AddCheckAsync(results,
            "Device switch teardown stops video before flashback disposal",
            CaptureService_DeviceSwitchTeardown_StopsVideoBeforeFlashbackDisposal);
        await AddCheckAsync(results,
            "Flashback lifecycle logs use outcome names",
            CaptureService_FlashbackLifecycleLogs_UseOutcomeNames);
        await AddCheckAsync(results,
            "Flashback frame-rate rational matches delivered cadence",
            CaptureService_FlashbackFrameRateParts_PreserveOnlyDeliveredCadenceRational);
        await AddCheckAsync(results,
            "Flashback enable/disable preserves preview state",
            CaptureService_FlashbackEnableDisable_PreservesPreviewState);
        await AddCheckAsync(results,
            "Capture session coordinator exposes expected lifecycle API",
            CaptureSessionCoordinator_HasExpectedPublicMethods);
        await AddCheckAsync(results,
            "Capture session coordinator command kind covers flashback commands",
            CaptureSessionCoordinator_CaptureCommandKind_HasExpectedValues);
        await AddCheckAsync(results,
            "Capture session snapshot exposes lifecycle contract",
            CaptureSessionCoordinator_CaptureSessionSnapshot_HasFullContract);
        await AddCheckAsync(results,
            "Capture session transition policy defines core lifecycle rules",
            CaptureSessionTransitionPolicy_DefinesCoreLifecycleRules);
        await AddCheckAsync(results,
            "Capture session transition policy resolves steady state",
            CaptureSessionTransitionPolicy_ResolvesSteadyStateFromRuntimeFlags);
        await AddCheckAsync(results,
            "Capture service transition lock uses transition policy",
            CaptureService_RunTransition_UsesTransitionPolicy);
        await AddCheckAsync(results,
            "Capture service session-state writers stay in lifecycle partials",
            CaptureService_SessionStateWritersStayInLifecyclePartials);
        await AddCheckAsync(results,
            "Capture session coordinator cancellation and worker tokens stay bounded",
            CaptureSessionCoordinator_CancellationAndWorkerTokensStayBounded);
        await AddCheckAsync(results,
            "Capture session coordinator accounts canceled queued commands",
            CaptureSessionCoordinator_CanceledQueuedCommandUpdatesAccounting);
        await AddCheckAsync(results,
            "Capture session coordinator coalesces latest queued command behaviorally",
            CaptureSessionCoordinator_CoalescesQueuedLatestOnlyAndAccountsSkip);
        await AddCheckAsync(results,
            "Capture session coordinator dispose drains queued commands before cancellation",
            CaptureSessionCoordinator_DisposeDrainsQueuedCommandBeforeCancellation);
        await AddCheckAsync(results,
            "Capture session coordinator coalesces flashback encoder cycles",
            CaptureSessionCoordinator_CoalescesFlashbackEncoderCycles);
        await AddCheckAsync(results,
            "Capture session coordinator disposal accounting classifies canceled queued commands",
            CaptureSessionCoordinator_DisposalAccounting_ClassifiesCanceledQueuedCommands);
        await AddCheckAsync(results,
            "Capture session coordinator propagates flashback mutation cancellation",
            CaptureSessionCoordinator_FlashbackMutationsPropagateRequestCancellation);
        await AddCheckAsync(results,
            "Capture session coordinator keeps committed stops uncancelable",
            CaptureSessionCoordinator_CommittedStopsDoNotPropagateRequestCancellation);
        await AddCheckAsync(results,
            "Capture session coordinator logs inactive flashback command rejections",
            CaptureSessionCoordinator_LogsInactiveFlashbackCommandRejections);
        await AddCheckAsync(results,
            "Capture session coordinator models live in focused file",
            CaptureSessionCoordinator_ModelsLiveInFocusedFile);
        await AddCheckAsync(results,
            "Capture session coordinator command facade lives in focused partial",
            CaptureSessionCoordinator_CommandFacadeLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Capture session coordinator Flashback facade lives in focused partial",
            CaptureSessionCoordinator_FlashbackFacadeLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Capture session coordinator queue worker lives in focused partial",
            CaptureSessionCoordinator_QueueWorkerLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Capture session coordinator snapshot projection lives in focused partial",
            CaptureSessionCoordinator_SnapshotProjectionLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Capture session coordinator disposal lives in focused partial",
            CaptureSessionCoordinator_DisposalLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Service namespaces follow service folders",
            ServiceNamespaces_FollowServiceFolders);
        await AddCheckAsync(results,
            "MF device enumerator source ownership lives in focused partials",
            MfDeviceEnumerator_SourceOwnershipLivesInFocusedPartials);
        await AddCheckAsync(results,
            "Capture discovery source ownership lives in focused partials",
            CaptureDiscoverySourceOwnership_LivesInFocusedPartials);
        await AddCheckAsync(results,
            "AutomationCommandKind source ownership is contract-aligned",
            AutomationContracts_SourceOwnership_IsModelAligned);
        await AddCheckAsync(results,
            "Diagnostics snapshot refresh is serialized for recording responses",
            DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses);
    }
}
