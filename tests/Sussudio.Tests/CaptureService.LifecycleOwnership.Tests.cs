using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class CaptureServiceLifecycleOwnershipTests
{
    [Fact]
    public void CaptureService_LastFailureTelemetryState_LivesWithCleanupLifecycle()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
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
            AssertContains(rootText, fieldName);
            AssertContains(cleanupText, fieldName);
        }

        AssertContains(cleanupText, "private readonly object _recordingFailureTelemetryLock = new();");
        AssertContains(cleanupText, "private bool _lastRecordingEncodingFailed;");
        AssertContains(cleanupText, "private string? _lastRecordingEncodingFailureType;");
        AssertContains(cleanupText, "private string? _lastRecordingEncodingFailureMessage;");
        AssertContains(cleanupText, "private bool _lastFlashbackEncodingFailed;");
        AssertContains(cleanupText, "private string? _lastFlashbackEncodingFailureType;");
        AssertContains(cleanupText, "private string? _lastFlashbackEncodingFailureMessage;");
        AssertContains(cleanupText, "private void RecordLastRecordingFailure(Exception ex)");
        AssertContains(cleanupText, "private void RecordLastFlashbackFailure(Exception ex)");
        AssertContains(cleanupText, "private void ClearLastRecordingFailure()");
        AssertContains(cleanupText, "private void ClearLastFlashbackFailure()");
        AssertContains(cleanupText, "private void BeginFatalCaptureCleanup(Exception ex)");
        AssertContains(cleanupText, "EnterCleanupState();");
        AssertContains(cleanupText, "EnterFaultedState();");
        AssertContains(cleanupText, "GetLastFailureTelemetry()");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Failures.cs")),
            "fatal failure cleanup folded into CaptureService.cs");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Cleanup.cs")),
            "cleanup lifecycle folded into CaptureService.cs");
    }

    [Fact]
    public void CaptureService_FlashbackBackendFailureCleanup_LivesWithCleanupLifecycleWithoutSessionStateWrites()
    {
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        AssertContains(cleanupText, "private void BeginFatalCaptureCleanup(Exception ex)");
        AssertContains(cleanupText, "private void BeginFlashbackBackendCleanup(Exception ex)");
        AssertContains(cleanupText, "private static bool IsGpuDeviceLost(Exception ex)");
        AssertContains(cleanupText, "_flashbackBackend.PreserveRecoverySegments(\"device_lost\");");
        AssertDoesNotContain(cleanupText, "_sessionState =");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackBackendFailureCleanup.cs")),
            "Flashback backend failure cleanup folded into CaptureService.cs");
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
        var telemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Telemetry.cs")),
            "source telemetry polling folded into CaptureService.Snapshots.cs");

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
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");
        var resourceReleaseText = cleanupText;
        var failureCleanupText = cleanupText;

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
        AssertOccursBefore(
            stateMachineText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);",
            "_state = transitionState;");
        AssertContains(cleanupText, "private async Task CleanupForDisposalAsync()");
        AssertContains(cleanupText, "EnterCleanupState();");
        AssertContains(cleanupText, "await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertContains(cleanupText, "public void Dispose()");
        AssertContains(cleanupText, "public async ValueTask DisposeAsync()");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.ResourceRelease.cs")), "CaptureService resource-release helpers stay folded into CaptureService.cs");
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
        AssertContains(failureCleanupText, "private void BeginFlashbackBackendCleanup(Exception ex)");
        AssertContains(failureCleanupText, "private static bool IsGpuDeviceLost(Exception ex)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackBackendFailureCleanup.cs")),
            "CaptureService Flashback backend failure cleanup folded into cleanup lifecycle");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Failures.cs")),
            "CaptureService failure callbacks folded into CaptureService.cs");

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

    internal static Task CaptureSessionTransitionPolicy_DefinesCoreLifecycleRules()
    {
        var policyType = RequireType("Sussudio.Models.CaptureSessionTransitionPolicy");
        var stateType = RequireType("Sussudio.Models.CaptureSessionState");
        var canEnter = policyType.GetMethod(
            "CanEnterTransition",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { stateType, stateType },
            modifiers: null)
            ?? throw new InvalidOperationException("CaptureSessionTransitionPolicy.CanEnterTransition not found.");

        var states = new[]
        {
            "Uninitialized",
            "Initializing",
            "Ready",
            "Previewing",
            "Recording",
            "CleaningUp",
            "Faulted",
            "Disposed"
        };

        var allowedTransitions = new HashSet<string>
        {
            "Uninitialized->Uninitialized",
            "Uninitialized->Initializing",
            "Uninitialized->Ready",
            "Uninitialized->Previewing",
            "Uninitialized->CleaningUp",
            "Initializing->Initializing",
            "Initializing->Ready",
            "Initializing->Previewing",
            "Initializing->CleaningUp",
            "Ready->Initializing",
            "Ready->Ready",
            "Ready->Previewing",
            "Ready->Recording",
            "Ready->CleaningUp",
            "Previewing->Initializing",
            "Previewing->Ready",
            "Previewing->Previewing",
            "Previewing->Recording",
            "Previewing->CleaningUp",
            "Recording->Initializing",
            "Recording->Ready",
            "Recording->Previewing",
            "Recording->Recording",
            "Recording->CleaningUp",
            "CleaningUp->CleaningUp",
            "Faulted->Initializing",
            "Faulted->Ready",
            "Faulted->Previewing",
            "Faulted->CleaningUp",
            "Faulted->Faulted"
        };

        foreach (var currentState in states)
        {
            foreach (var transitionState in states)
            {
                var key = $"{currentState}->{transitionState}";
                AssertCanEnterTransition(
                    canEnter,
                    stateType,
                    currentState,
                    transitionState,
                    expected: allowedTransitions.Contains(key));
            }
        }

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionTransitionPolicy_ResolvesSteadyStateFromRuntimeFlags()
    {
        var policyType = RequireType("Sussudio.Models.CaptureSessionTransitionPolicy");
        var method = policyType.GetMethod(
            "ResolveSteadyState",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) },
            modifiers: null)
            ?? throw new InvalidOperationException("CaptureSessionTransitionPolicy.ResolveSteadyState not found.");

        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Disposed"),
            ResolveState(method, isDisposed: true, isRecording: true, isVideoPreviewActive: true, isAudioPreviewActive: true, isInitialized: true),
            "Disposed steady state precedence");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Recording"),
            ResolveState(method, isDisposed: false, isRecording: true, isVideoPreviewActive: true, isAudioPreviewActive: true, isInitialized: true),
            "Recording steady state precedence");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Previewing"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: true, isInitialized: true),
            "Audio preview steady state");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Ready"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: false, isInitialized: true),
            "Initialized steady state");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Uninitialized"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: false, isInitialized: false),
            "Uninitialized steady state");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RunTransition_UsesTransitionPolicy()
    {
        var transitionExecutionText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs");
        var stateMachineText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs");

        AssertContains(
            transitionExecutionText,
            "private async Task RunTransitionAsync(");
        AssertContains(
            transitionExecutionText,
            "_sessionStateMachine.EnterTransition(transitionState);");
        AssertContains(
            transitionExecutionText,
            "_sessionStateMachine.ResolveSteadyState(BuildSteadyStateInputs());");
        AssertContains(
            stateMachineText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);");
        AssertContains(
            stateMachineText,
            "CaptureSessionTransitionPolicy.ResolveSteadyState(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionStateMachine.cs")),
            "capture session state machine folded into capture model owner");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_InPlaceMutationsUseCurrentStateTransition()
    {
        var currentStateTransitionOwners = new[]
        {
            "Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs",
            "Sussudio/Services/Capture/CaptureService.FlashbackControls.cs"
        };

        foreach (var owner in currentStateTransitionOwners)
        {
            var ownerText = ReadRepoFile(owner);
            AssertContains(ownerText, "RunTransitionAsync(CurrentSessionState,");
        }

        var lifecycleTransitionOwners = new[]
        {
            "Sussudio/Services/Capture/CaptureService.cs",
            "Sussudio/Services/Capture/CaptureService.cs",
            "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs",
            "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"
        };

        foreach (var owner in lifecycleTransitionOwners)
        {
            var ownerText = ReadRepoFile(owner);
            AssertDoesNotContain(ownerText, "RunTransitionAsync(CurrentSessionState,");
        }

        return Task.CompletedTask;
    }


    private static readonly string[] CaptureServiceAudioFiles =
    {
        "Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs",
        "Sussudio/Services/Capture/CaptureService.cs"
    };

    private static string ReadCaptureServiceAudioSource()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceAudioCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    internal static Task PreviewStopCompatibilityOverloads_ArePreserved()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadCaptureServiceAudioSource();
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var viewModelPreviewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureServiceText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(captureServiceText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(coordinatorText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(coordinatorText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(coordinatorText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(viewModelPreviewStateText, "public Task StopPreviewAsync()\n        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);");
        AssertContains(viewModelPreviewStateText, "public Task StopPreviewAsync(bool userInitiated)\n        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);");

        return Task.CompletedTask;
    }

    internal static Task PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity()
    {
        AssertPreviewStopSurface("Sussudio.Services.Capture.CaptureService");
        AssertPreviewStopSurface("Sussudio.Services.Capture.CaptureSessionCoordinator");
        return Task.CompletedTask;
    }

    private static void AssertPreviewStopSurface(string typeName)
    {
        var type = RequireType(typeName);
        AssertStopSurface(type, "StopVideoPreviewAsync", "StopVideoPreviewWithTeardownAsync");
        AssertStopSurface(type, "StopAudioPreviewAsync", "StopAudioPreviewWithTeardownAsync");
    }

    private static void AssertStopSurface(Type type, string stopMethodName, string teardownMethodName)
    {
        var publicInstance = BindingFlags.Instance | BindingFlags.Public;
        var oneParameterStopOverloads = type.GetMethods(publicInstance)
            .Where(method => method.Name == stopMethodName && method.GetParameters().Length == 1)
            .ToArray();

        AssertEqual(1, oneParameterStopOverloads.Length, $"{type.FullName}.{stopMethodName} one-parameter overload count");
        AssertEqual(
            typeof(CancellationToken).FullName,
            oneParameterStopOverloads[0].GetParameters()[0].ParameterType.FullName,
            $"{type.FullName}.{stopMethodName} single parameter");

        var boolFirstParameterOverloads = type.GetMethods(publicInstance)
            .Where(method =>
            {
                if (method.Name != stopMethodName)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length > 0 && parameters[0].ParameterType == typeof(bool);
            })
            .ToArray();
        AssertEqual(0, boolFirstParameterOverloads.Length, $"{type.FullName}.{stopMethodName} bool-first overload count");

        var teardownMethod = type.GetMethod(teardownMethodName, publicInstance, binder: null, types: new[] { typeof(CancellationToken) }, modifiers: null);
        AssertNotNull(teardownMethod, $"{type.FullName}.{teardownMethodName}(CancellationToken)");
    }

    internal static Task PreviewStartup_ToleratesMissingAudioCaptureDevices()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))");
        AssertContains(captureServiceText, "Audio preview requested but no audio capture device is available; continuing with video-only preview.");
        AssertDoesNotContain(captureServiceText, "Audio preview is enabled but no audio capture device is available.");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_PreviewLifecycleLivesInCohesiveOwner()
    {
        var startText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs").Replace("\r\n", "\n");
        var audioGraphText = startText;
        var stopText = startText;
        var freshPipelineText = ExtractTextBetween(
            startText,
            "private async Task StartFreshPreviewPipelineAsync(",
            "private async Task DisposePreviewPipelineAsync(");
        var videoPipelineResourcesText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");
        var flashbackPreviewBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs").Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");
        var libAvFinalizeText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"))
            .Replace("\r\n", "\n");
        var recordingRollbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs").Replace("\r\n", "\n");

        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewLifecycle.cs")),
            "video and audio preview lifecycle share one owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.cs")),
            "old preview start partial folded into preview lifecycle owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.AudioPreviewLifecycle.cs")),
            "old audio preview partial folded into preview lifecycle owner");
        AssertContains(startText, "public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(startText, "await RecyclePreviewPipelineForStartAsync(");
        AssertContains(startText, "if (await TryStartPreviewFromRetainedPipelineAsync(settings, transitionToken).ConfigureAwait(false))");
        AssertContains(startText, "await StartFreshPreviewPipelineAsync(");
        AssertContains(startText, "private async Task RecyclePreviewPipelineForStartAsync(");
        AssertContains(startText, "PREVIEW_START recycle_pipeline=1 reason=settings_changed");
        AssertContains(startText, "PREVIEW_START recycle_pipeline=1 reason=flashback_disabled");
        AssertContains(startText, "PREVIEW_START recycle_flashback=1 reason=flashback_settings_changed");
        AssertContains(startText, "private async Task<bool> TryStartPreviewFromRetainedPipelineAsync(");
        AssertContains(startText, "FLASHBACK_FAST_PATH_FORMAT_MISMATCH");
        AssertContains(startText, "await EnsureFlashbackAudioInputsAsync(settings, transitionToken, \"preview_fast_path\")");
        AssertContains(startText, "private async Task StartFreshPreviewPipelineAsync(");
        AssertContains(startText, "await StartPreviewAudioGraphAsync(settings, audioDeviceId, transitionToken)");
        AssertContains(startText, "var previewStartRollbackToken = CancellationToken.None;");
        AssertContains(startText, "private bool CanReuseVideoCaptureForPreview(UnifiedVideoCapture capture, CaptureSettings settings)");
        AssertContains(startText, "private static bool CanReuseFlashbackBackend(CaptureSettings current, CaptureSettings next)");
        AssertContains(startText, "private static CaptureSettings CloneCaptureSettings(CaptureSettings source)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.Recycle.cs")),
            "old preview-start recycle partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.FastPath.cs")),
            "old preview-start fast-path partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.FreshPipeline.cs")),
            "old preview-start fresh-pipeline partial removed");
        AssertContains(audioGraphText, "private async Task<WasapiAudioCapture?> StartPreviewAudioGraphAsync(");
        AssertContains(audioGraphText, "private async Task StartPreviewMicrophoneMonitorAsync(");
        AssertContains(audioGraphText, "private async Task RollbackPreviewAudioCaptureStartupAsync(");
        AssertContains(stopText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(stopText, "private Task StopVideoPreviewCoreAsync(bool teardownPipeline, CancellationToken cancellationToken = default)");
        AssertContains(stopText, "private async Task DisposePreviewPipelineAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStop.cs")),
            "preview stop and disposal folded into preview lifecycle owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewReuse.cs")),
            "preview reuse helper partial folded into preview start");
        AssertContains(videoPipelineResourcesText, "internal sealed class CaptureVideoPipelineResources");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureVideoPipelineResources.cs")),
            "video pipeline resources folded into CaptureService.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CapturePipelineResources.cs")),
            "capture pipeline resources folded into CaptureService.cs");
        AssertContains(videoPipelineResourcesText, "public UnifiedVideoCapture? Capture { get; set; }");
        AssertContains(videoPipelineResourcesText, "public IPreviewFrameSink? PreviewFrameSink { get; set; }");
        AssertContains(videoPipelineResourcesText, "public UnifiedVideoCapture.MjpegPipelineTimingMetrics LastMjpegPipelineTimingMetrics { get; private set; }");
        AssertContains(videoPipelineResourcesText, "public ParallelMjpegDecodePipeline.PipelineTimingMetrics? LastFullMjpegPipelineTimingMetrics { get; private set; }");
        AssertContains(videoPipelineResourcesText, "public void CacheMjpegTimingMetrics(UnifiedVideoCapture? capture)");
        AssertContains(videoPipelineResourcesText, "public CaptureMjpegTimingSnapshot GetMjpegTimingSnapshot(UnifiedVideoCapture? capture)");
        AssertContains(videoPipelineResourcesText, "public Task ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(videoPipelineResourcesText, "UNIFIED_VIDEO_DEFERRED_PREVIEW_DETACH_WARN");
        AssertContains(videoPipelineResourcesText, "UNIFIED_VIDEO_DEFERRED_CLEANUP_END");
        AssertContains(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs"),
            "private readonly CaptureVideoPipelineResources _videoPipeline = new();");
        AssertDoesNotContain(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs"),
            "_unifiedVideoCapture");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.VideoPipelineLifecycle.cs")),
            "video pipeline lifecycle helper partial folded into preview start");
        AssertContains(startText, "internal void SetPreviewFrameSink(IPreviewFrameSink? sink)");
        AssertContains(startText, "private void AttachUnifiedVideoCapture(UnifiedVideoCapture unifiedVideoCapture)");
        AssertContains(startText, "private void DetachUnifiedVideoCapture(UnifiedVideoCapture? unifiedVideoCapture)");
        AssertContains(startText, "private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)");
        AssertDoesNotContain(startText, "private IPreviewFrameSink? _previewFrameSink");
        AssertDoesNotContain(startText, "private Task ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(stopText, "_recordingBackend.ClearPendingLibAvDrainIfCompletedSuccessfully();");
        AssertContains(startText, "private void TryApplySharedPreviewDevice(UnifiedVideoCapture? capture, IPreviewFrameSink? sink)");
        AssertContains(startText, "_videoPipeline.CacheMjpegTimingMetrics(unifiedVideoCapture);");
        AssertContains(cleanupText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(stopText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(libAvFinalizeText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(recordingRollbackText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertDoesNotContain(startText, "private UnifiedVideoCapture.MjpegPipelineTimingMetrics _lastMjpegPipelineTimingMetrics;");
        AssertDoesNotContain(startText, "private ParallelMjpegDecodePipeline.PipelineTimingMetrics? _lastFullMjpegPipelineTimingMetrics;");
        AssertDoesNotContain(flashbackPreviewBackendText, "ScheduleDeferredUnifiedVideoCaptureCleanup");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewPipeline.cs")),
            "old preview pipeline partial removed after video lifecycle promotion");
        AssertDoesNotContain(freshPipelineText, "new WasapiAudioCapture()");
        AssertDoesNotContain(freshPipelineText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewDisposal.cs")),
            "old preview disposal partial removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_AudioOwnershipLivesWithPreviewLifecycleOwner()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs");
        var audioPreviewText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs");
        var resourceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs");

        AssertContains(rootText, "private readonly PreviewAudioGraphResources _previewAudioGraph = new();");
        AssertContains(rootText, "internal sealed class PreviewAudioGraphResources");
        AssertContains(resourceText, "internal sealed class PreviewAudioGraphResources");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "PreviewAudioGraphResources.cs")),
            "preview audio graph resources folded into CaptureService.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CapturePipelineResources.cs")),
            "capture pipeline resources folded into CaptureService.cs");
        AssertContains(resourceText, "public WasapiAudioCapture? ProgramCapture;");
        AssertContains(resourceText, "public WasapiAudioCapture? MicrophoneCapture;");
        AssertContains(resourceText, "public WasapiAudioPlayback? Playback;");
        AssertContains(resourceText, "public float PreviewVolume = 1.0f;");
        AssertContains(resourceText, "private bool _captureFaulted;");
        AssertContains(resourceText, "private string? _captureFaultMessage;");
        AssertContains(resourceText, "public void RecordCaptureFault(");
        AssertContains(resourceText, "public PreviewAudioCaptureFaultSnapshot ConsumeCaptureFault()");
        AssertDoesNotContain(rootText, "get => _previewAudioGraph.ProgramCapture;");
        AssertDoesNotContain(rootText, "get => _previewAudioGraph.MicrophoneCapture;");
        AssertDoesNotContain(rootText, "get => _previewAudioGraph.Playback;");
        AssertDoesNotContain(rootText, "private WasapiAudioCapture? _wasapiAudioCapture");
        AssertDoesNotContain(rootText, "private WasapiAudioCapture? _microphoneCapture");
        AssertDoesNotContain(rootText, "private WasapiAudioPlayback? _wasapiAudioPlayback");
        AssertDoesNotContain(rootText, "private float _previewVolume");
        AssertDoesNotContain(rootText, "private bool _isMonitoringMuted");
        AssertDoesNotContain(rootText, "private bool _wasapiAudioCaptureFaulted;");
        AssertDoesNotContain(rootText, "private string? _wasapiAudioCaptureFaultMessage;");
        AssertContains(audioPreviewText, "public void SetPreviewVolume(");
        AssertContains(audioPreviewText, "public void SetMonitoringMuted(");
        AssertContains(audioPreviewText, "private void OnWasapiAudioLevelUpdated(");
        AssertContains(audioPreviewText, "private void OnWasapiCaptureFailed(");
        AssertContains(audioPreviewText, "public Task StartAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewWithTeardownAsync(");
        AssertContains(audioPreviewText, "private Task StopAudioPreviewCoreAsync(");
        AssertContains(audioPreviewText, "public Task UpdateAudioInputAsync(");
        AssertContains(audioPreviewText, "Logger.Log($\"Live audio input switch:");
        AssertContains(audioPreviewText, "Logger.Log(\"AUDIO_INPUT_SWITCH_CANCEL_DEFERRED\");");
        AssertContains(audioPreviewText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertContains(audioPreviewText, "RunTransitionAsync(CurrentSessionState,");
        AssertContains(audioPreviewText, "private async Task DisposeMicrophoneCaptureAsync()");
        AssertContains(audioPreviewText, "private void OnMicrophoneAudioLevelUpdated(");
        AssertContains(audioPreviewText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(audioPreviewText, "private readonly record struct MicrophoneMonitorRestartOptions(");
        AssertDoesNotContain(audioPreviewText, "private async Task StartWasapiPlaybackAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Audio.cs")),
            "old audio event projection partial removed after audio preview lifecycle consolidation");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.AudioInputSwitching.cs")),
            "live audio input switching folded into CaptureService.PreviewLifecycle.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.MicrophoneMonitor.cs")),
            "microphone monitor state and restart folded into CaptureService.PreviewLifecycle.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.MicrophoneMonitor.Update.cs")),
            "old microphone monitor update partial removed after monitor consolidation");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.MicrophoneMonitor.Restart.cs")),
            "old microphone monitor restart partial removed after monitor consolidation");

        AssertContains(resourceText, "public async Task StartPlaybackAsync(");
        AssertContains(resourceText, "public void StopPlayback(");
        AssertContains(resourceText, "public void DetachCapture(");
        AssertContains(resourceText, "private static void SafeClearCapturePlayback(");
        AssertContains(resourceText, "private static void DisposePlaybackBestEffort(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.WasapiPlayback.cs")),
            "old WASAPI playback partial removed after PreviewAudioGraphResources promotion");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_MicrophoneRestartAfterRecordingLivesInPreviewLifecycleOwner()
    {
        var flashbackFinalizationText = ExtractTextBetween(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs").Replace("\r\n", "\n"),
            "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(",
            "private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        var recordingLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var libAvFinalizationText = ExtractTextBetween(
            recordingLifecycleText,
            "private async Task<OperationCanceledException?> RestoreLibAvPreviewFeaturesAfterRecordingAsync(",
            "private readonly record struct LibAvFinalizeStepResult(");
        var finalizationText = string.Join(
            "\n",
            flashbackFinalizationText,
            libAvFinalizationText)
            .Replace("\r\n", "\n");
        var microphoneRootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(microphoneRootText, "private readonly record struct MicrophoneMonitorRestartOptions(");
        AssertContains(microphoneRootText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneRootText, "new WasapiAudioCapture()");
        AssertContains(microphoneRootText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertContains(microphoneRootText, "micCapture.CaptureFailed += OnWasapiCaptureFailed;");
        AssertContains(microphoneRootText, "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));");
        AssertContains(microphoneRootText, "FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
        AssertContains(microphoneRootText, "Logger.Log($\"{options.RestartLogEvent} device='\" + (_micMonitorDeviceName ?? \"?\") + \"'\");");
        AssertContains(microphoneRootText, "Logger.Log($\"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}\");");
        AssertOccursBefore(
            microphoneRootText,
            "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));",
            "_previewAudioGraph.MicrophoneCapture = micCapture;");

        AssertContains(finalizationText, "await RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(finalizationText, "OnlyWhenMissing: true,");
        AssertContains(finalizationText, "DisposeWarningEvent: \"FLASHBACK_MIC_RESTART_DISPOSE_WARN\"");
        AssertContains(finalizationText, "OnlyWhenMissing: false,");
        AssertContains(finalizationText, "FlashbackAttachReason: \"mic_monitor_restart\",");
        AssertContains(finalizationText, "RestartLogEvent: \"MIC_MONITOR_RESTART\",");
        AssertContains(finalizationText, "DisposeWarningEvent: \"MIC_MONITOR_RESTART_DISPOSE_WARN\"");
        AssertDoesNotContain(finalizationText, "WasapiAudioCapture? micCapture = null;");
        AssertDoesNotContain(finalizationText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(finalizationText, "micCapture.CaptureFailed += OnWasapiCaptureFailed;");

        return Task.CompletedTask;
    }

    internal static async Task AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        SetPropertyOrBackingField(device, "AudioDeviceId", null);
        SetPropertyOrBackingField(device, "AudioDeviceName", null);
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        string? lastStatus = null;
        var handler = new EventHandler<string>((_, status) => lastStatus = status);
        var statusChanged = captureService.GetType().GetEvent("StatusChanged", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("CaptureService.StatusChanged event not found.");
        statusChanged.AddEventHandler(captureService, handler);

        try
        {
            var startAudioPreview = captureService.GetType().GetMethod(
                "StartAudioPreviewAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(CancellationToken) },
                modifiers: null);
            if (startAudioPreview == null)
            {
                throw new InvalidOperationException("CaptureService.StartAudioPreviewAsync method not found.");
            }

            if (startAudioPreview.Invoke(captureService, new object?[] { CancellationToken.None }) is not Task task)
            {
                throw new InvalidOperationException("CaptureService.StartAudioPreviewAsync did not return a Task.");
            }

            await task.ConfigureAwait(false);

            AssertEqual(false, GetBoolProperty(captureService, "IsAudioPreviewActive"), "IsAudioPreviewActive");
            AssertEqual("Audio preview unavailable", lastStatus, "StatusChanged");
        }
        finally
        {
            statusChanged.RemoveEventHandler(captureService, handler);
            await DisposeAsync(captureService).ConfigureAwait(false);
        }
    }

    internal static Task PreviewBackendLog_ReflectsVideoOnlyFallback()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "_previewAudioGraph.ProgramCapture != null");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video + WASAPI audio ingest.\"");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video only (no audio capture endpoint).\"");

        return Task.CompletedTask;
    }
}
