using System.Globalization;
using Sussudio.Models;
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

    private static void WriteHelp()
    {
        Console.WriteLine("ssctl");
        Console.WriteLine("Usage:");
        Console.WriteLine("  ssctl [--json] [--pipe NAME] [--timeout MS] <command>");
        Console.WriteLine();
        Console.WriteLine("Query:");
        WriteCatalogHelpLine(AutomationCommandKind.GetSnapshot, "[--json]");
        WriteCatalogHelpLine(AutomationCommandKind.GetDiagnostics, "[--json]");
        WriteCatalogHelpLine(AutomationCommandKind.GetCaptureOptions, "[--json]");
        WriteCatalogHelpLine(AutomationCommandKind.GetAutomationManifest, "[--json]");
        WriteCatalogHelpLine(AutomationCommandKind.GetPerformanceTimeline, "[--json]");
        Console.WriteLine("  memory [--json]");
        WriteCatalogHelpLine(AutomationCommandKind.GetAudioRampTrace, "[--json]");
        Console.WriteLine("  presentmon [--seconds N] [--pid PID|--process NAME] [--swapchain HEX] [--app-present-id N] [--app-source-seq N] [--app-present-utc-ms N] [--capture-start-utc-ms N] [--presentmon PATH] [--output PATH] [--keep-csv] [--json]");
        Console.WriteLine($"  diagnostic-session [--scenario {DiagnosticSessionScenarios.HelpList}] [--seconds N] [--sample-ms N] [--output PATH] [--presentmon] [--presentmon-path PATH] [--verify] [--leave-running] [--json]");
        Console.WriteLine();
        Console.WriteLine("Control:");
        WriteCatalogHelpLine(AutomationCommandKind.SetPreviewEnabled);
        WriteCatalogHelpLine(AutomationCommandKind.SetRecordingEnabled);
        WriteCatalogHelpLine(AutomationCommandKind.CaptureWindowScreenshot);
        WriteCatalogHelpLine(AutomationCommandKind.CapturePreviewFrame);
        WriteCatalogHelpLine(AutomationCommandKind.OpenRecordingsFolder);
        Console.WriteLine();
        Console.WriteLine("Configure:");
        WriteCatalogHelpLine(AutomationCommandKind.SetResolution);
        WriteCatalogHelpLine(AutomationCommandKind.SetFrameRate);
        WriteCatalogHelpLine(AutomationCommandKind.SetRecordingFormat);
        WriteCatalogHelpLine(AutomationCommandKind.SetQuality);
        WriteCatalogHelpLine(AutomationCommandKind.SetCustomBitrate);
        WriteCatalogHelpLine(AutomationCommandKind.SetPreset);
        WriteCatalogHelpLine(AutomationCommandKind.SetSplitEncodeMode);
        WriteCatalogHelpLine(AutomationCommandKind.SetVideoFormat);
        WriteCatalogHelpLine(AutomationCommandKind.SetMjpegDecoderCount);
        WriteCatalogHelpLine(AutomationCommandKind.SetHdrEnabled);
        WriteCatalogHelpLine(AutomationCommandKind.SetTrueHdrPreviewEnabled);
        WriteCatalogHelpLine(AutomationCommandKind.SetAudioEnabled);
        WriteCatalogHelpLine(AutomationCommandKind.SetAudioPreviewEnabled);
        WriteCatalogHelpLine(AutomationCommandKind.SetPreviewVolume);
        WriteCatalogHelpLine(AutomationCommandKind.SetDeviceAudioMode);
        WriteCatalogHelpLine(AutomationCommandKind.SetAnalogAudioGain);
        WriteCatalogHelpLine(AutomationCommandKind.SetOutputPath);
        WriteCatalogHelpLine(AutomationCommandKind.SetShowAllCaptureOptions);
        WriteCatalogHelpLine(AutomationCommandKind.SetMicrophoneEnabled);
        Console.WriteLine();
        Console.WriteLine("Device:");
        WriteCatalogHelpLine(AutomationCommandKind.RefreshDevices);
        Console.WriteLine("  device list");
        WriteCatalogHelpLine(AutomationCommandKind.SelectDevice);
        WriteCatalogHelpLine(AutomationCommandKind.SelectAudioInputDevice);
        WriteCatalogHelpLine(AutomationCommandKind.SetCustomAudioInput);
        Console.WriteLine();
        Console.WriteLine("Flashback:");
        WriteCatalogHelpLine(AutomationCommandKind.SetFlashbackEnabled);
        WriteCatalogHelpLine(AutomationCommandKind.SetFlashbackTimelineVisible);
        Console.WriteLine("  flashback play [<ms>]");
        Console.WriteLine("  flashback pause");
        Console.WriteLine("  flashback go-live");
        Console.WriteLine("  flashback seek <ms>");
        Console.WriteLine("  flashback begin-scrub <ms>");
        Console.WriteLine("  flashback update-scrub <ms>");
        Console.WriteLine("  flashback end-scrub [<ms>]");
        Console.WriteLine("  flashback set-in|set-out|clear-range");
        WriteCatalogHelpLine(AutomationCommandKind.FlashbackExport);
        WriteCatalogHelpLine(AutomationCommandKind.FlashbackGetSegments);
        WriteCatalogHelpLine(AutomationCommandKind.RestartFlashback);
        Console.WriteLine();
        Console.WriteLine("Window:");
        Console.WriteLine("  window close|minimize|maximize|restore|center");
        WriteCatalogHelpLine(AutomationCommandKind.SetFullScreenEnabled);
        Console.WriteLine("  window snap left|right|top-left|top-right|bottom-left|bottom-right");
        Console.WriteLine("  window move <x> <y>");
        Console.WriteLine("  window resize <w> <h>");
        Console.WriteLine();
        Console.WriteLine("Wait / Verify:");
        WriteCatalogHelpLine(AutomationCommandKind.WaitForCondition, "[--timeout MS] [--poll MS]");
        Console.WriteLine("  verify [path] [--profile NAME|--verification-profile NAME]");
        Console.WriteLine("  assert <json>|<field> <op> <value>");
        Console.WriteLine("  probe source|color");
        WriteCatalogHelpLine(AutomationCommandKind.SetStatsVisible);
        WriteCatalogHelpLine(AutomationCommandKind.SetStatsSectionVisible);
        WriteCatalogHelpLine(AutomationCommandKind.SetFrameTimeOverlayVisible);
        WriteCatalogHelpLine(AutomationCommandKind.SetSettingsVisible);
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine("  --json            Print raw JSON responses where supported");
        Console.WriteLine("  --pipe NAME       Named pipe (default: SussudioAutomation)");
        Console.WriteLine("  --timeout MS      Response timeout override for pipe calls");
        Console.WriteLine("  --verbose         On error, print full stack trace + InnerException chain to stderr");
        Console.WriteLine("  --help            Show this help");
    }

    private static void WriteCatalogHelpLine(AutomationCommandKind kind, string? suffix = null)
    {
        var command = AutomationCommandCatalog.Get(kind).CliHelp;
        Console.WriteLine(string.IsNullOrWhiteSpace(suffix)
            ? $"  {command}"
            : $"  {command} {suffix}");
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
