using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddPresentationPreviewChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Recording start and stop failures propagate to callers",
            MainViewModelCapture_RecordingFailuresPropagateToCallers);
        await AddCheckAsync(results,
            "Window close cancels until recording stop completes",
            MainWindowClose_CancelsCloseUntilRecordingStopCompletes);
        await AddCheckAsync(results,
            "Window screenshot capture completes on dispatcher failure and cancellation",
            MainWindowScreenshot_CompletesOnDispatcherFailureAndCancellation);
        await AddCheckAsync(results,
            "External FFmpeg and HDR probes use bounded process supervision",
            ExternalProcessProbes_UseBoundedProcessSupervisor);
        await AddCheckAsync(results,
            "Recording stop propagates unified video stop failures",
            RecordingStop_PropagatesUnifiedVideoStopFailure);
        await AddCheckAsync(results,
            "Preview stop compatibility overloads are preserved",
            PreviewStopCompatibilityOverloads_ArePreserved);
        await AddCheckAsync(results,
            "Preview stop API surface has no default-literal ambiguity",
            PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity);
        await AddCheckAsync(results,
            "Emergency recording stop does not dispatch to blocked UI thread",
            EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread);
        await AddCheckAsync(results,
            "Flashback buffer manager cleans stale session directories",
            FlashbackBufferManager_CleansStaleSessionDirectories);
        await AddCheckAsync(results,
            "Flashback buffer manager preserves marked recovery sessions",
            FlashbackBufferManager_PreservesMarkedRecoverySessions);
        await AddCheckAsync(results,
            "Project file preserves main's English-only publish locale policy",
            ProjectFile_PreservesEnglishOnlyPublishLocalePolicy);
        await AddCheckAsync(results,
            "Show all capture options unlocks source-filtered frame rates",
            ShowAllCaptureOptions_UnlocksSourceFilteredFrameRates);
        await AddCheckAsync(results,
            "Resolution selection policy lives in focused partial",
            ResolutionSelectionPolicy_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Frame-rate timing policy lives in focused partial",
            FrameRateTimingPolicy_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Diagnostics loop does not rebuild automation options each poll",
            DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll);
        await AddCheckAsync(results,
            "Preview startup state lives in preview startup partial",
            PreviewStartup_StateLivesInPreviewStartupPartial);
        await AddCheckAsync(results,
            "Preview startup tolerates missing audio capture devices",
            PreviewStartup_ToleratesMissingAudioCaptureDevices);
        await AddCheckAsync(results,
            "Preview startup begins device discovery before recording capability probes finish",
            PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish);
        await AddCheckAsync(results,
            "Preview startup primes UI and audio before preview reveal",
            PreviewStartup_PrimesUiAndAudioBeforePreviewReveal);
        await AddCheckAsync(results,
            "Preview stop ramps audio down before preview teardown",
            PreviewStop_RampsAudioDownBeforePreviewTeardown);
        await AddCheckAsync(results,
            "Audio preview stays inactive when no audio capture device exists",
            AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists);
        await AddCheckAsync(results,
            "Audio monitoring visuals follow runtime preview activity",
            AudioMonitoringVisuals_FollowRuntimePreviewActivity);
        await AddCheckAsync(results,
            "Preview backend log reflects video-only fallback",
            PreviewBackendLog_ReflectsVideoOnlyFallback);
        await AddCheckAsync(results,
            "MainViewModel automation routes preview volume persistence through save hook",
            MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook);
        await AddCheckAsync(results,
            "MainViewModel capture routes audio monitoring through coordinator",
            MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator);
        await AddCheckAsync(results,
            "MainViewModel capture settings projection lives in focused partial",
            MainViewModelCaptureSettings_OwnsSettingsProjection);
        await AddCheckAsync(results,
            "MainViewModel audio controls map analog gain curve and clamp endpoints",
            MainViewModelAudioControls_MapsAnalogGainCurveAndClamps);
        await AddCheckAsync(results,
            "MainViewModel audio monitoring preserves volume persistence and ramped routing",
            MainViewModelAudioMonitoring_PreservesVolumePersistenceAndRampedRouting);
        await AddCheckAsync(results,
            "MainViewModel audio controls preserve microphone and device guards",
            MainViewModelAudioControls_PreserveMicrophoneVolumeAndDeviceGuards);
        await AddCheckAsync(results,
            "Native XU audio control profiles live in focused partial",
            NativeXuAudioControlService_ProfilesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "Native XU audio control transport lives in focused partial",
            NativeXuAudioControlService_TransportLivesInFocusedPartial);
        await AddCheckAsync(results,
            "MainViewModel audio meters own callback meter state",
            MainViewModelAudioMeters_OwnCallbackMeterState);
        await AddCheckAsync(results,
            "MainViewModel uses dependency composition seam",
            MainViewModel_UsesDependencyCompositionSeam);
        await AddCheckAsync(results,
            "Audio ramp trace exposes control and render-side envelope telemetry",
            AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry);
        await AddCheckAsync(results,
            "Live pixel format surfaces prefer source subtype over decoded output",
            LivePixelFormatSurfaces_PreferReaderSourceSubtype);
        await AddCheckAsync(results,
            "Stats overlay lifecycle lives in controller",
            StatsOverlayLifecycle_LivesInController);
        await AddCheckAsync(results,
            "Stats diagnostic row pooling lives in controller",
            StatsDiagnosticRowPooling_LivesInController);
        await AddCheckAsync(results,
            "Settings shelf lifecycle lives in controller",
            SettingsShelfLifecycle_LivesInController);
        await AddCheckAsync(results,
            "Splash loading phrases live in controller",
            SplashLoadingPhrases_LiveInController);
        await AddCheckAsync(results,
            "Launch entrance animation lives in controller",
            LaunchEntranceAnimation_LivesInController);
        await AddCheckAsync(results,
            "MainWindow startup hosting lives in startup partial",
            MainWindowStartupHosting_LivesInStartupPartial);
        await AddCheckAsync(results,
            "MainWindow shell resize telemetry lives in sizing partial",
            MainWindowShellResizeTelemetry_LivesInSizingPartial);
        await AddCheckAsync(results,
            "Preview renderer runtime state lives in renderer partial",
            PreviewRendererRuntimeState_LivesInRendererPartial);
        await AddCheckAsync(results,
            "MainWindow title presentation lives in title partial",
            MainWindowTitlePresentation_LivesInTitlePartial);
        await AddCheckAsync(results,
            "MainWindow close lifecycle and native helpers are split",
            MainWindowCloseLifecycleAndNativeHelpers_AreSplit);
        await AddCheckAsync(results,
            "Control bar hover animations live in controller",
            ControlBarHoverAnimations_LiveInController);
        await AddCheckAsync(results,
            "Shell elevation setup lives in controller",
            ShellElevationSetup_LivesInController);
        await AddCheckAsync(results,
            "Preview transition animations live in controller",
            PreviewTransitionAnimations_LiveInController);
        await AddCheckAsync(results,
            "Record button width animation lives in controller",
            RecordButtonWidthAnimation_LivesInController);
        await AddCheckAsync(results,
            "Recording button action lives in controller",
            RecordingButtonAction_LivesInController);
        await AddCheckAsync(results,
            "Live signal info presentation lives in controller",
            LiveSignalInfoPresentation_LivesInController);
        await AddCheckAsync(results,
            "Preview audio fade state lives in controller",
            PreviewAudioFadeState_LivesInController);
        await AddCheckAsync(results,
            "Microphone controls live in controller",
            MicrophoneControls_LiveInController);
        await AddCheckAsync(results,
            "Responsive shell layout lives in controller",
            ResponsiveShellLayout_LivesInController);
        await AddCheckAsync(results,
            "Capture selection binding sync lives in controller",
            CaptureSelectionBindingSync_LivesInController);
        await AddCheckAsync(results,
            "Capture device button actions live in controller",
            CaptureDeviceButtonActions_LiveInController);
        await AddCheckAsync(results,
            "Capture option presentation lives in focused partial",
            CaptureOptionPresentation_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Output path display lives in controller",
            OutputPathDisplay_LivesInController);
        await AddCheckAsync(results,
            "Output path button actions live in controller",
            OutputPathButtonActions_LiveInController);
        await AddCheckAsync(results,
            "Preview screenshot button workflow lives in controller",
            PreviewScreenshotButtonWorkflow_LivesInController);
        await AddCheckAsync(results,
            "Stats panels use source telemetry for HDMI input format and HDR",
            StatsPanels_UseSourceTelemetry_ForHdmiInput);
        await AddCheckAsync(results,
            "Stats presentation logic lives in focused builder",
            StatsPresentationLogic_LivesInFocusedBuilder);
        await AddCheckAsync(results,
            "Stats snapshot construction lives in focused builder",
            StatsSnapshotConstruction_LivesInFocusedBuilder);
        await AddCheckAsync(results,
            "Stats snapshot builder maps health and renderer metrics",
            StatsSnapshotBuilder_MapsHealthAndRendererMetrics);
        await AddCheckAsync(results,
            "Stats live summary shows current preview frame time and 1 percent low",
            StatsLiveSummary_ShowsCurrentPreviewFrameTimeAndOnePercentLow);
        await AddCheckAsync(results,
            "Frame-time overlay uses detected-FPS bounded millisecond range",
            FrameTimeOverlay_UsesDetectedFpsBoundedRange);
        await AddCheckAsync(results,
            "D3D preview letterbox rect calculates correctly",
            D3D11PreviewRenderer_ComputeLetterboxRect_CalculatesCorrectly);
        await AddCheckAsync(results,
            "D3D preview black edge counting works correctly",
            D3D11PreviewRenderer_BlackEdgeCounting_WorksCorrectly);
        await AddCheckAsync(results,
            "D3D preview device lost exceptions classify correctly",
            D3D11PreviewRenderer_IsDeviceLostException_ClassifiesCorrectly);
        await AddCheckAsync(results,
            "D3D preview present cadence metrics expose expected properties",
            D3D11PreviewRenderer_PresentCadenceMetrics_HasExpectedProperties);
        await AddCheckAsync(results,
            "D3D preview present cadence ignores suppressed frames",
            D3D11PreviewRenderer_PresentCadenceSuppression_SkipsSamplesAndResetsBaseline);
        await AddCheckAsync(results,
            "D3D preview PNG CRC table generates 256 entries",
            D3D11PreviewRenderer_InitPngCrc32Table_Generates256Entries);
        await AddCheckAsync(results,
            "D3D preview diagnostics expose swap-chain and render timing contract",
            D3D11PreviewRenderer_DiagnosticsContract_ExposesSwapChainAndRenderTiming);
        await AddCheckAsync(results,
            "D3D preview frame types live in focused partial",
            D3D11PreviewRenderer_FrameTypesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview frame submission lives in focused partial",
            D3D11PreviewRenderer_SubmissionLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview frame ownership lives in focused partial",
            D3D11PreviewRenderer_FrameOwnershipLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview DXGI frame statistics live in focused partial",
            D3D11PreviewRenderer_DxgiFrameStatisticsLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview panel binding lives in focused partial",
            D3D11PreviewRenderer_PanelBindingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview frame upload lives in focused partial",
            D3D11PreviewRenderer_FrameUploadLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview shader rendering lives in focused partial",
            D3D11PreviewRenderer_ShaderRenderingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview shader sources live in focused file",
            D3D11PreviewRenderer_ShaderSourcesLiveInFocusedFile);
        await AddCheckAsync(results,
            "D3D preview slow-frame diagnostics live in focused partial",
            D3D11PreviewRenderer_SlowFrameDiagnosticsLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview metric tracking lives in focused partial",
            D3D11PreviewRenderer_MetricTrackingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview frame-latency wait lives in focused partial",
            D3D11PreviewRenderer_FrameLatencyLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview input resources live in focused partial",
            D3D11PreviewRenderer_InputResourcesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview lifecycle lives in focused partial",
            D3D11PreviewRenderer_LifecycleLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview device initialization lives in focused partial",
            D3D11PreviewRenderer_DeviceInitializationLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview viewport helpers live in focused partial",
            D3D11PreviewRenderer_ViewportHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview screenshot encoding lives in focused partial",
            D3D11PreviewRenderer_ScreenshotEncodingLivesInFocusedPartial);
        await AddCheckAsync(results,
            "D3D preview device-lost recovery lives in focused partial",
            D3D11PreviewRenderer_DeviceLostRecoveryLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Preview pacing classifier rejects weak samples",
            PreviewPacingClassifier_RequiresStableSampleUnlessHardSignal);
        await AddCheckAsync(results,
            "Preview pacing classifier prefers source capture when source drops",
            PreviewPacingClassifier_ClassifiesSourceCaptureBeforePreviewTail);
        await AddCheckAsync(results,
            "Preview pacing classifier flags compositor misses first",
            PreviewPacingClassifier_ClassifiesCompositorMissBeforePresentBlocked);
        await AddCheckAsync(results,
            "Preview pacing classifier flags dominant render upload",
            PreviewPacingClassifier_ClassifiesDominantRenderUpload);
        await AddCheckAsync(results,
            "Preview pacing classifier flags frame latency wait timeout",
            PreviewPacingClassifier_ClassifiesFrameLatencyWaitTimeout);
        await AddCheckAsync(results,
            "Preview pacing classifier ignores stale lifetime signals",
            PreviewPacingClassifier_IgnoresStaleLifetimeSignalsWithoutRecentDeltas);
        await AddCheckAsync(results,
            "Preview pacing classifier flags recent jitter schedule-late",
            PreviewPacingClassifier_ClassifiesRecentJitterScheduleLate);
        await AddCheckAsync(results,
            "Preview pacing classifier models live in focused file",
            PreviewPacingClassifier_ModelsLiveInFocusedFile);
        await AddCheckAsync(results,
            "Preview pacing classifier is wired into automation snapshots",
            PreviewPacingClassifier_IsWiredIntoAutomationSnapshots);
        await AddCheckAsync(results,
            "D3D preview transition drain drops pending frames",
            D3D11PreviewRenderer_DropPendingFrames_DrainsQueueAndMarksGeneration);
        await AddCheckAsync(results,
            "D3D preview frame capture cancellation clears pending request",
            D3D11PreviewRenderer_FrameCaptureCancellationClearsPendingRequest);
        await AddCheckAsync(results,
            "Shared D3D device references are duplicated under lifecycle lock",
            SharedD3DDeviceManager_DuplicatesReferencesUnderLifecycleLock);
    }
}
