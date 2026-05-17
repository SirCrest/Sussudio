using System.Threading.Tasks;

static partial class Program
{
    private static Task DeviceFormatProbeRetargetApplication_LivesInFocusedPartial()
    {
        var probeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs").Replace("\r\n", "\n");
        var retargetPolicyText = ReadRepoFile("Sussudio/ViewModels/DeviceFormatProbeRetargetPolicy.cs").Replace("\r\n", "\n");

        AssertContains(probeControllerText, "private sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(probeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(probeControllerText, "target.SupportedFormats.Clear();");
        AssertContains(probeControllerText, "_viewModel.RebuildSelectedDeviceCapabilities(_viewModel.SelectedDevice, resetTelemetryState: false);");
        AssertContains(probeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(probeControllerText, "private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(");
        AssertContains(probeControllerText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertContains(probeControllerText, "_viewModel.SelectedResolution = retargetDecision.TargetResolution;");
        AssertContains(probeControllerText, "_viewModel.RebuildFrameRateOptions();");
        AssertContains(probeControllerText, "_viewModel.SelectedResolution = previousResolution;");
        AssertContains(probeControllerText, "_viewModel.GetCaptureRuntimeSnapshot();");
        AssertDoesNotContain(retargetPolicyText, "EnqueueUiOperation(");
        AssertDoesNotContain(retargetPolicyText, "GetCaptureRuntimeSnapshot(");

        return Task.CompletedTask;
    }
}
