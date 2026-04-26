using System.ComponentModel;
using System.IO;
using System.Text.Json;
using ElgatoCapture.Tools;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class PresentMonTools
{
    [McpServerTool, Description("Capture OS-level present/frame pacing metrics for ElgatoCapture using the PresentMon console executable.")]
    public static async Task<string> capture_presentmon(
        PipeClient pipeClient,
        [Description("Capture duration in seconds. Defaults to 10; clamped to 1-300.")] int seconds = 10,
        [Description("Optional target process id. Defaults to the newest ElgatoCapture process.")] int? processId = null,
        [Description("Optional process name when processId is not provided. Defaults to ElgatoCapture.")] string processName = "ElgatoCapture",
        [Description("Optional expected DXGI swap-chain address, usually PreviewD3DSwapChainAddress from get_app_state_raw.")] string? swapChainAddress = null,
        [Description("Optional path to PresentMon.exe / PresentMon-*-x64.exe. Env vars ELGATOCAPTURE_PRESENTMON_PATH or PRESENTMON_PATH also work.")] string? presentMonPath = null,
        [Description("Optional CSV output path. The CSV is deleted unless keepCsv is true.")] string? outputPath = null,
        [Description("Keep the raw PresentMon CSV and return its path.")] bool keepCsv = false,
        [Description("Ask PresentMon to track GPU video engine metrics when supported.")] bool trackGpuVideo = true)
    {
        swapChainAddress ??= await TryResolvePreviewSwapChainAddressAsync(pipeClient).ConfigureAwait(false);
        var result = await PresentMonProbe.RunAsync(new PresentMonProbeOptions
        {
            DurationSeconds = seconds,
            ProcessId = processId,
            ProcessName = processName,
            ExpectedSwapChainAddress = swapChainAddress,
            PresentMonPath = presentMonPath,
            OutputFile = outputPath,
            KeepCsv = keepCsv,
            TrackGpuVideo = trackGpuVideo
        }).ConfigureAwait(false);

        return PresentMonProbe.Format(result);
    }

    [McpServerTool(UseStructuredContent = true), Description("Capture raw structured PresentMon frame pacing summary for ElgatoCapture.")]
    public static async Task<object> capture_presentmon_raw(
        PipeClient pipeClient,
        [Description("Capture duration in seconds. Defaults to 10; clamped to 1-300.")] int seconds = 10,
        [Description("Optional target process id. Defaults to the newest ElgatoCapture process.")] int? processId = null,
        [Description("Optional process name when processId is not provided. Defaults to ElgatoCapture.")] string processName = "ElgatoCapture",
        [Description("Optional expected DXGI swap-chain address, usually PreviewD3DSwapChainAddress from get_app_state_raw.")] string? swapChainAddress = null,
        [Description("Optional path to PresentMon.exe / PresentMon-*-x64.exe. Env vars ELGATOCAPTURE_PRESENTMON_PATH or PRESENTMON_PATH also work.")] string? presentMonPath = null,
        [Description("Optional CSV output path. The CSV is deleted unless keepCsv is true.")] string? outputPath = null,
        [Description("Keep the raw PresentMon CSV and return its path.")] bool keepCsv = false,
        [Description("Ask PresentMon to track GPU video engine metrics when supported.")] bool trackGpuVideo = true)
    {
        swapChainAddress ??= await TryResolvePreviewSwapChainAddressAsync(pipeClient).ConfigureAwait(false);
        return await PresentMonProbe.RunAsync(new PresentMonProbeOptions
        {
            DurationSeconds = seconds,
            ProcessId = processId,
            ProcessName = processName,
            ExpectedSwapChainAddress = swapChainAddress,
            PresentMonPath = presentMonPath,
            OutputFile = outputPath,
            KeepCsv = keepCsv,
            TrackGpuVideo = trackGpuVideo
        }).ConfigureAwait(false);
    }

    private static async Task<string?> TryResolvePreviewSwapChainAddressAsync(PipeClient pipeClient)
    {
        try
        {
            var response = await pipeClient.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response) ||
                !response.TryGetProperty("Snapshot", out var snapshot))
            {
                return null;
            }

            var address = AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DSwapChainAddress", string.Empty);
            return string.IsNullOrWhiteSpace(address) ? null : address;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
