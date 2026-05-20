using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task MjpegPreviewJitter_DropsSoftDeadlineOverflowToRecoverLatency()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 3);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var now = Stopwatch.GetTimestamp();
        var staleTick = now - Stopwatch.Frequency;

        for (var i = 0; i < 5; i++)
        {
            frames.Add(CreateRawBufferedFrame(bufferedFrameType, staleTick + i));
        }

        InvokeNonPublicInstanceMethod(jitter, "DropLatencyOverflowFrames", new object?[] { now });

        AssertEqual(3, frames.Count, "soft deadline overflow leaves target depth");
        AssertEqual(2L, GetLongPrivateField(jitter, "_deadlineDropCount"), "soft deadline overflow drops");
        AssertEqual(2L, GetLongPrivateField(jitter, "_totalDropped"), "soft deadline overflow total drops");
        AssertEqual(3, GetIntPrivateField(jitter, "_targetDepth"), "soft deadline overflow does not increase latency target");

        foreach (IDisposable frame in frames)
        {
            frame.Dispose();
        }

        frames.Clear();
        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_DropsExpiredFramesBelowTargetDepth()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 6);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var now = Stopwatch.GetTimestamp();
        var staleTick = now - Stopwatch.Frequency;

        frames.Add(CreateRawBufferedFrame(bufferedFrameType, staleTick));
        frames.Add(CreateRawBufferedFrame(bufferedFrameType, staleTick + 1));

        InvokeNonPublicInstanceMethod(jitter, "DropDeadlineExpiredFrames", new object?[] { now });

        AssertEqual(0, frames.Count, "expired preview frames below target depth");
        AssertEqual(2L, GetLongPrivateField(jitter, "_deadlineDropCount"), "deadline drops");
        AssertEqual(2L, GetLongPrivateField(jitter, "_totalDropped"), "total drops");
        AssertEqual(7, GetIntPrivateField(jitter, "_targetDepth"), "adaptive target after deadline drop");
        AssertEqual(1L, GetLongPrivateField(jitter, "_targetIncreaseCount"), "target increase count");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_SkipsMissingPreviewSequenceAfterDeadline()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 6);
        SetPrivateField(jitter, "_nextPreviewSequence", 10L);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var owner = CreatePooledVideoFrame(frameType, nv12, 12L, 100L, 200L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(owner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)owner).Dispose();

        frames.Add(CreateLeaseBufferedFrame(bufferedFrameType, lease, Stopwatch.GetTimestamp() - Stopwatch.Frequency));

        var dequeued = InvokeNonPublicInstanceMethod(jitter, "TryDequeue", null)
            ?? throw new InvalidOperationException("Expected jitter buffer to dequeue the first available frame after deadline.");

        AssertEqual(0, frames.Count, "remaining preview frame count");
        AssertEqual(13L, GetLongPrivateField(jitter, "_nextPreviewSequence"), "next preview sequence");
        AssertEqual(2L, GetLongPrivateField(jitter, "_deadlineDropCount"), "virtual deadline skips");
        AssertEqual(2L, GetLongPrivateField(jitter, "_totalDropped"), "total preview skips");
        AssertEqual(1L, GetLongPrivateField(jitter, "_targetIncreaseCount"), "target increase count after missing sequence");

        ((IDisposable)dequeued).Dispose();
        AssertEqual(1, pool.ReturnCount, "dequeued preview lease return count");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_LateSequenceDoesNotCountAsQueued()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 3);
        SetPrivateField(jitter, "_nextPreviewSequence", 10L);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var owner = CreatePooledVideoFrame(frameType, nv12, 9L, 100L, 200L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(owner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)owner).Dispose();

        var lateFrame = CreateLeaseBufferedFrame(bufferedFrameType, lease, Stopwatch.GetTimestamp());
        InvokeNonPublicInstanceMethod(jitter, "EnqueueBufferedFrame", new[] { lateFrame });

        AssertEqual(0, frames.Count, "late preview sequence was not queued");
        AssertEqual(0L, GetLongPrivateField(jitter, "_totalQueued"), "late preview sequence does not increment queued count");
        AssertEqual(1L, GetLongPrivateField(jitter, "_totalDropped"), "late preview sequence increments dropped count");
        AssertEqual(1L, GetLongPrivateField(jitter, "_deadlineDropCount"), "late preview sequence increments deadline drop count");
        AssertEqual(1, pool.ReturnCount, "late preview sequence returns frame lease");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_ClearResetsPreviewSequence()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 3);
        SetPrivateField(jitter, "_nextPreviewSequence", 10L);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();

        var clearedOwner = CreatePooledVideoFrame(frameType, nv12, 10L, 100L, 200L, 16, 16, 384, pool);
        var clearedLease = addLeaseMethod.Invoke(clearedOwner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)clearedOwner).Dispose();
        frames.Add(CreateLeaseBufferedFrame(bufferedFrameType, clearedLease, Stopwatch.GetTimestamp()));

        jitterType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance)!
            .Invoke(jitter, Array.Empty<object>());

        AssertEqual(0, frames.Count, "clear drains queued preview frames");
        AssertEqual(-1L, GetLongPrivateField(jitter, "_nextPreviewSequence"), "clear resets preview sequence");
        AssertEqual(1, pool.ReturnCount, "cleared preview lease return count");

        var resumedOwner = CreatePooledVideoFrame(frameType, nv12, 100L, 300L, 400L, 16, 16, 384, pool);
        var resumedLease = addLeaseMethod.Invoke(resumedOwner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)resumedOwner).Dispose();
        frames.Add(CreateLeaseBufferedFrame(bufferedFrameType, resumedLease, Stopwatch.GetTimestamp()));

        var dequeued = InvokeNonPublicInstanceMethod(jitter, "TryDequeue", null)
            ?? throw new InvalidOperationException("Expected jitter buffer to accept the first frame after clear.");

        AssertEqual(0L, GetLongPrivateField(jitter, "_underflowCount"), "clear resume does not create underflows");
        AssertEqual(0L, GetLongPrivateField(jitter, "_deadlineDropCount"), "clear resume does not create deadline skips");
        AssertEqual(101L, GetLongPrivateField(jitter, "_nextPreviewSequence"), "next preview sequence after resume");

        ((IDisposable)dequeued).Dispose();
        AssertEqual(2, pool.ReturnCount, "resumed preview lease return count");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_ReprimesAfterSuppressionResume()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 3);
        SetPrivateField(jitter, "_nextPreviewSequence", 10L);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();

        var staleOwner = CreatePooledVideoFrame(frameType, nv12, 10L, 100L, 200L, 16, 16, 384, pool);
        var staleLease = addLeaseMethod.Invoke(staleOwner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)staleOwner).Dispose();
        frames.Add(CreateLeaseBufferedFrame(bufferedFrameType, staleLease, Stopwatch.GetTimestamp()));

        jitterType.GetMethod("ResetForPreviewSuppression", BindingFlags.Public | BindingFlags.Instance)!
            .Invoke(jitter, Array.Empty<object>());

        AssertEqual(0, frames.Count, "suppression drains queued preview frames");
        AssertEqual(-1L, GetLongPrivateField(jitter, "_nextPreviewSequence"), "suppression resets preview sequence");
        AssertEqual("suppressed", GetStringPrivateField(jitter, "_lastDropReason"), "suppression drop reason");
        AssertEqual(1, pool.ReturnCount, "suppressed preview lease return count");

        var raceOwner = CreatePooledVideoFrame(frameType, nv12, 11L, 300L, 400L, 16, 16, 384, pool);
        var raceLease = addLeaseMethod.Invoke(raceOwner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)raceOwner).Dispose();
        jitterType.GetMethod("Enqueue", new[] { raceLease.GetType() })!
            .Invoke(jitter, new[] { raceLease });

        AssertEqual(0, frames.Count, "suppressed enqueue is rejected");
        AssertEqual(2, pool.ReturnCount, "suppressed enqueue returns raced preview lease");

        jitterType.GetMethod("ReprimeAfterPreviewResume", BindingFlags.Public | BindingFlags.Instance)!
            .Invoke(jitter, Array.Empty<object>());

        var now = Stopwatch.GetTimestamp();
        AssertEqual(
            true,
            (bool)(InvokeNonPublicInstanceMethod(jitter, "TryRecordResumeReprimeMiss", new object?[] { now })
                   ?? throw new InvalidOperationException("TryRecordResumeReprimeMiss returned null.")),
            "first resume miss is reclassified");
        AssertEqual(1L, GetLongPrivateField(jitter, "_resumeReprimeCount"), "resume reprime count");
        AssertEqual(0L, GetLongPrivateField(jitter, "_underflowCount"), "resume reprime does not increment underflows");
        AssertEqual("resume-reprime", GetStringPrivateField(jitter, "_lastUnderflowReason"), "resume reprime reason");
        AssertEqual(
            false,
            (bool)(InvokeNonPublicInstanceMethod(jitter, "TryRecordResumeReprimeMiss", new object?[] { now })
                   ?? throw new InvalidOperationException("TryRecordResumeReprimeMiss returned null.")),
            "resume reprime budget is single-use");
        SetPrivateField(jitter, "_resumeReprimeMissBudget", 1);
        SetPrivateField(jitter, "_resumeReprimeStartTick", now - Stopwatch.Frequency);
        AssertEqual(
            false,
            (bool)(InvokeNonPublicInstanceMethod(jitter, "TryRecordResumeReprimeMiss", new object?[] { now })
                   ?? throw new InvalidOperationException("TryRecordResumeReprimeMiss returned null.")),
            "stale resume reprime budget does not mask later underflows");
        AssertEqual(1L, GetLongPrivateField(jitter, "_resumeReprimeCount"), "stale reprime does not increment count");

        return Task.CompletedTask;
    }
}
