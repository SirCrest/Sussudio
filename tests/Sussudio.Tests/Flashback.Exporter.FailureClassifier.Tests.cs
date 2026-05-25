using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackExportFailureClassifier_MapsCommandFailures()
    {
        var captureServiceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = captureServiceType.GetMethod(
            "ClassifyFlashbackExportFailureKind",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureService.ClassifyFlashbackExportFailureKind was not found.");
        var exportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(exportText, "internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)");
        AssertContains(diagnosticsText, "internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)");
        AssertContains(diagnosticsText, "private static bool IsFlashbackExportCancelled(string? statusMessage)");
        AssertContains(diagnosticsText, "private static bool ContainsFlashbackExportFailureText(string statusMessage, string value)");

        AssertEqual(
            "BufferInactive",
            method.Invoke(null, new object?[] { "Flashback buffer not active" })?.ToString(),
            "inactive buffer export rejection is classified");
        AssertEqual(
            "InvalidRequest",
            method.Invoke(null, new object?[] { "Flashback export duration must be finite, greater than zero, and within TimeSpan range." })?.ToString(),
            "invalid duration export rejection is classified");
        AssertEqual(
            "InvalidRange",
            method.Invoke(null, new object?[] { "Flashback export range is empty or invalid." })?.ToString(),
            "invalid export range is classified");
        AssertEqual(
            "UnavailableDuringRecording",
            method.Invoke(null, new object?[] { "Cannot export while Flashback is the active recording backend." })?.ToString(),
            "recording backend export rejection is classified");
        AssertEqual(
            "InvalidOutputPath",
            method.Invoke(null, new object?[] { "Flashback export failed: output path is a directory." })?.ToString(),
            "output path export rejection is classified");
        AssertEqual(
            "InputUnavailable",
            method.Invoke(null, new object?[] { "Flashback buffer has no active file" })?.ToString(),
            "missing active file export rejection is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=avio_open2 code=-13 msg='Permission denied'" })?.ToString(),
            "output open failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=av_interleaved_write_frame code=-5 msg='I/O error'" })?.ToString(),
            "output packet write failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_ERROR operation=avformat_alloc_output_context2 msg='Output context allocation failed.'" })?.ToString(),
            "output context allocation failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_ERROR operation=avformat_new_stream msg='Stream allocation returned null.'" })?.ToString(),
            "output stream allocation failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=avcodec_parameters_copy code=-22 msg='Invalid argument'" })?.ToString(),
            "output stream parameter copy failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=av_dict_set(movflags) code=-12 msg='Cannot allocate memory'" })?.ToString(),
            "output muxer option failure is classified");
        AssertEqual(
            "InputReadFailed",
            method.Invoke(null, new object?[] { "FLASHBACK_EXPORT_LIBAV_ERROR operation=av_read_frame code=-5 msg='I/O error'" })?.ToString(),
            "input read failure is classified");
        AssertEqual(
            "NoMediaWritten",
            method.Invoke(null, new object?[] { "Flashback export wrote no packets." })?.ToString(),
            "empty media export failure is classified");
        AssertEqual(
            "NoMediaWritten",
            method.Invoke(null, new object?[] { "Flashback export failed: output file is empty 'clip.mp4'." })?.ToString(),
            "empty completed output export failure is classified");
        AssertEqual(
            "OutputWriteFailed",
            method.Invoke(null, new object?[] { "Flashback export failed: output file length unavailable 'clip.mp4'." })?.ToString(),
            "unreadable completed output export failure is classified");
        AssertEqual(
            "IncompleteLiveEdge",
            method.Invoke(null, new object?[] { "Flashback export skipped a live-edge segment." })?.ToString(),
            "live-edge segment export failure is classified");
        AssertEqual(
            "ForceRotateFailed",
            method.Invoke(null, new object?[] { "Flashback export failed: live-edge segment rotation failed." })?.ToString(),
            "live-edge force-rotate failure is classified");
        AssertEqual(
            "ForceRotateFailed",
            method.Invoke(null, new object?[] { "Flashback export failed: rotation failed." })?.ToString(),
            "generic rotation failure is classified");
        AssertEqual(
            "SegmentUnavailable",
            method.Invoke(null, new object?[] { "Flashback export failed: no segment paths were readable." })?.ToString(),
            "missing segment export failure is classified");
        AssertEqual(
            "InvalidInputStream",
            method.Invoke(null, new object?[] { "Flashback export failed: input had no streams." })?.ToString(),
            "invalid input stream export failure is classified");
        AssertEqual(
            "Disposed",
            method.Invoke(null, new object?[] { "Flashback exporter is disposed." })?.ToString(),
            "disposed exporter failure is classified");
        AssertEqual(
            "Cancelled",
            method.Invoke(null, new object?[] { "Flashback export cancelled." })?.ToString(),
            "cancelled export failure is classified");
        AssertEqual(
            "Timeout",
            method.Invoke(null, new object?[] { "Flashback export lock timed out after 30s." })?.ToString(),
            "export timeout failure is classified");

        return Task.CompletedTask;
    }
}
