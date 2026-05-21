using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing preview reinitialization transition adapter.
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
        => _previewRendererHostController.StopRendererForReinitTeardownAsync();

    private void HandlePreviewReinitializingChanged()
        => _previewReinitTransitionController.HandleReinitializingChanged(
            new PreviewReinitCompletionPresentationContext
            {
                IsPreviewReinitializing = ViewModel.IsPreviewReinitializing,
                IsPreviewing = ViewModel.IsPreviewing,
                IsFirstVisualConfirmed = IsPreviewFirstVisualConfirmed,
                AttemptLabel = PreviewStartupAttemptLabel,
                CallerName = nameof(HandleViewModelPropertyChangedAsync),
                UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,
                RevealUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
                StopPreviewStartupOverlay = StopPreviewStartupOverlay,
                ResetPreviewContentTransform = ResetPreviewContentTransform,
                ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,
            });
}
