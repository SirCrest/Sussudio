using System.Text.Json;

namespace Sussudio.Tools;

public readonly record struct PresentMonProbeCorrelation(
    string? SwapChainAddress,
    long? PresentId,
    long? SourceSequenceNumber,
    long? PresentUtcUnixMs);

public static partial class PresentMonProbe
{
    public static PresentMonProbeOptions CreateOptions(
        int durationSeconds,
        int? processId = null,
        string? processName = null,
        string? swapChainAddress = null,
        long? appPresentId = null,
        long? appSourceSequenceNumber = null,
        long? appPresentUtcUnixMs = null,
        long? captureStartUtcUnixMs = null,
        string? presentMonPath = null,
        string? outputFile = null,
        bool keepCsv = false,
        bool trackGpuVideo = true,
        PresentMonProbeCorrelation correlation = default)
    {
        return new PresentMonProbeOptions
        {
            ProcessId = processId,
            ProcessName = string.IsNullOrWhiteSpace(processName) ? "Sussudio" : processName,
            DurationSeconds = durationSeconds,
            PresentMonPath = presentMonPath,
            OutputFile = outputFile,
            ExpectedSwapChainAddress = string.IsNullOrWhiteSpace(swapChainAddress)
                ? correlation.SwapChainAddress
                : swapChainAddress,
            AppPresentId = appPresentId ?? correlation.PresentId,
            AppSourceSequenceNumber = appSourceSequenceNumber ?? correlation.SourceSequenceNumber,
            AppPresentUtcUnixMs = appPresentUtcUnixMs ?? correlation.PresentUtcUnixMs,
            CaptureStartUtcUnixMs = captureStartUtcUnixMs,
            KeepCsv = keepCsv,
            TrackGpuVideo = trackGpuVideo
        };
    }

    public static PresentMonProbeCorrelation ReadPreviewCorrelation(JsonElement snapshot)
    {
        if (snapshot.ValueKind is not JsonValueKind.Object)
        {
            return default;
        }

        var address = AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DSwapChainAddress", string.Empty);
        return new PresentMonProbeCorrelation(
            string.IsNullOrWhiteSpace(address) ? null : address,
            GetPositiveLong(snapshot, "PreviewD3DLastRenderedPreviewPresentId"),
            GetNonNegativeLong(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber"),
            GetPositiveLong(snapshot, "PreviewD3DLastRenderedUtcUnixMs"));
    }

    private static long? GetPositiveLong(JsonElement snapshot, string name)
    {
        var value = AutomationSnapshotFormatter.GetLong(snapshot, name, 0);
        return value > 0 ? value : null;
    }

    private static long? GetNonNegativeLong(JsonElement snapshot, string name)
    {
        var value = AutomationSnapshotFormatter.GetLong(snapshot, name, -1);
        return value >= 0 ? value : null;
    }
}
