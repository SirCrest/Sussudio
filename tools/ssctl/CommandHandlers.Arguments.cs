namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static string RequireWord(IReadOnlyList<string> args, int index, string usage)
    {
        if (index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new UsageException(usage);
        }

        return args[index];
    }

    private static void EnsureArgCount(IReadOnlyList<string> args, int expected, string usage)
    {
        if (args.Count != expected)
        {
            throw new UsageException(usage);
        }
    }

    private static void EnsureNoArgs(IReadOnlyList<string> args, string usage)
    {
        if (args.Count != 0)
        {
            throw new UsageException(usage);
        }
    }

    private static string JoinRemaining(IReadOnlyList<string> args, int startIndex)
    {
        if (startIndex >= args.Count)
        {
            throw new UsageException("Missing required value.");
        }

        return JoinRange(args, startIndex, args.Count);
    }

    private static string JoinRange(IReadOnlyList<string> args, int startIndex, int endExclusive)
        => string.Join(" ", args.Skip(startIndex).Take(endExclusive - startIndex));
}
