namespace ElgatoCapture.Models;

public sealed class EncoderSupport
{
    public bool HasH264Nvenc { get; init; }
    public bool HasHevcNvenc { get; init; }
    public bool HasAv1Nvenc { get; init; }

    public bool HasLibX264 { get; init; }
    public bool HasLibX265 { get; init; }
    public bool HasLibSvtAv1 { get; init; }
    public bool HasLibAomAv1 { get; init; }

    public bool HasH264 => HasH264Nvenc || HasLibX264;
    public bool HasHevc => HasHevcNvenc || HasLibX265;
    public bool HasAv1 => HasAv1Nvenc || HasLibSvtAv1 || HasLibAomAv1;

    public string? PreferredAv1Encoder
        => HasAv1Nvenc ? "av1_nvenc"
        : HasLibSvtAv1 ? "libsvtav1"
        : HasLibAomAv1 ? "libaom-av1"
        : null;

    public static EncoderSupport Empty { get; } = new();
}
