using System;
using System.Diagnostics;
using System.Threading;
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

}
