using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static async Task<int> HandlePresentMonAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var seconds = ParseOptionalIntFlag(context.Rest, "--seconds") ?? 10;
        var pid = ParseOptionalIntFlag(context.Rest, "--pid");
        var processName = ParseOptionalStringFlag(context.Rest, "--process") ?? "Sussudio";
        var presentMonPath = ParseOptionalStringFlag(context.Rest, "--presentmon");
        var outputPath = ParseOptionalStringFlag(context.Rest, "--output");
        var swapChainAddress = ParseOptionalStringFlag(context.Rest, "--swapchain");
        var appPresentId = ParseOptionalLongFlag(context.Rest, "--app-present-id");
        var appSourceSequenceNumber = ParseOptionalLongFlag(context.Rest, "--app-source-seq");
        var appPresentUtcUnixMs = ParseOptionalLongFlag(context.Rest, "--app-present-utc-ms");
        var captureStartUtcUnixMs = ParseOptionalLongFlag(context.Rest, "--capture-start-utc-ms");
        var keepCsv = ConsumeFlag(context.Rest, "--keep-csv");
        var noGpuVideo = ConsumeFlag(context.Rest, "--no-gpu-video");
        EnsureNoArgs(context.Rest, "presentmon [--seconds N] [--pid PID|--process NAME] [--swapchain HEX] [--app-present-id N] [--app-source-seq N] [--app-present-utc-ms N] [--capture-start-utc-ms N] [--presentmon PATH] [--output PATH] [--keep-csv] [--json]");
        swapChainAddress ??= await TryResolvePreviewSwapChainAddressAsync(context).ConfigureAwait(false);

        var result = await PresentMonProbe.RunAsync(new PresentMonProbeOptions
        {
            ProcessId = pid,
            ProcessName = processName,
            DurationSeconds = seconds,
            PresentMonPath = presentMonPath,
            OutputFile = outputPath,
            ExpectedSwapChainAddress = swapChainAddress,
            AppPresentId = appPresentId,
            AppSourceSequenceNumber = appSourceSequenceNumber,
            AppPresentUtcUnixMs = appPresentUtcUnixMs,
            CaptureStartUtcUnixMs = captureStartUtcUnixMs,
            KeepCsv = keepCsv,
            TrackGpuVideo = !noGpuVideo
        }).ConfigureAwait(false);

        Console.WriteLine(json ? PrettyJson(result) : PresentMonProbe.Format(result));
        return result.Success ? 0 : 3;
    }

    private static async Task<string?> TryResolvePreviewSwapChainAddressAsync(CommandContext context)
    {
        try
        {
            var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response) ||
                !response.TryGetProperty("Snapshot", out var snapshot))
            {
                return null;
            }

            var address = AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DSwapChainAddress", string.Empty);
            return string.IsNullOrWhiteSpace(address) ? null : address;
        }
        catch
        {
            return null;
        }
    }
}
