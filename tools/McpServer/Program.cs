using McpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sussudio.Models;
using Sussudio.Tools;
using System.Text.Json;

// The host ignores missing option values, so reject an incomplete explicit credential.
for (var index = 0; index < args.Length; index++)
{
    var argument = args[index];
    if (argument.Contains('=') ||
        !(argument.StartsWith("--", StringComparison.Ordinal) || argument.StartsWith("/", StringComparison.Ordinal)))
        continue;

    if (argument.Equals("--token", StringComparison.OrdinalIgnoreCase) && index + 1 == args.Length)
    {
        Console.Error.WriteLine($"Missing value for {argument}.");
        Environment.ExitCode = 2;
        return;
    }

    // Preserve the host's pairing even when an option's value resembles another option.
    index++;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton(_ => new PipeClient(pipeName: null, authToken: builder.Configuration["token"]));
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync().ConfigureAwait(false);

namespace McpServer
{
    // MCP-side adapter over the shared automation pipe client. Tool handlers call
    // this instead of handling pipe connection and synthetic error shaping directly.
    public sealed class PipeClient
    {
        private readonly string _pipeName;
        private readonly string? _authToken;

        public PipeClient()
            : this(null)
        {
        }

        internal PipeClient(string? pipeName)
            : this(pipeName, authToken: null)
        {
        }

        internal PipeClient(string? pipeName, string? authToken)
        {
            var configuredPipeName = string.IsNullOrWhiteSpace(pipeName)
                ? Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE")
                : pipeName;
            _pipeName = string.IsNullOrWhiteSpace(configuredPipeName)
                ? AutomationPipeProtocol.DefaultPipeName
                : configuredPipeName;
            _authToken = authToken;
        }

        public async Task<JsonElement> SendCommandAsync(
            string commandName,
            Dictionary<string, object?>? payload = null,
            int? responseTimeoutMs = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await AutomationCommandTransport.SendCommandAsync(
                _pipeName,
                commandName,
                payload,
                callResponseTimeoutMs: responseTimeoutMs,
                unknownCommandHandling: AutomationUnknownCommandHandling.ReturnSyntheticError,
                authToken: _authToken,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            // Shared tools retain synthetic pipe-canceled responses; MCP requests
            // must remain cancelled after the transport has released its pipe.
            cancellationToken.ThrowIfCancellationRequested();
            return response;
        }

        public async Task<JsonElement> SendCommandAsync(
            AutomationCommandKind kind,
            Dictionary<string, object?>? payload = null,
            int? responseTimeoutMs = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await AutomationCommandTransport.SendCommandAsync(
                _pipeName,
                kind,
                payload,
                callResponseTimeoutMs: responseTimeoutMs,
                unknownCommandHandling: AutomationUnknownCommandHandling.ReturnSyntheticError,
                authToken: _authToken,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return response;
        }
    }
}
