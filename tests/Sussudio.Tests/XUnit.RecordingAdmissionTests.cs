using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class RecordingAdmissionTests
    {
        public RecordingAdmissionTests() => global::Program.EnsureTargetAssemblyLoadedForXUnit();

        [Theory]
        [InlineData("raw", 1)]
        [InlineData("raw", 0)]
        [InlineData("raw", -1)]
        [InlineData("pooled", 1)]
        [InlineData("pooled", 0)]
        [InlineData("pooled", -1)]
        [InlineData("lease", 1)]
        [InlineData("lease", 0)]
        [InlineData("lease", -1)]
        [InlineData("gpu", 1)]
        [InlineData("gpu", 0)]
        public void CaptureAccountsForActualAdmissionAndPreservesOwnership(string path, int outcome)
            => global::Program.AssertCaptureRecordingAdmission(path, outcome);

        [Theory]
        [InlineData("Sussudio.Services.Recording.LibAvRecordingSink")]
        [InlineData("Sussudio.Services.Flashback.FlashbackEncoderSink")]
        public void StoppedSinkReleasesRejectedLease(string sinkType)
            => global::Program.AssertStoppedSinkReleasesRejectedLease(sinkType);
    }
}

internal static partial class Program
{
    private delegate void RawRecordingAdmission(ReadOnlySpan<byte> data, int width, int height, bool isP010, long sequence);

