using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private long _lastFlashbackDroppedFrames;
    private long _lastFlashbackVideoEncoderDroppedFrames;
    private long _lastFlashbackVideoSequenceGaps;
    private long _lastFlashbackGpuFramesDropped;
    private long _lastFlashbackVideoBackpressureEvents;
    private long _lastFlashbackRecordingEvalTick;

    private FlashbackRecordingRecentCounters UpdateFlashbackRecordingRecentCounters(
        CaptureHealthSnapshot snapshot,
        long nowTick)
    {
        var droppedFrames = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackDroppedFrames) : 0;
        var encoderDroppedFrames = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackVideoEncoderDroppedFrames) : 0;
        var sequenceGaps = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackVideoSequenceGaps) : 0;
        var gpuFramesDropped = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackGpuFramesDropped) : 0;
        var backpressureEvents = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackVideoBackpressureEvents) : 0;

        var previousTick = Interlocked.Exchange(ref _lastFlashbackRecordingEvalTick, nowTick);
        var previousDroppedFrames = Interlocked.Exchange(ref _lastFlashbackDroppedFrames, droppedFrames);
        var previousEncoderDroppedFrames = Interlocked.Exchange(ref _lastFlashbackVideoEncoderDroppedFrames, encoderDroppedFrames);
        var previousSequenceGaps = Interlocked.Exchange(ref _lastFlashbackVideoSequenceGaps, sequenceGaps);
        var previousGpuFramesDropped = Interlocked.Exchange(ref _lastFlashbackGpuFramesDropped, gpuFramesDropped);
        var previousBackpressureEvents = Interlocked.Exchange(ref _lastFlashbackVideoBackpressureEvents, backpressureEvents);

        if (previousTick == 0 || nowTick < previousTick)
        {
            return FlashbackRecordingRecentCounters.Empty;
        }

        return new FlashbackRecordingRecentCounters(
            Math.Max(0, droppedFrames - previousDroppedFrames),
            Math.Max(0, encoderDroppedFrames - previousEncoderDroppedFrames),
            Math.Max(0, sequenceGaps - previousSequenceGaps),
            Math.Max(0, gpuFramesDropped - previousGpuFramesDropped),
            Math.Max(0, backpressureEvents - previousBackpressureEvents));
    }
}
