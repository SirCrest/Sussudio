using System;
using System.Collections.Generic;
using System.IO;

namespace ElgatoCapture.Models;

internal sealed record FlashbackBufferOptions
{
    // 350 Mbps worst case (4K120 MJPEG) = 43.75 MB/s. 30% headroom → 57 MB/s.
    private const long SafetyBytesPerSecond = 57L * 1024 * 1024;

    public TimeSpan BufferDuration { get; init; } = TimeSpan.FromMinutes(5);
    public string TempDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ElgatoCapture",
        "Flashback");
    public TimeSpan SegmentDuration { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Safety cap derived from BufferDuration. Not user-configurable — just a guardrail
    /// against bugs in PTS-based eviction.
    /// </summary>
    public long MaxDiskBytes
    {
        get
        {
            if (BufferDuration <= TimeSpan.Zero)
                return 0;

            var maxSeconds = long.MaxValue / (double)SafetyBytesPerSecond;
            if (BufferDuration.TotalSeconds >= maxSeconds)
                return long.MaxValue;

            return (long)(BufferDuration.TotalSeconds * SafetyBytesPerSecond);
        }
    }
}

internal sealed record FlashbackSessionContext
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required double FrameRate { get; init; }
    public int? FrameRateNumerator { get; init; }
    public int? FrameRateDenominator { get; init; }
    public required uint BitRate { get; init; }
    public required bool IsP010 { get; init; }
    public required string CodecName { get; init; }
    public string? NvencPreset { get; init; }
    public bool HdrEnabled { get; init; }
    public bool IsFullRangeInput { get; init; }
    public string? HdrMasterDisplayMetadata { get; init; }
    public int HdrMaxCll { get; init; }
    public int HdrMaxFall { get; init; }
    public IntPtr D3D11DevicePtr { get; init; }
    public IntPtr D3D11DeviceContextPtr { get; init; }
    public bool AudioEnabled { get; init; }
    public bool MicrophoneEnabled { get; init; }
}

public enum FlashbackPlaybackState
{
    Disabled,
    Buffering,
    Live,
    Scrubbing,
    Playing,
    Paused
}

internal sealed record ExportProgress(int SegmentsProcessed, int TotalSegments, double Percent);

internal sealed record FlashbackExportSegment
{
    public required string Path { get; init; }
    public TimeSpan? StartPts { get; init; }
    public TimeSpan? EndPts { get; init; }
}

/// <summary>
/// Groups the parameters for a flashback export operation (single-file or multi-segment).
/// </summary>
internal sealed record FlashbackExportRequest
{
    /// <summary>Segment files with buffer timeline metadata for multi-segment export.</summary>
    public IReadOnlyList<FlashbackExportSegment>? Segments { get; init; }

    /// <summary>Segment file paths for multi-segment export, or null for single-file export.</summary>
    public IReadOnlyList<string>? SegmentPaths { get; init; }

    /// <summary>Single .ts input path for single-file export. Ignored when SegmentPaths is set.</summary>
    public string? InputTsPath { get; init; }

    public required TimeSpan InPoint { get; init; }
    public required TimeSpan OutPoint { get; init; }
    public required string OutputPath { get; init; }
    public bool FastStart { get; init; } = true;
}
