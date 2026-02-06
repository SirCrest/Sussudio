using System;
using System.IO;

namespace ElgatoCapture.Models;

public enum RecordingFormat
{
    H264Mp4,
    HevcMp4,
    Av1Mp4,
    UncompressedAvi
}

public enum VideoQuality
{
    Auto,       // Let the encoder decide based on resolution
    Low,        // ~8 Mbps for 1080p, scales with resolution
    Medium,     // ~15 Mbps for 1080p
    High,       // ~25 Mbps for 1080p
    VeryHigh,   // ~40 Mbps for 1080p
    Lossless,   // ~80+ Mbps, highest quality
    Custom      // User-specified bitrate
}

public class CaptureSettings
{
    public uint Width { get; set; } = 1920;
    public uint Height { get; set; } = 1080;
    public double FrameRate { get; set; } = 60;
    public RecordingFormat Format { get; set; } = RecordingFormat.H264Mp4;
    public VideoQuality Quality { get; set; } = VideoQuality.High;
    public double CustomBitrateMbps { get; set; } = 50; // Used when Quality is Custom
    public bool HdrEnabled { get; set; }
    public string OutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    public bool AudioEnabled { get; set; } = true;
    public bool UseCustomAudioInput { get; set; }
    public string? AudioDeviceId { get; set; }
    public string? AudioDeviceName { get; set; }
    public RecordingPipelineOptions PipelineOptions { get; set; } = new();

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
            VideoQuality.VeryHigh => 40,
            VideoQuality.Lossless => 80,
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
        var extension = Format == RecordingFormat.UncompressedAvi ? "avi" : "mp4";
        var formatSuffix = Format switch
        {
            RecordingFormat.H264Mp4 => "H264",
            RecordingFormat.HevcMp4 => "HEVC",
            RecordingFormat.Av1Mp4 => "AV1",
            RecordingFormat.UncompressedAvi => "RAW",
            _ => "VIDEO"
        };
        return $"Capture_{timestamp}_{formatSuffix}.{extension}";
    }

    public string GetFullOutputPath() => Path.Combine(OutputPath, GetOutputFileName());
}
