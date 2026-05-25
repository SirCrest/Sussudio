using System.Threading.Tasks;

static partial class Program
{
    internal static Task DeviceFormatProbeRetargetApplication_LivesInFocusedPartial()
    {
        var probeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs").Replace("\r\n", "\n");
        var retargetApplierText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.cs").Replace("\r\n", "\n");
        var retargetPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertContains(probeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(probeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(probeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(probeControllerText, "_viewModel.");
        AssertContains(probeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(probeControllerText, "target.SupportedFormats.Clear();");
        AssertContains(probeControllerText, "_context.RebuildSelectedDeviceCapabilities(selectedDevice, false);");
        AssertContains(probeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(probeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(probeControllerText, "private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(retargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(retargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(retargetApplierText, "_viewModel.");
        AssertContains(retargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetApplierText, "_context.SetSelectedResolution(retargetDecision.TargetResolution);");
        AssertContains(retargetApplierText, "_context.RebuildFrameRateOptions();");
        AssertContains(retargetApplierText, "_context.SetSelectedResolution(previousResolution);");
        AssertContains(retargetApplierText, "_context.GetCaptureRuntimeSnapshot();");
        AssertDoesNotContain(retargetPolicyText, "EnqueueUiOperation(");
        AssertDoesNotContain(retargetPolicyText, "GetCaptureRuntimeSnapshot(");

        return Task.CompletedTask;
    }
}
