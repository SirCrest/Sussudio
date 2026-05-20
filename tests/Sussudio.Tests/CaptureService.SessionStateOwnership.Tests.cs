using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_SessionStateWritesRouteThroughCoordination()
    {
        var captureServiceFiles = Directory
            .GetFiles(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture"), "CaptureService*.cs")
            .Select(path => new
            {
                FileName = Path.GetFileName(path),
                RelativePath = Path.GetRelativePath(GetRepoRoot(), path).Replace('\\', '/')
            })
            .ToArray();

        var directWriterCount = captureServiceFiles.Sum(file => Regex.Matches(
            ReadRepoCodeWithoutCommentsOrStrings(file.RelativePath),
            @"\b_sessionState\s*=").Count);

        AssertEqual(0, directWriterCount, "CaptureService direct _sessionState writer count");

        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");
        var coordinationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Coordination.cs").Replace("\r\n", "\n");
        var stateMachineText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionStateMachine.cs").Replace("\r\n", "\n");
        var resourceReleaseText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.ResourceRelease.cs").Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Cleanup.cs").Replace("\r\n", "\n");
        var disposalLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.DisposalLifecycle.cs").Replace("\r\n", "\n");
        var flashbackBackendFailureCleanupPath = "Sussudio/Services/Capture/CaptureService.FlashbackBackendFailureCleanup.cs";
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), flashbackBackendFailureCleanupPath.Replace('/', Path.DirectorySeparatorChar))),
            "CaptureService Flashback backend failure cleanup partial exists");
        var failureCleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FailureCleanup.cs").Replace("\r\n", "\n");
        var flashbackBackendFailureCleanupText = ReadRepoFile(flashbackBackendFailureCleanupPath).Replace("\r\n", "\n");

        AssertContains(rootText, "private readonly CaptureSessionStateMachine _sessionStateMachine = new();");
        AssertContains(rootText, "public CaptureSessionState SessionState => CurrentSessionState;");
        AssertContains(coordinationText, "private void EnterTransitionState(CaptureSessionState transitionState)");
        AssertContains(coordinationText, "=> _sessionStateMachine.EnterTransition(transitionState);");
        AssertContains(coordinationText, "private void ResolveSessionSteadyState()");
        AssertContains(coordinationText, "=> _sessionStateMachine.ResolveSteadyState(BuildSteadyStateInputs());");
        AssertContains(coordinationText, "private CaptureSessionState CurrentSessionState");
        AssertContains(coordinationText, "=> _sessionStateMachine.State;");
        AssertContains(coordinationText, "private long CurrentSessionGeneration");
        AssertContains(coordinationText, "=> _sessionStateMachine.Generation;");
        AssertContains(coordinationText, "private CaptureSessionSteadyStateInputs BuildSteadyStateInputs()");
        AssertContains(coordinationText, "private void EnterCleanupState()");
        AssertContains(coordinationText, "=> _sessionStateMachine.EnterCleanup();");
        AssertContains(coordinationText, "private void EnterFaultedState()");
        AssertContains(coordinationText, "=> _sessionStateMachine.EnterFaulted();");
        AssertContains(coordinationText, "private void EnterDisposedState()");
        AssertContains(coordinationText, "=> _sessionStateMachine.EnterDisposed();");
        AssertContains(coordinationText, "private void ResetSessionStateAfterCleanup()");
        AssertContains(coordinationText, "=> _sessionStateMachine.ResetAfterCleanup(_isDisposed != 0);");
        AssertContains(stateMachineText, "internal sealed class CaptureSessionStateMachine");
        AssertContains(stateMachineText, "private CaptureSessionState _state = CaptureSessionState.Uninitialized;");
        AssertContains(stateMachineText, "private long _generation;");
        AssertContains(stateMachineText, "public long Generation => Interlocked.Read(ref _generation);");
        AssertContains(stateMachineText, "public void EnterTransition(CaptureSessionState transitionState)");
        AssertContains(stateMachineText, "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);");
        AssertContains(stateMachineText, "Interlocked.Increment(ref _generation);");
        AssertContains(stateMachineText, "_state = transitionState;");
        AssertContains(stateMachineText, "public void ResolveSteadyState(CaptureSessionSteadyStateInputs inputs)");
        AssertContains(stateMachineText, "=> _state = CaptureSessionTransitionPolicy.ResolveSteadyState(");
        AssertContains(stateMachineText, "public void ResetAfterCleanup(bool isDisposed)");
        AssertContains(stateMachineText, "=> _state = isDisposed ? CaptureSessionState.Disposed : CaptureSessionState.Uninitialized;");
        AssertDoesNotContain(coordinationText, "CleanupForDisposalAsync");
        AssertDoesNotContain(coordinationText, "public void Dispose()");
        AssertDoesNotContain(coordinationText, "public async ValueTask DisposeAsync()");
        AssertDoesNotContain(coordinationText, "private static void ReleaseSemaphoreBestEffort(");
        AssertDoesNotContain(coordinationText, "private static void ResumeFlashbackEvictionBestEffort(");
        AssertOccursBefore(
            stateMachineText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);",
            "_state = transitionState;");
        AssertContains(disposalLifecycleText, "private async Task CleanupForDisposalAsync()");
        AssertContains(disposalLifecycleText, "EnterCleanupState();");
        AssertContains(disposalLifecycleText, "await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertContains(disposalLifecycleText, "public void Dispose()");
        AssertContains(disposalLifecycleText, "public async ValueTask DisposeAsync()");
        AssertDoesNotContain(disposalLifecycleText, "private void DisposeCoordinationLocksBestEffort()");
        AssertDoesNotContain(disposalLifecycleText, "private static void DisposeSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(resourceReleaseText, "private void DisposeCoordinationLocksBestEffort()");
        AssertContains(resourceReleaseText, "private static void DisposeSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(resourceReleaseText, "private static void ReleaseSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(resourceReleaseText, "private void ReleaseFlashbackBackendLeaseIfHeld(ref bool backendLeaseHeld)");
        AssertContains(resourceReleaseText, "private void ReleaseFlashbackExportOperationLockIfHeld(ref bool exportOperationLockHeld)");
        AssertContains(resourceReleaseText, "private static void ResumeFlashbackEvictionBestEffort(FlashbackBufferManager? bufferManager, string operation)");
        AssertContains(resourceReleaseText, "CAPTURE_SERVICE_SEMAPHORE_RELEASE_WARN");
        AssertContains(resourceReleaseText, "CAPTURE_SERVICE_SEMAPHORE_DISPOSE_WARN");
        AssertContains(resourceReleaseText, "FLASHBACK_EVICTION_RESUME_WARN");
        AssertContains(disposalLifecycleText, "EnterDisposedState();");
        AssertDoesNotContain(disposalLifecycleText, "_sessionState =");
        AssertContains(
            cleanupText,
            "ResetSessionStateAfterCleanup();");
        AssertDoesNotContain(cleanupText, "_sessionState =");

        var fatalCleanupText = ExtractMemberCode(failureCleanupText, "BeginFatalCaptureCleanup");
        AssertContains(fatalCleanupText, "EnterCleanupState();");
        AssertContains(fatalCleanupText, "EnterFaultedState();");
        AssertDoesNotContain(failureCleanupText, "_sessionState =");
        AssertDoesNotContain(failureCleanupText, "BeginFlashbackBackendCleanup(");
        AssertDoesNotContain(failureCleanupText, "IsGpuDeviceLost(");
        AssertContains(flashbackBackendFailureCleanupText, "private void BeginFlashbackBackendCleanup(Exception ex)");
        AssertContains(flashbackBackendFailureCleanupText, "private static bool IsGpuDeviceLost(Exception ex)");
        AssertDoesNotContain(flashbackBackendFailureCleanupText, "_sessionState =");

        return Task.CompletedTask;
    }
}
