using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static WasapiAudioProjection BuildWasapiAudioProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            CaptureCallbackCount = captureRuntime.WasapiCaptureCallbackCount,
            CaptureCallbackAvgIntervalMs = captureRuntime.WasapiCaptureCallbackAvgIntervalMs,
            CaptureCallbackMaxIntervalMs = captureRuntime.WasapiCaptureCallbackMaxIntervalMs,
            CaptureCallbackSevereGapCount = captureRuntime.WasapiCaptureCallbackSevereGapCount,
            CaptureAudioDiscontinuityCount = captureRuntime.WasapiCaptureAudioDiscontinuityCount,
            CaptureAudioTimestampErrorCount = captureRuntime.WasapiCaptureAudioTimestampErrorCount,
            CaptureAudioGlitchCount = captureRuntime.WasapiCaptureAudioGlitchCount,
            CaptureCallbackSilenceCount = captureRuntime.WasapiCaptureCallbackSilenceCount,
            CaptureLastCallbackTickMs = captureRuntime.WasapiCaptureLastCallbackTickMs,
            CaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,
            CaptureAudioLevelLastFireTickMs = captureRuntime.WasapiCaptureAudioLevelLastFireTickMs,
            PlaybackRenderCallbackCount = captureRuntime.WasapiPlaybackRenderCallbackCount,
            PlaybackRenderSilenceCount = captureRuntime.WasapiPlaybackRenderSilenceCount,
            PlaybackQueueDepth = captureRuntime.WasapiPlaybackQueueDepth,
            PlaybackQueueDropCount = captureRuntime.WasapiPlaybackQueueDropCount,
            PlaybackQueueDurationMs = captureRuntime.WasapiPlaybackQueueDurationMs,
            PlaybackActiveChunkDurationMs = captureRuntime.WasapiPlaybackActiveChunkDurationMs,
            PlaybackEndpointQueuedDurationMs = captureRuntime.WasapiPlaybackEndpointQueuedDurationMs,
            PlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,
            PlaybackStreamLatencyMs = captureRuntime.WasapiPlaybackStreamLatencyMs,
            PlaybackLastRenderTickMs = captureRuntime.WasapiPlaybackLastRenderTickMs
        };

    private readonly record struct WasapiAudioProjection
    {
        public long CaptureCallbackCount { get; init; }
        public double CaptureCallbackAvgIntervalMs { get; init; }
        public double CaptureCallbackMaxIntervalMs { get; init; }
        public long CaptureCallbackSevereGapCount { get; init; }
        public long CaptureAudioDiscontinuityCount { get; init; }
        public long CaptureAudioTimestampErrorCount { get; init; }
        public long CaptureAudioGlitchCount { get; init; }
        public int CaptureCallbackSilenceCount { get; init; }
        public long CaptureLastCallbackTickMs { get; init; }
        public long CaptureAudioLevelEventsFired { get; init; }
        public long CaptureAudioLevelLastFireTickMs { get; init; }
        public long PlaybackRenderCallbackCount { get; init; }
        public int PlaybackRenderSilenceCount { get; init; }
        public int PlaybackQueueDepth { get; init; }
        public int PlaybackQueueDropCount { get; init; }
        public double PlaybackQueueDurationMs { get; init; }
        public double PlaybackActiveChunkDurationMs { get; init; }
        public double PlaybackEndpointQueuedDurationMs { get; init; }
        public double PlaybackBufferedDurationMs { get; init; }
        public double PlaybackStreamLatencyMs { get; init; }
        public long PlaybackLastRenderTickMs { get; init; }
    }

    private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(
        WasapiAudioProjection wasapi)
        => new()
        {
            CallbackCount = wasapi.CaptureCallbackCount,
            CallbackAvgIntervalMs = wasapi.CaptureCallbackAvgIntervalMs,
            CallbackMaxIntervalMs = wasapi.CaptureCallbackMaxIntervalMs,
            CallbackSevereGapCount = wasapi.CaptureCallbackSevereGapCount,
            AudioDiscontinuityCount = wasapi.CaptureAudioDiscontinuityCount,
            AudioTimestampErrorCount = wasapi.CaptureAudioTimestampErrorCount,
            AudioGlitchCount = wasapi.CaptureAudioGlitchCount,
            CallbackSilenceCount = wasapi.CaptureCallbackSilenceCount,
            LastCallbackTickMs = wasapi.CaptureLastCallbackTickMs,
            AudioLevelEventsFired = wasapi.CaptureAudioLevelEventsFired,
            AudioLevelLastFireTickMs = wasapi.CaptureAudioLevelLastFireTickMs
        };

    private readonly record struct WasapiCaptureFlattenedProjection
    {
        public long CallbackCount { get; init; }
        public double CallbackAvgIntervalMs { get; init; }
        public double CallbackMaxIntervalMs { get; init; }
        public long CallbackSevereGapCount { get; init; }
        public long AudioDiscontinuityCount { get; init; }
        public long AudioTimestampErrorCount { get; init; }
        public long AudioGlitchCount { get; init; }
        public int CallbackSilenceCount { get; init; }
        public long LastCallbackTickMs { get; init; }
        public long AudioLevelEventsFired { get; init; }
        public long AudioLevelLastFireTickMs { get; init; }
    }

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
