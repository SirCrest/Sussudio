using System.Text.Json;
using Sussudio.Models;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static async Task<int> HandleWaitAsync(CommandContext context)
    {
        var condition = RequireWord(context.Rest, 0, "wait <condition> [--timeout ms] [--poll ms]");
        var args = context.Rest.Skip(1).ToList();
        var timeoutMs = ParseOptionalIntFlag(args, "--timeout");
        var pollMs = ParseOptionalIntFlag(args, "--poll");
        EnsureNoArgs(args, "wait <condition> [--timeout ms] [--poll ms]");

        var payload = new Dictionary<string, object?> { ["condition"] = condition };
        if (timeoutMs.HasValue)
        {
            payload["timeoutMs"] = timeoutMs.Value;
        }

        if (pollMs.HasValue)
        {
            payload["pollMs"] = pollMs.Value;
        }

        var responseTimeoutMs = Math.Max(timeoutMs.GetValueOrDefault(0) + 5000, 60000);
        var response = await context.Transport.SendCommandAsync(
            AutomationCommandKind.WaitForCondition,
            payload,
            responseTimeoutMs).ConfigureAwait(false);
        return WriteResponse(response, context.GlobalJson, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static async Task<int> HandleAssertAsync(CommandContext context)
    {
        object assertionsPayload;
        if (context.Rest.Count == 3 && !LooksLikeJson(context.Rest[0]))
        {
            assertionsPayload = new[]
            {
                new Dictionary<string, object?>
                {
                    ["field"] = context.Rest[0],
                    ["op"] = context.Rest[1],
                    ["value"] = ParseAssertionValue(context.Rest[2])
                }
            };
        }
        else
        {
            var assertionsJson = JoinRemaining(context.Rest, 0);
            if (string.IsNullOrWhiteSpace(assertionsJson))
            {
                throw new UsageException("assert <json> OR assert <field> <op> <value>");
            }

            using var document = JsonDocument.Parse(assertionsJson);
            assertionsPayload = document.RootElement.Clone();
        }

        var response = await context.Transport.SendCommandAsync(
            AutomationCommandKind.AssertSnapshot,
            new Dictionary<string, object?> { ["assertions"] = assertionsPayload }).ConfigureAwait(false);
        return WriteResponse(response, context.GlobalJson, responseValue => Formatters.FormatResult(responseValue, includeData: true));
    }

    private static Task<int> HandleProbeAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "probe source|color").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "probe source|color");
        return subcommand switch
        {
            "source" => HandleSimpleCommandAsync(context, AutomationCommandKind.ProbeVideoSource, includeData: true),
            "color" => HandleSimpleCommandAsync(context, AutomationCommandKind.ProbePreviewColor, includeData: true),
            _ => throw new UsageException($"Unknown probe command '{subcommand}'.")
        };
    }

    private static async Task<int> HandleVerifyAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var verificationProfile =
            ParseOptionalStringFlag(context.Rest, "--profile") ??
            ParseOptionalStringFlag(context.Rest, "--verification-profile");
        if (context.Rest.Count > 0)
        {
            var filePath = JoinRemaining(context.Rest, 0);
            var payload = new Dictionary<string, object?> { ["filePath"] = filePath };
            if (!string.IsNullOrWhiteSpace(verificationProfile))
            {
                payload["verificationProfile"] = verificationProfile;
            }

            var response = await context.Transport.SendCommandAsync(
                AutomationCommandKind.VerifyFile,
                payload,
                60000).ConfigureAwait(false);
            return WriteResponse(response, json, value => Formatters.FormatResult(value, includeData: true));
        }
        else
        {
            // Verify last recording (existing behavior)
            var response = await context.Transport.SendCommandAsync(AutomationCommandKind.VerifyLastRecording).ConfigureAwait(false);
            return WriteResponse(response, json, value => Formatters.FormatResult(value, includeData: true));
        }
    }
}
