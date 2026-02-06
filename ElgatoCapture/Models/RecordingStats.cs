namespace ElgatoCapture.Models;

public readonly struct RecordingStats
{
    public RecordingStats(long videoBytes, long audioBytes)
    {
        VideoBytes = videoBytes;
        AudioBytes = audioBytes;
    }

    public long VideoBytes { get; }
    public long AudioBytes { get; }
    public long TotalBytes => VideoBytes + AudioBytes;
}
