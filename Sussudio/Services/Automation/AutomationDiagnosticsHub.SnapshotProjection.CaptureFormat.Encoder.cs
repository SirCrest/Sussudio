using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatEncoderProjection BuildCaptureFormatEncoderProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            InputPixelFormat = captureRuntime.EncoderInputPixelFormat,
            OutputPixelFormat = captureRuntime.EncoderOutputPixelFormat,
            VideoCodec = captureRuntime.EncoderVideoCodec,
            VideoProfile = captureRuntime.EncoderVideoProfile,
            TenBitPipelineConfirmed = captureRuntime.EncoderTenBitPipelineConfirmed
        };

    private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            InputPixelFormat = captureFormat.Encoder.InputPixelFormat,
            OutputPixelFormat = captureFormat.Encoder.OutputPixelFormat,
            VideoCodec = captureFormat.Encoder.VideoCodec,
            VideoProfile = captureFormat.Encoder.VideoProfile,
            TenBitPipelineConfirmed = captureFormat.Encoder.TenBitPipelineConfirmed
        };

    private readonly record struct CaptureFormatEncoderProjection
    {
        public string? InputPixelFormat { get; init; }
        public string? OutputPixelFormat { get; init; }
        public string? VideoCodec { get; init; }
        public string? VideoProfile { get; init; }
        public bool? TenBitPipelineConfirmed { get; init; }
    }

    private readonly record struct CaptureFormatEncoderFlattenedProjection
    {
        public string? InputPixelFormat { get; init; }
        public string? OutputPixelFormat { get; init; }
        public string? VideoCodec { get; init; }
        public string? VideoProfile { get; init; }
        public bool? TenBitPipelineConfirmed { get; init; }
    }
}
