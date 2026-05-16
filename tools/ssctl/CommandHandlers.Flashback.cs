namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static Task<int> HandleFlashbackAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "flashback on|off|timeline|play|pause|go-live|seek|begin-scrub|update-scrub|end-scrub|set-in|set-out|clear-range|export|segments|apply").ToLowerInvariant();
        switch (subcommand)
        {
            case "timeline":
            {
                EnsureArgCount(context.Rest, 2, "flashback timeline show|hide");
                var visible = ParseShowHide(context.Rest[1], "flashback timeline show|hide");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.SetFlashbackTimelineVisible,
                    new Dictionary<string, object?> { ["visible"] = visible }, includeData: false);
            }
            case "on":
            case "enable":
                EnsureArgCount(context.Rest, 1, "flashback on|off");
                return HandleSimpleCommandAsync(context, "SetFlashbackEnabled",
                    new Dictionary<string, object?> { ["enabled"] = true }, includeData: false);
            case "off":
            case "disable":
                EnsureArgCount(context.Rest, 1, "flashback on|off");
                return HandleSimpleCommandAsync(context, "SetFlashbackEnabled",
                    new Dictionary<string, object?> { ["enabled"] = false }, includeData: false);
            case "apply":
            case "restart":
                return HandleSimpleCommandAsync(context, "RestartFlashback", includeData: false);
            case "play":
            {
                var playPayload = new Dictionary<string, object?> { ["action"] = "play" };
                if (context.Rest.Count >= 2)
                    playPayload["positionMs"] = ParseFlashbackPositionMs(context.Rest[1]);
                return HandleSimpleCommandAsync(context, "FlashbackAction", playPayload, includeData: true);
            }
            case "pause":
                return HandleSimpleCommandAsync(context, "FlashbackAction",
                    new Dictionary<string, object?> { ["action"] = "pause" }, includeData: true);
            case "go-live":
                return HandleSimpleCommandAsync(context, "FlashbackAction",
                    new Dictionary<string, object?> { ["action"] = "go-live" }, includeData: true);
            case "seek":
                return HandleSimpleCommandAsync(context, "FlashbackAction",
                    new Dictionary<string, object?>
                    {
                        ["action"] = "seek",
                        ["positionMs"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, "flashback seek <ms>"))
                    }, includeData: true);
            case "begin-scrub":
                return HandleSimpleCommandAsync(context, "FlashbackAction",
                    new Dictionary<string, object?>
                    {
                        ["action"] = "begin-scrub",
                        ["positionMs"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, "flashback begin-scrub <ms>"))
                    }, includeData: true);
            case "update-scrub":
                return HandleSimpleCommandAsync(context, "FlashbackAction",
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
                return HandleSimpleCommandAsync(context, "FlashbackAction", payload, includeData: true);
            }
            case "set-in":
            case "set-in-point":
                EnsureArgCount(context.Rest, 1, "flashback set-in");
                return HandleSimpleCommandAsync(context, "FlashbackAction",
                    new Dictionary<string, object?> { ["action"] = "set-in-point" }, includeData: true);
            case "set-out":
            case "set-out-point":
                EnsureArgCount(context.Rest, 1, "flashback set-out");
                return HandleSimpleCommandAsync(context, "FlashbackAction",
                    new Dictionary<string, object?> { ["action"] = "set-out-point" }, includeData: true);
            case "clear-range":
            case "clear-in-out":
                EnsureArgCount(context.Rest, 1, "flashback clear-range");
                return HandleSimpleCommandAsync(context, "FlashbackAction",
                    new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, includeData: true);
            case "export":
                return HandleFlashbackExportAsync(context);
            case "segments":
                return HandleSimpleCommandAsync(context, "FlashbackGetSegments", includeData: true);
            default:
                throw new UsageException($"Unknown flashback command '{subcommand}'. Expected on, off, play, pause, go-live, seek, begin-scrub, update-scrub, end-scrub, set-in, set-out, clear-range, export, or segments.");
        }
    }

}
