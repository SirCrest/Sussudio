using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for preview button commands.
public sealed partial class MainWindow
{
    private PreviewButtonActionController _previewButtonActionController = null!;

    private void InitializePreviewButtonActionController()
    {
        _previewButtonActionController = new PreviewButtonActionController(new PreviewButtonActionControllerContext
        {
            ViewModel = ViewModel,
            SetPreviewStopRequestedByUser = value => _previewStopRequestedByUser = value,
            GetPreviewStartupAttemptId = () => PreviewStartupAttemptId,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            StartPreviewAudioFadeOutAsync = () => StartPreviewAudioFadeOutAsync(),
            AnimatePreviewOutAsync = AnimatePreviewOutAsync,
            ClearPreviewReinitAnimation = operationName =>
            {
                _isPreviewReinitAnimating = false;
                Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={operationName}", operationName);
            },
            ResetPreviewContentTransform = ResetPreviewContentTransform,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
        });
    }

    private Task TogglePreviewFromButtonAsync()
        => _previewButtonActionController.TogglePreviewAsync(nameof(PreviewButton_Click));

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click));
    }
}
