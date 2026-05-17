using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio;

// XAML-facing adapter for recording output-path text truncation, tooltip, and
// output-path property-change projection.
public sealed partial class MainWindow
{
    private OutputPathDisplayController _outputPathDisplayController = null!;

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
