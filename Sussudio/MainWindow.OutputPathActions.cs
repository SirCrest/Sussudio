using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for recording output-path button commands.
public sealed partial class MainWindow
{
    private OutputPathActionController _outputPathActionController = null!;

    private void InitializeOutputPathActionController()
    {
        _outputPathActionController = new OutputPathActionController(new OutputPathActionControllerContext
        {
            GetWindowHandle = () => _hwnd,
            GetOutputPath = () => ViewModel.OutputPath,
            SetOutputPath = path => ViewModel.OutputPath = path,
            SetStatusText = text => ViewModel.StatusText = text,
            OpenRecordingsFolderAsync = () => OpenRecordingsFolderAsync()
        });
    }

    private Task BrowseOutputPathFromButtonAsync()
        => _outputPathActionController.BrowseAsync();

    private Task OpenRecordingsFolderFromButtonAsync()
        => _outputPathActionController.OpenRecordingsFolderIfAvailableAsync();

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => BrowseOutputPathFromButtonAsync(), nameof(BrowseButton_Click));
    }

    private void OpenRecordingsButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => OpenRecordingsFolderFromButtonAsync(), nameof(OpenRecordingsButton_Click));
    }
}
