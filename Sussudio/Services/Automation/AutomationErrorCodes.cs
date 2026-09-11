using System.Collections.Generic;

namespace Sussudio.Services.Automation;

// Protocol-level error identity for the automation pipe, mirroring the pattern
// FlashbackExportFailureCodes and RecordingFailureCodes establish for their
// producers. Every value here travels to ssctl, the MCP server and the generic
// AutomationClient in AutomationCommandResponse.ErrorCode, so these strings are
// a wire contract: change one only alongside its consumers.
internal static class AutomationErrorCodes
{
    // Dispatch and preflight
    internal const string Canceled = "canceled";
    internal const string CommandFailed = "command-failed";
    internal const string ManifestMismatch = "manifest-mismatch";
    internal const string NotReady = "not-ready";
    internal const string Unauthorized = "unauthorized";
    internal const string UnsupportedCommand = "unsupported-command";

    // Per-command outcomes
    internal const string AssertionFailed = "assertion-failed";
    internal const string ExportFailed = "export-failed";
    internal const string FlashbackActionFailed = "flashback-action-failed";
    internal const string Timeout = "timeout";
    internal const string VerificationFailed = "verification-failed";
    internal const string WindowCloseActionIdMismatch = "window-close-action-id-mismatch";
    internal const string WindowCloseActionIdRequired = "window-close-action-id-required";
    internal const string WindowCloseNotArmed = "window-close-not-armed";

    // Transport-level failures raised by the pipe server before dispatch
    internal const string ExecutionFailed = "execution-failed";
    internal const string InvalidJson = "invalid-json";
    internal const string InvalidRequest = "invalid-request";
    internal const string RequestTimeout = "request-timeout";
    internal const string RequestTooLarge = "request-too-large";

    // Every declared code, for tests and diagnostics that need to prove the
    // registry and the emitting sites agree.
    internal static IReadOnlyCollection<string> All { get; } = new[]
    {
        Canceled,
        CommandFailed,
        ManifestMismatch,
        NotReady,
        Unauthorized,
        UnsupportedCommand,
        AssertionFailed,
        ExportFailed,
        FlashbackActionFailed,
        Timeout,
        VerificationFailed,
        WindowCloseActionIdMismatch,
        WindowCloseActionIdRequired,
        WindowCloseNotArmed,
        ExecutionFailed,
        InvalidJson,
        InvalidRequest,
        RequestTimeout,
        RequestTooLarge,
    };
}
