using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelChecksAsync(List<CheckResult> results)
    {
        await AddRecordingModelLibAvSinkChecksAsync(results);
        await AddRecordingModelCaptureRuntimeChecksAsync(results);
        await AddRecordingModelRecordingContractChecksAsync(results);
        await AddRecordingModelCaptureSettingsChecksAsync(results);
        await AddRecordingModelFlashbackBufferChecksAsync(results);
        await AddRecordingModelDeviceModelChecksAsync(results);
        await AddRecordingModelAutomationContractChecksAsync(results);
        await AddRecordingModelSourceSignalTelemetryChecksAsync(results);
    }

    private static async Task AddRecordingModelLibAvSinkChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "LibAv recording drain loop interleaves audio with bounded video batches",
            LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches);
        await AddCheckAsync(results,
            "LibAv recording encoding loop and packet drains live in focused partials",
            LibAvRecordingSink_EncodingLoopAndPacketDrainsLiveInFocusedPartials);
        await AddCheckAsync(results,
            "LibAv recording audio queues live in focused partial",
            LibAvRecordingSink_AudioQueuesLiveInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv recording video queue submission lives in focused partial",
            LibAvRecordingSink_VideoQueueSubmissionLivesInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv recording startup and stop lifecycle live in focused partials",
            LibAvRecordingSink_LifecycleHelpersLiveInFocusedPartials);
    }

    private static async Task AddRecordingModelCaptureRuntimeChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MJPG HFR mode only activates for SDR 4K120-style settings",
            CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest);
        await AddCheckAsync(results,
            "Strict HFR fatal handler clears active session state",
            CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState);
        await AddCheckAsync(results,
            "Capture errors refresh ViewModel runtime flags",
            CaptureErrors_RefreshViewModelRuntimeFlags);
    }

    private static async Task AddRecordingModelRecordingContractChecksAsync(List<CheckResult> results)
    {
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
        await AddCheckAsync(results,
            "RecordingStats computes totals and preserves estimate flag",
            RecordingStats_ComputesTotalsAndPreservesEstimateFlag);
    }

    private static async Task AddRecordingModelCaptureSettingsChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Capture mode options preserve display text and metadata",
            CaptureModeOptions_PreserveDisplayTextAndMetadata);
        await AddCheckAsync(results,
            "Capture mode options builder builds resolution and video format options",
            CaptureModeOptionsBuilder_BuildsResolutionAndVideoFormatOptions);
        await AddCheckAsync(results,
            "Recording settings selection policy preserves HDR and SDR choices",
            RecordingSettingsSelectionPolicy_PreservesHdrAndSdrChoices);
        await AddCheckAsync(results,
            "Recording settings selection policy parses model values",
            RecordingSettingsSelectionPolicy_ParsesModelValues);
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
    }

    private static async Task AddRecordingModelDeviceModelChecksAsync(List<CheckResult> results)
    {
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
    }

    private static async Task AddRecordingModelAutomationContractChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "AutomationWindowAction has expected values",
            AutomationWindowAction_HasExpectedValues);
    }

    private static async Task AddRecordingModelSourceSignalTelemetryChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "SourceSignalTelemetrySnapshot defaults have expected values",
            SourceSignalTelemetrySnapshot_DefaultsHaveExpectedValues);
        await AddCheckAsync(results,
            "SourceSignalTelemetrySnapshot properties round-trip",
            SourceSignalTelemetrySnapshot_PropertiesRoundTrip);
        await AddCheckAsync(results,
            "SourceSignalTelemetrySnapshot preserves full telemetry contract",
            SourceSignalTelemetrySnapshot_PreservesFullTelemetryContract);
    }

}
