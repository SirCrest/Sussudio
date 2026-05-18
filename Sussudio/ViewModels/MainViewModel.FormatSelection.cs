namespace Sussudio.ViewModels;

/// <summary>
/// Format and frame-rate selection adapter: selected-format assignment and
/// pixel-format option collection mutation route through the capture-mode
/// option rebuild owner.
/// </summary>
public partial class MainViewModel
{
    private void UpdateSelectedFormat()
        => _captureModeOptionRebuildController.UpdateSelectedFormat();

    private void RebuildVideoFormatOptions()
        => _captureModeOptionRebuildController.RebuildVideoFormatOptions();
}
