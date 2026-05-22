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
        var transitionExecutionText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.TransitionExecution.cs").Replace("\r\n", "\n");
        var stateMachineText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionStateMachine.cs").Replace("\r\n", "\n");
        var resourceReleaseText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.ResourceRelease.cs").Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Cleanup.cs").Replace("\r\n", "\n");
        var flashbackBackendFailureCleanupPath = "Sussudio/Services/Capture/CaptureService.FlashbackBackendFailureCleanup.cs";
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), flashbackBackendFailureCleanupPath.Replace('/', Path.DirectorySeparatorChar))),
            "CaptureService Flashback backend failure cleanup partial exists");
        var failureCleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FailureCleanup.cs").Replace("\r\n", "\n");
        var flashbackBackendFailureCleanupText = ReadRepoFile(flashbackBackendFailureCleanupPath).Replace("\r\n", "\n");

        AssertContains(rootText, "private readonly CaptureSessionStateMachine _sessionStateMachine = new();");
        AssertContains(rootText, "public CaptureSessionState SessionState => CurrentSessionState;");
        AssertContains(transitionExecutionText, "private async Task RunTransitionAsync(");
        AssertContains(transitionExecutionText, "await _sessionTransitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(transitionExecutionText, "ReleaseSemaphoreBestEffort(_sessionTransitionLock, \"session_transition\");");
        AssertContains(transitionExecutionText, "private void EnterTransitionState(CaptureSessionState transitionState)");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.EnterTransition(transitionState);");
        AssertContains(transitionExecutionText, "private void ResolveSessionSteadyState()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.ResolveSteadyState(BuildSteadyStateInputs());");
        AssertContains(transitionExecutionText, "private CaptureSessionState CurrentSessionState");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.State;");
        AssertContains(transitionExecutionText, "private long CurrentSessionGeneration");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.Generation;");
        AssertContains(transitionExecutionText, "private CaptureSessionSteadyStateInputs BuildSteadyStateInputs()");
        AssertContains(transitionExecutionText, "private void EnterCleanupState()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.EnterCleanup();");
        AssertContains(transitionExecutionText, "private void EnterFaultedState()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.EnterFaulted();");
        AssertContains(transitionExecutionText, "private void EnterDisposedState()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.EnterDisposed();");
        AssertContains(transitionExecutionText, "private void ResetSessionStateAfterCleanup()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.ResetAfterCleanup(_isDisposed != 0);");
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
        AssertDoesNotContain(transitionExecutionText, "CleanupForDisposalAsync");
        AssertDoesNotContain(transitionExecutionText, "public void Dispose()");
        AssertDoesNotContain(transitionExecutionText, "public async ValueTask DisposeAsync()");
        AssertDoesNotContain(transitionExecutionText, "private static void ReleaseSemaphoreBestEffort(");
        AssertDoesNotContain(transitionExecutionText, "private static void ResumeFlashbackEvictionBestEffort(");
        AssertOccursBefore(
            stateMachineText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);",
            "_state = transitionState;");
        AssertContains(cleanupText, "private async Task CleanupForDisposalAsync()");
        AssertContains(cleanupText, "EnterCleanupState();");
        AssertContains(cleanupText, "await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertContains(cleanupText, "public void Dispose()");
        AssertContains(cleanupText, "public async ValueTask DisposeAsync()");
        AssertDoesNotContain(cleanupText, "private void DisposeCoordinationLocksBestEffort()");
        AssertDoesNotContain(cleanupText, "private static void DisposeSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(resourceReleaseText, "private void DisposeCoordinationLocksBestEffort()");
        AssertContains(resourceReleaseText, "private static void DisposeSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(resourceReleaseText, "private static void ReleaseSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(resourceReleaseText, "private void ReleaseFlashbackBackendLeaseIfHeld(ref bool backendLeaseHeld)");
        AssertContains(resourceReleaseText, "private void ReleaseFlashbackExportOperationLockIfHeld(ref bool exportOperationLockHeld)");
        AssertContains(resourceReleaseText, "private static void ResumeFlashbackEvictionBestEffort(FlashbackBufferManager? bufferManager, string operation)");
        AssertContains(resourceReleaseText, "CAPTURE_SERVICE_SEMAPHORE_RELEASE_WARN");
        AssertContains(resourceReleaseText, "CAPTURE_SERVICE_SEMAPHORE_DISPOSE_WARN");
        AssertContains(resourceReleaseText, "FLASHBACK_EVICTION_RESUME_WARN");
        AssertContains(cleanupText, "EnterDisposedState();");
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
