using Sussudio.ViewModels;

namespace Sussudio;

// Live source-signal ViewModel property projections.
public sealed partial class MainWindow
{
    private bool TryHandleLiveSignalPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.LiveResolution):
            case nameof(MainViewModel.LiveFrameRate):
            case nameof(MainViewModel.LivePixelFormat):
                UpdateLiveSignalInfoVisibility();
                return true;

            default:
                return false;
        }
    }
}
