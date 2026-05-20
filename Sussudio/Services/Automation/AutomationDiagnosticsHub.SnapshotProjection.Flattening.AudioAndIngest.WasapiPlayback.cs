namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(
        WasapiAudioProjection wasapi)
        => new()
        {
            RenderCallbackCount = wasapi.PlaybackRenderCallbackCount,
            RenderSilenceCount = wasapi.PlaybackRenderSilenceCount,
            QueueDepth = wasapi.PlaybackQueueDepth,
            QueueDropCount = wasapi.PlaybackQueueDropCount,
            QueueDurationMs = wasapi.PlaybackQueueDurationMs,
            ActiveChunkDurationMs = wasapi.PlaybackActiveChunkDurationMs,
            EndpointQueuedDurationMs = wasapi.PlaybackEndpointQueuedDurationMs,
            BufferedDurationMs = wasapi.PlaybackBufferedDurationMs,
            StreamLatencyMs = wasapi.PlaybackStreamLatencyMs,
            LastRenderTickMs = wasapi.PlaybackLastRenderTickMs
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
