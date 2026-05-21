using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the capture settings automation controller.
/// </summary>
internal sealed class MainViewModelCaptureSettingsAutomationControllerContext
{
    public required Func<Func<bool>, CancellationToken, Task<bool>> InvokeBooleanOnUiThreadAsync { get; init; }
    public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
    public required Func<IEnumerable<ResolutionOption>> GetAvailableResolutions { get; init; }
    public required Func<IEnumerable<FrameRateOption>> GetAvailableFrameRates { get; init; }
    public required Func<IEnumerable<string>> GetAvailableVideoFormats { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Action<string?> SetSelectedResolution { get; init; }
    public required Action<double> SetSelectedFrameRate { get; init; }
    public required Action<string> SetSelectedVideoFormat { get; init; }
    public required Action<int> SetMjpegDecoderCount { get; init; }
    public required Action SelectAutoFrameRate { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
    public required Func<string, Task> ReinitializeDeviceAsync { get; init; }
}
