using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    public void CaptureService_FlashbackBackendFailureCleanup_LivesWithFailureCallbacksWithoutSessionStateWrites()
    {
        var failuresText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Failures.cs")
            .Replace("\r\n", "\n");

        AssertContains(failuresText, "private void BeginFatalCaptureCleanup(Exception ex)");
        AssertContains(failuresText, "private void BeginFlashbackBackendCleanup(Exception ex)");
        AssertContains(failuresText, "private static bool IsGpuDeviceLost(Exception ex)");
        AssertContains(failuresText, "_flashbackBackend.PreserveRecoverySegments(\"device_lost\");");
        AssertDoesNotContain(failuresText, "_sessionState =");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackBackendFailureCleanup.cs")),
            "Flashback backend failure cleanup partial folded into CaptureService.Failures.cs");
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
    internal static Task CaptureService_InitializationLivesWithServiceRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var telemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Telemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private static ISourceSignalTelemetryProvider CreateDefaultTelemetryProvider()");
        AssertContains(rootText, "public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "=> RunTransitionAsync(CaptureSessionState.Initializing, async transitionToken =>");
        AssertContains(rootText, "_audioDeviceId = settings.UseCustomAudioInput ? settings.AudioDeviceId : device.AudioDeviceId;");
        AssertContains(rootText, "_actualPixelFormat = settings.RequestedPixelFormat ?? (settings.HdrEnabled ? \"P010\" : \"NV12\");");
        AssertContains(rootText, "ResetObservedPixelTelemetry();");
        AssertContains(rootText, "ResetCachedMjpegTimingMetrics();");
        AssertContains(rootText, "_latestSourceTelemetry = BuildFallbackTelemetry();");
        AssertContains(rootText, "await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);");
        AssertContains(rootText, "TryCorrectFrameRateFromTelemetry();");
        AssertContains(rootText, "StatusChanged?.Invoke(this, \"Initialized\");");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Initialization.cs")),
            "old initialization partial removed");
        AssertContains(telemetryText, "private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()");
        AssertContains(telemetryText, "private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(");
        AssertContains(telemetryText, "private void TryCorrectFrameRateFromTelemetry()");
        AssertContains(telemetryText, "private static string ResolveFrameRateArg(");
        AssertContains(telemetryText, "private void CaptureEncoderRuntimeTelemetry(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.TelemetryFallback.cs")),
            "old telemetry fallback partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.CaptureFormatTelemetry.cs")),
            "old capture-format telemetry partial removed");

        return Task.CompletedTask;
    }

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
        var transitionExecutionText = rootText;
        var stateMachineText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs").Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Cleanup.cs").Replace("\r\n", "\n");
        var resourceReleaseText = cleanupText;
        var failuresText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Failures.cs").Replace("\r\n", "\n");

        AssertContains(rootText, "private readonly CaptureSessionStateMachine _sessionStateMachine = new();");
        AssertContains(rootText, "public CaptureSessionState SessionState => CurrentSessionState;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.TransitionExecution.cs")),
            "CaptureService transition transaction helpers stay folded into CaptureService.cs");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionStateMachine.cs")),
            "mutable capture session state machine lives with capture model owner");
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
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.ResourceRelease.cs")), "CaptureService resource-release helpers stay folded into Cleanup.cs");
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

        var fatalCleanupText = ExtractMemberCode(failuresText, "BeginFatalCaptureCleanup");
        AssertContains(fatalCleanupText, "EnterCleanupState();");
        AssertContains(fatalCleanupText, "EnterFaultedState();");
        AssertDoesNotContain(failuresText, "_sessionState =");
        AssertContains(failuresText, "private void BeginFlashbackBackendCleanup(Exception ex)");
        AssertContains(failuresText, "private static bool IsGpuDeviceLost(Exception ex)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackBackendFailureCleanup.cs")),
            "CaptureService Flashback backend failure cleanup partial folded into failures");

        return Task.CompletedTask;
    }

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
