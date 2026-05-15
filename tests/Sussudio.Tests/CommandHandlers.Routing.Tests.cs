using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
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

        var flashbackExportPipeName = $"ssctl-flashback-export-{Guid.NewGuid():N}";
        var flashbackExportTransport = Activator.CreateInstance(transportType, flashbackExportPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for flashback export command test.");
        var flashbackExportOutputPath = Path.Combine("temp", "ssctl flashback export", "export with spaces.mp4");
        var flashbackExportArguments = new List<string> { "flashback", "export", "--range", "--force", "2.5", flashbackExportOutputPath };
        var flashbackExportExitCode = -1;
        JsonElement flashbackExportRequest = await CapturePipeRequestAsync(
            flashbackExportPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { flashbackExportTransport, flashbackExportArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                flashbackExportExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, flashbackExportExitCode, "flashback export exit code");
        AssertEqual(42, flashbackExportRequest.GetProperty("command").GetInt32(), "flashback export command id");
        var flashbackExportPayload = flashbackExportRequest.GetProperty("payload");
        AssertEqual(2.5d, flashbackExportPayload.GetProperty("seconds").GetDouble(), "flashback export payload seconds");
        AssertEqual(flashbackExportOutputPath, flashbackExportPayload.GetProperty("outputPath").GetString(), "flashback export payload path");
        AssertEqual(true, flashbackExportPayload.GetProperty("useSelectionRange").GetBoolean(), "flashback export payload range");
        AssertEqual(true, flashbackExportPayload.GetProperty("force").GetBoolean(), "flashback export payload force");
        AssertEqual(
            true,
            Directory.Exists(Path.GetDirectoryName(flashbackExportOutputPath) ?? "."),
            "flashback export parent directory created");

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

        var deviceListPipeName = $"ssctl-device-list-{Guid.NewGuid():N}";
        var deviceListTransport = Activator.CreateInstance(transportType, deviceListPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for device list command test.");
        var deviceListArguments = new List<string> { "device", "list" };
        var deviceListExitCode = -1;
        JsonElement[] deviceListRequests = await CapturePipeRequestsAsync(
            deviceListPipeName,
            expectedCount: 2,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { deviceListTransport, deviceListArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                deviceListExitCode = await task.ConfigureAwait(false);
            },
            i => i == 0
                ? "{\"Success\":true,\"Message\":\"refresh ok\"}"
                : "{\"Success\":true,\"Message\":\"options ok\",\"Data\":{\"Devices\":[],\"AudioInputDevices\":[]}}")
            .ConfigureAwait(false);

        AssertEqual(0, deviceListExitCode, "device list exit code");
        AssertEqual(3, deviceListRequests[0].GetProperty("command").GetInt32(), "device list refresh command id");
        AssertEqual(29, deviceListRequests[1].GetProperty("command").GetInt32(), "device list options command id");

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

        var windowClosePipeName = $"ssctl-window-close-{Guid.NewGuid():N}";
        var windowCloseTransport = Activator.CreateInstance(transportType, windowClosePipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for window close command test.");
        var windowCloseArguments = new List<string> { "window", "close" };
        var windowCloseExitCode = -1;
        JsonElement[] windowCloseRequests = await CapturePipeRequestsAsync(
            windowClosePipeName,
            expectedCount: 2,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { windowCloseTransport, windowCloseArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                windowCloseExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, windowCloseExitCode, "window close exit code");
        AssertEqual(18, windowCloseRequests[0].GetProperty("command").GetInt32(), "window close arm command id");
        AssertEqual(true, windowCloseRequests[0].GetProperty("payload").GetProperty("armed").GetBoolean(), "window close arm payload");
        AssertEqual(19, windowCloseRequests[1].GetProperty("command").GetInt32(), "window close action command id");
        AssertEqual("Close", windowCloseRequests[1].GetProperty("payload").GetProperty("action").GetString(), "window close action payload");

        var windowCloseDeniedPipeName = $"ssctl-window-close-denied-{Guid.NewGuid():N}";
        var windowCloseDeniedTransport = Activator.CreateInstance(transportType, windowCloseDeniedPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for window close denied command test.");
        var windowCloseDeniedArguments = new List<string> { "window", "close" };
        var windowCloseDeniedExitCode = -1;
        JsonElement[] windowCloseDeniedRequests = await CapturePipeRequestsAsync(
            windowCloseDeniedPipeName,
            expectedCount: 1,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { windowCloseDeniedTransport, windowCloseDeniedArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                windowCloseDeniedExitCode = await task.ConfigureAwait(false);
            },
            _ => "{\"Success\":false,\"Message\":\"arm denied\"}")
            .ConfigureAwait(false);

        AssertEqual(3, windowCloseDeniedExitCode, "window close denied exit code");
        AssertEqual(18, windowCloseDeniedRequests[0].GetProperty("command").GetInt32(), "window close denied arm command id");

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

        var verifyLastPipeName = $"ssctl-verify-last-{Guid.NewGuid():N}";
        var verifyLastTransport = Activator.CreateInstance(transportType, verifyLastPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for verify last command test.");
        var verifyLastArguments = new List<string> { "verify" };
        var verifyLastExitCode = -1;
        JsonElement verifyLastRequest = await CapturePipeRequestAsync(
            verifyLastPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { verifyLastTransport, verifyLastArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                verifyLastExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, verifyLastExitCode, "verify last exit code");
        AssertEqual(21, verifyLastRequest.GetProperty("command").GetInt32(), "verify last command id");

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

        var waitPipeName = $"ssctl-wait-{Guid.NewGuid():N}";
        var waitTransport = Activator.CreateInstance(transportType, waitPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for wait command test.");
        var waitArguments = new List<string> { "wait", "preview-ready", "--timeout", "12500", "--poll", "250" };
        var waitExitCode = -1;
        JsonElement waitRequest = await CapturePipeRequestAsync(
            waitPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { waitTransport, waitArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                waitExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, waitExitCode, "wait exit code");
        AssertEqual(20, waitRequest.GetProperty("command").GetInt32(), "wait command id");
        var waitPayload = waitRequest.GetProperty("payload");
        AssertEqual("preview-ready", waitPayload.GetProperty("condition").GetString(), "wait condition payload");
        AssertEqual(12500, waitPayload.GetProperty("timeoutMs").GetInt32(), "wait timeout payload");
        AssertEqual(250, waitPayload.GetProperty("pollMs").GetInt32(), "wait poll payload");

        var probePipeName = $"ssctl-probe-color-{Guid.NewGuid():N}";
        var probeTransport = Activator.CreateInstance(transportType, probePipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for probe command test.");
        var probeArguments = new List<string> { "probe", "color" };
        var probeExitCode = -1;
        JsonElement probeRequest = await CapturePipeRequestAsync(
            probePipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { probeTransport, probeArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                probeExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, probeExitCode, "probe color exit code");
        AssertEqual(25, probeRequest.GetProperty("command").GetInt32(), "probe color command id");

        var statsSectionPipeName = $"ssctl-stats-section-{Guid.NewGuid():N}";
        var statsSectionTransport = Activator.CreateInstance(transportType, statsSectionPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for stats section command test.");
        var statsSectionArguments = new List<string> { "stats", "section", "Preview Cadence", "hide" };
        var statsSectionExitCode = -1;
        JsonElement statsSectionRequest = await CapturePipeRequestAsync(
            statsSectionPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { statsSectionTransport, statsSectionArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                statsSectionExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, statsSectionExitCode, "stats section exit code");
        AssertEqual(38, statsSectionRequest.GetProperty("command").GetInt32(), "stats section command id");
        var statsSectionPayload = statsSectionRequest.GetProperty("payload");
        AssertEqual("Preview Cadence", statsSectionPayload.GetProperty("section").GetString(), "stats section name payload");
        AssertEqual(false, statsSectionPayload.GetProperty("visible").GetBoolean(), "stats section visible payload");

        var settingsPipeName = $"ssctl-settings-show-{Guid.NewGuid():N}";
        var settingsTransport = Activator.CreateInstance(transportType, settingsPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for settings command test.");
        var settingsArguments = new List<string> { "settings", "show" };
        var settingsExitCode = -1;
        JsonElement settingsRequest = await CapturePipeRequestAsync(
            settingsPipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { settingsTransport, settingsArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                settingsExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, settingsExitCode, "settings show exit code");
        AssertEqual(40, settingsRequest.GetProperty("command").GetInt32(), "settings show command id");
        AssertEqual(true, settingsRequest.GetProperty("payload").GetProperty("visible").GetBoolean(), "settings show visible payload");

        var frameTimePipeName = $"ssctl-frametime-hide-{Guid.NewGuid():N}";
        var frameTimeTransport = Activator.CreateInstance(transportType, frameTimePipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for frametime command test.");
        var frameTimeArguments = new List<string> { "frame-time", "hide" };
        var frameTimeExitCode = -1;
        JsonElement frameTimeRequest = await CapturePipeRequestAsync(
            frameTimePipeName,
            async () =>
            {
                var task = executeAsync.Invoke(null, new object?[] { frameTimeTransport, frameTimeArguments, false }) as Task<int>
                    ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                frameTimeExitCode = await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(0, frameTimeExitCode, "frametime hide exit code");
        AssertEqual(49, frameTimeRequest.GetProperty("command").GetInt32(), "frametime hide command id");
        AssertEqual(false, frameTimeRequest.GetProperty("payload").GetProperty("visible").GetBoolean(), "frametime hide visible payload");

        var commandHandlersSource = ReadSsctlCommandHandlersFamilyText();
        var commandHandlersRootSource = ReadRepoFile("tools/ssctl/CommandHandlers.cs");
        AssertDoesNotContain(commandHandlersRootSource, "private static Task<int> HandleFlashbackAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs"), "HandleAudioRampTraceAsync");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs"), "HandleDiagnosticSessionAsync");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs"), "HandlePresentMonAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.DiagnosticSession.cs"), "HandleDiagnosticSessionAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.PresentMon.cs"), "HandlePresentMonAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.PresentMon.cs"), "TryResolvePreviewSwapChainAddressAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.CaptureControls.cs"), "HandleSetAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "HandleDeviceAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"RefreshDevices\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"GetCaptureOptions\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"SelectDevice\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"SelectAudioInputDevice\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"SetCustomAudioInput\"");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "HandleWindowAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleWindowAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "\"ArmClose\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "\"WindowAction\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "\"SetFullScreenEnabled\"");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleDeviceAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Recordings.cs"), "HandleRecordingsAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Recordings.cs"), "\"OpenRecordingsFolder\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleWaitAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "\"WaitForCondition\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "Math.Max(timeoutMs.GetValueOrDefault(0) + 5000, 60000)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleAssertAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "JsonDocument.Parse(assertionsJson)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "\"AssertSnapshot\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleProbeAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "\"ProbeVideoSource\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "\"ProbePreviewColor\"");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleStatsAsync");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleVerifyAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "HandleStatsAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "HandleSettingsAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "HandleFrameTimeAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "\"SetStatsVisible\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "\"SetStatsSectionVisible\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "\"SetSettingsVisible\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "\"SetFrameTimeOverlayVisible\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "HandleVerifyAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "\"VerifyFile\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "\"VerifyLastRecording\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "ConsumeFlag(context.Rest, \"--json\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "ParseOptionalStringFlag(context.Rest, \"--verification-profile\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs"), "HandleFlashbackAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs"), "return HandleFlashbackExportAsync(context);");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs"), "ParseFlashbackExportSeconds(context.Rest[1])");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "private static Task<int> HandleFlashbackExportAsync(CommandContext context)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "ConsumeFlag(context.Rest, \"--range\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "? ParseFlashbackExportSeconds(context.Rest[1])");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? \".\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flags.cs"), "private static bool ConsumeFlag(List<string> args, string flag)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Arguments.cs"), "private static string JoinRemaining(IReadOnlyList<string> args, int startIndex)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Json.cs"), "private static bool LooksLikeJson(string value)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Values.cs"), "private static object? ParseAssertionValue(string value)");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers" + ".Parsing" + ".cs")),
            "old ssctl parsing grab-bag removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.DeviceWindow.cs")),
            "old ssctl device/window grab-bag removed");
    }
}
