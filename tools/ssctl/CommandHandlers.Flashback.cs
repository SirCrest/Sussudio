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
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = true }, includeData: false);
            case "off":
            case "disable":
                EnsureArgCount(context.Rest, 1, "flashback on|off");
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false }, includeData: false);
            case "apply":
            case "restart":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.RestartFlashback, includeData: false);
            case "play":
            case "pause":
            case "go-live":
            case "seek":
            case "begin-scrub":
            case "update-scrub":
            case "end-scrub":
            case "set-in":
            case "set-in-point":
            case "set-out":
            case "set-out-point":
            case "clear-range":
            case "clear-in-out":
                return HandleFlashbackActionAsync(context, subcommand);
            case "export":
                return HandleFlashbackExportAsync(context);
            case "segments":
                return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackGetSegments, includeData: true);
            default:
                throw new UsageException($"Unknown flashback command '{subcommand}'. Expected on, off, play, pause, go-live, seek, begin-scrub, update-scrub, end-scrub, set-in, set-out, clear-range, export, or segments.");
        }
    }

}
