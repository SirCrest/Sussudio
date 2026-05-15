namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static async Task<int> HandleDeviceAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "device <command>").ToLowerInvariant();
        switch (subcommand)
        {
            case "refresh":
                EnsureArgCount(context.Rest, 1, "device refresh");
                return await HandleSimpleCommandAsync(
                    context,
                    "RefreshDevices",
                    includeData: false).ConfigureAwait(false);
            case "list":
            {
                EnsureArgCount(context.Rest, 1, "device list");
                var refreshResponse = await context.Transport.SendCommandAsync("RefreshDevices").ConfigureAwait(false);
                if (!IsSuccess(refreshResponse))
                {
                    return WriteResponse(refreshResponse, context.GlobalJson, response => Formatters.FormatResult(response, includeData: false));
                }

                var optionsResponse = await context.Transport.SendCommandAsync("GetCaptureOptions").ConfigureAwait(false);
                return WriteResponse(optionsResponse, context.GlobalJson, Formatters.FormatDeviceList);
            }
            case "select":
                if (context.Rest.Count < 2)
                {
                    throw new UsageException("device select <name>");
                }

                return await HandleSimpleCommandAsync(
                    context,
                    "SelectDevice",
                    new Dictionary<string, object?> { ["deviceName"] = JoinRemaining(context.Rest, 1) },
                    includeData: false).ConfigureAwait(false);
            case "audio-select":
                if (context.Rest.Count < 2)
                {
                    throw new UsageException("device audio-select <name>");
                }

                return await HandleSimpleCommandAsync(
                    context,
                    "SelectAudioInputDevice",
                    new Dictionary<string, object?> { ["deviceName"] = JoinRemaining(context.Rest, 1) },
                    includeData: false).ConfigureAwait(false);
            case "custom-audio":
                EnsureArgCount(context.Rest, 2, "device custom-audio on|off");
                return await HandleSimpleCommandAsync(
                    context,
                    "SetCustomAudioInput",
                    new Dictionary<string, object?> { ["enabled"] = ParseOnOff(RequireWord(context.Rest, 1, "device custom-audio on|off")) },
                    includeData: false).ConfigureAwait(false);
            default:
                throw new UsageException($"Unknown device command '{subcommand}'.");
        }
    }
}
