using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task MjpegPreviewJitter_ExposesAdaptiveDeadlinePolicy()
    {
        var source = ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs");
        var pipelineSource = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs");
        var captureSource = ReadUnifiedVideoCaptureSource();
        AssertContains(source, "DropDeadlineExpiredFrames");
        AssertContains(source, "DropLatencyOverflowFrames");
        AssertContains(source, "SoftDeadlineExtraFrames = 2");
        AssertContains(source, "AggressiveCatchUpSurplusFrames = 4");
        AssertContains(source, "IncreaseTargetDepth");
        AssertContains(source, "MaybeDecreaseTargetDepth");
        AssertContains(source, "HasLatencyPressure");
        AssertContains(source, "GetAdjustedOutputIntervalTicks");
        AssertContains(source, "private enum DequeueMissReason");
        AssertContains(source, "TryDequeueCore(out var dequeueMissReason)");
        AssertContains(source, "dequeueMissReason == DequeueMissReason.WaitingForSequence");
        AssertContains(source, "_signal.WaitOne(1);");
        AssertContains(source, "DeadlineDropCount");
        AssertContains(source, "TargetIncreaseCount");
        AssertContains(source, "TargetDecreaseCount");
        AssertContains(source, "LastSelectedPreviewPresentId");
        AssertContains(source, "LastSelectedSourceSequenceNumber");
        AssertContains(source, "RecordSelectedFrame");
        AssertContains(source, "RecordDroppedFrame");
        AssertContains(source, "ResetForPreviewSuppression");
        AssertContains(source, "ReprimeAfterPreviewResume");
        AssertContains(source, "TryRecordResumeReprimeMiss");
        AssertContains(source, "ResumeReprimeCount");
        AssertContains(source, "if (AddFrameInOrder(frame))");
        AssertContains(source, "private bool AddFrameInOrder(BufferedFrame frame)");
        AssertContains(source, "return false;");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_MIN_TARGET_DEPTH");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_MAX_TARGET_DEPTH");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_MAX_DEPTH");
        AssertContains(source, "SUSSUDIO_PREVIEW_DISPLAY_CLOCK_PACING\", 1");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(pipelineSource, "PreviewFrameCallback");
        AssertContains(pipelineSource, "NotifyPreviewFrameDecoded");
        AssertContains(captureSource, "OnMjpegPipelinePreviewFrameDecoded");
        AssertContains(captureSource, "Volatile.Read(ref _mjpegPreviewJitterBuffer)?.ResetForPreviewSuppression()");
        AssertContains(captureSource, "Volatile.Read(ref _mjpegPreviewJitterBuffer)?.ReprimeAfterPreviewResume()");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_EmitLoopLivesWithLifecycleRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs")
            .Replace("\r\n", "\n");
        var queueIngressText = rootText;
        var framePacingText = rootText;
        var metricsText = rootText;

        AssertContains(queueIngressText, "private sealed class BufferedFrame : IDisposable");
        AssertContains(queueIngressText, "public void Enqueue(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick)");
        AssertContains(queueIngressText, "public void Enqueue(PooledVideoFrameLease frame)");
        AssertContains(queueIngressText, "private void EnqueueBufferedFrame(BufferedFrame frame)");
        AssertContains(queueIngressText, "private bool AddFrameInOrder(BufferedFrame frame)");
        AssertContains(queueIngressText, "private BufferedFrame RemoveOldestFrame()");
        AssertContains(queueIngressText, "private bool TryRecordResumeReprimeMiss(long nowTick)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MjpegPreviewJitterBuffer.FrameIngress.cs")),
            "MJPEG preview jitter queue ingress folded into the lifecycle root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MjpegPreviewJitterBuffer.Queue.cs")),
            "MJPEG preview jitter queue ordering folded into frame ingress owner");
        AssertContains(rootText, "private void EmitLoop()");
        AssertContains(rootText, "MmcssThreadRegistration.TryRegister(_mmcssTask, _mmcssPriority");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MjpegPreviewJitterBuffer.EmitLoop.cs")),
            "MJPEG preview jitter emit loop stays folded into the lifecycle root");
        AssertContains(framePacingText, "private long AlignDueTickToDisplayClock(IPreviewFrameSink? sink, long currentDueTick, long nowTick)");
        AssertContains(framePacingText, "private void SubmitFrame(IPreviewFrameSink sink, BufferedFrame frame)");
        AssertContains(framePacingText, "private void WaitForTicks(long ticks)");
        AssertContains(framePacingText, "private static extern uint timeBeginPeriod(uint uPeriod);");
        AssertContains(framePacingText, "private static extern uint timeEndPeriod(uint uPeriod);");
        AssertContains(framePacingText, "private void DropDeadlineExpiredFrames(long nowTick)");
        AssertContains(framePacingText, "private void IncreaseTargetDepth(long nowTick)");
        AssertContains(framePacingText, "private bool HasLatencyPressure(long nowTick)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MjpegPreviewJitterBuffer.Adaptive.cs")),
            "MJPEG preview adaptive deadline/depth policy folded into the lifecycle root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MjpegPreviewJitterBuffer.FramePacing.cs")),
            "MJPEG preview frame pacing folded into the lifecycle root");
        AssertContains(rootText, "private long AlignDueTickToDisplayClock(");
        AssertContains(rootText, "private void SubmitFrame(IPreviewFrameSink sink, BufferedFrame frame)");
        AssertContains(metricsText, "public Metrics GetMetrics()");
        AssertContains(metricsText, "private void RecordInputInterval(long nowTick)");
        AssertContains(metricsText, "private void RecordDroppedFrame(long sourceSequenceNumber, string reason)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MjpegPreviewJitterBuffer.Metrics.cs")),
            "MJPEG preview jitter metrics folded into the lifecycle root");

        return Task.CompletedTask;
    }

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
