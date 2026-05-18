namespace Sussudio.ViewModels;

/// <summary>
/// Resolution option rebuild compatibility adapter.
/// </summary>
public partial class MainViewModel
{
    private void RebuildResolutionOptions()
        => _captureModeOptionRebuildController.RebuildResolutionOptions();
}
