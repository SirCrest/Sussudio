using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelChecksAsync(List<CheckResult> results)
    {
        await AddRecordingModelLibAvSinkChecksAsync(results);
        await AddRecordingModelCaptureRuntimeChecksAsync(results);
        await AddRecordingModelFlashbackBufferChecksAsync(results);
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
            "Strict HFR fatal handler clears active session state",
            CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState);
        await AddCheckAsync(results,
            "Capture errors refresh ViewModel runtime flags",
            CaptureErrors_RefreshViewModelRuntimeFlags);
    }

}
