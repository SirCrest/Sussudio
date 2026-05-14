using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Sussudio.Services.Contracts;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private sealed class PendingFrame : IDisposable
    {
        public PendingFrame(
            ID3D11Texture2D? d3dTexture,
            int d3dSubresourceIndex,
            byte[]? rawData,
            int rawDataLength,
            int width,
            int height,
            bool isHdr,
            long arrivalTick,
            long sourceSequenceNumber = -1,
            long previewPresentId = 0,
            long schedulerSubmitTick = 0,
            long sourcePtsTicks = 0,
            PooledVideoFrameLease? frameLease = null,
            IntPtr d3dTextureY = default,
            IntPtr d3dTextureUV = default,
            ID3D11Texture2D? d3dTextureYObject = null,
            ID3D11Texture2D? d3dTextureUVObject = null,
            bool countForPresentCadence = true)
        {
            D3DTexture = d3dTexture;
            D3DSubresourceIndex = Math.Max(0, d3dSubresourceIndex);
            RawData = rawData;
            RawDataLength = rawDataLength;
            Width = width;
            Height = height;
            IsHdr = isHdr;
            ArrivalTick = arrivalTick;
            SourceSequenceNumber = sourceSequenceNumber;
            PreviewPresentId = previewPresentId;
            SourcePtsTicks = sourcePtsTicks;
            SchedulerSubmitTick = schedulerSubmitTick;
            FrameLease = frameLease;
            D3DTextureY = d3dTextureY;
            D3DTextureUV = d3dTextureUV;
            D3DTextureYObject = d3dTextureYObject;
            D3DTextureUVObject = d3dTextureUVObject;
            CountForPresentCadence = countForPresentCadence;
        }

        public ID3D11Texture2D? D3DTexture { get; private set; }
        public int D3DSubresourceIndex { get; }
        public IntPtr D3DTextureY { get; private set; }
        public IntPtr D3DTextureUV { get; private set; }
        public ID3D11Texture2D? D3DTextureYObject { get; private set; }
        public ID3D11Texture2D? D3DTextureUVObject { get; private set; }
        public byte[]? RawData { get; private set; }
        public int RawDataLength { get; private set; }
        public PooledVideoFrameLease? FrameLease { get; private set; }
        public int Width { get; }
        public int Height { get; }
        public bool IsHdr { get; }
        public long ArrivalTick { get; }
        public long SourceSequenceNumber { get; }
        public long PreviewPresentId { get; }
        public long SourcePtsTicks { get; }
        public long SchedulerSubmitTick { get; }
        public bool CountForPresentCadence { get; }
        public long SubmissionGeneration { get; set; }

        public void Dispose()
        {
            D3DTexture?.Dispose();
            D3DTexture = null;
            if (D3DTextureYObject != null)
            {
                D3DTextureYObject.Dispose();
                D3DTextureYObject = null;
                D3DTextureY = IntPtr.Zero;
            }
            else if (D3DTextureY != IntPtr.Zero)
            {
                Marshal.Release(D3DTextureY);
                D3DTextureY = IntPtr.Zero;
            }

            if (D3DTextureUVObject != null)
            {
                D3DTextureUVObject.Dispose();
                D3DTextureUVObject = null;
                D3DTextureUV = IntPtr.Zero;
            }
            else if (D3DTextureUV != IntPtr.Zero)
            {
                Marshal.Release(D3DTextureUV);
                D3DTextureUV = IntPtr.Zero;
            }

            if (RawData != null)
            {
                ArrayPool<byte>.Shared.Return(RawData);
                RawData = null;
                RawDataLength = 0;
            }

            FrameLease?.Dispose();
            FrameLease = null;
        }
    }

    public readonly record struct PresentCadenceMetrics(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double P99IntervalMs,
        double MaxIntervalMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SampleDurationMs,
        double[] RecentIntervalsMs,
        double JitterStdDevMs,
        long SlowFrameCount,
        double SlowFramePercent);

    public readonly record struct CpuStageTimingMetrics(
        int SampleCount,
        double AverageMs,
        double P95Ms,
        double P99Ms,
        double MaxMs);

    public readonly record struct RenderCpuTimingMetrics(
        CpuStageTimingMetrics InputUpload,
        CpuStageTimingMetrics RenderSubmit,
        CpuStageTimingMetrics PresentCall,
        CpuStageTimingMetrics TotalFrame);

    public readonly record struct PipelineLatencyMetrics(
        int SampleCount,
        double AverageMs,
        double P95Ms,
        double P99Ms,
        double MaxMs);

    public readonly record struct FrameLatencyWaitMetrics(
        bool Enabled,
        bool HandleActive,
        long CallCount,
        long SignaledCount,
        long TimeoutCount,
        long UnexpectedResultCount,
        uint LastResult,
        double LastWaitMs,
        CpuStageTimingMetrics Timing);

    public readonly record struct FrameOwnershipMetrics(
        long LastSubmittedPreviewPresentId,
        long LastSubmittedSourceSequenceNumber,
        long LastSubmittedSourcePtsTicks,
        long LastSubmittedQpc,
        long LastSubmittedUtcUnixMs,
        long LastRenderedPreviewPresentId,
        long LastRenderedSourceSequenceNumber,
        long LastRenderedSourcePtsTicks,
        long LastRenderedQpc,
        long LastRenderedUtcUnixMs,
        double LastRenderedSchedulerToPresentMs,
        double LastRenderedPipelineLatencyMs,
        long LastDroppedPreviewPresentId,
        long LastDroppedSourceSequenceNumber,
        long LastDroppedSourcePtsTicks,
        long LastDroppedQpc,
        long LastDroppedUtcUnixMs,
        string LastDropReason);

    public readonly record struct DxgiFrameStatisticsMetrics(
        long SampleCount,
        long SuccessCount,
        long FailureCount,
        string LastError,
        long PresentCount,
        long PresentRefreshCount,
        long SyncRefreshCount,
        long SyncQpcTime,
        long LastPresentDelta,
        long LastPresentRefreshDelta,
        long LastSyncRefreshDelta,
        long MissedRefreshCount);
}
