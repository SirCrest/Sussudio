using System.IO;
using System.Text.Json;
using Sussudio.Models;

namespace Sussudio.Tools.Ssctl;

// Agent-sense verbs: `audio listen` (ears: records and analyzes the app's own rendered audio) and `ui tree` / `ui invoke`
// (fingers: reads and operates the live WinUI controls). Payload shapes and help text come from the automation command
// catalog; formatting lives in SenseFormatters. All three are projections of app commands 65-67; there is no tool-only
// verb here. Contract: docs/architecture/performance-telemetry.md sections 2.9 and 2.10.
internal static partial class CommandHandlers
{
    private const string AudioUsage = "audio listen [--seconds N] [--wav PATH] [--latency] [--json]";
    private const string UiUsage = "ui tree|invoke ...";
    private const string UiTreeUsage = "ui tree [--root NAME] [--depth N] [--all] [--json]";
    private const string UiInvokeUsage = "ui invoke <target> [action] [value] [--force] [--json]";

    private static Task<int> HandleAudioAsync(CommandContext context)
    {
        var verb = RequireWord(context.Rest, 0, AudioUsage).ToLowerInvariant();
        context.Rest.RemoveAt(0);
        return verb switch
        {
            "listen" => HandleAudioListenAsync(context),
            _ => throw new UsageException(AudioUsage),
        };
    }

    private static async Task<int> HandleAudioListenAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var measureLatency = ConsumeFlag(context.Rest, "--latency");
        var secondsText = ParseOptionalStringFlag(context.Rest, "--seconds");
        var wav = ParseOptionalStringFlag(context.Rest, "--wav");
        EnsureNoArgs(context.Rest, AudioUsage);

        var payload = new Dictionary<string, object?>();
        double? seconds = secondsText is null ? null : ParseDouble(secondsText);
        if (seconds is not null)
        {
            // Range is enforced by the app (0.5 to 30) so there is one source of truth; this only rejects what is not a number.
            payload[AutomationPayloadKeys.Seconds] = seconds.Value;
        }

        if (wav is not null)
        {
            payload[AutomationPayloadKeys.OutputPath] = Path.GetFullPath(wav);
        }

        if (measureLatency)
        {
            payload[AutomationPayloadKeys.MeasureLatency] = true;
        }

        // The capture itself takes `seconds` of wall time on top of activation and analysis; the catalog declares the
        // extended response timeout, which the transport applies for this command kind.
        var response = await context.SendCommandAsync(AutomationCommandKind.CaptureAudioOutput, payload).ConfigureAwait(false);
        return WriteResponse(response, json, SenseFormatters.FormatAudioListen);
    }

    private static Task<int> HandleUiAsync(CommandContext context)
    {
        var verb = RequireWord(context.Rest, 0, UiUsage).ToLowerInvariant();
        context.Rest.RemoveAt(0);
        return verb switch
        {
            "tree" => HandleUiTreeAsync(context),
            "invoke" => HandleUiInvokeAsync(context),
            _ => throw new UsageException(UiUsage),
        };
    }

    private static async Task<int> HandleUiTreeAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var all = ConsumeFlag(context.Rest, "--all");
        var root = ParseOptionalStringFlag(context.Rest, "--root");
        var depth = ParseOptionalIntFlag(context.Rest, "--depth");
        EnsureNoArgs(context.Rest, UiTreeUsage);

        var payload = new Dictionary<string, object?>();
        if (root is not null)
        {
            payload[AutomationPayloadKeys.Root] = root;
        }

        if (depth is not null)
        {
            payload[AutomationPayloadKeys.MaxDepth] = depth.Value;
        }

        if (all)
        {
            payload[AutomationPayloadKeys.InteractiveOnly] = false;
        }

        var response = await context.SendCommandAsync(AutomationCommandKind.GetUiTree, payload).ConfigureAwait(false);
        return WriteResponse(response, json, SenseFormatters.FormatUiTree);
    }

    private static async Task<int> HandleUiInvokeAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var force = ConsumeFlag(context.Rest, "--force");
        var target = RequireWord(context.Rest, 0, UiInvokeUsage);

        var payload = new Dictionary<string, object?> { [AutomationPayloadKeys.Target] = target };
        if (context.Rest.Count > 1)
        {
            // The action is passed through unvalidated: the app owns the action list and answers an unknown one with
            // the supported set, so ssctl never drifts from it.
            payload[AutomationPayloadKeys.Action] = context.Rest[1];
        }

        if (context.Rest.Count > 2)
        {
            // Everything after the action is the value, so `set-value Hello world` needs no quoting.
            payload[AutomationPayloadKeys.Value] = JoinRemaining(context.Rest, 2);
        }

        if (force)
        {
            payload[AutomationPayloadKeys.Force] = true;
        }

        var response = await context.SendCommandAsync(AutomationCommandKind.InvokeUiElement, payload).ConfigureAwait(false);
        return WriteResponse(response, json, SenseFormatters.FormatUiInvoke);
    }
}
