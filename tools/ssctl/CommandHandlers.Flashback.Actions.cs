namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static Task<int> HandleFlashbackActionAsync(CommandContext context, string subcommand)
    {
        switch (subcommand)
        {
            case "play":
            {
                var playPayload = new Dictionary<string, object?> { ["action"] = "play" };
                if (context.Rest.Count >= 2)
                    playPayload["positionMs"] = ParseFlashbackPositionMs(context.Rest[1]);
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction, playPayload, includeData: true);
            }
            case "pause":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "pause" }, includeData: true);
            case "go-live":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "go-live" }, includeData: true);
            case "seek":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?>
                    {
                        ["action"] = "seek",
                        ["positionMs"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, "flashback seek <ms>"))
                    }, includeData: true);
            case "begin-scrub":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?>
                    {
                        ["action"] = "begin-scrub",
                        ["positionMs"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, "flashback begin-scrub <ms>"))
                    }, includeData: true);
            case "update-scrub":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?>
                    {
                        ["action"] = "update-scrub",
                        ["positionMs"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, "flashback update-scrub <ms>"))
                    }, includeData: true);
            case "end-scrub":
            {
                var payload = new Dictionary<string, object?> { ["action"] = "end-scrub" };
                if (context.Rest.Count >= 2)
                    payload["positionMs"] = ParseFlashbackPositionMs(context.Rest[1]);
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction, payload, includeData: true);
            }
            case "set-in":
            case "set-in-point":
                EnsureArgCount(context.Rest, 1, "flashback set-in");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "set-in-point" }, includeData: true);
            case "set-out":
            case "set-out-point":
                EnsureArgCount(context.Rest, 1, "flashback set-out");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "set-out-point" }, includeData: true);
            case "clear-range":
            case "clear-in-out":
                EnsureArgCount(context.Rest, 1, "flashback clear-range");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, includeData: true);
            default:
                throw new UsageException($"Unknown flashback action '{subcommand}'.");
        }
    }

    private static double ParseFlashbackPositionMs(string value)
    {
        var parsed = ParseDouble(value);
        if (!double.IsFinite(parsed) || parsed < 0 || parsed > TimeSpan.MaxValue.TotalMilliseconds)
        {
            throw new UsageException("Flashback position must be finite, non-negative, and within TimeSpan range.");
        }

        return parsed;
    }
}
