using System.Threading.Tasks;
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
            ViewModel = ViewModel,
            OpenRecordingsFolderAsync = () => OpenRecordingsFolderAsync()
        });
    }

    private Task BrowseOutputPathFromButtonAsync()
        => _outputPathActionController.BrowseAsync();

    private Task OpenRecordingsFolderFromButtonAsync()
        => _outputPathActionController.OpenRecordingsFolderIfAvailableAsync();
}
