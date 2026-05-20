namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(
        FlashbackRecordingProjection flashbackRecording)
        => new()
        {
            Active = flashbackRecording.Active,
            BufferedDurationMs = flashbackRecording.BufferedDurationMs,
            DiskBytes = flashbackRecording.DiskBytes,
            TotalBytesWritten = flashbackRecording.TotalBytesWritten,
            OutputBytes = flashbackRecording.OutputBytes,
            FilePath = flashbackRecording.FilePath,
            EncodedFrames = flashbackRecording.EncodedFrames,
            DroppedFrames = flashbackRecording.DroppedFrames,
            GpuEncoding = flashbackRecording.GpuEncoding
        };

    private readonly record struct FlashbackRecordingRuntimeFlattenedProjection
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
