using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    // Typed delta records carry the previous-baseline, current-sample, and
    // elapsed-tick information for one polling area. Update*RecentCounters
    // methods (AutomationDiagnosticsHub.Counters.cs) consume these to
    // produce area-scoped recent-counter results without mutating 25+
    // _lastX fields individually in the polling body.

    private readonly record struct PreviewJitterDelta(
        long PreviousUnderflows,
        long CurrentUnderflows,
        long PreviousDeadlineDrops,
        long CurrentDeadlineDrops,
        long PreviousTick,
        long NowTick);

    private readonly record struct MjpegDelta(
        long PreviousTotalDropped,
        long CurrentTotalDropped,
        long PreviousDecodeFailures,
        long CurrentDecodeFailures,
        long PreviousEmitFailures,
        long CurrentEmitFailures,
        long PreviousCompressedQueueDrops,
        long CurrentCompressedQueueDrops,
        long PreviousTick,
        long NowTick);

    private readonly record struct D3DRendererDelta(
        long PreviousSubmitted,
        long CurrentSubmitted,
        long PreviousRendered,
        long CurrentRendered,
        long PreviousDropped,
        long CurrentDropped,
        long PreviousTick,
        long NowTick);

    private readonly record struct D3DFrameStatsDelta(
        long PreviousMissedRefreshes,
        long CurrentMissedRefreshes,
        long PreviousFailures,
        long CurrentFailures,
        long PreviousTick,
        long NowTick);

    private readonly record struct FlashbackRecordingDelta(
        long PreviousDroppedFrames,
        long CurrentDroppedFrames,
        long PreviousEncoderDroppedFrames,
        long CurrentEncoderDroppedFrames,
        long PreviousSequenceGaps,
        long CurrentSequenceGaps,
        long PreviousGpuFramesDropped,
        long CurrentGpuFramesDropped,
        long PreviousBackpressureEvents,
        long CurrentBackpressureEvents,
        long PreviousTick,
        long NowTick);
}
