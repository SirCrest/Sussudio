using System.Text.Json;
using Sussudio.Tools;

// Generic automation-pipe client used by scripts and ad hoc debugging. ssctl
// is the friendlier CLI; this tool stays close to the raw command/payload
// protocol for low-level contract tests.
internal static partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Operator Ctrl-C / CI SIGTERM must give the in-flight pipe call a
        // chance to print a cleanup hint instead of leaving the app in a
        // recording state. Handler is idempotent across repeated Ctrl-C.
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler? cancelHandler = null;
        cancelHandler = (_, e) =>
        {
            if (cts.IsCancellationRequested)
            {
                // Second Ctrl-C: let the runtime terminate so a hung pipe
                // call does not trap the operator forever.
                return;
            }

            e.Cancel = true;
            try
            {
                Console.Error.WriteLine("AutomationClient: Ctrl-C received; cancelling. If a recording was started, send SetRecordingEnabled enabled=false to clean up.");
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
        try
        {
            var options = ParseArgs(args);
            if (options.ShowHelp)
            {
                WriteHelp();
                return 0;
            }

            if (string.IsNullOrWhiteSpace(options.Command))
            {
                Console.Error.WriteLine("Missing required --command.");
                WriteHelp();
                return 2;
            }

            var commandValue = AutomationPipeProtocol.ResolveCommand(options.Command);
            var payload = BuildPayload(options);
            var timeoutCommandName = AutomationPipeProtocol.TryGetCommandName(commandValue, out var canonicalCommandName)
                ? canonicalCommandName
                : options.Command;
            var responseTimeoutMs = options.ResponseTimeoutMs ??
                AutomationPipeProtocol.GetDefaultResponseTimeout(timeoutCommandName);

            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    options.PipeName,
                    options.Command,
                    payload,
                    options.ConnectTimeoutMs,
                    responseTimeoutMs,
                    options.AuthToken)
                .ConfigureAwait(false);
            var responseLine = result.ResponseJson;

            if (options.Pretty)
            {
                using var responseDocument = JsonDocument.Parse(responseLine);
                var pretty = JsonSerializer.Serialize(responseDocument.RootElement, JsonOptions.Pretty);
                Console.WriteLine(pretty);
            }
            else
            {
                Console.WriteLine(responseLine);
            }

            try
            {
                if (result.StateRead && result.Success)
                {
                    return 0;
                }
            }
            catch
            {
                // Keep zero exit behavior only for valid JSON success payloads.
            }

            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private sealed class Options
    {
        public string? Command { get; set; }
        public string PipeName { get; set; } = AutomationPipeProtocol.DefaultPipeName;
        public string? AuthToken { get; set; }
        public string PayloadJson { get; set; } = "{}";
        public string PayloadBase64 { get; set; } = string.Empty;
        public List<string> PayloadKv { get; } = [];
        public int ConnectTimeoutMs { get; set; } = AutomationPipeProtocol.DefaultConnectTimeoutMs;
        public int? ResponseTimeoutMs { get; set; }
        public bool Pretty { get; set; }
        public bool ShowHelp { get; set; }
    }
}
