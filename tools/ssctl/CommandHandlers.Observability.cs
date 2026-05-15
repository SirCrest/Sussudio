using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static async Task<int> HandleStateAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var response = await context.Transport.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatSnapshot);
    }

    private static async Task<int> HandleDiagnosticsAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var max = ParseOptionalIntFlag(context.Rest, "--max") ?? 100;
        EnsureNoArgs(context.Rest, "diagnostics [--max N] [--json]");

        var response = await context.Transport.SendCommandAsync(
            "GetDiagnostics",
            new Dictionary<string, object?> { ["maxEvents"] = max }).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatDiagnostics);
    }

    private static async Task<int> HandleOptionsAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "options [--json]");

        var response = await context.Transport.SendCommandAsync("GetCaptureOptions").ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatOptions);
    }

    private static async Task<int> HandleManifestAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "manifest [--json]");

        var response = await context.Transport.SendCommandAsync("GetAutomationManifest").ConfigureAwait(false);
        return WriteResponse(response, json, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static async Task<int> HandleTimelineAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var max = ParseOptionalIntFlag(context.Rest, "--max") ?? 240;
        EnsureNoArgs(context.Rest, "timeline [--max N] [--json]");

        var response = await context.Transport.SendCommandAsync(
            "GetPerformanceTimeline",
            new Dictionary<string, object?> { ["maxEntries"] = max }).ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatTimeline);
    }

    private static async Task<int> HandleMemoryAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "memory [--json]");

        var response = await context.Transport.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
        return WriteResponse(response, json, Formatters.FormatMemory);
    }

    private static async Task<int> HandleAudioRampTraceAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        EnsureNoArgs(context.Rest, "audio-ramp-trace [--json]");

        var response = await context.Transport.SendCommandAsync("GetAudioRampTrace").ConfigureAwait(false);
        return WriteResponse(response, json, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

}
