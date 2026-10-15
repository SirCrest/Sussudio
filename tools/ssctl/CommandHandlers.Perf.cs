using System.IO;
using Sussudio.Models;

namespace Sussudio.Tools.Ssctl;

// Performance-telemetry verbs: perf report|window|mark|trace, knobs, knob set|reset, log tail. Payload shapes and help
// text come from the automation command catalog; formatting lives in PerfFormatters. Contract:
// docs/architecture/performance-telemetry.md section 3.
internal static partial class CommandHandlers
{
    private const string PerfUsage = "perf report|window|mark|trace ...";
    private const string PerfReportUsage = "perf report [--window NAME] [--filter PREFIX] [--idle] [--json]";
    private const string PerfWindowUsage = "perf window begin|read|end|cancel|list [NAME] [--json]";
    private const string PerfMarkUsage = "perf mark <text>";
    private const string PerfTraceUsage = "perf trace start [--capacity N]|stop|status|dump [path]";
    private const string KnobsUsage = "knobs [FILTER] [--json]";
    private const string KnobUsage = "knob set <name> <value> | knob reset <name>";
    private const string LogUsage = "log tail [--max N] [--contains TEXT] [--json]";

    private static Task<int> HandlePerfAsync(CommandContext context)
    {
        var verb = RequireWord(context.Rest, 0, PerfUsage).ToLowerInvariant();
        context.Rest.RemoveAt(0);
        return verb switch
        {
            "report" => HandlePerfReportAsync(context),
            "window" => HandlePerfWindowAsync(context),
            "mark" => HandlePerfMarkAsync(context),
            "trace" => HandlePerfTraceAsync(context),
            _ => throw new UsageException(PerfUsage),
        };
    }

    private static async Task<int> HandlePerfReportAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var includeIdle = ConsumeFlag(context.Rest, "--idle");
        var window = ParseOptionalStringFlag(context.Rest, "--window");
        var filter = ParseOptionalStringFlag(context.Rest, "--filter");
        EnsureNoArgs(context.Rest, PerfReportUsage);

        var payload = new Dictionary<string, object?>();
        if (window is not null)
        {
            payload[AutomationPayloadKeys.Window] = window;
        }

        if (filter is not null)
        {
            payload[AutomationPayloadKeys.Filter] = filter;
        }

        if (includeIdle)
        {
            payload[AutomationPayloadKeys.IncludeIdle] = true;
        }

