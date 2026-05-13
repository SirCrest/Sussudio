using System.Globalization;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;
using static Sussudio.Tools.DiagnosticSessionFlashbackRejectedExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
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

        var sessionLock = DiagnosticSessionOutputLock.Acquire(outputDirectory);

        try
        {
        var startedUtc = DateTimeOffset.UtcNow;
        var runnerProcessId = Environment.ProcessId;

        var actions = new List<string>();
        var warnings = new List<string>();
        var samples = new List<DiagnosticSessionSample>();
        var runState = new DiagnosticSessionRunState(
            sessionId,
            scenario,
            outputDirectory,
            startedUtc,
            runnerProcessId,
            () => cancellationToken.IsCancellationRequested,
            warnings);
        var livePath = runState.LivePath;
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
            var setupResult = await DiagnosticSessionScenarioSetup.RunAsync(
                    scenario,
                    scenarioPlan,
                    initialSnapshot,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    TryWaitAsync,
                    scenarioCancellationToken)
                .ConfigureAwait(false);
            startedPreview = setupResult.StartedPreview;
            startedRecording = setupResult.StartedRecording;
            enabledFlashback = setupResult.EnabledFlashback;
            disabledFlashback = setupResult.DisabledFlashback;

            var scenarioStartup = await DiagnosticSessionScenarioStartup.StartAsync(
                    options,
                    scenarioPlan,
                    durationSeconds,
                    outputDirectory,
                    backgroundTasks,
                    actions,
                    warnings,
                    (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                    SendRawWithConnectRetryAsync,
                    (command, payload, timeoutMs, allowFailure) => SendAsync(command, payload, timeoutMs, allowFailure),
                    scenarioCancellationToken)
                .ConfigureAwait(false);
            startedFlashbackPlayback = scenarioStartup.StartedFlashbackPlayback;

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
            RecordTerminalException(ex, runState.LastStage);
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
            var cleanupResult = await DiagnosticSessionCleanupActions.RunAsync(
                    options,
                    initialSnapshot,
                    startedRecording,
                    startedPreview,
                    enabledFlashback,
                    disabledFlashback,
                    startedFlashbackPlayback,
                    actions,
                    SendWithTokenAsync,
                    TryWaitWithTokenAsync,
                    SetStage,
                    RecordTerminalException)
                .ConfigureAwait(false);
            stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;

            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        var recordingCheckResult = await DiagnosticSessionRecordingChecks.RunAsync(
                options,
                scenarioPlan,
                scenario,
                outputDirectory,
                initialSnapshot,
                samples,
                startedRecording,
                flashbackRecordingSettingsDeferredPresetState,
                actions,
                warnings,
                (command, payload, timeoutMs) => SendAsync(command, payload, timeoutMs),
                SetStage,
                RecordTerminalException,
                cancellationToken)
            .ConfigureAwait(false);
        verification = recordingCheckResult.Verification;

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
        var playbackResultMetrics = BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics);
        if (playbackResultMetrics.SeekForwardDecodeCapHitsDelta > 0)
        {
            warnings.Add(
                "flashback playback seek forward-decode cap hit during session " +
                $"delta={playbackResultMetrics.SeekForwardDecodeCapHitsDelta} " +
                $"total={playbackResultMetrics.SeekForwardDecodeCapHitsAtEnd}");
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
                playbackSessionMetrics.Observed ? playbackResultMetrics.EndSnapshot : lastSnapshot,
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
                      runState.TerminalException is null &&
                      diagnosticHealthSucceeded &&
                      (presentMon is null || presentMon.Success) &&
                      (!verificationSucceeded.HasValue || verificationSucceeded.Value) &&
                      flashbackWarningsSucceeded,
            StartedUtc = startedUtc,
            CompletedUtc = completedUtc,
            TerminalState = terminalState,
            LastStage = GetResultLastStage(),
            UnhandledException = runState.TerminalException is null ? null : DiagnosticSessionRunState.FormatTerminalException(runState.TerminalException),
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
            FlashbackPlaybackPendingCommandsAtEnd = playbackResultMetrics.PendingCommandsAtEnd,
            FlashbackPlaybackMaxPendingCommandsObserved = playbackResultMetrics.MaxPendingCommandsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyMsObserved = playbackResultMetrics.MaxCommandQueueLatencyMsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyCommandObserved = playbackResultMetrics.MaxCommandQueueLatencyCommandObserved,
            FlashbackPlaybackCommandsDroppedAtEnd = playbackResultMetrics.CommandsDroppedAtEnd,
            FlashbackPlaybackCommandsSkippedNotReadyAtEnd = playbackResultMetrics.CommandsSkippedNotReadyAtEnd,
            FlashbackPlaybackScrubUpdatesCoalescedAtEnd = playbackResultMetrics.ScrubUpdatesCoalescedAtEnd,
            FlashbackPlaybackSeekCommandsCoalescedAtEnd = playbackResultMetrics.SeekCommandsCoalescedAtEnd,
            FlashbackPlaybackLastCommandFailureAtEnd = playbackResultMetrics.LastCommandFailureAtEnd,
            FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd = playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd,
            FlashbackPlaybackObservedFpsAtEnd = playbackResultMetrics.ObservedFpsAtEnd,
            FlashbackPlaybackMinObservedFpsObserved = playbackSessionMetrics.MinObservedFpsObserved,
            FlashbackPlaybackAvgFrameMsAtEnd = playbackResultMetrics.AvgFrameMsAtEnd,
            FlashbackPlaybackP99FrameMsAtEnd = playbackResultMetrics.P99FrameMsAtEnd,
            FlashbackPlaybackMaxFrameMsAtEnd = playbackResultMetrics.MaxFrameMsAtEnd,
            FlashbackPlaybackOnePercentLowFpsAtEnd = playbackResultMetrics.OnePercentLowFpsAtEnd,
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
            FlashbackPlaybackDecodeAvgMsAtEnd = playbackResultMetrics.DecodeAvgMsAtEnd,
            FlashbackPlaybackDecodeP95MsAtEnd = playbackResultMetrics.DecodeP95MsAtEnd,
            FlashbackPlaybackDecodeP99MsAtEnd = playbackResultMetrics.DecodeP99MsAtEnd,
            FlashbackPlaybackDecodeMaxMsAtEnd = playbackResultMetrics.DecodeMaxMsAtEnd,
            FlashbackPlaybackMaxDecodePhaseAtEnd = playbackResultMetrics.MaxDecodePhaseAtEnd,
            FlashbackPlaybackMaxDecodeReceiveMsAtEnd = playbackResultMetrics.MaxDecodeReceiveMsAtEnd,
            FlashbackPlaybackMaxDecodeFeedMsAtEnd = playbackResultMetrics.MaxDecodeFeedMsAtEnd,
            FlashbackPlaybackMaxDecodeReadMsAtEnd = playbackResultMetrics.MaxDecodeReadMsAtEnd,
            FlashbackPlaybackMaxDecodeSendMsAtEnd = playbackResultMetrics.MaxDecodeSendMsAtEnd,
            FlashbackPlaybackMaxDecodeAudioMsAtEnd = playbackResultMetrics.MaxDecodeAudioMsAtEnd,
            FlashbackPlaybackMaxDecodeConvertMsAtEnd = playbackResultMetrics.MaxDecodeConvertMsAtEnd,
            FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd = playbackResultMetrics.MaxDecodeUtcUnixMsAtEnd,
            FlashbackPlaybackMaxDecodePositionMsAtEnd = playbackResultMetrics.MaxDecodePositionMsAtEnd,
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
            FlashbackPlaybackFrameCountAtEnd = playbackResultMetrics.FrameCountAtEnd,
            FlashbackPlaybackLateFramesAtEnd = playbackResultMetrics.LateFramesAtEnd,
            FlashbackPlaybackSlowFramesAtEnd = playbackResultMetrics.SlowFramesAtEnd,
            FlashbackPlaybackSlowFramePercentAtEnd = playbackResultMetrics.SlowFramePercentAtEnd,
            FlashbackPlaybackDroppedFramesAtEnd = playbackResultMetrics.DroppedFramesAtEnd,
            FlashbackPlaybackDroppedFramesDelta = playbackSessionMetrics.DroppedFramesDelta,
            FlashbackPlaybackAudioMasterDelayDoublesAtEnd = playbackResultMetrics.AudioMasterDelayDoublesAtEnd,
            FlashbackPlaybackAudioMasterDelayShrinksAtEnd = playbackResultMetrics.AudioMasterDelayShrinksAtEnd,
            FlashbackPlaybackAudioMasterFallbacksAtEnd = playbackResultMetrics.AudioMasterFallbacksAtEnd,
            FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd = playbackResultMetrics.AudioMasterUnavailableFallbacksAtEnd,
            FlashbackPlaybackAudioMasterStaleFallbacksAtEnd = playbackResultMetrics.AudioMasterStaleFallbacksAtEnd,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd = playbackResultMetrics.AudioMasterDriftOutlierFallbacksAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd = playbackResultMetrics.AudioMasterLastFallbackReasonAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd = playbackResultMetrics.AudioMasterLastFallbackClockAgeMsAtEnd,
            FlashbackPlaybackMaxAudioMasterDelayDoublesObserved = playbackSessionMetrics.MaxAudioMasterDelayDoublesObserved,
            FlashbackPlaybackMaxAudioMasterDelayShrinksObserved = playbackSessionMetrics.MaxAudioMasterDelayShrinksObserved,
            FlashbackPlaybackMaxAudioMasterFallbacksObserved = playbackSessionMetrics.MaxAudioMasterFallbacksObserved,
            FlashbackPlaybackMaxAudioBufferedDurationMsObserved = playbackSessionMetrics.MaxAudioBufferedDurationMsObserved,
            FlashbackPlaybackMaxAudioQueueDurationMsObserved = playbackSessionMetrics.MaxAudioQueueDurationMsObserved,
            FlashbackPlaybackMaxAbsAvDriftMsObserved = playbackSessionMetrics.MaxAbsAvDriftMsObserved,
            FlashbackPlaybackSubmitFailuresAtEnd = playbackResultMetrics.SubmitFailuresAtEnd,
            FlashbackPlaybackSubmitFailuresDelta = playbackSessionMetrics.SubmitFailuresDelta,
            FlashbackPlaybackSegmentSwitchesAtEnd = playbackResultMetrics.SegmentSwitchesAtEnd,
            FlashbackPlaybackFmp4ReopensAtEnd = playbackResultMetrics.Fmp4ReopensAtEnd,
            FlashbackPlaybackWriteHeadWaitsAtEnd = playbackResultMetrics.WriteHeadWaitsAtEnd,
            FlashbackPlaybackNearLiveSnapsAtEnd = playbackResultMetrics.NearLiveSnapsAtEnd,
            FlashbackPlaybackDecodeErrorSnapsAtEnd = playbackResultMetrics.DecodeErrorSnapsAtEnd,
            FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd = playbackResultMetrics.LastWriteHeadWaitGapMsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd = playbackResultMetrics.SeekForwardDecodeCapHitsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsDelta = playbackResultMetrics.SeekForwardDecodeCapHitsDelta,
            FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd = playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd,
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
            result.UnhandledException = runState.TerminalException is null ? null : DiagnosticSessionRunState.FormatTerminalException(runState.TerminalException);
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
            runState.SetStage(stage);
        }

        void RecordTerminalException(Exception ex, string stage)
        {
            runState.RecordTerminalException(ex, stage);
        }

        string GetTerminalState()
        {
            return runState.GetTerminalState();
        }

        string GetResultLastStage()
            => runState.GetResultLastStage();

        async Task WriteArtifactBestEffortAsync<T>(string stage, string path, T value)
        {
            await runState.WriteArtifactBestEffortAsync(stage, path, value).ConfigureAwait(false);
        }

        async Task WriteLiveStateBestEffortAsync(DateTimeOffset? completedUtcOverride = null, string? terminalStateOverride = null)
        {
            await runState.WriteLiveStateBestEffortAsync(
                    samples,
                    initialSnapshot,
                    commandFailureCount,
                    completedUtcOverride,
                    terminalStateOverride)
                .ConfigureAwait(false);
        }

        async Task WriteSamplingLiveStateBestEffortAsync()
        {
            await runState.WriteSamplingLiveStateBestEffortAsync(
                    samples,
                    initialSnapshot,
                    commandFailureCount)
                .ConfigureAwait(false);
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
        }
        finally
        {
            sessionLock.Dispose();
        }
    }

    public static string Format(DiagnosticSessionResult result)
    {
        return DiagnosticSessionResultFormatter.Format(result);
    }

}
