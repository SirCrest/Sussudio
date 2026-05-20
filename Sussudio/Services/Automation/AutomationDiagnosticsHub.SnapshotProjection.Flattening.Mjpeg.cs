namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(MjpegProjection mjpeg)
    {
        return new()
        {
            TotalDecoded = mjpeg.TotalDecoded,
            TotalEmitted = mjpeg.TotalEmitted,
            TotalDropped = mjpeg.TotalDropped,
            CompressedFramesQueued = mjpeg.CompressedFramesQueued,
            CompressedFramesDequeued = mjpeg.CompressedFramesDequeued,
            CompressedDropsQueueFull = mjpeg.CompressedDropsQueueFull,
            CompressedDropsByteBudget = mjpeg.CompressedDropsByteBudget,
            CompressedDropsDisposed = mjpeg.CompressedDropsDisposed,
            DecodeFailures = mjpeg.DecodeFailures,
            ReorderCollisions = mjpeg.ReorderCollisions,
            EmitFailures = mjpeg.EmitFailures,
            CompressedQueueDepth = mjpeg.CompressedQueueDepth,
            CompressedQueueBytes = mjpeg.CompressedQueueBytes,
            CompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,
            ReorderSkips = mjpeg.ReorderSkips,
            ReorderBufferDepth = mjpeg.ReorderBufferDepth,
        };
    }

    private readonly record struct MjpegFlattenedProjection
    {
        public long TotalDecoded { get; init; }
        public long TotalEmitted { get; init; }
        public long TotalDropped { get; init; }
        public long CompressedFramesQueued { get; init; }
        public long CompressedFramesDequeued { get; init; }
        public long CompressedDropsQueueFull { get; init; }
        public long CompressedDropsByteBudget { get; init; }
        public long CompressedDropsDisposed { get; init; }
        public long DecodeFailures { get; init; }
        public long ReorderCollisions { get; init; }
        public long EmitFailures { get; init; }
        public int CompressedQueueDepth { get; init; }
        public long CompressedQueueBytes { get; init; }
        public long CompressedQueueByteBudget { get; init; }
        public long ReorderSkips { get; init; }
        public int ReorderBufferDepth { get; init; }
    }
}
