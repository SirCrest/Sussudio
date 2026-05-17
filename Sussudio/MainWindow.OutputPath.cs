using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio;

// XAML-facing adapter for recording output-path display and button commands.
public sealed partial class MainWindow
{
    private OutputPathActionController _outputPathActionController = null!;
    private OutputPathDisplayController _outputPathDisplayController = null!;

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

    private void InitializeOutputPathDisplayController()
    {
        _outputPathDisplayController = new OutputPathDisplayController(new OutputPathDisplayControllerContext
        {
            OutputPathTextBox = OutputPathTextBox,
            GetOutputPath = () => ViewModel.OutputPath,
        });
    }

    private void AttachOutputPathDisplay()
        => _outputPathDisplayController.Attach();

    private void UpdateOutputPathDisplay()
        => _outputPathDisplayController.Update();

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
