namespace Sussudio.Models;

public readonly struct RecordingStats
{
    public RecordingStats(long videoBytes, long audioBytes, bool isFlashbackEstimate = false, bool isFailure = false)
    {
        VideoBytes = videoBytes;
        AudioBytes = audioBytes;
        IsFlashbackEstimate = isFlashbackEstimate;
        IsFailure = isFailure;
    }

    public long VideoBytes { get; }
    public long AudioBytes { get; }
    public long TotalBytes => VideoBytes + AudioBytes;

    /// <summary>
    /// True when the bytes come from the flashback buffer (estimated, not final file size).
    /// </summary>
    public bool IsFlashbackEstimate { get; }

    /// <summary>
    /// True when the snapshot couldn't be computed (exception caught). Distinguishes
    /// legitimate zero (no recording) from swallowed failure that previously read as zero.
    /// </summary>
    public bool IsFailure { get; }
}
