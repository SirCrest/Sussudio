using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task EcctlCommandHandlers_RouteCoreCommandGroups()
    {
        var assemblyPath = Path.Combine("tools", "ecctl", "bin", "Debug", "net8.0", "ecctl.dll");
        var ecctlAssembly = LoadToolAssembly(assemblyPath);
        var transportType = ecctlAssembly.GetType("EcCtl.PipeTransport")
            ?? throw new InvalidOperationException("EcCtl.PipeTransport type not found.");
        var commandHandlersType = ecctlAssembly.GetType("EcCtl.CommandHandlers")
            ?? throw new InvalidOperationException("EcCtl.CommandHandlers type not found.");
        var executeAsync = commandHandlersType.GetMethod("ExecuteAsync", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("EcCtl.CommandHandlers.ExecuteAsync not found.");

        var devicePipeName = $"ecctl-device-audio-{Guid.NewGuid():N}";
        var deviceTransport = Activator.CreateInstance(transportType, devicePipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for device command test.");
        var deviceArguments = new List<string> { "device", "audio-select", "Synthetic Mic" };
        var deviceExitCode = -1;
        JsonElement deviceRequest = await CapturePipeRequestAsync(
            devicePipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { deviceTransport, deviceArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                deviceExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, deviceExitCode, "device audio-select exit code");
        AssertEqual(5, deviceRequest.GetProperty("command").GetInt32(), "device audio-select command id");
        // Auth token is null when not configured via env var
        AssertEqual("Synthetic Mic", deviceRequest.GetProperty("payload").GetProperty("audioDeviceName").GetString(), "device audio-select payload key");

        var previewPipeName = $"ecctl-preview-{Guid.NewGuid():N}";
        var previewTransport = Activator.CreateInstance(transportType, previewPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for preview command test.");
        var previewArguments = new List<string> { "preview", "start" };
        var previewExitCode = -1;
        JsonElement previewRequest = await CapturePipeRequestAsync(
            previewPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { previewTransport, previewArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                previewExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, previewExitCode, "preview start exit code");
        AssertEqual(16, previewRequest.GetProperty("command").GetInt32(), "preview start command id");
        AssertEqual(true, previewRequest.GetProperty("payload").GetProperty("enabled").GetBoolean(), "preview start payload enabled");

        var flashbackPipeName = $"ecctl-flashback-{Guid.NewGuid():N}";
        var flashbackTransport = Activator.CreateInstance(transportType, flashbackPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for flashback command test.");
        var flashbackArguments = new List<string> { "flashback", "off" };
        var flashbackExitCode = -1;
        JsonElement flashbackRequest = await CapturePipeRequestAsync(
            flashbackPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { flashbackTransport, flashbackArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                flashbackExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, flashbackExitCode, "flashback off exit code");
        AssertEqual(47, flashbackRequest.GetProperty("command").GetInt32(), "flashback off command id");
        AssertEqual(false, flashbackRequest.GetProperty("payload").GetProperty("enabled").GetBoolean(), "flashback off payload enabled");
    }
}
