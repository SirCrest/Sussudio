using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for delayed preview reveal after first visual.
public sealed partial class MainWindow
{
    private PreviewFadeInController _previewFadeInController = null!;

    private void InitializePreviewFadeInController()
    {
        _previewFadeInController = new PreviewFadeInController(new PreviewFadeInControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            GetRenderer = () => _d3dRenderer,
            AnimatePreviewInAsync = AnimatePreviewInAsync,
            StartPreviewAudioFadeIn = () => StartPreviewAudioFadeIn(),
        });
    }

    private void SchedulePreviewFadeIn()
        => _previewFadeInController.Schedule();

    private void StopPreviewFadeInTimer()
        => _previewFadeInController.Stop();
}
