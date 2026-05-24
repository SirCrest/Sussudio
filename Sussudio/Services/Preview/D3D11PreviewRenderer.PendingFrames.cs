using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Services.Contracts;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal interface IPreviewFrameQueueControl
{
    int DropPendingFrames(string reason);
}

internal sealed partial class D3D11PreviewRenderer
{
    private readonly ManualResetEventSlim _frameReadyEvent = new(false);
    private readonly ConcurrentQueue<PendingFrame> _pendingFrames = new();
    private int _pendingFrameCount;

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

    private void EnqueuePendingFrame(PendingFrame frame)
    {
        lock (_lifecycleLock)
        {
            if (Volatile.Read(ref _disposed) != 0 ||
                Volatile.Read(ref _stopRequested) != 0 ||
                _renderThread == null)
            {
                TrackFrameDropped(frame, "renderer-stopped");
                frame.Dispose();
                return;
            }

            frame.SubmissionGeneration = Interlocked.Read(ref _submissionGeneration);
            var pendingFrameCount = Interlocked.Increment(ref _pendingFrameCount);
            _pendingFrames.Enqueue(frame);
            TrackFrameSubmitted(frame);

            // Trim oldest frames if the queue exceeds the elastic limit.
            // Under normal operation the render thread keeps up and the queue
            // stays at 0-1 (no added latency). The extra slots only absorb
            // brief render hiccups instead of dropping frames.
            while (pendingFrameCount > _maxPendingFrames)
            {
                if (TryDequeuePendingFrame(out var oldest))
                {
                    TrackFrameDropped(oldest, "renderer-backlog");
                    oldest.Dispose();
                    pendingFrameCount = PendingFrameCount;
                }
                else
                {
                    Interlocked.Exchange(ref _pendingFrameCount, 0);
                    break;
                }
            }

            Volatile.Write(ref _naturalWidth, frame.Width);
            Volatile.Write(ref _naturalHeight, frame.Height);
            Interlocked.Increment(ref _framesSubmitted);
        }

        SignalFrameReady("pending_frame");
    }

    private void SignalFrameReady(string operation)
    {
        try
        {
            _frameReadyEvent.Set();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"D3D11_PREVIEW_FRAME_SIGNAL_SKIPPED op={operation} reason=disposed");
        }
    }

    private void ResetFrameReady(string operation)
    {
        try
        {
            _frameReadyEvent.Reset();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"D3D11_PREVIEW_FRAME_RESET_SKIPPED op={operation} reason=disposed");
        }
    }

    private bool TryDequeuePendingFrame(out PendingFrame frame)
    {
        if (_pendingFrames.TryDequeue(out var dequeued))
        {
            frame = dequeued;
            DecrementPendingFrameCount();
            return true;
        }

        frame = null!;
        return false;
    }

    public int DropPendingFrames(string reason)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? "explicit-drain"
            : reason.Trim();
        var dropped = 0;
        Volatile.Write(ref _submissionGenerationDropReason, normalizedReason);
        Interlocked.Increment(ref _submissionGeneration);
        lock (_lifecycleLock)
        {
            while (TryDequeuePendingFrame(out var stale))
            {
                TrackFrameDropped(stale, normalizedReason);
                stale.Dispose();
                dropped++;
            }
        }

        if (dropped > 0)
        {
            Logger.Log($"D3D11_PREVIEW_PENDING_DRAIN reason={normalizedReason} dropped={dropped}");
            if (_pendingFrames.IsEmpty &&
                Volatile.Read(ref _compositionTransformDirty) == 0 &&
                Volatile.Read(ref _sharedDeviceResetPending) == 0)
            {
                ResetFrameReady("pending_drain");
            }
        }

        return dropped;
    }

    private void DecrementPendingFrameCount()
    {
        while (true)
        {
            var current = Volatile.Read(ref _pendingFrameCount);
            if (current <= 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _pendingFrameCount, current - 1, current) == current)
            {
                return;
            }
        }
    }
}
