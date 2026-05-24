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

    private static Task<int> HandleRecordingsAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "recordings open").ToLowerInvariant();
        switch (subcommand)
        {
            case "open":
                EnsureArgCount(context.Rest, 1, "recordings open");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.OpenRecordingsFolder, includeData: false);
            default:
                throw new UsageException($"Unknown recordings command '{subcommand}'.");
        }
    }

    private static Task<int> HandleStatsAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "stats show|hide").ToLowerInvariant();
        if (subcommand == "section")
        {
            if (context.Rest.Count < 3)
            {
                throw new UsageException("stats section <name> show|hide");
            }

            var sectionName = JoinRange(context.Rest, 1, context.Rest.Count - 1);
            var visible = ParseShowHide(context.Rest[^1], "stats section <name> show|hide");
            return HandleSimpleCommandAsync(
                context,
                Sussudio.Models.AutomationCommandKind.SetStatsSectionVisible,
                new Dictionary<string, object?>
                {
                    ["section"] = sectionName,
                    ["visible"] = visible
                },
                includeData: false);
        }

        EnsureArgCount(context.Rest, 1, "stats show|hide");
        return HandleSimpleCommandAsync(
            context,
            Sussudio.Models.AutomationCommandKind.SetStatsVisible,
            new Dictionary<string, object?> { ["visible"] = ParseShowHide(subcommand, "stats show|hide") },
            includeData: false);
    }

    private static Task<int> HandleSettingsAsync(CommandContext context)
    {
        EnsureArgCount(context.Rest, 1, "settings show|hide");
        var visible = ParseShowHide(context.Rest[0], "settings show|hide");
        return HandleSimpleCommandAsync(
            context,
            Sussudio.Models.AutomationCommandKind.SetSettingsVisible,
            new Dictionary<string, object?> { ["visible"] = visible },
            includeData: false);
    }

    private static Task<int> HandleFrameTimeAsync(CommandContext context)
    {
        EnsureArgCount(context.Rest, 1, "frametime show|hide");
        var visible = ParseShowHide(context.Rest[0], "frametime show|hide");
        return HandleSimpleCommandAsync(
            context,
            Sussudio.Models.AutomationCommandKind.SetFrameTimeOverlayVisible,
            new Dictionary<string, object?> { ["visible"] = visible },
            includeData: false);
    }
}
