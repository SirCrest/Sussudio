using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)
    {
        var timing = BuildMjpegTimingProjection(health);
        var previewJitter = BuildMjpegPreviewJitterProjection(health);
        var packetHash = BuildMjpegPacketHashProjection(health);

        return new()
        {
            Timing = timing,
            TotalDecoded = health.MjpegTotalDecoded,
            TotalEmitted = health.MjpegTotalEmitted,
            TotalDropped = health.MjpegTotalDropped,
            CompressedFramesQueued = health.MjpegCompressedFramesQueued,
            CompressedFramesDequeued = health.MjpegCompressedFramesDequeued,
            CompressedDropsQueueFull = health.MjpegCompressedDropsQueueFull,
            CompressedDropsByteBudget = health.MjpegCompressedDropsByteBudget,
            CompressedDropsDisposed = health.MjpegCompressedDropsDisposed,
            DecodeFailures = health.MjpegDecodeFailures,
            ReorderCollisions = health.MjpegReorderCollisions,
            EmitFailures = health.MjpegEmitFailures,
            CompressedQueueDepth = health.MjpegCompressedQueueDepth,
            CompressedQueueBytes = health.MjpegCompressedQueueBytes,
            CompressedQueueByteBudget = health.MjpegCompressedQueueByteBudget,
            ReorderSkips = health.MjpegReorderSkips,
            ReorderBufferDepth = health.MjpegReorderBufferDepth,
            PreviewJitter = previewJitter,
            PacketHash = packetHash,
        };
    }

    private readonly record struct MjpegProjection
    {
        public MjpegTimingProjection Timing { get; init; }
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
        public MjpegPreviewJitterProjection PreviewJitter { get; init; }
        public MjpegPacketHashProjection PacketHash { get; init; }
    }
}
