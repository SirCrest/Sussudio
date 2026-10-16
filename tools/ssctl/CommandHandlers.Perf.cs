using System.IO;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools.Performance;

namespace Sussudio.Tools.Ssctl;

// Performance-telemetry verbs: perf report|window|mark|trace|experiment|lease, knobs, knob set|reset, log tail. Payload shapes
// and help text come from the automation command catalog; formatting lives in PerfFormatters. Contract:
// docs/architecture/performance-telemetry.md section 3.
//
// Two verbs are tool-only and have no automation command behind them (section 7): `perf trace analyze` reads a dump
// file on this machine and needs no running app, and `perf experiment` is a client of the existing commands (knobs,
// preview and recording control, performance windows) driven by the shared runner in tools/Common/Performance.
internal static partial class CommandHandlers
{
    private const string PerfUsage = "perf report|window|mark|trace|experiment|lease ...";
    private const string PerfReportUsage = "perf report [--window NAME] [--filter PREFIX] [--idle] [--json]";
    private const string PerfWindowUsage = "perf window begin|read|end|cancel|list [NAME] [--json]";
    private const string PerfMarkUsage = "perf mark <text>";
    private const string PerfTraceUsage = "perf trace start [--capacity N]|stop|status|dump [path]|analyze <file> [--top N] [--json]";
    private const string PerfTraceAnalyzeUsage = "perf trace analyze <file> [--top N] [--json]";
    private const string PerfExperimentUsage = "perf experiment <spec.json> [--out DIR] [--json]";
    private const string PerfLeaseUsage = "perf lease [status|release --force] [--json]";
    private const int MaxAnalyzeTopFrames = 1000;
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
            "experiment" => HandlePerfExperimentAsync(context),
            "lease" => HandlePerfLeaseAsync(context),
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
        if (action == "analyze")
        {
            return HandlePerfTraceAnalyze(context, json);
        }

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

    // Offline: reads a dump written by `perf trace dump` and prints where the frames spent their time. No pipe traffic,
    // so it works with the app closed. A file that is not a complete trace is exit 3 with the reason, like any other
    // failed result; the JSON form keeps stdout machine-readable with the same Success/Message envelope.
    private static int HandlePerfTraceAnalyze(CommandContext context, bool json)
    {
        var top = ParseOptionalIntFlag(context.Rest, "--top");
        if (top is < 1 or > MaxAnalyzeTopFrames)
        {
            throw new UsageException($"{PerfTraceAnalyzeUsage} (--top is 1 to {MaxAnalyzeTopFrames})");
        }

        var file = RequireWord(context.Rest, 0, PerfTraceAnalyzeUsage);
        EnsureArgCount(context.Rest, 1, PerfTraceAnalyzeUsage);

        try
        {
            var options = top is { } topFrames ? new FrameTraceAnalysisOptions { TopFrames = topFrames } : null;
            var analysis = FrameTraceAnalyzer.AnalyzeFile(Path.GetFullPath(file), options);
            Console.Write(json ? analysis.ToJson() + Environment.NewLine : analysis.FormatSummary());
            return 0;
        }
        catch (Exception ex) when (ex is FrameTraceFormatException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return WritePerfToolFailure(ex.Message, json);
        }
    }

    // Runs a knob A/B experiment against the live app (section 7). Takes minutes: variants x repeats x (warmup +
    // measure window) plus restarts. Progress goes to stderr so `--json` leaves stdout as one JSON document. Ctrl-C
    // cancels the run; the runner still restores the app before it returns. Exit 0 only when every run completed and
    // the pre-experiment state was verified restored; any other outcome, including a spec the app rejects, is exit 3.
    private static async Task<int> HandlePerfExperimentAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var outputDirectory = ParseOptionalStringFlag(context.Rest, "--out");
        var specPath = RequireWord(context.Rest, 0, PerfExperimentUsage);
        EnsureArgCount(context.Rest, 1, PerfExperimentUsage);

        PerfExperimentSpec spec;
        string? resolvedOutput;
        try
        {
            spec = PerfExperimentSpec.Load(Path.GetFullPath(specPath));
            resolvedOutput = outputDirectory is null ? null : Path.GetFullPath(outputDirectory);
        }
        catch (PerfExperimentSpecException ex)
        {
            return WritePerfToolFailure(
                "Invalid experiment spec:" + Environment.NewLine + string.Join(Environment.NewLine, ex.Problems.Select(problem => "  - " + problem)),
                json);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return WritePerfToolFailure(ex.Message, json);
        }

        var result = await PerfExperimentRunner.RunAsync(
                spec,
                (command, payload, responseTimeoutMs, commandToken) =>
                    context.Transport.SendCommandAsync(command, payload, responseTimeoutMs, commandToken),
                new PerfExperimentOptions
                {
                    OutputDirectory = resolvedOutput,
                    Progress = line => Console.Error.WriteLine(line),
                },
                context.RequestCancellationToken)
            .ConfigureAwait(false);

        Console.Write(json ? result.ToJson() + Environment.NewLine : result.FormatSummary());
        return result.Succeeded ? 0 : 3;
    }

    // Status, or a forced release of a lease a dead or stuck experiment left behind. A plain release needs the holder's
    // lease id, which only the experiment runner has, so the CLI offers only the forced form.
    private static async Task<int> HandlePerfLeaseAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var force = ConsumeFlag(context.Rest, "--force");
        var action = context.Rest.Count == 0 ? "status" : RequireWord(context.Rest, 0, PerfLeaseUsage).ToLowerInvariant();
        if (context.Rest.Count > 0)
        {
            context.Rest.RemoveAt(0);
        }

        EnsureNoArgs(context.Rest, PerfLeaseUsage);
        if (action is not ("status" or "release") || force != (action == "release"))
        {
            throw new UsageException(PerfLeaseUsage);
        }

        var payload = new Dictionary<string, object?> { [AutomationPayloadKeys.Action] = action };
        if (force)
        {
            payload[AutomationPayloadKeys.Force] = true;
        }

        var response = await context.SendCommandAsync(AutomationCommandKind.PerfExperimentLease, payload).ConfigureAwait(false);
        return WriteResponse(response, json, PerfFormatters.FormatLease);
    }

    private static int WritePerfToolFailure(string message, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { Success = false, Message = message }));
        }
        else
        {
            Console.Error.WriteLine(message);
        }

        return 3;
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
