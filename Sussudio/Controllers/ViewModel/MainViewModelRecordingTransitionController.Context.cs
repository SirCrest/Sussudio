using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the recording transition controller.
/// </summary>
internal sealed class MainViewModelRecordingTransitionControllerContext
{
    public required Func<bool> IsRecording { get; init; }
    public required Action<bool> SetIsRecording { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Func<bool> HasSelectedDevice { get; init; }
    public required Func<string> GetStatusText { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Action<bool> SetIsRecordingTransitioning { get; init; }
    public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
    public required Func<CaptureSettings> BuildCaptureSettings { get; init; }
    public required Func<CaptureSettings, CancellationToken, Task> StartRecordingAsync { get; init; }
    public required Func<CancellationToken, Task> StopRecordingAsync { get; init; }
    public required Func<bool> GetSessionIsRecording { get; init; }
    public required Action RestartRecordingStopwatch { get; init; }
    public required Action StopRecordingStopwatch { get; init; }
    public required Action ClearRecordingBitrateSamples { get; init; }
    public required Action<string> SetRecordingSizeInfo { get; init; }
    public required Action<string> SetRecordingBitrateInfo { get; init; }
    public required Func<string> GetRecordingTime { get; init; }
}
