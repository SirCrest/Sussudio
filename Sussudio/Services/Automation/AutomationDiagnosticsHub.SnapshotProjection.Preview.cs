using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(
        PreviewRuntimeSnapshot previewRuntime,
        PreviewHdrState previewHdrState,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),
            Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),
            Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),
            Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),
            GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),
            Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)
        };

    private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(
        PreviewRuntimeProjection previewSummary)
        => new()
        {
            Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),
            Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),
            Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),
            Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),
            GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),
            Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)
        };

    private readonly record struct PreviewRuntimeProjection
    {
        public PreviewRuntimeFrameProjection Frame { get; init; }
        public PreviewRuntimeCadenceProjection Cadence { get; init; }
        public PreviewRuntimeSurfaceProjection Surface { get; init; }
        public PreviewRuntimeStartupProjection Startup { get; init; }
        public PreviewRuntimeGpuPlaybackProjection GpuPlayback { get; init; }
        public PreviewRuntimeColorProjection Color { get; init; }
    }

    private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            FramesArrived = previewRuntime.FramesArrived,
            FramesDisplayed = previewRuntime.FramesDisplayed,
            FramesDropped = previewRuntime.FramesDropped,
            EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs
        };

    private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(
        PreviewRuntimeFrameProjection frame)
        => new()
        {
            FramesArrived = frame.FramesArrived,
            FramesDisplayed = frame.FramesDisplayed,
            FramesDropped = frame.FramesDropped,
            EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs
        };

    private readonly record struct PreviewRuntimeFrameProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
        public long EstimatedPipelineLatencyMs { get; init; }
    }

    private readonly record struct PreviewRuntimeFrameFlattenedProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
        public long EstimatedPipelineLatencyMs { get; init; }
    }

    private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            SampleCount = previewRuntime.DisplayCadenceSampleCount,
            ObservedFps = previewRuntime.DisplayCadenceObservedFps,
            ExpectedIntervalMs = previewRuntime.DisplayCadenceExpectedIntervalMs,
            AverageIntervalMs = previewRuntime.DisplayCadenceAverageIntervalMs,
            P95IntervalMs = previewRuntime.DisplayCadenceP95IntervalMs,
            P99IntervalMs = previewRuntime.DisplayCadenceP99IntervalMs,
            MaxIntervalMs = previewRuntime.DisplayCadenceMaxIntervalMs,
            OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,
            FivePercentLowFps = previewRuntime.DisplayCadenceFivePercentLowFps,
            SampleDurationMs = previewRuntime.DisplayCadenceSampleDurationMs,
            RecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,
            JitterStdDevMs = previewRuntime.DisplayCadenceJitterStdDevMs,
            SlowFrameCount = previewRuntime.DisplayCadenceSlowFrameCount,
            SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent
        };

    private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(
        PreviewRuntimeCadenceProjection cadence)
        => new()
        {
            SampleCount = cadence.SampleCount,
            ObservedFps = cadence.ObservedFps,
            ExpectedIntervalMs = cadence.ExpectedIntervalMs,
            AverageIntervalMs = cadence.AverageIntervalMs,
            P95IntervalMs = cadence.P95IntervalMs,
            P99IntervalMs = cadence.P99IntervalMs,
            MaxIntervalMs = cadence.MaxIntervalMs,
            OnePercentLowFps = cadence.OnePercentLowFps,
            FivePercentLowFps = cadence.FivePercentLowFps,
            SampleDurationMs = cadence.SampleDurationMs,
            RecentIntervalsMs = cadence.RecentIntervalsMs,
            JitterStdDevMs = cadence.JitterStdDevMs,
            SlowFrameCount = cadence.SlowFrameCount,
            SlowFramePercent = cadence.SlowFramePercent
        };

    private readonly record struct PreviewRuntimeCadenceProjection
    {
        public int SampleCount { get; init; }
        public double ObservedFps { get; init; }
        public double ExpectedIntervalMs { get; init; }
        public double AverageIntervalMs { get; init; }
        public double P95IntervalMs { get; init; }
        public double P99IntervalMs { get; init; }
        public double MaxIntervalMs { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentIntervalsMs { get; init; }
        public double JitterStdDevMs { get; init; }
        public long SlowFrameCount { get; init; }
        public double SlowFramePercent { get; init; }
    }

    private readonly record struct PreviewRuntimeCadenceFlattenedProjection
    {
        public int SampleCount { get; init; }
        public double ObservedFps { get; init; }
        public double ExpectedIntervalMs { get; init; }
        public double AverageIntervalMs { get; init; }
        public double P95IntervalMs { get; init; }
        public double P99IntervalMs { get; init; }
        public double MaxIntervalMs { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentIntervalsMs { get; init; }
        public double JitterStdDevMs { get; init; }
        public long SlowFrameCount { get; init; }
        public double SlowFramePercent { get; init; }
    }

    private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(
        PreviewHdrState previewHdrState,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            HdrInputDetected = previewHdrState.InputDetected,
            ToneMapMode = previewHdrState.ToneMapMode,
            ColorContext = captureRuntime.NegotiatedPixelFormat,
            AdapterColorMetadata = captureRuntime.PreviewColorMetadata
        };

    private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(
        PreviewRuntimeColorProjection color)
        => new()
        {
            HdrInputDetected = color.HdrInputDetected,
            ToneMapMode = color.ToneMapMode,
            ColorContext = color.ColorContext,
            AdapterColorMetadata = color.AdapterColorMetadata
        };

    private readonly record struct PreviewRuntimeColorProjection
    {
        public bool HdrInputDetected { get; init; }
        public string ToneMapMode { get; init; }
        public string? ColorContext { get; init; }
        public string AdapterColorMetadata { get; init; }
    }

    private readonly record struct PreviewRuntimeColorFlattenedProjection
    {
        public bool HdrInputDetected { get; init; }
        public string ToneMapMode { get; init; }
        public string? ColorContext { get; init; }
        public string AdapterColorMetadata { get; init; }
    }

    private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            GpuActive = previewRuntime.GpuActive,
            PlaceholderVisible = previewRuntime.PlaceholderVisible,
            GpuElementVisible = previewRuntime.GpuElementVisible,
            CpuElementVisible = previewRuntime.CpuElementVisible,
            RendererAttached = previewRuntime.RendererAttached
        };

    private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(
        PreviewRuntimeSurfaceProjection surface)
        => new()
        {
            GpuActive = surface.GpuActive,
            PlaceholderVisible = surface.PlaceholderVisible,
            GpuElementVisible = surface.GpuElementVisible,
            CpuElementVisible = surface.CpuElementVisible,
            RendererAttached = surface.RendererAttached
        };

    private readonly record struct PreviewRuntimeSurfaceProjection
    {
        public bool GpuActive { get; init; }
        public bool PlaceholderVisible { get; init; }
        public bool GpuElementVisible { get; init; }
        public bool CpuElementVisible { get; init; }
        public bool RendererAttached { get; init; }
    }

    private readonly record struct PreviewRuntimeSurfaceFlattenedProjection
    {
        public bool GpuActive { get; init; }
        public bool PlaceholderVisible { get; init; }
        public bool GpuElementVisible { get; init; }
        public bool CpuElementVisible { get; init; }
        public bool RendererAttached { get; init; }
    }

    private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            State = previewRuntime.StartupState,
            AttemptId = previewRuntime.StartupAttemptId,
            ElapsedMs = previewRuntime.StartupElapsedMs,
            TimeoutMs = previewRuntime.StartupTimeoutMs,
            GpuSignalMediaOpened = previewRuntime.StartupGpuSignalMediaOpened,
            GpuSignalFirstFrame = previewRuntime.StartupGpuSignalFirstFrame,
            GpuSignalPlaybackAdvancing = previewRuntime.StartupGpuSignalPlaybackAdvancing,
            RequiredSignals = previewRuntime.StartupRequiredSignals,
            ReceivedSignals = previewRuntime.StartupReceivedSignals,
            Strategy = previewRuntime.StartupStrategy.ToString(),
            MissingSignals = previewRuntime.StartupMissingSignals,
            RecoveryAttemptCount = previewRuntime.StartupRecoveryAttemptCount,
            LastFailureReason = previewRuntime.StartupLastFailureReason,
            FirstVisualConfirmed = previewRuntime.FirstVisualConfirmed,
            BlankSuspected = previewRuntime.BlankSuspected,
            Stalled = previewRuntime.StallSuspected,
            RendererMode = previewRuntime.RendererMode
        };

    private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(
        PreviewRuntimeStartupProjection startup)
        => new()
        {
            State = startup.State,
            AttemptId = startup.AttemptId,
            ElapsedMs = startup.ElapsedMs,
            TimeoutMs = startup.TimeoutMs,
            GpuSignalMediaOpened = startup.GpuSignalMediaOpened,
            GpuSignalFirstFrame = startup.GpuSignalFirstFrame,
            GpuSignalPlaybackAdvancing = startup.GpuSignalPlaybackAdvancing,
            RequiredSignals = startup.RequiredSignals,
            ReceivedSignals = startup.ReceivedSignals,
            Strategy = startup.Strategy,
            MissingSignals = startup.MissingSignals,
            RecoveryAttemptCount = startup.RecoveryAttemptCount,
            LastFailureReason = startup.LastFailureReason,
            FirstVisualConfirmed = startup.FirstVisualConfirmed,
            BlankSuspected = startup.BlankSuspected,
            Stalled = startup.Stalled,
            RendererMode = startup.RendererMode
        };

    private readonly record struct PreviewRuntimeStartupProjection
    {
        public string State { get; init; }
        public string? AttemptId { get; init; }
        public double? ElapsedMs { get; init; }
        public int TimeoutMs { get; init; }
        public bool GpuSignalMediaOpened { get; init; }
        public bool GpuSignalFirstFrame { get; init; }
        public bool GpuSignalPlaybackAdvancing { get; init; }
        public PreviewStartupSignalFlags RequiredSignals { get; init; }
        public PreviewStartupSignalFlags ReceivedSignals { get; init; }
        public string Strategy { get; init; }
        public string? MissingSignals { get; init; }
        public int RecoveryAttemptCount { get; init; }
        public string? LastFailureReason { get; init; }
        public bool FirstVisualConfirmed { get; init; }
        public bool BlankSuspected { get; init; }
        public bool Stalled { get; init; }
        public string RendererMode { get; init; }
    }

    private readonly record struct PreviewRuntimeStartupFlattenedProjection
    {
        public string State { get; init; }
        public string? AttemptId { get; init; }
        public double? ElapsedMs { get; init; }
        public int TimeoutMs { get; init; }
        public bool GpuSignalMediaOpened { get; init; }
        public bool GpuSignalFirstFrame { get; init; }
        public bool GpuSignalPlaybackAdvancing { get; init; }
        public PreviewStartupSignalFlags RequiredSignals { get; init; }
        public PreviewStartupSignalFlags ReceivedSignals { get; init; }
        public string Strategy { get; init; }
        public string? MissingSignals { get; init; }
        public int RecoveryAttemptCount { get; init; }
        public string? LastFailureReason { get; init; }
        public bool FirstVisualConfirmed { get; init; }
        public bool BlankSuspected { get; init; }
        public bool Stalled { get; init; }
        public string RendererMode { get; init; }
    }

    private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            PlaybackState = previewRuntime.GpuPlaybackState,
            NaturalVideoWidth = previewRuntime.GpuNaturalVideoWidth,
            NaturalVideoHeight = previewRuntime.GpuNaturalVideoHeight,
            PositionMs = previewRuntime.GpuPositionMs,
            PositionEventCount = previewRuntime.GpuPositionEventCount
        };

    private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(
        PreviewRuntimeGpuPlaybackProjection gpuPlayback)
        => new()
        {
            PlaybackState = gpuPlayback.PlaybackState,
            NaturalVideoWidth = gpuPlayback.NaturalVideoWidth,
            NaturalVideoHeight = gpuPlayback.NaturalVideoHeight,
            PositionMs = gpuPlayback.PositionMs,
            PositionEventCount = gpuPlayback.PositionEventCount
        };

    private readonly record struct PreviewRuntimeGpuPlaybackProjection
    {
        public string PlaybackState { get; init; }
        public int NaturalVideoWidth { get; init; }
        public int NaturalVideoHeight { get; init; }
        public double PositionMs { get; init; }
        public long PositionEventCount { get; init; }
    }

    private readonly record struct PreviewRuntimeGpuPlaybackFlattenedProjection
    {
        public string PlaybackState { get; init; }
        public int NaturalVideoWidth { get; init; }
        public int NaturalVideoHeight { get; init; }
        public double PositionMs { get; init; }
        public long PositionEventCount { get; init; }
    }

    private readonly record struct PreviewRuntimeFlattenedProjection
    {
        public PreviewRuntimeFrameFlattenedProjection Frame { get; init; }
        public PreviewRuntimeCadenceFlattenedProjection Cadence { get; init; }
        public PreviewRuntimeSurfaceFlattenedProjection Surface { get; init; }
        public PreviewRuntimeStartupFlattenedProjection Startup { get; init; }
        public PreviewRuntimeGpuPlaybackFlattenedProjection GpuPlayback { get; init; }
        public PreviewRuntimeColorFlattenedProjection Color { get; init; }
    }

    private static PreviewD3DProjection BuildPreviewD3DProjection(
        PreviewRuntimeSnapshot previewRuntime,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
    {
        var cpuTiming = BuildPreviewD3DCpuTimingProjection(previewRuntime);
        var frameFlow = BuildPreviewD3DFrameFlowProjection(previewRuntime);
        var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);
        var pipelineLatency = BuildPreviewD3DPipelineLatencyProjection(previewRuntime);
        var frameStats = BuildPreviewD3DFrameStatsProjection(
            previewRuntime,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);

        return new()
        {
            PresentSyncInterval = previewRuntime.D3DPresentSyncInterval,
            MaxFrameLatency = previewRuntime.D3DMaxFrameLatency,
            SwapChainBufferCount = previewRuntime.D3DSwapChainBufferCount,
            SwapChainAddress = previewRuntime.D3DSwapChainAddress,
            FramesSubmitted = previewRuntime.D3DFramesSubmitted,
            FramesRendered = previewRuntime.D3DFramesRendered,
            FramesDropped = previewRuntime.D3DFramesDropped,
            RenderThreadFailureCount = previewRuntime.D3DRenderThreadFailureCount,
            LastRenderThreadFailureType = previewRuntime.D3DLastRenderThreadFailureType,
            LastRenderThreadFailureMessage = previewRuntime.D3DLastRenderThreadFailureMessage,
            LastRenderThreadFailureHResult = previewRuntime.D3DLastRenderThreadFailureHResult,
            PendingFrameCount = previewRuntime.D3DPendingFrameCount,
            InputColorSpace = previewRuntime.D3DInputColorSpace,
            OutputColorSpace = previewRuntime.D3DOutputColorSpace,
            CpuTiming = cpuTiming,
            FrameLatencyWait = frameLatencyWait,
            PipelineLatency = pipelineLatency,
            FrameStats = frameStats,
            FrameFlow = frameFlow
        };
    }

    private readonly record struct PreviewD3DProjection
    {
        public int PresentSyncInterval { get; init; }
        public int MaxFrameLatency { get; init; }
        public int SwapChainBufferCount { get; init; }
        public string SwapChainAddress { get; init; }
        public long FramesSubmitted { get; init; }
        public long FramesRendered { get; init; }
        public long FramesDropped { get; init; }
        public long RenderThreadFailureCount { get; init; }
        public string LastRenderThreadFailureType { get; init; }
        public string LastRenderThreadFailureMessage { get; init; }
        public int LastRenderThreadFailureHResult { get; init; }
        public int PendingFrameCount { get; init; }
        public string InputColorSpace { get; init; }
        public string OutputColorSpace { get; init; }
        public PreviewD3DCpuTimingProjection CpuTiming { get; init; }
        public PreviewD3DFrameLatencyWaitProjection FrameLatencyWait { get; init; }
        public PreviewD3DPipelineLatencyProjection PipelineLatency { get; init; }
        public PreviewD3DFrameStatsProjection FrameStats { get; init; }
        public PreviewD3DFrameFlowProjection FrameFlow { get; init; }
    }

    private static PreviewD3DCpuTimingProjection BuildPreviewD3DCpuTimingProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            SampleCount = previewRuntime.D3DCpuTimingSampleCount,
            InputUploadAvgMs = previewRuntime.D3DInputUploadCpuAvgMs,
            InputUploadP95Ms = previewRuntime.D3DInputUploadCpuP95Ms,
            InputUploadP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,
            InputUploadMaxMs = previewRuntime.D3DInputUploadCpuMaxMs,
            RenderSubmitAvgMs = previewRuntime.D3DRenderSubmitCpuAvgMs,
            RenderSubmitP95Ms = previewRuntime.D3DRenderSubmitCpuP95Ms,
            RenderSubmitP99Ms = previewRuntime.D3DRenderSubmitCpuP99Ms,
            RenderSubmitMaxMs = previewRuntime.D3DRenderSubmitCpuMaxMs,
            PresentCallAvgMs = previewRuntime.D3DPresentCallAvgMs,
            PresentCallP95Ms = previewRuntime.D3DPresentCallP95Ms,
            PresentCallP99Ms = previewRuntime.D3DPresentCallP99Ms,
            PresentCallMaxMs = previewRuntime.D3DPresentCallMaxMs,
            TotalFrameAvgMs = previewRuntime.D3DTotalFrameCpuAvgMs,
            TotalFrameP95Ms = previewRuntime.D3DTotalFrameCpuP95Ms,
            TotalFrameP99Ms = previewRuntime.D3DTotalFrameCpuP99Ms,
            TotalFrameMaxMs = previewRuntime.D3DTotalFrameCpuMaxMs
        };

    private readonly record struct PreviewD3DCpuTimingProjection
    {
        public int SampleCount { get; init; }
        public double InputUploadAvgMs { get; init; }
        public double InputUploadP95Ms { get; init; }
        public double InputUploadP99Ms { get; init; }
        public double InputUploadMaxMs { get; init; }
        public double RenderSubmitAvgMs { get; init; }
        public double RenderSubmitP95Ms { get; init; }
        public double RenderSubmitP99Ms { get; init; }
        public double RenderSubmitMaxMs { get; init; }
        public double PresentCallAvgMs { get; init; }
        public double PresentCallP95Ms { get; init; }
        public double PresentCallP99Ms { get; init; }
        public double PresentCallMaxMs { get; init; }
        public double TotalFrameAvgMs { get; init; }
        public double TotalFrameP95Ms { get; init; }
        public double TotalFrameP99Ms { get; init; }
        public double TotalFrameMaxMs { get; init; }
    }

    private static PreviewD3DPipelineLatencyProjection BuildPreviewD3DPipelineLatencyProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            SampleCount = previewRuntime.D3DPipelineLatencySampleCount,
            AvgMs = previewRuntime.D3DPipelineLatencyAvgMs,
            P95Ms = previewRuntime.D3DPipelineLatencyP95Ms,
            P99Ms = previewRuntime.D3DPipelineLatencyP99Ms,
            MaxMs = previewRuntime.D3DPipelineLatencyMaxMs
        };

    private readonly record struct PreviewD3DPipelineLatencyProjection
    {
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
    }

    private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            Enabled = previewRuntime.D3DFrameLatencyWaitEnabled,
            HandleActive = previewRuntime.D3DFrameLatencyWaitHandleActive,
            CallCount = previewRuntime.D3DFrameLatencyWaitCallCount,
            SignaledCount = previewRuntime.D3DFrameLatencyWaitSignaledCount,
            TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,
            UnexpectedResultCount = previewRuntime.D3DFrameLatencyWaitUnexpectedResultCount,
            LastResult = previewRuntime.D3DFrameLatencyWaitLastResult,
            LastMs = previewRuntime.D3DFrameLatencyWaitLastMs,
            SampleCount = previewRuntime.D3DFrameLatencyWaitSampleCount,
            AvgMs = previewRuntime.D3DFrameLatencyWaitAvgMs,
            P95Ms = previewRuntime.D3DFrameLatencyWaitP95Ms,
            P99Ms = previewRuntime.D3DFrameLatencyWaitP99Ms,
            MaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs
        };

    private readonly record struct PreviewD3DFrameLatencyWaitProjection
    {
        public bool Enabled { get; init; }
        public bool HandleActive { get; init; }
        public long CallCount { get; init; }
        public long SignaledCount { get; init; }
        public long TimeoutCount { get; init; }
        public long UnexpectedResultCount { get; init; }
        public uint LastResult { get; init; }
        public double LastMs { get; init; }
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
    }

    private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(
        PreviewRuntimeSnapshot previewRuntime,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
        => new()
        {
            SampleCount = previewRuntime.D3DFrameStatsSampleCount,
            SuccessCount = previewRuntime.D3DFrameStatsSuccessCount,
            FailureCount = previewRuntime.D3DFrameStatsFailureCount,
            LastError = previewRuntime.D3DFrameStatsLastError,
            PresentCount = previewRuntime.D3DFrameStatsPresentCount,
            PresentRefreshCount = previewRuntime.D3DFrameStatsPresentRefreshCount,
            SyncRefreshCount = previewRuntime.D3DFrameStatsSyncRefreshCount,
            SyncQpcTime = previewRuntime.D3DFrameStatsSyncQpcTime,
            LastPresentDelta = previewRuntime.D3DFrameStatsLastPresentDelta,
            LastPresentRefreshDelta = previewRuntime.D3DFrameStatsLastPresentRefreshDelta,
            LastSyncRefreshDelta = previewRuntime.D3DFrameStatsLastSyncRefreshDelta,
            MissedRefreshCount = previewRuntime.D3DFrameStatsMissedRefreshCount,
            RecentMissedRefreshCount = recentD3DMissedRefreshes,
            RecentFailureCount = recentD3DStatsFailures
        };

    private readonly record struct PreviewD3DFrameStatsProjection
    {
        public long SampleCount { get; init; }
        public long SuccessCount { get; init; }
        public long FailureCount { get; init; }
        public string LastError { get; init; }
        public long PresentCount { get; init; }
        public long PresentRefreshCount { get; init; }
        public long SyncRefreshCount { get; init; }
        public long SyncQpcTime { get; init; }
        public long LastPresentDelta { get; init; }
        public long LastPresentRefreshDelta { get; init; }
        public long LastSyncRefreshDelta { get; init; }
        public long MissedRefreshCount { get; init; }
        public long RecentMissedRefreshCount { get; init; }
        public long RecentFailureCount { get; init; }
    }

    private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            LastSubmittedPreviewPresentId = previewRuntime.D3DLastSubmittedPreviewPresentId,
            LastSubmittedSourceSequenceNumber = previewRuntime.D3DLastSubmittedSourceSequenceNumber,
            LastSubmittedSourcePtsTicks = previewRuntime.D3DLastSubmittedSourcePtsTicks,
            LastSubmittedQpc = previewRuntime.D3DLastSubmittedQpc,
            LastSubmittedUtcUnixMs = previewRuntime.D3DLastSubmittedUtcUnixMs,
            LastRenderedPreviewPresentId = previewRuntime.D3DLastRenderedPreviewPresentId,
            LastRenderedSourceSequenceNumber = previewRuntime.D3DLastRenderedSourceSequenceNumber,
            LastRenderedSourcePtsTicks = previewRuntime.D3DLastRenderedSourcePtsTicks,
            LastRenderedQpc = previewRuntime.D3DLastRenderedQpc,
            LastRenderedUtcUnixMs = previewRuntime.D3DLastRenderedUtcUnixMs,
            LastRenderedSchedulerToPresentMs = previewRuntime.D3DLastRenderedSchedulerToPresentMs,
            LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,
            LastDroppedPreviewPresentId = previewRuntime.D3DLastDroppedPreviewPresentId,
            LastDroppedSourceSequenceNumber = previewRuntime.D3DLastDroppedSourceSequenceNumber,
            LastDroppedSourcePtsTicks = previewRuntime.D3DLastDroppedSourcePtsTicks,
            LastDroppedQpc = previewRuntime.D3DLastDroppedQpc,
            LastDroppedUtcUnixMs = previewRuntime.D3DLastDroppedUtcUnixMs,
            LastDropReason = previewRuntime.D3DLastDropReason,
            RecentSlowFrames = previewRuntime.D3DRecentSlowFrames
        };

    private readonly record struct PreviewD3DFrameFlowProjection
    {
        public long LastSubmittedPreviewPresentId { get; init; }
        public long LastSubmittedSourceSequenceNumber { get; init; }
        public long LastSubmittedSourcePtsTicks { get; init; }
        public long LastSubmittedQpc { get; init; }
        public long LastSubmittedUtcUnixMs { get; init; }
        public long LastRenderedPreviewPresentId { get; init; }
        public long LastRenderedSourceSequenceNumber { get; init; }
        public long LastRenderedSourcePtsTicks { get; init; }
        public long LastRenderedQpc { get; init; }
        public long LastRenderedUtcUnixMs { get; init; }
        public double LastRenderedSchedulerToPresentMs { get; init; }
        public double LastRenderedPipelineLatencyMs { get; init; }
        public long LastDroppedPreviewPresentId { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDroppedSourcePtsTicks { get; init; }
        public long LastDroppedQpc { get; init; }
        public long LastDroppedUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public PreviewSlowFrameDiagnostic[] RecentSlowFrames { get; init; }
    }

    private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(
        PreviewD3DProjection previewD3D)
        => new()
        {
            PresentSyncInterval = previewD3D.PresentSyncInterval,
            MaxFrameLatency = previewD3D.MaxFrameLatency,
            SwapChainBufferCount = previewD3D.SwapChainBufferCount,
            SwapChainAddress = previewD3D.SwapChainAddress,
            FramesSubmitted = previewD3D.FramesSubmitted,
            FramesRendered = previewD3D.FramesRendered,
            FramesDropped = previewD3D.FramesDropped,
            RenderThreadFailureCount = previewD3D.RenderThreadFailureCount,
            LastRenderThreadFailureType = previewD3D.LastRenderThreadFailureType,
            LastRenderThreadFailureMessage = previewD3D.LastRenderThreadFailureMessage,
            LastRenderThreadFailureHResult = previewD3D.LastRenderThreadFailureHResult,
            PendingFrameCount = previewD3D.PendingFrameCount,
            InputColorSpace = previewD3D.InputColorSpace,
            OutputColorSpace = previewD3D.OutputColorSpace,
            CpuTiming = BuildPreviewD3DCpuTimingFlattenedProjection(previewD3D.CpuTiming),
            LatencyAndStats = BuildPreviewD3DLatencyAndStatsFlattenedProjection(
                previewD3D.PipelineLatency,
                previewD3D.FrameLatencyWait,
                previewD3D.FrameStats),
            FrameFlow = BuildPreviewD3DFrameFlowFlattenedProjection(previewD3D.FrameFlow)
        };

    private readonly record struct PreviewD3DFlattenedProjection
    {
        public int PresentSyncInterval { get; init; }
        public int MaxFrameLatency { get; init; }
        public int SwapChainBufferCount { get; init; }
        public string SwapChainAddress { get; init; }
        public long FramesSubmitted { get; init; }
        public long FramesRendered { get; init; }
        public long FramesDropped { get; init; }
        public long RenderThreadFailureCount { get; init; }
        public string LastRenderThreadFailureType { get; init; }
        public string LastRenderThreadFailureMessage { get; init; }
        public int LastRenderThreadFailureHResult { get; init; }
        public int PendingFrameCount { get; init; }
        public string InputColorSpace { get; init; }
        public string OutputColorSpace { get; init; }
        public PreviewD3DCpuTimingFlattenedProjection CpuTiming { get; init; }
        public PreviewD3DLatencyAndStatsFlattenedProjection LatencyAndStats { get; init; }
        public PreviewD3DFrameFlowFlattenedProjection FrameFlow { get; init; }
    }

    private static PreviewD3DCpuTimingFlattenedProjection BuildPreviewD3DCpuTimingFlattenedProjection(
        PreviewD3DCpuTimingProjection cpuTiming)
        => new()
        {
            SampleCount = cpuTiming.SampleCount,
            InputUploadCpuAvgMs = cpuTiming.InputUploadAvgMs,
            InputUploadCpuP95Ms = cpuTiming.InputUploadP95Ms,
            InputUploadCpuP99Ms = cpuTiming.InputUploadP99Ms,
            InputUploadCpuMaxMs = cpuTiming.InputUploadMaxMs,
            RenderSubmitCpuAvgMs = cpuTiming.RenderSubmitAvgMs,
            RenderSubmitCpuP95Ms = cpuTiming.RenderSubmitP95Ms,
            RenderSubmitCpuP99Ms = cpuTiming.RenderSubmitP99Ms,
            RenderSubmitCpuMaxMs = cpuTiming.RenderSubmitMaxMs,
            PresentCallAvgMs = cpuTiming.PresentCallAvgMs,
            PresentCallP95Ms = cpuTiming.PresentCallP95Ms,
            PresentCallP99Ms = cpuTiming.PresentCallP99Ms,
            PresentCallMaxMs = cpuTiming.PresentCallMaxMs,
            TotalFrameCpuAvgMs = cpuTiming.TotalFrameAvgMs,
            TotalFrameCpuP95Ms = cpuTiming.TotalFrameP95Ms,
            TotalFrameCpuP99Ms = cpuTiming.TotalFrameP99Ms,
            TotalFrameCpuMaxMs = cpuTiming.TotalFrameMaxMs
        };

    private readonly record struct PreviewD3DCpuTimingFlattenedProjection
    {
        public int SampleCount { get; init; }
        public double InputUploadCpuAvgMs { get; init; }
        public double InputUploadCpuP95Ms { get; init; }
        public double InputUploadCpuP99Ms { get; init; }
        public double InputUploadCpuMaxMs { get; init; }
        public double RenderSubmitCpuAvgMs { get; init; }
        public double RenderSubmitCpuP95Ms { get; init; }
        public double RenderSubmitCpuP99Ms { get; init; }
        public double RenderSubmitCpuMaxMs { get; init; }
        public double PresentCallAvgMs { get; init; }
        public double PresentCallP95Ms { get; init; }
        public double PresentCallP99Ms { get; init; }
        public double PresentCallMaxMs { get; init; }
        public double TotalFrameCpuAvgMs { get; init; }
        public double TotalFrameCpuP95Ms { get; init; }
        public double TotalFrameCpuP99Ms { get; init; }
        public double TotalFrameCpuMaxMs { get; init; }
    }

    private static PreviewD3DLatencyAndStatsFlattenedProjection BuildPreviewD3DLatencyAndStatsFlattenedProjection(
        PreviewD3DPipelineLatencyProjection pipelineLatency,
        PreviewD3DFrameLatencyWaitProjection frameLatencyWait,
        PreviewD3DFrameStatsProjection frameStats)
        => new()
        {
            PipelineLatencySampleCount = pipelineLatency.SampleCount,
            PipelineLatencyAvgMs = pipelineLatency.AvgMs,
            PipelineLatencyP95Ms = pipelineLatency.P95Ms,
            PipelineLatencyP99Ms = pipelineLatency.P99Ms,
            PipelineLatencyMaxMs = pipelineLatency.MaxMs,
            FrameLatencyWaitEnabled = frameLatencyWait.Enabled,
            FrameLatencyWaitHandleActive = frameLatencyWait.HandleActive,
            FrameLatencyWaitCallCount = frameLatencyWait.CallCount,
            FrameLatencyWaitSignaledCount = frameLatencyWait.SignaledCount,
            FrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount,
            FrameLatencyWaitUnexpectedResultCount = frameLatencyWait.UnexpectedResultCount,
            FrameLatencyWaitLastResult = frameLatencyWait.LastResult,
            FrameLatencyWaitLastMs = frameLatencyWait.LastMs,
            FrameLatencyWaitSampleCount = frameLatencyWait.SampleCount,
            FrameLatencyWaitAvgMs = frameLatencyWait.AvgMs,
            FrameLatencyWaitP95Ms = frameLatencyWait.P95Ms,
            FrameLatencyWaitP99Ms = frameLatencyWait.P99Ms,
            FrameLatencyWaitMaxMs = frameLatencyWait.MaxMs,
            FrameStatsSampleCount = frameStats.SampleCount,
            FrameStatsSuccessCount = frameStats.SuccessCount,
            FrameStatsFailureCount = frameStats.FailureCount,
            FrameStatsLastError = frameStats.LastError,
            FrameStatsPresentCount = frameStats.PresentCount,
            FrameStatsPresentRefreshCount = frameStats.PresentRefreshCount,
            FrameStatsSyncRefreshCount = frameStats.SyncRefreshCount,
            FrameStatsSyncQpcTime = frameStats.SyncQpcTime,
            FrameStatsLastPresentDelta = frameStats.LastPresentDelta,
            FrameStatsLastPresentRefreshDelta = frameStats.LastPresentRefreshDelta,
            FrameStatsLastSyncRefreshDelta = frameStats.LastSyncRefreshDelta,
            FrameStatsMissedRefreshCount = frameStats.MissedRefreshCount,
            FrameStatsRecentMissedRefreshCount = frameStats.RecentMissedRefreshCount,
            FrameStatsRecentFailureCount = frameStats.RecentFailureCount
        };

    private readonly record struct PreviewD3DLatencyAndStatsFlattenedProjection
    {
        public int PipelineLatencySampleCount { get; init; }
        public double PipelineLatencyAvgMs { get; init; }
        public double PipelineLatencyP95Ms { get; init; }
        public double PipelineLatencyP99Ms { get; init; }
        public double PipelineLatencyMaxMs { get; init; }
        public bool FrameLatencyWaitEnabled { get; init; }
        public bool FrameLatencyWaitHandleActive { get; init; }
        public long FrameLatencyWaitCallCount { get; init; }
        public long FrameLatencyWaitSignaledCount { get; init; }
        public long FrameLatencyWaitTimeoutCount { get; init; }
        public long FrameLatencyWaitUnexpectedResultCount { get; init; }
        public uint FrameLatencyWaitLastResult { get; init; }
        public double FrameLatencyWaitLastMs { get; init; }
        public int FrameLatencyWaitSampleCount { get; init; }
        public double FrameLatencyWaitAvgMs { get; init; }
        public double FrameLatencyWaitP95Ms { get; init; }
        public double FrameLatencyWaitP99Ms { get; init; }
        public double FrameLatencyWaitMaxMs { get; init; }
        public long FrameStatsSampleCount { get; init; }
        public long FrameStatsSuccessCount { get; init; }
        public long FrameStatsFailureCount { get; init; }
        public string FrameStatsLastError { get; init; }
        public long FrameStatsPresentCount { get; init; }
        public long FrameStatsPresentRefreshCount { get; init; }
        public long FrameStatsSyncRefreshCount { get; init; }
        public long FrameStatsSyncQpcTime { get; init; }
        public long FrameStatsLastPresentDelta { get; init; }
        public long FrameStatsLastPresentRefreshDelta { get; init; }
        public long FrameStatsLastSyncRefreshDelta { get; init; }
        public long FrameStatsMissedRefreshCount { get; init; }
        public long FrameStatsRecentMissedRefreshCount { get; init; }
        public long FrameStatsRecentFailureCount { get; init; }
    }

    private static PreviewD3DFrameFlowFlattenedProjection BuildPreviewD3DFrameFlowFlattenedProjection(
        PreviewD3DFrameFlowProjection frameFlow)
        => new()
        {
            LastSubmittedPreviewPresentId = frameFlow.LastSubmittedPreviewPresentId,
            LastSubmittedSourceSequenceNumber = frameFlow.LastSubmittedSourceSequenceNumber,
            LastSubmittedSourcePtsTicks = frameFlow.LastSubmittedSourcePtsTicks,
            LastSubmittedQpc = frameFlow.LastSubmittedQpc,
            LastSubmittedUtcUnixMs = frameFlow.LastSubmittedUtcUnixMs,
            LastRenderedPreviewPresentId = frameFlow.LastRenderedPreviewPresentId,
            LastRenderedSourceSequenceNumber = frameFlow.LastRenderedSourceSequenceNumber,
            LastRenderedSourcePtsTicks = frameFlow.LastRenderedSourcePtsTicks,
            LastRenderedQpc = frameFlow.LastRenderedQpc,
            LastRenderedUtcUnixMs = frameFlow.LastRenderedUtcUnixMs,
            LastRenderedSchedulerToPresentMs = frameFlow.LastRenderedSchedulerToPresentMs,
            LastRenderedPipelineLatencyMs = frameFlow.LastRenderedPipelineLatencyMs,
            LastDroppedPreviewPresentId = frameFlow.LastDroppedPreviewPresentId,
            LastDroppedSourceSequenceNumber = frameFlow.LastDroppedSourceSequenceNumber,
            LastDroppedSourcePtsTicks = frameFlow.LastDroppedSourcePtsTicks,
            LastDroppedQpc = frameFlow.LastDroppedQpc,
            LastDroppedUtcUnixMs = frameFlow.LastDroppedUtcUnixMs,
            LastDropReason = frameFlow.LastDropReason,
            RecentSlowFrames = frameFlow.RecentSlowFrames
        };

    private readonly record struct PreviewD3DFrameFlowFlattenedProjection
    {
        public long LastSubmittedPreviewPresentId { get; init; }
        public long LastSubmittedSourceSequenceNumber { get; init; }
        public long LastSubmittedSourcePtsTicks { get; init; }
        public long LastSubmittedQpc { get; init; }
        public long LastSubmittedUtcUnixMs { get; init; }
        public long LastRenderedPreviewPresentId { get; init; }
        public long LastRenderedSourceSequenceNumber { get; init; }
        public long LastRenderedSourcePtsTicks { get; init; }
        public long LastRenderedQpc { get; init; }
        public long LastRenderedUtcUnixMs { get; init; }
        public double LastRenderedSchedulerToPresentMs { get; init; }
        public double LastRenderedPipelineLatencyMs { get; init; }
        public long LastDroppedPreviewPresentId { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDroppedSourcePtsTicks { get; init; }
        public long LastDroppedQpc { get; init; }
        public long LastDroppedUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public PreviewSlowFrameDiagnostic[] RecentSlowFrames { get; init; }
    }
}
