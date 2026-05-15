using System.ComponentModel;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP wrapper for PresentMon capture and parsed OS presentation metrics.
public static partial class PresentMonTools
{
    [McpServerTool, Description("Capture OS-level present/frame pacing metrics for Sussudio using the PresentMon console executable.")]
    public static async Task<CallToolResult> capture_presentmon(
        PipeClient pipeClient,
        [Description("Capture duration in seconds. Defaults to 10; clamped to 1-300.")] int seconds = 10,
        [Description("Optional target process id. Defaults to the newest Sussudio process.")] int? processId = null,
        [Description("Optional process name when processId is not provided. Defaults to Sussudio.")] string processName = "Sussudio",
        [Description("Optional expected DXGI swap-chain address, usually PreviewD3DSwapChainAddress from get_app_state_raw.")] string? swapChainAddress = null,
        [Description("Optional app-side D3D preview present id to correlate with PresentMon.")] long? appPresentId = null,
        [Description("Optional app-side decoded source sequence number for the correlated present.")] long? appSourceSequenceNumber = null,
        [Description("Optional UTC Unix milliseconds for the app-side Present return.")] long? appPresentUtcUnixMs = null,
        [Description("Optional path to PresentMon.exe / PresentMon-*-x64.exe. Env vars SUSSUDIO_PRESENTMON_PATH or PRESENTMON_PATH also work.")] string? presentMonPath = null,
        [Description("Optional CSV output path. The CSV is deleted unless keepCsv is true.")] string? outputPath = null,
        [Description("Keep the raw PresentMon CSV and return its path.")] bool keepCsv = false,
        [Description("Ask PresentMon to track GPU video engine metrics when supported.")] bool trackGpuVideo = true)
    {
        var resolved = await TryResolvePreviewPresentCorrelationAsync(pipeClient).ConfigureAwait(false);
        var result = await PresentMonProbe.RunAsync(CreatePresentMonProbeOptions(
            seconds,
            processId,
            processName,
            swapChainAddress,
            appPresentId,
            appSourceSequenceNumber,
            appPresentUtcUnixMs,
            presentMonPath,
            outputPath,
            keepCsv,
            trackGpuVideo,
            resolved))
            .ConfigureAwait(false);

        return McpToolResultFactory.FromText(PresentMonProbe.Format(result));
    }

    [McpServerTool(UseStructuredContent = true), Description("Capture raw structured PresentMon frame pacing summary for Sussudio.")]
    public static async Task<object> capture_presentmon_raw(
        PipeClient pipeClient,
        [Description("Capture duration in seconds. Defaults to 10; clamped to 1-300.")] int seconds = 10,
        [Description("Optional target process id. Defaults to the newest Sussudio process.")] int? processId = null,
        [Description("Optional process name when processId is not provided. Defaults to Sussudio.")] string processName = "Sussudio",
        [Description("Optional expected DXGI swap-chain address, usually PreviewD3DSwapChainAddress from get_app_state_raw.")] string? swapChainAddress = null,
        [Description("Optional app-side D3D preview present id to correlate with PresentMon.")] long? appPresentId = null,
        [Description("Optional app-side decoded source sequence number for the correlated present.")] long? appSourceSequenceNumber = null,
        [Description("Optional UTC Unix milliseconds for the app-side Present return.")] long? appPresentUtcUnixMs = null,
        [Description("Optional path to PresentMon.exe / PresentMon-*-x64.exe. Env vars SUSSUDIO_PRESENTMON_PATH or PRESENTMON_PATH also work.")] string? presentMonPath = null,
        [Description("Optional CSV output path. The CSV is deleted unless keepCsv is true.")] string? outputPath = null,
        [Description("Keep the raw PresentMon CSV and return its path.")] bool keepCsv = false,
        [Description("Ask PresentMon to track GPU video engine metrics when supported.")] bool trackGpuVideo = true)
    {
        var resolved = await TryResolvePreviewPresentCorrelationAsync(pipeClient).ConfigureAwait(false);
        return await PresentMonProbe.RunAsync(CreatePresentMonProbeOptions(
            seconds,
            processId,
            processName,
            swapChainAddress,
            appPresentId,
            appSourceSequenceNumber,
            appPresentUtcUnixMs,
            presentMonPath,
            outputPath,
            keepCsv,
            trackGpuVideo,
            resolved))
            .ConfigureAwait(false);
    }

    private static PresentMonProbeOptions CreatePresentMonProbeOptions(
        int seconds,
        int? processId,
        string processName,
        string? swapChainAddress,
        long? appPresentId,
        long? appSourceSequenceNumber,
        long? appPresentUtcUnixMs,
        string? presentMonPath,
        string? outputPath,
        bool keepCsv,
        bool trackGpuVideo,
        PresentMonCorrelation resolved)
    {
        return new PresentMonProbeOptions
        {
            DurationSeconds = seconds,
            ProcessId = processId,
            ProcessName = processName,
            ExpectedSwapChainAddress = swapChainAddress ?? resolved.SwapChainAddress,
            AppPresentId = appPresentId ?? resolved.PresentId,
            AppSourceSequenceNumber = appSourceSequenceNumber ?? resolved.SourceSequenceNumber,
            AppPresentUtcUnixMs = appPresentUtcUnixMs ?? resolved.PresentUtcUnixMs,
            PresentMonPath = presentMonPath,
            OutputFile = outputPath,
            KeepCsv = keepCsv,
            TrackGpuVideo = trackGpuVideo
        };
    }
}
