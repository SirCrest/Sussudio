using System;
using System.Threading;

namespace Sussudio.ViewModels;

/// <summary>
/// Shared Flashback export current-operation checks and CTS cleanup.
/// </summary>
public partial class MainViewModel
{
    private bool IsCurrentFlashbackExport(int exportId, CancellationTokenSource exportCts)
        => Volatile.Read(ref _flashbackExportOperationId) == exportId && ReferenceEquals(_exportCts, exportCts);

    private static void CancelFlashbackExportCts(CancellationTokenSource? cts)
    {
        if (cts == null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // A previous automation export may have completed on a background
            // thread while its UI cleanup was still queued.
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_CTS_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static void DisposeFlashbackExportCtsBestEffort(CancellationTokenSource cts, string operation)
    {
        try
        {
            cts.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}
