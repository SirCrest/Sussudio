using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private bool TryWaitForExportLock(string outputPath, CancellationToken ct, out FinalizeResult cancellationResult)
    {
        try
        {
            if (!_exportLock.Wait(TimeSpan.FromSeconds(ExportLockWaitTimeoutSeconds), ct))
            {
                var message = $"Flashback export lock timed out after {ExportLockWaitTimeoutSeconds}s.";
                Logger.Log($"FLASHBACK_EXPORT_LOCK_WAIT_TIMEOUT timeout_s={ExportLockWaitTimeoutSeconds}");
                cancellationResult = FinalizeResult.Failure(outputPath, message);
                return false;
            }

            cancellationResult = null!;
            return true;
        }
        catch (OperationCanceledException)
        {
            const string message = "Flashback export cancelled.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            cancellationResult = FinalizeResult.Failure(outputPath, message);
            return false;
        }
        catch (ObjectDisposedException)
        {
            cancellationResult = CreateDisposedExportResult(outputPath);
            return false;
        }
    }

    private void ReleaseExportLockBestEffort(string operation)
    {
        try
        {
            _exportLock.Release();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_LOCK_RELEASE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void DisposeExportLockBestEffort()
    {
        try
        {
            _exportLock.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_LOCK_DISPOSE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}
