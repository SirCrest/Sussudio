namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private void ApplyRecordingSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.OutputPath is not null)
        {
            OutputPath = loadPlan.OutputPath;
        }

        if (loadPlan.SelectedRecordingFormat is not null)
        {
            SelectedRecordingFormat = loadPlan.SelectedRecordingFormat;
        }

        if (loadPlan.SelectedQuality is not null)
        {
            SelectedQuality = loadPlan.SelectedQuality;
        }

        if (loadPlan.SelectedPreset is not null)
        {
            SelectedPreset = loadPlan.SelectedPreset;
        }

        if (loadPlan.SelectedSplitEncodeMode is not null)
        {
            SelectedSplitEncodeMode = loadPlan.SelectedSplitEncodeMode;
        }

        if (loadPlan.CustomBitrateMbps.HasValue)
        {
            CustomBitrateMbps = loadPlan.CustomBitrateMbps.Value;
        }

        if (loadPlan.IsHdrEnabled.HasValue)
        {
            IsHdrEnabled = loadPlan.IsHdrEnabled.Value;
        }
    }
}
