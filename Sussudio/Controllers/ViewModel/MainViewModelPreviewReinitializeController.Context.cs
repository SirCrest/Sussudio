using System;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed class MainViewModelPreviewReinitializeControllerContext
    {
        public required Func<CaptureDevice?> SelectedDevice { get; init; }
        public required Func<MediaFormat?> SelectedFormat { get; init; }
        public required Func<bool> IsRecording { get; init; }
        public required Func<bool> IsInitialized { get; init; }
        public required Action<bool> SetIsInitialized { get; init; }
        public required Func<bool> IsPreviewing { get; init; }
        public required Func<bool> IsPreviewReinitializing { get; init; }
        public required Action<bool> SetIsPreviewReinitializing { get; init; }
        public required Action<string> SetStatusText { get; init; }
        public required Func<bool> CancelPreviewRestartAfterReinitialize { get; init; }
        public required Action<bool> SetCancelPreviewRestartAfterReinitialize { get; init; }
        public required Func<int> IncrementReinitializeGeneration { get; init; }
        public required Func<int> ReadReinitializeGeneration { get; init; }
        public required Func<Task?> PendingFlashbackCycleTask { get; init; }
        public required Action<Task> ClearPendingFlashbackCycleIfSameAndCompleted { get; init; }
        public required Func<Task> WaitReinitializeGateAsync { get; init; }
        public required Action ReleaseReinitializeGate { get; init; }
        public required Func<string, Task> NotifyPreviewReinitRequestedAsync { get; init; }
        public required Func<Task> NotifyRendererStopAsync { get; init; }
    }
}
