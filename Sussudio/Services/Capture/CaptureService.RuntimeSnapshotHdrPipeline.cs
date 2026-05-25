using System;
using Sussudio.Models;
using Sussudio.Services.Contracts;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Read-only HDR/encoder pipeline projection for runtime snapshots. It reports
// parity and downgrade evidence without changing selected capture settings.
public partial class CaptureService
{
    private sealed class RuntimeHdrPipelineSnapshotFields
    {
        public bool HdrRequested { get; init; }
        public string? EncoderInputPixelFormat { get; init; }
        public string? EncoderOutputPixelFormat { get; init; }
        public string? EncoderVideoCodec { get; init; }
        public string? EncoderVideoProfile { get; init; }
        public bool? EncoderTenBitPipelineConfirmed { get; init; }
        public bool MfConvertersDisabled { get; init; }
        public string NegotiatedMediaSubtypeToken { get; init; } = "NV12";
        public bool HdrOutputActive { get; init; }
        public string HdrActivationReason { get; init; } = "Unknown";
        public string HdrRuntimeState { get; init; } = "Inactive";
        public string HdrReadinessReason { get; init; } = string.Empty;
        public bool HdrAutoDowngraded { get; init; }
        public string HdrAutoDowngradeReason { get; init; } = string.Empty;
        public string HdrDowngradeCode { get; init; } = string.Empty;
        public bool HdrRequestedButSourceNot10Bit { get; init; }
        public string RequestedPipelineMode { get; init; } = "SDR";
        public string ActivePipelineMode { get; init; } = "SDR";
        public bool PipelineModeMatched { get; init; } = true;
        public string PipelineModeStatus { get; init; } = "Ready";
        public string PipelineModeReason { get; init; } = string.Empty;
    }

    private sealed class RuntimeHdrWarmupSnapshotFields
    {
        public string State { get; init; } = "NotRequested";
        public int RequiredP010Frames { get; init; }
        public int AllowedNonP010Frames { get; init; }
        public int ObservedP010Frames { get; init; }
        public int ObservedNonP010Frames { get; init; }
    }

    private static RuntimeHdrPipelineSnapshotFields CaptureRuntimeHdrPipelineSnapshotFields(
        CaptureSettings? requestedSettings,
        string? encoderInputPixelFormat,
        RecordingContext? recordingContext,
        bool recordingActive,
        SourceSignalTelemetrySnapshot sourceTelemetry,
        bool mfConvertersDisabled)
    {
        var hdrRequested = requestedSettings?.HdrEnabled == true &&
                           requestedSettings.HdrOutputMode == HdrOutputMode.Hdr10Pq;
        var requestedPipelineMode = hdrRequested ? "HDR10-PQ" : "SDR";
        var encoderOutputPixelFormat = ResolveEncoderOutputPixelFormat(recordingContext, requestedSettings);
        var encoderVideoCodec = ResolveEncoderCodecName(requestedSettings);
        var encoderVideoProfile = ResolveEncoderVideoProfile(recordingContext, requestedSettings);
        bool? encoderTenBitPipelineConfirmed = recordingActive
            ? recordingContext?.HdrPipelineActive == true
            : null;
        var negotiatedMediaSubtypeToken = string.Equals(encoderInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase)
            ? "P010|MFVideoFormat_P010"
            : "NV12";
        var activePipelineMode = recordingActive
            ? (string.Equals(
                encoderInputPixelFormat,
                "p010le",
                StringComparison.OrdinalIgnoreCase)
                ? "HDR10-PQ"
                : "SDR")
            : requestedPipelineMode;
        var pipelineModeMatched = string.Equals(
            requestedPipelineMode,
            activePipelineMode,
            StringComparison.OrdinalIgnoreCase);
        var pipelineModeStatus = recordingActive
            ? (pipelineModeMatched ? "Active" : "Violation")
            : "Ready";
        var pipelineModeReason = pipelineModeMatched
            ? string.Empty
            : $"Requested pipeline '{requestedPipelineMode}', but active encoder ingress is '{activePipelineMode}' " +
              $"(pixel-format={encoderInputPixelFormat ?? "unknown"}).";
        var hdrOutputActive = recordingActive &&
                              string.Equals(
                                  activePipelineMode,
                                  "HDR10-PQ",
                                  StringComparison.OrdinalIgnoreCase);
        var hdrAutoDowngraded = hdrRequested && recordingActive && !pipelineModeMatched;

        return new RuntimeHdrPipelineSnapshotFields
        {
            HdrRequested = hdrRequested,
            EncoderInputPixelFormat = encoderInputPixelFormat,
            EncoderOutputPixelFormat = encoderOutputPixelFormat,
            EncoderVideoCodec = encoderVideoCodec,
            EncoderVideoProfile = encoderVideoProfile,
            EncoderTenBitPipelineConfirmed = encoderTenBitPipelineConfirmed,
            MfConvertersDisabled = mfConvertersDisabled,
            NegotiatedMediaSubtypeToken = negotiatedMediaSubtypeToken,
            HdrOutputActive = hdrOutputActive,
            HdrActivationReason = hdrOutputActive
                ? "P010 pipeline is active."
                : hdrRequested
                    ? (recordingActive
                        ? "HDR requested but the active recording pipeline is not in HDR mode."
                        : "HDR requested and waiting for recording start.")
                    : "HDR not requested.",
            HdrRuntimeState = hdrOutputActive
                ? "Active"
                : hdrRequested
                    ? (recordingActive ? "Violation" : "Ready")
                    : "Inactive",
            HdrReadinessReason = hdrOutputActive
                ? string.Empty
                : hdrRequested
                    ? (recordingActive
                        ? pipelineModeReason
                        : "HDR requested and will activate when recording starts.")
                    : string.Empty,
            HdrAutoDowngraded = hdrAutoDowngraded,
            HdrAutoDowngradeReason = hdrAutoDowngraded
                ? pipelineModeReason
                : string.Empty,
            HdrDowngradeCode = hdrAutoDowngraded ? "encoder-input-not-p010" : string.Empty,
            HdrRequestedButSourceNot10Bit = hdrRequested && sourceTelemetry.IsHdr == false,
            RequestedPipelineMode = requestedPipelineMode,
            ActivePipelineMode = activePipelineMode,
            PipelineModeMatched = pipelineModeMatched,
            PipelineModeStatus = pipelineModeStatus,
            PipelineModeReason = pipelineModeReason
        };
    }

