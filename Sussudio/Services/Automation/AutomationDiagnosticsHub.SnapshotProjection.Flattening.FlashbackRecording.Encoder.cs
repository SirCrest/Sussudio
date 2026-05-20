namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(
        FlashbackRecordingProjection flashbackRecording)
        => new()
        {
            CodecName = flashbackRecording.EncoderCodecName,
            TargetBitRate = flashbackRecording.EncoderTargetBitRate,
            Width = flashbackRecording.EncoderWidth,
            Height = flashbackRecording.EncoderHeight,
            FrameRate = flashbackRecording.EncoderFrameRate,
            FrameRateNumerator = flashbackRecording.EncoderFrameRateNumerator,
            FrameRateDenominator = flashbackRecording.EncoderFrameRateDenominator
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
