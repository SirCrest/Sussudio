using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

// Standard LibAv recording stop/finalize path: stop capture fan-out, drain and
// dispose the recording sink, clean idle preview resources, and publish outcome.
public partial class CaptureService
{
    private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync(string fallbackStatusMessage, bool emergency, CancellationToken cancellationToken)
    {
        var detachedBackend = _recordingBackend.DetachLibAvBackend();
        var sink = detachedBackend.Sink;
        var libAvSink = detachedBackend.LibAvSink;
        var recordingContext = detachedBackend.Context;
        var fallbackOutputPath = recordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);

        var result = FinalizeResult.Success(fallbackOutputPath, fallbackStatusMessage);
        OperationCanceledException? cancellationException = null;

        var videoBoundary = await StopUnifiedVideoRecordingForLibAvFinalizeAsync(
            result,
            fallbackOutputPath,
            cancellationToken).ConfigureAwait(false);
        result = videoBoundary.Result;
        cancellationException = videoBoundary.CancellationException;

        await DetachLibAvRecordingAudioBeforeSinkStopAsync().ConfigureAwait(false);

        var sinkStop = await StopAndDisposeLibAvSinkForFinalizeAsync(
            sink,
            libAvSink,
            result,
            fallbackOutputPath,
            emergency,
            cancellationException,
            cancellationToken).ConfigureAwait(false);
        result = sinkStop.Result;
        cancellationException = sinkStop.CancellationException;

        var libAvFinalAudioCounters = libAvSink != null
            ? GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_wasapiAudioCapture, libAvSink, _activeRecordingSettings))
            : RecordingAudioIntegrityCounterSnapshot.Disabled;

        var idlePreviewDisposal = await DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(
            result,
            fallbackOutputPath,
            cancellationException).ConfigureAwait(false);
        result = idlePreviewDisposal.Result;
        cancellationException = idlePreviewDisposal.CancellationException;

        result = FoldLibAvAudioFaultIntoFinalizeResult(result, cancellationException);

        PublishLibAvRecordingIntegrity(
            libAvSink,
            result,
            videoBoundary,
            libAvFinalAudioCounters);

        await CompleteLibAvRecordingFinalizeStateAsync().ConfigureAwait(false);

        cancellationException = await RestoreLibAvPreviewFeaturesAfterRecordingAsync(
            cancellationException,
            cancellationToken).ConfigureAwait(false);

        PublishRecordingFinalizedOutcome(result, updateOutputPath: true);

        if (cancellationException != null)
        {
            throw cancellationException;
        }

        return result;
    }

    private FinalizeResult FoldLibAvAudioFaultIntoFinalizeResult(
        FinalizeResult result,
        OperationCanceledException? cancellationException)
    {
        var wasapiAudioCaptureFault = _previewAudioGraph.ConsumeCaptureFault();
        if (!wasapiAudioCaptureFault.Faulted || cancellationException != null || !result.Succeeded)
        {
            return result;
        }

        var statusMessage = string.IsNullOrWhiteSpace(wasapiAudioCaptureFault.Message)
            ? "Recording failed (WASAPI audio capture faulted)."
            : $"Recording failed (WASAPI audio capture faulted: {wasapiAudioCaptureFault.Message})";
        Logger.Log($"RECORDING_AUDIO_FAULT status='{statusMessage}'");
        return FinalizeResult.Failure(result.OutputPath, statusMessage);
    }

    private void PublishLibAvRecordingIntegrity(
        LibAvRecordingSink? libAvSink,
        FinalizeResult result,
        LibAvVideoBoundaryStopResult videoBoundary,
        RecordingAudioIntegrityCounterSnapshot libAvFinalAudioCounters)
    {
        if (libAvSink == null)
        {
            return;
        }

        CaptureEncoderRuntimeTelemetry(libAvSink);
        _lastRecordingIntegrity = BuildRecordingIntegritySummary(
            backend: "LibAv",
            recordingActive: false,
            finalizeSucceeded: result.Succeeded,
            finalizeStatus: result.StatusMessage,
            completedUtc: DateTimeOffset.UtcNow,
            sourceFrames: videoBoundary.RecordingFramesDeliveredToBoundary,
            acceptedFrames: videoBoundary.RecordingFramesAcceptedByBoundary,
            counters: GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(libAvSink)),
            audioCounters: libAvFinalAudioCounters);
        _recordingIntegrityCounterBaseline = null;
        _recordingIntegrityAudioBaseline = null;
        LogRecordingIntegritySummary(_lastRecordingIntegrity);
    }

    private async Task CompleteLibAvRecordingFinalizeStateAsync()
    {
        _recordingStopwatch.Stop();
        _isRecording = false;
        if (!_isVideoPreviewActive) await StopTelemetryPollAsync().ConfigureAwait(false);
        _recordingBackend.ClearContextAndSettings();
        _mfConvertersDisabled = false;
    }
}
