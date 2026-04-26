using System.ComponentModel;
using ElgatoCapture.Tools;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class PresentMonTools
{
    [McpServerTool, Description("Capture OS-level present/frame pacing metrics for ElgatoCapture using the PresentMon console executable.")]
    public static async Task<string> capture_presentmon(
        [Description("Capture duration in seconds. Defaults to 10; clamped to 1-300.")] int seconds = 10,
        [Description("Optional target process id. Defaults to the newest ElgatoCapture process.")] int? processId = null,
        [Description("Optional process name when processId is not provided. Defaults to ElgatoCapture.")] string processName = "ElgatoCapture",
        [Description("Optional path to PresentMon.exe / PresentMon-*-x64.exe. Env vars ELGATOCAPTURE_PRESENTMON_PATH or PRESENTMON_PATH also work.")] string? presentMonPath = null,
        [Description("Optional CSV output path. The CSV is deleted unless keepCsv is true.")] string? outputPath = null,
        [Description("Keep the raw PresentMon CSV and return its path.")] bool keepCsv = false,
        [Description("Ask PresentMon to track GPU video engine metrics when supported.")] bool trackGpuVideo = true)
    {
        var result = await PresentMonProbe.RunAsync(new PresentMonProbeOptions
        {
            DurationSeconds = seconds,
            ProcessId = processId,
            ProcessName = processName,
            PresentMonPath = presentMonPath,
            OutputFile = outputPath,
            KeepCsv = keepCsv,
            TrackGpuVideo = trackGpuVideo
        }).ConfigureAwait(false);

        return PresentMonProbe.Format(result);
    }

    [McpServerTool(UseStructuredContent = true), Description("Capture raw structured PresentMon frame pacing summary for ElgatoCapture.")]
    public static async Task<object> capture_presentmon_raw(
        [Description("Capture duration in seconds. Defaults to 10; clamped to 1-300.")] int seconds = 10,
        [Description("Optional target process id. Defaults to the newest ElgatoCapture process.")] int? processId = null,
        [Description("Optional process name when processId is not provided. Defaults to ElgatoCapture.")] string processName = "ElgatoCapture",
        [Description("Optional path to PresentMon.exe / PresentMon-*-x64.exe. Env vars ELGATOCAPTURE_PRESENTMON_PATH or PRESENTMON_PATH also work.")] string? presentMonPath = null,
        [Description("Optional CSV output path. The CSV is deleted unless keepCsv is true.")] string? outputPath = null,
        [Description("Keep the raw PresentMon CSV and return its path.")] bool keepCsv = false,
        [Description("Ask PresentMon to track GPU video engine metrics when supported.")] bool trackGpuVideo = true)
    {
        return await PresentMonProbe.RunAsync(new PresentMonProbeOptions
        {
            DurationSeconds = seconds,
            ProcessId = processId,
            ProcessName = processName,
            PresentMonPath = presentMonPath,
            OutputFile = outputPath,
            KeepCsv = keepCsv,
            TrackGpuVideo = trackGpuVideo
        }).ConfigureAwait(false);
    }
}