        var response = await context.SendCommandAsync(AutomationCommandKind.GetPerfReport, payload).ConfigureAwait(false);
        return WriteResponse(response, json, PerfFormatters.FormatReport);
    }

    private static async Task<int> HandlePerfWindowAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var action = RequireWord(context.Rest, 0, PerfWindowUsage).ToLowerInvariant();
        context.Rest.RemoveAt(0);
        if (action is not ("begin" or "read" or "end" or "cancel" or "list"))
        {
            throw new UsageException(PerfWindowUsage);
        }

        var payload = new Dictionary<string, object?> { [AutomationPayloadKeys.Action] = action };
        if (action == "list")
        {
            EnsureNoArgs(context.Rest, PerfWindowUsage);
        }
        else
        {
            payload[AutomationPayloadKeys.Name] = RequireWord(context.Rest, 0, PerfWindowUsage);
            EnsureArgCount(context.Rest, 1, PerfWindowUsage);
        }

        var response = await context.SendCommandAsync(AutomationCommandKind.PerfWindow, payload).ConfigureAwait(false);
        Func<System.Text.Json.JsonElement, string> formatter = action switch
        {
            "read" or "end" => PerfFormatters.FormatReport,
            "list" => PerfFormatters.FormatWindowList,
            _ => value => Formatters.FormatResult(value, includeData: false),
        };
        return WriteResponse(response, json, formatter);
    }

    private static async Task<int> HandlePerfMarkAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        if (context.Rest.Count == 0)
        {
            throw new UsageException(PerfMarkUsage);
        }

        var response = await context.SendCommandAsync(
            AutomationCommandKind.AddPerfMarker,
            new Dictionary<string, object?> { [AutomationPayloadKeys.Text] = JoinRemaining(context.Rest, 0) }).ConfigureAwait(false);
        return WriteResponse(response, json, value => Formatters.FormatResult(value, includeData: false));
    }

    private static async Task<int> HandlePerfTraceAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var action = RequireWord(context.Rest, 0, PerfTraceUsage).ToLowerInvariant();
        context.Rest.RemoveAt(0);

        var payload = new Dictionary<string, object?>();
        switch (action)
        {
            case "start":
                var capacity = ParseOptionalIntFlag(context.Rest, "--capacity");
                EnsureNoArgs(context.Rest, PerfTraceUsage);
                if (capacity is not null)
                {
                    payload[AutomationPayloadKeys.Capacity] = capacity.Value;
                }

                break;
            case "stop":
            case "status":
                EnsureNoArgs(context.Rest, PerfTraceUsage);
                break;
            case "dump":
                if (context.Rest.Count > 1)
                {
                    throw new UsageException(PerfTraceUsage);
                }

                // The app resolves a relative path against its own working directory, so the CLI resolves it first.
                if (context.Rest.Count == 1)
                {
                    payload[AutomationPayloadKeys.OutputPath] = Path.GetFullPath(context.Rest[0]);
                }

                break;
            default:
                throw new UsageException(PerfTraceUsage);
        }

        payload[AutomationPayloadKeys.Action] = action;
        var response = await context.SendCommandAsync(AutomationCommandKind.FrameTrace, payload).ConfigureAwait(false);
        return WriteResponse(response, json, value => Formatters.FormatResult(value, includeData: true));
    }

    private static async Task<int> HandleKnobsAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        if (context.Rest.Count > 1)
        {
            throw new UsageException(KnobsUsage);
        }

        var payload = new Dictionary<string, object?>();
        if (context.Rest.Count == 1)
        {
            payload[AutomationPayloadKeys.Filter] = context.Rest[0];
        }

        var response = await context.SendCommandAsync(AutomationCommandKind.GetTuningKnobs, payload).ConfigureAwait(false);
        return WriteResponse(response, json, PerfFormatters.FormatKnobs);
    }

    private static async Task<int> HandleKnobAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var verb = RequireWord(context.Rest, 0, KnobUsage).ToLowerInvariant();
        context.Rest.RemoveAt(0);

        var payload = new Dictionary<string, object?>();
        switch (verb)
        {
            case "set":
                payload[AutomationPayloadKeys.Name] = RequireWord(context.Rest, 0, KnobUsage);
                payload[AutomationPayloadKeys.Value] = RequireWord(context.Rest, 1, KnobUsage);
                EnsureArgCount(context.Rest, 2, KnobUsage);
                break;
            case "reset":
                payload[AutomationPayloadKeys.Name] = RequireWord(context.Rest, 0, KnobUsage);
                payload[AutomationPayloadKeys.Reset] = true;
                EnsureArgCount(context.Rest, 1, KnobUsage);
                break;
            default:
                throw new UsageException(KnobUsage);
        }

        var response = await context.SendCommandAsync(AutomationCommandKind.SetTuningKnob, payload).ConfigureAwait(false);
        return WriteResponse(response, json, PerfFormatters.FormatKnobChange);
    }

    private static async Task<int> HandleLogAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var verb = RequireWord(context.Rest, 0, LogUsage).ToLowerInvariant();
        context.Rest.RemoveAt(0);
        if (verb != "tail")
        {
            throw new UsageException(LogUsage);
        }

        var max = ParseOptionalIntFlag(context.Rest, "--max");
        var contains = ParseOptionalStringFlag(context.Rest, "--contains");
        EnsureNoArgs(context.Rest, LogUsage);

        var payload = new Dictionary<string, object?>();
        if (max is not null)
        {
            payload[AutomationPayloadKeys.MaxEntries] = max.Value;
        }

        if (contains is not null)
        {
            payload[AutomationPayloadKeys.Contains] = contains;
        }

        var response = await context.SendCommandAsync(AutomationCommandKind.GetLogTail, payload).ConfigureAwait(false);
        return WriteResponse(response, json, PerfFormatters.FormatLogTail);
    }
}
