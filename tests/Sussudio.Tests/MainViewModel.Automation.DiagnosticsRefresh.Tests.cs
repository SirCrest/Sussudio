using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses()
    {
        var diagnostics = ReadAutomationDiagnosticsHubSourceFamily();
        var countersText = ReadAutomationDiagnosticsHubCountersSource();
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertDiagnosticsRefreshCoreOwnership(diagnostics);
        AssertDiagnosticsAlertEventOwnership(diagnostics);
        AssertDiagnosticsSnapshotStatusProjectionOwnership(diagnostics);
        AssertDiagnosticsRefreshSnapshotProjectionOwnership(diagnostics);
        AssertDiagnosticsRefreshPipelineOwnership(diagnostics, dispatcherText);
        AssertDiagnosticsRefreshFlashbackRecordingAndStorageAlertCoverage(diagnostics, countersText);
        AssertDiagnosticsRefreshFlashbackPlaybackAndPreviewAlertCoverage(diagnostics, countersText);
        AssertDiagnosticsRefreshFlashbackExportOwnership(dispatcherText);
        AssertDiagnosticsRefreshSourceReaderOwnership();

        var diagnosticSessionSources = ReadDiagnosticSessionSourceFamily();
        AssertDiagnosticSessionCoreOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionPlaybackMetricsOwnership(diagnosticSessionSources.SourceFamilyText);
        AssertDiagnosticSessionPreviewMetricsOwnership(diagnosticSessionSources.SourceFamilyText, diagnostics);
        AssertDiagnosticSessionExportRecordingOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionFlashbackScenarioOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionToolSurfaceOwnership();

        return Task.CompletedTask;
    }

    private static void AssertDiagnosticsRefreshCoreOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertDiagnosticsRefreshEvaluationOwnership(diagnostics);
        AssertDiagnosticsRefreshRuntimeOwnership(diagnostics);
        AssertDiagnosticsRefreshSnapshotConstructionOwnership(diagnostics);
    }

    internal static Task Diagnostics_HdrTruthVerdict_TreatsHdrSourceSdrRequestAsExpected()
    {
        var diagnosticsType = RequireType("Sussudio.Services.Automation.AutomationDiagnosticsHub");
        var runtimeType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var verifierResultType = RequireType("Sussudio.Models.RecordingVerificationResult");
        var method = diagnosticsType.GetMethod(
            "BuildHdrTruthVerdict",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { runtimeType, typeof(bool), verifierResultType },
            modifiers: null)
            ?? throw new InvalidOperationException("BuildHdrTruthVerdict not found.");

        var runtime = Activator.CreateInstance(runtimeType)!;
        SetPropertyBackingField(runtime, "LatestObservedFramePixelFormat", "NV12");
        SetPropertyBackingField(runtime, "ObservedNv12FrameCount", 1L);
        SetPropertyBackingField(runtime, "SourceIsHdr", (bool?)true);

        var verdict = method.Invoke(null, new object?[] { runtime, false, null })
            ?? throw new InvalidOperationException("BuildHdrTruthVerdict returned null.");

        AssertEqual("expected-sdr-capture", GetStringProperty(verdict, "SourceVsCaptureParity"), "SourceVsCaptureParity");
        AssertEqual("sdr-8bit", GetStringProperty(verdict, "FinalClassification"), "FinalClassification");

        return Task.CompletedTask;
    }

    private static void AssertDiagnosticsRefreshRuntimeOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.VerificationText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.VerificationText, "public async Task<RecordingVerificationResult> VerifyFileAsync");
        AssertContains(diagnostics.VerificationText, "private bool ShouldAutoVerifySnapshot(");
        AssertContains(diagnostics.VerificationText, "private RecordingVerificationResult? CaptureLastVerificationForSnapshot(");
        AssertContains(diagnostics.VerificationText, "private void ScheduleAutoVerificationIfNeeded(");
        AssertContains(diagnostics.VerificationText, "Automatic recording verification started.");
        AssertContains(diagnostics.VerificationText, "private static CaptureRuntimeSnapshot ApplyVerificationProfile(");
        AssertContains(diagnostics.VerificationText, "string.Equals(verificationProfile, \"flashback-export\"");
        AssertDoesNotContain(diagnostics.HubText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnostics.HubText, "private readonly IAutomationSnapshotQueryPort _snapshotQueryPort;");
        AssertContains(diagnostics.HubText, "IAutomationSnapshotQueryPort snapshotQueryPort,");
        AssertContains(diagnostics.HubText, "_snapshotQueryPort = snapshotQueryPort ?? throw new ArgumentNullException(nameof(snapshotQueryPort));");
        AssertDoesNotContain(diagnostics.HubText, "IAutomationViewModel viewModel,");
        AssertDoesNotContain(diagnostics.HubText, "private readonly IAutomationViewModel _viewModel;");
        AssertContains(diagnostics.SnapshotsText, "await _snapshotQueryPort\n            .GetViewModelRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.SnapshotsText, "await _snapshotQueryPort\n            .GetCaptureRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.VerificationText, "await _snapshotQueryPort\n                .GetCaptureRuntimeSnapshotAsync(cancellationToken)");
        AssertContains(diagnostics.SnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnostics.SnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertContains(diagnostics.SnapshotsText, "private static PreviewPacingClassification ClassifyPreviewPacing(");
        AssertContains(diagnostics.SnapshotsText, "ClassifyPreviewPacing(");
        AssertContains(diagnostics.HubText, "public void Start()");
        AssertContains(diagnostics.HubText, "private async Task RunLoopAsync(CancellationToken cancellationToken)");
        AssertContains(diagnostics.HdrText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.HdrText, "private static PreviewHdrState BuildPreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private readonly record struct PreviewHdrState(");
        AssertContains(diagnostics.HdrText, "private static bool IsHdrSubtype(string? subtype)");
        AssertContains(diagnostics.HdrText, "static string NormalizeFormatToken(string? text)");
        AssertDoesNotContain(diagnostics.HubText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnostics.SnapshotsText, "var previewHdrState = BuildPreviewHdrState(captureRuntime, viewModelSnapshot, previewRuntime);");
        AssertDoesNotContain(diagnostics.SnapshotsText, "var previewHdrInputDetected =");
        AssertContains(diagnostics.SnapshotsText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
    }

    private static void AssertDiagnosticsRefreshPipelineOwnership(AutomationDiagnosticsHubSourceFamily diagnostics, string dispatcherText)
    {
        AssertContains(diagnostics.SnapshotsText, "var snapshot = BuildAutomationSnapshot(");
        AssertDoesNotContain(diagnostics.SnapshotsText, "new AutomationSnapshot");
        AssertContains(diagnostics.SnapshotsText, "AppendPerformanceTimelineEntry(snapshot);");
        AssertContains(diagnostics.SnapshotsCoreText, "public AutomationSnapshot GetLatestSnapshot()");
        AssertContains(diagnostics.SnapshotsCoreText, "public Task<AutomationSnapshot> RefreshSnapshotNowAsync(CancellationToken cancellationToken = default)");
        AssertContains(diagnostics.SnapshotsCoreText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SnapshotsCoreText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SnapshotsCoreText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnostics.SnapshotsCoreText, "private AudioSignalState UpdateAudioSignalState(");
        AssertContains(diagnostics.SnapshotsCoreText, "private bool UpdateRecordingFileGrowthState(");
        AssertContains(diagnostics.SnapshotsCoreText, "private readonly record struct AudioSignalState(");
        AssertContains(diagnostics.SnapshotsText, "UpdateAudioSignalState(viewModelSnapshot, nowTick);");
        AssertContains(diagnostics.SnapshotsText, "UpdateRecordingFileGrowthState(");
        AssertContains(diagnostics.SnapshotsCoreText, "var audioSignalPresent = viewModelSnapshot.AudioPeak >= AudioSignalThreshold;");
        AssertContains(diagnostics.SnapshotsCoreText, "private LastOutputProbe ProbeLastOutput(");
        AssertContains(diagnostics.SnapshotsCoreText, "private readonly record struct LastOutputProbe(");
        AssertContains(diagnostics.SnapshotsCoreText, "private ProcessResourceSnapshot CaptureProcessResourceSnapshot()");
        AssertContains(diagnostics.SnapshotsCoreText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnostics.SnapshotsCoreText, "private readonly record struct ProcessResourceSnapshot(");
        AssertContains(diagnostics.TimelineText, "public IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline");
        AssertContains(diagnostics.TimelineText, "private void AppendPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.TimelineText, "BuildPerformanceTimelineEntry(snapshot)");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineEntry BuildPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.TimelineText, "var core = BuildPerformanceTimelineCoreProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var preview = BuildPerformanceTimelinePreviewProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var flashbackPlayback = BuildPerformanceTimelineFlashbackPlaybackProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var flashbackExport = BuildPerformanceTimelineFlashbackExportProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "var system = BuildPerformanceTimelineSystemProjection(snapshot);");
        AssertContains(diagnostics.TimelineText, "CaptureCadenceFivePercentLowFps = core.CaptureCadenceFivePercentLowFps");
        AssertContains(diagnostics.TimelineText, "PreviewD3DPresentCallP95Ms = preview.D3DPresentCallP95Ms");
        AssertContains(diagnostics.TimelineText, "FlashbackPlaybackCommandsEnqueued = flashbackPlayback.CommandsEnqueued");
        AssertContains(diagnostics.TimelineText, "FlashbackExportPercent = flashbackExport.Percent");
        AssertContains(diagnostics.TimelineText, "ProcessCpuPercent = system.ProcessCpuPercent");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineCoreProjection BuildPerformanceTimelineCoreProjection(");
        AssertContains(diagnostics.TimelineText, "CaptureCadenceFivePercentLowFps: snapshot.CaptureCadenceFivePercentLowFps");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "private static PerformanceTimelinePreviewProjection BuildPerformanceTimelinePreviewProjection(");
        AssertContains(diagnostics.TimelineProjectionPreviewText, "D3DPresentCallP95Ms: snapshot.PreviewD3DPresentCallP95Ms");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackProjection BuildPerformanceTimelineFlashbackPlaybackProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var cadence = BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var decode = BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var commands = BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var audioMaster = BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var stages = BuildPerformanceTimelineFlashbackPlaybackStagesProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "var backend = BuildPerformanceTimelineFlashbackPlaybackBackendProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackCadenceProjection BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackDecodeProjection BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackCommandsProjection BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackAudioMasterProjection BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackStagesProjection BuildPerformanceTimelineFlashbackPlaybackStagesProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackBackendProjection BuildPerformanceTimelineFlashbackPlaybackBackendProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "CommandsEnqueued: snapshot.FlashbackPlaybackCommandsEnqueued");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineFlashbackExportProjection BuildPerformanceTimelineFlashbackExportProjection(");
        AssertContains(diagnostics.TimelineText, "Percent: snapshot.FlashbackExportPercent");
        AssertContains(diagnostics.TimelineText, "private static PerformanceTimelineSystemProjection BuildPerformanceTimelineSystemProjection(");
        AssertContains(diagnostics.TimelineText, "ProcessCpuPercent: snapshot.ProcessCpuPercent");
        AssertDoesNotContain(diagnostics.HubText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnostics.SnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnostics.SnapshotsText, "var lastVerification = CaptureLastVerificationForSnapshot(recordingStarted);");
        AssertDoesNotContain(diagnostics.SnapshotsText, "_lastVerification = null;");
        AssertContains(diagnostics.SnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertDoesNotContain(diagnostics.SnapshotsText, "Automatic recording verification started.");
        AssertContains(diagnostics.SnapshotsCoreText, "new FileInfo(lastOutputPath).Length");
        AssertContains(diagnostics.SnapshotsCoreText, "GC.GetGCMemoryInfo()");
        AssertDoesNotContain(diagnostics.HubText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnostics.SourceFamilyText, "private readonly SemaphoreSlim _refreshGate = new(1, 1);");
        AssertContains(diagnostics.SourceFamilyText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SourceFamilyText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetSnapshot:\n                return await ExecuteGetSnapshotCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "private async Task<AutomationCommandResponse> ExecuteGetSnapshotCommandAsync(");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);\n        var assertions = ParseAssertions(payload);");
        AssertContains(dispatcherText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync");
        AssertContains(dispatcherText, "return (true, snapshot);");
        AssertContains(dispatcherText, "snapshot: snapshot");
        AssertContains(dispatcherText, "AutomationSnapshot? snapshot = null");
        AssertContains(dispatcherText, "Snapshot = includeSnapshot ? snapshot ?? _diagnosticsHub.GetLatestSnapshot() : null");
    }

    private static void AssertDiagnosticsPreviewRuntimeProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewRuntimeFlattening.Frame.EstimatedPipelineLatencyMs,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewFramesArrived = previewRuntimeFlattening.Frame.FramesArrived,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.Cadence.OnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewAdapterColorMetadata = previewRuntimeFlattening.Color.AdapterColorMetadata,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "OnePercentLowFps = cadence.OnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererAttached = surface.RendererAttached");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Strategy = startup.Strategy,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "PositionEventCount = gpuPlayback.PositionEventCount");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = color.AdapterColorMetadata");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererAttached = previewRuntime.RendererAttached");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "PlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = captureRuntime.PreviewColorMetadata");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewHdrInputDetected = previewHdrState.InputDetected,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionText, "PreviewAdapterColorMetadata = captureRuntime.PreviewColorMetadata,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewFramesArrived = previewSummary.FramesArrived,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewStartupStrategy = previewSummary.Startup.Strategy,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewAdapterColorMetadata = previewSummary.AdapterColorMetadata,");
    }

    private static void AssertDiagnosticSessionToolSurfaceOwnership()
    {
        var diagnosticSessionToolSources = ReadDiagnosticSessionToolSurfaceSourceFamily();
        var ssctlProgramText = diagnosticSessionToolSources.SsctlProgramText;
        var ssctlHelpText = diagnosticSessionToolSources.SsctlHelpText;
        var ssctlCommandHandlersText = diagnosticSessionToolSources.SsctlCommandHandlersText;
        var mcpDiagnosticSessionText = diagnosticSessionToolSources.McpDiagnosticSessionText;
        AssertContains(ssctlProgramText, "SsctlHelpWriter.Write(Console.Out);");
        AssertDoesNotContain(ssctlProgramText, "DiagnosticSessionScenarioCatalog.HelpList");
        AssertContains(ssctlHelpText, "DiagnosticSessionOptions.CliUsage");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.CliUsage");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultScenario");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultDurationSeconds");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionOptions.DefaultSampleIntervalMs");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionScenarioCatalog.Description");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultScenario");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultDurationSeconds");
        AssertContains(mcpDiagnosticSessionText, "DiagnosticSessionOptions.DefaultSampleIntervalMs");
        AssertDoesNotContain(mcpDiagnosticSessionText, "Session scenario: observe,");
        AssertDoesNotContain(mcpDiagnosticSessionText, "string scenario = \"observe\"");
        AssertDoesNotContain(mcpDiagnosticSessionText, "int seconds = 10");
        AssertDoesNotContain(mcpDiagnosticSessionText, "int sampleIntervalMs = 1000");
    }
}
static partial class Program
{
    private static void AssertDiagnosticSessionCoreOwnership(DiagnosticSessionSourceFamily diagnosticSessionSources)
    {
        var diagnosticSessionText = diagnosticSessionSources.SourceFamilyText;
        var diagnosticSessionModelsText = diagnosticSessionSources.ModelsText;
        var diagnosticScenariosText = diagnosticSessionSources.ScenariosText;
        AssertContains(diagnosticSessionText, "var scenario = DiagnosticSessionScenarioCatalog.Normalize(options.Scenario);");
        AssertContains(diagnosticSessionText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(diagnosticSessionText, "var backgroundTasks = new DiagnosticSessionBackgroundTasks();");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.NeedsPreview(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.NeedsRecording(scenario)");
        AssertContains(diagnosticSessionText, "scenarioPlan.RequiresFlashbackRecordingReadiness");
        AssertContains(diagnosticSessionText, "scenarioPlan.UsesFlashbackScenarioWarningPolicy");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.IsPreviewCycleScenario");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionBackgroundTasks");
        AssertContains(diagnosticSessionText, "internal static class DiagnosticSessionScenarioSetup");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionLiveStateWriter");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionRunContext : IDisposable");
        AssertContains(diagnosticSessionText, "RunState = new DiagnosticSessionRunState(");
        AssertContains(diagnosticSessionText, "_liveStateWriter = new DiagnosticSessionLiveStateWriter(");
        AssertContains(diagnosticSessionText, ".CompleteRegisteredScenarioWorkAsync(");
        AssertContains(diagnosticSessionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertContains(diagnosticScenariosText, "internal static class DiagnosticSessionScenarioCatalog");
        AssertDoesNotContain(diagnosticScenariosText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPlayback = \"flashback-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackStress = \"flashback-stress\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackScrubStress = \"flashback-scrub-stress\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRestartCycle = \"flashback-restart-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackEncoderCycle = \"flashback-encoder-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportPlayback = \"flashback-export-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackSegmentPlayback = \"flashback-segment-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRangeExport = \"flashback-range-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRangeExportAudioSwitch = \"flashback-range-export-audio-switch\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackLifecycle = \"flashback-lifecycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportConcurrent = \"flashback-export-concurrent\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackDisableDuringExport = \"flashback-disable-during-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRotatedExport = \"flashback-rotated-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPreviewCycle = \"flashback-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPlaybackPreviewCycle = \"flashback-playback-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecording = \"flashback-recording\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingPreviewCycle = \"flashback-recording-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingSettingsDeferred = \"flashback-recording-settings-deferred\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingExportRejected = \"flashback-recording-export-rejected\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportRejected = \"flashback-export-rejected\";");
        AssertContains(diagnosticSessionText, "internal readonly record struct DiagnosticSessionScenarioPlan(");
        AssertContains(diagnosticSessionText, "catch (AutomationPipeException ex) when (ex is not AutomationPipeConnectException)");
        AssertContains(diagnosticSessionText, "return BuildLocalFailureResponse(command, ex.Message);");
        AssertContains(diagnosticSessionText, "catch (JsonException ex)");
        AssertContains(diagnosticSessionModelsText, "public sealed class DiagnosticSessionResult");
        AssertDoesNotContain(diagnosticSessionModelsText, "public sealed partial class DiagnosticSessionResult");
        AssertContains(diagnosticSessionModelsText, "public string TerminalState { get; set; }");
        AssertContains(diagnosticSessionText, "LivePath = _liveStateWriter.LivePath;");
        AssertContains(diagnosticSessionText, "CreateUnknownInitialSnapshot()");
        AssertContains(diagnosticSessionText, "InitialSnapshotKnown = initialSnapshotResult.Known;");
        AssertContains(diagnosticSessionText, "skipped state-mutating scenario");
        AssertContains(diagnosticSessionText, "CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs))");
        AssertContains(diagnosticSessionText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionText, "recordingCleanupTimeoutMs,");
        AssertContains(diagnosticSessionText, "private static async Task<bool> StopRecordingForCleanupAsync(");
        AssertContains(diagnosticSessionText, "var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;");
        AssertContains(diagnosticSessionText, "if (!startedRecording || (!shouldStopRecordingForVerification && options.LeaveRunning))");
        AssertContains(diagnosticSessionText, "recording stopped for verification");
        AssertContains(diagnosticSessionText, "var stoppedRecordingForVerification = await StopRecordingForCleanupAsync(");
        AssertContains(diagnosticSessionText, "var stoppedRecordingForVerification = shouldStopRecordingForVerification &&");
        AssertContains(diagnosticSessionText, "var diagnosticHealthSnapshot = request.StoppedRecordingForVerification");
        AssertContains(diagnosticSessionText, ".WaitAsync(cancellationToken)");
        AssertContains(diagnosticSessionText, "context.ScenarioCancellationSource.Cancel();");
        AssertContains(diagnosticSessionText, "WriteSamplingLiveStateBestEffortAsync");
        AssertContains(diagnosticSessionText, "context.RecordTerminalException(ex, context.GetLastStage())");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, \"final-snapshot\");");
        AssertContains(diagnosticSessionText, "WriteArtifactBestEffortAsync(\"write-samples\", paths.SamplesPath, samples)");
        AssertContains(diagnosticSessionText, "await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None)");
    }
}
static partial class Program
{
    private static void AssertDiagnosticSessionExportRecordingOwnership(DiagnosticSessionSourceFamily diagnosticSessionSources)
    {
        var diagnosticSessionText = diagnosticSessionSources.SourceFamilyText;
        var diagnosticScenariosText = diagnosticSessionSources.ScenariosText;

        AssertContains(diagnosticSessionText, "FlashbackRecordingFileGrowthObserved");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoEncoderPacketsWrittenDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegritySequenceGapsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegrityQueueDroppedFramesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegritySequenceGapsDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta");
        AssertContains(diagnosticSessionText, "firstRecordingSample,\n                \"RecordingIntegritySequenceGaps\")");
        AssertContains(diagnosticSessionText, "firstRecordingSample,\n                \"RecordingIntegrityQueueDroppedFrames\")");
        AssertContains(diagnosticSessionText, "Flashback Recording:");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxElapsedMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMessageAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportFailureKindAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportOutputPathAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportForceRotateFallbacksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportForceRotateFallbacksDelta");
        AssertContains(diagnosticSessionText, "FlashbackExportLastForceRotateFallbackSegmentsAtEnd");
        AssertContains(diagnosticSessionText, "LastExportIdAtEnd");
        AssertContains(diagnosticSessionText, "LastExportSuccessAtEnd");
        AssertContains(diagnosticSessionText, "LastExportMessageAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxLastProgressAgeMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxOutputBytesObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxThroughputBytesPerSecObserved");
        AssertContains(diagnosticSessionText, "BuildFlashbackExportSessionMetrics(initialSnapshot, samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "var healthSnapshot = lastSnapshot;");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, \"final-snapshot\");");
        AssertContains(diagnosticSessionText, "exportId > baselineExportId");
        AssertContains(diagnosticSessionText, "baselineExportActive && exportId == baselineExportId");
        AssertContains(diagnosticSessionText, "lastExportId == exportId");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(");
        AssertContains(diagnosticSessionText, "var shouldRunVerification =");
        AssertContains(diagnosticSessionText, "recording verification skipped: scenario does not produce a recording or export artifact");
        AssertContains(diagnosticSessionText, "verificationCommand = \"VerifyFile\"");
        AssertContains(diagnosticSessionText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExport,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-range-export.mp4\"");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExportAudioSwitch,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-range-export-audio-switch.mp4\"");
        AssertContains(diagnosticScenariosText, "FlashbackExportConcurrent,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-concurrent-a.mp4\"");
        AssertContains(diagnosticScenariosText, "FlashbackRotatedExport,");
        AssertContains(diagnosticScenariosText, "FlashbackExportVerificationFileName: \"flashback-rotated-export.mp4\"");
        AssertContains(diagnosticScenariosText, "return exportPath.Length > 0;");
        AssertDoesNotContain(diagnosticScenariosText, "return exportPath.Length > 0 && File.Exists(exportPath);");
        AssertContains(diagnosticSessionText, "expected BufferInactive failure kind");
        AssertContains(diagnosticSessionText, "expected UnavailableDuringRecording failure kind");
        AssertContains(diagnosticSessionText, "flashback rejected export observed status={status} kind={failureKind}");
        AssertContains(diagnosticSessionText, "flashback recording rejected export observed status={status} kind={failureKind}");
        AssertContains(diagnosticSessionText, "Flashback Export:");
        AssertContains(diagnosticSessionText, "failureKindEnd={FormatOptional(result.FlashbackExportFailureKindAtEnd)}");
        AssertContains(diagnosticSessionText, "messageEnd={FormatOptional(result.FlashbackExportMessageAtEnd)}");
        AssertContains(diagnosticSessionText, "forceRotateFallbacksDelta={result.FlashbackExportForceRotateFallbacksDelta}");
        AssertContains(diagnosticSessionText, "lastResultIdEnd={result.LastExportIdAtEnd}");
        AssertContains(diagnosticSessionText, "lastSuccessEnd={FormatOptional(result.LastExportSuccessAtEnd)}");
        AssertContains(diagnosticSessionText, "lastMessageEnd={FormatOptional(result.LastExportMessageAtEnd)}");
        AssertContains(diagnosticSessionText, "pathEnd={FormatOptional(result.FlashbackExportOutputPathAtEnd)}");
        AssertContains(diagnosticSessionText, "maxThroughput={FormatBytes((long)result.FlashbackExportMaxThroughputBytesPerSecObserved)}/s");
        AssertContains(diagnosticSessionText, "BuildFlashbackRecordingMetrics(initialSnapshot, samples)");
        AssertContains(diagnosticSessionText, "seqGapsDelta={result.FlashbackRecordingIntegritySequenceGapsDelta}");
        AssertContains(diagnosticSessionText, "queueDropsDelta={result.FlashbackRecordingIntegrityQueueDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "Flashback video sequence gaps increased delta={metrics.IntegritySequenceGapsDelta}");
        AssertContains(diagnosticSessionText, "Flashback dropped frames increased delta={metrics.IntegrityQueueDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "private static void ValidateCleanupLifecycleRestored(");
        AssertContains(diagnosticSessionText, "cleanup: preview remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: Flashback remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: playback did not return live state={state}");
        AssertContains(diagnosticSessionText, "metrics.MaxPendingCommandsObserved = Math.Max(");
        AssertContains(diagnosticSessionText, "if (maxCommandQueueLatencyMs > metrics.MaxCommandQueueLatencyMsObserved)");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyMsObserved = maxCommandQueueLatencyMs;");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyCommandObserved = GetString(snapshot, \"FlashbackPlaybackMaxCommandQueueLatencyCommand\") ?? string.Empty;");
    }
}
