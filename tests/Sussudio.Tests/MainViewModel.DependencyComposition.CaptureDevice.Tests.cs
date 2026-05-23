using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelCaptureDeviceControllers_UseDependencyCompositionContexts()
    {
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var audioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var deviceAudioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs").Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceRefreshController.cs").Replace("\r\n", "\n");
        var deviceAudioRequestControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs").Replace("\r\n", "\n");
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
        var deviceFormatProbeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs").Replace("\r\n", "\n");
        var deviceFormatProbeRetargetApplierText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.cs").Replace("\r\n", "\n");

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

        AssertContains(controllerGraphText, "internal static MainViewModelFrameRateTimingResolver CreateFrameRateTimingResolver(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelFrameRateTimingResolverContext");
        AssertContains(controllerGraphText, "GetRuntimeSnapshot = () => viewModel._captureService.GetRuntimeSnapshot(),");
        AssertContains(controllerGraphText, "viewModel._frameRateTimingResolver);");
        AssertContains(controllerGraphText, "private static MainViewModelCaptureModeOptionRebuildController CreateCaptureModeOptionRebuildController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelCaptureModeOptionRebuildController(\n                new MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertContains(controllerGraphText, "TryGetEffectiveResolutionSelection = viewModel.TryGetEffectiveResolutionSelection,");
        AssertContains(controllerGraphText, "ApplyResolvedFrameRateSelection = viewModel.ApplyResolvedFrameRateSelection,");
        AssertContains(controllerGraphText, "SetSelectedFormat = value => viewModel.SelectedFormat = value,");

        AssertContains(deviceRefreshControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshController");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshControllerContext");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelDeviceRefreshControllerContext _context;");
        AssertDoesNotContain(deviceRefreshControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceRefreshControllerText, "_viewModel.");
        AssertContains(deviceRefreshControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertDoesNotContain(deviceRefreshControllerText, "await _viewModel.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceRefreshController CreateDeviceRefreshController(");
        AssertContains(controllerGraphText, "new MainViewModelDeviceRefreshControllerContext");
        AssertContains(controllerGraphText, "viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)");
        AssertContains(controllerGraphText, "BeginBackgroundFormatProbe = (device, scanGeneration) =>");

        AssertContains(deviceAudioRequestControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestController");
        AssertDoesNotContain(deviceAudioRequestControllerText, "partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerText, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "_viewModel.");
        AssertContains(deviceAudioRequestControllerText, "public void HandleSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioRequestControllerText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerText, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");
        AssertContains(deviceAudioRequestControllerText, "public void CancelPendingAudioControlWork()");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceAudioRequestController CreateDeviceAudioRequestController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(controllerGraphText, "ApplyDeviceAudioModeAsync = (reason, targetDevice, cancellationToken) =>");
        AssertContains(controllerGraphText, "ApplyAnalogAudioGainAsync = (reason, targetDevice, cancellationToken) =>");

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
        AssertContains(controllerGraphText, "private static MainViewModelCaptureSettingsAutomationController CreateCaptureSettingsAutomationController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(controllerGraphText, "SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,");
        AssertContains(controllerGraphText, "ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,");

        AssertContains(recordingSettingsAutomationControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingSettingsAutomationControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationController");
        AssertContains(recordingSettingsAutomationControllerText, "public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)");
        AssertContains(recordingSettingsAutomationControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(recordingSettingsAutomationControllerContextText, "internal sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(recordingSettingsAutomationControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "_viewModel.");
        AssertContains(recordingSettingsAutomationControllerText, "_context.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertContains(controllerGraphText, "private static MainViewModelRecordingSettingsAutomationController CreateRecordingSettingsAutomationController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingSettingsAutomationControllerContext");

        AssertContains(recordingCapabilityControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityController");
        AssertContains(recordingCapabilityControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(recordingCapabilityControllerContextText, "internal sealed class MainViewModelRecordingCapabilityControllerContext");
        AssertContains(recordingCapabilityControllerText, "private readonly MainViewModelRecordingCapabilityControllerContext _context;");
        AssertDoesNotContain(recordingCapabilityControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingCapabilityControllerText, "_viewModel.");
        AssertContains(controllerGraphText, "private static MainViewModelRecordingCapabilityController CreateRecordingCapabilityController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelRecordingCapabilityControllerContext");
        AssertContains(controllerGraphText, "ReplaceAvailableRecordingFormats = formats =>");
        AssertContains(controllerGraphText, "NotifySelectedRecordingFormatChanged = () => viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat)),");

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
        AssertContains(frameRateTimingResolverText, "internal sealed class MainViewModelFrameRateTimingResolverContext");
        AssertContains(captureModeOptionRebuildControllerContextText, "public required string AutoResolutionValue { get; init; }");
        AssertContains(captureModeOptionRebuildControllerContextText, "public required double AutoFrameRateValue { get; init; }");
        AssertContains(controllerGraphText, "AutoResolutionValue = AutoResolutionValue,");
        AssertContains(controllerGraphText, "AutoFrameRateValue = AutoFrameRateValue,");
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
        AssertContains(deviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(deviceFormatProbeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeControllerText, "_viewModel.");
        AssertContains(deviceFormatProbeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier = _context.CreateRetargetApplier();");
        AssertContains(deviceFormatProbeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(deviceFormatProbeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(deviceFormatProbeRetargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(deviceFormatProbeRetargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(deviceFormatProbeRetargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(deviceFormatProbeRetargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(deviceFormatProbeRetargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceFormatProbeRetargetApplierText, "_viewModel.");
        AssertEqual(
            true,
            deviceFormatProbeRetargetApplierText.Split('\n').Length >= 100,
            "device format probe retarget applier is a substantial ownership file");
        AssertContains(deviceFormatProbeRetargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(controllerGraphText, "private static MainViewModelDeviceFormatProbeController CreateDeviceFormatProbeController(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(controllerGraphText, "new MainViewModelDeviceFormatProbeRetargetApplierContext");

        return Task.CompletedTask;
    }
}
