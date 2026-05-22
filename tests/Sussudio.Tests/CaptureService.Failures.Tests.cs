using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public sealed class CaptureServiceFailureOwnershipTests
{
    [Fact]
    public void CaptureService_LastFailureTelemetryState_LivesInFailuresPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var failuresText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Failures.cs")
            .Replace("\r\n", "\n");

        var fieldNames = new[]
        {
            "_recordingFailureTelemetryLock",
            "_lastRecordingEncodingFailed",
            "_lastRecordingEncodingFailureType",
            "_lastRecordingEncodingFailureMessage",
            "_lastFlashbackEncodingFailed",
            "_lastFlashbackEncodingFailureType",
            "_lastFlashbackEncodingFailureMessage",
        };

        foreach (var fieldName in fieldNames)
        {
            AssertDoesNotContain(rootText, fieldName);
            AssertContains(failuresText, fieldName);
        }

        AssertContains(failuresText, "private readonly object _recordingFailureTelemetryLock = new();");
        AssertContains(failuresText, "private bool _lastRecordingEncodingFailed;");
        AssertContains(failuresText, "private string? _lastRecordingEncodingFailureType;");
        AssertContains(failuresText, "private string? _lastRecordingEncodingFailureMessage;");
        AssertContains(failuresText, "private bool _lastFlashbackEncodingFailed;");
        AssertContains(failuresText, "private string? _lastFlashbackEncodingFailureType;");
        AssertContains(failuresText, "private string? _lastFlashbackEncodingFailureMessage;");
        AssertContains(failuresText, "private void RecordLastRecordingFailure(Exception ex)");
        AssertContains(failuresText, "private void RecordLastFlashbackFailure(Exception ex)");
        AssertContains(failuresText, "private void ClearLastRecordingFailure()");
        AssertContains(failuresText, "private void ClearLastFlashbackFailure()");
        AssertContains(failuresText, "private void BeginFatalCaptureCleanup(Exception ex)");
        AssertContains(failuresText, "EnterCleanupState();");
        AssertContains(failuresText, "EnterFaultedState();");
        AssertContains(failuresText, "GetLastFailureTelemetry()");
    }

    [Fact]
    public void CaptureService_FlashbackBackendFailureCleanup_LivesInFocusedPartialWithoutSessionStateWrites()
    {
        var failuresText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Failures.cs")
            .Replace("\r\n", "\n");
        var flashbackBackendFailureCleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBackendFailureCleanup.cs")
            .Replace("\r\n", "\n");

        AssertContains(failuresText, "private void BeginFatalCaptureCleanup(Exception ex)");
        AssertDoesNotContain(failuresText, "IsGpuDeviceLost(");
        AssertContains(flashbackBackendFailureCleanupText, "private void BeginFlashbackBackendCleanup(Exception ex)");
        AssertContains(flashbackBackendFailureCleanupText, "private static bool IsGpuDeviceLost(Exception ex)");
        AssertDoesNotContain(flashbackBackendFailureCleanupText, "_sessionState =");
    }

    private static void AssertContains(string text, string expected)
        => Assert.Contains(expected, text);

    private static void AssertDoesNotContain(string text, string expected)
        => Assert.DoesNotContain(expected, text);

    private static string ReadRepoFile(string relativePath)
    {
        var path = Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Sussudio.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Sussudio repo root.");
    }
}

static partial class Program
{
    internal static async Task CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);
        SetPrivateField(captureService, "_isVideoPreviewActive", true);
        SetPrivateField(captureService, "_isAudioPreviewActive", true);
        SetPrivateField(captureService, "_isRecording", true);

        InvokeNonPublicInstanceMethod(
            captureService,
            "OnUnifiedVideoCaptureFatalError",
            new object?[] { null, new InvalidOperationException("synthetic hfr failure") });

        await WaitForConditionAsync(
            () =>
                string.Equals(GetPropertyValue(captureService, "SessionState")?.ToString(), "Faulted", StringComparison.Ordinal) &&
                !GetBoolProperty(captureService, "IsInitialized") &&
                !GetBoolProperty(captureService, "IsVideoPreviewActive") &&
                !GetBoolProperty(captureService, "IsAudioPreviewActive") &&
                !GetBoolProperty(captureService, "IsRecording"),
            "CaptureService fatal cleanup").ConfigureAwait(false);

        AssertEqual("Faulted", GetPropertyValue(captureService, "SessionState")?.ToString(), "SessionState");
        AssertEqual(false, GetBoolProperty(captureService, "IsInitialized"), "IsInitialized");
        AssertEqual(false, GetBoolProperty(captureService, "IsVideoPreviewActive"), "IsVideoPreviewActive");
        AssertEqual(false, GetBoolProperty(captureService, "IsAudioPreviewActive"), "IsAudioPreviewActive");
        AssertEqual(false, GetBoolProperty(captureService, "IsRecording"), "IsRecording");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }
}