    private static RuntimeHdrWarmupSnapshotFields CaptureRuntimeHdrWarmupSnapshotFields(
        RuntimeHdrPipelineSnapshotFields hdrPipeline,
        bool recordingActive,
        ObservedFrameSnapshotFields observedTelemetry)
    {
        var observedP010FrameCount = observedTelemetry.ObservedP010FrameCount;
        var observedNonP010FrameCount =
            observedTelemetry.ObservedNv12FrameCount +
            observedTelemetry.ObservedOtherFrameCount;

        return new RuntimeHdrWarmupSnapshotFields
        {
            State = ResolveHdrWarmupState(
                hdrPipeline.HdrRequested,
                hdrPipeline.HdrOutputActive,
                recordingActive,
                observedP010FrameCount),
            RequiredP010Frames = hdrPipeline.HdrRequested ? 1 : 0,
            AllowedNonP010Frames = hdrPipeline.HdrRequested ? 2 : 0,
            ObservedP010Frames = (int)Math.Min(int.MaxValue, observedP010FrameCount),
            ObservedNonP010Frames = (int)Math.Min(int.MaxValue, Math.Max(0L, observedNonP010FrameCount))
        };
    }

    private static string ResolveHdrWarmupState(
        bool hdrRequested,
        bool hdrOutputActive,
        bool isRecording,
        long observedP010Frames)
    {
        if (!hdrRequested)
        {
            return "NotRequested";
        }

        if (hdrOutputActive)
        {
            return "Satisfied";
        }

        if (observedP010Frames > 0)
        {
            return isRecording ? "Partial" : "Pending";
        }

        return isRecording ? "Degraded" : "Pending";
    }
}

// Single policy gate for enabling HDR output. Environment overrides live beside
// the HDR runtime projection so capture setup and UI readiness stay consistent.
internal static class HdrOutputPolicy
{
    public static bool IsEnabled(CaptureSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var hdrRequested = settings.HdrEnabled && settings.HdrOutputMode == HdrOutputMode.Hdr10Pq;
        if (!hdrRequested)
        {
            return false;
        }

        if (EnvironmentHelpers.TryGetBoolFromEnv("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", out var forceOff) && forceOff)
        {
            Logger.Log("HDR output requested but SUSSUDIO_HDR_OUTPUT_FORCE_OFF disables the HDR pipeline.");
            return false;
        }

        return true;
    }
}
