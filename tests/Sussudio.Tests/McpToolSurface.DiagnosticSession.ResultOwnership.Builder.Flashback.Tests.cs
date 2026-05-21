static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderFlashbackProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Flattening.cs")
            .Replace("\r\n", "\n");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.ProjectionSet.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandsResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCommandsResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCadenceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCadenceResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackOnePercentLowResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackOnePercentLowResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackDecodeResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackDecodeResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackAudioMasterResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackAudioMasterResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackStagesResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackStagesResult.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackRecordingResult.cs")
            .Replace("\r\n", "\n");
        var flashbackExportResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackExportResult.cs")
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
        AssertContains(flashbackPlaybackCommandsResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackCommandsResultText, "private static DiagnosticSessionFlashbackPlaybackCommandsResultProjection BuildFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackCommandsResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertContains(flashbackPlaybackCommandsResultText, "FlashbackPlaybackMaxCommandQueueLatencyCommandObserved: playbackResultMetrics.MaxCommandQueueLatencyCommandObserved");
        AssertContains(flashbackPlaybackCommandsResultText, "FlashbackPlaybackLastCommandFailureAtEnd: playbackResultMetrics.LastCommandFailureAtEnd");
        AssertContains(flashbackPlaybackCommandsResultText, "FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd: playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd");
        AssertContains(flashbackPlaybackCadenceResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackCadenceResultText, "private static DiagnosticSessionFlashbackPlaybackCadenceResultProjection BuildFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackCadenceResultText, "FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta");
        AssertContains(flashbackPlaybackOnePercentLowResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection(");
        AssertContains(flashbackPlaybackOnePercentLowResultText, "private static DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection BuildFlashbackPlaybackOnePercentLowResultProjection(");
        AssertContains(flashbackPlaybackOnePercentLowResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertContains(flashbackPlaybackOnePercentLowResultText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms: playbackSessionMetrics.MinOnePercentLowDecodeP99Ms");
        AssertContains(flashbackPlaybackDecodeResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackDecodeResultText, "private static DiagnosticSessionFlashbackPlaybackDecodeResultProjection BuildFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackDecodeResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertContains(flashbackPlaybackDecodeResultText, "FlashbackPlaybackMaxDecodeP99MsObserved: playbackSessionMetrics.MaxDecodeP99MsObserved");
        AssertContains(flashbackPlaybackDecodeResultText, "FlashbackPlaybackMaxDecodePositionMsObserved: playbackSessionMetrics.MaxDecodePositionMsObserved");
        AssertContains(flashbackPlaybackAudioMasterResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackAudioMasterResultText, "private static DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection BuildFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackAudioMasterResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertContains(flashbackPlaybackAudioMasterResultText, "FlashbackPlaybackMaxAudioMasterFallbacksObserved: playbackSessionMetrics.MaxAudioMasterFallbacksObserved");
        AssertContains(flashbackPlaybackAudioMasterResultText, "FlashbackPlaybackMaxAbsAvDriftMsObserved: playbackSessionMetrics.MaxAbsAvDriftMsObserved");
        AssertContains(flashbackPlaybackStagesResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackStagesResultText, "private static DiagnosticSessionFlashbackPlaybackStagesResultProjection BuildFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackStagesResultText, "FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta");
        AssertContains(flashbackPlaybackStagesResultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertContains(flashbackPlaybackStagesResultText, "FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd: playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd");
        AssertDoesNotContain(builderText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertDoesNotContain(flashbackPlaybackCommandsResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertDoesNotContain(flashbackPlaybackCadenceResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertDoesNotContain(flashbackPlaybackCadenceResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved");
        AssertDoesNotContain(flashbackPlaybackOnePercentLowResultText, "FlashbackPlaybackPendingCommandsAtEnd");
        AssertDoesNotContain(flashbackPlaybackOnePercentLowResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd");
        AssertDoesNotContain(flashbackPlaybackDecodeResultText, "FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta");
        AssertDoesNotContain(flashbackPlaybackStagesResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertDoesNotContain(flashbackPlaybackCommandsResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertDoesNotContain(flashbackPlaybackCadenceResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertDoesNotContain(flashbackPlaybackDecodeResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertDoesNotContain(flashbackPlaybackStagesResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
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
        AssertDoesNotContain(flashbackRecordingResultText, "private readonly record struct DiagnosticSessionFlashbackExportResultProjection(");
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
        AssertDoesNotContain(flashbackExportResultText, "private readonly record struct DiagnosticSessionFlashbackRecordingResultProjection(");
        AssertDoesNotContain(flatteningText, "FlashbackExportObserved = exportMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackExportForceRotateFallbacksAtEnd = flashbackExportForceRotateFallbacksAtEnd");
        AssertDoesNotContain(flatteningText, "LastExportSuccessAtEnd = exportMetrics");
    }
}
