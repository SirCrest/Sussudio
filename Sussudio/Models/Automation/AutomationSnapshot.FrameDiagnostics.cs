using System;

namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public long EstimatedPipelineLatencyMs { get; init; }
    public double ExpectedCaptureFrameRate { get; init; }
    public int CaptureCadenceSampleCount { get; init; }
    public double CaptureCadenceObservedFps { get; init; }
    public double CaptureCadenceExpectedIntervalMs { get; init; }
    public double CaptureCadenceAverageIntervalMs { get; init; }
    public double CaptureCadenceP95IntervalMs { get; init; }
    public double CaptureCadenceP99IntervalMs { get; init; }
    public double CaptureCadenceMaxIntervalMs { get; init; }
    public double CaptureCadenceOnePercentLowFps { get; init; }
    public double CaptureCadenceFivePercentLowFps { get; init; }
    public double CaptureCadenceSampleDurationMs { get; init; }
    public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();
    public double CaptureCadenceJitterStdDevMs { get; init; }
    public long CaptureCadenceSevereGapCount { get; init; }
    public long CaptureCadenceEstimatedDroppedFrames { get; init; }
    public double CaptureCadenceEstimatedDropPercent { get; init; }

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

    public bool MjpegPreviewJitterEnabled { get; init; }
    public int MjpegPreviewJitterTargetDepth { get; init; }
    public int MjpegPreviewJitterMaxDepth { get; init; }
    public int MjpegPreviewJitterQueueDepth { get; init; }
    public long MjpegPreviewJitterTotalQueued { get; init; }
    public long MjpegPreviewJitterTotalSubmitted { get; init; }
    public long MjpegPreviewJitterTotalDropped { get; init; }
    public long MjpegPreviewJitterUnderflowCount { get; init; }
    public long MjpegPreviewJitterResumeReprimeCount { get; init; }
    public int MjpegPreviewJitterInputSampleCount { get; init; }
    public double MjpegPreviewJitterInputAvgMs { get; init; }
    public double MjpegPreviewJitterInputP95Ms { get; init; }
    public double MjpegPreviewJitterInputMaxMs { get; init; }
    public int MjpegPreviewJitterOutputSampleCount { get; init; }
    public double MjpegPreviewJitterOutputAvgMs { get; init; }
    public double MjpegPreviewJitterOutputP95Ms { get; init; }
    public double MjpegPreviewJitterOutputMaxMs { get; init; }
    public int MjpegPreviewJitterLatencySampleCount { get; init; }
    public double MjpegPreviewJitterLatencyAvgMs { get; init; }
    public double MjpegPreviewJitterLatencyP95Ms { get; init; }
    public double MjpegPreviewJitterLatencyMaxMs { get; init; }
    public long MjpegPreviewJitterDeadlineDropCount { get; init; }
    public long MjpegPreviewJitterClearedDropCount { get; init; }
    public long MjpegPreviewJitterTargetIncreaseCount { get; init; }
    public long MjpegPreviewJitterTargetDecreaseCount { get; init; }
    public long MjpegPreviewJitterLastSelectedPreviewPresentId { get; init; }
    public long MjpegPreviewJitterLastSelectedSourceSequenceNumber { get; init; }
    public long MjpegPreviewJitterLastSelectedQpc { get; init; }
    public double MjpegPreviewJitterLastSelectedSourceLatencyMs { get; init; }
    public long MjpegPreviewJitterLastDroppedSourceSequenceNumber { get; init; }
    public long MjpegPreviewJitterLastDropQpc { get; init; }
    public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;
    public long MjpegPreviewJitterLastUnderflowQpc { get; init; }
    public string MjpegPreviewJitterLastUnderflowReason { get; init; } = string.Empty;
    public int MjpegPreviewJitterLastUnderflowQueueDepth { get; init; }
    public double MjpegPreviewJitterLastUnderflowInputAgeMs { get; init; }
    public double MjpegPreviewJitterLastUnderflowOutputAgeMs { get; init; }
    public double MjpegPreviewJitterLastScheduleLateMs { get; init; }
    public double MjpegPreviewJitterMaxScheduleLateMs { get; init; }
    public long MjpegPreviewJitterScheduleLateCount { get; init; }

    public int MjpegPacketHashSampleCount { get; init; }
    public long MjpegPacketHashUniqueFrameCount { get; init; }
    public long MjpegPacketHashDuplicateFrameCount { get; init; }
    public long MjpegPacketHashLongestDuplicateRun { get; init; }
    public double MjpegPacketHashInputObservedFps { get; init; }
    public double MjpegPacketHashUniqueObservedFps { get; init; }
    public double MjpegPacketHashDuplicateFramePercent { get; init; }
    public string MjpegPacketHashLastHash { get; init; } = string.Empty;
    public bool MjpegPacketHashLastFrameDuplicate { get; init; }
    public string MjpegPacketHashPattern { get; init; } = "NoSamples";
    public double[] MjpegPacketHashRecentInputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] MjpegPacketHashRecentUniqueIntervalsMs { get; init; } = Array.Empty<double>();
    public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();

    public int VisualCadenceSampleCount { get; init; }
    public long VisualCadenceChangedFrameCount { get; init; }
    public long VisualCadenceRepeatFrameCount { get; init; }
    public long VisualCadenceLongestRepeatRun { get; init; }
    public double VisualCadenceOutputObservedFps { get; init; }
    public double VisualCadenceChangeObservedFps { get; init; }
    public double VisualCadenceRepeatFramePercent { get; init; }
    public double VisualCadenceLastDelta { get; init; }
    public double VisualCadenceAverageDelta { get; init; }
    public double VisualCadenceP95Delta { get; init; }
    public double VisualCadenceMotionScore { get; init; }
    public string VisualCadenceMotionConfidence { get; init; } = "NoSamples";
    public double[] VisualCadenceRecentOutputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] VisualCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();
    public int VisualCenterCadenceSampleCount { get; init; }
    public long VisualCenterCadenceChangedFrameCount { get; init; }
    public long VisualCenterCadenceRepeatFrameCount { get; init; }
    public long VisualCenterCadenceLongestRepeatRun { get; init; }
    public double VisualCenterCadenceOutputObservedFps { get; init; }
    public double VisualCenterCadenceChangeObservedFps { get; init; }
    public double VisualCenterCadenceRepeatFramePercent { get; init; }
    public double VisualCenterCadenceLastDelta { get; init; }
    public double VisualCenterCadenceAverageDelta { get; init; }
    public double VisualCenterCadenceP95Delta { get; init; }
    public double VisualCenterCadenceMotionScore { get; init; }
    public string VisualCenterCadenceMotionConfidence { get; init; } = "NoSamples";
    public double[] VisualCenterCadenceRecentOutputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();
}
