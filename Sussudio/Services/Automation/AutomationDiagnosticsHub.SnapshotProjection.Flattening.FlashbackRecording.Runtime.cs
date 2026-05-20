namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(
        FlashbackRecordingRuntimeProjection runtime)
        => new()
        {
            Active = runtime.Active,
            BufferedDurationMs = runtime.BufferedDurationMs,
            DiskBytes = runtime.DiskBytes,
            TotalBytesWritten = runtime.TotalBytesWritten,
            OutputBytes = runtime.OutputBytes,
            FilePath = runtime.FilePath,
            EncodedFrames = runtime.EncodedFrames,
            DroppedFrames = runtime.DroppedFrames,
            GpuEncoding = runtime.GpuEncoding
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
