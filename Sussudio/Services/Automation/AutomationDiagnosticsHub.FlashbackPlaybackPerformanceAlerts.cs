using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateFlashbackPlaybackPerformanceAlerts(AutomationSnapshot snapshot)
    {
        var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(
            snapshot.FlashbackPlaybackTargetFps,
            snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate));
        var selectedCaptureFps = snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate);
        var playbackActive =
            string.Equals(snapshot.FlashbackPlaybackState, "Playing", StringComparison.OrdinalIgnoreCase);
        var playbackTargetBelowSelection =
            playbackActive &&
            selectedCaptureFps >= 90 &&
            snapshot.FlashbackPlaybackTargetFps > 0 &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackTargetFps <= selectedCaptureFps * FlashbackPlaybackSlowFpsRatio;
        var playbackPresentCadenceCapped =
            playbackActive &&
            snapshot.FlashbackPlaybackTargetFps >= 90 &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.PreviewCadenceSampleCount >= PreviewPerfectionMinSamples &&
            snapshot.PreviewCadenceObservedFps > 0 &&
            snapshot.PreviewCadenceObservedFps <= snapshot.FlashbackPlaybackTargetFps * FlashbackPlaybackSlowFpsRatio;
        var playbackSlow =
            playbackActive &&
            playbackTargetFps > 0 &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackObservedFps > 0 &&
            snapshot.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio;
        var playbackFrametimeDegraded =
            IsFlashbackPlaybackFrametimeDegraded(
                snapshot.FlashbackPlaybackState,
                playbackTargetFps,
                snapshot.FlashbackPlaybackFrameCount,
                snapshot.FlashbackPlaybackCadenceSampleCount,
                snapshot.FlashbackPlaybackOnePercentLowFps);
        var playbackAudioMasterFallbackDominant =
            playbackActive &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackAudioMasterFallbacks >= snapshot.FlashbackPlaybackFrameCount * FlashbackPlaybackAudioMasterFallbackWarningRatio;
        var playbackAudioQueueBacklog =
            playbackActive &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.WasapiPlaybackQueueDepth >= FlashbackPlaybackAudioQueueBacklogWarningDepth;

        SetAlertState(
            "flashback-playback-target-below-selection",
            playbackTargetBelowSelection,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback target is below the selected capture rate: playbackTarget={snapshot.FlashbackPlaybackTargetFps:0.##}fps " +
            $"selected={selectedCaptureFps:0.##}fps encoder={snapshot.EncoderFrameRate:0.##}fps expected={snapshot.ExpectedCaptureFrameRate:0.##}fps " +
            $"source={(snapshot.DetectedSourceFrameRate ?? 0):0.##}fps observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps frames={snapshot.FlashbackPlaybackFrameCount}.",
            "Flashback playback target matches the selected capture rate.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-present-capped",
            playbackPresentCadenceCapped,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback is targeting HFR but D3D present cadence is below target: target={snapshot.FlashbackPlaybackTargetFps:0.##}fps " +
            $"playbackObserved={snapshot.FlashbackPlaybackObservedFps:0.##}fps presentObserved={snapshot.PreviewCadenceObservedFps:0.##}fps " +
            $"present1pctLow={snapshot.PreviewCadenceOnePercentLowFps:0.##}fps sync={snapshot.PreviewD3DPresentSyncInterval} " +
            $"latency={snapshot.PreviewD3DMaxFrameLatency} buffers={snapshot.PreviewD3DSwapChainBufferCount} " +
            $"renderDrops={snapshot.PreviewD3DFramesDropped} lastDrop={snapshot.PreviewD3DLastDropReason}.",
            "Flashback playback present cadence returned to the HFR target range.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-slow",
            playbackSlow,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback is below target rate: observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps target={playbackTargetFps:0.##}fps " +
            $"selected={selectedCaptureFps:0.##}fps encoder={snapshot.EncoderFrameRate:0.##}fps present={snapshot.PreviewCadenceObservedFps:0.##}fps " +
            $"frames={snapshot.FlashbackPlaybackFrameCount} late={snapshot.FlashbackPlaybackLateFrames} dropped={snapshot.FlashbackPlaybackDroppedFrames} submitFailures={snapshot.FlashbackPlaybackSubmitFailures} " +
            $"audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={snapshot.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={snapshot.FlashbackPlaybackAudioMasterFallbacks} " +
            $"switches={snapshot.FlashbackPlaybackSegmentSwitches} fmp4Reopens={snapshot.FlashbackPlaybackFmp4Reopens} writeHeadWaits={snapshot.FlashbackPlaybackWriteHeadWaits}.",
            "Flashback playback returned to target rate.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-frametime-degraded",
            playbackFrametimeDegraded,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback frametime degraded: onePercentLow={snapshot.FlashbackPlaybackOnePercentLowFps:0.##}fps target={playbackTargetFps:0.##}fps " +
            $"p99={snapshot.FlashbackPlaybackP99FrameMs:0.##}ms max={snapshot.FlashbackPlaybackMaxFrameMs:0.##}ms slow={snapshot.FlashbackPlaybackSlowFramePercent:0.##}% " +
            $"ptsMismatch={snapshot.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={snapshot.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{snapshot.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms seekCapHits={snapshot.FlashbackPlaybackSeekForwardDecodeCapHits} lastSeekCap={snapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap} " +
            $"decodeP99={snapshot.FlashbackPlaybackDecodeP99Ms:0.##}ms decodeMax={snapshot.FlashbackPlaybackDecodeMaxMs:0.##}ms " +
            $"decodePhase={snapshot.FlashbackPlaybackMaxDecodePhase} decodeReceive={snapshot.FlashbackPlaybackMaxDecodeReceiveMs:0.##}ms " +
            $"decodeFeed={snapshot.FlashbackPlaybackMaxDecodeFeedMs:0.##}ms decodeRead={snapshot.FlashbackPlaybackMaxDecodeReadMs:0.##}ms decodeSend={snapshot.FlashbackPlaybackMaxDecodeSendMs:0.##}ms " +
            $"decodeAudio={snapshot.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms decodeConvert={snapshot.FlashbackPlaybackMaxDecodeConvertMs:0.##}ms decodeMaxPos={snapshot.FlashbackPlaybackMaxDecodePositionMs}ms " +
            $"samples={snapshot.FlashbackPlaybackCadenceSampleCount} " +
            $"audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={snapshot.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={snapshot.FlashbackPlaybackAudioMasterFallbacks}.",
            "Flashback playback frametime returned to target range.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-audio-master-fallback",
            playbackAudioMasterFallbackDominant,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback is using wall-clock pacing instead of audio-master pacing: " +
            $"fallbacks={snapshot.FlashbackPlaybackAudioMasterFallbacks} frames={snapshot.FlashbackPlaybackFrameCount} " +
            $"target={snapshot.FlashbackPlaybackTargetFps:0.##}fps observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps " +
            $"avDrift={snapshot.FlashbackAvDriftMs:0.##}ms renderCallbacks={snapshot.WasapiPlaybackRenderCallbackCount} " +
            $"renderSilence={snapshot.WasapiPlaybackRenderSilenceCount} queueDepth={snapshot.WasapiPlaybackQueueDepth}.",
            "Flashback playback returned to audio-master pacing.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-audio-queue-backlog",
            playbackAudioQueueBacklog,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback audio queue is backing up: queueDepth={snapshot.WasapiPlaybackQueueDepth} " +
            $"drops={snapshot.WasapiPlaybackQueueDropCount} renderSilence={snapshot.WasapiPlaybackRenderSilenceCount} " +
            $"avDrift={snapshot.FlashbackAvDriftMs:0.##}ms target={snapshot.FlashbackPlaybackTargetFps:0.##}fps " +
            $"observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps audioMasterFallback={snapshot.FlashbackPlaybackAudioMasterFallbacks}.",
            "Flashback playback audio queue returned to healthy depth.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-submit-failures",
            snapshot.FlashbackPlaybackSubmitFailures > 0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback frame submission failed: submitFailures={snapshot.FlashbackPlaybackSubmitFailures} state={snapshot.FlashbackPlaybackState} " +
            $"frames={snapshot.FlashbackPlaybackFrameCount} threadAlive={snapshot.FlashbackPlaybackThreadAlive}.",
            "Flashback playback frame submission recovered.",
            throttleMs: 5000);
    }
}
