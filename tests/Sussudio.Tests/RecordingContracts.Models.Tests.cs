using System;
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
}
