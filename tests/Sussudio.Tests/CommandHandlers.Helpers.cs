using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private readonly record struct SsctlCommandRoutingContext(Type TransportType, MethodInfo ExecuteAsync);

    private static SsctlCommandRoutingContext CreateSsctlCommandRoutingContext()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssembly(assemblyPath);
        var transportType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.PipeTransport")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport type not found.");
        var commandHandlersType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.CommandHandlers")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.CommandHandlers type not found.");
        var executeAsync = commandHandlersType.GetMethod("ExecuteAsync", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.CommandHandlers.ExecuteAsync not found.");

        return new SsctlCommandRoutingContext(transportType, executeAsync);
    }

    private static object CreateSsctlTransport(SsctlCommandRoutingContext context, string pipeName)
        => Activator.CreateInstance(context.TransportType, pipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for ssctl command-handler routing test.");

    private static async Task<(int ExitCode, JsonElement Request)> CaptureSsctlRequestAsync(
        SsctlCommandRoutingContext context,
        string pipeName,
        List<string> arguments)
    {
        var transport = CreateSsctlTransport(context, pipeName);
        var exitCode = -1;
        JsonElement request = await CapturePipeRequestAsync(
                pipeName,
                async () =>
                {
                    var task = context.ExecuteAsync.Invoke(null, new object?[] { transport, arguments, false }) as Task<int>
                        ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                    exitCode = await task.ConfigureAwait(false);
                })
            .ConfigureAwait(false);

        return (exitCode, request);
    }

    private static async Task<(int ExitCode, JsonElement[] Requests)> CaptureSsctlRequestsAsync(
        SsctlCommandRoutingContext context,
        string pipeName,
        int expectedCount,
        List<string> arguments,
        Func<int, string>? responseFactory = null)
    {
        var transport = CreateSsctlTransport(context, pipeName);
        var exitCode = -1;
        JsonElement[] requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount,
                async () =>
                {
                    var task = context.ExecuteAsync.Invoke(null, new object?[] { transport, arguments, false }) as Task<int>
                        ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                    exitCode = await task.ConfigureAwait(false);
                },
                responseFactory)
            .ConfigureAwait(false);

        return (exitCode, requests);
    }

    private static string ReadSsctlCommandHandlersFamilyText()
    {
        var files = new[]
        {
            "tools/ssctl/CommandHandlers.cs",
            "tools/ssctl/CommandHandlers.Arguments.cs",
            "tools/ssctl/CommandHandlers.AutomationFlow.cs",
            "tools/ssctl/CommandHandlers.CaptureControls.cs",
            "tools/ssctl/CommandHandlers.Context.cs",
            "tools/ssctl/CommandHandlers.Device.cs",
            "tools/ssctl/CommandHandlers.DiagnosticSession.cs",
            "tools/ssctl/CommandHandlers.Flashback.cs",
            "tools/ssctl/CommandHandlers.Flashback.Export.cs",
            "tools/ssctl/CommandHandlers.Flags.cs",
            "tools/ssctl/CommandHandlers.Json.cs",
            "tools/ssctl/CommandHandlers.Observability.cs",
            "tools/ssctl/CommandHandlers.PresentMon.cs",
            "tools/ssctl/CommandHandlers.Recordings.cs",
            "tools/ssctl/CommandHandlers.Transport.cs",
            "tools/ssctl/CommandHandlers.UiVisibility.cs",
            "tools/ssctl/CommandHandlers.Values.cs",
            "tools/ssctl/CommandHandlers.Verification.cs",
            "tools/ssctl/CommandHandlers.Window.cs",
        };

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }
}
