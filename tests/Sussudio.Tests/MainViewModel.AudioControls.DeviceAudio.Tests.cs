using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAudioControls_PreserveMicrophoneVolumeAndDeviceGuards()
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        AssertNotNull(viewModelType.GetMethod("SaveMicrophoneVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SaveMicrophoneVolume");

        var audioControlsCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioControls.cs");
        var deviceAudioModeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioMode.cs")
            .Replace("\r\n", "\n");
        var deviceAudioModeCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.DeviceAudioMode.cs");
        var deviceAudioRefreshText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioRefresh.cs")
            .Replace("\r\n", "\n");
        var deviceAudioRefreshCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.DeviceAudioRefresh.cs");
        var analogAudioGainCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AnalogAudioGain.cs");
        var deviceAudioRequestControllerCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs");
        var deviceAudioRequestControllerContextCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.Context.cs");
        var microphoneVolumeCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.MicrophoneVolume.cs");
        var audioCode = audioControlsCode + "\n" + deviceAudioModeCode + "\n" + deviceAudioRefreshCode + "\n" + analogAudioGainCode + "\n" + deviceAudioRequestControllerCode + "\n" + microphoneVolumeCode;
        var setMicrophoneEndpointVolume = ExtractMemberCode(audioCode, "SetMicrophoneEndpointVolume");
        var getMicrophoneEndpointVolume = ExtractMemberCode(audioCode, "GetMicrophoneEndpointVolume");
        var refreshDeviceAudioControls = ExtractMemberCode(audioCode, "RefreshDeviceAudioControlsAsync");
        var applyDeviceAudioMode = ExtractMemberCode(audioCode, "ApplyDeviceAudioModeAsync");
        var applyAnalogAudioGain = ExtractMemberCode(audioCode, "ApplyAnalogAudioGainAsync");
        var isCurrentSelectedDevice = ExtractMemberCode(audioCode, "IsCurrentSelectedDevice");
        var suppressedRefresh = ExtractMemberCode(audioCode, "WithAudioControlRefreshSuppressed");
        var normalizeDeviceAudioMode = ExtractMemberCode(audioCode, "NormalizeDeviceAudioMode");

        AssertContains(microphoneVolumeCode, "internal void SaveMicrophoneVolume() => SaveSettings();");
        AssertContains(microphoneVolumeCode, "partial void OnMicrophoneVolumeChanged(double value)");
        AssertDoesNotContain(audioControlsCode, "SetMicrophoneEndpointVolume");
        AssertDoesNotContain(audioControlsCode, "GetMicrophoneEndpointVolume");
        AssertDoesNotContain(audioControlsCode, "private async Task RefreshDeviceAudioControlsAsync");
        AssertContains(deviceAudioRefreshCode, "private async Task RefreshDeviceAudioControlsAsync");
        AssertContains(deviceAudioRefreshText, "Device-native audio-control support probing and state readback.");
        AssertDoesNotContain(audioControlsCode, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(deviceAudioModeCode, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(deviceAudioModeText, "Device-native audio mode switching and failure readback.");
        AssertDoesNotContain(audioControlsCode, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertContains(analogAudioGainCode, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertContains(deviceAudioRequestControllerCode, "private sealed partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerContextCode, "private sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerCode, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "_viewModel.");
        AssertContains(deviceAudioRequestControllerCode, "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioRequestControllerCode, "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertDoesNotContain(audioControlsCode, "TryApplyAtDeviceAudioModeAsync");
        AssertDoesNotContain(audioControlsCode, "SetInputSourceAsync");

        AssertContains(setMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)");
        AssertContains(setMicrophoneEndpointVolume, "WasapiComInterop.SetEndpointVolume(deviceId, (float)(Math.Clamp(volumePercent, 0.0, 100.0) / 100.0));");
        AssertOccursBefore(setMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)", "WasapiComInterop.SetEndpointVolume");

        AssertContains(getMicrophoneEndpointVolume, "return 100.0;");
        AssertContains(getMicrophoneEndpointVolume, "return WasapiComInterop.GetEndpointVolume(deviceId) * 100.0;");
        AssertOccursBefore(getMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)", "WasapiComInterop.GetEndpointVolume");

        AssertContains(refreshDeviceAudioControls, "IsDeviceAudioControlSupported = false;");
        AssertContains(refreshDeviceAudioControls, "SelectedDeviceAudioMode = DeviceAudioMode.Hdmi;");
        AssertContains(refreshDeviceAudioControls, "AnalogAudioGainPercent = 50;");
        AssertContains(refreshDeviceAudioControls, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out _, out _)");
        AssertContains(refreshDeviceAudioControls, "await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);");
        AssertContains(refreshDeviceAudioControls, "_pendingSavedDeviceAudioMode = null;");
        AssertContains(refreshDeviceAudioControls, "_pendingSavedAnalogAudioGainPercent = null;");
        AssertOccursBefore(refreshDeviceAudioControls, "if (device == null)", "IsDeviceAudioControlSupported = false;");
        AssertOccursBefore(refreshDeviceAudioControls, "NativeXuDeviceSupport.TryGetSupported4kXIds", "var state = await _deviceAudioControlService.ReadStateAsync");
        var refreshInitialReadback = ExtractTextBetween(
            refreshDeviceAudioControls,
            "var state = await _deviceAudioControlService.ReadStateAsync",
            "WithAudioControlRefreshSuppressed(() =>");
        AssertContains(refreshInitialReadback, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(refreshInitialReadback, "if (!IsCurrentSelectedDevice(device))");
        var refreshRestoreReadback = ExtractTextBetween(
            refreshDeviceAudioControls,
            "var refreshedState = await _deviceAudioControlService.ReadStateAsync",
            "_pendingSavedDeviceAudioMode = null;");
        AssertContains(refreshRestoreReadback, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(refreshRestoreReadback, "if (!IsCurrentSelectedDevice(device))");

        AssertContains(applyDeviceAudioMode, "if (device == null || !IsDeviceAudioControlSupported)");
        AssertContains(applyDeviceAudioMode, "if (!IsCurrentSelectedDevice(device))");
        AssertContains(applyDeviceAudioMode, "var mode = NormalizeDeviceAudioMode(explicitMode ?? SelectedDeviceAudioMode);");
        AssertContains(applyDeviceAudioMode, "var gainByte = DeviceAudioGainMapper.PercentToGainByte(AnalogAudioGainPercent);");
        AssertContains(applyDeviceAudioMode, "NativeXuAtCommandProvider.SwitchAudioInputAsync(device, isAnalog, gainByte, cancellationToken)");
        AssertContains(applyDeviceAudioMode, "var failureState = await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);");
        AssertContains(applyDeviceAudioMode, "StatusText =");
        AssertContains(applyDeviceAudioMode, "if (reapplyAnalogGain && string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))");
        AssertContains(applyDeviceAudioMode, "ApplyAnalogAudioGainAsync(");
        AssertContains(applyDeviceAudioMode, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);");
        AssertContains(applyDeviceAudioMode, "if (persistSettings)");
        AssertOccursBefore(applyDeviceAudioMode, "if (device == null || !IsDeviceAudioControlSupported)", "NativeXuAtCommandProvider.SwitchAudioInputAsync");
        AssertOccursBefore(applyDeviceAudioMode, "if (!IsCurrentSelectedDevice(device))", "NativeXuAtCommandProvider.SwitchAudioInputAsync");
        AssertOccursBefore(applyDeviceAudioMode, "NativeXuAtCommandProvider.SwitchAudioInputAsync", "var failureState = await _deviceAudioControlService.ReadStateAsync");
        AssertOccursBefore(applyDeviceAudioMode, "var failureState = await _deviceAudioControlService.ReadStateAsync", "StatusText =");
        AssertOccursBefore(applyDeviceAudioMode, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = mode);", "if (persistSettings)");

        AssertContains(applyAnalogAudioGain, "var gainPercent = Math.Clamp(explicitPercent ?? AnalogAudioGainPercent, 0.0, 100.0);");
        AssertContains(applyAnalogAudioGain, "var gainByte = DeviceAudioGainMapper.PercentToGainByte(gainPercent);");
        AssertContains(applyAnalogAudioGain, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)");
        AssertContains(applyAnalogAudioGain, "StatusText =");
        AssertContains(applyAnalogAudioGain, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);");
        AssertContains(applyAnalogAudioGain, "SaveSettings();");
        AssertOccursBefore(applyAnalogAudioGain, "if (device == null || !IsDeviceAudioControlSupported)", "NativeXuAtCommandProvider.SetAnalogGainAsync");
        AssertOccursBefore(applyAnalogAudioGain, "if (!IsCurrentSelectedDevice(device))", "NativeXuAtCommandProvider.SetAnalogGainAsync");
        AssertOccursBefore(applyAnalogAudioGain, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)", "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);");
        AssertOccursBefore(applyAnalogAudioGain, "if (persistSettings)", "SaveSettings();");

        AssertContains(isCurrentSelectedDevice, "string.Equals(selected.Id, device.Id, StringComparison.OrdinalIgnoreCase)");
        AssertContains(isCurrentSelectedDevice, "string.Equals(selected.NativeXuInterfacePath, device.NativeXuInterfacePath, StringComparison.OrdinalIgnoreCase)");
        AssertContains(suppressedRefresh, "_isRefreshingDeviceAudioControls = true;");
        AssertContains(suppressedRefresh, "finally");
        AssertContains(suppressedRefresh, "_isRefreshingDeviceAudioControls = false;");
        AssertOccursBefore(suppressedRefresh, "_isRefreshingDeviceAudioControls = true;", "try");
        AssertOccursBefore(suppressedRefresh, "finally", "_isRefreshingDeviceAudioControls = false;");
        AssertContains(normalizeDeviceAudioMode, "string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase)");
        AssertContains(normalizeDeviceAudioMode, "? DeviceAudioMode.Analog");
        AssertContains(normalizeDeviceAudioMode, ": DeviceAudioMode.Hdmi;");

        return Task.CompletedTask;
    }

    private static Task MainViewModelDeviceAudioRequestController_OwnsDeviceAudioRequestLifetime()
    {
        var deviceAudioRequestControllerCode = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs")
            .Replace("\r\n", "\n");
        var deviceAudioRequestControllerContextCode = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.Context.cs")
            .Replace("\r\n", "\n");
        var deviceAudioGainRequestControllerCode = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.Gain.cs")
            .Replace("\r\n", "\n");
        var controllerStart = deviceAudioRequestControllerCode.IndexOf(
            "private sealed partial class MainViewModelDeviceAudioRequestController",
            StringComparison.Ordinal);
        AssertEqual(true, controllerStart >= 0, "device audio request controller class marker");
        var controllerBody = deviceAudioRequestControllerCode[controllerStart..];
        var handleModeChange = ExtractMemberCode(controllerBody, "HandleSelectedDeviceAudioModeChanged");
        var refreshControls = ExtractMemberCode(controllerBody, "RequestDeviceAudioControlsRefresh");
        var cancelWork = ExtractMemberCode(controllerBody, "CancelPendingAudioControlWork");
        var gainControllerStart = deviceAudioGainRequestControllerCode.IndexOf(
            "private sealed partial class MainViewModelDeviceAudioRequestController",
            StringComparison.Ordinal);
        AssertEqual(true, gainControllerStart >= 0, "device audio gain request controller class marker");
        var gainControllerBody = deviceAudioGainRequestControllerCode[gainControllerStart..];
        var handleGainChange = ExtractMemberCode(gainControllerBody, "HandleAnalogAudioGainPercentChanged");
        var flashPersist = ExtractMemberCode(gainControllerBody, "ScheduleAnalogGainFlashPersist");

        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _gainFlashDebounceCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _gainXuDebounceCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _deviceAudioModeCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _deviceAudioRefreshCts;");
        AssertContains(deviceAudioRequestControllerCode, "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioRequestControllerCode, "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerCode, "=> _deviceAudioRequestController.ScheduleAnalogGainFlashPersist(device, gainByte);");
        AssertContains(deviceAudioRequestControllerContextCode, "private sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerCode, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "_viewModel.");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");
        AssertContains(deviceAudioGainRequestControllerCode, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioGainRequestControllerCode, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");

        AssertContains(refreshControls, "_deviceAudioRefreshCts = refreshCts;");
        AssertContains(refreshControls, "_context.RefreshDeviceAudioControlsAsync(targetDevice, true, refreshToken)");
        AssertContains(refreshControls, "\"device audio controls refresh\", true");
        AssertContains(refreshControls, "if (ReferenceEquals(_deviceAudioRefreshCts, refreshCts))");
        AssertContains(refreshControls, "refreshCts.Dispose();");

        AssertContains(handleModeChange, "oldCts?.Cancel();");
        AssertContains(handleModeChange, "_deviceAudioModeCts = cts;");
        AssertContains(handleModeChange, "_context.ApplyDeviceAudioModeAsync(\"device audio mode change\", targetDevice, token)");
        AssertContains(handleModeChange, "if (ReferenceEquals(_deviceAudioModeCts, cts))");
        AssertContains(handleModeChange, "cts.Dispose();");
        AssertContains(handleModeChange, "_context.SaveSettings();");

        AssertContains(handleGainChange, "oldCts?.Cancel();");
        AssertContains(handleGainChange, "_gainXuDebounceCts = cts;");
        AssertContains(handleGainChange, "await Task.Delay(200, token).ConfigureAwait(false);");
        AssertContains(handleGainChange, "_context.ApplyAnalogAudioGainAsync(\"analog audio gain change\", targetDevice, token)");
        AssertContains(handleGainChange, "if (ReferenceEquals(_gainXuDebounceCts, cts))");
        AssertContains(handleGainChange, "cts.Dispose();");
        AssertContains(handleGainChange, "_context.SaveSettings();");

        AssertContains(flashPersist, "oldCts?.Cancel();");
        AssertContains(flashPersist, "_gainFlashDebounceCts = cts;");
        AssertContains(flashPersist, "await Task.Delay(300, token).ConfigureAwait(false);");
        AssertContains(flashPersist, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertContains(flashPersist, "if (ReferenceEquals(_gainFlashDebounceCts, cts))");
        AssertContains(flashPersist, "cts.Dispose();");
        AssertOccursBefore(flashPersist, "await Task.Delay(300, token).ConfigureAwait(false);", "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertOccursBefore(flashPersist, "if (ReferenceEquals(_gainFlashDebounceCts, cts))", "cts.Dispose();");

        AssertContains(cancelWork, "var flashCts = _gainFlashDebounceCts;");
        AssertContains(cancelWork, "_gainFlashDebounceCts = null;");
        AssertContains(cancelWork, "flashCts?.Cancel();");
        AssertContains(cancelWork, "var xuCts = _gainXuDebounceCts;");
        AssertContains(cancelWork, "_gainXuDebounceCts = null;");
        AssertContains(cancelWork, "xuCts?.Cancel();");
        AssertContains(cancelWork, "var modeCts = _deviceAudioModeCts;");
        AssertContains(cancelWork, "_deviceAudioModeCts = null;");
        AssertContains(cancelWork, "modeCts?.Cancel();");
        AssertContains(cancelWork, "var refreshCts = _deviceAudioRefreshCts;");
        AssertContains(cancelWork, "_deviceAudioRefreshCts = null;");
        AssertContains(cancelWork, "refreshCts?.Cancel();");

        return Task.CompletedTask;
    }
}
