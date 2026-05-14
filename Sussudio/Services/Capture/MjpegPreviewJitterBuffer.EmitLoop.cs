using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class MjpegPreviewJitterBuffer
{
    private void EmitLoop()
    {
        using var mmcss = MmcssThreadRegistration.TryRegister(_mmcssTask, _mmcssPriority, message => Logger.Log(message));
        var primed = false;
        var nextDueTick = 0L;

        while (Volatile.Read(ref _disposed) == 0)
        {
            if (!primed)
            {
                var targetDepth = Volatile.Read(ref _targetDepth);
                if (GetDepth() < targetDepth)
                {
                    _signal.WaitOne(2);
                    continue;
                }

                primed = true;
                nextDueTick = Stopwatch.GetTimestamp();
            }

            var now = Stopwatch.GetTimestamp();
            var clockSink = _displayClockPacingEnabled ? _getPreviewSink() : null;
            nextDueTick = AlignDueTickToDisplayClock(clockSink, nextDueTick, now);
            var remainingTicks = nextDueTick - now;
            if (remainingTicks > 0)
            {
                WaitForTicks(remainingTicks);
                continue;
            }

            var scheduleLateTicks = Math.Max(0, now - nextDueTick);
            RecordScheduleLate(scheduleLateTicks);
            var sink = clockSink ?? _getPreviewSink();
            if (sink == null)
            {
                ClearQueue();
                primed = false;
                continue;
            }

            if (_isPreviewSuppressed())
            {
                ResetForPreviewSuppression();
                primed = false;
                continue;
            }

            DropDeadlineExpiredFrames(now);
            DropLatencyOverflowFrames(now);
            MaybeDecreaseTargetDepth(now);

            var frame = TryDequeueCore(out var dequeueMissReason);
            if (frame == null)
            {
                if (dequeueMissReason == DequeueMissReason.WaitingForSequence)
                {
                    _signal.WaitOne(1);
                    continue;
                }

                if (dequeueMissReason == DequeueMissReason.EmptyQueue &&
                    TryRecordResumeReprimeMiss(now))
                {
                    primed = false;
                    continue;
                }

                Interlocked.Increment(ref _underflowCount);
                RecordUnderflow(now);
                IncreaseTargetDepth(now);
                primed = false;
                continue;
            }

            using (frame)
            {
                SubmitFrame(sink, frame);
            }

            var outputIntervalTicks = GetAdjustedOutputIntervalTicks();
            var submittedTick = Stopwatch.GetTimestamp();
            if (_displayClockPacingEnabled)
            {
                nextDueTick = AlignDueTickToDisplayClock(sink, submittedTick + outputIntervalTicks, submittedTick);
            }
            else if (scheduleLateTicks > _frameIntervalTicks * LateScheduleResetFrames)
            {
                nextDueTick = submittedTick + outputIntervalTicks;
            }
            else
            {
                nextDueTick += outputIntervalTicks;
            }
        }
    }

    private long AlignDueTickToDisplayClock(IPreviewFrameSink? sink, long currentDueTick, long nowTick)
    {
        if (!_displayClockPacingEnabled ||
            sink is not IPreviewDisplayClock displayClock ||
            !displayClock.TryGetDisplayClock(out var clock) ||
            clock.LastPresentTick <= 0)
        {
            return currentDueTick;
        }

        if (clock.LastPresentTick <= Interlocked.Read(ref _lastDisplayClockPacedPresentTick))
        {
            return currentDueTick;
        }

        var intervalTicks = clock.FrameIntervalTicks > 0 ? clock.FrameIntervalTicks : _frameIntervalTicks;
        var submitDelayTicks = MsToTicks(_displayClockSubmitDelayMs);
        var minLeadTicks = MsToTicks(_displayClockMinLeadMs);
        var nextPresentTick = clock.LastPresentTick + intervalTicks;
        while (nextPresentTick <= nowTick)
        {
            nextPresentTick += intervalTicks;
        }

        var preferredDueTick = clock.LastPresentTick + submitDelayTicks;
        while (preferredDueTick <= nowTick)
        {
            preferredDueTick += intervalTicks;
        }

        var latestSafeSubmitTick = nextPresentTick - minLeadTicks;
        if (nowTick <= latestSafeSubmitTick && preferredDueTick > latestSafeSubmitTick)
        {
            Interlocked.Exchange(ref _lastDisplayClockPacedPresentTick, clock.LastPresentTick);
            return nowTick;
        }

        if (preferredDueTick <= latestSafeSubmitTick)
        {
            Interlocked.Exchange(ref _lastDisplayClockPacedPresentTick, clock.LastPresentTick);
            return preferredDueTick;
        }

        Interlocked.Exchange(ref _lastDisplayClockPacedPresentTick, clock.LastPresentTick);
        return nextPresentTick + submitDelayTicks;
    }

    private void SubmitFrame(IPreviewFrameSink sink, BufferedFrame frame)
    {
        var submitTick = Stopwatch.GetTimestamp();
        var previewPresentId = Interlocked.Increment(ref _nextPreviewPresentId);
        try
        {
            if (frame.Lease != null)
            {
                var lease = frame.Lease;
                frame.Lease = null;
                try
                {
                    _previewFrameProbe?.Invoke(
                        lease.Memory.Span,
                        frame.Width,
                        frame.Height,
                        lease.PixelFormat,
                        frame.ArrivalTick,
                        frame.SequenceNumber);
                    sink.SubmitRawFrameLease(
                        lease,
                        isHdr: false,
                        PreviewFrameTracking.Default with
                        {
                            PreviewPresentId = previewPresentId,
                            SchedulerSubmitTick = submitTick,
                        });
                    lease = null;
                }
                finally
                {
                    lease?.Dispose();
                }
            }
            else
            {
                _previewFrameProbe?.Invoke(
                    frame.Buffer.AsSpan(0, frame.Length),
                    frame.Width,
                    frame.Height,
                    frame.PixelFormat,
                    frame.ArrivalTick,
                    frame.SequenceNumber);
                unsafe
                {
                    fixed (byte* pointer = frame.Buffer)
                    {
                        sink.SubmitRawFrame(
                            (IntPtr)pointer,
                            frame.Length,
                            frame.Width,
                            frame.Height,
                            false,
                            PreviewFrameTracking.Default with
                            {
                                ArrivalTick = frame.ArrivalTick,
                                SourceSequenceNumber = frame.SequenceNumber,
                                PreviewPresentId = previewPresentId,
                                SchedulerSubmitTick = submitTick,
                            });
                    }
                }
            }

            var now = Stopwatch.GetTimestamp();
            RecordSelectedFrame(frame, previewPresentId, submitTick);
            RecordOutputInterval(now);
            RecordQueueLatency(frame.EnqueueTick, now);
            Interlocked.Increment(ref _totalSubmitted);
        }
        catch (Exception ex)
        {
            RecordDroppedFrame(frame.SequenceNumber, "submit-failed");
            Interlocked.Increment(ref _totalDropped);
            Logger.Log($"MJPEG_PREVIEW_JITTER_SUBMIT_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void WaitForTicks(long ticks)
    {
        var deadline = Stopwatch.GetTimestamp() + ticks;

        while (Volatile.Read(ref _disposed) == 0)
        {
            var remainingTicks = deadline - Stopwatch.GetTimestamp();
            if (remainingTicks <= 0)
            {
                return;
            }

            var ms = remainingTicks * 1000.0 / Stopwatch.Frequency;
            if (ms >= 2.0)
            {
                Thread.Sleep(Math.Max(1, (int)Math.Floor(ms - 0.5)));
            }
            else if (ms > 0)
            {
                Thread.SpinWait(64);
            }
            else
            {
                return;
            }
        }
    }

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);
}
