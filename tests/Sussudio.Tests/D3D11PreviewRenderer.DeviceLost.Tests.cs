using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_IsDeviceLostException_ClassifiesCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod("IsDeviceLostException",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("IsDeviceLostException not found.");

        // Regular exception → false
        var regularEx = new InvalidOperationException("test");
        AssertEqual(false, (bool)method.Invoke(null, new object[] { regularEx })!, "Regular exception is not device lost");

        // COMException with DeviceRemoved HRESULT → true
        var deviceRemovedEx = new System.Runtime.InteropServices.COMException("Device removed", unchecked((int)0x887A0005));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceRemovedEx })!, "DeviceRemoved COMException is device lost");

        // COMException with DeviceReset HRESULT → true
        var deviceResetEx = new System.Runtime.InteropServices.COMException("Device reset", unchecked((int)0x887A0007));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceResetEx })!, "DeviceReset COMException is device lost");

        // COMException with other HRESULT → false
        var otherComEx = new System.Runtime.InteropServices.COMException("Other", unchecked((int)0x80004005));
        AssertEqual(false, (bool)method.Invoke(null, new object[] { otherComEx })!, "Other COMException is not device lost");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_DeviceLostRecoveryLivesInFocusedPartial()
    {
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var deviceLostText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceLost.cs")
            .Replace("\r\n", "\n");

        AssertContains(deviceLostText, "private void HandleDeviceLost(Exception ex)");
        AssertContains(deviceLostText, "private static bool IsDeviceLostException(Exception ex)");
        AssertContains(deviceLostText, "TrackFrameDropped(stalePending, \"device-lost\");");
        AssertContains(deviceLostText, "ResultCode.DeviceRemoved");
        AssertContains(deviceLostText, "unchecked((int)0x887A0005)");
        AssertDoesNotContain(resourcesText, "private void HandleDeviceLost(Exception ex)");
        AssertDoesNotContain(resourcesText, "private static bool IsDeviceLostException(Exception ex)");

        return Task.CompletedTask;
    }
}
