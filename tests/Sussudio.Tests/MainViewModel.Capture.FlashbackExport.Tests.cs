using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task CaptureService_FlashbackExportsReleaseBackendLeaseBeforeNativeExport()
    {
        var exportOperationsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            .Replace("\r\n", "\n");
        var exportCoreText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = exportOperationsText
            + "\n" + exportCoreText
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.ResourceRelease.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs")
                .Replace("\r\n", "\n");

        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackRangeAsync");
        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        AssertContains(exportOperationsText, "return await ExportFlashbackCoreAsync(");
        AssertDoesNotContain(exportOperationsText, "private async Task<FinalizeResult> ExportFlashbackCoreAsync");
        AssertContains(exportCoreText, "private async Task<FinalizeResult> ExportFlashbackCoreAsync");
        AssertContains(exportCoreText, "bufferManager.PauseEviction();");
        AssertContains(exportCoreText, "ForceRotateForExport");
        AssertContains(exportCoreText, "CreateFlashbackExportThrottleDelayProvider");

        var rangeExport = ExtractTextBetween(
            captureServiceText,
            "internal async Task<FinalizeResult> ExportFlashbackRangeAsync",
            "    internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        AssertContains(rangeExport, "FlashbackExporter? flashbackExporter;");
        AssertContains(rangeExport, "flashbackExporter = bufferManager != null\n                ? _flashbackExporter ??= new FlashbackExporter()\n                : _flashbackExporter;");
        AssertContains(rangeExport, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);\n            exportOperationLockHeld = true;");
        AssertOccursBefore(rangeExport, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);", "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
        AssertContains(rangeExport, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(rangeExport, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);", "return await ExportFlashbackCoreAsync(");
        AssertContains(rangeExport, "snapshotExporter: flashbackExporter,");
        AssertContains(rangeExport, "exportOperationLockAlreadyHeld: true,");

        var lastNExport = ExtractTextBetween(
            exportOperationsText,
            "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync",
            "\n}\n");
        AssertContains(lastNExport, "FlashbackExporter? flashbackExporter;");
        AssertContains(lastNExport, "flashbackExporter = bufferManager != null\n                ? _flashbackExporter ??= new FlashbackExporter()\n                : _flashbackExporter;");
        AssertContains(lastNExport, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);\n            exportOperationLockHeld = true;");
        AssertOccursBefore(lastNExport, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);", "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
        AssertContains(lastNExport, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(lastNExport, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);", "return await ExportFlashbackCoreAsync(");
        AssertContains(lastNExport, "snapshotExporter: flashbackExporter,");
        AssertContains(lastNExport, "exportOperationLockAlreadyHeld: true,");

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
        AssertContains(exportCore, "var forceRotateFallbackUsed = false;");
        AssertContains(exportCore, "forceRotateFallbackUsed = true;");
        AssertContains(exportCore, "live-edge partial fallback: active segment was not closed before timeout; export may omit the newest frames");
        AssertContains(exportCore, "if (forceRotateFallbackUsed && result.Succeeded)\n            {\n                result = FinalizeResult.Success(");
        AssertContains(exportCore, "RecordLastFlashbackExportResult(exportId, result);\n            CompleteFlashbackExportDiagnostics(exportId, result);");

        var backendCleanup = ExtractTextBetween(
            captureServiceText,
            "private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync",
            "    private Task ScheduleDeferredUnifiedVideoCaptureCleanup");
        AssertContains(backendCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(backendCleanup, "bool exportOperationLockAlreadyHeld = false)");
        AssertContains(backendCleanup, "var lockAcquired = exportOperationLockAlreadyHeld;");
        AssertContains(backendCleanup, "if (!exportOperationLockAlreadyHeld)");
        AssertContains(backendCleanup, "request.Reason");
        AssertContains(backendCleanup, "request.FlashbackExporter.Dispose();");
        AssertContains(backendCleanup, "request.BufferManager.PurgeAllSegments();");
        AssertContains(backendCleanup, "FLASHBACK_BACKEND_CLEANUP_LOCK_REUSED");
        AssertContains(backendCleanup, "if (lockAcquired && releaseLockOnExit)");

        var disposeBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposeFlashbackPreviewBackendAsync",
            "    private async Task DisposeFlashbackPreviewBackendCoreAsync");
        AssertContains(disposeBackend, "await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(disposeBackend, "ExportOperationLockAlreadyHeld: true");
        AssertContains(disposeBackend, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");

        var disposeBackendCore = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposeFlashbackPreviewBackendCoreAsync",
            "    /// <summary>\n    /// Cycles the flashback encoder sink after recording stops.");
        AssertContains(disposeBackendCore, "FlashbackPreviewBackendDisposalRequest request)");
        AssertContains(disposeBackendCore, "request.ExportOperationLockAlreadyHeld");
        AssertContains(disposeBackendCore, "request.PurgeSegments ? \"preview_backend_dispose_purge\" : \"preview_backend_dispose\"");
        AssertContains(disposeBackendCore, "\"preview_backend_dispose\",\n                request.ExportOperationLockAlreadyHeld)");

        return Task.CompletedTask;
    }

}
