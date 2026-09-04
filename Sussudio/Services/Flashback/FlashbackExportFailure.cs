using System;
using System.Collections.Generic;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Carries export failures independently of messages, paths, and native error text.
/// The kind strings are part of the automation response contract.
/// </summary>
internal static class FlashbackExportFailure
{
    internal const string Cancelled = "flashback-export-cancelled";
    internal const string InvalidRequest = "flashback-export-invalid-request";
    internal const string UnavailableDuringRecording = "flashback-export-unavailable-during-recording";
    internal const string BufferInactive = "flashback-export-buffer-inactive";
    internal const string InvalidRange = "flashback-export-invalid-range";
    internal const string InvalidOutputPath = "flashback-export-invalid-output-path";
    internal const string OutputWriteFailed = "flashback-export-output-write-failed";
    internal const string ForceRotateFailed = "flashback-export-force-rotate-failed";
    internal const string IncompleteLiveEdge = "flashback-export-incomplete-live-edge";
    internal const string SegmentUnavailable = "flashback-export-segment-unavailable";
    internal const string InputUnavailable = "flashback-export-input-unavailable";
    internal const string InputReadFailed = "flashback-export-input-read-failed";
    internal const string InvalidInputStream = "flashback-export-invalid-input-stream";
    internal const string NoMediaWritten = "flashback-export-no-media-written";
    internal const string Disposed = "flashback-export-disposed";
    internal const string Timeout = "flashback-export-timeout";
    internal const string Failed = "flashback-export-failed";

    internal static FinalizeResult Create(
        string outputPath,
        string message,
        string failureCode,
        IEnumerable<string>? preservedArtifacts = null)
        => FinalizeResult.Failure(outputPath, message, preservedArtifacts, failureCode);

    internal static bool IsCancelled(FinalizeResult result)
        => !result.Succeeded && result.FailureCode == Cancelled;

    internal static string GetKind(FinalizeResult result)
        => result.Succeeded ? string.Empty : result.FailureCode switch
        {
            Cancelled => "Cancelled",
            InvalidRequest => "InvalidRequest",
            UnavailableDuringRecording => "UnavailableDuringRecording",
            BufferInactive => "BufferInactive",
            InvalidRange => "InvalidRange",
            InvalidOutputPath => "InvalidOutputPath",
            OutputWriteFailed => "OutputWriteFailed",
            ForceRotateFailed => "ForceRotateFailed",
            IncompleteLiveEdge => "IncompleteLiveEdge",
            SegmentUnavailable => "SegmentUnavailable",
            InputUnavailable => "InputUnavailable",
            InputReadFailed => "InputReadFailed",
            InvalidInputStream => "InvalidInputStream",
            NoMediaWritten => "NoMediaWritten",
            Disposed => "Disposed",
            Timeout => "Timeout",
            _ => "Failed"
        };

    internal static string GetExceptionCode(Exception exception)
        => exception switch
        {
            FlashbackExportException exportException => exportException.FailureCode,
            ObjectDisposedException => Disposed,
            TimeoutException => Timeout,
            _ => Failed
        };
}

internal sealed class FlashbackExportException : InvalidOperationException
{
    internal FlashbackExportException(string message, string failureCode)
        : base(message)
    {
        FailureCode = failureCode;
    }

    internal string FailureCode { get; }
}
