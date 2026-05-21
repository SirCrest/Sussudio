using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the device refresh controller.
/// </summary>
internal sealed class MainViewModelDeviceRefreshControllerContext
{
    public required Action<string> SetStatusText { get; init; }
    public required Func<long> IncrementDeviceScanGeneration { get; init; }
    public required Func<string?> GetSelectedAudioInputDeviceId { get; init; }
    public required Func<string?> GetSelectedMicrophoneDeviceId { get; init; }
    public required Func<string?> GetSelectedDeviceId { get; init; }
    public required Func<Task<DeviceService.DeviceDiscoveryResult>> EnumerateCaptureDeviceDiscoveryAsync { get; init; }
    public required Action<List<AudioInputDevice>, IReadOnlyList<CaptureDevice>, string?, string?, string?> ApplyStartupAudioDeviceScan { get; init; }
    public required Action<IReadOnlyList<CaptureDevice>> ReplaceDevices { get; init; }
    public required Func<IList<CaptureDevice>> GetDevices { get; init; }
    public required Action<CaptureDevice, long> BeginBackgroundFormatProbe { get; init; }
    public required Func<string> GetLastDiscoverySummary { get; init; }
    public required Action<CaptureDevice?> SetSelectedDevice { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Func<string?> GetPendingSavedDeviceId { get; init; }
    public required Action<string?> SetPendingSavedDeviceId { get; init; }
}
