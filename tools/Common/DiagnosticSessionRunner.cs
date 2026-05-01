using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using static ElgatoCapture.Tools.AutomationSnapshotFormatter;

namespace ElgatoCapture.Tools;

public sealed class DiagnosticSessionOptions
{
    public string Scenario { get; init; } = "observe";
    public int DurationSeconds { get; init; } = 10;
    public int SampleIntervalMs { get; init; } = 1000;
    public string? OutputDirectory { get; init; }
    public bool IncludePresentMon { get; init; }
    public string? PresentMonPath { get; init; }
    public bool VerifyRecording { get; init; }
    public bool LeaveRunning { get; init; }
}

public sealed class DiagnosticSessionResult
{
    public string SessionId { get; init; } = string.Empty;
    public string Scenario { get; init; } = "observe";
    public bool Success { get; init; }
    public int DurationSeconds { get; init; }
    public int SampleIntervalMs { get; init; }
    public int SampleCount { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
    public string SummaryPath { get; init; } = string.Empty;
    public string SamplesPath { get; init; } = string.Empty;
    public string FrameLedgerPath { get; init; } = string.Empty;
    public string TimelinePath { get; init; } = string.Empty;
    public string HealthStatus { get; init; } = "Unknown";
    public string LikelyStage { get; init; } = "diagnostic_unavailable";
    public string Summary { get; init; } = string.Empty;
    public string Evidence { get; init; } = string.Empty;
    public int FlashbackPlaybackPendingCommandsAtEnd { get; init; }
    public int FlashbackPlaybackMaxPendingCommandsObserved { get; init; }
    public int FlashbackPlaybackMaxCommandQueueLatencyMsObserved { get; init; }
    public long FlashbackPlaybackCommandsDroppedAtEnd { get; init; }
    public long FlashbackPlaybackCommandsSkippedNotReadyAtEnd { get; init; }
    public bool FlashbackRecordingBackendObserved { get; init; }
    public bool FlashbackRecordingFileGrowthObserved { get; init; }
    public long FlashbackRecordingVideoFramesSubmittedDelta { get; init; }
    public long FlashbackRecordingVideoEncoderPacketsWrittenDelta { get; init; }
    public long FlashbackRecordingIntegritySequenceGapsAtEnd { get; init; }
    public long FlashbackRecordingIntegrityQueueDroppedFramesAtEnd { get; init; }
    public bool RecordingVerificationRun { get; init; }
    public bool? RecordingVerificationSucceeded { get; init; }
    public string? RecordingVerificationMessage { get; init; }
    public PresentMonProbeResult? PresentMon { get; init; }
    public string[] Actions { get; init; } = Array.Empty<string>();
    public string[] Warnings { get; init; } = Array.Empty<string>();
}

public sealed class DiagnosticSessionSample
{
    public long OffsetMs { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public JsonElement Snapshot { get; init; }
}

public static class DiagnosticSessionRunner
{
    private const int FlashbackStressMaxPlaybackPendingCommands = 3;
    private const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;
    private const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;

    private readonly record struct FlashbackSegmentProbe(
        int SequenceNumber,
        long StartPtsMs,
        long EndPtsMs,
        bool IsActive);

    public static async Task<DiagnosticSessionResult> RunAsync(
        DiagnosticSessionOptions options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sendCommandAsync);

        var scenario = NormalizeScenario(options.Scenario);
        var durationSeconds = Math.Clamp(options.DurationSeconds, 0, 24 * 60 * 60);
        var sampleIntervalMs = Math.Clamp(options.SampleIntervalMs, 100, 60_000);
        var sessionId = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var outputDirectory = string.IsNullOrWhiteSpace(options.OutputDirectory)
            ? Path.Combine(Environment.CurrentDirectory, "temp", "diagnostic-sessions", sessionId)
            : Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

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
        var runFlashbackStress = scenario == "flashback-stress";
        var runFlashbackScrubStress = scenario == "flashback-scrub-stress";
        var runFlashbackRestartCycle = scenario == "flashback-restart-cycle";
        var runFlashbackEncoderCycle = scenario == "flashback-encoder-cycle";
        var runFlashbackExportPlayback = scenario == "flashback-export-playback";
        var runFlashbackSegmentPlayback = scenario == "flashback-segment-playback";
        var runFlashbackRangeExport = scenario == "flashback-range-export";
        var runFlashbackLifecycle = scenario == "flashback-lifecycle";
        var runFlashbackExportConcurrent = scenario == "flashback-export-concurrent";
        var runFlashbackDisableDuringExport = scenario == "flashback-disable-during-export";
        var runFlashbackPreviewCycle = scenario == "flashback-preview-cycle";
        var runFlashbackRecording = scenario == "flashback-recording";
        var runFlashbackRecordingPreviewCycle = scenario == "flashback-recording-preview-cycle";
        var runFlashbackRecordingSettingsDeferred = scenario == "flashback-recording-settings-deferred";
        var runFlashbackRecordingExportRejected = scenario == "flashback-recording-export-rejected";
        var runFlashbackExportRejected = scenario == "flashback-export-rejected";
        string? flashbackRecordingSettingsDeferredTargetPreset = null;
        using var commandSendGate = new SemaphoreSlim(1, 1);

        var initialResponse = await SendAsync("GetSnapshot", null, null).ConfigureAwait(false);
        var initialSnapshot = TryGetSnapshot(initialResponse, out var initial)
            ? initial
            : default;

