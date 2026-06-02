using System.Globalization;
using System.IO;
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
                options.Json,
                cts.Token).ConfigureAwait(false);
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

// Owns the operator-facing ssctl help text. Program decides when help is shown.
internal static class SsctlHelpWriter
{
    internal static void Write(TextWriter writer)
    {
        WriteHeader(writer);
        WriteQuerySection(writer);
        WriteControlSection(writer);
        WriteConfigureSection(writer);
        WriteDeviceSection(writer);
        WriteFlashbackSection(writer);
        WriteWindowSection(writer);
        WriteWaitVerifySection(writer);
        WriteFlagsSection(writer);
    }

    private static void WriteCatalogHelpLine(TextWriter writer, AutomationCommandKind kind, string? suffix = null)
    {
        var command = AutomationCommandCatalog.Get(kind).CliHelp;
        writer.WriteLine(string.IsNullOrWhiteSpace(suffix)
            ? $"  {command}"
            : $"  {command} {suffix}");
    }

    private static void WriteHeader(TextWriter writer)
    {
        writer.WriteLine("ssctl");
        writer.WriteLine("Usage:");
        writer.WriteLine("  ssctl [--json] [--pipe NAME] [--timeout MS] <command>");
        writer.WriteLine();
    }

    private static void WriteQuerySection(TextWriter writer)
    {
        writer.WriteLine("Query:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetSnapshot, "[--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetDiagnostics, "[--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetCaptureOptions, "[--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetAutomationManifest, "[--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetPerformanceTimeline, "[--json]");
        writer.WriteLine("  memory [--json]");
        WriteCatalogHelpLine(writer, AutomationCommandKind.GetAudioRampTrace, "[--json]");
        writer.WriteLine("  presentmon [--seconds N] [--pid PID|--process NAME] [--swapchain HEX] [--app-present-id N] [--app-source-seq N] [--app-present-utc-ms N] [--capture-start-utc-ms N] [--presentmon PATH] [--output PATH] [--keep-csv] [--json]");
        writer.WriteLine($"  {DiagnosticSessionOptions.CliUsage}");
        writer.WriteLine();
    }

    private static void WriteControlSection(TextWriter writer)
    {
        writer.WriteLine("Control:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetPreviewEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetRecordingEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.CaptureWindowScreenshot);
        WriteCatalogHelpLine(writer, AutomationCommandKind.CapturePreviewFrame);
        WriteCatalogHelpLine(writer, AutomationCommandKind.OpenRecordingsFolder);
        writer.WriteLine();
    }

    private static void WriteConfigureSection(TextWriter writer)
    {
        writer.WriteLine("Configure:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetResolution);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFrameRate);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetRecordingFormat);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetQuality);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetCustomBitrate);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetPreset);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetSplitEncodeMode);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetVideoFormat);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetMjpegDecoderCount);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetHdrEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetTrueHdrPreviewEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetAudioEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetAudioPreviewEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetPreviewVolume);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetDeviceAudioMode);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetAnalogAudioGain);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetOutputPath);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetShowAllCaptureOptions);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetMicrophoneEnabled);
        writer.WriteLine();
    }

    private static void WriteDeviceSection(TextWriter writer)
    {
        writer.WriteLine("Device:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.RefreshDevices);
        writer.WriteLine("  device list");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SelectDevice);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SelectAudioInputDevice);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetCustomAudioInput);
        writer.WriteLine();
    }

    private static void WriteFlashbackSection(TextWriter writer)
    {
        writer.WriteLine("Flashback:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFlashbackEnabled);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFlashbackTimelineVisible);
        writer.WriteLine("  flashback play [<ms>]");
        writer.WriteLine("  flashback pause");
        writer.WriteLine("  flashback go-live");
        writer.WriteLine("  flashback seek <ms>");
        writer.WriteLine("  flashback begin-scrub <ms>");
        writer.WriteLine("  flashback update-scrub <ms>");
        writer.WriteLine("  flashback end-scrub [<ms>]");
        writer.WriteLine("  flashback set-in|set-out|clear-range");
        WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackExport);
        WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackGetSegments);
        WriteCatalogHelpLine(writer, AutomationCommandKind.RestartFlashback);
        writer.WriteLine();
    }

    private static void WriteWindowSection(TextWriter writer)
    {
        writer.WriteLine("Window:");
        writer.WriteLine("  window close|minimize|maximize|restore|center");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFullScreenEnabled);
        writer.WriteLine("  window snap left|right|top-left|top-right|bottom-left|bottom-right");
        writer.WriteLine("  window move <x> <y>");
        writer.WriteLine("  window resize <w> <h>");
        writer.WriteLine();
    }

    private static void WriteWaitVerifySection(TextWriter writer)
    {
        writer.WriteLine("Wait / Verify:");
        WriteCatalogHelpLine(writer, AutomationCommandKind.WaitForCondition, "[--timeout MS] [--poll MS]");
        writer.WriteLine("  verify [path] [--profile NAME|--verification-profile NAME]");
        writer.WriteLine("  assert <json>|<field> <op> <value>");
        writer.WriteLine("  probe source|color");
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetStatsVisible);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetStatsSectionVisible);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetFrameTimeOverlayVisible);
        WriteCatalogHelpLine(writer, AutomationCommandKind.SetSettingsVisible);
        writer.WriteLine();
    }

    private static void WriteFlagsSection(TextWriter writer)
    {
        writer.WriteLine("Flags:");
        writer.WriteLine("  --json            Print raw JSON responses where supported");
        writer.WriteLine("  --pipe NAME       Named pipe (default: SussudioAutomation)");
        writer.WriteLine("  --timeout MS      Response timeout override for pipe calls");
        writer.WriteLine("  --verbose         On error, print full stack trace + InnerException chain to stderr");
        writer.WriteLine("  --help            Show this help");
    }
}

internal sealed class UsageException : Exception
{
    public UsageException(string message)
        : base(message)
    {
    }
}
