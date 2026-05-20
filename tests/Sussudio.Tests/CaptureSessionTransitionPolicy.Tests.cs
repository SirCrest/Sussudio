using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
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
        var serviceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Coordination.cs");

        AssertContains(
            serviceText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_sessionState, transitionState);");
        AssertContains(
            serviceText,
            "CaptureSessionTransitionPolicy.ResolveSteadyState(");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_InPlaceMutationsUseCurrentStateTransition()
    {
        var currentStateTransitionOwners = new[]
        {
            "Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs",
            "Sussudio/Services/Capture/CaptureService.FlashbackBufferSettings.cs",
            "Sussudio/Services/Capture/CaptureService.FlashbackControls.cs",
            "Sussudio/Services/Capture/CaptureService.FlashbackEncoderSettings.cs",
            "Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs"
        };

        foreach (var owner in currentStateTransitionOwners)
        {
            var ownerText = ReadRepoFile(owner);
            AssertContains(ownerText, "RunTransitionAsync(_sessionState,");
        }

        var lifecycleTransitionOwners = new[]
        {
            "Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs",
            "Sussudio/Services/Capture/CaptureService.Cleanup.cs",
            "Sussudio/Services/Capture/CaptureService.Initialization.cs",
            "Sussudio/Services/Capture/CaptureService.PreviewStart.cs",
            "Sussudio/Services/Capture/CaptureService.PreviewStop.cs",
            "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs",
            "Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs"
        };

        foreach (var owner in lifecycleTransitionOwners)
        {
            var ownerText = ReadRepoFile(owner);
            AssertDoesNotContain(ownerText, "RunTransitionAsync(_sessionState,");
        }

        return Task.CompletedTask;
    }
}
