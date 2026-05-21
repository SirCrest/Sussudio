namespace Sussudio.ViewModels;

/// <summary>
/// Stable recording capability facade methods over the controller-owned refresh logic.
/// </summary>
public partial class MainViewModel
{
    private void StartRecordingCapabilityRefresh()
        => _recordingCapabilityController.Start();

    private void RebuildRecordingFormatOptions()
        => _recordingCapabilityController.RebuildRecordingFormatOptions();
}
