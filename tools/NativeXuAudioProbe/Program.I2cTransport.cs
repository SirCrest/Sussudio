using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;

static class NativeXuProbeI2cTransport
{
    public static async Task<byte[]?> SendI2cAtGetAsync(CaptureDevice device, byte[] i2cFrame)
    {
        return await SendI2cViaAtAsync(device, 0x1C, i2cFrame);
    }

    public static async Task<bool> SendI2cAtSetAsync(CaptureDevice device, byte[] i2cFrame)
    {
        var resp = await SendI2cViaAtAsync(device, 0x1B, i2cFrame);
        return resp != null;
    }

    public static async Task<byte[]?> SendI2cViaAtAsync(CaptureDevice device, int atOpcode, byte[] i2cPayload)
    {
        if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(device, out _, out _))
        {
            return null;
        }

        var interfaces = GetSelectedKsInterfaces(device);
        var xuGuid = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");

        foreach (var ksIf in interfaces)
        {
            using var handle = KsExtensionUnitNative.TryOpen(ksIf.Path, out _);
            if (handle == null)
            {
                continue;
            }

            if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out _))
            {
                continue;
            }

            foreach (var node in (nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>()).Where(n => n.IsDevSpecific))
            {
                var atFrame = BuildAtFrameWithPayload(atOpcode, i2cPayload);

                var trigger = new byte[] { (byte)(atFrame.Length & 0xFF), (byte)((atFrame.Length >> 8) & 0xFF) };
                if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, node.NodeId, xuGuid, 2, trigger, out _))
                {
                    continue;
                }

                if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, node.NodeId, xuGuid, 1, atFrame, out _))
                {
                    continue;
                }

                if (!KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, 2, 2, out var lenData, out var lenBytes, out _))
                {
                    continue;
                }

                var respLen = lenBytes >= 2 ? BitConverter.ToUInt16(lenData, 0) : 0;
                if (respLen <= 0 || respLen > 1024)
                {
                    return Array.Empty<byte>();
                }

                if (!KsExtensionUnitNative.TryXuGetDirect(handle, node.NodeId, xuGuid, 1, respLen, out var respData, out var respBytes, out _))
                {
                    continue;
                }

                if (respBytes >= 5 && respData[0] == 0xA1)
                {
                    var envLen = respData[1];
                    var payloadLen = envLen - 2;
                    if (payloadLen > 0 && 4 + payloadLen <= respBytes)
                    {
                        var result = new byte[payloadLen];
                        Array.Copy(respData, 4, result, 0, payloadLen);
                        return result;
                    }
                }

                var raw = new byte[respBytes];
                Array.Copy(respData, raw, respBytes);
                return raw;
            }
        }

        return null;
    }

    public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> GetSelectedKsInterfaces(CaptureDevice device)
    {
        if (string.IsNullOrWhiteSpace(device.NativeXuInterfacePath))
        {
            Console.Error.WriteLine("Selected device has no native XU interface path.");
            return Array.Empty<KsExtensionUnitNative.KsInterfacePath>();
        }

        return new[] { new KsExtensionUnitNative.KsInterfacePath(device.NativeXuInterfacePath, Guid.Empty) };
    }

    public static byte[] BuildAtFrameWithPayload(int cmdCode, byte[] payload)
    {
        var dataLen = 4 + payload.Length;
        var frame = new byte[5 + dataLen];
        frame[0] = 0xA1;
        frame[1] = (byte)dataLen;
        frame[2] = 0x00;
        frame[3] = 0x00;
        frame[4] = (byte)(cmdCode & 0xFF);
        frame[5] = (byte)((cmdCode >> 8) & 0xFF);
        frame[6] = (byte)((cmdCode >> 16) & 0xFF);
        frame[7] = (byte)((cmdCode >> 24) & 0xFF);
        Array.Copy(payload, 0, frame, 8, payload.Length);

        byte sum = 0;
        for (var i = 0; i < frame.Length - 1; i++)
        {
            sum += frame[i];
        }

        frame[^1] = unchecked((byte)(~sum + 1));
        return frame;
    }
}
