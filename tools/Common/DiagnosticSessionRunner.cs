using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

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
    public bool Success { get; set; }
    public DateTimeOffset StartedUtc { get; init; }
    public DateTimeOffset CompletedUtc { get; set; }
    public string TerminalState { get; set; } = "unknown";
    public string LastStage { get; set; } = string.Empty;
    public string? UnhandledException { get; set; }
    public int RunnerProcessId { get; init; }
    public int DurationSeconds { get; init; }
    public int SampleIntervalMs { get; init; }
    public int SampleCount { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
    public string LivePath { get; init; } = string.Empty;
    public string SummaryPath { get; init; } = string.Empty;
    public string SamplesPath { get; init; } = string.Empty;
    public string FrameLedgerPath { get; init; } = string.Empty;
    public string TimelinePath { get; init; } = string.Empty;
    public string HealthStatus { get; init; } = "Unknown";
    public string LikelyStage { get; init; } = "diagnostic_unavailable";
    public string Summary { get; init; } = string.Empty;
    public string Evidence { get; init; } = string.Empty;
    public string SelectedResolutionAtEnd { get; init; } = string.Empty;
    public double SelectedFrameRateAtEnd { get; init; }
    public string SelectedFriendlyFrameRateAtEnd { get; init; } = string.Empty;
    public string SelectedExactFrameRateArgAtEnd { get; init; } = string.Empty;
    public string SelectedVideoFormatAtEnd { get; init; } = string.Empty;
    public string VideoRequestedSubtypeAtEnd { get; init; } = string.Empty;
    public string VideoNegotiatedSubtypeAtEnd { get; init; } = string.Empty;
    public int SourceWidthAtEnd { get; init; }
    public int SourceHeightAtEnd { get; init; }
    public double DetectedSourceFrameRateAtEnd { get; init; }
    public string DetectedSourceFrameRateArgAtEnd { get; init; } = string.Empty;
    public bool SourceIsHdrAtEnd { get; init; }
    public string SourceTelemetrySummaryAtEnd { get; init; } = string.Empty;
    public int FlashbackPlaybackPendingCommandsAtEnd { get; init; }
    public int FlashbackPlaybackMaxPendingCommandsObserved { get; init; }
    public int FlashbackPlaybackMaxCommandQueueLatencyMsObserved { get; init; }
    public long FlashbackPlaybackCommandsDroppedAtEnd { get; init; }
    public long FlashbackPlaybackCommandsSkippedNotReadyAtEnd { get; init; }
    public long FlashbackPlaybackScrubUpdatesCoalescedAtEnd { get; init; }
    public long FlashbackPlaybackSeekCommandsCoalescedAtEnd { get; init; }
    public string FlashbackPlaybackLastCommandFailureAtEnd { get; init; } = string.Empty;
    public long FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd { get; init; }
    public double FlashbackPlaybackObservedFpsAtEnd { get; init; }
    public double FlashbackPlaybackMinObservedFpsObserved { get; init; }
    public double FlashbackPlaybackAvgFrameMsAtEnd { get; init; }
    public double FlashbackPlaybackP99FrameMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxFrameMsAtEnd { get; init; }
    public double FlashbackPlaybackOnePercentLowFpsAtEnd { get; init; }
    public double FlashbackPlaybackMinOnePercentLowFpsObserved { get; init; }
    public long FlashbackPlaybackMinOnePercentLowOffsetMs { get; init; }
    public long FlashbackPlaybackMinOnePercentLowFrameCount { get; init; }
    public double FlashbackPlaybackMinOnePercentLowP99FrameMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowMaxFrameMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowDecodeP99Ms { get; init; }
    public double FlashbackPlaybackMinOnePercentLowDecodeMaxMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowAvDriftMs { get; init; }
    public long FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks { get; init; }
    public double FlashbackPlaybackMaxP99FrameMsObserved { get; init; }
    public double FlashbackPlaybackMaxFrameMsObserved { get; init; }
    public double FlashbackPlaybackMaxSlowFramePercentObserved { get; init; }
    public double FlashbackPlaybackDecodeAvgMsAtEnd { get; init; }
    public double FlashbackPlaybackDecodeP95MsAtEnd { get; init; }
    public double FlashbackPlaybackDecodeP99MsAtEnd { get; init; }
    public double FlashbackPlaybackDecodeMaxMsAtEnd { get; init; }
    public string FlashbackPlaybackMaxDecodePhaseAtEnd { get; init; } = string.Empty;
    public double FlashbackPlaybackMaxDecodeReceiveMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeFeedMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeReadMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeSendMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeAudioMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeConvertMsAtEnd { get; init; }
    public long FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd { get; init; }
    public long FlashbackPlaybackMaxDecodePositionMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeP99MsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeMsObserved { get; init; }
    public string FlashbackPlaybackMaxDecodePhaseObserved { get; init; } = string.Empty;
    public double FlashbackPlaybackMaxDecodeReceiveMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeFeedMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeReadMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeSendMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeAudioMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeConvertMsObserved { get; init; }
    public long FlashbackPlaybackMaxDecodeUtcUnixMsObserved { get; init; }
    public long FlashbackPlaybackMaxDecodePositionMsObserved { get; init; }
    public long FlashbackPlaybackFrameCountAtEnd { get; init; }
    public long FlashbackPlaybackLateFramesAtEnd { get; init; }
    public long FlashbackPlaybackSlowFramesAtEnd { get; init; }
    public double FlashbackPlaybackSlowFramePercentAtEnd { get; init; }
    public long FlashbackPlaybackDroppedFramesAtEnd { get; init; }
    public long FlashbackPlaybackDroppedFramesDelta { get; init; }
    public long FlashbackPlaybackAudioMasterDelayDoublesAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterDelayShrinksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterFallbacksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterStaleFallbacksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd { get; init; }
    public string FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd { get; init; } = string.Empty;
    public double FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd { get; init; }
    public long FlashbackPlaybackMaxAudioMasterDelayDoublesObserved { get; init; }
    public long FlashbackPlaybackMaxAudioMasterDelayShrinksObserved { get; init; }
    public long FlashbackPlaybackMaxAudioMasterFallbacksObserved { get; init; }
    public double FlashbackPlaybackMaxAudioBufferedDurationMsObserved { get; init; }
    public double FlashbackPlaybackMaxAudioQueueDurationMsObserved { get; init; }
    public double FlashbackPlaybackMaxAbsAvDriftMsObserved { get; init; }
    public long FlashbackPlaybackSubmitFailuresAtEnd { get; init; }
    public long FlashbackPlaybackSubmitFailuresDelta { get; init; }
    public long FlashbackPlaybackSegmentSwitchesAtEnd { get; init; }
    public long FlashbackPlaybackFmp4ReopensAtEnd { get; init; }
    public long FlashbackPlaybackWriteHeadWaitsAtEnd { get; init; }
    public long FlashbackPlaybackNearLiveSnapsAtEnd { get; init; }
    public long FlashbackPlaybackDecodeErrorSnapsAtEnd { get; init; }
    public long FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd { get; init; }
    public bool FlashbackRecordingBackendObserved { get; init; }
    public bool FlashbackRecordingFileGrowthObserved { get; init; }
    public long FlashbackRecordingVideoFramesSubmittedDelta { get; init; }
    public long FlashbackRecordingVideoEncoderPacketsWrittenDelta { get; init; }
    public long FlashbackRecordingIntegritySequenceGapsAtEnd { get; init; }
    public long FlashbackRecordingIntegrityQueueDroppedFramesAtEnd { get; init; }
    public long FlashbackRecordingIntegritySequenceGapsDelta { get; init; }
    public long FlashbackRecordingIntegrityQueueDroppedFramesDelta { get; init; }
    public bool FlashbackExportObserved { get; init; }
    public bool FlashbackExportActiveAtEnd { get; init; }
    public string FlashbackExportStatusAtEnd { get; init; } = string.Empty;
    public string FlashbackExportMessageAtEnd { get; init; } = string.Empty;
    public string FlashbackExportFailureKindAtEnd { get; init; } = string.Empty;
    public string FlashbackExportOutputPathAtEnd { get; init; } = string.Empty;
    public long LastExportIdAtEnd { get; init; }
    public string LastExportSuccessAtEnd { get; init; } = string.Empty;
    public string LastExportMessageAtEnd { get; init; } = string.Empty;
    public long FlashbackExportMaxElapsedMsObserved { get; init; }
    public long FlashbackExportMaxLastProgressAgeMsObserved { get; init; }
    public long FlashbackExportMaxOutputBytesObserved { get; init; }
    public double FlashbackExportMaxThroughputBytesPerSecObserved { get; init; }
    public double PreviewCadenceOnePercentLowFpsAtEnd { get; init; }
    public double PreviewCadenceMinOnePercentLowFpsObserved { get; init; }
    public long PreviewD3DFrameStatsMissedRefreshDelta { get; init; }
    public long PreviewD3DFrameStatsFailureDelta { get; init; }
    public long PreviewSchedulerDroppedAtEnd { get; init; }
    public long PreviewSchedulerDeadlineDropsAtEnd { get; init; }
    public long PreviewSchedulerClearedDropsAtEnd { get; init; }
    public long PreviewSchedulerUnderflowsAtEnd { get; init; }
    public long PreviewSchedulerDroppedDelta { get; init; }
    public long PreviewSchedulerDeadlineDropsDelta { get; init; }
    public long PreviewSchedulerClearedDropsDelta { get; init; }
    public long PreviewSchedulerUnderflowsDelta { get; init; }
    public string PreviewSchedulerLastDropReasonAtEnd { get; init; } = string.Empty;
    public string PreviewSchedulerLastUnderflowReasonAtEnd { get; init; } = string.Empty;
    public double PreviewSchedulerLastUnderflowInputAgeMsAtEnd { get; init; }
    public double PreviewSchedulerLastUnderflowOutputAgeMsAtEnd { get; init; }
    public double PreviewSchedulerMaxScheduleLateMsObserved { get; init; }
    public long PreviewSchedulerScheduleLateDelta { get; init; }
    public int PreviewD3DMaxRecentSlowFramesObserved { get; init; }
    public string PreviewD3DLatestSlowFrameReason { get; init; } = string.Empty;
    public double PreviewD3DLatestSlowFrameOverBudgetMs { get; init; }
    public double PreviewD3DLatestSlowFramePresentIntervalMs { get; init; }
    public double PreviewD3DLatestSlowFrameTotalFrameCpuMs { get; init; }
    public double PreviewD3DLatestSlowFramePresentCallMs { get; init; }
    public int PreviewD3DLatestSlowFramePendingFrameCount { get; init; }
    public double PreviewD3DInputUploadCpuP99MsAtEnd { get; init; }
    public double PreviewD3DInputUploadCpuMaxMsObserved { get; init; }
    public double PreviewD3DRenderSubmitCpuP99MsAtEnd { get; init; }
    public double PreviewD3DRenderSubmitCpuMaxMsObserved { get; init; }
    public double PreviewD3DPresentCallP99MsAtEnd { get; init; }
    public double PreviewD3DPresentCallMaxMsObserved { get; init; }
    public double PreviewD3DTotalFrameCpuP99MsAtEnd { get; init; }
    public double PreviewD3DTotalFrameCpuMaxMsObserved { get; init; }
    public double VisualCadenceOutputFpsAtEnd { get; init; }
    public double VisualCadenceChangeFpsAtEnd { get; init; }
    public double VisualCadenceMinChangeFpsObserved { get; init; }
    public double VisualCadenceRepeatPercentAtEnd { get; init; }
    public double VisualCadenceMaxRepeatPercentObserved { get; init; }
    public long VisualCadenceRepeatFramesAtEnd { get; init; }
    public long VisualCadenceLongestRepeatRunAtEnd { get; init; }
    public double ProcessCpuPercentAtEnd { get; init; }
    public double ProcessCpuMaxPercentObserved { get; init; }
    public bool RecordingVerificationRun { get; init; }
    public bool? RecordingVerificationSucceeded { get; init; }
    public string? RecordingVerificationMessage { get; init; }
    public PresentMonProbeResult? PresentMon { get; init; }
    public string[] Actions { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();
}

public sealed class DiagnosticSessionSample
{
    public long OffsetMs { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public JsonElement Snapshot { get; init; }
}

public static class DiagnosticSessionRunner
{
    private const int FlashbackStressMaxPlaybackPendingCommands = 4;
    private const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;
    private const double FlashbackStressPlaybackWarmSeconds = 10.0;
    private const long FlashbackStressAudioUnavailableFallbackAllowance = 2;
    private const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;

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

    private readonly record struct PlaybackCommandHealth(
        long Dropped,
        long Skipped,
        long SubmitFailures,
        long CoalescedScrub,
        long CoalescedSeek,
        long NonCoalescedDropped);

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
        var runFlashbackPlayback = scenario == "flashback-playback";
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
        var runFlashbackRotatedExport = scenario == "flashback-rotated-export";
        var runFlashbackPreviewCycle = scenario == "flashback-preview-cycle";
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
        Task? flashbackExportConcurrentTask = null;
        Task? flashbackDisableDuringExportTask = null;
        Task? flashbackRotatedExportTask = null;
        Task? flashbackPreviewCycleTask = null;
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

        await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        JsonElement initialSnapshot = CreateEmptyJsonObject();
        var initialSnapshotKnown = false;
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
            if (!options.LeaveRunning)
            {
                if (startedRecording)
                {
                    try
                    {
                        SetStage("cleanup-stop-recording");
                        using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(45));
                        var stopResponse = await SendWithTokenAsync("SetRecordingEnabled", new Dictionary<string, object?> { ["enabled"] = false }, 45_000, false, cleanupCts.Token).ConfigureAwait(false);
                        actions.Add("recording stopped");
                        if (AutomationSnapshotFormatter.IsSuccess(stopResponse))
                        {
                            await TryWaitWithTokenAsync("RecordingStopped", 30_000, cleanupCts.Token).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        RecordTerminalException(ex, "cleanup-stop-recording");
                    }
                }

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
                        using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
                        await SendWithTokenAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = false }, 15_000, false, cleanupCts.Token).ConfigureAwait(false);
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
                        using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
                        await SendWithTokenAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = true }, 15_000, false, cleanupCts.Token).ConfigureAwait(false);
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

        if (options.VerifyRecording || startedRecording)
        {
            try
            {
                SetStage("recording-verification");
                var verificationCommand = "VerifyLastRecording";
                Dictionary<string, object?>? verificationPayload = null;
                if (!startedRecording &&
                    TryGetFlashbackExportVerificationPath(scenario, outputDirectory, out var exportVerificationPath))
                {
                    verificationCommand = "VerifyFile";
                    verificationPayload = new Dictionary<string, object?>
                    {
                        ["filePath"] = exportVerificationPath,
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
        var healthStatus = GetString(healthSnapshot, "DiagnosticHealthStatus") ?? "Unknown";
        var likelyStage = GetString(healthSnapshot, "DiagnosticLikelyStage") ?? "diagnostic_unavailable";
        var summary = GetString(healthSnapshot, "DiagnosticSummary") ?? string.Empty;
        var evidence = GetString(healthSnapshot, "DiagnosticEvidence") ?? string.Empty;
        var playbackSessionMetrics = BuildFlashbackPlaybackSessionMetrics(initialSnapshot, samples, lastSnapshot);
        var playbackEndSnapshot = playbackSessionMetrics.EndSnapshot;
        var playbackPendingAtEnd = playbackSessionMetrics.Observed
            ? GetInt(playbackEndSnapshot, "FlashbackPlaybackPendingCommands")
            : 0;
        var playbackMaxPendingObserved = playbackSessionMetrics.MaxPendingCommandsObserved;
        var playbackMaxLatencyObserved = playbackSessionMetrics.MaxCommandQueueLatencyMsObserved;
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
        if (runFlashbackPlayback)
        {
            ValidateFlashbackPlaybackSession(playbackSessionMetrics.Observed ? playbackEndSnapshot : lastSnapshot, playbackSessionMetrics, durationSeconds, warnings);
        }

        var recordingMetrics = BuildFlashbackRecordingMetrics(initialSnapshot, samples);
        var exportMetrics = BuildFlashbackExportSessionMetrics(initialSnapshot, samples, lastSnapshot);
        var previewCadenceMetrics = BuildPreviewCadenceSessionMetrics(samples, lastSnapshot);
        var previewD3DMetrics = BuildPreviewD3DMetrics(initialSnapshot, lastSnapshot, samples);
        var visualCadenceMetrics = BuildVisualCadenceSessionMetrics(samples, lastSnapshot);
        var previewSchedulerDroppedAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterTotalDropped") ?? 0;
        var previewSchedulerDeadlineDropsAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterDeadlineDropCount") ?? 0;
        var previewSchedulerClearedDropsAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterClearedDropCount") ?? 0;
        var previewSchedulerUnderflowsAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterUnderflowCount") ?? 0;
        var previewSchedulerDroppedDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterTotalDropped");
        var previewSchedulerDeadlineDropsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterDeadlineDropCount");
        var previewSchedulerClearedDropsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterClearedDropCount");
        var previewSchedulerUnderflowsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterUnderflowCount");
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
            runFlashbackLifecycle ||
            runFlashbackExportConcurrent ||
            runFlashbackDisableDuringExport ||
            runFlashbackRotatedExport ||
            runFlashbackPreviewCycle ||
            runFlashbackRecording ||
            runFlashbackRecordingPreviewCycle ||
            runFlashbackRecordingSettingsDeferred ||
            runFlashbackRecordingExportRejected ||
            runFlashbackExportRejected;
        if (isFlashbackScenario)
        {
            ValidateFlashbackPreviewScheduler(
                previewSchedulerDeadlineDropsDelta,
                previewSchedulerUnderflowsDelta,
                previewD3DMetrics.StatsFailureDelta,
                warnings);
        }

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
                      (presentMon is null || presentMon.Success) &&
                      (!verificationSucceeded.HasValue || verificationSucceeded.Value) &&
                      (!(runFlashbackPlayback || runFlashbackStress || runFlashbackScrubStress || runFlashbackRestartCycle || runFlashbackEncoderCycle || runFlashbackExportPlayback || runFlashbackSegmentPlayback || runFlashbackRangeExport || runFlashbackLifecycle || runFlashbackExportConcurrent || runFlashbackDisableDuringExport || runFlashbackRotatedExport || runFlashbackPreviewCycle || runFlashbackRecording || runFlashbackRecordingPreviewCycle || runFlashbackRecordingSettingsDeferred || runFlashbackRecordingExportRejected || runFlashbackExportRejected) || warnings.Count == 0),
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
            PreviewSchedulerDroppedDelta = previewSchedulerDroppedDelta,
            PreviewSchedulerDeadlineDropsDelta = previewSchedulerDeadlineDropsDelta,
            PreviewSchedulerClearedDropsDelta = previewSchedulerClearedDropsDelta,
            PreviewSchedulerUnderflowsDelta = previewSchedulerUnderflowsDelta,
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
                            CommandFailureCount = commandFailureCount,
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
            await ObserveTaskAfterFaultAsync(flashbackExportConcurrentTask, "flashback-export-concurrent-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackDisableDuringExportTask, "flashback-disable-during-export-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackRotatedExportTask, "flashback-rotated-export-task").ConfigureAwait(false);
            await ObserveTaskAfterFaultAsync(flashbackPreviewCycleTask, "flashback-preview-cycle-task").ConfigureAwait(false);
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

    public static string Format(DiagnosticSessionResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"== Diagnostic Session: {(result.Success ? "PASS" : "FAIL")} ==");
        builder.AppendLine($"Scenario: {result.Scenario} | Duration: {result.DurationSeconds}s | Samples: {result.SampleCount} @ {result.SampleIntervalMs}ms");
        builder.AppendLine($"Terminal: {result.TerminalState} | LastStage: {result.LastStage} | RunnerPid: {result.RunnerProcessId}");
        if (!string.IsNullOrWhiteSpace(result.UnhandledException))
        {
            builder.AppendLine($"Terminal Exception: {result.UnhandledException}");
        }

        builder.AppendLine($"Health: {result.HealthStatus} | Stage: {result.LikelyStage}");
        if (!string.IsNullOrWhiteSpace(result.Summary))
        {
            builder.AppendLine($"Summary: {result.Summary}");
        }

        if (!string.IsNullOrWhiteSpace(result.Evidence))
        {
            builder.AppendLine($"Evidence: {result.Evidence}");
        }

        builder.AppendLine(
            "Capture Mode: " +
            $"selected={FormatOptional(result.SelectedResolutionAtEnd)} @{FormatFrameRate(result.SelectedFrameRateAtEnd, result.SelectedFriendlyFrameRateAtEnd, result.SelectedExactFrameRateArgAtEnd)} " +
            $"format={FormatOptional(result.SelectedVideoFormatAtEnd)} requested={FormatOptional(result.VideoRequestedSubtypeAtEnd)} negotiated={FormatOptional(result.VideoNegotiatedSubtypeAtEnd)} " +
            $"source={result.SourceWidthAtEnd}x{result.SourceHeightAtEnd} @{FormatFrameRate(result.DetectedSourceFrameRateAtEnd, string.Empty, result.DetectedSourceFrameRateArgAtEnd)} " +
            $"hdr={result.SourceIsHdrAtEnd} telemetry={FormatOptional(result.SourceTelemetrySummaryAtEnd)}");

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
            $"skippedEnd={result.FlashbackPlaybackCommandsSkippedNotReadyAtEnd} " +
            $"coalescedScrubEnd={result.FlashbackPlaybackScrubUpdatesCoalescedAtEnd} " +
            $"coalescedSeekEnd={result.FlashbackPlaybackSeekCommandsCoalescedAtEnd} " +
            $"failureEnd={FormatOptional(result.FlashbackPlaybackLastCommandFailureAtEnd)} " +
            $"failureUtcEnd={result.FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd}");
        builder.AppendLine(
            "Flashback Playback Perf: " +
            $"fpsEnd={result.FlashbackPlaybackObservedFpsAtEnd:0.##} " +
            $"fpsMin={result.FlashbackPlaybackMinObservedFpsObserved:0.##} " +
            $"avgFrameMsEnd={result.FlashbackPlaybackAvgFrameMsAtEnd:0.##} " +
            $"p99FrameMsEnd={result.FlashbackPlaybackP99FrameMsAtEnd:0.##} " +
            $"maxFrameMsEnd={result.FlashbackPlaybackMaxFrameMsAtEnd:0.##} " +
            $"onePercentLowFpsEnd={result.FlashbackPlaybackOnePercentLowFpsAtEnd:0.##} " +
            $"onePercentLowFpsMin={result.FlashbackPlaybackMinOnePercentLowFpsObserved:0.##} " +
            $"onePercentLowMinOffsetMs={result.FlashbackPlaybackMinOnePercentLowOffsetMs} " +
            $"onePercentLowMinFrames={result.FlashbackPlaybackMinOnePercentLowFrameCount} " +
            $"onePercentLowMinP99FrameMs={result.FlashbackPlaybackMinOnePercentLowP99FrameMs:0.##} " +
            $"onePercentLowMinMaxFrameMs={result.FlashbackPlaybackMinOnePercentLowMaxFrameMs:0.##} " +
            $"onePercentLowMinDecodeP99Ms={result.FlashbackPlaybackMinOnePercentLowDecodeP99Ms:0.##} " +
            $"onePercentLowMinDecodeMaxMs={result.FlashbackPlaybackMinOnePercentLowDecodeMaxMs:0.##} " +
            $"onePercentLowMinAvDriftMs={result.FlashbackPlaybackMinOnePercentLowAvDriftMs:0.##} " +
            $"onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks} " +
            $"p99FrameMsMax={result.FlashbackPlaybackMaxP99FrameMsObserved:0.##} " +
            $"maxFrameMsObserved={result.FlashbackPlaybackMaxFrameMsObserved:0.##} " +
            $"framesEnd={result.FlashbackPlaybackFrameCountAtEnd} " +
            $"lateEnd={result.FlashbackPlaybackLateFramesAtEnd} " +
            $"slowEnd={result.FlashbackPlaybackSlowFramesAtEnd} " +
            $"slowPctEnd={result.FlashbackPlaybackSlowFramePercentAtEnd:0.##} " +
            $"slowPctMax={result.FlashbackPlaybackMaxSlowFramePercentObserved:0.##} " +
            $"droppedFramesEnd={result.FlashbackPlaybackDroppedFramesAtEnd} " +
            $"droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta} " +
            $"audioMasterDoubleEnd={result.FlashbackPlaybackAudioMasterDelayDoublesAtEnd} " +
            $"audioMasterDoubleMax={result.FlashbackPlaybackMaxAudioMasterDelayDoublesObserved} " +
            $"audioMasterShrinkEnd={result.FlashbackPlaybackAudioMasterDelayShrinksAtEnd} " +
            $"audioMasterShrinkMax={result.FlashbackPlaybackMaxAudioMasterDelayShrinksObserved} " +
            $"audioMasterFallbackEnd={result.FlashbackPlaybackAudioMasterFallbacksAtEnd} " +
            $"audioMasterFallbackMax={result.FlashbackPlaybackMaxAudioMasterFallbacksObserved} " +
            $"audioMasterUnavailableEnd={result.FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd} " +
            $"audioMasterStaleEnd={result.FlashbackPlaybackAudioMasterStaleFallbacksAtEnd} " +
            $"audioMasterDriftOutlierEnd={result.FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd} " +
            $"audioMasterLastFallback={FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)} " +
            $"audioMasterLastFallbackAgeMs={result.FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd:0.##} " +
            $"audioBufferedMsMax={result.FlashbackPlaybackMaxAudioBufferedDurationMsObserved:0.##} " +
            $"audioQueueMsMax={result.FlashbackPlaybackMaxAudioQueueDurationMsObserved:0.##} " +
            $"absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##} " +
            $"submitFailuresEnd={result.FlashbackPlaybackSubmitFailuresAtEnd} " +
            $"submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}");
        builder.AppendLine(
            "Flashback Playback Decode: " +
            $"avgMsEnd={result.FlashbackPlaybackDecodeAvgMsAtEnd:0.##} " +
            $"p95MsEnd={result.FlashbackPlaybackDecodeP95MsAtEnd:0.##} " +
            $"p99MsEnd={result.FlashbackPlaybackDecodeP99MsAtEnd:0.##} " +
            $"maxMsEnd={result.FlashbackPlaybackDecodeMaxMsAtEnd:0.##} " +
            $"phaseEnd={result.FlashbackPlaybackMaxDecodePhaseAtEnd} " +
            $"receiveMsEnd={result.FlashbackPlaybackMaxDecodeReceiveMsAtEnd:0.##} " +
            $"feedMsEnd={result.FlashbackPlaybackMaxDecodeFeedMsAtEnd:0.##} " +
            $"readMsEnd={result.FlashbackPlaybackMaxDecodeReadMsAtEnd:0.##} " +
            $"sendMsEnd={result.FlashbackPlaybackMaxDecodeSendMsAtEnd:0.##} " +
            $"audioMsEnd={result.FlashbackPlaybackMaxDecodeAudioMsAtEnd:0.##} " +
            $"convertMsEnd={result.FlashbackPlaybackMaxDecodeConvertMsAtEnd:0.##} " +
            $"maxPosEnd={result.FlashbackPlaybackMaxDecodePositionMsAtEnd}ms " +
            $"p99MsMax={result.FlashbackPlaybackMaxDecodeP99MsObserved:0.##} " +
            $"maxMsObserved={result.FlashbackPlaybackMaxDecodeMsObserved:0.##} " +
            $"phaseObserved={result.FlashbackPlaybackMaxDecodePhaseObserved} " +
            $"receiveMsObserved={result.FlashbackPlaybackMaxDecodeReceiveMsObserved:0.##} " +
            $"feedMsObserved={result.FlashbackPlaybackMaxDecodeFeedMsObserved:0.##} " +
            $"readMsObserved={result.FlashbackPlaybackMaxDecodeReadMsObserved:0.##} " +
            $"sendMsObserved={result.FlashbackPlaybackMaxDecodeSendMsObserved:0.##} " +
            $"audioMsObserved={result.FlashbackPlaybackMaxDecodeAudioMsObserved:0.##} " +
            $"convertMsObserved={result.FlashbackPlaybackMaxDecodeConvertMsObserved:0.##} " +
            $"maxPosObserved={result.FlashbackPlaybackMaxDecodePositionMsObserved}ms");
        builder.AppendLine(
            "Flashback Playback Stages: " +
            $"switchesEnd={result.FlashbackPlaybackSegmentSwitchesAtEnd} " +
            $"fmp4ReopensEnd={result.FlashbackPlaybackFmp4ReopensAtEnd} " +
            $"writeHeadWaitsEnd={result.FlashbackPlaybackWriteHeadWaitsAtEnd} " +
            $"nearLiveSnapsEnd={result.FlashbackPlaybackNearLiveSnapsAtEnd} " +
            $"decodeErrorSnapsEnd={result.FlashbackPlaybackDecodeErrorSnapsAtEnd} " +
            $"lastWriteHeadGapMsEnd={result.FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd}");
        builder.AppendLine(
            "Flashback Recording: " +
            $"backendObserved={result.FlashbackRecordingBackendObserved} " +
            $"fileGrowthObserved={result.FlashbackRecordingFileGrowthObserved} " +
            $"submittedDelta={result.FlashbackRecordingVideoFramesSubmittedDelta} " +
            $"packetsDelta={result.FlashbackRecordingVideoEncoderPacketsWrittenDelta} " +
            $"seqGapsEnd={result.FlashbackRecordingIntegritySequenceGapsAtEnd} " +
            $"seqGapsDelta={result.FlashbackRecordingIntegritySequenceGapsDelta} " +
            $"queueDropsEnd={result.FlashbackRecordingIntegrityQueueDroppedFramesAtEnd} " +
            $"queueDropsDelta={result.FlashbackRecordingIntegrityQueueDroppedFramesDelta}");
        builder.AppendLine(
            "Flashback Export: " +
            $"observed={result.FlashbackExportObserved} " +
            $"activeEnd={result.FlashbackExportActiveAtEnd} " +
            $"statusEnd={FormatOptional(result.FlashbackExportStatusAtEnd)} " +
            $"failureKindEnd={FormatOptional(result.FlashbackExportFailureKindAtEnd)} " +
            $"messageEnd={FormatOptional(result.FlashbackExportMessageAtEnd)} " +
            $"lastResultIdEnd={result.LastExportIdAtEnd} " +
            $"lastSuccessEnd={FormatOptional(result.LastExportSuccessAtEnd)} " +
            $"lastMessageEnd={FormatOptional(result.LastExportMessageAtEnd)} " +
            $"pathEnd={FormatOptional(result.FlashbackExportOutputPathAtEnd)} " +
            $"maxElapsedMs={result.FlashbackExportMaxElapsedMsObserved} " +
            $"maxProgressAgeMs={result.FlashbackExportMaxLastProgressAgeMsObserved} " +
            $"maxBytes={FormatBytes(result.FlashbackExportMaxOutputBytesObserved)} " +
            $"maxThroughput={FormatBytes((long)result.FlashbackExportMaxThroughputBytesPerSecObserved)}/s");
        builder.AppendLine(
            "Preview Scheduler: " +
            $"droppedEnd={result.PreviewSchedulerDroppedAtEnd} " +
            $"droppedDelta={result.PreviewSchedulerDroppedDelta} " +
            $"clearedDropsEnd={result.PreviewSchedulerClearedDropsAtEnd} " +
            $"clearedDropsDelta={result.PreviewSchedulerClearedDropsDelta} " +
            $"deadlineDropsEnd={result.PreviewSchedulerDeadlineDropsAtEnd} " +
            $"deadlineDropsDelta={result.PreviewSchedulerDeadlineDropsDelta} " +
            $"underflowsEnd={result.PreviewSchedulerUnderflowsAtEnd} " +
            $"underflowsDelta={result.PreviewSchedulerUnderflowsDelta} " +
            $"lastUnderflowReasonEnd={FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)} " +
            $"lastUnderflowInputAgeMsEnd={result.PreviewSchedulerLastUnderflowInputAgeMsAtEnd:0.##} " +
            $"lastUnderflowOutputAgeMsEnd={result.PreviewSchedulerLastUnderflowOutputAgeMsAtEnd:0.##} " +
            $"scheduleLateMaxMsObserved={result.PreviewSchedulerMaxScheduleLateMsObserved:0.##} " +
            $"scheduleLateDelta={result.PreviewSchedulerScheduleLateDelta} " +
            $"lastDropReasonEnd={FormatOptional(result.PreviewSchedulerLastDropReasonAtEnd)}");
        builder.AppendLine(
            "Preview D3D Perf: " +
            $"onePercentLowFpsEnd={result.PreviewCadenceOnePercentLowFpsAtEnd:0.##} " +
            $"onePercentLowFpsMin={result.PreviewCadenceMinOnePercentLowFpsObserved:0.##} " +
            $"missedRefreshDelta={result.PreviewD3DFrameStatsMissedRefreshDelta} " +
            $"statsFailureDelta={result.PreviewD3DFrameStatsFailureDelta} " +
            $"maxRecentSlowFrames={result.PreviewD3DMaxRecentSlowFramesObserved} " +
            $"latestSlowReason={FormatOptional(result.PreviewD3DLatestSlowFrameReason)} " +
            $"overBudgetMs={result.PreviewD3DLatestSlowFrameOverBudgetMs:0.##} " +
            $"presentIntervalMs={result.PreviewD3DLatestSlowFramePresentIntervalMs:0.##} " +
            $"totalFrameCpuMs={result.PreviewD3DLatestSlowFrameTotalFrameCpuMs:0.##} " +
            $"presentCallMs={result.PreviewD3DLatestSlowFramePresentCallMs:0.##} " +
            $"pending={result.PreviewD3DLatestSlowFramePendingFrameCount}");
        builder.AppendLine(
            "Preview D3D CPU Timing: " +
            $"inputUploadP99End={result.PreviewD3DInputUploadCpuP99MsAtEnd:0.##} " +
            $"inputUploadMaxObserved={result.PreviewD3DInputUploadCpuMaxMsObserved:0.##} " +
            $"renderSubmitP99End={result.PreviewD3DRenderSubmitCpuP99MsAtEnd:0.##} " +
            $"renderSubmitMaxObserved={result.PreviewD3DRenderSubmitCpuMaxMsObserved:0.##} " +
            $"presentCallP99End={result.PreviewD3DPresentCallP99MsAtEnd:0.##} " +
            $"presentCallMaxObserved={result.PreviewD3DPresentCallMaxMsObserved:0.##} " +
            $"totalFrameP99End={result.PreviewD3DTotalFrameCpuP99MsAtEnd:0.##} " +
            $"totalFrameMaxObserved={result.PreviewD3DTotalFrameCpuMaxMsObserved:0.##}");
        builder.AppendLine(
            "Preview Visual Cadence: " +
            $"outputFpsEnd={result.VisualCadenceOutputFpsAtEnd:0.##} " +
            $"changeFpsEnd={result.VisualCadenceChangeFpsAtEnd:0.##} " +
            $"changeFpsMin={result.VisualCadenceMinChangeFpsObserved:0.##} " +
            $"repeatPctEnd={result.VisualCadenceRepeatPercentAtEnd:0.###} " +
            $"repeatPctMax={result.VisualCadenceMaxRepeatPercentObserved:0.###} " +
            $"repeatFramesEnd={result.VisualCadenceRepeatFramesAtEnd} " +
            $"longestRepeatRunEnd={result.VisualCadenceLongestRepeatRunAtEnd}");
        builder.AppendLine(
            "Process Perf: " +
            $"cpuPercentEnd={result.ProcessCpuPercentAtEnd:0.##} " +
            $"cpuPercentMaxObserved={result.ProcessCpuMaxPercentObserved:0.##}");

        builder.AppendLine($"Artifacts: {result.OutputDirectory}");
        builder.AppendLine($"  Live: {result.LivePath}");
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

            if (warmedAudioFallbackDelta > 0)
            {
                if (warmedAudioStaleDelta > 0 || warmedAudioDriftOutlierDelta > 0)
                {
                    warnings.Add(
                        "flashback stress: audio-master harmful fallbacks increased during warmed playback " +
                        $"staleDelta={warmedAudioStaleDelta} driftOutlierDelta={warmedAudioDriftOutlierDelta} " +
                        $"totalDelta={warmedAudioFallbackDelta}");
                }
                else if (warmedAudioUnavailableDelta > FlashbackStressAudioUnavailableFallbackAllowance)
                {
                    warnings.Add(
                        "flashback stress: audio-master unavailable fallbacks exceeded startup allowance " +
                        $"unavailableDelta={warmedAudioUnavailableDelta} allowance={FlashbackStressAudioUnavailableFallbackAllowance} " +
                        $"totalDelta={warmedAudioFallbackDelta}");
                }
                else if (warmedAudioUnavailableDelta <= 0)
                {
                    warnings.Add($"flashback stress: audio-master unclassified fallbacks increased during warmed playback delta={warmedAudioFallbackDelta}");
                }
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
                !GetBool(lastSnapshot, "FlashbackPlaybackThreadAlive") &&
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
            var threadAlive = GetBool(lastSnapshot, "FlashbackPlaybackThreadAlive");
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
            new Dictionary<string, object?> { ["seconds"] = 3, ["outputPath"] = exportPath },
            60_000);

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
                return (await sendCommandAsync(command, payload, responseTimeoutMs)
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false))
                    .Clone();
            }
            catch (AutomationPipeConnectException ex)
            {
                lastConnectException = ex;
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
            catch (AutomationPipeException ex) when (ex is not AutomationPipeConnectException)
            {
                return BuildLocalFailureResponse(command, ex.Message);
            }
            catch (JsonException ex)
            {
                return BuildLocalFailureResponse(command, ex.Message);
            }
        }

        if (lastConnectException is not null)
        {
            return BuildLocalFailureResponse(command, lastConnectException.Message);
        }

        return BuildLocalFailureResponse(command, "command was not attempted before retry timeout elapsed");
    }

    private static JsonElement CreateEmptyJsonObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
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

        var commandHealth = BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot);
        var state = GetString(lastSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        var threadAlive = GetBool(lastSnapshot, "FlashbackPlaybackThreadAlive");
        var maxPending = GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands");
        var maxLatencyMs = GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs");

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
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback range export: Flashback buffer did not become range-ready within 30s");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);

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
                CreateFlashbackExportVerifyPayload(exportPath),
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
        var commandHealth = BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot);
        var state = GetString(finalSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (pending > 0)
        {
            warnings.Add($"flashback range export: pending commands remained after go-live pending={pending}");
        }

        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0)
        {
            warnings.Add(
                "flashback range export: " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures}");
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

    private static void ValidateFlashbackPlaybackSession(
        JsonElement lastSnapshot,
        FlashbackPlaybackSessionMetrics metrics,
        int durationSeconds,
        List<string> warnings)
    {
        var targetFps = GetDouble(lastSnapshot, "FlashbackPlaybackTargetFps");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
        }

        var frameCount = metrics.EndSessionFrameCount;
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

            var minimumObservedFps = targetFps * 0.95;
            if (metrics.MinObservedFpsObserved > 0 && metrics.MinObservedFpsObserved < minimumObservedFps)
            {
                warnings.Add($"flashback playback: observed FPS dipped below floor min={metrics.MinObservedFpsObserved:0.##} floor={minimumObservedFps:0.##}");
            }

            var minimumOnePercentLow = targetFps * 0.80;
            if (metrics.MinOnePercentLowFpsObserved > 0 && metrics.MinOnePercentLowFpsObserved < minimumOnePercentLow)
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
        List<string> warnings)
    {
        if (deadlineDropsDelta > 0)
        {
            warnings.Add($"flashback preview: scheduler deadline drops increased delta={deadlineDropsDelta}");
        }

        if (underflowsDelta > 0)
        {
            warnings.Add($"flashback preview: scheduler underflows increased delta={underflowsDelta}");
        }

        if (d3dStatsFailureDelta > 0)
        {
            warnings.Add($"flashback preview: D3D frame stats failures increased delta={d3dStatsFailureDelta}");
        }
    }

    private static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(
        JsonElement initialSnapshot,
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
            IntegrityQueueDroppedFramesAtEnd = GetNullableLong(finalRecordingSample, "RecordingIntegrityQueueDroppedFrames") ?? 0,
            IntegritySequenceGapsDelta = GetResetAwareCounterDelta(
                finalRecordingSample,
                firstRecordingSample,
                "RecordingIntegritySequenceGaps"),
            IntegrityQueueDroppedFramesDelta = GetResetAwareCounterDelta(
                finalRecordingSample,
                firstRecordingSample,
                "RecordingIntegrityQueueDroppedFrames")
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
        public long IntegritySequenceGapsDelta { get; init; }
        public long IntegrityQueueDroppedFramesDelta { get; init; }
    }

    private static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new FlashbackPlaybackSessionMetrics { BaselineSnapshot = initialSnapshot };
        var baselinePlaybackActive = IsPlaybackSnapshotActive(initialSnapshot);
        var baselineFrameCount = GetNullableLong(initialSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var baselineCommandsEnqueued = GetNullableLong(initialSnapshot, "FlashbackPlaybackCommandsEnqueued") ?? 0;
        var baselineCommandsProcessed = GetNullableLong(initialSnapshot, "FlashbackPlaybackCommandsProcessed") ?? 0;
        foreach (var sample in samples)
        {
            ObservePlaybackSnapshot(
                metrics,
                sample.Snapshot,
                sample.OffsetMs,
                baselineFrameCount,
                baselineCommandsEnqueued,
                baselineCommandsProcessed,
                baselinePlaybackActive);
        }

        ObservePlaybackSnapshot(
            metrics,
            lastSnapshot,
            samples.Count > 0 ? samples[^1].OffsetMs : 0,
            baselineFrameCount,
            baselineCommandsEnqueued,
            baselineCommandsProcessed,
            baselinePlaybackActive);

        if (double.IsPositiveInfinity(metrics.MinOnePercentLowFpsObserved))
        {
            metrics.MinOnePercentLowFpsObserved = 0;
        }

        if (double.IsPositiveInfinity(metrics.MinObservedFpsObserved))
        {
            metrics.MinObservedFpsObserved = 0;
        }

        if (metrics.Observed)
        {
            metrics.DroppedFramesDelta = GetResetAwareCounterDelta(
                metrics.EndSnapshot,
                initialSnapshot,
                "FlashbackPlaybackDroppedFrames");
            metrics.SubmitFailuresDelta = GetResetAwareCounterDelta(
                metrics.EndSnapshot,
                initialSnapshot,
                "FlashbackPlaybackSubmitFailures");
        }

        return metrics;
    }

    private static void ObservePlaybackSnapshot(
        FlashbackPlaybackSessionMetrics metrics,
        JsonElement snapshot,
        long offsetMs,
        long baselineFrameCount,
        long baselineCommandsEnqueued,
        long baselineCommandsProcessed,
        bool baselinePlaybackActive)
    {
        var frameCount = GetNullableLong(snapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var sessionFrameCount = frameCount >= baselineFrameCount
            ? frameCount - baselineFrameCount
            : frameCount;
        var targetFps = GetDouble(snapshot, "FlashbackPlaybackTargetFps");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(snapshot, "SelectedExactFrameRate");
        }

        var minimumPlaybackFramesForLowPercentile = Math.Max(
            240,
            targetFps > 0 ? (long)Math.Ceiling(targetFps * 10.0) : 240);
        var commandsEnqueued = GetNullableLong(snapshot, "FlashbackPlaybackCommandsEnqueued") ?? 0;
        var commandsProcessed = GetNullableLong(snapshot, "FlashbackPlaybackCommandsProcessed") ?? 0;
        var relevantToSession =
            IsPlaybackSnapshotActive(snapshot) ||
            GetInt(snapshot, "FlashbackPlaybackPendingCommands") > 0 ||
            frameCount > baselineFrameCount ||
            commandsEnqueued > baselineCommandsEnqueued ||
            commandsProcessed > baselineCommandsProcessed ||
            baselinePlaybackActive;
        if (!relevantToSession)
        {
            return;
        }

        metrics.Observed = true;
        metrics.EndSnapshot = snapshot;
        metrics.EndSessionFrameCount = sessionFrameCount;
        metrics.MaxPendingCommandsObserved = Math.Max(
            metrics.MaxPendingCommandsObserved,
            GetInt(snapshot, "FlashbackPlaybackMaxPendingCommands"));
        metrics.MaxCommandQueueLatencyMsObserved = Math.Max(
            metrics.MaxCommandQueueLatencyMsObserved,
            GetInt(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs"));

        var observedFps = GetDouble(snapshot, "FlashbackPlaybackObservedFps");
        if (observedFps > 0)
        {
            metrics.MinObservedFpsObserved = Math.Min(metrics.MinObservedFpsObserved, observedFps);
        }

        var onePercentLow = GetDouble(snapshot, "FlashbackPlaybackOnePercentLowFps");
        if (onePercentLow > 0 && sessionFrameCount >= minimumPlaybackFramesForLowPercentile)
        {
            if (onePercentLow < metrics.MinOnePercentLowFpsObserved)
            {
                metrics.MinOnePercentLowFpsObserved = onePercentLow;
                metrics.MinOnePercentLowOffsetMs = offsetMs;
                metrics.MinOnePercentLowFrameCount = frameCount;
                metrics.MinOnePercentLowP99FrameMs = GetDouble(snapshot, "FlashbackPlaybackP99FrameMs");
                metrics.MinOnePercentLowMaxFrameMs = GetDouble(snapshot, "FlashbackPlaybackMaxFrameMs");
                metrics.MinOnePercentLowDecodeP99Ms = GetDouble(snapshot, "FlashbackPlaybackDecodeP99Ms");
                metrics.MinOnePercentLowDecodeMaxMs = GetDouble(snapshot, "FlashbackPlaybackDecodeMaxMs");
                metrics.MinOnePercentLowAvDriftMs = GetDouble(snapshot, "FlashbackAvDriftMs");
                metrics.MinOnePercentLowAudioMasterFallbacks =
                    GetNullableLong(snapshot, "FlashbackPlaybackAudioMasterFallbacks") ?? 0;
            }
        }

        metrics.MaxP99FrameMsObserved = Math.Max(metrics.MaxP99FrameMsObserved, GetDouble(snapshot, "FlashbackPlaybackP99FrameMs"));
        metrics.MaxFrameMsObserved = Math.Max(metrics.MaxFrameMsObserved, GetDouble(snapshot, "FlashbackPlaybackMaxFrameMs"));
        metrics.MaxSlowFramePercentObserved = Math.Max(metrics.MaxSlowFramePercentObserved, GetDouble(snapshot, "FlashbackPlaybackSlowFramePercent"));
        metrics.MaxDecodeP99MsObserved = Math.Max(metrics.MaxDecodeP99MsObserved, GetDouble(snapshot, "FlashbackPlaybackDecodeP99Ms"));
        var decodeMaxMs = GetDouble(snapshot, "FlashbackPlaybackDecodeMaxMs");
        if (decodeMaxMs >= metrics.MaxDecodeMsObserved)
        {
            metrics.MaxDecodeMsObserved = decodeMaxMs;
            metrics.MaxDecodePhaseObserved = GetString(snapshot, "FlashbackPlaybackMaxDecodePhase") ?? string.Empty;
            metrics.MaxDecodeReceiveMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeReceiveMs");
            metrics.MaxDecodeFeedMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeFeedMs");
            metrics.MaxDecodeReadMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeReadMs");
            metrics.MaxDecodeSendMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeSendMs");
            metrics.MaxDecodeAudioMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeAudioMs");
            metrics.MaxDecodeConvertMsObserved = GetDouble(snapshot, "FlashbackPlaybackMaxDecodeConvertMs");
            metrics.MaxDecodeUtcUnixMsObserved = GetNullableLong(snapshot, "FlashbackPlaybackMaxDecodeUtcUnixMs") ?? 0;
            metrics.MaxDecodePositionMsObserved = GetNullableLong(snapshot, "FlashbackPlaybackMaxDecodePositionMs") ?? 0;
        }
        metrics.MaxAudioMasterDelayDoublesObserved = Math.Max(
            metrics.MaxAudioMasterDelayDoublesObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterDelayDoubles"));
        metrics.MaxAudioMasterDelayShrinksObserved = Math.Max(
            metrics.MaxAudioMasterDelayShrinksObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterDelayShrinks"));
        metrics.MaxAudioMasterFallbacksObserved = Math.Max(
            metrics.MaxAudioMasterFallbacksObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterFallbacks"));
        metrics.MaxAudioBufferedDurationMsObserved = Math.Max(metrics.MaxAudioBufferedDurationMsObserved, GetDouble(snapshot, "WasapiPlaybackBufferedDurationMs"));
        metrics.MaxAudioQueueDurationMsObserved = Math.Max(metrics.MaxAudioQueueDurationMsObserved, GetDouble(snapshot, "WasapiPlaybackQueueDurationMs"));
        metrics.MaxAbsAvDriftMsObserved = Math.Max(metrics.MaxAbsAvDriftMsObserved, Math.Abs(GetDouble(snapshot, "FlashbackAvDriftMs")));
    }

    private static bool IsPlaybackSnapshotActive(JsonElement snapshot)
    {
        var state = GetString(snapshot, "FlashbackPlaybackState") ?? string.Empty;
        return GetBool(snapshot, "FlashbackPlaybackThreadAlive") ||
               string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, "Paused", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(state, "Seeking", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FlashbackPlaybackSessionMetrics
    {
        public bool Observed { get; set; }
        public JsonElement BaselineSnapshot { get; init; }
        public JsonElement EndSnapshot { get; set; }
        public long EndSessionFrameCount { get; set; }
        public int MaxPendingCommandsObserved { get; set; }
        public int MaxCommandQueueLatencyMsObserved { get; set; }
        public double MinObservedFpsObserved { get; set; } = double.PositiveInfinity;
        public double MinOnePercentLowFpsObserved { get; set; } = double.PositiveInfinity;
        public long MinOnePercentLowOffsetMs { get; set; }
        public long MinOnePercentLowFrameCount { get; set; }
        public double MinOnePercentLowP99FrameMs { get; set; }
        public double MinOnePercentLowMaxFrameMs { get; set; }
        public double MinOnePercentLowDecodeP99Ms { get; set; }
        public double MinOnePercentLowDecodeMaxMs { get; set; }
        public double MinOnePercentLowAvDriftMs { get; set; }
        public long MinOnePercentLowAudioMasterFallbacks { get; set; }
        public double MaxP99FrameMsObserved { get; set; }
        public double MaxFrameMsObserved { get; set; }
        public double MaxSlowFramePercentObserved { get; set; }
        public double MaxDecodeP99MsObserved { get; set; }
        public double MaxDecodeMsObserved { get; set; }
        public string MaxDecodePhaseObserved { get; set; } = string.Empty;
        public double MaxDecodeReceiveMsObserved { get; set; }
        public double MaxDecodeFeedMsObserved { get; set; }
        public double MaxDecodeReadMsObserved { get; set; }
        public double MaxDecodeSendMsObserved { get; set; }
        public double MaxDecodeAudioMsObserved { get; set; }
        public double MaxDecodeConvertMsObserved { get; set; }
        public long MaxDecodeUtcUnixMsObserved { get; set; }
        public long MaxDecodePositionMsObserved { get; set; }
        public long MaxAudioMasterDelayDoublesObserved { get; set; }
        public long MaxAudioMasterDelayShrinksObserved { get; set; }
        public long MaxAudioMasterFallbacksObserved { get; set; }
        public double MaxAudioBufferedDurationMsObserved { get; set; }
        public double MaxAudioQueueDurationMsObserved { get; set; }
        public double MaxAbsAvDriftMsObserved { get; set; }
        public long DroppedFramesDelta { get; set; }
        public long SubmitFailuresDelta { get; set; }
    }

    private static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new FlashbackExportSessionMetrics();
        var baselineExportId = GetNullableLong(initialSnapshot, "FlashbackExportId") ?? 0;
        var baselineExportActive = GetBool(initialSnapshot, "FlashbackExportActive");
        foreach (var sample in samples)
        {
            ObserveExportSnapshot(metrics, sample.Snapshot, baselineExportId, baselineExportActive);
        }

        ObserveExportSnapshot(metrics, lastSnapshot, baselineExportId, baselineExportActive);
        return metrics;
    }

    private static void ObserveExportSnapshot(
        FlashbackExportSessionMetrics metrics,
        JsonElement snapshot,
        long baselineExportId,
        bool baselineExportActive)
    {
        var exportId = GetNullableLong(snapshot, "FlashbackExportId") ?? 0;
        var status = GetString(snapshot, "FlashbackExportStatus") ?? string.Empty;
        var active = GetBool(snapshot, "FlashbackExportActive");
        var relevantToSession =
            active ||
            exportId > baselineExportId ||
            baselineExportActive && exportId == baselineExportId ||
            baselineExportId <= 0 &&
            !string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "NotStarted", StringComparison.OrdinalIgnoreCase);
        if (!relevantToSession)
        {
            return;
        }

        metrics.Observed = true;
        metrics.ActiveAtEnd = active;
        metrics.StatusAtEnd = status;
        metrics.MessageAtEnd = GetString(snapshot, "FlashbackExportMessage") ?? string.Empty;
        metrics.FailureKindAtEnd = GetString(snapshot, "FlashbackExportFailureKind") ?? string.Empty;
        metrics.OutputPathAtEnd = GetString(snapshot, "FlashbackExportOutputPath") ?? string.Empty;
        var lastExportId = GetNullableLong(snapshot, "LastExportId") ?? 0;
        metrics.LastExportIdAtEnd = lastExportId;
        if (!active && exportId > 0 && lastExportId == exportId)
        {
            metrics.LastSuccessAtEnd = GetString(snapshot, "LastExportSuccess") ?? string.Empty;
            metrics.LastMessageAtEnd = GetString(snapshot, "LastExportMessage") ?? string.Empty;
        }
        else
        {
            metrics.LastSuccessAtEnd = string.Empty;
            metrics.LastMessageAtEnd = string.Empty;
        }
        metrics.MaxElapsedMsObserved = Math.Max(
            metrics.MaxElapsedMsObserved,
            GetNullableLong(snapshot, "FlashbackExportElapsedMs") ?? 0);
        metrics.MaxLastProgressAgeMsObserved = Math.Max(
            metrics.MaxLastProgressAgeMsObserved,
            GetNullableLong(snapshot, "FlashbackExportLastProgressAgeMs") ?? 0);
        metrics.MaxOutputBytesObserved = Math.Max(
            metrics.MaxOutputBytesObserved,
            GetNullableLong(snapshot, "FlashbackExportOutputBytes") ?? 0);
        metrics.MaxThroughputBytesPerSecObserved = Math.Max(
            metrics.MaxThroughputBytesPerSecObserved,
            GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"));
    }

    private sealed class FlashbackExportSessionMetrics
    {
        public bool Observed { get; set; }
        public bool ActiveAtEnd { get; set; }
        public string StatusAtEnd { get; set; } = "NotStarted";
        public string MessageAtEnd { get; set; } = string.Empty;
        public string FailureKindAtEnd { get; set; } = string.Empty;
        public string OutputPathAtEnd { get; set; } = string.Empty;
        public long LastExportIdAtEnd { get; set; }
        public string LastSuccessAtEnd { get; set; } = string.Empty;
        public string LastMessageAtEnd { get; set; } = string.Empty;
        public long MaxElapsedMsObserved { get; set; }
        public long MaxLastProgressAgeMsObserved { get; set; }
        public long MaxOutputBytesObserved { get; set; }
        public double MaxThroughputBytesPerSecObserved { get; set; }
    }

    private static PreviewCadenceSessionMetrics BuildPreviewCadenceSessionMetrics(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new PreviewCadenceSessionMetrics
        {
            OnePercentLowFpsAtEnd = GetDouble(lastSnapshot, "PreviewCadenceOnePercentLowFps")
        };
        ObservePreviewCadenceSnapshot(metrics, lastSnapshot);
        foreach (var sample in samples)
        {
            ObservePreviewCadenceSnapshot(metrics, sample.Snapshot);
        }

        if (double.IsPositiveInfinity(metrics.MinOnePercentLowFpsObserved))
        {
            metrics.MinOnePercentLowFpsObserved = 0;
        }

        return metrics;
    }

    private static void ObservePreviewCadenceSnapshot(PreviewCadenceSessionMetrics metrics, JsonElement snapshot)
    {
        var onePercentLow = GetDouble(snapshot, "PreviewCadenceOnePercentLowFps");
        if (onePercentLow > 0)
        {
            metrics.MinOnePercentLowFpsObserved = Math.Min(metrics.MinOnePercentLowFpsObserved, onePercentLow);
        }
    }

    private sealed class PreviewCadenceSessionMetrics
    {
        public double OnePercentLowFpsAtEnd { get; init; }
        public double MinOnePercentLowFpsObserved { get; set; } = double.PositiveInfinity;
    }

    private static VisualCadenceSessionMetrics BuildVisualCadenceSessionMetrics(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new VisualCadenceSessionMetrics
        {
            OutputFpsAtEnd = GetDouble(lastSnapshot, "VisualCadenceOutputObservedFps"),
            ChangeFpsAtEnd = GetDouble(lastSnapshot, "VisualCadenceChangeObservedFps"),
            RepeatPercentAtEnd = GetDouble(lastSnapshot, "VisualCadenceRepeatFramePercent"),
            RepeatFramesAtEnd = GetNullableLong(lastSnapshot, "VisualCadenceRepeatFrameCount") ?? 0,
            LongestRepeatRunAtEnd = GetNullableLong(lastSnapshot, "VisualCadenceLongestRepeatRun") ?? 0
        };
        ObserveVisualCadenceSnapshot(metrics, lastSnapshot);
        foreach (var sample in samples)
        {
            ObserveVisualCadenceSnapshot(metrics, sample.Snapshot);
        }

        if (double.IsPositiveInfinity(metrics.MinChangeFpsObserved))
        {
            metrics.MinChangeFpsObserved = 0;
        }

        return metrics;
    }

    private static void ObserveVisualCadenceSnapshot(VisualCadenceSessionMetrics metrics, JsonElement snapshot)
    {
        var changeFps = GetDouble(snapshot, "VisualCadenceChangeObservedFps");
        if (changeFps > 0)
        {
            metrics.MinChangeFpsObserved = Math.Min(metrics.MinChangeFpsObserved, changeFps);
        }

        metrics.MaxRepeatPercentObserved = Math.Max(
            metrics.MaxRepeatPercentObserved,
            GetDouble(snapshot, "VisualCadenceRepeatFramePercent"));
    }

    private sealed class VisualCadenceSessionMetrics
    {
        public double OutputFpsAtEnd { get; init; }
        public double ChangeFpsAtEnd { get; init; }
        public double MinChangeFpsObserved { get; set; } = double.PositiveInfinity;
        public double RepeatPercentAtEnd { get; init; }
        public double MaxRepeatPercentObserved { get; set; }
        public long RepeatFramesAtEnd { get; init; }
        public long LongestRepeatRunAtEnd { get; init; }
    }

    private static PreviewD3DMetrics BuildPreviewD3DMetrics(
        JsonElement initialSnapshot,
        JsonElement lastSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var missedRefreshStart = GetNullableLong(initialSnapshot, "PreviewD3DFrameStatsMissedRefreshCount") ?? 0;
        var missedRefreshEnd = GetNullableLong(lastSnapshot, "PreviewD3DFrameStatsMissedRefreshCount") ?? 0;
        var failureStart = GetNullableLong(initialSnapshot, "PreviewD3DFrameStatsFailureCount") ?? 0;
        var failureEnd = GetNullableLong(lastSnapshot, "PreviewD3DFrameStatsFailureCount") ?? 0;
        var metrics = new PreviewD3DMetrics
        {
            MissedRefreshDelta = Math.Max(0, missedRefreshEnd - missedRefreshStart),
            StatsFailureDelta = Math.Max(0, failureEnd - failureStart),
            InputUploadCpuP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DInputUploadCpuP99Ms"),
            RenderSubmitCpuP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DRenderSubmitCpuP99Ms"),
            PresentCallP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DPresentCallP99Ms"),
            TotalFrameCpuP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DTotalFrameCpuP99Ms")
        };

        foreach (var sample in samples)
        {
            ObservePreviewD3DCpuTiming(metrics, sample.Snapshot);
            metrics.MaxRecentSlowFramesObserved = Math.Max(
                metrics.MaxRecentSlowFramesObserved,
                CountArrayItems(sample.Snapshot, "PreviewD3DRecentSlowFrames"));
            if (TryGetLatestSlowFrame(sample.Snapshot, out var slowFrame))
            {
                metrics.LatestSlowFrameReason = GetSlowFrameReason(slowFrame);
                metrics.LatestSlowFrameOverBudgetMs = GetDouble(slowFrame, "WorstOverBudgetMs");
                metrics.LatestSlowFramePresentIntervalMs = GetDouble(slowFrame, "PresentIntervalMs");
                metrics.LatestSlowFrameTotalFrameCpuMs = GetDouble(slowFrame, "TotalFrameCpuMs");
                metrics.LatestSlowFramePresentCallMs = GetDouble(slowFrame, "PresentCallMs");
                metrics.LatestSlowFramePendingFrameCount = GetInt(slowFrame, "PendingFrameCount");
            }
        }

        metrics.MaxRecentSlowFramesObserved = Math.Max(
            metrics.MaxRecentSlowFramesObserved,
            CountArrayItems(lastSnapshot, "PreviewD3DRecentSlowFrames"));
        ObservePreviewD3DCpuTiming(metrics, lastSnapshot);
        if (TryGetLatestSlowFrame(lastSnapshot, out var lastSlowFrame))
        {
            metrics.LatestSlowFrameReason = GetSlowFrameReason(lastSlowFrame);
            metrics.LatestSlowFrameOverBudgetMs = GetDouble(lastSlowFrame, "WorstOverBudgetMs");
            metrics.LatestSlowFramePresentIntervalMs = GetDouble(lastSlowFrame, "PresentIntervalMs");
            metrics.LatestSlowFrameTotalFrameCpuMs = GetDouble(lastSlowFrame, "TotalFrameCpuMs");
            metrics.LatestSlowFramePresentCallMs = GetDouble(lastSlowFrame, "PresentCallMs");
            metrics.LatestSlowFramePendingFrameCount = GetInt(lastSlowFrame, "PendingFrameCount");
        }

        return metrics;
    }

    private static string GetSlowFrameReason(JsonElement slowFrame)
        => GetString(slowFrame, "SlowReason") ?? GetString(slowFrame, "Reason") ?? string.Empty;

    private sealed class PreviewD3DMetrics
    {
        public long MissedRefreshDelta { get; init; }
        public long StatsFailureDelta { get; init; }
        public int MaxRecentSlowFramesObserved { get; set; }
        public string LatestSlowFrameReason { get; set; } = string.Empty;
        public double LatestSlowFrameOverBudgetMs { get; set; }
        public double LatestSlowFramePresentIntervalMs { get; set; }
        public double LatestSlowFrameTotalFrameCpuMs { get; set; }
        public double LatestSlowFramePresentCallMs { get; set; }
        public int LatestSlowFramePendingFrameCount { get; set; }
        public double InputUploadCpuP99MsAtEnd { get; init; }
        public double InputUploadCpuMaxMsObserved { get; set; }
        public double RenderSubmitCpuP99MsAtEnd { get; init; }
        public double RenderSubmitCpuMaxMsObserved { get; set; }
        public double PresentCallP99MsAtEnd { get; init; }
        public double PresentCallMaxMsObserved { get; set; }
        public double TotalFrameCpuP99MsAtEnd { get; init; }
        public double TotalFrameCpuMaxMsObserved { get; set; }
    }

    private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)
    {
        metrics.InputUploadCpuMaxMsObserved = Math.Max(
            metrics.InputUploadCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DInputUploadCpuMaxMs"));
        metrics.RenderSubmitCpuMaxMsObserved = Math.Max(
            metrics.RenderSubmitCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DRenderSubmitCpuMaxMs"));
        metrics.PresentCallMaxMsObserved = Math.Max(
            metrics.PresentCallMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DPresentCallMaxMs"));
        metrics.TotalFrameCpuMaxMsObserved = Math.Max(
            metrics.TotalFrameCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DTotalFrameCpuMaxMs"));
    }

    private static int CountArrayItems(JsonElement snapshot, string propertyName)
    {
        return snapshot.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;
    }

    private static bool TryGetLatestSlowFrame(JsonElement snapshot, out JsonElement slowFrame)
    {
        if (snapshot.TryGetProperty("PreviewD3DRecentSlowFrames", out var frames) &&
            frames.ValueKind == JsonValueKind.Array &&
            frames.GetArrayLength() > 0)
        {
            slowFrame = frames.EnumerateArray().Last().Clone();
            return true;
        }

        slowFrame = default;
        return false;
    }

    private static PlaybackCommandHealth BuildPlaybackCommandHealth(JsonElement snapshot, JsonElement baselineSnapshot)
    {
        var dropped = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackCommandsDropped");
        var skipped = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackCommandsSkippedNotReady");
        var submitFailures = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackSubmitFailures");
        var coalescedScrub = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackScrubUpdatesCoalesced");
        var coalescedSeek = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackSeekCommandsCoalesced");
        return new PlaybackCommandHealth(
            dropped,
            skipped,
            submitFailures,
            coalescedScrub,
            coalescedSeek,
            Math.Max(0, dropped - coalescedScrub));
    }

    private static long GetCounterDelta(JsonElement snapshot, JsonElement baselineSnapshot, string propertyName)
    {
        var current = GetNullableLong(snapshot, propertyName) ?? 0;
        var baseline = baselineSnapshot.ValueKind == JsonValueKind.Object
            ? GetNullableLong(baselineSnapshot, propertyName) ?? 0
            : 0;
        return Math.Max(0, current - baseline);
    }

    private static long GetResetAwareCounterDelta(JsonElement snapshot, JsonElement baselineSnapshot, string propertyName)
    {
        var current = GetNullableLong(snapshot, propertyName) ?? 0;
        var baseline = baselineSnapshot.ValueKind == JsonValueKind.Object
            ? GetNullableLong(baselineSnapshot, propertyName) ?? 0
            : 0;
        return current >= baseline ? current - baseline : current;
    }

    private static string FormatOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }

    private static string FormatFrameRate(double fps, string friendlyFps, string exactArg)
    {
        var display = !string.IsNullOrWhiteSpace(friendlyFps)
            ? friendlyFps
            : fps > 0
                ? fps.ToString("0.###", CultureInfo.InvariantCulture)
                : "0";
        return !string.IsNullOrWhiteSpace(exactArg)
            ? $"{display}fps ({exactArg})"
            : $"{display}fps";
    }

    private static async Task SampleLoopAsync(
        int durationSeconds,
        int sampleIntervalMs,
        List<DiagnosticSessionSample> samples,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken,
        Func<Task>? sampleCheckpointAsync = null)
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
                if (sampleCheckpointAsync is not null)
                {
                    await sampleCheckpointAsync().ConfigureAwait(false);
                }
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

    private static bool TryGetFlashbackExportVerificationPath(
        string scenario,
        string outputDirectory,
        out string exportPath)
    {
        exportPath = scenario switch
        {
            "flashback" or "flashback-stress" => Path.Combine(outputDirectory, "flashback-stress-export.mp4"),
            "flashback-restart-cycle" => Path.Combine(outputDirectory, "flashback-restart-cycle-export.mp4"),
            "flashback-encoder-cycle" => Path.Combine(outputDirectory, "flashback-encoder-cycle-export.mp4"),
            "flashback-export-playback" => Path.Combine(outputDirectory, "flashback-export-playback.mp4"),
            "flashback-range-export" => Path.Combine(outputDirectory, "flashback-range-export.mp4"),
            "flashback-disable-during-export" => Path.Combine(outputDirectory, "flashback-disable-during-export.mp4"),
            "flashback-rotated-export" => Path.Combine(outputDirectory, "flashback-rotated-export.mp4"),
            "flashback-preview-cycle" => Path.Combine(outputDirectory, "flashback-preview-off-export.mp4"),
            _ => string.Empty
        };

        return exportPath.Length > 0 && File.Exists(exportPath);
    }

    private static string NormalizeScenario(string? scenario)
    {
        var normalized = string.IsNullOrWhiteSpace(scenario)
            ? "observe"
            : scenario.Trim().ToLowerInvariant();
        return normalized switch
        {
            "observe" or "preview-only" or "recording-only" or "flashback" or "flashback-playback" or "flashback-stress" or "flashback-scrub-stress" or "flashback-restart-cycle" or "flashback-encoder-cycle" or "flashback-export-playback" or "flashback-segment-playback" or "flashback-range-export" or "flashback-lifecycle" or "flashback-export-concurrent" or "flashback-disable-during-export" or "flashback-rotated-export" or "flashback-preview-cycle" or "flashback-recording" or "flashback-recording-preview-cycle" or "flashback-recording-settings-deferred" or "flashback-recording-export-rejected" or "flashback-export-rejected" or "combined" => normalized,
            _ => throw new ArgumentException($"Unknown diagnostic session scenario '{scenario}'.", nameof(scenario))
        };
    }

    private static bool ScenarioNeedsPreview(string scenario)
        => scenario is "preview-only" or "flashback" or "flashback-playback" or "flashback-stress" or "flashback-scrub-stress" or "flashback-restart-cycle" or "flashback-encoder-cycle" or "flashback-export-playback" or "flashback-segment-playback" or "flashback-range-export" or "flashback-lifecycle" or "flashback-export-concurrent" or "flashback-disable-during-export" or "flashback-rotated-export" or "flashback-preview-cycle" or "flashback-recording" or "flashback-recording-preview-cycle" or "flashback-recording-settings-deferred" or "flashback-recording-export-rejected" or "combined";

    private static bool ScenarioNeedsRecording(string scenario)
        => scenario is "recording-only" or "flashback-recording" or "flashback-recording-preview-cycle" or "flashback-recording-settings-deferred" or "flashback-recording-export-rejected" or "combined";

    private static bool ScenarioNeedsFlashback(string scenario)
        => scenario is "flashback" or "flashback-playback" or "flashback-stress" or "flashback-scrub-stress" or "flashback-restart-cycle" or "flashback-encoder-cycle" or "flashback-export-playback" or "flashback-segment-playback" or "flashback-range-export" or "flashback-lifecycle" or "flashback-export-concurrent" or "flashback-disable-during-export" or "flashback-rotated-export" or "flashback-preview-cycle" or "flashback-recording" or "flashback-recording-preview-cycle" or "flashback-recording-settings-deferred" or "flashback-recording-export-rejected" or "combined";

}
