namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(
        AudioAndIngestProjection audioAndIngest)
        => new()
        {
            RenderCallbackCount = audioAndIngest.WasapiPlaybackRenderCallbackCount,
            RenderSilenceCount = audioAndIngest.WasapiPlaybackRenderSilenceCount,
            QueueDepth = audioAndIngest.WasapiPlaybackQueueDepth,
            QueueDropCount = audioAndIngest.WasapiPlaybackQueueDropCount,
            QueueDurationMs = audioAndIngest.WasapiPlaybackQueueDurationMs,
            ActiveChunkDurationMs = audioAndIngest.WasapiPlaybackActiveChunkDurationMs,
            EndpointQueuedDurationMs = audioAndIngest.WasapiPlaybackEndpointQueuedDurationMs,
            BufferedDurationMs = audioAndIngest.WasapiPlaybackBufferedDurationMs,
            StreamLatencyMs = audioAndIngest.WasapiPlaybackStreamLatencyMs,
            LastRenderTickMs = audioAndIngest.WasapiPlaybackLastRenderTickMs
        };

    private readonly record struct WasapiPlaybackFlattenedProjection
    {
        public long RenderCallbackCount { get; init; }
        public int RenderSilenceCount { get; init; }
        public int QueueDepth { get; init; }
        public int QueueDropCount { get; init; }
        public double QueueDurationMs { get; init; }
        public double ActiveChunkDurationMs { get; init; }
        public double EndpointQueuedDurationMs { get; init; }
        public double BufferedDurationMs { get; init; }
        public double StreamLatencyMs { get; init; }
        public long LastRenderTickMs { get; init; }
    }
}
