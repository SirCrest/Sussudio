using System;
using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// Preview reinitialization adapter. PreviewReinitTransitionController owns the
// animation/first-visual transition state while MainWindow keeps renderer-stop
// and XAML presentation side effects in the original order.
public sealed partial class MainWindow
{
    private PreviewReinitTransitionController _previewReinitTransitionController = null!;

    private void InitializePreviewReinitTransitionController()
        => _previewReinitTransitionController = new PreviewReinitTransitionController();

    private bool IsPreviewReinitAnimating
        => _previewReinitTransitionController.IsAnimating;

    private async Task ViewModel_PreviewReinitRequested(string reason)
    {
        if (!ViewModel.IsPreviewing)
        {
            return;
        }

        _previewReinitTransitionController.BeginAnimateOut(reason, nameof(ViewModel_PreviewReinitRequested));
        await AnimatePreviewOutAsync();
    }

    private Task ViewModel_PreviewRendererStopRequested()
    {
        // Stop the render thread before the capture pipeline teardown. This ensures
        // no native D3D calls (VideoProcessorBlt/Present) are in flight when
        // UnifiedVideoCapture disposes the shared D3D11 device and DXGI manager.
        //
        // IMPORTANT: this only drains and detaches the active renderer. The later
        // attach step may replace the SwapChainPanel surface for HDR/SDR or mode
        // changes because WinUI can keep native DXGI state behind a panel even
        // after SetSwapChain(null). Replacing the surface happens after capture
        // teardown so the old renderer is no longer receiving frames.
        var renderer = _previewRendererHostController.Renderer;
        if (renderer != null)
        {
            Logger.Log("PREVIEW_REINIT_RENDERER_STOP: stopping render thread before pipeline teardown");
            try
            {
                DisposeD3DPreviewRendererForReinit();
            }
            catch (TimeoutException ex)
            {
                // Render thread did not exit before its stop timeout. The renderer's
                // stop path has already logged details and the fresh attach path will
                // replace the panel surface if needed. Swallow the exception so reinit
                // can continue rather than crashing the UI thread mid-resolution-change.
                Logger.Log($"PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; continuing reinit with orphan render thread expected to exit shortly.");
            }
        }

        return Task.CompletedTask;
    }

    private void HandlePreviewReinitializingChanged()
    {
        UpdateDeviceApplyButtonState();
        switch (_previewReinitTransitionController.GetCompletionPresentation(
            ViewModel.IsPreviewReinitializing,
            ViewModel.IsPreviewing,
            IsPreviewFirstVisualConfirmed))
        {
            case PreviewReinitCompletionPresentation.RevealUnavailablePlaceholder:
                _previewReinitTransitionController.Clear(nameof(HandleViewModelPropertyChangedAsync), logWhenInactive: false);
                RevealPreviewUnavailablePlaceholder();
                break;

            case PreviewReinitCompletionPresentation.ResetConfirmedVisual:
                _previewReinitTransitionController.ResetConfirmedVisualTransition(
                    PreviewStartupAttemptLabel,
                    "reinit-stop-failed",
                    nameof(HandleViewModelPropertyChangedAsync));
                StopPreviewStartupOverlay();
                ResetPreviewContentTransform();
                break;

            case PreviewReinitCompletionPresentation.ShowStartPreviewButton:
                ShowStartPreviewButtonPresentation();
                break;
        }
    }
}
