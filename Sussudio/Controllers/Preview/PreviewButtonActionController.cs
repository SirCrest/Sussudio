using System;
using System.Threading.Tasks;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class PreviewButtonActionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Action<bool> SetPreviewStopRequestedByUser { get; init; }
    public required Func<string?> GetPreviewStartupAttemptId { get; init; }
    public required Action StopPreviewFadeInTimer { get; init; }
    public required Func<Task> StartPreviewAudioFadeOutAsync { get; init; }
    public required Func<Task> AnimatePreviewOutAsync { get; init; }
    public required Action<string> ClearPreviewReinitAnimation { get; init; }
    public required Action ResetPreviewContentTransform { get; init; }
    public required Action RevealPreviewUnavailablePlaceholder { get; init; }
}

internal sealed class PreviewButtonActionController
{
    private readonly PreviewButtonActionControllerContext _context;

    public PreviewButtonActionController(PreviewButtonActionControllerContext context)
    {
        _context = context;
    }

    public async Task TogglePreviewAsync(string operationName)
    {
        var viewModel = _context.ViewModel;
        if (viewModel.IsPreviewReinitializing && !viewModel.IsPreviewing)
        {
            _context.SetPreviewStopRequestedByUser(true);
            viewModel.CancelPendingPreviewRestart();
            Logger.Log($"PREVIEW_REINIT_CANCEL_REQUESTED attempt={_context.GetPreviewStartupAttemptId() ?? "none"}", operationName);
            return;
        }

        if (viewModel.IsPreviewing)
        {
            _context.SetPreviewStopRequestedByUser(true);
            _context.StopPreviewFadeInTimer();
            var audioFadeOutTask = _context.StartPreviewAudioFadeOutAsync();
            var previewFadeOutTask = _context.AnimatePreviewOutAsync();
            await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);
            try
            {
                await viewModel.StopPreviewAsync(userInitiated: true);
            }
            finally
            {
                _context.ClearPreviewReinitAnimation(operationName);
                _context.ResetPreviewContentTransform();
            }

            return;
        }

        _context.SetPreviewStopRequestedByUser(false);
        await viewModel.StartPreviewAsync(userInitiated: true);
        if (!viewModel.IsPreviewing)
        {
            _context.RevealPreviewUnavailablePlaceholder();
        }
    }
}
