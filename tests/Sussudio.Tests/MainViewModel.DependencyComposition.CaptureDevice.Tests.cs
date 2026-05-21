using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelCaptureDeviceControllers_UseDependencyCompositionContexts()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var deviceAudioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs").Replace("\r\n", "\n");
        var controllerGraphCaptureSettingsAutomationText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.CaptureSettingsAutomation.cs").Replace("\r\n", "\n");
        var controllerGraphCaptureModesText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.CaptureModes.cs").Replace("\r\n", "\n");
        var controllerGraphDeviceAudioText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.DeviceAudio.cs").Replace("\r\n", "\n");
        var controllerGraphDeviceFormatProbeText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.DeviceFormatProbe.cs").Replace("\r\n", "\n");
        var controllerGraphDeviceText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Device.cs").Replace("\r\n", "\n");
        var controllerGraphRecordingCapabilityText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.RecordingCapability.cs").Replace("\r\n", "\n");
        var controllerGraphRecordingSettingsAutomationText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.RecordingSettingsAutomation.cs").Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceRefreshController.cs").Replace("\r\n", "\n");
        var deviceRefreshControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceRefreshController.Context.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.Context.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerGainText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.Gain.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureSettingsAutomationController.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureSettingsAutomationController.Context.cs").Replace("\r\n", "\n");
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.cs").Replace("\r\n", "\n");
        var recordingSettingsAutomationControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.Context.cs").Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.cs").Replace("\r\n", "\n");
        var recordingCapabilityControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.Context.cs").Replace("\r\n", "\n");
        var captureModeOptionRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var captureModeOptionRebuildControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.Context.cs").Replace("\r\n", "\n");
        var captureModeOptionFrameRateRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.FrameRate.cs").Replace("\r\n", "\n");
        var captureModeOptionResolutionRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.Resolution.cs").Replace("\r\n", "\n");
        var frameRateTimingResolverText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelFrameRateTimingResolver.cs").Replace("\r\n", "\n");
        var frameRateTimingResolverContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelFrameRateTimingResolver.Context.cs").Replace("\r\n", "\n");
        var deviceFormatProbeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs").Replace("\r\n", "\n");
        var deviceFormatProbeControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.Context.cs").Replace("\r\n", "\n");
        var deviceFormatProbeRetargetApplierText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.cs").Replace("\r\n", "\n");
        var deviceFormatProbeRetargetApplierContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.Context.cs").Replace("\r\n", "\n");

        AssertContains(deviceAudioStateText, "public partial ObservableCollection<string> AvailableDeviceAudioModes");
        AssertContains(deviceAudioStateText, "public partial bool IsDeviceAudioControlSupported");
        AssertContains(deviceAudioStateText, "public partial string SelectedDeviceAudioMode");
        AssertContains(deviceAudioStateText, "public partial double AnalogAudioGainPercent");
        AssertDoesNotContain(audioStateText, "SelectedDeviceAudioMode");
        AssertDoesNotContain(audioStateText, "AnalogAudioGainPercent");

        AssertContains(controllerGraphText, "var deviceAudioRequestController = CreateDeviceAudioRequestController(viewModel);");
        AssertContains(controllerGraphText, "var recordingCapabilityController = CreateRecordingCapabilityController(viewModel);");
        AssertContains(controllerGraphText, "var captureSettingsAutomationController = CreateCaptureSettingsAutomationController(viewModel);");
        AssertContains(controllerGraphText, "var recordingSettingsAutomationController = CreateRecordingSettingsAutomationController(viewModel);");
        AssertContains(controllerGraphText, "var deviceFormatProbeController = CreateDeviceFormatProbeController(viewModel);");
        AssertContains(controllerGraphText, "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertOccursBefore(
            controllerGraphText,
            "var previewLifecycleController = CreatePreviewLifecycleController(viewModel);",
            "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");

        AssertContains(controllerGraphCaptureModesText, "private sealed partial class MainViewModelControllerGraph");
        AssertContains(controllerGraphDeviceText, "private sealed partial class MainViewModelControllerGraph");
        AssertContains(controllerGraphCaptureModesText, "internal static MainViewModelFrameRateTimingResolver CreateFrameRateTimingResolver(MainViewModel viewModel)");
        AssertContains(controllerGraphCaptureModesText, "new MainViewModelFrameRateTimingResolverContext");
        AssertContains(controllerGraphCaptureModesText, "GetRuntimeSnapshot = () => viewModel._captureService.GetRuntimeSnapshot(),");
        AssertContains(controllerGraphCaptureModesText, "viewModel._frameRateTimingResolver);");
        AssertContains(controllerGraphCaptureModesText, "private static MainViewModelCaptureModeOptionRebuildController CreateCaptureModeOptionRebuildController(MainViewModel viewModel)");
        AssertContains(controllerGraphCaptureModesText, "new MainViewModelCaptureModeOptionRebuildController(\n                new MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertContains(controllerGraphCaptureModesText, "TryGetEffectiveResolutionSelection = viewModel.TryGetEffectiveResolutionSelection,");
        AssertContains(controllerGraphCaptureModesText, "ApplyResolvedFrameRateSelection = viewModel.ApplyResolvedFrameRateSelection,");
        AssertContains(controllerGraphCaptureModesText, "SetSelectedFormat = value => viewModel.SelectedFormat = value,");

        AssertContains(deviceRefreshControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshController");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(deviceRefreshControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(deviceRefreshControllerContextText, "internal sealed class MainViewModelDeviceRefreshControllerContext");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelDeviceRefreshControllerContext _context;");
        AssertDoesNotContain(deviceRefreshControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceRefreshControllerText, "_viewModel.");
        AssertContains(deviceRefreshControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertDoesNotContain(deviceRefreshControllerText, "await _viewModel.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertContains(controllerGraphDeviceText, "private static MainViewModelDeviceRefreshController CreateDeviceRefreshController(");
        AssertContains(controllerGraphDeviceText, "new MainViewModelDeviceRefreshControllerContext");
        AssertContains(controllerGraphDeviceText, "viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)");
        AssertContains(controllerGraphDeviceText, "BeginBackgroundFormatProbe = (device, scanGeneration) =>");

        AssertContains(deviceAudioRequestControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceAudioRequestControllerText, "internal sealed partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(deviceAudioRequestControllerContextText, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerText, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "_viewModel.");
        AssertContains(deviceAudioRequestControllerText, "public void HandleSelectedDeviceAudioModeChanged(string value)");
        AssertDoesNotContain(deviceAudioRequestControllerText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerGainText, "namespace Sussudio.Controllers;");
        AssertContains(deviceAudioRequestControllerGainText, "internal sealed partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerGainText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerGainText, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");
        AssertDoesNotContain(deviceAudioRequestControllerGainText, "_viewModel.");
        AssertContains(deviceAudioRequestControllerText, "public void CancelPendingAudioControlWork()");
        AssertContains(controllerGraphDeviceAudioText, "private static MainViewModelDeviceAudioRequestController CreateDeviceAudioRequestController(MainViewModel viewModel)");
        AssertContains(controllerGraphDeviceAudioText, "new MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(controllerGraphDeviceAudioText, "ApplyDeviceAudioModeAsync = (reason, targetDevice, cancellationToken) =>");
        AssertContains(controllerGraphDeviceAudioText, "ApplyAnalogAudioGainAsync = (reason, targetDevice, cancellationToken) =>");

        AssertContains(captureSettingsAutomationControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureSettingsAutomationControllerText, "internal sealed class MainViewModelCaptureSettingsAutomationController");
        AssertEqual(
            true,
            captureSettingsAutomationControllerText.Split('\n').Length >= 100,
            "capture settings automation controller is a substantial ownership file");
        AssertContains(captureSettingsAutomationControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(captureSettingsAutomationControllerContextText, "internal sealed class MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(captureSettingsAutomationControllerText, "private readonly MainViewModelCaptureSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "_viewModel.");
        AssertContains(captureSettingsAutomationControllerText, "private readonly SemaphoreSlim _captureModeGate = new(1, 1);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(controllerGraphCaptureSettingsAutomationText, "private static MainViewModelCaptureSettingsAutomationController CreateCaptureSettingsAutomationController(MainViewModel viewModel)");
        AssertContains(controllerGraphCaptureSettingsAutomationText, "new MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(controllerGraphCaptureSettingsAutomationText, "SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,");
        AssertContains(controllerGraphCaptureSettingsAutomationText, "ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,");

        AssertContains(recordingSettingsAutomationControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingSettingsAutomationControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationController");
        AssertContains(recordingSettingsAutomationControllerText, "public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)");
        AssertContains(recordingSettingsAutomationControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(recordingSettingsAutomationControllerContextText, "internal sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(recordingSettingsAutomationControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "_viewModel.");
        AssertContains(recordingSettingsAutomationControllerText, "_context.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertContains(controllerGraphRecordingSettingsAutomationText, "private static MainViewModelRecordingSettingsAutomationController CreateRecordingSettingsAutomationController(MainViewModel viewModel)");
        AssertContains(controllerGraphRecordingSettingsAutomationText, "new MainViewModelRecordingSettingsAutomationControllerContext");

        AssertContains(recordingCapabilityControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityController");
        AssertContains(recordingCapabilityControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(recordingCapabilityControllerContextText, "internal sealed class MainViewModelRecordingCapabilityControllerContext");
        AssertContains(recordingCapabilityControllerText, "private readonly MainViewModelRecordingCapabilityControllerContext _context;");
        AssertDoesNotContain(recordingCapabilityControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingCapabilityControllerText, "_viewModel.");
        AssertContains(controllerGraphRecordingCapabilityText, "private static MainViewModelRecordingCapabilityController CreateRecordingCapabilityController(MainViewModel viewModel)");
        AssertContains(controllerGraphRecordingCapabilityText, "new MainViewModelRecordingCapabilityControllerContext");
        AssertContains(controllerGraphRecordingCapabilityText, "ReplaceAvailableRecordingFormats = formats =>");
        AssertContains(controllerGraphRecordingCapabilityText, "NotifySelectedRecordingFormatChanged = () => viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat)),");

        AssertContains(captureModeOptionRebuildControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureModeOptionRebuildControllerText, "internal sealed partial class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(captureModeOptionRebuildControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(captureModeOptionRebuildControllerContextText, "internal sealed class MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertContains(captureModeOptionRebuildControllerText, "private readonly MainViewModelCaptureModeOptionRebuildControllerContext _context;");
        AssertContains(captureModeOptionRebuildControllerText, "private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;");
        AssertDoesNotContain(captureModeOptionRebuildControllerContextText, "ResolvePreferredTimingFamily");
        AssertDoesNotContain(captureModeOptionRebuildControllerContextText, "ResolveDetectedSourceFrameRate");
        AssertDoesNotContain(captureModeOptionRebuildControllerContextText, "BuildFrameRateTimingVariants");
        AssertContains(frameRateTimingResolverText, "namespace Sussudio.Controllers;");
        AssertContains(frameRateTimingResolverText, "internal sealed class MainViewModelFrameRateTimingResolver");
        AssertContains(frameRateTimingResolverText, "public FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertContains(frameRateTimingResolverText, "public (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(");
        AssertContains(frameRateTimingResolverText, "public IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)");
        AssertContains(frameRateTimingResolverContextText, "internal sealed class MainViewModelFrameRateTimingResolverContext");
        AssertContains(captureModeOptionRebuildControllerContextText, "public required string AutoResolutionValue { get; init; }");
        AssertContains(captureModeOptionRebuildControllerContextText, "public required double AutoFrameRateValue { get; init; }");
        AssertContains(controllerGraphCaptureModesText, "AutoResolutionValue = AutoResolutionValue,");
        AssertContains(controllerGraphCaptureModesText, "AutoFrameRateValue = AutoFrameRateValue,");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "_viewModel.");
        AssertDoesNotContain(captureModeOptionFrameRateRebuildControllerText, "_viewModel.");
        AssertDoesNotContain(captureModeOptionResolutionRebuildControllerText, "_viewModel.");
        AssertContains(captureModeOptionFrameRateRebuildControllerText, "internal sealed partial class MainViewModelCaptureModeOptionRebuildController");
        AssertEqual(
            true,
            captureModeOptionFrameRateRebuildControllerText.Split('\n').Length >= 100,
            "capture mode option frame-rate rebuild partial is a substantial ownership file");
        AssertContains(captureModeOptionFrameRateRebuildControllerText, "_frameRateTimingResolver.ResolveDetectedSourceFrameRate(");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(captureModeOptionFrameRateRebuildControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeOptionRebuildControllerText, "public void UpdateSelectedFormat()");
        AssertDoesNotContain(captureModeOptionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(captureModeOptionResolutionRebuildControllerText, "internal sealed partial class MainViewModelCaptureModeOptionRebuildController");
        AssertEqual(
            true,
            captureModeOptionResolutionRebuildControllerText.Split('\n').Length >= 100,
            "capture mode option resolution rebuild partial is a substantial ownership file");
        AssertContains(captureModeOptionResolutionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(captureModeOptionResolutionRebuildControllerText, "=> RebuildFrameRateOptions();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelResolutionOptionRebuildController.cs")),
            "old standalone resolution option rebuild controller removed");

        AssertContains(deviceFormatProbeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(deviceFormatProbeControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeControllerContextText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(deviceFormatProbeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "_viewModel.");
        AssertContains(deviceFormatProbeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier = _context.CreateRetargetApplier();");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(deviceFormatProbeRetargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeRetargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(deviceFormatProbeRetargetApplierContextText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeRetargetApplierContextText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(deviceFormatProbeRetargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(deviceFormatProbeRetargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeRetargetApplierText, "_viewModel.");
        AssertEqual(
            true,
            deviceFormatProbeRetargetApplierText.Split('\n').Length >= 100,
            "device format probe retarget applier is a substantial ownership file");
        AssertContains(deviceFormatProbeRetargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(controllerGraphDeviceFormatProbeText, "private static MainViewModelDeviceFormatProbeController CreateDeviceFormatProbeController(MainViewModel viewModel)");
        AssertContains(controllerGraphDeviceFormatProbeText, "new MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(controllerGraphDeviceFormatProbeText, "new MainViewModelDeviceFormatProbeRetargetApplierContext");

        return Task.CompletedTask;
    }
}
