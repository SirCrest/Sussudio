using System.Reflection;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task SsctlFormatters_TimelineOutputPreservesTableAndSummary()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssemblyIsolated(assemblyPath);
        var formatterType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.Formatters")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters type not found.");
        var formatTimeline = formatterType.GetMethod("FormatTimeline", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters.FormatTimeline not found.");

        const string json = """
                            {
                              "Data": [
                                {
                                  "TimestampUtc": "2026-05-15T00:00:00Z",
                                  "CaptureFps": 119.5,
                                  "PreviewFps": 118.2,
                                  "VideoQueueDepth": 1,
                                  "VideoDrops": 2,
                                  "CaptureCadenceAverageMs": 8.0,
                                  "CaptureCadenceP95Ms": 8.4,
                                  "CaptureCadenceP99Ms": 8.8,
                                  "CaptureCadenceMaxMs": 10.0,
                                  "CaptureCadenceOnePercentLowFps": 112.0,
                                  "PreviewCadenceAverageMs": 8.1,
                                  "PreviewCadenceP95Ms": 8.6,
                                  "PreviewCadenceMaxMs": 11.0,
                                  "PreviewCadenceOnePercentLowFps": 110.0,
                                  "PreviewCadenceSlowFramePercent": 0.5,
                                  "PreviewD3DPendingFrameCount": 0,
                                  "PreviewD3DPresentCallP95Ms": 0.6,
                                  "PreviewD3DTotalFrameCpuP95Ms": 1.5,
                                  "PreviewD3DPipelineLatencyP95Ms": 2.5,
                                  "PreviewD3DFrameLatencyWaitTimeoutCount": 0,
                                  "PreviewD3DFrameLatencyWaitP95Ms": 0.2,
                                  "PreviewD3DFrameStatsRecentMissedRefreshCount": 0,
                                  "PreviewD3DFrameStatsRecentFailureCount": 0,
                                  "PipelineLatencyMs": 3,
                                  "ProcessCpuPercent": 7.5,
                                  "MemoryWorkingSetMb": 200.0,
                                  "MemoryManagedHeapMb": 40.0,
                                  "GcGen0Collections": 1,
                                  "GcGen1Collections": 0,
                                  "GcGen2Collections": 0,
                                  "GcPauseTimePercent": 0.1,
                                  "ThreadPoolWorkerAvailable": 32760,
                                  "ThreadPoolIoAvailable": 1000
                                },
                                {
                                  "TimestampUtc": "2026-05-15T00:00:01Z",
                                  "CaptureFps": 118.0,
                                  "PreviewFps": 117.0,
                                  "VideoQueueDepth": 2,
                                  "VideoDrops": 5,
                                  "CaptureCadenceAverageMs": 8.5,
                                  "CaptureCadenceP95Ms": 9.0,
                                  "CaptureCadenceP99Ms": 9.5,
                                  "CaptureCadenceMaxMs": 12.0,
                                  "CaptureCadenceOnePercentLowFps": 108.0,
                                  "PreviewCadenceAverageMs": 8.8,
                                  "PreviewCadenceP95Ms": 9.2,
                                  "PreviewCadenceMaxMs": 13.0,
                                  "PreviewCadenceOnePercentLowFps": 105.0,
                                  "PreviewCadenceSlowFramePercent": 1.5,
                                  "PreviewD3DPendingFrameCount": 1,
                                  "PreviewD3DPresentCallP95Ms": 0.8,
                                  "PreviewD3DTotalFrameCpuP95Ms": 1.8,
                                  "PreviewD3DPipelineLatencyP95Ms": 2.9,
                                  "PreviewD3DFrameLatencyWaitTimeoutCount": 1,
                                  "PreviewD3DFrameLatencyWaitP95Ms": 0.4,
                                  "PreviewD3DFrameStatsRecentMissedRefreshCount": 2,
                                  "PreviewD3DFrameStatsRecentFailureCount": 1,
                                  "PipelineLatencyMs": 4,
                                  "ProcessCpuPercent": 8.5,
                                  "MemoryWorkingSetMb": 205.0,
                                  "MemoryManagedHeapMb": 42.0,
                                  "GcGen0Collections": 3,
                                  "GcGen1Collections": 1,
                                  "GcGen2Collections": 1,
                                  "GcPauseTimePercent": 0.2,
                                  "ThreadPoolWorkerAvailable": 32759,
                                  "ThreadPoolIoAvailable": 999
                                }
                              ]
                            }
                            """;

        using var document = JsonDocument.Parse(json);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string output;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            output = formatTimeline.Invoke(null, new object[] { document.RootElement })?.ToString()
                ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters.FormatTimeline returned null.");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(output, "Performance Timeline (2 samples)");
        AssertContains(output, "Timestamp                | CapAvg | CapP95");
        AssertContains(output, "2026-05-15T00:00:00Z");
        AssertContains(output, "== Trend Summary (first vs last sample) ==");
        AssertContains(output, "Capture Avg:    8.0ms -> 8.5ms (delta: +0.5ms)");
        AssertContains(output, "Video Drops:    2 -> 5 (delta: +3)");
        AssertContains(output, "Working Set:    200.0MB -> 205.0MB (delta: +5.0MB)");

        return Task.CompletedTask;
    }
}
