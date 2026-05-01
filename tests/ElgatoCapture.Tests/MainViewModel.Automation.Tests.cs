using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook()
    {
        var vmType = RequireType("ElgatoCapture.ViewModels.MainViewModel");

        // SavePreviewVolume must exist as the persistence hook
        var savePreviewVolume = vmType.GetMethod(
            "SavePreviewVolume",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        AssertNotNull(savePreviewVolume, "MainViewModel.SavePreviewVolume");

        // PreviewVolume observable property must exist
        var previewVolume = vmType.GetProperty("PreviewVolume", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(previewVolume, "MainViewModel.PreviewVolume");

        // ShowAllCaptureOptions observable property must exist
        var showAll = vmType.GetProperty("ShowAllCaptureOptions", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(showAll, "MainViewModel.ShowAllCaptureOptions");

        // IsAudioPreviewEnabled observable property must exist
        var audioPreview = vmType.GetProperty("IsAudioPreviewEnabled", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(audioPreview, "MainViewModel.IsAudioPreviewEnabled");

        // Automation interface method must exist
        var getOptionsSnapshot = vmType.GetMethod(
            "GetAutomationOptionsSnapshotAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(getOptionsSnapshot, "MainViewModel.GetAutomationOptionsSnapshotAsync");

        return Task.CompletedTask;
    }

    private static Task MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate()
    {
        var automationText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.Automation.cs")
            .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadRepoFile("ElgatoCapture/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(captureText, "public Task ToggleRecordingAsync()\n        => SetRecordingDesiredStateAsync(!IsRecording);");
        AssertContains(captureText, "Recording transition already in progress.");
        AssertContains(captureText, "await inFlight;");
        AssertContains(captureText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(captureText, "var task = RecordingTransitionInnerAsync(enabled, cancellationToken);");
        AssertContains(captureText, "await StartRecordingAsync(cancellationToken);");
        AssertContains(captureText, "await StopRecordingAsync(cancellationToken);");
        AssertContains(captureText, "await BeginRecordingTransitionAsync(enabled, cancellationToken);");
        AssertContains(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(captureText, "await _sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertContains(automationText, "return SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(dispatcherText, "return CreateResponse(correlationId, $\"Recording {(enabled ? \"started\" : \"stopped\")}.\"");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertContains(dispatcherText, "snapshot: snapshot");

        return Task.CompletedTask;
    }

    private static Task MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface()
    {
        var automationInterfaceType = RequireType("ElgatoCapture.Services.Automation.IAutomationViewModel");
        AssertEqual(
            false,
            automationInterfaceType.GetProperty("IsMicrophoneEnabled") != null,
            "IAutomationViewModel sync microphone setter");
        AssertTaskReturningMethod(automationInterfaceType, "SetMicrophoneEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetFlashbackEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "ExecuteFlashbackActionAsync", typeof(bool));
        AssertTaskReturningMethod(
            automationInterfaceType,
            "GetFlashbackSegmentsAsync",
            typeof(IReadOnlyList<>).MakeGenericType(RequireType("ElgatoCapture.Models.FlashbackSegmentInfo")));
        AssertTaskReturningMethod(
            automationInterfaceType,
            "ProbeVideoSourceAsync",
            RequireType("ElgatoCapture.Models.VideoSourceProbeResult"));
        AssertTaskReturningMethod(
            automationInterfaceType,
            "ProbePreviewColorAsync",
            RequireType("ElgatoCapture.Models.PreviewColorProbeResult"));

        var interfaceText = ReadRepoFile("ElgatoCapture/Services/Automation/IAutomationViewModel.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadRepoFile("ElgatoCapture/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");
        var automationText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.Automation.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadRepoFile("ElgatoCapture/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var pipeServerText = ReadRepoFile("ElgatoCapture/Services/Automation/NamedPipeAutomationServer.cs")
            .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var deviceManagementText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.DeviceManagement.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("ElgatoCapture/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(interfaceText, "bool FlashbackPlay();");
        AssertDoesNotContain(interfaceText, "bool FlashbackPause();");
        AssertDoesNotContain(interfaceText, "bool FlashbackGoLive();");
        AssertDoesNotContain(interfaceText, "bool FlashbackBeginScrub(TimeSpan position);");
        AssertDoesNotContain(interfaceText, "bool FlashbackEndScrub();");
        AssertDoesNotContain(interfaceText, "VideoSourceProbeResult ProbeVideoSource();");
        AssertDoesNotContain(interfaceText, "PreviewColorProbeResult ProbePreviewColor();");
        AssertContains(dispatcherText, "await _viewModel.ExecuteFlashbackActionAsync(action, position, cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "await _viewModel.SetFlashbackEnabledAsync(enabled, cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "await _viewModel.GetFlashbackSegmentsAsync(cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "await _viewModel.ProbeVideoSourceAsync(cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "await _viewModel.ProbePreviewColorAsync(cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "await _viewModel.SetMicrophoneEnabledAsync(enabled, cancellationToken).ConfigureAwait(false)");
        AssertDoesNotContain(dispatcherText, "_viewModel.IsMicrophoneEnabled =");
        AssertContains(automationText, "public Task<bool> ExecuteFlashbackActionAsync(");
        AssertContains(automationText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationText, "InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken)");
        AssertContains(automationText, "=> FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);");
        AssertContains(automationText, "=> FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);");
        AssertContains(automationText, "=> FromSynchronousSnapshot(GetFlashbackSegments, cancellationToken);");
        AssertContains(automationText, "public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)\n        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);");
        AssertContains(automationText, "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertContains(automationText, "await _sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken).ConfigureAwait(false)");
        AssertContains(automationText, "CancellationToken.None).ConfigureAwait(false);");
        AssertContains(automationText, "_flashbackBitrateSamples.Clear();");
        AssertContains(automationText, "=> InvokeOnUiThreadAsync(() => RefreshDevicesAsync(cancellationToken), cancellationToken);");
        AssertContains(automationText, "await StartPreviewAsync(userInitiated: true, cancellationToken);");
        AssertContains(automationText, "await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);");
        AssertContains(captureText, "private async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureText, "public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(captureText, "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(deviceManagementText, "public async Task RefreshDevicesAsync(CancellationToken cancellationToken = default)");
        AssertContains(deviceManagementText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n                {\n                    throw;\n                }");
        AssertContains(automationText, "_suppressMicrophoneMonitorUpdate = true;");
        AssertContains(automationText, "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(");
        AssertContains(automationText, "cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "var previousEnabled = _micMonitorEnabled;");
        AssertContains(captureServiceText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);\n\n                _micMonitorEnabled = enabled;");
        var microphoneUpdateIndex = automationText.IndexOf(
            "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(",
            StringComparison.Ordinal);
        var microphonePersistIndex = automationText.IndexOf(
            "IsMicrophoneEnabled = enabled;",
            StringComparison.Ordinal);
        AssertEqual(
            true,
            microphoneUpdateIndex >= 0 && microphonePersistIndex > microphoneUpdateIndex,
            "automation microphone persists after monitor update");
        AssertContains(viewModelText, "if (_suppressMicrophoneMonitorUpdate)");
        AssertContains(viewModelText, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");
        AssertContains(coordinatorText, "return Task.FromCanceled(cancellationToken);");
        AssertContains(coordinatorText, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(coordinatorText, "cancellationRegistration = cancellationToken.Register");
        AssertContains(coordinatorText, "workItem.CancellationRegistration.Dispose();");
        AssertContains(coordinatorText, "public bool PropagateCancellationToOperation { get; init; }");
        AssertContains(coordinatorText, "bool propagateCancellationToOperation = false");
        AssertContains(coordinatorText, "propagateCancellationToOperation: true");
        AssertContains(pipeServerText, "var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestTimeout.Token, cancellationToken);");
        AssertContains(pipeServerText, "if (await WaitForDispatchCompletionAsync(dispatchTask, requestCancellation.Token).ConfigureAwait(false))");
        AssertContains(pipeServerText, "using var registration = cancellationToken.Register(");
        AssertContains(pipeServerText, "ObserveTimedOutDispatch(dispatchTask, request.Command, requestTimeout, requestCancellation);");
        AssertContains(pipeServerText, "Request timed out after {_requestTimeoutMs} ms.");
        AssertContains(pipeServerText, "\"request-timeout\"");
        AssertDoesNotContain(dispatcherText, "WaitConditionRefreshCadenceMs");

        return Task.CompletedTask;
    }

    private static void AssertTaskReturningMethod(Type type, string methodName, Type? resultType)
    {
        var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(method, $"{type.FullName}.{methodName}");
        AssertEqual(
            true,
            method!.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken)),
            $"{type.FullName}.{methodName} cancellation token");

        if (resultType == null)
        {
            AssertEqual(typeof(Task).FullName, method.ReturnType.FullName, $"{type.FullName}.{methodName} return type");
            return;
        }

        AssertEqual(true, method.ReturnType.IsGenericType, $"{type.FullName}.{methodName} generic Task return");
        AssertEqual(
            typeof(Task<>).FullName,
            method.ReturnType.GetGenericTypeDefinition().FullName,
            $"{type.FullName}.{methodName} generic Task definition");
        AssertEqual(
            resultType.FullName,
            method.ReturnType.GenericTypeArguments[0].FullName,
            $"{type.FullName}.{methodName} task result");
    }

    private static Task DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses()
    {
        var diagnosticsText = ReadRepoFile("ElgatoCapture/Services/Automation/AutomationDiagnosticsHub.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadRepoFile("ElgatoCapture/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");

        AssertContains(diagnosticsText, "private readonly SemaphoreSlim _refreshGate = new(1, 1);");
        AssertContains(diagnosticsText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnosticsText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetSnapshot:\n                {\n                    var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);\n                    var assertions = ParseAssertions(payload);");
        AssertContains(dispatcherText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync");
        AssertContains(dispatcherText, "return (true, snapshot);");
        AssertContains(dispatcherText, "snapshot: snapshot");
        AssertContains(dispatcherText, "AutomationSnapshot? snapshot = null");
        AssertContains(dispatcherText, "Snapshot = includeSnapshot ? snapshot ?? _diagnosticsHub.GetLatestSnapshot() : null");
        AssertContains(diagnosticsText, "\"flashback-export-stalled\"");
        AssertContains(diagnosticsText, "DiagnosticsCategory.Flashback");
        AssertContains(diagnosticsText, "health.FlashbackExportActive");
        AssertContains(diagnosticsText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnosticsText, "private const int FlashbackPlaybackCommandStallThresholdMs = 1000;");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackPendingCommands > 0");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs > snapshot.FlashbackPlaybackLastCommandProcessedUtcUnixMs");
        AssertContains(diagnosticsText, "Flashback playback command queue has not drained");
        AssertContains(diagnosticsText, "\"flashback_playback\"");
        AssertContains(diagnosticsText, "\"Flashback playback command queue is stalled.\"");
        AssertContains(diagnosticsText, "queuedAge={playbackCommandQueueAgeMs}ms");
        AssertContains(diagnosticsText, "\"flashback_export\"");
        AssertContains(diagnosticsText, "UpdatePreviewJitterRecentCounters(health, nowTick)");
        AssertContains(diagnosticsText, "recentDeadlineDrops={recentPreviewDeadlineDrops} recentUnderflows={recentPreviewUnderflows}");
        AssertContains(diagnosticsText, "if (recentPreviewDeadlineDrops > 0 ||\n            recentPreviewUnderflows > 3)");
        AssertContains(diagnosticsText, "var presentCadenceOverBudget =\n            previewRuntime.DisplayCadenceExpectedIntervalMs > 0 &&\n            previewRuntime.DisplayCadenceP95IntervalMs > previewRuntime.DisplayCadenceExpectedIntervalMs * 1.5;");
        AssertContains(diagnosticsText, "var unsyncedPresentCallSlow =\n            previewRuntime.D3DPresentSyncInterval == 0 &&\n            previewRuntime.D3DPresentCallP95Ms > 4.0;");
        AssertContains(diagnosticsText, "if (presentCadenceOverBudget ||\n            unsyncedPresentCallSlow)");
        AssertContains(diagnosticsText, "if (rendererSubmitted >= DiagnosticThresholds.RendererDropWarningMinSamples &&\n            rendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent)");
        AssertDoesNotContain(diagnosticsText, "rendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent) ||\n            previewRuntime.DisplayCadenceSlowFramePercent > 1.0");

        var captureServiceText = ReadRepoFile("ElgatoCapture/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        AssertContains(captureServiceText, "private readonly SemaphoreSlim _flashbackExportOperationLock = new(1, 1);");
        AssertContains(captureServiceText, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);");
        AssertContains(captureServiceText, "var exportId = 0L;");
        AssertContains(captureServiceText, "var evictionPaused = false;");
        AssertContains(captureServiceText, "exportId = BeginFlashbackExportDiagnostics(inPoint, outPoint, outputPath);");
        AssertContains(captureServiceText, "evictionPaused = bufferManager != null;");
        AssertContains(captureServiceText, "if (exportId != 0)");
        AssertContains(captureServiceText, "if (evictionPaused)");
        AssertContains(captureServiceText, "_lastExportResult = failure;");
        AssertContains(captureServiceText, "private FinalizeResult FailFlashbackExport(string outputPath, string statusMessage)");
        AssertContains(captureServiceText, "_lastExportResult = result;");
        AssertContains(captureServiceText, "RecordRejectedFlashbackExportDiagnostics(outputPath, result);");
        AssertContains(captureServiceText, "private void RecordRejectedFlashbackExportDiagnostics(string outputPath, FinalizeResult result)");
        AssertContains(captureServiceText, "if (_flashbackExportActive)");
        AssertContains(captureServiceText, "_flashbackExportStartedUtcUnixMs = now;");
        AssertContains(captureServiceText, "_flashbackExportCompletedUtcUnixMs = now;");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback buffer not active\");");
        AssertContains(captureServiceText, "? \"Cancelled\"");
        AssertContains(captureServiceText, "private static bool IsFlashbackExportCancelled(string? statusMessage)");
        AssertContains(captureServiceText, "_flashbackExportOperationLock.Release();");
        AssertContains(captureServiceText, "_flashbackExportOperationLock.Dispose();");
        AssertContains(captureServiceText, "Segments = BuildFlashbackExportSegments(bufferManager, segmentPaths)");
        AssertContains(captureServiceText, "StartPts = TimeSpan.FromMilliseconds(info.StartPtsMs)");

        var flashbackExporterText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        AssertContains(flashbackExporterText, "if (request.Segments is { Count: > 0 })");
        AssertContains(flashbackExporterText, "var useSegmentTimeline = segment.StartPts.HasValue");
        AssertContains(flashbackExporterText, "var comparePtsUs = useSegmentTimeline");
        AssertContains(flashbackExporterText, "ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(flashbackExporterText, "FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR");

        var sourceReaderText = ReadRepoFile("ElgatoCapture/Services/Capture/MfSourceReaderVideoCapture.cs")
            .Replace("\r\n", "\n");
        AssertContains(sourceReaderText, "Keep source cadence state coherent with diagnostics snapshots");
        AssertContains(sourceReaderText, "lock (_cadenceLock)");

        var diagnosticSessionText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        AssertContains(diagnosticSessionText, "var runFlashbackStress = scenario == \"flashback-stress\";");
        AssertContains(diagnosticSessionText, "var runFlashbackScrubStress = scenario == \"flashback-scrub-stress\";");
        AssertContains(diagnosticSessionText, "var runFlashbackRestartCycle = scenario == \"flashback-restart-cycle\";");
        AssertContains(diagnosticSessionText, "var runFlashbackExportPlayback = scenario == \"flashback-export-playback\";");
        AssertContains(diagnosticSessionText, "var runFlashbackLifecycle = scenario == \"flashback-lifecycle\";");
        AssertContains(diagnosticSessionText, "var runFlashbackExportConcurrent = scenario == \"flashback-export-concurrent\";");
        AssertContains(diagnosticSessionText, "var runFlashbackRecording = scenario == \"flashback-recording\";");
        AssertContains(diagnosticSessionText, "var runFlashbackExportRejected = scenario == \"flashback-export-rejected\";");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackPendingCommandsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxPendingCommandsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxCommandQueueLatencyMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackCommandsDroppedAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackCommandsSkippedNotReadyAtEnd");
        AssertContains(diagnosticSessionText, "Flashback Playback Commands:");
        AssertContains(diagnosticSessionText, "FlashbackRecordingBackendObserved");
        AssertContains(diagnosticSessionText, "FlashbackRecordingFileGrowthObserved");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoEncoderPacketsWrittenDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegritySequenceGapsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegrityQueueDroppedFramesAtEnd");
        AssertContains(diagnosticSessionText, "Flashback Recording:");
        AssertContains(diagnosticSessionText, "BuildFlashbackRecordingMetrics(samples)");
        AssertContains(diagnosticSessionText, "GetMaxSnapshotInt(samples, lastSnapshot, \"FlashbackPlaybackMaxPendingCommands\")");
        AssertContains(diagnosticSessionText, "GetMaxSnapshotInt(samples, lastSnapshot, \"FlashbackPlaybackMaxCommandQueueLatencyMs\")");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackStressAsync(");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackScrubStressAsync(");
        AssertContains(diagnosticSessionText, "flashback scrub stress seek burst requested");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(diagnosticSessionText, "flashback restart cycle export verified");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(diagnosticSessionText, "flashback export during playback verified");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackLifecycleAsync(");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(diagnosticSessionText, "var exportTaskA = sendCommandAsync(\"FlashbackExport\", exportPayloadA, 60_000);");
        AssertContains(diagnosticSessionText, "var exportTaskB = sendCommandAsync(\"FlashbackExport\", exportPayloadB, 60_000);");
        AssertContains(diagnosticSessionText, "flashback concurrent exports verified");
        AssertContains(diagnosticSessionText, "flashback lifecycle disabled during playback");
        AssertContains(diagnosticSessionText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(diagnosticSessionText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(diagnosticSessionText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(diagnosticSessionText, "private static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(diagnosticSessionText, "private static void ValidateFlashbackRecordingSession(");
        AssertContains(diagnosticSessionText, "\"flashback recording: RecordingBackend never reported Flashback\"");
        AssertContains(diagnosticSessionText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(diagnosticSessionText, "submittedDelta");
        AssertContains(diagnosticSessionText, "packetsDelta");
        AssertContains(diagnosticSessionText, "RecordingIntegritySequenceGaps");
        AssertContains(diagnosticSessionText, "RecordingIntegrityQueueDroppedFrames");
        AssertContains(diagnosticSessionText, "GetInt(snapshot, \"FlashbackBufferedDurationMs\") >= 8_000");
        AssertContains(diagnosticSessionText, "GetInt(snapshot, \"FlashbackEncodedFrames\") >= 240");
        AssertContains(diagnosticSessionText, "\"flashback stress: Flashback buffer did not become export-ready within 30s\"");
        AssertContains(diagnosticSessionText, "\"FlashbackAction\", new Dictionary<string, object?> { [\"action\"] = \"pause\" }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"seek\", [\"positionMs\"] = 500 }");
        AssertContains(diagnosticSessionText, "foreach (var positionMs in new[] { 750, 1_250, 2_000, 3_250, 1_500 })");
        AssertContains(diagnosticSessionText, "actions.Add(\"flashback scrub burst requested\");");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"seconds\"] = 1, [\"outputPath\"] = exportPath }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"filePath\"] = exportPath, [\"strict\"] = true }");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(diagnosticSessionText, "$\"maxPending={GetInt(lastSnapshot, \"FlashbackPlaybackMaxPendingCommands\")} \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyMs={GetInt(lastSnapshot, \"FlashbackPlaybackMaxCommandQueueLatencyMs\")}\"");
        AssertContains(diagnosticSessionText, "private const int FlashbackStressMaxPlaybackPendingCommands = 3;");
        AssertContains(diagnosticSessionText, "private const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback command latency exceeded threshold \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyMs={maxLatencyMs}/{FlashbackStressMaxPlaybackCommandLatencyMs}\"");
        AssertContains(diagnosticSessionText, "\"flashback-rejected-export.mp4\"");
        AssertContains(diagnosticSessionText, "$\"flashback export rejected: expected Failed status, got {status}\"");
        AssertContains(diagnosticSessionText, "message.Contains(\"Flashback buffer not active\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticSessionText, "(!(runFlashbackStress || runFlashbackScrubStress || runFlashbackRestartCycle || runFlashbackExportPlayback || runFlashbackLifecycle || runFlashbackExportConcurrent || runFlashbackRecording || runFlashbackExportRejected) || warnings.Count == 0)");
        AssertContains(diagnosticSessionText, "\"observe\" or \"preview-only\" or \"recording-only\" or \"flashback\" or \"flashback-stress\" or \"flashback-scrub-stress\" or \"flashback-restart-cycle\" or \"flashback-export-playback\" or \"flashback-lifecycle\" or \"flashback-export-concurrent\" or \"flashback-recording\" or \"flashback-export-rejected\" or \"combined\"");

        var ecctlProgramText = ReadRepoFile("tools/ecctl/Program.cs")
            .Replace("\r\n", "\n");
        var ecctlCommandHandlersText = ReadRepoFile("tools/ecctl/CommandHandlers.cs")
            .Replace("\r\n", "\n");
        var mcpDiagnosticSessionText = ReadRepoFile("tools/McpServer/Tools/DiagnosticSessionTools.cs")
            .Replace("\r\n", "\n");
        AssertContains(ecctlProgramText, "flashback-export-playback");
        AssertContains(ecctlCommandHandlersText, "flashback-export-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-export-playback");

        return Task.CompletedTask;
    }

    private static Task AutomationProtocol_SetRecordingUsesRecordingSizedTimeout()
    {
        var protocolText = ReadRepoFile("tools/Common/AutomationPipeProtocol.cs")
            .Replace("\r\n", "\n");
        var clientText = ReadRepoFile("tools/AutomationClient/Program.cs")
            .Replace("\r\n", "\n");

        AssertContains(protocolText, "internal const int DefaultResponseTimeoutMs = 15000;");
        AssertContains(protocolText, "internal const int ExtendedResponseTimeoutMs = 60000;");
        AssertContains(protocolText, "internal const int RecordingResponseTimeoutMs = 150000;");
        AssertContains(protocolText, "internal const int FlashbackMutationResponseTimeoutMs = 305000;");
        AssertContains(protocolText, "commandName = ResolveCanonicalCommandName(commandName);");
        AssertContains(protocolText, "\"SetRecordingEnabled\" => RecordingResponseTimeoutMs");
        AssertContains(protocolText, "\"RestartFlashback\" or \"SetFlashbackEnabled\" => FlashbackMutationResponseTimeoutMs");
        AssertContains(protocolText, "_ => DefaultResponseTimeoutMs");
        AssertContains(protocolText, "return commandTimeoutMs;");
        AssertDoesNotContain(protocolText, "AlignResponseTimeoutWithServerRequest");
        AssertContains(clientText, "AutomationPipeProtocol.TryGetCommandName(commandValue, out var canonicalCommandName)");
        AssertContains(clientText, "AutomationPipeProtocol.GetDefaultResponseTimeout(timeoutCommandName)");
        AssertContains(clientText, "public int? ResponseTimeoutMs { get; set; }");
        var pipeClientText = ReadRepoFile("tools/Common/AutomationPipeClient.cs")
            .Replace("\r\n", "\n");
        AssertDoesNotContain(pipeClientText, "AlignResponseTimeoutWithServerRequest");

        var protocolType = RequireType("ElgatoCapture.Tools.AutomationPipeProtocol");
        var getDefaultResponseTimeout = protocolType.GetMethod(
            "GetDefaultResponseTimeout",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.GetDefaultResponseTimeout not found.");

        foreach (var acceptedName in new[] { "SetRecordingEnabled", "setrecordingenabled", "set-recording-enabled", "17" })
        {
            var timeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { acceptedName })!;
            AssertEqual(150000, timeoutMs, $"SetRecordingEnabled timeout for '{acceptedName}'");
        }

        var defaultTimeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { "GetSnapshot" })!;
        AssertEqual(15000, defaultTimeoutMs, "GetSnapshot timeout remains bounded");

        var extendedTimeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { "FlashbackExport" })!;
        AssertEqual(60000, extendedTimeoutMs, "FlashbackExport uses extended timeout");

        foreach (var acceptedName in new[] { "SetFlashbackEnabled", "set-flashback-enabled", "RestartFlashback" })
        {
            var timeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { acceptedName })!;
            AssertEqual(305000, timeoutMs, $"Flashback mutation timeout for '{acceptedName}' outlives server cancellation");
        }

        return Task.CompletedTask;
    }

    private static Task MainViewModelCapture_RecordingFailuresPropagateToCallers()
    {
        var captureText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureText, "Logger.LogException(ex);");
        AssertContains(captureText, "IsRecording = _sessionCoordinator.Snapshot.IsRecording;");
        AssertContains(captureText, "StatusText = $\"Recording failed: {ex.Message}\";");
        AssertContains(captureText, "StatusText = $\"Stop recording failed: {ex.Message}\";");
        AssertContains(captureText, "throw;");

        return Task.CompletedTask;
    }

    private static Task MainWindowClose_CancelsCloseUntilRecordingStopCompletes()
    {
        var windowCtorText = ReadRepoFile("ElgatoCapture/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var windowManagementText = ReadRepoFile("ElgatoCapture/MainWindow.WindowManagement.cs")
            .Replace("\r\n", "\n");

        AssertContains(windowCtorText, "appWindow.Closing += MainWindow_Closing;");
        AssertContains(windowManagementText, "args.Cancel = true;");
        AssertContains(windowManagementText, "TryStopRecordingBeforeCloseAsync");
        AssertContains(windowManagementText, "const int StopBudgetMs = 120_000;");
        AssertContains(windowManagementText, "close cancelled to protect recording");
        AssertContains(windowManagementText, "RequestWindowClose();");
        AssertDoesNotContain(windowManagementText, "MP4 may be truncated.");

        return Task.CompletedTask;
    }

    private static Task ExternalProcessProbes_UseBoundedProcessSupervisor()
    {
        var ffmpegText = ReadRepoFile("ElgatoCapture/Services/Runtime/FfmpegRuntimeLocator.cs")
            .Replace("\r\n", "\n");
        var hdrText = ReadRepoFile("ElgatoCapture/Services/Recording/HdrValidationRunner.cs")
            .Replace("\r\n", "\n");

        AssertContains(ffmpegText, "private const int ProbeTimeoutMs = 10_000;");
        AssertContains(ffmpegText, "new ProcessSupervisor().RunAsync");
        AssertContains(ffmpegText, "TimeoutMs = ProbeTimeoutMs");
        AssertContains(ffmpegText, "if (!result.Started || result.TimedOut || result.ExitCode != 0)");
        AssertContains(ffmpegText, "return result.Started && !result.TimedOut && result.ExitCode == 0;");
        AssertContains(hdrText, "private const int ValidationTimeoutMs = 30_000;");
        AssertContains(hdrText, "new ProcessSupervisor().RunAsync");
        AssertContains(hdrText, "validator-timeout");

        return Task.CompletedTask;
    }

    private static Task RecordingStop_PropagatesUnifiedVideoStopFailure()
    {
        var captureServiceText = ReadRepoFile("ElgatoCapture/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "Unified video recording stop failed");
        AssertContains(captureServiceText, "FinalizeResult.Failure(fallbackOutputPath, $\"Unified video recording stop failed: {ex.Message}\");");
        AssertContains(captureServiceText, "var sinkResult = await sink.StopAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "if (result.Succeeded)\n                {\n                    result = sinkResult;");

        return Task.CompletedTask;
    }

    private static Task PreviewStopCompatibilityOverloads_ArePreserved()
    {
        var captureServiceText = ReadRepoFile("ElgatoCapture/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadRepoFile("ElgatoCapture/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureServiceText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(captureServiceText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(coordinatorText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(coordinatorText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(coordinatorText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(viewModelText, "public Task StopPreviewAsync()\n        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);");
        AssertContains(viewModelText, "public Task StopPreviewAsync(bool userInitiated)\n        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);");

        return Task.CompletedTask;
    }

    private static Task PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity()
    {
        AssertPreviewStopSurface("ElgatoCapture.Services.Capture.CaptureService");
        AssertPreviewStopSurface("ElgatoCapture.Services.Capture.CaptureSessionCoordinator");
        return Task.CompletedTask;
    }

    private static void AssertPreviewStopSurface(string typeName)
    {
        var type = RequireType(typeName);
        AssertStopSurface(type, "StopVideoPreviewAsync", "StopVideoPreviewWithTeardownAsync");
        AssertStopSurface(type, "StopAudioPreviewAsync", "StopAudioPreviewWithTeardownAsync");
    }

    private static void AssertStopSurface(Type type, string stopMethodName, string teardownMethodName)
    {
        var publicInstance = BindingFlags.Instance | BindingFlags.Public;
        var oneParameterStopOverloads = type.GetMethods(publicInstance)
            .Where(method => method.Name == stopMethodName && method.GetParameters().Length == 1)
            .ToArray();

        AssertEqual(1, oneParameterStopOverloads.Length, $"{type.FullName}.{stopMethodName} one-parameter overload count");
        AssertEqual(
            typeof(CancellationToken).FullName,
            oneParameterStopOverloads[0].GetParameters()[0].ParameterType.FullName,
            $"{type.FullName}.{stopMethodName} single parameter");

        var boolFirstParameterOverloads = type.GetMethods(publicInstance)
            .Where(method =>
            {
                if (method.Name != stopMethodName)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length > 0 && parameters[0].ParameterType == typeof(bool);
            })
            .ToArray();
        AssertEqual(0, boolFirstParameterOverloads.Length, $"{type.FullName}.{stopMethodName} bool-first overload count");

        var teardownMethod = type.GetMethod(teardownMethodName, publicInstance, binder: null, types: new[] { typeof(CancellationToken) }, modifiers: null);
        AssertNotNull(teardownMethod, $"{type.FullName}.{teardownMethodName}(CancellationToken)");
    }

    private static Task EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread()
    {
        var appText = ReadRepoFile("ElgatoCapture/App.xaml.cs")
            .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("ElgatoCapture/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureText, "internal Task StopRecordingForEmergencyAsync");
        AssertContains(captureText, "=> _sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertContains(appText, "var task = viewModel.StopRecordingForEmergencyAsync();");
        AssertContains(appText, "if (e.IsTerminating || !recoverable)");
        AssertDoesNotContain(appText, "Task.Run(async () =>");
        AssertDoesNotContain(appText, "StopRecordingAndWaitAsync().ConfigureAwait(false)");
        AssertDoesNotContain(appText, "viewModel == null || !viewModel.IsRecording");
        AssertDoesNotContain(captureText, "if (!IsRecording)");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_CleansStaleSessionDirectories()
    {
        var bufferText = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackBufferManager.cs")
            .Replace("\r\n", "\n");

        AssertContains(bufferText, "private static readonly TimeSpan StaleSessionMinAge = TimeSpan.FromHours(12);");
        AssertContains(bufferText, "private const int MaxStaleSessionDirectoryScansPerInit = 64;");
        AssertContains(bufferText, "private const int MaxStaleRootSegmentFileScansPerInit = 512;");
        AssertContains(bufferText, "CleanupStaleRootSegmentFiles(_options.TempDirectory);");
        AssertContains(bufferText, "CleanupStaleSessionDirectories(_options.TempDirectory, sessionDirectory);");
        AssertContains(bufferText, "if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))");
        AssertContains(bufferText, "info.EnumerateFiles(\"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(bufferText, "Directory.EnumerateFiles(tempDirectory, \"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(bufferText, "Directory.Delete(fullPath, recursive: true);");

        return Task.CompletedTask;
    }
}
