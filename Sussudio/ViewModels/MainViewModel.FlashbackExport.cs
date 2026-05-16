using System;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Flashback UI export commands and picker flow.
/// </summary>
public partial class MainViewModel
{
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
}
