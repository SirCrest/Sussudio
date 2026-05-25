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
        var source = ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FrameIngress.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Metrics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FramePacing.cs");
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
        var frameIngressText = ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FrameIngress.cs")
            .Replace("\r\n", "\n");
        var framePacingText = ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FramePacing.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Metrics.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameIngressText, "private sealed class BufferedFrame : IDisposable");
        AssertContains(frameIngressText, "public void Enqueue(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick)");
        AssertContains(frameIngressText, "public void Enqueue(PooledVideoFrameLease frame)");
        AssertContains(frameIngressText, "private void EnqueueBufferedFrame(BufferedFrame frame)");
        AssertContains(frameIngressText, "private bool AddFrameInOrder(BufferedFrame frame)");
        AssertContains(frameIngressText, "private BufferedFrame RemoveOldestFrame()");
        AssertContains(frameIngressText, "private bool TryRecordResumeReprimeMiss(long nowTick)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MjpegPreviewJitterBuffer.Queue.cs")),
            "MJPEG preview jitter queue ordering folded into frame ingress owner");
        AssertDoesNotContain(rootText, "private sealed class BufferedFrame : IDisposable");
        AssertDoesNotContain(rootText, "public void Enqueue(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick)");
        AssertDoesNotContain(rootText, "public void Enqueue(PooledVideoFrameLease frame)");
        AssertDoesNotContain(rootText, "private void EnqueueBufferedFrame(BufferedFrame frame)");
        AssertDoesNotContain(rootText, "private bool AddFrameInOrder(BufferedFrame frame)");
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
            "MJPEG preview adaptive deadline/depth policy folded into frame pacing owner");
        AssertDoesNotContain(rootText, "private long AlignDueTickToDisplayClock(");
        AssertDoesNotContain(rootText, "private void SubmitFrame(IPreviewFrameSink sink, BufferedFrame frame)");
        AssertContains(metricsText, "public Metrics GetMetrics()");
        AssertContains(metricsText, "private void RecordInputInterval(long nowTick)");
        AssertContains(metricsText, "private void RecordDroppedFrame(long sourceSequenceNumber, string reason)");

        return Task.CompletedTask;
    }
}
