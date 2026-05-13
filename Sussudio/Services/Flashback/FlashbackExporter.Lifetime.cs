using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    public void Dispose()
    {
        CancellationTokenSource? disposeCts;
        lock (_lifetimeSync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            disposeCts = _disposeCts;
        }

        Logger.Log("FLASHBACK_EXPORT_DISPOSE");

        // Signal any running export to cancel. ExportCore/ExportSegmentsCore will exit
        // via OperationCanceledException, clean up native state, and release _exportLock.
        try { disposeCts?.Cancel(); }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_DISPOSE_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }

        // Wait for the export task to release the lock. The CTS is cancelled so
        // the task should exit promptly. Timeout prevents app hang if FFmpeg is stuck.
        var lockAcquired = _exportLock.Wait(TimeSpan.FromSeconds(10));
        if (!lockAcquired)
        {
            Logger.Log("FLASHBACK_EXPORT_DISPOSE: timed out waiting for export lock (10s)");
            Logger.Log("FLASHBACK_EXPORT_DISPOSE_TIMEOUT cleanup_invoked=false");
            Logger.Log("FLASHBACK_EXPORT_DISPOSE_TIMEOUT_OK");
            DisposeLinkedCtsBestEffort(disposeCts, "dispose_timeout");
            ClearDisposeCtsReference(disposeCts);
            GC.SuppressFinalize(this);
            return;
        }

        try
        {
            CleanupNativeState();
        }
        finally
        {
            if (lockAcquired)
            {
                ReleaseExportLockBestEffort("dispose");
            }
        }

        DisposeExportLockBestEffort();
        DisposeLinkedCtsBestEffort(disposeCts, "dispose");
        ClearDisposeCtsReference(disposeCts);
        GC.SuppressFinalize(this);
    }
}
