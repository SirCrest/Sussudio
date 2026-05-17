namespace Sussudio.ViewModels;

/// <summary>
/// User-facing capture-option visibility changes. Settings persistence flows
/// through MainViewModelSettingsPersistenceProjection; source-rate unlock policy
/// stays in MainViewModel.FrameRateSourceFilterPolicy.cs.
/// </summary>
public partial class MainViewModel
{
    partial void OnShowAllCaptureOptionsChanged(bool value)
    {
        if (IsRecording)
        {
            _pendingModeOptionsRefresh = true;
            SaveSettings();
            return;
        }

        _pendingModeOptionsRefresh = false;
        RebuildResolutionOptions();
        SaveSettings();
    }
}
