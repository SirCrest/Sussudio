namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private void ApplyFlashbackSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.FlashbackGpuDecode.HasValue)
        {
            FlashbackGpuDecode = loadPlan.FlashbackGpuDecode.Value;
        }

        if (loadPlan.FlashbackBufferMinutes.HasValue)
        {
            FlashbackBufferMinutes = loadPlan.FlashbackBufferMinutes.Value;
        }
    }
}
