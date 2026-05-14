using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

static partial class Program
{
    private sealed record CheckResult(string Name, bool Passed, string? Detail = null);

    private static Assembly? _assembly;

    private static async Task<int> Main(string[] args)
    {
        var assemblyPath = ResolveAssemblyPath(args);
        if (!File.Exists(assemblyPath))
        {
            Console.Error.WriteLine($"Target assembly not found: {assemblyPath}");
            Console.Error.WriteLine("Build the app first: dotnet build Sussudio/Sussudio.csproj -c Debug -p:Platform=x64");
            return 2;
        }

        _assembly = Assembly.LoadFrom(assemblyPath);

        var results = new List<CheckResult>
        {
            await RunCheckAsync(
                "Observed telemetry uses explicit counters",
                GetRuntimeSnapshot_UsesObservedTelemetryStateInsteadOfInferredCounts),
            await RunCheckAsync(
                "Runtime snapshot preserves MJPG source subtype when observed frames are NV12",
                GetRuntimeSnapshot_PreservesReaderSourceSubtype_WhenObservedFramesAreDecoded),
            await RunCheckAsync(
                "Telemetry alignment mismatch surfaces reason",
                GetRuntimeSnapshot_TelemetryAlignment_Mismatch_WhenSourceModeDiffersFromRequest),
            await RunCheckAsync(
                "Telemetry unavailable maps to unavailable state",
                GetRuntimeSnapshot_TelemetryAlignment_Unavailable_WhenTelemetryUnavailable),
            await RunCheckAsync(
                "HDR truth treats HDR source with SDR request as expected",
                Diagnostics_HdrTruthVerdict_TreatsHdrSourceSdrRequestAsExpected),
            await RunCheckAsync(
                "NativeXu telemetry accepts known 4K X product revisions",
                NativeXuTelemetry_AcceptsKnown4kXProductRevisions),
            await RunCheckAsync(
                "KS extension-unit native helper is split by boundary",
                KsExtensionUnitNative_SourceOwnership_IsSplitByNativeBoundary),
            await RunCheckAsync(
                "NativeXu telemetry rolling poll lives in focused partial",
                NativeXuAtCommandProvider_RollingPollLivesInFocusedPartial),
            await RunCheckAsync(
                "NativeXu audio command sequences live in focused partial",
                NativeXuAtCommandProvider_AudioCommandsLiveInFocusedPartial),
            await RunCheckAsync(
                "NativeXu payload decoding lives in focused partial",
                NativeXuAtCommandProvider_PayloadDecodingLivesInFocusedPartial),
            await RunCheckAsync(
                "Health snapshot propagates structured source telemetry details",
                CaptureHealthSnapshot_PropagatesStructuredSourceTelemetryDetails),
            await RunCheckAsync(
                "Automation snapshots expose high-confidence source telemetry fields",
                AutomationSnapshots_ExposeHighConfidenceSourceTelemetryFields),
            await RunCheckAsync(
                "HDR idle snapshot reports ready pipeline parity",
                GetRuntimeSnapshot_PipelineParity_Ready_WhenHdrRequestedAndIdle),
            await RunCheckAsync(
                "HDR recording mismatch reports violation",
                GetRuntimeSnapshot_PipelineParity_Violation_WhenHdrRequestedButIngressIsSdr),
            await RunCheckAsync(
                "Thread health probes default cleanly when inactive",
                GetRuntimeSnapshot_ThreadHealthProbes_DefaultToZeroWhenInactive),
            await RunCheckAsync(
                "CaptureService encoder codec names map recording formats",
                CaptureService_ResolveEncoderCodecName_MapsFormats),
            await RunCheckAsync(
                "CaptureService encoder output pixel format distinguishes HDR",
                CaptureService_ResolveEncoderOutputPixelFormat_DistinguishesHdr),
            await RunCheckAsync(
                "CaptureService telemetry age computes bounded seconds",
                CaptureService_ResolveTelemetryAgeSeconds_ComputesCorrectly),
            await RunCheckAsync(
                "CaptureService HDR warmup state resolves expected states",
                CaptureService_ResolveHdrWarmupState_ReturnsCorrectStates),
            await RunCheckAsync(
                "CaptureService observed pixel format normalization is stable",
                CaptureService_NormalizeObservedPixelFormat_NormalizesCorrectly),
            await RunCheckAsync(
                "CaptureService source telemetry backend maps origins",
                CaptureService_ResolveSourceTelemetryBackend_MapsOrigins),
            await RunCheckAsync(
                "CaptureService encoder video profile maps formats and HDR",
                CaptureService_ResolveEncoderVideoProfile_MapsFormatsAndHdr),
            await RunCheckAsync(
                "CaptureService tick age uses empty-tick sentinel",
                CaptureService_ComputeTickAge_ReturnsCorrectValues),
            await RunCheckAsync(
                "CaptureService telemetry alignment detects mismatches",
                CaptureService_ResolveTelemetryAlignment_DetectsMismatches),
            await RunCheckAsync(
                "CaptureService telemetry circuit state resolves open and closed",
                CaptureService_ResolveSourceTelemetryCircuitState_ReturnsCorrectState),
            await RunCheckAsync(
                "Health snapshot uses cached MJPEG timing metrics when capture is gone",
                GetHealthSnapshot_UsesCachedMjpegTimingMetricsWhenCaptureIsGone),
            await RunCheckAsync(
                "Diagnostics snapshot mirrors MJPEG timing metrics",
                GetDiagnosticsSnapshot_PropagatesMjpegTimingMetrics),
            await RunCheckAsync(
                "Automation snapshot contract exposes full CPU MJPEG metrics",
                AutomationSnapshot_ExposesFullCpuMjpegMetrics),
            await RunCheckAsync(
                "Frame ledger retains bounded recent events",
                FrameLedger_RetainsBoundedRecentEvents),
            await RunCheckAsync(
                "Frame ledger snapshot contract exposes recent events",
                FrameLedger_SnapshotContractExposesRecentEvents),
            await RunCheckAsync(
                "Recording integrity summary defaults explicitly",
                RecordingIntegritySummary_DefaultsAreExplicit),
            await RunCheckAsync(
                "Recording integrity snapshot contract exposes automation fields",
                RecordingIntegritySnapshotContract_ExposesAutomationFields),
            await RunCheckAsync(
                "Recording integrity automation projection lives in focused partial",
                RecordingIntegrityAutomationProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Recording integrity flags audio discontinuity and drift",
                RecordingIntegritySummary_FlagsAudioDiscontinuityAndDrift),
            await RunCheckAsync(
                "Recording integrity tolerates active in-flight frame",
                RecordingIntegritySummary_ToleratesSingleActiveInFlightFrame),
            await RunCheckAsync(
                "Recording verifier fails when output file is missing",
                RecordingVerifier_ReturnsFailure_WhenFileDoesNotExist),
            await RunCheckAsync(
                "Recording verifier fails when output file is empty",
                RecordingVerifier_ReturnsFailure_WhenFileIsEmpty),
            await RunCheckAsync(
                "Recording verifier fails when output path is null",
                RecordingVerifier_ReturnsFailure_WhenOutputPathIsNull),
            await RunCheckAsync(
                "Recording verifier implements verification interface",
                RecordingVerifier_ImplementsIRecordingVerifier),
            await RunCheckAsync(
                "Recording verifier cadence analysis lives in focused partial",
                RecordingVerifier_CadenceAnalysisLivesInFocusedPartial),
            await RunCheckAsync(
                "Recording verifier probe validation and result shaping live in focused partials",
                RecordingVerifier_ProbeValidationAndResultsLiveInFocusedPartials),
            await RunCheckAsync(
                "Recording verification result exposes expected properties",
                RecordingVerificationResult_HasExpectedProperties),
            await RunCheckAsync(
                "Recording verifier fails when ffprobe is unavailable",
                RecordingVerifier_ReturnsFailure_WhenFfprobeUnavailable),
            await RunCheckAsync(
                "Recording verifier runs ffprobe below normal priority",
                RecordingVerifier_RunsFfprobeBelowNormalPriority),
            await RunCheckAsync(
                "Recording verifier passes HEVC when all fields match",
                RecordingVerifier_PassesVerification_WhenAllFieldsMatch_Hevc),
            await RunCheckAsync(
                "Recording verifier detects H264 codec when HEVC is expected",
                RecordingVerifier_DetectsCodecMismatch_WhenH264InsteadOfHevc),
            await RunCheckAsync(
                "Recording verifier uses flashback export verification format",
                RecordingVerifier_UsesFlashbackExportVerificationFormat),
            await RunCheckAsync(
                "Recording verifier uses flashback recording verification format",
                RecordingVerifier_UsesFlashbackRecordingVerificationFormat),
            await RunCheckAsync(
                "Recording verifier detects resolution mismatch",
                RecordingVerifier_DetectsResolutionMismatch),
            await RunCheckAsync(
                "Recording verifier detects frame-rate mismatch",
                RecordingVerifier_DetectsFrameRateMismatch),
            await RunCheckAsync(
                "Recording verifier passes HDR validation when metadata is present",
                RecordingVerifier_PassesHdrValidation_WhenAllHdrFieldsPresent),
            await RunCheckAsync(
                "Recording verifier detects HDR colorimetry mismatch",
                RecordingVerifier_DetectsHdrColorimetryMismatch),
            await RunCheckAsync(
                "Recording verifier passes H264 format",
                RecordingVerifier_PassesVerification_ForH264Format),
            await RunCheckAsync(
                "Recording verifier tolerates NTSC frame-rate drift",
                RecordingVerifier_PassesNtscFrameRateWithinTolerance),
            await RunCheckAsync(
                "Recording verifier fails when ffprobe exits nonzero",
                RecordingVerifier_ReturnsFailure_WhenFfprobeExitsNonZero),
            await RunCheckAsync(
                "LibAv encoder HDR bitstream filters map codecs",
                LibAvEncoder_GetHdrBitstreamFilterName_MapsCodecs),
            await RunCheckAsync(
                "LibAv encoder chains HDR and MPEG-TS bitstream filters",
                LibAvEncoder_VideoBitstreamFilterSpec_ChainsHdrAndMpegTsFilters),
            await RunCheckAsync(
                "LibAv encoder expected frame sizes match pixel formats",
                LibAvEncoder_GetExpectedFrameSizeBytes_CalculatesCorrectly),
            await RunCheckAsync(
                "LibAv encoder NVENC presets map correctly",
                LibAvEncoder_MapNvencPreset_MapsCorrectly),
            await RunCheckAsync(
                "LibAv encoder throws on negative native errors",
                LibAvEncoder_ThrowIfError_ThrowsOnNegative),
            await RunCheckAsync(
                "LibAv encoder rational inversion swaps numerator and denominator",
                LibAvEncoder_Invert_SwapsNumeratorDenominator),
            await RunCheckAsync(
                "LibAv encoder HDR rationals parse correctly",
                LibAvEncoder_ChromaticityAndLuminanceRationals_ParseCorrectly),
            await RunCheckAsync(
                "LibAv encoder accepts valid options",
                LibAvEncoder_ValidateOptions_AcceptsValidOptions),
            await RunCheckAsync(
                "LibAv encoder rejects empty output path",
                LibAvEncoder_ValidateOptions_RejectsEmptyOutputPath),
            await RunCheckAsync(
                "LibAv encoder rejects zero dimensions",
                LibAvEncoder_ValidateOptions_RejectsZeroDimensions),
            await RunCheckAsync(
                "LibAv encoder rejects HDR with H264",
                LibAvEncoder_ValidateOptions_RejectsHdrWithH264),
            await RunCheckAsync(
                "LibAv encoder rejects HDR without P010",
                LibAvEncoder_ValidateOptions_RejectsHdrWithoutP010),
            await RunCheckAsync(
                "LibAv encoder rejects mismatched frame-rate parts",
                LibAvEncoder_ValidateOptions_RejectsMismatchedFrameRateParts),
            await RunCheckAsync(
                "LibAv encoder fragments MP4 tightly for flashback playback",
                LibAvEncoder_FragmentedMp4UsesShortFragmentsForPlayback),
            await RunCheckAsync(
                "LibAv encoder dumps MPEG-TS headers for rotated flashback segments",
                LibAvEncoder_MpegTsNvencDumpsHeadersForRotatedSegments),
            await RunCheckAsync(
                "LibAv encoder packet writing lives in focused partial",
                LibAvEncoder_PacketWritingLivesInFocusedPartial),
            await RunCheckAsync(
                "LibAv encoder frame copy lives in focused partial",
                LibAvEncoder_FrameCopyLivesInFocusedPartial),
            await RunCheckAsync(
                "LibAv encoder video submission lives in focused partial",
                LibAvEncoder_VideoSubmissionLivesInFocusedPartial),
            await RunCheckAsync(
                "LibAv encoder diagnostics helpers live in focused partial",
                LibAvEncoder_DiagnosticsHelpersLiveInFocusedPartial),
            await RunCheckAsync(
                "LibAv encoder setup and models live in focused partials",
                LibAvEncoder_SetupAndModelsLiveInFocusedPartials),
            await RunCheckAsync(
                "Flashback integrity uses recording-scoped sequence gaps",
                FlashbackRecordingIntegrity_UsesRecordingScopedSequenceGaps),
            await RunCheckAsync(
                "Shared formatter renders recording integrity",
                SharedFormatter_RendersRecordingIntegrity),
            await RunCheckAsync(
                "Automation options contract exposes advanced MCP control state",
                AutomationOptionsSnapshot_ExposesAdvancedControlState),
            await RunCheckAsync(
                "FFmpeg runtime locator prefers app-local ffmpeg folder",
                FfmpegRuntimeLocator_PrefersAppLocalRuntimeFolder),
            await RunCheckAsync(
                "Shared automation formatter renders MJPEG timing section when fields exist",
                SharedFormatter_RendersMjpegTimingSection_WhenFieldsExist),
            await RunCheckAsync(
                "Automation command maps stay aligned for advanced MCP controls",
                AutomationCommandMaps_StayAligned_ForAdvancedMcpControls),
            await RunCheckAsync(
                "Dedicated LibAv verification script uses flashback-off strict workflow",
                DedicatedLibAvVerificationScript_UsesFlashbackOffAndStrictVerification),
            await RunCheckAsync(
                "App wires recoverable and fatal unhandled exception policy",
                App_Xaml_WiresUnhandledExceptionPolicy),
            await RunCheckAsync(
                "Bool converters preserve inversion and visibility mappings",
                BoolConverters_PreserveInversionAndVisibilityMappings),
            await RunCheckAsync(
                "Display formatters map source HDR states",
                DisplayFormatters_FormatSourceHdr_MapsKnownAndUnknownStates),
            await RunCheckAsync(
                "Logging JSON context serializes structured snapshot payloads",
                LoggingJsonContext_SerializesStructuredSnapshotPayloads),
            await RunCheckAsync(
                "UI automation commands are not blocked on device readiness",
                UiAutomationCommands_AreNotBlockedOnDeviceReadiness),
            await RunCheckAsync(
                "MainWindow automation IDs cover the agent-critical UI surface",
                MainWindowAutomationIds_CoverAgentCriticalSurface),
            await RunCheckAsync(
                "MainWindow full-screen automation awaits transition tasks",
                MainWindowFullScreenAutomation_AwaitsTransitionTask),
            await RunCheckAsync(
                "MainWindow window automation commands live in controller",
                MainWindowWindowAutomationCommands_LiveInController),
            await RunCheckAsync(
                "MainWindow UI dispatching lives in dispatching partial",
                MainWindowUiDispatching_LivesInDispatchingPartial),
            await RunCheckAsync(
                "Automation dispatcher extracts string payload fields",
                AutomationCommandDispatcher_GetString_ExtractsFromJsonPayload),
            await RunCheckAsync(
                "Automation dispatcher extracts bool payload fields",
                AutomationCommandDispatcher_GetBool_ExtractsFromJsonPayload),
            await RunCheckAsync(
                "Automation dispatcher extracts int payload fields",
                AutomationCommandDispatcher_GetInt_ExtractsFromJsonPayload),
            await RunCheckAsync(
                "Automation dispatcher extracts double payload fields",
                AutomationCommandDispatcher_GetDouble_ExtractsFromJsonPayload),
            await RunCheckAsync(
                "Automation dispatcher rejects non-finite double payload fields",
                AutomationCommandDispatcher_GetDouble_RejectsNonFiniteValues),
            await RunCheckAsync(
                "Automation dispatcher requires missing string fields",
                AutomationCommandDispatcher_RequireString_ThrowsOnMissing),
            await RunCheckAsync(
                "Automation dispatcher ready-device gate classifies commands",
                AutomationCommandDispatcher_RequiresReadyDevices_ClassifiesCommands),
            await RunCheckAsync(
                "Automation dispatcher window close waits for completion",
                AutomationCommandDispatcher_WindowClose_AwaitsCloseCompletion),
            await RunCheckAsync(
                "Automation dispatcher preview health waits for first visual",
                AutomationCommandDispatcher_PreviewRendererHealthy_RequiresFirstVisual),
            await RunCheckAsync(
                "Automation dispatcher authorization contract is token-gated",
                AutomationCommandDispatcher_AuthorizesConfiguredTokens),
            await RunCheckAsync(
                "Automation dispatcher manifest command is read-only and readiness-independent",
                AutomationCommandDispatcher_GetAutomationManifest_IsReadOnlyAndReadinessIndependent),
            await RunCheckAsync(
                "Automation dispatcher flashback failures return playback diagnostics",
                AutomationCommandDispatcher_FlashbackActionFailure_ReturnsPlaybackDiagnostics),
            await RunCheckAsync(
                "Automation dispatcher handles every AutomationCommandKind value",
                AutomationCommandDispatcher_AllCommandKinds_AreHandled),
            await RunCheckAsync(
                "Automation pipe server gates default security fallback on auth token",
                NamedPipeAutomationServer_GatesDefaultSecurityFallbackOnAuthToken),
            await RunCheckAsync(
                "MainWindow wires automation pipe auth fallback policy",
                MainWindowAutomation_WiresPipeAuthFallbackPolicy),
            await RunCheckAsync(
                "Stream Deck scope documents automation auth envelope",
                StreamDeckPluginScope_DocumentsAutomationAuthEnvelope),
            await RunCheckAsync(
                "Automation preview volume persists through the settings path",
                AutomationPreviewVolume_PersistsThroughSettingsPath),
            await RunCheckAsync(
                "Automation UI settings persist through the settings path",
                AutomationUiSettings_PersistThroughSettingsPath),
            await RunCheckAsync(
                "Automation device selection routes through apply reinit",
                AutomationDeviceSelection_RoutesThroughApplyReinit),
            await RunCheckAsync(
                "Automation capture mode changes await reinitialization",
                AutomationCaptureModeChanges_AwaitReinitialization),
            await RunCheckAsync(
                "Automation recording transitions use shared lifecycle gate",
                MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate),
            await RunCheckAsync(
                "Automation flashback and probe commands use async view-model surface",
                MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface),
            await RunCheckAsync(
                "Main window flashback scrub ends on release cancel and capture lost",
                MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost),
            await RunCheckAsync(
                "Main window flashback toggle rolls back UI state on failure",
                MainWindowFlashbackToggle_RollsBackUiStateOnFailure),
            await RunCheckAsync(
                "Flashback polling timers live in controller",
                FlashbackPollingTimers_LiveInController),
            await RunCheckAsync(
                "Flashback playhead motion lives in focused partial",
                FlashbackPlayheadMotion_LivesInFocusedPartial),
            await RunCheckAsync(
                "Flashback marker presentation lives in focused partial",
                FlashbackMarkerPresentation_LivesInFocusedPartial),
            await RunCheckAsync(
                "Flashback mutations route through capture coordinator",
                MainViewModelCapture_RoutesFlashbackMutationsThroughCoordinator),
            await RunCheckAsync(
                "Flashback exports release backend lease before native export",
                CaptureService_FlashbackExportsReleaseBackendLeaseBeforeNativeExport),
            await RunCheckAsync(
                "Retained flashback preview pipeline recycles on settings changes",
                CaptureService_RecyclesRetainedFlashbackPreviewPipeline_WhenSettingsChange),
            await RunCheckAsync(
                "Device switch teardown stops video before flashback disposal",
                CaptureService_DeviceSwitchTeardown_StopsVideoBeforeFlashbackDisposal),
            await RunCheckAsync(
                "Flashback lifecycle logs use outcome names",
                CaptureService_FlashbackLifecycleLogs_UseOutcomeNames),
            await RunCheckAsync(
                "Flashback frame-rate rational matches delivered cadence",
                CaptureService_FlashbackFrameRateParts_PreserveOnlyDeliveredCadenceRational),
            await RunCheckAsync(
                "Flashback enable/disable preserves preview state",
                CaptureService_FlashbackEnableDisable_PreservesPreviewState),
            await RunCheckAsync(
                "Capture session coordinator exposes expected lifecycle API",
                CaptureSessionCoordinator_HasExpectedPublicMethods),
            await RunCheckAsync(
                "Capture session coordinator command kind covers flashback commands",
                CaptureSessionCoordinator_CaptureCommandKind_HasExpectedValues),
            await RunCheckAsync(
                "Capture session snapshot exposes lifecycle contract",
                CaptureSessionCoordinator_CaptureSessionSnapshot_HasFullContract),
            await RunCheckAsync(
                "Capture session transition policy defines core lifecycle rules",
                CaptureSessionTransitionPolicy_DefinesCoreLifecycleRules),
            await RunCheckAsync(
                "Capture session transition policy resolves steady state",
                CaptureSessionTransitionPolicy_ResolvesSteadyStateFromRuntimeFlags),
            await RunCheckAsync(
                "Capture service transition lock uses transition policy",
                CaptureService_RunTransition_UsesTransitionPolicy),
            await RunCheckAsync(
                "Capture session coordinator accounts canceled queued commands",
                CaptureSessionCoordinator_CanceledQueuedCommandUpdatesAccounting),
            await RunCheckAsync(
                "Capture session coordinator coalesces latest queued command behaviorally",
                CaptureSessionCoordinator_CoalescesQueuedLatestOnlyAndAccountsSkip),
            await RunCheckAsync(
                "Capture session coordinator dispose drains queued commands before cancellation",
                CaptureSessionCoordinator_DisposeDrainsQueuedCommandBeforeCancellation),
            await RunCheckAsync(
                "Capture session coordinator coalesces flashback encoder cycles",
                CaptureSessionCoordinator_CoalescesFlashbackEncoderCycles),
            await RunCheckAsync(
                "Capture session coordinator disposal accounting classifies canceled queued commands",
                CaptureSessionCoordinator_DisposalAccounting_ClassifiesCanceledQueuedCommands),
            await RunCheckAsync(
                "Capture session coordinator propagates flashback mutation cancellation",
                CaptureSessionCoordinator_FlashbackMutationsPropagateRequestCancellation),
            await RunCheckAsync(
                "Capture session coordinator keeps committed stops uncancelable",
                CaptureSessionCoordinator_CommittedStopsDoNotPropagateRequestCancellation),
            await RunCheckAsync(
                "Capture session coordinator logs inactive flashback command rejections",
                CaptureSessionCoordinator_LogsInactiveFlashbackCommandRejections),
            await RunCheckAsync(
                "Capture session coordinator models live in focused file",
                CaptureSessionCoordinator_ModelsLiveInFocusedFile),
            await RunCheckAsync(
                "Capture session coordinator Flashback facade lives in focused partial",
                CaptureSessionCoordinator_FlashbackFacadeLivesInFocusedPartial),
            await RunCheckAsync(
                "Capture session coordinator queue worker lives in focused partial",
                CaptureSessionCoordinator_QueueWorkerLivesInFocusedPartial),
            await RunCheckAsync(
                "Capture session coordinator snapshot projection lives in focused partial",
                CaptureSessionCoordinator_SnapshotProjectionLivesInFocusedPartial),
            await RunCheckAsync(
                "Capture session coordinator disposal lives in focused partial",
                CaptureSessionCoordinator_DisposalLivesInFocusedPartial),
            await RunCheckAsync(
                "Service namespaces follow service folders",
                ServiceNamespaces_FollowServiceFolders),
            await RunCheckAsync(
                "MF device enumerator source ownership lives in focused partials",
                MfDeviceEnumerator_SourceOwnershipLivesInFocusedPartials),
            await RunCheckAsync(
                "AutomationCommandKind source ownership is contract-aligned",
                AutomationCommandKind_SourceOwnership_IsModelAligned),
            await RunCheckAsync(
                "Diagnostics snapshot refresh is serialized for recording responses",
                DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses),
            await RunCheckAsync(
                "Automation diagnostics snapshot status projection lives in focused partial",
                AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics snapshot evaluation projection lives in focused partial",
                AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics audio projection lives in focused partial",
                AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics capture command projection lives in focused partial",
                AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics user settings projection lives in focused partial",
                AutomationDiagnosticsUserSettingsProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics capture format projection lives in focused partial",
                AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics capture transport projection lives in focused partial",
                AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics HDR pipeline projection lives in focused partial",
                AutomationDiagnosticsHdrPipelineProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics capture cadence projection lives in focused partial",
                AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics MJPEG projection lives in focused partial",
                AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics source signal projection lives in focused partial",
                AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics source telemetry projection lives in focused partial",
                AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics recording pipeline projection lives in focused partial",
                AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics recording backend projection lives in focused partial",
                AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics recording output projection lives in focused partial",
                AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics process resource projection lives in focused partial",
                AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics AV sync projection lives in focused partial",
                AutomationDiagnosticsAvSyncProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics preview runtime projection lives in focused partial",
                AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics preview D3D projection lives in focused partial",
                AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics Flashback export projection lives in focused partial",
                AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics Flashback recording projection lives in focused partial",
                AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation diagnostics Flashback playback projection lives in focused partial",
                AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial),
            await RunCheckAsync(
                "Automation SetRecordingEnabled uses recording-sized client timeout",
                AutomationProtocol_SetRecordingUsesRecordingSizedTimeout),
            await RunCheckAsync(
                "Recording start and stop failures propagate to callers",
                MainViewModelCapture_RecordingFailuresPropagateToCallers),
            await RunCheckAsync(
                "Window close cancels until recording stop completes",
                MainWindowClose_CancelsCloseUntilRecordingStopCompletes),
            await RunCheckAsync(
                "Window screenshot capture completes on dispatcher failure and cancellation",
                MainWindowScreenshot_CompletesOnDispatcherFailureAndCancellation),
            await RunCheckAsync(
                "External FFmpeg and HDR probes use bounded process supervision",
                ExternalProcessProbes_UseBoundedProcessSupervisor),
            await RunCheckAsync(
                "Recording stop propagates unified video stop failures",
                RecordingStop_PropagatesUnifiedVideoStopFailure),
            await RunCheckAsync(
                "Preview stop compatibility overloads are preserved",
                PreviewStopCompatibilityOverloads_ArePreserved),
            await RunCheckAsync(
                "Preview stop API surface has no default-literal ambiguity",
                PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity),
            await RunCheckAsync(
                "Emergency recording stop does not dispatch to blocked UI thread",
                EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread),
            await RunCheckAsync(
                "Flashback buffer manager cleans stale session directories",
                FlashbackBufferManager_CleansStaleSessionDirectories),
            await RunCheckAsync(
                "Flashback buffer manager preserves marked recovery sessions",
                FlashbackBufferManager_PreservesMarkedRecoverySessions),
            await RunCheckAsync(
                "Project file preserves main's English-only publish locale policy",
                ProjectFile_PreservesEnglishOnlyPublishLocalePolicy),
            await RunCheckAsync(
                "Show all capture options unlocks source-filtered frame rates",
                ShowAllCaptureOptions_UnlocksSourceFilteredFrameRates),
            await RunCheckAsync(
                "Resolution selection policy lives in focused partial",
                ResolutionSelectionPolicy_LivesInFocusedPartial),
            await RunCheckAsync(
                "Frame-rate timing policy lives in focused partial",
                FrameRateTimingPolicy_LivesInFocusedPartial),
            await RunCheckAsync(
                "Diagnostics loop does not rebuild automation options each poll",
                DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll),
            await RunCheckAsync(
                "Preview startup state lives in preview startup partial",
                PreviewStartup_StateLivesInPreviewStartupPartial),
            await RunCheckAsync(
                "Preview startup tolerates missing audio capture devices",
                PreviewStartup_ToleratesMissingAudioCaptureDevices),
            await RunCheckAsync(
                "Preview startup begins device discovery before recording capability probes finish",
                PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish),
            await RunCheckAsync(
                "Preview startup primes UI and audio before preview reveal",
                PreviewStartup_PrimesUiAndAudioBeforePreviewReveal),
            await RunCheckAsync(
                "Preview stop ramps audio down before preview teardown",
                PreviewStop_RampsAudioDownBeforePreviewTeardown),
            await RunCheckAsync(
                "Audio preview stays inactive when no audio capture device exists",
                AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists),
            await RunCheckAsync(
                "Audio monitoring visuals follow runtime preview activity",
                AudioMonitoringVisuals_FollowRuntimePreviewActivity),
            await RunCheckAsync(
                "Preview backend log reflects video-only fallback",
                PreviewBackendLog_ReflectsVideoOnlyFallback),
            await RunCheckAsync(
                "MainViewModel automation routes preview volume persistence through save hook",
                MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook),
            await RunCheckAsync(
                "MainViewModel capture routes audio monitoring through coordinator",
                MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator),
            await RunCheckAsync(
                "MainViewModel capture settings projection lives in focused partial",
                MainViewModelCaptureSettings_OwnsSettingsProjection),
            await RunCheckAsync(
                "MainViewModel audio controls map analog gain curve and clamp endpoints",
                MainViewModelAudioControls_MapsAnalogGainCurveAndClamps),
            await RunCheckAsync(
                "MainViewModel audio monitoring preserves volume persistence and ramped routing",
                MainViewModelAudioMonitoring_PreservesVolumePersistenceAndRampedRouting),
            await RunCheckAsync(
                "MainViewModel audio controls preserve microphone and device guards",
                MainViewModelAudioControls_PreserveMicrophoneVolumeAndDeviceGuards),
            await RunCheckAsync(
                "Native XU audio control profiles live in focused partial",
                NativeXuAudioControlService_ProfilesLiveInFocusedPartial),
            await RunCheckAsync(
                "Native XU audio control transport lives in focused partial",
                NativeXuAudioControlService_TransportLivesInFocusedPartial),
            await RunCheckAsync(
                "MainViewModel audio meters own callback meter state",
                MainViewModelAudioMeters_OwnCallbackMeterState),
            await RunCheckAsync(
                "Audio ramp trace exposes control and render-side envelope telemetry",
                AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry),
            await RunCheckAsync(
                "Live pixel format surfaces prefer source subtype over decoded output",
                LivePixelFormatSurfaces_PreferReaderSourceSubtype),
            await RunCheckAsync(
                "Stats overlay lifecycle lives in controller",
                StatsOverlayLifecycle_LivesInController),
            await RunCheckAsync(
                "Stats diagnostic row pooling lives in controller",
                StatsDiagnosticRowPooling_LivesInController),
            await RunCheckAsync(
                "Settings shelf lifecycle lives in controller",
                SettingsShelfLifecycle_LivesInController),
            await RunCheckAsync(
                "Splash loading phrases live in controller",
                SplashLoadingPhrases_LiveInController),
            await RunCheckAsync(
                "Launch entrance animation lives in controller",
                LaunchEntranceAnimation_LivesInController),
            await RunCheckAsync(
                "MainWindow startup hosting lives in startup partial",
                MainWindowStartupHosting_LivesInStartupPartial),
            await RunCheckAsync(
                "MainWindow shell resize telemetry lives in sizing partial",
                MainWindowShellResizeTelemetry_LivesInSizingPartial),
            await RunCheckAsync(
                "Preview renderer runtime state lives in renderer partial",
                PreviewRendererRuntimeState_LivesInRendererPartial),
            await RunCheckAsync(
                "MainWindow title presentation lives in title partial",
                MainWindowTitlePresentation_LivesInTitlePartial),
            await RunCheckAsync(
                "MainWindow close lifecycle and native helpers are split",
                MainWindowCloseLifecycleAndNativeHelpers_AreSplit),
            await RunCheckAsync(
                "Control bar hover animations live in controller",
                ControlBarHoverAnimations_LiveInController),
            await RunCheckAsync(
                "Shell elevation setup lives in controller",
                ShellElevationSetup_LivesInController),
            await RunCheckAsync(
                "Preview transition animations live in controller",
                PreviewTransitionAnimations_LiveInController),
            await RunCheckAsync(
                "Record button width animation lives in controller",
                RecordButtonWidthAnimation_LivesInController),
            await RunCheckAsync(
                "Recording button action lives in controller",
                RecordingButtonAction_LivesInController),
            await RunCheckAsync(
                "Live signal info presentation lives in controller",
                LiveSignalInfoPresentation_LivesInController),
            await RunCheckAsync(
                "Preview audio fade state lives in controller",
                PreviewAudioFadeState_LivesInController),
            await RunCheckAsync(
                "Microphone controls live in controller",
                MicrophoneControls_LiveInController),
            await RunCheckAsync(
                "Responsive shell layout lives in controller",
                ResponsiveShellLayout_LivesInController),
            await RunCheckAsync(
                "Capture selection binding sync lives in controller",
                CaptureSelectionBindingSync_LivesInController),
            await RunCheckAsync(
                "Capture device button actions live in controller",
                CaptureDeviceButtonActions_LiveInController),
            await RunCheckAsync(
                "Capture option presentation lives in focused partial",
                CaptureOptionPresentation_LivesInFocusedPartial),
            await RunCheckAsync(
                "Output path display lives in controller",
                OutputPathDisplay_LivesInController),
            await RunCheckAsync(
                "Output path button actions live in controller",
                OutputPathButtonActions_LiveInController),
            await RunCheckAsync(
                "Preview screenshot button workflow lives in controller",
                PreviewScreenshotButtonWorkflow_LivesInController),
            await RunCheckAsync(
                "Stats panels use source telemetry for HDMI input format and HDR",
                StatsPanels_UseSourceTelemetry_ForHdmiInput),
            await RunCheckAsync(
                "Stats presentation logic lives in focused builder",
                StatsPresentationLogic_LivesInFocusedBuilder),
            await RunCheckAsync(
                "Stats snapshot construction lives in focused builder",
                StatsSnapshotConstruction_LivesInFocusedBuilder),
            await RunCheckAsync(
                "Stats snapshot builder maps health and renderer metrics",
                StatsSnapshotBuilder_MapsHealthAndRendererMetrics),
            await RunCheckAsync(
                "Stats live summary shows current preview frame time and 1 percent low",
                StatsLiveSummary_ShowsCurrentPreviewFrameTimeAndOnePercentLow),
            await RunCheckAsync(
                "Frame-time overlay uses detected-FPS bounded millisecond range",
                FrameTimeOverlay_UsesDetectedFpsBoundedRange),
            await RunCheckAsync(
                "D3D preview letterbox rect calculates correctly",
                D3D11PreviewRenderer_ComputeLetterboxRect_CalculatesCorrectly),
            await RunCheckAsync(
                "D3D preview black edge counting works correctly",
                D3D11PreviewRenderer_BlackEdgeCounting_WorksCorrectly),
            await RunCheckAsync(
                "D3D preview device lost exceptions classify correctly",
                D3D11PreviewRenderer_IsDeviceLostException_ClassifiesCorrectly),
            await RunCheckAsync(
                "D3D preview present cadence metrics expose expected properties",
                D3D11PreviewRenderer_PresentCadenceMetrics_HasExpectedProperties),
            await RunCheckAsync(
                "D3D preview present cadence ignores suppressed frames",
                D3D11PreviewRenderer_PresentCadenceSuppression_SkipsSamplesAndResetsBaseline),
            await RunCheckAsync(
                "D3D preview PNG CRC table generates 256 entries",
                D3D11PreviewRenderer_InitPngCrc32Table_Generates256Entries),
            await RunCheckAsync(
                "D3D preview diagnostics expose swap-chain and render timing contract",
                D3D11PreviewRenderer_DiagnosticsContract_ExposesSwapChainAndRenderTiming),
            await RunCheckAsync(
                "D3D preview frame types live in focused partial",
                D3D11PreviewRenderer_FrameTypesLiveInFocusedPartial),
            await RunCheckAsync(
                "D3D preview frame submission lives in focused partial",
                D3D11PreviewRenderer_SubmissionLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview frame ownership lives in focused partial",
                D3D11PreviewRenderer_FrameOwnershipLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview DXGI frame statistics live in focused partial",
                D3D11PreviewRenderer_DxgiFrameStatisticsLiveInFocusedPartial),
            await RunCheckAsync(
                "D3D preview panel binding lives in focused partial",
                D3D11PreviewRenderer_PanelBindingLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview frame upload lives in focused partial",
                D3D11PreviewRenderer_FrameUploadLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview shader rendering lives in focused partial",
                D3D11PreviewRenderer_ShaderRenderingLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview slow-frame diagnostics live in focused partial",
                D3D11PreviewRenderer_SlowFrameDiagnosticsLiveInFocusedPartial),
            await RunCheckAsync(
                "D3D preview metric tracking lives in focused partial",
                D3D11PreviewRenderer_MetricTrackingLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview frame-latency wait lives in focused partial",
                D3D11PreviewRenderer_FrameLatencyLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview input resources live in focused partial",
                D3D11PreviewRenderer_InputResourcesLiveInFocusedPartial),
            await RunCheckAsync(
                "D3D preview lifecycle lives in focused partial",
                D3D11PreviewRenderer_LifecycleLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview device initialization lives in focused partial",
                D3D11PreviewRenderer_DeviceInitializationLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview viewport helpers live in focused partial",
                D3D11PreviewRenderer_ViewportHelpersLiveInFocusedPartial),
            await RunCheckAsync(
                "D3D preview screenshot encoding lives in focused partial",
                D3D11PreviewRenderer_ScreenshotEncodingLivesInFocusedPartial),
            await RunCheckAsync(
                "D3D preview device-lost recovery lives in focused partial",
                D3D11PreviewRenderer_DeviceLostRecoveryLivesInFocusedPartial),
            await RunCheckAsync(
                "Preview pacing classifier rejects weak samples",
                PreviewPacingClassifier_RequiresStableSampleUnlessHardSignal),
            await RunCheckAsync(
                "Preview pacing classifier prefers source capture when source drops",
                PreviewPacingClassifier_ClassifiesSourceCaptureBeforePreviewTail),
            await RunCheckAsync(
                "Preview pacing classifier flags compositor misses first",
                PreviewPacingClassifier_ClassifiesCompositorMissBeforePresentBlocked),
            await RunCheckAsync(
                "Preview pacing classifier flags dominant render upload",
                PreviewPacingClassifier_ClassifiesDominantRenderUpload),
            await RunCheckAsync(
                "Preview pacing classifier flags frame latency wait timeout",
                PreviewPacingClassifier_ClassifiesFrameLatencyWaitTimeout),
            await RunCheckAsync(
                "Preview pacing classifier ignores stale lifetime signals",
                PreviewPacingClassifier_IgnoresStaleLifetimeSignalsWithoutRecentDeltas),
            await RunCheckAsync(
                "Preview pacing classifier flags recent jitter schedule-late",
                PreviewPacingClassifier_ClassifiesRecentJitterScheduleLate),
            await RunCheckAsync(
                "Preview pacing classifier is wired into automation snapshots",
                PreviewPacingClassifier_IsWiredIntoAutomationSnapshots),
            await RunCheckAsync(
                "D3D preview transition drain drops pending frames",
                D3D11PreviewRenderer_DropPendingFrames_DrainsQueueAndMarksGeneration),
            await RunCheckAsync(
                "D3D preview frame capture cancellation clears pending request",
                D3D11PreviewRenderer_FrameCaptureCancellationClearsPendingRequest),
            await RunCheckAsync(
                "Shared D3D device references are duplicated under lifecycle lock",
                SharedD3DDeviceManager_DuplicatesReferencesUnderLifecycleLock),
            await RunCheckAsync(
                "MCP raw app state keeps capture options separate",
                McpToolSurface_KeepsCaptureOptionsSeparateFromRawState),
            await RunCheckAsync(
                "MCP host tool schema uses PipeClient as a service",
                McpHostToolSchema_UsesPipeClientAsService),
            await RunCheckAsync(
                "MCP PipeClient honors Sussudio pipe environment",
                McpPipeClient_HonorsSussudioAutomationPipeEnvironment),
            await RunCheckAsync(
                "MCP host tool invocation returns pipe failures",
                McpHostToolInvocation_ReturnsPipeFailureInsteadOfClosingTransport),
            await RunCheckAsync(
                "MCP capture settings tool routes provided settings",
                McpCaptureSettingsTools_RouteProvidedSettings),
            await RunCheckAsync(
                "MCP recording tool routes recording toggle",
                McpRecordingTools_RouteRecordingToggle),
            await RunCheckAsync(
                "MCP flashback tool routes enable toggle",
                McpFlashbackTools_RouteEnableToggle),
            await RunCheckAsync(
                "MCP tool command formatter batches pending commands",
                McpToolCommandFormatter_BatchesPendingCommands),
            await RunCheckAsync(
                "MCP device tool routes refresh selections and custom audio",
                McpDeviceTools_RouteRefreshSelectionsAndCustomAudio),
            await RunCheckAsync(
                "MCP pipeline settings tool routes pipeline and audio commands",
                McpPipelineSettingsTools_RoutePipelineAndAudioCommands),
            await RunCheckAsync(
                "MCP UI settings tools route UI commands",
                McpUiSettingsTools_RouteUiCommands),
            await RunCheckAsync(
                "MCP verification tools format verification responses",
                McpVerificationTools_FormatVerificationResponses),
            await RunCheckAsync(
                "MCP diagnostic session tool records snapshot artifacts",
                McpDiagnosticSessionTool_RecordsSnapshotArtifacts),
            await RunCheckAsync(
                "MCP diagnostic session tool surfaces diagnostic failures",
                McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError),
            await RunCheckAsync(
                "Diagnostic session runner writes terminal artifacts on final snapshot failure",
                DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts),
            await RunCheckAsync(
                "Diagnostic session model ownership is split from runner behavior",
                DiagnosticSessionModels_AreSplitFromRunnerBehavior),
            await RunCheckAsync(
                "Diagnostic session initial snapshot has a named owner",
                DiagnosticSessionInitialSnapshot_OwnsBaselineCapture),
            await RunCheckAsync(
                "Diagnostic session result formatting has a named owner",
                DiagnosticSessionResultFormatter_OwnsFormattedSummaryText),
            await RunCheckAsync(
                "Diagnostic session result construction has a named owner",
                DiagnosticSessionResultBuilder_OwnsSummaryConstruction),
            await RunCheckAsync(
                "Diagnostic session summary writer has a named owner",
                DiagnosticSessionSummaryWriter_OwnsSummaryWriteFailures),
            await RunCheckAsync(
                "Diagnostic session result artifacts have a named owner",
                DiagnosticSessionResultArtifacts_OwnPreSummaryWrites),
            await RunCheckAsync(
                "Diagnostic session shared text helpers have a named owner",
                DiagnosticSessionText_OwnsSharedFormattingHelpers),
            await RunCheckAsync(
                "Diagnostic session pipe retry policy has a named owner",
                DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification),
            await RunCheckAsync(
                "Diagnostic session command channel has a named owner",
                DiagnosticSessionCommandChannel_OwnsSerializedCommandSending),
            await RunCheckAsync(
                "Diagnostic session JSON artifacts have a named owner",
                DiagnosticSessionJsonArtifacts_OwnsArtifactsAndResponseExtraction),
            await RunCheckAsync(
                "Diagnostic session run state has a named owner",
                DiagnosticSessionRunState_OwnsTerminalAndLiveState),
            await RunCheckAsync(
                "Diagnostic session output lock has a named owner",
                DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock),
            await RunCheckAsync(
                "Diagnostic session scenario plan has a named owner",
                DiagnosticSessionScenarioPlan_OwnsScenarioFlags),
            await RunCheckAsync(
                "Diagnostic session scenario setup has a named owner",
                DiagnosticSessionScenarioSetup_OwnsInitialMutations),
            await RunCheckAsync(
                "Diagnostic session background tasks have a named owner",
                DiagnosticSessionBackgroundTasks_OwnTaskDraining),
            await RunCheckAsync(
                "Diagnostic session PresentMon startup has a named owner",
                DiagnosticSessionPresentMonStartup_OwnsPresentMonLaunch),
            await RunCheckAsync(
                "Diagnostic session cleanup policy has a named owner",
                DiagnosticSessionCleanupPolicy_OwnsRestoreWarnings),
            await RunCheckAsync(
                "Diagnostic session recording checks have a named owner",
                DiagnosticSessionRecordingChecks_OwnPostRunRecordingVerification),
            await RunCheckAsync(
                "Diagnostic session post-run snapshots have a named owner",
                DiagnosticSessionPostRunSnapshots_OwnTimelineAndFinalSnapshot),
            await RunCheckAsync(
                "Diagnostic session Flashback cycle scenarios have a named owner",
                DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows),
            await RunCheckAsync(
                "Diagnostic session sampler has a named owner",
                DiagnosticSessionSampler_OwnsSampleLoopOrdering),
            await RunCheckAsync(
                "Diagnostic session metrics have a named owner",
                DiagnosticSessionMetrics_OwnsSessionMetricProjection),
            await RunCheckAsync(
                "Diagnostic session Flashback metrics have a named owner",
                DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection),
            await RunCheckAsync(
                "Diagnostic session Flashback preview cycle scenarios have a named owner",
                DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows),
            await RunCheckAsync(
                "Diagnostic session Flashback rejected exports have a named owner",
                DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows),
            await RunCheckAsync(
                "Diagnostic session Flashback recording settings scenarios have a named owner",
                DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow),
            await RunCheckAsync(
                "Diagnostic session Flashback lifecycle scenarios have a named owner",
                DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow),
            await RunCheckAsync(
                "Diagnostic session Flashback segment playback scenarios have a named owner",
                DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow),
            await RunCheckAsync(
                "Diagnostic session Flashback export scenarios have a named owner",
                DiagnosticSessionFlashbackExportScenarios_OwnExportFlows),
            await RunCheckAsync(
                "Diagnostic session Flashback export helpers have a named owner",
                DiagnosticSessionFlashbackExports_OwnsExportHelpers),
            await RunCheckAsync(
                "Diagnostic session Flashback segment waits have a named owner",
                DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing),
            await RunCheckAsync(
                "Diagnostic session Flashback stress scenario has a named owner",
                DiagnosticSessionFlashbackStressScenario_OwnsStressFlow),
            await RunCheckAsync(
                "Diagnostic session Flashback snapshot waits have a named owner",
                DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits),
            await RunCheckAsync(
                "Diagnostic session Flashback validation has a named owner",
                DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy),
            await RunCheckAsync(
                "Diagnostic session health policy has a named owner",
                DiagnosticSessionHealthPolicy_OwnsHealthTolerances),
            await RunCheckAsync(
                "Diagnostic session runner verifies flashback export during playback",
                DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow),
            await RunCheckAsync(
                "Diagnostic session runner ignores transient flashback warmup warnings",
                DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings),
            await RunCheckAsync(
                "Diagnostic session runner tolerates sparse source cadence warnings only without source drops",
                DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops),
            await RunCheckAsync(
                "Diagnostic session runner fails unknown initial snapshot without mutating state",
                DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState),
            await RunCheckAsync(
                "Diagnostic session runner retries synthetic pipe connect failures",
                DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures),
            await RunCheckAsync(
                "Diagnostic session runner rejects concurrent invocation on same output directory",
                DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory),
            await RunCheckAsync(
                "Diagnostic session Flashback stress scenario classifies audio-master fallbacks",
                DiagnosticSessionFlashbackStressScenario_ClassifiesAudioMasterFallbacks),
            await RunCheckAsync(
                "MCP performance timeline exposes D3D P99 stage timing",
                McpPerformanceTimelineTool_ExposesD3DP99StageTiming),
            await RunCheckAsync(
                "MCP performance timeline renders flashback command counters",
                McpPerformanceTimelineTool_RendersFlashbackCommandCounters),
            await RunCheckAsync(
                "MCP frame pacing verdict flags half-rate preview and playback",
                McpFramePacingVerdictTool_FlagsHalfRatePreviewAndPlayback),
            await RunCheckAsync(
                "MCP frame pacing verdict flags insufficient sample duration",
                McpFramePacingVerdictTool_FlagsInsufficientSampleDuration),
            await RunCheckAsync(
                "MCP wait tool routes condition waits",
                McpWaitTools_RouteConditionWaits),
            await RunCheckAsync(
                "MCP window screenshot tool formats screenshot responses",
                McpWindowScreenshotTool_FormatsScreenshotResponses),
            await RunCheckAsync(
                "MCP window tool routes window actions",
                McpWindowTools_RouteWindowActions),
            await RunCheckAsync(
                "MCP preview color probe tool formats probe responses",
                McpPreviewColorProbeTool_FormatsProbeResponses),
            await RunCheckAsync(
                "MCP preview tool routes preview toggle",
                McpPreviewTools_RoutePreviewToggle),
            await RunCheckAsync(
                "MCP video source probe tool formats probe responses",
                McpVideoSourceProbeTool_FormatsProbeResponses),
            await RunCheckAsync(
                "Unified video capture CPU MJPEG emit reports NV12",
                UnifiedVideoCapture_CpuMjpegEmitReportsNv12),
            await RunCheckAsync(
                "Unified video capture retains MJPEG pipeline on stop failure",
                UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails),
            await RunCheckAsync(
                "MJPEG pipeline timing metrics calculate uniform samples",
                ParallelMjpegDecodePipeline_ComputeTimingMetrics_CalculatesCorrectly),
            await RunCheckAsync(
                "MJPEG pipeline timing metrics calculate P95 samples",
                ParallelMjpegDecodePipeline_ComputeTimingMetrics_P95Calculation),
            await RunCheckAsync(
                "MJPEG pipeline copy ring extracts insertion-order window",
                ParallelMjpegDecodePipeline_CopyRing_ExtractsCorrectWindow),
            await RunCheckAsync(
                "MJPEG pipeline elapsed milliseconds uses stopwatch ticks",
                ParallelMjpegDecodePipeline_GetElapsedMilliseconds_ComputesCorrectly),
            await RunCheckAsync(
                "MJPEG pipeline remaining timeout clamps past deadlines",
                ParallelMjpegDecodePipeline_GetRemainingTimeout_ReturnsCorrectTimeSpan),
            await RunCheckAsync(
                "MJPEG pipeline lifecycle lives in focused partial",
                ParallelMjpegDecodePipeline_LifecycleLivesInFocusedPartial),
            await RunCheckAsync(
                "MJPEG pipeline reorder lives in focused partial",
                ParallelMjpegDecodePipeline_ReorderLivesInFocusedPartial),
            await RunCheckAsync(
                "MJPEG pipeline timing metrics expose expected properties",
                ParallelMjpegDecodePipeline_PipelineTimingMetrics_HasExpectedProperties),
            await RunCheckAsync(
                "Software MJPEG decoder exposes dimensions and NV12 size",
                SoftwareMjpegDecoder_Properties_ExposeCorrectDimensions),
            await RunCheckAsync(
                "Pooled video frame leases return buffer after final release",
                PooledVideoFrame_LeaseLifecycle_ReturnsBufferAfterLastRelease),
            await RunCheckAsync(
                "Pooled video frame rejects leases after return",
                PooledVideoFrame_AddLeaseAfterReturn_Throws),
            await RunCheckAsync(
                "Pooled video frame closes new leases after owner release",
                PooledVideoFrame_OwnerDisposeClosesNewLeasesButExistingLeaseRemainsReadable),
            await RunCheckAsync(
                "MJPEG pooled frame fanout exposes lease contracts",
                MjpegPooledFrameFanout_ExposesLeaseContracts),
            await RunCheckAsync(
                "MJPEG shared reorder does not synthesize recording skips",
                ParallelMjpegDecodePipeline_SharedReorder_DoesNotSynthesizeRecordingSkips),
            await RunCheckAsync(
                "MJPEG startup non-JPEG samples drop before sequencing",
                ParallelMjpegDecodePipeline_DropsStartupNonJpegBeforeSequencing),
            await RunCheckAsync(
                "MJPEG known losses skip instead of fataling capture",
                ParallelMjpegDecodePipeline_KnownLossSkipsInsteadOfSignalingFatal),
            await RunCheckAsync(
                "MJPEG packet hash current duplicate run lowers unique FPS",
                FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps),
            await RunCheckAsync(
                "Decoded visual cadence samples exact crop pixels in one pass",
                VisualCadenceTracker_UsesExactCropPixelsWithOnePassDiff),
            await RunCheckAsync(
                "MJPEG leased video packets release queued leases",
                MjpegLeasedVideoPackets_ReleaseQueuedLeases),
            await RunCheckAsync(
                "MJPEG preview jitter exposes adaptive deadline policy",
                MjpegPreviewJitter_ExposesAdaptiveDeadlinePolicy),
            await RunCheckAsync(
                "MJPEG preview jitter emit loop lives in focused partial",
                MjpegPreviewJitter_EmitLoopLivesInFocusedPartial),
            await RunCheckAsync(
                "MJPEG preview jitter drops soft deadline overflow to recover latency",
                MjpegPreviewJitter_DropsSoftDeadlineOverflowToRecoverLatency),
            await RunCheckAsync(
                "MJPEG preview jitter drops expired frames below target depth",
                MjpegPreviewJitter_DropsExpiredFramesBelowTargetDepth),
            await RunCheckAsync(
                "MJPEG preview jitter skips missing preview sequence after deadline",
                MjpegPreviewJitter_SkipsMissingPreviewSequenceAfterDeadline),
            await RunCheckAsync(
                "MJPEG preview jitter does not count late sequence frames as queued",
                MjpegPreviewJitter_LateSequenceDoesNotCountAsQueued),
            await RunCheckAsync(
                "MJPEG preview jitter clear resets preview sequence",
                MjpegPreviewJitter_ClearResetsPreviewSequence),
            await RunCheckAsync(
                "MJPEG preview jitter reprimes after suppression resume",
                MjpegPreviewJitter_ReprimesAfterSuppressionResume),
            await RunCheckAsync(
                "D3D preview pending frame releases queued lease",
                D3DPreviewPendingFrame_ReleasesQueuedLease),
            await RunCheckAsync(
                "Recording video queues fail explicitly instead of evicting frames",
                RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames),
            await RunCheckAsync(
                "Capture service recording lifecycle lives in focused partial",
                CaptureService_RecordingLifecycleLivesInFocusedPartial),
            await RunCheckAsync(
                "Capture service recording rollback lives in focused partial",
                CaptureService_RecordingRollbackLivesInFocusedPartial),
            await RunCheckAsync(
                "LibAv recording stop validates final output",
                LibAvRecordingSink_StopValidatesFinalOutput),
            await RunCheckAsync(
                "Recording video try enqueue paths do not block capture callbacks",
                RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks),
            await RunCheckAsync(
                "Unified video capture sink fan-out lives in focused partial",
                UnifiedVideoCapture_SinkFanoutLivesInFocusedPartial),
            await RunCheckAsync(
                "Unified video capture lifecycle lives in focused partial",
                UnifiedVideoCapture_LifecycleLivesInFocusedPartial),
            await RunCheckAsync(
                "WASAPI audio capture rejects incomplete hot audio writes",
                WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks),
            await RunCheckAsync(
                "WASAPI audio capture conversion lives in focused partial",
                WasapiAudioCapture_ConversionLivesInFocusedPartial),
            await RunCheckAsync(
                "WASAPI audio capture diagnostics live in focused partial",
                WasapiAudioCapture_DiagnosticsLivesInFocusedPartial),
            await RunCheckAsync(
                "WASAPI COM interop contracts live in focused file",
                WasapiComInterop_ContractsLiveInFocusedFile),
            await RunCheckAsync(
                "WASAPI audio capture stop uses bounded thread join",
                WasapiAudioCapture_StopUsesBoundedThreadJoin),
            await RunCheckAsync(
                "CaptureService flashback backend ownership uses resource aggregate",
                CaptureService_FlashbackBackendOwnershipUsesResourceAggregate),
            await RunCheckAsync(
                "LibAv recording drain loop interleaves audio with bounded video batches",
                LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches),
            await RunCheckAsync(
                "LibAv recording encoding loop lives in focused partial",
                LibAvRecordingSink_EncodingLoopLivesInFocusedPartial),
            await RunCheckAsync(
                "LibAv recording audio queues live in focused partial",
                LibAvRecordingSink_AudioQueuesLiveInFocusedPartial),
            await RunCheckAsync(
                "LibAv recording lifecycle helpers live in focused partials",
                LibAvRecordingSink_LifecycleHelpersLiveInFocusedPartials),
            await RunCheckAsync(
                "MJPG HFR mode only activates for SDR 4K120-style settings",
                CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest),
            await RunCheckAsync(
                "Strict HFR fatal handler clears active session state",
                CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState),
            await RunCheckAsync(
                "Capture errors refresh ViewModel runtime flags",
                CaptureErrors_RefreshViewModelRuntimeFlags),

            // --- RecordingContracts ---
            await RunCheckAsync(
                "FinalizeResult.Success produces empty preserved list",
                FinalizeResult_Success_ProducesEmptyPreservedList),
            await RunCheckAsync(
                "FinalizeResult.Failure deduplicates and filters preserved artifacts",
                FinalizeResult_Failure_DeduplicatesAndFiltersArtifacts),

            // --- RecordingArtifactManager ---
            await RunCheckAsync(
                "FinalizeContext returns success when post-mux audio disabled",
                ArtifactManager_FinalizeContext_ReturnsSuccess_WhenPostMuxDisabled),
            await RunCheckAsync(
                "FinalizeContext preserves temp artifacts when mux fails",
                ArtifactManager_FinalizeContext_PreservesTempArtifacts_WhenMuxFails),
            await RunCheckAsync(
                "FinalizeContext rejects invalid final output",
                ArtifactManager_FinalizeContext_RejectsInvalidFinalOutput),
            await RunCheckAsync(
                "RollbackAsync deletes all artifacts when post-mux enabled",
                ArtifactManager_RollbackAsync_DeletesAllArtifacts_WhenPostMuxEnabled),
            await RunCheckAsync(
                "RollbackAsync is safe with null context",
                ArtifactManager_RollbackAsync_SafeWithNullContext),

            // --- RecordingStats ---
            await RunCheckAsync(
                "RecordingStats computes totals and preserves estimate flag",
                RecordingStats_ComputesTotalsAndPreservesEstimateFlag),

            // --- CaptureSettings ---
            await RunCheckAsync(
                "Capture mode options preserve display text and metadata",
                CaptureModeOptions_PreserveDisplayTextAndMetadata),
            await RunCheckAsync(
                "Capture mode options builder builds resolution and video format options",
                CaptureModeOptionsBuilder_BuildsResolutionAndVideoFormatOptions),
            await RunCheckAsync(
                "Capture settings defaults preserve output and pipeline contracts",
                CaptureSettings_DefaultsAndOutputContracts),
            await RunCheckAsync(
                "Capture settings MJPEG HFR mode handles force case and instance state",
                CaptureSettings_MjpegHighFrameRateMode_HandlesForceCaseAndInstanceState),
            await RunCheckAsync(
                "Encoder support computes availability and preferred encoders",
                EncoderSupport_ComputesAvailabilityAndPreferredEncoders),
            await RunCheckAsync(
                "GetTargetBitrate scales by resolution and frame rate",
                CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate),
            await RunCheckAsync(
                "GetTargetBitrate applies codec efficiency for HEVC and AV1",
                CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency),
            await RunCheckAsync(
                "GetTargetBitrate clamps custom quality to range",
                CaptureSettings_GetTargetBitrate_ClampsCustomQuality),
            await RunCheckAsync(
                "GetOutputFileName includes format suffix",
                CaptureSettings_GetOutputFileName_IncludesFormatSuffix),
            await RunCheckAsync(
                "MJPEG HFR mode requires SDR and MJPG pixel format",
                CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat),

            // --- FlashbackBufferManager ---
            await RunCheckAsync(
                "FlashbackBufferManager Initialize clears recording PTS",
                FlashbackBufferManager_InitializeClearsRecordingPts),
            await RunCheckAsync(
                "FlashbackBufferManager segment lookup returns correct file for position",
                FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment),
            await RunCheckAsync(
                "FlashbackBufferManager segment completion rejects invalid metadata",
                FlashbackBufferManager_SegmentCompletionRejectsInvalidMetadata),
            await RunCheckAsync(
                "FlashbackBufferManager segment completion rejects outside paths",
                FlashbackBufferManager_SegmentCompletionRejectsOutsidePaths),
            await RunCheckAsync(
                "FlashbackBufferManager delete helper rejects outside paths",
                FlashbackBufferManager_TryDeleteFileRejectsOutsidePaths),
            await RunCheckAsync(
                "FlashbackBufferManager segment diagnostics clamp active counters",
                FlashbackBufferManager_SegmentDiagnosticsClampActiveCounters),
            await RunCheckAsync(
                "FlashbackBufferManager math helpers live in focused partial",
                FlashbackBufferManager_MathHelpersLiveInFocusedPartial),
            await RunCheckAsync(
                "FlashbackBufferManager segment query helpers live in focused partial",
                FlashbackBufferManager_SegmentQueriesLiveInFocusedPartial),
            await RunCheckAsync(
                "FlashbackBufferManager segment mutation lives in focused partial",
                FlashbackBufferManager_SegmentMutationLiveInFocusedPartial),
            await RunCheckAsync(
                "FlashbackBufferManager lifecycle helpers live in focused partial",
                FlashbackBufferManager_LifecycleHelpersLiveInFocusedPartial),
            await RunCheckAsync(
                "FlashbackBufferManager latest PTS clamps invalid buffer duration",
                FlashbackBufferManager_UpdateLatestPts_ClampsInvalidBufferDuration),
            await RunCheckAsync(
                "FlashbackBufferManager segment rotation keeps total bytes written monotonic",
                FlashbackBufferManager_SegmentRotationKeepsTotalBytesWrittenMonotonic),
            await RunCheckAsync(
                "FlashbackBufferManager same-path completion extends latest segment",
                FlashbackBufferManager_SamePathCompletionExtendsLatestSegment),
            await RunCheckAsync(
                "FlashbackBufferManager ignores updates after dispose",
                FlashbackBufferManager_IgnoresUpdatesAfterDispose),
            await RunCheckAsync(
                "FlashbackBufferManager ignores destructive operations after dispose",
                FlashbackBufferManager_IgnoresDestructiveOperationsAfterDispose),
            await RunCheckAsync(
                "FlashbackBufferManager valid segment lookup skips missing files",
                FlashbackBufferManager_GetValidSegmentFileForPosition_SkipsMissingFiles),
            await RunCheckAsync(
                "FlashbackBufferManager stale left-edge lookup uses oldest segment",
                FlashbackBufferManager_GetValidSegmentFileForPosition_StaleLeftEdgeUsesOldest),
            await RunCheckAsync(
                "FlashbackBufferManager GetNextSegmentFile walks forward through segments",
                FlashbackBufferManager_GetNextSegmentFile_WalksForward),
            await RunCheckAsync(
                "FlashbackBufferManager segment path lookups normalize equivalent paths",
                FlashbackBufferManager_SegmentPathLookupsNormalizeEquivalentPaths),
            await RunCheckAsync(
                "FlashbackBufferManager segment start PTS skips missing files",
                FlashbackBufferManager_GetSegmentStartPts_SkipsMissingFiles),
            await RunCheckAsync(
                "FlashbackBufferManager GetNextSegmentFile skips missing indexed segments",
                FlashbackBufferManager_GetNextSegmentFile_SkipsMissingIndexedSegments),
            await RunCheckAsync(
                "FlashbackBufferManager GetValidSegmentPaths returns overlapping segments",
                FlashbackBufferManager_GetValidSegmentPaths_ReturnsOverlapping),
            await RunCheckAsync(
                "FlashbackBufferManager segment info skips missing files",
                FlashbackBufferManager_GetSegmentInfoList_SkipsMissingFiles),
            await RunCheckAsync(
                "FlashbackBufferManager active file path requires existing file",
                FlashbackBufferManager_ActiveFilePath_RequiresExistingFile),
            await RunCheckAsync(
                "FlashbackBufferManager segment count skips missing files",
                FlashbackBufferManager_SegmentCount_SkipsMissingFiles),
            await RunCheckAsync(
                "FlashbackBufferManager eviction updates disk byte totals",
                FlashbackBufferManager_EvictOldestSegments_UpdatesTotalDiskBytes),
            await RunCheckAsync(
                "FlashbackBufferManager eviction keeps rejected segments accounted",
                FlashbackBufferManager_EvictOldestSegments_KeepsRejectedSegmentsAccounted),
            await RunCheckAsync(
                "FlashbackBufferManager eviction pause and resume are balanced",
                FlashbackBufferManager_EvictionPauseResume_Balanced),
            await RunCheckAsync(
                "FlashbackBufferManager abandons startup-generated segment paths",
                FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath),
            await RunCheckAsync(
                "FlashbackBufferManager purges retain locked active segment path",
                FlashbackBufferManager_PurgesRetainLockedActivePath),
            await RunCheckAsync(
                "FlashbackBufferManager partial purge accounts for deleted active segment",
                FlashbackBufferManager_PurgeCompletedSegments_AccountsForActiveBytesOnPartialPurge),
            await RunCheckAsync(
                "FlashbackBufferManager full purge reports active bytes once",
                FlashbackBufferManager_PurgeAllSegmentsCore_ReportsActiveBytesOnce),
            await RunCheckAsync(
                "FlashbackBufferManager removes stale legacy root segments",
                FlashbackBufferManager_RemovesStaleLegacyRootSegments),
            await RunCheckAsync(
                "FlashbackBufferManager preserves unrelated empty temp directories",
                FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories),
            await RunCheckAsync(
                "FlashbackBufferManager trims startup session cache budget",
                FlashbackBufferManager_TrimsStartupSessionCacheBudget),
            await RunCheckAsync(
                "FlashbackBufferManager rejects unsafe session ids",
                FlashbackBufferManager_RejectsUnsafeSessionIds),
            await RunCheckAsync(
                "FlashbackBufferManager validates segment extensions",
                FlashbackBufferManager_ValidatesSegmentExtensions),

            // --- GpuPipelineHandles ---
            await RunCheckAsync(
                "GpuPipelineHandles.None returns zeroed struct",
                GpuPipelineHandles_None_ReturnsZeroedStruct),

            // --- RecordingContextRequest ---
            await RunCheckAsync(
                "RecordingContextRequest defaults match RecordingContext defaults",
                RecordingContextRequest_DefaultsMatchRecordingContextDefaults),

            // --- Device Models ---
            await RunCheckAsync(
                "AudioInputDevice display name falls back to unknown",
                AudioInputDevice_DisplayName_UsesNameOrUnknownFallback),
            await RunCheckAsync(
                "AudioLevelEventArgs exposes peak RMS and clipped state",
                AudioLevelEventArgs_ExposesPeakRmsAndClippedState),
            await RunCheckAsync(
                "CaptureDevice preserves display and metadata defaults",
                CaptureDevice_DisplayNameAndDefaults_PreserveDeviceMetadata),
            await RunCheckAsync(
                "CaptureDiagnosticsSnapshot preserves diagnostics telemetry contract",
                CaptureDiagnosticsSnapshot_DefaultsAndRoundTripsCoreTelemetry),
            await RunCheckAsync(
                "CaptureHealthSnapshot extends diagnostics with health telemetry",
                CaptureHealthSnapshot_ExtendsDiagnosticsWithFlashbackSourceAndAvSync),

            // --- MediaFormat ---
            await RunCheckAsync(
                "MediaFormat equality with matching rational frame rates",
                MediaFormat_Equality_WithMatchingRationalFrameRates),
            await RunCheckAsync(
                "MediaFormat inequality when dimensions differ",
                MediaFormat_Inequality_WhenDimensionsDiffer),
            await RunCheckAsync(
                "MediaFormat GetHashCode consistency for equal objects",
                MediaFormat_GetHashCode_ConsistencyForEqualObjects),

            // --- AutomationContracts ---
            await RunCheckAsync(
                "AutomationCommandKind preserves numeric values through GetAutomationManifest",
                AutomationCommandKind_PreservesNumericValuesThroughGetAutomationManifest),
            await RunCheckAsync(
                "AutomationWindowAction has expected values",
                AutomationWindowAction_HasExpectedValues),

            // --- RuntimePaths ---
            await RunCheckAsync(
                "RuntimePaths GetRepoLogFile returns path under repo root",
                RuntimePaths_GetRepoLogFile_ReturnsPathUnderRepoRoot),
            await RunCheckAsync(
                "RuntimePaths paths contain expected directory names",
                RuntimePaths_PathsContainExpectedDirectoryNames),
            await RunCheckAsync(
                "MMCSS registration uses Unicode AVRT entry point",
                MmcssThreadRegistration_UsesUnicodeAvrtEntryPoint),

            // --- SourceSignalTelemetrySnapshot ---
            await RunCheckAsync(
                "SourceSignalTelemetrySnapshot defaults have expected values",
                SourceSignalTelemetrySnapshot_DefaultsHaveExpectedValues),
            await RunCheckAsync(
                "SourceSignalTelemetrySnapshot properties round-trip",
                SourceSignalTelemetrySnapshot_PropertiesRoundTrip),
            await RunCheckAsync(
                "SourceSignalTelemetrySnapshot preserves full telemetry contract",
                SourceSignalTelemetrySnapshot_PreservesFullTelemetryContract),

            // --- HdrOutputPolicy ---
            await RunCheckAsync(
                "HdrOutputPolicy returns true when HDR and Hdr10Pq requested",
                HdrOutputPolicy_ReturnsTrue_WhenHdrAndHdr10PqRequested),
            await RunCheckAsync(
                "HdrOutputPolicy returns false when HDR disabled",
                HdrOutputPolicy_ReturnsFalse_WhenHdrDisabled),
            await RunCheckAsync(
                "HdrOutputPolicy returns false for non-Hdr10Pq mode",
                HdrOutputPolicy_ReturnsFalse_WhenNotHdr10Pq),
            await RunCheckAsync(
                "HdrOutputPolicy force-off env disables HDR output",
                HdrOutputPolicy_ReturnsFalse_WhenForceOffEnvSet),
            await RunCheckAsync(
                "HdrOutputPolicy ignores removed legacy enabled env switch",
                HdrOutputPolicy_IgnoresLegacyEnabledEnvSwitch),

            // --- FlashbackPlaybackState enum ---
            await RunCheckAsync(
                "Flashback models preserve buffer session and export contracts",
                FlashbackModels_PreserveBufferSessionExportContracts),
            await RunCheckAsync(
                "Flashback buffer options max disk bytes scales with duration",
                FlashbackBufferOptions_MaxDiskBytes_ScalesWithDuration),
            await RunCheckAsync(
                "FlashbackPlaybackState enum has all expected states",
                FlashbackPlaybackState_HasAllExpectedStates),
            await RunCheckAsync(
                "Flashback playback initial state is live",
                FlashbackPlaybackController_InitialState_IsLive),
            await RunCheckAsync(
                "Flashback playback commands no-op before initialize",
                FlashbackPlaybackController_CommandsNoOpBeforeInitialize),
            await RunCheckAsync(
                "Flashback playback successful no-ops clear stale failures",
                FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure),
            await RunCheckAsync(
                "Flashback playback coalesced commands clear stale failures",
                FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure),
            await RunCheckAsync(
                "Flashback playback worker exit rearms future commands",
                FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart),
            await RunCheckAsync(
                "Flashback playback command queue accepts newest control when full",
                FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull),
            await RunCheckAsync(
                "Flashback encoder resolves fractional frame rates",
                FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates),
            await RunCheckAsync(
                "Flashback encoder maps codec names",
                FlashbackEncoderSink_MapCodecName_MapsFormats),
            await RunCheckAsync(
                "Flashback encoder counters default to zero",
                FlashbackEncoderSink_CountersDefaultToZero),
            await RunCheckAsync(
                "Flashback encoder bounds high-resolution CPU queue capacity",
                FlashbackEncoderSink_HighResolutionCpuQueueCapacityIsBounded),
            await RunCheckAsync(
                "Flashback export throttle responds to live queue pressure",
                CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure),
            await RunCheckAsync(
                "Flashback encoder force-rotate drain rejects video enqueues",
                FlashbackEncoderSink_ForceRotateDrainingRejectsVideoAndGpuEnqueues),
            await RunCheckAsync(
                "Flashback encoder start failure rolls back started state",
                FlashbackEncoderSink_StartFailureRollsBackStartedState),
            await RunCheckAsync(
                "Flashback encoder dispose resets GPU queue depth",
                FlashbackEncoderSink_DisposeResetsGpuQueueDepth),
            await RunCheckAsync(
                "Flashback encoder PTS guards invalid frame rates",
                FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate),
            await RunCheckAsync(
                "Flashback in/out points default to unset",
                FlashbackPlaybackController_InOutPoints_DefaultToUnset),
            await RunCheckAsync(
                "Flashback in/out points clear invalid counterpart",
                FlashbackPlaybackController_InOutPoints_ClearInvalidCounterpart),
            await RunCheckAsync(
                "Flashback in/out point setters normalize markers",
                FlashbackPlaybackController_InOutPointSettersNormalizeMarkers),
            await RunCheckAsync(
                "Flashback in/out point changes stop after dispose",
                FlashbackPlaybackController_InOutPointChangesStopAfterDispose),
            await RunCheckAsync(
                "Flashback clamp bounds stale markers to buffered duration",
                FlashbackPlaybackController_ClampPosition_BoundsMarkersToBufferedDuration),
            await RunCheckAsync(
                "Flashback command positions clamp before file lookup",
                FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup),
            await RunCheckAsync(
                "Flashback playback timestamp arithmetic is saturating",
                FlashbackPlaybackController_TimestampArithmeticIsSaturating),
            await RunCheckAsync(
                "Flashback end-of-segment open failures snap live",
                FlashbackPlaybackController_EndOfSegmentOpenFailuresSnapLive),
            await RunCheckAsync(
                "Flashback normal playback uses tight near-live snap",
                FlashbackPlaybackController_NormalPlaybackUsesTightNearLiveSnap),
            await RunCheckAsync(
                "Flashback snap-live clears open file identity",
                FlashbackPlaybackController_SnapLiveClearsOpenFileIdentity),
            await RunCheckAsync(
                "Flashback pause from live displays a buffered frame before paused",
                FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused),
            await RunCheckAsync(
                "Flashback playback guards invalid decoder frame rates",
                FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps),
            await RunCheckAsync(
                "Flashback playback PTS cadence telemetry tracks mismatches",
                FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches),
            await RunCheckAsync(
                "Flashback nudge opens decoder after pause from live",
                FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused),
            await RunCheckAsync(
                "Flashback playback releases decoded frames after submit failures",
                FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames),
            await RunCheckAsync(
                "Flashback playback guards fMP4 reopen retries",
                FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded),
            await RunCheckAsync(
                "Flashback scrub coalescing does not requeue control commands",
                FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands),
            await RunCheckAsync(
                "Flashback seek slots preserve control command barriers",
                FlashbackPlaybackController_SeekSlots_PreserveControlCommandBarriers),
            await RunCheckAsync(
                "Flashback playback transitions use best-effort audio preview guards",
                FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards),
            await RunCheckAsync(
                "Flashback playback metric reset clears decode timings",
                FlashbackPlaybackController_ResetClearsDecodeMetrics),
            await RunCheckAsync(
                "Flashback decoder calculates NV12 frame buffer sizes",
                FlashbackDecoder_CalculateFrameBufferSize_Nv12),
            await RunCheckAsync(
                "Flashback decoder calculates P010 frame buffer sizes",
                FlashbackDecoder_CalculateFrameBufferSize_P010),
            await RunCheckAsync(
                "Flashback decoder validation helpers live in focused partial",
                FlashbackDecoder_ValidationHelpersLiveInFocusedPartial),
            await RunCheckAsync(
                "Flashback decoder lifetime cleanup lives in focused partial",
                FlashbackDecoder_LifetimeCleanupLivesInFocusedPartial),
            await RunCheckAsync(
                "Flashback decoder diagnostics and guards live in focused partials",
                FlashbackDecoder_DiagnosticsAndGuardsLiveInFocusedPartials),
            await RunCheckAsync(
                "Flashback decoder output types live in focused file",
                FlashbackDecoder_OutputTypesLiveInFocusedFile),
            await RunCheckAsync(
                "Flashback decoder video setup lives in focused partial",
                FlashbackDecoder_VideoSetupLivesInFocusedPartial),
            await RunCheckAsync(
                "Flashback decoder seeking lives in focused partial",
                FlashbackDecoder_SeekingLivesInFocusedPartial),
            await RunCheckAsync(
                "Flashback decoder defaults to closed state",
                FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized),
            await RunCheckAsync(
                "Flashback decoder dispose before initialize is safe",
                FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow),
            await RunCheckAsync(
                "Flashback decoder unreferences discarded audio frames",
                FlashbackDecoder_DiscardedAudioFramesAreUnreffed),
            await RunCheckAsync(
                "Flashback decoder MJPEG playback uses low-latency single-thread decode",
                FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode),
            await RunCheckAsync(
                "Flashback decoder rejects invalid timestamps",
                FlashbackDecoder_PtsConversionRejectsInvalidTimestamps),
            await RunCheckAsync(
                "Flashback decoder input streams and frame sizes are bounded",
                FlashbackDecoder_InputStreamsAndFrameSizesAreBounded),
            await RunCheckAsync(
                "Flashback decoder audio output buffers are bounded",
                FlashbackDecoder_AudioOutputBuffersAreBounded),
            await RunCheckAsync(
                "Flashback decoder audio setup lives in audio output partial",
                FlashbackDecoder_AudioSetupLivesInAudioOutputPartial),
            await RunCheckAsync(
                "Flashback decoder software frame planes are validated",
                FlashbackDecoder_SoftwareFramePlanesAreValidated),
            await RunCheckAsync(
                "Flashback decoder D3D11 frames are validated",
                FlashbackDecoder_D3D11FramesAreValidated),
            await RunCheckAsync(
                "Flashback decoder held-frame cleanup is best effort",
                FlashbackDecoder_HeldFrameCleanupIsBestEffort),
            await RunCheckAsync(
                "Flashback decoder decode loops observe cancellation",
                FlashbackDecoder_DecodeLoopsObserveCancellation),
            await RunCheckAsync(
                "Flashback decoder rejects initialize after dispose",
                FlashbackDecoder_RejectsInitializeAfterDispose),
            await RunCheckAsync(
                "Flashback decoder clears audio callback on dispose",
                FlashbackDecoder_ClearsAudioCallbackOnDispose),
            await RunCheckAsync(
                "Flashback encoder sink restores active segment after rotation failure",
                FlashbackEncoderSink_RotateFailureRestoresActiveSegment),
            await RunCheckAsync(
                "Flashback encoder sink registers segments on cancellation and rotation failure",
                FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure),
            await RunCheckAsync(
                "Flashback encoder sink rejects force rotate after encoder failure",
                FlashbackEncoderSink_ForceRotateRejectsFailedEncoder),
            await RunCheckAsync(
                "Flashback encoder sink skips completed force rotate requests",
                FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest),
            await RunCheckAsync(
                "Flashback encoder sink logs fatal segment registration failures",
                FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged),
            await RunCheckAsync(
                "Flashback encoder sink validates audio packets before rent",
                FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent),
            await RunCheckAsync(
                "Flashback encoder sink interleaves audio with bounded video batches",
                FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches),
            await RunCheckAsync(
                "Flashback encoder sink packet drains live in focused partial",
                FlashbackEncoderSink_PacketDrainLivesInFocusedPartial),
            await RunCheckAsync(
                "Flashback encoder sink startup lives in focused partial",
                FlashbackEncoderSink_StartupLivesInFocusedPartial),
            await RunCheckAsync(
                "Flashback suppressed exceptions use app logs",
                FlashbackSuppressedExceptionsUseAppLogs),
            await RunCheckAsync(
                "Flashback exporter cleanup ignores nonexistent directories",
                FlashbackExporter_CleanupOrphanedTempFiles_HandlesNonexistentDirectory),
            await RunCheckAsync(
                "Flashback exporter cleanup deletes orphaned temp files",
                FlashbackExporter_CleanupOrphanedTempFiles_DeletesTempFiles),
            await RunCheckAsync(
                "Flashback exporter does not scan user output directory for orphans",
                FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans),
            await RunCheckAsync(
                "Flashback exporter task wrappers dispose linked cancellation",
                FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation),
            await RunCheckAsync(
                "Flashback exporter ownership is split across focused partials",
                FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials),
            await RunCheckAsync(
                "Flashback exporter rejects null requests",
                FlashbackExporter_RejectsNullRequests),
            await RunCheckAsync(
                "Flashback exporter fails when input file is missing",
                FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound),
            await RunCheckAsync(
                "Flashback exporter fails when output path is empty",
                FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty),
            await RunCheckAsync(
                "Flashback exporter fails when no segment paths are provided",
                FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments),
            await RunCheckAsync(
                "Flashback exporter output path validation returns failure",
                FlashbackExporter_OutputPathValidation_ReturnsFailure),
            await RunCheckAsync(
                "Flashback export failure classifier maps command failures",
                FlashbackExportFailureClassifier_MapsCommandFailures),
            await RunCheckAsync(
                "Flashback exporter rejects directory output paths",
                FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory),
            await RunCheckAsync(
                "Flashback exporter rejects invalid export ranges",
                FlashbackExporter_RejectsInvalidExportRanges),
            await RunCheckAsync(
                "Flashback rejected export diagnostics preserve attempted range",
                FlashbackExportRejectedDiagnostics_PreserveAttemptedRange),
            await RunCheckAsync(
                "Flashback exporter rejects empty segment paths",
                FlashbackExporter_RejectsEmptySegmentPaths),
            await RunCheckAsync(
                "Flashback exporter rejects duplicate segment paths",
                FlashbackExporter_RejectsDuplicateSegmentPaths),
            await RunCheckAsync(
                "Flashback exporter progress callbacks are best effort",
                FlashbackExporter_ProgressCallbacksAreBestEffort),
            await RunCheckAsync(
                "Flashback exporter releases buffered segment packets on failures",
                FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures),
            await RunCheckAsync(
                "Flashback exporter timestamp conversions are saturating",
                FlashbackExporter_TimestampConversionsAreSaturating),
            await RunCheckAsync(
                "Flashback exporter input stream counts are bounded",
                FlashbackExporter_InputStreamCountsAreBounded),
            await RunCheckAsync(
                "Flashback exporter segment template validation guards missing video streams",
                FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream),
            await RunCheckAsync(
                "Flashback exporter fails when requested segments are skipped",
                FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped),
            await RunCheckAsync(
                "Flashback exporter returns cancellation result while waiting for export lock",
                FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled),
            await RunCheckAsync(
                "Flashback exporter cancellation wins before validation",
                FlashbackExporter_CancellationWinsBeforeValidation),
            await RunCheckAsync(
                "Flashback exporter fails fast when segment files are gone",
                FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone),
            await RunCheckAsync(
                "Flashback exporter dispose timeout does not tear down active native state",
                FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState),
            await RunCheckAsync(
                "Flashback exporter rejects output paths that overwrite source segments",
                FlashbackExporter_RejectsOutputPathThatOverwritesSource),
            await RunCheckAsync(
                "Flashback exporter invalid temp output preserves existing exports",
                FlashbackExporter_InvalidTempOutputDoesNotReplaceExistingExport),
            await RunCheckAsync(
                "Flashback exporter refuses to overwrite existing destination when force is false",
                FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse),
            await RunCheckAsync(
                "Flashback exporter overwrites existing destination when force is true",
                FlashbackExporter_OverwritesWhenForceTrue),
            await RunCheckAsync(
                "Flashback exporter deletes invalid moved final outputs",
                FlashbackExporter_FinalValidationFailureDeletesMovedOutput),
            await RunCheckAsync(
                "Flashback exporter rejects blocked temp output paths before native export",
                FlashbackExporter_RejectsBlockedTempOutputPathBeforeNativeExport),

            // --- RecordingPipelineOptions ---
            await RunCheckAsync(
                "Recording pipeline options preserve defaults and capacity bounds",
                RecordingPipelineOptions_DefaultsAndCapacityBounds),
            await RunCheckAsync(
                "RecordingPipelineOptions resolves video queue capacity from frame rate",
                RecordingPipelineOptions_ResolvesVideoQueueCapacity),

            // --- NvmlSnapshot computed properties ---
            await RunCheckAsync(
                "NvmlSnapshot computed properties convert units correctly",
                NvmlSnapshot_ComputedProperties_ConvertUnits),

            // --- CaptureSessionSnapshot defaults ---
            await RunCheckAsync(
                "CaptureSessionSnapshot has correct default state",
                CaptureSessionSnapshot_DefaultState),

            // --- ProcessSpec and ProcessRunResult contracts ---
            await RunCheckAsync(
                "ProcessSpec default timeout is 30 seconds",
                ProcessSpec_DefaultTimeout_Is30Seconds),

            // --- Tool CommandMap & Formatter Alignment ---
            await RunCheckAsync(
                "Automation pipe protocol resolves commands timeouts auth and envelopes",
                AutomationPipeProtocol_ResolvesCommandsTimeoutsAuthAndEnvelopes),
            await RunCheckAsync(
                "Automation command catalog covers command metadata and policy",
                AutomationCommandCatalog_CoversCommandsAndPolicyMetadata),
            await RunCheckAsync(
                "Automation pipe connect failures are classified for CLI and MCP",
                AutomationPipeConnectFailures_AreClassifiedForCliAndMcp),
            await RunCheckAsync(
                "Reliability gates run tools and offline regression harness",
                ReliabilityGates_RunToolsAndOfflineHarness),
            await RunCheckAsync(
                "Automation manifest covers catalog metadata",
                AutomationManifest_CoversCatalogMetadata),
            await RunCheckAsync(
                "Automation path-bearing commands have validation coverage",
                AutomationCommandCatalog_PathBearingCommandsHaveValidationCoverage),
            await RunCheckAsync(
                "Automation manifest serialization is stable",
                AutomationManifest_SerializationIsStable),
            await RunCheckAsync(
                "Automation response state parses status and retry contracts",
                AutomationResponseState_ParsesStatusAndRetryContracts),
            await RunCheckAsync(
                "Automation snapshot formatter formats core sections and typed accessors",
                AutomationSnapshotFormatter_FormatsCoreSectionsAndTypedAccessors),
            await RunCheckAsync(
                "Shared AutomationPipeProtocol CommandMap covers every AutomationCommandKind enum value",
                SharedProtocol_CommandMap_CoversEveryAutomationCommandKind),
            await RunCheckAsync(
                "MCP PipeClient delegates to shared protocol for command resolution",
                PipeClient_UsesSharedProtocol_ForCommandResolution),
            await RunCheckAsync(
                "ResponseFormatter.IsSuccess correctly parses success and failure JSON",
                ResponseFormatter_IsSuccess_ParsesSuccessAndFailureJson),
            await RunCheckAsync(
                "ResponseFormatter.Get handles all JSON value kinds correctly",
                ResponseFormatter_Get_HandlesAllJsonValueKinds),
            await RunCheckAsync(
                "ssctl Formatters snapshot fields align with MCP ResponseFormatter",
                SsctlFormatters_SnapshotFields_AlignWithMcpResponseFormatter),
            await RunCheckAsync(
                "AutomationClient delegates to shared protocol for command resolution",
                AutomationClient_UsesSharedProtocol_ForCommandResolution),
            await RunCheckAsync(
                "ssctl CommandHandlers route core command groups",
                SsctlCommandHandlers_RouteCoreCommandGroups),
            await RunCheckAsync(
                "PresentMon parser selects dominant non-artifact swap chain",
                PresentMonParser_SelectsDominantNonArtifactSwapChain),
            await RunCheckAsync(
                "ssctl Formatters emit core snapshot sections",
                SsctlFormatters_EmitCoreSnapshotSections),
            await RunCheckAsync(
                "ssctl PipeTransport exposes advanced automation command ids",
                SsctlPipeTransport_ExposesAdvancedAutomationCommandIds),
            await RunCheckAsync(
                "RTK I2C probe guards unsafe native paths",
                RtkI2cProbe_GuardsUnsafeNativePaths)
        };

        var failed = results.Where(r => !r.Passed).ToList();
        foreach (var result in results)
        {
            Console.WriteLine(result.Passed
                ? $"PASS: {result.Name}"
                : $"FAIL: {result.Name} :: {result.Detail}");
        }

        if (failed.Count == 0)
        {
            Console.WriteLine("All runtime snapshot regression checks passed.");
            return 0;
        }

        Console.Error.WriteLine($"{failed.Count} regression checks failed.");
        return 1;
    }

