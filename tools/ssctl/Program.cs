using System.Globalization;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

// Entry point for ssctl, the local CLI over Sussudio's automation pipe. It owns
// process-level argument parsing and exit codes; command behavior is delegated
// to CommandHandlers.
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Ensure operator Ctrl-C (or CI SIGTERM) gives the in-flight command a
        // chance to surface a cleanup hint instead of vanishing while the app
        // is still recording. The handler is idempotent: repeated Ctrl-C
        // collapses to a single cancellation request, and the second press
        // falls through to the runtime's default terminate behavior.
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler? cancelHandler = null;
        cancelHandler = (_, e) =>
        {
            if (cts.IsCancellationRequested)
            {
                // Second Ctrl-C: let the runtime terminate so a hung command
                // does not trap the operator forever.
                return;
            }

            e.Cancel = true;
            try
            {
                Console.Error.WriteLine("ssctl: Ctrl-C received; cancelling. If a recording was started, run 'ssctl record stop' to clean up.");
            }
            catch
            {
                // Console may already be torn down during shutdown.
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CTS already disposed; nothing more to do.
            }
        };
        Console.CancelKeyPress += cancelHandler;

        var verbose = args.Contains("--verbose");
        try
        {
            var options = CliOptions.Parse(args);
            if (options.ShowHelp)
            {
                SsctlHelpWriter.Write(Console.Out);
                return 0;
            }

            if (options.Arguments.Count == 0)
            {
                throw new UsageException("Missing command.");
            }

            var transport = new PipeTransport(options.PipeName, options.ResponseTimeoutMs);
            return await CommandHandlers.ExecuteAsync(
                transport,
                options.Arguments,
                options.Json).ConfigureAwait(false);
        }
        catch (UsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            SsctlHelpWriter.Write(Console.Out);
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(verbose ? ex.ToString() : FormatExceptionChain(ex));
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    // Without --verbose, emit the full inner-exception chain (not just the
    // outer message) so operators see why a transport call failed without
    // needing to re-run with --verbose to get the stack trace.
    private static string FormatExceptionChain(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        var current = ex;
        while (current != null)
        {
            if (sb.Length > 0)
            {
                sb.Append(" → ");
            }
            sb.Append(current.GetType().Name).Append(": ").Append(current.Message);
            current = current.InnerException;
        }
        return sb.ToString();
    }

    private sealed class CliOptions
    {
        public bool Json { get; private set; }
        public bool ShowHelp { get; private set; }
        public string PipeName { get; private set; } = AutomationPipeProtocol.DefaultPipeName;
        public int? ResponseTimeoutMs { get; private set; }
        public IReadOnlyList<string> Arguments { get; private set; } = Array.Empty<string>();

        public static CliOptions Parse(string[] args)
        {
            var options = new CliOptions();
            if (args.Any(IsHelpFlag))
            {
                options.ShowHelp = true;
                return options;
            }

            var remaining = new List<string>();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--json":
                        options.Json = true;
                        continue;
                    case "--verbose":
                        // Handled directly in Main so it survives Parse() throwing.
                        continue;
                    case "--pipe":
                        options.PipeName = NextValue(args, ref i, arg);
                        continue;
                    case "--timeout":
                        options.ResponseTimeoutMs = ParsePositiveInt(NextValue(args, ref i, arg), arg);
                        continue;
                    default:
                        remaining.AddRange(args[i..]);
                        options.Arguments = remaining;
                        return options;
                }
            }

            options.Arguments = remaining;
            return options;
        }

        private static bool IsHelpFlag(string value)
            => value is "--help" or "-h" or "/?";
    }

    private static string NextValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new UsageException($"Missing value for {option}.");
        }

        index++;
        return args[index];
    }

    private static int ParsePositiveInt(string value, string option)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new UsageException($"Invalid value '{value}' for {option}.");
        }

        return parsed;
    }
}

internal sealed class UsageException : Exception
{
    public UsageException(string message)
        : base(message)
    {
    }
}
