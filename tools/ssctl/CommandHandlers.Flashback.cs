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

    private static Task<int> HandleFlashbackExportAsync(CommandContext context)
    {
        var useSelectionRange = ConsumeFlag(context.Rest, "--range");
        var force = ConsumeFlag(context.Rest, "--force");
        var seconds = context.Rest.Count >= 2
            ? ParseFlashbackExportSeconds(context.Rest[1])
            : 300;
        var outputPath = context.Rest.Count >= 3
            ? JoinRemaining(context.Rest, 2)
            : $"temp/flashback_export_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackExport,
            new Dictionary<string, object?>
            {
                ["seconds"] = seconds,
                ["outputPath"] = outputPath,
                ["useSelectionRange"] = useSelectionRange,
                ["force"] = force
            }, includeData: true);
    }

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
