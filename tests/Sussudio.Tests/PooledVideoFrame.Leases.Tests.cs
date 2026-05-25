using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PooledVideoFrame_LeaseLifecycle_ReturnsBufferAfterLastRelease()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var frame = CreatePooledVideoFrame(frameType, nv12, 123L, 10L, 20L, 1920, 1080, 1024, pool);

        AssertEqual(123L, GetLongProperty(frame, "SequenceNumber"), "SequenceNumber");
        AssertEqual(1024, GetIntProperty(frame, "Length"), "Length");
        AssertEqual(1, GetIntProperty(frame, "LeaseCount"), "initial LeaseCount");
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), "initial IsReturned");
        AssertEqual(1, pool.RentCount, "pool rent count");

        var lease1 = addLeaseMethod.Invoke(frame, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        var lease2 = addLeaseMethod.Invoke(frame, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");

        AssertEqual(3, GetIntProperty(frame, "LeaseCount"), "LeaseCount after two leases");
        ((IDisposable)lease1).Dispose();
        AssertEqual(2, GetIntProperty(frame, "LeaseCount"), "LeaseCount after lease1 dispose");
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), "IsReturned with owner and lease2 alive");

        ((IDisposable)frame).Dispose();
        AssertEqual(1, GetIntProperty(frame, "LeaseCount"), "LeaseCount after owner dispose");
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), "IsReturned with lease2 alive");

        ((IDisposable)lease2).Dispose();
        AssertEqual(0, GetIntProperty(frame, "LeaseCount"), "LeaseCount after final lease dispose");
        AssertEqual(true, GetBoolProperty(frame, "IsReturned"), "IsReturned after final release");
        AssertEqual(1, pool.ReturnCount, "pool return count");

        ((IDisposable)lease2).Dispose();
        AssertEqual(0, GetIntProperty(frame, "LeaseCount"), "LeaseCount after duplicate lease dispose");
        AssertEqual(1, pool.ReturnCount, "pool return count after duplicate dispose");

        AssertPropertyThrowsObjectDisposed(frame, "Memory");

        return Task.CompletedTask;
    }

    internal static Task PooledVideoFrame_AddLeaseAfterReturn_Throws()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var frame = CreatePooledVideoFrame(frameType, nv12, 7L, 1L, 2L, 16, 16, 384, new TrackingArrayPool());

        ((IDisposable)frame).Dispose();
        AssertEqual(true, GetBoolProperty(frame, "IsReturned"), "IsReturned after owner-only dispose");

        try
        {
            addLeaseMethod.Invoke(frame, Array.Empty<object>());
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
            return Task.CompletedTask;
        }

        throw new InvalidOperationException("AddLease after return should throw ObjectDisposedException.");
    }

    internal static Task PooledVideoFrame_OwnerDisposeClosesNewLeasesButExistingLeaseRemainsReadable()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var frame = CreatePooledVideoFrame(frameType, nv12, 8L, 3L, 4L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(frame, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");

        ((IDisposable)frame).Dispose();
        AssertEqual(1, GetIntProperty(frame, "LeaseCount"), "LeaseCount with existing lease after owner dispose");
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), "IsReturned with existing lease after owner dispose");
        AssertPropertyThrowsObjectDisposed(frame, "Memory");
        AssertAddLeaseThrows(addLeaseMethod, frame);

        _ = GetPropertyValue(lease, "Memory");
        AssertEqual(8L, GetLongProperty(lease, "SequenceNumber"), "lease SequenceNumber");
        AssertEqual(384, GetIntProperty(lease, "Length"), "lease Length");

        ((IDisposable)lease).Dispose();
        AssertEqual(true, GetBoolProperty(frame, "IsReturned"), "IsReturned after existing lease dispose");
        AssertEqual(1, pool.ReturnCount, "pool return count");
        AssertPropertyThrowsObjectDisposed(lease, "Memory");

        return Task.CompletedTask;
    }

    internal static Task MjpegPooledFrameFanout_ExposesLeaseContracts()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var leaseType = RequireType("Sussudio.Services.Contracts.PooledVideoFrameLease");
        var leaseEncoderType = RequireType("Sussudio.Services.Contracts.IRawVideoFrameLeaseEncoder");
        var pipelineEmitCallbackType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+EmitFrameCallback");
        var previewSinkType = RequireType("Sussudio.Services.Contracts.IPreviewFrameSink");
        var jitterBufferType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var libAvSinkType = RequireType("Sussudio.Services.Recording.LibAvRecordingSink");
        var flashbackSinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");

        var emitInvoke = pipelineEmitCallbackType.GetMethod("Invoke")
            ?? throw new InvalidOperationException("EmitFrameCallback.Invoke not found.");
        var emitParameter = emitInvoke.GetParameters().SingleOrDefault()
            ?? throw new InvalidOperationException("EmitFrameCallback should have one parameter.");
        AssertEqual(frameType, emitParameter.ParameterType, "MJPEG emit callback frame type");

        var previewLeaseEnqueue = jitterBufferType.GetMethod(
            "Enqueue",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { leaseType },
            modifiers: null);
        AssertNotNull(previewLeaseEnqueue, "MjpegPreviewJitterBuffer.Enqueue(PooledVideoFrameLease)");

        var trackingType = previewSinkType.Assembly.GetType("Sussudio.Services.Contracts.PreviewFrameTracking", throwOnError: true)!;
        var rendererLeaseSubmit = previewSinkType.GetMethod(
            "SubmitRawFrameLease",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { leaseType, typeof(bool), trackingType },
            modifiers: null);
        AssertNotNull(rendererLeaseSubmit, "IPreviewFrameSink.SubmitRawFrameLease(PooledVideoFrameLease, bool, PreviewFrameTracking)");
        AssertEqual(true, previewSinkType.IsAssignableFrom(rendererType), "D3D11PreviewRenderer implements preview lease sink");
        AssertEqual(true, leaseEncoderType.IsAssignableFrom(libAvSinkType), "LibAvRecordingSink implements lease encoder");
        AssertEqual(true, leaseEncoderType.IsAssignableFrom(flashbackSinkType), "FlashbackEncoderSink implements lease encoder");

        return Task.CompletedTask;
    }
}
