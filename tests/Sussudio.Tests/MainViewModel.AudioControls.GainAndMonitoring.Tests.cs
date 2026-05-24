using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelAudioControls_MapsAnalogGainCurveAndClamps()
    {
        var mapperType = RequireType("Sussudio.ViewModels.DeviceAudioGainMapper");
        var mapPercent = mapperType.GetMethod("PercentToGainByte", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.PercentToGainByte was not found.");
        var mapByte = mapperType.GetMethod("GainByteToPercent", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioGainMapper.GainByteToPercent was not found.");
        var deviceAudioStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs")
            .Replace("\r\n", "\n");
        var deviceAudioModeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioMode.cs")
            .Replace("\r\n", "\n");

        AssertContains(deviceAudioModeText, "DeviceAudioGainMapper.PercentToGainByte(AnalogAudioGainPercent)");
        AssertContains(deviceAudioStateText, "DeviceAudioGainMapper.PercentToGainByte(gainPercent)");
        AssertContains(deviceAudioStateText, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertDoesNotContain(deviceAudioStateText, "private static byte MapPercentToGainByte");
        AssertDoesNotContain(deviceAudioStateText, "private static double MapGainByteToPercent");
        AssertContains(deviceAudioStateText, "internal static class DeviceAudioGainMapper");
        AssertContains(deviceAudioStateText, "private const double GainCurveK = 4.0;");
        AssertContains(deviceAudioStateText, "internal static byte PercentToGainByte");
        AssertContains(deviceAudioStateText, "internal static double GainByteToPercent");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "DeviceAudioGainMapper.cs")), "DeviceAudioGainMapper folded into MainViewModel.DeviceAudioState.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AnalogAudioGain.cs")), "analog gain XU writes folded into MainViewModel.DeviceAudioState.cs");

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

    internal static Task MainViewModelAudioMonitoring_PreservesVolumePersistenceAndRampedRouting()
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        AssertNotNull(viewModelType.GetProperty("SuppressVolumeSave", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SuppressVolumeSave");
        AssertNotNull(viewModelType.GetProperty("VolumeSaveOverride", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.VolumeSaveOverride");
        AssertNotNull(viewModelType.GetMethod("SavePreviewVolume", BindingFlags.Instance | BindingFlags.NonPublic), "MainViewModel.SavePreviewVolume");

        var audioStateCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioState.cs");
        var audioInputSelectionCode = ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/MainViewModel.AudioInputSelection.cs");
        var transitionCode =
            ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs") +
            "\n" +
            ReadRepoCodeWithoutCommentsOrStrings("Sussudio/ViewModels/PreviewAudioVolumeTransitionController.Ramps.cs");
        var previewChanged = ExtractMemberCode(audioStateCode, "OnPreviewVolumeChanged");
        var handlePreviewChanged = ExtractMemberCode(transitionCode, "HandlePreviewVolumeChanged");
        var rampDown = ExtractMemberCode(transitionCode, "RampDownForAudioTransitionAsync");
        var rampUp = ExtractMemberCode(transitionCode, "RampUpForAudioTransitionAsync");
        var primeTransition = ExtractMemberCode(transitionCode, "PrimeForAudioTransition");
        var restoreTransition = ExtractMemberCode(transitionCode, "RestoreAfterUnavailableAudio");
        var monitoringTransition = ExtractMemberCode(audioStateCode, "SetAudioMonitoringEnabledWithVolumeTransitionAsync");
        var audioPreviewChanged = ExtractMemberCode(audioStateCode, "OnIsAudioPreviewEnabledChanged");
        var applyAudioInputSelection = ExtractMemberCode(audioInputSelectionCode, "ApplyAudioInputSelectionAsync");

        AssertContains(audioStateCode, "get => _previewAudioVolumeTransitionController.SuppressVolumeSave;");
        AssertContains(audioStateCode, "set => _previewAudioVolumeTransitionController.SuppressVolumeSave = value;");
        AssertContains(audioStateCode, "get => _previewAudioVolumeTransitionController.VolumeSaveOverride;");
        AssertContains(audioStateCode, "set => _previewAudioVolumeTransitionController.VolumeSaveOverride = value;");
        AssertContains(previewChanged, "_previewAudioVolumeTransitionController.HandlePreviewVolumeChanged(value);");
        AssertContains(audioStateCode, "internal void SavePreviewVolume() => SaveSettings();");
        AssertContains(audioStateCode, "private async Task RampPreviewVolumeDownForStopAsync(CancellationToken cancellationToken)");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.PreviewVolumeTransitions.cs")), "MainViewModel.PreviewVolumeTransitions.cs folded into audio state");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AudioMonitoring.cs")), "MainViewModel.AudioMonitoring.cs folded into audio state");
        AssertDoesNotContain(audioStateCode, "private const int PreviewAudioRampDownSteps");
        AssertContains(transitionCode, "internal sealed partial class PreviewAudioVolumeTransitionController");
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
        AssertDoesNotContain(audioStateCode, "private async Task ApplyAudioInputSelectionAsync");
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

    private static void AssertNear(double expected, double actual, double tolerance, string fieldName)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected {expected:0.###} +/- {tolerance:0.###}, actual {actual:0.###}.");
        }
    }
}
