using System;
using System.Threading.Tasks;

namespace Sussudio.Controllers;

internal sealed class MainViewModelDisposalControllerContext
{
    public required Func<bool> TryBeginDispose { get; init; }
    public required Action CancelActiveFlashbackExport { get; init; }
    public required Action CancelPendingAudioControlWork { get; init; }
    public required Action StopRuntimeForDispose { get; init; }
    public required Func<Task> CleanupSessionCoordinatorAsync { get; init; }
    public required Func<Task> DisposeSessionCoordinatorAsync { get; init; }
    public required Func<Task> DisposeCaptureServiceAsync { get; init; }
    public required Action DisposeCaptureService { get; init; }
    public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }
}
