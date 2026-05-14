using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureSessionTransitionPolicy_DefinesCoreLifecycleRules()
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

        AssertCanEnterTransition(canEnter, stateType, "Uninitialized", "Initializing", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Ready", "Previewing", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Previewing", "Recording", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Recording", "Ready", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Faulted", "CleaningUp", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Uninitialized", "Uninitialized", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Disposed", "Ready", expected: false);
        AssertCanEnterTransition(canEnter, stateType, "Ready", "Disposed", expected: false);
        AssertCanEnterTransition(canEnter, stateType, "Uninitialized", "Recording", expected: false);

        return Task.CompletedTask;
    }

    private static Task CaptureSessionTransitionPolicy_ResolvesSteadyStateFromRuntimeFlags()
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

    private static Task CaptureService_RunTransition_UsesTransitionPolicy()
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
}
