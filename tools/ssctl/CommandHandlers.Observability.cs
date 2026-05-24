using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static async Task<int> HandleStateAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatSnapshot);
    }

    private static async Task<int> HandleDiagnosticsAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var max = ParseOptionalIntFlag(context.Rest, "--max") ?? 100;
        EnsureNoArgs(context.Rest, "diagnostics [--max N] [--json]");

        var response = await context.Transport.SendCommandAsync(
            Sussudio.Models.AutomationCommandKind.GetDiagnostics,
            new Dictionary<string, object?> { ["maxEvents"] = max }).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatDiagnostics);
    }

    private static async Task<int> HandleOptionsAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "options [--json]");

        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetCaptureOptions).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatOptions);
    }

    private static async Task<int> HandleManifestAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "manifest [--json]");

        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetAutomationManifest).ConfigureAwait(false);
        return WriteResponse(response, json, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static async Task<int> HandleTimelineAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var max = ParseOptionalIntFlag(context.Rest, "--max") ?? 240;
        EnsureNoArgs(context.Rest, "timeline [--max N] [--json]");

        var response = await context.Transport.SendCommandAsync(
            Sussudio.Models.AutomationCommandKind.GetPerformanceTimeline,
            new Dictionary<string, object?> { ["maxEntries"] = max }).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatTimeline);
    }

    private static async Task<int> HandleMemoryAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "memory [--json]");

        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatMemory);
    }

    private static async Task<int> HandleAudioRampTraceAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "audio-ramp-trace [--json]");

        var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetAudioRampTrace).ConfigureAwait(false);
        return WriteResponse(response, json, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

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
        var resolved = await TryResolvePreviewPresentCorrelationAsync(context).ConfigureAwait(false);

        var result = await PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(
            seconds,
            pid,
            processName,
            swapChainAddress,
            appPresentId,
            appSourceSequenceNumber,
            appPresentUtcUnixMs,
            captureStartUtcUnixMs,
            presentMonPath,
            outputPath,
            keepCsv,
            !noGpuVideo,
            resolved)).ConfigureAwait(false);

        Console.WriteLine(json ? PrettyJson(result) : PresentMonProbe.Format(result));
        return result.Success ? 0 : 3;
    }

    private static async Task<PresentMonProbeCorrelation> TryResolvePreviewPresentCorrelationAsync(CommandContext context)
    {
        try
        {
            var response = await context.Transport.SendCommandAsync(Sussudio.Models.AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response) ||
                !response.TryGetProperty("Snapshot", out var snapshot))
            {
                return default;
            }

            return PresentMonProbe.ReadPreviewCorrelation(snapshot);
        }
        catch
        {
            return default;
        }
    }

    private static async Task<int> HandleDiagnosticSessionAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var scenario = ParseOptionalStringFlag(context.Rest, "--scenario") ?? DiagnosticSessionOptions.DefaultScenario;
        var seconds = ParseOptionalIntFlag(context.Rest, "--seconds") ?? DiagnosticSessionOptions.DefaultDurationSeconds;
        var sampleIntervalMs = ParseOptionalIntFlag(context.Rest, "--sample-ms") ?? DiagnosticSessionOptions.DefaultSampleIntervalMs;
        var outputDirectory = ParseOptionalStringFlag(context.Rest, "--output");
        var presentMonPath = ParseOptionalStringFlag(context.Rest, "--presentmon-path");
        var includePresentMon = ConsumeFlag(context.Rest, "--presentmon");
        var verify = ConsumeFlag(context.Rest, "--verify");
        var leaveRunning = ConsumeFlag(context.Rest, "--leave-running");
        EnsureNoArgs(context.Rest, DiagnosticSessionOptions.CliUsage);

        var result = await DiagnosticSessionRunner.RunAsync(
                new DiagnosticSessionOptions
                {
                    Scenario = scenario,
                    DurationSeconds = seconds,
                    SampleIntervalMs = sampleIntervalMs,
                    OutputDirectory = outputDirectory,
                    IncludePresentMon = includePresentMon,
                    PresentMonPath = presentMonPath,
                    VerifyRecording = verify,
                    LeaveRunning = leaveRunning
                },
                (command, payload, responseTimeoutMs) => context.Transport.SendCommandAsync(command, payload, responseTimeoutMs))
            .ConfigureAwait(false);

        Console.WriteLine(json ? PrettyJson(result) : DiagnosticSessionRunner.Format(result));
        return result.Success ? 0 : 3;
    }

}
