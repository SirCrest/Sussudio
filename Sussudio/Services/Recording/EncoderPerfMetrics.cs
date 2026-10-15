using System.Diagnostics;
using Sussudio.Services.Performance;

namespace Sussudio.Services.Recording;

/// <summary>
/// Which performance-telemetry instruments a <see cref="LibAvEncoder"/> and its owning sink record into
/// (docs/architecture/performance-telemetry.md section 2.2). The recording sink and the Flashback sink drive the same
/// encoder type, so the role is injected at construction instead of the encoder knowing who owns it. Every method
/// is observation only: one QPC read, one histogram record and one trace check, no allocation and no lock, and all
/// of them run on the sink's single encode thread, outside the sink's queue lock.
/// </summary>
internal sealed class EncoderPerfMetrics
{
    public static readonly EncoderPerfMetrics Recording = new(
        PerfMetrics.RecordingQueueDwell,
        PerfMetrics.RecordingEncodeSubmit,
        PerfMetrics.RecordingMuxWrite,
        FrameTraceEventKind.RecordingQueueDwell,
        FrameTraceEventKind.RecordingEncodeSubmit,
        FrameTraceEventKind.RecordingMuxWrite);

    public static readonly EncoderPerfMetrics Flashback = new(
        PerfMetrics.FlashbackQueueDwell,
        PerfMetrics.FlashbackEncodeSubmit,
        PerfMetrics.FlashbackMuxWrite,
        FrameTraceEventKind.FlashbackQueueDwell,
        FrameTraceEventKind.FlashbackEncodeSubmit,
        FrameTraceEventKind.FlashbackMuxWrite);

    private readonly PerfHistogram _queueDwell;
    private readonly PerfHistogram _encodeSubmit;
    private readonly PerfHistogram _muxWrite;
    private readonly FrameTraceEventKind _queueDwellKind;
    private readonly FrameTraceEventKind _encodeSubmitKind;
    private readonly FrameTraceEventKind _muxWriteKind;

    private EncoderPerfMetrics(
        PerfHistogram queueDwell,
        PerfHistogram encodeSubmit,
        PerfHistogram muxWrite,
        FrameTraceEventKind queueDwellKind,
        FrameTraceEventKind encodeSubmitKind,
        FrameTraceEventKind muxWriteKind)
    {
        _queueDwell = queueDwell;
        _encodeSubmit = encodeSubmit;
        _muxWrite = muxWrite;
        _queueDwellKind = queueDwellKind;
        _encodeSubmitKind = encodeSubmitKind;
        _muxWriteKind = muxWriteKind;
    }

    /// <summary>
    /// Enqueue to dequeue time of one video packet. <paramref name="enqueueQpc"/> is zero for a packet that was not
    /// stamped (a test fixture or a packet built by other code), which is skipped rather than recorded as a bogus
    /// dwell since process start. <paramref name="frameId"/> is the frame's arrival timestamp when the packet carries
    /// it, otherwise -1.
    /// </summary>
    public void RecordQueueDwell(long enqueueQpc, long frameId)
    {
        if (enqueueQpc <= 0)
        {
            return;
        }

        var dwellTicks = Stopwatch.GetTimestamp() - enqueueQpc;
        _queueDwell.RecordTicks(dwellTicks);
        FrameTrace.Record(_queueDwellKind, enqueueQpc, dwellTicks, frameId);
    }

    /// <summary>Whole <c>avcodec_send_frame</c> plus receive/drain duration of one frame; mux time is nested inside it.</summary>
    public void RecordEncodeSubmit(long startTick, long frameId)
    {
        var elapsedTicks = Stopwatch.GetTimestamp() - startTick;
        _encodeSubmit.RecordTicks(elapsedTicks);
        FrameTrace.Record(_encodeSubmitKind, startTick, elapsedTicks, frameId);
    }

    /// <summary>One <c>av_interleaved_write_frame</c> call. A packet is not tied to one source frame, so the trace id is -1.</summary>
    public void RecordMuxWrite(long startTick)
    {
        var elapsedTicks = Stopwatch.GetTimestamp() - startTick;
        _muxWrite.RecordTicks(elapsedTicks);
        FrameTrace.Record(_muxWriteKind, startTick, elapsedTicks, -1);
    }
}
