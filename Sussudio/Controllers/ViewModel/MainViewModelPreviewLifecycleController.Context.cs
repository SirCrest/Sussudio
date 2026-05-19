using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed class MainViewModelPreviewLifecycleControllerContext
    {
        public required CaptureSessionCoordinator SessionCoordinator { get; init; }
        public required Func<CaptureSettings> BuildCaptureSettings { get; init; }
        public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
        public required Func<CancellationToken, Task> RampPreviewVolumeDownForStopAsync { get; init; }
        public required Func<MainViewModelPreviewLifecycleController, MainViewModelPreviewReinitializeController> CreateReinitializeController { get; init; }
        public required Func<CaptureDevice?> SelectedDevice { get; init; }
        public required Action<CaptureDevice> SetSelectedDevice { get; init; }
        public required Func<bool> IsInitialized { get; init; }
        public required Action<bool> SetIsInitialized { get; init; }
        public required Func<bool> IsPreviewing { get; init; }
        public required Action<bool> SetIsPreviewing { get; init; }
        public required Func<bool> IsPreviewReinitializing { get; init; }
        public required Func<bool> IsRecording { get; init; }
        public required Func<bool> ShouldStartAudioPreview { get; init; }
        public required Func<bool> IsAudioPreviewActive { get; init; }
        public required Action<string> SetStatusText { get; init; }
        public required Action RaisePreviewStartRequested { get; init; }
        public required Action RaisePreviewStopRequested { get; init; }
        public required Action ApplyLatestSourceTelemetryForPreviewStart { get; init; }
    }
}
