using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;
using static Sussudio.Tools.DiagnosticSessionFlashbackCycleScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackExportScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;
using static Sussudio.Tools.DiagnosticSessionFlashbackPreviewCycleScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackRejectedExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;
using static Sussudio.Tools.DiagnosticSessionFlashbackStressScenario;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;
using static Sussudio.Tools.DiagnosticSessionSampler;
using static Sussudio.Tools.DiagnosticSessionText;

namespace Sussudio.Tools;

public static class DiagnosticSessionRunner
{
    // Scenario names and broad requirements live in DiagnosticSessionScenarios.
    // RunAsync reads like a phase plan: setup, optional background scenario
    // task, sampling loop, cleanup, then summary.

    public static async Task<DiagnosticSessionResult> RunAsync(
        DiagnosticSessionOptions options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sendCommandAsync);

        var scenario = DiagnosticSessionScenarios.Normalize(options.Scenario);
        var durationSeconds = Math.Clamp(options.DurationSeconds, 0, 24 * 60 * 60);
        var sampleIntervalMs = Math.Clamp(options.SampleIntervalMs, 100, 60_000);
        var sessionId = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var outputDirectory = string.IsNullOrWhiteSpace(options.OutputDirectory)
            ? Path.Combine(Environment.CurrentDirectory, "temp", "diagnostic-sessions", sessionId)
            : Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        // Per-output-directory exclusive lock. Prevents two concurrent diagnostic-session
        // invocations from corrupting the manifest, final.snapshot.json, and per-scenario
        // JSON files in the same OutputDirectory (e.g., parallel CI matrix jobs sharing an
        // artifacts root). FileShare.None blocks other openers; DeleteOnClose self-cleans
        // on normal exit; the OS releases the handle on crash.
        var lockPath = Path.Combine(outputDirectory, ".sussudio-diag.lock");
        FileStream? lockHandle;
        try
        {
            lockHandle = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Another diagnostic session is already running in '{outputDirectory}'. " +
                $"Wait for it to finish or choose a different output directory. ({ex.Message})",
                ex);
        }

        try
        {
        var livePath = Path.Combine(outputDirectory, "session-live.json");
        var startedUtc = DateTimeOffset.UtcNow;
        var runnerProcessId = Environment.ProcessId;
        var lastStage = "initializing";
        var lastSamplingLiveStateUtc = DateTimeOffset.MinValue;
        Exception? terminalException = null;
        string? terminalExceptionStage = null;

        var actions = new List<string>();
        var warnings = new List<string>();
        var samples = new List<DiagnosticSessionSample>();
        JsonElement? timeline = null;
        JsonElement? verification = null;
        PresentMonProbeResult? presentMon = null;
        var commandFailureCount = 0;
        var startedPreview = false;
        var startedRecording = false;
        var enabledFlashback = false;
        var disabledFlashback = false;
        var startedFlashbackPlayback = false;
        var stoppedRecordingForVerification = false;
        var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);
        var runFlashbackPlayback = scenarioPlan.RunFlashbackPlayback;
        var runFlashbackStress = scenarioPlan.RunFlashbackStress;
        var runFlashbackScrubStress = scenarioPlan.RunFlashbackScrubStress;
        var runFlashbackRestartCycle = scenarioPlan.RunFlashbackRestartCycle;
        var runFlashbackEncoderCycle = scenarioPlan.RunFlashbackEncoderCycle;
        var runFlashbackExportPlayback = scenarioPlan.RunFlashbackExportPlayback;
        var runFlashbackSegmentPlayback = scenarioPlan.RunFlashbackSegmentPlayback;
        var runFlashbackRangeExport = scenarioPlan.RunFlashbackRangeExport;
        var runFlashbackRangeExportAudioSwitch = scenarioPlan.RunFlashbackRangeExportAudioSwitch;
        var runFlashbackLifecycle = scenarioPlan.RunFlashbackLifecycle;
        var runFlashbackExportConcurrent = scenarioPlan.RunFlashbackExportConcurrent;
        var runFlashbackDisableDuringExport = scenarioPlan.RunFlashbackDisableDuringExport;
        var runFlashbackRotatedExport = scenarioPlan.RunFlashbackRotatedExport;
        var runFlashbackPreviewCycle = scenarioPlan.RunFlashbackPreviewCycle;
        var runFlashbackPlaybackPreviewCycle = scenarioPlan.RunFlashbackPlaybackPreviewCycle;
        var runFlashbackRecording = scenarioPlan.RunFlashbackRecording;
        var runFlashbackRecordingPreviewCycle = scenarioPlan.RunFlashbackRecordingPreviewCycle;
        var runFlashbackRecordingSettingsDeferred = scenarioPlan.RunFlashbackRecordingSettingsDeferred;
        var runFlashbackRecordingExportRejected = scenarioPlan.RunFlashbackRecordingExportRejected;
        var runFlashbackExportRejected = scenarioPlan.RunFlashbackExportRejected;
        FlashbackRecordingSettingsDeferredPresetState flashbackRecordingSettingsDeferredPresetState = default;
        var commandSendGate = new SemaphoreSlim(1, 1);
        using var scenarioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scenarioCancellationToken = scenarioCts.Token;
        var backgroundTasks = new DiagnosticSessionBackgroundTasks();

        async Task<JsonElement> SendRawWithConnectRetryAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs)
            => await SendRawWithConnectRetryWithTokenAsync(command, payload, responseTimeoutMs, scenarioCancellationToken).ConfigureAwait(false);

        async Task<JsonElement> SendRawWithConnectRetryWithTokenAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs,
            CancellationToken commandCancellationToken)
        {
            var response = await SendCommandWithConnectRetryAsync(
                    sendCommandAsync,
                    command,
                    payload,
                    responseTimeoutMs,
                    TimeSpan.FromSeconds(30),
                    commandCancellationToken)
                .ConfigureAwait(false);
            return response ?? BuildLocalFailureResponse(command, "no response after connect retry");
        }

        JsonElement initialSnapshot = CreateEmptyJsonObject();
        var initialSnapshotKnown = false;
        await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        try
        {
            SetStage("initial-snapshot");
            var initialResponse = await SendAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(initialResponse, out var initial))
            {
                initialSnapshot = initial;
                initialSnapshotKnown = true;
            }
            else
            {
                commandFailureCount++;
                warnings.Add("initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped");
            }
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, "initial-snapshot");
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        try
        {
            SetStage("scenario-setup");
            if (!initialSnapshotKnown && scenario != DiagnosticSessionScenarios.Observe)
            {
                commandFailureCount++;
                warnings.Add($"initial-snapshot: skipped state-mutating scenario '{scenario}' because the initial app state is unknown");
            }
            else
            {
                if (DiagnosticSessionScenarios.NeedsFlashback(scenario) && !GetBool(initialSnapshot, "FlashbackActive"))
                {
                    await SendAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
                    enabledFlashback = true;
                    actions.Add("flashback enabled");
                }

            if (runFlashbackExportRejected && GetBool(initialSnapshot, "FlashbackActive"))
            {
                await SendAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = false }, null).ConfigureAwait(false);
                disabledFlashback = true;
                actions.Add("flashback disabled for rejected export");
            }

            if (DiagnosticSessionScenarios.NeedsPreview(scenario) && !GetBool(initialSnapshot, "IsPreviewing"))
            {
                await SendAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
                startedPreview = true;
                actions.Add("preview started");
                await TryWaitAsync("VideoFramesFlowing", 15_000).ConfigureAwait(false);
            }

            if (DiagnosticSessionScenarios.NeedsRecording(scenario) && !GetBool(initialSnapshot, "IsRecording"))
            {
                if (scenarioPlan.RequiresFlashbackRecordingReadiness &&
                    !await WaitForFlashbackStressBufferReadyAsync(
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken).ConfigureAwait(false))
                {
                    warnings.Add("flashback recording: Flashback buffer did not become recording-ready within 30s");
                }

                await SendAsync("SetRecordingEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
                startedRecording = true;
                actions.Add("recording started");
                await TryWaitAsync("RecordingFileGrowing", 20_000).ConfigureAwait(false);
            }

            if (options.IncludePresentMon)
            {
                var correlationSnapshotResponse = await SendAsync("GetSnapshot", null, null).ConfigureAwait(false);
                TryGetSnapshot(correlationSnapshotResponse, out var correlationSnapshot);
                backgroundTasks.SetPresentMon(PresentMonProbe.RunAsync(new PresentMonProbeOptions
                {
                    ProcessName = "Sussudio",
                    DurationSeconds = Math.Max(1, durationSeconds),
                    PresentMonPath = options.PresentMonPath,
                    OutputFile = Path.Combine(outputDirectory, "presentmon.csv"),
                    ExpectedSwapChainAddress = GetString(correlationSnapshot, "PreviewD3DSwapChainAddress"),
                    AppPresentId = GetNullableLong(correlationSnapshot, "PreviewD3DLastRenderedPreviewPresentId"),
                    AppSourceSequenceNumber = GetNullableLong(correlationSnapshot, "PreviewD3DLastRenderedSourceSequenceNumber"),
                    AppPresentUtcUnixMs = GetNullableLong(correlationSnapshot, "PreviewD3DLastRenderedUtcUnixMs"),
                    KeepCsv = true
                }));
                actions.Add("presentmon capture started");
            }

            if (runFlashbackStress)
            {
                backgroundTasks.AddScenario(
                    1,
                    "flashback-stress-task",
                    RunFlashbackStressAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback stress started");
            }

            if (runFlashbackScrubStress)
            {
                backgroundTasks.AddScenario(
                    3,
                    "flashback-scrub-stress-task",
                    RunFlashbackScrubStressAsync(
                        actions,
                        warnings,
                        SendRawWithConnectRetryAsync,
                        scenarioCancellationToken));
                actions.Add("flashback scrub stress started");
            }

            if (runFlashbackRestartCycle)
            {
                backgroundTasks.AddScenario(
                    4,
                    "flashback-restart-cycle-task",
                    RunFlashbackRestartCycleAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback restart cycle started");
            }

            if (runFlashbackEncoderCycle)
            {
                backgroundTasks.AddScenario(
                    5,
                    "flashback-encoder-cycle-task",
                    RunFlashbackEncoderCycleAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback encoder cycle started");
            }

            if (runFlashbackExportPlayback)
            {
                backgroundTasks.AddScenario(
                    6,
                    "flashback-export-playback-task",
                    RunFlashbackExportPlaybackAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback export playback started");
            }

            if (runFlashbackSegmentPlayback)
            {
                backgroundTasks.AddScenario(
                    7,
                    "flashback-segment-playback-task",
                    RunFlashbackSegmentPlaybackAsync(
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback segment playback started");
            }

            if (runFlashbackRangeExport)
            {
                backgroundTasks.AddScenario(
                    8,
                    "flashback-range-export-task",
                    RunFlashbackRangeExportAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback range export started");
            }

            if (runFlashbackRangeExportAudioSwitch)
            {
                backgroundTasks.AddScenario(
                    9,
                    "flashback-range-export-audio-switch-task",
                    RunFlashbackRangeExportAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        SendRawWithConnectRetryAsync,
                        scenarioCancellationToken,
                        scenarioLabel: "flashback range export audio switch",
                        exportFileName: "flashback-range-export-audio-switch.mp4",
                        outPointMs: 15_000,
                        switchAudioDuringExport: true));
                actions.Add("flashback range export audio switch started");
            }

            if (runFlashbackLifecycle)
            {
                backgroundTasks.AddScenario(
                    2,
                    "flashback-lifecycle-task",
                    RunFlashbackLifecycleAsync(
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback lifecycle started");
            }

            if (runFlashbackExportConcurrent)
            {
                backgroundTasks.AddScenario(
                    10,
                    "flashback-export-concurrent-task",
                    RunFlashbackExportConcurrentAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        SendRawWithConnectRetryAsync,
                        scenarioCancellationToken));
                actions.Add("flashback concurrent export started");
            }

            if (runFlashbackDisableDuringExport)
            {
                backgroundTasks.AddScenario(
                    11,
                    "flashback-disable-during-export-task",
                    RunFlashbackDisableDuringExportAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        SendRawWithConnectRetryAsync,
                        scenarioCancellationToken));
                actions.Add("flashback disable during export started");
            }

            if (runFlashbackRotatedExport)
            {
                backgroundTasks.AddScenario(
                    12,
                    "flashback-rotated-export-task",
                    RunFlashbackRotatedExportAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback rotated export started");
            }

            if (runFlashbackPreviewCycle)
            {
                backgroundTasks.AddScenario(
                    13,
                    "flashback-preview-cycle-task",
                    RunFlashbackPreviewCycleAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback preview cycle started");
            }

            if (runFlashbackPlaybackPreviewCycle)
            {
                backgroundTasks.AddScenario(
                    14,
                    "flashback-playback-preview-cycle-task",
                    RunFlashbackPlaybackPreviewCycleAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback playback preview cycle started");
            }

            if (runFlashbackRecordingPreviewCycle)
            {
                backgroundTasks.AddScenario(
                    15,
                    "flashback-recording-preview-cycle-task",
                    RunFlashbackRecordingPreviewCycleAsync(
                        actions,
                        warnings,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken));
                actions.Add("flashback recording preview cycle started");
            }

            if (runFlashbackRecordingSettingsDeferred)
            {
                backgroundTasks.SetRecordingSettingsDeferred(RunFlashbackRecordingSettingsDeferredAsync(
                        actions,
                        warnings,
                        (command, payload, timeoutMs, allowFailure) => SendAsync(command, payload, timeoutMs, allowFailure),
                        scenarioCancellationToken));
                actions.Add("flashback recording settings deferred started");
            }

            if (runFlashbackPlayback)
            {
                if (!await WaitForFlashbackStressBufferReadyAsync(
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        scenarioCancellationToken).ConfigureAwait(false))
                {
                    warnings.Add("flashback playback: Flashback buffer did not become playback-ready within 30s");
                }

                var playResponse = await SendAsync(
                        "FlashbackAction",
                        new Dictionary<string, object?> { ["action"] = "play", ["positionMs"] = 1000 },
                        null)
                    .ConfigureAwait(false);
                if (AutomationSnapshotFormatter.IsSuccess(playResponse))
                {
                    startedFlashbackPlayback = true;
                    actions.Add("flashback playback started at 1000ms");
                    var playingSnapshot = await WaitForFlashbackPlaybackStateAsync(
                            (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                            "Playing",
                            TimeSpan.FromSeconds(5),
                            scenarioCancellationToken)
                        .ConfigureAwait(false);
                    if (playingSnapshot is null)
                    {
                        warnings.Add("flashback playback: playback did not report Playing within 5s");
                    }
                }
                else
                {
                    warnings.Add($"flashback playback: play command failed - {AutomationSnapshotFormatter.Get(playResponse, "Message", "unknown error")}");
                }
            }

            SetStage("sampling");
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
            await SampleLoopAsync(
                    durationSeconds,
                    sampleIntervalMs,
                    samples,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken,
                    WriteSamplingLiveStateBestEffortAsync)
                .ConfigureAwait(false);

            await backgroundTasks.AwaitScenarioTasksAsync().ConfigureAwait(false);
            flashbackRecordingSettingsDeferredPresetState = await backgroundTasks
                .AwaitRecordingSettingsDeferredAsync(flashbackRecordingSettingsDeferredPresetState)
                .ConfigureAwait(false);

            if (runFlashbackExportRejected)
            {
                await RunFlashbackExportRejectedAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs, allowFailure) => SendAsync(command, payload, timeoutMs, allowFailure),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (runFlashbackRecordingExportRejected)
            {
                await RunFlashbackRecordingExportRejectedAsync(
                        outputDirectory,
                        actions,
                        warnings,
                        (command, payload, timeoutMs, allowFailure) => SendAsync(command, payload, timeoutMs, allowFailure),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            presentMon = await backgroundTasks.AwaitPresentMonAsync(presentMon, warnings).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, lastStage);
            scenarioCts.Cancel();
            var backgroundTaskDrain = await backgroundTasks.ObserveAfterFaultAsync(
                    warnings,
                    SetStage,
                    RecordTerminalException,
                    () => WriteLiveStateBestEffortAsync(),
                    presentMon,
                    flashbackRecordingSettingsDeferredPresetState)
                .ConfigureAwait(false);
            presentMon = backgroundTaskDrain.PresentMon;
            flashbackRecordingSettingsDeferredPresetState = backgroundTaskDrain.RecordingSettingsDeferredPresetState;
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }
        finally
        {
            var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;
            if (startedRecording && (shouldStopRecordingForVerification || !options.LeaveRunning))
            {
                try
                {
                    SetStage("cleanup-stop-recording");
                    const int recordingCleanupTimeoutMs = 300_000;
                    using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs));
                    var stopResponse = await SendWithTokenAsync("SetRecordingEnabled", new Dictionary<string, object?> { ["enabled"] = false }, recordingCleanupTimeoutMs, false, cleanupCts.Token).ConfigureAwait(false);
                    actions.Add(shouldStopRecordingForVerification && options.LeaveRunning
                        ? "recording stopped for verification"
                        : "recording stopped");
                    stoppedRecordingForVerification = shouldStopRecordingForVerification &&
                                                       AutomationSnapshotFormatter.IsSuccess(stopResponse);
                    if (AutomationSnapshotFormatter.IsSuccess(stopResponse))
                    {
                        await TryWaitWithTokenAsync("RecordingStopped", recordingCleanupTimeoutMs, cleanupCts.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    RecordTerminalException(ex, "cleanup-stop-recording");
                }
            }

            if (!options.LeaveRunning)
            {
                if (startedFlashbackPlayback)
                {
                    try
                    {
                        SetStage("cleanup-go-live");
                        using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
                        await SendWithTokenAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, 15_000, false, cleanupCts.Token).ConfigureAwait(false);
                        actions.Add("flashback playback returned live");
                    }
                    catch (Exception ex)
                    {
                        RecordTerminalException(ex, "cleanup-go-live");
                    }
                }

                if (startedPreview && !GetBool(initialSnapshot, "IsPreviewing"))
                {
                    try
                    {
                        SetStage("cleanup-stop-preview");
                        using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
                        await SendWithTokenAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = false }, 15_000, false, cleanupCts.Token).ConfigureAwait(false);
                        actions.Add("preview stopped");
                    }
                    catch (Exception ex)
                    {
                        RecordTerminalException(ex, "cleanup-stop-preview");
                    }
                }

                if (enabledFlashback && !GetBool(initialSnapshot, "FlashbackActive"))
                {
                    try
                    {
                        SetStage("cleanup-restore-flashback-off");
                        var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout("SetFlashbackEnabled");
                        using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                        await SendWithTokenAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = false }, cleanupTimeoutMs, false, cleanupCts.Token).ConfigureAwait(false);
                        actions.Add("flashback restored off");
                    }
                    catch (Exception ex)
                    {
                        RecordTerminalException(ex, "cleanup-restore-flashback-off");
                    }
                }

                if (disabledFlashback && GetBool(initialSnapshot, "FlashbackActive"))
                {
                    try
                    {
                        SetStage("cleanup-restore-flashback-on");
                        var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout("SetFlashbackEnabled");
                        using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                        await SendWithTokenAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = true }, cleanupTimeoutMs, false, cleanupCts.Token).ConfigureAwait(false);
                        actions.Add("flashback restored on");
                    }
                    catch (Exception ex)
                    {
                        RecordTerminalException(ex, "cleanup-restore-flashback-on");
                    }
                }
            }

            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        if (runFlashbackRecordingSettingsDeferred)
        {
            try
            {
                SetStage("settings-deferred-restore");
                await VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(
                        actions,
                        warnings,
                        flashbackRecordingSettingsDeferredPresetState,
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RecordTerminalException(ex, "settings-deferred-restore");
            }
        }

        var hasFlashbackExportVerificationPath = DiagnosticSessionScenarios.TryGetFlashbackExportVerificationPath(
            scenario,
            outputDirectory,
            out var flashbackExportVerificationPath);
        var shouldRunVerification =
            startedRecording ||
            (options.VerifyRecording && hasFlashbackExportVerificationPath);
        if (shouldRunVerification)
        {
            try
            {
                SetStage("recording-verification");
                var verificationCommand = "VerifyLastRecording";
                Dictionary<string, object?>? verificationPayload = null;
                if (!startedRecording)
                {
                    verificationCommand = "VerifyFile";
                    verificationPayload = new Dictionary<string, object?>
                    {
                        ["filePath"] = flashbackExportVerificationPath,
                        ["strict"] = true,
                        ["verificationProfile"] = "flashback-export"
                    };
                }

                var verificationResponse = await SendAsync(verificationCommand, verificationPayload, 60_000).ConfigureAwait(false);
                if (TryGetVerification(verificationResponse, out var verificationElement))
                {
                    verification = verificationElement.Clone();
                }
                else
                {
                    warnings.Add(AutomationSnapshotFormatter.Get(verificationResponse, "Message", "Verification did not return data."));
                }
            }
            catch (Exception ex)
            {
                RecordTerminalException(ex, "recording-verification");
            }
        }
        else if (options.VerifyRecording)
        {
            actions.Add("recording verification skipped: scenario does not produce a recording or export artifact");
        }

        if (scenarioPlan.RequiresFlashbackRecordingValidation)
        {
            try
            {
                SetStage("recording-validation");
                ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings);
            }
            catch (Exception ex)
            {
                RecordTerminalException(ex, "recording-validation");
            }
        }

        try
        {
            SetStage("timeline");
            var timelineResponse = await SendAsync(
                    "GetPerformanceTimeline",
                    new Dictionary<string, object?> { ["maxEntries"] = 240 },
                    null)
                .ConfigureAwait(false);
            if (timelineResponse.TryGetProperty("Data", out var timelineData))
            {
                timeline = timelineData.Clone();
            }
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, "timeline");
        }

        var lastSnapshot = samples.Count > 0
            ? samples[^1].Snapshot
            : initialSnapshot;
        var healthSnapshot = lastSnapshot;
        try
        {
            SetStage("final-snapshot");
            var finalSnapshotResponse = await SendAsync("GetSnapshot", null, null).ConfigureAwait(false);
            healthSnapshot = TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot)
                ? finalSnapshot
                : lastSnapshot;
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, "final-snapshot");
        }

        SetStage("result-analysis");
        var diagnosticHealthSnapshot = stoppedRecordingForVerification
            ? lastSnapshot
            : healthSnapshot;
        var healthStatus = GetString(diagnosticHealthSnapshot, "DiagnosticHealthStatus") ?? "Unknown";
        var likelyStage = GetString(diagnosticHealthSnapshot, "DiagnosticLikelyStage") ?? "diagnostic_unavailable";
        var summary = GetString(diagnosticHealthSnapshot, "DiagnosticSummary") ?? string.Empty;
        var evidence = GetString(diagnosticHealthSnapshot, "DiagnosticEvidence") ?? string.Empty;
        var playbackSessionMetrics = BuildFlashbackPlaybackSessionMetrics(initialSnapshot, samples, lastSnapshot);
        var playbackEndSnapshot = playbackSessionMetrics.EndSnapshot;
        var playbackPendingAtEnd = playbackSessionMetrics.Observed
            ? GetInt(playbackEndSnapshot, "FlashbackPlaybackPendingCommands")
            : 0;
        var playbackMaxPendingObserved = playbackSessionMetrics.MaxPendingCommandsObserved;
        var playbackMaxLatencyObserved = playbackSessionMetrics.MaxCommandQueueLatencyMsObserved;
        var playbackMaxLatencyCommandObserved = playbackSessionMetrics.MaxCommandQueueLatencyCommandObserved;
        var playbackDroppedAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackCommandsDropped") ?? 0 : 0;
        var playbackSkippedAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackCommandsSkippedNotReady") ?? 0 : 0;
        var playbackScrubCoalescedAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackScrubUpdatesCoalesced") ?? 0 : 0;
        var playbackSeekCoalescedAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackSeekCommandsCoalesced") ?? 0 : 0;
        var playbackLastCommandFailureAtEnd = playbackSessionMetrics.Observed ? GetString(playbackEndSnapshot, "FlashbackPlaybackLastCommandFailure") ?? string.Empty : string.Empty;
        var playbackLastCommandFailureUtcUnixMsAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs") ?? 0 : 0;
        var playbackObservedFpsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackObservedFps") : 0;
        var playbackAvgFrameMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackAvgFrameMs") : 0;
        var playbackP99FrameMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackP99FrameMs") : 0;
        var playbackMaxFrameMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackMaxFrameMs") : 0;
        var playbackOnePercentLowFpsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackOnePercentLowFps") : 0;
        var playbackDecodeAvgMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackDecodeAvgMs") : 0;
        var playbackDecodeP95MsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackDecodeP95Ms") : 0;
        var playbackDecodeP99MsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackDecodeP99Ms") : 0;
        var playbackDecodeMaxMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackDecodeMaxMs") : 0;
        var playbackMaxDecodePhaseAtEnd = playbackSessionMetrics.Observed ? GetString(playbackEndSnapshot, "FlashbackPlaybackMaxDecodePhase") ?? string.Empty : string.Empty;
        var playbackMaxDecodeReceiveMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackMaxDecodeReceiveMs") : 0;
        var playbackMaxDecodeFeedMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackMaxDecodeFeedMs") : 0;
        var playbackMaxDecodeReadMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackMaxDecodeReadMs") : 0;
        var playbackMaxDecodeSendMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackMaxDecodeSendMs") : 0;
        var playbackMaxDecodeAudioMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackMaxDecodeAudioMs") : 0;
        var playbackMaxDecodeConvertMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackMaxDecodeConvertMs") : 0;
        var playbackMaxDecodeUtcUnixMsAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackMaxDecodeUtcUnixMs") ?? 0 : 0;
        var playbackMaxDecodePositionMsAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackMaxDecodePositionMs") ?? 0 : 0;
        var playbackFrameCountAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackFrameCount") ?? 0 : 0;
        var playbackLateFramesAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackLateFrames") ?? 0 : 0;
        var playbackSlowFramesAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackSlowFrames") ?? 0 : 0;
        var playbackSlowFramePercentAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackSlowFramePercent") : 0;
        var playbackDroppedFramesAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackDroppedFrames") ?? 0 : 0;
        var playbackAudioMasterDelayDoublesAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackAudioMasterDelayDoubles") ?? 0 : 0;
        var playbackAudioMasterDelayShrinksAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackAudioMasterDelayShrinks") ?? 0 : 0;
        var playbackAudioMasterFallbacksAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackAudioMasterFallbacks") ?? 0 : 0;
        var playbackAudioMasterUnavailableFallbacksAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks") ?? 0 : 0;
        var playbackAudioMasterStaleFallbacksAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackAudioMasterStaleFallbacks") ?? 0 : 0;
        var playbackAudioMasterDriftOutlierFallbacksAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks") ?? 0 : 0;
        var playbackAudioMasterLastFallbackReasonAtEnd = playbackSessionMetrics.Observed ? GetString(playbackEndSnapshot, "FlashbackPlaybackAudioMasterLastFallbackReason") ?? string.Empty : string.Empty;
        var playbackAudioMasterLastFallbackClockAgeMsAtEnd = playbackSessionMetrics.Observed ? GetDouble(playbackEndSnapshot, "FlashbackPlaybackAudioMasterLastFallbackClockAgeMs") : 0;
        var playbackSubmitFailuresAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackSubmitFailures") ?? 0 : 0;
        var playbackSegmentSwitchesAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackSegmentSwitches") ?? 0 : 0;
        var playbackFmp4ReopensAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackFmp4Reopens") ?? 0 : 0;
        var playbackWriteHeadWaitsAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackWriteHeadWaits") ?? 0 : 0;
        var playbackNearLiveSnapsAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackNearLiveSnaps") ?? 0 : 0;
        var playbackDecodeErrorSnapsAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackDecodeErrorSnaps") ?? 0 : 0;
        var playbackLastWriteHeadWaitGapMsAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackLastWriteHeadWaitGapMs") ?? 0 : 0;
        var playbackSeekForwardDecodeCapHitsAtEnd = playbackSessionMetrics.Observed ? GetNullableLong(playbackEndSnapshot, "FlashbackPlaybackSeekForwardDecodeCapHits") ?? 0 : 0;
        var playbackSeekForwardDecodeCapHitsDelta = playbackSessionMetrics.Observed
            ? GetCounterDelta(playbackEndSnapshot, initialSnapshot, "FlashbackPlaybackSeekForwardDecodeCapHits")
            : 0;
        var playbackLastSeekHitForwardDecodeCapAtEnd = playbackSessionMetrics.Observed &&
                                                      GetBool(playbackEndSnapshot, "FlashbackPlaybackLastSeekHitForwardDecodeCap");
        if (playbackSeekForwardDecodeCapHitsDelta > 0)
        {
            warnings.Add(
                "flashback playback seek forward-decode cap hit during session " +
                $"delta={playbackSeekForwardDecodeCapHitsDelta} total={playbackSeekForwardDecodeCapHitsAtEnd}");
        }
        var flashbackExportForceRotateFallbacksAtEnd = GetNullableLong(lastSnapshot, "FlashbackExportForceRotateFallbacks") ?? 0;
        var flashbackExportForceRotateFallbacksDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "FlashbackExportForceRotateFallbacks");
        var flashbackExportLastForceRotateFallbackSegmentsAtEnd = GetInt(lastSnapshot, "FlashbackExportLastForceRotateFallbackSegments");
        if (flashbackExportForceRotateFallbacksDelta > 0)
        {
            warnings.Add(
                "flashback export used force-rotate partial fallback " +
                $"delta={flashbackExportForceRotateFallbacksDelta} total={flashbackExportForceRotateFallbacksAtEnd} " +
                $"segments={flashbackExportLastForceRotateFallbackSegmentsAtEnd}");
        }
        var recordingMetrics = BuildFlashbackRecordingMetrics(initialSnapshot, samples);
        var exportMetrics = BuildFlashbackExportSessionMetrics(initialSnapshot, samples, lastSnapshot);
        var sourceCadenceMetrics = BuildSourceCadenceSessionMetrics(samples, lastSnapshot);
        var previewCadenceMetrics = BuildPreviewCadenceSessionMetrics(samples, lastSnapshot);
        var previewD3DMetrics = BuildPreviewD3DMetrics(initialSnapshot, lastSnapshot, samples);
        var visualCadenceMetrics = BuildVisualCadenceSessionMetrics(samples, lastSnapshot);
        if (runFlashbackPlayback)
        {
            ValidateFlashbackPlaybackSession(
                playbackSessionMetrics.Observed ? playbackEndSnapshot : lastSnapshot,
                playbackSessionMetrics,
                visualCadenceMetrics,
                durationSeconds,
                warnings);
        }

        var sourceReaderFramesDroppedDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MfSourceReaderFramesDropped");
        var videoIngestErrorsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "VideoIngestErrorCount");
        var previewSchedulerDroppedAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterTotalDropped") ?? 0;
        var previewSchedulerDeadlineDropsAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterDeadlineDropCount") ?? 0;
        var previewSchedulerClearedDropsAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterClearedDropCount") ?? 0;
        var previewSchedulerUnderflowsAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterUnderflowCount") ?? 0;
        var previewSchedulerResumeReprimesAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterResumeReprimeCount") ?? 0;
        var previewSchedulerDroppedDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterTotalDropped");
        var previewSchedulerDeadlineDropsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterDeadlineDropCount");
        var previewSchedulerClearedDropsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterClearedDropCount");
        var previewSchedulerUnderflowsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterUnderflowCount");
        var previewSchedulerResumeReprimesDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterResumeReprimeCount");
        var previewSchedulerScheduleLateDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterScheduleLateCount");
        var previewSchedulerMaxScheduleLateMsObserved = samples
            .Select(sample => GetDouble(sample.Snapshot, "MjpegPreviewJitterMaxScheduleLateMs"))
            .Append(GetDouble(lastSnapshot, "MjpegPreviewJitterMaxScheduleLateMs"))
            .DefaultIfEmpty(0)
            .Max();
        var isFlashbackScenario = scenarioPlan.UsesFlashbackScenarioWarningPolicy;
        ValidateCleanupLifecycleRestored(
            options.LeaveRunning,
            startedPreview,
            enabledFlashback,
            startedFlashbackPlayback,
            initialSnapshot,
            healthSnapshot,
            warnings);
        var toleratesSourceSignalHealthWarning = scenarioPlan.ToleratesSourceSignalHealthWarning;
        if (isFlashbackScenario)
        {
            var previewTargetFps = GetDouble(lastSnapshot, "ExpectedCaptureFrameRate");
            if (previewTargetFps <= 0)
            {
                previewTargetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
            }

            var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, previewTargetFps);
            var toleratesPreviewCycleSchedulerSettling =
                scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy;
            var toleratesSparsePreviewSchedulerDeadlineDrops =
                IsSparsePreviewSchedulerDeadlineDropRun(
                    previewSchedulerDeadlineDropsDelta,
                    previewSchedulerUnderflowsDelta,
                    durationSeconds,
                    visualCadenceHealthy);
            var toleratesSparseScrubSchedulerTransitions =
                scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&
                IsSparsePreviewSchedulerStressRun(
                    previewSchedulerDeadlineDropsDelta,
                    previewSchedulerUnderflowsDelta,
                    durationSeconds,
                    visualCadenceHealthy);
            ValidateFlashbackPreviewScheduler(
                previewSchedulerDeadlineDropsDelta,
                previewSchedulerUnderflowsDelta,
                previewD3DMetrics.StatsFailureDelta,
                previewCadenceMetrics,
                visualCadenceMetrics,
                previewD3DMetrics,
                previewTargetFps,
                toleratesPreviewCycleSchedulerSettling ||
                    toleratesSparsePreviewSchedulerDeadlineDrops ||
                    toleratesSparseScrubSchedulerTransitions,
                warnings);
        }

        var diagnosticHealthObservation = BuildSessionDiagnosticHealthObservation(
            samples,
            diagnosticHealthSnapshot,
            isFlashbackScenario);
        var sparseSourceCaptureCadenceWarning =
            isFlashbackScenario &&
            IsSparseSourceCaptureCadenceWarningRun(
                diagnosticHealthObservation,
                sourceCadenceMetrics,
                sourceReaderFramesDroppedDelta,
                videoIngestErrorsDelta,
                durationSeconds,
                IsVisualCadenceSessionHealthy(visualCadenceMetrics, GetDouble(lastSnapshot, "ExpectedCaptureFrameRate")));
        var toleratesFlashbackForceRotateDrainWarning = scenarioPlan.ToleratesFlashbackForceRotateDrainWarning;
        var diagnosticHealthTolerated =
            (toleratesSourceSignalHealthWarning &&
             IsSourceSignalDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (toleratesFlashbackForceRotateDrainWarning &&
             IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            sparseSourceCaptureCadenceWarning ||
            (isFlashbackScenario &&
             scenarioPlan.IsPreviewCycleScenario &&
             IsVisualCadenceSessionHealthy(visualCadenceMetrics, GetDouble(lastSnapshot, "ExpectedCaptureFrameRate")) &&
             IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (isFlashbackScenario &&
             IsSparsePreviewSchedulerDeadlineDropRun(
                 previewSchedulerDeadlineDropsDelta,
                 previewSchedulerUnderflowsDelta,
                 durationSeconds,
                 IsVisualCadenceSessionHealthy(visualCadenceMetrics, GetDouble(lastSnapshot, "ExpectedCaptureFrameRate"))) &&
             IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation));
        var diagnosticHealthSucceeded =
            !IsFailingDiagnosticHealthSeverity(diagnosticHealthObservation.Severity) ||
            diagnosticHealthTolerated;
        if (!diagnosticHealthSucceeded)
        {
            warnings.Add(
                "diagnostic health degraded during session: " +
                $"health={diagnosticHealthObservation.HealthStatus} " +
                $"stage={diagnosticHealthObservation.LikelyStage} " +
                $"offsetMs={diagnosticHealthObservation.OffsetMs} " +
                $"evidence={FormatOptional(diagnosticHealthObservation.Evidence)}");
        }
        else if (diagnosticHealthTolerated &&
                 !sparseSourceCaptureCadenceWarning &&
                 !IsSparsePreviewSchedulerDeadlineDropRun(
                     previewSchedulerDeadlineDropsDelta,
                     previewSchedulerUnderflowsDelta,
                     durationSeconds,
                     IsVisualCadenceSessionHealthy(visualCadenceMetrics, GetDouble(lastSnapshot, "ExpectedCaptureFrameRate"))))
        {
            var toleratedReason = IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)
                ? "preview scheduler transition warning tolerated for preview-cycle scenario"
                : IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)
                    ? "flashback force-rotate drain warning tolerated for flashback scenario"
                : "source-signal warning tolerated for export reliability scenario";
            warnings.Add(
                $"diagnostic health {toleratedReason}: " +
                $"health={diagnosticHealthObservation.HealthStatus} " +
                $"stage={diagnosticHealthObservation.LikelyStage} " +
                $"offsetMs={diagnosticHealthObservation.OffsetMs} " +
                $"evidence={FormatOptional(diagnosticHealthObservation.Evidence)}");
        }

        var flashbackWarningsSucceeded = !isFlashbackScenario ||
                                         warnings.All(warning => IsToleratedFlashbackScenarioWarning(
                                             warning,
                                             toleratesSourceSignalHealthWarning,
                                             toleratesFlashbackForceRotateDrainWarning,
                                             scenarioPlan.IsPreviewCycleScenario));

        var processCpuMaxPercentObserved = samples
            .Select(sample => GetDouble(sample.Snapshot, "ProcessCpuPercent"))
            .Append(GetDouble(lastSnapshot, "ProcessCpuPercent"))
            .DefaultIfEmpty(0.0)
            .Max();

        var samplesPath = Path.Combine(outputDirectory, "samples.json");
        var frameLedgerPath = Path.Combine(outputDirectory, "frame-ledger.json");
        var timelinePath = Path.Combine(outputDirectory, "timeline.json");
        var summaryPath = Path.Combine(outputDirectory, "summary.json");

        await WriteArtifactBestEffortAsync("write-samples", samplesPath, samples).ConfigureAwait(false);
        await WriteArtifactBestEffortAsync("write-frame-ledger", frameLedgerPath, BuildFrameLedgerTrace(sessionId, samples)).ConfigureAwait(false);
        await WriteArtifactBestEffortAsync("write-timeline", timelinePath, timeline).ConfigureAwait(false);

        var verificationSucceeded = verification.HasValue
            ? GetBool(verification.Value, "Succeeded")
            : (bool?)null;
        var completedUtc = DateTimeOffset.UtcNow;
        var terminalState = GetTerminalState();
        SetStage("summary");
        var result = new DiagnosticSessionResult
        {
            SessionId = sessionId,
            Scenario = scenario,
            Success = commandFailureCount == 0 &&
                      terminalException is null &&
                      diagnosticHealthSucceeded &&
                      (presentMon is null || presentMon.Success) &&
                      (!verificationSucceeded.HasValue || verificationSucceeded.Value) &&
                      flashbackWarningsSucceeded,
            StartedUtc = startedUtc,
            CompletedUtc = completedUtc,
            TerminalState = terminalState,
            LastStage = GetResultLastStage(),
            UnhandledException = terminalException is null ? null : FormatTerminalException(terminalException),
            RunnerProcessId = runnerProcessId,
            DurationSeconds = durationSeconds,
            SampleIntervalMs = sampleIntervalMs,
            SampleCount = samples.Count,
            OutputDirectory = outputDirectory,
            LivePath = livePath,
            SummaryPath = summaryPath,
            SamplesPath = samplesPath,
            FrameLedgerPath = frameLedgerPath,
            TimelinePath = timelinePath,
            HealthStatus = healthStatus,
            LikelyStage = likelyStage,
            Summary = summary,
            Evidence = evidence,
            SelectedResolutionAtEnd = GetString(lastSnapshot, "SelectedResolution") ?? string.Empty,
            SelectedFrameRateAtEnd = GetDouble(lastSnapshot, "SelectedFrameRate"),
            SelectedFriendlyFrameRateAtEnd = GetString(lastSnapshot, "SelectedFriendlyFrameRate") ?? string.Empty,
            SelectedExactFrameRateArgAtEnd = GetString(lastSnapshot, "SelectedExactFrameRateArg") ?? string.Empty,
            SelectedVideoFormatAtEnd = GetString(lastSnapshot, "SelectedVideoFormat") ?? string.Empty,
            VideoRequestedSubtypeAtEnd = GetString(lastSnapshot, "VideoRequestedSubtype") ?? string.Empty,
            VideoNegotiatedSubtypeAtEnd = GetString(lastSnapshot, "VideoNegotiatedSubtype") ?? string.Empty,
            SourceWidthAtEnd = (int)(GetNullableLong(lastSnapshot, "SourceWidth") ?? 0),
            SourceHeightAtEnd = (int)(GetNullableLong(lastSnapshot, "SourceHeight") ?? 0),
            DetectedSourceFrameRateAtEnd = GetDouble(lastSnapshot, "DetectedSourceFrameRate"),
            DetectedSourceFrameRateArgAtEnd = GetString(lastSnapshot, "DetectedSourceFrameRateArg") ?? string.Empty,
            SourceIsHdrAtEnd = GetBool(lastSnapshot, "SourceIsHdr"),
            SourceTelemetrySummaryAtEnd = GetString(lastSnapshot, "SourceTelemetrySummaryText") ?? string.Empty,
            FlashbackPlaybackPendingCommandsAtEnd = playbackPendingAtEnd,
            FlashbackPlaybackMaxPendingCommandsObserved = playbackMaxPendingObserved,
            FlashbackPlaybackMaxCommandQueueLatencyMsObserved = playbackMaxLatencyObserved,
            FlashbackPlaybackMaxCommandQueueLatencyCommandObserved = playbackMaxLatencyCommandObserved,
            FlashbackPlaybackCommandsDroppedAtEnd = playbackDroppedAtEnd,
            FlashbackPlaybackCommandsSkippedNotReadyAtEnd = playbackSkippedAtEnd,
            FlashbackPlaybackScrubUpdatesCoalescedAtEnd = playbackScrubCoalescedAtEnd,
            FlashbackPlaybackSeekCommandsCoalescedAtEnd = playbackSeekCoalescedAtEnd,
            FlashbackPlaybackLastCommandFailureAtEnd = playbackLastCommandFailureAtEnd,
            FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd = playbackLastCommandFailureUtcUnixMsAtEnd,
            FlashbackPlaybackObservedFpsAtEnd = playbackObservedFpsAtEnd,
            FlashbackPlaybackMinObservedFpsObserved = playbackSessionMetrics.MinObservedFpsObserved,
            FlashbackPlaybackAvgFrameMsAtEnd = playbackAvgFrameMsAtEnd,
            FlashbackPlaybackP99FrameMsAtEnd = playbackP99FrameMsAtEnd,
            FlashbackPlaybackMaxFrameMsAtEnd = playbackMaxFrameMsAtEnd,
            FlashbackPlaybackOnePercentLowFpsAtEnd = playbackOnePercentLowFpsAtEnd,
            FlashbackPlaybackMinOnePercentLowFpsObserved = playbackSessionMetrics.MinOnePercentLowFpsObserved,
            FlashbackPlaybackOnePercentLowSampleWindowObserved = playbackSessionMetrics.OnePercentLowSampleWindowObserved,
            FlashbackPlaybackOnePercentLowMinimumFrames = playbackSessionMetrics.MinimumOnePercentLowFrameCount,
            FlashbackPlaybackMaxSessionFrameCountObserved = playbackSessionMetrics.MaxSessionFrameCountObserved,
            FlashbackPlaybackMinOnePercentLowOffsetMs = playbackSessionMetrics.MinOnePercentLowOffsetMs,
            FlashbackPlaybackMinOnePercentLowFrameCount = playbackSessionMetrics.MinOnePercentLowFrameCount,
            FlashbackPlaybackMinOnePercentLowP99FrameMs = playbackSessionMetrics.MinOnePercentLowP99FrameMs,
            FlashbackPlaybackMinOnePercentLowMaxFrameMs = playbackSessionMetrics.MinOnePercentLowMaxFrameMs,
            FlashbackPlaybackMinOnePercentLowDecodeP99Ms = playbackSessionMetrics.MinOnePercentLowDecodeP99Ms,
            FlashbackPlaybackMinOnePercentLowDecodeMaxMs = playbackSessionMetrics.MinOnePercentLowDecodeMaxMs,
            FlashbackPlaybackMinOnePercentLowAvDriftMs = playbackSessionMetrics.MinOnePercentLowAvDriftMs,
            FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks = playbackSessionMetrics.MinOnePercentLowAudioMasterFallbacks,
            FlashbackPlaybackMaxP99FrameMsObserved = playbackSessionMetrics.MaxP99FrameMsObserved,
            FlashbackPlaybackMaxFrameMsObserved = playbackSessionMetrics.MaxFrameMsObserved,
            FlashbackPlaybackMaxSlowFramePercentObserved = playbackSessionMetrics.MaxSlowFramePercentObserved,
            FlashbackPlaybackDecodeAvgMsAtEnd = playbackDecodeAvgMsAtEnd,
            FlashbackPlaybackDecodeP95MsAtEnd = playbackDecodeP95MsAtEnd,
            FlashbackPlaybackDecodeP99MsAtEnd = playbackDecodeP99MsAtEnd,
            FlashbackPlaybackDecodeMaxMsAtEnd = playbackDecodeMaxMsAtEnd,
            FlashbackPlaybackMaxDecodePhaseAtEnd = playbackMaxDecodePhaseAtEnd,
            FlashbackPlaybackMaxDecodeReceiveMsAtEnd = playbackMaxDecodeReceiveMsAtEnd,
            FlashbackPlaybackMaxDecodeFeedMsAtEnd = playbackMaxDecodeFeedMsAtEnd,
            FlashbackPlaybackMaxDecodeReadMsAtEnd = playbackMaxDecodeReadMsAtEnd,
            FlashbackPlaybackMaxDecodeSendMsAtEnd = playbackMaxDecodeSendMsAtEnd,
            FlashbackPlaybackMaxDecodeAudioMsAtEnd = playbackMaxDecodeAudioMsAtEnd,
            FlashbackPlaybackMaxDecodeConvertMsAtEnd = playbackMaxDecodeConvertMsAtEnd,
            FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd = playbackMaxDecodeUtcUnixMsAtEnd,
            FlashbackPlaybackMaxDecodePositionMsAtEnd = playbackMaxDecodePositionMsAtEnd,
            FlashbackPlaybackMaxDecodeP99MsObserved = playbackSessionMetrics.MaxDecodeP99MsObserved,
            FlashbackPlaybackMaxDecodeMsObserved = playbackSessionMetrics.MaxDecodeMsObserved,
            FlashbackPlaybackMaxDecodePhaseObserved = playbackSessionMetrics.MaxDecodePhaseObserved,
            FlashbackPlaybackMaxDecodeReceiveMsObserved = playbackSessionMetrics.MaxDecodeReceiveMsObserved,
            FlashbackPlaybackMaxDecodeFeedMsObserved = playbackSessionMetrics.MaxDecodeFeedMsObserved,
            FlashbackPlaybackMaxDecodeReadMsObserved = playbackSessionMetrics.MaxDecodeReadMsObserved,
            FlashbackPlaybackMaxDecodeSendMsObserved = playbackSessionMetrics.MaxDecodeSendMsObserved,
            FlashbackPlaybackMaxDecodeAudioMsObserved = playbackSessionMetrics.MaxDecodeAudioMsObserved,
            FlashbackPlaybackMaxDecodeConvertMsObserved = playbackSessionMetrics.MaxDecodeConvertMsObserved,
            FlashbackPlaybackMaxDecodeUtcUnixMsObserved = playbackSessionMetrics.MaxDecodeUtcUnixMsObserved,
            FlashbackPlaybackMaxDecodePositionMsObserved = playbackSessionMetrics.MaxDecodePositionMsObserved,
            FlashbackPlaybackFrameCountAtEnd = playbackFrameCountAtEnd,
            FlashbackPlaybackLateFramesAtEnd = playbackLateFramesAtEnd,
            FlashbackPlaybackSlowFramesAtEnd = playbackSlowFramesAtEnd,
            FlashbackPlaybackSlowFramePercentAtEnd = playbackSlowFramePercentAtEnd,
            FlashbackPlaybackDroppedFramesAtEnd = playbackDroppedFramesAtEnd,
            FlashbackPlaybackDroppedFramesDelta = playbackSessionMetrics.DroppedFramesDelta,
            FlashbackPlaybackAudioMasterDelayDoublesAtEnd = playbackAudioMasterDelayDoublesAtEnd,
            FlashbackPlaybackAudioMasterDelayShrinksAtEnd = playbackAudioMasterDelayShrinksAtEnd,
            FlashbackPlaybackAudioMasterFallbacksAtEnd = playbackAudioMasterFallbacksAtEnd,
            FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd = playbackAudioMasterUnavailableFallbacksAtEnd,
            FlashbackPlaybackAudioMasterStaleFallbacksAtEnd = playbackAudioMasterStaleFallbacksAtEnd,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd = playbackAudioMasterDriftOutlierFallbacksAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd = playbackAudioMasterLastFallbackReasonAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd = playbackAudioMasterLastFallbackClockAgeMsAtEnd,
            FlashbackPlaybackMaxAudioMasterDelayDoublesObserved = playbackSessionMetrics.MaxAudioMasterDelayDoublesObserved,
            FlashbackPlaybackMaxAudioMasterDelayShrinksObserved = playbackSessionMetrics.MaxAudioMasterDelayShrinksObserved,
            FlashbackPlaybackMaxAudioMasterFallbacksObserved = playbackSessionMetrics.MaxAudioMasterFallbacksObserved,
            FlashbackPlaybackMaxAudioBufferedDurationMsObserved = playbackSessionMetrics.MaxAudioBufferedDurationMsObserved,
            FlashbackPlaybackMaxAudioQueueDurationMsObserved = playbackSessionMetrics.MaxAudioQueueDurationMsObserved,
            FlashbackPlaybackMaxAbsAvDriftMsObserved = playbackSessionMetrics.MaxAbsAvDriftMsObserved,
            FlashbackPlaybackSubmitFailuresAtEnd = playbackSubmitFailuresAtEnd,
            FlashbackPlaybackSubmitFailuresDelta = playbackSessionMetrics.SubmitFailuresDelta,
            FlashbackPlaybackSegmentSwitchesAtEnd = playbackSegmentSwitchesAtEnd,
            FlashbackPlaybackFmp4ReopensAtEnd = playbackFmp4ReopensAtEnd,
            FlashbackPlaybackWriteHeadWaitsAtEnd = playbackWriteHeadWaitsAtEnd,
            FlashbackPlaybackNearLiveSnapsAtEnd = playbackNearLiveSnapsAtEnd,
            FlashbackPlaybackDecodeErrorSnapsAtEnd = playbackDecodeErrorSnapsAtEnd,
            FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd = playbackLastWriteHeadWaitGapMsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd = playbackSeekForwardDecodeCapHitsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsDelta = playbackSeekForwardDecodeCapHitsDelta,
            FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd = playbackLastSeekHitForwardDecodeCapAtEnd,
            FlashbackRecordingBackendObserved = recordingMetrics.BackendObserved,
            FlashbackRecordingFileGrowthObserved = recordingMetrics.FileGrowthObserved,
            FlashbackRecordingVideoFramesSubmittedDelta = recordingMetrics.VideoFramesSubmittedDelta,
            FlashbackRecordingVideoEncoderPacketsWrittenDelta = recordingMetrics.VideoEncoderPacketsWrittenDelta,
            FlashbackRecordingIntegritySequenceGapsAtEnd = recordingMetrics.IntegritySequenceGapsAtEnd,
            FlashbackRecordingIntegrityQueueDroppedFramesAtEnd = recordingMetrics.IntegrityQueueDroppedFramesAtEnd,
            FlashbackRecordingIntegritySequenceGapsDelta = recordingMetrics.IntegritySequenceGapsDelta,
            FlashbackRecordingIntegrityQueueDroppedFramesDelta = recordingMetrics.IntegrityQueueDroppedFramesDelta,
            FlashbackExportObserved = exportMetrics.Observed,
            FlashbackExportActiveAtEnd = exportMetrics.ActiveAtEnd,
            FlashbackExportStatusAtEnd = exportMetrics.StatusAtEnd,
            FlashbackExportMessageAtEnd = exportMetrics.MessageAtEnd,
            FlashbackExportFailureKindAtEnd = exportMetrics.FailureKindAtEnd,
            FlashbackExportOutputPathAtEnd = exportMetrics.OutputPathAtEnd,
            FlashbackExportForceRotateFallbacksAtEnd = flashbackExportForceRotateFallbacksAtEnd,
            FlashbackExportForceRotateFallbacksDelta = flashbackExportForceRotateFallbacksDelta,
            FlashbackExportLastForceRotateFallbackSegmentsAtEnd = flashbackExportLastForceRotateFallbackSegmentsAtEnd,
            LastExportIdAtEnd = exportMetrics.LastExportIdAtEnd,
            LastExportSuccessAtEnd = exportMetrics.LastSuccessAtEnd,
            LastExportMessageAtEnd = exportMetrics.LastMessageAtEnd,
            FlashbackExportMaxElapsedMsObserved = exportMetrics.MaxElapsedMsObserved,
            FlashbackExportMaxLastProgressAgeMsObserved = exportMetrics.MaxLastProgressAgeMsObserved,
            FlashbackExportMaxOutputBytesObserved = exportMetrics.MaxOutputBytesObserved,
            FlashbackExportMaxThroughputBytesPerSecObserved = exportMetrics.MaxThroughputBytesPerSecObserved,
            PreviewCadenceOnePercentLowFpsAtEnd = previewCadenceMetrics.OnePercentLowFpsAtEnd,
            PreviewCadenceMinOnePercentLowFpsObserved = previewCadenceMetrics.MinOnePercentLowFpsObserved,
            PreviewSchedulerDroppedAtEnd = previewSchedulerDroppedAtEnd,
            PreviewSchedulerDeadlineDropsAtEnd = previewSchedulerDeadlineDropsAtEnd,
            PreviewSchedulerClearedDropsAtEnd = previewSchedulerClearedDropsAtEnd,
            PreviewSchedulerUnderflowsAtEnd = previewSchedulerUnderflowsAtEnd,
            PreviewSchedulerResumeReprimesAtEnd = previewSchedulerResumeReprimesAtEnd,
            PreviewSchedulerDroppedDelta = previewSchedulerDroppedDelta,
            PreviewSchedulerDeadlineDropsDelta = previewSchedulerDeadlineDropsDelta,
            PreviewSchedulerClearedDropsDelta = previewSchedulerClearedDropsDelta,
            PreviewSchedulerUnderflowsDelta = previewSchedulerUnderflowsDelta,
            PreviewSchedulerResumeReprimesDelta = previewSchedulerResumeReprimesDelta,
            PreviewSchedulerLastDropReasonAtEnd = GetString(lastSnapshot, "MjpegPreviewJitterLastDropReason") ?? string.Empty,
            PreviewSchedulerLastUnderflowReasonAtEnd = GetString(lastSnapshot, "MjpegPreviewJitterLastUnderflowReason") ?? string.Empty,
            PreviewSchedulerLastUnderflowInputAgeMsAtEnd = GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs"),
            PreviewSchedulerLastUnderflowOutputAgeMsAtEnd = GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs"),
            PreviewSchedulerMaxScheduleLateMsObserved = previewSchedulerMaxScheduleLateMsObserved,
            PreviewSchedulerScheduleLateDelta = previewSchedulerScheduleLateDelta,
            PreviewD3DFrameStatsMissedRefreshDelta = previewD3DMetrics.MissedRefreshDelta,
            PreviewD3DFrameStatsFailureDelta = previewD3DMetrics.StatsFailureDelta,
            PreviewD3DMaxRecentSlowFramesObserved = previewD3DMetrics.MaxRecentSlowFramesObserved,
            PreviewD3DLatestSlowFrameReason = previewD3DMetrics.LatestSlowFrameReason,
            PreviewD3DLatestSlowFrameOverBudgetMs = previewD3DMetrics.LatestSlowFrameOverBudgetMs,
            PreviewD3DLatestSlowFramePresentIntervalMs = previewD3DMetrics.LatestSlowFramePresentIntervalMs,
            PreviewD3DLatestSlowFrameTotalFrameCpuMs = previewD3DMetrics.LatestSlowFrameTotalFrameCpuMs,
            PreviewD3DLatestSlowFramePresentCallMs = previewD3DMetrics.LatestSlowFramePresentCallMs,
            PreviewD3DLatestSlowFramePendingFrameCount = previewD3DMetrics.LatestSlowFramePendingFrameCount,
            PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DMetrics.InputUploadCpuP99MsAtEnd,
            PreviewD3DInputUploadCpuMaxMsObserved = previewD3DMetrics.InputUploadCpuMaxMsObserved,
            PreviewD3DRenderSubmitCpuP99MsAtEnd = previewD3DMetrics.RenderSubmitCpuP99MsAtEnd,
            PreviewD3DRenderSubmitCpuMaxMsObserved = previewD3DMetrics.RenderSubmitCpuMaxMsObserved,
            PreviewD3DPresentCallP99MsAtEnd = previewD3DMetrics.PresentCallP99MsAtEnd,
            PreviewD3DPresentCallMaxMsObserved = previewD3DMetrics.PresentCallMaxMsObserved,
            PreviewD3DTotalFrameCpuP99MsAtEnd = previewD3DMetrics.TotalFrameCpuP99MsAtEnd,
            PreviewD3DTotalFrameCpuMaxMsObserved = previewD3DMetrics.TotalFrameCpuMaxMsObserved,
            VisualCadenceOutputFpsAtEnd = visualCadenceMetrics.OutputFpsAtEnd,
            VisualCadenceChangeFpsAtEnd = visualCadenceMetrics.ChangeFpsAtEnd,
            VisualCadenceMinChangeFpsObserved = visualCadenceMetrics.MinChangeFpsObserved,
            VisualCadenceRepeatPercentAtEnd = visualCadenceMetrics.RepeatPercentAtEnd,
            VisualCadenceMaxRepeatPercentObserved = visualCadenceMetrics.MaxRepeatPercentObserved,
            VisualCadenceRepeatFramesAtEnd = visualCadenceMetrics.RepeatFramesAtEnd,
            VisualCadenceLongestRepeatRunAtEnd = visualCadenceMetrics.LongestRepeatRunAtEnd,
            ProcessCpuPercentAtEnd = GetDouble(lastSnapshot, "ProcessCpuPercent"),
            ProcessCpuMaxPercentObserved = processCpuMaxPercentObserved,
            RecordingVerificationRun = verification.HasValue,
            RecordingVerificationSucceeded = verificationSucceeded,
            RecordingVerificationMessage = verification.HasValue
                ? GetString(verification.Value, "Message") ?? string.Empty
                : null,
            PresentMon = presentMon,
            Actions = actions.ToArray(),
            Warnings = warnings.ToArray()
        };

        var summaryWritten = false;
        try
        {
            await WriteJsonAsync(summaryPath, result, CancellationToken.None).ConfigureAwait(false);
            summaryWritten = true;
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, "summary-write");
            completedUtc = DateTimeOffset.UtcNow;
            terminalState = GetTerminalState();
            result.Success = false;
            result.CompletedUtc = completedUtc;
            result.TerminalState = terminalState;
            result.LastStage = GetResultLastStage();
            result.UnhandledException = terminalException is null ? null : FormatTerminalException(terminalException);
            result.Warnings = warnings.ToArray();
        }

        if (summaryWritten)
        {
            SetStage("summary-written");
        }

        await WriteLiveStateBestEffortAsync(completedUtc, GetTerminalState()).ConfigureAwait(false);
        return result;

        void SetStage(string stage)
        {
            lastStage = stage;
        }

        void RecordTerminalException(Exception ex, string stage)
        {
            SetStage(stage);
            if (terminalException is null)
            {
                terminalException = ex;
                terminalExceptionStage = stage;
            }

            warnings.Add($"{stage}: {FormatTerminalException(ex)}");
        }

        static string FormatTerminalException(Exception ex)
        {
            return string.IsNullOrWhiteSpace(ex.Message)
                ? ex.GetType().Name
                : $"{ex.GetType().Name}: {ex.Message}";
        }

        string GetTerminalState()
        {
            if (terminalException is OperationCanceledException || cancellationToken.IsCancellationRequested)
            {
                return "canceled";
            }

            return terminalException is null ? "completed" : "failed";
        }

        string GetResultLastStage()
            => terminalExceptionStage ?? lastStage;

        async Task WriteArtifactBestEffortAsync<T>(string stage, string path, T value)
        {
            try
            {
                SetStage(stage);
                await WriteJsonAsync(path, value, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RecordTerminalException(ex, stage);
            }
        }

        async Task WriteLiveStateBestEffortAsync(DateTimeOffset? completedUtcOverride = null, string? terminalStateOverride = null)
        {
            try
            {
                var liveHealthSnapshot = samples.Count > 0
                    ? samples[^1].Snapshot
                    : initialSnapshot;
                await WriteJsonAsync(
                        livePath,
                        new
                        {
                            SessionId = sessionId,
                            Scenario = scenario,
                            StartedUtc = startedUtc,
                            UpdatedUtc = DateTimeOffset.UtcNow,
                            CompletedUtc = completedUtcOverride,
                            TerminalState = terminalStateOverride ?? (terminalException is null ? "running" : GetTerminalState()),
                            LastStage = terminalStateOverride is null ? lastStage : GetResultLastStage(),
                            RunnerProcessId = runnerProcessId,
                            OutputDirectory = outputDirectory,
                            SummaryPath = Path.Combine(outputDirectory, "summary.json"),
                            SampleCount = samples.Count,
                            HealthStatus = GetDiagnosticHealthStatus(liveHealthSnapshot),
                            LikelyStage = GetDiagnosticLikelyStage(liveHealthSnapshot),
                            CommandFailureCount = commandFailureCount,
                            WarningCount = warnings.Count,
                            LastWarning = warnings.Count > 0 ? warnings[^1] : string.Empty,
                            UnhandledException = terminalException is null ? null : FormatTerminalException(terminalException)
                        },
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch
            {
                // The live-state file is diagnostic breadcrumbs only.
            }
        }

        async Task WriteSamplingLiveStateBestEffortAsync()
        {
            var now = DateTimeOffset.UtcNow;
            if (now - lastSamplingLiveStateUtc < TimeSpan.FromSeconds(5))
            {
                return;
            }

            lastSamplingLiveStateUtc = now;
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        async Task<JsonElement> SendAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs,
            bool allowFailure = false)
            => await SendWithTokenAsync(command, payload, responseTimeoutMs, allowFailure, scenarioCancellationToken).ConfigureAwait(false);

        async Task<JsonElement> SendWithTokenAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs,
            bool allowFailure,
            CancellationToken commandCancellationToken)
        {
            await commandSendGate.WaitAsync(commandCancellationToken).ConfigureAwait(false);
            try
            {
                var response = await SendRawWithConnectRetryWithTokenAsync(command, payload, responseTimeoutMs, commandCancellationToken).ConfigureAwait(false);
                if (!AutomationSnapshotFormatter.IsSuccess(response) && !allowFailure)
                {
                    commandFailureCount++;
                    warnings.Add($"{command}: {AutomationSnapshotFormatter.Get(response, "Message", "command failed")}");
                }

                return response.Clone();
            }
            finally
            {
                commandSendGate.Release();
            }
        }

        async Task TryWaitAsync(string condition, int timeoutMs)
            => await TryWaitWithTokenAsync(condition, timeoutMs, scenarioCancellationToken).ConfigureAwait(false);

        async Task TryWaitWithTokenAsync(string condition, int timeoutMs, CancellationToken waitCancellationToken)
        {
            var response = await SendWithTokenAsync(
                    "WaitForCondition",
                    new Dictionary<string, object?>
                    {
                        ["condition"] = condition,
                        ["timeoutMs"] = timeoutMs,
                        ["pollMs"] = 250
                    },
                    timeoutMs + 2_000,
                    false,
                    waitCancellationToken)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                warnings.Add($"wait {condition}: {AutomationSnapshotFormatter.Get(response, "Message", "not met")}");
            }
        }

        static CancellationTokenSource CreateCleanupCts(TimeSpan timeout)
            => new(timeout);
        }
        finally
        {
            lockHandle.Dispose();
        }
    }

    public static string Format(DiagnosticSessionResult result)
    {
        return DiagnosticSessionResultFormatter.Format(result);
    }

}
