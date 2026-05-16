using Sussudio.ViewModels;

namespace Sussudio;

// Shell-level ViewModel property projections for app chrome visibility.
public sealed partial class MainWindow
{
    private bool TryHandleShellPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsStatsVisible):
                HandleStatsVisibleChanged();
                return true;

            case nameof(MainViewModel.IsSettingsVisible):
                ApplySettingsVisibility(ViewModel.IsSettingsVisible);
                return true;

            default:
                return false;
        }
    }

    private void HandleStatsVisibleChanged()
        => _statsOverlayController.SyncStatsVisibility(ViewModel.IsStatsVisible);
}
