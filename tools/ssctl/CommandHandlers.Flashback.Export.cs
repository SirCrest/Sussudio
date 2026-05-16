namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
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
}
