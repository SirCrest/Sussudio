using System.Globalization;
using ElgatoCapture.Tools;

namespace EcCtl;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);
            if (options.ShowHelp)
            {
                WriteHelp();
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
            WriteHelp();
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void WriteHelp()
    {
        Console.WriteLine("ecctl");
        Console.WriteLine("Usage:");
        Console.WriteLine("  ecctl [--json] [--pipe NAME] [--timeout MS] <command>");
        Console.WriteLine();
        Console.WriteLine("Query:");
        Console.WriteLine("  state [--json]");
        Console.WriteLine("  diagnostics [--max N] [--json]");
        Console.WriteLine("  options [--json]");
        Console.WriteLine("  timeline [--max N] [--json]");
        Console.WriteLine("  memory [--json]");
        Console.WriteLine("  presentmon [--seconds N] [--pid PID|--process NAME] [--swapchain HEX] [--app-present-id N] [--app-source-seq N] [--app-present-utc-ms N] [--capture-start-utc-ms N] [--presentmon PATH] [--output PATH] [--keep-csv] [--json]");
        Console.WriteLine("  diagnostic-session [--scenario observe|preview-only|recording-only|flashback|flashback-stress|flashback-recording|combined] [--seconds N] [--sample-ms N] [--output PATH] [--presentmon] [--presentmon-path PATH] [--verify] [--leave-running] [--json]");
        Console.WriteLine();
        Console.WriteLine("Control:");
        Console.WriteLine("  preview start|stop");
        Console.WriteLine("  record start|stop");
        Console.WriteLine("  screenshot [path]");
        Console.WriteLine("  frame [path]");
        Console.WriteLine();
        Console.WriteLine("Configure:");
        Console.WriteLine("  set resolution <value>");
        Console.WriteLine("  set fps <value>");
        Console.WriteLine("  set format <value>");
        Console.WriteLine("  set quality <value>");
        Console.WriteLine("  set bitrate <value>");
        Console.WriteLine("  set preset <value>");
        Console.WriteLine("  set split <value>");
        Console.WriteLine("  set video-format <value>");
        Console.WriteLine("  set decoders <value>");
        Console.WriteLine("  set hdr on|off");
        Console.WriteLine("  set hdr-preview on|off");
        Console.WriteLine("  set audio on|off");
        Console.WriteLine("  set audio-preview on|off");
        Console.WriteLine("  set volume <value>");
        Console.WriteLine("  set audio-mode hdmi|analog");
        Console.WriteLine("  set gain <value>");
        Console.WriteLine("  set output <path>");
        Console.WriteLine("  set show-all on|off");
        Console.WriteLine("  set mic on|off");
        Console.WriteLine();
        Console.WriteLine("Device:");
        Console.WriteLine("  device list");
        Console.WriteLine("  device select <name>");
        Console.WriteLine("  device audio-select <name>");
        Console.WriteLine("  device custom-audio on|off");
        Console.WriteLine();
        Console.WriteLine("Flashback:");
        Console.WriteLine("  flashback on|off");
        Console.WriteLine("  flashback play [<ms>]");
        Console.WriteLine("  flashback pause");
        Console.WriteLine("  flashback go-live");
        Console.WriteLine("  flashback seek <ms>");
        Console.WriteLine("  flashback apply");
        Console.WriteLine();
        Console.WriteLine("Window:");
        Console.WriteLine("  window close|minimize|maximize|restore|center");
        Console.WriteLine("  window snap left|right|top-left|top-right|bottom-left|bottom-right");
        Console.WriteLine("  window move <x> <y>");
        Console.WriteLine("  window resize <w> <h>");
        Console.WriteLine();
        Console.WriteLine("Wait / Verify:");
        Console.WriteLine("  wait <condition> [--timeout MS] [--poll MS]");
        Console.WriteLine("  verify");
        Console.WriteLine("  assert <json>");
        Console.WriteLine("  probe source|color");
        Console.WriteLine("  stats show|hide");
        Console.WriteLine("  stats section <name> show|hide");
        Console.WriteLine("  settings show|hide");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine("  --json            Print raw JSON responses where supported");
        Console.WriteLine("  --pipe NAME       Named pipe (default: ElgatoCaptureAutomation)");
        Console.WriteLine("  --timeout MS      Response timeout override for pipe calls");
        Console.WriteLine("  --help            Show this help");
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
