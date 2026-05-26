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

    internal static Task D3DPreviewPendingFrame_ReleasesQueuedLease()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var leaseType = RequireType("Sussudio.Services.Contracts.PooledVideoFrameLease");
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var pendingFrameType = RequireNestedType(rendererType, "PendingFrame");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var owner = CreatePooledVideoFrame(frameType, nv12, 77L, 1L, 2L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(owner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)owner).Dispose();

        var constructor = pendingFrameType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single(ctor => ctor.GetParameters().Any(parameter => parameter.ParameterType == leaseType));
        var args = constructor.GetParameters()
            .Select(parameter => CreatePendingFrameArgument(parameter, leaseType, lease))
            .ToArray();
        var pendingFrame = constructor.Invoke(args)
            ?? throw new InvalidOperationException("PendingFrame constructor returned null.");

        ((IDisposable)pendingFrame).Dispose();

        AssertEqual(true, GetBoolProperty(owner, "IsReturned"), "pending preview frame lease returned");
        AssertEqual(1, pool.ReturnCount, "pending preview frame pool return count");

        return Task.CompletedTask;
    }

    internal static Task MjpegLeasedVideoPackets_ReleaseQueuedLeases()
    {
        AssertLeasedPacketReturnDisposesLease(
            sinkTypeName: "Sussudio.Services.Recording.LibAvRecordingSink",
            packetTypeName: "Sussudio.Services.Recording.LibAvRecordingSink+VideoFramePacket");
        AssertLeasedPacketReturnDisposesLease(
            sinkTypeName: "Sussudio.Services.Flashback.FlashbackEncoderSink",
            packetTypeName: "Sussudio.Services.Flashback.FlashbackEncoderSink+VideoFramePacket");

        return Task.CompletedTask;
    }

    private static void AssertLeasedPacketReturnDisposesLease(string sinkTypeName, string packetTypeName)
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var leaseType = RequireType("Sussudio.Services.Contracts.PooledVideoFrameLease");
        var sinkType = RequireType(sinkTypeName);
        var packetType = RequireType(packetTypeName);
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");
        var frameFactory = packetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == "Frame" &&
                method.GetParameters().Length == 2 &&
                method.GetParameters()[0].ParameterType == leaseType &&
                method.GetParameters()[1].ParameterType == typeof(long))
            ?? throw new InvalidOperationException($"{packetTypeName}.Frame(PooledVideoFrameLease, long) not found.");
        var returnPacket = sinkType.GetMethod("ReturnVideoPacket", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{sinkTypeName}.ReturnVideoPacket not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var frame = CreatePooledVideoFrame(frameType, nv12, 55L, 11L, 12L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(frame, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");

        ((IDisposable)frame).Dispose();
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), $"{sinkType.Name} owner dispose keeps queued lease alive");
        AssertEqual(0, pool.ReturnCount, $"{sinkType.Name} pool return before packet cleanup");

        var packet = frameFactory.Invoke(null, new[] { lease, Environment.TickCount64 })
            ?? throw new InvalidOperationException($"{packetTypeName}.Frame returned null.");
        returnPacket.Invoke(null, new[] { packet });

        AssertEqual(true, GetBoolProperty(frame, "IsReturned"), $"{sinkType.Name} packet cleanup returns frame");
        AssertEqual(1, pool.ReturnCount, $"{sinkType.Name} pool return after packet cleanup");
    }
}
