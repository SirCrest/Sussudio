using CommunityToolkit.Mvvm.ComponentModel;

namespace Sussudio.ViewModels;

// HDR capture-selection and runtime presentation state.
public partial class MainViewModel
{
    private const string HdrToggleBlockedWhileRecordingMessage = "Stop recording before switching between HDR and SDR pipelines.";

    [ObservableProperty]
    public partial bool IsHdrEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsHdrAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsTrueHdrPreviewEnabled { get; set; }

    [ObservableProperty]
    public partial string HdrResolutionSupportHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HdrRuntimeState { get; set; } = "Inactive";

    [ObservableProperty]
    public partial string HdrReadinessReason { get; set; } = string.Empty;
}
