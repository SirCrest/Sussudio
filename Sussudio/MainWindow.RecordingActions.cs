using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for recording button commands and preview-state logging.
public sealed partial class MainWindow
{
    private RecordingButtonActionController _recordingButtonActionController = null!;

    private void InitializeRecordingButtonActionController()
    {
        _recordingButtonActionController = new RecordingButtonActionController(new RecordingButtonActionControllerContext
        {
            ViewModel = ViewModel,
            GetPreviewActivitySnapshot = () => new RecordingPreviewActivitySnapshot(
                _d3dRenderer != null && PreviewSwapChainPanel.Visibility == Visibility.Visible,
                _previewSource != null && PreviewImage.Visibility == Visibility.Visible,
                NoDevicePlaceholder.Visibility == Visibility.Visible)
        });
    }

    private Task ToggleRecordingFromButtonAsync()
        => _recordingButtonActionController.ToggleRecordingAsync();

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ToggleRecordingFromButtonAsync(), nameof(RecordButton_Click));
    }
}
