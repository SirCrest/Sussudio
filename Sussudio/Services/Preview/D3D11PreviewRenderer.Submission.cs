using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Services.Contracts;
using Sussudio.Services.Runtime;
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
    private bool _loggedNv12ShaderMissing;
    private int _lastNv12IsHdr = -1; // tri-state: -1 = unset, 0 = SDR, 1 = HDR

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

    public void SubmitRawFrame(
        IntPtr data,
        int dataLength,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        if (data == IntPtr.Zero || dataLength <= 0 || width <= 0 || height <= 0)
        {
            return;
        }

        var copied = ArrayPool<byte>.Shared.Rent(dataLength);
        try
        {
            Marshal.Copy(data, copied, 0, dataLength);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(copied);
            throw;
        }

        var frame = new PendingFrame(
            null,
            0,
            copied,
            dataLength,
            width,
            height,
            isHdr,
            tracking.ArrivalTick,
            tracking.SourceSequenceNumber,
            tracking.PreviewPresentId,
            tracking.SchedulerSubmitTick,
            tracking.SourcePtsTicks,
            countForPresentCadence: tracking.CountForPresentCadence);
        EnqueuePendingFrame(frame);
    }

    public void SubmitRawFrameLease(
        PooledVideoFrameLease frame,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            frame.Dispose();
            return;
        }

        if (frame.Length <= 0 || frame.Width <= 0 || frame.Height <= 0)
        {
            frame.Dispose();
            return;
        }

        EnqueuePendingFrame(new PendingFrame(
            null,
            0,
            null,
            frame.Length,
            frame.Width,
            frame.Height,
            isHdr,
            frame.ArrivalTick,
            frame.SequenceNumber,
            tracking.PreviewPresentId,
            tracking.SchedulerSubmitTick,
            sourcePtsTicks: 0,
            frameLease: frame,
            countForPresentCadence: tracking.CountForPresentCadence));
    }

    public void SubmitTexture(
        IntPtr d3dTexture,
        int subresourceIndex,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        if (d3dTexture == IntPtr.Zero || width <= 0 || height <= 0)
        {
            return;
        }

        if (Volatile.Read(ref _sharedDeviceActive) == 0)
        {
            throw new InvalidOperationException("Shared D3D11 device is not active for texture submission.");
        }

        IntPtr ownedTexturePtr = IntPtr.Zero;
        ID3D11Texture2D? texture = null;
        try
        {
            Marshal.AddRef(d3dTexture);
            ownedTexturePtr = d3dTexture;
            texture = new ID3D11Texture2D(ownedTexturePtr);
        }
        catch
        {
            texture?.Dispose();
            if (texture == null && ownedTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(ownedTexturePtr);
            }

            throw;
        }

        var frame = new PendingFrame(
            texture,
            subresourceIndex,
            null,
            0,
            width,
            height,
            isHdr,
            tracking.ArrivalTick,
            tracking.SourceSequenceNumber,
            tracking.PreviewPresentId,
            tracking.SchedulerSubmitTick,
            tracking.SourcePtsTicks,
            countForPresentCadence: tracking.CountForPresentCadence);
        EnqueuePendingFrame(frame);
    }

    public void SubmitNv12PlaneTextures(
        IntPtr yTexturePtr,
        IntPtr uvTexturePtr,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        if (yTexturePtr == IntPtr.Zero || uvTexturePtr == IntPtr.Zero || width <= 0 || height <= 0)
        {
            return;
        }

        if (_nv12PS == null)
        {
            if (!_loggedNv12ShaderMissing)
            {
                Logger.Log("D3D11_RENDERER_WARN NV12 pixel shader not available - frames will be dropped via this path");
                _loggedNv12ShaderMissing = true;
            }
            return;
        }

        if (Volatile.Read(ref _sharedDeviceActive) == 0)
        {
            throw new InvalidOperationException("Shared D3D11 device is not active for NV12 texture submission.");
        }

        // Log the first frame and any HDR/SDR transition through the NV12 plane path.
        var hdrInt = isHdr ? 1 : 0;
        var prev = Interlocked.Exchange(ref _lastNv12IsHdr, hdrInt);
        if (prev != hdrInt)
        {
            var prevLabel = prev == -1 ? "unset" : (prev == 1 ? "HDR" : "SDR");
            var curLabel = isHdr ? "HDR" : "SDR";
            Logger.Log($"D3D11_PREVIEW_NV12_HDR_TRANSITION from={prevLabel} to={curLabel} pathTag=PlaneTextures");
        }

        IntPtr ownedYTexturePtr = IntPtr.Zero;
        IntPtr ownedUvTexturePtr = IntPtr.Zero;
        ID3D11Texture2D? yTexture = null;
        ID3D11Texture2D? uvTexture = null;
        try
        {
            Marshal.AddRef(yTexturePtr);
            ownedYTexturePtr = yTexturePtr;
            yTexture = new ID3D11Texture2D(ownedYTexturePtr);

            Marshal.AddRef(uvTexturePtr);
            ownedUvTexturePtr = uvTexturePtr;
            uvTexture = new ID3D11Texture2D(ownedUvTexturePtr);

            EnqueueNv12Frame(
                ownedYTexturePtr,
                yTexture,
                ownedUvTexturePtr,
                uvTexture,
                width,
                height,
                isHdr,
                tracking);
        }
        catch
        {
            uvTexture?.Dispose();
            if (uvTexture == null && ownedUvTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(ownedUvTexturePtr);
            }

            yTexture?.Dispose();
            if (yTexture == null && ownedYTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(ownedYTexturePtr);
            }

            throw;
        }
    }

    private void EnqueueNv12Frame(
        IntPtr yTexturePtr,
        ID3D11Texture2D yTexture,
        IntPtr uvTexturePtr,
        ID3D11Texture2D uvTexture,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        var frame = new PendingFrame(
            d3dTexture: null,
            d3dSubresourceIndex: 0,
            rawData: null,
            rawDataLength: 0,
            width: width,
            height: height,
            isHdr: isHdr,
            arrivalTick: tracking.ArrivalTick,
            sourceSequenceNumber: tracking.SourceSequenceNumber,
            previewPresentId: tracking.PreviewPresentId,
            schedulerSubmitTick: tracking.SchedulerSubmitTick,
            sourcePtsTicks: tracking.SourcePtsTicks,
            d3dTextureY: yTexturePtr,
            d3dTextureUV: uvTexturePtr,
            d3dTextureYObject: yTexture,
            d3dTextureUVObject: uvTexture,
            countForPresentCadence: tracking.CountForPresentCadence);
        EnqueuePendingFrame(frame);
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
