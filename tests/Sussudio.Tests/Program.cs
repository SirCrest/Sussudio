using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using System.Xml.Linq;

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
                "Service namespaces follow service folders",
                ServiceNamespaces_FollowServiceFolders),
            await RunCheckAsync(
                "AutomationCommandKind source ownership is contract-aligned",
                AutomationCommandKind_SourceOwnership_IsModelAligned),
            await RunCheckAsync(
                "Diagnostics snapshot refresh is serialized for recording responses",
                DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses),
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
                "Diagnostic session result formatting has a named owner",
                DiagnosticSessionResultFormatter_OwnsFormattedSummaryText),
            await RunCheckAsync(
                "Diagnostic session shared text helpers have a named owner",
                DiagnosticSessionText_OwnsSharedFormattingHelpers),
            await RunCheckAsync(
                "Diagnostic session pipe retry policy has a named owner",
                DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification),
            await RunCheckAsync(
                "Diagnostic session JSON artifacts have a named owner",
                DiagnosticSessionJsonArtifacts_OwnsArtifactsAndResponseExtraction),
            await RunCheckAsync(
                "Diagnostic session run state has a named owner",
                DiagnosticSessionRunState_OwnsTerminalAndLiveState),
            await RunCheckAsync(
                "Diagnostic session scenario plan has a named owner",
                DiagnosticSessionScenarioPlan_OwnsScenarioFlags),
            await RunCheckAsync(
                "Diagnostic session background tasks have a named owner",
                DiagnosticSessionBackgroundTasks_OwnTaskDraining),
            await RunCheckAsync(
                "Diagnostic session cleanup policy has a named owner",
                DiagnosticSessionCleanupPolicy_OwnsRestoreWarnings),
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
                "LibAv recording stop validates final output",
                LibAvRecordingSink_StopValidatesFinalOutput),
            await RunCheckAsync(
                "Recording video try enqueue paths do not block capture callbacks",
                RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks),
            await RunCheckAsync(
                "WASAPI audio capture rejects incomplete hot audio writes",
                WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks),
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

    private static async Task GetRuntimeSnapshot_UsesObservedTelemetryStateInsteadOfInferredCounts()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_firstObservedFramePixelFormat", "NV12");
        SetPrivateField(captureService, "_latestObservedFramePixelFormat", "BGRA8");
        SetPrivateField(captureService, "_latestObservedSurfaceFormat", "BGRA8");
        SetPrivateField(captureService, "_observedP010FrameCount", 0L);
        SetPrivateField(captureService, "_observedNv12FrameCount", 2L);
        SetPrivateField(captureService, "_observedOtherFrameCount", 3L);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual(0L, GetLongProperty(snapshot, "ObservedP010FrameCount"), "ObservedP010FrameCount");
        AssertEqual(2L, GetLongProperty(snapshot, "ObservedNv12FrameCount"), "ObservedNv12FrameCount");
        AssertEqual(3L, GetLongProperty(snapshot, "ObservedOtherFrameCount"), "ObservedOtherFrameCount");
        AssertEqual("NV12", GetStringProperty(snapshot, "FirstObservedFramePixelFormat"), "FirstObservedFramePixelFormat");
        AssertEqual("BGRA8", GetStringProperty(snapshot, "LatestObservedFramePixelFormat"), "LatestObservedFramePixelFormat");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_PreservesReaderSourceSubtype_WhenObservedFramesAreDecoded()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_actualPixelFormat", "MJPG");
        SetPrivateField(captureService, "_latestObservedFramePixelFormat", "NV12");

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("MJPG", GetStringProperty(snapshot, "ReaderSourceSubtype"), "ReaderSourceSubtype");
        AssertEqual("NV12", GetStringProperty(snapshot, "LatestObservedFramePixelFormat"), "LatestObservedFramePixelFormat");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_TelemetryAlignment_Mismatch_WhenSourceModeDiffersFromRequest()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var sourceTelemetry = CreateInstance("Sussudio.Models.SourceSignalTelemetrySnapshot");
        SetPropertyOrBackingField(sourceTelemetry, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(sourceTelemetry, "Origin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(sourceTelemetry, "OriginDetail", "RegressionHarness");
        SetPropertyOrBackingField(sourceTelemetry, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(sourceTelemetry, "Width", 1280);
        SetPropertyOrBackingField(sourceTelemetry, "Height", 720);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateExact", 30d);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateArg", "30/1");
        SetPropertyOrBackingField(sourceTelemetry, "IsHdr", false);
        SetPrivateField(captureService, "_latestSourceTelemetry", sourceTelemetry);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("Mismatch", GetStringProperty(snapshot, "TelemetryAlignmentStatus"), "TelemetryAlignmentStatus");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "width expected");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "hdr expected");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_TelemetryAlignment_Unavailable_WhenTelemetryUnavailable()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var createUnavailable = telemetryType.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null);
        if (createUnavailable == null)
        {
            throw new InvalidOperationException("SourceSignalTelemetrySnapshot.CreateUnavailable not found.");
        }

        var unavailableTelemetry = createUnavailable.Invoke(null, new object?[] { "regression-harness-unavailable", null });
        SetPrivateField(captureService, "_latestSourceTelemetry", unavailableTelemetry);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("Unavailable", GetStringProperty(snapshot, "TelemetryAlignmentStatus"), "TelemetryAlignmentStatus");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "unavailable");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static Task Diagnostics_HdrTruthVerdict_TreatsHdrSourceSdrRequestAsExpected()
    {
        var diagnosticsType = RequireType("Sussudio.Services.Automation.AutomationDiagnosticsHub");
        var runtimeType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var verifierResultType = RequireType("Sussudio.Models.RecordingVerificationResult");
        var method = diagnosticsType.GetMethod(
            "BuildHdrTruthVerdict",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { runtimeType, typeof(bool), verifierResultType },
            modifiers: null)
            ?? throw new InvalidOperationException("BuildHdrTruthVerdict not found.");

        var runtime = Activator.CreateInstance(runtimeType)!;
        SetPropertyBackingField(runtime, "LatestObservedFramePixelFormat", "NV12");
        SetPropertyBackingField(runtime, "ObservedNv12FrameCount", 1L);
        SetPropertyBackingField(runtime, "SourceIsHdr", (bool?)true);

        var verdict = method.Invoke(null, new object?[] { runtime, false, null })
            ?? throw new InvalidOperationException("BuildHdrTruthVerdict returned null.");

        AssertEqual("expected-sdr-capture", GetStringProperty(verdict, "SourceVsCaptureParity"), "SourceVsCaptureParity");
        AssertEqual("sdr-8bit", GetStringProperty(verdict, "FinalClassification"), "FinalClassification");

        return Task.CompletedTask;
    }

    private static async Task NativeXuTelemetry_AcceptsKnown4kXProductRevisions()
    {
        var provider = CreateInstance("Sussudio.Services.Telemetry.NativeXuAtCommandProvider");

        foreach (var productId in new[] { "009b", "009c", "009d" })
        {
            var device = BuildDevice($"\\\\?\\usb#vid_0fd9&pid_{productId}&mi_00#synthetic#{Guid.NewGuid():N}\\global");
            var readAsync = provider.GetType().GetMethod(
                "ReadAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { device.GetType(), typeof(CancellationToken) },
                modifiers: null);
            if (readAsync == null)
            {
                throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync method not found.");
            }

            if (readAsync.Invoke(provider, new[] { device, CancellationToken.None }) is not Task task)
            {
                throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync did not return a Task.");
            }

            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync task result not found.");
            var snapshot = resultProperty.GetValue(task)
                ?? throw new InvalidOperationException("NativeXuAtCommandProvider.ReadAsync returned null snapshot.");
            var diagnostic = GetStringProperty(snapshot, "DiagnosticSummary");
            if (string.Equals(diagnostic, "nativexu-device-unsupported", StringComparison.Ordinal) ||
                diagnostic.StartsWith("nativexu-device-unsupported:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"NativeXu provider rejected 4K X product revision {productId} as unsupported.");
            }
        }
    }

    private static async Task CaptureHealthSnapshot_PropagatesStructuredSourceTelemetryDetails()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var sourceTelemetry = CreateInstance("Sussudio.Models.SourceSignalTelemetrySnapshot");
        SetPropertyOrBackingField(sourceTelemetry, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(sourceTelemetry, "Origin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(sourceTelemetry, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(sourceTelemetry, "Width", 3840);
        SetPropertyOrBackingField(sourceTelemetry, "Height", 2160);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateExact", 119.88d);
        SetPropertyOrBackingField(sourceTelemetry, "IsHdr", true);
        SetPropertyOrBackingField(sourceTelemetry, "VideoFormat", "YCbCr422");
        SetPropertyOrBackingField(sourceTelemetry, "Colorimetry", "BT.2020");
        SetPropertyOrBackingField(sourceTelemetry, "Quantization", "Limited");
        SetPropertyOrBackingField(sourceTelemetry, "HdrTransferFunction", "HDR10 / PQ");
        SetPropertyOrBackingField(sourceTelemetry, "HdrTransferCode", 2);
        SetPropertyOrBackingField(sourceTelemetry, "AudioFormat", "Unknown (2)");
        SetPropertyOrBackingField(sourceTelemetry, "AudioSampleRate", "Unknown (7)");
        SetPropertyOrBackingField(sourceTelemetry, "InputSource", "HDMI (0)");
        SetPropertyOrBackingField(sourceTelemetry, "UsbHostProtocol", "Isochronous (2)");
        SetPropertyOrBackingField(sourceTelemetry, "HdcpMode", "Unknown (1)");
        SetPropertyOrBackingField(sourceTelemetry, "HdcpVersion", "0200");
        SetPropertyOrBackingField(sourceTelemetry, "RxTxHdcpVersion", "Unknown (3)");
        SetPropertyOrBackingField(sourceTelemetry, "RawTimingHex", "3000CA0830117008");

        var detailEntryType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
        var detailEntry = Activator.CreateInstance(detailEntryType, "Signal Details", "Quantization", "Limited", "Limited")
            ?? throw new InvalidOperationException("SourceTelemetryDetailEntry instance creation failed.");
        var detailArray = Array.CreateInstance(detailEntryType, 1);
        detailArray.SetValue(detailEntry, 0);
        SetPropertyOrBackingField(sourceTelemetry, "DetailEntries", detailArray);

        SetPrivateField(captureService, "_latestSourceTelemetry", sourceTelemetry);

        var health = InvokeInstanceMethod(captureService, "GetHealthSnapshot");
        AssertEqual("YCbCr422", GetStringProperty(health, "SourceVideoFormat"), "SourceVideoFormat");
        AssertEqual("BT.2020", GetStringProperty(health, "SourceColorimetry"), "SourceColorimetry");
        AssertEqual("Limited", GetStringProperty(health, "SourceQuantization"), "SourceQuantization");
        AssertEqual("HDR10 / PQ", GetStringProperty(health, "SourceHdrTransferFunction"), "SourceHdrTransferFunction");
        AssertEqual("Unknown (2)", GetStringProperty(health, "SourceAudioFormat"), "SourceAudioFormat");
        AssertEqual("Unknown (7)", GetStringProperty(health, "SourceAudioSampleRate"), "SourceAudioSampleRate");
        AssertEqual("HDMI (0)", GetStringProperty(health, "SourceInputSource"), "SourceInputSource");
        AssertEqual("Isochronous (2)", GetStringProperty(health, "SourceUsbHostProtocol"), "SourceUsbHostProtocol");
        AssertEqual("Unknown (1)", GetStringProperty(health, "SourceHdcpMode"), "SourceHdcpMode");
        AssertEqual("0200", GetStringProperty(health, "SourceHdcpVersion"), "SourceHdcpVersion");
        AssertEqual("Unknown (3)", GetStringProperty(health, "SourceRxTxHdcpVersion"), "SourceRxTxHdcpVersion");
        AssertEqual("3000CA0830117008", GetStringProperty(health, "SourceRawTimingHex"), "SourceRawTimingHex");

        var details = GetPropertyValue(health, "SourceTelemetryDetails") as System.Collections.IEnumerable
            ?? throw new InvalidOperationException("SourceTelemetryDetails should be enumerable.");
        var detailCount = 0;
        foreach (var _ in details)
        {
            detailCount++;
        }

        AssertEqual(1, detailCount, "SourceTelemetryDetails.Count");
        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static Task AutomationSnapshots_ExposeHighConfidenceSourceTelemetryFields()
    {
        var contractsText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs").Replace("\r\n", "\n");
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs").Replace("\r\n", "\n");

        AssertContains(contractsText, "public string? SourceFirmware { get; init; }");
        AssertContains(contractsText, "public string? SourceAudioFormat { get; init; }");
        AssertContains(contractsText, "public string? SourceAudioSampleRate { get; init; }");
        AssertContains(contractsText, "public string? SourceInputSource { get; init; }");
        AssertContains(contractsText, "public string? SourceUsbHostProtocol { get; init; }");
        AssertContains(contractsText, "public string? SourceHdcpMode { get; init; }");
        AssertContains(contractsText, "public string? SourceHdcpVersion { get; init; }");
        AssertContains(contractsText, "public string? SourceRxTxHdcpVersion { get; init; }");
        AssertContains(contractsText, "public string? SourceRawTimingHex { get; init; }");

        AssertContains(diagnosticsHubText, "SourceFirmware = captureRuntime.SourceFirmware,");
        AssertContains(diagnosticsHubText, "SourceAudioFormat = captureRuntime.SourceAudioFormat,");
        AssertContains(diagnosticsHubText, "SourceAudioSampleRate = captureRuntime.SourceAudioSampleRate,");
        AssertContains(diagnosticsHubText, "SourceInputSource = captureRuntime.SourceInputSource,");
        AssertContains(diagnosticsHubText, "SourceUsbHostProtocol = captureRuntime.SourceUsbHostProtocol,");
        AssertContains(diagnosticsHubText, "SourceHdcpMode = captureRuntime.SourceHdcpMode,");
        AssertContains(diagnosticsHubText, "SourceHdcpVersion = captureRuntime.SourceHdcpVersion,");
        AssertContains(diagnosticsHubText, "SourceRxTxHdcpVersion = captureRuntime.SourceRxTxHdcpVersion,");
        AssertContains(diagnosticsHubText, "SourceRawTimingHex = captureRuntime.SourceRawTimingHex,");

        return Task.CompletedTask;
    }

    private static async Task GetRuntimeSnapshot_PipelineParity_Ready_WhenHdrRequestedAndIdle()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "RequestedPipelineMode"), "RequestedPipelineMode");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "ActivePipelineMode"), "ActivePipelineMode");
        AssertEqual(true, GetBoolProperty(snapshot, "PipelineModeMatched"), "PipelineModeMatched");
        AssertEqual("Ready", GetStringProperty(snapshot, "PipelineModeStatus"), "PipelineModeStatus");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_PipelineParity_Violation_WhenHdrRequestedButIngressIsSdr()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_activeRecordingSettings", settings);
        SetPrivateField(captureService, "_isRecording", true);
        SetPrivateField(captureService, "_activeVideoInputPixelFormat", "nv12");

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "RequestedPipelineMode"), "RequestedPipelineMode");
        AssertEqual("SDR", GetStringProperty(snapshot, "ActivePipelineMode"), "ActivePipelineMode");
        AssertEqual(false, GetBoolProperty(snapshot, "PipelineModeMatched"), "PipelineModeMatched");
        AssertEqual("Violation", GetStringProperty(snapshot, "PipelineModeStatus"), "PipelineModeStatus");
        AssertContains(GetStringProperty(snapshot, "PipelineModeReason"), "Requested pipeline");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_ThreadHealthProbes_DefaultToZeroWhenInactive()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual(false, GetBoolProperty(snapshot, "SourceReaderReadOutstanding"), "SourceReaderReadOutstanding");
        AssertEqual(0L, GetLongProperty(snapshot, "SourceReaderReadOutstandingMs"), "SourceReaderReadOutstandingMs");
        AssertEqual(0L, GetLongProperty(snapshot, "SourceReaderLastFrameTickMs"), "SourceReaderLastFrameTickMs");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiCaptureCallbackCount"), "WasapiCaptureCallbackCount");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiCaptureAudioLevelEventsFired"), "WasapiCaptureAudioLevelEventsFired");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiPlaybackRenderCallbackCount"), "WasapiPlaybackRenderCallbackCount");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiPlaybackQueueDropCount"), "WasapiPlaybackQueueDropCount");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackQueueDurationMs"), 0.0001, "WasapiPlaybackQueueDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackActiveChunkDurationMs"), 0.0001, "WasapiPlaybackActiveChunkDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackEndpointQueuedDurationMs"), 0.0001, "WasapiPlaybackEndpointQueuedDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackBufferedDurationMs"), 0.0001, "WasapiPlaybackBufferedDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackStreamLatencyMs"), 0.0001, "WasapiPlaybackStreamLatencyMs");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetHealthSnapshot_UsesCachedMjpegTimingMetricsWhenCaptureIsGone()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(
            captureService,
            "_lastMjpegPipelineTimingMetrics",
            CreateMjpegTimingMetrics(
                decodeSampleCount: 7,
                decodeAvgMs: 1.5,
                decodeP95Ms: 2.5,
                decodeMaxMs: 3.5,
                interopCopySampleCount: 5,
                interopCopyAvgMs: 4.5,
                interopCopyP95Ms: 5.5,
                interopCopyMaxMs: 6.5,
                callbackSampleCount: 9,
                callbackAvgMs: 7.5,
                callbackP95Ms: 8.5,
                callbackMaxMs: 9.5));
        SetPrivateField(
            captureService,
            "_lastFullMjpegPipelineTimingMetrics",
            CreateFullMjpegPipelineTimingMetrics(
                decoderCount: 3,
                decodeSampleCount: 17,
                decodeAvgMs: 4.1,
                decodeP95Ms: 4.6,
                decodeMaxMs: 5.2,
                reorderSampleCount: 19,
                reorderAvgMs: 0.7,
                reorderP95Ms: 1.1,
                reorderMaxMs: 1.8,
                pipelineSampleCount: 23,
                pipelineAvgMs: 5.1,
                pipelineP95Ms: 5.7,
                pipelineMaxMs: 6.4,
                totalDecoded: 101,
                totalEmitted: 97,
                totalDropped: 4,
                reorderSkips: 2,
                reorderBufferDepth: 1,
                perDecoder: new[]
                {
                    CreatePerDecoderMetrics(0, 31, 4.0, 4.4, 4.9),
                    CreatePerDecoderMetrics(1, 33, 4.2, 4.7, 5.3),
                    CreatePerDecoderMetrics(2, 35, 4.1, 4.8, 5.4)
                }));
        SetPrivateField(captureService, "_unifiedVideoCapture", null);

        var snapshot = InvokeInstanceMethod(captureService, "GetHealthSnapshot");
        AssertEqual(7L, GetLongProperty(snapshot, "MjpegDecodeSampleCount"), "MjpegDecodeSampleCount");
        AssertEqual(5L, GetLongProperty(snapshot, "MjpegInteropCopySampleCount"), "MjpegInteropCopySampleCount");
        AssertEqual(9L, GetLongProperty(snapshot, "MjpegCallbackSampleCount"), "MjpegCallbackSampleCount");
        AssertEqual(1.5, GetDoubleProperty(snapshot, "MjpegDecodeAvgMs"), "MjpegDecodeAvgMs");
        AssertEqual(8.5, GetDoubleProperty(snapshot, "MjpegCallbackP95Ms"), "MjpegCallbackP95Ms");
        AssertEqual(3L, GetLongProperty(snapshot, "MjpegDecoderCount"), "MjpegDecoderCount");
        AssertEqual(19L, GetLongProperty(snapshot, "MjpegReorderSampleCount"), "MjpegReorderSampleCount");
        AssertEqual(23L, GetLongProperty(snapshot, "MjpegPipelineSampleCount"), "MjpegPipelineSampleCount");
        AssertEqual(101L, GetLongProperty(snapshot, "MjpegTotalDecoded"), "MjpegTotalDecoded");
        AssertEqual(97L, GetLongProperty(snapshot, "MjpegTotalEmitted"), "MjpegTotalEmitted");
        AssertEqual(4L, GetLongProperty(snapshot, "MjpegTotalDropped"), "MjpegTotalDropped");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedFramesQueued"), "MjpegCompressedFramesQueued");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedDropsQueueFull"), "MjpegCompressedDropsQueueFull");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedDropsDisposed"), "MjpegCompressedDropsDisposed");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegDecodeFailures"), "MjpegDecodeFailures");
        AssertEqual(2L, GetLongProperty(snapshot, "MjpegReorderSkips"), "MjpegReorderSkips");
        AssertEqual(1L, GetLongProperty(snapshot, "MjpegReorderBufferDepth"), "MjpegReorderBufferDepth");
        AssertEqual(0.7, GetDoubleProperty(snapshot, "MjpegReorderAvgMs"), "MjpegReorderAvgMs");
        AssertEqual(5.7, GetDoubleProperty(snapshot, "MjpegPipelineP95Ms"), "MjpegPipelineP95Ms");

        var perDecoder = GetPropertyValue(snapshot, "MjpegPerDecoder") as Array
            ?? throw new InvalidOperationException("MjpegPerDecoder was not an array.");
        AssertEqual(3, perDecoder.Length, "MjpegPerDecoder.Length");
        AssertEqual(1, GetIntProperty(perDecoder.GetValue(1)!, "WorkerIndex"), "MjpegPerDecoder[1].WorkerIndex");
        AssertEqual(33L, GetLongProperty(perDecoder.GetValue(1)!, "SampleCount"), "MjpegPerDecoder[1].SampleCount");
        AssertEqual(4.8, GetDoubleProperty(perDecoder.GetValue(2)!, "P95Ms"), "MjpegPerDecoder[2].P95Ms");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetDiagnosticsSnapshot_PropagatesMjpegTimingMetrics()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(
            captureService,
            "_lastMjpegPipelineTimingMetrics",
            CreateMjpegTimingMetrics(
                decodeSampleCount: 11,
                decodeAvgMs: 10.1,
                decodeP95Ms: 10.2,
                decodeMaxMs: 10.3,
                interopCopySampleCount: 12,
                interopCopyAvgMs: 11.1,
                interopCopyP95Ms: 11.2,
                interopCopyMaxMs: 11.3,
                callbackSampleCount: 13,
                callbackAvgMs: 12.1,
                callbackP95Ms: 12.2,
                callbackMaxMs: 12.3));
        SetPrivateField(
            captureService,
            "_lastFullMjpegPipelineTimingMetrics",
            CreateFullMjpegPipelineTimingMetrics(
                decoderCount: 4,
                decodeSampleCount: 40,
                decodeAvgMs: 6.1,
                decodeP95Ms: 7.1,
                decodeMaxMs: 8.2,
                reorderSampleCount: 41,
                reorderAvgMs: 1.2,
                reorderP95Ms: 1.9,
                reorderMaxMs: 2.8,
                pipelineSampleCount: 42,
                pipelineAvgMs: 7.4,
                pipelineP95Ms: 8.6,
                pipelineMaxMs: 9.9,
                totalDecoded: 400,
                totalEmitted: 390,
                totalDropped: 10,
                reorderSkips: 3,
                reorderBufferDepth: 2,
                perDecoder: new[]
                {
                    CreatePerDecoderMetrics(0, 100, 5.8, 6.7, 7.8),
                    CreatePerDecoderMetrics(1, 101, 6.0, 7.0, 8.0),
                    CreatePerDecoderMetrics(2, 99, 6.2, 7.2, 8.3),
                    CreatePerDecoderMetrics(3, 100, 6.4, 7.4, 8.5)
                }));
        SetPrivateField(captureService, "_unifiedVideoCapture", null);

        var snapshot = InvokeInstanceMethod(captureService, "GetDiagnosticsSnapshot");
        AssertEqual(11L, GetLongProperty(snapshot, "MjpegDecodeSampleCount"), "MjpegDecodeSampleCount");
        AssertEqual(12L, GetLongProperty(snapshot, "MjpegInteropCopySampleCount"), "MjpegInteropCopySampleCount");
        AssertEqual(13L, GetLongProperty(snapshot, "MjpegCallbackSampleCount"), "MjpegCallbackSampleCount");
        AssertEqual(10.2, GetDoubleProperty(snapshot, "MjpegDecodeP95Ms"), "MjpegDecodeP95Ms");
        AssertEqual(12.3, GetDoubleProperty(snapshot, "MjpegCallbackMaxMs"), "MjpegCallbackMaxMs");
        AssertEqual(4L, GetLongProperty(snapshot, "MjpegDecoderCount"), "MjpegDecoderCount");
        AssertEqual(41L, GetLongProperty(snapshot, "MjpegReorderSampleCount"), "MjpegReorderSampleCount");
        AssertEqual(42L, GetLongProperty(snapshot, "MjpegPipelineSampleCount"), "MjpegPipelineSampleCount");
        AssertEqual(400L, GetLongProperty(snapshot, "MjpegTotalDecoded"), "MjpegTotalDecoded");
        AssertEqual(390L, GetLongProperty(snapshot, "MjpegTotalEmitted"), "MjpegTotalEmitted");
        AssertEqual(10L, GetLongProperty(snapshot, "MjpegTotalDropped"), "MjpegTotalDropped");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedFramesDequeued"), "MjpegCompressedFramesDequeued");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedDropsByteBudget"), "MjpegCompressedDropsByteBudget");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegEmitFailures"), "MjpegEmitFailures");
        AssertEqual(3L, GetLongProperty(snapshot, "MjpegReorderSkips"), "MjpegReorderSkips");
        AssertEqual(2L, GetLongProperty(snapshot, "MjpegReorderBufferDepth"), "MjpegReorderBufferDepth");
        AssertEqual(7.4, GetDoubleProperty(snapshot, "MjpegPipelineAvgMs"), "MjpegPipelineAvgMs");

        var perDecoder = GetPropertyValue(snapshot, "MjpegPerDecoder") as Array
            ?? throw new InvalidOperationException("MjpegPerDecoder was not an array.");
        AssertEqual(4, perDecoder.Length, "MjpegPerDecoder.Length");
        AssertEqual(99L, GetLongProperty(perDecoder.GetValue(2)!, "SampleCount"), "MjpegPerDecoder[2].SampleCount");
        AssertEqual(8.5, GetDoubleProperty(perDecoder.GetValue(3)!, "MaxMs"), "MjpegPerDecoder[3].MaxMs");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static Task AutomationSnapshot_ExposesFullCpuMjpegMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var decoderType = RequireType("Sussudio.Models.MjpegDecoderAutomationSnapshot");

        AssertNotNull(snapshotType.GetProperty("MjpegDecoderCount"), "AutomationSnapshot.MjpegDecoderCount");
        AssertNotNull(snapshotType.GetProperty("MjpegReorderSampleCount"), "AutomationSnapshot.MjpegReorderSampleCount");
        AssertNotNull(snapshotType.GetProperty("MjpegPipelineSampleCount"), "AutomationSnapshot.MjpegPipelineSampleCount");
        AssertNotNull(snapshotType.GetProperty("MjpegTotalDecoded"), "AutomationSnapshot.MjpegTotalDecoded");
        AssertNotNull(snapshotType.GetProperty("MjpegTotalEmitted"), "AutomationSnapshot.MjpegTotalEmitted");
        AssertNotNull(snapshotType.GetProperty("MjpegTotalDropped"), "AutomationSnapshot.MjpegTotalDropped");
        AssertNotNull(snapshotType.GetProperty("MjpegCompressedFramesQueued"), "AutomationSnapshot.MjpegCompressedFramesQueued");
        AssertNotNull(snapshotType.GetProperty("MjpegCompressedFramesDequeued"), "AutomationSnapshot.MjpegCompressedFramesDequeued");
        AssertNotNull(snapshotType.GetProperty("MjpegCompressedDropsQueueFull"), "AutomationSnapshot.MjpegCompressedDropsQueueFull");
        AssertNotNull(snapshotType.GetProperty("MjpegCompressedDropsByteBudget"), "AutomationSnapshot.MjpegCompressedDropsByteBudget");
        AssertNotNull(snapshotType.GetProperty("MjpegCompressedDropsDisposed"), "AutomationSnapshot.MjpegCompressedDropsDisposed");
        AssertNotNull(snapshotType.GetProperty("MjpegDecodeFailures"), "AutomationSnapshot.MjpegDecodeFailures");
        AssertNotNull(snapshotType.GetProperty("MjpegReorderCollisions"), "AutomationSnapshot.MjpegReorderCollisions");
        AssertNotNull(snapshotType.GetProperty("MjpegEmitFailures"), "AutomationSnapshot.MjpegEmitFailures");
        AssertNotNull(snapshotType.GetProperty("MjpegCompressedQueueDepth"), "AutomationSnapshot.MjpegCompressedQueueDepth");
        AssertNotNull(snapshotType.GetProperty("MjpegCompressedQueueBytes"), "AutomationSnapshot.MjpegCompressedQueueBytes");
        AssertNotNull(snapshotType.GetProperty("MjpegCompressedQueueByteBudget"), "AutomationSnapshot.MjpegCompressedQueueByteBudget");
        AssertNotNull(snapshotType.GetProperty("MjpegReorderSkips"), "AutomationSnapshot.MjpegReorderSkips");
        AssertNotNull(snapshotType.GetProperty("MjpegReorderBufferDepth"), "AutomationSnapshot.MjpegReorderBufferDepth");
        AssertNotNull(snapshotType.GetProperty("MjpegPreviewJitterLastSelectedPreviewPresentId"), "AutomationSnapshot.MjpegPreviewJitterLastSelectedPreviewPresentId");
        AssertNotNull(snapshotType.GetProperty("MjpegPreviewJitterLastSelectedSourceSequenceNumber"), "AutomationSnapshot.MjpegPreviewJitterLastSelectedSourceSequenceNumber");
        AssertNotNull(snapshotType.GetProperty("MjpegPreviewJitterLastSelectedSourceLatencyMs"), "AutomationSnapshot.MjpegPreviewJitterLastSelectedSourceLatencyMs");
        AssertNotNull(snapshotType.GetProperty("MjpegPreviewJitterLastDroppedSourceSequenceNumber"), "AutomationSnapshot.MjpegPreviewJitterLastDroppedSourceSequenceNumber");
        AssertNotNull(snapshotType.GetProperty("MjpegPreviewJitterClearedDropCount"), "AutomationSnapshot.MjpegPreviewJitterClearedDropCount");
        AssertNotNull(snapshotType.GetProperty("MjpegPreviewJitterResumeReprimeCount"), "AutomationSnapshot.MjpegPreviewJitterResumeReprimeCount");
        AssertNotNull(snapshotType.GetProperty("MjpegPreviewJitterLastDropReason"), "AutomationSnapshot.MjpegPreviewJitterLastDropReason");
        AssertNotNull(snapshotType.GetProperty("PreviewD3DFrameLatencyWaitTimeoutCount"), "AutomationSnapshot.PreviewD3DFrameLatencyWaitTimeoutCount");
        AssertNotNull(snapshotType.GetProperty("PreviewD3DFrameLatencyWaitP95Ms"), "AutomationSnapshot.PreviewD3DFrameLatencyWaitP95Ms");
        AssertNotNull(snapshotType.GetProperty("PreviewD3DFrameLatencyWaitMaxMs"), "AutomationSnapshot.PreviewD3DFrameLatencyWaitMaxMs");
        AssertNotNull(snapshotType.GetProperty("PreviewD3DFrameStatsRecentMissedRefreshCount"), "AutomationSnapshot.PreviewD3DFrameStatsRecentMissedRefreshCount");
        AssertNotNull(snapshotType.GetProperty("PreviewD3DFrameStatsRecentFailureCount"), "AutomationSnapshot.PreviewD3DFrameStatsRecentFailureCount");
        AssertNotNull(snapshotType.GetProperty("PreviewD3DRenderThreadFailureCount"), "AutomationSnapshot.PreviewD3DRenderThreadFailureCount");
        AssertNotNull(snapshotType.GetProperty("PreviewD3DLastRenderThreadFailureType"), "AutomationSnapshot.PreviewD3DLastRenderThreadFailureType");
        AssertNotNull(snapshotType.GetProperty("PreviewD3DLastRenderThreadFailureMessage"), "AutomationSnapshot.PreviewD3DLastRenderThreadFailureMessage");
        AssertNotNull(snapshotType.GetProperty("PreviewD3DLastRenderThreadFailureHResult"), "AutomationSnapshot.PreviewD3DLastRenderThreadFailureHResult");
        AssertNotNull(snapshotType.GetProperty("DiagnosticHealthStatus"), "AutomationSnapshot.DiagnosticHealthStatus");
        AssertNotNull(snapshotType.GetProperty("DiagnosticLikelyStage"), "AutomationSnapshot.DiagnosticLikelyStage");
        AssertNotNull(snapshotType.GetProperty("DiagnosticSummary"), "AutomationSnapshot.DiagnosticSummary");
        AssertNotNull(snapshotType.GetProperty("DiagnosticEvidence"), "AutomationSnapshot.DiagnosticEvidence");
        AssertNotNull(snapshotType.GetProperty("DiagnosticSourceLane"), "AutomationSnapshot.DiagnosticSourceLane");
        AssertNotNull(snapshotType.GetProperty("DiagnosticDecodeLane"), "AutomationSnapshot.DiagnosticDecodeLane");
        AssertNotNull(snapshotType.GetProperty("DiagnosticPreviewLane"), "AutomationSnapshot.DiagnosticPreviewLane");
        AssertNotNull(snapshotType.GetProperty("DiagnosticRenderLane"), "AutomationSnapshot.DiagnosticRenderLane");
        AssertNotNull(snapshotType.GetProperty("DiagnosticPresentLane"), "AutomationSnapshot.DiagnosticPresentLane");
        AssertNotNull(snapshotType.GetProperty("DiagnosticRecordingLane"), "AutomationSnapshot.DiagnosticRecordingLane");
        AssertNotNull(snapshotType.GetProperty("DiagnosticAudioLane"), "AutomationSnapshot.DiagnosticAudioLane");
        AssertNotNull(snapshotType.GetProperty("PreviewPacingLikelySlowStage"), "AutomationSnapshot.PreviewPacingLikelySlowStage");
        AssertNotNull(snapshotType.GetProperty("PreviewPacingSlowStageConfidence"), "AutomationSnapshot.PreviewPacingSlowStageConfidence");
        AssertNotNull(snapshotType.GetProperty("PreviewPacingSlowStageEvidence"), "AutomationSnapshot.PreviewPacingSlowStageEvidence");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandCommandsEnqueued"), "AutomationSnapshot.CaptureCommandCommandsEnqueued");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandCommandsCompleted"), "AutomationSnapshot.CaptureCommandCommandsCompleted");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandCommandsFailed"), "AutomationSnapshot.CaptureCommandCommandsFailed");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandCommandsCanceled"), "AutomationSnapshot.CaptureCommandCommandsCanceled");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandCommandsCoalesced"), "AutomationSnapshot.CaptureCommandCommandsCoalesced");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandPendingCommands"), "AutomationSnapshot.CaptureCommandPendingCommands");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandMaxPendingCommands"), "AutomationSnapshot.CaptureCommandMaxPendingCommands");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandOldestPendingCommandAgeMs"), "AutomationSnapshot.CaptureCommandOldestPendingCommandAgeMs");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandLastQueueLatencyMs"), "AutomationSnapshot.CaptureCommandLastQueueLatencyMs");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandMaxQueueLatencyMs"), "AutomationSnapshot.CaptureCommandMaxQueueLatencyMs");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandLastCommand"), "AutomationSnapshot.CaptureCommandLastCommand");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandLastOutcome"), "AutomationSnapshot.CaptureCommandLastOutcome");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandLastCorrelationId"), "AutomationSnapshot.CaptureCommandLastCorrelationId");
        AssertNotNull(snapshotType.GetProperty("CaptureCommandLastError"), "AutomationSnapshot.CaptureCommandLastError");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoFramesSubmittedToEncoder"), "AutomationSnapshot.RecordingVideoFramesSubmittedToEncoder");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoEncoderPts"), "AutomationSnapshot.RecordingVideoEncoderPts");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoEncoderPacketsWritten"), "AutomationSnapshot.RecordingVideoEncoderPacketsWritten");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoEncoderDroppedFrames"), "AutomationSnapshot.RecordingVideoEncoderDroppedFrames");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoSequenceGaps"), "AutomationSnapshot.RecordingVideoSequenceGaps");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoQueueOldestFrameAgeMs"), "AutomationSnapshot.RecordingVideoQueueOldestFrameAgeMs");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoQueueLatencyP95Ms"), "AutomationSnapshot.RecordingVideoQueueLatencyP95Ms");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoQueueLatencyP99Ms"), "AutomationSnapshot.RecordingVideoQueueLatencyP99Ms");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoBackpressureWaitMs"), "AutomationSnapshot.RecordingVideoBackpressureWaitMs");
        AssertNotNull(snapshotType.GetProperty("RecordingVideoBackpressureEvents"), "AutomationSnapshot.RecordingVideoBackpressureEvents");
        AssertNotNull(snapshotType.GetProperty("FlashbackTotalBytesWritten"), "AutomationSnapshot.FlashbackTotalBytesWritten");
        AssertNotNull(snapshotType.GetProperty("FlashbackTempDriveFreeBytes"), "AutomationSnapshot.FlashbackTempDriveFreeBytes");
        AssertNotNull(snapshotType.GetProperty("FlashbackStartupCacheBudgetBytes"), "AutomationSnapshot.FlashbackStartupCacheBudgetBytes");
        AssertNotNull(snapshotType.GetProperty("FlashbackStartupCacheBytes"), "AutomationSnapshot.FlashbackStartupCacheBytes");
        AssertNotNull(snapshotType.GetProperty("FlashbackStartupCacheSessionCount"), "AutomationSnapshot.FlashbackStartupCacheSessionCount");
        AssertNotNull(snapshotType.GetProperty("FlashbackStartupCacheDeletedSessionCount"), "AutomationSnapshot.FlashbackStartupCacheDeletedSessionCount");
        AssertNotNull(snapshotType.GetProperty("FlashbackStartupCacheFreedBytes"), "AutomationSnapshot.FlashbackStartupCacheFreedBytes");
        AssertNotNull(snapshotType.GetProperty("FlashbackStartupCacheOverBudget"), "AutomationSnapshot.FlashbackStartupCacheOverBudget");
        AssertNotNull(snapshotType.GetProperty("FatalCleanupInProgress"), "AutomationSnapshot.FatalCleanupInProgress");
        AssertNotNull(snapshotType.GetProperty("FlashbackCleanupInProgress"), "AutomationSnapshot.FlashbackCleanupInProgress");
        AssertNotNull(snapshotType.GetProperty("FlashbackForceRotateActive"), "AutomationSnapshot.FlashbackForceRotateActive");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoFramesSubmittedToEncoder"), "AutomationSnapshot.FlashbackVideoFramesSubmittedToEncoder");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoEncoderPacketsWritten"), "AutomationSnapshot.FlashbackVideoEncoderPacketsWritten");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoSequenceGaps"), "AutomationSnapshot.FlashbackVideoSequenceGaps");
        AssertNotNull(snapshotType.GetProperty("FlashbackBackendSettingsStale"), "AutomationSnapshot.FlashbackBackendSettingsStale");
        AssertNotNull(snapshotType.GetProperty("FlashbackBackendSettingsStaleReason"), "AutomationSnapshot.FlashbackBackendSettingsStaleReason");
        AssertNotNull(snapshotType.GetProperty("FlashbackBackendActiveFormat"), "AutomationSnapshot.FlashbackBackendActiveFormat");
        AssertNotNull(snapshotType.GetProperty("FlashbackBackendRequestedFormat"), "AutomationSnapshot.FlashbackBackendRequestedFormat");
        AssertNotNull(snapshotType.GetProperty("FlashbackBackendActivePreset"), "AutomationSnapshot.FlashbackBackendActivePreset");
        AssertNotNull(snapshotType.GetProperty("FlashbackBackendRequestedPreset"), "AutomationSnapshot.FlashbackBackendRequestedPreset");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoQueueOldestFrameAgeMs"), "AutomationSnapshot.FlashbackVideoQueueOldestFrameAgeMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoQueueLatencyP95Ms"), "AutomationSnapshot.FlashbackVideoQueueLatencyP95Ms");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoQueueLatencyP99Ms"), "AutomationSnapshot.FlashbackVideoQueueLatencyP99Ms");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoBackpressureWaitMs"), "AutomationSnapshot.FlashbackVideoBackpressureWaitMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoBackpressureEvents"), "AutomationSnapshot.FlashbackVideoBackpressureEvents");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackThreadAlive"), "AutomationSnapshot.FlashbackPlaybackThreadAlive");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackDroppedFrames"), "AutomationSnapshot.FlashbackPlaybackDroppedFrames");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackAudioMasterDelayDoubles"), "AutomationSnapshot.FlashbackPlaybackAudioMasterDelayDoubles");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackAudioMasterDelayShrinks"), "AutomationSnapshot.FlashbackPlaybackAudioMasterDelayShrinks");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackAudioMasterFallbacks"), "AutomationSnapshot.FlashbackPlaybackAudioMasterFallbacks");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackSegmentSwitches"), "AutomationSnapshot.FlashbackPlaybackSegmentSwitches");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackFmp4Reopens"), "AutomationSnapshot.FlashbackPlaybackFmp4Reopens");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackWriteHeadWaits"), "AutomationSnapshot.FlashbackPlaybackWriteHeadWaits");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackNearLiveSnaps"), "AutomationSnapshot.FlashbackPlaybackNearLiveSnaps");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackDecodeErrorSnaps"), "AutomationSnapshot.FlashbackPlaybackDecodeErrorSnaps");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackSubmitFailures"), "AutomationSnapshot.FlashbackPlaybackSubmitFailures");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastDropUtcUnixMs"), "AutomationSnapshot.FlashbackPlaybackLastDropUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastDropReason"), "AutomationSnapshot.FlashbackPlaybackLastDropReason");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastSubmitFailureUtcUnixMs"), "AutomationSnapshot.FlashbackPlaybackLastSubmitFailureUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastSubmitFailure"), "AutomationSnapshot.FlashbackPlaybackLastSubmitFailure");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastSegmentSwitchUtcUnixMs"), "AutomationSnapshot.FlashbackPlaybackLastSegmentSwitchUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastFmp4ReopenUtcUnixMs"), "AutomationSnapshot.FlashbackPlaybackLastFmp4ReopenUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastWriteHeadWaitGapMs"), "AutomationSnapshot.FlashbackPlaybackLastWriteHeadWaitGapMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackCadenceSampleCount"), "AutomationSnapshot.FlashbackPlaybackCadenceSampleCount");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackP95FrameMs"), "AutomationSnapshot.FlashbackPlaybackP95FrameMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackP99FrameMs"), "AutomationSnapshot.FlashbackPlaybackP99FrameMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxFrameMs"), "AutomationSnapshot.FlashbackPlaybackMaxFrameMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackSlowFrames"), "AutomationSnapshot.FlashbackPlaybackSlowFrames");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackSlowFramePercent"), "AutomationSnapshot.FlashbackPlaybackSlowFramePercent");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackTargetFps"), "AutomationSnapshot.FlashbackPlaybackTargetFps");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackOnePercentLowFps"), "AutomationSnapshot.FlashbackPlaybackOnePercentLowFps");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackPtsCadenceMismatchCount"), "AutomationSnapshot.FlashbackPlaybackPtsCadenceMismatchCount");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastPtsCadenceDeltaMs"), "AutomationSnapshot.FlashbackPlaybackLastPtsCadenceDeltaMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastPtsCadenceExpectedMs"), "AutomationSnapshot.FlashbackPlaybackLastPtsCadenceExpectedMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackSeekForwardDecodeCapHits"), "AutomationSnapshot.FlashbackPlaybackSeekForwardDecodeCapHits");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastSeekHitForwardDecodeCap"), "AutomationSnapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackDecodeSampleCount"), "AutomationSnapshot.FlashbackPlaybackDecodeSampleCount");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackDecodeAvgMs"), "AutomationSnapshot.FlashbackPlaybackDecodeAvgMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackDecodeP95Ms"), "AutomationSnapshot.FlashbackPlaybackDecodeP95Ms");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackDecodeP99Ms"), "AutomationSnapshot.FlashbackPlaybackDecodeP99Ms");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackDecodeMaxMs"), "AutomationSnapshot.FlashbackPlaybackDecodeMaxMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxDecodePhase"), "AutomationSnapshot.FlashbackPlaybackMaxDecodePhase");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxDecodeReceiveMs"), "AutomationSnapshot.FlashbackPlaybackMaxDecodeReceiveMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxDecodeFeedMs"), "AutomationSnapshot.FlashbackPlaybackMaxDecodeFeedMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxDecodeReadMs"), "AutomationSnapshot.FlashbackPlaybackMaxDecodeReadMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxDecodeSendMs"), "AutomationSnapshot.FlashbackPlaybackMaxDecodeSendMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxDecodeAudioMs"), "AutomationSnapshot.FlashbackPlaybackMaxDecodeAudioMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxDecodeConvertMs"), "AutomationSnapshot.FlashbackPlaybackMaxDecodeConvertMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxDecodeUtcUnixMs"), "AutomationSnapshot.FlashbackPlaybackMaxDecodeUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxDecodePositionMs"), "AutomationSnapshot.FlashbackPlaybackMaxDecodePositionMs");
        AssertNotNull(snapshotType.GetProperty("CaptureCadenceP99IntervalMs"), "AutomationSnapshot.CaptureCadenceP99IntervalMs");
        AssertNotNull(snapshotType.GetProperty("CaptureCadenceOnePercentLowFps"), "AutomationSnapshot.CaptureCadenceOnePercentLowFps");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackCommandsEnqueued"), "AutomationSnapshot.FlashbackPlaybackCommandsEnqueued");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackCommandsProcessed"), "AutomationSnapshot.FlashbackPlaybackCommandsProcessed");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackCommandsDropped"), "AutomationSnapshot.FlashbackPlaybackCommandsDropped");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackCommandsSkippedNotReady"), "AutomationSnapshot.FlashbackPlaybackCommandsSkippedNotReady");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackScrubUpdatesCoalesced"), "AutomationSnapshot.FlashbackPlaybackScrubUpdatesCoalesced");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackSeekCommandsCoalesced"), "AutomationSnapshot.FlashbackPlaybackSeekCommandsCoalesced");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackCommandQueueCapacity"), "AutomationSnapshot.FlashbackPlaybackCommandQueueCapacity");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackPendingCommands"), "AutomationSnapshot.FlashbackPlaybackPendingCommands");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxPendingCommands"), "AutomationSnapshot.FlashbackPlaybackMaxPendingCommands");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastCommandQueueLatencyMs"), "AutomationSnapshot.FlashbackPlaybackLastCommandQueueLatencyMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxCommandQueueLatencyMs"), "AutomationSnapshot.FlashbackPlaybackMaxCommandQueueLatencyMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackMaxCommandQueueLatencyCommand"), "AutomationSnapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastCommandQueued"), "AutomationSnapshot.FlashbackPlaybackLastCommandQueued");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastCommandProcessed"), "AutomationSnapshot.FlashbackPlaybackLastCommandProcessed");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastCommandQueuedUtcUnixMs"), "AutomationSnapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastCommandProcessedUtcUnixMs"), "AutomationSnapshot.FlashbackPlaybackLastCommandProcessedUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastCommandFailureUtcUnixMs"), "AutomationSnapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackPlaybackLastCommandFailure"), "AutomationSnapshot.FlashbackPlaybackLastCommandFailure");
        AssertNotNull(snapshotType.GetProperty("FlashbackAudioQueueCapacity"), "AutomationSnapshot.FlashbackAudioQueueCapacity");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoQueueRejectedFrames"), "AutomationSnapshot.FlashbackVideoQueueRejectedFrames");
        AssertNotNull(snapshotType.GetProperty("FlashbackVideoQueueLastRejectReason"), "AutomationSnapshot.FlashbackVideoQueueLastRejectReason");
        AssertNotNull(snapshotType.GetProperty("FlashbackGpuQueueRejectedFrames"), "AutomationSnapshot.FlashbackGpuQueueRejectedFrames");
        AssertNotNull(snapshotType.GetProperty("FlashbackGpuQueueLastRejectReason"), "AutomationSnapshot.FlashbackGpuQueueLastRejectReason");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportActive"), "AutomationSnapshot.FlashbackExportActive");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportId"), "AutomationSnapshot.FlashbackExportId");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportStatus"), "AutomationSnapshot.FlashbackExportStatus");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportOutputPath"), "AutomationSnapshot.FlashbackExportOutputPath");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportStartedUtcUnixMs"), "AutomationSnapshot.FlashbackExportStartedUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportLastProgressUtcUnixMs"), "AutomationSnapshot.FlashbackExportLastProgressUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportCompletedUtcUnixMs"), "AutomationSnapshot.FlashbackExportCompletedUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportElapsedMs"), "AutomationSnapshot.FlashbackExportElapsedMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportLastProgressAgeMs"), "AutomationSnapshot.FlashbackExportLastProgressAgeMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportOutputBytes"), "AutomationSnapshot.FlashbackExportOutputBytes");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportThroughputBytesPerSec"), "AutomationSnapshot.FlashbackExportThroughputBytesPerSec");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportSegmentsProcessed"), "AutomationSnapshot.FlashbackExportSegmentsProcessed");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportTotalSegments"), "AutomationSnapshot.FlashbackExportTotalSegments");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportPercent"), "AutomationSnapshot.FlashbackExportPercent");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportInPointMs"), "AutomationSnapshot.FlashbackExportInPointMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportOutPointMs"), "AutomationSnapshot.FlashbackExportOutPointMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportMessage"), "AutomationSnapshot.FlashbackExportMessage");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportFailureKind"), "AutomationSnapshot.FlashbackExportFailureKind");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportForceRotateFallbacks"), "AutomationSnapshot.FlashbackExportForceRotateFallbacks");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportLastForceRotateFallbackUtcUnixMs"), "AutomationSnapshot.FlashbackExportLastForceRotateFallbackUtcUnixMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportLastForceRotateFallbackSegments"), "AutomationSnapshot.FlashbackExportLastForceRotateFallbackSegments");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportLastForceRotateFallbackInPointMs"), "AutomationSnapshot.FlashbackExportLastForceRotateFallbackInPointMs");
        AssertNotNull(snapshotType.GetProperty("FlashbackExportLastForceRotateFallbackOutPointMs"), "AutomationSnapshot.FlashbackExportLastForceRotateFallbackOutPointMs");
        AssertNotNull(snapshotType.GetProperty("LastExportId"), "AutomationSnapshot.LastExportId");
        AssertNotNull(snapshotType.GetProperty("MjpegPacketHashSampleCount"), "AutomationSnapshot.MjpegPacketHashSampleCount");
        AssertNotNull(snapshotType.GetProperty("MjpegPacketHashInputObservedFps"), "AutomationSnapshot.MjpegPacketHashInputObservedFps");
        AssertNotNull(snapshotType.GetProperty("MjpegPacketHashUniqueObservedFps"), "AutomationSnapshot.MjpegPacketHashUniqueObservedFps");
        AssertNotNull(snapshotType.GetProperty("MjpegPacketHashDuplicateFramePercent"), "AutomationSnapshot.MjpegPacketHashDuplicateFramePercent");
        AssertNotNull(snapshotType.GetProperty("MjpegPacketHashPattern"), "AutomationSnapshot.MjpegPacketHashPattern");
        AssertNotNull(snapshotType.GetProperty("MjpegPacketHashRecentDuplicateFlags"), "AutomationSnapshot.MjpegPacketHashRecentDuplicateFlags");
        AssertNotNull(snapshotType.GetProperty("VisualCadenceSampleCount"), "AutomationSnapshot.VisualCadenceSampleCount");
        AssertNotNull(snapshotType.GetProperty("VisualCadenceChangeObservedFps"), "AutomationSnapshot.VisualCadenceChangeObservedFps");
        AssertNotNull(snapshotType.GetProperty("VisualCadenceRepeatFramePercent"), "AutomationSnapshot.VisualCadenceRepeatFramePercent");
        AssertNotNull(snapshotType.GetProperty("VisualCadenceMotionConfidence"), "AutomationSnapshot.VisualCadenceMotionConfidence");
        AssertNotNull(snapshotType.GetProperty("VisualCadenceRecentChangeIntervalsMs"), "AutomationSnapshot.VisualCadenceRecentChangeIntervalsMs");
        AssertNotNull(snapshotType.GetProperty("VisualCenterCadenceSampleCount"), "AutomationSnapshot.VisualCenterCadenceSampleCount");
        AssertNotNull(snapshotType.GetProperty("VisualCenterCadenceChangeObservedFps"), "AutomationSnapshot.VisualCenterCadenceChangeObservedFps");
        AssertNotNull(snapshotType.GetProperty("VisualCenterCadenceRepeatFramePercent"), "AutomationSnapshot.VisualCenterCadenceRepeatFramePercent");
        AssertNotNull(snapshotType.GetProperty("VisualCenterCadenceMotionConfidence"), "AutomationSnapshot.VisualCenterCadenceMotionConfidence");
        AssertNotNull(snapshotType.GetProperty("VisualCenterCadenceRecentChangeIntervalsMs"), "AutomationSnapshot.VisualCenterCadenceRecentChangeIntervalsMs");

        var perDecoderProperty = snapshotType.GetProperty("MjpegPerDecoder")
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder missing.");
        var elementType = perDecoderProperty.PropertyType.GetElementType()
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder element type missing.");
        AssertEqual(decoderType, elementType, "AutomationSnapshot.MjpegPerDecoder[] element type");

        AssertNotNull(decoderType.GetProperty("WorkerIndex"), "MjpegDecoderAutomationSnapshot.WorkerIndex");
        AssertNotNull(decoderType.GetProperty("SampleCount"), "MjpegDecoderAutomationSnapshot.SampleCount");
        AssertNotNull(decoderType.GetProperty("AvgMs"), "MjpegDecoderAutomationSnapshot.AvgMs");
        AssertNotNull(decoderType.GetProperty("P95Ms"), "MjpegDecoderAutomationSnapshot.P95Ms");
        AssertNotNull(decoderType.GetProperty("MaxMs"), "MjpegDecoderAutomationSnapshot.MaxMs");

        return Task.CompletedTask;
    }

    private static Task AutomationOptionsSnapshot_ExposesAdvancedControlState()
    {
        var optionsType = RequireType("Sussudio.Models.AutomationOptionsSnapshot");
        var stringOptionType = RequireType("Sussudio.Models.AutomationStringOption");
        var intOptionType = RequireType("Sussudio.Models.AutomationIntOption");

        AssertNotNull(optionsType.GetProperty("Presets"), "AutomationOptionsSnapshot.Presets");
        AssertNotNull(optionsType.GetProperty("SplitEncodeModes"), "AutomationOptionsSnapshot.SplitEncodeModes");
        AssertNotNull(optionsType.GetProperty("VideoFormats"), "AutomationOptionsSnapshot.VideoFormats");
        AssertNotNull(optionsType.GetProperty("MjpegDecoderCounts"), "AutomationOptionsSnapshot.MjpegDecoderCounts");
        AssertNotNull(optionsType.GetProperty("SelectedPreset"), "AutomationOptionsSnapshot.SelectedPreset");
        AssertNotNull(optionsType.GetProperty("SelectedSplitEncodeMode"), "AutomationOptionsSnapshot.SelectedSplitEncodeMode");
        AssertNotNull(optionsType.GetProperty("SelectedVideoFormat"), "AutomationOptionsSnapshot.SelectedVideoFormat");
        AssertNotNull(optionsType.GetProperty("ShowAllCaptureOptions"), "AutomationOptionsSnapshot.ShowAllCaptureOptions");
        AssertNotNull(optionsType.GetProperty("PreviewVolumePercent"), "AutomationOptionsSnapshot.PreviewVolumePercent");
        AssertNotNull(optionsType.GetProperty("IsStatsVisible"), "AutomationOptionsSnapshot.IsStatsVisible");

        var presetsProperty = optionsType.GetProperty("Presets")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.Presets missing.");
        AssertEqual(stringOptionType, presetsProperty.PropertyType.GetElementType(), "AutomationOptionsSnapshot.Presets[] element type");

        var decoderCountsProperty = optionsType.GetProperty("MjpegDecoderCounts")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.MjpegDecoderCounts missing.");
        AssertEqual(intOptionType, decoderCountsProperty.PropertyType.GetElementType(), "AutomationOptionsSnapshot.MjpegDecoderCounts[] element type");

        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        AssertNotNull(snapshotType.GetProperty("SelectedVideoFormat"), "AutomationSnapshot.SelectedVideoFormat");
        AssertNotNull(snapshotType.GetProperty("ShowAllCaptureOptions"), "AutomationSnapshot.ShowAllCaptureOptions");
        AssertNotNull(snapshotType.GetProperty("PreviewVolumePercent"), "AutomationSnapshot.PreviewVolumePercent");
        AssertNotNull(snapshotType.GetProperty("IsStatsVisible"), "AutomationSnapshot.IsStatsVisible");

        return Task.CompletedTask;
    }

    private static Task FfmpegRuntimeLocator_PrefersAppLocalRuntimeFolder()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ec-ffmpeg-locator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var localFfmpegDir = Path.Combine(tempRoot, "ffmpeg");
        Directory.CreateDirectory(localFfmpegDir);

        try
        {
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "avcodec-62.dll"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "avutil-60.dll"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "ffmpeg.exe"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(localFfmpegDir, "ffprobe.exe"), Array.Empty<byte>());

            var locatorType = RequireType("Sussudio.Services.Runtime.FfmpegRuntimeLocator");
            var resolveRuntime = locatorType.GetMethod(
                                     "TryResolveNativeRuntimeRoot",
                                     BindingFlags.Static | BindingFlags.NonPublic,
                                     binder: null,
                                     types: new[] { typeof(string), typeof(string).MakeByRefType() },
                                     modifiers: null)
                                 ?? throw new InvalidOperationException("FfmpegRuntimeLocator.TryResolveNativeRuntimeRoot overload not found.");
            var runtimeArgs = new object?[] { tempRoot, null };
            var resolved = (bool)(resolveRuntime.Invoke(null, runtimeArgs)
                                  ?? throw new InvalidOperationException("FfmpegRuntimeLocator.TryResolveNativeRuntimeRoot returned null."));
            AssertEqual(true, resolved, "FfmpegRuntimeLocator.TryResolveNativeRuntimeRoot resolved");
            AssertEqual(localFfmpegDir, runtimeArgs[1]?.ToString(), "FfmpegRuntimeLocator native runtime root");

            var findToolPath = locatorType.GetMethod(
                                   "FindToolPath",
                                   BindingFlags.Static | BindingFlags.NonPublic,
                                   binder: null,
                                   types: new[] { typeof(string), typeof(string) },
                                   modifiers: null)
                               ?? throw new InvalidOperationException("FfmpegRuntimeLocator.FindToolPath overload not found.");
            var ffmpegPath = findToolPath.Invoke(null, new object?[] { "ffmpeg.exe", tempRoot })?.ToString()
                             ?? throw new InvalidOperationException("FfmpegRuntimeLocator.FindToolPath(ffmpeg.exe) returned null.");
            var ffprobePath = findToolPath.Invoke(null, new object?[] { "ffprobe.exe", tempRoot })?.ToString()
                              ?? throw new InvalidOperationException("FfmpegRuntimeLocator.FindToolPath(ffprobe.exe) returned null.");

            AssertEqual(Path.Combine(localFfmpegDir, "ffmpeg.exe"), ffmpegPath, "FfmpegRuntimeLocator ffmpeg.exe path");
            AssertEqual(Path.Combine(localFfmpegDir, "ffprobe.exe"), ffprobePath, "FfmpegRuntimeLocator ffprobe.exe path");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    private static Task SharedFormatter_RendersMjpegTimingSection_WhenFieldsExist()
    {
        var toolAssembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var formatterType = toolAssembly.GetType("Sussudio.Tools.AutomationSnapshotFormatter")
            ?? throw new InvalidOperationException("Sussudio.Tools.AutomationSnapshotFormatter type not found.");
        var formatSnapshot = formatterType.GetMethod("FormatSnapshot", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("AutomationSnapshotFormatter.FormatSnapshot not found.");

        const string json = """
                            {"Snapshot":{"SessionState":"Ready","StatusText":"Idle","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":true,"IsRecording":false,"SelectedResolution":"3840x2160","SelectedFrameRate":120,"SelectedRecordingFormat":"HEVC","SelectedQuality":"High","SelectedPreset":"P5","SelectedSplitEncodeMode":"Auto","SelectedVideoFormat":"MJPG","ShowAllCaptureOptions":true,"PreviewVolumePercent":42.5,"IsStatsVisible":true,"IsHdrEnabled":false,"IsHdrAvailable":true,"HdrOutputActive":false,"HdrRuntimeState":"Inactive","RequestedPipelineMode":"SDR","ActivePipelineMode":"SDR","PipelineModeMatched":true,"IsAudioEnabled":true,"IsAudioPreviewEnabled":false,"IsCustomAudioInputEnabled":false,"AudioPeak":0,"AudioClipping":false,"AudioSignalPresent":false,"AudioReaderActive":false,"AudioFramesArrived":0,"AudioFramesWrittenToSink":0,"VideoReaderActive":true,"IngestVideoFramesArrived":120,"IngestVideoFramesWrittenToSink":120,"EncoderVideoFramesEnqueued":0,"EncoderVideoFramesEncoded":0,"FfmpegVideoQueueDepth":0,"VideoDropsQueueSaturated":0,"IngestLastVideoFrameAgeMs":5,"EncoderLastEnqueueAgeMs":0,"EncoderLastWriteAgeMs":0,"MemoryPreference":"Gpu","VideoRequestedSubtype":"MJPG","VideoNegotiatedSubtype":"MJPG","VideoIngestErrorCount":0,"SourceReaderReadOutstanding":false,"SourceReaderReadOutstandingMs":0,"SourceReaderLastFrameTickMs":0,"SourceReaderFrameChannelDepth":0,"WasapiCaptureCallbackCount":0,"WasapiCaptureCallbackAvgIntervalMs":0,"WasapiCaptureCallbackMaxIntervalMs":0,"WasapiCaptureCallbackSilenceCount":0,"WasapiCaptureLastCallbackTickMs":0,"WasapiCaptureAudioLevelEventsFired":0,"WasapiPlaybackRenderCallbackCount":0,"WasapiPlaybackRenderSilenceCount":0,"WasapiPlaybackQueueDepth":0,"WasapiPlaybackQueueDropCount":0,"WasapiPlaybackLastRenderTickMs":0,"OutputPath":"","RecordingTime":"00:00:00","RecordingSizeInfo":"0 B","RecordingBitrateInfo":"0 Mbps","RecordingBackend":"None","AudioPathMode":"None","MuxResult":"NotAttempted","LastOutputPath":"","LastOutputSizeBytes":0,"LastFinalizeStatus":"None","PerformanceScore":100,"PerformancePerfectionMet":true,"PerformanceSummary":"OK","EstimatedPipelineLatencyMs":1,"CaptureCadenceObservedFps":120,"ExpectedCaptureFrameRate":120,"CaptureCadenceSampleCount":300,"CaptureCadenceAverageIntervalMs":8.3,"CaptureCadenceP95IntervalMs":8.5,"CaptureCadenceMaxIntervalMs":9.0,"CaptureCadenceJitterStdDevMs":0.1,"CaptureCadenceSevereGapCount":0,"CaptureCadenceEstimatedDroppedFrames":0,"CaptureCadenceEstimatedDropPercent":0,"MjpegDecodeSampleCount":300,"MjpegDecodeAvgMs":2.1,"MjpegDecodeP95Ms":3.4,"MjpegDecodeMaxMs":5.6,"MjpegInteropCopySampleCount":300,"MjpegInteropCopyAvgMs":0.9,"MjpegInteropCopyP95Ms":1.4,"MjpegInteropCopyMaxMs":2.2,"MjpegCallbackSampleCount":300,"MjpegCallbackAvgMs":4.5,"MjpegCallbackP95Ms":6.7,"MjpegCallbackMaxMs":9.1,"MjpegDecoderCount":2,"MjpegReorderSampleCount":300,"MjpegReorderAvgMs":0.4,"MjpegReorderP95Ms":0.8,"MjpegReorderMaxMs":1.2,"MjpegPipelineSampleCount":300,"MjpegPipelineAvgMs":5.1,"MjpegPipelineP95Ms":7.0,"MjpegPipelineMaxMs":9.4,"MjpegTotalDecoded":301,"MjpegTotalEmitted":300,"MjpegTotalDropped":1,"MjpegReorderSkips":2,"MjpegReorderBufferDepth":1,"MjpegPerDecoder":[{"WorkerIndex":0,"SampleCount":150,"AvgMs":2.0,"P95Ms":3.0,"MaxMs":4.0},{"WorkerIndex":1,"SampleCount":151,"AvgMs":2.2,"P95Ms":3.2,"MaxMs":4.2}],"PreviewRendererMode":"D3D11VideoProcessor","PreviewStartupState":"Rendering","PreviewFirstVisualConfirmed":true,"PreviewD3DFramesSubmitted":120,"PreviewD3DFramesRendered":120,"PreviewD3DFramesDropped":0,"PreviewD3DInputColorSpace":"BT.709","PreviewD3DOutputColorSpace":"sRGB","PreviewCadenceObservedFps":120,"PreviewPacingLikelySlowStage":"MjpegDecode","PreviewPacingSlowStageConfidence":"Medium","PreviewPacingSlowStageEvidence":"decode p95 over budget","DetectedSourceFrameRate":120,"SourceWidth":3840,"SourceHeight":2160,"SourceIsHdr":false,"SourceTelemetryAvailability":"Available","SourceTelemetryConfidence":"High"}}
                            """;
        using var document = JsonDocument.Parse(json);
        var output = formatSnapshot.Invoke(null, new object[] { document.RootElement, false })?.ToString()
            ?? throw new InvalidOperationException("AutomationSnapshotFormatter.FormatSnapshot returned null.");

        AssertContains(output, "== MJPEG Pipeline Timing ==");
        AssertContains(output, "Preset: P5");
        AssertContains(output, "Video Format: MJPG | Split Encode: Auto | MJPEG Decoders: 2");
        AssertContains(output, "UI: Show All Options=true | Preview Volume=42.5% | Stats Visible=true");
        AssertContains(output, "Decode: avg=2.1ms");
        AssertContains(output, "Interop Copy: avg=0.9ms");
        AssertContains(output, "Total Callback: avg=4.5ms");
        AssertContains(output, "Decoders: 2 | Decoded=301 Emitted=300 Dropped=1");
        AssertContains(output, "Reorder: avg=0.4ms");
        AssertContains(output, "Pipeline: avg=5.1ms");
        AssertContains(output, "== Diagnostics ==");
        AssertContains(output, "Legacy Score:");
        AssertContains(output, "Pacing Classifier: stage=MjpegDecode confidence=Medium evidence=decode p95 over budget");
        AssertContains(output, "Frame Time:");
        AssertContains(output, "Average Rate:");
        AssertContains(output, "Decoder[0]: avg=2.0ms");
        AssertContains(output, "Decoder[1]: avg=2.2ms");
        return Task.CompletedTask;
    }

    private static Task AutomationCommandMaps_StayAligned_ForAdvancedMcpControls()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        var protocolText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs");
        var scriptText = ReadRepoFile("tools/send-automation-command.ps1");
        var resolveCommand = protocolType.GetMethod(
            "ResolveCommand",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.ResolveCommand not found.");

        foreach (var (name, ordinal) in new[]
        {
            ("GetCaptureOptions", 29),
            ("SetPreset", 30),
            ("SetSplitEncodeMode", 31),
            ("SetMjpegDecoderCount", 32),
            ("SetShowAllCaptureOptions", 33),
            ("SetPreviewVolume", 34),
            ("SetStatsVisible", 35)
        })
        {
            AssertEqual(ordinal, Convert.ToInt32(Enum.Parse(enumType, name)), $"AutomationCommandKind.{name}");
            AssertEqual(ordinal, Convert.ToInt32(resolveCommand.Invoke(null, new object?[] { name })), $"AutomationPipeProtocol.ResolveCommand({name})");
        }

        AssertContains(protocolText, "Enum.GetValues<AutomationCommandKind>()");

        AssertContains(scriptText, "AutomationClient\\AutomationClient.csproj");
        AssertContains(scriptText, "Get-AutomationClientInputWriteTimeUtc");
        AssertContains(scriptText, "Test-AutomationClientBuildFresh");
        AssertContains(scriptText, "AutomationClient build failed with exit code $LASTEXITCODE.");
        AssertContains(scriptText, "AutomationClient build output is stale after rebuild");
        AssertContains(scriptText, "$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"");
        AssertContains(scriptText, "\"--command\", $Command");
        AssertContains(scriptText, "$payloadBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($PayloadJson))");
        AssertContains(scriptText, "\"--payload-base64\", $payloadBase64");
        AssertContains(scriptText, "[int]$ResponseTimeoutMs = 0");
        AssertContains(scriptText, "\"--response-timeout-ms\", $ResponseTimeoutMs");
        AssertDoesNotContain(scriptText, "function Resolve-AutomationCommand");

        return Task.CompletedTask;
    }

    private static Task McpToolSurface_KeepsCaptureOptionsSeparateFromRawState()
    {
        var captureSettingsToolsText = ReadRepoFile("tools/McpServer/Tools/CaptureSettingsTools.cs");
        var appStateToolText = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        var captureOptionsToolText = ReadRepoFile("tools/McpServer/Tools/CaptureOptionsTools.cs");
        var uiSettingsToolText = ReadRepoFile("tools/McpServer/Tools/UiSettingsTools.cs");
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertContains(captureSettingsToolsText, "string? preset = null");
        AssertContains(captureSettingsToolsText, "string? splitEncodeMode = null");
        AssertContains(captureSettingsToolsText, "int? mjpegDecoderCount = null");
        AssertContains(captureSettingsToolsText, "\"SetPreset\"");
        AssertContains(captureSettingsToolsText, "\"SetSplitEncodeMode\"");
        AssertContains(captureSettingsToolsText, "\"SetMjpegDecoderCount\"");

        AssertContains(appStateToolText, "get_app_state_raw");
        AssertContains(appStateToolText, "UseStructuredContent = true");
        AssertDoesNotContain(appStateToolText, "SendCommandAsync(\"GetCaptureOptions\")");
        AssertContains(captureOptionsToolText, "get_capture_options");
        AssertContains(captureOptionsToolText, "\"GetCaptureOptions\"");
        AssertContains(captureOptionsToolText, "UseStructuredContent = true");
        AssertContains(uiSettingsToolText, "configure_ui");
        AssertContains(uiSettingsToolText, "\"SetShowAllCaptureOptions\"");
        AssertContains(uiSettingsToolText, "\"SetPreviewVolume\"");
        AssertContains(uiSettingsToolText, "\"SetStatsVisible\"");
        if (snapshotType.GetProperty("Options") != null)
        {
            throw new InvalidOperationException("AutomationSnapshot.Options should not be present when capture options are a separate surface.");
        }

        return Task.CompletedTask;
    }

    private static Task UiAutomationCommands_AreNotBlockedOnDeviceReadiness()
    {
        var dispatcherText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs");

        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetShowAllCaptureOptions => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetPreviewVolume => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetStatsVisible => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.GetCaptureOptions => true,");

        return Task.CompletedTask;
    }

    private static Task AutomationPreviewVolume_PersistsThroughSettingsPath()
    {
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationUi.cs").Replace("\r\n", "\n");
        AssertContains(automationUiText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(automationUiText, "public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }");
        AssertContains(automationUiText, "public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        return Task.CompletedTask;
    }

    private static Task AutomationUiSettings_PersistThroughSettingsPath()
    {
        var settingsPartialText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Settings.cs").Replace("\r\n", "\n");
        var settingsServiceText = ReadRepoFile("Sussudio/Services/Runtime/SettingsService.cs").Replace("\r\n", "\n");

        AssertContains(settingsServiceText, "public bool? ShowAllCaptureOptions { get; set; }");
        AssertContains(settingsServiceText, "public bool? IsStatsVisible { get; set; }");
        AssertContains(settingsPartialText, "if (settings.ShowAllCaptureOptions.HasValue)");
        AssertContains(settingsPartialText, "if (settings.IsStatsVisible.HasValue)");
        AssertContains(settingsPartialText, "ShowAllCaptureOptions = ShowAllCaptureOptions,");
        AssertContains(settingsPartialText, "IsStatsVisible = IsStatsVisible,");
        AssertContains(settingsPartialText, "partial void OnIsStatsVisibleChanged(bool value)");
        AssertContains(settingsPartialText, "partial void OnShowAllCaptureOptionsChanged(bool value)");
        AssertContains(settingsPartialText, "RebuildResolutionOptions();\n        SaveSettings();");

        return Task.CompletedTask;
    }

    private static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs").Replace("\r\n", "\n");
        var captureModeAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCaptureMode.cs").Replace("\r\n", "\n");

        AssertContains(viewModelText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(captureModeAutomationText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureModeAutomationText, "await _automationCaptureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureModeAutomationText, "_suppressFormatChangeReinitialize = true;");
        AssertContains(captureModeAutomationText, "_suppressFormatChangeReinitialize = false;");
        AssertContains(captureModeAutomationText, "return wasPreviewing && SelectedFormat != null;");
        AssertContains(captureModeAutomationText, "ReinitializeDeviceAsync($\"automation {reason}\")");
        AssertContains(captureModeAutomationText, "_automationCaptureModeGate.Release();");
        AssertContains(captureModeAutomationText, "return SetAutomationCaptureModeAsync(\"resolution\"");
        AssertContains(captureModeAutomationText, "return SetAutomationCaptureModeAsync(\"frame rate\"");
        AssertContains(captureModeAutomationText, "return SetAutomationCaptureModeAsync(\"video format\"");
        AssertContains(captureModeAutomationText, "return SetAutomationCaptureModeAsync(\"mjpeg decoder count\"");
        AssertDoesNotContain(automationText, "private async Task SetAutomationCaptureModeAsync(");

        return Task.CompletedTask;
    }

    private static Task AutomationDeviceSelection_RoutesThroughApplyReinit()
    {
        var automationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs").Replace("\r\n", "\n");
        var selectDevice = ExtractTextBetween(
            automationText,
            "public Task SelectDeviceAsync",
            "public Task SelectAudioInputDeviceAsync");

        AssertContains(selectDevice, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(selectDevice, "await ApplySelectedDeviceAsync(target, cancellationToken).ConfigureAwait(true);");
        AssertDoesNotContain(selectDevice, "SelectedDevice = target;");

        return Task.CompletedTask;
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
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");

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
        var runtimeContractsText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var runtimeSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs").Replace("\r\n", "\n");
        var dispatcherText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs").Replace("\r\n", "\n");
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

        AssertContains(playbackText, "UpdateOutputLevel(destinationSpan);");
        AssertContains(playbackText, "public float TargetVolume => _targetVolume;");
        AssertContains(playbackText, "public float CurrentVolume => _currentVolume;");
        AssertContains(playbackText, "public float LastOutputPeak => _lastOutputPeak;");
        AssertContains(playbackText, "public float LastOutputRms => _lastOutputRms;");

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
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");

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

    private static Task SettingsShelfLifecycle_LivesInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var fullScreenText = ReadRepoFile("Sussudio/MainWindow.FullScreen.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var settingsShelfText = ReadRepoFile("Sussudio/MainWindow.SettingsShelf.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/SettingsShelfController.cs").Replace("\r\n", "\n");

        AssertContains(settingsShelfText, "private SettingsShelfController _settingsShelfController = null!;");
        AssertContains(settingsShelfText, "private void InitializeSettingsShelfController()");
        AssertContains(settingsShelfText, "=> _settingsShelfController.Toggle();");
        AssertContains(settingsShelfText, "=> _settingsShelfController.ApplyVisibility(visible);");
        AssertContains(settingsShelfText, "=> _settingsShelfController.ResetAnimationState();");
        AssertContains(mainWindowText, "InitializeSettingsShelfController();");
        AssertContains(fullScreenText, "ResetSettingsShelfAnimation = ResetSettingsShelfAnimationForFullScreen,");
        AssertContains(controllerText, "internal sealed class SettingsShelfController");
        AssertContains(controllerText, "private bool _isAnimating;");
        AssertContains(controllerText, "public bool IsAnimating => _isAnimating;");
        AssertContains(controllerText, "public void Toggle()");
        AssertContains(controllerText, "public void ApplyVisibility(bool visible)");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.UpdateLayout();");
        AssertContains(controllerText, "EnableDependentAnimation = true");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.Visibility = Visibility.Collapsed;");
        AssertDoesNotContain(mainWindowText, "private bool _isSettingsShelfAnimating;");
        AssertDoesNotContain(animationsText, "private void AnimateSettingsShelf(");
        AssertDoesNotContain(eventHandlersText, "private void SettingsToggleButton_Click(");

        return Task.CompletedTask;
    }

    private static Task SplashLoadingPhrases_LiveInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var launchEntranceControllerText = ReadRepoFile("Sussudio/Controllers/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var splashAdapterText = ReadRepoFile("Sussudio/MainWindow.SplashLoading.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/SplashLoadingPhraseController.cs").Replace("\r\n", "\n");

        AssertContains(splashAdapterText, "private SplashLoadingPhraseController _splashLoadingPhraseController = null!;");
        AssertContains(splashAdapterText, "private void InitializeSplashLoadingPhraseController()");
        AssertContains(splashAdapterText, "SplashLoadingTextA = SplashLoadingTextA,");
        AssertContains(splashAdapterText, "SplashLoadingTransformB = SplashLoadingTransformB,");
        AssertContains(splashAdapterText, "=> _splashLoadingPhraseController.Start();");
        AssertContains(splashAdapterText, "=> _splashLoadingPhraseController.Stop();");
        AssertContains(mainWindowText, "InitializeSplashLoadingPhraseController();");
        AssertContains(launchEntranceControllerText, "_context.StartSplashLoadingPhrases();");
        AssertContains(launchEntranceControllerText, "_context.StopSplashLoadingPhrases();");
        AssertContains(controllerText, "internal sealed class SplashLoadingPhraseController");
        AssertContains(controllerText, "private static readonly string[] DefaultSplashLoadingPhrases");
        AssertContains(controllerText, "private DispatcherTimer? _splashPhraseTimer;");
        AssertContains(controllerText, "private static string[] LoadSplashPhrases()");
        AssertContains(controllerText, "private TimeSpan NextSplashPhraseInterval()");
        AssertContains(controllerText, "private void CyclePhrase()");
        AssertContains(controllerText, "Path.Combine(AppContext.BaseDirectory, \"SplashPhrases.md\")");
        AssertContains(controllerText, "storyboard.Begin();");
        AssertDoesNotContain(animationsText, "private DispatcherTimer? _splashPhraseTimer;");
        AssertDoesNotContain(animationsText, "private static string[] LoadSplashPhrases()");
        AssertDoesNotContain(animationsText, "private void CycleSplashPhrase()");

        return Task.CompletedTask;
    }

    private static Task LaunchEntranceAnimation_LivesInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/MainWindow.Startup.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.LaunchEntrance.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private LaunchEntranceAnimationController _launchEntranceAnimationController = null!;");
        AssertContains(adapterText, "private void InitializeLaunchEntranceAnimationController()");
        AssertContains(adapterText, "SplashContent = SplashContent,");
        AssertContains(adapterText, "PreviewBorder = PreviewBorder,");
        AssertContains(adapterText, "PreviewBorderScale = PreviewBorderScale,");
        AssertContains(adapterText, "GetEntranceButtons = GetEntranceButtons,");
        AssertContains(adapterText, "IsPreviewFirstVisualConfirmed = () => _previewFirstVisualConfirmed,");
        AssertContains(adapterText, "FadeInControlBarShadow = () => FadeInShadow(_controlBarShadowVisual, delayMs: 400, durationMs: 500),");
        AssertContains(adapterText, "=> _launchEntranceAnimationController.PrepareInitialState();");
        AssertContains(adapterText, "=> _launchEntranceAnimationController.PlaySplashAndEntrance();");
        AssertContains(mainWindowText, "InitializeLaunchEntranceAnimationController();");
        AssertContains(mainWindowText, "PrepareLaunchEntranceInitialState();");
        AssertContains(startupText, "PlaySplashAndEntrance();");
        AssertContains(controllerText, "internal sealed class LaunchEntranceAnimationController");
        AssertContains(controllerText, "private bool _played;");
        AssertContains(controllerText, "private Storyboard? _activeStoryboard;");
        AssertContains(controllerText, "public void PrepareInitialState()");
        AssertContains(controllerText, "_context.ControlBarBorder.RenderTransform = new TranslateTransform { Y = 16 };");
        AssertContains(controllerText, "_context.PreviewBorderScale.ScaleX = 0.97;");
        AssertContains(controllerText, "foreach (var button in _context.GetEntranceButtons())");
        AssertContains(controllerText, "public void PlaySplashAndEntrance()");
        AssertContains(controllerText, "private void PlayEntranceAnimation()");
        AssertContains(controllerText, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertContains(controllerText, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertDoesNotContain(mainWindowText, "private bool _entranceAnimationPlayed;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _entranceStoryboard;");
        AssertDoesNotContain(mainWindowText, "ControlBarBorder.Opacity = 0;");
        AssertDoesNotContain(mainWindowText, "var entranceButtons = GetEntranceButtons();");
        AssertDoesNotContain(animationsText, "private void PlaySplashAndEntrance()");
        AssertDoesNotContain(animationsText, "private void PlayEntranceAnimation()");

        return Task.CompletedTask;
    }

    private static Task MainWindowStartupHosting_LivesInStartupPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/MainWindow.Startup.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(startupText, "private int _automationServicesStarted;");
        AssertContains(startupText, "private void MainWindow_Loaded(object sender, RoutedEventArgs e)");
        AssertContains(startupText, "Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += uncloakOnFirstFrame;");
        AssertContains(startupText, "DwmSetWindowAttribute(_hwnd, DWMWA_CLOAK, ref cloakFalse, sizeof(int));");
        AssertContains(startupText, "await ViewModel.InitializeAsync();");
        AssertContains(startupText, "PrimePreviewAudioFadeIn();");
        AssertContains(startupText, "await ViewModel.RefreshDevicesAsync();");
        AssertContains(startupText, "RevealPreviewUnavailablePlaceholder();");
        AssertContains(startupText, "StartAutomationServices();");
        AssertContains(startupText, "PlaySplashAndEntrance();");
        AssertContains(startupText, "private void StartAutomationServices()");
        AssertContains(startupText, "_automationDiagnosticsHub.Start();");
        AssertContains(startupText, "Automation control ready on pipe");
        AssertContains(startupText, "Automation control disabled on pipe");
        AssertContains(mainWindowText, "mainContent.Loaded += MainWindow_Loaded;");
        AssertDoesNotContain(mainWindowText, "private int _automationServicesStarted;");
        AssertDoesNotContain(closeLifecycleText, "private void MainWindow_Loaded(");
        AssertDoesNotContain(closeLifecycleText, "private void StartAutomationServices()");
        AssertDoesNotContain(closeLifecycleText, "_automationServicesStarted");

        return Task.CompletedTask;
    }

    private static Task MainWindowShellResizeTelemetry_LivesInSizingPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var windowSizingText = ReadRepoFile("Sussudio/MainWindow.WindowSizing.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs").Replace("\r\n", "\n");

        AssertContains(windowSizingText, "private long _previewLastResizeLogTick;");
        AssertContains(windowSizingText, "private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)");
        AssertContains(windowSizingText, "if (!ViewModel.IsPreviewing ||");
        AssertContains(windowSizingText, "_d3dRenderer == null ||");
        AssertContains(windowSizingText, "PreviewSwapChainPanel.Visibility != Visibility.Visible");
        AssertContains(windowSizingText, "Interlocked.Read(ref _previewLastResizeLogTick)");
        AssertContains(windowSizingText, "Interlocked.CompareExchange(ref _previewLastResizeLogTick, nowTick, lastLogTick)");
        AssertContains(windowSizingText, "Preview resize active. Updating compositor transform without resizing swap-chain buffers.");
        AssertContains(mainWindowText, "mainContent.SizeChanged += MainWindow_SizeChanged;");
        AssertContains(shutdownCleanupText, "mainContent.SizeChanged -= MainWindow_SizeChanged;");
        AssertContains(previewRendererText, "_previewLastResizeLogTick = 0;");
        AssertDoesNotContain(mainWindowText, "private long _previewLastResizeLogTick;");
        AssertDoesNotContain(closeLifecycleText, "private void MainWindow_SizeChanged(");
        AssertDoesNotContain(closeLifecycleText, "_previewLastResizeLogTick");

        return Task.CompletedTask;
    }

    private static Task PreviewRendererRuntimeState_LivesInRendererPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs").Replace("\r\n", "\n");
        var previewSurfaceText = ReadRepoFile("Sussudio/MainWindow.PreviewSurface.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotText = ReadRepoFile("Sussudio/MainWindow.PreviewRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");

        AssertContains(previewRendererText, "private SoftwareBitmapSource? _previewSource;");
        AssertContains(previewRendererText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertContains(previewSurfaceText, "Preview surface presentation");
        AssertContains(previewSurfaceText, "private SpriteVisual? _videoShadowVisual;");
        AssertContains(previewSurfaceText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertContains(previewSurfaceText, "private void UpdateVideoContentOverlays()");
        AssertContains(previewSurfaceText, "private void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceText, "private void SetupControlBarShadow()");
        AssertContains(previewRendererText, "private long _previewFramesArrived;");
        AssertContains(previewRendererText, "private long _previewFramesDisplayed;");
        AssertContains(previewRendererText, "private long _previewFramesDropped;");
        AssertContains(previewRendererText, "private long _previewLastPresentedTick;");
        AssertContains(previewRendererText, "private long _lastRendererStopTick;");
        AssertContains(previewRendererText, "private long _rendererReinitUnsafeWindows;");
        AssertContains(previewRendererText, "private double _previewMinPresentationIntervalMs;");
        AssertContains(previewRendererText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertContains(previewRendererText, "private double ResolvePreviewExpectedIntervalMs()");
        AssertContains(previewRuntimeSnapshotText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewRuntimeSnapshotText, "return GetPreviewRuntimeSnapshot();");
        AssertContains(previewRuntimeSnapshotText, "completion.TrySetResult(GetPreviewRuntimeSnapshot());");
        AssertContains(previewRuntimeSnapshotText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertContains(previewRuntimeSnapshotText, "var d3d = _d3dRenderer;");
        AssertContains(previewRuntimeSnapshotText, "return new PreviewRuntimeSnapshot");
        AssertContains(previewRendererText, "var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;");
        AssertContains(previewRendererText, "return Math.Max(1.0, 1000.0 / sourceFps);");
        AssertContains(previewRendererText, "_previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();");
        AssertContains(statsOverlayText, "GetPresentCadenceMetrics(_previewMinPresentationIntervalMs)");
        AssertDoesNotContain(mainWindowText, "private SoftwareBitmapSource? _previewSource;");
        AssertDoesNotContain(mainWindowText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private long _previewFramesArrived;");
        AssertDoesNotContain(mainWindowText, "private long _previewFramesDisplayed;");
        AssertDoesNotContain(mainWindowText, "private long _previewFramesDropped;");
        AssertDoesNotContain(mainWindowText, "private long _previewLastPresentedTick;");
        AssertDoesNotContain(mainWindowText, "private long _lastRendererStopTick;");
        AssertDoesNotContain(mainWindowText, "private long _rendererReinitUnsafeWindows;");
        AssertDoesNotContain(mainWindowText, "private double _previewMinPresentationIntervalMs;");
        AssertDoesNotContain(mainWindowText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertDoesNotContain(mainWindowText, "private double ResolvePreviewExpectedIntervalMs()");
        AssertDoesNotContain(mainWindowText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync");
        AssertDoesNotContain(previewRendererText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync");
        AssertDoesNotContain(previewRendererText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertDoesNotContain(mainWindowText, "private static bool IsHdrSubtype");

        return Task.CompletedTask;
    }

    private static Task MainWindowTitlePresentation_LivesInTitlePartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var titleText = ReadRepoFile("Sussudio/MainWindow.WindowTitle.cs").Replace("\r\n", "\n");

        AssertContains(titleText, "private readonly string _windowTitleBase;");
        AssertContains(titleText, "private static string BuildWindowTitleBase()");
        AssertContains(titleText, "Environment.ProcessPath");
        AssertContains(titleText, "File.GetLastWriteTime(exePath)");
        AssertContains(titleText, "CultureInfo.InvariantCulture");
        AssertContains(titleText, "private void ApplyWindowTitle()");
        AssertContains(titleText, "Title = $\"{_windowTitleBase} - REC {ViewModel.RecordingTime}\";");
        AssertContains(mainWindowText, "_windowTitleBase = BuildWindowTitleBase();");
        AssertContains(mainWindowText, "ApplyWindowTitle();");
        AssertContains(propertyChangedText, "ApplyWindowTitle();");
        AssertDoesNotContain(mainWindowText, "private static string BuildWindowTitleBase()");
        AssertDoesNotContain(mainWindowText, "private void ApplyWindowTitle()");
        AssertDoesNotContain(mainWindowText, "CultureInfo.InvariantCulture");

        return Task.CompletedTask;
    }

    private static Task MainWindowCloseLifecycleAndNativeHelpers_AreSplit()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var nativeWindowText = ReadRepoFile("Sussudio/MainWindow.NativeWindow.cs").Replace("\r\n", "\n");
        var oldWindowManagementPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Sussudio",
            "MainWindow.WindowManagement.cs");

        if (File.Exists(oldWindowManagementPath))
        {
            throw new InvalidOperationException("MainWindow.WindowManagement.cs should not return as a catch-all partial.");
        }

        AssertContains(closeLifecycleText, "private int _windowCloseRequested;");
        AssertContains(closeLifecycleText, "private int _windowCloseCleanupStarted;");
        AssertContains(closeLifecycleText, "private TaskCompletionSource<object?>? _windowCloseCompletion;");
        AssertContains(closeLifecycleText, "private bool _isWindowClosing;");
        AssertContains(closeLifecycleText, "private async void MainWindow_Closing(");
        AssertContains(closeLifecycleText, "private async Task<bool> TryStopRecordingBeforeCloseAsync()");
        AssertContains(shutdownCleanupText, "Post-close shutdown cleanup");
        AssertContains(shutdownCleanupText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");
        AssertContains(closeLifecycleText, "public Task CloseAsync(CancellationToken cancellationToken = default)");
        AssertContains(closeLifecycleText, "private Task GetWindowCloseCompletionTask(CancellationToken cancellationToken)");
        AssertContains(closeLifecycleText, "private void RequestWindowClose()");
        AssertContains(closeLifecycleText, "private static bool IsCloseAlreadyInProgressException(Exception ex)");
        AssertContains(shutdownCleanupText, "StopLiveSignalInfoTimers();");
        AssertContains(shutdownCleanupText, "StopMicMeterRowAnimation();");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(nativeWindowText, "private Microsoft.UI.Windowing.AppWindow GetAppWindow()");
        AssertContains(nativeWindowText, "private static extern int DwmSetWindowAttribute(");
        AssertContains(nativeWindowText, "private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;");
        AssertContains(nativeWindowText, "private const int DWMWA_CLOAK = 13;");
        AssertContains(mainWindowText, "appWindow.Closing += MainWindow_Closing;");
        AssertContains(mainWindowText, "Closed += MainWindow_Closed;");
        AssertDoesNotContain(mainWindowText, "private int _windowCloseRequested;");
        AssertDoesNotContain(mainWindowText, "private bool _isWindowClosing;");
        AssertDoesNotContain(closeLifecycleText, "private Microsoft.UI.Windowing.AppWindow GetAppWindow()");
        AssertDoesNotContain(closeLifecycleText, "DwmSetWindowAttribute(");
        AssertDoesNotContain(closeLifecycleText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");

        return Task.CompletedTask;
    }

    private static Task ControlBarHoverAnimations_LiveInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var launchEntranceControllerText = ReadRepoFile("Sussudio/Controllers/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ControlBarAnimations.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/ControlBarAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ControlBarAnimationController _controlBarAnimationController = null!;");
        AssertContains(adapterText, "private void InitializeControlBarAnimationController()");
        AssertContains(adapterText, "SettingsToggleButton,");
        AssertContains(adapterText, "FrameTimeOverlayToggle,");
        AssertContains(adapterText, "=> _controlBarAnimationController.AttachHoverAnimations();");
        AssertContains(adapterText, "=> _controlBarAnimationController.EntranceButtons;");
        AssertContains(mainWindowText, "InitializeControlBarAnimationController();");
        AssertContains(mainWindowText, "SetupButtonHoverAnimations();");
        AssertContains(launchEntranceControllerText, "var buttons = _context.GetEntranceButtons();");
        AssertContains(controllerText, "internal sealed class ControlBarAnimationController");
        AssertContains(controllerText, "public IReadOnlyList<FrameworkElement> EntranceButtons");
        AssertContains(controllerText, "public void AttachHoverAnimations()");
        AssertContains(controllerText, "private static void AnimateScale(");
        AssertDoesNotContain(animationsText, "private FrameworkElement[] GetControlBarButtons()");
        AssertDoesNotContain(animationsText, "private void SetupButtonHoverAnimations()");
        AssertDoesNotContain(animationsText, "private static void AnimateScale(");

        return Task.CompletedTask;
    }

    private static Task ShellElevationSetup_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ShellElevation.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/ShellElevationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ShellElevationController _shellElevationController = null!;");
        AssertContains(adapterText, "private void InitializeShellElevationController()");
        AssertContains(adapterText, "ControlBarBorder = ControlBarBorder,");
        AssertContains(adapterText, "SettingsOverlayPanel = SettingsOverlayPanel,");
        AssertContains(adapterText, "RecordButton = RecordButton,");
        AssertContains(adapterText, "private void ApplyShellElevation()");
        AssertContains(adapterText, "=> _shellElevationController.Apply();");
        AssertContains(mainWindowText, "InitializeShellElevationController();");
        AssertContains(mainWindowText, "ApplyShellElevation();");
        AssertContains(controllerText, "internal sealed class ShellElevationController");
        AssertContains(controllerText, "var controlBarShadow = new ThemeShadow();");
        AssertContains(controllerText, "controlBarShadow.Receivers.Add(_context.SettingsOverlayPanel);");
        AssertContains(controllerText, "_context.ControlBarBorder.Translation = new Vector3(0, 0, 32);");
        AssertContains(controllerText, "var recordButtonShadow = new ThemeShadow();");
        AssertContains(controllerText, "_context.RecordButton.Translation = new Vector3(0, 0, 16);");
        AssertDoesNotContain(mainWindowText, "new Microsoft.UI.Xaml.Media.ThemeShadow()");
        AssertDoesNotContain(mainWindowText, "ControlBarBorder.Translation = new System.Numerics.Vector3(0, 0, 32);");
        AssertDoesNotContain(mainWindowText, "RecordButton.Translation = new System.Numerics.Vector3(0, 0, 16);");

        return Task.CompletedTask;
    }

    private static Task PreviewTransitionAnimations_LiveInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var launchEntranceControllerText = ReadRepoFile("Sussudio/Controllers/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;");
        AssertContains(adapterText, "private void InitializePreviewTransitionAnimationController()");
        AssertContains(adapterText, "PreviewBorder = PreviewBorder,");
        AssertContains(adapterText, "PreviewContentGrid = PreviewContentGrid,");
        AssertContains(adapterText, "StopPreviewFadeInTimer = StopPreviewFadeInTimer,");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.AddPreviewShellEntranceAnimations(storyboard, easing, beginMs, durationMs);");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.ResetPreviewContentTransform();");
        AssertContains(adapterText, "FadeOutShadow(_videoShadowVisual, durationMs: 150);");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.PrepareStartupPresentation();");
        AssertContains(adapterText, "=> PreviewTransitionAnimationController.FadeInElement(element);");
        AssertContains(mainWindowText, "InitializePreviewTransitionAnimationController();");
        AssertContains(launchEntranceControllerText, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertContains(controllerText, "internal sealed class PreviewTransitionAnimationController");
        AssertContains(controllerText, "public void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)");
        AssertContains(controllerText, "public Task AnimatePreviewOutAsync()");
        AssertContains(controllerText, "public Task AnimatePreviewInAsync()");
        AssertContains(controllerText, "public void PrepareStartupPresentation()");
        AssertContains(controllerText, "public void RevealUnavailablePlaceholder()");
        AssertContains(controllerText, "public static void FadeOutElement(UIElement element)");
        AssertContains(controllerText, "private Task AnimatePreviewTransitionAsync(");
        AssertContains(controllerText, "private static Task BeginStoryboardAsync(");
        AssertDoesNotContain(animationsText, "private Task AnimatePreviewTransitionAsync(");
        AssertDoesNotContain(animationsText, "private static Task BeginStoryboardAsync(");
        AssertDoesNotContain(animationsText, "private void ResetPreviewContentTransform()");
        AssertDoesNotContain(animationsText, "private void PreparePreviewStartupPresentation()");
        AssertDoesNotContain(animationsText, "private static void FadeOutElement(UIElement element)");

        return Task.CompletedTask;
    }

    private static Task RecordButtonWidthAnimation_LivesInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var recordingPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedRecording.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.RecordButtonAnimations.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/RecordButtonAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private RecordButtonAnimationController _recordButtonAnimationController = null!;");
        AssertContains(adapterText, "private void InitializeRecordButtonAnimationController()");
        AssertContains(adapterText, "RecordButton = RecordButton,");
        AssertContains(adapterText, "=> _recordButtonAnimationController.AnimateWidth(from, to, onCompleted);");
        AssertContains(mainWindowText, "InitializeRecordButtonAnimationController();");
        AssertContains(propertyChangedText, "HandleRecordingChanged();");
        AssertContains(recordingPropertyChangedText, "Recording-specific ViewModel property projections");
        AssertContains(recordingPropertyChangedText, "AnimateRecordButtonWidth(36, targetWidth);");
        AssertContains(recordingPropertyChangedText, "AnimateRecordButtonWidth(currentWidth, 36, () =>");
        AssertContains(controllerText, "internal sealed class RecordButtonAnimationController");
        AssertContains(controllerText, "public void AnimateWidth(double from, double to, Action? onCompleted = null)");
        AssertContains(controllerText, "Storyboard.SetTarget(anim, _context.RecordButton);");
        AssertContains(controllerText, "_context.RecordButton.Width = to == 36 ? 36 : double.NaN;");
        AssertDoesNotContain(animationsText, "private void AnimateRecordButtonWidth(");
        AssertDoesNotContain(animationsText, "Storyboard.SetTarget(anim, RecordButton);");

        return Task.CompletedTask;
    }

    private static Task RecordingButtonAction_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.RecordingActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/RecordingButtonActionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private RecordingButtonActionController _recordingButtonActionController = null!;");
        AssertContains(adapterText, "private void InitializeRecordingButtonActionController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "GetPreviewActivitySnapshot = () => new RecordingPreviewActivitySnapshot(");
        AssertContains(adapterText, "_d3dRenderer != null && PreviewSwapChainPanel.Visibility == Visibility.Visible");
        AssertContains(adapterText, "_previewSource != null && PreviewImage.Visibility == Visibility.Visible");
        AssertContains(adapterText, "NoDevicePlaceholder.Visibility == Visibility.Visible");
        AssertContains(adapterText, "private Task ToggleRecordingFromButtonAsync()");
        AssertContains(adapterText, "=> _recordingButtonActionController.ToggleRecordingAsync();");
        AssertContains(mainWindowText, "InitializeRecordingButtonActionController();");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => ToggleRecordingFromButtonAsync(), nameof(RecordButton_Click));");
        AssertContains(controllerText, "internal readonly record struct RecordingPreviewActivitySnapshot");
        AssertContains(controllerText, "public bool RendererActive => GpuActive || CpuActive;");
        AssertContains(controllerText, "public async Task ToggleRecordingAsync()");
        AssertContains(controllerText, "await _context.ViewModel.ToggleRecordingAsync();");
        AssertContains(controllerText, "if (!_context.ViewModel.IsRecording)");
        AssertContains(controllerText, "PreviewStateDuringRecording: rendererActive={snapshot.RendererActive}");
        AssertContains(controllerText, "WARNING: preview renderer appears inactive while recording.");
        AssertDoesNotContain(eventHandlersText, "ViewModel.ToggleRecordingAsync();");
        AssertDoesNotContain(eventHandlersText, "PreviewStateDuringRecording");
        AssertDoesNotContain(eventHandlersText, "WARNING: preview renderer appears inactive while recording.");

        return Task.CompletedTask;
    }

    private static Task LiveSignalInfoPresentation_LivesInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var liveSignalAdapterText = ReadRepoFile("Sussudio/MainWindow.LiveSignalInfo.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/LiveSignalInfoController.cs").Replace("\r\n", "\n");

        AssertContains(liveSignalAdapterText, "private LiveSignalInfoController _liveSignalInfoController = null!;");
        AssertContains(liveSignalAdapterText, "private void InitializeLiveSignalInfoController()");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.Update(");
        AssertContains(liveSignalAdapterText, "ViewModel.LiveResolution,");
        AssertContains(liveSignalAdapterText, "private void StopLiveSignalInfoTimers()");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.StopTimers();");
        AssertContains(mainWindowText, "InitializeLiveSignalInfoController();");
        AssertContains(shutdownCleanupText, "StopLiveSignalInfoTimers();");
        AssertContains(controllerText, "internal sealed class LiveSignalInfoController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _showDebounceTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _hideDebounceTimer;");
        AssertContains(controllerText, "public void Update(string liveResolution, string liveFrameRate, string livePixelFormat)");
        AssertContains(controllerText, "private bool HasCompleteLiveSignal()");
        AssertContains(controllerText, "private void AnimateIn()");
        AssertContains(controllerText, "private void AnimateOut()");
        AssertDoesNotContain(mainWindowText, "private bool _liveSignalInfoVisible;");
        AssertDoesNotContain(mainWindowText, "private DispatcherQueueTimer? _liveSignalDebounceTimer;");
        AssertDoesNotContain(animationsText, "private void UpdateLiveSignalInfoVisibility()");
        AssertDoesNotContain(animationsText, "private void AnimateLiveSignalInfoIn()");
        AssertDoesNotContain(animationsText, "private void AnimateLiveSignalInfoOut()");

        return Task.CompletedTask;
    }

    private static Task PreviewAudioFadeState_LivesInController()
    {
        var animationsText = ReadRepoFile("Sussudio/MainWindow.Animations.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewAudioFade.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewAudioFadeController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewAudioFadeController _previewAudioFadeController = null!;");
        AssertContains(adapterText, "private bool IsPreviewAudioFadeInActive => _previewAudioFadeController.IsFadingIn;");
        AssertContains(adapterText, "private bool IsPreviewAudioFadeAnimationActive => _previewAudioFadeController.IsAnimationActive;");
        AssertContains(adapterText, "private void InitializePreviewAudioFadeController()");
        AssertContains(adapterText, "=> _previewAudioFadeController.PrimeFadeIn();");
        AssertContains(adapterText, "=> _previewAudioFadeController.StartFadeIn(durationMs);");
        AssertContains(adapterText, "=> _previewAudioFadeController.StartFadeOutAsync(durationMs);");
        AssertContains(adapterText, "=> _previewAudioFadeController.CancelFadeInForUser();");
        AssertContains(mainWindowText, "InitializePreviewAudioFadeController();");
        AssertContains(bindingsText, "IsPreviewAudioFadeInActive || IsPreviewAudioFadeAnimationActive");
        AssertContains(propertyChangedText, "await HandlePreviewingChangedAsync();");
        AssertContains(propertyChangedText, "HandlePreviewVolumeChanged();");
        AssertContains(audioPropertyChangedText, "if (IsPreviewAudioFadeInActive)");
        AssertContains(previewPropertyChangedText, "PrimePreviewAudioFadeIn();");
        AssertContains(controllerText, "internal sealed class PreviewAudioFadeController");
        AssertContains(controllerText, "private double _savedPreviewVolume;");
        AssertContains(controllerText, "private Storyboard? _volumeFadeStoryboard;");
        AssertContains(controllerText, "public void PrimeFadeIn()");
        AssertContains(controllerText, "public async Task StartFadeOutAsync(int durationMs = 450)");
        AssertContains(controllerText, "Sussudio.Logger.Log(\"PREVIEW_AUDIO_FADE_OUT_COMPLETED\");");
        AssertDoesNotContain(mainWindowText, "private double _savedPreviewVolume;");
        AssertDoesNotContain(mainWindowText, "private bool _isVolumeFadingIn;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _previewVolumeFadeStoryboard;");
        AssertDoesNotContain(animationsText, "private void PrimePreviewAudioFadeIn()");
        AssertDoesNotContain(animationsText, "private void CompletePreviewAudioFadeIn(");
        AssertDoesNotContain(animationsText, "private async Task StartPreviewAudioFadeOutAsync(");

        return Task.CompletedTask;
    }

    private static Task MicrophoneControls_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.MicrophoneControls.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var audioPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/MicrophoneControlsController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private MicrophoneControlsController _microphoneControlsController = null!;");
        AssertContains(adapterText, "private void InitializeMicrophoneControlsController()");
        AssertContains(adapterText, "=> _microphoneControlsController.AttachVolumeBindings();");
        AssertContains(adapterText, "=> _microphoneControlsController.SyncVolumeControls(volumePercent);");
        AssertContains(adapterText, "=> _microphoneControlsController.ApplyInitialVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.UpdateVisibility();");
        AssertContains(adapterText, "=> _microphoneControlsController.StopRowAnimation();");
        AssertContains(mainWindowText, "InitializeMicrophoneControlsController();");
        AssertContains(bindingsText, "SetupMicrophoneVolumeBindings();");
        AssertContains(bindingsText, "ApplyInitialMicrophoneControlsVisibility();");
        AssertContains(propertyChangedText, "HandleMicrophoneEnabledChanged();");
        AssertContains(propertyChangedText, "HandleMicrophoneVolumeChanged();");
        AssertContains(audioPropertyChangedText, "UpdateMicrophoneControlsVisibility();");
        AssertContains(audioPropertyChangedText, "SyncMicrophoneVolumeControls(ViewModel.MicrophoneVolume);");
        AssertContains(shutdownCleanupText, "StopMicMeterRowAnimation();");
        AssertContains(controllerText, "internal sealed class MicrophoneControlsController");
        AssertContains(controllerText, "private bool _syncingVolumeControls;");
        AssertContains(controllerText, "private Storyboard? _activeRowStoryboard;");
        AssertContains(controllerText, "public void AttachVolumeBindings()");
        AssertContains(controllerText, "public void SyncVolumeControls(double volumePercent)");
        AssertContains(controllerText, "public void ApplyInitialVisibility()");
        AssertContains(controllerText, "public void UpdateVisibility()");
        AssertContains(controllerText, "public void StopRowAnimation()");
        AssertContains(controllerText, "private Storyboard CreateRowStoryboard(bool showing)");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _micMeterRowStoryboard;");
        AssertDoesNotContain(mainWindowText, "private bool _syncingMicrophoneVolumeControls;");
        AssertDoesNotContain(mainWindowText, "private const double MicMeterRowHeight = 14;");
        AssertDoesNotContain(bindingsText, "MicVolumeSlider.ValueChanged +=");
        AssertDoesNotContain(bindingsText, "private void SyncMicrophoneVolumeControls(double volumePercent)");
        AssertDoesNotContain(bindingsText, "private Storyboard CreateMicMeterRowStoryboard(bool showing)");

        return Task.CompletedTask;
    }

    private static Task ResponsiveShellLayout_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ResponsiveShellLayout.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/ResponsiveShellLayoutController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;");
        AssertContains(adapterText, "private void InitializeResponsiveShellLayoutController()");
        AssertContains(adapterText, "ControlBarBorder = ControlBarBorder,");
        AssertContains(adapterText, "CaptureSettingsGrid = CaptureSettingsGrid,");
        AssertContains(adapterText, "private void SetupResponsiveShellLayoutBindings()");
        AssertContains(adapterText, "=> _responsiveShellLayoutController.Attach();");
        AssertContains(mainWindowText, "InitializeResponsiveShellLayoutController();");
        AssertContains(bindingsText, "SetupResponsiveShellLayoutBindings();");
        AssertContains(controllerText, "internal sealed class ResponsiveShellLayoutController");
        AssertContains(controllerText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertContains(controllerText, "private const double CaptureSettingsNarrowWidth = 700.0;");
        AssertContains(controllerText, "private bool _toggleLabelsVisible;");
        AssertContains(controllerText, "private bool _captureSettingsNarrow;");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(controllerText, "_context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);");
        AssertContains(controllerText, "private void ApplyNarrowCaptureSettingsLayout()");
        AssertContains(controllerText, "private void ApplyWideCaptureSettingsLayout()");
        AssertDoesNotContain(mainWindowText, "private bool _toggleLabelsVisible;");
        AssertDoesNotContain(mainWindowText, "private bool _captureSettingsNarrow;");
        AssertDoesNotContain(mainWindowText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(bindingsText, "private void UpdateToggleLabelVisibility(");
        AssertDoesNotContain(bindingsText, "private void CaptureSettingsGrid_SizeChanged(");

        return Task.CompletedTask;
    }

    private static Task CaptureSelectionBindingSync_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureSelectionBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureSelectionBindingController _captureSelectionBindingController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureSelectionBindingController()");
        AssertContains(adapterText, "DeviceComboBox = DeviceComboBox,");
        AssertContains(adapterText, "AnalogAudioGainValueTextBlock = AnalogAudioGainValueTextBlock");
        AssertContains(adapterText, "private void EnsureDeviceSelection()");
        AssertContains(adapterText, "=> _captureSelectionBindingController.EnsureDeviceSelection();");
        AssertContains(adapterText, "private void UpdateDeviceApplyButtonState()");
        AssertContains(mainWindowText, "InitializeCaptureSelectionBindingController();");
        AssertContains(bindingsText, "AttachCaptureSelectionBindings();");
        AssertContains(propertyChangedText, "EnsureResolutionSelection();");
        AssertContains(propertyChangedText, "ApplyDeviceAudioControlState();");
        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertContains(controllerText, "public void AttachCollectionBindings()");
        AssertContains(controllerText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(controllerText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");
        AssertContains(controllerText, "public void ApplyDeviceAudioControlState()");
        AssertContains(controllerText, "public bool HasPendingDeviceSelection()");
        AssertContains(controllerText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertDoesNotContain(mainWindowText, "_selectionSyncQueued");
        AssertDoesNotContain(bindingsText, "private void QueueSelectionSync(");
        AssertDoesNotContain(bindingsText, "private static void AttachCollectionSync(");
        AssertDoesNotContain(bindingsText, "private void EnsureDeviceSelection()");

        return Task.CompletedTask;
    }

    private static Task CaptureDeviceButtonActions_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureDeviceActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/CaptureDeviceActionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private CaptureDeviceActionController _captureDeviceActionController = null!;");
        AssertContains(adapterText, "private void InitializeCaptureDeviceActionController()");
        AssertContains(adapterText, "RefreshButton = RefreshButton,");
        AssertContains(adapterText, "ApplyDeviceButton = ApplyDeviceButton,");
        AssertContains(adapterText, "UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState");
        AssertContains(adapterText, "private Task RefreshDevicesFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.RefreshDevicesAsync();");
        AssertContains(adapterText, "private Task ApplySelectedDeviceFromButtonAsync()");
        AssertContains(adapterText, "=> _captureDeviceActionController.ApplySelectedDeviceAsync();");
        AssertContains(mainWindowText, "InitializeCaptureDeviceActionController();");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => RefreshDevicesFromButtonAsync(), nameof(RefreshButton_Click));");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => ApplySelectedDeviceFromButtonAsync(), nameof(ApplyDeviceButton_Click));");
        AssertContains(controllerText, "internal sealed class CaptureDeviceActionController");
        AssertContains(controllerText, "public async Task RefreshDevicesAsync()");
        AssertContains(controllerText, "new ProgressRing { Width = 16, Height = 16, IsActive = true }");
        AssertContains(controllerText, "await _context.ViewModel.RefreshDevicesAsync();");
        AssertContains(controllerText, "new FontIcon { Glyph = \"\\uE72C\", FontSize = 14 }");
        AssertContains(controllerText, "public async Task ApplySelectedDeviceAsync()");
        AssertContains(controllerText, "_context.DeviceComboBox.SelectedItem is not CaptureDevice selectedDevice");
        AssertContains(controllerText, "await _context.ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertContains(controllerText, "_context.UpdateDeviceApplyButtonState();");
        AssertDoesNotContain(eventHandlersText, "ViewModel.RefreshDevicesAsync();");
        AssertDoesNotContain(eventHandlersText, "ViewModel.ApplySelectedDeviceAsync(selectedDevice);");
        AssertDoesNotContain(eventHandlersText, "UpdateDeviceApplyButtonState();");

        return Task.CompletedTask;
    }

    private static Task CaptureOptionPresentation_LivesInFocusedPartial()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.CaptureOptionPresentation.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");

        AssertContains(captureOptionText, "private void UpdateDecoderCountVisibility()");
        AssertContains(captureOptionText, "private int _selectedDecoderCount = 4;");
        AssertContains(captureOptionText, "private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)");
        AssertContains(captureOptionText, "ViewModel.MjpegDecoderCount = count;");
        AssertContains(captureOptionText, "private double GetSelectedFriendlyFrameRate()");
        AssertContains(captureOptionText, "private void RefreshHdrHintText()");
        AssertContains(captureOptionText, "private void UpdateFpsTelemetryTooltip()");
        AssertContains(captureOptionText, "private void ApplyHdrToggleEnabledState()");
        AssertContains(captureOptionText, "private void ApplyBitrateVisibility()");
        AssertContains(captureOptionText, "private void ApplyAudioClipVisibility()");
        AssertContains(captureOptionText, "ViewModel.SelectedFormat?.PixelFormat");
        AssertContains(captureOptionText, "Stop recording before switching between HDR and SDR pipelines.");
        AssertContains(captureOptionText, "ViewModel.SourceTelemetrySummaryText");
        AssertContains(bindingsText, "ApplyHdrToggleEnabledState();");
        AssertContains(bindingsText, "RefreshHdrHintText();");
        AssertContains(bindingsText, "UpdateFpsTelemetryTooltip();");
        AssertContains(bindingsText, "ApplyBitrateVisibility();");
        AssertContains(bindingsText, "ApplyAudioClipVisibility();");
        AssertContains(propertyChangedText, "UpdateOutputPathDisplay();");
        AssertContains(propertyChangedText, "ApplyAudioClipVisibility();");
        AssertContains(propertyChangedText, "ApplyHdrToggleEnabledState();");
        AssertContains(propertyChangedText, "RefreshHdrHintText();");
        AssertContains(propertyChangedText, "UpdateFpsTelemetryTooltip();");
        AssertContains(propertyChangedText, "ApplyBitrateVisibility();");
        AssertDoesNotContain(bindingsText, "private void UpdateDecoderCountVisibility()");
        AssertDoesNotContain(bindingsText, "private void DecoderCountComboBox_SelectionChanged(");
        AssertDoesNotContain(bindingsText, "private void RefreshHdrHintText()");
        AssertDoesNotContain(bindingsText, "private void ApplyBitrateVisibility()");
        AssertDoesNotContain(ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n"), "private int _selectedDecoderCount = 4;");

        return Task.CompletedTask;
    }

    private static Task OutputPathDisplay_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.OutputPathDisplay.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/OutputPathDisplayController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private OutputPathDisplayController _outputPathDisplayController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathDisplayController()");
        AssertContains(adapterText, "OutputPathTextBox = OutputPathTextBox,");
        AssertContains(adapterText, "GetOutputPath = () => ViewModel.OutputPath,");
        AssertContains(adapterText, "private void AttachOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathDisplayController.Attach();");
        AssertContains(adapterText, "private void UpdateOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathDisplayController.Update();");
        AssertContains(mainWindowText, "InitializeOutputPathDisplayController();");
        AssertContains(bindingsText, "AttachOutputPathDisplay();");
        AssertContains(propertyChangedText, "UpdateOutputPathDisplay();");
        AssertContains(controllerText, "internal sealed class OutputPathDisplayController");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(controllerText, "public void Update()");
        AssertContains(controllerText, "ToolTipService.SetToolTip(_context.OutputPathTextBox, path);");
        AssertContains(controllerText, "var maxChars = (int)((availableWidth - 20) / 7);");
        AssertContains(controllerText, "var parts = path.Split('\\\\', '/');");
        AssertContains(controllerText, "var candidate = $\"{root}\\\\...\\\\{tail}\";");
        AssertDoesNotContain(bindingsText, "OutputPathTextBox.SizeChanged += (s, e) => UpdateOutputPathDisplay();");
        AssertDoesNotContain(bindingsText, "private void UpdateOutputPathDisplay()");
        AssertDoesNotContain(bindingsText, "ToolTipService.SetToolTip(OutputPathTextBox, path);");

        return Task.CompletedTask;
    }

    private static Task OutputPathButtonActions_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.OutputPathActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/OutputPathActionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private OutputPathActionController _outputPathActionController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathActionController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "OpenRecordingsFolderAsync = () => OpenRecordingsFolderAsync()");
        AssertContains(adapterText, "private Task BrowseOutputPathFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathActionController.BrowseAsync();");
        AssertContains(adapterText, "private Task OpenRecordingsFolderFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathActionController.OpenRecordingsFolderIfAvailableAsync();");
        AssertContains(mainWindowText, "InitializeOutputPathActionController();");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => BrowseOutputPathFromButtonAsync(), nameof(BrowseButton_Click));");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => OpenRecordingsFolderFromButtonAsync(), nameof(OpenRecordingsButton_Click));");
        AssertContains(controllerText, "internal sealed class OutputPathActionController");
        AssertContains(controllerText, "public Task BrowseAsync()");
        AssertContains(controllerText, "=> _context.ViewModel.BrowseOutputPathAsync();");
        AssertContains(controllerText, "public Task OpenRecordingsFolderIfAvailableAsync()");
        AssertContains(controllerText, "string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)");
        AssertContains(controllerText, "return _context.OpenRecordingsFolderAsync();");
        AssertDoesNotContain(eventHandlersText, "ViewModel.BrowseOutputPathAsync()");
        AssertDoesNotContain(eventHandlersText, "System.IO.Directory.Exists(path)");

        return Task.CompletedTask;
    }

    private static Task PreviewScreenshotButtonWorkflow_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var eventHandlersText = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewScreenshot.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewScreenshotController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewScreenshotController _previewScreenshotController = null!;");
        AssertContains(adapterText, "private void InitializePreviewScreenshotController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "ScreenshotButton = ScreenshotButton,");
        AssertContains(adapterText, "private Task CapturePreviewScreenshotAsync()");
        AssertContains(adapterText, "=> _previewScreenshotController.CaptureAsync();");
        AssertContains(mainWindowText, "InitializePreviewScreenshotController();");
        AssertContains(eventHandlersText, "_ = RunUiEventHandlerAsync(() => CapturePreviewScreenshotAsync(), nameof(ScreenshotButton_Click));");
        AssertContains(controllerText, "internal sealed class PreviewScreenshotController");
        AssertContains(controllerText, "public async Task CaptureAsync()");
        AssertContains(controllerText, "Start preview before capturing a screenshot");
        AssertContains(controllerText, "Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), \"Sussudio\")");
        AssertContains(controllerText, "Directory.CreateDirectory(outputDir);");
        AssertContains(controllerText, "CapturePreviewFrameAsync(filePath)");
        AssertContains(controllerText, "Screenshot saved: {Path.GetFileName(filePath)}");
        AssertContains(controllerText, "SCREENSHOT_SAVED");
        AssertContains(controllerText, "SCREENSHOT_FAILED");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = false;");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = true;");
        AssertDoesNotContain(eventHandlersText, "Directory.CreateDirectory(outputDir);");
        AssertDoesNotContain(eventHandlersText, "CapturePreviewFrameAsync(filePath)");

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

    private static Task CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 3840u);
        SetPropertyOrBackingField(settings, "Height", 2160u);
        SetPropertyOrBackingField(settings, "FrameRate", 120d);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", "MJPG");
        SetPropertyOrBackingField(settings, "HdrEnabled", false);

        AssertEqual(true, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "UseMjpegHighFrameRateMode");

        SetPropertyOrBackingField(settings, "HdrEnabled", true);
        AssertEqual(false, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "UseMjpegHighFrameRateMode HDR");

        SetPropertyOrBackingField(settings, "HdrEnabled", false);
        SetPropertyOrBackingField(settings, "Width", 1920u);
        AssertEqual(false, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "UseMjpegHighFrameRateMode non-4k");

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

    private static Task FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps()
    {
        var tracker = CreateInstance("Sussudio.Services.Capture.FrameFingerprintCadenceTracker");
        var trackerType = tracker.GetType();
        var recordFrame = trackerType.GetMethod("RecordFrame", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FrameFingerprintCadenceTracker.RecordFrame not found.");
        var getMetrics = trackerType.GetMethod("GetMetrics", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FrameFingerprintCadenceTracker.GetMetrics not found.");

        var intervalTicks = Math.Max(1, Stopwatch.Frequency / 120);
        var tick = Stopwatch.Frequency;
        for (ulong hash = 1; hash <= 120; hash++)
        {
            recordFrame.Invoke(tracker, new object?[] { hash, tick });
            tick += intervalTicks;
        }

        var repeatedHash = 120UL;
        for (var i = 0; i < 90; i++)
        {
            recordFrame.Invoke(tracker, new object?[] { repeatedHash, tick });
            tick += intervalTicks;
        }

        var metrics = getMetrics.Invoke(tracker, new object?[] { 180 })
            ?? throw new InvalidOperationException("FrameFingerprintCadenceTracker.GetMetrics returned null.");

        AssertEqual("DuplicateRun", GetStringProperty(metrics, "Pattern"), "packet hash pattern during trailing duplicate run");
        AssertEqual(true, GetBoolProperty(metrics, "LastFrameDuplicate"), "packet hash last-frame duplicate state");

        var duplicatePercent = GetDoubleProperty(metrics, "DuplicateFramePercent");
        if (duplicatePercent < 40)
        {
            throw new InvalidOperationException($"Duplicate percent did not reflect recent duplicate run: {duplicatePercent:0.00}%.");
        }

        var uniqueFps = GetDoubleProperty(metrics, "UniqueObservedFps");
        if (uniqueFps >= 80)
        {
            throw new InvalidOperationException($"Unique FPS stayed stale during duplicate run: {uniqueFps:0.00} fps.");
        }

        return Task.CompletedTask;
    }

    private static Task VisualCadenceTracker_UsesExactCropPixelsWithOnePassDiff()
    {
        var trackerSource = ReadRepoFile("Sussudio/Services/Capture/VisualCadenceTracker.cs").Replace("\r\n", "\n");
        var captureSource = (ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.Preview.cs"))
            .Replace("\r\n", "\n");

        AssertContains(trackerSource, "DefaultSampleColumns = 640");
        AssertContains(trackerSource, "DefaultSampleRows = 360");
        AssertContains(trackerSource, "sampleX = cropX + Math.Max(0, (cropWidth - sampleWidth) / 2)");
        AssertContains(trackerSource, "sampleY = cropY + Math.Max(0, (cropHeight - sampleHeight) / 2)");
        AssertContains(trackerSource, "var x = sampleX + col;");
        AssertContains(trackerSource, "var y = sampleY + row;");
        AssertContains(trackerSource, "SampleLumaAndCompare(");
        AssertContains(trackerSource, "destination[index] = luma;");
        AssertContains(trackerSource, "if (previous != null && previous[index] != luma)");
        AssertContains(trackerSource, "_lastSample = new byte[_sampleSize * 2]");
        AssertContains(trackerSource, "if (bytesPerLuma == 2)");
        AssertContains(trackerSource, "if (previous != null && previous[index] != secondLuma)");
        AssertContains(trackerSource, "sample.ChangedPixels");
        AssertContains(trackerSource, "PromoteCurrentSample(sampleLength, bytesPerLuma)");
        AssertContains(trackerSource, "_lastSample = _currentSample;");
        AssertContains(trackerSource, "AddValueSample(_deltaWindow, ref _deltaCount, ref _deltaIndex, delta)");
        AssertContains(trackerSource, "if (delta > 0)");
        AssertDoesNotContain(trackerSource, "ChangeThreshold");
        AssertDoesNotContain(trackerSource, "ComputeAverageDelta");
        AssertDoesNotContain(trackerSource, "Array.Copy(_currentSample, _lastSample");
        AssertDoesNotContain(trackerSource, "ComputeChangedPixelCount");

        AssertContains(captureSource, "previewFrameProbe: null");
        AssertContains(captureSource, "frame.ArrivalTick");
        AssertContains(captureSource, "cropLeft: 0.25");
        AssertContains(captureSource, "cropWidth: 0.5");
        AssertContains(captureSource, "sampleColumns: 320");
        AssertContains(captureSource, "cropLeft: 0.375");
        AssertContains(captureSource, "cropWidth: 0.25");

        return Task.CompletedTask;
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

    // ── RecordingContracts tests ──

    private static Task FinalizeResult_Success_ProducesEmptyPreservedList()
    {
        var resultType = RequireType("Sussudio.Services.Contracts.FinalizeResult");
        var successMethod = resultType.GetMethod("Success", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FinalizeResult.Success not found");
        var result = successMethod.Invoke(null, new object[] { "/path/output.mp4", "Stopped" })!;

        AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
        AssertEqual("/path/output.mp4", GetStringProperty(result, "OutputPath"), "OutputPath");
        AssertEqual("Stopped", GetStringProperty(result, "StatusMessage"), "StatusMessage");
        var artifacts = GetPropertyValue(result, "PreservedArtifacts");
        AssertEqual(0, GetCountProperty(artifacts), "PreservedArtifacts.Count");

        return Task.CompletedTask;
    }

    private static Task FinalizeResult_Failure_DeduplicatesAndFiltersArtifacts()
    {
        var resultType = RequireType("Sussudio.Services.Contracts.FinalizeResult");
        var failureMethod = resultType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FinalizeResult.Failure not found");

        var artifacts = new List<string?> { "/path/a.mp4", "/path/A.mp4", null!, "", " ", "/path/b.m4a" }
            .Where(s => true) as IEnumerable<string>;
        var result = failureMethod.Invoke(null, new object?[] { "/output.mp4", "mux failed", artifacts })!;

        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
        var preserved = GetPropertyValue(result, "PreservedArtifacts");
        AssertEqual(2, GetCountProperty(preserved), "PreservedArtifacts.Count");

        return Task.CompletedTask;
    }

    // ── RecordingArtifactManager tests ──

    private static Task ArtifactManager_FinalizeContext_ReturnsSuccess_WhenPostMuxDisabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var finalPath = Path.Combine(tempDir, "video.mp4");
            File.WriteAllText(finalPath, "video-data");

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(usePostMuxAudio: false, finalPath: finalPath);

            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");
            var result = finalizeMethod.Invoke(manager, new object?[] { context, true, null })!;

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual(finalPath, GetStringProperty(result, "OutputPath"), "OutputPath");

            return Task.CompletedTask;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    private static Task ArtifactManager_FinalizeContext_PreservesTempArtifacts_WhenMuxFails()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var finalPath = Path.Combine(tempDir, "final.mp4");
            File.WriteAllText(videoPath, "video-data");
            File.WriteAllText(audioPath, "audio-data");
            File.WriteAllBytes(finalPath, Array.Empty<byte>()); // empty placeholder

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: finalPath);

            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");
            var result = finalizeMethod.Invoke(manager, new object?[] { context, false, "encoder error" })!;

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            var preserved = GetPropertyValue(result, "PreservedArtifacts");
            AssertEqual(2, GetCountProperty(preserved), "PreservedArtifacts.Count");

            // Empty final file should have been deleted
            if (File.Exists(finalPath))
                throw new InvalidOperationException("Expected empty final file to be deleted");

            return Task.CompletedTask;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    private static Task ArtifactManager_FinalizeContext_RejectsInvalidFinalOutput()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var emptyFinalPath = Path.Combine(tempDir, "empty-final.mp4");
            var missingFinalPath = Path.Combine(tempDir, "missing-final.mp4");
            File.WriteAllText(videoPath, "video-data");
            File.WriteAllText(audioPath, "audio-data");
            File.WriteAllBytes(emptyFinalPath, Array.Empty<byte>());

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");

            var directContext = BuildRecordingContext(usePostMuxAudio: false, finalPath: emptyFinalPath);
            var directResult = finalizeMethod.Invoke(manager, new object?[] { directContext, true, null })!;
            AssertEqual(false, GetBoolProperty(directResult, "Succeeded"), "Direct empty output finalize fails");
            AssertContains(GetStringProperty(directResult, "StatusMessage"), "final output invalid");
            AssertContains(GetStringProperty(directResult, "StatusMessage"), "output file is empty");

            var muxContext = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: missingFinalPath);
            var muxResult = finalizeMethod.Invoke(manager, new object?[] { muxContext, true, null })!;
            AssertEqual(false, GetBoolProperty(muxResult, "Succeeded"), "Mux success with missing final output fails");
            AssertContains(GetStringProperty(muxResult, "StatusMessage"), "output file is missing");
            var preserved = GetPropertyValue(muxResult, "PreservedArtifacts");
            AssertEqual(2, GetCountProperty(preserved), "Invalid mux final preserves temp artifacts");
            AssertEqual(true, File.Exists(videoPath), "Invalid mux final preserves video temp");
            AssertEqual(true, File.Exists(audioPath), "Invalid mux final preserves audio temp");

            return Task.CompletedTask;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    private static Task ArtifactManager_RollbackAsync_DeletesAllArtifacts_WhenPostMuxEnabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var finalPath = Path.Combine(tempDir, "final.mp4");
            File.WriteAllText(videoPath, "v");
            File.WriteAllText(audioPath, "a");
            File.WriteAllText(finalPath, "f");

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: finalPath);

            var rollbackMethod = manager.GetType().GetMethod("RollbackAsync")
                ?? throw new InvalidOperationException("RollbackAsync not found");
            var task = rollbackMethod.Invoke(manager, new object?[] { context, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("RollbackAsync did not return Task");
            task.GetAwaiter().GetResult();

            if (File.Exists(videoPath))
                throw new InvalidOperationException("Expected video temp to be deleted");
            if (File.Exists(audioPath))
                throw new InvalidOperationException("Expected audio temp to be deleted");
            if (File.Exists(finalPath))
                throw new InvalidOperationException("Expected final output to be deleted");

            return Task.CompletedTask;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    private static Task ArtifactManager_RollbackAsync_SafeWithNullContext()
    {
        var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
        var rollbackMethod = manager.GetType().GetMethod("RollbackAsync")
            ?? throw new InvalidOperationException("RollbackAsync not found");

        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var task = rollbackMethod.Invoke(manager, new object?[] { null, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("RollbackAsync did not return Task");
        task.GetAwaiter().GetResult();

        return Task.CompletedTask;
    }

    // ── CaptureSettings tests ──

    private static Task CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate()
    {
        // 4K60 H264 High: 25 * (3840*2160/2073600) * (60/30) * 1.0 = 25 * 3.98 * 2 = ~199.07 → clamped to 200
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 3840u);
        SetPropertyOrBackingField(settings, "Height", 2160u);
        SetPropertyOrBackingField(settings, "FrameRate", 60.0);
        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "H264Mp4"));
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "High"));

        var bitrate = InvokeInstanceMethod(settings, "GetTargetBitrate");
        var bps = Convert.ToUInt32(bitrate);

        // 4K60 H264 High should be at or near 200 Mbps cap
        if (bps < 150_000_000 || bps > 200_000_000)
            throw new InvalidOperationException($"Expected 4K60 H264 High ~200 Mbps, got {bps / 1_000_000.0:F1} Mbps");

        // 1080p30 H264 High: 25 * 1.0 * 1.0 * 1.0 = 25 Mbps
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 30.0);
        var lowBitrate = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));
        if (lowBitrate < 24_000_000 || lowBitrate > 26_000_000)
            throw new InvalidOperationException($"Expected 1080p30 H264 High ~25 Mbps, got {lowBitrate / 1_000_000.0:F1} Mbps");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency()
    {
        // 1080p60 at each codec: H264 > HEVC > AV1
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60.0);
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "High"));

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "H264Mp4"));
        var h264 = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        var hevc = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1 = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));

        if (hevc >= h264)
            throw new InvalidOperationException($"HEVC ({hevc}) should be less than H264 ({h264})");
        if (av1 >= hevc)
            throw new InvalidOperationException($"AV1 ({av1}) should be less than HEVC ({hevc})");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetTargetBitrate_ClampsCustomQuality()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "Custom"));

        // Over max: should clamp to 300 Mbps
        SetPropertyOrBackingField(settings, "CustomBitrateMbps", 999.0);
        var over = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));
        AssertEqual(300_000_000u, over, "CustomBitrate over-max clamp");

        // Under min: should clamp to 1 Mbps
        SetPropertyOrBackingField(settings, "CustomBitrateMbps", 0.1);
        var under = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));
        AssertEqual(1_000_000u, under, "CustomBitrate under-min clamp");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetOutputFileName_IncludesFormatSuffix()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1Name = InvokeInstanceMethod(settings, "GetOutputFileName").ToString()!;
        AssertContains(av1Name, "_AV1.");
        AssertContains(av1Name, ".mp4");

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        var hevcName = InvokeInstanceMethod(settings, "GetOutputFileName").ToString()!;
        AssertContains(hevcName, "_HEVC.");

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "H264Mp4"));
        var h264Name = InvokeInstanceMethod(settings, "GetOutputFileName").ToString()!;
        AssertContains(h264Name, "_H264.");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat()
    {
        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var method = settingsType.GetMethod("IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("IsMjpegHighFrameRateMode not found");

        // SDR + MJPG + 4K120 → true
        var result1 = (bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, false, false })!;
        AssertEqual(true, result1, "SDR+MJPG+4K120 should be HFR");

        // HDR + MJPG → false (HDR disqualifies)
        var result2 = (bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, true, false })!;
        AssertEqual(false, result2, "HDR should not be HFR");

        // SDR + NV12 → false (wrong pixel format)
        var result3 = (bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120.0, false, false })!;
        AssertEqual(false, result3, "NV12 should not be HFR");

        // SDR + MJPG + 1080p60 → false (too low res/fps)
        var result4 = (bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60.0, false, false })!;
        AssertEqual(false, result4, "1080p60 should not be HFR");

        return Task.CompletedTask;
    }

    // ── FlashbackBufferManager tests ──

    private static object CreateInitializedBufferManager(string tempDir)
    {
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var options = RuntimeHelpers.GetUninitializedObject(optionsType);
        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
        SetPropertyBackingField(options, "TempDirectory", tempDir);
        SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

        var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var manager = RuntimeHelpers.GetUninitializedObject(managerType);
        SetPrivateField(manager, "_options", options);
        SetPrivateField(manager, "_indexLock", new object());
        SetPrivateField(manager, "_sessionId", "test-session");
        SetPrivateField(manager, "_sessionDirectory", tempDir);
        SetPrivateField(manager, "_activeSegmentPath", Path.Combine(tempDir, "fb_test_0003.ts"));
        SetPrivateField(manager, "_activeSegmentStartPtsTicks", -1L);
        SetPrivateField(manager, "_nextSegmentIndex", 4);

        // Initialize the completed segments list via reflection
        var listType = managerType.GetField("_completedSegments", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = listType.GetValue(manager);
        if (list == null)
        {
            // GetUninitializedObject skips ctor — create the list
            var csType = managerType.GetNestedType("CompletedSegment", BindingFlags.NonPublic)!;
            var listGenericType = typeof(List<>).MakeGenericType(csType);
            list = Activator.CreateInstance(listGenericType)!;
            listType.SetValue(manager, list);
        }

        return manager;
    }

    private static void AddCompletedSegment(object manager, string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)
    {
        var managerType = manager.GetType();
        var csType = managerType.GetNestedType("CompletedSegment", BindingFlags.NonPublic)!;
        var listField = managerType.GetField("_completedSegments", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = listField.GetValue(manager)!;
        var addMethod = list.GetType().GetMethod("Add")!;

        var countProp = list.GetType().GetProperty("Count")!;
        var seqNum = (int)countProp.GetValue(list)!;

        var segment = Activator.CreateInstance(csType, path, seqNum, startPts, endPts, sizeBytes)!;
        addMethod.Invoke(list, new[] { segment });
    }

    private static Task FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "public string? GetSegmentFileForPosition(TimeSpan absolutePts)\n        => GetValidSegmentFileForPosition(absolutePts);");

        // Add 3 segments: 0-5s, 5-10s, 10-15s
        var seg0 = Path.Combine(tempDir, "seg0.ts");
        var seg1 = Path.Combine(tempDir, "seg1.ts");
        var seg2 = Path.Combine(tempDir, "seg2.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(seg0, "segment");
        File.WriteAllText(seg1, "segment");
        File.WriteAllText(seg2, "segment");
        File.WriteAllText(active, "active");
        AddCompletedSegment(manager, seg0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 1000);
        AddCompletedSegment(manager, seg1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 1000);
        AddCompletedSegment(manager, seg2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 1000);

        var method = manager.GetType().GetMethod("GetSegmentFileForPosition")!;

        // Position 7s → segment 1 (5-10s)
        var result1 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(7) }) as string;
        AssertEqual(seg1, result1!, "Position 7s");

        // Position 0s → segment 0 (0-5s)
        var result2 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(0) }) as string;
        AssertEqual(seg0, result2!, "Position 0s");

        // Position 20s → not in any completed segment → falls back to active
        var result3 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(20) }) as string;
        AssertContains(result3!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentCompletionRejectsInvalidMetadata()
    {
        var source = ReadFlashbackBufferManagerSource();

        AssertContains(source, "if (string.IsNullOrWhiteSpace(path))\n        {\n            Logger.Log(\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=empty_path\");\n            return;\n        }");
        AssertContains(source, "if (endPts <= startPts)\n        {\n            Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=invalid_range path='{Path.GetFileName(path)}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds}\");\n            return;\n        }");
        AssertContains(source, "if (!IsPathInSessionDirectory(path))\n            {\n                Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=outside_session path='{Path.GetFileName(path)}'\");\n                return;\n            }");
        AssertContains(source, "if (!File.Exists(path))\n            {\n                Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=missing_file path='{Path.GetFileName(path)}'\");\n                return;\n            }");
        AssertContains(source, "var existingIndex = _completedSegments.FindIndex(seg => IsSameSegmentPath(seg.Path, path));");
        AssertContains(source, "if (existingIndex >= 0)\n            {\n                if (!TryExtendCompletedSegment(existingIndex, path, startPts, endPts, safeSizeBytes, pathIsActiveSegment))");
        AssertContains(source, "private bool TryExtendCompletedSegment(");
        AssertContains(source, "if (!pathIsActiveSegment && !existing.AllowSamePathExtension)");
        AssertContains(source, "AllowSamePathExtension = pathIsActiveSegment");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_EXTEND");
        AssertContains(source, "if (_completedSegments.Count > 0 && startPts < _completedSegments[^1].EndPts)");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=non_monotonic");
        AssertContains(source, "private bool IsPathInSessionDirectory(string path)");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_PATH_WARN");
        AssertContains(source, "var safeSizeBytes = Math.Max(0, sizeBytes);");
        AssertContains(source, "private int _completedSegmentSequence;");
        AssertContains(source, "var sequenceNumber = _completedSegmentSequence++;");
        AssertContains(source, "_completedSegments.Add(new CompletedSegment(path, sequenceNumber, startPts, endPts, safeSizeBytes)\n            {\n                AllowSamePathExtension = pathIsActiveSegment\n            });");
        AssertContains(source, "_completedSegmentBytes = AddNonNegativeSaturated(_completedSegmentBytes, safeSizeBytes);");
        AssertContains(source, "_previousActiveSegmentBytes = pathIsActiveSegment ? safeSizeBytes : 0;");
        AssertContains(source, "_completedSegmentSequence = 0;");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

            var missingSegmentPath = Path.Combine(tempDir, "segment-missing.ts");
            onSegmentCompleted.Invoke(manager, new object[]
            {
                missingSegmentPath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                1000L
            });

            AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Missing segment should not allocate sequence");
            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Missing segment should not update bytes");

            var segment0Path = Path.Combine(tempDir, "segment-0.ts");
            File.WriteAllBytes(segment0Path, new byte[] { 0x47 });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                segment0Path,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                1000L
            });
            var overlappingSegmentPath = Path.Combine(tempDir, "segment-overlap.ts");
            File.WriteAllBytes(overlappingSegmentPath, new byte[] { 0x47 });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", "segment-0.ts"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(6),
                1000L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Duplicate segment path should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Duplicate segment path should not update bytes");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", "segment-0.ts"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(8),
                1500L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Non-active duplicate segment growth should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Non-active duplicate segment growth should not update bytes");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                overlappingSegmentPath,
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(7),
                1000L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Overlapping segment should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Overlapping segment should not update bytes");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentCompletionRejectsOutsidePaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var outsideDir = Path.Combine(Path.GetTempPath(), $"fbtest_outside_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(outsideDir);

        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

            var outsidePath = Path.Combine(outsideDir, "outside.ts");
            onSegmentCompleted.Invoke(manager, new object[]
            {
                outsidePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                1200L
            });

            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Outside segment path should not update bytes");
            AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Outside segment path should not allocate sequence");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
            try { Directory.Delete(outsideDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_TryDeleteFileRejectsOutsidePaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var outsideDir = Path.Combine(Path.GetTempPath(), $"fbdelete_outside_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(outsideDir);

        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var tryDeleteFile = manager.GetType().GetMethod("TryDeleteFile", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackBufferManager.TryDeleteFile not found.");

            var outsidePath = Path.Combine(outsideDir, "outside.ts");
            File.WriteAllText(outsidePath, "keep");

            var result = (bool)tryDeleteFile.Invoke(manager, new object[] { outsidePath })!;
            AssertEqual(false, result, "Outside delete should be rejected");
            AssertEqual(true, File.Exists(outsidePath), "Outside delete should preserve file");

            var source = ReadFlashbackBufferManagerSource();
            AssertContains(source, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session");
            AssertOccursBefore(source, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session", "File.Delete(filePath);");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
            try { Directory.Delete(outsideDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentDiagnosticsClampActiveCounters()
    {
        var source = ReadFlashbackBufferManagerSource();

        AssertContains(source, "var activeEndPts = TimeSpan.FromTicks(Math.Max(activeStartPts.Ticks, Interlocked.Read(ref _latestPtsTicks)));");
        AssertContains(source, "var activeSizeBytes = Math.Max(0, _totalDiskBytes - _completedSegmentBytes);");
        AssertContains(source, "EndPtsMs = (long)activeEndPts.TotalMilliseconds,");
        AssertContains(source, "SizeBytes = activeSizeBytes,");
        AssertContains(source, "var safeActiveSegmentBytes = Math.Max(0, activeSegmentBytes);");
        AssertContains(source, "var accountedActiveSegmentBytes = safeActiveSegmentBytes;");
        AssertContains(source, "accountedActiveSegmentBytes = SubtractNonNegative(safeActiveSegmentBytes, _completedSegments[^1].SizeBytes);");
        AssertContains(source, "_totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, accountedActiveSegmentBytes);");
        AssertContains(source, "_completedSegmentBytes = GetCompletedSegmentBytesSaturated();");
        AssertContains(source, "private long GetCompletedSegmentBytesSaturated()");
        AssertContains(source, "_totalDiskBytes = AddNonNegativeSaturated(_completedSegmentBytes, retainedActiveBytes);");
        AssertContains(source, "freedBytes = AddNonNegativeSaturated(freedBytes, _completedSegments[i].SizeBytes);");
        AssertContains(source, "FLASHBACK_BUFFER_DELETE_WARN path='{filePath}' type={ex.GetType().Name} msg='{ex.Message}'");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_UpdateLatestPts_ClampsInvalidBufferDuration()
    {
        var source = ReadFlashbackBufferManagerSource();
        var cleanupSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
            .Replace("\r\n", "\n");

        AssertContains(source, "var maxTicks = Math.Max(0, _options.BufferDuration.Ticks);");
        AssertContains(source, "var duration = NonNegativeDeltaTicks(ptsTicks, startTicks);");
        AssertContains(source, "var newStartTicks = Math.Max(0, ptsTicks - maxTicks);");
        AssertContains(source, "Interlocked.CompareExchange(ref _validStartPtsTicks, newStartTicks, startTicks);");
        AssertContains(source, "private static long AddNonNegativeSaturated(long left, long right)");
        AssertContains(source, "private static long SubtractNonNegative(long left, long right)");
        AssertContains(source, "private static long NonNegativeDeltaTicks(long latestTicks, long startTicks)");
        AssertContains(source, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(source, "var totalDuration = NonNegativeDeltaTicks(latestTicks, startTicks);");
        AssertContains(source, "var evictTicks = ToNonNegativeLongSaturated(excessBytes / bytesPerTick);");
        AssertContains(source, "var newStart = AddNonNegativeSaturated(Math.Max(0, startTicks), evictTicks);");
        AssertContains(cleanupSource, "directoryBytes = AddNonNegativeSaturated(directoryBytes, file.Length);");
        AssertContains(cleanupSource, "totalCacheBytes = AddNonNegativeSaturated(totalCacheBytes, directoryBytes);");
        AssertContains(cleanupSource, "totalCacheBytes = SubtractNonNegative(totalCacheBytes, candidate.SizeBytes);");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentRotationKeepsTotalBytesWrittenMonotonic()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
        var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
            ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

        updateDiskBytes.Invoke(manager, new object[] { 1000L });
        var completedPath = Path.Combine(tempDir, "completed-0.ts");
        File.WriteAllBytes(completedPath, new byte[] { 0x47 });
        onSegmentCompleted.Invoke(manager, new object[]
        {
            completedPath,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            1200L
        });
        AssertEqual(1200L, GetLongProperty(manager, "TotalBytesWritten"), "Final segment bytes counted at rotation");

        updateDiskBytes.Invoke(manager, new object[] { 100L });
        AssertEqual(1300L, GetLongProperty(manager, "TotalBytesWritten"), "First bytes from next segment counted after rotation");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SamePathCompletionExtendsLatestSegment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var getValidSegmentPaths = manager.GetType().GetMethod("GetValidSegmentPaths")
                ?? throw new InvalidOperationException("FlashbackBufferManager.GetValidSegmentPaths not found.");
            var getSegmentInfoList = manager.GetType().GetMethod("GetSegmentInfoList")
                ?? throw new InvalidOperationException("FlashbackBufferManager.GetSegmentInfoList not found.");

            var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(activePath, new byte[] { 0x47 });

            updateDiskBytes.Invoke(manager, new object[] { 1000L });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                activePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10),
                1000L
            });
            AssertEqual(1000L, GetLongProperty(manager, "TotalDiskBytes"), "Initial same-path completion tracks one physical active file");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Initial same-path completion does not double count active bytes");

            updateDiskBytes.Invoke(manager, new object[] { 1500L });
            AssertEqual(1500L, GetLongProperty(manager, "TotalDiskBytes"), "Same active file growth is counted as a delta after completion");
            AssertEqual(1500L, GetLongProperty(manager, "TotalBytesWritten"), "Same active file growth advances monotonic bytes by delta");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", Path.GetFileName(activePath)),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
                2000L
            });

            var paths = ((IEnumerable<string>)getValidSegmentPaths.Invoke(manager, new object[]
            {
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(19)
            })!).ToArray();
            AssertEqual(1, paths.Length, "Extended same-path segment remains exportable for tail range");
            AssertEqual(activePath, paths[0], "Extended same-path segment export path");
            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Same-path extension keeps original segment sequence");
            AssertEqual(2000L, GetLongProperty(manager, "TotalDiskBytes"), "Extended same-path completion updates completed disk bytes");
            AssertEqual(2000L, GetLongProperty(manager, "TotalBytesWritten"), "Extended same-path completion advances monotonic bytes by growth delta");

            var infos = ((System.Collections.IEnumerable)getSegmentInfoList.Invoke(manager, Array.Empty<object>())!)
                .Cast<object>()
                .ToArray();
            var completedInfo = infos.First(info => GetPropertyValue(info, "IsActive") is false);
            AssertEqual(0L, (long)GetPropertyValue(completedInfo, "StartPtsMs")!, "Extended segment keeps original start");
            AssertEqual(20_000L, (long)GetPropertyValue(completedInfo, "EndPtsMs")!, "Extended segment updates end");
            AssertEqual(2000L, (long)GetPropertyValue(completedInfo, "SizeBytes")!, "Extended segment updates size");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_IgnoresUpdatesAfterDispose()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_disposed_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        var updateLatestPts = manager.GetType().GetMethod("UpdateLatestPts")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateLatestPts not found.");
        var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
        var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
            ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

        ((IDisposable)manager).Dispose();

        updateLatestPts.Invoke(manager, new object[] { TimeSpan.FromSeconds(5) });
        updateDiskBytes.Invoke(manager, new object[] { 4096L });
        onSegmentCompleted.Invoke(manager, new object[]
        {
            Path.Combine(tempDir, "completed-after-dispose.ts"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            1200L
        });

        AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "LatestPts")!, "Disposed manager ignores latest PTS updates");
        AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Disposed manager ignores disk and segment byte updates");
        AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Disposed manager does not allocate segment sequence");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private volatile bool _disposed;");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=disposed");
        AssertContains(source, "public void UpdateLatestPts(TimeSpan pts)\n    {\n        if (_disposed)\n        {\n            return;\n        }");
        AssertContains(source, "public void UpdateDiskBytes(long activeSegmentBytes)\n    {\n        if (_disposed)\n        {\n            return;\n        }");

        try { Directory.Delete(tempDir, recursive: true); } catch { }
        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_IgnoresDestructiveOperationsAfterDispose()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_disposed_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var completedPath = Path.Combine(tempDir, "segment-0.ts");
        var activePath = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(completedPath, "segment");
        File.WriteAllText(activePath, "active");
        AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 7);

        var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");
        var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");
        var abandonGenerated = manager.GetType().GetMethod("AbandonGeneratedSegmentPath")
            ?? throw new InvalidOperationException("FlashbackBufferManager.AbandonGeneratedSegmentPath not found.");
        var finalizeCycle = manager.GetType().GetMethod("FinalizeActiveSegmentForCycle")
            ?? throw new InvalidOperationException("FlashbackBufferManager.FinalizeActiveSegmentForCycle not found.");

        ((IDisposable)manager).Dispose();

        purgeCompleted.Invoke(manager, null);
        purgeAll.Invoke(manager, null);
        abandonGenerated.Invoke(manager, new object?[] { activePath, null });
        finalizeCycle.Invoke(manager, null);

        AssertEqual(false, File.Exists(completedPath), "Dispose purges completed segment before post-dispose purge attempts");
        AssertEqual(false, File.Exists(activePath), "Dispose purges active segment before post-dispose purge attempts");
        AssertEqual(0, GetIntProperty(manager, "SegmentCount"), "Disposed destructive operations keep the disposed empty index stable");
        AssertEqual(string.Empty, GetStringProperty(manager, "ActiveFilePath"), "Disposed destructive operations keep active path cleared");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "FLASHBACK_PURGE_SKIP reason=disposed");
        AssertContains(source, "FLASHBACK_BUFFER_PURGE_SKIP reason=disposed");

        try { Directory.Delete(tempDir, recursive: true); } catch { }
        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_PreservesMarkedRecoverySessions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_recovery_preserve_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var completedPath = Path.Combine(tempDir, "segment-0.ts");
        var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
        File.WriteAllText(completedPath, "segment");
        File.WriteAllText(activePath, "active");
        AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 7);

        var markPreserved = manager.GetType().GetMethod("MarkSessionPreservedForRecovery")
            ?? throw new InvalidOperationException("FlashbackBufferManager.MarkSessionPreservedForRecovery not found.");
        var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");

        markPreserved.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "IsSessionPreservedForRecovery"), "Recovery-preserved manager exposes preserved state");
        SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(2).Ticks);
        InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives normal eviction");

        purgeAll.Invoke(manager, null);

        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives explicit purge");
        AssertEqual(true, File.Exists(activePath), "Recovery-preserved active segment survives explicit purge");

        ((IDisposable)manager).Dispose();

        AssertEqual(true, Directory.Exists(tempDir), "Recovery-preserved session directory survives dispose");
        AssertEqual(true, File.Exists(Path.Combine(tempDir, ".flashback-recovery-preserve")), "Recovery marker survives dispose");
        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives dispose");
        AssertEqual(true, File.Exists(activePath), "Recovery-preserved active segment survives dispose");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private bool _preserveSessionForRecovery;");
        AssertContains(source, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(source, "FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved");
        AssertContains(source, "FLASHBACK_BUFFER_EVICT_SKIP reason=recovery_preserved");
        AssertContains(source, "FLASHBACK_BUFFER_DISPOSE_PRESERVE_RECOVERY");

        try { Directory.Delete(tempDir, recursive: true); } catch { }
        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetValidSegmentFileForPosition_SkipsMissingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingOldest = Path.Combine(tempDir, "missing-oldest.ts");
        var existingFallback = Path.Combine(tempDir, "existing-fallback.ts");
        File.WriteAllText(existingFallback, "segment");

        AddCompletedSegment(manager, missingOldest, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingFallback, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentFileForPosition")!;

        var fallback = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(2) }) as string;
        AssertEqual(existingFallback, fallback!, "Missing target should fall back to first existing completed segment");

        File.Delete(existingFallback);
        var missingAll = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(2) }) as string;
        AssertEqual(null, missingAll, "Missing completed and active segments should return null");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetValidSegmentFileForPosition_StaleLeftEdgeUsesOldest()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var oldest = Path.Combine(tempDir, "oldest.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(oldest, "oldest");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, oldest, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentFileForPosition")!;
        var fallback = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(1) }) as string;

        AssertEqual(oldest, fallback!, "Position before first segment should use oldest existing segment, not active");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetNextSegmentFile_WalksForward()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var a = Path.Combine(tempDir, "a.ts");
        var b = Path.Combine(tempDir, "b.ts");
        var c = Path.Combine(tempDir, "c.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");
        File.WriteAllText(c, "c");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, a, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, b, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, c, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);

        var method = manager.GetType().GetMethod("GetNextSegmentFile")!;

        var nextA = method.Invoke(manager, new object[] { a }) as string;
        AssertEqual(b, nextA!, "a to b");

        var nextB = method.Invoke(manager, new object[] { b }) as string;
        AssertEqual(c, nextB!, "b to c");

        var nextC = method.Invoke(manager, new object[] { c }) as string;
        AssertContains(nextC!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentPathLookupsNormalizeEquivalentPaths()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private static bool IsSameSegmentPath(string? left, string? right)");
        AssertContains(source, "Path.GetFullPath(left)");
        AssertContains(source, "Path.GetFullPath(right)");
        AssertContains(source, "FLASHBACK_BUFFER_PATH_COMPARE_WARN");
        AssertContains(source, "if (IsSameSegmentPath(_completedSegments[i].Path, currentPath))");
        AssertContains(source, "if (IsSameSegmentPath(seg.Path, path) && File.Exists(seg.Path))");
        AssertContains(source, "if (IsSameSegmentPath(_activeSegmentPath, path) &&");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var a = Path.Combine(tempDir, "a.ts");
        var b = Path.Combine(tempDir, "b.ts");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");

        AddCompletedSegment(manager, a, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, b, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var equivalentA = Path.Combine(tempDir, ".", "a.ts");
        var nextMethod = manager.GetType().GetMethod("GetNextSegmentFile")!;
        var next = nextMethod.Invoke(manager, new object[] { equivalentA }) as string;
        AssertEqual(b, next!, "Equivalent completed segment path should walk to next segment");

        var startMethod = manager.GetType().GetMethod("GetSegmentStartPts")!;
        var start = (TimeSpan?)startMethod.Invoke(manager, new object[] { equivalentA });
        AssertEqual(TimeSpan.Zero, start!.Value, "Equivalent completed segment path should resolve start PTS");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetSegmentStartPts_SkipsMissingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetSegmentStartPts")!;

        var missingStart = (TimeSpan?)method.Invoke(manager, new object[] { missingCompleted });
        AssertEqual(null, missingStart, "Missing completed segment should not expose start PTS");

        var existingStart = (TimeSpan?)method.Invoke(manager, new object[] { existingCompleted });
        AssertEqual(TimeSpan.FromSeconds(5), existingStart!.Value, "Existing completed segment should expose start PTS");

        manager.GetType().GetMethod("MarkActiveSegmentStart")!
            .Invoke(manager, new object[] { active, TimeSpan.FromSeconds(12) });
        var activeStart = (TimeSpan?)method.Invoke(manager, new object[] { active });
        AssertEqual(TimeSpan.FromSeconds(12), activeStart!.Value, "Active segment should expose marked encoder start PTS");

        File.Delete(active);
        var missingActiveStart = (TimeSpan?)method.Invoke(manager, new object[] { active });
        AssertEqual(null, missingActiveStart, "Missing active segment should not expose start PTS");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetNextSegmentFile_SkipsMissingIndexedSegments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var current = Path.Combine(tempDir, "current.ts");
        var missingNext = Path.Combine(tempDir, "missing-next.ts");
        var existingNext = Path.Combine(tempDir, "existing-next.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(current, "current");
        File.WriteAllText(existingNext, "next");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, current, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, missingNext, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, existingNext, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);

        var method = manager.GetType().GetMethod("GetNextSegmentFile")!;
        var next = method.Invoke(manager, new object[] { current }) as string;

        AssertEqual(existingNext, next!, "Next segment lookup should skip missing indexed segment");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetValidSegmentPaths_ReturnsOverlapping()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var s0 = Path.Combine(tempDir, "s0.ts");
        var s1 = Path.Combine(tempDir, "s1.ts");
        var s2 = Path.Combine(tempDir, "s2.ts");
        var s3 = Path.Combine(tempDir, "s3.ts");
        File.WriteAllText(s0, "segment");
        File.WriteAllText(s1, "segment");
        File.WriteAllText(s2, "segment");
        File.WriteAllText(s3, "segment");

        AddCompletedSegment(manager, s0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, s1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, s2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);
        AddCompletedSegment(manager, s3, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20), 500);

        var method = manager.GetType().GetMethod("GetValidSegmentPaths")!;

        // Range 3s-12s should include s0 (0-5 overlaps), s1 (5-10), s2 (10-15 overlaps)
        var result = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(12) })!;
        var count = GetCountProperty(result);
        AssertEqual(3, count, "3s-12s should span 3 segments");

        // Range 5s-5.5s should include only s1 (5-10)
        var narrow = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5) })!;
        AssertEqual(1, GetCountProperty(narrow), "5s-5.5s should be 1 segment");

        File.Delete(s1);
        var missing = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5) })!;
        AssertEqual(0, GetCountProperty(missing), "Missing overlapping file should not be returned");

        var emptyRange = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(8) })!;
        AssertEqual(0, GetCountProperty(emptyRange), "Empty range should not return segments");

        var invertedRange = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(3) })!;
        AssertEqual(0, GetCountProperty(invertedRange), "Inverted range should not return segments");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_GetSegmentInfoList_SkipsMissingFiles()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "if (!File.Exists(seg.Path))\n                {\n                    continue;\n                }");
        AssertContains(source, "if (_activeSegmentPath != null && File.Exists(_activeSegmentPath))");
        AssertContains(source, "SequenceNumber = Math.Max(0, _nextSegmentIndex - 1),");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetSegmentInfoList")!;
        var result = method.Invoke(manager, null)!;

        AssertEqual(2, GetCountProperty(result), "Segment info should include existing completed plus active");
        var infos = ((System.Collections.IEnumerable)result).Cast<object>().ToArray();
        var activeInfo = infos.Single(info => GetBoolProperty(info, "IsActive"));
        AssertEqual(3, GetIntProperty(activeInfo, "SequenceNumber"), "Active segment sequence should match current generated segment index");
        AssertEqual(10_000L, GetLongProperty(activeInfo, "StartPtsMs"), "Unmarked active segment start should fall back to completed end");

        manager.GetType().GetMethod("MarkActiveSegmentStart")!
            .Invoke(manager, new object[] { active, TimeSpan.FromSeconds(12) });
        var markedResult = method.Invoke(manager, null)!;
        var markedActiveInfo = ((System.Collections.IEnumerable)markedResult)
            .Cast<object>()
            .Single(info => GetBoolProperty(info, "IsActive"));
        AssertEqual(12_000L, GetLongProperty(markedActiveInfo, "StartPtsMs"), "Marked active segment start should follow encoder PTS");

        File.Delete(active);
        var withoutActive = method.Invoke(manager, null)!;

        AssertEqual(1, GetCountProperty(withoutActive), "Segment info should omit missing active file");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_ActiveFilePath_RequiresExistingFile()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "return _activeSegmentPath != null && File.Exists(_activeSegmentPath)\n                    ? _activeSegmentPath\n                    : null;");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        AssertEqual(null, GetPropertyValue(manager, "ActiveFilePath"), "Missing active file should not be exposed");

        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(active, "active");

        AssertEqual(active, (string)GetPropertyValue(manager, "ActiveFilePath")!, "Existing active file should be exposed");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_SegmentCount_SkipsMissingFiles()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "return _completedSegments.Count(seg => File.Exists(seg.Path)) +\n                    (_activeSegmentPath != null && File.Exists(_activeSegmentPath) ? 1 : 0);");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        AssertEqual(2, GetIntProperty(manager, "SegmentCount"), "Segment count should include existing completed plus active");

        File.Delete(active);

        AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Segment count should omit missing active file");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_EvictOldestSegments_UpdatesTotalDiskBytes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_evict_bytes_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var firstSegment = Path.Combine(tempDir, "seg0.ts");
            var secondSegment = Path.Combine(tempDir, "seg1.ts");
            var activeSegment = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(firstSegment, new byte[100]);
            File.WriteAllBytes(secondSegment, new byte[200]);
            File.WriteAllBytes(activeSegment, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                firstSegment,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                secondSegment,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });

            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Setup should track completed and active bytes");

            SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(1).Ticks);
            InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

            var deleteDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (File.Exists(firstSegment) && DateTime.UtcNow < deleteDeadline)
            {
                Thread.Sleep(25);
            }

            AssertEqual(false, File.Exists(firstSegment), "Eviction should delete the expired completed segment");
            AssertEqual(true, File.Exists(secondSegment), "Eviction should retain overlapping completed segment");
            AssertEqual(250L, GetLongProperty(manager, "TotalDiskBytes"), "Eviction subtracts deleted completed segment bytes");
            AssertEqual(200L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Completed byte cache matches retained segment");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_EvictOldestSegments_KeepsRejectedSegmentsAccounted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_evict_locked_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var firstSegment = Path.Combine(tempDir, "seg0.ts");
            var secondSegment = Path.Combine(tempDir, "seg1.ts");
            var activeSegment = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(firstSegment, new byte[100]);
            File.WriteAllBytes(secondSegment, new byte[200]);
            File.WriteAllBytes(activeSegment, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                firstSegment,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                secondSegment,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });

            SetPrivateField(manager, "_sessionDirectory", Path.Combine(tempDir, "different-session"));
            SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(1).Ticks);
            InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

            AssertEqual(true, File.Exists(firstSegment), "Rejected expired segment remains on disk");
            AssertEqual(true, File.Exists(secondSegment), "Later segment is not evicted past a rejected predecessor");
            AssertEqual(3, GetIntProperty(manager, "SegmentCount"), "Rejected completed segments remain tracked with active segment");
            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Rejected segment bytes stay in disk accounting");
            AssertEqual(300L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Completed byte cache retains rejected segment");

            var source = ReadFlashbackBufferManagerSource();
            AssertContains(source, "if (DeleteFileForEviction(oldest.Path, oldest.SizeBytes, \"valid_window\"))");
            AssertContains(source, "private static bool DeleteEvictedFile");
            AssertContains(source, "FLASHBACK_BUFFER_EVICT_DELETE_WARN");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_EvictionPauseResume_Balanced()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        var pauseMethod = manager.GetType().GetMethod("PauseEviction")!;
        var resumeMethod = manager.GetType().GetMethod("ResumeEviction")!;

        // Initially not paused
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "Initial EvictionPaused");

        // Pause → paused
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 pause");

        // Double-pause → still paused (count-based)
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 2 pauses");

        // Resume once → still paused (count = 1)
        resumeMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 resume (count=1)");

        // Resume again → unpaused (count = 0)
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After 2 resumes (count=0)");

        // Extra resume → remains unpaused and must not underflow the pause counter.
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After unbalanced resume");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED");
        AssertContains(source, "var unbalancedEndPts = ClampEndPtsToStart(_recordingStartPts, _recordingEndPts);");
        AssertContains(source, "_recordingEndPts = ClampEndPtsToStart(\n                    _recordingStartPts,\n                    TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)));");
        AssertContains(source, "var rangeSeconds = TimeSpan.FromTicks(NonNegativeDeltaTicks(_recordingEndPts.Ticks, _recordingStartPts.Ticks)).TotalSeconds;");
        AssertContains(source, "private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)");
        AssertDoesNotContain(source, "range_s={(_recordingEndPts - _recordingStartPts).TotalSeconds:F1}");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_startup_abandon_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            SetPrivateField(manager, "_activeSegmentPath", null);
            var startingIndex = (int)GetPrivateField(manager, "_nextSegmentIndex")!;
            var getFilePath = manager.GetType().GetMethod("AcquireSegmentPath", new[] { typeof(bool).MakeByRefType() })
                ?? throw new InvalidOperationException("FlashbackBufferManager.AcquireSegmentPath(out bool) not found.");
            var abandonGenerated = manager.GetType().GetMethod("AbandonGeneratedSegmentPath")
                ?? throw new InvalidOperationException("FlashbackBufferManager.AbandonGeneratedSegmentPath not found.");

            object?[] args = { false };
            var generatedPath = (string)getFilePath.Invoke(manager, args)!;
            AssertEqual(true, (bool)args[0]!, "Fresh AcquireSegmentPath reports generated path");
            AssertEqual(generatedPath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Generated path becomes raw active segment");
            AssertEqual(startingIndex + 1, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Generated path advances segment index");

            File.WriteAllBytes(generatedPath, new byte[17]);
            abandonGenerated.Invoke(manager, new object?[] { generatedPath, null });

            AssertEqual<string?>(null, (string?)GetPrivateField(manager, "_activeSegmentPath"), "Abandon clears startup-generated active path");
            AssertEqual(false, File.Exists(generatedPath), "Abandon deletes partial startup segment file");
            AssertEqual(startingIndex, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Abandon rolls back generated segment index");

            object?[] retryArgs = { false };
            var retryPath = (string)getFilePath.Invoke(manager, retryArgs)!;
            AssertEqual(true, (bool)retryArgs[0]!, "Retry after abandon generates a fresh path");
            AssertEqual(generatedPath, retryPath, "Retry reuses the rolled-back segment slot");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_RemovesStaleLegacyRootSegments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_legacy_cleanup_{Guid.NewGuid():N}");
        object? manager = null;
        Directory.CreateDirectory(tempDir);

        try
        {
            var staleRootSegment = Path.Combine(tempDir, "fb_legacy_0001.ts");
            var recentRootSegment = Path.Combine(tempDir, "fb_recent_0001.ts");
            var unrelatedFile = Path.Combine(tempDir, "unrelated.ts");
            File.WriteAllText(staleRootSegment, "stale");
            File.WriteAllText(recentRootSegment, "recent");
            File.WriteAllText(unrelatedFile, "keep");

            File.SetLastWriteTimeUtc(staleRootSegment, DateTime.UtcNow - TimeSpan.FromHours(13));
            File.SetLastWriteTimeUtc(recentRootSegment, DateTime.UtcNow);
            File.SetLastWriteTimeUtc(unrelatedFile, DateTime.UtcNow - TimeSpan.FromHours(13));

            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            manager = Activator.CreateInstance(managerType, new[] { options })!;
            var initialize = managerType.GetMethod("Initialize")
                ?? throw new InvalidOperationException("FlashbackBufferManager.Initialize not found.");
            initialize.Invoke(manager, new object[] { "current-session" });

            AssertEqual(false, File.Exists(staleRootSegment), "Stale root fb_* segment removed");
            AssertEqual(true, File.Exists(recentRootSegment), "Recent root fb_* segment preserved");
            AssertEqual(true, File.Exists(unrelatedFile), "Unrelated root file preserved");
            AssertEqual(true, Directory.Exists(Path.Combine(tempDir, "current-session")), "Current session directory created");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_PurgesRetainLockedActivePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_locked_active_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);
        string? activePath = null;

        try
        {
            activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(activePath, new byte[50]);
            File.SetAttributes(activePath, File.GetAttributes(activePath) | FileAttributes.ReadOnly);

            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");
            var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");

            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Setup tracks active bytes");
            AssertEqual(50L, GetLongProperty(manager, "TotalBytesWritten"), "Setup tracks active bytes written");

            purgeCompleted.Invoke(manager, null);

            AssertEqual(true, File.Exists(activePath), "Read-only active file remains on disk");
            AssertEqual(activePath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Read-only active path remains tracked");
            AssertEqual(activePath, GetStringProperty(manager, "ActiveFilePath"), "ActiveFilePath still reports read-only active segment");
            AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Segment count still includes read-only active segment");
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Read-only active bytes remain in disk accounting");
            AssertEqual(50L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Read-only active byte baseline is preserved");

            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(50L, GetLongProperty(manager, "TotalBytesWritten"), "Same active bytes are not double-counted after failed purge");

            purgeAll.Invoke(manager, null);
            AssertEqual(true, File.Exists(activePath), "Read-only active file remains after full purge attempt");
            AssertEqual(activePath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Full purge keeps read-only active path tracked");
            AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Full purge segment count still includes read-only active segment");
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Full purge keeps read-only active bytes in disk accounting");
            AssertEqual(50L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Full purge keeps read-only active byte baseline");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(activePath) && File.Exists(activePath))
            {
                try { File.SetAttributes(activePath, FileAttributes.Normal); } catch { }
            }

            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_PurgeAllSegmentsCore_ReportsActiveBytesOnce()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_full_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var completedPath = Path.Combine(tempDir, "completed.ts");
            var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(completedPath, new byte[300]);
            File.WriteAllBytes(activePath, new byte[50]);
            AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 300L);
            SetPrivateField(manager, "_completedSegmentBytes", 300L);
            SetPrivateField(manager, "_totalDiskBytes", 350L);

            var purgeCore = manager.GetType().GetMethod("PurgeAllSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegmentsCore not found.");
            var result = purgeCore.Invoke(manager, null)!;
            var segments = Convert.ToInt32(result.GetType().GetField("Item1")!.GetValue(result));
            var freedBytes = Convert.ToInt64(result.GetType().GetField("Item2")!.GetValue(result));

            AssertEqual(2, segments, "Full purge reports completed plus active segment");
            AssertEqual(350L, freedBytes, "Full purge reports completed plus active bytes exactly once");
            AssertEqual(false, File.Exists(completedPath), "Full purge deletes completed segment");
            AssertEqual(false, File.Exists(activePath), "Full purge deletes active segment");
            AssertEqual(0L, GetLongProperty(manager, "TotalDiskBytes"), "Full purge resets total disk bytes");
            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Full purge resets monotonic bytes for a new buffer session");

            var source = ReadFlashbackBufferManagerSource();
            var purgeCoreBlock = ExtractTextBetween(
                source,
                "private (int Segments, long FreedBytes) PurgeAllSegmentsCore()",
                "    private void EvictOldestSegments()");
            AssertOccursBefore(purgeCoreBlock, "var activeBytes = _activeSegmentPath != null", "if (_activeSegmentPath != null)");
            AssertContains(purgeCoreBlock, "_completedSegmentBytes = GetCompletedSegmentBytesSaturated();");
            AssertContains(purgeCoreBlock, "var retainedActiveBytes = _activeSegmentPath != null ? activeBytes : 0;");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_PurgeCompletedSegments_AccountsForActiveBytesOnPartialPurge()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_partial_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);
        FileStream? lockedCompleted = null;

        try
        {
            var completedPath = Path.Combine(tempDir, "completed-locked.ts");
            var deletableCompletedPath = Path.Combine(tempDir, "completed-deletable.ts");
            var activePath = Path.Combine(tempDir, "fb_test_0003.ts");
            File.WriteAllBytes(completedPath, new byte[100]);
            File.WriteAllBytes(deletableCompletedPath, new byte[200]);
            File.WriteAllBytes(activePath, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                completedPath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                deletableCompletedPath,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Setup should track completed plus active bytes");

            lockedCompleted = new FileStream(completedPath, FileMode.Open, FileAccess.Read, FileShare.None);
            purgeCompleted.Invoke(manager, null);

            AssertEqual(false, File.Exists(activePath), "Partial purge should still delete stale active segment");
            AssertEqual(false, File.Exists(deletableCompletedPath), "Partial purge deletes unlocked completed segments");
            AssertEqual(true, File.Exists(completedPath), "Partial purge retains locked completed segments");
            AssertEqual(100L, GetLongProperty(manager, "TotalDiskBytes"), "Partial purge subtracts deleted completed and active bytes");
            AssertEqual(100L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Partial purge preserves retained completed byte accounting");
            AssertEqual(0L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Partial purge resets active byte baseline");

            updateDiskBytes.Invoke(manager, new object[] { 25L });
            AssertEqual(125L, GetLongProperty(manager, "TotalDiskBytes"), "Next active bytes are added to retained completed bytes");
            AssertEqual(375L, GetLongProperty(manager, "TotalBytesWritten"), "Next active segment bytes are counted after purge baseline reset");
        }
        finally
        {
            lockedCompleted?.Dispose();
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    // ── GpuPipelineHandles / RecordingContextRequest tests ──

    private static Task FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_stale_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var staleFlashbackSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var unrelatedEmptyDirectory = Path.Combine(tempDir, "empty-but-not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(staleFlashbackSession);
            Directory.CreateDirectory(unrelatedEmptyDirectory);

            var staleTime = DateTime.UtcNow - TimeSpan.FromHours(13);
            Directory.SetLastWriteTimeUtc(staleFlashbackSession, staleTime);
            Directory.SetLastWriteTimeUtc(unrelatedEmptyDirectory, staleTime);

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupCacheCleanup");
            var cleanup = cleanupType.GetMethod("CleanupStaleSessionDirectories", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupStaleSessionDirectories not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession });

            AssertEqual(true, Directory.Exists(currentSession), "Current empty session directory preserved");
            AssertEqual(false, Directory.Exists(staleFlashbackSession), "Plausible stale empty flashback session removed");
            AssertEqual(true, Directory.Exists(unrelatedEmptyDirectory), "Unrelated stale empty directory preserved");

            var cleanupSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Sussudio", "Services", "Flashback", "FlashbackStartupCacheCleanup.cs"))
                .Replace("\r\n", "\n");
            var scannerSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Sussudio", "Services", "Flashback", "FlashbackSessionRecoveryScanner.cs"))
                .Replace("\r\n", "\n");
            AssertContains(cleanupSource, "FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir");
            AssertContains(scannerSource, "internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)");
            AssertContains(scannerSource, "internal static bool IsLowerHexString(ReadOnlySpan<char> value)");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_TrimsStartupSessionCacheBudget()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_cache_budget_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, "current-session");
            var oldSession = Path.Combine(tempDir, "old-session");
            var recentSession = Path.Combine(tempDir, "recent-session");
            var preservedSession = Path.Combine(tempDir, "preserved-session");
            var nonFlashbackDirectory = Path.Combine(tempDir, "not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(oldSession);
            Directory.CreateDirectory(recentSession);
            Directory.CreateDirectory(preservedSession);
            Directory.CreateDirectory(nonFlashbackDirectory);

            WriteSizedFile(Path.Combine(currentSession, "fb_current_0001.ts"), 1);
            WriteSizedFile(Path.Combine(oldSession, "fb_old_0001.ts"), 20);
            WriteSizedFile(Path.Combine(recentSession, "fb_recent_0001.ts"), 10);
            WriteSizedFile(Path.Combine(preservedSession, "fb_preserved_0001.ts"), 100);
            File.WriteAllText(Path.Combine(preservedSession, ".flashback-recovery-preserve"), "keep");
            File.WriteAllText(Path.Combine(nonFlashbackDirectory, "notes.txt"), "keep");

            var now = DateTime.UtcNow;
            File.SetLastWriteTimeUtc(Path.Combine(oldSession, "fb_old_0001.ts"), now - TimeSpan.FromHours(2));
            File.SetLastWriteTimeUtc(Path.Combine(recentSession, "fb_recent_0001.ts"), now - TimeSpan.FromMinutes(5));

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupCacheCleanup");
            var cleanup = cleanupType.GetMethod("CleanupSessionCacheBudget", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupSessionCacheBudget not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession, 25L });

            AssertEqual(true, Directory.Exists(currentSession), "Current session preserved");
            AssertEqual(false, Directory.Exists(oldSession), "Oldest session removed to satisfy budget");
            AssertEqual(true, Directory.Exists(recentSession), "Recent session preserved once budget is satisfied");
            AssertEqual(true, Directory.Exists(preservedSession), "Recovery-preserved session skipped");
            AssertEqual(true, Directory.Exists(nonFlashbackDirectory), "Non-flashback directory preserved");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_RejectsUnsafeSessionIds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_session_id_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            using var manager = (IDisposable)Activator.CreateInstance(managerType, new[] { options })!;
            var initialize = managerType.GetMethod("Initialize")
                ?? throw new InvalidOperationException("FlashbackBufferManager.Initialize not found.");

            try
            {
                initialize.Invoke(manager, new object[] { "..\\outside-session" });
                throw new InvalidOperationException("Expected unsafe session id to be rejected.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
            {
            }

            AssertEqual(false, Directory.Exists(Path.Combine(Directory.GetParent(tempDir)!.FullName, "outside-session")), "Unsafe session id must not create outside directory");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_ValidatesSegmentExtensions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_segment_ext_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            using var manager = (IDisposable)Activator.CreateInstance(managerType, new[] { options })!;
            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "safe-session" });

            var setExtension = managerType.GetMethod("SetSegmentExtension")
                ?? throw new InvalidOperationException("SetSegmentExtension not found.");
            var generatePath = managerType.GetMethod("GenerateSegmentPath")
                ?? throw new InvalidOperationException("GenerateSegmentPath not found.");

            setExtension.Invoke(manager, new object[] { ".TS" });
            var transportPath = (string)generatePath.Invoke(manager, null)!;
            AssertEqual(true, transportPath.EndsWith(".ts", StringComparison.Ordinal), "Transport stream extension normalized");

            setExtension.Invoke(manager, new object[] { ".Mp4" });
            var mp4Path = (string)generatePath.Invoke(manager, null)!;
            AssertEqual(true, mp4Path.EndsWith(".mp4", StringComparison.Ordinal), "MP4 extension normalized");

            try
            {
                setExtension.Invoke(manager, new object[] { "..\\escape.ts" });
                throw new InvalidOperationException("Expected unsafe segment extension to be rejected.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
            {
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static void WriteSizedFile(string path, int byteCount)
    {
        File.WriteAllBytes(path, Enumerable.Repeat((byte)0x47, byteCount).ToArray());
    }

    private static Task GpuPipelineHandles_None_ReturnsZeroedStruct()
    {
        var handlesType = RequireType("Sussudio.Services.Contracts.GpuPipelineHandles");
        var noneProp = handlesType.GetProperty("None", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("GpuPipelineHandles.None not found");
        var none = noneProp.GetValue(null)!;

        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "D3D11DevicePtr")!, "D3D11DevicePtr");
        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "D3D11DeviceContextPtr")!, "D3D11DeviceContextPtr");
        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "CudaHwDeviceCtxPtr")!, "CudaHwDeviceCtxPtr");
        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "CudaHwFramesCtxPtr")!, "CudaHwFramesCtxPtr");

        return Task.CompletedTask;
    }

    private static Task RecordingContextRequest_DefaultsMatchRecordingContextDefaults()
    {
        var request = CreateInstance("Sussudio.Services.Contracts.RecordingContextRequest");
        AssertEqual("30", GetStringProperty(request, "FrameRateArg"), "FrameRateArg default");
        AssertEqual("nv12", GetStringProperty(request, "VideoInputPixelFormat"), "VideoInputPixelFormat default");
        AssertEqual(false, GetBoolProperty(request, "IsFullRangeInput"), "IsFullRangeInput default");
        AssertEqual(false, GetBoolProperty(request, "UsePostMuxAudio"), "UsePostMuxAudio default");

        return Task.CompletedTask;
    }

    // --- MediaFormat tests ---

    private static Task MediaFormat_Equality_WithMatchingRationalFrameRates()
    {
        var a = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(a, "Width", 1920u);
        SetPropertyOrBackingField(a, "Height", 1080u);
        SetPropertyOrBackingField(a, "FrameRateNumerator", 60000u);
        SetPropertyOrBackingField(a, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(a, "PixelFormat", "NV12");
        SetPropertyOrBackingField(a, "IsHdr", false);

        var b = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(b, "Width", 1920u);
        SetPropertyOrBackingField(b, "Height", 1080u);
        SetPropertyOrBackingField(b, "FrameRateNumerator", 60000u);
        SetPropertyOrBackingField(b, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(b, "PixelFormat", "NV12");
        SetPropertyOrBackingField(b, "IsHdr", false);

        AssertEqual(true, a.Equals(b), "MediaFormat rational equality");
        return Task.CompletedTask;
    }

    private static Task MediaFormat_Inequality_WhenDimensionsDiffer()
    {
        var a = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(a, "Width", 1920u);
        SetPropertyOrBackingField(a, "Height", 1080u);
        SetPropertyOrBackingField(a, "FrameRate", 60.0);
        SetPropertyOrBackingField(a, "PixelFormat", "NV12");
        SetPropertyOrBackingField(a, "IsHdr", false);

        var b = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(b, "Width", 3840u);
        SetPropertyOrBackingField(b, "Height", 2160u);
        SetPropertyOrBackingField(b, "FrameRate", 60.0);
        SetPropertyOrBackingField(b, "PixelFormat", "NV12");
        SetPropertyOrBackingField(b, "IsHdr", false);

        AssertEqual(false, a.Equals(b), "MediaFormat dimension inequality");
        return Task.CompletedTask;
    }

    private static Task MediaFormat_GetHashCode_ConsistencyForEqualObjects()
    {
        var a = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(a, "Width", 3840u);
        SetPropertyOrBackingField(a, "Height", 2160u);
        SetPropertyOrBackingField(a, "FrameRateNumerator", 120000u);
        SetPropertyOrBackingField(a, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(a, "PixelFormat", "P010");
        SetPropertyOrBackingField(a, "IsHdr", true);

        var b = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(b, "Width", 3840u);
        SetPropertyOrBackingField(b, "Height", 2160u);
        SetPropertyOrBackingField(b, "FrameRateNumerator", 120000u);
        SetPropertyOrBackingField(b, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(b, "PixelFormat", "P010");
        SetPropertyOrBackingField(b, "IsHdr", true);

        AssertEqual(a.GetHashCode(), b.GetHashCode(), "MediaFormat hash consistency");
        return Task.CompletedTask;
    }

    // --- AutomationContracts tests ---

    private static Task AutomationCommandKind_PreservesNumericValuesThroughGetAutomationManifest()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var expectedCommands = ExpectedAutomationCommands();
        AssertEqual(expectedCommands.Length, Enum.GetValues(enumType).Length, "AutomationCommandKind value count");

        for (int i = 0; i < expectedCommands.Length; i++)
        {
            var (name, value) = expectedCommands[i];
            var parsed = Enum.Parse(enumType, name);
            AssertEqual(value, Convert.ToInt32(parsed), $"AutomationCommandKind.{name}");
            if (!Enum.IsDefined(enumType, value))
            {
                throw new InvalidOperationException(
                    $"AutomationCommandKind missing sequential value {value}.");
            }
        }

        return Task.CompletedTask;
    }

    private static (string Name, int Value)[] ExpectedAutomationCommands() =>
    [
        ("Authenticate", 0),
        ("GetSnapshot", 1),
        ("GetDiagnostics", 2),
        ("RefreshDevices", 3),
        ("SelectDevice", 4),
        ("SelectAudioInputDevice", 5),
        ("SetCustomAudioInput", 6),
        ("SetResolution", 7),
        ("SetFrameRate", 8),
        ("SetRecordingFormat", 9),
        ("SetQuality", 10),
        ("SetCustomBitrate", 11),
        ("SetHdrEnabled", 12),
        ("SetAudioEnabled", 13),
        ("SetAudioPreviewEnabled", 14),
        ("SetOutputPath", 15),
        ("SetPreviewEnabled", 16),
        ("SetRecordingEnabled", 17),
        ("ArmClose", 18),
        ("WindowAction", 19),
        ("WaitForCondition", 20),
        ("VerifyLastRecording", 21),
        ("AssertSnapshot", 22),
        ("SetTrueHdrPreviewEnabled", 23),
        ("ProbeVideoSource", 24),
        ("ProbePreviewColor", 25),
        ("CapturePreviewFrame", 26),
        ("CaptureWindowScreenshot", 27),
        ("SetVideoFormat", 28),
        ("GetCaptureOptions", 29),
        ("SetPreset", 30),
        ("SetSplitEncodeMode", 31),
        ("SetMjpegDecoderCount", 32),
        ("SetShowAllCaptureOptions", 33),
        ("SetPreviewVolume", 34),
        ("SetStatsVisible", 35),
        ("SetDeviceAudioMode", 36),
        ("GetPerformanceTimeline", 37),
        ("SetStatsSectionVisible", 38),
        ("SetAnalogAudioGain", 39),
        ("SetSettingsVisible", 40),
        ("FlashbackAction", 41),
        ("FlashbackExport", 42),
        ("FlashbackGetSegments", 43),
        ("VerifyFile", 44),
        ("RestartFlashback", 45),
        ("SetMicrophoneEnabled", 46),
        ("SetFlashbackEnabled", 47),
        ("GetAudioRampTrace", 48),
        ("SetFrameTimeOverlayVisible", 49),
        ("SetFlashbackTimelineVisible", 50),
        ("GetAutomationManifest", 51),
        ("SetFullScreenEnabled", 52),
        ("OpenRecordingsFolder", 53)
    ];

    private static Task AutomationWindowAction_HasExpectedValues()
    {
        var enumType = RequireType("Sussudio.Models.AutomationWindowAction");
        var names = Enum.GetNames(enumType);

        // Verify expected members exist
        var expectedNames = new[]
        {
            "Minimize", "Maximize", "Restore", "Close",
            "SnapLeft", "SnapRight", "SnapTopLeft", "SnapTopRight",
            "SnapBottomLeft", "SnapBottomRight", "Center", "Move", "Resize"
        };
        AssertEqual(expectedNames.Length, names.Length, "AutomationWindowAction count");

        foreach (var expected in expectedNames)
        {
            if (!Enum.IsDefined(enumType, Enum.Parse(enumType, expected)))
                throw new InvalidOperationException(
                    $"AutomationWindowAction missing expected value '{expected}'.");
        }

        return Task.CompletedTask;
    }

    // --- RuntimePaths tests ---

    private static Task RuntimePaths_GetRepoLogFile_ReturnsPathUnderRepoRoot()
    {
        var runtimePathsType = RequireType("Sussudio.RuntimePaths");
        var getRepoLogFile = runtimePathsType.GetMethod(
            "GetRepoLogFile",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        if (getRepoLogFile == null)
            throw new InvalidOperationException("RuntimePaths.GetRepoLogFile not found.");

        var logPath = (string)getRepoLogFile.Invoke(null, new object[] { "test.log" })!;
        AssertContains(logPath, "test.log");

        // The log path should be a rooted path
        if (!Path.IsPathRooted(logPath))
            throw new InvalidOperationException(
                $"GetRepoLogFile returned non-rooted path: {logPath}");

        return Task.CompletedTask;
    }

    private static Task RuntimePaths_PathsContainExpectedDirectoryNames()
    {
        var runtimePathsType = RequireType("Sussudio.RuntimePaths");

        var getRepoLogRoot = runtimePathsType.GetMethod(
            "GetRepoLogRoot", BindingFlags.Public | BindingFlags.Static);
        if (getRepoLogRoot == null)
            throw new InvalidOperationException("RuntimePaths.GetRepoLogRoot not found.");
        var logRoot = (string)getRepoLogRoot.Invoke(null, null)!;
        AssertContains(logRoot, "logs");

        var getRepoTempRoot = runtimePathsType.GetMethod(
            "GetRepoTempRoot", BindingFlags.Public | BindingFlags.Static);
        if (getRepoTempRoot == null)
            throw new InvalidOperationException("RuntimePaths.GetRepoTempRoot not found.");
        var tempRoot = (string)getRepoTempRoot.Invoke(null, null)!;
        AssertContains(tempRoot, "temp");

        return Task.CompletedTask;
    }

    private static Task MmcssThreadRegistration_UsesUnicodeAvrtEntryPoint()
    {
        var source = ReadRepoFile("Sussudio/Services/Runtime/MmcssThreadRegistration.cs");
        AssertContains(source, "EntryPoint = \"AvSetMmThreadCharacteristicsW\"");
        AssertContains(source, "MMCSS registered task=");

        return Task.CompletedTask;
    }

    // --- SourceSignalTelemetrySnapshot tests ---

    private static Task SourceSignalTelemetrySnapshot_DefaultsHaveExpectedValues()
    {
        var type = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var instance = RuntimeHelpers.GetUninitializedObject(type);

        // Uninitialized record: nullable properties should be default (null for nullable, 0 for value types)
        // Use the factory method to test proper defaults
        var createMethod = type.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!;
        var snapshot = createMethod.Invoke(null, new object?[] { "test-reason", null })!;

        AssertEqual("Unavailable",
            GetStringProperty(snapshot, "Availability"),
            "CreateUnavailable Availability");
        AssertEqual("Unknown",
            GetStringProperty(snapshot, "Origin"),
            "CreateUnavailable Origin");
        AssertEqual("Unavailable",
            GetStringProperty(snapshot, "OriginDetail"),
            "CreateUnavailable OriginDetail");
        AssertContains(GetStringProperty(snapshot, "DiagnosticSummary"), "test-reason");

        return Task.CompletedTask;
    }

    private static Task SourceSignalTelemetrySnapshot_PropertiesRoundTrip()
    {
        var type = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);

        SetPropertyBackingField(snapshot, "Width", (int?)1920);
        SetPropertyBackingField(snapshot, "Height", (int?)1080);
        SetPropertyBackingField(snapshot, "FrameRateExact", (double?)59.94);
        SetPropertyBackingField(snapshot, "IsHdr", (bool?)true);
        SetPropertyBackingField(snapshot, "VideoFormat", "P010");
        SetPropertyBackingField(snapshot, "Firmware", "1.2.3");

        AssertEqual(1920, GetIntProperty(snapshot, "Width"), "Width round-trip");
        AssertEqual(1080, GetIntProperty(snapshot, "Height"), "Height round-trip");
        AssertEqual("P010", GetStringProperty(snapshot, "VideoFormat"), "VideoFormat round-trip");
        AssertEqual("1.2.3", GetStringProperty(snapshot, "Firmware"), "Firmware round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "IsHdr"), "IsHdr round-trip");

        return Task.CompletedTask;
    }

    // ── HdrOutputPolicy tests ──

    private static Task HdrOutputPolicy_ReturnsTrue_WhenHdrAndHdr10PqRequested()
    {
        var previousForceOff = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF");
        try
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", null);
            var result = InvokeHdrOutputPolicy(hdrEnabled: true, hdrOutputMode: "Hdr10Pq");
            AssertEqual(true, result, "HDR enabled + Hdr10Pq should return true");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", previousForceOff);
        }

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_ReturnsFalse_WhenHdrDisabled()
    {
        var result = InvokeHdrOutputPolicy(hdrEnabled: false, hdrOutputMode: "Hdr10Pq");
        AssertEqual(false, result, "HDR disabled should return false");

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_ReturnsFalse_WhenNotHdr10Pq()
    {
        var result = InvokeHdrOutputPolicy(hdrEnabled: true, hdrOutputMode: "Off");
        AssertEqual(false, result, "HdrOutputMode=Off should return false");

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_ReturnsFalse_WhenForceOffEnvSet()
    {
        var previousForceOff = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF");
        try
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", "true");
            var result = InvokeHdrOutputPolicy(hdrEnabled: true, hdrOutputMode: "Hdr10Pq");
            AssertEqual(false, result, "force-off env switch should disable HDR output");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", previousForceOff);
        }

        return Task.CompletedTask;
    }

    private static Task HdrOutputPolicy_IgnoresLegacyEnabledEnvSwitch()
    {
        var previousForceOff = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF");
        var previousLegacyEnabled = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_ENABLED");
        try
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", null);
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_ENABLED", "false");
            var result = InvokeHdrOutputPolicy(hdrEnabled: true, hdrOutputMode: "Hdr10Pq");
            AssertEqual(true, result, "legacy enabled env switch should no longer disable HDR output");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", previousForceOff);
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_ENABLED", previousLegacyEnabled);
        }

        return Task.CompletedTask;
    }

    private static bool InvokeHdrOutputPolicy(bool hdrEnabled, string hdrOutputMode)
    {
        var policyType = RequireType("Sussudio.Services.Capture.HdrOutputPolicy");
        var method = policyType.GetMethod("IsEnabled", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("HdrOutputPolicy.IsEnabled not found");

        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "HdrEnabled", hdrEnabled);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("Sussudio.Models.HdrOutputMode", hdrOutputMode));

        return (bool)method.Invoke(null, new[] { settings })!;
    }

    // ── FlashbackPlaybackState enum test ──

    private static Task FlashbackPlaybackState_HasAllExpectedStates()
    {
        var enumType = RequireType("Sussudio.Models.FlashbackPlaybackState");
        var names = Enum.GetNames(enumType);

        // Expected states from the state machine design
        var expected = new HashSet<string> { "Disabled", "Buffering", "Live", "Scrubbing", "Playing", "Paused" };
        foreach (var name in expected)
        {
            if (!names.Contains(name))
                throw new InvalidOperationException($"Missing FlashbackPlaybackState: {name}");
        }

        AssertEqual(expected.Count, names.Length, "FlashbackPlaybackState count");

        return Task.CompletedTask;
    }

    // ── RecordingPipelineOptions / NvmlSnapshot / Coordinator / ProcessSpec tests ──

    private static Task RecordingPipelineOptions_ResolvesVideoQueueCapacity()
    {
        var options = CreateInstance("Sussudio.Models.RecordingPipelineOptions");

        // Default: 250ms latency, min=4, max=30
        // At 60fps: ceil(60 * 250 / 1000) = ceil(15) = 15 → clamp(15, 4, 30) = 15
        var method = options.GetType().GetMethod("ResolveVideoQueueCapacity")!;
        var at60 = (int)method.Invoke(options, new object[] { 60.0 })!;
        AssertEqual(15, at60, "60fps default latency");

        // At 120fps: ceil(120 * 250 / 1000) = ceil(30) = 30 → clamp(30, 4, 30) = 30
        var at120 = (int)method.Invoke(options, new object[] { 120.0 })!;
        AssertEqual(30, at120, "120fps default latency");

        // At 30fps: ceil(30 * 250 / 1000) = ceil(7.5) = 8 → clamp(8, 4, 30) = 8
        var at30 = (int)method.Invoke(options, new object[] { 30.0 })!;
        AssertEqual(8, at30, "30fps default latency");

        // Zero fps falls back to 60fps: ceil(60 * 250 / 1000) = 15
        var atZero = (int)method.Invoke(options, new object[] { 0.0 })!;
        AssertEqual(15, atZero, "0fps fallback to 60");

        return Task.CompletedTask;
    }

    private static Task NvmlSnapshot_ComputedProperties_ConvertUnits()
    {
        var snapshotType = RequireType("Sussudio.Services.Gpu.NvmlSnapshot");
        // Constructor: GpuName, GpuUtil%, MemUtil%, NvdecUtil%, NvencUtil%, PcieTxKB, PcieRxKB,
        //              VramUsedB, VramTotalB, TempC, PowerMw, ClockMHz, MemClockMHz
        var snapshot = Activator.CreateInstance(snapshotType,
            "RTX 4090",        // GpuName
            (uint?)85,         // GpuUtilizationPercent
            (uint?)40,         // GpuMemoryUtilizationPercent
            (uint?)50,         // NvdecUtilizationPercent
            (uint?)75,         // NvencUtilizationPercent
            (uint?)1024,       // PcieTxKBps (1024 KB/s = 1.0 MB/s)
            (uint?)2048,       // PcieRxKBps (2048 KB/s = 2.0 MB/s)
            (ulong?)2147483648,// VramUsedBytes (2 GB)
            (ulong?)25769803776,// VramTotalBytes (24 GB)
            (uint?)65,         // GpuTemperatureC
            (uint?)350000,     // GpuPowerMilliwatts (350W)
            (uint?)2520,       // GpuClockMHz
            (uint?)10501)!;    // GpuMemClockMHz

        // GpuPowerW = 350000 / 1000 = 350.0
        var powerW = GetPropertyValue(snapshot, "GpuPowerW");
        AssertEqual(350.0, (double)powerW!, "GpuPowerW");

        // PcieTxMBps = 1024 / 1024 = 1.0
        var txMB = GetPropertyValue(snapshot, "PcieTxMBps");
        AssertEqual(1.0, (double)txMB!, "PcieTxMBps");

        // PcieRxMBps = 2048 / 1024 = 2.0
        var rxMB = GetPropertyValue(snapshot, "PcieRxMBps");
        AssertEqual(2.0, (double)rxMB!, "PcieRxMBps");

        // VramUsedMB = 2147483648 / (1024*1024) = 2048
        var usedMB = GetPropertyValue(snapshot, "VramUsedMB");
        AssertEqual(2048UL, (ulong)usedMB!, "VramUsedMB");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionSnapshot_DefaultState()
    {
        var snapshotType = RequireType("Sussudio.Services.Capture.CaptureSessionSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(snapshotType);

        AssertEqual(false, GetBoolProperty(snapshot, "IsRecording"), "IsRecording default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsInitialized"), "IsInitialized default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsVideoPreviewActive"), "IsVideoPreviewActive default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsAudioPreviewActive"), "IsAudioPreviewActive default");
        AssertEqual(0, (int)GetPropertyValue(snapshot, "PendingCommands")!, "PendingCommands default");
        AssertEqual(0L, GetLongProperty(snapshot, "CommandsCoalesced"), "CommandsCoalesced default");
        AssertEqual("None", GetStringProperty(snapshot, "LastOutcome"), "LastOutcome default");

        return Task.CompletedTask;
    }

    private static Task ProcessSpec_DefaultTimeout_Is30Seconds()
    {
        var specType = RequireType("Sussudio.Services.Runtime.ProcessSpec");
        var spec = RuntimeHelpers.GetUninitializedObject(specType);
        // ProcessSpec uses init-only with defaults — GetUninitializedObject bypasses ctor
        // So test the contract by checking the source
        var sourceText = ReadRepoFile("Sussudio/Services/Runtime/ProcessSupervisor.cs");
        AssertContains(sourceText, "public int TimeoutMs { get; init; } = 30_000;");
        AssertContains(sourceText, "public string Arguments { get; init; } = string.Empty;");
        AssertContains(sourceText, "public ProcessPriorityClass? PriorityClass { get; init; }");
        AssertContains(sourceText, "process.PriorityClass = priorityClass;");

        // ProcessRunResult contract
        AssertContains(sourceText, "public bool Started { get; init; }");
        AssertContains(sourceText, "public bool TimedOut { get; init; }");
        AssertContains(sourceText, "public string StdOut { get; init; } = string.Empty;");
        AssertContains(sourceText, "public string StdErr { get; init; } = string.Empty;");

        return Task.CompletedTask;
    }

    // ── Tool CommandMap & Formatter Alignment tests ──

    private static Task SharedProtocol_CommandMap_CoversEveryAutomationCommandKind()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var enumNames = Enum.GetNames(enumType);
        var expectedCommands = ExpectedAutomationCommands();
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");

        if (enumNames.Length == 0)
            throw new InvalidOperationException("AutomationCommandKind enum has no members.");

        var commandMapProperty = protocolType.GetProperty(
            "CommandMap",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.CommandMap not found.");
        var commandMap = commandMapProperty.GetValue(null) as IReadOnlyDictionary<string, int>
            ?? throw new InvalidOperationException("AutomationPipeProtocol.CommandMap has an unexpected shape.");

        AssertEqual(expectedCommands.Length, commandMap.Count,
            "AutomationPipeProtocol CommandMap entry count vs golden command table");
        foreach (var (name, ordinal) in expectedCommands)
        {
            if (!commandMap.TryGetValue(name, out var mappedOrdinal))
            {
                throw new InvalidOperationException($"AutomationPipeProtocol.CommandMap missing '{name}'.");
            }

            AssertEqual(ordinal, mappedOrdinal, $"AutomationPipeProtocol.CommandMap[{name}]");
            AssertEqual(ordinal, Convert.ToInt32(Enum.Parse(enumType, name)), $"AutomationCommandKind.{name}");
        }

        AssertEqual(enumNames.Length, commandMap.Count,
            "AutomationPipeProtocol CommandMap entry count vs AutomationCommandKind enum count");

        return Task.CompletedTask;
    }

    private static Task PipeClient_UsesSharedProtocol_ForCommandResolution()
    {
        var pipeClientText = ReadRepoFile("tools/McpServer/PipeClient.cs");

        // PipeClient should delegate to AutomationPipeProtocol, not have its own CommandMap
        AssertContains(pipeClientText, "AutomationPipeProtocol");
        AssertDoesNotContain(pipeClientText, "CommandMap = new");

        return Task.CompletedTask;
    }

    private static Task ResponseFormatter_IsSuccess_ParsesSuccessAndFailureJson()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var isSuccess = RequireNonPublicStaticMethod(formatterType, "IsSuccess");

        using (var docTrue = JsonDocument.Parse("{\"Success\": true, \"Message\": \"ok\"}"))
        {
            AssertEqual(true, (bool)isSuccess.Invoke(null, new object[] { docTrue.RootElement })!, "IsSuccess with Success=true");
        }

        using (var docFalse = JsonDocument.Parse("{\"Success\": false, \"Message\": \"fail\"}"))
        {
            AssertEqual(false, (bool)isSuccess.Invoke(null, new object[] { docFalse.RootElement })!, "IsSuccess with Success=false");
        }

        using (var docMissing = JsonDocument.Parse("{\"Message\": \"no success field\"}"))
        {
            AssertEqual(false, (bool)isSuccess.Invoke(null, new object[] { docMissing.RootElement })!, "IsSuccess with missing Success property");
        }

        return Task.CompletedTask;
    }

    private static Task ResponseFormatter_Get_HandlesAllJsonValueKinds()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var get = RequireNonPublicStaticMethod(formatterType, "Get");

        var json = @"{
            ""str"": ""hello"",
            ""num"": 42,
            ""boolTrue"": true,
            ""boolFalse"": false,
            ""nullVal"": null,
            ""emptyArr"": [],
            ""nonEmptyArr"": [1, 2],
            ""obj"": { ""nested"": true },
            ""emptyStr"": """"
        }";

        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;

        AssertEqual("hello", (string)get.Invoke(null, new object[] { el, "str", "N/A" })!, "Get string value");
        AssertEqual("42", (string)get.Invoke(null, new object[] { el, "num", "N/A" })!, "Get number value");
        AssertEqual("true", (string)get.Invoke(null, new object[] { el, "boolTrue", "N/A" })!, "Get bool true");
        AssertEqual("false", (string)get.Invoke(null, new object[] { el, "boolFalse", "N/A" })!, "Get bool false");
        AssertEqual("N/A", (string)get.Invoke(null, new object[] { el, "nullVal", "N/A" })!, "Get null value");
        AssertEqual("N/A", (string)get.Invoke(null, new object[] { el, "nonExistent", "N/A" })!, "Get missing property");
        AssertEqual("custom", (string)get.Invoke(null, new object[] { el, "nonExistent", "custom" })!, "Get missing with custom fallback");
        AssertEqual("N/A", (string)get.Invoke(null, new object[] { el, "emptyArr", "N/A" })!, "Get empty array");
        AssertEqual("", (string)get.Invoke(null, new object[] { el, "emptyStr", "N/A" })!, "Get empty string");

        return Task.CompletedTask;
    }

    private static Task SsctlFormatters_SnapshotFields_AlignWithMcpResponseFormatter()
    {
        var mcpText = ReadRepoFile("tools/Common/AutomationSnapshotFormatter.cs");
        var ssctlText = ReadRepoFile("tools/ssctl/Formatters.cs");

        var mcpFields = ExtractSnapshotFields(mcpText);
        var ssctlFields = ExtractSnapshotFields(ssctlText);

        if (mcpFields.Count == 0)
            throw new InvalidOperationException("Failed to extract any snapshot fields from AutomationSnapshotFormatter.");
        if (ssctlFields.Count == 0)
            throw new InvalidOperationException("Failed to extract any snapshot fields from ssctl Formatters.");

        var missingInSsctl = new List<string>();
        foreach (var field in mcpFields)
        {
            if (!ssctlFields.Contains(field))
                missingInSsctl.Add(field);
        }

        if (missingInSsctl.Count > 0)
        {
            throw new InvalidOperationException(
                $"AutomationSnapshotFormatter references {missingInSsctl.Count} snapshot field(s) " +
                $"missing from ssctl Formatters: {string.Join(", ", missingInSsctl)}");
        }

        return Task.CompletedTask;
    }

    private static Task AutomationClient_UsesSharedProtocol_ForCommandResolution()
    {
        var clientText = ReadRepoFile("tools/AutomationClient/Program.cs");

        // AutomationClient should delegate to AutomationPipeProtocol, not have its own CommandMap
        AssertContains(clientText, "AutomationPipeProtocol");
        AssertContains(clientText, "--payload-base64");
        AssertContains(clientText, "Convert.FromBase64String(options.PayloadBase64)");
        AssertDoesNotContain(clientText, "CommandMap = new");

        return Task.CompletedTask;
    }

    private static Task PresentMonParser_SelectsDominantNonArtifactSwapChain()
    {
        var toolAssembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var probeType = toolAssembly.GetType("Sussudio.Tools.PresentMonProbe")
            ?? throw new InvalidOperationException("Sussudio.Tools.PresentMonProbe type not found.");
        var parseCsv = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv(string) not found.");
        var parseCsvWithExpectedSwapChain = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(string) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv(string,string) not found.");
        var optionsType = toolAssembly.GetType("Sussudio.Tools.PresentMonProbeOptions")
            ?? throw new InvalidOperationException("PresentMonProbeOptions type not found.");
        var parseCsvWithCorrelation = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(string), optionsType, typeof(long?) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv correlation overload not found.");

        var csvPath = Path.Combine(Path.GetTempPath(), $"presentmon_parser_{Guid.NewGuid():N}.csv");
        File.WriteAllText(
            csvPath,
            """
            Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
            Sussudio.exe,1234,0xABC,DXGI,0,0,0,Composed: Flip,0.0000,8.3333,8.3333,NA,16.0000,0.0700,8.2500,2.0000,7.0000,NA
            Sussudio.exe,1234,0xABC,DXGI,0,0,0,Composed: Flip,8.3333,8.3334,8.3334,NA,16.1000,0.0710,8.2600,2.1000,7.1000,NA
            Sussudio.exe,1234,0x0,Other,-1,0,0,Composed: Flip,1000.0000,999.0000,999.0000,NA,16.2000,0.0800,999.0000,2.2000,7.2000,NA
            """);

        try
        {
            var summary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null.");

            AssertEqual(2, GetIntProperty(summary, "SampleCount"), "selected PresentMon sample count");
            AssertEqual(3, GetIntProperty(summary, "RawSampleCount"), "raw PresentMon sample count");
            AssertEqual(1, GetIntProperty(summary, "ExcludedSampleCount"), "excluded PresentMon sample count");
            AssertEqual("0xABC", GetStringProperty(summary, "SelectedSwapChainAddress"), "selected PresentMon swap chain");

            var betweenPresents = GetPropertyValue(summary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("BetweenPresentsMs was null.");
            AssertNearlyEqual(8.33335, GetDoubleProperty(betweenPresents, "Average"), 0.0001, "selected PresentMon average");
            AssertNearlyEqual(8.3334, GetDoubleProperty(betweenPresents, "Max"), 0.0001, "selected PresentMon max");

            var swapChains = GetPropertyValue(summary, "SwapChains")
                ?? throw new InvalidOperationException("SwapChains was null.");
            AssertEqual(2, GetCountProperty(swapChains), "PresentMon swap chain summary count");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
                Sussudio.exe,1234,0xAAA,DXGI,0,0,0,Composed: Flip,0.0000,99.0000,99.0000,8.3333,16.0000,0.0700,8.2500,2.0000,7.0000,20.0000
                Sussudio.exe,1234,0x0000000000000BBB,DXGI,0,0,0,Composed: Flip,8.3333,8.3333,8.3333,8.3333,16.1000,0.0710,8.2600,2.1000,7.1000,20.1000
                """);

            var expectedSwapChainSummary = parseCsvWithExpectedSwapChain.Invoke(null, new object[] { csvPath, "0xbbb" })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for expected swap-chain CSV.");
            AssertEqual("0xBBB", GetStringProperty(expectedSwapChainSummary, "SelectedSwapChainAddress"), "expected PresentMon selected swap chain");
            AssertEqual(true, GetBoolProperty(expectedSwapChainSummary, "ExpectedSwapChainMatched"), "expected PresentMon swap chain matched");
            var expectedBetweenPresents = GetPropertyValue(expectedSwapChainSummary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("expected BetweenPresentsMs was null.");
            AssertNearlyEqual(8.3333, GetDoubleProperty(expectedBetweenPresents, "Average"), 0.0001, "expected swap-chain PresentMon average");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,CPUStartTime,FrameTime,CPUBusy,GPUTime,DisplayedTime,MsUntilDisplayed,DisplayLatency
                Sussudio.exe,1234,0xBBB,DXGI,0,0,0,Composed: Flip,90.0000,8.3333,8.2000,6.0000,8.3333,6.0000,12.0000
                Sussudio.exe,1234,0xBBB,DXGI,0,0,0,Composed: Flip,104.0000,8.3333,8.2000,6.0000,NA,20.0000,18.0000
                """);
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("Failed to create PresentMonProbeOptions.");
            SetPropertyOrBackingField(options, "AppPresentId", 42L);
            SetPropertyOrBackingField(options, "AppSourceSequenceNumber", 1001L);
            SetPropertyOrBackingField(options, "AppPresentUtcUnixMs", 1105L);
            SetPropertyOrBackingField(options, "CaptureStartUtcUnixMs", 1000L);
            var correlatedSummary = parseCsvWithCorrelation.Invoke(null, new object?[] { csvPath, "0xBBB", options, 1000L })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for correlated CSV.");
            var appCorrelation = GetPropertyValue(correlatedSummary, "AppCorrelation")
                ?? throw new InvalidOperationException("AppCorrelation was null.");
            AssertEqual(true, GetBoolProperty(appCorrelation, "Available"), "PresentMon app correlation available");
            AssertEqual(42L, GetLongProperty(appCorrelation, "AppPresentId"), "PresentMon app present id");
            AssertEqual(1001L, GetLongProperty(appCorrelation, "AppSourceSequenceNumber"), "PresentMon app source sequence");
            AssertEqual(1, GetIntProperty(appCorrelation, "PresentMonRowIndex"), "PresentMon correlated row index");
            AssertNearlyEqual(1.0, GetDoubleProperty(appCorrelation, "DeltaMs"), 0.0001, "PresentMon app correlation delta");
            AssertEqual("SupersededOrNotDisplayed", GetStringProperty(appCorrelation, "Outcome"), "PresentMon app correlation outcome");

            var missingExpectedSwapChainSummary = parseCsvWithExpectedSwapChain.Invoke(null, new object[] { csvPath, "0xCCC" })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for missing expected swap-chain CSV.");
            AssertEqual(0, GetIntProperty(missingExpectedSwapChainSummary, "SampleCount"), "missing expected PresentMon sample count");
            AssertEqual(2, GetIntProperty(missingExpectedSwapChainSummary, "RawSampleCount"), "missing expected raw PresentMon sample count");
            AssertEqual(2, GetIntProperty(missingExpectedSwapChainSummary, "ExcludedSampleCount"), "missing expected excluded PresentMon sample count");
            AssertEqual("0xCCC", GetStringProperty(missingExpectedSwapChainSummary, "ExpectedSwapChainAddress"), "missing expected PresentMon swap chain");
            AssertEqual(false, GetBoolProperty(missingExpectedSwapChainSummary, "ExpectedSwapChainMatched"), "missing expected PresentMon swap chain matched");
            AssertEqual(string.Empty, GetStringProperty(missingExpectedSwapChainSummary, "SelectedSwapChainAddress"), "missing expected selected PresentMon swap chain");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,CPUStartTime,FrameTime,CPUBusy,CPUWait,GPULatency,GPUTime,GPUBusy,GPUWait,VideoBusy,DisplayLatency,DisplayedTime
                Sussudio.exe,1234,0xDEF,DXGI,0,0,0,Composed: Flip,0.0000,9.0000,8.9000,0.1000,3.0000,6.0000,2.0000,4.0000,7.0000,22.0000,8.3333
                Sussudio.exe,1234,0xDEF,DXGI,0,0,0,Composed: Flip,9.0000,7.6666,7.5000,0.1666,3.0000,6.5000,2.5000,4.0000,7.0000,22.5000,8.3334
                """);

            var v2Summary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for v2 CSV.");
            var v2BetweenPresents = GetPropertyValue(v2Summary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("v2 BetweenPresentsMs was null.");
            var v2CpuBusy = GetPropertyValue(v2Summary, "CpuBusyMs")
                ?? throw new InvalidOperationException("v2 CpuBusyMs was null.");
            var v2GpuBusy = GetPropertyValue(v2Summary, "GpuBusyMs")
                ?? throw new InvalidOperationException("v2 GpuBusyMs was null.");
            var v2GpuTime = GetPropertyValue(v2Summary, "GpuTimeMs")
                ?? throw new InvalidOperationException("v2 GpuTimeMs was null.");
            AssertNearlyEqual(8.3333, GetDoubleProperty(v2BetweenPresents, "Average"), 0.0001, "v2 PresentMon frame time average");
            AssertNearlyEqual(8.2, GetDoubleProperty(v2CpuBusy, "Average"), 0.0001, "v2 PresentMon CPU busy average");
            AssertNearlyEqual(2.25, GetDoubleProperty(v2GpuBusy, "Average"), 0.0001, "v2 PresentMon GPU busy average");
            AssertNearlyEqual(6.25, GetDoubleProperty(v2GpuTime, "Average"), 0.0001, "v2 PresentMon GPU time average");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
                Sussudio.exe,1234,0x0,Other,-1,0,0,Composed: Flip,1000.0000,999.0000,999.0000,NA,16.2000,0.0800,999.0000,2.2000,7.2000,NA
                """);

            var artifactOnlySummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for artifact-only CSV.");
            AssertEqual(0, GetIntProperty(artifactOnlySummary, "SampleCount"), "artifact-only selected sample count");
            AssertEqual(1, GetIntProperty(artifactOnlySummary, "RawSampleCount"), "artifact-only raw sample count");
            AssertEqual(1, GetIntProperty(artifactOnlySummary, "ExcludedSampleCount"), "artifact-only excluded sample count");
            AssertEqual(string.Empty, GetStringProperty(artifactOnlySummary, "SelectedSwapChainAddress"), "artifact-only selected swap chain");
        }
        finally
        {
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
            }
        }

        return Task.CompletedTask;
    }

    private static HashSet<string> ExtractSnapshotFields(string sourceText)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        while (index < sourceText.Length)
        {
            var getIdx = sourceText.IndexOf("Get(snapshot,", index, StringComparison.Ordinal);
            if (getIdx < 0)
                break;

            var afterComma = getIdx + "Get(snapshot,".Length;
            var quoteIdx = sourceText.IndexOf('"', afterComma);
            if (quoteIdx < 0 || quoteIdx - afterComma > 10)
            {
                index = afterComma;
                continue;
            }

            var endQuoteIdx = sourceText.IndexOf('"', quoteIdx + 1);
            if (endQuoteIdx < 0)
            {
                index = quoteIdx + 1;
                continue;
            }

            var fieldName = sourceText.Substring(quoteIdx + 1, endQuoteIdx - quoteIdx - 1);
            if (fieldName.Length > 0)
                fields.Add(fieldName);

            index = endQuoteIdx + 1;
        }

        return fields;
    }

    // ── Test helpers for new tests ──

    private static object BuildRecordingContext(
        bool usePostMuxAudio,
        string? videoPath = null,
        string? audioTempPath = null,
        string? finalPath = null)
    {
        var settings = BuildSettings(hdrEnabled: false);
        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var context = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(context, "Settings", settings);
        SetPropertyBackingField(context, "UsePostMuxAudio", usePostMuxAudio);
        SetPropertyBackingField(context, "EffectiveFrameRate", 60.0);
        SetPropertyBackingField(context, "FrameRateArg", "60");
        SetPropertyBackingField(context, "EffectiveWidth", 1920u);
        SetPropertyBackingField(context, "EffectiveHeight", 1080u);
        SetPropertyBackingField(context, "VideoInputPixelFormat", "nv12");
        SetPropertyBackingField(context, "VideoOutputPath", videoPath ?? "/tmp/video.mp4");
        SetPropertyBackingField(context, "FinalOutputPath", finalPath ?? "/tmp/final.mp4");
        SetPropertyBackingField(context, "AudioTempPath", audioTempPath);
        SetPropertyBackingField(context, "HdrPipelineActive", false);
        return context;
    }

    private static void SetPropertyBackingField(object instance, string propertyName, object? value)
    {
        // Try init-only property backing field patterns
        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(instance, value);
            return;
        }

        // Fall back to SetPropertyOrBackingField
        SetPropertyOrBackingField(instance, propertyName, value);
    }

    private static int GetCountProperty(object? collection)
    {
        if (collection == null)
            throw new InvalidOperationException("Collection is null");

        var countProp = collection.GetType().GetProperty("Count");
        if (countProp != null)
            return (int)(countProp.GetValue(collection) ?? 0);
        // IReadOnlyList<T> might not expose Count directly; try ICollection
        var iface = collection.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));
        if (iface != null)
        {
            var cp = iface.GetProperty("Count");
            return (int)(cp?.GetValue(collection) ?? 0);
        }
        throw new InvalidOperationException("No Count property found");
    }

    private static object BuildDevice(string id = "device-1")
    {
        var device = CreateInstance("Sussudio.Models.CaptureDevice");
        SetPropertyOrBackingField(device, "Id", id);
        SetPropertyOrBackingField(device, "Name", "Synthetic Capture Device");
        SetPropertyOrBackingField(device, "AudioDeviceId", "audio-1");
        SetPropertyOrBackingField(device, "AudioDeviceName", "Synthetic Audio");
        return device;
    }

    private static object BuildSettings(bool hdrEnabled)
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60d);
        SetPropertyOrBackingField(settings, "RequestedFrameRateArg", "60/1");
        SetPropertyOrBackingField(settings, "RequestedFrameRateNumerator", 60u);
        SetPropertyOrBackingField(settings, "RequestedFrameRateDenominator", 1u);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", hdrEnabled ? "P010" : "NV12");
        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "High"));
        SetPropertyOrBackingField(settings, "HdrEnabled", hdrEnabled);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("Sussudio.Models.HdrOutputMode", "Hdr10Pq"));
        SetPropertyOrBackingField(settings, "AudioEnabled", true);
        SetPropertyOrBackingField(settings, "OutputPath", Path.GetTempPath());
        return settings;
    }

    private static async Task InvokeInitializeAsync(object captureService, object device, object settings)
    {
        var initialize = captureService.GetType().GetMethod(
            "InitializeAsync",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { device.GetType(), settings.GetType(), typeof(CancellationToken) },
            modifiers: null);
        if (initialize == null)
        {
            throw new InvalidOperationException("CaptureService.InitializeAsync method not found.");
        }

        var task = initialize.Invoke(captureService, new[] { device, settings, CancellationToken.None }) as Task;
        if (task == null)
        {
            throw new InvalidOperationException("CaptureService.InitializeAsync did not return a Task.");
        }

        await task.ConfigureAwait(false);
    }

    private static async Task DisposeAsync(object captureService)
    {
        await DisposeValueTaskAsync(captureService).ConfigureAwait(false);
    }

    private static async Task DisposeValueTaskAsync(object instance)
    {
        var disposeAsync = instance.GetType().GetMethod("DisposeAsync", BindingFlags.Public | BindingFlags.Instance);
        if (disposeAsync == null)
        {
            return;
        }

        var valueTask = disposeAsync.Invoke(instance, null);
        if (valueTask == null)
        {
            return;
        }

        var asTaskMethod = valueTask.GetType().GetMethod("AsTask", BindingFlags.Public | BindingFlags.Instance);
        if (asTaskMethod?.Invoke(valueTask, null) is Task task)
        {
            await task.ConfigureAwait(false);
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, string description, int timeoutMs = 2000, int pollMs = 25)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(pollMs).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Timed out waiting for condition: {description}");
    }

    private static object CreateInstance(string typeName)
    {
        var type = RequireType(typeName);
        var instance = Activator.CreateInstance(type);
        if (instance == null)
        {
            throw new InvalidOperationException($"Failed to create instance of '{typeName}'.");
        }

        return instance;
    }

    private static object CreateUninitializedObject(Type type)
        => RuntimeHelpers.GetUninitializedObject(type);

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sussudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }

    private static string ReadRepoFile(string relativePath)
        => File.ReadAllText(Path.Combine(GetRepoRoot(), relativePath));

    private static Type RequireType(string typeName)
    {
        if (_assembly == null)
        {
            throw new InvalidOperationException("Target assembly is not loaded.");
        }

        var type = _assembly.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        foreach (var reference in _assembly.GetReferencedAssemblies())
        {
            try
            {
                var referencedAssembly = Assembly.Load(reference);
                type = referencedAssembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            catch
            {
                // Keep the original missing-type error below; unrelated
                // platform references do not matter to this offline runner.
            }
        }

        throw new InvalidOperationException($"Type '{typeName}' not found in target assembly or referenced assemblies.");
    }

    private static object ParseEnum(string typeName, string value)
    {
        var type = RequireType(typeName);
        return Enum.Parse(type, value, ignoreCase: true);
    }

    private static object InvokeInstanceMethod(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found on '{instance.GetType().Name}'.");
        }

        return method.Invoke(instance, null)
               ?? throw new InvalidOperationException($"Method '{methodName}' returned null.");
    }

    private static object? InvokeNonPublicInstanceMethod(object instance, string methodName, object?[]? arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Non-public method '{methodName}' not found on '{instance.GetType().Name}'.");
        }

        return method.Invoke(instance, arguments);
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field '{fieldName}' on '{instance.GetType().Name}'.");
        }

        field.SetValue(instance, value);
    }

    private static void SeedPipelineStopFailureState(object pipeline, Type pipelineType)
    {
        SetPrivateField(pipeline, "_workQueue", CreateUnboundedChannelFieldValue(pipelineType, "_workQueue"));
        SetPrivateField(pipeline, "_workers", Array.Empty<Thread>());
        SetPrivateField(pipeline, "_decoders", CreateEmptyArrayFieldValue(pipelineType, "_decoders"));
        SetPrivateField(pipeline, "_reorderFrames", Activator.CreateInstance(typeof(SortedDictionary<,>).MakeGenericType(
            typeof(long),
            RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+DecodedFrame")))!);
        SetPrivateField(pipeline, "_knownMissingSequences", new SortedSet<long>());
        SetPrivateField(pipeline, "_reorderLock", new object());
        SetPrivateField(pipeline, "_emitSignal", new AutoResetEvent(false));
    }

    private static object CreateEmptyArrayFieldValue(Type declaringType, string fieldName)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        var elementType = field.FieldType.GetElementType()
            ?? throw new InvalidOperationException($"Field '{fieldName}' on '{declaringType.Name}' was not an array.");
        return Array.CreateInstance(elementType, 0);
    }

    private static object CreateUnboundedChannelFieldValue(Type declaringType, string fieldName)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        var itemType = field.FieldType.GetGenericArguments().SingleOrDefault()
            ?? throw new InvalidOperationException($"Field '{fieldName}' on '{declaringType.Name}' was not a generic channel.");
        var method = typeof(Channel).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
                candidate.Name == nameof(Channel.CreateUnbounded) &&
                candidate.IsGenericMethodDefinition &&
                candidate.GetParameters().Length == 0);
        return method.MakeGenericMethod(itemType).Invoke(null, null)
               ?? throw new InvalidOperationException($"Failed to create channel for '{fieldName}'.");
    }

    private static object CreateSizedArrayFieldValue(Type declaringType, string fieldName, int length)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        var elementType = field.FieldType.GetElementType()
            ?? throw new InvalidOperationException($"Field '{fieldName}' on '{declaringType.Name}' was not an array.");
        return Array.CreateInstance(elementType, length);
    }

    private static object CreateFieldInstance(Type declaringType, string fieldName)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        return Activator.CreateInstance(field.FieldType)
               ?? throw new InvalidOperationException($"Failed to create field instance for '{fieldName}'.");
    }

    private static object? GetPrivateField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field '{fieldName}' on '{instance.GetType().Name}'.");
        }

        return field.GetValue(instance);
    }

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (backingField != null)
        {
            backingField.SetValue(instance, value);
            return;
        }

        throw new InvalidOperationException(
            $"Property '{propertyName}' is not writable and backing field was not found on '{instance.GetType().Name}'.");
    }

    private static string GetStringProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return value?.ToString() ?? string.Empty;
    }

    private static long GetLongProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToInt64(value);
    }

    private static int GetIntProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToInt32(value);
    }

    private static double GetDoubleProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToDouble(value);
    }

    private static bool GetBoolProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToBoolean(value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' not found on '{instance.GetType().Name}'.");
        }

        return property.GetValue(instance);
    }

    private static void AssertEqual<T>(T expected, T actual, string fieldName)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected '{expected}', actual '{actual}'.");
        }
    }

    private static void AssertNearlyEqual(double expected, double actual, double tolerance, string fieldName)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected '{expected}', actual '{actual}', tolerance '{tolerance}'.");
        }
    }

    private static void AssertContains(string value, string token)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedToken = NormalizeLineEndings(token);
        if (normalizedValue.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{value}' to contain '{token}'.");
        }
    }

    private static void AssertDoesNotContain(string value, string token)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedToken = NormalizeLineEndings(token);
        if (normalizedValue.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected value not to contain '{token}'.");
        }
    }

    private static string NormalizeLineEndings(string value)
        => value.Replace("\r\n", "\n").Replace('\r', '\n');

    private static void AssertNotNull(object? value, string fieldName)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: value was null.");
        }
    }

    private static object CreateMjpegTimingMetrics(
        int decodeSampleCount,
        double decodeAvgMs,
        double decodeP95Ms,
        double decodeMaxMs,
        int interopCopySampleCount,
        double interopCopyAvgMs,
        double interopCopyP95Ms,
        double interopCopyMaxMs,
        int callbackSampleCount,
        double callbackAvgMs,
        double callbackP95Ms,
        double callbackMaxMs)
    {
        var type = RequireType("Sussudio.Services.Capture.UnifiedVideoCapture+MjpegPipelineTimingMetrics");
        return Activator.CreateInstance(
                   type,
                   decodeSampleCount,
                   decodeAvgMs,
                   decodeP95Ms,
                   decodeMaxMs,
                   interopCopySampleCount,
                   interopCopyAvgMs,
                   interopCopyP95Ms,
                   interopCopyMaxMs,
                   callbackSampleCount,
                   callbackAvgMs,
                   callbackP95Ms,
                   callbackMaxMs)
               ?? throw new InvalidOperationException("Failed to create MjpegPipelineTimingMetrics.");
    }

    private static object CreateFullMjpegPipelineTimingMetrics(
        int decoderCount,
        int decodeSampleCount,
        double decodeAvgMs,
        double decodeP95Ms,
        double decodeMaxMs,
        int reorderSampleCount,
        double reorderAvgMs,
        double reorderP95Ms,
        double reorderMaxMs,
        int pipelineSampleCount,
        double pipelineAvgMs,
        double pipelineP95Ms,
        double pipelineMaxMs,
        long totalDecoded,
        long totalEmitted,
        long totalDropped,
        long reorderSkips,
        int reorderBufferDepth,
        object[] perDecoder,
        long compressedFramesQueued = 0,
        long compressedFramesDequeued = 0,
        long compressedDropsQueueFull = 0,
        long compressedDropsByteBudget = 0,
        long compressedDropsDisposed = 0,
        long decodeFailures = 0,
        long reorderCollisions = 0,
        long emitFailures = 0,
        int compressedQueueDepth = 0,
        long compressedQueueBytes = 0,
        long compressedQueueByteBudget = 0)
    {
        var type = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PipelineTimingMetrics");
        var perDecoderArray = Array.CreateInstance(
            RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PerDecoderMetrics"),
            perDecoder.Length);
        for (var i = 0; i < perDecoder.Length; i++)
        {
            perDecoderArray.SetValue(perDecoder[i], i);
        }

        return Activator.CreateInstance(
                   type,
                   decoderCount,
                   decodeSampleCount,
                   decodeAvgMs,
                   decodeP95Ms,
                   decodeMaxMs,
                   reorderSampleCount,
                   reorderAvgMs,
                   reorderP95Ms,
                   reorderMaxMs,
                   pipelineSampleCount,
                   pipelineAvgMs,
                   pipelineP95Ms,
                   pipelineMaxMs,
                   totalDecoded,
                   totalEmitted,
                   totalDropped,
                   compressedFramesQueued,
                   compressedFramesDequeued,
                   compressedDropsQueueFull,
                   compressedDropsByteBudget,
                   compressedDropsDisposed,
                   decodeFailures,
                   reorderCollisions,
                   emitFailures,
                   compressedQueueDepth,
                   compressedQueueBytes,
                   compressedQueueByteBudget,
                   reorderSkips,
                   reorderBufferDepth,
                   perDecoderArray)
               ?? throw new InvalidOperationException("Failed to create full MJPEG pipeline timing metrics.");
    }

    private static object CreatePerDecoderMetrics(
        int workerIndex,
        int sampleCount,
        double avgMs,
        double p95Ms,
        double maxMs)
    {
        var type = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PerDecoderMetrics");
        return Activator.CreateInstance(type, workerIndex, sampleCount, avgMs, p95Ms, maxMs)
               ?? throw new InvalidOperationException("Failed to create per-decoder MJPEG metrics.");
    }

    private delegate void ClosedMjpegEmitDelegate(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick);

    private static readonly Dictionary<string, Assembly> ToolAssemblyCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Assembly> IsolatedToolAssemblyCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, AssemblyLoadContext> IsolatedToolAssemblyContexts = new(StringComparer.OrdinalIgnoreCase);

    private static Assembly LoadToolAssembly(string relativeAssemblyPath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(GetRepoRoot(), relativeAssemblyPath));
        if (ToolAssemblyCache.TryGetValue(fullPath, out var cached))
        {
            return cached;
        }

        RequireFreshToolAssembly(relativeAssemblyPath, fullPath);
        var assemblyDirectory = Path.GetDirectoryName(fullPath)
                                ?? throw new InvalidOperationException($"Tool assembly directory not found for '{fullPath}'.");

        Assembly? ResolveToolAssemblyDependency(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            var dependencyPath = Path.Combine(assemblyDirectory, $"{assemblyName.Name}.dll");
            return File.Exists(dependencyPath)
                ? context.LoadFromAssemblyPath(dependencyPath)
                : null;
        }

        AssemblyLoadContext.Default.Resolving += ResolveToolAssemblyDependency;
        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
            ToolAssemblyCache[fullPath] = assembly;
            return assembly;
        }
        finally
        {
            AssemblyLoadContext.Default.Resolving -= ResolveToolAssemblyDependency;
        }
    }

    private static Assembly LoadToolAssemblyIsolated(string relativeAssemblyPath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(GetRepoRoot(), relativeAssemblyPath));
        if (IsolatedToolAssemblyCache.TryGetValue(fullPath, out var cached))
        {
            return cached;
        }

        RequireFreshToolAssembly(relativeAssemblyPath, fullPath);
        var loadContext = new ToolAssemblyLoadContext(fullPath);
        var assembly = loadContext.LoadFromAssemblyPath(fullPath);
        IsolatedToolAssemblyCache[fullPath] = assembly;
        IsolatedToolAssemblyContexts[fullPath] = loadContext;
        return assembly;
    }

    private sealed class ToolAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public ToolAssemblyLoadContext(string mainAssemblyToLoadPath)
            : base(isCollectible: false)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }
    }

    private static void RequireFreshToolAssembly(string relativeAssemblyPath, string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"Required tool assembly was not found: {relativeAssemblyPath}. Build it first with: {GetToolBuildCommand(relativeAssemblyPath)}");
        }

        var assemblyWriteTime = File.GetLastWriteTimeUtc(fullPath);
        var newestInputWriteTime = GetNewestToolInputWriteTimeUtc(relativeAssemblyPath);
        if (newestInputWriteTime > assemblyWriteTime)
        {
            throw new InvalidOperationException(
                $"Required tool assembly is stale: {relativeAssemblyPath}. Build it again with: {GetToolBuildCommand(relativeAssemblyPath)}");
        }
    }

    private static DateTime GetNewestToolInputWriteTimeUtc(string relativeAssemblyPath)
    {
        var root = GetRepoRoot();
        var projectDirectory = GetToolProjectDirectory(relativeAssemblyPath);
        var inputDirectories = EnumerateToolInputDirectories(projectDirectory)
            .Concat(EnumerateToolInputDirectories(Path.Combine(root, "tools", "Common")))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var inputFiles = inputDirectories
            .SelectMany(Directory.EnumerateFiles)
            .Concat(EnumerateToolProjectCompileIncludes(projectDirectory))
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "Sussudio.Automation.Contracts"), "*.cs"))
            .Where(file => File.Exists(file) && IsToolInputFile(file))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var newest = DateTime.MinValue;
        foreach (var file in inputFiles)
        {
            var writeTime = File.GetLastWriteTimeUtc(file);
            if (writeTime > newest)
            {
                newest = writeTime;
            }
        }

        foreach (var directory in inputDirectories)
        {
            var writeTime = Directory.GetLastWriteTimeUtc(directory);
            if (writeTime > newest)
            {
                newest = writeTime;
            }
        }

        return newest;
    }

    private static IEnumerable<string> EnumerateToolProjectCompileIncludes(string projectDirectory)
    {
        foreach (var projectFile in Directory.EnumerateFiles(projectDirectory, "*.csproj"))
        {
            XDocument project;
            try
            {
                project = XDocument.Load(projectFile);
            }
            catch
            {
                continue;
            }

            var projectFileDirectory = Path.GetDirectoryName(projectFile)
                                       ?? throw new InvalidOperationException($"Project directory not found for '{projectFile}'.");
            foreach (var include in project.Descendants()
                         .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.OrdinalIgnoreCase))
                         .Select(element => element.Attribute("Include")?.Value)
                         .Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var expanded = include!.Replace('\\', Path.DirectorySeparatorChar);
                if (expanded.Contains('*'))
                {
                    continue;
                }

                yield return Path.GetFullPath(Path.Combine(projectFileDirectory, expanded));
            }
        }
    }

    private static string GetToolProjectDirectory(string relativeAssemblyPath)
    {
        var root = GetRepoRoot();
        var normalized = relativeAssemblyPath.Replace('\\', '/');
        if (normalized.StartsWith("tools/ssctl/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "ssctl");
        }

        if (normalized.StartsWith("tools/McpServer/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "McpServer");
        }

        if (normalized.StartsWith("tools/AutomationClient/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "AutomationClient");
        }

        if (normalized.StartsWith("tools/NativeXuAudioProbe/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "NativeXuAudioProbe");
        }

        throw new InvalidOperationException($"No tool project mapping is configured for '{relativeAssemblyPath}'.");
    }

    private static string GetToolBuildCommand(string relativeAssemblyPath)
    {
        var normalized = relativeAssemblyPath.Replace('\\', '/');
        if (normalized.StartsWith("tools/ssctl/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/ssctl/ssctl.csproj -c Debug --no-restore";
        }

        if (normalized.StartsWith("tools/McpServer/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/McpServer/McpServer.csproj -c Debug --no-restore";
        }

        if (normalized.StartsWith("tools/AutomationClient/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/AutomationClient/AutomationClient.csproj -c Debug --no-restore";
        }

        if (normalized.StartsWith("tools/NativeXuAudioProbe/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj -c Debug --no-restore";
        }

        return "dotnet build";
    }

    private static bool IsToolInputFile(string file)
    {
        var extension = Path.GetExtension(file);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".props", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".targets", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateToolInputDirectories(string directory)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        yield return directory;
        foreach (var childDirectory in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(childDirectory);
            if (name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("obj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var nestedDirectory in EnumerateToolInputDirectories(childDirectory))
            {
                yield return nestedDirectory;
            }
        }
    }

    private static async Task<JsonElement> CapturePipeRequestAsync(string pipeName, Func<Task> clientAction)
    {
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                clientAction,
                _ => "{\"Success\":true}")
            .ConfigureAwait(false);
        return requests[0];
    }
}
