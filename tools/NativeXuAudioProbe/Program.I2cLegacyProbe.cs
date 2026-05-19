using Microsoft.Win32.SafeHandles;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;
using static NativeXuProbeI2cTransport;

static class NativeXuProbeI2cLegacyProbe
{
    public static int Run()
    {
        // Probe I2C AT commands through XU selectors.
        // Tests whether rtk_sendI2CATCommand uses the same XU path with different framing.
        // Shim captured I2C AT format: [00 4A type(01=SET|02=GET) 00 opcode value_bytes...]
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
        Console.WriteLine($"Device: {dev.Name} Id: {dev.Id}");

        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(dev, out _, out _))
        {
            Console.Error.WriteLine("Cannot parse vendor/product IDs");
            return 1;
        }

        var xuGuid = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");
        var interfaces = GetSelectedKsInterfaces(dev);
        Console.WriteLine($"Found {interfaces.Count} KS interfaces");

        foreach (var ksIf in interfaces)
        {
            Console.WriteLine($"\n=== Interface: {ksIf.Path} ===");
            using var handle = KsExtensionUnitNative.TryOpen(ksIf.Path, out var openErr);
            if (handle == null)
            {
                Console.WriteLine($"  Open failed: win32={openErr}");
                continue;
            }

            if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out var topoErr))
            {
                Console.WriteLine($"  Topology read failed: {topoErr}");
                continue;
            }

            var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
            Console.WriteLine($"  Nodes: {string.Join(", ", nodeList.Select(n => $"{n.NodeId}(devSpec={n.IsDevSpecific})"))}");

            foreach (var node in nodeList.Where(n => n.IsDevSpecific))
            {
                Console.WriteLine($"\n  --- Node {node.NodeId} ---");
                ScanSelectors(handle, node.NodeId, xuGuid);
                ProbeRawI2cFrames(handle, node.NodeId, xuGuid);
                ProbeAlternateSelectors(handle, node.NodeId, xuGuid);
                ProbeAtWrappedI2cFrames(handle, node.NodeId, xuGuid);
            }
        }

        return 0;
    }

    private static void ScanSelectors(SafeFileHandle handle, int nodeId, Guid xuGuid)
    {
        Console.WriteLine("  Scanning selectors 1-15:");
        for (int sel = 1; sel <= 15; sel++)
        {
            if (KsExtensionUnitNative.TryXuGetDirect(handle, nodeId, xuGuid, sel, 64, out var getData, out var getBytes, out var getWin32))
            {
                Console.WriteLine($"    Sel {sel,2}: GET OK ({getBytes} bytes) {BitConverter.ToString(getData, 0, Math.Min(getBytes, 32))}");
            }
            else
            {
                Console.WriteLine($"    Sel {sel,2}: GET failed win32={getWin32}");
            }
        }
    }

    private static void ProbeRawI2cFrames(SafeFileHandle handle, int nodeId, Guid xuGuid)
    {
        var testFrames = new (string label, byte[] frame)[]
        {
            ("I2C GET 0x09 param=0x42", new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }),
            ("I2C GET 0x04 param=0x0E", new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }),
            ("I2C GET 0x03 param=0xA0", new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x03, 0xA0 }),
        };

        Console.WriteLine("\n  Sending raw I2C AT frames via S2(trigger)+S1(payload):");
        foreach (var (label, frame) in testFrames)
        {
            Console.WriteLine($"\n    {label}: {BitConverter.ToString(frame)}");

            var trigger = new byte[] { (byte)(frame.Length & 0xFF), (byte)((frame.Length >> 8) & 0xFF) };
            if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, xuGuid, 2, trigger, out var trigWin32))
            {
                Console.WriteLine($"      Trigger(S2) failed: win32={trigWin32}");
                continue;
            }

            if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, xuGuid, 1, frame, out var sendWin32))
            {
                Console.WriteLine($"      Send(S1) failed: win32={sendWin32}");
                continue;
            }

            if (KsExtensionUnitNative.TryXuGetDirect(handle, nodeId, xuGuid, 2, 2, out var lenData, out var lenBytes, out var lenWin32))
            {
                int respLen = lenBytes >= 2 ? BitConverter.ToUInt16(lenData, 0) : 0;
                Console.WriteLine($"      Response length(S2): {respLen}");

                if (respLen > 0 && respLen < 1024)
                {
                    if (KsExtensionUnitNative.TryXuGetDirect(handle, nodeId, xuGuid, 1, respLen, out var respData, out var respBytes, out _))
                    {
                        Console.WriteLine($"      Response(S1) ({respBytes} bytes): {BitConverter.ToString(respData, 0, Math.Min(respBytes, 64))}");
                    }
                }
                else
                {
                    Console.WriteLine("      No response or invalid length");
                }
            }
            else
            {
                Console.WriteLine($"      GetLength(S2) failed: win32={lenWin32}");
            }
        }
    }

    private static void ProbeAlternateSelectors(SafeFileHandle handle, int nodeId, Guid xuGuid)
    {
        Console.WriteLine("\n  Testing I2C AT frame on alternate selectors:");
        var probeFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 };
        for (int sel = 3; sel <= 10; sel++)
        {
            if (KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, xuGuid, sel, probeFrame, out var setWin32))
            {
                Console.WriteLine($"    Sel {sel}: SET OK! Reading back...");
                if (KsExtensionUnitNative.TryXuGetDirect(handle, nodeId, xuGuid, sel, 64, out var resp, out var respB, out _))
                {
                    Console.WriteLine($"    Sel {sel}: GET ({respB} bytes): {BitConverter.ToString(resp, 0, Math.Min(respB, 32))}");
                }
            }
            else
            {
                Console.WriteLine($"    Sel {sel}: SET failed win32={setWin32}");
            }
        }
    }

    private static void ProbeAtWrappedI2cFrames(SafeFileHandle handle, int nodeId, Guid xuGuid)
    {
        Console.WriteLine("\n  Testing I2C frame wrapped in AT envelope:");
        foreach (var (i2cLabel, i2cOpcode) in new[] { ("I2C_GET(0x1C)", 0x1C), ("I2C_SET(0x1B)", 0x1B) })
        {
            var i2cPayload = new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 };
            var atFrame = BuildAtFrameWithPayload(i2cOpcode, i2cPayload);
            Console.WriteLine($"    {i2cLabel}: AT frame = {BitConverter.ToString(atFrame)}");

            var trig = new byte[] { (byte)(atFrame.Length & 0xFF), (byte)((atFrame.Length >> 8) & 0xFF) };
            if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, xuGuid, 2, trig, out var tw))
            {
                Console.WriteLine($"      Trigger failed: win32={tw}");
                continue;
            }

            if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, xuGuid, 1, atFrame, out var sw))
            {
                Console.WriteLine($"      Send failed: win32={sw}");
                continue;
            }

            if (KsExtensionUnitNative.TryXuGetDirect(handle, nodeId, xuGuid, 2, 2, out var ld, out var lb, out _))
            {
                int rl = lb >= 2 ? BitConverter.ToUInt16(ld, 0) : 0;
                Console.WriteLine($"      Response length: {rl}");
                if (rl > 0 && rl < 1024)
                {
                    if (KsExtensionUnitNative.TryXuGetDirect(handle, nodeId, xuGuid, 1, rl, out var rd, out var rb, out _))
                    {
                        Console.WriteLine($"      Response ({rb} bytes): {BitConverter.ToString(rd, 0, Math.Min(rb, 64))}");
                    }
                }
            }
            else
            {
                Console.WriteLine("      GetLength failed");
            }
        }
    }
}
