using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_InitializationLivesWithServiceRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var telemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Telemetry.cs")
            .Replace("\r\n", "\n");
        var telemetryFallbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.TelemetryFallback.cs")
            .Replace("\r\n", "\n");
        var captureFormatTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.CaptureFormatTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private static ISourceSignalTelemetryProvider CreateDefaultTelemetryProvider()");
        AssertContains(rootText, "public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "=> RunTransitionAsync(CaptureSessionState.Initializing, async transitionToken =>");
        AssertContains(rootText, "_audioDeviceId = settings.UseCustomAudioInput ? settings.AudioDeviceId : device.AudioDeviceId;");
        AssertContains(rootText, "_actualPixelFormat = settings.RequestedPixelFormat ?? (settings.HdrEnabled ? \"P010\" : \"NV12\");");
        AssertContains(rootText, "ResetObservedPixelTelemetry();");
        AssertContains(rootText, "ResetCachedMjpegTimingMetrics();");
        AssertContains(rootText, "_latestSourceTelemetry = BuildFallbackTelemetry();");
        AssertContains(rootText, "await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);");
        AssertContains(rootText, "TryCorrectFrameRateFromTelemetry();");
        AssertContains(rootText, "StatusChanged?.Invoke(this, \"Initialized\");");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Initialization.cs")),
            "old initialization partial removed");
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
