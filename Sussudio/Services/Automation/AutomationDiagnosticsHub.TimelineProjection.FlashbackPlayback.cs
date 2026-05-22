using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineFlashbackPlaybackProjection BuildPerformanceTimelineFlashbackPlaybackProjection(
        AutomationSnapshot snapshot)
    {
        var cadence = BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(snapshot);
        var decode = BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(snapshot);
        var commands = BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(snapshot);
        var audioMaster = BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(snapshot);
        var stages = BuildPerformanceTimelineFlashbackPlaybackStagesProjection(snapshot);
        var backend = BuildPerformanceTimelineFlashbackPlaybackBackendProjection(snapshot);

        return new(
            State: snapshot.FlashbackPlaybackState,
            TargetFps: cadence.TargetFps,
            ObservedFps: cadence.ObservedFps,
            P99FrameMs: cadence.P99FrameMs,
            MaxFrameMs: cadence.MaxFrameMs,
            OnePercentLowFps: cadence.OnePercentLowFps,
            FivePercentLowFps: cadence.FivePercentLowFps,
            SlowFramePercent: cadence.SlowFramePercent,
            DecodeP99Ms: decode.DecodeP99Ms,
            DecodeMaxMs: decode.DecodeMaxMs,
            MaxDecodePhase: decode.MaxDecodePhase,
            MaxDecodeReceiveMs: decode.MaxDecodeReceiveMs,
            MaxDecodeFeedMs: decode.MaxDecodeFeedMs,
            MaxDecodeReadMs: decode.MaxDecodeReadMs,
            MaxDecodeSendMs: decode.MaxDecodeSendMs,
            MaxDecodeAudioMs: decode.MaxDecodeAudioMs,
            MaxDecodeConvertMs: decode.MaxDecodeConvertMs,
            MaxDecodeUtcUnixMs: decode.MaxDecodeUtcUnixMs,
            MaxDecodePositionMs: decode.MaxDecodePositionMs,
            SeekForwardDecodeCapHits: decode.SeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap: decode.LastSeekHitForwardDecodeCap,
            PendingCommands: commands.PendingCommands,
            MaxPendingCommands: commands.MaxPendingCommands,
            CommandsEnqueued: commands.CommandsEnqueued,
            CommandsProcessed: commands.CommandsProcessed,
            CommandsDropped: commands.CommandsDropped,
            CommandsSkippedNotReady: commands.CommandsSkippedNotReady,
            ScrubUpdatesCoalesced: commands.ScrubUpdatesCoalesced,
            SeekCommandsCoalesced: commands.SeekCommandsCoalesced,
            LastCommandQueued: commands.LastCommandQueued,
            LastCommandProcessed: commands.LastCommandProcessed,
            MaxCommandQueueLatencyMs: commands.MaxCommandQueueLatencyMs,
            MaxCommandQueueLatencyCommand: commands.MaxCommandQueueLatencyCommand,
            SubmitFailures: stages.SubmitFailures,
            LastDropUtcUnixMs: stages.LastDropUtcUnixMs,
            LastDropReason: stages.LastDropReason,
            LastSubmitFailureUtcUnixMs: stages.LastSubmitFailureUtcUnixMs,
            LastSubmitFailure: stages.LastSubmitFailure,
            DroppedFrames: cadence.DroppedFrames,
            AudioMasterDelayDoubles: audioMaster.DelayDoubles,
            AudioMasterDelayShrinks: audioMaster.DelayShrinks,
            AudioMasterFallbacks: audioMaster.Fallbacks,
            AudioMasterUnavailableFallbacks: audioMaster.UnavailableFallbacks,
            AudioMasterStaleFallbacks: audioMaster.StaleFallbacks,
            AudioMasterDriftOutlierFallbacks: audioMaster.DriftOutlierFallbacks,
            AudioMasterLastFallbackReason: audioMaster.LastFallbackReason,
            AudioMasterLastFallbackClockAgeMs: audioMaster.LastFallbackClockAgeMs,
            SegmentSwitches: stages.SegmentSwitches,
            Fmp4Reopens: stages.Fmp4Reopens,
            WriteHeadWaits: stages.WriteHeadWaits,
            NearLiveSnaps: stages.NearLiveSnaps,
            DecodeErrorSnaps: stages.DecodeErrorSnaps,
            LastWriteHeadWaitGapMs: stages.LastWriteHeadWaitGapMs,
            LastCommandFailureUtcUnixMs: commands.LastCommandFailureUtcUnixMs,
            LastCommandFailure: commands.LastCommandFailure,
            BackendSettingsStale: backend.SettingsStale,
            BackendSettingsStaleReason: backend.SettingsStaleReason,
            BackendActiveFormat: backend.ActiveFormat,
            BackendRequestedFormat: backend.RequestedFormat,
            BackendActivePreset: backend.ActivePreset,
            BackendRequestedPreset: backend.RequestedPreset,
            VideoQueueRejectedFrames: backend.VideoQueueRejectedFrames,
            VideoQueueLastRejectReason: backend.VideoQueueLastRejectReason,
            GpuQueueRejectedFrames: backend.GpuQueueRejectedFrames,
            GpuQueueLastRejectReason: backend.GpuQueueLastRejectReason,
            FatalCleanupInProgress: backend.FatalCleanupInProgress,
            CleanupInProgress: backend.CleanupInProgress,
            ForceRotateRequested: backend.ForceRotateRequested,
            ForceRotateDraining: backend.ForceRotateDraining);
    }

    private static PerformanceTimelineFlashbackPlaybackCadenceProjection BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(
        AutomationSnapshot snapshot)
        => new(
            TargetFps: snapshot.FlashbackPlaybackTargetFps,
            ObservedFps: snapshot.FlashbackPlaybackObservedFps,
            P99FrameMs: snapshot.FlashbackPlaybackP99FrameMs,
            MaxFrameMs: snapshot.FlashbackPlaybackMaxFrameMs,
            OnePercentLowFps: snapshot.FlashbackPlaybackOnePercentLowFps,
            FivePercentLowFps: snapshot.FlashbackPlaybackFivePercentLowFps,
            SlowFramePercent: snapshot.FlashbackPlaybackSlowFramePercent,
            DroppedFrames: snapshot.FlashbackPlaybackDroppedFrames);

    private readonly record struct PerformanceTimelineFlashbackPlaybackCadenceProjection(
        double TargetFps,
        double ObservedFps,
        double P99FrameMs,
        double MaxFrameMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SlowFramePercent,
        long DroppedFrames);

    private static PerformanceTimelineFlashbackPlaybackDecodeProjection BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(
        AutomationSnapshot snapshot)
        => new(
            DecodeP99Ms: snapshot.FlashbackPlaybackDecodeP99Ms,
            DecodeMaxMs: snapshot.FlashbackPlaybackDecodeMaxMs,
            MaxDecodePhase: snapshot.FlashbackPlaybackMaxDecodePhase,
            MaxDecodeReceiveMs: snapshot.FlashbackPlaybackMaxDecodeReceiveMs,
            MaxDecodeFeedMs: snapshot.FlashbackPlaybackMaxDecodeFeedMs,
            MaxDecodeReadMs: snapshot.FlashbackPlaybackMaxDecodeReadMs,
            MaxDecodeSendMs: snapshot.FlashbackPlaybackMaxDecodeSendMs,
            MaxDecodeAudioMs: snapshot.FlashbackPlaybackMaxDecodeAudioMs,
            MaxDecodeConvertMs: snapshot.FlashbackPlaybackMaxDecodeConvertMs,
            MaxDecodeUtcUnixMs: snapshot.FlashbackPlaybackMaxDecodeUtcUnixMs,
            MaxDecodePositionMs: snapshot.FlashbackPlaybackMaxDecodePositionMs,
            SeekForwardDecodeCapHits: snapshot.FlashbackPlaybackSeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap: snapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap);

    private readonly record struct PerformanceTimelineFlashbackPlaybackDecodeProjection(
        double DecodeP99Ms,
        double DecodeMaxMs,
        string MaxDecodePhase,
        double MaxDecodeReceiveMs,
        double MaxDecodeFeedMs,
        double MaxDecodeReadMs,
        double MaxDecodeSendMs,
        double MaxDecodeAudioMs,
        double MaxDecodeConvertMs,
        long MaxDecodeUtcUnixMs,
        long MaxDecodePositionMs,
        long SeekForwardDecodeCapHits,
        bool LastSeekHitForwardDecodeCap);

    private static PerformanceTimelineFlashbackPlaybackCommandsProjection BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(
        AutomationSnapshot snapshot)
        => new(
            PendingCommands: snapshot.FlashbackPlaybackPendingCommands,
            MaxPendingCommands: snapshot.FlashbackPlaybackMaxPendingCommands,
            CommandsEnqueued: snapshot.FlashbackPlaybackCommandsEnqueued,
            CommandsProcessed: snapshot.FlashbackPlaybackCommandsProcessed,
            CommandsDropped: snapshot.FlashbackPlaybackCommandsDropped,
            CommandsSkippedNotReady: snapshot.FlashbackPlaybackCommandsSkippedNotReady,
            ScrubUpdatesCoalesced: snapshot.FlashbackPlaybackScrubUpdatesCoalesced,
            SeekCommandsCoalesced: snapshot.FlashbackPlaybackSeekCommandsCoalesced,
            LastCommandQueued: snapshot.FlashbackPlaybackLastCommandQueued,
            LastCommandProcessed: snapshot.FlashbackPlaybackLastCommandProcessed,
            MaxCommandQueueLatencyMs: snapshot.FlashbackPlaybackMaxCommandQueueLatencyMs,
            MaxCommandQueueLatencyCommand: snapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            LastCommandFailureUtcUnixMs: snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            LastCommandFailure: snapshot.FlashbackPlaybackLastCommandFailure);

    private readonly record struct PerformanceTimelineFlashbackPlaybackCommandsProjection(
        int PendingCommands,
        int MaxPendingCommands,
        long CommandsEnqueued,
        long CommandsProcessed,
        long CommandsDropped,
        long CommandsSkippedNotReady,
        long ScrubUpdatesCoalesced,
        long SeekCommandsCoalesced,
        string LastCommandQueued,
        string LastCommandProcessed,
        long MaxCommandQueueLatencyMs,
        string MaxCommandQueueLatencyCommand,
        long LastCommandFailureUtcUnixMs,
        string LastCommandFailure);

    private static PerformanceTimelineFlashbackPlaybackAudioMasterProjection BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(
        AutomationSnapshot snapshot)
        => new(
            DelayDoubles: snapshot.FlashbackPlaybackAudioMasterDelayDoubles,
            DelayShrinks: snapshot.FlashbackPlaybackAudioMasterDelayShrinks,
            Fallbacks: snapshot.FlashbackPlaybackAudioMasterFallbacks,
            UnavailableFallbacks: snapshot.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            StaleFallbacks: snapshot.FlashbackPlaybackAudioMasterStaleFallbacks,
            DriftOutlierFallbacks: snapshot.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            LastFallbackReason: snapshot.FlashbackPlaybackAudioMasterLastFallbackReason,
            LastFallbackClockAgeMs: snapshot.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs);

    private readonly record struct PerformanceTimelineFlashbackPlaybackAudioMasterProjection(
        long DelayDoubles,
        long DelayShrinks,
        long Fallbacks,
        long UnavailableFallbacks,
        long StaleFallbacks,
        long DriftOutlierFallbacks,
        string LastFallbackReason,
        double LastFallbackClockAgeMs);

    private static PerformanceTimelineFlashbackPlaybackStagesProjection BuildPerformanceTimelineFlashbackPlaybackStagesProjection(
        AutomationSnapshot snapshot)
        => new(
            SubmitFailures: snapshot.FlashbackPlaybackSubmitFailures,
            LastDropUtcUnixMs: snapshot.FlashbackPlaybackLastDropUtcUnixMs,
            LastDropReason: snapshot.FlashbackPlaybackLastDropReason,
            LastSubmitFailureUtcUnixMs: snapshot.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
            LastSubmitFailure: snapshot.FlashbackPlaybackLastSubmitFailure,
            SegmentSwitches: snapshot.FlashbackPlaybackSegmentSwitches,
            Fmp4Reopens: snapshot.FlashbackPlaybackFmp4Reopens,
            WriteHeadWaits: snapshot.FlashbackPlaybackWriteHeadWaits,
            NearLiveSnaps: snapshot.FlashbackPlaybackNearLiveSnaps,
            DecodeErrorSnaps: snapshot.FlashbackPlaybackDecodeErrorSnaps,
            LastWriteHeadWaitGapMs: snapshot.FlashbackPlaybackLastWriteHeadWaitGapMs);

    private readonly record struct PerformanceTimelineFlashbackPlaybackStagesProjection(
        long SubmitFailures,
        long LastDropUtcUnixMs,
        string LastDropReason,
        long LastSubmitFailureUtcUnixMs,
        string LastSubmitFailure,
        long SegmentSwitches,
        long Fmp4Reopens,
        long WriteHeadWaits,
        long NearLiveSnaps,
        long DecodeErrorSnaps,
        long LastWriteHeadWaitGapMs);

    private static PerformanceTimelineFlashbackPlaybackBackendProjection BuildPerformanceTimelineFlashbackPlaybackBackendProjection(
        AutomationSnapshot snapshot)
        => new(
            SettingsStale: snapshot.FlashbackBackendSettingsStale,
            SettingsStaleReason: snapshot.FlashbackBackendSettingsStaleReason,
            ActiveFormat: snapshot.FlashbackBackendActiveFormat,
            RequestedFormat: snapshot.FlashbackBackendRequestedFormat,
            ActivePreset: snapshot.FlashbackBackendActivePreset,
            RequestedPreset: snapshot.FlashbackBackendRequestedPreset,
            VideoQueueRejectedFrames: snapshot.FlashbackVideoQueueRejectedFrames,
            VideoQueueLastRejectReason: snapshot.FlashbackVideoQueueLastRejectReason,
            GpuQueueRejectedFrames: snapshot.FlashbackGpuQueueRejectedFrames,
            GpuQueueLastRejectReason: snapshot.FlashbackGpuQueueLastRejectReason,
            FatalCleanupInProgress: snapshot.FatalCleanupInProgress,
            CleanupInProgress: snapshot.FlashbackCleanupInProgress,
            ForceRotateRequested: snapshot.FlashbackForceRotateRequested,
            ForceRotateDraining: snapshot.FlashbackForceRotateDraining);

    private readonly record struct PerformanceTimelineFlashbackPlaybackBackendProjection(
        bool SettingsStale,
        string SettingsStaleReason,
        string ActiveFormat,
        string RequestedFormat,
        string ActivePreset,
        string RequestedPreset,
        long VideoQueueRejectedFrames,
        string VideoQueueLastRejectReason,
        long GpuQueueRejectedFrames,
        string GpuQueueLastRejectReason,
        bool FatalCleanupInProgress,
        bool CleanupInProgress,
        bool ForceRotateRequested,
        bool ForceRotateDraining);

    private readonly record struct PerformanceTimelineFlashbackPlaybackProjection(
        string State,
        double TargetFps,
        double ObservedFps,
        double P99FrameMs,
        double MaxFrameMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SlowFramePercent,
        double DecodeP99Ms,
        double DecodeMaxMs,
        string MaxDecodePhase,
        double MaxDecodeReceiveMs,
        double MaxDecodeFeedMs,
        double MaxDecodeReadMs,
        double MaxDecodeSendMs,
        double MaxDecodeAudioMs,
        double MaxDecodeConvertMs,
        long MaxDecodeUtcUnixMs,
        long MaxDecodePositionMs,
        long SeekForwardDecodeCapHits,
        bool LastSeekHitForwardDecodeCap,
        int PendingCommands,
        int MaxPendingCommands,
        long CommandsEnqueued,
        long CommandsProcessed,
        long CommandsDropped,
        long CommandsSkippedNotReady,
        long ScrubUpdatesCoalesced,
        long SeekCommandsCoalesced,
        string LastCommandQueued,
        string LastCommandProcessed,
        long MaxCommandQueueLatencyMs,
        string MaxCommandQueueLatencyCommand,
        long SubmitFailures,
        long LastDropUtcUnixMs,
        string LastDropReason,
        long LastSubmitFailureUtcUnixMs,
        string LastSubmitFailure,
        long DroppedFrames,
        long AudioMasterDelayDoubles,
        long AudioMasterDelayShrinks,
        long AudioMasterFallbacks,
        long AudioMasterUnavailableFallbacks,
        long AudioMasterStaleFallbacks,
        long AudioMasterDriftOutlierFallbacks,
        string AudioMasterLastFallbackReason,
        double AudioMasterLastFallbackClockAgeMs,
        long SegmentSwitches,
        long Fmp4Reopens,
        long WriteHeadWaits,
        long NearLiveSnaps,
        long DecodeErrorSnaps,
        long LastWriteHeadWaitGapMs,
        long LastCommandFailureUtcUnixMs,
        string LastCommandFailure,
        bool BackendSettingsStale,
        string BackendSettingsStaleReason,
        string BackendActiveFormat,
        string BackendRequestedFormat,
        string BackendActivePreset,
        string BackendRequestedPreset,
        long VideoQueueRejectedFrames,
        string VideoQueueLastRejectReason,
        long GpuQueueRejectedFrames,
        string GpuQueueLastRejectReason,
        bool FatalCleanupInProgress,
        bool CleanupInProgress,
        bool ForceRotateRequested,
        bool ForceRotateDraining);
}
