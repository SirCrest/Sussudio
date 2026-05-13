using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for recording output-path text truncation and tooltip.
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
}
