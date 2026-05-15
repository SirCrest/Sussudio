namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static Task<int> HandleRecordingsAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "recordings open").ToLowerInvariant();
        switch (subcommand)
        {
            case "open":
                EnsureArgCount(context.Rest, 1, "recordings open");
                return HandleSimpleCommandAsync(context, "OpenRecordingsFolder", includeData: false);
            default:
                throw new UsageException($"Unknown recordings command '{subcommand}'.");
        }
    }
}
