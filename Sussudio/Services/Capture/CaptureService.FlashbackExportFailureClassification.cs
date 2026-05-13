using System;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static bool IsFlashbackExportCancelled(string? statusMessage)
        => statusMessage?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true;

    internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return string.Empty;
        }

        if (IsFlashbackExportCancelled(statusMessage))
        {
            return "Cancelled";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "request is required") ||
            ContainsFlashbackExportFailureText(statusMessage, "duration must be finite"))
        {
            return "InvalidRequest";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "active recording backend"))
        {
            return "UnavailableDuringRecording";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "buffer not active"))
        {
            return "BufferInactive";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "in point") ||
            ContainsFlashbackExportFailureText(statusMessage, "export range"))
        {
            return "InvalidRange";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "output path") ||
            ContainsFlashbackExportFailureText(statusMessage, "output directory") ||
            ContainsFlashbackExportFailureText(statusMessage, "overwrite source"))
        {
            return "InvalidOutputPath";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "operation=avio_open2") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_alloc_output_context2") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_new_stream") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avcodec_parameters_copy") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_dict_set") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_write_header") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_interleaved_write_frame") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_write_trailer") ||
            ContainsFlashbackExportFailureText(statusMessage, "output file length unavailable") ||
            ContainsFlashbackExportFailureText(statusMessage, "temporary export file was not created") ||
            ContainsFlashbackExportFailureText(statusMessage, "access is denied") ||
            ContainsFlashbackExportFailureText(statusMessage, "permission denied") ||
            ContainsFlashbackExportFailureText(statusMessage, "sharing violation"))
        {
            return "OutputWriteFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "rotation failed"))
        {
            return "ForceRotateFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "live-edge segment"))
        {
            return "IncompleteLiveEdge";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "no segment paths") ||
            ContainsFlashbackExportFailureText(statusMessage, "segment path") ||
            ContainsFlashbackExportFailureText(statusMessage, "segment files") ||
            ContainsFlashbackExportFailureText(statusMessage, "readable segment"))
        {
            return "SegmentUnavailable";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "input file not found") ||
            ContainsFlashbackExportFailureText(statusMessage, "buffer has no active file"))
        {
            return "InputUnavailable";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_open_input") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_read_frame"))
        {
            return "InputReadFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "input context") ||
            ContainsFlashbackExportFailureText(statusMessage, "input had no streams") ||
            ContainsFlashbackExportFailureText(statusMessage, "stream count"))
        {
            return "InvalidInputStream";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "no usable video stream") ||
            ContainsFlashbackExportFailureText(statusMessage, "no segment had complete video parameters") ||
            ContainsFlashbackExportFailureText(statusMessage, "output file is empty") ||
            ContainsFlashbackExportFailureText(statusMessage, "no video packets") ||
            ContainsFlashbackExportFailureText(statusMessage, "no packets"))
        {
            return "NoMediaWritten";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "disposed"))
        {
            return "Disposed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "timeout") ||
            ContainsFlashbackExportFailureText(statusMessage, "timed out"))
        {
            return "Timeout";
        }

        return "Failed";
    }

    private static bool ContainsFlashbackExportFailureText(string statusMessage, string value)
        => statusMessage.Contains(value, StringComparison.OrdinalIgnoreCase);
}
