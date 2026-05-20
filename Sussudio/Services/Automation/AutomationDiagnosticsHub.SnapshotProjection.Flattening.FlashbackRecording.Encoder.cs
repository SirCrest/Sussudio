namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(
        FlashbackRecordingEncoderProjection encoder)
        => new()
        {
            CodecName = encoder.CodecName,
            TargetBitRate = encoder.TargetBitRate,
            Width = encoder.Width,
            Height = encoder.Height,
            FrameRate = encoder.FrameRate,
            FrameRateNumerator = encoder.FrameRateNumerator,
            FrameRateDenominator = encoder.FrameRateDenominator
        };

    private readonly record struct FlashbackRecordingEncoderFlattenedProjection
    {
        public string? CodecName { get; init; }
        public uint TargetBitRate { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public double FrameRate { get; init; }
        public int? FrameRateNumerator { get; init; }
        public int? FrameRateDenominator { get; init; }
    }
}
