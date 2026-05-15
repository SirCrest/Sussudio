namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
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
                "SetStatsSectionVisible",
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
            "SetStatsVisible",
            new Dictionary<string, object?> { ["visible"] = ParseShowHide(subcommand, "stats show|hide") },
            includeData: false);
    }

    private static Task<int> HandleSettingsAsync(CommandContext context)
    {
        EnsureArgCount(context.Rest, 1, "settings show|hide");
        var visible = ParseShowHide(context.Rest[0], "settings show|hide");
        return HandleSimpleCommandAsync(
            context,
            "SetSettingsVisible",
            new Dictionary<string, object?> { ["visible"] = visible },
            includeData: false);
    }

    private static Task<int> HandleFrameTimeAsync(CommandContext context)
    {
        EnsureArgCount(context.Rest, 1, "frametime show|hide");
        var visible = ParseShowHide(context.Rest[0], "frametime show|hide");
        return HandleSimpleCommandAsync(
            context,
            "SetFrameTimeOverlayVisible",
            new Dictionary<string, object?> { ["visible"] = visible },
            includeData: false);
    }
}
