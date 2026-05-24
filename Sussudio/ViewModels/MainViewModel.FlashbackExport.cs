using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Flashback UI export commands and picker flow.
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

    public async Task ExportFlashbackAsync()
    {
        if (!EnsureFlashbackActiveForExport("export"))
        {
            return;
        }

        var file = await PickFlashbackExportFileAsync($"Flashback_{DateTime.Now:yyyyMMdd_HHmmss}");
        if (file == null) return;

        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        var inPoint = playback.InPoint;
        var outPoint = playback.OutPoint;

        // UI flow: the file picker already confirmed any overwrite with the user.
        // Pass force=true so the exporter does not refuse the user-chosen path.
        var outcome = await ExportFlashbackCoreAsync(async (progress, ct) =>
            await _sessionCoordinator.ExportFlashbackRangeAsync(
                inPoint,
                outPoint,
                file.Path,
                progress,
                ct,
                playback.InPointFilePts,
                playback.OutPointFilePts,
                force: true));
        switch (outcome)
        {
            case ExportFlashbackOutcome.Stale:
                return;
            case ExportFlashbackOutcome.Failed failed:
                StatusText = $"Export error: {failed.ErrorMessage}";
                break;
            case ExportFlashbackOutcome.Succeeded succeeded:
                StatusText = succeeded.Result.Succeeded
                    ? $"Export complete: {file.Path}"
                    : $"Export failed: {succeeded.Result.StatusMessage}";
                break;
        }
    }

    public async Task SaveFlashbackLast5mAsync()
    {
        if (!EnsureFlashbackActiveForExport("save_last_5m"))
        {
            return;
        }

        var file = await PickFlashbackExportFileAsync($"Flashback_Last5m_{DateTime.Now:yyyyMMdd_HHmmss}");
        if (file == null) return;

        // UI flow: the file picker already confirmed any overwrite with the user.
        var outcome = await ExportFlashbackCoreAsync(async (progress, ct) =>
            await _sessionCoordinator.ExportFlashbackLastNSecondsAsync(300, file.Path, progress, ct, force: true));
        switch (outcome)
        {
            case ExportFlashbackOutcome.Stale:
                return;
            case ExportFlashbackOutcome.Failed failed:
                StatusText = $"Save error: {failed.ErrorMessage}";
                break;
            case ExportFlashbackOutcome.Succeeded succeeded:
                StatusText = succeeded.Result.Succeeded
                    ? $"Saved last 5 minutes: {file.Path}"
                    : $"Save failed: {succeeded.Result.StatusMessage}";
                break;
        }
    }

    private async Task<Windows.Storage.StorageFile?> PickFlashbackExportFileAsync(string suggestedFileName)
    {
        var picker = new Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
        picker.FileTypeChoices.Add("MP4 Video", new[] { ".mp4" });
        picker.SuggestedFileName = suggestedFileName;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowHandle);
        return await picker.PickSaveFileAsync();
    }

    private bool EnsureFlashbackActiveForExport(string operation)
    {
        if (_sessionCoordinator.IsFlashbackActive)
        {
            return true;
        }

        Logger.Log($"FLASHBACK_EXPORT_UI_REJECTED op={operation} reason=inactive");
        StatusText = "Flashback export unavailable: flashback is not active.";
        return false;
    }

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
