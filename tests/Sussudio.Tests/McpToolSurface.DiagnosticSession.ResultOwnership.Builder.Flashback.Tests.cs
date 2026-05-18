static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderFlashbackProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Flattening.cs")
            .Replace("\r\n", "\n");
        var flashbackResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackResult.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(builderText, "FlashbackPlayback: BuildFlashbackPlaybackResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackPlaybackResult = resultProjections.FlashbackPlayback;");
        AssertContains(flatteningText, "var flashbackPlaybackCommandsResult = flashbackPlaybackResult.CommandsResult;");
        AssertContains(flatteningText, "var flashbackPlaybackCadenceResult = flashbackPlaybackResult.CadenceResult;");
        AssertContains(flatteningText, "var flashbackPlaybackDecodeResult = flashbackPlaybackResult.DecodeResult;");
        AssertContains(flatteningText, "var flashbackPlaybackAudioMasterResult = flashbackPlaybackResult.AudioMasterResult;");
        AssertContains(flatteningText, "var flashbackPlaybackStagesResult = flashbackPlaybackResult.StagesResult;");
        AssertContains(builderText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertContains(builderText, "private static DiagnosticSessionFlashbackPlaybackResultProjection BuildFlashbackPlaybackResultProjection(");
        AssertContains(builderText, "CommandsResult: commandsResult");
        AssertContains(builderText, "CadenceResult: cadenceResult");
        AssertContains(builderText, "DecodeResult: decodeResult");
        AssertContains(builderText, "AudioMasterResult: audioMasterResult");
        AssertContains(builderText, "StagesResult: stagesResult");
        AssertContains(flashbackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackResultText, "private static DiagnosticSessionFlashbackPlaybackCommandsResultProjection BuildFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertContains(flashbackResultText, "FlashbackPlaybackMaxCommandQueueLatencyCommandObserved: playbackResultMetrics.MaxCommandQueueLatencyCommandObserved");
        AssertContains(flashbackResultText, "FlashbackPlaybackLastCommandFailureAtEnd: playbackResultMetrics.LastCommandFailureAtEnd");
        AssertContains(flashbackResultText, "FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd: playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd");
        AssertContains(flashbackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackResultText, "private static DiagnosticSessionFlashbackPlaybackCadenceResultProjection BuildFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertContains(flashbackResultText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms: playbackSessionMetrics.MinOnePercentLowDecodeP99Ms");
        AssertContains(flashbackResultText, "FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta");
        AssertContains(flashbackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackResultText, "private static DiagnosticSessionFlashbackPlaybackDecodeResultProjection BuildFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertContains(flashbackResultText, "FlashbackPlaybackMaxDecodeP99MsObserved: playbackSessionMetrics.MaxDecodeP99MsObserved");
        AssertContains(flashbackResultText, "FlashbackPlaybackMaxDecodePositionMsObserved: playbackSessionMetrics.MaxDecodePositionMsObserved");
        AssertContains(flashbackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackResultText, "private static DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection BuildFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertContains(flashbackResultText, "FlashbackPlaybackMaxAudioMasterFallbacksObserved: playbackSessionMetrics.MaxAudioMasterFallbacksObserved");
        AssertContains(flashbackResultText, "FlashbackPlaybackMaxAbsAvDriftMsObserved: playbackSessionMetrics.MaxAbsAvDriftMsObserved");
        AssertContains(flashbackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackResultText, "private static DiagnosticSessionFlashbackPlaybackStagesResultProjection BuildFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackResultText, "FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta");
        AssertContains(flashbackResultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertContains(flashbackResultText, "FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd: playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd");
        AssertContains(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackPendingCommandsAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackDroppedFramesDelta = flashbackPlaybackCadenceResult.FlashbackPlaybackDroppedFramesDelta,");
        AssertContains(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodePhaseAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterFallbacksAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = flashbackPlaybackStagesResult.FlashbackPlaybackSeekForwardDecodeCapHitsDelta,");
        AssertDoesNotContain(builderText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertDoesNotContain(builderText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertDoesNotContain(builderText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertDoesNotContain(builderText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertDoesNotContain(builderText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = playbackSessionMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = playbackResultMetrics");
        AssertContains(builderText, "FlashbackRecording: BuildFlashbackRecordingResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackRecordingResult = resultProjections.FlashbackRecording;");
        AssertContains(flashbackResultText, "private readonly record struct DiagnosticSessionFlashbackRecordingResultProjection(");
        AssertContains(flashbackResultText, "private static DiagnosticSessionFlashbackRecordingResultProjection BuildFlashbackRecordingResultProjection(");
        AssertContains(flashbackResultText, "FlashbackRecordingBackendObserved: recordingMetrics.BackendObserved");
        AssertContains(flashbackResultText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta: recordingMetrics.IntegrityQueueDroppedFramesDelta");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingBackendObserved = recordingMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta = recordingMetrics");
        AssertContains(builderText, "FlashbackExport: BuildFlashbackExportResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackExportResult = resultProjections.FlashbackExport;");
        AssertContains(flashbackResultText, "private readonly record struct DiagnosticSessionFlashbackExportResultProjection(");
        AssertContains(flashbackResultText, "private static DiagnosticSessionFlashbackExportResultProjection BuildFlashbackExportResultProjection(");
        AssertContains(flashbackResultText, "FlashbackExportObserved: exportMetrics.Observed");
        AssertContains(flashbackResultText, "FlashbackExportForceRotateFallbacksDelta: exportMetrics.ForceRotateFallbacksDelta");
        AssertContains(flashbackResultText, "LastExportSuccessAtEnd: exportMetrics.LastSuccessAtEnd");
        AssertContains(flashbackResultText, "FlashbackExportMaxThroughputBytesPerSecObserved: exportMetrics.MaxThroughputBytesPerSecObserved");
        AssertDoesNotContain(flatteningText, "FlashbackExportObserved = exportMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackExportForceRotateFallbacksAtEnd = flashbackExportForceRotateFallbacksAtEnd");
        AssertDoesNotContain(flatteningText, "LastExportSuccessAtEnd = exportMetrics");
    }
}
