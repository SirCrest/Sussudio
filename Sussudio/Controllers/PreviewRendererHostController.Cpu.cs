using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class PreviewRendererHostController
{
    private void StartCpuRenderer()
    {
        // Fallback CPU preview path: SoftwareBitmapSource -> Image (unchanged)
        _context.SetupVideoFrameShadow();
        _context.PreviewContentGrid.SizeChanged += _context.PreviewContentGridSizeChangedHandler;
        _context.ViewModel.SetPreviewFrameSink(null);
        _context.ConfigurePreviewStartupSignals(PreviewStartupStrategy.CpuSoftwareBitmap, PreviewStartupSignalFlags.FirstVisual);
        _previewSource = new SoftwareBitmapSource();
        _context.MarkPreviewRendererAttached();
        _context.PreviewImage.Source = _previewSource;
        _context.PreviewImage.Visibility = Visibility.Visible;
        _context.SetGpuPreviewVisibility(Visibility.Collapsed);
        _context.Log($"Preview renderer started (mode=CpuSoftwareBitmap, expectedIntervalMs={_previewMinPresentationIntervalMs:0.###}).");
        _context.Log($"PREVIEW_RENDERER_ATTACHED mode=CpuSoftwareBitmap attempt={_context.GetPreviewStartupAttemptLabel()}");
    }
}
