namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private void ApplySettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        ApplyRecordingSettingsLoadPlan(loadPlan);
        ApplyAudioSettingsLoadPlan(loadPlan);
        ApplyUiSettingsLoadPlan(loadPlan);
        ApplyDeviceAudioSettingsLoadPlan(loadPlan);
        ApplyFlashbackSettingsLoadPlan(loadPlan);
        StageDeferredDeviceSettingsLoadPlan(loadPlan);
    }
}
