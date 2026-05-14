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
}
