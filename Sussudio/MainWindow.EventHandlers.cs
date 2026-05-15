using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Sussudio;

// Preview button input handler. Other one-line XAML command bridges live beside
// their owning adapter/controller partials.
public sealed partial class MainWindow
{
    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            if (ViewModel.IsPreviewReinitializing && !ViewModel.IsPreviewing)
            {
                _previewStopRequestedByUser = true;
                ViewModel.CancelPendingPreviewRestart();
                Logger.Log($"PREVIEW_REINIT_CANCEL_REQUESTED attempt={_previewStartupAttemptId ?? "none"}");
                return;
            }

            if (ViewModel.IsPreviewing)
            {
                _previewStopRequestedByUser = true;
                StopPreviewFadeInTimer();
                var audioFadeOutTask = StartPreviewAudioFadeOutAsync();
                var previewFadeOutTask = AnimatePreviewOutAsync();
                await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);
                try
                {
                    await ViewModel.StopPreviewAsync(userInitiated: true);
                }
                finally
                {
                    _isPreviewReinitAnimating = false;
                    Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(PreviewButton_Click)}");
                    ResetPreviewContentTransform();
                }
            }
            else
            {
                _previewStopRequestedByUser = false;
                await ViewModel.StartPreviewAsync(userInitiated: true);
                if (!ViewModel.IsPreviewing)
                {
                    RevealPreviewUnavailablePlaceholder();
                }
            }
        }, nameof(PreviewButton_Click));
    }
}
