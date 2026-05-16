using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureService_SessionStateWritersStayInLifecyclePartials()
    {
        var captureServiceFiles = Directory
            .GetFiles(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture"), "CaptureService*.cs")
            .Select(path => new
            {
                FileName = Path.GetFileName(path),
                RelativePath = Path.GetRelativePath(GetRepoRoot(), path).Replace('\\', '/')
            })
            .ToArray();

        var writerCounts = captureServiceFiles.ToDictionary(
            file => file.FileName,
            file => Regex.Matches(
                ReadRepoCodeWithoutCommentsOrStrings(file.RelativePath),
                @"\b_sessionState\s*=").Count,
            StringComparer.Ordinal);

        var expectedWriterCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["CaptureService.cs"] = 1,
            ["CaptureService.Coordination.cs"] = 4,
            ["CaptureService.Cleanup.cs"] = 1,
            ["CaptureService.DisposalLifecycle.cs"] = 3,
            ["CaptureService.FailureCleanup.cs"] = 2
        };

        var actualWriterFiles = writerCounts
            .Where(pair => pair.Value > 0)
            .Select(pair => pair.Key)
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray();
        var expectedWriterFiles = expectedWriterCounts.Keys
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        AssertEqual(
            string.Join("|", expectedWriterFiles),
            string.Join("|", actualWriterFiles),
            "CaptureService _sessionState writer files");
        AssertEqual(11, writerCounts.Values.Sum(), "CaptureService _sessionState total writer count");

        foreach (var expected in expectedWriterCounts)
        {
            AssertEqual(
                expected.Value,
                writerCounts[expected.Key],
                $"CaptureService _sessionState writer count for {expected.Key}");
        }

        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");
        var coordinationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Coordination.cs").Replace("\r\n", "\n");
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

        AssertContains(rootText, "private CaptureSessionState _sessionState = CaptureSessionState.Uninitialized;");
        AssertContains(coordinationText, "_sessionState = transitionState;");
        AssertContains(coordinationText, "_sessionState = ResolveSteadyState();");
        AssertContains(coordinationText, "_sessionState = CaptureSessionState.Faulted;");
        AssertDoesNotContain(coordinationText, "CleanupForDisposalAsync");
        AssertDoesNotContain(coordinationText, "public void Dispose()");
        AssertDoesNotContain(coordinationText, "public async ValueTask DisposeAsync()");
        AssertDoesNotContain(coordinationText, "private static void ReleaseSemaphoreBestEffort(");
        AssertDoesNotContain(coordinationText, "private static void ResumeFlashbackEvictionBestEffort(");
        AssertOccursBefore(
            coordinationText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_sessionState, transitionState);",
            "_sessionState = transitionState;");
        AssertContains(disposalLifecycleText, "private async Task CleanupForDisposalAsync()");
        AssertContains(disposalLifecycleText, "_sessionState = CaptureSessionState.CleaningUp;");
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
        AssertContains(disposalLifecycleText, "_sessionState = CaptureSessionState.Disposed;");
        AssertContains(
            cleanupText,
            "_sessionState = _isDisposed != 0 ? CaptureSessionState.Disposed : CaptureSessionState.Uninitialized;");

        var fatalCleanupText = ExtractMemberCode(failureCleanupText, "BeginFatalCaptureCleanup");
        AssertContains(fatalCleanupText, "_sessionState = CaptureSessionState.CleaningUp;");
        AssertContains(fatalCleanupText, "_sessionState = CaptureSessionState.Faulted;");
        AssertDoesNotContain(failureCleanupText, "BeginFlashbackBackendCleanup(");
        AssertDoesNotContain(failureCleanupText, "IsGpuDeviceLost(");
        AssertContains(flashbackBackendFailureCleanupText, "private void BeginFlashbackBackendCleanup(Exception ex)");
        AssertContains(flashbackBackendFailureCleanupText, "private static bool IsGpuDeviceLost(Exception ex)");
        AssertDoesNotContain(flashbackBackendFailureCleanupText, "_sessionState =");

        return Task.CompletedTask;
    }
}
