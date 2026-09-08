using McpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Sussudio.Models;
using Sussudio.Tools;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<PipeClient>();
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

        public PipeClient()
            : this(null)
        {
        }

        internal PipeClient(string? pipeName)
        {
            var configuredPipeName = string.IsNullOrWhiteSpace(pipeName)
                ? Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE")
                : pipeName;
            _pipeName = string.IsNullOrWhiteSpace(configuredPipeName)
                ? AutomationPipeProtocol.DefaultPipeName
                : configuredPipeName;
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
                responseTimeoutMs: responseTimeoutMs,
                unknownCommandHandling: AutomationUnknownCommandHandling.ReturnSyntheticError,
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
                responseTimeoutMs: responseTimeoutMs,
                unknownCommandHandling: AutomationUnknownCommandHandling.ReturnSyntheticError,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return response;
        }
    }
}
