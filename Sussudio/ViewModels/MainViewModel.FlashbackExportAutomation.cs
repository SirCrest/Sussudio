using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation-facing Flashback export command.
/// </summary>
public partial class MainViewModel
{
    public async Task<FinalizeResult> ExportFlashbackAutomationAsync(
        double seconds, string outputPath, bool useSelectionRange, bool force, CancellationToken cancellationToken = default)
    {
        var exportId = Interlocked.Increment(ref _flashbackExportOperationId);
        var oldExportCts = _exportCts;
        CancelFlashbackExportCts(oldExportCts);
        _exportCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var exportCts = _exportCts;

        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            if (IsCurrentFlashbackExport(exportId, exportCts))
            {
                IsFlashbackExporting = true;
                FlashbackExportProgress = 0;
            }
        }))
        {
            Logger.Log("FLASHBACK_EXPORT_START_UI_ENQUEUE_FAILED source=automation");
            if (IsCurrentFlashbackExport(exportId, exportCts))
            {
                IsFlashbackExporting = true;
                FlashbackExportProgress = 0;
            }
        }
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
                    Logger.Log($"FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=automation percent={p.Percent:0.###}");
                }
            });

            if (useSelectionRange)
            {
                var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
                return await _sessionCoordinator.ExportFlashbackRangeAsync(
                    playback.InPoint,
                    playback.OutPoint,
                    outputPath,
                    progress,
                    exportCts.Token,
                    playback.InPointFilePts,
                    playback.OutPointFilePts,
                    force);
            }

            return await _sessionCoordinator.ExportFlashbackLastNSecondsAsync(
                seconds, outputPath, progress, exportCts.Token, force);
        }
        finally
        {
            if (!_dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (IsCurrentFlashbackExport(exportId, exportCts))
                    {
                        IsFlashbackExporting = false;
                        FlashbackExportProgress = 0;
                        _exportCts = null;
                    }
                }
                finally
                {
                    DisposeFlashbackExportCtsBestEffort(exportCts, "automation_dispatcher_cleanup");
                }
            }))
            {
                if (IsCurrentFlashbackExport(exportId, exportCts))
                {
                    IsFlashbackExporting = false;
                    FlashbackExportProgress = 0;
                    _exportCts = null;
                }
                DisposeFlashbackExportCtsBestEffort(exportCts, "automation_inline_cleanup");
            }
        }
    }
}