        try
        {
            if (ScenarioNeedsFlashback(scenario) && !GetBool(initialSnapshot, "FlashbackActive"))
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

            if (ScenarioNeedsPreview(scenario) && !GetBool(initialSnapshot, "IsPreviewing"))
            {
                await SendAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
                startedPreview = true;
                actions.Add("preview started");
                await TryWaitAsync("VideoFramesFlowing", 15_000).ConfigureAwait(false);
            }

            if (ScenarioNeedsRecording(scenario) && !GetBool(initialSnapshot, "IsRecording"))
            {
                if ((runFlashbackRecording || runFlashbackRecordingPreviewCycle || runFlashbackRecordingSettingsDeferred || runFlashbackRecordingExportRejected) &&
                    !await WaitForFlashbackStressBufferReadyAsync(
                        (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                        cancellationToken).ConfigureAwait(false))
                {
                    warnings.Add("flashback recording: Flashback buffer did not become recording-ready within 30s");
                }

                await SendAsync("SetRecordingEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
                startedRecording = true;
                actions.Add("recording started");
                await TryWaitAsync("RecordingFileGrowing", 20_000).ConfigureAwait(false);
            }

            Task<PresentMonProbeResult>? presentMonTask = null;
            if (options.IncludePresentMon)
            {
                var correlationSnapshotResponse = await SendAsync("GetSnapshot", null, null).ConfigureAwait(false);
                TryGetSnapshot(correlationSnapshotResponse, out var correlationSnapshot);
                presentMonTask = PresentMonProbe.RunAsync(new PresentMonProbeOptions
                {
                    ProcessName = "ElgatoCapture",
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

            Task? flashbackStressTask = null;
            if (runFlashbackStress)
            {
                flashbackStressTask = RunFlashbackStressAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken);
                actions.Add("flashback stress started");
            }

            Task? flashbackLifecycleTask = null;
            Task? flashbackScrubStressTask = null;
            Task? flashbackRestartCycleTask = null;
            Task? flashbackExportPlaybackTask = null;
            if (runFlashbackScrubStress)
            {
                flashbackScrubStressTask = RunFlashbackScrubStressAsync(
                    actions,
                    warnings,
                    sendCommandAsync,
                    cancellationToken);
                actions.Add("flashback scrub stress started");
            }

            if (runFlashbackRestartCycle)
            {
                flashbackRestartCycleTask = RunFlashbackRestartCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken);
                actions.Add("flashback restart cycle started");
            }

            Task? flashbackEncoderCycleTask = null;
            if (runFlashbackEncoderCycle)
            {
                flashbackEncoderCycleTask = RunFlashbackEncoderCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken);
                actions.Add("flashback encoder cycle started");
            }

            if (runFlashbackExportPlayback)
            {
                flashbackExportPlaybackTask = RunFlashbackExportPlaybackAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken);
                actions.Add("flashback export playback started");
            }

            Task? flashbackSegmentPlaybackTask = null;
            if (runFlashbackSegmentPlayback)
            {
                flashbackSegmentPlaybackTask = RunFlashbackSegmentPlaybackAsync(
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken);
                actions.Add("flashback segment playback started");
            }

            Task? flashbackRangeExportTask = null;
            if (runFlashbackRangeExport)
            {
                flashbackRangeExportTask = RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken);
                actions.Add("flashback range export started");
            }

            if (runFlashbackLifecycle)
            {
                flashbackLifecycleTask = RunFlashbackLifecycleAsync(
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken);
                actions.Add("flashback lifecycle started");
            }

            Task? flashbackExportConcurrentTask = null;
            if (runFlashbackExportConcurrent)
            {
                flashbackExportConcurrentTask = RunFlashbackExportConcurrentAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendCommandAsync,
                    cancellationToken);
                actions.Add("flashback concurrent export started");
            }

            Task? flashbackDisableDuringExportTask = null;
            Task? flashbackPreviewCycleTask = null;
            Task? flashbackRecordingPreviewCycleTask = null;
            Task<string?>? flashbackRecordingSettingsDeferredTask = null;
            if (runFlashbackDisableDuringExport)
            {
                flashbackDisableDuringExportTask = RunFlashbackDisableDuringExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendCommandAsync,
                    cancellationToken);
                actions.Add("flashback disable during export started");
            }

            if (runFlashbackPreviewCycle)
            {
                flashbackPreviewCycleTask = RunFlashbackPreviewCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken);
                actions.Add("flashback preview cycle started");
            }

            if (runFlashbackRecordingPreviewCycle)
            {
                flashbackRecordingPreviewCycleTask = RunFlashbackRecordingPreviewCycleAsync(
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken);
                actions.Add("flashback recording preview cycle started");
            }

            if (runFlashbackRecordingSettingsDeferred)
            {
                flashbackRecordingSettingsDeferredTask = RunFlashbackRecordingSettingsDeferredAsync(
                    actions,
                    warnings,
                    (command, payload, timeoutMs, allowFailure) => SendAsync(command, payload, timeoutMs, allowFailure),
                    cancellationToken);
                actions.Add("flashback recording settings deferred started");
            }

            await SampleLoopAsync(
                    durationSeconds,
                    sampleIntervalMs,
                    samples,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken)
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

            if (flashbackExportConcurrentTask is not null)
            {
                await flashbackExportConcurrentTask.ConfigureAwait(false);
            }

