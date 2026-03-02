using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;

namespace ElgatoCapture.Services;

public sealed class SourceReaderPreviewAdapter : IDisposable
{
    private readonly int _nv12SampleCount;
    private readonly double _fps;
    private readonly MediaStreamSource _streamSource;
    private readonly MediaSource _mediaSource;
    private readonly ManualResetEventSlim _frameReadyEvent = new(false);
    private byte[]? _latestNv12Frame;
    private long _frameIndex;
    private long _framesEnqueued;
    private long _samplesRequested;
    private long _samplesDelivered;
    private long _samplesTimedOut;
    private int _disposed;

    public SourceReaderPreviewAdapter(int width, int height, double fps)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        if (fps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fps), "FPS must be positive.");
        }

        _fps = fps;
        _nv12SampleCount = checked(width * height + width * (height / 2));

        var encoding = VideoEncodingProperties.CreateUncompressed(
            MediaEncodingSubtypes.Nv12,
            (uint)width,
            (uint)height);

        var descriptor = new VideoStreamDescriptor(encoding);
        _streamSource = new MediaStreamSource(descriptor)
        {
            BufferTime = TimeSpan.Zero
        };
        _streamSource.SampleRequested += OnSampleRequested;

        _mediaSource = MediaSource.CreateFromMediaStreamSource(_streamSource);
    }

    public IMediaPlaybackSource PlaybackSource => _mediaSource;
    public long FramesEnqueued => Volatile.Read(ref _framesEnqueued);
    public long SamplesRequested => Volatile.Read(ref _samplesRequested);
    public long SamplesDelivered => Volatile.Read(ref _samplesDelivered);
    public long SamplesTimedOut => Volatile.Read(ref _samplesTimedOut);

    public void EnqueueFrame(ReadOnlySpan<byte> frameData, int width, int height)
    {
        if (IsDisposed)
        {
            return;
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }

        var nv12SampleCount = _nv12SampleCount;
        var expectedP010Bytes = checked(nv12SampleCount * 2);

        if (frameData.Length < expectedP010Bytes)
        {
            return;
        }

        var nv12 = ArrayPool<byte>.Shared.Rent(nv12SampleCount);
        var p010Words = MemoryMarshal.Cast<byte, ushort>(frameData.Slice(0, expectedP010Bytes));
        for (var i = 0; i < nv12SampleCount; i++)
        {
            nv12[i] = (byte)(p010Words[i] >> 8);
        }

        var prev = Interlocked.Exchange(ref _latestNv12Frame, nv12);
        if (prev != null)
        {
            ArrayPool<byte>.Shared.Return(prev);
        }

        _frameReadyEvent.Set();
        Interlocked.Increment(ref _framesEnqueued);
    }

    private void OnSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        Interlocked.Increment(ref _samplesRequested);

        while (!IsDisposed)
        {
            bool frameReady;
            try
            {
                frameReady = _frameReadyEvent.Wait(TimeSpan.FromMilliseconds(200));
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (!frameReady)
            {
                Interlocked.Increment(ref _samplesTimedOut);
                continue;
            }

            var frame = Interlocked.Exchange(ref _latestNv12Frame, null);
            if (frame == null)
            {
                if (Volatile.Read(ref _latestNv12Frame) == null)
                {
                    TryResetFrameReadyEvent();
                }

                continue;
            }

            if (Volatile.Read(ref _latestNv12Frame) == null)
            {
                TryResetFrameReadyEvent();
            }

            Interlocked.Increment(ref _samplesDelivered);
            var frameIndex = Interlocked.Increment(ref _frameIndex) - 1;
            var ticks = (long)Math.Round((frameIndex * TimeSpan.TicksPerSecond) / _fps, MidpointRounding.AwayFromZero);
            var timestamp = TimeSpan.FromTicks(ticks);

            var nv12Count = _nv12SampleCount;
            var sample = MediaStreamSample.CreateFromBuffer(frame.AsBuffer(0, nv12Count), timestamp);
            sample.Processed += (s, e) => ArrayPool<byte>.Shared.Return(frame);
            args.Request.Sample = sample;
            return;
        }

        // Only reach here during disposal/shutdown.
        args.Request.Sample = null;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _frameReadyEvent.Set();
        _streamSource.SampleRequested -= OnSampleRequested;
        _mediaSource.Dispose();
        _frameReadyEvent.Dispose();
        var lastFrame = Interlocked.Exchange(ref _latestNv12Frame, null);
        if (lastFrame != null)
        {
            ArrayPool<byte>.Shared.Return(lastFrame);
        }
    }

    private void TryResetFrameReadyEvent()
    {
        try
        {
            _frameReadyEvent.Reset();
        }
        catch (ObjectDisposedException)
        {
            // Best effort during shutdown.
        }
    }

    private bool IsDisposed => Volatile.Read(ref _disposed) != 0;
}
