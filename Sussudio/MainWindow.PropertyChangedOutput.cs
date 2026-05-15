using Sussudio.ViewModels;

namespace Sussudio;

// Output-path ViewModel property projections.
public sealed partial class MainWindow
{
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
