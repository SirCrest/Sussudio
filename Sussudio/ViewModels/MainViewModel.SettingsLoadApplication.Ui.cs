namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private void ApplyUiSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.ShowAllCaptureOptions.HasValue)
        {
            ShowAllCaptureOptions = loadPlan.ShowAllCaptureOptions.Value;
        }

        if (loadPlan.IsStatsVisible.HasValue)
        {
            IsStatsVisible = loadPlan.IsStatsVisible.Value;
        }
    }
}
