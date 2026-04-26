using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAudioControls_MapsAnalogGainCurveAndClamps()
    {
        var viewModelType = RequireType("ElgatoCapture.ViewModels.MainViewModel");
        var mapPercent = viewModelType.GetMethod("MapPercentToGainByte", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MainViewModel.MapPercentToGainByte was not found.");
        var mapByte = viewModelType.GetMethod("MapGainByteToPercent", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MainViewModel.MapGainByteToPercent was not found.");

        AssertEqual((byte)0, (byte)mapPercent.Invoke(null, new object[] { -25d })!, "MapPercentToGainByte clamps below zero");
        AssertEqual((byte)0, (byte)mapPercent.Invoke(null, new object[] { 0d })!, "MapPercentToGainByte zero");
        AssertEqual((byte)255, (byte)mapPercent.Invoke(null, new object[] { 100d })!, "MapPercentToGainByte one hundred");
        AssertEqual((byte)255, (byte)mapPercent.Invoke(null, new object[] { 150d })!, "MapPercentToGainByte clamps above one hundred");

        var gain25 = (byte)mapPercent.Invoke(null, new object[] { 25d })!;
        var gain50 = (byte)mapPercent.Invoke(null, new object[] { 50d })!;
        var gain75 = (byte)mapPercent.Invoke(null, new object[] { 75d })!;
        AssertEqual(true, gain25 > 0 && gain25 < gain50 && gain50 < gain75 && gain75 < 255, "MapPercentToGainByte monotonic curve");

        AssertNear(0d, (double)mapByte.Invoke(null, new object[] { (byte)0 })!, 0.0001d, "MapGainByteToPercent zero");
        AssertNear(100d, (double)mapByte.Invoke(null, new object[] { (byte)255 })!, 0.0001d, "MapGainByteToPercent max");
        AssertNear(50d, (double)mapByte.Invoke(null, new object[] { gain50 })!, 1.0d, "MapGainByteToPercent round-trip midpoint");

        return Task.CompletedTask;
    }

    private static Task MainViewModelAudioControls_PreserveRoutingPersistenceAndDeviceGuards()
    {
        var viewModelType = RequireType("ElgatoCapture.ViewModels.MainViewModel");
        AssertNotNull(viewModelType.GetProperty("SuppressVolumeSave", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SuppressVolumeSave");
        AssertNotNull(viewModelType.GetProperty("VolumeSaveOverride", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.VolumeSaveOverride");
        AssertNotNull(viewModelType.GetMethod("SavePreviewVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SavePreviewVolume");
        AssertNotNull(viewModelType.GetMethod("SaveMicrophoneVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SaveMicrophoneVolume");

        var audioCode = ReadRepoCodeWithoutCommentsOrStrings("ElgatoCapture/ViewModels/MainViewModel.AudioControls.cs");
        var previewChanged = ExtractMemberCode(audioCode, "OnPreviewVolumeChanged");
        var setMicrophoneEndpointVolume = ExtractMemberCode(audioCode, "SetMicrophoneEndpointVolume");
        var getMicrophoneEndpointVolume = ExtractMemberCode(audioCode, "GetMicrophoneEndpointVolume");
        var audioPreviewChanged = ExtractMemberCode(audioCode, "OnIsAudioPreviewEnabledChanged");
        var applyAudioInputSelection = ExtractMemberCode(audioCode, "ApplyAudioInputSelectionAsync");
        var refreshDeviceAudioControls = ExtractMemberCode(audioCode, "RefreshDeviceAudioControlsAsync");
        var applyDeviceAudioMode = ExtractMemberCode(audioCode, "ApplyDeviceAudioModeAsync");
        var applyAnalogAudioGain = ExtractMemberCode(audioCode, "ApplyAnalogAudioGainAsync");
        var isCurrentSelectedDevice = ExtractMemberCode(audioCode, "IsCurrentSelectedDevice");
        var suppressedRefresh = ExtractMemberCode(audioCode, "WithAudioControlRefreshSuppressed");
        var normalizeDeviceAudioMode = ExtractMemberCode(audioCode, "NormalizeDeviceAudioMode");

        AssertContains(previewChanged, "_sessionCoordinator.SetPreviewVolume((float)Math.Clamp(value, 0.0, 1.0));");
        AssertContains(audioCode, "internal void SavePreviewVolume() => SaveSettings();");
        AssertContains(audioCode, "internal void SaveMicrophoneVolume() => SaveSettings();");

        AssertContains(setMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)");
        AssertContains(setMicrophoneEndpointVolume, "WasapiComInterop.SetEndpointVolume(deviceId, (float)(Math.Clamp(volumePercent, 0.0, 100.0) / 100.0));");
        AssertOccursBefore(setMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)", "WasapiComInterop.SetEndpointVolume");

        AssertContains(getMicrophoneEndpointVolume, "return 100.0;");
        AssertContains(getMicrophoneEndpointVolume, "return WasapiComInterop.GetEndpointVolume(deviceId) * 100.0;");
        AssertOccursBefore(getMicrophoneEndpointVolume, "string.IsNullOrWhiteSpace(deviceId)", "WasapiComInterop.GetEndpointVolume");

        AssertContains(audioPreviewChanged, "if (value && !IsAudioEnabled)");
        AssertContains(audioPreviewChanged, "if (!value && !IsRecording)");
        AssertContains(audioPreviewChanged, "if (IsPreviewing && IsInitialized)");
        AssertOccursBefore(audioPreviewChanged, "if (value && !IsAudioEnabled)", "IsAudioPreviewEnabled = false;");
        AssertOccursBefore(audioPreviewChanged, "if (!value && !IsRecording)", "ResetAudioMeter();");
        AssertOccursBefore(audioPreviewChanged, "if (IsPreviewing && IsInitialized)", "EnqueueUiOperation(() => _sessionCoordinator.UpdateAudioMonitoringAsync(value), description);");
        AssertOccursBefore(audioPreviewChanged, "EnqueueUiOperation(() => _sessionCoordinator.UpdateAudioMonitoringAsync(value), description);", "SaveSettings();");

        AssertContains(applyAudioInputSelection, "if (IsCustomAudioInputEnabled)");
        AssertContains(applyAudioInputSelection, "audioDeviceId = SelectedAudioInputDevice?.Id;");
        AssertContains(applyAudioInputSelection, "audioDeviceId = SelectedDevice?.AudioDeviceId;");
        AssertContains(applyAudioInputSelection, "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertOccursBefore(applyAudioInputSelection, "if (IsCustomAudioInputEnabled)", "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");

        AssertContains(refreshDeviceAudioControls, "IsDeviceAudioControlSupported = false;");
        AssertContains(refreshDeviceAudioControls, "SelectedDeviceAudioMode = DeviceAudioMode.Hdmi;");
        AssertContains(refreshDeviceAudioControls, "AnalogAudioGainPercent = 50;");
        AssertContains(refreshDeviceAudioControls, "NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out _, out _)");
        AssertContains(refreshDeviceAudioControls, "await _deviceAudioControlService.ReadStateAsync(device, cancellationToken).ConfigureAwait(false);");
        AssertContains(refreshDeviceAudioControls, "_pendingSavedDeviceAudioMode = null;");
        AssertContains(refreshDeviceAudioControls, "_pendingSavedAnalogAudioGainPercent = null;");
        AssertOccursBefore(refreshDeviceAudioControls, "if (device == null)", "IsDeviceAudioControlSupported = false;");
        AssertOccursBefore(refreshDeviceAudioControls, "NativeXuAtCommandProvider.TryGetSupported4kXIds", "var state = await _deviceAudioControlService.ReadStateAsync");
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
        AssertContains(applyDeviceAudioMode, "var gainByte = MapPercentToGainByte(AnalogAudioGainPercent);");
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
        AssertContains(applyAnalogAudioGain, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)");
        AssertContains(applyAnalogAudioGain, "StatusText =");
        AssertContains(applyAnalogAudioGain, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);");
        AssertContains(applyAnalogAudioGain, "await Task.Delay(300, token).ConfigureAwait(false);");
        AssertContains(applyAnalogAudioGain, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertContains(applyAnalogAudioGain, "if (ReferenceEquals(_gainFlashDebounceCts, cts))");
        AssertContains(applyAnalogAudioGain, "cts.Dispose();");
        AssertContains(applyAnalogAudioGain, "SaveSettings();");
        AssertOccursBefore(applyAnalogAudioGain, "if (device == null || !IsDeviceAudioControlSupported)", "NativeXuAtCommandProvider.SetAnalogGainAsync");
        AssertOccursBefore(applyAnalogAudioGain, "if (!IsCurrentSelectedDevice(device))", "NativeXuAtCommandProvider.SetAnalogGainAsync");
        AssertOccursBefore(applyAnalogAudioGain, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)", "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);");
        AssertOccursBefore(applyAnalogAudioGain, "await Task.Delay(300, token).ConfigureAwait(false);", "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertOccursBefore(applyAnalogAudioGain, "if (ReferenceEquals(_gainFlashDebounceCts, cts))", "cts.Dispose();");
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

    private static void AssertNear(double expected, double actual, double tolerance, string fieldName)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected {expected:0.###} +/- {tolerance:0.###}, actual {actual:0.###}.");
        }
    }
}
