using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)
        => new()
        {
            DecodeSampleCount = health.MjpegDecodeSampleCount,
            DecodeAvgMs = health.MjpegDecodeAvgMs,
            DecodeP95Ms = health.MjpegDecodeP95Ms,
            DecodeMaxMs = health.MjpegDecodeMaxMs,
            InteropCopySampleCount = health.MjpegInteropCopySampleCount,
            InteropCopyAvgMs = health.MjpegInteropCopyAvgMs,
            InteropCopyP95Ms = health.MjpegInteropCopyP95Ms,
            InteropCopyMaxMs = health.MjpegInteropCopyMaxMs,
            CallbackSampleCount = health.MjpegCallbackSampleCount,
            CallbackAvgMs = health.MjpegCallbackAvgMs,
            CallbackP95Ms = health.MjpegCallbackP95Ms,
            CallbackMaxMs = health.MjpegCallbackMaxMs,
            DecoderCount = health.MjpegDecoderCount,
            ReorderSampleCount = health.MjpegReorderSampleCount,
            ReorderAvgMs = health.MjpegReorderAvgMs,
            ReorderP95Ms = health.MjpegReorderP95Ms,
            ReorderMaxMs = health.MjpegReorderMaxMs,
            PipelineSampleCount = health.MjpegPipelineSampleCount,
            PipelineAvgMs = health.MjpegPipelineAvgMs,
            PipelineP95Ms = health.MjpegPipelineP95Ms,
            PipelineMaxMs = health.MjpegPipelineMaxMs,
            PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder
                ? Array.ConvertAll(
                    perDecoder,
                    worker => new MjpegDecoderAutomationSnapshot(
                        worker.WorkerIndex,
                        worker.SampleCount,
                        worker.AvgMs,
                        worker.P95Ms,
                        worker.MaxMs))
                : Array.Empty<MjpegDecoderAutomationSnapshot>()
        };

    private readonly record struct MjpegTimingProjection
    {
        public int DecodeSampleCount { get; init; }
        public double DecodeAvgMs { get; init; }
        public double DecodeP95Ms { get; init; }
        public double DecodeMaxMs { get; init; }
        public int InteropCopySampleCount { get; init; }
        public double InteropCopyAvgMs { get; init; }
        public double InteropCopyP95Ms { get; init; }
        public double InteropCopyMaxMs { get; init; }
        public int CallbackSampleCount { get; init; }
        public double CallbackAvgMs { get; init; }
        public double CallbackP95Ms { get; init; }
        public double CallbackMaxMs { get; init; }
        public int DecoderCount { get; init; }
        public int ReorderSampleCount { get; init; }
        public double ReorderAvgMs { get; init; }
        public double ReorderP95Ms { get; init; }
        public double ReorderMaxMs { get; init; }
        public int PipelineSampleCount { get; init; }
        public double PipelineAvgMs { get; init; }
        public double PipelineP95Ms { get; init; }
        public double PipelineMaxMs { get; init; }
        public MjpegDecoderAutomationSnapshot[] PerDecoder { get; init; }
    }

    private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(
        MjpegTimingProjection timing)
        => new()
        {
            DecodeSampleCount = timing.DecodeSampleCount,
            DecodeAvgMs = timing.DecodeAvgMs,
            DecodeP95Ms = timing.DecodeP95Ms,
            DecodeMaxMs = timing.DecodeMaxMs,
            InteropCopySampleCount = timing.InteropCopySampleCount,
            InteropCopyAvgMs = timing.InteropCopyAvgMs,
            InteropCopyP95Ms = timing.InteropCopyP95Ms,
            InteropCopyMaxMs = timing.InteropCopyMaxMs,
            CallbackSampleCount = timing.CallbackSampleCount,
            CallbackAvgMs = timing.CallbackAvgMs,
            CallbackP95Ms = timing.CallbackP95Ms,
            CallbackMaxMs = timing.CallbackMaxMs,
            DecoderCount = timing.DecoderCount,
            ReorderSampleCount = timing.ReorderSampleCount,
            ReorderAvgMs = timing.ReorderAvgMs,
            ReorderP95Ms = timing.ReorderP95Ms,
            ReorderMaxMs = timing.ReorderMaxMs,
            PipelineSampleCount = timing.PipelineSampleCount,
            PipelineAvgMs = timing.PipelineAvgMs,
            PipelineP95Ms = timing.PipelineP95Ms,
            PipelineMaxMs = timing.PipelineMaxMs,
            PerDecoder = timing.PerDecoder
        };

    private readonly record struct MjpegTimingFlattenedProjection
    {
        public int DecodeSampleCount { get; init; }
        public double DecodeAvgMs { get; init; }
        public double DecodeP95Ms { get; init; }
        public double DecodeMaxMs { get; init; }
        public int InteropCopySampleCount { get; init; }
        public double InteropCopyAvgMs { get; init; }
        public double InteropCopyP95Ms { get; init; }
        public double InteropCopyMaxMs { get; init; }
        public int CallbackSampleCount { get; init; }
        public double CallbackAvgMs { get; init; }
        public double CallbackP95Ms { get; init; }
        public double CallbackMaxMs { get; init; }
        public int DecoderCount { get; init; }
        public int ReorderSampleCount { get; init; }
        public double ReorderAvgMs { get; init; }
        public double ReorderP95Ms { get; init; }
        public double ReorderMaxMs { get; init; }
        public int PipelineSampleCount { get; init; }
        public double PipelineAvgMs { get; init; }
        public double PipelineP95Ms { get; init; }
        public double PipelineMaxMs { get; init; }
        public MjpegDecoderAutomationSnapshot[] PerDecoder { get; init; }
    }
}
