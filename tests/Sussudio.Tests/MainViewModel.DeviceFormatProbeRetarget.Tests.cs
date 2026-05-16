using System.Threading.Tasks;

static partial class Program
{
    private static Task DeviceFormatProbeRetargetApplication_LivesInFocusedPartial()
    {
        var probeEventText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceFormatProbes.cs").Replace("\r\n", "\n");
        var retargetApplicationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceFormatProbeRetarget.cs").Replace("\r\n", "\n");
        var retargetPolicyText = ReadRepoFile("Sussudio/ViewModels/DeviceFormatProbeRetargetPolicy.cs").Replace("\r\n", "\n");

        AssertContains(probeEventText, "private void OnDeviceFormatProbeCompleted");
        AssertContains(probeEventText, "target.SupportedFormats.Clear();");
        AssertContains(probeEventText, "RebuildSelectedDeviceCapabilities(SelectedDevice, resetTelemetryState: false);");
        AssertContains(probeEventText, "TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(probeEventText, "DeviceFormatProbeRetargetDecisionKind.HdrRetarget");
        AssertDoesNotContain(probeEventText, "DeviceFormatProbeRetargetDecisionKind.SdrNv12Retarget");
        AssertDoesNotContain(probeEventText, "GetCaptureRuntimeSnapshot();");

        AssertContains(retargetApplicationText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(retargetApplicationText, "private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(");
        AssertContains(retargetApplicationText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetApplicationText, "SelectedResolution = retargetDecision.TargetResolution;");
        AssertContains(retargetApplicationText, "RebuildFrameRateOptions();");
        AssertContains(retargetApplicationText, "SelectedResolution = previousResolution;");
        AssertContains(retargetApplicationText, "GetCaptureRuntimeSnapshot();");
        AssertDoesNotContain(retargetApplicationText, "target.SupportedFormats.Clear();");
        AssertDoesNotContain(retargetPolicyText, "EnqueueUiOperation(");
        AssertDoesNotContain(retargetPolicyText, "GetCaptureRuntimeSnapshot(");

        return Task.CompletedTask;
    }
}
