using System;
using Sussudio.Models;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Capture;

// Read-only HDR/encoder pipeline projection for runtime snapshots. It reports
// parity and downgrade evidence without changing selected capture settings.
public partial class CaptureService
{
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
}
