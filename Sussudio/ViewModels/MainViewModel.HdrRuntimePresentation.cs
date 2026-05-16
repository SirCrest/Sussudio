using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// HDR runtime ViewModel property projection from capture runtime snapshots.
/// </summary>
public partial class MainViewModel
{
    private void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        HdrRuntimeState = runtime.HdrRuntimeState;
        HdrReadinessReason = runtime.HdrReadinessReason;
        UpdateTargetSummary();
    }
}
