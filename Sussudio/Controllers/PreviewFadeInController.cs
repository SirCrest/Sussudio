using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

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
