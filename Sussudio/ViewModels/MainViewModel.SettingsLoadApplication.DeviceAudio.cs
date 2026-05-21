namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private void ApplyDeviceAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.SelectedDeviceAudioMode is not null)
        {
            SelectedDeviceAudioMode = loadPlan.SelectedDeviceAudioMode;
        }

        if (loadPlan.AnalogAudioGainPercent.HasValue)
        {
            AnalogAudioGainPercent = loadPlan.AnalogAudioGainPercent.Value;
        }
    }
}
