using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddMcpDiagnosticsPipelineChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MCP raw app state keeps capture options separate",
            McpToolSurface_KeepsCaptureOptionsSeparateFromRawState);
        await AddCheckAsync(results,
            "MCP host tool schema uses PipeClient as a service",
            McpHostToolSchema_UsesPipeClientAsService);
        await AddCheckAsync(results,
            "MCP PipeClient honors Sussudio pipe environment",
            McpPipeClient_HonorsSussudioAutomationPipeEnvironment);
        await AddCheckAsync(results,
            "MCP host tool invocation returns pipe failures",
            McpHostToolInvocation_ReturnsPipeFailureInsteadOfClosingTransport);
        await AddCheckAsync(results,
            "MCP capture settings tool routes provided settings",
            McpCaptureSettingsTools_RouteProvidedSettings);
        await AddCheckAsync(results,
            "MCP recording tool routes recording toggle",
            McpRecordingTools_RouteRecordingToggle);
        await AddCheckAsync(results,
            "MCP flashback tool routes enable toggle",
            McpFlashbackTools_RouteEnableToggle);
        await AddCheckAsync(results,
            "MCP tool command formatter batches pending commands",
            McpToolCommandFormatter_BatchesPendingCommands);
        await AddCheckAsync(results,
            "MCP device tool routes refresh selections and custom audio",
            McpDeviceTools_RouteRefreshSelectionsAndCustomAudio);
        await AddCheckAsync(results,
            "MCP pipeline settings tool routes pipeline and audio commands",
            McpPipelineSettingsTools_RoutePipelineAndAudioCommands);
        await AddCheckAsync(results,
            "MCP UI settings tools route UI commands",
            McpUiSettingsTools_RouteUiCommands);
        await AddCheckAsync(results,
            "MCP verification tools format verification responses",
            McpVerificationTools_FormatVerificationResponses);
        await AddCheckAsync(results,
            "MCP diagnostic session tool records snapshot artifacts",
            McpDiagnosticSessionTool_RecordsSnapshotArtifacts);
        await AddCheckAsync(results,
            "MCP diagnostic session tool surfaces diagnostic failures",
            McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError);
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
        await AddCheckAsync(results,
            "MCP performance timeline exposes D3D P99 stage timing",
            McpPerformanceTimelineTool_ExposesD3DP99StageTiming);
        await AddCheckAsync(results,
            "MCP performance timeline renders flashback command counters",
            McpPerformanceTimelineTool_RendersFlashbackCommandCounters);
        await AddCheckAsync(results,
            "MCP frame pacing verdict flags half-rate preview and playback",
            McpFramePacingVerdictTool_FlagsHalfRatePreviewAndPlayback);
        await AddCheckAsync(results,
            "MCP frame pacing verdict flags insufficient sample duration",
            McpFramePacingVerdictTool_FlagsInsufficientSampleDuration);
        await AddCheckAsync(results,
            "MCP wait tool routes condition waits",
            McpWaitTools_RouteConditionWaits);
        await AddCheckAsync(results,
            "MCP window screenshot tool formats screenshot responses",
            McpWindowScreenshotTool_FormatsScreenshotResponses);
        await AddCheckAsync(results,
            "MCP window tool routes window actions",
            McpWindowTools_RouteWindowActions);
        await AddCheckAsync(results,
            "MCP preview color probe tool formats probe responses",
            McpPreviewColorProbeTool_FormatsProbeResponses);
        await AddCheckAsync(results,
            "MCP preview tool routes preview toggle",
            McpPreviewTools_RoutePreviewToggle);
        await AddCheckAsync(results,
            "MCP video source probe tool formats probe responses",
            McpVideoSourceProbeTool_FormatsProbeResponses);
        await AddCheckAsync(results,
            "Unified video capture CPU MJPEG emit reports NV12",
            UnifiedVideoCapture_CpuMjpegEmitReportsNv12);
        await AddCheckAsync(results,
            "Unified video capture retains MJPEG pipeline on stop failure",
            UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails);
        await AddCheckAsync(results,
            "MJPEG pipeline timing metrics calculate uniform samples",
            ParallelMjpegDecodePipeline_ComputeTimingMetrics_CalculatesCorrectly);
        await AddCheckAsync(results,
            "MJPEG pipeline timing metrics calculate P95 samples",
            ParallelMjpegDecodePipeline_ComputeTimingMetrics_P95Calculation);
        await AddCheckAsync(results,
            "MJPEG pipeline copy ring extracts insertion-order window",
            ParallelMjpegDecodePipeline_CopyRing_ExtractsCorrectWindow);
        await AddCheckAsync(results,
            "MJPEG pipeline elapsed milliseconds uses stopwatch ticks",
            ParallelMjpegDecodePipeline_GetElapsedMilliseconds_ComputesCorrectly);
        await AddCheckAsync(results,
            "MJPEG pipeline remaining timeout clamps past deadlines",
            ParallelMjpegDecodePipeline_GetRemainingTimeout_ReturnsCorrectTimeSpan);
        await AddCheckAsync(results,
            "MJPEG pipeline lifecycle lives in focused partial",
            ParallelMjpegDecodePipeline_LifecycleLivesInFocusedPartial);
        await AddCheckAsync(results,
            "MJPEG pipeline reorder lives in focused partial",
            ParallelMjpegDecodePipeline_ReorderLivesInFocusedPartial);
        await AddCheckAsync(results,
            "MJPEG pipeline timing metrics expose expected properties",
            ParallelMjpegDecodePipeline_PipelineTimingMetrics_HasExpectedProperties);
        await AddCheckAsync(results,
            "Software MJPEG decoder exposes dimensions and NV12 size",
            SoftwareMjpegDecoder_Properties_ExposeCorrectDimensions);
        await AddCheckAsync(results,
            "Pooled video frame leases return buffer after final release",
            PooledVideoFrame_LeaseLifecycle_ReturnsBufferAfterLastRelease);
        await AddCheckAsync(results,
            "Pooled video frame rejects leases after return",
            PooledVideoFrame_AddLeaseAfterReturn_Throws);
        await AddCheckAsync(results,
            "Pooled video frame closes new leases after owner release",
            PooledVideoFrame_OwnerDisposeClosesNewLeasesButExistingLeaseRemainsReadable);
        await AddCheckAsync(results,
            "MJPEG pooled frame fanout exposes lease contracts",
            MjpegPooledFrameFanout_ExposesLeaseContracts);
        await AddCheckAsync(results,
            "MJPEG shared reorder does not synthesize recording skips",
            ParallelMjpegDecodePipeline_SharedReorder_DoesNotSynthesizeRecordingSkips);
        await AddCheckAsync(results,
            "MJPEG startup non-JPEG samples drop before sequencing",
            ParallelMjpegDecodePipeline_DropsStartupNonJpegBeforeSequencing);
        await AddCheckAsync(results,
            "MJPEG known losses skip instead of fataling capture",
            ParallelMjpegDecodePipeline_KnownLossSkipsInsteadOfSignalingFatal);
        await AddCheckAsync(results,
            "MJPEG packet hash current duplicate run lowers unique FPS",
            FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps);
        await AddCheckAsync(results,
            "Decoded visual cadence samples exact crop pixels in one pass",
            VisualCadenceTracker_UsesExactCropPixelsWithOnePassDiff);
        await AddCheckAsync(results,
            "MJPEG leased video packets release queued leases",
            MjpegLeasedVideoPackets_ReleaseQueuedLeases);
        await AddCheckAsync(results,
            "MJPEG preview jitter exposes adaptive deadline policy",
            MjpegPreviewJitter_ExposesAdaptiveDeadlinePolicy);
        await AddCheckAsync(results,
            "MJPEG preview jitter emit loop lives in focused partial",
            MjpegPreviewJitter_EmitLoopLivesInFocusedPartial);
        await AddCheckAsync(results,
            "MJPEG preview jitter drops soft deadline overflow to recover latency",
            MjpegPreviewJitter_DropsSoftDeadlineOverflowToRecoverLatency);
        await AddCheckAsync(results,
            "MJPEG preview jitter drops expired frames below target depth",
            MjpegPreviewJitter_DropsExpiredFramesBelowTargetDepth);
        await AddCheckAsync(results,
            "MJPEG preview jitter skips missing preview sequence after deadline",
            MjpegPreviewJitter_SkipsMissingPreviewSequenceAfterDeadline);
        await AddCheckAsync(results,
            "MJPEG preview jitter does not count late sequence frames as queued",
            MjpegPreviewJitter_LateSequenceDoesNotCountAsQueued);
        await AddCheckAsync(results,
            "MJPEG preview jitter clear resets preview sequence",
            MjpegPreviewJitter_ClearResetsPreviewSequence);
        await AddCheckAsync(results,
            "MJPEG preview jitter reprimes after suppression resume",
            MjpegPreviewJitter_ReprimesAfterSuppressionResume);
        await AddCheckAsync(results,
            "D3D preview pending frame releases queued lease",
            D3DPreviewPendingFrame_ReleasesQueuedLease);
        await AddCheckAsync(results,
            "Recording video queues fail explicitly instead of evicting frames",
            RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames);
        await AddCheckAsync(results,
            "Capture service recording lifecycle lives in focused partial",
            CaptureService_RecordingLifecycleLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Capture service recording rollback lives in focused partial",
            CaptureService_RecordingRollbackLivesInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv recording stop validates final output",
            LibAvRecordingSink_StopValidatesFinalOutput);
        await AddCheckAsync(results,
            "Recording video try enqueue paths do not block capture callbacks",
            RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks);
        await AddCheckAsync(results,
            "Unified video capture sink fan-out lives in focused partial",
            UnifiedVideoCapture_SinkFanoutLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Unified video capture lifecycle lives in focused partial",
            UnifiedVideoCapture_LifecycleLivesInFocusedPartial);
        await AddCheckAsync(results,
            "WASAPI audio capture rejects incomplete hot audio writes",
            WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks);
        await AddCheckAsync(results,
            "WASAPI audio capture conversion lives in focused partial",
            WasapiAudioCapture_ConversionLivesInFocusedPartial);
        await AddCheckAsync(results,
            "WASAPI audio capture diagnostics live in focused partial",
            WasapiAudioCapture_DiagnosticsLivesInFocusedPartial);
        await AddCheckAsync(results,
            "WASAPI COM interop contracts live in focused file",
            WasapiComInterop_ContractsLiveInFocusedFile);
        await AddCheckAsync(results,
            "WASAPI audio capture stop uses bounded thread join",
            WasapiAudioCapture_StopUsesBoundedThreadJoin);
        await AddCheckAsync(results,
            "CaptureService flashback backend ownership uses resource aggregate",
            CaptureService_FlashbackBackendOwnershipUsesResourceAggregate);
        await AddCheckAsync(results,
            "CaptureService Flashback orchestration lives in focused partials",
            CaptureService_FlashbackOrchestrationLivesInFocusedPartials);
    }
}
