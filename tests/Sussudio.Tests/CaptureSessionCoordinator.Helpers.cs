using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static string ReadCaptureSessionCoordinatorSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Models.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Commands.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.Playback.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Queue.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.QueueExecution.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Snapshot.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Disposal.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static void AssertCanEnterTransition(
        MethodInfo canEnter,
        Type stateType,
        string currentState,
        string transitionState,
        bool expected)
    {
        var actual = canEnter.Invoke(
            null,
            new[] { Enum.Parse(stateType, currentState), Enum.Parse(stateType, transitionState) });
        AssertEqual(expected, (bool)actual!, $"{currentState} -> {transitionState}");
    }

    private static object ResolveState(
        MethodInfo resolve,
        bool isDisposed,
        bool isRecording,
        bool isVideoPreviewActive,
        bool isAudioPreviewActive,
        bool isInitialized)
        => resolve.Invoke(
            null,
            new object[]
            {
                isDisposed,
                isRecording,
                isVideoPreviewActive,
                isAudioPreviewActive,
                isInitialized
            })
           ?? throw new InvalidOperationException("ResolveSteadyState returned null.");

    private sealed record CaptureSessionCoordinatorHarness(
        object Coordinator,
        object CaptureService,
        Type CommandKindType,
        MethodInfo EnqueueMethod);

    private static CaptureSessionCoordinatorHarness CreateCaptureSessionCoordinatorHarness()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");
        var captureServiceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var commandKindType = RequireType("Sussudio.Services.Capture.CaptureCommandKind");
        var captureService = Activator.CreateInstance(captureServiceType)
            ?? throw new InvalidOperationException("Failed to create CaptureService.");
        var coordinator = Activator.CreateInstance(coordinatorType, captureService)
            ?? throw new InvalidOperationException("Failed to create CaptureSessionCoordinator.");
        var enqueueMethod = coordinatorType.GetMethod("EnqueueAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureSessionCoordinator.EnqueueAsync not found.");
        return new CaptureSessionCoordinatorHarness(coordinator, captureService, commandKindType, enqueueMethod);
    }

    private static Task EnqueueCoordinatorOperation(
        CaptureSessionCoordinatorHarness harness,
        string commandKind,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default,
        bool coalesceLatest = false,
        bool propagateCancellationToOperation = false)
    {
        var kind = Enum.Parse(harness.CommandKindType, commandKind);
        return (Task)(harness.EnqueueMethod.Invoke(
                   harness.Coordinator,
                   new object?[]
                   {
                       kind,
                       operation,
                       cancellationToken,
                       coalesceLatest,
                       propagateCancellationToOperation
                   })
               ?? throw new InvalidOperationException("CaptureSessionCoordinator.EnqueueAsync returned null."));
    }

    private static object GetCoordinatorSnapshot(object coordinator)
        => GetPropertyValue(coordinator, "Snapshot")
           ?? throw new InvalidOperationException("CaptureSessionCoordinator.Snapshot returned null.");

    private static async Task DisposeCaptureSessionCoordinatorHarnessAsync(CaptureSessionCoordinatorHarness harness)
    {
        await InvokeDisposeAsync(harness.Coordinator).ConfigureAwait(false);
        await InvokeDisposeAsync(harness.CaptureService).ConfigureAwait(false);
    }

    private static async Task InvokeDisposeAsync(object target)
    {
        var disposeAsync = target.GetType().GetMethod("DisposeAsync", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{target.GetType().Name}.DisposeAsync not found.");
        var result = disposeAsync.Invoke(target, Array.Empty<object?>());
        switch (result)
        {
            case ValueTask valueTask:
                await valueTask.ConfigureAwait(false);
                return;
            case Task task:
                await task.ConfigureAwait(false);
                return;
            default:
                throw new InvalidOperationException($"{target.GetType().Name}.DisposeAsync returned unsupported result.");
        }
    }

    private static async Task AssertTaskCanceledAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new InvalidOperationException("Expected task to be canceled.");
    }
}
