using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Sussudio;

// User-input event handlers. These bridge XAML controls to view-model commands
// and keep animations/fades coordinated around the actual capture transitions.
public sealed partial class MainWindow
{
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => RefreshDevicesFromButtonAsync(), nameof(RefreshButton_Click));
    }
    private void ApplyDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ApplySelectedDeviceFromButtonAsync(), nameof(ApplyDeviceButton_Click));
    }
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
    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ToggleRecordingFromButtonAsync(), nameof(RecordButton_Click));
    }
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => BrowseOutputPathFromButtonAsync(), nameof(BrowseButton_Click));
    }
    private void OpenRecordingsButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => OpenRecordingsFolderFromButtonAsync(), nameof(OpenRecordingsButton_Click));
    }
    private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => CapturePreviewScreenshotAsync(), nameof(ScreenshotButton_Click));
    }
}
