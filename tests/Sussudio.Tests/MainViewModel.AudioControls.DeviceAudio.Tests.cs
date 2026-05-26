using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelAudioControls_PreserveMicrophoneVolumeAndDeviceGuards()
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        AssertNotNull(viewModelType.GetMethod("SaveMicrophoneVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SaveMicrophoneVolume");

        var deviceAudioModeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioModeCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs");
        var deviceAudioRefreshText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioRefreshCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs");
        var deviceAudioRequestControllerCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs");
        var deviceAudioStateCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs");
        var microphoneVolumeCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioState.cs");
        var audioCode = deviceAudioModeCode + "\n" + deviceAudioRefreshCode + "\n" + deviceAudioRequestControllerCode + "\n" + deviceAudioStateCode + "\n" + microphoneVolumeCode;
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
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.MicrophoneVolume.cs")), "MainViewModel.MicrophoneVolume.cs folded into audio state");
        AssertDoesNotContain(deviceAudioStateCode, "SetMicrophoneEndpointVolume");
        AssertDoesNotContain(deviceAudioStateCode, "GetMicrophoneEndpointVolume");
        AssertContains(deviceAudioStateCode, "private async Task RefreshDeviceAudioControlsAsync");
        AssertContains(deviceAudioRefreshText, "Device-native audio-control support probing and state readback.");
        AssertContains(deviceAudioStateCode, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(deviceAudioModeText, "Device-native audio mode switching and failure readback.");
        AssertContains(deviceAudioStateCode, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceAudioRefresh.cs")), "MainViewModel.DeviceAudioRefresh.cs folded into device audio state");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceAudioMode.cs")), "MainViewModel.DeviceAudioMode.cs folded into device audio state");
        AssertContains(deviceAudioRequestControllerCode, "internal sealed class MainViewModelDeviceAudioRequestController");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerCode, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerCode, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "_viewModel.");
        AssertContains(deviceAudioStateCode, "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioStateCode, "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertDoesNotContain(deviceAudioStateCode, "TryApplyAtDeviceAudioModeAsync");
        AssertDoesNotContain(deviceAudioStateCode, "SetInputSourceAsync");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AnalogAudioGain.cs")), "MainViewModel analog gain writes folded into device audio state");

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

    internal static Task MainViewModelDeviceAudioRequestController_OwnsDeviceAudioRequestLifetime()
    {
        var deviceAudioRequestControllerCode = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs")
            .Replace("\r\n", "\n");
        var deviceAudioStateCode = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs")
            .Replace("\r\n", "\n");
        var controllerStart = deviceAudioRequestControllerCode.IndexOf(
            "internal sealed class MainViewModelDeviceAudioRequestController",
            StringComparison.Ordinal);
        AssertEqual(true, controllerStart >= 0, "device audio request controller class marker");
        var controllerBody = deviceAudioRequestControllerCode[controllerStart..];
        var handleModeChange = ExtractMemberCode(controllerBody, "HandleSelectedDeviceAudioModeChanged");
        var refreshControls = ExtractMemberCode(controllerBody, "RequestDeviceAudioControlsRefresh");
        var cancelWork = ExtractMemberCode(controllerBody, "CancelPendingAudioControlWork");
        var handleGainChange = ExtractMemberCode(controllerBody, "HandleAnalogAudioGainPercentChanged");
        var flashPersist = ExtractMemberCode(controllerBody, "ScheduleAnalogGainFlashPersist");

        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _gainFlashDebounceCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _gainXuDebounceCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _deviceAudioModeCts;");
        AssertContains(deviceAudioRequestControllerCode, "private CancellationTokenSource? _deviceAudioRefreshCts;");
        AssertContains(deviceAudioStateCode, "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioStateCode, "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioStateCode, "=> _deviceAudioRequestController.ScheduleAnalogGainFlashPersist(device, gainByte);");
        AssertContains(deviceAudioRequestControllerCode, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerCode, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerCode, "_viewModel.");
        AssertContains(deviceAudioRequestControllerCode, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerCode, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");

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

    internal static Task NativeXuAudioControlService_LivesInCohesiveServiceFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Audio/NativeXuAudioControlService.cs")
            .Replace("\r\n", "\n");
        var probeProjectText = ReadRepoFile("tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj");

        AssertContains(rootText, "internal sealed class NativeXuAudioControlService");
        AssertDoesNotContain(rootText, "partial class NativeXuAudioControlService");
        AssertContains(rootText, "public async Task<DeviceAudioControlState> ReadStateAsync(");
        AssertContains(rootText, "public async Task<bool> SetAudioModeAsync(");
        AssertContains(rootText, "public async Task<bool> SetAnalogGainPercentAsync(");
        AssertContains(rootText, "internal sealed record DeviceAudioControlState(");
        var deviceSupportText = ReadRepoFile("Sussudio/Services/Capture/NativeXu/NativeXuDeviceSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private static readonly int[] InputByteIndexes");
        AssertContains(rootText, "private static readonly int[] DynamicByteIndexes");
        AssertContains(rootText, "private static readonly byte[] HdmiReference = ParseHex(");
        AssertContains(rootText, "private static readonly byte[] AnalogReference = ParseHex(");
        AssertContains(rootText, "private static bool TryGetTargetInputReference(string? mode, out byte[] reference)");
        AssertContains(rootText, "private static AudioDecodeDecision DecodeInput(byte[] payload)");
        AssertContains(rootText, "private static AnalogGainDecision DecodeGain(byte[] payload)");
        AssertContains(rootText, "private static byte[] ParseHex(string hex)");
        AssertContains(rootText, "private async Task<bool> UpdatePayloadAsync(");
        AssertContains(rootText, "private async Task<RawPayloadSnapshot?> ReadPreferredPayloadAsync(");
        AssertContains(rootText, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId)");
        AssertContains(rootText, "NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
        AssertContains(rootText, "private static IEnumerable<RawControlCandidate> EnumerateCandidates(");
        AssertContains(rootText, "private static bool TryReadRawPayload(");
        AssertContains(rootText, "private static bool TryWriteRawPayload(");
        AssertContains(rootText, "private static byte[] NormalizePayload(byte[] rawPayload)");
        AssertContains(rootText, "private static byte[] RehydrateRawPayload(byte[] rawPayload, byte[] normalizedPayload)");
        AssertContains(rootText, "private static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken)");
        AssertContains(rootText, "NativeXuDeviceSupport.EnumerateSelectedInterfacePath(selectedInterfacePath)");
        AssertContains(rootText, "NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken)");
        AssertContains(rootText, "private readonly record struct GainProfile");
        AssertContains(rootText, "private readonly record struct RawControlCandidate");
        AssertContains(rootText, "private readonly record struct RawPayloadSnapshot");
        AssertDoesNotContain(rootText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfaces(");
        AssertContains(deviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfacePath(");
        AssertContains(deviceSupportText, "public static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken = default)");
        AssertContains(probeProjectText, "NativeXuAudioControlService.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.Profiles.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.Transport.cs");
        AssertDoesNotContain(probeProjectText, "NativeXuAudioControlService.RawTransport.cs");
        AssertContains(probeProjectText, "NativeXuDeviceSupport.cs");
        foreach (var removedFile in new[]
        {
            "NativeXuAudioControlService.Profiles.cs",
            "NativeXuAudioControlService.Transport.cs",
            "NativeXuAudioControlService.RawTransport.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", removedFile)),
                $"{removedFile} removed");
        }

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_LivesInFocusedHelper()
    {
        var adapterText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioDeviceDiscovery.cs")), "audio device discovery adapter stays folded into AudioState");
        AssertContains(policyText, "internal static class AudioDeviceSelectionPolicy");
        AssertContains(policyText, "internal static AudioDeviceSelection SelectStartup(");
        AssertContains(policyText, "internal static AudioDeviceSelection SelectRefresh(");
        AssertContains(policyText, "internal static IReadOnlyList<AudioInputDevice> FilterOutCaptureCardAudio(");
        AssertContains(policyText, "SelectByPreviousSavedOrFirst(availableDevices, previousAudioId, savedAudioId)");
        AssertContains(policyText, "SelectByPreviousOrFirst(availableDevices, previousAudioId)");
        AssertContains(adapterText, "AudioDeviceSelectionPolicy.SelectStartup(");
        AssertContains(adapterText, "AudioDeviceSelectionPolicy.SelectRefresh(");
        AssertContains(adapterText, "ReplaceCollection(AudioInputDevices, selection.AvailableDevices);");
        AssertContains(adapterText, "ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);");
        AssertContains(adapterText, "Logger.Log($\"SETTINGS_RESTORE: saved audio device '{savedAudioId}' not found, using fallback.\");");
        AssertContains(adapterText, "Logger.Log($\"Audio device list refreshed ({AudioInputDevices.Count} devices).\");");
        AssertDoesNotContain(policyText, "ReplaceCollection(");
        AssertDoesNotContain(policyText, "Logger.Log(");
        AssertDoesNotContain(policyText, "_pendingSaved");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_StartupFiltersCaptureCardAndUsesSavedFallbacks()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("CAPTURE-AUDIO"),
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"));
        var videoDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.CaptureDevice",
            CreateAudioDeviceSelectionPolicyCapture("video-first", "other-capture"),
            CreateAudioDeviceSelectionPolicyCapture("video-previous", "capture-audio"));

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "video-previous",
            "missing-audio",
            "saved-audio",
            "missing-mic",
            "saved-mic");

        var availableIds = GetAudioDeviceSelectionAvailableIds(selection);
        AssertEqual(3, availableIds.Length, "Startup audio list filters the capture-card endpoint");
        AssertEqual("first-audio", availableIds[0], "Startup first filtered audio id");
        AssertEqual("saved-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Startup saved audio fallback");
        AssertEqual("saved-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Startup saved microphone fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Startup saved audio found");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Startup saved microphone found");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_StartupPreservesPreviousSelections()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-audio"),
            CreateAudioDeviceSelectionPolicyAudio("previous-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"),
            CreateAudioDeviceSelectionPolicyAudio("previous-mic"));
        var videoDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.CaptureDevice");

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "missing-video",
            "previous-audio",
            "saved-audio",
            "previous-mic",
            "saved-mic");

        AssertEqual("previous-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Startup preserves previous audio");
        AssertEqual("previous-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Startup preserves previous microphone");
        AssertEqual(true, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Startup keeps existing saved-audio fallback log decision");
        AssertEqual(true, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Startup keeps existing saved-microphone fallback log decision");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_RefreshPreservesPreviousAudioAndSavedMicrophoneFallback()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("capture-audio"),
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"),
            CreateAudioDeviceSelectionPolicyAudio("previous-audio"));

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectRefresh",
            audioDevices,
            "CAPTURE-AUDIO",
            "previous-audio",
            "missing-mic",
            "saved-mic");

        var availableIds = GetAudioDeviceSelectionAvailableIds(selection);
        AssertEqual(3, availableIds.Length, "Refresh audio list filters selected capture-card endpoint");
        AssertEqual("first-audio", availableIds[0], "Refresh first filtered audio id");
        AssertEqual("previous-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Refresh preserves previous audio");
        AssertEqual("saved-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Refresh saved microphone fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Refresh does not log saved audio fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Refresh does not log saved microphone fallback");

        return Task.CompletedTask;
    }

    internal static Task AudioDeviceSelectionPolicy_EmptyListsReturnNullSelections()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.AudioInputDevice");
        var videoDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.CaptureDevice");

        var startupSelection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "missing-video",
            "previous-audio",
            "saved-audio",
            "previous-mic",
            "saved-mic");
        AssertEqual(0, GetAudioDeviceSelectionAvailableIds(startupSelection).Length, "Startup empty audio list");
        AssertEqual(null, GetPropertyValue(startupSelection, "SelectedAudioInputDevice"), "Startup empty audio selection");
        AssertEqual(null, GetPropertyValue(startupSelection, "SelectedMicrophoneDevice"), "Startup empty microphone selection");
        AssertEqual(true, GetBoolProperty(startupSelection, "ShouldLogSavedAudioFallback"), "Startup empty saved audio fallback log decision");
        AssertEqual(true, GetBoolProperty(startupSelection, "ShouldLogSavedMicrophoneFallback"), "Startup empty saved microphone fallback log decision");

        var refreshSelection = InvokeAudioDeviceSelectionPolicy(
            "SelectRefresh",
            audioDevices,
            null,
            "previous-audio",
            "previous-mic",
            "saved-mic");
        AssertEqual(0, GetAudioDeviceSelectionAvailableIds(refreshSelection).Length, "Refresh empty audio list");
        AssertEqual(null, GetPropertyValue(refreshSelection, "SelectedAudioInputDevice"), "Refresh empty audio selection");
        AssertEqual(null, GetPropertyValue(refreshSelection, "SelectedMicrophoneDevice"), "Refresh empty microphone selection");
        AssertEqual(false, GetBoolProperty(refreshSelection, "ShouldLogSavedMicrophoneFallback"), "Refresh empty saved microphone log decision");

        return Task.CompletedTask;
    }

    private static object InvokeAudioDeviceSelectionPolicy(string methodName, params object?[] arguments)
    {
        var policyType = RequireType("Sussudio.ViewModels.AudioDeviceSelectionPolicy");
        var method = policyType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing AudioDeviceSelectionPolicy.{methodName}.");
        return method.Invoke(null, arguments)
               ?? throw new InvalidOperationException($"AudioDeviceSelectionPolicy.{methodName} returned null.");
    }

    private static object CreateAudioDeviceSelectionPolicyAudio(string id)
    {
        var audioType = RequireType("Sussudio.Models.AudioInputDevice");
        var audio = Activator.CreateInstance(audioType)
            ?? throw new InvalidOperationException("Failed to create AudioInputDevice.");
        SetPropertyOrBackingField(audio, "Id", id);
        SetPropertyOrBackingField(audio, "Name", id);
        return audio;
    }

    private static object CreateAudioDeviceSelectionPolicyCapture(string id, string? audioDeviceId)
    {
        var captureType = RequireType("Sussudio.Models.CaptureDevice");
        var capture = Activator.CreateInstance(captureType)
            ?? throw new InvalidOperationException("Failed to create CaptureDevice.");
        SetPropertyOrBackingField(capture, "Id", id);
        SetPropertyOrBackingField(capture, "Name", id);
        SetPropertyOrBackingField(capture, "AudioDeviceId", audioDeviceId);
        return capture;
    }

    private static object CreateAudioDeviceSelectionPolicyList(string elementTypeName, params object[] items)
    {
        var elementType = RequireType(elementTypeName);
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(elementType))
            ?? throw new InvalidOperationException($"Failed to create list for {elementTypeName}."));
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static string? GetAudioDeviceSelectionId(object selection, string propertyName)
    {
        var device = GetPropertyValue(selection, propertyName);
        return device != null ? GetStringProperty(device, "Id") : null;
    }

    private static string[] GetAudioDeviceSelectionAvailableIds(object selection)
    {
        var devices = (IEnumerable)(GetPropertyValue(selection, "AvailableDevices")
            ?? throw new InvalidOperationException("AudioDeviceSelection.AvailableDevices was null."));
        return devices.Cast<object>().Select(device => GetStringProperty(device, "Id")).ToArray();
    }
}
