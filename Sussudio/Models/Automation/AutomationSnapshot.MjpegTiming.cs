using System;

namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public int MjpegDecodeSampleCount { get; init; }
    public double MjpegDecodeAvgMs { get; init; }
    public double MjpegDecodeP95Ms { get; init; }
    public double MjpegDecodeMaxMs { get; init; }
    public int MjpegInteropCopySampleCount { get; init; }
    public double MjpegInteropCopyAvgMs { get; init; }
    public double MjpegInteropCopyP95Ms { get; init; }
    public double MjpegInteropCopyMaxMs { get; init; }
    public int MjpegCallbackSampleCount { get; init; }
    public double MjpegCallbackAvgMs { get; init; }
    public double MjpegCallbackP95Ms { get; init; }
    public double MjpegCallbackMaxMs { get; init; }
    public int MjpegDecoderCount { get; init; }
    public int MjpegReorderSampleCount { get; init; }
    public double MjpegReorderAvgMs { get; init; }
    public double MjpegReorderP95Ms { get; init; }
    public double MjpegReorderMaxMs { get; init; }
    public int MjpegPipelineSampleCount { get; init; }
    public double MjpegPipelineAvgMs { get; init; }
    public double MjpegPipelineP95Ms { get; init; }
    public double MjpegPipelineMaxMs { get; init; }
    public long MjpegTotalDecoded { get; init; }
    public long MjpegTotalEmitted { get; init; }
    public long MjpegTotalDropped { get; init; }
    public long MjpegCompressedFramesQueued { get; init; }
    public long MjpegCompressedFramesDequeued { get; init; }
    public long MjpegCompressedDropsQueueFull { get; init; }
    public long MjpegCompressedDropsByteBudget { get; init; }
    public long MjpegCompressedDropsDisposed { get; init; }
    public long MjpegDecodeFailures { get; init; }
    public long MjpegReorderCollisions { get; init; }
    public long MjpegEmitFailures { get; init; }
    public int MjpegCompressedQueueDepth { get; init; }
    public long MjpegCompressedQueueBytes { get; init; }
    public long MjpegCompressedQueueByteBudget { get; init; }
    public long MjpegReorderSkips { get; init; }
    public int MjpegReorderBufferDepth { get; init; }
    public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder { get; init; } = Array.Empty<MjpegDecoderAutomationSnapshot>();
}
