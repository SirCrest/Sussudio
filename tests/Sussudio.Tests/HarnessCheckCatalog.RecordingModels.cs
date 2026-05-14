using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "LibAv recording drain loop interleaves audio with bounded video batches",
            LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches);
        await AddCheckAsync(results,
            "LibAv recording encoding loop lives in focused partial",
            LibAvRecordingSink_EncodingLoopLivesInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv recording audio queues live in focused partial",
            LibAvRecordingSink_AudioQueuesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv recording lifecycle helpers live in focused partials",
            LibAvRecordingSink_LifecycleHelpersLiveInFocusedPartials);
        await AddCheckAsync(results,
            "MJPG HFR mode only activates for SDR 4K120-style settings",
            CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest);
        await AddCheckAsync(results,
            "Strict HFR fatal handler clears active session state",
            CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState);
        await AddCheckAsync(results,
            "Capture errors refresh ViewModel runtime flags",
            CaptureErrors_RefreshViewModelRuntimeFlags);

        // --- RecordingContracts ---
        await AddCheckAsync(results,
            "FinalizeResult.Success produces empty preserved list",
            FinalizeResult_Success_ProducesEmptyPreservedList);
        await AddCheckAsync(results,
            "FinalizeResult.Failure deduplicates and filters preserved artifacts",
            FinalizeResult_Failure_DeduplicatesAndFiltersArtifacts);

        // --- RecordingArtifactManager ---
        await AddCheckAsync(results,
            "FinalizeContext returns success when post-mux audio disabled",
            ArtifactManager_FinalizeContext_ReturnsSuccess_WhenPostMuxDisabled);
        await AddCheckAsync(results,
            "FinalizeContext preserves temp artifacts when mux fails",
            ArtifactManager_FinalizeContext_PreservesTempArtifacts_WhenMuxFails);
        await AddCheckAsync(results,
            "FinalizeContext rejects invalid final output",
            ArtifactManager_FinalizeContext_RejectsInvalidFinalOutput);
        await AddCheckAsync(results,
            "RollbackAsync deletes all artifacts when post-mux enabled",
            ArtifactManager_RollbackAsync_DeletesAllArtifacts_WhenPostMuxEnabled);
        await AddCheckAsync(results,
            "RollbackAsync is safe with null context",
            ArtifactManager_RollbackAsync_SafeWithNullContext);

        // --- RecordingStats ---
        await AddCheckAsync(results,
            "RecordingStats computes totals and preserves estimate flag",
            RecordingStats_ComputesTotalsAndPreservesEstimateFlag);

        // --- CaptureSettings ---
        await AddCheckAsync(results,
            "Capture mode options preserve display text and metadata",
            CaptureModeOptions_PreserveDisplayTextAndMetadata);
        await AddCheckAsync(results,
            "Capture mode options builder builds resolution and video format options",
            CaptureModeOptionsBuilder_BuildsResolutionAndVideoFormatOptions);
        await AddCheckAsync(results,
            "Capture settings defaults preserve output and pipeline contracts",
            CaptureSettings_DefaultsAndOutputContracts);
        await AddCheckAsync(results,
            "Capture settings MJPEG HFR mode handles force case and instance state",
            CaptureSettings_MjpegHighFrameRateMode_HandlesForceCaseAndInstanceState);
        await AddCheckAsync(results,
            "Encoder support computes availability and preferred encoders",
            EncoderSupport_ComputesAvailabilityAndPreferredEncoders);
        await AddCheckAsync(results,
            "GetTargetBitrate scales by resolution and frame rate",
            CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate);
        await AddCheckAsync(results,
            "GetTargetBitrate applies codec efficiency for HEVC and AV1",
            CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency);
        await AddCheckAsync(results,
            "GetTargetBitrate clamps custom quality to range",
            CaptureSettings_GetTargetBitrate_ClampsCustomQuality);
        await AddCheckAsync(results,
            "GetOutputFileName includes format suffix",
            CaptureSettings_GetOutputFileName_IncludesFormatSuffix);
        await AddCheckAsync(results,
            "MJPEG HFR mode requires SDR and MJPG pixel format",
            CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat);

        // --- FlashbackBufferManager ---
        await AddCheckAsync(results,
            "FlashbackBufferManager Initialize clears recording PTS",
            FlashbackBufferManager_InitializeClearsRecordingPts);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment lookup returns correct file for position",
            FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment completion rejects invalid metadata",
            FlashbackBufferManager_SegmentCompletionRejectsInvalidMetadata);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment completion rejects outside paths",
            FlashbackBufferManager_SegmentCompletionRejectsOutsidePaths);
        await AddCheckAsync(results,
            "FlashbackBufferManager delete helper rejects outside paths",
            FlashbackBufferManager_TryDeleteFileRejectsOutsidePaths);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment diagnostics clamp active counters",
            FlashbackBufferManager_SegmentDiagnosticsClampActiveCounters);
        await AddCheckAsync(results,
            "FlashbackBufferManager math helpers live in focused partial",
            FlashbackBufferManager_MathHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment query helpers live in focused partial",
            FlashbackBufferManager_SegmentQueriesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment mutation lives in focused partial",
            FlashbackBufferManager_SegmentMutationLiveInFocusedPartial);
        await AddCheckAsync(results,
            "FlashbackBufferManager lifecycle helpers live in focused partial",
            FlashbackBufferManager_LifecycleHelpersLiveInFocusedPartial);
        await AddCheckAsync(results,
            "FlashbackBufferManager latest PTS clamps invalid buffer duration",
            FlashbackBufferManager_UpdateLatestPts_ClampsInvalidBufferDuration);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment rotation keeps total bytes written monotonic",
            FlashbackBufferManager_SegmentRotationKeepsTotalBytesWrittenMonotonic);
        await AddCheckAsync(results,
            "FlashbackBufferManager same-path completion extends latest segment",
            FlashbackBufferManager_SamePathCompletionExtendsLatestSegment);
        await AddCheckAsync(results,
            "FlashbackBufferManager ignores updates after dispose",
            FlashbackBufferManager_IgnoresUpdatesAfterDispose);
        await AddCheckAsync(results,
            "FlashbackBufferManager ignores destructive operations after dispose",
            FlashbackBufferManager_IgnoresDestructiveOperationsAfterDispose);
        await AddCheckAsync(results,
            "FlashbackBufferManager valid segment lookup skips missing files",
            FlashbackBufferManager_GetValidSegmentFileForPosition_SkipsMissingFiles);
        await AddCheckAsync(results,
            "FlashbackBufferManager stale left-edge lookup uses oldest segment",
            FlashbackBufferManager_GetValidSegmentFileForPosition_StaleLeftEdgeUsesOldest);
        await AddCheckAsync(results,
            "FlashbackBufferManager GetNextSegmentFile walks forward through segments",
            FlashbackBufferManager_GetNextSegmentFile_WalksForward);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment path lookups normalize equivalent paths",
            FlashbackBufferManager_SegmentPathLookupsNormalizeEquivalentPaths);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment start PTS skips missing files",
            FlashbackBufferManager_GetSegmentStartPts_SkipsMissingFiles);
        await AddCheckAsync(results,
            "FlashbackBufferManager GetNextSegmentFile skips missing indexed segments",
            FlashbackBufferManager_GetNextSegmentFile_SkipsMissingIndexedSegments);
        await AddCheckAsync(results,
            "FlashbackBufferManager GetValidSegmentPaths returns overlapping segments",
            FlashbackBufferManager_GetValidSegmentPaths_ReturnsOverlapping);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment info skips missing files",
            FlashbackBufferManager_GetSegmentInfoList_SkipsMissingFiles);
        await AddCheckAsync(results,
            "FlashbackBufferManager active file path requires existing file",
            FlashbackBufferManager_ActiveFilePath_RequiresExistingFile);
        await AddCheckAsync(results,
            "FlashbackBufferManager segment count skips missing files",
            FlashbackBufferManager_SegmentCount_SkipsMissingFiles);
        await AddCheckAsync(results,
            "FlashbackBufferManager eviction updates disk byte totals",
            FlashbackBufferManager_EvictOldestSegments_UpdatesTotalDiskBytes);
        await AddCheckAsync(results,
            "FlashbackBufferManager eviction keeps rejected segments accounted",
            FlashbackBufferManager_EvictOldestSegments_KeepsRejectedSegmentsAccounted);
        await AddCheckAsync(results,
            "FlashbackBufferManager eviction pause and resume are balanced",
            FlashbackBufferManager_EvictionPauseResume_Balanced);
        await AddCheckAsync(results,
            "FlashbackBufferManager abandons startup-generated segment paths",
            FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath);
        await AddCheckAsync(results,
            "FlashbackBufferManager purges retain locked active segment path",
            FlashbackBufferManager_PurgesRetainLockedActivePath);
        await AddCheckAsync(results,
            "FlashbackBufferManager partial purge accounts for deleted active segment",
            FlashbackBufferManager_PurgeCompletedSegments_AccountsForActiveBytesOnPartialPurge);
        await AddCheckAsync(results,
            "FlashbackBufferManager full purge reports active bytes once",
            FlashbackBufferManager_PurgeAllSegmentsCore_ReportsActiveBytesOnce);
        await AddCheckAsync(results,
            "FlashbackBufferManager removes stale legacy root segments",
            FlashbackBufferManager_RemovesStaleLegacyRootSegments);
        await AddCheckAsync(results,
            "FlashbackBufferManager preserves unrelated empty temp directories",
            FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories);
        await AddCheckAsync(results,
            "FlashbackBufferManager trims startup session cache budget",
            FlashbackBufferManager_TrimsStartupSessionCacheBudget);
        await AddCheckAsync(results,
            "FlashbackBufferManager rejects unsafe session ids",
            FlashbackBufferManager_RejectsUnsafeSessionIds);
        await AddCheckAsync(results,
            "FlashbackBufferManager validates segment extensions",
            FlashbackBufferManager_ValidatesSegmentExtensions);

        // --- GpuPipelineHandles ---
        await AddCheckAsync(results,
            "GpuPipelineHandles.None returns zeroed struct",
            GpuPipelineHandles_None_ReturnsZeroedStruct);

        // --- RecordingContextRequest ---
        await AddCheckAsync(results,
            "RecordingContextRequest defaults match RecordingContext defaults",
            RecordingContextRequest_DefaultsMatchRecordingContextDefaults);

        // --- Device Models ---
        await AddCheckAsync(results,
            "AudioInputDevice display name falls back to unknown",
            AudioInputDevice_DisplayName_UsesNameOrUnknownFallback);
        await AddCheckAsync(results,
            "AudioLevelEventArgs exposes peak RMS and clipped state",
            AudioLevelEventArgs_ExposesPeakRmsAndClippedState);
        await AddCheckAsync(results,
            "CaptureDevice preserves display and metadata defaults",
            CaptureDevice_DisplayNameAndDefaults_PreserveDeviceMetadata);
        await AddCheckAsync(results,
            "CaptureDiagnosticsSnapshot preserves diagnostics telemetry contract",
            CaptureDiagnosticsSnapshot_DefaultsAndRoundTripsCoreTelemetry);
        await AddCheckAsync(results,
            "CaptureHealthSnapshot extends diagnostics with health telemetry",
            CaptureHealthSnapshot_ExtendsDiagnosticsWithFlashbackSourceAndAvSync);

        // --- MediaFormat ---
        await AddCheckAsync(results,
            "MediaFormat equality with matching rational frame rates",
            MediaFormat_Equality_WithMatchingRationalFrameRates);
        await AddCheckAsync(results,
            "MediaFormat inequality when dimensions differ",
            MediaFormat_Inequality_WhenDimensionsDiffer);
        await AddCheckAsync(results,
            "MediaFormat GetHashCode consistency for equal objects",
            MediaFormat_GetHashCode_ConsistencyForEqualObjects);

        // --- AutomationContracts ---
        await AddCheckAsync(results,
            "AutomationCommandKind preserves numeric values through GetAutomationManifest",
            AutomationCommandKind_PreservesNumericValuesThroughGetAutomationManifest);
        await AddCheckAsync(results,
            "AutomationWindowAction has expected values",
            AutomationWindowAction_HasExpectedValues);

        // --- RuntimePaths ---
        await AddCheckAsync(results,
            "RuntimePaths GetRepoLogFile returns path under repo root",
            RuntimePaths_GetRepoLogFile_ReturnsPathUnderRepoRoot);
        await AddCheckAsync(results,
            "RuntimePaths paths contain expected directory names",
            RuntimePaths_PathsContainExpectedDirectoryNames);
        await AddCheckAsync(results,
            "MMCSS registration uses Unicode AVRT entry point",
            MmcssThreadRegistration_UsesUnicodeAvrtEntryPoint);

        // --- SourceSignalTelemetrySnapshot ---
        await AddCheckAsync(results,
            "SourceSignalTelemetrySnapshot defaults have expected values",
            SourceSignalTelemetrySnapshot_DefaultsHaveExpectedValues);
        await AddCheckAsync(results,
            "SourceSignalTelemetrySnapshot properties round-trip",
            SourceSignalTelemetrySnapshot_PropertiesRoundTrip);
        await AddCheckAsync(results,
            "SourceSignalTelemetrySnapshot preserves full telemetry contract",
            SourceSignalTelemetrySnapshot_PreservesFullTelemetryContract);

        // --- HdrOutputPolicy ---
        await AddCheckAsync(results,
            "HdrOutputPolicy returns true when HDR and Hdr10Pq requested",
            HdrOutputPolicy_ReturnsTrue_WhenHdrAndHdr10PqRequested);
        await AddCheckAsync(results,
            "HdrOutputPolicy returns false when HDR disabled",
            HdrOutputPolicy_ReturnsFalse_WhenHdrDisabled);
        await AddCheckAsync(results,
            "HdrOutputPolicy returns false for non-Hdr10Pq mode",
            HdrOutputPolicy_ReturnsFalse_WhenNotHdr10Pq);
        await AddCheckAsync(results,
            "HdrOutputPolicy force-off env disables HDR output",
            HdrOutputPolicy_ReturnsFalse_WhenForceOffEnvSet);
        await AddCheckAsync(results,
            "HdrOutputPolicy ignores removed legacy enabled env switch",
            HdrOutputPolicy_IgnoresLegacyEnabledEnvSwitch);
    }
}
