static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderFlashbackProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Flattening.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandsResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCommandsResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCadenceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCadenceResult.cs")
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
        AssertContains(builderText, "FlashbackPlayback: BuildFlashbackPlaybackResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackPlaybackResult = resultProjections.FlashbackPlayback;");
        AssertContains(flatteningText, "var flashbackPlaybackCommandsResult = flashbackPlaybackResult.CommandsResult;");
        AssertContains(flatteningText, "var flashbackPlaybackCadenceResult = flashbackPlaybackResult.CadenceResult;");
        AssertContains(flatteningText, "var flashbackPlaybackDecodeResult = flashbackPlaybackResult.DecodeResult;");
        AssertContains(flatteningText, "var flashbackPlaybackAudioMasterResult = flashbackPlaybackResult.AudioMasterResult;");
        AssertContains(flatteningText, "var flashbackPlaybackStagesResult = flashbackPlaybackResult.StagesResult;");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackResultProjection BuildFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "CommandsResult: commandsResult");
        AssertContains(flashbackPlaybackResultText, "CadenceResult: cadenceResult");
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
        AssertContains(flashbackPlaybackCadenceResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertContains(flashbackPlaybackCadenceResultText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms: playbackSessionMetrics.MinOnePercentLowDecodeP99Ms");
        AssertContains(flashbackPlaybackCadenceResultText, "FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta");
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
        AssertContains(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackPendingCommandsAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackDroppedFramesDelta = flashbackPlaybackCadenceResult.FlashbackPlaybackDroppedFramesDelta,");
        AssertContains(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodePhaseAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterFallbacksAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = flashbackPlaybackStagesResult.FlashbackPlaybackSeekForwardDecodeCapHitsDelta,");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = playbackSessionMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = playbackResultMetrics");
        AssertContains(builderText, "FlashbackRecording: BuildFlashbackRecordingResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackRecordingResult = resultProjections.FlashbackRecording;");
        AssertContains(flashbackRecordingResultText, "private readonly record struct DiagnosticSessionFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "private static DiagnosticSessionFlashbackRecordingResultProjection BuildFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingBackendObserved: recordingMetrics.BackendObserved");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta: recordingMetrics.IntegrityQueueDroppedFramesDelta");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingBackendObserved = recordingMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta = recordingMetrics");
        AssertContains(builderText, "FlashbackExport: BuildFlashbackExportResultProjection(analysis)");
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
