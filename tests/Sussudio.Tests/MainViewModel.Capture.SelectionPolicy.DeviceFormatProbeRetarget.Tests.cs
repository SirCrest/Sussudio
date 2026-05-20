using System.Threading.Tasks;

static partial class Program
{
    internal static Task DeviceFormatProbeRetargetPolicy_LivesInFocusedHelper()
    {
        var probeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs").Replace("\r\n", "\n");
        var probeControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.Context.cs").Replace("\r\n", "\n");
        var retargetApplierText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.cs").Replace("\r\n", "\n");
        var retargetApplierContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.Context.cs").Replace("\r\n", "\n");
        var retargetPolicyText = ReadRepoFile("Sussudio/ViewModels/DeviceFormatProbeRetargetPolicy.cs").Replace("\r\n", "\n");

        AssertContains(probeControllerText, "private sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(probeControllerContextText, "private sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(probeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(probeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(probeControllerText, "_viewModel.");
        AssertContains(probeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(probeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertContains(probeControllerText, "_context.RebuildSelectedDeviceCapabilities(selectedDevice, false);");
        AssertContains(probeControllerText, "FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
        AssertDoesNotContain(probeControllerText, "var nv12Candidates = target.SupportedFormats");
        AssertDoesNotContain(probeControllerText, "ShouldPreserveMjpegHighFrameRateMode(_viewModel.SelectedFormat)");
        AssertDoesNotContain(probeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(probeControllerText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertDoesNotContain(probeControllerText, "RebuildFrameRateOptions();");
        AssertDoesNotContain(probeControllerText, "EnqueueUiOperation(");
        AssertContains(retargetApplierText, "private sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(retargetApplierContextText, "private sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(retargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(retargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(retargetApplierText, "_viewModel.");
        AssertContains(retargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetApplierText, "RebuildFrameRateOptions();");
        AssertContains(retargetApplierText, "EnqueueUiOperation(");
        AssertContains(retargetPolicyText, "internal static class DeviceFormatProbeRetargetPolicy");
        AssertContains(retargetPolicyText, "internal static DeviceFormatProbeRetargetDecision Decide(DeviceFormatProbeRetargetRequest request)");
        AssertContains(retargetPolicyText, "internal sealed record DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetPolicyText, "internal sealed record DeviceFormatProbeRetargetDecision(");
        AssertContains(retargetPolicyText, "CaptureSettings.IsMjpegHighFrameRateMode(");
        AssertContains(retargetPolicyText, "\"format probe (HDR retarget)\"");
        AssertContains(retargetPolicyText, "\"format probe (SDR nv12 retarget)\"");
        AssertContains(retargetPolicyText, "\"format probe (session mismatch)\"");
        AssertDoesNotContain(retargetPolicyText, "Logger.Log(");
        AssertDoesNotContain(retargetPolicyText, "ReinitializeDeviceAsync(");
        AssertDoesNotContain(retargetPolicyText, "SelectedResolution =");
        AssertDoesNotContain(retargetPolicyText, "RebuildFrameRateOptions(");

        return Task.CompletedTask;
    }

    internal static Task DeviceFormatProbeRetargetPolicy_PreservesRetargetDecisionBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");

        var hdrDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: true,
            modeChanged: true,
            previousResolution: "3840x2160",
            previousFrameRate: 120,
            selectedResolution: "1920x1080",
            selectedFrameRate: 120,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1920, 1080, 120, "P010", isHdr: true),
            supportedFormats: CreateMediaFormatList(mediaFormatType),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("HdrRetarget", GetEnumName(hdrDecision, "Kind"), "HDR retarget decision");
        AssertEqual("format probe (HDR retarget)", GetStringProperty(hdrDecision, "ReinitializeReason"), "HDR retarget reason");
        AssertEqual("format probe hdr retarget", GetStringProperty(hdrDecision, "UiOperationName"), "HDR retarget UI operation");

        var mjpgHfrDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: false,
            modeChanged: false,
            previousResolution: "3840x2160",
            previousFrameRate: 120,
            selectedResolution: "3840x2160",
            selectedFrameRate: 120,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "MJPG", isHdr: false),
            supportedFormats: CreateMediaFormatList(
                mediaFormatType,
                CreateTestMediaFormat(mediaFormatType, 1920, 1080, 120, "NV12", isHdr: false)),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("PreserveMjpegHighFrameRate", GetEnumName(mjpgHfrDecision, "Kind"), "MJPG HFR preserve decision");

        var sdrNv12Decision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: false,
            modeChanged: false,
            previousResolution: "1280x720",
            previousFrameRate: 60,
            selectedResolution: "1280x720",
            selectedFrameRate: 60,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1280, 720, 60, "MJPG", isHdr: false),
            supportedFormats: CreateMediaFormatList(
                mediaFormatType,
                CreateTestMediaFormat(mediaFormatType, 3840, 2160, 30, "NV12", isHdr: false),
                CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false),
                CreateTestMediaFormat(mediaFormatType, 1280, 720, 60, "MJPG", isHdr: false)),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("SdrNv12Retarget", GetEnumName(sdrNv12Decision, "Kind"), "SDR NV12 retarget decision");
        AssertEqual("1920x1080", GetStringProperty(sdrNv12Decision, "TargetResolution"), "SDR NV12 target resolution");
        AssertEqual(60d, sdrNv12Decision.GetType().GetProperty("TargetFrameRate")!.GetValue(sdrNv12Decision), "SDR NV12 target frame rate");
        AssertEqual("format probe (SDR nv12 retarget)", GetStringProperty(sdrNv12Decision, "ReinitializeReason"), "SDR NV12 reason");
        AssertEqual("format probe sdr retarget", GetStringProperty(sdrNv12Decision, "UiOperationName"), "SDR NV12 UI operation");

        var sessionMismatchDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: false,
            modeChanged: false,
            previousResolution: "1920x1080",
            previousFrameRate: 60,
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false),
            supportedFormats: CreateMediaFormatList(mediaFormatType),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: true,
            sessionActualWidth: 1280,
            sessionActualHeight: 720);
        AssertEqual("SessionMismatch", GetEnumName(sessionMismatchDecision, "Kind"), "session mismatch decision");
        AssertEqual("format probe (session mismatch)", GetStringProperty(sessionMismatchDecision, "ReinitializeReason"), "session mismatch reason");
        AssertEqual("format probe session mismatch", GetStringProperty(sessionMismatchDecision, "UiOperationName"), "session mismatch UI operation");

        var restoreDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: false,
            isHdrEnabled: false,
            modeChanged: true,
            previousResolution: "3840x2160",
            previousFrameRate: 60,
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false),
            supportedFormats: CreateMediaFormatList(mediaFormatType),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("RestoreActiveSelection", GetEnumName(restoreDecision, "Kind"), "recording-time restore decision");

        return Task.CompletedTask;
    }
}
