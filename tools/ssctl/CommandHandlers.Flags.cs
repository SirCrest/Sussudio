namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static bool ConsumeFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        args.RemoveAt(index);
        return true;
    }

    private static int? ParseOptionalIntFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Count)
        {
            throw new UsageException($"Missing value for {flag}.");
        }

        var value = ParseInt(args[index + 1]);
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }

    private static string? ParseOptionalStringFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Count)
        {
            throw new UsageException($"Missing value for {flag}.");
        }

        var value = args[index + 1];
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }

    private static long? ParseOptionalLongFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Count)
        {
            throw new UsageException($"Missing value for {flag}.");
        }

        var value = ParseLong(args[index + 1]);
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }
}
