using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the late device-format probe controller.
/// </summary>
internal sealed class MainViewModelDeviceFormatProbeControllerContext
{
    public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
    public required Func<long> ReadDeviceScanGeneration { get; init; }
    public required Func<string, CaptureDevice?> FindDeviceById { get; init; }
    public required Action<bool> SetPendingSdrAutoSelectionForDeviceChange { get; init; }
    public required Action<int?> SetPendingSdrAutoFriendlyFrameRateBucket { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Func<double> GetSelectedFrameRate { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
    public required Action<CaptureDevice, bool> RebuildSelectedDeviceCapabilities { get; init; }
    public required Func<MainViewModelDeviceFormatProbeRetargetApplier> CreateRetargetApplier { get; init; }
}
