using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static string ReadSsctlCommandHandlersFamilyText()
    {
        var files = new[]
        {
            "tools/ssctl/CommandHandlers.cs",
            "tools/ssctl/CommandHandlers.AutomationFlow.cs",
            "tools/ssctl/CommandHandlers.CaptureControls.cs",
            "tools/ssctl/CommandHandlers.Context.cs",
            "tools/ssctl/CommandHandlers.DeviceWindow.cs",
            "tools/ssctl/CommandHandlers.Flashback.cs",
            "tools/ssctl/CommandHandlers.Observability.cs",
            "tools/ssctl/CommandHandlers.Parsing.cs",
            "tools/ssctl/CommandHandlers.Transport.cs"
        };

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }

    private static async Task SsctlCommandHandlers_RouteCoreCommandGroups()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssembly(assemblyPath);
        var transportType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.PipeTransport")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport type not found.");
        var commandHandlersType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.CommandHandlers")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.CommandHandlers type not found.");
        var executeAsync = commandHandlersType.GetMethod("ExecuteAsync", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.CommandHandlers.ExecuteAsync not found.");

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

        var deviceRefreshPipeName = $"ssctl-device-refresh-{Guid.NewGuid():N}";
        var deviceRefreshTransport = Activator.CreateInstance(transportType, deviceRefreshPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for device refresh command test.");
        var deviceRefreshArguments = new List<string> { "device", "refresh" };
        var deviceRefreshExitCode = -1;
        JsonElement deviceRefreshRequest = await CapturePipeRequestAsync(
            deviceRefreshPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { deviceRefreshTransport, deviceRefreshArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                deviceRefreshExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, deviceRefreshExitCode, "device refresh exit code");
        AssertEqual(3, deviceRefreshRequest.GetProperty("command").GetInt32(), "device refresh command id");

        var fullscreenPipeName = $"ssctl-fullscreen-{Guid.NewGuid():N}";
        var fullscreenTransport = Activator.CreateInstance(transportType, fullscreenPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for fullscreen command test.");
        var fullscreenArguments = new List<string> { "window", "fullscreen", "on" };
        var fullscreenExitCode = -1;
        JsonElement fullscreenRequest = await CapturePipeRequestAsync(
            fullscreenPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { fullscreenTransport, fullscreenArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                fullscreenExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, fullscreenExitCode, "window fullscreen exit code");
        AssertEqual(52, fullscreenRequest.GetProperty("command").GetInt32(), "window fullscreen command id");
        AssertEqual(true, fullscreenRequest.GetProperty("payload").GetProperty("enabled").GetBoolean(), "window fullscreen payload enabled");

        var recordingsPipeName = $"ssctl-recordings-open-{Guid.NewGuid():N}";
        var recordingsTransport = Activator.CreateInstance(transportType, recordingsPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for recordings command test.");
        var recordingsArguments = new List<string> { "recordings", "open" };
        var recordingsExitCode = -1;
        JsonElement recordingsRequest = await CapturePipeRequestAsync(
            recordingsPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { recordingsTransport, recordingsArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                recordingsExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, recordingsExitCode, "recordings open exit code");
        AssertEqual(53, recordingsRequest.GetProperty("command").GetInt32(), "recordings open command id");

        var manifestPipeName = $"ssctl-manifest-{Guid.NewGuid():N}";
        var manifestTransport = Activator.CreateInstance(transportType, manifestPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for manifest command test.");
        var manifestArguments = new List<string> { "manifest" };
        var manifestExitCode = -1;
        JsonElement manifestRequest = await CapturePipeRequestAsync(
            manifestPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { manifestTransport, manifestArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                manifestExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, manifestExitCode, "manifest exit code");
        AssertEqual(51, manifestRequest.GetProperty("command").GetInt32(), "manifest command id");

        var verifyPipeName = $"ssctl-verify-profile-{Guid.NewGuid():N}";
        var verifyTransport = Activator.CreateInstance(transportType, verifyPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for verify command test.");
        var verifyArguments = new List<string> { "verify", @"C:\captures\clip.mp4", "--profile", "flashback-export" };
        var verifyExitCode = -1;
        JsonElement verifyRequest = await CapturePipeRequestAsync(
            verifyPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { verifyTransport, verifyArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                verifyExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, verifyExitCode, "verify profile exit code");
        AssertEqual(44, verifyRequest.GetProperty("command").GetInt32(), "verify file command id");
        AssertEqual(@"C:\captures\clip.mp4", verifyRequest.GetProperty("payload").GetProperty("filePath").GetString(), "verify file payload path");
        AssertEqual("flashback-export", verifyRequest.GetProperty("payload").GetProperty("verificationProfile").GetString(), "verify file payload profile");

        var audioRampPipeName = $"ssctl-audio-ramp-trace-{Guid.NewGuid():N}";
        var audioRampTransport = Activator.CreateInstance(transportType, audioRampPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for audio ramp trace command test.");
        var audioRampArguments = new List<string> { "audio-ramp-trace" };
        var audioRampExitCode = -1;
        JsonElement audioRampRequest = await CapturePipeRequestAsync(
            audioRampPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { audioRampTransport, audioRampArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                audioRampExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, audioRampExitCode, "audio-ramp-trace exit code");
        AssertEqual(48, audioRampRequest.GetProperty("command").GetInt32(), "audio-ramp-trace command id");

        var assertPipeName = $"ssctl-assert-simple-{Guid.NewGuid():N}";
        var assertTransport = Activator.CreateInstance(transportType, assertPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for assert command test.");
        var assertArguments = new List<string> { "assert", "IsRecording", "eq", "false" };
        var assertExitCode = -1;
        JsonElement assertRequest = await CapturePipeRequestAsync(
            assertPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { assertTransport, assertArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                assertExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, assertExitCode, "assert simple exit code");
        AssertEqual(22, assertRequest.GetProperty("command").GetInt32(), "assert simple command id");
        var assertPayload = assertRequest.GetProperty("payload").GetProperty("assertions")[0];
        AssertEqual("IsRecording", assertPayload.GetProperty("field").GetString(), "assert simple field");
        AssertEqual("eq", assertPayload.GetProperty("op").GetString(), "assert simple op");
        AssertEqual(false, assertPayload.GetProperty("value").GetBoolean(), "assert simple value");

        var commandHandlersSource = ReadSsctlCommandHandlersFamilyText();
        var commandHandlersRootSource = ReadRepoFile("tools/ssctl/CommandHandlers.cs");
        AssertDoesNotContain(commandHandlersRootSource, "private static Task<int> HandleFlashbackAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs"), "HandleDiagnosticSessionAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.CaptureControls.cs"), "HandleSetAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.DeviceWindow.cs"), "HandleWindowAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleAssertAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs"), "HandleFlashbackAsync");
        AssertContains(commandHandlersSource, "\"manifest\" => HandleManifestAsync(context)");
        AssertContains(commandHandlersSource, "\"audio-ramp-trace\" => HandleAudioRampTraceAsync(context)");
        AssertContains(commandHandlersSource, "\"recordings\" => HandleRecordingsAsync(context)");
        AssertContains(commandHandlersSource, "context.Transport.SendCommandAsync(\"GetAutomationManifest\")");
        AssertContains(commandHandlersSource, "context.Transport.SendCommandAsync(\"GetAudioRampTrace\")");
        AssertContains(commandHandlersSource, "\"RefreshDevices\",");
        AssertContains(commandHandlersSource, "\"SetFullScreenEnabled\",");
        AssertContains(commandHandlersSource, "\"OpenRecordingsFolder\"");
        AssertContains(commandHandlersSource, "playPayload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersSource, "return HandleSimpleCommandAsync(context, \"FlashbackAction\", playPayload, includeData: true);");
        AssertContains(commandHandlersSource, "ParseOptionalStringFlag(context.Rest, \"--profile\")");
        AssertContains(commandHandlersSource, "payload[\"verificationProfile\"] = verificationProfile;");
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
        AssertContains(commandHandlersSource, "assert <json> OR assert <field> <op> <value>");
        AssertContains(commandHandlersSource, "private static object? ParseAssertionValue(string value)");
    }

    private static Task SsctlHelp_FlashbackExportIncludesForceFlag()
    {
        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        var flashbackHandlersText = ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs")
            .Replace("\r\n", "\n");

        AssertContains(catalogText, "\"flashback export [seconds] [path] [--range] [--force]\"");
        AssertContains(flashbackHandlersText, "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(ssctlProgramText, "flashback export [seconds] [path] [--range] [--force]");

        return Task.CompletedTask;
    }
}
