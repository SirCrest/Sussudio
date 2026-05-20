using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish()
    {
        var settingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs")
            .Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs")
            .Replace("\r\n", "\n");
        var controllerGraphDeviceText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Device.cs")
            .Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceRefreshController.cs")
            .Replace("\r\n", "\n");

        var initialize = ExtractMemberCode(settingsText, "InitializeAsync");
        AssertContains(initialize, "LoadSettings();");
        AssertContains(initialize, "StartRecordingCapabilityRefresh();");
        AssertContains(initialize, "return Task.CompletedTask;");
        AssertDoesNotContain(initialize, "await Task.WhenAll");
        AssertOccursBefore(initialize, "LoadSettings();", "StartRecordingCapabilityRefresh();");

        var startupRefresh = ExtractMemberCode(recordingCapabilityControllerText, "Start");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshRecordingFormatCapabilitiesAsync(), \"recording formats\");");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshSplitEncodeCapabilitiesAsync(), \"split encode modes\");");
        AssertDoesNotContain(settingsText, "private void StartRecordingCapabilityRefresh()");
        AssertContains(recordingCapabilityControllerText, "private void StartRecordingCapabilityRefresh()");
        AssertContains(recordingCapabilityControllerText, "=> _recordingCapabilityController.Start();");

        var recordingFormatRefresh = ExtractMemberCode(recordingCapabilityControllerText, "RefreshRecordingFormatCapabilitiesAsync");
        AssertContains(recordingFormatRefresh, "support.HasH264Nvenc");
        AssertContains(recordingFormatRefresh, "support.HasHevcNvenc");
        AssertContains(recordingFormatRefresh, "support.HasAv1Nvenc");
        AssertDoesNotContain(recordingFormatRefresh, "support.HasAv1)");

        var splitEncodeRefresh = ExtractMemberCode(recordingCapabilityControllerText, "RefreshSplitEncodeCapabilitiesAsync");
        AssertContains(splitEncodeRefresh, "if (!support.Supports2Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"2-way\");");
        AssertContains(splitEncodeRefresh, "if (!support.Supports3Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"3-way\");");
        AssertContains(splitEncodeRefresh, "_context.SetSelectedSplitEncodeMode(\"Auto\");");

        AssertContains(rootViewModelText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertContains(controllerGraphText, "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphDeviceText, "viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)");

        var refreshDevices = ExtractMemberCode(deviceRefreshControllerText, "RefreshDevicesAsync");
        AssertContains(refreshDevices, "var discovery = await _context.EnumerateCaptureDeviceDiscoveryAsync()");
        AssertContains(refreshDevices, "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "_context.EnumerateCaptureDeviceDiscoveryAsync()", "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "ApplyStartupAudioDeviceScan(", "_context.ReplaceDevices(devices.ToList());");
        AssertOccursBefore(refreshDevices, "_context.ReplaceDevices(devices.ToList());", "_context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);");
        AssertOccursBefore(refreshDevices, "_context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);", "ApplySuccessfulDeviceScanAsync(");
        var successfulScan = ExtractTextBetween(
            deviceRefreshControllerText,
            "private async Task ApplySuccessfulDeviceScanAsync",
            "\n    }\n}");
        AssertOccursBefore(successfulScan, "var savedDeviceId = _context.GetPendingSavedDeviceId();", "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertOccursBefore(successfulScan, "_context.SetSelectedDevice(nextSelectedDevice);", "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertOccursBefore(refreshDevices, "_context.EnumerateCaptureDeviceDiscoveryAsync()", "ApplySuccessfulDeviceScanAsync(");

        return Task.CompletedTask;
    }
}