    internal static void AssertCaptureRecordingAdmission(string path, int outcome)
    {
        var captureType = RequireType("Sussudio.Services.Capture.UnifiedVideoCapture");
        var capture = Activator.CreateInstance(captureType, nonPublic: true)!;
        var calls = 0;
        object? transferredLease = null;
        bool Admit(object? lease, int size)
        {
            calls++;
            Assert.Equal(384, size);
            transferredLease = lease;
            if (lease != null)
                Assert.Equal(731L, GetLongProperty(lease, "SequenceNumber"));
            if (outcome < 0)
                throw new InvalidOperationException("scripted admission failure");
            // Real sinks release ownership on false; accepted packets retain it until drained.
            if (outcome == 0)
                (lease as IDisposable)?.Dispose();
            return outcome > 0;
        }

        var encoder = CreateAdmissionEncoder(path == "lease", size => Admit(null, size),
            lease => Admit(lease, 384), (texture, subresource) =>
            {
                Assert.Equal(new IntPtr(1234), texture);
                Assert.Equal(3, subresource);
                return Admit(null, 384);
            });
        SetPrivateField(capture, "_recordingActive", true);
        SetPrivateField(capture, "_recordingEncoder", encoder);
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var format = Enum.Parse(RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat"), "Nv12");
        var pool = new TrackingArrayPool();
        var frame = CreatePooledVideoFrame(frameType, format, 731L, 11L, 12L, 16, 16, 384, pool);
        try
        {
            if (path == "raw")
            {
                var method = captureType.GetMethod("EnqueueRecordingFrame", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(ReadOnlySpan<byte>), typeof(int), typeof(int), typeof(bool), typeof(long) }, null)!;
                method.CreateDelegate<RawRecordingAdmission>(capture)(new byte[384], 16, 16, false, 731L);
            }
            else if (path == "gpu")
            {
                captureType.GetMethod("EnqueueGpuRecordingFrame", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(capture, new object[] { encoder, new IntPtr(1234), 3, 731L });
            }
            else
            {
                captureType.GetMethod("EnqueueRecordingFrame", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { frameType }, null)!.Invoke(capture, new[] { frame });
            }

            Assert.Equal(1, calls);
            Assert.Equal(1L, GetLongProperty(capture, "RecordingFramesDelivered"));
            Assert.Equal(outcome > 0 ? 1L : 0L, GetLongProperty(capture, "VideoFramesWrittenToSink"));
            Assert.Equal(outcome > 0 ? 0L : 1L, GetLongProperty(capture, "RecordingFramesRejected"));
            Assert.Equal(outcome == 0 ? 1L : 0L, GetLongProperty(capture, "RecordingQueueRejectedFrames"));
            Assert.Equal(outcome < 0 ? 1L : 0L, GetLongProperty(capture, "VideoFramesDropped"));
            var summary = captureType.GetMethod("GetFrameLedgerSummary")!.Invoke(capture, new object[] { 64 })!;
            var events = (IEnumerable)summary.GetType().GetProperty("RecentEvents")!.GetValue(summary)!;
            var entry = Assert.Single(events.Cast<object>());
            Assert.Equal(731L, GetLongProperty(entry, "SourceSequence"));
            Assert.Equal("RecordingEnqueued", entry.GetType().GetProperty("Stage")!.GetValue(entry)!.ToString());
            Assert.Equal(outcome > 0, entry.GetType().GetProperty("Accepted")!.GetValue(entry));
            Assert.Equal(outcome > 0 ? null : outcome == 0 ? "queue_rejected" : "exception",
                entry.GetType().GetProperty("Reason")!.GetValue(entry));

            Assert.False(GetBoolProperty(frame, "IsReturned"));
            Assert.Equal(0, pool.ReturnCount);
            ((IDisposable)frame).Dispose();
            Assert.Equal(path == "lease" && outcome > 0 ? 0 : 1, pool.ReturnCount);
            if (path == "lease")
            {
                Assert.NotNull(transferredLease);
                // An accepted lease must still be readable after capture returns and its owner releases.
                if (outcome > 0)
                    Assert.Equal(384, ((ReadOnlyMemory<byte>)transferredLease!.GetType().GetProperty("Memory")!.GetValue(transferredLease)!).Length);
                else
                    Assert.Null(GetPrivateField(transferredLease!, "_frame"));
                ((IDisposable)transferredLease!).Dispose();
            }
            Assert.Equal(1, pool.ReturnCount);
        }
        finally
        {
            (transferredLease as IDisposable)?.Dispose();
            ((IDisposable)frame).Dispose();
        }
    }

    internal static void AssertStoppedSinkReleasesRejectedLease(string sinkTypeName)
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var format = Enum.Parse(RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat"), "Nv12");
        var pool = new TrackingArrayPool();
        var frame = CreatePooledVideoFrame(frameType, format, 731L, 11L, 12L, 16, 16, 384, pool);
        var lease = frameType.GetMethod("AddLease")!.Invoke(frame, null)!;
        try
        {
            // Only the stopped rejection branch runs; no native backend or queues are initialized.
            var sinkType = RequireType(sinkTypeName);
            using var sink = (IDisposable)(sinkTypeName.EndsWith("FlashbackEncoderSink", StringComparison.Ordinal)
                ? sinkType.GetConstructor(new[] { RequireType("Sussudio.Models.FlashbackBufferOptions") })!.Invoke(new object?[] { null })
                : Activator.CreateInstance(sinkType)!);
            var contract = RequireType("Sussudio.Services.Contracts.IRawVideoFrameLeaseTryEncoder");
            Assert.Equal(false, contract.GetMethod("TryEnqueueRawVideoFrame")!.Invoke(sink, new[] { lease }));
            Assert.Null(GetPrivateField(lease, "_frame"));
            Assert.Equal(0, pool.ReturnCount);
            Assert.False(GetBoolProperty(frame, "IsReturned"));
            ((IDisposable)frame).Dispose();
            Assert.Equal(1, pool.ReturnCount);
        }
        finally
        {
            ((IDisposable)lease).Dispose();
            ((IDisposable)frame).Dispose();
        }
    }

    private static object CreateAdmissionEncoder(bool withLease, Func<int, bool> raw,
        Func<object, bool> lease, Func<IntPtr, int, bool> gpu)
    {
        // The app is loaded reflectively. Emit only these three forwarding methods so spans
        // never pass through reflection, and use the app's existing test friend assembly.
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Sussudio.Tests"), AssemblyBuilderAccess.RunAndCollect);
        var type = assembly.DefineDynamicModule("Admission").DefineType("AdmissionEncoder", TypeAttributes.Public | TypeAttributes.Sealed);
        type.DefineDefaultConstructor(MethodAttributes.Public);
        var rawContract = RequireType("Sussudio.Services.Contracts.IRawVideoFrameTryEncoder");
        var gpuContract = RequireType("Sussudio.Services.Contracts.IGpuVideoFrameTryEncoder");
        var leaseContract = RequireType("Sussudio.Services.Contracts.IRawVideoFrameLeaseTryEncoder");
        foreach (var contract in withLease ? new[] { rawContract, gpuContract, leaseContract } : new[] { rawContract, gpuContract })
        {
            type.AddInterfaceImplementation(contract);
            var source = contract.GetMethods().Single();
            var callbackType = contract == rawContract ? typeof(Func<int, bool>) : contract == gpuContract ? typeof(Func<IntPtr, int, bool>) : typeof(Func<object, bool>);
            var field = type.DefineField(contract.Name, callbackType, FieldAttributes.Public);
            var method = type.DefineMethod(source.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
                typeof(bool), source.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            if (contract != rawContract)
                il.Emit(OpCodes.Ldarg_1);
            if (contract != leaseContract)
                il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, callbackType.GetMethod("Invoke")!);
            il.Emit(OpCodes.Ret);
            type.DefineMethodOverride(method, source);
        }
        var result = Activator.CreateInstance(type.CreateType()!)!;
        result.GetType().GetField(rawContract.Name)!.SetValue(result, raw);
        result.GetType().GetField(gpuContract.Name)!.SetValue(result, gpu);
        if (withLease)
            result.GetType().GetField(leaseContract.Name)!.SetValue(result, lease);
        return result;
    }
}
