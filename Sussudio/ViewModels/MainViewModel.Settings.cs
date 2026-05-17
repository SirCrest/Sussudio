using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Settings initialization and simple persistence reactions.
/// </summary>
public partial class MainViewModel
{
    public Task InitializeAsync()
    {
        LoadSettings();
        StartRecordingCapabilityRefresh();
        return Task.CompletedTask;
    }

    partial void OnOutputPathChanged(string value)
    {
        SaveSettings();
    }

    partial void OnIsStatsVisibleChanged(bool value)
    {
        SaveSettings();
    }
}
