using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task GpuPipelineHandles_None_ReturnsZeroedStruct()
    {
        var handlesType = RequireType("Sussudio.Services.Contracts.GpuPipelineHandles");
        var noneProp = handlesType.GetProperty("None", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("GpuPipelineHandles.None not found");
        var none = noneProp.GetValue(null)!;

        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "D3D11DevicePtr")!, "D3D11DevicePtr");
        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "D3D11DeviceContextPtr")!, "D3D11DeviceContextPtr");
        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "CudaHwDeviceCtxPtr")!, "CudaHwDeviceCtxPtr");
        AssertEqual(IntPtr.Zero, (IntPtr)GetPropertyValue(none, "CudaHwFramesCtxPtr")!, "CudaHwFramesCtxPtr");

        return Task.CompletedTask;
    }

    private static Task RecordingContextRequest_DefaultsMatchRecordingContextDefaults()
    {
        var request = CreateInstance("Sussudio.Services.Contracts.RecordingContextRequest");
        AssertEqual("30", GetStringProperty(request, "FrameRateArg"), "FrameRateArg default");
        AssertEqual("nv12", GetStringProperty(request, "VideoInputPixelFormat"), "VideoInputPixelFormat default");
        AssertEqual(false, GetBoolProperty(request, "IsFullRangeInput"), "IsFullRangeInput default");
        AssertEqual(false, GetBoolProperty(request, "UsePostMuxAudio"), "UsePostMuxAudio default");

        return Task.CompletedTask;
    }

    private static Task FinalizeResult_Success_ProducesEmptyPreservedList()
    {
        var resultType = RequireType("Sussudio.Services.Contracts.FinalizeResult");
        var successMethod = resultType.GetMethod("Success", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FinalizeResult.Success not found");
        var result = successMethod.Invoke(null, new object[] { "/path/output.mp4", "Stopped" })!;

        AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
        AssertEqual("/path/output.mp4", GetStringProperty(result, "OutputPath"), "OutputPath");
        AssertEqual("Stopped", GetStringProperty(result, "StatusMessage"), "StatusMessage");
        var artifacts = GetPropertyValue(result, "PreservedArtifacts");
        AssertEqual(0, GetCountProperty(artifacts), "PreservedArtifacts.Count");

        return Task.CompletedTask;
    }

    private static Task FinalizeResult_Failure_DeduplicatesAndFiltersArtifacts()
    {
        var resultType = RequireType("Sussudio.Services.Contracts.FinalizeResult");
        var failureMethod = resultType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FinalizeResult.Failure not found");

        var artifacts = new List<string?> { "/path/a.mp4", "/path/A.mp4", null!, "", " ", "/path/b.m4a" }
            .Where(s => true) as IEnumerable<string>;
        var result = failureMethod.Invoke(null, new object?[] { "/output.mp4", "mux failed", artifacts })!;

        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
        var preserved = GetPropertyValue(result, "PreservedArtifacts");
        AssertEqual(2, GetCountProperty(preserved), "PreservedArtifacts.Count");

        return Task.CompletedTask;
    }
}
