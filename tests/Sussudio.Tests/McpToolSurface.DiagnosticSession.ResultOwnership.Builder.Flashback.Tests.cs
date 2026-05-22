static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderFlashbackProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Flattening.cs")
            .Replace("\r\n", "\n");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var flashbackExportResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "FlashbackPlayback: BuildFlashbackPlaybackResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackPlaybackResult = resultProjections.FlashbackPlayback;");
        AssertContains(flatteningText, "var flashbackPlaybackCommandsResult = flashbackPlaybackResult.CommandsResult;");
        AssertContains(flatteningText, "var flashbackPlaybackCadenceResult = flashbackPlaybackResult.CadenceResult;");
        AssertContains(flatteningText, "var flashbackPlaybackOnePercentLowResult = flashbackPlaybackResult.OnePercentLowResult;");
        AssertContains(flatteningText, "var flashbackPlaybackDecodeResult = flashbackPlaybackResult.DecodeResult;");
        AssertContains(flatteningText, "var flashbackPlaybackAudioMasterResult = flashbackPlaybackResult.AudioMasterResult;");
        AssertContains(flatteningText, "var flashbackPlaybackStagesResult = flashbackPlaybackResult.StagesResult;");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackResultProjection BuildFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "CommandsResult: commandsResult");
        AssertContains(flashbackPlaybackResultText, "CadenceResult: cadenceResult");
        AssertContains(flashbackPlaybackResultText, "OnePercentLowResult: onePercentLowResult");
        AssertContains(flashbackPlaybackResultText, "DecodeResult: decodeResult");
        AssertContains(flashbackPlaybackResultText, "AudioMasterResult: audioMasterResult");
        AssertContains(flashbackPlaybackResultText, "StagesResult: stagesResult");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackCommandsResultProjection BuildFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxCommandQueueLatencyCommandObserved: playbackResultMetrics.MaxCommandQueueLatencyCommandObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackLastCommandFailureAtEnd: playbackResultMetrics.LastCommandFailureAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd: playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackCadenceResultProjection BuildFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection BuildFlashbackPlaybackOnePercentLowResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms: playbackSessionMetrics.MinOnePercentLowDecodeP99Ms");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackDecodeResultProjection BuildFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodeP99MsObserved: playbackSessionMetrics.MaxDecodeP99MsObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodePositionMsObserved: playbackSessionMetrics.MaxDecodePositionMsObserved");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection BuildFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxAudioMasterFallbacksObserved: playbackSessionMetrics.MaxAudioMasterFallbacksObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxAbsAvDriftMsObserved: playbackSessionMetrics.MaxAbsAvDriftMsObserved");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackStagesResultProjection BuildFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd: playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd");
        AssertDoesNotContain(builderText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertDoesNotContain(flashbackRecordingResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(");
        AssertDoesNotContain(flashbackExportResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackPendingCommandsAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackDroppedFramesDelta = flashbackPlaybackCadenceResult.FlashbackPlaybackDroppedFramesDelta,");
        AssertContains(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowFpsObserved,");
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
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = flashbackPlaybackCadenceResult");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = playbackResultMetrics");
        AssertContains(projectionSetText, "FlashbackRecording: BuildFlashbackRecordingResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackRecordingResult = resultProjections.FlashbackRecording;");
        AssertContains(flashbackRecordingResultText, "private readonly record struct DiagnosticSessionFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "private static DiagnosticSessionFlashbackRecordingResultProjection BuildFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingBackendObserved: recordingMetrics.BackendObserved");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta: recordingMetrics.IntegrityQueueDroppedFramesDelta");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingBackendObserved = recordingMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta = recordingMetrics");
        AssertContains(projectionSetText, "FlashbackExport: BuildFlashbackExportResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackExportResult = resultProjections.FlashbackExport;");
        AssertContains(flashbackExportResultText, "private readonly record struct DiagnosticSessionFlashbackExportResultProjection(");
        AssertContains(flashbackExportResultText, "private static DiagnosticSessionFlashbackExportResultProjection BuildFlashbackExportResultProjection(");
        AssertContains(flashbackExportResultText, "FlashbackExportObserved: exportMetrics.Observed");
        AssertContains(flashbackExportResultText, "FlashbackExportForceRotateFallbacksDelta: exportMetrics.ForceRotateFallbacksDelta");
        AssertContains(flashbackExportResultText, "LastExportSuccessAtEnd: exportMetrics.LastSuccessAtEnd");
        AssertContains(flashbackExportResultText, "FlashbackExportMaxThroughputBytesPerSecObserved: exportMetrics.MaxThroughputBytesPerSecObserved");
        AssertDoesNotContain(flatteningText, "FlashbackExportObserved = exportMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackExportForceRotateFallbacksAtEnd = flashbackExportForceRotateFallbacksAtEnd");
        AssertDoesNotContain(flatteningText, "LastExportSuccessAtEnd = exportMetrics");
    }
}
