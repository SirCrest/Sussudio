namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static async Task<int> HandleWindowAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "window <command>").ToLowerInvariant();
        switch (subcommand)
        {
            case "close":
            {
                EnsureArgCount(context.Rest, 1, "window close");
                var armResponse = await context.Transport.SendCommandAsync(
                    Sussudio.Models.AutomationCommandKind.ArmClose,
                    new Dictionary<string, object?> { ["armed"] = true }).ConfigureAwait(false);
                if (!IsSuccess(armResponse))
                {
                    return WriteResponse(armResponse, context.GlobalJson, response => Formatters.FormatResult(response, includeData: false));
                }

                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.WindowAction,
                    new Dictionary<string, object?> { ["action"] = "Close" },
                    includeData: false).ConfigureAwait(false);
            }
            case "minimize":
            case "maximize":
            case "restore":
            case "center":
                EnsureArgCount(context.Rest, 1, $"window {subcommand}");
                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.WindowAction,
                    new Dictionary<string, object?> { ["action"] = Capitalize(subcommand) },
                    includeData: false).ConfigureAwait(false);
            case "fullscreen":
            case "full-screen":
                EnsureArgCount(context.Rest, 2, "window fullscreen on|off");
                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.SetFullScreenEnabled,
                    new Dictionary<string, object?> { ["enabled"] = ParseOnOff(RequireWord(context.Rest, 1, "window fullscreen on|off")) },
                    includeData: false).ConfigureAwait(false);
            case "snap":
                EnsureArgCount(context.Rest, 2, "window snap <dir>");
                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.WindowAction,
                    new Dictionary<string, object?> { ["action"] = MapSnapAction(RequireWord(context.Rest, 1, "window snap <dir>")) },
                    includeData: false).ConfigureAwait(false);
            case "move":
                EnsureArgCount(context.Rest, 3, "window move <x> <y>");
                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.WindowAction,
                    new Dictionary<string, object?>
                    {
                        ["action"] = "Move",
                        ["x"] = ParseInt(RequireWord(context.Rest, 1, "window move <x> <y>")),
                        ["y"] = ParseInt(RequireWord(context.Rest, 2, "window move <x> <y>"))
                    },
                    includeData: false).ConfigureAwait(false);
            case "resize":
                EnsureArgCount(context.Rest, 3, "window resize <w> <h>");
                return await HandleSimpleCommandAsync(
                    context,
                    Sussudio.Models.AutomationCommandKind.WindowAction,
                    new Dictionary<string, object?>
                    {
                        ["action"] = "Resize",
                        ["width"] = ParseInt(RequireWord(context.Rest, 1, "window resize <w> <h>")),
                        ["height"] = ParseInt(RequireWord(context.Rest, 2, "window resize <w> <h>"))
                    },
                    includeData: false).ConfigureAwait(false);
            default:
                throw new UsageException($"Unknown window command '{subcommand}'.");
        }
    }
}
