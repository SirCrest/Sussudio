using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Deferred cleanup paths detach live producers first, then wait for encoders or
// exports to drain before disposing native resources and temporary artifacts.
public partial class CaptureService
{
    private void ScheduleDeferredFlashbackBackendCleanup(
        Task sinkCompletionTask,
        FlashbackBackendArtifactCleanupRequest request,
        int attempt = 0)
        => _flashbackBackend.ScheduleDeferredArtifactCleanup(
            sinkCompletionTask,
            request,
            WaitForFlashbackBackendCleanupExportLockAsync,
            ReleaseFlashbackBackendCleanupExportLock,
            attempt);

    private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync(
        FlashbackBackendArtifactCleanupRequest request,
        string mode,
        bool exportOperationLockAlreadyHeld = false)
        => await _flashbackBackend.CleanupArtifactsAfterExportAsync(
                request,
                mode,
                WaitForFlashbackBackendCleanupExportLockAsync,
                ReleaseFlashbackBackendCleanupExportLock,
                exportOperationLockAlreadyHeld)
            .ConfigureAwait(false);

    private Task<bool> WaitForFlashbackBackendCleanupExportLockAsync()
        => _flashbackExportOperationLock.WaitAsync(
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

    private void ReleaseFlashbackBackendCleanupExportLock(string mode)
        => ReleaseSemaphoreBestEffort(_flashbackExportOperationLock, $"flashback_backend_cleanup_{mode}");

}