    private static string ResolveAssemblyPath(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        var root = GetRepoRoot();
        return Path.Combine(
            root,
            "Sussudio",
            "bin",
            "x64",
            "Debug",
            "net8.0-windows10.0.19041.0",
            "win-x64",
            "Sussudio.dll");
    }

    private static async Task<CheckResult> RunCheckAsync(string name, Func<Task> check)
    {
        try
        {
            await check().ConfigureAwait(false);
            return new CheckResult(name, true);
        }
        catch (Exception ex)
        {
            return new CheckResult(name, false, ex is TargetInvocationException { InnerException: { } inner }
                ? $"{inner.GetType().Name}: {inner.Message}"
                : ex.Message);
        }
    }

    private static Task ProjectFile_PreservesEnglishOnlyPublishLocalePolicy()
    {
        var projectText = ReadRepoFile("Sussudio/Sussudio.csproj").Replace("\r\n", "\n");
        AssertContains(projectText, "<SatelliteResourceLanguages>en-US</SatelliteResourceLanguages>");
        AssertContains(projectText, "AfterTargets=\"Build;Publish\"");
        AssertContains(projectText, "$_.Name.ToLowerInvariant() -ne 'en-us'");
        AssertContains(projectText, "'$(PublishDir)' != ''");
        AssertContains(projectText, "^[A-Za-z]{2,3}(-[A-Za-z]+)+$");
        return Task.CompletedTask;
    }

