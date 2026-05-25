using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Services.Preview;
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

internal sealed class PreviewButtonPresentationControllerContext
{
    public required Button PreviewButton { get; init; }
    public required FontIcon PreviewButtonIcon { get; init; }
}

internal sealed class PreviewButtonPresentationController
{
    private const string StopPreviewGlyph = "\uE71A";
    private const string StartPreviewGlyph = "\uE768";

    private readonly PreviewButtonPresentationControllerContext _context;

    public PreviewButtonPresentationController(PreviewButtonPresentationControllerContext context)
    {
        _context = context;
    }

    public void ShowStopPreview()
    {
        _context.PreviewButtonIcon.Glyph = StopPreviewGlyph;
        ToolTipService.SetToolTip(_context.PreviewButton, "Stop Preview");
    }

    public void ShowStartPreview()
    {
        _context.PreviewButtonIcon.Glyph = StartPreviewGlyph;
        ToolTipService.SetToolTip(_context.PreviewButton, "Start Preview");
    }
}

internal sealed class PreviewFadeInControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Func<D3D11PreviewRenderer?> GetRenderer { get; init; }
    public required Func<Task> AnimatePreviewInAsync { get; init; }
    public required Action StartPreviewAudioFadeIn { get; init; }
}

internal sealed class PreviewFadeInController
{
    private const int PreviewFadeInFrameThreshold = 3;

    private readonly PreviewFadeInControllerContext _context;
    private DispatcherQueueTimer? _timer;

    public PreviewFadeInController(PreviewFadeInControllerContext context)
    {
        _context = context;
    }

    public void Schedule()
    {
        Stop();

        var renderer = _context.GetRenderer();
        if (renderer == null)
        {
            _timer = _context.DispatcherQueue.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.IsRepeating = false;
            _timer.Tick += (_, _) =>
            {
                Stop();
                _ = _context.AnimatePreviewInAsync();
                _context.StartPreviewAudioFadeIn();
            };
            _timer.Start();
            return;
        }

        var baselineFrames = renderer.FramesRendered;
        _timer = _context.DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) =>
        {
            var current = _context.GetRenderer();
            if (current == null || current != renderer)
            {
                Stop();
                _ = _context.AnimatePreviewInAsync();
                _context.StartPreviewAudioFadeIn();
                return;
            }

            var rendered = current.FramesRendered - baselineFrames;
            if (rendered >= PreviewFadeInFrameThreshold)
            {
                Stop();
                Logger.Log($"PREVIEW_FADE_IN_READY framesRendered={rendered} baseline={baselineFrames}");
                _ = _context.AnimatePreviewInAsync();
                _context.StartPreviewAudioFadeIn();
            }
        };
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }
}
