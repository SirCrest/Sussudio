using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio;

// XAML-facing adapter for recording output-path display and button commands.
public sealed partial class MainWindow
{
    private OutputPathController _outputPathController = null!;

    private void InitializeOutputPathController()
    {
        _outputPathController = new OutputPathController(new OutputPathControllerContext
        {
            OutputPathTextBox = OutputPathTextBox,
            GetWindowHandle = () => _hwnd,
            GetOutputPath = () => ViewModel.OutputPath,
            SetOutputPath = path => ViewModel.OutputPath = path,
            SetStatusText = text => ViewModel.StatusText = text,
            OpenRecordingsFolderAsync = () => OpenRecordingsFolderAsync()
        });
    }

    private void AttachOutputPathDisplay()
        => _outputPathController.AttachDisplay();

    private void UpdateOutputPathDisplay()
        => _outputPathController.UpdateDisplay();

    private Task BrowseOutputPathFromButtonAsync()
        => _outputPathController.BrowseAsync();

    private Task OpenRecordingsFolderFromButtonAsync()
        => _outputPathController.OpenRecordingsFolderIfAvailableAsync();

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => BrowseOutputPathFromButtonAsync(), nameof(BrowseButton_Click));
    }

    private void OpenRecordingsButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => OpenRecordingsFolderFromButtonAsync(), nameof(OpenRecordingsButton_Click));
    }

    private bool TryHandleOutputPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.OutputPath):
                UpdateOutputPathDisplay();
                return true;

            default:
                return false;
        }
    }
}
