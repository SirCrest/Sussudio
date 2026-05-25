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
