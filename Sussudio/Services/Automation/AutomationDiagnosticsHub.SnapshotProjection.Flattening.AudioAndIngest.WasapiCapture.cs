namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(
        AudioAndIngestProjection audioAndIngest)
        => new()
        {
            CallbackCount = audioAndIngest.WasapiCaptureCallbackCount,
            CallbackAvgIntervalMs = audioAndIngest.WasapiCaptureCallbackAvgIntervalMs,
            CallbackMaxIntervalMs = audioAndIngest.WasapiCaptureCallbackMaxIntervalMs,
            CallbackSevereGapCount = audioAndIngest.WasapiCaptureCallbackSevereGapCount,
            AudioDiscontinuityCount = audioAndIngest.WasapiCaptureAudioDiscontinuityCount,
            AudioTimestampErrorCount = audioAndIngest.WasapiCaptureAudioTimestampErrorCount,
            AudioGlitchCount = audioAndIngest.WasapiCaptureAudioGlitchCount,
            CallbackSilenceCount = audioAndIngest.WasapiCaptureCallbackSilenceCount,
            LastCallbackTickMs = audioAndIngest.WasapiCaptureLastCallbackTickMs,
            AudioLevelEventsFired = audioAndIngest.WasapiCaptureAudioLevelEventsFired,
            AudioLevelLastFireTickMs = audioAndIngest.WasapiCaptureAudioLevelLastFireTickMs
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
