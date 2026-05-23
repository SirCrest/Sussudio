using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Shared Flashback export lifecycle, progress, and stale-result classification.
/// </summary>
public partial class MainViewModel
{
    private abstract record ExportFlashbackOutcome
    {
        public sealed record Succeeded(FinalizeResult Result) : ExportFlashbackOutcome;
        public sealed record Failed(string ErrorMessage) : ExportFlashbackOutcome;
        public sealed record Stale : ExportFlashbackOutcome;
    }

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

    private async Task<ExportFlashbackOutcome> ExportFlashbackCoreAsync(
        Func<IProgress<ExportProgress>, CancellationToken, Task<FinalizeResult>> exportAction)
    {
        // Export snapshots the flashback backend under CaptureService locks, then runs
        // outside the transition lock so long FFmpeg work does not block lifecycle commands.
        var exportId = Interlocked.Increment(ref _flashbackExportOperationId);
        var oldExportCts = _exportCts;
        CancelFlashbackExportCts(oldExportCts);
        _exportCts = new CancellationTokenSource();
        var exportCts = _exportCts;
        var ct = exportCts.Token;

        IsFlashbackExporting = true;
        FlashbackExportProgress = 0;
        try
        {
            var progress = new Progress<ExportProgress>(p =>
            {
                if (!_dispatcherQueue.TryEnqueue(() =>
                {
                    if (IsCurrentFlashbackExport(exportId, exportCts))
                    {
                        FlashbackExportProgress = p.Percent;
                    }
                }))
                {
                    Logger.Log($"FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=ui percent={p.Percent:0.###}");
                }
            });

            var result = await exportAction(progress, ct);
            return IsCurrentFlashbackExport(exportId, exportCts)
                ? new ExportFlashbackOutcome.Succeeded(result)
                : new ExportFlashbackOutcome.Stale();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            return IsCurrentFlashbackExport(exportId, exportCts)
                ? new ExportFlashbackOutcome.Failed(ex.Message)
                : new ExportFlashbackOutcome.Stale();
        }
        finally
        {
            if (IsCurrentFlashbackExport(exportId, exportCts))
            {
                IsFlashbackExporting = false;
                FlashbackExportProgress = 0;
                _exportCts = null;
                DisposeFlashbackExportCtsBestEffort(exportCts, "ui_current");
            }
            else
            {
                DisposeFlashbackExportCtsBestEffort(exportCts, "ui_stale");
            }
        }
    }
}
