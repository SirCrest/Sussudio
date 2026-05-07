using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteCoreCommandGroups()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssembly(assemblyPath);
        var transportType = ssctlAssembly.GetType("EcCtl.PipeTransport")
            ?? throw new InvalidOperationException("EcCtl.PipeTransport type not found.");
        var commandHandlersType = ssctlAssembly.GetType("EcCtl.CommandHandlers")
            ?? throw new InvalidOperationException("EcCtl.CommandHandlers type not found.");
        var executeAsync = commandHandlersType.GetMethod("ExecuteAsync", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("EcCtl.CommandHandlers.ExecuteAsync not found.");

        var devicePipeName = $"ssctl-device-audio-{Guid.NewGuid():N}";
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
        AssertEqual("Synthetic Mic", deviceRequest.GetProperty("payload").GetProperty("deviceName").GetString(), "device audio-select payload key");

        var previewPipeName = $"ssctl-preview-{Guid.NewGuid():N}";
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

        var flashbackPipeName = $"ssctl-flashback-{Guid.NewGuid():N}";
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

        var commandHandlersSource = ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n");
        AssertContains(commandHandlersSource, "playPayload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersSource, "return HandleSimpleCommandAsync(context, \"FlashbackAction\", playPayload, includeData: true);");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback seek <ms>\"))");
        AssertContains(commandHandlersSource, "[\"action\"] = \"begin-scrub\"");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback begin-scrub <ms>\"))");
        AssertContains(commandHandlersSource, "[\"action\"] = \"update-scrub\"");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback update-scrub <ms>\"))");
        AssertContains(commandHandlersSource, "var payload = new Dictionary<string, object?> { [\"action\"] = \"end-scrub\" };");
        AssertContains(commandHandlersSource, "payload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersSource, "? ParseFlashbackExportSeconds(context.Rest[1])");
        AssertContains(commandHandlersSource, "private static double ParseFlashbackPositionMs(string value)");
        AssertContains(commandHandlersSource, "Flashback position must be finite, non-negative, and within TimeSpan range.");
        AssertContains(commandHandlersSource, "private static double ParseFlashbackExportSeconds(string value)");
        AssertContains(commandHandlersSource, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
    }
}
