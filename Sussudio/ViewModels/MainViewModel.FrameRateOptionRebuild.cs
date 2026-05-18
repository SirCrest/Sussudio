namespace Sussudio.ViewModels;

/// <summary>
/// Frame-rate option rebuild compatibility adapter.
/// </summary>
public partial class MainViewModel
{
    private void RebuildFrameRateOptions()
        => _captureModeOptionRebuildController.RebuildFrameRateOptions();
}
