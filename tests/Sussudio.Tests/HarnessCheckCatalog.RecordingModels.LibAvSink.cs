using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelLibAvSinkChecksAsync(List<CheckResult> results)
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
            "LibAv recording video queue submission lives in focused partial",
            LibAvRecordingSink_VideoQueueSubmissionLivesInFocusedPartial);
        await AddCheckAsync(results,
            "LibAv recording startup and stop lifecycle live in focused partials",
            LibAvRecordingSink_LifecycleHelpersLiveInFocusedPartials);
    }
}
