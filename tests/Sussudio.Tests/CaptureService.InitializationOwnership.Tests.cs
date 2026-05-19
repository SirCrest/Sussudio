using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureService_InitializationLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var initializationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Initialization.cs")
            .Replace("\r\n", "\n");
        var telemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Telemetry.cs")
            .Replace("\r\n", "\n");
        var telemetryFallbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.TelemetryFallback.cs")
            .Replace("\r\n", "\n");
        var captureFormatTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.CaptureFormatTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "public Task InitializeAsync(");
        AssertContains(rootText, "private static ISourceSignalTelemetryProvider CreateDefaultTelemetryProvider()");
        AssertContains(initializationText, "public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(initializationText, "=> RunTransitionAsync(CaptureSessionState.Initializing, async transitionToken =>");
        AssertContains(initializationText, "_audioDeviceId = settings.UseCustomAudioInput ? settings.AudioDeviceId : device.AudioDeviceId;");
        AssertContains(initializationText, "_actualPixelFormat = settings.RequestedPixelFormat ?? (settings.HdrEnabled ? \"P010\" : \"NV12\");");
        AssertContains(initializationText, "ResetObservedPixelTelemetry();");
        AssertContains(initializationText, "ResetCachedMjpegTimingMetrics();");
        AssertContains(initializationText, "_latestSourceTelemetry = BuildFallbackTelemetry();");
        AssertContains(initializationText, "await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);");
        AssertContains(initializationText, "TryCorrectFrameRateFromTelemetry();");
        AssertContains(initializationText, "StatusChanged?.Invoke(this, \"Initialized\");");
        AssertContains(telemetryFallbackText, "private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()");
        AssertContains(captureFormatTelemetryText, "private void TryCorrectFrameRateFromTelemetry()");
        AssertContains(captureFormatTelemetryText, "private static string ResolveFrameRateArg(");
        AssertContains(captureFormatTelemetryText, "private void CaptureEncoderRuntimeTelemetry(");
        AssertDoesNotContain(telemetryText, "private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()");
        AssertDoesNotContain(telemetryText, "private void TryCorrectFrameRateFromTelemetry()");
        AssertDoesNotContain(telemetryText, "private static string ResolveFrameRateArg(");
        AssertDoesNotContain(telemetryText, "private void CaptureEncoderRuntimeTelemetry(");

        return Task.CompletedTask;
    }
}
