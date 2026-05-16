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
            ["CaptureService.Coordination.cs"] = 7,
            ["CaptureService.Cleanup.cs"] = 1,
            ["CaptureService.Failures.cs"] = 2
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
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Cleanup.cs").Replace("\r\n", "\n");
        var failuresText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Failures.cs").Replace("\r\n", "\n");

        AssertContains(rootText, "private CaptureSessionState _sessionState = CaptureSessionState.Uninitialized;");
        AssertContains(coordinationText, "_sessionState = transitionState;");
        AssertContains(coordinationText, "_sessionState = ResolveSteadyState();");
        AssertContains(coordinationText, "_sessionState = CaptureSessionState.Faulted;");
        AssertContains(coordinationText, "_sessionState = CaptureSessionState.CleaningUp;");
        AssertContains(coordinationText, "_sessionState = CaptureSessionState.Disposed;");
        AssertOccursBefore(
            coordinationText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_sessionState, transitionState);",
            "_sessionState = transitionState;");
        AssertContains(
            cleanupText,
            "_sessionState = _isDisposed != 0 ? CaptureSessionState.Disposed : CaptureSessionState.Uninitialized;");

        var fatalCleanupText = ExtractMemberCode(failuresText, "BeginFatalCaptureCleanup");
        var flashbackBackendCleanupText = ExtractMemberCode(failuresText, "BeginFlashbackBackendCleanup");
        AssertContains(fatalCleanupText, "_sessionState = CaptureSessionState.CleaningUp;");
        AssertContains(fatalCleanupText, "_sessionState = CaptureSessionState.Faulted;");
        AssertDoesNotContain(flashbackBackendCleanupText, "_sessionState =");

        return Task.CompletedTask;
    }
}
