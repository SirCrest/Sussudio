using System;
using System.IO;

namespace ElgatoCapture.Models;

public enum RecordingFormat
{
    H264Mp4,
    HevcMp4,
    Av1Mp4
}

public enum VideoQuality
{
    Auto,       // Let the encoder decide based on resolution
    Low,        // ~8 Mbps for 1080p, scales with resolution
    Medium,     // ~15 Mbps for 1080p
    High,       // ~25 Mbps for 1080p
    SuperHigh,  // ~40 Mbps for 1080p
    Custom      // User-specified bitrate
}

public enum HdrOutputMode
{
    Off,
    Hdr10Pq
}

public enum PreviewMode
{
    GpuFast,
    TrueHdr
}

public class CaptureSettings
{
    public uint Width { get; set; } = 1920;
    public uint Height { get; set; } = 1080;
    public double FrameRate { get; set; } = 60;
    public string? RequestedFrameRateArg { get; set; }
    public uint? RequestedFrameRateNumerator { get; set; }
    public uint? RequestedFrameRateDenominator { get; set; }
    public string? RequestedPixelFormat { get; set; }
    public RecordingFormat Format { get; set; } = RecordingFormat.H264Mp4;
    public VideoQuality Quality { get; set; } = VideoQuality.High;
    public string NvencPreset { get; set; } = "Auto";
    public string SplitEncodeMode { get; set; } = "Auto";
    public double CustomBitrateMbps { get; set; } = 50; // Used when Quality is Custom
    public bool HdrEnabled { get; set; }
    public HdrOutputMode HdrOutputMode { get; set; } = HdrOutputMode.Hdr10Pq;
    public int HdrNominalPeakNits { get; set; } = 1000;
    // Optional HDR10 static metadata (only emitted when explicitly configured).
    public int HdrMaxCll { get; set; }
    public int HdrMaxFall { get; set; }
    public string HdrMasterDisplayMetadata { get; set; } = string.Empty;
    public PreviewMode PreviewMode { get; set; } = PreviewMode.GpuFast;
    public string OutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    public bool AudioEnabled { get; set; } = true;
    public bool UseCustomAudioInput { get; set; }
    public string? AudioDeviceId { get; set; }
    public string? AudioDeviceName { get; set; }
    public bool MicrophoneEnabled { get; set; }
    public string? MicrophoneDeviceId { get; set; }
    public string? MicrophoneDeviceName { get; set; }
    public AudioPathMode AudioPathMode { get; set; } = AudioPathMode.PostMuxDefault;
    public RecordingPipelineOptions PipelineOptions { get; set; } = new();
    public bool ForceMjpegDecode { get; set; }
    public bool FlashbackGpuDecode { get; set; } = true;
    public int FlashbackBufferMinutes { get; set; } = 5;
    public int MjpegDecoderCount { get; set; } = 6;

    public bool UseMjpegHighFrameRateMode =>
        IsMjpegHighFrameRateMode(RequestedPixelFormat, Width, Height, FrameRate, HdrEnabled, ForceMjpegDecode);

    public static bool IsMjpegHighFrameRateMode(
        string? requestedPixelFormat,
        uint width,
        uint height,
        double frameRate,
        bool hdrEnabled,
        bool force = false)
    {
        if (hdrEnabled)
        {
            return false;
        }

        if (!string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return force || (width >= 3840 && height >= 2160 && frameRate >= 100);
    }

    /// <summary>
    /// Calculates the target video bitrate based on quality setting, resolution, and frame rate.
    /// Returns bitrate in bits per second.
    /// </summary>
    public uint GetTargetBitrate()
    {
        // For Custom quality, use the user-specified bitrate directly
        if (Quality == VideoQuality.Custom)
        {
            var customMbps = Math.Clamp(CustomBitrateMbps, 1, 300);
            return (uint)(customMbps * 1_000_000);
        }

        // Base bitrates for 1080p30 (in Mbps)
        double baseMbps = Quality switch
        {
            VideoQuality.Low => 8,
            VideoQuality.Medium => 15,
            VideoQuality.High => 25,
            VideoQuality.SuperHigh => 40,
            VideoQuality.Auto => 20, // Default for Auto
            _ => 20
        };

        // Scale by resolution (relative to 1080p = 2,073,600 pixels)
        double pixelCount = Width * Height;
        double resolutionScale = pixelCount / 2_073_600.0;

        // Scale by frame rate (relative to 30fps)
        double frameRateScale = FrameRate / 30.0;

        // Codec efficiency factors (lower = more efficient)
        double codecFactor = Format switch
        {
            RecordingFormat.HevcMp4 => 0.6,
            RecordingFormat.Av1Mp4 => 0.5,
            _ => 1.0
        };

        // Calculate final bitrate
        double finalMbps = baseMbps * resolutionScale * frameRateScale * codecFactor;

        // Clamp to reasonable range (1 Mbps to 200 Mbps)
        finalMbps = Math.Clamp(finalMbps, 1, 200);

        // Convert to bits per second
        return (uint)(finalMbps * 1_000_000);
    }

    public string GetOutputFileName()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        const string extension = "mp4";
        var formatSuffix = Format switch
        {
            RecordingFormat.H264Mp4 => "H264",
            RecordingFormat.HevcMp4 => "HEVC",
            RecordingFormat.Av1Mp4 => "AV1",
            _ => "VIDEO"
        };
        return $"Capture_{timestamp}_{formatSuffix}.{extension}";
    }

    public string GetFullOutputPath() => Path.Combine(OutputPath, GetOutputFileName());
}

public sealed record SplitEncodeSupport(bool Supports2Way, bool Supports3Way)
{
    public static SplitEncodeSupport NvencUnavailable { get; } = new(false, false);
}
