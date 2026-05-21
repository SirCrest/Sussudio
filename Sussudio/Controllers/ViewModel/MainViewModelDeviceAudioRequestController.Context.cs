using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed class MainViewModelDeviceAudioRequestControllerContext
{
    public required Func<Func<Task>, string, bool, bool> EnqueueUiOperation { get; init; }
    public required Func<bool> IsDisposing { get; init; }
    public required Func<bool> IsLoadingSettings { get; init; }
    public required Func<bool> IsRefreshingDeviceAudioControls { get; init; }
    public required Func<bool> IsDeviceAudioControlSupported { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<string> GetSelectedDeviceAudioMode { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Action SaveSettings { get; init; }
    public required Func<CaptureDevice?, bool, CancellationToken, Task> RefreshDeviceAudioControlsAsync { get; init; }
    public required Func<string, CaptureDevice?, CancellationToken, Task<bool>> ApplyDeviceAudioModeAsync { get; init; }
    public required Func<string, CaptureDevice?, CancellationToken, Task<bool>> ApplyAnalogAudioGainAsync { get; init; }
    public required Func<CaptureDevice, bool> IsCurrentSelectedDevice { get; init; }
}
