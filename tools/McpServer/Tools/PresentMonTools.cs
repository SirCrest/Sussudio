using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP wrapper for PresentMon capture and parsed OS presentation metrics.
public static class PresentMonTools
{
    [McpServerTool, Description("Capture OS-level present/frame pacing metrics for Sussudio using the PresentMon console executable.")]
    public static async Task<string> capture_presentmon(
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
        swapChainAddress ??= resolved.SwapChainAddress;
        appPresentId ??= resolved.PresentId;
        appSourceSequenceNumber ??= resolved.SourceSequenceNumber;
        appPresentUtcUnixMs ??= resolved.PresentUtcUnixMs;
        var result = await PresentMonProbe.RunAsync(new PresentMonProbeOptions
        {
            DurationSeconds = seconds,
            ProcessId = processId,
            ProcessName = processName,
            ExpectedSwapChainAddress = swapChainAddress,
            AppPresentId = appPresentId,
            AppSourceSequenceNumber = appSourceSequenceNumber,
            AppPresentUtcUnixMs = appPresentUtcUnixMs,
            PresentMonPath = presentMonPath,
            OutputFile = outputPath,
            KeepCsv = keepCsv,
            TrackGpuVideo = trackGpuVideo
        }).ConfigureAwait(false);

        return PresentMonProbe.Format(result);
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
        swapChainAddress ??= resolved.SwapChainAddress;
        appPresentId ??= resolved.PresentId;
        appSourceSequenceNumber ??= resolved.SourceSequenceNumber;
        appPresentUtcUnixMs ??= resolved.PresentUtcUnixMs;
        return await PresentMonProbe.RunAsync(new PresentMonProbeOptions
        {
            DurationSeconds = seconds,
            ProcessId = processId,
            ProcessName = processName,
            ExpectedSwapChainAddress = swapChainAddress,
            AppPresentId = appPresentId,
            AppSourceSequenceNumber = appSourceSequenceNumber,
            AppPresentUtcUnixMs = appPresentUtcUnixMs,
            PresentMonPath = presentMonPath,
            OutputFile = outputPath,
            KeepCsv = keepCsv,
            TrackGpuVideo = trackGpuVideo
        }).ConfigureAwait(false);
    }

    private static async Task<(string? SwapChainAddress, long? PresentId, long? SourceSequenceNumber, long? PresentUtcUnixMs)> TryResolvePreviewPresentCorrelationAsync(PipeClient pipeClient)
    {
        try
        {
            var response = await pipeClient.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response) ||
                !response.TryGetProperty("Snapshot", out var snapshot))
            {
                return default;
            }

            var address = AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DSwapChainAddress", string.Empty);
            return (
                string.IsNullOrWhiteSpace(address) ? null : address,
                GetPositiveLong(snapshot, "PreviewD3DLastRenderedPreviewPresentId"),
                GetNonNegativeLong(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber"),
                GetPositiveLong(snapshot, "PreviewD3DLastRenderedUtcUnixMs"));
        }
        catch (JsonException)
        {
            return default;
        }
        catch (IOException)
        {
            return default;
        }
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
