using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static bool SendSelector4Command(
        SafeFileHandle handle,
        int nodeId,
        int cmdCode,
        byte[] inputData,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var atFrame = BuildAtWriteFrame(cmdCode, inputData);
        var payload = new byte[I2cPayloadSize];
        Array.Copy(atFrame, 0, payload, 0, atFrame.Length);

        cancellationToken.ThrowIfCancellationRequested();
        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, I2cSelector, payload, out var win32))
        {
            Logger.Log($"NATIVEXU_SEL4_FAILED cmd=0x{cmdCode:X2} win32={FormatWin32Code(win32)}");
            return false;
        }

        return true;
    }
}
