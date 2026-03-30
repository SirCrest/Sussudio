using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    // ── CaptureSessionCoordinator: API surface contract ──

    private static Task CaptureSessionCoordinator_HasExpectedPublicMethods()
    {
        var coordinatorType = RequireType("ElgatoCapture.Services.Capture.CaptureSessionCoordinator");

        // Core lifecycle methods
        var expectedMethods = new[]
        {
            "InitializeAsync",
            "StartVideoPreviewAsync",
            "StopVideoPreviewAsync",
            "StartRecordingAsync",
            "StopRecordingAsync",
            "CleanupAsync",
            "StartAudioPreviewAsync",
            "UpdateAudioMonitoringAsync",
            "UpdateAudioInputAsync"
        };

        foreach (var methodName in expectedMethods)
        {
            var method = coordinatorType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            AssertNotNull(method, $"CaptureSessionCoordinator.{methodName}");
        }

        // Snapshot property
        var snapshotProp = coordinatorType.GetProperty("Snapshot", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(snapshotProp, "CaptureSessionCoordinator.Snapshot");

        // Implements IDisposable and IAsyncDisposable
        AssertEqual(true, typeof(IDisposable).IsAssignableFrom(coordinatorType),
            "Implements IDisposable");
        AssertEqual(true, typeof(IAsyncDisposable).IsAssignableFrom(coordinatorType),
            "Implements IAsyncDisposable");

        return Task.CompletedTask;
    }

    // ── CaptureSessionCoordinator: CaptureCommand shape ──

    private static Task CaptureSessionCoordinator_CaptureCommandKind_HasExpectedValues()
    {
        var commandKindType = RequireType("ElgatoCapture.Services.Capture.CaptureCommandKind");

        // Core command kinds should exist
        var expectedValues = new[]
        {
            "Initialize", "StartVideoPreview", "StopVideoPreview",
            "StartRecording", "StopRecording", "Cleanup"
        };

        foreach (var value in expectedValues)
        {
            var parsed = Enum.Parse(commandKindType, value);
            AssertNotNull(parsed, $"CaptureCommandKind.{value}");
        }

        return Task.CompletedTask;
    }

    // ── CaptureSessionCoordinator: CaptureSessionSnapshot ──

    private static Task CaptureSessionCoordinator_CaptureSessionSnapshot_HasFullContract()
    {
        var snapshotType = RequireType("ElgatoCapture.Services.Capture.CaptureSessionSnapshot");

        var expectedProps = new[]
        {
            "LastTransitionUtc", "LastCommand", "LastCorrelationId",
            "LastError", "PendingCommands", "SessionState",
            "IsRecording", "IsInitialized", "IsVideoPreviewActive", "IsAudioPreviewActive"
        };

        foreach (var prop in expectedProps)
        {
            var propInfo = snapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            AssertNotNull(propInfo, $"CaptureSessionSnapshot.{prop}");
        }

        // Default state should be clean
        var snapshot = Activator.CreateInstance(snapshotType)!;
        AssertEqual(false, GetBoolProperty(snapshot, "IsRecording"), "Default IsRecording");
        AssertEqual(false, GetBoolProperty(snapshot, "IsInitialized"), "Default IsInitialized");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(snapshot, "PendingCommands")), "Default PendingCommands");

        return Task.CompletedTask;
    }
}
