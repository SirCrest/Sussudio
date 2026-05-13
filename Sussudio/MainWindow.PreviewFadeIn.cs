using System;
using Microsoft.UI.Dispatching;

namespace Sussudio;

// Delays preview reveal until a few rendered frames have landed, so startup
// does not expose the first black or stale source-reader frame.
public sealed partial class MainWindow
{
    private const int PreviewFadeInFrameThreshold = 3;

    private DispatcherQueueTimer? _previewFadeInTimer;

    private void SchedulePreviewFadeIn()
    {
        StopPreviewFadeInTimer();

        var renderer = _d3dRenderer;
        if (renderer == null)
        {
            // CPU fallback path: no frame counter, just animate in after a short delay.
            _previewFadeInTimer = _dispatcherQueue.CreateTimer();
            _previewFadeInTimer.Interval = TimeSpan.FromMilliseconds(50);
            _previewFadeInTimer.IsRepeating = false;
            _previewFadeInTimer.Tick += (_, _) =>
            {
                StopPreviewFadeInTimer();
                _ = AnimatePreviewInAsync();
                StartPreviewAudioFadeIn();
            };
            _previewFadeInTimer.Start();
            return;
        }

        // Wait until the renderer has rendered enough frames for the signal to stabilize.
        var baselineFrames = renderer.FramesRendered;
        _previewFadeInTimer = _dispatcherQueue.CreateTimer();
        _previewFadeInTimer.Interval = TimeSpan.FromMilliseconds(16);
        _previewFadeInTimer.IsRepeating = true;
        _previewFadeInTimer.Tick += (_, _) =>
        {
            var current = _d3dRenderer;
            if (current == null || current != renderer)
            {
                StopPreviewFadeInTimer();
                _ = AnimatePreviewInAsync();
                StartPreviewAudioFadeIn();
                return;
            }

            var rendered = current.FramesRendered - baselineFrames;
            if (rendered >= PreviewFadeInFrameThreshold)
            {
                StopPreviewFadeInTimer();
                Logger.Log($"PREVIEW_FADE_IN_READY framesRendered={rendered} baseline={baselineFrames}");
                _ = AnimatePreviewInAsync();
                StartPreviewAudioFadeIn();
            }
        };
        _previewFadeInTimer.Start();
    }

    private void StopPreviewFadeInTimer()
    {
        _previewFadeInTimer?.Stop();
        _previewFadeInTimer = null;
    }
}
