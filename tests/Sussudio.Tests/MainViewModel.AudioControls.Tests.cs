using System.Reflection;
using System.Threading.Tasks;

// Tests for view-model audio control persistence and ramp tracing.
static partial class Program
{
    private static Task MainViewModelAudioControls_MapsAnalogGainCurveAndClamps()
    {
        var mapperType = RequireType("Sussudio.ViewModels.DeviceAudioGainMapper");
        var mapPercent = mapperType.GetMethod("PercentToGainByte", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.PercentToGainByte was not found.");
        var mapByte = mapperType.GetMethod("GainByteToPercent", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.GainByteToPercent was not found.");
        var audioControlsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioControls.cs")
            .Replace("\r\n", "\n");
        var deviceAudioModeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioMode.cs")
            .Replace("\r\n", "\n");
        var analogAudioGainText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AnalogAudioGain.cs")
            .Replace("\r\n", "\n");
        var gainMapperText = ReadRepoFile("Sussudio/ViewModels/DeviceAudioGainMapper.cs")
            .Replace("\r\n", "\n");

        AssertContains(deviceAudioModeText, "DeviceAudioGainMapper.PercentToGainByte(AnalogAudioGainPercent)");
        AssertContains(analogAudioGainText, "DeviceAudioGainMapper.PercentToGainByte(gainPercent)");
        AssertContains(analogAudioGainText, "Device-native analog gain application and deferred flash persistence.");
        AssertDoesNotContain(audioControlsText, "private static byte MapPercentToGainByte");
        AssertDoesNotContain(audioControlsText, "private static double MapGainByteToPercent");
        AssertContains(gainMapperText, "private const double GainCurveK = 4.0;");
        AssertContains(gainMapperText, "internal static byte PercentToGainByte");
        AssertContains(gainMapperText, "internal static double GainByteToPercent");

        AssertEqual((byte)0, (byte)mapPercent.Invoke(null, new object[] { -25d })!, "PercentToGainByte clamps below zero");
        AssertEqual((byte)0, (byte)mapPercent.Invoke(null, new object[] { 0d })!, "PercentToGainByte zero");
        AssertEqual((byte)255, (byte)mapPercent.Invoke(null, new object[] { 100d })!, "PercentToGainByte one hundred");
        AssertEqual((byte)255, (byte)mapPercent.Invoke(null, new object[] { 150d })!, "PercentToGainByte clamps above one hundred");

        var gain25 = (byte)mapPercent.Invoke(null, new object[] { 25d })!;
        var gain50 = (byte)mapPercent.Invoke(null, new object[] { 50d })!;
        var gain75 = (byte)mapPercent.Invoke(null, new object[] { 75d })!;
        AssertEqual(true, gain25 > 0 && gain25 < gain50 && gain50 < gain75 && gain75 < 255, "PercentToGainByte monotonic curve");

        AssertNear(0d, (double)mapByte.Invoke(null, new object[] { (byte)0 })!, 0.0001d, "GainByteToPercent zero");
        AssertNear(100d, (double)mapByte.Invoke(null, new object[] { (byte)255 })!, 0.0001d, "GainByteToPercent max");
        AssertNear(50d, (double)mapByte.Invoke(null, new object[] { gain50 })!, 1.0d, "GainByteToPercent round-trip midpoint");

        return Task.CompletedTask;
    }

    private static Task MainViewModelAudioMonitoring_PreservesVolumePersistenceAndRampedRouting()
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        AssertNotNull(viewModelType.GetProperty("SuppressVolumeSave", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SuppressVolumeSave");
        AssertNotNull(viewModelType.GetProperty("VolumeSaveOverride", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.VolumeSaveOverride");
        AssertNotNull(viewModelType.GetMethod("SavePreviewVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SavePreviewVolume");

        var monitoringCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioMonitoring.cs");
        var audioInputSelectionCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioInputSelection.cs");
        var transitionCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs");
        var previewChanged = ExtractMemberCode(monitoringCode, "OnPreviewVolumeChanged");
        var handlePreviewChanged = ExtractMemberCode(transitionCode, "HandlePreviewVolumeChanged");
        var rampDown = ExtractMemberCode(transitionCode, "RampDownForAudioTransitionAsync");
        var rampUp = ExtractMemberCode(transitionCode, "RampUpForAudioTransitionAsync");
        var primeTransition = ExtractMemberCode(transitionCode, "PrimeForAudioTransition");
        var restoreTransition = ExtractMemberCode(transitionCode, "RestoreAfterUnavailableAudio");
        var monitoringTransition = ExtractMemberCode(monitoringCode, "SetAudioMonitoringEnabledWithVolumeTransitionAsync");
        var audioPreviewChanged = ExtractMemberCode(monitoringCode, "OnIsAudioPreviewEnabledChanged");
        var applyAudioInputSelection = ExtractMemberCode(audioInputSelectionCode, "ApplyAudioInputSelectionAsync");

        AssertContains(monitoringCode, "get => _previewAudioVolumeTransitionController.SuppressVolumeSave;");
        AssertContains(monitoringCode, "set => _previewAudioVolumeTransitionController.SuppressVolumeSave = value;");
        AssertContains(monitoringCode, "get => _previewAudioVolumeTransitionController.VolumeSaveOverride;");
        AssertContains(monitoringCode, "set => _previewAudioVolumeTransitionController.VolumeSaveOverride = value;");
        AssertContains(previewChanged, "_previewAudioVolumeTransitionController.HandlePreviewVolumeChanged(value);");
        AssertContains(monitoringCode, "internal void SavePreviewVolume() => SaveSettings();");
        AssertDoesNotContain(monitoringCode, "private const int PreviewAudioRampDownSteps");
        AssertContains(transitionCode, "internal sealed class PreviewAudioVolumeTransitionController");
        AssertContains(transitionCode, "private const int RampDownSteps = 18;");
        AssertContains(transitionCode, "private const int RampDownDelayMs = 25;");
        AssertContains(transitionCode, "private const int RampUpSteps = 30;");
        AssertContains(transitionCode, "private const int RampUpDelayMs = 30;");

        AssertContains(handlePreviewChanged, "if (!SuppressVolumeSave)");
        AssertContains(handlePreviewChanged, "VolumeSaveOverride = null;");
        AssertContains(handlePreviewChanged, "_context.SetSessionPreviewVolume((float)Math.Clamp(value, 0.0, 1.0));");
        AssertOccursBefore(handlePreviewChanged, "VolumeSaveOverride = null;", "_context.SetSessionPreviewVolume");

        AssertContains(rampDown, "var persistedVolume = PersistedVolumeTarget;");
        AssertContains(rampDown, "VolumeSaveOverride = persistedVolume;");
        AssertContains(rampDown, "_context.SetPreviewVolume(startingVolume * eased);");
        AssertContains(rampDown, "_context.SetPreviewVolume(0);");
        AssertContains(rampUp, "VolumeSaveOverride = volumeTarget;");
        AssertContains(rampUp, "_context.SetPreviewVolume(volumeTarget * eased);");
        AssertContains(rampUp, "VolumeSaveOverride = null;");
        AssertContains(primeTransition, "var volumeTarget = PersistedVolumeTarget;");
        AssertContains(primeTransition, "_context.SetPreviewVolume(0);");
        AssertContains(restoreTransition, "_context.SetPreviewVolume(volumeTarget);");

        AssertContains(monitoringTransition, "var volumeTarget = PrimePreviewVolumeForAudioTransition(reason);");
        AssertContains(monitoringTransition, "await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);");
        AssertContains(monitoringTransition, "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, cancellationToken, traceSession: false);");
        AssertContains(monitoringTransition, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, cancellationToken, traceSession: false);");
        AssertContains(monitoringTransition, "await _sessionCoordinator.StopAudioPreviewWithTeardownAsync(cancellationToken);");
        AssertContains(monitoringTransition, "await _sessionCoordinator.UpdateAudioMonitoringAsync(false, cancellationToken);");
        AssertOccursBefore(monitoringTransition, "var volumeTarget = PrimePreviewVolumeForAudioTransition(reason);", "await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);");
        AssertOccursBefore(monitoringTransition, "await _sessionCoordinator.UpdateAudioMonitoringAsync(true, cancellationToken);", "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, cancellationToken, traceSession: false);");
        AssertOccursBefore(monitoringTransition, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, cancellationToken, traceSession: false);", "await _sessionCoordinator.UpdateAudioMonitoringAsync(false, cancellationToken);");

        AssertContains(audioPreviewChanged, "if (value && !IsAudioEnabled)");
        AssertContains(audioPreviewChanged, "if (_suppressAudioPreviewEnabledChangeOperation)");
        AssertContains(audioPreviewChanged, "if (!value && !IsRecording)");
        AssertContains(audioPreviewChanged, "if (IsPreviewing && IsInitialized)");
        AssertContains(audioPreviewChanged, "SetAudioMonitoringEnabledWithVolumeTransitionAsync(value, description, teardownCapture: false)");
        AssertDoesNotContain(monitoringCode, "private async Task ApplyAudioInputSelectionAsync");
        AssertContains(audioInputSelectionCode, "private async Task ApplyAudioInputSelectionAsync");
        AssertOccursBefore(audioPreviewChanged, "if (value && !IsAudioEnabled)", "IsAudioPreviewEnabled = false;");
        AssertOccursBefore(audioPreviewChanged, "if (_suppressAudioPreviewEnabledChangeOperation)", "if (!value && !IsRecording)");
        AssertOccursBefore(audioPreviewChanged, "if (!value && !IsRecording)", "ResetAudioMeter();");
        AssertOccursBefore(audioPreviewChanged, "if (IsPreviewing && IsInitialized)", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(value, description, teardownCapture: false)");

        AssertContains(applyAudioInputSelection, "if (IsCustomAudioInputEnabled)");
        AssertContains(applyAudioInputSelection, "audioDeviceId = SelectedAudioInputDevice?.Id;");
        AssertContains(applyAudioInputSelection, "audioDeviceId = SelectedDevice?.AudioDeviceId;");
        AssertContains(applyAudioInputSelection, "var shouldRampMonitoring = IsPreviewing && _captureService.IsAudioPreviewActive;");
        AssertContains(applyAudioInputSelection, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, traceSession: false);");
        AssertContains(applyAudioInputSelection, "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertContains(applyAudioInputSelection, "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, traceSession: false);");
        AssertOccursBefore(applyAudioInputSelection, "if (IsCustomAudioInputEnabled)", "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertOccursBefore(applyAudioInputSelection, "await RampPreviewVolumeDownForAudioTransitionAsync(reason, traceSession: false);", "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);");
        AssertOccursBefore(applyAudioInputSelection, "await _sessionCoordinator.UpdateAudioInputAsync(audioDeviceId, audioDeviceName);", "await RampPreviewVolumeUpForAudioTransitionAsync(volumeTarget, reason, traceSession: false);");

        return Task.CompletedTask;
    }

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
        var microphoneVolumeCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.MicrophoneVolume.cs");
        var audioCode = audioControlsCode + "\n" + deviceAudioModeCode + "\n" + deviceAudioRefreshCode + "\n" + analogAudioGainCode + "\n" + microphoneVolumeCode;
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

    private static Task NativeXuAudioControlService_ProfilesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.cs")
            .Replace("\r\n", "\n");
        var profilesText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.Profiles.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "internal sealed partial class NativeXuAudioControlService");
        AssertContains(rootText, "public async Task<DeviceAudioControlState> ReadStateAsync(");
        AssertContains(rootText, "public async Task<bool> SetAudioModeAsync(");
        AssertContains(rootText, "public async Task<bool> SetAnalogGainPercentAsync(");
        AssertContains(rootText, "internal sealed record DeviceAudioControlState(");
        AssertContains(ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj"), "NativeXuAudioControlService.Profiles.cs");
        AssertContains(profilesText, "internal sealed partial class NativeXuAudioControlService");
        AssertContains(profilesText, "private static readonly int[] InputByteIndexes");
        AssertContains(profilesText, "private static readonly int[] DynamicByteIndexes");
        AssertContains(profilesText, "private static readonly byte[] HdmiReference = ParseHex(");
        AssertContains(profilesText, "private static readonly byte[] AnalogReference = ParseHex(");
        AssertContains(profilesText, "private static bool TryGetTargetInputReference(string? mode, out byte[] reference)");
        AssertContains(profilesText, "private static AudioDecodeDecision DecodeInput(byte[] payload)");
        AssertContains(profilesText, "private static AnalogGainDecision DecodeGain(byte[] payload)");
        AssertContains(profilesText, "private static byte[] ParseHex(string hex)");
        AssertContains(profilesText, "private readonly record struct GainProfile");
        AssertDoesNotContain(rootText, "private static readonly int[] InputByteIndexes");
        AssertDoesNotContain(rootText, "private static AudioDecodeDecision DecodeInput(byte[] payload)");
        AssertDoesNotContain(rootText, "private static byte[] ParseHex(string hex)");
        AssertDoesNotContain(rootText, "private async Task<bool> UpdatePayloadAsync(");
        AssertDoesNotContain(rootText, "private static IEnumerable<RawControlCandidate> EnumerateCandidates(");

        return Task.CompletedTask;
    }

    private static Task NativeXuAudioControlService_TransportLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.cs")
            .Replace("\r\n", "\n");
        var transportText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.Transport.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(transportText, "internal sealed partial class NativeXuAudioControlService");
        AssertContains(transportText, "private async Task<bool> UpdatePayloadAsync(");
        AssertContains(transportText, "private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(");
        AssertContains(transportText, "private static IEnumerable<RawControlCandidate> EnumerateCandidates(");
        AssertContains(transportText, "private static bool TryReadRawPayload(");
        AssertContains(transportText, "private static bool TryWriteRawPayload(");
        AssertContains(transportText, "private static byte[] NormalizePayload(byte[] rawPayload)");
        AssertContains(transportText, "private static byte[] RehydrateRawPayload(byte[] rawPayload, byte[] normalizedPayload)");
        AssertContains(transportText, "private static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken)");
        AssertContains(transportText, "private readonly record struct RawControlCandidate");
        AssertContains(transportText, "private readonly record struct RawPayloadSnapshot");
        AssertContains(transportText, "NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
        AssertContains(transportText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(probeProjectText, "NativeXuAudioControlService.Transport.cs");
        AssertDoesNotContain(rootText, "private static readonly Guid XuGuid");
        AssertDoesNotContain(rootText, "private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(");
        AssertDoesNotContain(rootText, "private static bool TryReadRawPayload(");

        return Task.CompletedTask;
    }

    private static Task MainViewModelAudioMeters_OwnCallbackMeterState()
    {
        var baseText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs")
            .Replace("\r\n", "\n");
        var metersText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioMeters.cs")
            .Replace("\r\n", "\n");

        AssertContains(metersText, "public double AudioMeterTarget;");
        AssertContains(metersText, "public double MicrophoneMeterTarget;");
        AssertContains(metersText, "public event Action? AudioMeterActivated;");
        AssertContains(metersText, "public event Action? MicrophoneMeterActivated;");
        AssertContains(metersText, "private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertContains(metersText, "private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertContains(metersText, "private void ResetAudioMeter()");
        AssertContains(metersText, "public void ResetAudioMeterTimerFlag()");
        AssertContains(metersText, "private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)");
        AssertContains(runtimeLifecycleControllerText, "_viewModel._captureService.AudioLevelUpdated += _viewModel.OnAudioLevelUpdated;");
        AssertContains(runtimeLifecycleControllerText, "_viewModel._captureService.MicrophoneAudioLevelUpdated += _viewModel.OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "_captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(baseText, "private const double MeterFloorDb");
        AssertDoesNotContain(baseText, "private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)");
        AssertDoesNotContain(baseText, "private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)");

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
