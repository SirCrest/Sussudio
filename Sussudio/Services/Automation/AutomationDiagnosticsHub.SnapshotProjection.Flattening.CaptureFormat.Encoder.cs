namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            InputPixelFormat = captureFormat.EncoderInputPixelFormat,
            OutputPixelFormat = captureFormat.EncoderOutputPixelFormat,
            VideoCodec = captureFormat.EncoderVideoCodec,
            VideoProfile = captureFormat.EncoderVideoProfile,
            TenBitPipelineConfirmed = captureFormat.EncoderTenBitPipelineConfirmed
        };

    private readonly record struct CaptureFormatEncoderFlattenedProjection
    {
        public string? InputPixelFormat { get; init; }
        public string? OutputPixelFormat { get; init; }
        public string? VideoCodec { get; init; }
        public string? VideoProfile { get; init; }
        public bool? TenBitPipelineConfirmed { get; init; }
    }
}
