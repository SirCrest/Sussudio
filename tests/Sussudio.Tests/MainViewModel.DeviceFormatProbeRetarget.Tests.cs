using System.Threading.Tasks;

static partial class Program
{
    private static Task DeviceFormatProbeRetargetApplication_LivesInFocusedPartial()
    {
        var probeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs").Replace("\r\n", "\n");
        var retargetApplierText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.cs").Replace("\r\n", "\n");
        var retargetPolicyText = ReadRepoFile("Sussudio/ViewModels/DeviceFormatProbeRetargetPolicy.cs").Replace("\r\n", "\n");

        AssertContains(probeControllerText, "private sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(probeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(probeControllerText, "target.SupportedFormats.Clear();");
        AssertContains(probeControllerText, "_viewModel.RebuildSelectedDeviceCapabilities(_viewModel.SelectedDevice, resetTelemetryState: false);");
        AssertContains(probeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(probeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(probeControllerText, "private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "private sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(retargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetApplierText, "_viewModel.SelectedResolution = retargetDecision.TargetResolution;");
        AssertContains(retargetApplierText, "_viewModel.RebuildFrameRateOptions();");
        AssertContains(retargetApplierText, "_viewModel.SelectedResolution = previousResolution;");
        AssertContains(retargetApplierText, "_viewModel.GetCaptureRuntimeSnapshot();");
        AssertDoesNotContain(retargetPolicyText, "EnqueueUiOperation(");
        AssertDoesNotContain(retargetPolicyText, "GetCaptureRuntimeSnapshot(");

        return Task.CompletedTask;
    }
}
