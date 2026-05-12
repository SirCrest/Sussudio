using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;
using static Sussudio.Tools.DiagnosticSessionSampler;

namespace Sussudio.Tools;

public static class DiagnosticSessionRunner
{
    // Scenario names and broad requirements live in DiagnosticSessionScenarios.
    // RunAsync reads like a phase plan: setup, optional background scenario
    // task, sampling loop, cleanup, then summary.
    private const int FlashbackStressMaxPlaybackPendingCommands = 4;
    private const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;
    private const double FlashbackStressPlaybackWarmSeconds = 10.0;
    private const long FlashbackStressAudioUnavailableFallbackAllowance = 4;
    private const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;
    private const double FlashbackDiagnosticWarmupFraction = 0.20;
    private const long FlashbackDiagnosticMaxWarmupMs = 10_000;

    private readonly record struct FlashbackSegmentProbe(
        int SequenceNumber,
        long StartPtsMs,
        long EndPtsMs,
        bool IsActive);

    private readonly record struct FlashbackSegmentPlaybackTarget(
        FlashbackSegmentProbe Segment,
        long ValidStartPtsMs,
        long BoundaryPositionMs,
        long BufferedDurationMs);

    private readonly record struct DiagnosticHealthObservation(
        string HealthStatus,
        string LikelyStage,
        string Evidence,
        long OffsetMs,
        int Severity);

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
        var runFlashbackPlayback = scenario == "flashback-playback";
        var runFlashbackStress = scenario == "flashback-stress";
        var runFlashbackScrubStress = scenario == "flashback-scrub-stress";
        var runFlashbackRestartCycle = scenario == "flashback-restart-cycle";
        var runFlashbackEncoderCycle = scenario == "flashback-encoder-cycle";
        var runFlashbackExportPlayback = scenario == "flashback-export-playback";
        var runFlashbackSegmentPlayback = scenario == "flashback-segment-playback";
        var runFlashbackRangeExport = scenario == "flashback-range-export";
        var runFlashbackRangeExportAudioSwitch = scenario == "flashback-range-export-audio-switch";
        var runFlashbackLifecycle = scenario == "flashback-lifecycle";
        var runFlashbackExportConcurrent = scenario == "flashback-export-concurrent";
        var runFlashbackDisableDuringExport = scenario == "flashback-disable-during-export";
        var runFlashbackRotatedExport = scenario == "flashback-rotated-export";
        var runFlashbackPreviewCycle = scenario == "flashback-preview-cycle";
        var runFlashbackPlaybackPreviewCycle = scenario == "flashback-playback-preview-cycle";
        var runFlashbackRecording = scenario == "flashback-recording";
        var runFlashbackRecordingPreviewCycle = scenario == "flashback-recording-preview-cycle";
        var runFlashbackRecordingSettingsDeferred = scenario == "flashback-recording-settings-deferred";
        var runFlashbackRecordingExportRejected = scenario == "flashback-recording-export-rejected";
        var runFlashbackExportRejected = scenario == "flashback-export-rejected";
        FlashbackRecordingSettingsDeferredPresetState flashbackRecordingSettingsDeferredPresetState = default;
        var commandSendGate = new SemaphoreSlim(1, 1);
        using var scenarioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scenarioCancellationToken = scenarioCts.Token;
        Task<PresentMonProbeResult>? presentMonTask = null;
        Task? flashbackStressTask = null;
        Task? flashbackLifecycleTask = null;
        Task? flashbackScrubStressTask = null;
        Task? flashbackRestartCycleTask = null;
        Task? flashbackEncoderCycleTask = null;
        Task? flashbackExportPlaybackTask = null;
        Task? flashbackSegmentPlaybackTask = null;
        Task? flashbackRangeExportTask = null;
        Task? flashbackRangeExportAudioSwitchTask = null;
        Task? flashbackExportConcurrentTask = null;
        Task? flashbackDisableDuringExportTask = null;
        Task? flashbackRotatedExportTask = null;
        Task? flashbackPreviewCycleTask = null;
        Task? flashbackPlaybackPreviewCycleTask = null;
        Task? flashbackRecordingPreviewCycleTask = null;
        Task<FlashbackRecordingSettingsDeferredPresetState>? flashbackRecordingSettingsDeferredTask = null;

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
            if (!initialSnapshotKnown && scenario != "observe")
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
                if ((runFlashbackRecording || runFlashbackRecordingPreviewCycle || runFlashbackRecordingSettingsDeferred || runFlashbackRecordingExportRejected) &&
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
                presentMonTask = PresentMonProbe.RunAsync(new PresentMonProbeOptions
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
                });
                actions.Add("presentmon capture started");
            }

            if (runFlashbackStress)
            {
                flashbackStressTask = RunFlashbackStressAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback stress started");
            }

            if (runFlashbackScrubStress)
            {
                flashbackScrubStressTask = RunFlashbackScrubStressAsync(
                    actions,
                    warnings,
                    SendRawWithConnectRetryAsync,
                    scenarioCancellationToken);
                actions.Add("flashback scrub stress started");
            }

            if (runFlashbackRestartCycle)
            {
                flashbackRestartCycleTask = RunFlashbackRestartCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback restart cycle started");
            }

            if (runFlashbackEncoderCycle)
            {
                flashbackEncoderCycleTask = RunFlashbackEncoderCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback encoder cycle started");
            }

            if (runFlashbackExportPlayback)
            {
                flashbackExportPlaybackTask = RunFlashbackExportPlaybackAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback export playback started");
            }

            if (runFlashbackSegmentPlayback)
            {
                flashbackSegmentPlaybackTask = RunFlashbackSegmentPlaybackAsync(
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback segment playback started");
            }

            if (runFlashbackRangeExport)
            {
                flashbackRangeExportTask = RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback range export started");
            }

            if (runFlashbackRangeExportAudioSwitch)
            {
                flashbackRangeExportAudioSwitchTask = RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    SendRawWithConnectRetryAsync,
                    scenarioCancellationToken,
                    scenarioLabel: "flashback range export audio switch",
                    exportFileName: "flashback-range-export-audio-switch.mp4",
                    outPointMs: 15_000,
                    switchAudioDuringExport: true);
                actions.Add("flashback range export audio switch started");
            }

            if (runFlashbackLifecycle)
            {
                flashbackLifecycleTask = RunFlashbackLifecycleAsync(
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback lifecycle started");
            }

            if (runFlashbackExportConcurrent)
            {
                flashbackExportConcurrentTask = RunFlashbackExportConcurrentAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    SendRawWithConnectRetryAsync,
                    scenarioCancellationToken);
                actions.Add("flashback concurrent export started");
            }

            if (runFlashbackDisableDuringExport)
            {
                flashbackDisableDuringExportTask = RunFlashbackDisableDuringExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    SendRawWithConnectRetryAsync,
                    scenarioCancellationToken);
                actions.Add("flashback disable during export started");
            }

            if (runFlashbackRotatedExport)
            {
                flashbackRotatedExportTask = RunFlashbackRotatedExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback rotated export started");
            }

            if (runFlashbackPreviewCycle)
            {
                flashbackPreviewCycleTask = RunFlashbackPreviewCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback preview cycle started");
            }

            if (runFlashbackPlaybackPreviewCycle)
            {
                flashbackPlaybackPreviewCycleTask = RunFlashbackPlaybackPreviewCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback playback preview cycle started");
            }

            if (runFlashbackRecordingPreviewCycle)
            {
                flashbackRecordingPreviewCycleTask = RunFlashbackRecordingPreviewCycleAsync(
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    scenarioCancellationToken);
                actions.Add("flashback recording preview cycle started");
            }

            if (runFlashbackRecordingSettingsDeferred)
            {
                flashbackRecordingSettingsDeferredTask = RunFlashbackRecordingSettingsDeferredAsync(
                    actions,
                    warnings,
                    (command, payload, timeoutMs, allowFailure) => SendAsync(command, payload, timeoutMs, allowFailure),
                    scenarioCancellationToken);
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

            if (flashbackStressTask is not null)
            {
                await flashbackStressTask.ConfigureAwait(false);
            }

            if (flashbackLifecycleTask is not null)
            {
                await flashbackLifecycleTask.ConfigureAwait(false);
            }

            if (flashbackScrubStressTask is not null)
            {
                await flashbackScrubStressTask.ConfigureAwait(false);
            }

            if (flashbackRestartCycleTask is not null)
            {
                await flashbackRestartCycleTask.ConfigureAwait(false);
            }

            if (flashbackEncoderCycleTask is not null)
            {
                await flashbackEncoderCycleTask.ConfigureAwait(false);
            }

            if (flashbackExportPlaybackTask is not null)
            {
                await flashbackExportPlaybackTask.ConfigureAwait(false);
            }

            if (flashbackSegmentPlaybackTask is not null)
            {
                await flashbackSegmentPlaybackTask.ConfigureAwait(false);
            }

            if (flashbackRangeExportTask is not null)
            {
                await flashbackRangeExportTask.ConfigureAwait(false);
            }

            if (flashbackRangeExportAudioSwitchTask is not null)
            {
                await flashbackRangeExportAudioSwitchTask.ConfigureAwait(false);
            }

            if (flashbackExportConcurrentTask is not null)
            {
                await flashbackExportConcurrentTask.ConfigureAwait(false);
            }

            if (flashbackDisableDuringExportTask is not null)
            {
                await flashbackDisableDuringExportTask.ConfigureAwait(false);
            }

            if (flashbackRotatedExportTask is not null)
            {
                await flashbackRotatedExportTask.ConfigureAwait(false);
            }

            if (flashbackPreviewCycleTask is not null)
            {
                await flashbackPreviewCycleTask.ConfigureAwait(false);
            }

            if (flashbackPlaybackPreviewCycleTask is not null)
            {
                await flashbackPlaybackPreviewCycleTask.ConfigureAwait(false);
            }

            if (flashbackRecordingPreviewCycleTask is not null)
            {
                await flashbackRecordingPreviewCycleTask.ConfigureAwait(false);
            }

            if (flashbackRecordingSettingsDeferredTask is not null)
            {
                flashbackRecordingSettingsDeferredPresetState = await flashbackRecordingSettingsDeferredTask.ConfigureAwait(false);
            }

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

            if (presentMonTask is not null)
            {
                presentMon = await presentMonTask.ConfigureAwait(false);
                if (!presentMon.Success)
                {
                    warnings.Add($"PresentMon failed: {presentMon.Message}");
                }
            }
            }
        }
        catch (Exception ex)
        {
            RecordTerminalException(ex, lastStage);
            scenarioCts.Cancel();
            await ObserveBackgroundTasksAfterFaultAsync().ConfigureAwait(false);
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

        if (runFlashbackRecording || runFlashbackRecordingPreviewCycle || runFlashbackRecordingSettingsDeferred || runFlashbackRecordingExportRejected)
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
        var isFlashbackScenario =
            runFlashbackPlayback ||
            runFlashbackStress ||
            runFlashbackScrubStress ||
            runFlashbackRestartCycle ||
            runFlashbackEncoderCycle ||
            runFlashbackExportPlayback ||
            runFlashbackSegmentPlayback ||
            runFlashbackRangeExport ||
            runFlashbackRangeExportAudioSwitch ||
            runFlashbackLifecycle ||
            runFlashbackExportConcurrent ||
            runFlashbackDisableDuringExport ||
            runFlashbackRotatedExport ||
            runFlashbackPreviewCycle ||
            runFlashbackPlaybackPreviewCycle ||
            runFlashbackRecording ||
            runFlashbackRecordingPreviewCycle ||
            runFlashbackRecordingSettingsDeferred ||
            runFlashbackRecordingExportRejected ||
            runFlashbackExportRejected ||
            scenario == "combined";
        ValidateCleanupLifecycleRestored(
            options.LeaveRunning,
            startedPreview,
            enabledFlashback,
            startedFlashbackPlayback,
            initialSnapshot,
            healthSnapshot,
            warnings);
        var toleratesSourceSignalHealthWarning =
            runFlashbackRangeExport ||
            runFlashbackRangeExportAudioSwitch ||
            runFlashbackExportConcurrent ||
            runFlashbackDisableDuringExport ||
            runFlashbackRotatedExport ||
            runFlashbackPreviewCycle ||
            runFlashbackPlaybackPreviewCycle;
        if (isFlashbackScenario)
        {
            var previewTargetFps = GetDouble(lastSnapshot, "ExpectedCaptureFrameRate");
            if (previewTargetFps <= 0)
            {
                previewTargetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
            }

            var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, previewTargetFps);
            var toleratesPreviewCycleSchedulerSettling =
                (runFlashbackPreviewCycle || runFlashbackPlaybackPreviewCycle || runFlashbackRecordingPreviewCycle) &&
                visualCadenceHealthy;
            var toleratesSparsePreviewSchedulerDeadlineDrops =
                IsSparsePreviewSchedulerDeadlineDropRun(
                    previewSchedulerDeadlineDropsDelta,
                    previewSchedulerUnderflowsDelta,
                    durationSeconds,
                    visualCadenceHealthy);
            var toleratesSparseScrubSchedulerTransitions =
                (runFlashbackScrubStress || runFlashbackSegmentPlayback) &&
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
        var toleratesFlashbackForceRotateDrainWarning =
            runFlashbackExportPlayback ||
            runFlashbackScrubStress ||
            runFlashbackRangeExport ||
            runFlashbackRangeExportAudioSwitch ||
            runFlashbackExportConcurrent ||
            runFlashbackDisableDuringExport ||
            runFlashbackRotatedExport;
        var diagnosticHealthTolerated =
            (toleratesSourceSignalHealthWarning &&
             IsSourceSignalDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (toleratesFlashbackForceRotateDrainWarning &&
             IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            sparseSourceCaptureCadenceWarning ||
            (isFlashbackScenario &&
              IsPreviewCycleScenario(runFlashbackPreviewCycle, runFlashbackPlaybackPreviewCycle, runFlashbackRecordingPreviewCycle) &&
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
                                              runFlashbackPreviewCycle || runFlashbackPlaybackPreviewCycle || runFlashbackRecordingPreviewCycle));

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

        async Task ObserveBackgroundTasksAfterFaultAsync()
        {
            SetStage("background-task-drain");
            await ObserveTaskAfterFaultAsync(flashbackStressTask, "flashback-stress-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackLifecycleTask, "flashback-lifecycle-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackScrubStressTask, "flashback-scrub-stress-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackRestartCycleTask, "flashback-restart-cycle-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackEncoderCycleTask, "flashback-encoder-cycle-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackExportPlaybackTask, "flashback-export-playback-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackSegmentPlaybackTask, "flashback-segment-playback-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackRangeExportTask, "flashback-range-export-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackRangeExportAudioSwitchTask, "flashback-range-export-audio-switch-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackExportConcurrentTask, "flashback-export-concurrent-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackDisableDuringExportTask, "flashback-disable-during-export-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackRotatedExportTask, "flashback-rotated-export-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackPreviewCycleTask, "flashback-preview-cycle-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackPlaybackPreviewCycleTask, "flashback-playback-preview-cycle-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackRecordingPreviewCycleTask, "flashback-recording-preview-cycle-task").ConfigureAwait(false);
            await ObservePresentMonTaskAfterFaultAsync().ConfigureAwait(false);
            await ObserveRecordingSettingsDeferredTaskAfterFaultAsync().ConfigureAwait(false);
            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        async Task ObserveTaskAfterFaultAsync(Task? task, string stage)
        {
            if (task is null || task.IsCompletedSuccessfully)
            {
                return;
            }

            try
            {
                var completedTask = task.IsCompleted
                    ? task
                    : await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
                if (!ReferenceEquals(completedTask, task))
                {
                    warnings.Add($"{stage}: task still running after diagnostic interruption");
                    return;
                }

                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RecordTerminalException(ex, stage);
            }
        }

        async Task ObservePresentMonTaskAfterFaultAsync()
        {
            if (presentMonTask is null || presentMonTask.IsCompletedSuccessfully)
            {
                if (presentMonTask is { IsCompletedSuccessfully: true } && presentMon is null)
                {
                    presentMon = await presentMonTask.ConfigureAwait(false);
                }

                return;
            }

            try
            {
                var completedTask = presentMonTask.IsCompleted
                    ? presentMonTask
                    : await Task.WhenAny(presentMonTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
                if (!ReferenceEquals(completedTask, presentMonTask))
                {
                    warnings.Add("presentmon-task: task still running after diagnostic interruption");
                    return;
                }

                presentMon = await presentMonTask.ConfigureAwait(false);
                if (!presentMon.Success)
                {
                    warnings.Add($"PresentMon failed: {presentMon.Message}");
                }
            }
            catch (Exception ex)
            {
                RecordTerminalException(ex, "presentmon-task");
            }
        }

        async Task ObserveRecordingSettingsDeferredTaskAfterFaultAsync()
        {
            if (flashbackRecordingSettingsDeferredTask is null || flashbackRecordingSettingsDeferredTask.IsCompletedSuccessfully)
            {
                if (flashbackRecordingSettingsDeferredTask is { IsCompletedSuccessfully: true })
                {
                    flashbackRecordingSettingsDeferredPresetState = await flashbackRecordingSettingsDeferredTask.ConfigureAwait(false);
                }

                return;
            }

            try
            {
                var completedTask = flashbackRecordingSettingsDeferredTask.IsCompleted
                    ? flashbackRecordingSettingsDeferredTask
                    : await Task.WhenAny(flashbackRecordingSettingsDeferredTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
                if (!ReferenceEquals(completedTask, flashbackRecordingSettingsDeferredTask))
                {
                    warnings.Add("flashback-recording-settings-deferred-task: task still running after diagnostic interruption");
                    return;
                }

                flashbackRecordingSettingsDeferredPresetState = await flashbackRecordingSettingsDeferredTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RecordTerminalException(ex, "flashback-recording-settings-deferred-task");
            }
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

    private static async Task RunFlashbackStressAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback stress: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback pause requested");

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 500 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback seek requested");

        foreach (var positionMs in new[] { 750, 1_250, 2_000, 3_250, 1_500 })
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            await sendCommandAsync(
                    "FlashbackAction",
                    new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = positionMs },
                    null)
                .ConfigureAwait(false);
        }
        actions.Add("flashback scrub burst requested");

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "play" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback play requested");

        var playbackBaselineSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        if (playbackBaselineSnapshot?.ValueKind != JsonValueKind.Object ||
            !string.Equals(
                GetString(playbackBaselineSnapshot.Value, "FlashbackPlaybackState"),
                "Playing",
                StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback stress: playback did not enter Playing before warm sample");
        }

        var warmBaselineSnapshot = playbackBaselineSnapshot?.ValueKind == JsonValueKind.Object
            ? playbackBaselineSnapshot.Value
            : baselineSnapshot;
        var baselineFrameCount = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var baselineAudioFallbacks = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackAudioMasterFallbacks") ?? 0;
        var baselineAudioUnavailableFallbacks = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks") ?? 0;
        var baselineAudioStaleFallbacks = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackAudioMasterStaleFallbacks") ?? 0;
        var baselineAudioDriftOutlierFallbacks = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks") ?? 0;
        var warmedPlaybackSnapshot = await WaitForFlashbackPlaybackWarmSampleAsync(
                sendCommandAsync,
                baselineFrameCount,
                FlashbackStressPlaybackWarmSeconds,
                TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (warmedPlaybackSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add($"flashback stress: playback did not warm for {FlashbackStressPlaybackWarmSeconds:0.#}s before go-live");
        }
        else
        {
            var warmedFrames = GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0;
            var warmedObservedFps = GetDouble(warmedPlaybackSnapshot.Value, "FlashbackPlaybackObservedFps");
            var warmedOnePercentLow = GetDouble(warmedPlaybackSnapshot.Value, "FlashbackPlaybackOnePercentLowFps");
            var warmedTargetFps = GetDouble(warmedPlaybackSnapshot.Value, "FlashbackPlaybackTargetFps");
            if (warmedTargetFps <= 0)
            {
                warmedTargetFps = GetDouble(warmedPlaybackSnapshot.Value, "SelectedExactFrameRate");
            }

            var warmedAudioFallbacks = GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackAudioMasterFallbacks") ?? 0;
            var warmedAudioFallbackDelta = Math.Max(0, warmedAudioFallbacks - baselineAudioFallbacks);
            var warmedAudioUnavailableDelta = Math.Max(
                0,
                (GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackAudioMasterUnavailableFallbacks") ?? 0) -
                baselineAudioUnavailableFallbacks);
            var warmedAudioStaleDelta = Math.Max(
                0,
                (GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackAudioMasterStaleFallbacks") ?? 0) -
                baselineAudioStaleFallbacks);
            var warmedAudioDriftOutlierDelta = Math.Max(
                0,
                (GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks") ?? 0) -
                baselineAudioDriftOutlierFallbacks);
            var warmedAudioLastFallbackReason = GetString(
                warmedPlaybackSnapshot.Value,
                "FlashbackPlaybackAudioMasterLastFallbackReason") ?? string.Empty;
            actions.Add(
                $"flashback playback warmed frames={Math.Max(0, warmedFrames - baselineFrameCount)} " +
                $"fps={warmedObservedFps:0.##} onePercentLow={warmedOnePercentLow:0.##} " +
                $"audioFallbackDelta={warmedAudioFallbackDelta} " +
                $"unavailableDelta={warmedAudioUnavailableDelta} " +
                $"staleDelta={warmedAudioStaleDelta} " +
                $"driftOutlierDelta={warmedAudioDriftOutlierDelta} " +
                $"lastAudioFallback={FormatOptional(warmedAudioLastFallbackReason)}");
            if (warmedTargetFps > 0)
            {
                var observedFloor = warmedTargetFps * 0.95;
                if (warmedObservedFps > 0 && warmedObservedFps < observedFloor)
                {
                    warnings.Add($"flashback stress: warmed playback observed FPS below floor fps={warmedObservedFps:0.##} floor={observedFloor:0.##}");
                }

                var onePercentLowFloor = warmedTargetFps * 0.80;
                if (warmedOnePercentLow > 0 && warmedOnePercentLow < onePercentLowFloor)
                {
                    warnings.Add($"flashback stress: warmed playback 1% low below floor fps={warmedOnePercentLow:0.##} floor={onePercentLowFloor:0.##}");
                }
            }

            var audioMasterFallbackWarning = ClassifyFlashbackStressAudioMasterFallbackWarning(
                warmedAudioFallbackDelta,
                warmedAudioUnavailableDelta,
                warmedAudioStaleDelta,
                warmedAudioDriftOutlierDelta);
            if (audioMasterFallbackWarning is { Length: > 0 })
            {
                warnings.Add(audioMasterFallbackWarning);
            }
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback go-live requested");

        var exportPath = Path.Combine(outputDirectory, "flashback-stress-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback stress export requested");

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(exportPath),
                    60_000)
                .ConfigureAwait(false);
            actions.Add("flashback stress export verified");
        }

        var drained = false;
        JsonElement lastSnapshot = default;
        var waitStarted = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(waitStarted) < TimeSpan.FromSeconds(10))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(snapshotResponse, out lastSnapshot) &&
                GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands") == 0 &&
                string.Equals(
                    GetString(lastSnapshot, "FlashbackPlaybackState"),
                    "Live",
                    StringComparison.OrdinalIgnoreCase))
            {
                drained = true;
                break;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        if (!drained)
        {
            warnings.Add(
                "flashback stress: playback command queue did not drain within 10s " +
                $"pending={GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands")} " +
                $"maxPending={GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands")} " +
                $"lastLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")} " +
                $"maxLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}");
        }

        if (lastSnapshot.ValueKind == JsonValueKind.Object)
        {
            var commandHealth = BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot);
            var state = GetString(lastSnapshot, "FlashbackPlaybackState") ?? "Unknown";
            var maxPending = GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands");
            var maxLatencyMs = GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs");
            if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0)
            {
                warnings.Add(
                    "flashback stress: " +
                    $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                    $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                    $"submitFailures={commandHealth.SubmitFailures}");
            }

            if (maxPending > FlashbackStressMaxPlaybackPendingCommands ||
                maxLatencyMs > FlashbackStressMaxPlaybackCommandLatencyMs)
            {
                warnings.Add(
                    "flashback stress: playback command latency exceeded threshold " +
                    $"maxPending={maxPending}/{FlashbackStressMaxPlaybackPendingCommands} " +
                    $"maxLatencyMs={maxLatencyMs}/{FlashbackStressMaxPlaybackCommandLatencyMs}");
            }

            if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"flashback stress: playback ended in state {state}");
            }
        }
    }

    private static string? ClassifyFlashbackStressAudioMasterFallbackWarning(
        long totalDelta,
        long unavailableDelta,
        long staleDelta,
        long driftOutlierDelta)
    {
        if (totalDelta <= 0)
        {
            return null;
        }

        if (staleDelta > 0 || driftOutlierDelta > 0)
        {
            return
                "flashback stress: audio-master harmful fallbacks increased during warmed playback " +
                $"staleDelta={staleDelta} driftOutlierDelta={driftOutlierDelta} " +
                $"totalDelta={totalDelta}";
        }

        if (unavailableDelta > FlashbackStressAudioUnavailableFallbackAllowance)
        {
            return
                "flashback stress: audio-master unavailable fallbacks exceeded startup allowance " +
                $"unavailableDelta={unavailableDelta} allowance={FlashbackStressAudioUnavailableFallbackAllowance} " +
                $"totalDelta={totalDelta}";
        }

        if (unavailableDelta <= 0)
        {
            return $"flashback stress: audio-master unclassified fallbacks increased during warmed playback delta={totalDelta}";
        }

        return null;
    }

    private static async Task RunFlashbackExportRejectedAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var exportPath = Path.Combine(outputDirectory, "flashback-rejected-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000,
                true)
            .ConfigureAwait(false);
        actions.Add("flashback rejected export requested");

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add("flashback export rejected: export unexpectedly succeeded while Flashback was inactive");
        }

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null, false).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot))
        {
            warnings.Add("flashback export rejected: no snapshot returned after rejected export");
            return;
        }

        var status = GetString(snapshot, "FlashbackExportStatus") ?? string.Empty;
        var message = GetString(snapshot, "FlashbackExportMessage") ?? string.Empty;
        var failureKind = GetString(snapshot, "FlashbackExportFailureKind") ?? string.Empty;
        var lastSuccess = GetString(snapshot, "LastExportSuccess") ?? string.Empty;
        actions.Add($"flashback rejected export observed status={status} kind={failureKind}");
        if (!string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export rejected: expected Failed status, got {status}");
        }

        if (!string.Equals(failureKind, "BufferInactive", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export rejected: expected BufferInactive failure kind, got {failureKind}");
        }

        if (!message.Contains("Flashback buffer not active", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export rejected: unexpected message '{message}'");
        }

        if (!string.Equals(lastSuccess, "false", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export rejected: expected LastExportSuccess=false, got {lastSuccess}");
        }
    }

    private static async Task RunFlashbackRecordingExportRejectedAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var readySnapshot = await WaitForFlashbackRecordingReadyAsync(
                (command, payload, timeoutMs) => sendCommandAsync(command, payload, timeoutMs, false),
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (readySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording export rejected: Flashback recording backend did not become ready");
            return;
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-recording-rejected-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000,
                true)
            .ConfigureAwait(false);
        actions.Add("flashback recording rejected export requested");

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add("flashback recording export rejected: export unexpectedly succeeded while Flashback recording backend was active");
        }

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null, false).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot))
        {
            warnings.Add("flashback recording export rejected: no snapshot returned after rejected export");
            return;
        }

        var status = GetString(snapshot, "FlashbackExportStatus") ?? string.Empty;
        var message = GetString(snapshot, "FlashbackExportMessage") ?? string.Empty;
        var failureKind = GetString(snapshot, "FlashbackExportFailureKind") ?? string.Empty;
        var lastSuccess = GetString(snapshot, "LastExportSuccess") ?? string.Empty;
        actions.Add($"flashback recording rejected export observed status={status} kind={failureKind}");
        if (!string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback recording export rejected: expected Failed status, got {status}");
        }

        if (!string.Equals(failureKind, "UnavailableDuringRecording", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback recording export rejected: expected UnavailableDuringRecording failure kind, got {failureKind}");
        }

        if (!message.Contains("Flashback export is unavailable while Flashback is the active recording backend", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback recording export rejected: unexpected message '{message}'");
        }

        if (!string.Equals(lastSuccess, "false", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback recording export rejected: expected LastExportSuccess=false, got {lastSuccess}");
        }

        if (!GetBool(snapshot, "IsRecording") ||
            !string.Equals(GetString(snapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording export rejected: recording backend changed after rejected export");
        }
    }

    private static async Task RunFlashbackExportConcurrentAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback concurrent export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var exportPathA = Path.Combine(outputDirectory, "flashback-concurrent-a.mp4");
        var exportPathB = Path.Combine(outputDirectory, "flashback-concurrent-b.mp4");
        // Diagnostic runs may execute against the same output directory across sessions;
        // pass force=true so the destination-exists guard does not break the diagnostic.
        var exportPayloadA = new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPathA, ["force"] = true };
        var exportPayloadB = new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPathB, ["force"] = true };

        var exportTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport");
        var exportTaskA = sendCommandAsync("FlashbackExport", exportPayloadA, exportTimeoutMs);
        var exportTaskB = sendCommandAsync("FlashbackExport", exportPayloadB, exportTimeoutMs);
        actions.Add("flashback concurrent export requests issued");

        var exportResponses = await Task.WhenAll(exportTaskA, exportTaskB).ConfigureAwait(false);
        for (var i = 0; i < exportResponses.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = exportResponses[i];
            var path = i == 0 ? exportPathA : exportPathB;
            var label = i == 0 ? "a" : "b";
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                warnings.Add(
                    $"flashback concurrent export {label}: {AutomationSnapshotFormatter.Get(response, "Message", "export failed")}");
                continue;
            }

            var verifyResponse = await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(path),
                    60_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
            {
                warnings.Add(
                    $"flashback concurrent export {label} verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            }
        }

        actions.Add("flashback concurrent exports verified");
    }

    private static async Task RunFlashbackDisableDuringExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback disable during export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-disable-during-export.mp4");
        var exportTask = sendCommandAsync(
            "FlashbackExport",
            new Dictionary<string, object?> { ["seconds"] = 3, ["outputPath"] = exportPath, ["force"] = true },
            AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport"));

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        var disableTask = SendCommandWithConnectRetryAsync(
            sendCommandAsync,
            "SetFlashbackEnabled",
            new Dictionary<string, object?> { ["enabled"] = false },
            305_000,
            TimeSpan.FromSeconds(30),
            cancellationToken);
        actions.Add("flashback disable/export requests issued");

        var exportResponse = await exportTask.ConfigureAwait(false);
        var disableResponse = await disableTask.ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback disable during export: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
        }

        if (disableResponse is null || !AutomationSnapshotFormatter.IsSuccess(disableResponse.Value))
        {
            var message = disableResponse is null
                ? "no response"
                : AutomationSnapshotFormatter.Get(disableResponse.Value, "Message", "unknown error");
            warnings.Add(
                $"flashback disable during export: disable failed - {message}");
        }

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            var verifyResponse = await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(exportPath),
                    60_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
            {
                warnings.Add(
                    $"flashback disable during export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            }
        }

        if (disableResponse.HasValue && AutomationSnapshotFormatter.IsSuccess(disableResponse.Value))
        {
            var inactiveSnapshot = await WaitForFlashbackActiveAsync(
                    sendCommandAsync,
                    expectedActive: false,
                    timeout: TimeSpan.FromSeconds(20),
                    cancellationToken)
                .ConfigureAwait(false);
            if (inactiveSnapshot?.ValueKind != JsonValueKind.Object)
            {
                warnings.Add("flashback disable during export: Flashback did not report inactive after disable");
            }
            else
            {
                if (GetBool(inactiveSnapshot.Value, "FlashbackPlaybackThreadAlive"))
                {
                    warnings.Add("flashback disable during export: playback worker still alive after disable");
                }

                if (GetInt(inactiveSnapshot.Value, "FlashbackPlaybackPendingCommands") > 0)
                {
                    warnings.Add(
                        "flashback disable during export: pending playback commands remained after disable " +
                        $"pending={GetInt(inactiveSnapshot.Value, "FlashbackPlaybackPendingCommands")}");
                }

                actions.Add("flashback disable during export verified");
            }
        }

        var enableResponse = await SendCommandWithConnectRetryAsync(
                sendCommandAsync,
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                305_000,
                TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        actions.Add("flashback re-enabled after disable/export");
        if (enableResponse is null || !AutomationSnapshotFormatter.IsSuccess(enableResponse.Value))
        {
            var message = enableResponse is null
                ? "no response"
                : AutomationSnapshotFormatter.Get(enableResponse.Value, "Message", "unknown error");
            warnings.Add(
                $"flashback disable during export: re-enable failed - {message}");
            return;
        }

        var activeSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        if (activeSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback disable during export: Flashback did not report active after re-enable");
        }
    }

    private static async Task RunFlashbackRotatedExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback rotated export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var completedSegment = await WaitForFlashbackCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(210),
                cancellationToken)
            .ConfigureAwait(false);
        if (completedSegment is null)
        {
            warnings.Add("flashback rotated export: no completed segment observed within 210s");
            return;
        }

        actions.Add(
            "flashback rotated segment observed " +
            $"seq={completedSegment.Value.SequenceNumber} " +
            $"startMs={completedSegment.Value.StartPtsMs} " +
            $"endMs={completedSegment.Value.EndPtsMs}");

        var exportPath = Path.Combine(outputDirectory, "flashback-rotated-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 180, ["outputPath"] = exportPath },
                300_000)
            .ConfigureAwait(false);
        actions.Add("flashback rotated export requested");

        var exportMessage = AutomationSnapshotFormatter.Get(exportResponse, "Message", string.Empty);
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback rotated export: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            return;
        }

        var exportedSegments = TryParseFlashbackExportSegmentCount(exportMessage);
        if (exportedSegments is null or < 2)
        {
            warnings.Add($"flashback rotated export: expected multi-segment export, got '{exportMessage}'");
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                120_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback rotated export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback rotated export verified");
    }

    private static int? TryParseFlashbackExportSegmentCount(string message)
    {
        const string marker = " from ";
        var markerIndex = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var digitsStart = markerIndex + marker.Length;
        while (digitsStart < message.Length && char.IsWhiteSpace(message[digitsStart]))
        {
            digitsStart++;
        }

        var digitsEnd = digitsStart;
        while (digitsEnd < message.Length && char.IsDigit(message[digitsEnd]))
        {
            digitsEnd++;
        }

        if (digitsEnd == digitsStart)
        {
            return null;
        }

        var suffix = message[digitsEnd..];
        if (!suffix.Contains("segment", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.TryParse(
            message.AsSpan(digitsStart, digitsEnd - digitsStart),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;
    }

    private static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath) =>
        new()
        {
            ["filePath"] = filePath,
            ["strict"] = true,
            ["verificationProfile"] = "flashback-export"
        };

    private static async Task RunFlashbackPreviewCycleAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback preview cycle: Flashback buffer did not become ready within 30s");
            return;
        }

        var beforeStopResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(beforeStopResponse, out var beforeStopSnapshot);
        var encodedBeforeStop = GetNullableLong(beforeStopSnapshot, "FlashbackEncodedFrames") ?? 0;

        var stopPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback preview cycle preview stopped");
        if (!AutomationSnapshotFormatter.IsSuccess(stopPreviewResponse))
        {
            warnings.Add(
                $"flashback preview cycle: preview stop failed - {AutomationSnapshotFormatter.Get(stopPreviewResponse, "Message", "unknown error")}");
            return;
        }

        var previewStoppedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStoppedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback preview cycle: preview did not report stopped");
            return;
        }

        if (!GetBool(previewStoppedSnapshot.Value, "FlashbackActive"))
        {
            warnings.Add("flashback preview cycle: Flashback became inactive when preview stopped");
            return;
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);
        var previewOffSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(previewOffSnapshotResponse, out var previewOffSnapshot))
        {
            warnings.Add("flashback preview cycle: no preview-off snapshot returned");
            return;
        }

        var encodedPreviewOff = GetNullableLong(previewOffSnapshot, "FlashbackEncodedFrames") ?? 0;
        if (!GetBool(previewOffSnapshot, "FlashbackActive"))
        {
            warnings.Add("flashback preview cycle: Flashback inactive while preview was off");
        }

        if (encodedPreviewOff <= encodedBeforeStop)
        {
            warnings.Add(
                "flashback preview cycle: Flashback frames did not advance while preview was off " +
                $"before={encodedBeforeStop} after={encodedPreviewOff}");
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-preview-off-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback preview cycle export while preview off requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback preview cycle: export while preview off failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
        }
        else
        {
            var verifyResponse = await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(exportPath),
                    60_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
            {
                warnings.Add(
                    $"flashback preview cycle export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            }
            else
            {
                actions.Add("flashback preview cycle export verified");
            }
        }

        var startPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback preview cycle preview restarted");
        if (!AutomationSnapshotFormatter.IsSuccess(startPreviewResponse))
        {
            warnings.Add(
                $"flashback preview cycle: preview restart failed - {AutomationSnapshotFormatter.Get(startPreviewResponse, "Message", "unknown error")}");
            return;
        }

        var previewStartedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStartedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback preview cycle: preview did not report active after restart");
            return;
        }

        if (!GetBool(previewStartedSnapshot.Value, "FlashbackActive"))
        {
            warnings.Add("flashback preview cycle: Flashback inactive after preview restart");
        }

        var framesFlowingResponse = await sendCommandAsync(
                "WaitForCondition",
                new Dictionary<string, object?>
                {
                    ["condition"] = "VideoFramesFlowing",
                    ["timeoutMs"] = 15_000,
                    ["pollMs"] = 250
                },
                17_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(framesFlowingResponse))
        {
            warnings.Add(
                $"flashback preview cycle: preview frames did not resume - {AutomationSnapshotFormatter.Get(framesFlowingResponse, "Message", "not met")}");
        }
    }

    private static async Task RunFlashbackPlaybackPreviewCycleAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback playback preview cycle: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        var playResponse = await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play", ["positionMs"] = 1000 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback playback preview cycle playback started");
        if (!AutomationSnapshotFormatter.IsSuccess(playResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: play command failed - {AutomationSnapshotFormatter.Get(playResponse, "Message", "unknown error")}");
            return;
        }

        var playingSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        if (playingSnapshot?.ValueKind != JsonValueKind.Object ||
            !string.Equals(GetString(playingSnapshot.Value, "FlashbackPlaybackState"), "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback playback preview cycle: playback did not report Playing before preview stop");
            return;
        }

        var playbackFrameCountBeforeStop = GetNullableLong(playingSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0;
        if (playbackFrameCountBeforeStop <= 0)
        {
            var warmSnapshot = await WaitForFlashbackPlaybackWarmSampleAsync(
                    sendCommandAsync,
                    playbackFrameCountBeforeStop,
                    0.25,
                    TimeSpan.FromSeconds(5),
                    cancellationToken)
                .ConfigureAwait(false);
            playbackFrameCountBeforeStop = warmSnapshot?.ValueKind == JsonValueKind.Object
                ? GetNullableLong(warmSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0
                : 0;
        }

        if (playbackFrameCountBeforeStop <= 0)
        {
            warnings.Add("flashback playback preview cycle: playback did not render frames before preview stop");
            return;
        }

        var stopPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback playback preview cycle preview stopped during playback");
        if (!AutomationSnapshotFormatter.IsSuccess(stopPreviewResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: preview stop failed - {AutomationSnapshotFormatter.Get(stopPreviewResponse, "Message", "unknown error")}");
            return;
        }

        var previewStoppedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStoppedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback playback preview cycle: preview did not report stopped");
            return;
        }

        if (!GetBool(previewStoppedSnapshot.Value, "FlashbackActive"))
        {
            warnings.Add("flashback playback preview cycle: Flashback became inactive when preview stopped");
            return;
        }

        var playbackStateAfterStop = GetString(previewStoppedSnapshot.Value, "FlashbackPlaybackState") ?? "Unknown";
        if (!string.Equals(playbackStateAfterStop, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback playback preview cycle: playback did not return live after preview stop state={playbackStateAfterStop}");
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-playback-preview-cycle.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback playback preview cycle export while preview off requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: export while preview off failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
        }
        else
        {
            var verifyResponse = await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(exportPath),
                    60_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
            {
                warnings.Add(
                    $"flashback playback preview cycle export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            }
            else
            {
                actions.Add("flashback playback preview cycle export verified");
            }
        }

        var startPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback playback preview cycle preview restarted");
        if (!AutomationSnapshotFormatter.IsSuccess(startPreviewResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: preview restart failed - {AutomationSnapshotFormatter.Get(startPreviewResponse, "Message", "unknown error")}");
            return;
        }

        var previewStartedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStartedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback playback preview cycle: preview did not report active after restart");
            return;
        }

        var framesFlowingResponse = await sendCommandAsync(
                "WaitForCondition",
                new Dictionary<string, object?>
                {
                    ["condition"] = "VideoFramesFlowing",
                    ["timeoutMs"] = 15_000,
                    ["pollMs"] = 250
                },
                17_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(framesFlowingResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: preview frames did not resume - {AutomationSnapshotFormatter.Get(framesFlowingResponse, "Message", "not met")}");
        }
    }

    private static async Task RunFlashbackRecordingPreviewCycleAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var recordingReadySnapshot = await WaitForFlashbackRecordingReadyAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (recordingReadySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording preview cycle: Flashback recording backend did not become ready");
            return;
        }

        var submittedBeforeStop = GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoFramesSubmittedToEncoder") ?? 0;
        var packetsBeforeStop = GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoEncoderPacketsWritten") ?? 0;

        var stopPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback recording preview cycle preview stopped");
        if (!AutomationSnapshotFormatter.IsSuccess(stopPreviewResponse))
        {
            warnings.Add(
                $"flashback recording preview cycle: preview stop failed - {AutomationSnapshotFormatter.Get(stopPreviewResponse, "Message", "unknown error")}");
            return;
        }

        var previewStoppedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStoppedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording preview cycle: preview did not report stopped");
            return;
        }

        if (!GetBool(previewStoppedSnapshot.Value, "IsRecording") ||
            !string.Equals(GetString(previewStoppedSnapshot.Value, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording preview cycle: Flashback recording backend stopped with preview");
            return;
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);
        var previewOffSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(previewOffSnapshotResponse, out var previewOffSnapshot))
        {
            warnings.Add("flashback recording preview cycle: no preview-off recording snapshot returned");
            return;
        }

        var submittedPreviewOff = GetNullableLong(previewOffSnapshot, "FlashbackVideoFramesSubmittedToEncoder") ?? 0;
        var packetsPreviewOff = GetNullableLong(previewOffSnapshot, "FlashbackVideoEncoderPacketsWritten") ?? 0;
        if (!GetBool(previewOffSnapshot, "IsRecording") ||
            !string.Equals(GetString(previewOffSnapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording preview cycle: recording inactive while preview was off");
        }

        if (submittedPreviewOff <= submittedBeforeStop || packetsPreviewOff <= packetsBeforeStop)
        {
            warnings.Add(
                "flashback recording preview cycle: recording counters did not advance while preview was off " +
                $"submitted={submittedBeforeStop}->{submittedPreviewOff} packets={packetsBeforeStop}->{packetsPreviewOff}");
        }

        var startPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback recording preview cycle preview restarted");
        if (!AutomationSnapshotFormatter.IsSuccess(startPreviewResponse))
        {
            warnings.Add(
                $"flashback recording preview cycle: preview restart failed - {AutomationSnapshotFormatter.Get(startPreviewResponse, "Message", "unknown error")}");
            return;
        }

        var previewStartedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStartedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording preview cycle: preview did not report active after restart");
            return;
        }

        if (!GetBool(previewStartedSnapshot.Value, "IsRecording") ||
            !string.Equals(GetString(previewStartedSnapshot.Value, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording preview cycle: Flashback recording backend inactive after preview restart");
        }

        var framesFlowingResponse = await sendCommandAsync(
                "WaitForCondition",
                new Dictionary<string, object?>
                {
                    ["condition"] = "VideoFramesFlowing",
                    ["timeoutMs"] = 15_000,
                    ["pollMs"] = 250
                },
                17_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(framesFlowingResponse))
        {
            warnings.Add(
                $"flashback recording preview cycle: preview frames did not resume - {AutomationSnapshotFormatter.Get(framesFlowingResponse, "Message", "not met")}");
        }
    }

    private readonly record struct FlashbackRecordingSettingsDeferredPresetState(
        string? OriginalPreset,
        string? DeferredPreset);

    private static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var recordingReadySnapshot = await WaitForFlashbackRecordingReadyAsync(
                (command, payload, timeoutMs) => sendCommandAsync(command, payload, timeoutMs, false),
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (recordingReadySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording settings deferred: Flashback recording backend did not become ready");
            return default;
        }

        var originalPreset = GetString(recordingReadySnapshot.Value, "SelectedPreset") ?? "P1";
        var cycledPreset = string.Equals(originalPreset, "P1", StringComparison.OrdinalIgnoreCase) ? "P2" : "P1";
        var presetState = new FlashbackRecordingSettingsDeferredPresetState(originalPreset, cycledPreset);
        var originalFilePath = GetString(recordingReadySnapshot.Value, "FlashbackFilePath") ?? string.Empty;
        var submittedBefore = GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoFramesSubmittedToEncoder") ?? 0;
        var packetsBefore = GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoEncoderPacketsWritten") ?? 0;

        var presetResponse = await sendCommandAsync(
                "SetPreset",
                new Dictionary<string, object?> { ["preset"] = cycledPreset },
                null,
                false)
            .ConfigureAwait(false);
        actions.Add($"flashback recording settings deferred preset changed to {cycledPreset}");
        if (!AutomationSnapshotFormatter.IsSuccess(presetResponse))
        {
            warnings.Add(
                $"flashback recording settings deferred: preset change failed - {AutomationSnapshotFormatter.Get(presetResponse, "Message", "unknown error")}");
            return presetState;
        }

        var restartResponse = await sendCommandAsync(
                "RestartFlashback",
                null,
                null,
                true)
            .ConfigureAwait(false);
        actions.Add("flashback recording settings deferred restart rejection requested");
        if (AutomationSnapshotFormatter.IsSuccess(restartResponse))
        {
            warnings.Add("flashback recording settings deferred: RestartFlashback unexpectedly succeeded during recording");
        }
        else
        {
            var message = AutomationSnapshotFormatter.Get(restartResponse, "Message", string.Empty);
            if (!message.Contains("recording", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"flashback recording settings deferred: restart rejection message did not mention recording - {message}");
            }
        }

        var disableResponse = await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                305_000,
                true)
            .ConfigureAwait(false);
        actions.Add("flashback recording settings deferred disable rejection requested");
        if (AutomationSnapshotFormatter.IsSuccess(disableResponse))
        {
            warnings.Add("flashback recording settings deferred: SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        }
        else
        {
            var message = AutomationSnapshotFormatter.Get(disableResponse, "Message", string.Empty);
            if (!message.Contains("recording", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"flashback recording settings deferred: disable rejection message did not mention recording - {message}");
            }
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);
        var afterResponse = await sendCommandAsync("GetSnapshot", null, null, false).ConfigureAwait(false);
        if (!TryGetSnapshot(afterResponse, out var afterSnapshot))
        {
            warnings.Add("flashback recording settings deferred: no post-mutation recording snapshot returned");
            return presetState;
        }

        if (!GetBool(afterSnapshot, "IsRecording") ||
            !string.Equals(GetString(afterSnapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording settings deferred: Flashback recording backend did not remain active after mutations");
        }

        var afterFilePath = GetString(afterSnapshot, "FlashbackFilePath") ?? string.Empty;
        if (!string.Equals(afterFilePath, originalFilePath, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording settings deferred: Flashback file path changed during recording settings deferral");
        }

        var submittedAfter = GetNullableLong(afterSnapshot, "FlashbackVideoFramesSubmittedToEncoder") ?? 0;
        var packetsAfter = GetNullableLong(afterSnapshot, "FlashbackVideoEncoderPacketsWritten") ?? 0;
        if (submittedAfter <= submittedBefore || packetsAfter <= packetsBefore)
        {
            warnings.Add(
                "flashback recording settings deferred: recording counters did not advance after mutation attempts " +
                $"submitted={submittedBefore}->{submittedAfter} packets={packetsBefore}->{packetsAfter}");
        }

        return presetState;
    }

    private static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(
        List<string> actions,
        List<string> warnings,
        FlashbackRecordingSettingsDeferredPresetState presetState,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presetState.DeferredPreset))
        {
            warnings.Add("flashback recording settings deferred: no expected preset was captured for post-stop verification");
            return;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback recording settings deferred: Flashback buffer did not become ready after recording stop");
            return;
        }

        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot))
        {
            warnings.Add("flashback recording settings deferred: no post-stop snapshot returned");
            return;
        }

        if (!string.Equals(GetString(snapshot, "SelectedPreset"), presetState.DeferredPreset, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                "flashback recording settings deferred: selected preset was not preserved after stop " +
                $"expected={presetState.DeferredPreset} actual={GetString(snapshot, "SelectedPreset") ?? "<null>"}");
        }

        if (!GetBool(snapshot, "FlashbackActive"))
        {
            warnings.Add("flashback recording settings deferred: Flashback inactive after recording stop");
            return;
        }

        if (GetNullableLong(snapshot, "FlashbackEncodedFrames") is not > 0)
        {
            warnings.Add("flashback recording settings deferred: post-stop Flashback encoder did not produce frames");
        }

        actions.Add("flashback recording settings deferred post-stop buffer verified");

        if (string.IsNullOrWhiteSpace(presetState.OriginalPreset) ||
            string.Equals(presetState.OriginalPreset, presetState.DeferredPreset, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var restoreResponse = await sendCommandAsync(
                "SetPreset",
                new Dictionary<string, object?> { ["preset"] = presetState.OriginalPreset },
                null)
            .ConfigureAwait(false);
        actions.Add($"flashback recording settings deferred preset restored to {presetState.OriginalPreset}");
        if (!AutomationSnapshotFormatter.IsSuccess(restoreResponse))
        {
            warnings.Add(
                $"flashback recording settings deferred: preset restore failed - {AutomationSnapshotFormatter.Get(restoreResponse, "Message", "unknown error")}");
            return;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback recording settings deferred: Flashback buffer did not become ready after preset restore");
            return;
        }

        var restoredSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(restoredSnapshotResponse, out var restoredSnapshot))
        {
            warnings.Add("flashback recording settings deferred: no post-restore snapshot returned");
            return;
        }

        if (!string.Equals(GetString(restoredSnapshot, "SelectedPreset"), presetState.OriginalPreset, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                "flashback recording settings deferred: selected preset was not restored " +
                $"expected={presetState.OriginalPreset} actual={GetString(restoredSnapshot, "SelectedPreset") ?? "<null>"}");
        }
    }

    private static async Task RunFlashbackScrubStressAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback scrub stress: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress pause requested");

        var beginResponse = await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "begin-scrub", ["positionMs"] = 500 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress begin requested");
        if (!AutomationSnapshotFormatter.IsSuccess(beginResponse))
        {
            warnings.Add($"flashback scrub stress: begin-scrub failed - {AutomationSnapshotFormatter.Get(beginResponse, "Message", "unknown error")}");
            return;
        }

        var scrubbingSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Scrubbing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        if (scrubbingSnapshot is null)
        {
            warnings.Add("flashback scrub stress: playback did not report Scrubbing within 5s");
        }

        var positions = new[]
        {
            250, 500, 750, 1_000, 1_250, 1_500, 1_750, 2_000,
            2_250, 2_500, 2_750, 3_000, 2_400, 1_800, 1_200, 600
        };
        var updateTasks = new Task<JsonElement>[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            updateTasks[i] = sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "update-scrub", ["positionMs"] = positions[i] },
                null);
        }

        var updateResponses = await Task.WhenAll(updateTasks).ConfigureAwait(false);
        actions.Add("flashback scrub stress update burst requested");
        var failedUpdates = 0;
        foreach (var response in updateResponses)
        {
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                failedUpdates++;
            }
        }

        if (failedUpdates > 0)
        {
            warnings.Add($"flashback scrub stress: {failedUpdates} update-scrub command(s) failed");
        }

        var endResponse = await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "end-scrub", ["positionMs"] = positions[^1] },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress end requested");
        if (!AutomationSnapshotFormatter.IsSuccess(endResponse))
        {
            warnings.Add($"flashback scrub stress: end-scrub failed - {AutomationSnapshotFormatter.Get(endResponse, "Message", "unknown error")}");
            return;
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "play" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress play requested");

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress go-live requested");

        var drained = false;
        JsonElement lastSnapshot = default;
        var waitStarted = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(waitStarted) < TimeSpan.FromSeconds(10))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(snapshotResponse, out lastSnapshot) &&
                GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands") == 0 &&
                string.Equals(
                    GetString(lastSnapshot, "FlashbackPlaybackState"),
                    "Live",
                    StringComparison.OrdinalIgnoreCase))
            {
                drained = true;
                break;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        if (!drained)
        {
            warnings.Add(
                "flashback scrub stress: playback did not settle live with an empty queue within 10s " +
                $"pending={GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands")} " +
                $"state={GetString(lastSnapshot, "FlashbackPlaybackState") ?? "Unknown"} " +
                $"threadAlive={GetBool(lastSnapshot, "FlashbackPlaybackThreadAlive")} " +
                $"maxPending={GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands")} " +
                $"lastLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")} " +
                $"maxLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")} " +
                $"maxLatencyCommand={FormatOptional(GetString(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand") ?? string.Empty)}");
            return;
        }

        var commandHealth = BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot);
        var state = GetString(lastSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        var maxPending = GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands");
        var maxLatencyMs = GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs");
        var maxLatencyCommand = GetString(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand") ?? string.Empty;

        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0)
        {
            warnings.Add(
                "flashback scrub stress: " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures}");
        }

        if (maxPending > FlashbackScrubStressMaxPlaybackPendingCommands ||
            maxLatencyMs > FlashbackStressMaxPlaybackCommandLatencyMs)
        {
            warnings.Add(
                "flashback scrub stress: playback command latency exceeded threshold " +
                $"maxPending={maxPending}/{FlashbackScrubStressMaxPlaybackPendingCommands} " +
                $"maxLatencyMs={maxLatencyMs}/{FlashbackStressMaxPlaybackCommandLatencyMs} " +
                $"maxLatencyCommand={FormatOptional(maxLatencyCommand)}");
        }

        if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback scrub stress: playback ended in state {state}");
        }
    }

    private static async Task RunFlashbackRestartCycleAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback restart cycle: Flashback buffer did not become ready before restart");
            return;
        }

        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "pause" },
                null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 750 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback restart cycle playback primed");

        var restartResponse = await sendCommandAsync("RestartFlashback", null, 305_000).ConfigureAwait(false);
        actions.Add("flashback restart requested");
        if (!AutomationSnapshotFormatter.IsSuccess(restartResponse))
        {
            warnings.Add($"flashback restart cycle: restart failed - {AutomationSnapshotFormatter.Get(restartResponse, "Message", "unknown error")}");
            return;
        }

        var activeSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        if (activeSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback restart cycle: Flashback did not report active after restart");
            return;
        }

        if (GetBool(activeSnapshot.Value, "FlashbackPlaybackThreadAlive"))
        {
            warnings.Add("flashback restart cycle: playback worker still alive after restart");
        }

        if (GetInt(activeSnapshot.Value, "FlashbackPlaybackPendingCommands") > 0)
        {
            warnings.Add(
                "flashback restart cycle: pending playback commands remained after restart " +
                $"pending={GetInt(activeSnapshot.Value, "FlashbackPlaybackPendingCommands")}");
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback restart cycle: Flashback buffer did not refill after restart");
            return;
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-restart-cycle-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback restart cycle export requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add($"flashback restart cycle: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            return;
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                60_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback restart cycle export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback restart cycle export verified");
    }

    private static async Task RunFlashbackEncoderCycleAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback encoder cycle: Flashback buffer did not become ready before preset change");
            return;
        }

        var beforeResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(beforeResponse, out var beforeSnapshot))
        {
            warnings.Add("flashback encoder cycle: no initial snapshot returned");
            return;
        }

        var originalPreset = GetString(beforeSnapshot, "SelectedPreset") ?? "P1";
        var cycledPreset = string.Equals(originalPreset, "P1", StringComparison.OrdinalIgnoreCase) ? "P2" : "P1";
        var originalFilePath = GetString(beforeSnapshot, "FlashbackFilePath") ?? string.Empty;

        try
        {
            var setResponse = await sendCommandAsync(
                    "SetPreset",
                    new Dictionary<string, object?> { ["preset"] = cycledPreset },
                    null)
                .ConfigureAwait(false);
            actions.Add($"flashback encoder preset changed to {cycledPreset}");
            if (!AutomationSnapshotFormatter.IsSuccess(setResponse))
            {
                warnings.Add($"flashback encoder cycle: preset change failed - {AutomationSnapshotFormatter.Get(setResponse, "Message", "unknown error")}");
                return;
            }

            if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
            {
                warnings.Add("flashback encoder cycle: Flashback buffer did not become ready after preset change");
                return;
            }

            var afterResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (!TryGetSnapshot(afterResponse, out var afterSnapshot))
            {
                warnings.Add("flashback encoder cycle: no post-cycle snapshot returned");
                return;
            }

            var framesAfter = GetNullableLong(afterSnapshot, "FlashbackEncodedFrames") ?? 0;
            if (framesAfter < 240)
            {
                warnings.Add($"flashback encoder cycle: post-cycle encoder did not reach readiness frame count frames={framesAfter}");
            }

            var afterFilePath = GetString(afterSnapshot, "FlashbackFilePath") ?? string.Empty;
            if (string.Equals(afterFilePath, originalFilePath, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("flashback encoder cycle: Flashback file path did not change after preset cycle");
            }

            if (GetInt(afterSnapshot, "FlashbackPlaybackPendingCommands") > 0 ||
                GetBool(afterSnapshot, "FlashbackPlaybackThreadAlive"))
            {
                warnings.Add(
                    "flashback encoder cycle: playback state not clean after preset cycle " +
                    $"pending={GetInt(afterSnapshot, "FlashbackPlaybackPendingCommands")} " +
                    $"threadAlive={GetBool(afterSnapshot, "FlashbackPlaybackThreadAlive")}");
            }

            var exportPath = Path.Combine(outputDirectory, "flashback-encoder-cycle-export.mp4");
            var exportResponse = await sendCommandAsync(
                    "FlashbackExport",
                    new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                    60_000)
                .ConfigureAwait(false);
            actions.Add("flashback encoder cycle export requested");
            if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
            {
                warnings.Add($"flashback encoder cycle: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
                return;
            }

            var verifyResponse = await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(exportPath),
                    60_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
            {
                warnings.Add(
                    $"flashback encoder cycle export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
                return;
            }

            actions.Add("flashback encoder cycle export verified");
        }
        finally
        {
            var restoreResponse = await sendCommandAsync(
                    "SetPreset",
                    new Dictionary<string, object?> { ["preset"] = originalPreset },
                    null)
                .ConfigureAwait(false);
            actions.Add($"flashback encoder preset restored to {originalPreset}");
            if (!AutomationSnapshotFormatter.IsSuccess(restoreResponse))
            {
                warnings.Add($"flashback encoder cycle: preset restore failed - {AutomationSnapshotFormatter.Get(restoreResponse, "Message", "unknown error")}");
            }
            else if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
            {
                warnings.Add("flashback encoder cycle: Flashback buffer did not become ready after preset restore");
            }
        }
    }

    private static async Task RunFlashbackExportPlaybackAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback export playback: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 1_000 },
                null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "play" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback export playback play requested");

        var playbackSnapshotOrNull = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        JsonElement playbackSnapshot;
        if (playbackSnapshotOrNull is null)
        {
            warnings.Add("flashback export playback: playback did not report Playing within 5s before export");
            var playbackSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            TryGetSnapshot(playbackSnapshotResponse, out var fallbackPlaybackSnapshot);
            playbackSnapshot = fallbackPlaybackSnapshot;
        }
        else
        {
            playbackSnapshot = playbackSnapshotOrNull.Value;
        }

        var playbackFrameCountBeforeExport = GetNullableLong(playbackSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var playbackStateBeforeExport = GetString(playbackSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (!string.Equals(playbackStateBeforeExport, "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export playback: expected Playing before export, got {playbackStateBeforeExport}");
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-export-playback.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback export during playback requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add($"flashback export playback: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            return;
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                60_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback export playback verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback export during playback verified");

        var postExportSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(postExportSnapshotResponse, out var postExportSnapshot);
        var playbackFrameCountAfterExport = GetNullableLong(postExportSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var playbackStateAfterExport = GetString(postExportSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (!string.Equals(playbackStateAfterExport, "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export playback: expected Playing after export, got {playbackStateAfterExport}");
        }

        if (playbackFrameCountAfterExport <= playbackFrameCountBeforeExport)
        {
            warnings.Add(
                "flashback export playback: playback frame count did not advance during export " +
                $"before={playbackFrameCountBeforeExport} after={playbackFrameCountAfterExport}");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback export playback go-live requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        var finalSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot))
        {
            warnings.Add("flashback export playback: no final snapshot returned");
            return;
        }

        var commandHealth = BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot);
        var pending = GetInt(finalSnapshot, "FlashbackPlaybackPendingCommands");
        var state = GetString(finalSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0)
        {
            warnings.Add(
                "flashback export playback: " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures}");
        }

        if (pending > 0)
        {
            warnings.Add($"flashback export playback: pending commands remained after go-live pending={pending}");
        }

        if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export playback: playback ended in state {state}");
        }
    }

    private static async Task RunFlashbackSegmentPlaybackAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback segment playback: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);

        var playbackTarget = await WaitForFlashbackPlayableCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);

        if (playbackTarget is null)
        {
            var rotationOk = await CreateFlashbackCompletedSegmentViaRecordingAsync(
                    actions,
                    warnings,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!rotationOk)
            {
                return;
            }

            playbackTarget = await WaitForFlashbackPlayableCompletedSegmentAsync(
                    sendCommandAsync,
                    TimeSpan.FromSeconds(20),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (playbackTarget is null)
        {
            warnings.Add("flashback segment playback: no playable completed segment became available after recording-assisted rotation");
            return;
        }

        var target = playbackTarget.Value;
        var completedSegment = target.Segment;
        actions.Add(
            "flashback segment playback live headroom established " +
            $"validStartMs={target.ValidStartPtsMs} boundaryPosMs={target.BoundaryPositionMs} " +
            $"bufferedMs={target.BufferedDurationMs}");

        var seekPositionMs = Math.Max(0, target.BoundaryPositionMs - 500);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "pause" },
                null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = seekPositionMs },
                null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play" },
                null)
            .ConfigureAwait(false);
        actions.Add(
            "flashback segment playback started near boundary " +
            $"segment={completedSegment.SequenceNumber} seekMs={seekPositionMs} " +
            $"boundaryPosMs={target.BoundaryPositionMs} endMs={completedSegment.EndPtsMs}");

        var playbackSnapshot = await WaitForFlashbackPlaybackBoundaryCrossAsync(
                sendCommandAsync,
                target.BoundaryPositionMs,
                TimeSpan.FromSeconds(35),
                cancellationToken)
            .ConfigureAwait(false);
        if (playbackSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback segment playback: no playback snapshot returned");
            return;
        }

        var state = GetString(playbackSnapshot.Value, "FlashbackPlaybackState") ?? "Unknown";
        var positionMs = GetNullableLong(playbackSnapshot.Value, "FlashbackPlaybackPositionMs") ?? 0;
        var frameCount = GetNullableLong(playbackSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0;
        var observedFps = GetDouble(playbackSnapshot.Value, "FlashbackPlaybackObservedFps");
        var lateFrames = GetNullableLong(playbackSnapshot.Value, "FlashbackPlaybackLateFrames") ?? 0;
        var commandHealth = BuildPlaybackCommandHealth(playbackSnapshot.Value, baselineSnapshot);
        var pending = GetInt(playbackSnapshot.Value, "FlashbackPlaybackPendingCommands");
        actions.Add(
            "flashback segment playback observed " +
            $"positionMs={positionMs} frames={frameCount} late={lateFrames} fps={observedFps:0.##}");

        if (!string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback segment playback: expected Playing after boundary playback, got {state}");
        }

        if (positionMs < target.BoundaryPositionMs + 250)
        {
            warnings.Add(
                "flashback segment playback: playback position did not cross completed segment boundary " +
                $"positionMs={positionMs} boundaryMs={target.BoundaryPositionMs} " +
                $"absoluteBoundaryMs={completedSegment.EndPtsMs} validStartMs={target.ValidStartPtsMs}");
        }

        var targetFps = GetDouble(playbackSnapshot.Value, "DetectedSourceFrameRate");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(playbackSnapshot.Value, "SelectedFrameRate");
        }

        if (frameCount <= 0)
        {
            warnings.Add(
                "flashback segment playback: playback frames did not advance " +
                $"frames={frameCount} observedFps={observedFps:0.##}");
        }
        else if (frameCount >= 120 && observedFps <= 1)
        {
            warnings.Add(
                "flashback segment playback: playback FPS did not warm after enough frames " +
                $"frames={frameCount} observedFps={observedFps:0.##}");
        }
        else if (targetFps >= 100 && frameCount >= 180 && observedFps < targetFps * 0.85)
        {
            warnings.Add(
                "flashback segment playback: playback FPS below source-rate target after warm sample " +
                $"frames={frameCount} observedFps={observedFps:0.##} targetFps={targetFps:0.##}");
        }

        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0 || pending > 0)
        {
            warnings.Add(
                "flashback segment playback: command queue unhealthy " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures} pending={pending}");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback segment playback go-live requested");

        var finalSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Live",
                TimeSpan.FromSeconds(3),
                cancellationToken)
            .ConfigureAwait(false);
        if (finalSnapshot?.ValueKind == JsonValueKind.Object)
        {
            var finalState = GetString(finalSnapshot.Value, "FlashbackPlaybackState") ?? "Unknown";
            if (!string.Equals(finalState, "Live", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"flashback segment playback: playback ended in state {finalState}");
            }
        }
    }

    private static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        long boundaryMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        JsonElement? lastSnapshot = null;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                lastSnapshot = snapshot;
                var positionMs = GetNullableLong(snapshot, "FlashbackPlaybackPositionMs") ?? 0;
                var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
                var pending = GetInt(snapshot, "FlashbackPlaybackPendingCommands");
                var state = GetString(snapshot, "FlashbackPlaybackState") ?? "Unknown";
                if (positionMs >= boundaryMs + 1_500 &&
                    frameCount >= 180 &&
                    pending == 0 &&
                    string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
                {
                    return snapshot;
                }
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return lastSnapshot;
    }

    private static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        const int requiredHeadroomMs = 8_000;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var segmentsResponse = await sendCommandAsync("FlashbackGetSegments", null, null).ConfigureAwait(false);
            var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetFlashbackSegments(segmentsResponse, out var segments) &&
                TryGetSnapshot(snapshotResponse, out var snapshot))
            {
                var bufferedDurationMs = GetNullableLong(snapshot, "FlashbackBufferedDurationMs") ?? 0;
                var latestPtsMs = segments.Count > 0
                    ? segments.Max(segment => segment.EndPtsMs)
                    : 0;
                var validStartPtsMs = Math.Max(0, latestPtsMs - bufferedDurationMs);
                var completed = segments
                    .Where(segment => !segment.IsActive && segment.EndPtsMs > segment.StartPtsMs)
                    .Select(segment => new
                    {
                        Segment = segment,
                        BoundaryPositionMs = Math.Max(0, segment.EndPtsMs - validStartPtsMs)
                    })
                    .Where(candidate =>
                        candidate.BoundaryPositionMs > 0 &&
                        candidate.BoundaryPositionMs + requiredHeadroomMs <= bufferedDurationMs)
                    .OrderByDescending(candidate => candidate.Segment.EndPtsMs)
                    .FirstOrDefault();
                if (completed is not null)
                {
                    return new FlashbackSegmentPlaybackTarget(
                        completed.Segment,
                        validStartPtsMs,
                        completed.BoundaryPositionMs,
                        bufferedDurationMs);
                }
            }

            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        long boundaryMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        const int requiredHeadroomMs = 8_000;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                var bufferedDurationMs = GetNullableLong(snapshot, "FlashbackBufferedDurationMs") ?? 0;
                if (bufferedDurationMs >= boundaryMs + requiredHeadroomMs)
                {
                    return true;
                }
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        string expectedState,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        JsonElement? lastSnapshot = null;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                lastSnapshot = snapshot;
                var state = GetString(snapshot, "FlashbackPlaybackState") ?? "Unknown";
                if (string.Equals(state, expectedState, StringComparison.OrdinalIgnoreCase))
                {
                    return snapshot;
                }
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return lastSnapshot;
    }

    private static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        long baselineFrameCount,
        double minimumSeconds,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        JsonElement? lastSnapshot = null;
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                lastSnapshot = snapshot;
                var state = GetString(snapshot, "FlashbackPlaybackState") ?? "Unknown";
                var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
                var sessionFrameCount = frameCount >= baselineFrameCount
                    ? frameCount - baselineFrameCount
                    : frameCount;
                var targetFps = GetDouble(snapshot, "FlashbackPlaybackTargetFps");
                if (targetFps <= 0)
                {
                    targetFps = GetDouble(snapshot, "SelectedExactFrameRate");
                }

                var minimumFrames = Math.Max(
                    240,
                    targetFps > 0
                        ? (long)Math.Ceiling(targetFps * minimumSeconds)
                        : 240);
                if (sessionFrameCount >= minimumFrames &&
                    string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
                {
                    return snapshot;
                }
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return lastSnapshot;
    }

    private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var startResponse = await sendCommandAsync(
                "SetRecordingEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback segment playback recording-assisted rotation started");
        if (!AutomationSnapshotFormatter.IsSuccess(startResponse))
        {
            warnings.Add(
                $"flashback segment playback: recording-assisted start failed - {AutomationSnapshotFormatter.Get(startResponse, "Message", "unknown error")}");
            return false;
        }

        var readySnapshot = await WaitForFlashbackRecordingReadyAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (readySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback segment playback: recording-assisted Flashback backend did not become ready");
            await TryStopRecordingAsync(sendCommandAsync).ConfigureAwait(false);
            return false;
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);

        var stopResponse = await sendCommandAsync(
                "SetRecordingEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback segment playback recording-assisted rotation stopped");
        if (!AutomationSnapshotFormatter.IsSuccess(stopResponse))
        {
            warnings.Add(
                $"flashback segment playback: recording-assisted stop failed - {AutomationSnapshotFormatter.Get(stopResponse, "Message", "unknown error")}");
            return false;
        }

        var stoppedResponse = await sendCommandAsync(
                "WaitForCondition",
                new Dictionary<string, object?>
                {
                    ["condition"] = "RecordingStopped",
                    ["timeoutMs"] = 30_000,
                    ["pollMs"] = 250
                },
                32_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(stoppedResponse))
        {
            warnings.Add(
                $"flashback segment playback: recording-assisted stop did not settle - {AutomationSnapshotFormatter.Get(stoppedResponse, "Message", "not met")}");
            return false;
        }

        return true;
    }

    private static async Task TryStopRecordingAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        try
        {
            await sendCommandAsync(
                    "SetRecordingEnabled",
                    new Dictionary<string, object?> { ["enabled"] = false },
                    null)
                .ConfigureAwait(false);
        }
        catch
        {
            // Best-effort cleanup for diagnostics; the caller records the primary warning.
        }
    }

    private static async Task RunFlashbackRangeExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken,
        string scenarioLabel = "flashback range export",
        string exportFileName = "flashback-range-export.mp4",
        int outPointMs = 5_000,
        bool switchAudioDuringExport = false)
    {
        const int liveEdgeSafetyMarginMs = 5_000;
        const int leftEdgeSafetyMarginMs = 10_000;
        var requiredBufferedDurationMs = Math.Max(
            20_000,
            outPointMs + liveEdgeSafetyMarginMs + leftEdgeSafetyMarginMs);
        var requiredEncodedFrames = Math.Max(240, (long)Math.Ceiling(requiredBufferedDurationMs / 1000.0 * 60.0));
        if (!await WaitForFlashbackStressBufferReadyAsync(
                sendCommandAsync,
                cancellationToken,
                requiredBufferedDurationMs,
                requiredEncodedFrames,
                TimeSpan.FromSeconds(45)).ConfigureAwait(false))
        {
            warnings.Add(
                $"{scenarioLabel}: Flashback buffer did not become range-ready " +
                $"within 45s bufferedMs>={requiredBufferedDurationMs} encodedFrames>={requiredEncodedFrames}");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);
        var bufferedDurationMs = GetNullableLong(baselineSnapshot, "FlashbackBufferedDurationMs") ?? 0;
        var rangeEndMs = (int)Math.Clamp(bufferedDurationMs - liveEdgeSafetyMarginMs, 0, int.MaxValue);
        var rangeStartMs = Math.Max(0, rangeEndMs - outPointMs);
        if (rangeStartMs < leftEdgeSafetyMarginMs)
        {
            warnings.Add(
                $"{scenarioLabel}: insufficient near-live range headroom " +
                $"bufferedMs={bufferedDurationMs} startMs={rangeStartMs} endMs={rangeEndMs} " +
                $"requiredStartMs>={leftEdgeSafetyMarginMs}");
            return;
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = rangeStartMs },
                null)
            .ConfigureAwait(false);
        if (!await WaitForFlashbackPlaybackPositionAsync(sendCommandAsync, rangeStartMs, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            warnings.Add($"{scenarioLabel}: playback did not reach in-point seek before marking range");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "set-in-point" }, null)
            .ConfigureAwait(false);
        actions.Add($"{scenarioLabel} in point set positionMs={rangeStartMs}");

        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = rangeEndMs },
                null)
            .ConfigureAwait(false);
        if (!await WaitForFlashbackPlaybackPositionAsync(sendCommandAsync, rangeEndMs, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            warnings.Add($"{scenarioLabel}: playback did not reach out-point seek before marking range");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "set-out-point" }, null)
            .ConfigureAwait(false);
        actions.Add($"{scenarioLabel} out point set positionMs={rangeEndMs}");

        var exportPath = Path.Combine(outputDirectory, exportFileName);
        var exportTask = sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?>
                {
                    ["seconds"] = 1,
                    ["outputPath"] = exportPath,
                    ["useSelectionRange"] = true,
                    ["force"] = true
                },
                60_000)
            ;
        Task? audioSwitchTask = null;
        if (switchAudioDuringExport)
        {
            audioSwitchTask = ToggleAudioEnabledDuringFlashbackExportAsync(
                exportTask,
                baselineSnapshot,
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken);
        }

        var exportResponse = await exportTask.ConfigureAwait(false);
        if (audioSwitchTask is not null)
        {
            await audioSwitchTask.ConfigureAwait(false);
        }

        actions.Add($"{scenarioLabel} requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add($"{scenarioLabel}: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
            return;
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                60_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"{scenarioLabel} verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
        }
        else
        {
            actions.Add($"{scenarioLabel} verified");
        }

        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot))
        {
            warnings.Add($"{scenarioLabel}: no snapshot returned after export");
            await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
            return;
        }

        var inPointMs = GetNullableLong(snapshot, "FlashbackExportInPointMs") ?? 0;
        var markedOutPointMs = GetNullableLong(snapshot, "FlashbackExportOutPointMs") ?? 0;
        var exportedDurationMs = markedOutPointMs - inPointMs;
        var expectedDurationMinMs = Math.Max(0, outPointMs - 1_000);
        var expectedDurationMaxMs = outPointMs + 2_000;
        if (exportedDurationMs < expectedDurationMinMs || exportedDurationMs > expectedDurationMaxMs)
        {
            warnings.Add(
                $"{scenarioLabel}: selected export duration outside expected range " +
                $"in={inPointMs} out={markedOutPointMs} duration={exportedDurationMs} " +
                $"expected={expectedDurationMinMs}-{expectedDurationMaxMs}");
        }

        var status = GetString(snapshot, "FlashbackExportStatus") ?? "Unknown";
        if (!string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"{scenarioLabel}: expected Succeeded status, got {status}");
        }

        await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
        actions.Add($"{scenarioLabel} cleared range and went live");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        var finalSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot))
        {
            warnings.Add($"{scenarioLabel}: no final snapshot returned");
            return;
        }

        var pending = GetInt(finalSnapshot, "FlashbackPlaybackPendingCommands");
        var commandHealth = BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot);
        var state = GetString(finalSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (pending > 0)
        {
            warnings.Add($"{scenarioLabel}: pending commands remained after go-live pending={pending}");
        }

        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0)
        {
            warnings.Add(
                $"{scenarioLabel}: " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures}");
        }

        if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"{scenarioLabel}: playback ended in state {state}");
        }
    }

    private static async Task ToggleAudioEnabledDuringFlashbackExportAsync(
        Task<JsonElement> exportTask,
        JsonElement baselineSnapshot,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var baselineAudioEnabled = GetBool(baselineSnapshot, "IsAudioEnabled");
        var toggledAudioEnabled = !baselineAudioEnabled;
        var exportRequestOutstandingBeforeToggle = false;

        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            exportRequestOutstandingBeforeToggle = !exportTask.IsCompleted;
            if (exportRequestOutstandingBeforeToggle)
            {
                actions.Add("flashback range export audio switch confirmed export command outstanding before audio toggle");
            }
            else
            {
                warnings.Add("flashback range export audio switch: export completed before audio toggle");
            }

            var toggleResponse = await sendCommandAsync(
                    "SetAudioEnabled",
                    new Dictionary<string, object?> { ["enabled"] = toggledAudioEnabled },
                    10_000)
                .ConfigureAwait(false);
            if (AutomationSnapshotFormatter.IsSuccess(toggleResponse))
            {
                actions.Add($"flashback range export audio switch toggled audio enabled to {toggledAudioEnabled}");
            }
            else
            {
                warnings.Add(
                    "flashback range export audio switch: audio toggle failed - " +
                    AutomationSnapshotFormatter.Get(toggleResponse, "Message", "unknown error"));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            warnings.Add($"flashback range export audio switch: audio toggle threw {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                var restoreResponse = await sendCommandAsync(
                        "SetAudioEnabled",
                        new Dictionary<string, object?> { ["enabled"] = baselineAudioEnabled },
                        10_000)
                    .ConfigureAwait(false);
                if (AutomationSnapshotFormatter.IsSuccess(restoreResponse))
                {
                    actions.Add($"flashback range export audio switch restored audio enabled to {baselineAudioEnabled}");
                }
                else
                {
                    warnings.Add(
                        "flashback range export audio switch: audio restore failed - " +
                        AutomationSnapshotFormatter.Get(restoreResponse, "Message", "unknown error"));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                warnings.Add($"flashback range export audio switch: audio restore threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private static async Task<bool> WaitForFlashbackPlaybackPositionAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        int targetPositionMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                var position = GetInt(snapshot, "FlashbackPlaybackPositionMs");
                if (Math.Abs(position - targetPositionMs) <= 1_500)
                {
                    return true;
                }
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private static async Task CleanupFlashbackSelectionAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
    }

    private static async Task RunFlashbackLifecycleAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback lifecycle: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "pause" },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle pause requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 1_000 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle seek requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play" },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle play requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle disabled during playback");

        var disabledSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (disabledSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback lifecycle: Flashback did not report inactive after disable");
        }
        else
        {
            if (GetBool(disabledSnapshot.Value, "FlashbackPlaybackThreadAlive"))
            {
                warnings.Add("flashback lifecycle: playback worker still alive after disable");
            }

            if (GetInt(disabledSnapshot.Value, "FlashbackPlaybackPendingCommands") > 0)
            {
                warnings.Add(
                    "flashback lifecycle: pending commands remained after disable " +
                    $"pending={GetInt(disabledSnapshot.Value, "FlashbackPlaybackPendingCommands")}");
            }
        }

        await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle re-enabled");

        var enabledSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        if (enabledSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback lifecycle: Flashback did not report active after re-enable");
        }
    }

    private static async Task<JsonElement?> WaitForFlashbackActiveAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        bool expectedActive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "FlashbackActive") == expectedActive)
            {
                return snapshot.Clone();
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task<JsonElement?> WaitForPreviewActiveAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        bool expectedActive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "IsPreviewing") == expectedActive)
            {
                return snapshot.Clone();
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "IsRecording") &&
                string.Equals(GetString(snapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase) &&
                GetBool(snapshot, "RecordingFileGrowing"))
            {
                return snapshot.Clone();
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task<bool> WaitForFlashbackStressBufferReadyAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken,
        int requiredBufferedDurationMs = 8_000,
        long requiredEncodedFrames = 240,
        TimeSpan? timeout = null)
    {
        var started = Stopwatch.GetTimestamp();
        var waitTimeout = timeout ?? TimeSpan.FromSeconds(30);
        while (Stopwatch.GetElapsedTime(started) < waitTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "FlashbackActive") &&
                GetInt(snapshot, "FlashbackBufferedDurationMs") >= requiredBufferedDurationMs &&
                (GetNullableLong(snapshot, "FlashbackEncodedFrames") ?? 0) >= requiredEncodedFrames)
            {
                return true;
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("FlashbackGetSegments", null, null).ConfigureAwait(false);
            if (TryGetFlashbackSegments(response, out var segments))
            {
                var completed = segments
                    .Where(segment => !segment.IsActive && segment.EndPtsMs > segment.StartPtsMs)
                    .OrderBy(segment => segment.EndPtsMs)
                    .FirstOrDefault();
                if (completed.EndPtsMs > completed.StartPtsMs)
                {
                    return completed;
                }
            }

            await Task.Delay(1_000, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static bool TryGetFlashbackSegments(JsonElement response, out List<FlashbackSegmentProbe> segments)
    {
        segments = new List<FlashbackSegmentProbe>();
        if (!response.TryGetProperty("Data", out var data) ||
            !data.TryGetProperty("Segments", out var segmentsElement) ||
            segmentsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var segment in segmentsElement.EnumerateArray())
        {
            if (segment.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            segments.Add(new FlashbackSegmentProbe(
                SequenceNumber: GetInt(segment, "SequenceNumber"),
                StartPtsMs: GetNullableLong(segment, "StartPtsMs") ?? 0,
                EndPtsMs: GetNullableLong(segment, "EndPtsMs") ?? 0,
                IsActive: GetBool(segment, "IsActive")));
        }

        return true;
    }

    private static void ValidateFlashbackRecordingSession(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        List<string> warnings)
    {
        var metrics = BuildFlashbackRecordingMetrics(initialSnapshot, samples);
        if (metrics.SampleCount == 0)
        {
            warnings.Add("flashback recording: no recording samples captured");
            return;
        }

        if (!metrics.BackendObserved)
        {
            warnings.Add("flashback recording: RecordingBackend never reported Flashback");
        }

        if (!metrics.FileGrowthObserved)
        {
            warnings.Add("flashback recording: recording file never reported growth");
        }

        if (metrics.VideoFramesSubmittedDelta <= 0)
        {
            warnings.Add("flashback recording: no Flashback video frames submitted to encoder");
        }

        if (metrics.VideoEncoderPacketsWrittenDelta <= 0)
        {
            warnings.Add("flashback recording: no Flashback encoder packets written");
        }

        if (metrics.IntegritySequenceGapsDelta > 0)
        {
            warnings.Add($"flashback recording: Flashback video sequence gaps increased delta={metrics.IntegritySequenceGapsDelta} end={metrics.IntegritySequenceGapsAtEnd}");
        }

        if (metrics.IntegrityQueueDroppedFramesDelta > 0)
        {
            warnings.Add($"flashback recording: Flashback dropped frames increased delta={metrics.IntegrityQueueDroppedFramesDelta} end={metrics.IntegrityQueueDroppedFramesAtEnd}");
        }
    }

    private static void ValidateCleanupLifecycleRestored(
        bool leaveRunning,
        bool startedPreview,
        bool enabledFlashback,
        bool startedFlashbackPlayback,
        JsonElement initialSnapshot,
        JsonElement finalSnapshot,
        List<string> warnings)
    {
        if (leaveRunning)
        {
            return;
        }

        if (startedPreview &&
            !GetBool(initialSnapshot, "IsPreviewing") &&
            GetBool(finalSnapshot, "IsPreviewing"))
        {
            warnings.Add("cleanup: preview remained active after restore");
        }

        if (enabledFlashback &&
            !GetBool(initialSnapshot, "FlashbackActive") &&
            GetBool(finalSnapshot, "FlashbackActive"))
        {
            warnings.Add("cleanup: Flashback remained active after restore");
        }

        if (startedFlashbackPlayback)
        {
            var state = GetString(finalSnapshot, "FlashbackPlaybackState") ?? "Unknown";
            if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"cleanup: playback did not return live state={state}");
            }
        }
    }

    private static void ValidateFlashbackPlaybackSession(
        JsonElement lastSnapshot,
        FlashbackPlaybackSessionMetrics metrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        int durationSeconds,
        List<string> warnings)
    {
        var targetFps = GetDouble(lastSnapshot, "FlashbackPlaybackTargetFps");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
        }

        var frameCount = Math.Max(metrics.EndSessionFrameCount, metrics.MaxSessionFrameCountObserved);
        if (frameCount <= 0)
        {
            warnings.Add("flashback playback: no playback frames were observed");
            return;
        }

        if (targetFps > 0 && durationSeconds > 0)
        {
            var minimumExpectedFrames = Math.Max(1, (long)Math.Floor(targetFps * durationSeconds * 0.80));
            if (frameCount < minimumExpectedFrames)
            {
                warnings.Add($"flashback playback: frame count below expected floor frames={frameCount} min={minimumExpectedFrames} targetFps={targetFps:0.##}");
            }

            var minimumOnePercentLow = targetFps * 0.80;
            var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);
            if (!visualCadenceHealthy &&
                metrics.MinOnePercentLowFpsObserved > 0 &&
                metrics.MinOnePercentLowFpsObserved < minimumOnePercentLow)
            {
                warnings.Add($"flashback playback: 1% low dipped below floor min={metrics.MinOnePercentLowFpsObserved:0.##} floor={minimumOnePercentLow:0.##}");
            }
        }

        if (metrics.DroppedFramesDelta > 0)
        {
            var droppedFrames = GetNullableLong(lastSnapshot, "FlashbackPlaybackDroppedFrames") ?? 0;
            warnings.Add($"flashback playback: dropped frames increased delta={metrics.DroppedFramesDelta} end={droppedFrames}");
        }

        if (metrics.SubmitFailuresDelta > 0)
        {
            var submitFailures = GetNullableLong(lastSnapshot, "FlashbackPlaybackSubmitFailures") ?? 0;
            warnings.Add($"flashback playback: submit failures increased delta={metrics.SubmitFailuresDelta} end={submitFailures}");
        }

        const double maxHealthyAudioBufferedMs = 250.0;
        if (metrics.MaxAudioBufferedDurationMsObserved > maxHealthyAudioBufferedMs)
        {
            warnings.Add($"flashback playback: audio buffered duration exceeded budget max={metrics.MaxAudioBufferedDurationMsObserved:0.##}ms budget={maxHealthyAudioBufferedMs:0.##}ms");
        }

        const double maxHealthyAvDriftMs = 250.0;
        if (metrics.MaxAbsAvDriftMsObserved > maxHealthyAvDriftMs)
        {
            warnings.Add($"flashback playback: absolute A/V drift exceeded budget max={metrics.MaxAbsAvDriftMsObserved:0.##}ms budget={maxHealthyAvDriftMs:0.##}ms");
        }
    }

    private static void ValidateFlashbackPreviewScheduler(
        long deadlineDropsDelta,
        long underflowsDelta,
        long d3dStatsFailureDelta,
        PreviewCadenceSessionMetrics previewCadenceMetrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        PreviewD3DMetrics previewD3DMetrics,
        double targetFps,
        bool tolerateSchedulerTransitionsWithHealthyVisualCadence,
        List<string> warnings)
    {
        if (deadlineDropsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence)
        {
            warnings.Add($"flashback preview: scheduler deadline drops increased delta={deadlineDropsDelta}");
        }

        if (underflowsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence)
        {
            warnings.Add($"flashback preview: scheduler underflows increased delta={underflowsDelta}");
        }

        if (d3dStatsFailureDelta > 0)
        {
            warnings.Add($"flashback preview: D3D frame stats failures increased delta={d3dStatsFailureDelta}");
        }

        if (targetFps < 100)
        {
            return;
        }

        var targetFrameMs = 1000.0 / targetFps;
        var onePercentLowFloor = targetFps * 0.80;
        var presentP99BudgetMs = targetFrameMs * 1.25;
        var totalP99BudgetMs = targetFrameMs * 1.35;
        var onePercentLowMiss =
            previewCadenceMetrics.MinOnePercentLowFpsObserved > 0 &&
            previewCadenceMetrics.MinOnePercentLowFpsObserved < onePercentLowFloor;
        var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);
        var presentP99Miss =
            previewD3DMetrics.PresentCallP99MsAtEnd > presentP99BudgetMs;
        var totalP99Miss =
            previewD3DMetrics.TotalFrameCpuP99MsAtEnd > totalP99BudgetMs;

        if ((onePercentLowMiss && !visualCadenceHealthy) || presentP99Miss || totalP99Miss)
        {
            warnings.Add(
                "flashback preview: present/display pressure " +
                $"targetFps={targetFps:0.##} " +
                $"onePercentLowFpsMin={previewCadenceMetrics.MinOnePercentLowFpsObserved:0.##}/{onePercentLowFloor:0.##} " +
                $"visualChangeFpsMin={visualCadenceMetrics.MinChangeFpsObserved:0.##} " +
                $"visualRepeatPctMax={visualCadenceMetrics.MaxRepeatPercentObserved:0.###} " +
                $"visualLongestRepeatRun={visualCadenceMetrics.LongestRepeatRunAtEnd} " +
                $"presentCallP99Ms={previewD3DMetrics.PresentCallP99MsAtEnd:0.##}/{presentP99BudgetMs:0.##} " +
                $"totalFrameCpuP99Ms={previewD3DMetrics.TotalFrameCpuP99MsAtEnd:0.##}/{totalP99BudgetMs:0.##} " +
                $"missedRefreshDelta={previewD3DMetrics.MissedRefreshDelta} " +
                $"underflowsDelta={underflowsDelta} " +
                $"latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)} " +
                $"latestSlowPresentCallMs={previewD3DMetrics.LatestSlowFramePresentCallMs:0.##} " +
                $"latestSlowTotalFrameCpuMs={previewD3DMetrics.LatestSlowFrameTotalFrameCpuMs:0.##} " +
                $"latestSlowPending={previewD3DMetrics.LatestSlowFramePendingFrameCount}");
        }
    }

    private static string FormatOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }

    private static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement finalSnapshot,
        bool isFlashbackScenario)
    {
        var worst = BuildWorstDiagnosticHealthObservation(samples, finalSnapshot);
        if (!isFlashbackScenario ||
            worst.Severity >= 3 ||
            samples.Count == 0)
        {
            return worst;
        }

        var finalOffsetMs = samples[^1].OffsetMs;
        if (finalOffsetMs <= 0)
        {
            return worst;
        }

        var warmupMs = Math.Min(
            FlashbackDiagnosticMaxWarmupMs,
            Math.Max(0, (long)Math.Ceiling(finalOffsetMs * FlashbackDiagnosticWarmupFraction)));
        return BuildWorstDiagnosticHealthObservationAfterOffset(samples, finalSnapshot, warmupMs);
    }

    private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservation(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement finalSnapshot)
    {
        var worst = BuildDiagnosticHealthObservation(
            finalSnapshot,
            samples.Count > 0 ? samples[^1].OffsetMs : 0);
        foreach (var sample in samples)
        {
            var observation = BuildDiagnosticHealthObservation(sample.Snapshot, sample.OffsetMs);
            if (observation.Severity > worst.Severity ||
                (observation.Severity == worst.Severity && observation.OffsetMs > worst.OffsetMs))
            {
                worst = observation;
            }
        }

        return worst;
    }

    private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement finalSnapshot,
        long minimumOffsetMs)
    {
        var worst = BuildDiagnosticHealthObservation(
            finalSnapshot,
            samples.Count > 0 ? samples[^1].OffsetMs : 0);
        foreach (var sample in samples)
        {
            if (sample.OffsetMs < minimumOffsetMs)
            {
                continue;
            }

            var observation = BuildDiagnosticHealthObservation(sample.Snapshot, sample.OffsetMs);
            if (observation.Severity > worst.Severity ||
                (observation.Severity == worst.Severity && observation.OffsetMs > worst.OffsetMs))
            {
                worst = observation;
            }
        }

        return worst;
    }

    private static DiagnosticHealthObservation BuildDiagnosticHealthObservation(JsonElement snapshot, long offsetMs)
    {
        var healthStatus = GetDiagnosticHealthStatus(snapshot);
        return new DiagnosticHealthObservation(
            healthStatus,
            GetDiagnosticLikelyStage(snapshot),
            GetString(snapshot, "DiagnosticEvidence") ?? string.Empty,
            offsetMs,
            GetDiagnosticHealthSeverity(healthStatus));
    }

    private static string GetDiagnosticHealthStatus(JsonElement snapshot)
        => GetString(snapshot, "DiagnosticHealthStatus") ?? "Unknown";

    private static string GetDiagnosticLikelyStage(JsonElement snapshot)
        => GetString(snapshot, "DiagnosticLikelyStage") ?? "diagnostic_unavailable";

    private static bool IsFailingDiagnosticHealthSeverity(int severity)
        => severity >= 2;

    private static bool IsSourceSignalDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "source_signal", StringComparison.OrdinalIgnoreCase);

    private static bool IsSourceCaptureDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "source_capture", StringComparison.OrdinalIgnoreCase);

    private static bool IsPreviewSchedulerDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "preview_scheduler", StringComparison.OrdinalIgnoreCase);

    private static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "flashback_recording", StringComparison.OrdinalIgnoreCase) &&
           observation.Evidence.Contains("lastReject=force_rotate_draining", StringComparison.OrdinalIgnoreCase);

    private static bool IsPreviewCycleScenario(
        bool runFlashbackPreviewCycle,
        bool runFlashbackPlaybackPreviewCycle,
        bool runFlashbackRecordingPreviewCycle)
        => runFlashbackPreviewCycle || runFlashbackPlaybackPreviewCycle || runFlashbackRecordingPreviewCycle;

    private static bool IsSparseSourceCaptureCadenceWarningRun(
        DiagnosticHealthObservation observation,
        SourceCadenceSessionMetrics sourceCadenceMetrics,
        long sourceReaderFramesDroppedDelta,
        long videoIngestErrorsDelta,
        int durationSeconds,
        bool visualCadenceHealthy)
    {
        if (!visualCadenceHealthy ||
            !IsSourceCaptureDiagnosticHealthObservation(observation) ||
            sourceReaderFramesDroppedDelta > 0 ||
            videoIngestErrorsDelta > 0 ||
            sourceCadenceMetrics.MaxDropPercentObserved > 0.1)
        {
            return false;
        }

        var allowedSparseEvents = Math.Max(1, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 180.0));
        return sourceCadenceMetrics.MaxEstimatedDroppedFramesObserved <= allowedSparseEvents &&
               sourceCadenceMetrics.MaxSevereGapCountObserved <= allowedSparseEvents;
    }

    private static bool IsSparsePreviewSchedulerDeadlineDropRun(
        long deadlineDropsDelta,
        long underflowsDelta,
        int durationSeconds,
        bool visualCadenceHealthy)
    {
        if (!visualCadenceHealthy || deadlineDropsDelta <= 0 || underflowsDelta > 0)
        {
            return false;
        }

        var allowedDrops = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 10.0));
        return deadlineDropsDelta <= allowedDrops;
    }

    private static bool IsSparsePreviewSchedulerStressRun(
        long deadlineDropsDelta,
        long underflowsDelta,
        int durationSeconds,
        bool visualCadenceHealthy)
    {
        if (!visualCadenceHealthy || deadlineDropsDelta <= 0 || underflowsDelta < 0)
        {
            return false;
        }

        var allowedDeadlineDrops = Math.Max(6, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 45.0));
        var allowedUnderflows = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 120.0));
        return deadlineDropsDelta <= allowedDeadlineDrops &&
               underflowsDelta <= allowedUnderflows;
    }

    private static bool IsToleratedFlashbackScenarioWarning(
        string warning,
        bool toleratesSourceSignalHealthWarning,
        bool toleratesFlashbackForceRotateDrainWarning,
        bool toleratesPreviewCycleSchedulerWarning)
    {
        if (toleratesSourceSignalHealthWarning &&
            warning.StartsWith(
                "diagnostic health source-signal warning tolerated for export reliability scenario:",
                StringComparison.Ordinal))
        {
            return true;
        }

        if (toleratesFlashbackForceRotateDrainWarning &&
            warning.StartsWith(
                "diagnostic health flashback force-rotate drain warning tolerated for flashback scenario:",
                StringComparison.Ordinal))
        {
            return true;
        }

        return toleratesPreviewCycleSchedulerWarning &&
               warning.StartsWith(
                   "diagnostic health preview scheduler transition warning tolerated for preview-cycle scenario:",
                   StringComparison.Ordinal);
    }

    private static int GetDiagnosticHealthSeverity(string? healthStatus)
    {
        return (healthStatus ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "critical" or "failed" or "faulted" or "error" => 4,
            "degraded" => 3,
            "warning" => 2,
            "warmingup" => 1,
            _ => 0
        };
    }

}
