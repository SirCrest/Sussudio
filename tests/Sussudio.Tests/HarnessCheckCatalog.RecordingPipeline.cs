using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingPipelineChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Recording video queues fail explicitly instead of evicting frames",
            RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames);
        await AddCheckAsync(results,
            "Capture service recording lifecycle and backend resources have focused owners",
            CaptureService_RecordingLifecycleAndBackendResourcesHaveFocusedOwners);
        await AddCheckAsync(results,
            "Capture service recording rollback lives in focused partial",
            CaptureService_RecordingRollbackLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Capture service recording outcome state lives in focused partial",
            CaptureService_RecordingOutcomeStateLivesInFocusedPartial);
        await AddCheckAsync(results,
            "Capture service audio ownership lives in focused partials",
            CaptureService_AudioOwnershipLivesInFocusedPartials);
        await AddCheckAsync(results,
            "Capture service microphone restart after recording lives in microphone monitor partial",
            CaptureService_MicrophoneRestartAfterRecordingLivesInMicrophoneMonitorPartial);
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
            "Unified video capture frame ingress lives in focused partial",
            UnifiedVideoCapture_FrameIngressLivesInFocusedPartial);
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
            "WASAPI audio capture initialization lives in focused partial",
            WasapiAudioCapture_InitializationLivesInFocusedPartial);
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
        await AddCheckAsync(results,
            "CaptureService recording finalization lives in focused partials",
            CaptureService_RecordingFinalizationLivesInFocusedPartials);
    }
}
