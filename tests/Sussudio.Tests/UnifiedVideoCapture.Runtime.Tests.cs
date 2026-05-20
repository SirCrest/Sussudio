using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task UnifiedVideoCapture_CpuMjpegEmitReportsNv12()
    {
        var unifiedVideoCapture = CreateInstance("Sussudio.Services.Capture.UnifiedVideoCapture");
        var observed = string.Empty;

        var setObserver = unifiedVideoCapture.GetType().GetMethod("SetPixelFormatDetectedCallback", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("SetPixelFormatDetectedCallback method not found.");
        setObserver.Invoke(unifiedVideoCapture, new object?[] { new Action<string>(value => observed = value) });

        var emitMethod = unifiedVideoCapture.GetType().GetMethod("OnMjpegPipelineFrameEmitted", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("OnMjpegPipelineFrameEmitted method not found.");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var rentMethod = frameType.GetMethod("Rent", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PooledVideoFrame.Rent method not found.");
        var frame = rentMethod.Invoke(
            null,
            new object[]
            {
                0L,
                0L,
                0L,
                2,
                2,
                Enum.Parse(formatType, "Nv12"),
                6
            })
            ?? throw new InvalidOperationException("PooledVideoFrame.Rent returned null.");
        try
        {
            emitMethod.Invoke(unifiedVideoCapture, new[] { frame });
        }
        finally
        {
            ((IDisposable)frame).Dispose();
        }

        AssertEqual("NV12", observed, "UnifiedVideoCapture.OnMjpegPipelineFrameEmitted observer format");
        return Task.CompletedTask;
    }

    internal static async Task UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails()
    {
        var unifiedVideoCapture = CreateInstance("Sussudio.Services.Capture.UnifiedVideoCapture");
        var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
        var pipeline = CreateUninitializedObject(pipelineType);
        SeedPipelineStopFailureState(pipeline, pipelineType);

        SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", pipeline);
        SetPrivateField(pipeline, "_emitThread", Thread.CurrentThread);

        try
        {
            var stopAsync = unifiedVideoCapture.GetType().GetMethod("StopAsync", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("UnifiedVideoCapture.StopAsync method not found.");
            if (stopAsync.Invoke(unifiedVideoCapture, null) is not Task stopTask)
            {
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync did not return a Task.");
            }

            try
            {
                await stopTask.ConfigureAwait(false);
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync unexpectedly succeeded.");
            }
            catch (InvalidOperationException ex)
            {
                AssertContains(ex.Message, "emitter_self_join");
            }

            var retainedPipeline = GetPrivateField(unifiedVideoCapture, "_mjpegPipeline");
            AssertEqual(pipeline, retainedPipeline, "UnifiedVideoCapture._mjpegPipeline retained on stop failure");
        }
        finally
        {
            SetPrivateField(pipeline, "_emitThread", null);
            SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", null);

            var disposeMethod = pipelineType.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("ParallelMjpegDecodePipeline.Dispose method not found.");
            disposeMethod.Invoke(pipeline, null);

            await DisposeValueTaskAsync(unifiedVideoCapture).ConfigureAwait(false);
        }
    }
}
