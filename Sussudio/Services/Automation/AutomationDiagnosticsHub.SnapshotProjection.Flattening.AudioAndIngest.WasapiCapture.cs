namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}
