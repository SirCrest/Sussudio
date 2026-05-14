using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "App wires recoverable and fatal unhandled exception policy",
            App_Xaml_WiresUnhandledExceptionPolicy);
        await AddCheckAsync(results,
            "Bool converters preserve inversion and visibility mappings",
            BoolConverters_PreserveInversionAndVisibilityMappings);
        await AddCheckAsync(results,
            "Display formatters map source HDR states",
            DisplayFormatters_FormatSourceHdr_MapsKnownAndUnknownStates);
        await AddCheckAsync(results,
            "Logging JSON context serializes structured snapshot payloads",
            LoggingJsonContext_SerializesStructuredSnapshotPayloads);
        await AddCheckAsync(results,
            "UI automation commands are not blocked on device readiness",
            UiAutomationCommands_AreNotBlockedOnDeviceReadiness);
        await AddCheckAsync(results,
            "MainWindow automation IDs cover the agent-critical UI surface",
            MainWindowAutomationIds_CoverAgentCriticalSurface);
        await AddCheckAsync(results,
            "MainWindow full-screen automation awaits transition tasks",
            MainWindowFullScreenAutomation_AwaitsTransitionTask);
        await AddCheckAsync(results,
            "MainWindow window automation commands live in controller",
            MainWindowWindowAutomationCommands_LiveInController);
        await AddCheckAsync(results,
            "MainWindow UI dispatching lives in dispatching partial",
            MainWindowUiDispatching_LivesInDispatchingPartial);
        await AddCheckAsync(results,
            "Automation dispatcher extracts string payload fields",
            AutomationCommandDispatcher_GetString_ExtractsFromJsonPayload);
        await AddCheckAsync(results,
            "Automation dispatcher extracts bool payload fields",
            AutomationCommandDispatcher_GetBool_ExtractsFromJsonPayload);
        await AddCheckAsync(results,
            "Automation dispatcher extracts int payload fields",
            AutomationCommandDispatcher_GetInt_ExtractsFromJsonPayload);
        await AddCheckAsync(results,
            "Automation dispatcher extracts double payload fields",
            AutomationCommandDispatcher_GetDouble_ExtractsFromJsonPayload);
        await AddCheckAsync(results,
            "Automation dispatcher rejects non-finite double payload fields",
            AutomationCommandDispatcher_GetDouble_RejectsNonFiniteValues);
        await AddCheckAsync(results,
            "Automation dispatcher requires missing string fields",
            AutomationCommandDispatcher_RequireString_ThrowsOnMissing);
        await AddCheckAsync(results,
            "Automation dispatcher ready-device gate classifies commands",
            AutomationCommandDispatcher_RequiresReadyDevices_ClassifiesCommands);
        await AddCheckAsync(results,
            "Automation dispatcher window close waits for completion",
            AutomationCommandDispatcher_WindowClose_AwaitsCloseCompletion);
        await AddCheckAsync(results,
            "Automation dispatcher preview health waits for first visual",
            AutomationCommandDispatcher_PreviewRendererHealthy_RequiresFirstVisual);
        await AddCheckAsync(results,
            "Automation dispatcher authorization contract is token-gated",
            AutomationCommandDispatcher_AuthorizesConfiguredTokens);
        await AddCheckAsync(results,
            "Automation dispatcher manifest command is read-only and readiness-independent",
            AutomationCommandDispatcher_GetAutomationManifest_IsReadOnlyAndReadinessIndependent);
        await AddCheckAsync(results,
            "Automation dispatcher flashback failures return playback diagnostics",
            AutomationCommandDispatcher_FlashbackActionFailure_ReturnsPlaybackDiagnostics);
        await AddCheckAsync(results,
            "Automation dispatcher Flashback commands live in focused partial",
            AutomationCommandDispatcher_FlashbackCommands_LiveInFocusedPartial);
        await AddCheckAsync(results,
            "Automation dispatcher handles every AutomationCommandKind value",
            AutomationCommandDispatcher_AllCommandKinds_AreHandled);
        await AddCheckAsync(results,
            "Automation pipe server gates default security fallback on auth token",
            NamedPipeAutomationServer_GatesDefaultSecurityFallbackOnAuthToken);
        await AddCheckAsync(results,
            "MainWindow wires automation pipe auth fallback policy",
            MainWindowAutomation_WiresPipeAuthFallbackPolicy);
        await AddCheckAsync(results,
            "Stream Deck scope documents automation auth envelope",
            StreamDeckPluginScope_DocumentsAutomationAuthEnvelope);
        await AddCheckAsync(results,
            "Automation preview volume persists through the settings path",
            AutomationPreviewVolume_PersistsThroughSettingsPath);
        await AddCheckAsync(results,
            "Automation UI settings persist through the settings path",
            AutomationUiSettings_PersistThroughSettingsPath);
        await AddCheckAsync(results,
            "Automation device selection routes through apply reinit",
            AutomationDeviceSelection_RoutesThroughApplyReinit);
        await AddCheckAsync(results,
            "Automation capture mode changes await reinitialization",
            AutomationCaptureModeChanges_AwaitReinitialization);
        await AddCheckAsync(results,
            "Automation recording transitions use shared lifecycle gate",
            MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate);
        await AddCheckAsync(results,
            "Automation flashback and probe commands use async view-model surface",
            MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface);
        await AddCheckAsync(results,
            "Main window flashback scrub ends on release cancel and capture lost",
            MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost);
        await AddCheckAsync(results,
            "Main window flashback toggle rolls back UI state on failure",
            MainWindowFlashbackToggle_RollsBackUiStateOnFailure);
        await AddCheckAsync(results,
            "Flashback polling timers live in controller",
            FlashbackPollingTimers_LiveInController);
        await AddCheckAsync(results,
            "Flashback playhead motion lives in focused partial",
            FlashbackPlayheadMotion_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback marker presentation lives in focused partial",
            FlashbackMarkerPresentation_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Flashback mutations route through capture coordinator",
            MainViewModelCapture_RoutesFlashbackMutationsThroughCoordinator);
        await AddCheckAsync(results,
            "Flashback exports release backend lease before native export",
            CaptureService_FlashbackExportsReleaseBackendLeaseBeforeNativeExport);
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
            "AutomationCommandKind source ownership is contract-aligned",
            AutomationCommandKind_SourceOwnership_IsModelAligned);
        await AddCheckAsync(results,
            "Diagnostics snapshot refresh is serialized for recording responses",
            DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses);
        await AddCheckAsync(results,
            "Automation diagnostics snapshot status projection lives in focused partial",
            AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics snapshot evaluation projection lives in focused partial",
            AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics audio projection lives in focused partial",
            AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics capture command projection lives in focused partial",
            AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics user settings projection lives in focused partial",
            AutomationDiagnosticsUserSettingsProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics capture format projection lives in focused partial",
            AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics capture transport projection lives in focused partial",
            AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics HDR pipeline projection lives in focused partial",
            AutomationDiagnosticsHdrPipelineProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics capture cadence projection lives in focused partial",
            AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics MJPEG projection lives in focused partial",
            AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics source signal projection lives in focused partial",
            AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics source telemetry projection lives in focused partial",
            AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics recording pipeline projection lives in focused partial",
            AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics recording backend projection lives in focused partial",
            AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics recording output projection lives in focused partial",
            AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics process resource projection lives in focused partial",
            AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics AV sync projection lives in focused partial",
            AutomationDiagnosticsAvSyncProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics preview runtime projection lives in focused partial",
            AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics preview D3D projection lives in focused partial",
            AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics Flashback export projection lives in focused partial",
            AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics Flashback recording projection lives in focused partial",
            AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics Flashback playback projection lives in focused partial",
            AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation SetRecordingEnabled uses recording-sized client timeout",
            AutomationProtocol_SetRecordingUsesRecordingSizedTimeout);
    }
}
