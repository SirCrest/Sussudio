using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            Active = health.FlashbackActive,
            BufferedDurationMs = health.FlashbackBufferedDurationMs,
            DiskBytes = health.FlashbackDiskBytes,
            TotalBytesWritten = health.FlashbackTotalBytesWritten,
            OutputBytes = health.FlashbackOutputBytes,
            FilePath = health.FlashbackFilePath,
            EncodedFrames = health.FlashbackEncodedFrames,
            DroppedFrames = health.FlashbackDroppedFrames,
            GpuEncoding = health.FlashbackGpuEncoding
        };

    private readonly record struct FlashbackRecordingRuntimeProjection
    {
        public bool Active { get; init; }
        public long BufferedDurationMs { get; init; }
        public long DiskBytes { get; init; }
        public long TotalBytesWritten { get; init; }
        public long OutputBytes { get; init; }
        public string? FilePath { get; init; }
        public long EncodedFrames { get; init; }
        public long DroppedFrames { get; init; }
        public bool GpuEncoding { get; init; }
    }
}