            if (flashbackDisableDuringExportTask is not null)
            {
                await flashbackDisableDuringExportTask.ConfigureAwait(false);
            }

            if (flashbackPreviewCycleTask is not null)
            {
                await flashbackPreviewCycleTask.ConfigureAwait(false);
            }

            if (flashbackRecordingPreviewCycleTask is not null)
            {
                await flashbackRecordingPreviewCycleTask.ConfigureAwait(false);
            }

            if (flashbackRecordingSettingsDeferredTask is not null)
            {
                flashbackRecordingSettingsDeferredTargetPreset = await flashbackRecordingSettingsDeferredTask.ConfigureAwait(false);
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
        finally
        {
            if (!options.LeaveRunning)
            {
                if (startedRecording)
                {
                    var stopResponse = await SendAsync("SetRecordingEnabled", new Dictionary<string, object?> { ["enabled"] = false }, null).ConfigureAwait(false);
                    actions.Add("recording stopped");
                    if (AutomationSnapshotFormatter.IsSuccess(stopResponse))
                    {
                        await TryWaitAsync("RecordingStopped", 30_000).ConfigureAwait(false);
                    }
                }

                if (startedPreview && !GetBool(initialSnapshot, "IsPreviewing"))
                {
                    await SendAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = false }, null).ConfigureAwait(false);
                    actions.Add("preview stopped");
                }

                if (enabledFlashback && !GetBool(initialSnapshot, "FlashbackActive"))
                {
                    await SendAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = false }, null).ConfigureAwait(false);
                    actions.Add("flashback restored off");
                }

                if (disabledFlashback && GetBool(initialSnapshot, "FlashbackActive"))
                {
                    await SendAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
                    actions.Add("flashback restored on");
                }
            }
        }

        if (runFlashbackRecordingSettingsDeferred)
        {
            await VerifyFlashbackRecordingSettingsAppliedAfterStopAsync(
                    actions,
                    warnings,
                    flashbackRecordingSettingsDeferredTargetPreset,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (options.VerifyRecording || startedRecording)
        {
            var verificationResponse = await SendAsync("VerifyLastRecording", null, 60_000).ConfigureAwait(false);
            if (TryGetVerification(verificationResponse, out var verificationElement))
            {
                verification = verificationElement.Clone();
            }
            else
            {
                warnings.Add(AutomationSnapshotFormatter.Get(verificationResponse, "Message", "Recording verification did not return data."));
            }
        }

        if (runFlashbackRecording || runFlashbackRecordingPreviewCycle || runFlashbackRecordingSettingsDeferred || runFlashbackRecordingExportRejected)
        {
            ValidateFlashbackRecordingSession(samples, warnings);
        }

        var timelineResponse = await SendAsync(
                "GetPerformanceTimeline",
                new Dictionary<string, object?> { ["maxEntries"] = 240 },
                null)
            .ConfigureAwait(false);
        if (timelineResponse.TryGetProperty("Data", out var timelineData))
        {
            timeline = timelineData.Clone();
        }

        var lastSnapshot = samples.Count > 0
            ? samples[^1].Snapshot
            : initialSnapshot;
        var healthStatus = GetString(lastSnapshot, "DiagnosticHealthStatus") ?? "Unknown";
        var likelyStage = GetString(lastSnapshot, "DiagnosticLikelyStage") ?? "diagnostic_unavailable";
        var summary = GetString(lastSnapshot, "DiagnosticSummary") ?? string.Empty;
        var evidence = GetString(lastSnapshot, "DiagnosticEvidence") ?? string.Empty;
        var playbackPendingAtEnd = GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands");
        var playbackMaxPendingObserved = GetMaxSnapshotInt(samples, lastSnapshot, "FlashbackPlaybackMaxPendingCommands");
        var playbackMaxLatencyObserved = GetMaxSnapshotInt(samples, lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs");
        var playbackDroppedAtEnd = GetNullableLong(lastSnapshot, "FlashbackPlaybackCommandsDropped") ?? 0;
        var playbackSkippedAtEnd = GetNullableLong(lastSnapshot, "FlashbackPlaybackCommandsSkippedNotReady") ?? 0;
        var recordingMetrics = BuildFlashbackRecordingMetrics(samples);

        var samplesPath = Path.Combine(outputDirectory, "samples.json");
        var frameLedgerPath = Path.Combine(outputDirectory, "frame-ledger.json");
        var timelinePath = Path.Combine(outputDirectory, "timeline.json");
        var summaryPath = Path.Combine(outputDirectory, "summary.json");

        await WriteJsonAsync(samplesPath, samples, cancellationToken).ConfigureAwait(false);
        await WriteJsonAsync(frameLedgerPath, BuildFrameLedgerTrace(sessionId, samples), cancellationToken).ConfigureAwait(false);
        await WriteJsonAsync(timelinePath, timeline, cancellationToken).ConfigureAwait(false);

        var verificationSucceeded = verification.HasValue
            ? GetBool(verification.Value, "Succeeded")
            : (bool?)null;
        var result = new DiagnosticSessionResult
        {
            SessionId = sessionId,
            Scenario = scenario,
            Success = commandFailureCount == 0 &&
                      (presentMon is null || presentMon.Success) &&
                      (!verificationSucceeded.HasValue || verificationSucceeded.Value) &&
                      (!(runFlashbackStress || runFlashbackScrubStress || runFlashbackRestartCycle || runFlashbackEncoderCycle || runFlashbackExportPlayback || runFlashbackSegmentPlayback || runFlashbackRangeExport || runFlashbackLifecycle || runFlashbackExportConcurrent || runFlashbackDisableDuringExport || runFlashbackPreviewCycle || runFlashbackRecording || runFlashbackRecordingPreviewCycle || runFlashbackRecordingSettingsDeferred || runFlashbackRecordingExportRejected || runFlashbackExportRejected) || warnings.Count == 0),
            DurationSeconds = durationSeconds,
            SampleIntervalMs = sampleIntervalMs,
            SampleCount = samples.Count,
            OutputDirectory = outputDirectory,
            SummaryPath = summaryPath,
            SamplesPath = samplesPath,
            FrameLedgerPath = frameLedgerPath,
            TimelinePath = timelinePath,
            HealthStatus = healthStatus,
            LikelyStage = likelyStage,
            Summary = summary,
            Evidence = evidence,
            FlashbackPlaybackPendingCommandsAtEnd = playbackPendingAtEnd,
            FlashbackPlaybackMaxPendingCommandsObserved = playbackMaxPendingObserved,
            FlashbackPlaybackMaxCommandQueueLatencyMsObserved = playbackMaxLatencyObserved,
            FlashbackPlaybackCommandsDroppedAtEnd = playbackDroppedAtEnd,
            FlashbackPlaybackCommandsSkippedNotReadyAtEnd = playbackSkippedAtEnd,
            FlashbackRecordingBackendObserved = recordingMetrics.BackendObserved,
            FlashbackRecordingFileGrowthObserved = recordingMetrics.FileGrowthObserved,
            FlashbackRecordingVideoFramesSubmittedDelta = recordingMetrics.VideoFramesSubmittedDelta,
            FlashbackRecordingVideoEncoderPacketsWrittenDelta = recordingMetrics.VideoEncoderPacketsWrittenDelta,
            FlashbackRecordingIntegritySequenceGapsAtEnd = recordingMetrics.IntegritySequenceGapsAtEnd,
            FlashbackRecordingIntegrityQueueDroppedFramesAtEnd = recordingMetrics.IntegrityQueueDroppedFramesAtEnd,
            RecordingVerificationRun = verification.HasValue,
            RecordingVerificationSucceeded = verificationSucceeded,
            RecordingVerificationMessage = verification.HasValue
                ? GetString(verification.Value, "Message") ?? string.Empty
                : null,
            PresentMon = presentMon,
            Actions = actions.ToArray(),
            Warnings = warnings.ToArray()
        };

        await WriteJsonAsync(summaryPath, result, cancellationToken).ConfigureAwait(false);
        return result;

        async Task<JsonElement> SendAsync(
            string command,
            Dictionary<string, object?>? payload,
            int? responseTimeoutMs,
            bool allowFailure = false)
        {
            await commandSendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var response = await sendCommandAsync(command, payload, responseTimeoutMs).ConfigureAwait(false);
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
        {
            var response = await SendAsync(
                    "WaitForCondition",
                    new Dictionary<string, object?>
                    {
                        ["condition"] = condition,
                        ["timeoutMs"] = timeoutMs,
                        ["pollMs"] = 250
                    },
                    timeoutMs + 2_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                warnings.Add($"wait {condition}: {AutomationSnapshotFormatter.Get(response, "Message", "not met")}");
            }
        }
    }

    public static string Format(DiagnosticSessionResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"== Diagnostic Session: {(result.Success ? "PASS" : "FAIL")} ==");
        builder.AppendLine($"Scenario: {result.Scenario} | Duration: {result.DurationSeconds}s | Samples: {result.SampleCount} @ {result.SampleIntervalMs}ms");
        builder.AppendLine($"Health: {result.HealthStatus} | Stage: {result.LikelyStage}");
        if (!string.IsNullOrWhiteSpace(result.Summary))
        {
            builder.AppendLine($"Summary: {result.Summary}");
        }

        if (!string.IsNullOrWhiteSpace(result.Evidence))
        {
            builder.AppendLine($"Evidence: {result.Evidence}");
        }

        if (result.RecordingVerificationRun)
        {
            var status = result.RecordingVerificationSucceeded == true ? "PASS" : "FAIL";
            builder.AppendLine($"Recording Verification: {status} | {result.RecordingVerificationMessage}");
        }

        if (result.PresentMon is not null)
        {
            builder.AppendLine($"PresentMon: {(result.PresentMon.Success ? "PASS" : "FAIL")} | {result.PresentMon.Message}");
        }

        builder.AppendLine(
            "Flashback Playback Commands: " +
            $"pendingEnd={result.FlashbackPlaybackPendingCommandsAtEnd} " +
            $"maxPending={result.FlashbackPlaybackMaxPendingCommandsObserved} " +
            $"maxLatencyMs={result.FlashbackPlaybackMaxCommandQueueLatencyMsObserved} " +
            $"droppedEnd={result.FlashbackPlaybackCommandsDroppedAtEnd} " +
            $"skippedEnd={result.FlashbackPlaybackCommandsSkippedNotReadyAtEnd}");
        builder.AppendLine(
            "Flashback Recording: " +
            $"backendObserved={result.FlashbackRecordingBackendObserved} " +
            $"fileGrowthObserved={result.FlashbackRecordingFileGrowthObserved} " +
            $"submittedDelta={result.FlashbackRecordingVideoFramesSubmittedDelta} " +
            $"packetsDelta={result.FlashbackRecordingVideoEncoderPacketsWrittenDelta} " +
            $"seqGapsEnd={result.FlashbackRecordingIntegritySequenceGapsAtEnd} " +
            $"queueDropsEnd={result.FlashbackRecordingIntegrityQueueDroppedFramesAtEnd}");

        builder.AppendLine($"Artifacts: {result.OutputDirectory}");
        builder.AppendLine($"  Summary: {result.SummaryPath}");
        builder.AppendLine($"  Samples: {result.SamplesPath}");
        builder.AppendLine($"  Frame Ledger: {result.FrameLedgerPath}");
        builder.AppendLine($"  Timeline: {result.TimelinePath}");

        if (result.Actions.Length > 0)
        {
            builder.AppendLine($"Actions: {string.Join(", ", result.Actions)}");
        }

        if (result.Warnings.Length > 0)
        {
            builder.AppendLine("Warnings:");
            foreach (var warning in result.Warnings)
            {
                builder.AppendLine($"  {warning}");
            }
        }

        return builder.ToString().TrimEnd();
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

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
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
                    new Dictionary<string, object?> { ["filePath"] = exportPath, ["strict"] = true },
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
                GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands") == 0)
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
            var dropped = GetInt(lastSnapshot, "FlashbackPlaybackCommandsDropped");
            var skipped = GetInt(lastSnapshot, "FlashbackPlaybackCommandsSkippedNotReady");
            var state = GetString(lastSnapshot, "FlashbackPlaybackState") ?? "Unknown";
            var threadAlive = GetBool(lastSnapshot, "FlashbackPlaybackThreadAlive");
            var maxPending = GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands");
            var maxLatencyMs = GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs");
            if (dropped > 0 || skipped > 0)
            {
                warnings.Add($"flashback stress: dropped={dropped} skipped={skipped}");
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

            if (threadAlive)
            {
                warnings.Add("flashback stress: playback worker still alive after drain wait");
            }
        }
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
        var lastSuccess = GetString(snapshot, "LastExportSuccess") ?? string.Empty;
        if (!string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export rejected: expected Failed status, got {status}");
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
        var lastSuccess = GetString(snapshot, "LastExportSuccess") ?? string.Empty;
        if (!string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback recording export rejected: expected Failed status, got {status}");
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
        var exportPayloadA = new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPathA };
        var exportPayloadB = new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPathB };

        var exportTaskA = sendCommandAsync("FlashbackExport", exportPayloadA, 60_000);
        var exportTaskB = sendCommandAsync("FlashbackExport", exportPayloadB, 60_000);
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
                    new Dictionary<string, object?> { ["filePath"] = path, ["strict"] = true },
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
            new Dictionary<string, object?> { ["seconds"] = 3, ["outputPath"] = exportPath },
            60_000);

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        var disableTask = sendCommandAsync(
            "SetFlashbackEnabled",
            new Dictionary<string, object?> { ["enabled"] = false },
            305_000);
        actions.Add("flashback disable/export requests issued");

        var exportResponse = await exportTask.ConfigureAwait(false);
        var disableResponse = await disableTask.ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback disable during export: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
        }

        if (!AutomationSnapshotFormatter.IsSuccess(disableResponse))
        {
            warnings.Add(
                $"flashback disable during export: disable failed - {AutomationSnapshotFormatter.Get(disableResponse, "Message", "unknown error")}");
        }

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            var verifyResponse = await sendCommandAsync(
                    "VerifyFile",
                    new Dictionary<string, object?> { ["filePath"] = exportPath, ["strict"] = true },
                    60_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
            {
                warnings.Add(
                    $"flashback disable during export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            }
        }

        if (AutomationSnapshotFormatter.IsSuccess(disableResponse))
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

    private static async Task<JsonElement?> SendCommandWithConnectRetryAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        string command,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        AutomationPipeConnectException? lastConnectException = null;
        while (Stopwatch.GetElapsedTime(started) < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return (await sendCommandAsync(command, payload, responseTimeoutMs).ConfigureAwait(false)).Clone();
            }
            catch (AutomationPipeConnectException ex)
            {
                lastConnectException = ex;
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        if (lastConnectException is not null)
        {
            return BuildLocalFailureResponse(command, lastConnectException.Message);
        }

        return BuildLocalFailureResponse(command, "command was not attempted before retry timeout elapsed");
    }

    private static JsonElement BuildLocalFailureResponse(string command, string message)
    {
        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["Success"] = false,
                ["Status"] = "failed",
                ["CommandLifecycle"] = "failed",
                ["Message"] = $"{command}: {message}"
            }));
        return document.RootElement.Clone();
    }

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
                    new Dictionary<string, object?> { ["filePath"] = exportPath, ["strict"] = true },
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

    private static async Task<string?> RunFlashbackRecordingSettingsDeferredAsync(
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
            return null;
        }

        var originalPreset = GetString(recordingReadySnapshot.Value, "SelectedPreset") ?? "P1";
        var cycledPreset = string.Equals(originalPreset, "P1", StringComparison.OrdinalIgnoreCase) ? "P2" : "P1";
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
            return cycledPreset;
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
            return cycledPreset;
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

        return cycledPreset;
    }

    private static async Task VerifyFlashbackRecordingSettingsAppliedAfterStopAsync(
        List<string> actions,
        List<string> warnings,
        string? expectedPreset,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expectedPreset))
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

        if (!string.Equals(GetString(snapshot, "SelectedPreset"), expectedPreset, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                "flashback recording settings deferred: selected preset was not preserved after stop " +
                $"expected={expectedPreset} actual={GetString(snapshot, "SelectedPreset") ?? "<null>"}");
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

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress pause requested");

        var positions = new[]
        {
            250, 500, 750, 1_000, 1_250, 1_500, 1_750, 2_000,
            2_250, 2_500, 2_750, 3_000, 2_400, 1_800, 1_200, 600
        };
        var seekTasks = new Task<JsonElement>[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            seekTasks[i] = sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = positions[i] },
                null);
        }

        var seekResponses = await Task.WhenAll(seekTasks).ConfigureAwait(false);
        actions.Add("flashback scrub stress seek burst requested");
        var failedSeeks = 0;
        foreach (var response in seekResponses)
        {
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                failedSeeks++;
            }
        }

        if (failedSeeks > 0)
        {
            warnings.Add($"flashback scrub stress: {failedSeeks} seek command(s) failed");
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
                GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands") == 0)
            {
                drained = true;
                break;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        if (!drained)
        {
            warnings.Add(
                "flashback scrub stress: playback command queue did not drain within 10s " +
                $"pending={GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands")} " +
                $"maxPending={GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands")} " +
                $"lastLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")} " +
                $"maxLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}");
            return;
        }

        var dropped = GetInt(lastSnapshot, "FlashbackPlaybackCommandsDropped");
        var skipped = GetInt(lastSnapshot, "FlashbackPlaybackCommandsSkippedNotReady");
        var state = GetString(lastSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        var threadAlive = GetBool(lastSnapshot, "FlashbackPlaybackThreadAlive");
        var maxPending = GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands");
        var maxLatencyMs = GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs");

        if (dropped > 0 || skipped > 0)
        {
            warnings.Add($"flashback scrub stress: dropped={dropped} skipped={skipped}");
        }

        if (maxPending > FlashbackScrubStressMaxPlaybackPendingCommands ||
            maxLatencyMs > FlashbackStressMaxPlaybackCommandLatencyMs)
        {
            warnings.Add(
                "flashback scrub stress: playback command latency exceeded threshold " +
                $"maxPending={maxPending}/{FlashbackScrubStressMaxPlaybackPendingCommands} " +
                $"maxLatencyMs={maxLatencyMs}/{FlashbackStressMaxPlaybackCommandLatencyMs}");
        }

        if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback scrub stress: playback ended in state {state}");
        }

        if (threadAlive)
        {
            warnings.Add("flashback scrub stress: playback worker still alive after drain wait");
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
                new Dictionary<string, object?> { ["filePath"] = exportPath, ["strict"] = true },
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
                    new Dictionary<string, object?> { ["filePath"] = exportPath, ["strict"] = true },
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

        await Task.Delay(750, cancellationToken).ConfigureAwait(false);
        var playbackSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(playbackSnapshotResponse, out var playbackSnapshot);
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
                new Dictionary<string, object?> { ["filePath"] = exportPath, ["strict"] = true },
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

        var dropped = GetInt(finalSnapshot, "FlashbackPlaybackCommandsDropped");
        var skipped = GetInt(finalSnapshot, "FlashbackPlaybackCommandsSkippedNotReady");
        var pending = GetInt(finalSnapshot, "FlashbackPlaybackPendingCommands");
        var state = GetString(finalSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (dropped > 0 || skipped > 0)
        {
            warnings.Add($"flashback export playback: dropped={dropped} skipped={skipped}");
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

        var completedSegment = await WaitForFlashbackCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);

        if (completedSegment is null)
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

            completedSegment = await WaitForFlashbackCompletedSegmentAsync(
                    sendCommandAsync,
                    TimeSpan.FromSeconds(20),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (completedSegment is null)
        {
            warnings.Add("flashback segment playback: no completed segment became available after recording-assisted rotation");
            return;
        }

        var seekPositionMs = Math.Max(0, completedSegment.Value.EndPtsMs - 500);
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
            $"segment={completedSegment.Value.SequenceNumber} seekMs={seekPositionMs} endMs={completedSegment.Value.EndPtsMs}");

        var playbackSnapshot = await WaitForFlashbackPlaybackBoundaryCrossAsync(
                sendCommandAsync,
                completedSegment.Value.EndPtsMs,
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
        var dropped = GetNullableLong(playbackSnapshot.Value, "FlashbackPlaybackCommandsDropped") ?? 0;
        var skipped = GetNullableLong(playbackSnapshot.Value, "FlashbackPlaybackCommandsSkippedNotReady") ?? 0;
        var pending = GetInt(playbackSnapshot.Value, "FlashbackPlaybackPendingCommands");
        actions.Add(
            "flashback segment playback observed " +
            $"positionMs={positionMs} frames={frameCount} late={lateFrames} fps={observedFps:0.##}");

        if (!string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback segment playback: expected Playing after boundary playback, got {state}");
        }

        if (positionMs < completedSegment.Value.EndPtsMs + 250)
        {
            warnings.Add(
                "flashback segment playback: playback position did not cross completed segment boundary " +
                $"positionMs={positionMs} boundaryMs={completedSegment.Value.EndPtsMs}");
        }

        if (frameCount <= 0 || observedFps <= 1)
        {
            warnings.Add(
                "flashback segment playback: playback frames did not advance " +
                $"frames={frameCount} observedFps={observedFps:0.##}");
        }

        if (dropped > 0 || skipped > 0 || pending > 0)
        {
            warnings.Add(
                "flashback segment playback: command queue unhealthy " +
                $"dropped={dropped} skipped={skipped} pending={pending}");
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
                if (positionMs >= boundaryMs + 250 &&
                    frameCount > 0 &&
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
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback range export: Flashback buffer did not become range-ready within 30s");
            return;
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 0 },
                null)
            .ConfigureAwait(false);
        if (!await WaitForFlashbackPlaybackPositionAsync(sendCommandAsync, 0, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback range export: playback did not reach in-point seek before marking range");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "set-in-point" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback range export in point set");

        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 5_000 },
                null)
            .ConfigureAwait(false);
        if (!await WaitForFlashbackPlaybackPositionAsync(sendCommandAsync, 5_000, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback range export: playback did not reach out-point seek before marking range");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "set-out-point" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback range export out point set");

        var exportPath = Path.Combine(outputDirectory, "flashback-range-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?>
                {
                    ["seconds"] = 1,
                    ["outputPath"] = exportPath,
                    ["useSelectionRange"] = true
                },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback selected range export requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add($"flashback range export: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
            return;
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                new Dictionary<string, object?> { ["filePath"] = exportPath, ["strict"] = true },
                60_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback range export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
        }
        else
        {
            actions.Add("flashback selected range export verified");
        }

        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot))
        {
            warnings.Add("flashback range export: no snapshot returned after export");
            await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
            return;
        }

        var inPointMs = GetNullableLong(snapshot, "FlashbackExportInPointMs") ?? 0;
        var outPointMs = GetNullableLong(snapshot, "FlashbackExportOutPointMs") ?? 0;
        var exportedDurationMs = outPointMs - inPointMs;
        if (exportedDurationMs < 4_000 || exportedDurationMs > 7_000)
        {
            warnings.Add(
                "flashback range export: selected export duration outside expected range " +
                $"in={inPointMs} out={outPointMs} duration={exportedDurationMs}");
        }

        var status = GetString(snapshot, "FlashbackExportStatus") ?? "Unknown";
        if (!string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback range export: expected Succeeded status, got {status}");
        }

        await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
        actions.Add("flashback range export cleared range and went live");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        var finalSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot))
        {
            warnings.Add("flashback range export: no final snapshot returned");
            return;
        }

        var pending = GetInt(finalSnapshot, "FlashbackPlaybackPendingCommands");
        var dropped = GetInt(finalSnapshot, "FlashbackPlaybackCommandsDropped");
        var skipped = GetInt(finalSnapshot, "FlashbackPlaybackCommandsSkippedNotReady");
        var state = GetString(finalSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (pending > 0)
        {
            warnings.Add($"flashback range export: pending commands remained after go-live pending={pending}");
        }

        if (dropped > 0 || skipped > 0)
        {
            warnings.Add($"flashback range export: dropped={dropped} skipped={skipped}");
        }

        if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback range export: playback ended in state {state}");
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
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(30))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot) &&
                GetBool(snapshot, "FlashbackActive") &&
                GetInt(snapshot, "FlashbackBufferedDurationMs") >= 8_000 &&
                GetInt(snapshot, "FlashbackEncodedFrames") >= 240)
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
        IReadOnlyList<DiagnosticSessionSample> samples,
        List<string> warnings)
    {
        var metrics = BuildFlashbackRecordingMetrics(samples);
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

        if (metrics.IntegritySequenceGapsAtEnd > 0)
        {
            warnings.Add("flashback recording: Flashback video sequence gaps were reported");
        }

        if (metrics.IntegrityQueueDroppedFramesAtEnd > 0)
        {
            warnings.Add("flashback recording: Flashback dropped frames were reported");
        }
    }

    private static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(
        IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var recordingSamples = samples
            .Select(sample => sample.Snapshot)
            .Where(snapshot => GetBool(snapshot, "IsRecording"))
            .ToArray();
        if (recordingSamples.Length == 0)
        {
            return new FlashbackRecordingSessionMetrics();
        }

        var firstRecordingSample = recordingSamples[0];
        var finalRecordingSample = recordingSamples[^1];
        return new FlashbackRecordingSessionMetrics
        {
            SampleCount = recordingSamples.Length,
            BackendObserved = recordingSamples.Any(snapshot =>
                string.Equals(GetString(snapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase)),
            FileGrowthObserved = recordingSamples.Any(snapshot => GetBool(snapshot, "RecordingFileGrowing")),
            VideoFramesSubmittedDelta =
                (GetNullableLong(finalRecordingSample, "FlashbackVideoFramesSubmittedToEncoder") ?? 0) -
                (GetNullableLong(firstRecordingSample, "FlashbackVideoFramesSubmittedToEncoder") ?? 0),
            VideoEncoderPacketsWrittenDelta =
                (GetNullableLong(finalRecordingSample, "FlashbackVideoEncoderPacketsWritten") ?? 0) -
                (GetNullableLong(firstRecordingSample, "FlashbackVideoEncoderPacketsWritten") ?? 0),
            IntegritySequenceGapsAtEnd = GetNullableLong(finalRecordingSample, "RecordingIntegritySequenceGaps") ?? 0,
            IntegrityQueueDroppedFramesAtEnd = GetNullableLong(finalRecordingSample, "RecordingIntegrityQueueDroppedFrames") ?? 0
        };
    }

    private sealed class FlashbackRecordingSessionMetrics
    {
        public int SampleCount { get; init; }
        public bool BackendObserved { get; init; }
        public bool FileGrowthObserved { get; init; }
        public long VideoFramesSubmittedDelta { get; init; }
        public long VideoEncoderPacketsWrittenDelta { get; init; }
        public long IntegritySequenceGapsAtEnd { get; init; }
        public long IntegrityQueueDroppedFramesAtEnd { get; init; }
    }

    private static async Task SampleLoopAsync(
        int durationSeconds,
        int sampleIntervalMs,
        List<DiagnosticSessionSample> samples,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var duration = TimeSpan.FromSeconds(durationSeconds);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                samples.Add(new DiagnosticSessionSample
                {
                    OffsetMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Snapshot = snapshot.Clone()
                });
            }

            var elapsed = Stopwatch.GetElapsedTime(started);
            if (elapsed >= duration)
            {
                break;
            }

            var remaining = duration - elapsed;
            var delay = TimeSpan.FromMilliseconds(Math.Min(sampleIntervalMs, Math.Max(1, remaining.TotalMilliseconds)));
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static object BuildFrameLedgerTrace(string sessionId, IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var events = new List<JsonElement>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sample in samples)
        {
            if (!sample.Snapshot.TryGetProperty("FrameLedgerRecentEvents", out var recentEvents) ||
                recentEvents.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in recentEvents.EnumerateArray())
            {
                var key =
                    $"{AutomationSnapshotFormatter.Get(item, "SourceSequence")}|{AutomationSnapshotFormatter.Get(item, "Stage")}|{AutomationSnapshotFormatter.Get(item, "QpcTimestamp")}";
                if (seen.Add(key))
                {
                    events.Add(item.Clone());
                }
            }
        }

        return new
        {
            SessionId = sessionId,
            SampleCount = samples.Count,
            EventCount = events.Count,
            Events = events
        };
    }

    private static int GetMaxSnapshotInt(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot,
        string propertyName)
    {
        var max = GetInt(lastSnapshot, propertyName);
        foreach (var sample in samples)
        {
            max = Math.Max(max, GetInt(sample.Snapshot, propertyName));
        }

        return max;
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions.Pretty);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryGetSnapshot(JsonElement response, out JsonElement snapshot)
    {
        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("Snapshot", out snapshot) &&
            snapshot.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        snapshot = default;
        return false;
    }

    private static bool TryGetVerification(JsonElement response, out JsonElement verification)
    {
        verification = default;
        if (response.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (response.TryGetProperty("Data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("Verification", out verification) &&
            verification.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        return response.TryGetProperty("Snapshot", out var snapshot) &&
               snapshot.ValueKind == JsonValueKind.Object &&
               snapshot.TryGetProperty("LastVerification", out verification) &&
               verification.ValueKind == JsonValueKind.Object;
    }

    private static string NormalizeScenario(string? scenario)
    {
        var normalized = string.IsNullOrWhiteSpace(scenario)
            ? "observe"
            : scenario.Trim().ToLowerInvariant();
        return normalized switch
        {
            "observe" or "preview-only" or "recording-only" or "flashback" or "flashback-stress" or "flashback-scrub-stress" or "flashback-restart-cycle" or "flashback-encoder-cycle" or "flashback-export-playback" or "flashback-segment-playback" or "flashback-range-export" or "flashback-lifecycle" or "flashback-export-concurrent" or "flashback-disable-during-export" or "flashback-preview-cycle" or "flashback-recording" or "flashback-recording-preview-cycle" or "flashback-recording-settings-deferred" or "flashback-recording-export-rejected" or "flashback-export-rejected" or "combined" => normalized,
            _ => throw new ArgumentException($"Unknown diagnostic session scenario '{scenario}'.", nameof(scenario))
        };
    }

    private static bool ScenarioNeedsPreview(string scenario)
        => scenario is "preview-only" or "flashback" or "flashback-stress" or "flashback-scrub-stress" or "flashback-restart-cycle" or "flashback-encoder-cycle" or "flashback-export-playback" or "flashback-segment-playback" or "flashback-range-export" or "flashback-lifecycle" or "flashback-export-concurrent" or "flashback-disable-during-export" or "flashback-preview-cycle" or "flashback-recording" or "flashback-recording-preview-cycle" or "flashback-recording-settings-deferred" or "flashback-recording-export-rejected" or "combined";

    private static bool ScenarioNeedsRecording(string scenario)
        => scenario is "recording-only" or "flashback-recording" or "flashback-recording-preview-cycle" or "flashback-recording-settings-deferred" or "flashback-recording-export-rejected" or "combined";

    private static bool ScenarioNeedsFlashback(string scenario)
        => scenario is "flashback" or "flashback-stress" or "flashback-scrub-stress" or "flashback-restart-cycle" or "flashback-encoder-cycle" or "flashback-export-playback" or "flashback-segment-playback" or "flashback-range-export" or "flashback-lifecycle" or "flashback-export-concurrent" or "flashback-disable-during-export" or "flashback-preview-cycle" or "flashback-recording" or "flashback-recording-preview-cycle" or "flashback-recording-settings-deferred" or "flashback-recording-export-rejected" or "combined";

}
