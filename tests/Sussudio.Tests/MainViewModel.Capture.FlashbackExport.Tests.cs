using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    internal static Task CaptureService_FlashbackExportsReleaseBackendLeaseBeforeNativeExport()
    {
        var exportOperationsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            .Replace("\r\n", "\n");
        var exportBackendSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportBackendSnapshot.cs")
            .Replace("\r\n", "\n");
        var exportCoreText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");
        var exportRequestPreparationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportRequestPreparation.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = exportOperationsText
            + "\n" + exportBackendSnapshotText
            + "\n" + exportCoreText
            + "\n" + exportRequestPreparationText
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.ResourceRelease.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.VideoPipelineLifecycle.cs")
                .Replace("\r\n", "\n");
        var backendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.PreviewDisposal.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackRangeAsync");
        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        AssertDoesNotContain(exportOperationsText, "private readonly record struct FlashbackExportBackendSnapshot(");
        AssertDoesNotContain(exportOperationsText, "private async Task<FlashbackExportBackendSnapshotResult> SnapshotFlashbackExportBackendAsync(");
        AssertContains(exportBackendSnapshotText, "private readonly record struct FlashbackExportBackendSnapshot(");
        AssertContains(exportBackendSnapshotText, "private async Task<FlashbackExportBackendSnapshotResult> SnapshotFlashbackExportBackendAsync(");
        AssertContains(exportOperationsText, "return await ExportFlashbackCoreAsync(");
        AssertDoesNotContain(exportOperationsText, "private async Task<FinalizeResult> ExportFlashbackCoreAsync");
        AssertContains(exportCoreText, "private async Task<FinalizeResult> ExportFlashbackCoreAsync");
        AssertContains(exportCoreText, "bufferManager.PauseEviction();");
        AssertContains(exportRequestPreparationText, "private FlashbackExportPreparationResult PrepareFlashbackExportRequest(");
        AssertContains(exportRequestPreparationText, "ForceRotateForExport");
        AssertContains(exportRequestPreparationText, "CreateFlashbackExportThrottleDelayProvider");

        var rangeExport = ExtractMemberCode(exportOperationsText, "ExportFlashbackRangeAsync");
        AssertContains(rangeExport, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(rangeExport, "operationName: \"range\",");
        AssertContains(rangeExport, "sessionReleaseOperation: \"flashback_export_snapshot_session\",");
        AssertContains(rangeExport, "var snapshot = snapshotResult.Snapshot;");
        AssertContains(rangeExport, "snapshotSink: snapshot.Sink,");
        AssertContains(rangeExport, "snapshotBufferManager: snapshot.BufferManager,");
        AssertContains(rangeExport, "snapshotExporter: snapshot.Exporter,");
        AssertContains(rangeExport, "exportOperationLockAlreadyHeld: true,");

        var lastNExport = ExtractMemberCode(exportOperationsText, "ExportFlashbackLastNSecondsAsync");
        AssertContains(lastNExport, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(lastNExport, "operationName: \"last_n\",");
        AssertContains(lastNExport, "sessionReleaseOperation: \"flashback_export_last_n_snapshot_session\",");
        AssertContains(lastNExport, "var snapshot = snapshotResult.Snapshot;");
        AssertContains(lastNExport, "snapshotSink: snapshot.Sink,");
        AssertContains(lastNExport, "snapshotBufferManager: snapshot.BufferManager,");
        AssertContains(lastNExport, "snapshotExporter: snapshot.Exporter,");
        AssertContains(lastNExport, "exportOperationLockAlreadyHeld: true,");

        var backendSnapshot = ExtractMemberCode(exportBackendSnapshotText, "SnapshotFlashbackExportBackendAsync");
        AssertContains(backendSnapshot, "var bufferManager = _flashbackBufferManager;");
        AssertContains(backendSnapshot, "var flashbackSink = _flashbackSink;");
        AssertContains(backendSnapshot, "var flashbackExporter = bufferManager != null\n                ? _flashbackExporter ??= new FlashbackExporter()\n                : _flashbackExporter;");
        AssertContains(backendSnapshot, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);\n            exportOperationLockHeld = true;");
        AssertOccursBefore(backendSnapshot, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);", "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
        AssertContains(backendSnapshot, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertContains(backendSnapshot, "new FlashbackExportBackendSnapshot(bufferManager, flashbackSink, flashbackExporter)");

        var exportCore = ExtractTextBetween(
            exportCoreText,
            "    private async Task<FinalizeResult> ExportFlashbackCoreAsync",
            "\n}\n");
        AssertContains(exportCore, "FlashbackExporter? snapshotExporter = null,");
        AssertContains(exportCore, "bool exportOperationLockAlreadyHeld = false,");
        AssertContains(exportCore, "var exportOperationLockHeld = exportOperationLockAlreadyHeld;");
        AssertContains(exportCore, "if (!exportOperationLockAlreadyHeld)");
        AssertContains(exportCore, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");
        AssertOccursBefore(exportCore, "if (bufferManager == null)", "var exporter = snapshotExporter;");
        AssertContains(exportCore, "var exporter = snapshotExporter;\n            if (exporter == null)\n            {\n                exporter = _flashbackExporter ??= new FlashbackExporter();\n            }");
        AssertContains(exportCore, "var preparedExport = PrepareFlashbackExportRequest(");
        AssertContains(exportCore, "if (preparedExport.FailureResult is { } preparationFailure)");
        AssertContains(exportRequestPreparationText, "var forceRotateFallbackUsed = false;");
        AssertContains(exportRequestPreparationText, "forceRotateFallbackUsed = true;");
        AssertContains(exportCore, "live-edge partial fallback: active segment was not closed before timeout; export may omit the newest frames");
        AssertContains(exportCore, "if (preparedExport.ForceRotateFallbackUsed && result.Succeeded)\n            {\n                result = FinalizeResult.Success(");
        AssertContains(exportCore, "RecordLastFlashbackExportResult(exportId, result);\n            CompleteFlashbackExportDiagnostics(exportId, result);");

        var backendCleanup = ExtractTextBetween(
            backendResourcesText,
            "public async Task<bool> CleanupArtifactsAfterExportAsync",
            "    public async Task<FlashbackPlaybackController> StartPreviewBackendAsync");
        AssertContains(backendCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(backendCleanup, "bool exportOperationLockAlreadyHeld = false)");
        AssertContains(backendCleanup, "var lockAcquired = exportOperationLockAlreadyHeld;");
        AssertContains(backendCleanup, "if (!exportOperationLockAlreadyHeld)");
        AssertContains(backendCleanup, "request.Reason");
        AssertContains(backendCleanup, "request.FlashbackExporter.Dispose();");
        AssertContains(backendCleanup, "request.BufferManager.PurgeAllSegments();");
        AssertContains(backendCleanup, "FLASHBACK_BACKEND_CLEANUP_LOCK_REUSED");
        AssertContains(backendCleanup, "if (lockAcquired && releaseLockOnExit)");
        AssertContains(backendCleanup, "releaseExportOperationLock(mode);");

        var cleanupBridge = ExtractTextBetween(
            captureServiceText,
            "private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync",
            "\n}");
        AssertContains(cleanupBridge, "_flashbackBackend.CleanupArtifactsAfterExportAsync(");
        AssertContains(cleanupBridge, "WaitForFlashbackBackendCleanupExportLockAsync");
        AssertContains(cleanupBridge, "ReleaseFlashbackBackendCleanupExportLock");

        var disposeBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposeFlashbackPreviewBackendAsync",
            "    private async Task DisposeFlashbackPreviewBackendCoreAsync");
        AssertContains(disposeBackend, "await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(disposeBackend, "exportOperationLockAlreadyHeld: true");
        AssertContains(disposeBackend, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");

        var disposeBackendCore = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposeFlashbackPreviewBackendCoreAsync",
            "    private async Task CycleFlashbackBufferAsync");
        AssertContains(disposeBackendCore, "FlashbackPreviewBackendDisposalRequest request)");
        AssertContains(disposeBackendCore, "_flashbackBackend.DisposePreviewBackendAsync(request)");

        var disposeBackendResources = ExtractTextBetween(
            backendResourcesText,
            "public async Task DisposePreviewBackendAsync",
            "    public void ScheduleDeferredArtifactCleanup");
        AssertContains(disposeBackendResources, "request.ExportOperationLockAlreadyHeld");
        AssertContains(disposeBackendResources, "request.PurgeSegments ? \"preview_backend_dispose_purge\" : \"preview_backend_dispose\"");
        AssertContains(disposeBackendResources, "\"preview_backend_dispose\",\n                request.AcquireExportOperationLockAsync,\n                request.ReleaseExportOperationLock,\n                request.ExportOperationLockAlreadyHeld)");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelFlashbackExport_RoutesThroughCoordinatorAndOwnsCtsLifecycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var disposalText = viewModelFiles["MainViewModel.Disposal.cs"];
        var flashbackExportText = viewModelFiles["MainViewModel.FlashbackExport.cs"];
        var flashbackExportOperationText = viewModelFiles["MainViewModel.FlashbackExportOperation.cs"];
        var flashbackExportAutomationText = viewModelFiles["MainViewModel.FlashbackExportAutomation.cs"];
        var rawDisposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Disposal.cs")
            .Replace("\r\n", "\n");
        var disposalControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDisposalController.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportOperationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportOperation.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportAutomation.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();

        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "_sessionCoordinator.ExportFlashbackRangeAsync(");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.InPointFilePts");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.OutPointFilePts");
        AssertContains(coordinatorText, "TimeSpan? InPointFilePts,");
        AssertContains(coordinatorText, "TimeSpan? OutPointFilePts,");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"export\")");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"save_last_5m\")");
        AssertContains(rawFlashbackExportText, "FLASHBACK_EXPORT_UI_REJECTED op={operation} reason=inactive");
        AssertContains(rawFlashbackExportText, "Flashback export unavailable: flashback is not active.");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "case ExportFlashbackOutcome.Stale:");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "case ExportFlashbackOutcome.Stale:");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackExportOperationId;");
        AssertContains(disposalText, "Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(disposalText, "var exportCts = Interlocked.Exchange(ref _exportCts, null);");
        AssertContains(disposalText, "CancelFlashbackExportCts(exportCts);");
        AssertContains(rawDisposalText, "private void CancelActiveFlashbackExportForDispose()");
        AssertContains(disposalControllerText, "_context.CancelActiveFlashbackExport();");
        AssertContains(disposalControllerText, "_context.StopRuntimeForDispose();");
        AssertOccursBefore(disposalControllerText, "_context.CancelActiveFlashbackExport();", "_context.StopRuntimeForDispose();");
        AssertOccursBefore(disposalControllerText, "_context.StopRuntimeForDispose();", "var stepTimeoutMs = EnvironmentHelpers.GetIntFromEnv(");
        AssertContains(rawDisposalText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"viewmodel_dispose\");");
        AssertContains(flashbackExportOperationText, "private abstract record ExportFlashbackOutcome");
        AssertContains(flashbackExportOperationText, "private async Task<ExportFlashbackOutcome> ExportFlashbackCoreAsync");
        AssertContains(flashbackExportOperationText, "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(flashbackExportOperationText, "CancelFlashbackExportCts(oldExportCts);");
        AssertContains(flashbackExportOperationText, "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertContains(flashbackExportOperationText, "_exportCts = null;");
        AssertContains(flashbackExportOperationText, "ReferenceEquals(_exportCts, exportCts)");
        AssertContains(flashbackExportOperationText, "private static void CancelFlashbackExportCts(CancellationTokenSource? cts)");
        AssertContains(flashbackExportOperationText, "catch (ObjectDisposedException)");
        AssertContains(rawFlashbackExportOperationText, "FLASHBACK_EXPORT_CTS_CANCEL_WARN");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "CancelFlashbackExportCts(oldExportCts);");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "FlashbackExportProgress = p.Percent;");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "exportCts.Token");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "_exportCts = null;");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "if (!_dispatcherQueue.TryEnqueue(");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "finally");
        AssertContains(rawFlashbackExportAutomationText, "IsFlashbackExporting = false;\n                    FlashbackExportProgress = 0;\n                    _exportCts = null;");
        AssertContains(flashbackExportOperationText, "private static void DisposeFlashbackExportCtsBestEffort(CancellationTokenSource cts, string operation)");
        AssertContains(rawFlashbackExportOperationText, "FLASHBACK_EXPORT_CTS_DISPOSE_WARN");
        AssertContains(rawFlashbackExportOperationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_current\");");
        AssertContains(rawFlashbackExportOperationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_stale\");");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_dispatcher_cleanup\");");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_inline_cleanup\");");
        AssertDoesNotContain(
            flashbackExportText + "\n" + flashbackExportOperationText + "\n" + flashbackExportAutomationText,
            "exportCts.Dispose();");

        return Task.CompletedTask;
    }

}
