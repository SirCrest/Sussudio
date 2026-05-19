using System;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed class MainViewModelDeviceFormatProbeRetargetApplierContext
    {
        public required Func<bool> IsHdrEnabled { get; init; }
        public required Func<string?> GetSelectedResolution { get; init; }
        public required Action<string?> SetSelectedResolution { get; init; }
        public required Func<double> GetSelectedFrameRate { get; init; }
        public required Action<double> SetSelectedFrameRate { get; init; }
        public required Func<MediaFormat?> GetSelectedFormat { get; init; }
        public required Func<string, bool> AvailableResolutionsContains { get; init; }
        public required Action<bool> SetIsRebuildingModeOptions { get; init; }
        public required Action<bool> SetIsApplyingAutomaticResolutionSelection { get; init; }
        public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
        public required Action RebuildFrameRateOptions { get; init; }
        public required Func<string, Task> ReinitializeDeviceAsync { get; init; }
        public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }
        public required Func<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshot { get; init; }
        public required Action UpdateSelectedFormat { get; init; }
        public required Action UpdateTargetSummary { get; init; }
    }
}