    private static Task ShowAllCaptureOptions_UnlocksSourceFilteredFrameRates()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateOptions.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "options = ShowAllCaptureOptions");
        AssertContains(mainViewModelText, "!IsSourceFilteredFrameRateDisableReason(option.DisableReason)");
        AssertContains(mainViewModelText, "IsEnabled = true");
        AssertContains(mainViewModelText, "DisableReason = string.Empty");

        return Task.CompletedTask;
    }

    private static Task ResolutionSelectionPolicy_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionOptions.cs").Replace("\r\n", "\n");
        var selectionPolicyText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionSelectionPolicy.cs").Replace("\r\n", "\n");

        AssertContains(resolutionOptionsText, "private void RebuildResolutionOptions()");
        AssertContains(resolutionOptionsText, "private bool TryResolveResolutionKey(");
        AssertDoesNotContain(resolutionOptionsText, "private ResolutionOption? SelectHdrResolutionOption(");
        AssertContains(selectionPolicyText, "private ResolutionOption? TrySelectSourceResolutionOption(");
        AssertContains(selectionPolicyText, "private ResolutionOption? SelectHdrResolutionOption(");
        AssertContains(selectionPolicyText, "private bool TrySelectSdrAutoResolutionOption(");
        AssertContains(selectionPolicyText, "private static bool TryParseResolutionKey(");
        AssertContains(selectionPolicyText, "private string BuildHdrSupportHintForResolution(");

        return Task.CompletedTask;
    }

    private static Task FrameRateTimingPolicy_LivesInFocusedPartial()
    {
        var formatSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FormatSelection.cs").Replace("\r\n", "\n");
        var timingText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateTiming.cs").Replace("\r\n", "\n");

        AssertContains(formatSelectionText, "private void UpdateSelectedFormat()");
        AssertContains(formatSelectionText, "private void RebuildVideoFormatOptions()");
        AssertContains(formatSelectionText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertDoesNotContain(formatSelectionText, "private FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertDoesNotContain(formatSelectionText, "private static bool TryInferFrameRateTimingFamily(");
        AssertContains(timingText, "private FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertContains(timingText, "private static MediaFormat SelectPreferredFrameRateFormat(");
        AssertContains(timingText, "private (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(");
        AssertContains(timingText, "private static bool TryInferFrameRateTimingFamily(");
        AssertContains(timingText, "private static bool TryParseFrameRateRational(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll()
    {
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");
        var automationSnapshotText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs");

        AssertDoesNotContain(diagnosticsHubText, "GetAutomationOptionsSnapshotAsync(cancellationToken)");
        AssertDoesNotContain(diagnosticsHubText, "Options = optionsSnapshot");
        AssertContains(automationSnapshotText, "GetAutomationOptionsSnapshotAsync");

        return Task.CompletedTask;
    }

    private static Task PreviewStartup_ToleratesMissingAudioCaptureDevices()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))");
        AssertContains(captureServiceText, "Audio preview requested but no audio capture device is available; continuing with video-only preview.");
        AssertDoesNotContain(captureServiceText, "Audio preview is enabled but no audio capture device is available.");

        return Task.CompletedTask;
    }

    private static async Task AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        SetPropertyOrBackingField(device, "AudioDeviceId", null);
        SetPropertyOrBackingField(device, "AudioDeviceName", null);
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        string? lastStatus = null;
        var handler = new EventHandler<string>((_, status) => lastStatus = status);
        var statusChanged = captureService.GetType().GetEvent("StatusChanged", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("CaptureService.StatusChanged event not found.");
        statusChanged.AddEventHandler(captureService, handler);

        try
        {
            var startAudioPreview = captureService.GetType().GetMethod(
                "StartAudioPreviewAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(CancellationToken) },
                modifiers: null);
            if (startAudioPreview == null)
            {
                throw new InvalidOperationException("CaptureService.StartAudioPreviewAsync method not found.");
            }

            if (startAudioPreview.Invoke(captureService, new object?[] { CancellationToken.None }) is not Task task)
            {
                throw new InvalidOperationException("CaptureService.StartAudioPreviewAsync did not return a Task.");
            }

            await task.ConfigureAwait(false);

            AssertEqual(false, GetBoolProperty(captureService, "IsAudioPreviewActive"), "IsAudioPreviewActive");
            AssertEqual("Audio preview unavailable", lastStatus, "StatusChanged");
        }
        finally
        {
            statusChanged.RemoveEventHandler(captureService, handler);
            await DisposeAsync(captureService).ConfigureAwait(false);
        }
    }

    private static Task AudioMonitoringVisuals_FollowRuntimePreviewActivity()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var audioMeterText = ReadRepoFile("Sussudio/MainWindow.AudioMeter.cs").Replace("\r\n", "\n");
        var audioMeterControllerText = ReadRepoFile("Sussudio/Controllers/AudioMeterController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "IsAudioPreviewActive");
        AssertContains(propertyChangedText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(propertyChangedText, "HandleAudioPreviewActiveChanged();");
        AssertContains(audioPropertyChangedText, "SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);");
        AssertContains(audioMeterText, "private AudioMeterController _audioMeterController = null!;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _audioMeterMonitoringStoryboard;");
        AssertContains(audioMeterControllerText, "private Storyboard? _audioMeterMonitoringStoryboard;");
        AssertContains(audioMeterControllerText, "_audioMeterMonitoringStoryboard?.Stop();");
        AssertContains(audioMeterControllerText, "AddOpacityAnimation(storyboard, _context.AudioMeterFill, isMonitoring ? 1.0 : 0.0");
        AssertContains(audioMeterControllerText, "AddOpacityAnimation(storyboard, _context.AudioPeakHoldIndicator, isMonitoring ? 0.9 : 0.4");
        AssertContains(audioMeterControllerText, "AddOpacityAnimation(storyboard, _context.AudioRangeMinMarker, isMonitoring ? 0.5 : 0.2");
        AssertContains(audioMeterControllerText, "AddOpacityAnimation(storyboard, _context.AudioRangeMaxMarker, isMonitoring ? 0.7 : 0.3");
        AssertContains(audioMeterControllerText, "private static void AddOpacityAnimation(");

        return Task.CompletedTask;
    }

    private static Task AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry()
    {
        var traceModelsText = ReadRepoFile("Sussudio/Models/Audio/AudioRampTraceModels.cs").Replace("\r\n", "\n");
        var audioMonitoringText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioMonitoring.cs").Replace("\r\n", "\n");
        var audioRampTraceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioRampTrace.cs").Replace("\r\n", "\n");
        var playbackText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs").Replace("\r\n", "\n");
        var playbackRenderText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.RenderThread.cs").Replace("\r\n", "\n");
        var playbackVolumeText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.Volume.cs").Replace("\r\n", "\n");
        var runtimeContractsText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var runtimeSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs").Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText().Replace("\r\n", "\n");
        var automationInterfaceText = ReadRepoFile("Sussudio/Services/Automation/IAutomationViewModel.cs").Replace("\r\n", "\n");

        AssertContains(traceModelsText, "public sealed class AudioRampTraceSnapshot");
        AssertContains(traceModelsText, "public sealed class AudioRampTraceEntry");
        AssertContains(traceModelsText, "public double PlaybackOutputPeak { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackOutputRms { get; init; }");
        AssertContains(traceModelsText, "public double PlaybackCurrentVolumePercent { get; init; }");
        AssertContains(traceModelsText, "public long PlaybackOutputAgeMs { get; init; }");

        AssertContains(audioRampTraceText, "private const int AudioRampTraceSampleIntervalMs = 10;");
        AssertContains(audioMonitoringText, "BeginAudioRampTraceSession(");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"volume-set\")");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"primed\"");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-started\"");
        AssertContains(audioMonitoringText, "RecordAudioRampTracePoint(\"monitoring-stopped\"");
        AssertContains(audioRampTraceText, "RunAudioRampTraceSamplerAsync");
        AssertContains(audioRampTraceText, "Task.Delay(AudioRampTraceSampleIntervalMs");
        AssertContains(audioRampTraceText, "GetAudioRampTraceSnapshotAsync");

        AssertContains(playbackRenderText, "UpdateOutputLevel(destinationSpan);");
        AssertContains(playbackRenderText, "private unsafe void RenderAvailableFrames()");
        AssertContains(playbackVolumeText, "internal sealed partial class WasapiAudioPlayback");
        AssertContains(playbackVolumeText, "public float TargetVolume => _targetVolume;");
        AssertContains(playbackVolumeText, "public float CurrentVolume => _currentVolume;");
        AssertContains(playbackVolumeText, "public float LastOutputPeak => _lastOutputPeak;");
        AssertContains(playbackVolumeText, "public float LastOutputRms => _lastOutputRms;");
        AssertContains(playbackVolumeText, "private void ApplyVolume(Span<byte> buffer)");
        AssertContains(playbackVolumeText, "private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)");
        AssertDoesNotContain(playbackText, "private void ApplyVolume(Span<byte> buffer)");
        AssertDoesNotContain(playbackText, "private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)");

        AssertContains(runtimeContractsText, "public double WasapiPlaybackTargetVolumePercent { get; init; }");
        AssertContains(runtimeContractsText, "public double WasapiPlaybackCurrentVolumePercent { get; init; }");
        AssertContains(runtimeContractsText, "public double WasapiPlaybackOutputPeak { get; init; }");
        AssertContains(runtimeContractsText, "public double WasapiPlaybackOutputRms { get; init; }");
        AssertContains(runtimeSnapshotText, "WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0,");
        AssertContains(runtimeSnapshotText, "WasapiPlaybackOutputPeak = wasapiPlayback?.LastOutputPeak ?? 0,");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetAudioRampTrace:");
        AssertContains(automationInterfaceText, "Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync");

        return Task.CompletedTask;
    }

    private static Task PreviewBackendLog_ReflectsVideoOnlyFallback()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "_wasapiAudioCapture != null");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video + WASAPI audio ingest.\"");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video only (no audio capture endpoint).\"");

        return Task.CompletedTask;
    }

    private static Task LivePixelFormatSurfaces_PreferReaderSourceSubtype()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Runtime.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "runtime.ReaderSourceSubtype ??");
        AssertContains(mainViewModelText, "runtime.LatestObservedFramePixelFormat ??");

        if (mainViewModelText.IndexOf("runtime.ReaderSourceSubtype ??", StringComparison.Ordinal) >
            mainViewModelText.IndexOf("runtime.LatestObservedFramePixelFormat ??", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MainViewModel.LivePixelFormat should prefer ReaderSourceSubtype before LatestObservedFramePixelFormat.");
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackPollingTimers_LiveInController()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var pollingAdapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackPolling.cs").Replace("\r\n", "\n");
        var timelineAdapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackTimeline.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/FlashbackPollingController.cs").Replace("\r\n", "\n");

        AssertContains(pollingAdapterText, "private FlashbackPollingController _flashbackPollingController = null!;");
        AssertContains(pollingAdapterText, "private void InitializeFlashbackPollingController()");
        AssertContains(pollingAdapterText, "IsWindowClosing = () => _isWindowClosing,");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartStatusPolling();");
        AssertContains(pollingAdapterText, "_flashbackPollingController.StopStatusPolling();");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartPlaybackPolling();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StopPlaybackPolling();");
        AssertContains(mainWindowText, "InitializeFlashbackPollingController();");
        AssertContains(timelineAdapterText, "StartStatusPolling = StartFlashbackStatusPolling,");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(flashbackText, "StartFlashbackPlaybackPolling();");
        AssertContains(flashbackText, "StopFlashbackPlaybackPolling();");
        AssertContains(controllerText, "internal sealed class FlashbackPollingController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _statusTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _playbackTimer;");
        AssertContains(controllerText, "public void StartStatusPolling()");
        AssertContains(controllerText, "public void StopStatusPolling()");
        AssertContains(controllerText, "public void StartPlaybackPolling()");
        AssertContains(controllerText, "public void StopPlaybackPolling()");
        AssertContains(controllerText, "_context.ViewModel.UpdateFlashbackBufferStatus();");
        AssertContains(controllerText, "_context.ViewModel.FlashbackPlaybackPosition = playback.PlaybackPosition;");
        AssertContains(controllerText, "FLASHBACK_STATUS_TIMER_FAIL");
        AssertContains(controllerText, "FLASHBACK_PLAYBACK_TIMER_FAIL");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackStatusTimer;");
        AssertDoesNotContain(flashbackText, "private void FlashbackStatusTimer_Tick(");
        AssertDoesNotContain(flashbackText, "private void FlashbackPlaybackTimer_Tick(");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlayheadMotion_LivesInFocusedPartial()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var scrubText = ReadRepoFile("Sussudio/MainWindow.FlashbackScrub.cs").Replace("\r\n", "\n");
        var playheadText = ReadRepoFile("Sussudio/MainWindow.FlashbackPlayhead.cs").Replace("\r\n", "\n");
        var pollingAdapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackPolling.cs").Replace("\r\n", "\n");

        AssertContains(playheadText, "Flashback current-time-indicator visuals");
        AssertContains(playheadText, "private enum FlashbackPlayheadMotion");
        AssertContains(playheadText, "private Visual? _flashbackPlayheadVisual;");
        AssertContains(playheadText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertContains(playheadText, "private void RefreshFlashbackCtiMotion(string reason)");
        AssertContains(playheadText, "private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)");
        AssertContains(playheadText, "StartLinearPlayheadExtrapolation(");
        AssertContains(playheadText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertContains(scrubText, "PositionFlashbackPlayhead(x, width, FlashbackPlayheadMotion.Magnetic);");
        AssertContains(flashbackText, "RefreshFlashbackCtiMotion(\"state_change\");");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertDoesNotContain(flashbackText, "private enum FlashbackPlayheadMotion");
        AssertDoesNotContain(flashbackText, "private Visual? _flashbackPlayheadVisual;");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(flashbackText, "private void RefreshFlashbackCtiMotion(string reason)");

        return Task.CompletedTask;
    }

    private static Task FlashbackMarkerPresentation_LivesInFocusedPartial()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var markerText = ReadRepoFile("Sussudio/MainWindow.FlashbackMarkers.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");

        AssertContains(markerText, "Flashback timeline marker presentation");
        AssertContains(markerText, "private static string FormatFlashbackDuration(TimeSpan ts)");
        AssertContains(markerText, "private void UpdateFlashbackMarkers()");
        AssertContains(markerText, "FlashbackInPointMarker.Visibility = Visibility.Visible;");
        AssertContains(markerText, "FlashbackOutPointMarker.Visibility = Visibility.Visible;");
        AssertContains(markerText, "FlashbackSelectionRegion.Visibility = Visibility.Visible;");
        AssertContains(flashbackText, "UpdateFlashbackMarkers();");
        AssertContains(flashbackText, "FormatFlashbackDuration(bufferDuration)");
        AssertContains(propertyChangedText, "HandleFlashbackRangeChanged();");
        AssertContains(flashbackPropertyChangedText, "Flashback-specific ViewModel property projections");
        AssertContains(flashbackPropertyChangedText, "UpdateFlashbackMarkers();");
        AssertDoesNotContain(flashbackText, "private void UpdateFlashbackMarkers()");
        AssertDoesNotContain(flashbackText, "private static string FormatFlashbackDuration(TimeSpan ts)");

        return Task.CompletedTask;
    }

    private static Task UnifiedVideoCapture_CpuMjpegEmitReportsNv12()
    {
        var unifiedVideoCapture = CreateInstance("Sussudio.Services.Capture.UnifiedVideoCapture");
        var observed = string.Empty;

        var setObserver = unifiedVideoCapture.GetType().GetMethod("SetPixelFormatDetectedCallback", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("SetPixelFormatDetectedCallback method not found.");
        setObserver.Invoke(unifiedVideoCapture, new object?[] { new Action<string>(value => observed = value) });

        var emitMethod = unifiedVideoCapture.GetType().GetMethod("OnMjpegPipelineFrameEmitted", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("OnMjpegPipelineFrameEmitted method not found.");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var rentMethod = frameType.GetMethod("Rent", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PooledVideoFrame.Rent method not found.");
        var frame = rentMethod.Invoke(
            null,
            new object[]
            {
                0L,
                0L,
                0L,
                2,
                2,
                Enum.Parse(formatType, "Nv12"),
                6
            })
            ?? throw new InvalidOperationException("PooledVideoFrame.Rent returned null.");
        try
        {
            emitMethod.Invoke(unifiedVideoCapture, new[] { frame });
        }
        finally
        {
            ((IDisposable)frame).Dispose();
        }

        AssertEqual("NV12", observed, "UnifiedVideoCapture.OnMjpegPipelineFrameEmitted observer format");
        return Task.CompletedTask;
    }

    private static async Task UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails()
    {
        var unifiedVideoCapture = CreateInstance("Sussudio.Services.Capture.UnifiedVideoCapture");
        var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
        var pipeline = CreateUninitializedObject(pipelineType);
        SeedPipelineStopFailureState(pipeline, pipelineType);

        SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", pipeline);
        SetPrivateField(pipeline, "_emitThread", Thread.CurrentThread);

        try
        {
            var stopAsync = unifiedVideoCapture.GetType().GetMethod("StopAsync", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("UnifiedVideoCapture.StopAsync method not found.");
            if (stopAsync.Invoke(unifiedVideoCapture, null) is not Task stopTask)
            {
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync did not return a Task.");
            }

            try
            {
                await stopTask.ConfigureAwait(false);
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync unexpectedly succeeded.");
            }
            catch (InvalidOperationException ex)
            {
                AssertContains(ex.Message, "emitter_self_join");
            }

            var retainedPipeline = GetPrivateField(unifiedVideoCapture, "_mjpegPipeline");
            AssertEqual(pipeline, retainedPipeline, "UnifiedVideoCapture._mjpegPipeline retained on stop failure");
        }
        finally
        {
            SetPrivateField(pipeline, "_emitThread", null);
            SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", null);

            var disposeMethod = pipelineType.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("ParallelMjpegDecodePipeline.Dispose method not found.");
            disposeMethod.Invoke(pipeline, null);

            await DisposeValueTaskAsync(unifiedVideoCapture).ConfigureAwait(false);
        }
    }

    private static async Task CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);
        SetPrivateField(captureService, "_isVideoPreviewActive", true);
        SetPrivateField(captureService, "_isAudioPreviewActive", true);
        SetPrivateField(captureService, "_isRecording", true);

        InvokeNonPublicInstanceMethod(
            captureService,
            "OnUnifiedVideoCaptureFatalError",
            new object?[] { null, new InvalidOperationException("synthetic hfr failure") });

        await WaitForConditionAsync(
            () =>
                string.Equals(GetPropertyValue(captureService, "SessionState")?.ToString(), "Faulted", StringComparison.Ordinal) &&
                !GetBoolProperty(captureService, "IsInitialized") &&
                !GetBoolProperty(captureService, "IsVideoPreviewActive") &&
                !GetBoolProperty(captureService, "IsAudioPreviewActive") &&
                !GetBoolProperty(captureService, "IsRecording"),
            "CaptureService fatal cleanup").ConfigureAwait(false);

        AssertEqual("Faulted", GetPropertyValue(captureService, "SessionState")?.ToString(), "SessionState");
        AssertEqual(false, GetBoolProperty(captureService, "IsInitialized"), "IsInitialized");
        AssertEqual(false, GetBoolProperty(captureService, "IsVideoPreviewActive"), "IsVideoPreviewActive");
        AssertEqual(false, GetBoolProperty(captureService, "IsAudioPreviewActive"), "IsAudioPreviewActive");
        AssertEqual(false, GetBoolProperty(captureService, "IsRecording"), "IsRecording");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static Task CaptureErrors_RefreshViewModelRuntimeFlags()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Runtime.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "IsInitialized = _captureService.IsInitialized;");
        AssertContains(mainViewModelText, "IsPreviewing = _captureService.IsVideoPreviewActive;");
        AssertContains(mainViewModelText, "IsRecording = _captureService.IsRecording;");
        AssertContains(mainViewModelText, "UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(mainViewModelText, "UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);");

        return Task.CompletedTask;
    }
}
