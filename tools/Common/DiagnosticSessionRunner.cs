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
        var runFlashbackStress = scenario == "flashback-stress";
        var runFlashbackRecording = scenario == "flashback-recording";
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

            if (ScenarioNeedsPreview(scenario) && !GetBool(initialSnapshot, "IsPreviewing"))
            {
                await SendAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
                startedPreview = true;
                actions.Add("preview started");
                await TryWaitAsync("VideoFramesFlowing", 15_000).ConfigureAwait(false);
            }

            if (ScenarioNeedsRecording(scenario) && !GetBool(initialSnapshot, "IsRecording"))
            {
                if (runFlashbackRecording &&
                    !await WaitForFlashbackStressBufferReadyAsync(SendAsync, cancellationToken).ConfigureAwait(false))
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
                    SendAsync,
                    cancellationToken);
                actions.Add("flashback stress started");
            }

            await SampleLoopAsync(durationSeconds, sampleIntervalMs, samples, SendAsync, cancellationToken).ConfigureAwait(false);

            if (flashbackStressTask is not null)
            {
                await flashbackStressTask.ConfigureAwait(false);
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
            }
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

        if (runFlashbackRecording)
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
                      (!(runFlashbackStress || runFlashbackRecording) || warnings.Count == 0),
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

        async Task<JsonElement> SendAsync(string command, Dictionary<string, object?>? payload, int? responseTimeoutMs)
        {
            await commandSendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var response = await sendCommandAsync(command, payload, responseTimeoutMs).ConfigureAwait(false);
                if (!AutomationSnapshotFormatter.IsSuccess(response))
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

    private static void ValidateFlashbackRecordingSession(
        IReadOnlyList<DiagnosticSessionSample> samples,
        List<string> warnings)
    {
        var recordingSamples = samples
            .Select(sample => sample.Snapshot)
            .Where(snapshot => GetBool(snapshot, "IsRecording"))
            .ToArray();
        if (recordingSamples.Length == 0)
        {
            warnings.Add("flashback recording: no recording samples captured");
            return;
        }

        if (!recordingSamples.Any(snapshot =>
                string.Equals(GetString(snapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("flashback recording: RecordingBackend never reported Flashback");
        }

        if (!recordingSamples.Any(snapshot => GetBool(snapshot, "RecordingFileGrowing")))
        {
            warnings.Add("flashback recording: recording file never reported growth");
        }

        var firstRecordingSample = recordingSamples[0];
        var finalRecordingSample = recordingSamples[^1];
        var submittedDelta =
            (GetNullableLong(finalRecordingSample, "FlashbackVideoFramesSubmittedToEncoder") ?? 0) -
            (GetNullableLong(firstRecordingSample, "FlashbackVideoFramesSubmittedToEncoder") ?? 0);
        if (submittedDelta <= 0)
        {
            warnings.Add("flashback recording: no Flashback video frames submitted to encoder");
        }

        var packetsDelta =
            (GetNullableLong(finalRecordingSample, "FlashbackVideoEncoderPacketsWritten") ?? 0) -
            (GetNullableLong(firstRecordingSample, "FlashbackVideoEncoderPacketsWritten") ?? 0);
        if (packetsDelta <= 0)
        {
            warnings.Add("flashback recording: no Flashback encoder packets written");
        }

        if (GetNullableLong(finalRecordingSample, "RecordingIntegritySequenceGaps") > 0)
        {
            warnings.Add("flashback recording: Flashback video sequence gaps were reported");
        }

        if (GetNullableLong(finalRecordingSample, "RecordingIntegrityQueueDroppedFrames") > 0)
        {
            warnings.Add("flashback recording: Flashback dropped frames were reported");
        }
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
            "observe" or "preview-only" or "recording-only" or "flashback" or "flashback-stress" or "flashback-recording" or "combined" => normalized,
            _ => throw new ArgumentException($"Unknown diagnostic session scenario '{scenario}'.", nameof(scenario))
        };
    }

    private static bool ScenarioNeedsPreview(string scenario)
        => scenario is "preview-only" or "flashback" or "flashback-stress" or "flashback-recording" or "combined";

    private static bool ScenarioNeedsRecording(string scenario)
        => scenario is "recording-only" or "flashback-recording" or "combined";

    private static bool ScenarioNeedsFlashback(string scenario)
        => scenario is "flashback" or "flashback-stress" or "flashback-recording" or "combined";

}
