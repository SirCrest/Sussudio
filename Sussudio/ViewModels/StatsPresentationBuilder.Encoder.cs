namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    private static StatsEncoderPresentation BuildEncoderPresentation(StatsSnapshot snapshot)
    {
        var driftVisible = snapshot.Recording && snapshot.AvSyncEncoderDriftMs.HasValue;
        var drift = driftVisible
            ? FormatEncoderDrift(snapshot)
            : string.Empty;

        if (string.IsNullOrEmpty(snapshot.EncoderCodecName))
        {
            return new StatsEncoderPresentation(
                DriftVisible: driftVisible,
                Drift: drift,
                Active: false,
                Codec: string.Empty,
                Resolution: string.Empty,
                FrameRate: string.Empty,
                Bitrate: string.Empty);
        }

        return new StatsEncoderPresentation(
            DriftVisible: driftVisible,
            Drift: drift,
            Active: true,
            Codec: FormatEncoderCodecName(snapshot.EncoderCodecName),
            Resolution: $"{snapshot.EncoderWidth} x {snapshot.EncoderHeight}",
            FrameRate: $"{snapshot.EncoderFrameRate:0.##} fps",
            Bitrate: FormatEncoderBitrate(snapshot.EncoderTargetBitRate));
    }

    private static string FormatEncoderCodecName(string codecName)
    {
        return codecName switch
        {
            "hevc_nvenc" => "HEVC (NVENC)",
            "h264_nvenc" => "H.264 (NVENC)",
            "av1_nvenc" => "AV1 (NVENC)",
            _ => codecName
        };
    }

    private static string FormatEncoderBitrate(uint targetBitRate)
    {
        var mbps = targetBitRate / 1_000_000.0;
        return $"{mbps:0.#} Mbps";
    }

    private static string FormatEncoderDrift(StatsSnapshot snapshot)
        => $"{FormatSignedMs(snapshot.AvSyncEncoderDriftMs)} ({snapshot.AvSyncEncoderCorrectionSamples ?? 0} corr)";

    private readonly record struct StatsEncoderPresentation(
        bool DriftVisible,
        string Drift,
        bool Active,
        string Codec,
        string Resolution,
        string FrameRate,
        string Bitrate);
}
