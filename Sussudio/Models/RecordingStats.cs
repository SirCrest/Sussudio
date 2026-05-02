namespace ElgatoCapture.Models;

public readonly struct RecordingStats
{
    public RecordingStats(long videoBytes, long audioBytes, bool isFlashbackEstimate = false)
    {
        VideoBytes = videoBytes;
        AudioBytes = audioBytes;
        IsFlashbackEstimate = isFlashbackEstimate;
    }

    public long VideoBytes { get; }
    public long AudioBytes { get; }
    public long TotalBytes => VideoBytes + AudioBytes;

    /// <summary>
    /// True when the bytes come from the flashback buffer (estimated, not final file size).
    /// </summary>
    public bool IsFlashbackEstimate { get; }
}
