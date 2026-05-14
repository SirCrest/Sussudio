using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;
using static NativeXuProbeI2cTransport;

static partial class NativeXuProbeI2cCommands
{
    public static async Task<int> RunHighSelectorProbeAsync(CaptureDevice dev)
    {
        // Probe XU selectors 18-35, focusing on 0x1B(27) and 0x1C(28)
        // which are the a1 values in rtk_sendI2CATCommand
        if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(dev, out var vidH, out var pidH))
        {
            Console.Error.WriteLine("Cannot parse device IDs");
            return 1;
        }
        var xuGuidH = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");
        var ifacesH = GetSelectedKsInterfaces(dev);
        var ksIfH = ifacesH.FirstOrDefault();
        using var hH = KsExtensionUnitNative.TryOpen(ksIfH.Path, out _);
        if (hH == null) { Console.Error.WriteLine("Cannot open"); return 1; }
        if (!KsExtensionUnitNative.TryReadTopologyNodes(hH, out var nsH, out _)) { Console.Error.WriteLine("No topology"); return 1; }
        var devNodeH = (nsH ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>()).FirstOrDefault(n => n.IsDevSpecific);
        int nidH = devNodeH.NodeId;

        // Probe selectors 18-40 with multiple buffer sizes
        Console.WriteLine("--- Probing selectors 18-40 ---");
        foreach (int bufSize in new[] { 2, 8, 64, 256, 1024 })
        {
            Console.WriteLine($"\n  Buffer: {bufSize}");
            for (int sel = 18; sel <= 40; sel++)
            {
                if (KsExtensionUnitNative.TryXuGetDirect(hH, nidH, xuGuidH, sel, bufSize, out var gd, out var gb, out var gw))
                {
                    var hasData = gd.Take(gb).Any(b => b != 0);
                    Console.WriteLine($"    Sel {sel,2} (0x{sel:X2}): GET OK ({gb}B) {(hasData ? BitConverter.ToString(gd, 0, Math.Min(gb, 16)) + " ***DATA***" : "all-zero")}");
                }
                else if (gw == 122) // INSUFFICIENT_BUFFER â€” selector exists but needs bigger buffer
                {
                    Console.WriteLine($"    Sel {sel,2} (0x{sel:X2}): BUFFER TOO SMALL (needs >{bufSize}B)");
                }
                else if (gw != 1168) // skip NOT_FOUND
                {
                    Console.WriteLine($"    Sel {sel,2} (0x{sel:X2}): GET failed win32={gw}");
                }
            }
        }

        // Focused test on selectors 0x1B(27) and 0x1C(28)
        Console.WriteLine("\n--- Focused test: selectors 0x1B(27) and 0x1C(28) ---");
        foreach (int sel in new[] { 0x1B, 0x1C })
        {
            Console.WriteLine($"\n  Selector 0x{sel:X2} ({sel}):");
            // Try SET with I2C frame
            var i2cFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 };
            foreach (int padSize in new[] { 6, 150, 525 })
            {
                var padded = new byte[padSize];
                Array.Copy(i2cFrame, padded, Math.Min(i2cFrame.Length, padSize));
                if (KsExtensionUnitNative.TryXuSetViaOutput(hH, nidH, xuGuidH, sel, padded, out var sw))
                {
                    Console.WriteLine($"    SET({padSize}B) OK!");
                    // Read back from multiple selectors
                    foreach (int rSel in new[] { sel, 3, 4 })
                    {
                        foreach (int rBuf in new[] { 256, 1024 })
                        {
                            if (KsExtensionUnitNative.TryXuGetDirect(hH, nidH, xuGuidH, rSel, rBuf, out var rd, out var rb, out _))
                            {
                                Console.WriteLine($"      GET sel={rSel} ({rb}B): {BitConverter.ToString(rd, 0, Math.Min(rb, 32))}");
                                break;
                            }
                        }
                    }
                    break;
                }
                else
                {
                    Console.WriteLine($"    SET({padSize}B) failed win32={sw}");
                }
            }
        }

        // Try ALL topology nodes (not just devSpec) for I2C AT
        Console.WriteLine("\n--- All topology nodes: I2C AT on non-devSpec nodes ---");
        foreach (var node in nsH ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>())
        {
            Console.WriteLine($"\n  Node {node.NodeId} (devSpec={node.IsDevSpecific}):");
            // Try AT S1/S2 protocol with I2C frame
            var atFrame = BuildAtFrameWithPayload(0x1C, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 });
            var trig = new byte[] { (byte)(atFrame.Length & 0xFF), (byte)((atFrame.Length >> 8) & 0xFF) };
            if (KsExtensionUnitNative.TryXuSetViaOutput(hH, node.NodeId, xuGuidH, 2, trig, out _) &&
                KsExtensionUnitNative.TryXuSetViaOutput(hH, node.NodeId, xuGuidH, 1, atFrame, out _))
            {
                if (KsExtensionUnitNative.TryXuGetDirect(hH, node.NodeId, xuGuidH, 2, 2, out var ld, out var lb, out _))
                {
                    int rl = lb >= 2 ? BitConverter.ToUInt16(ld, 0) : 0;
                    Console.WriteLine($"    Response length: {rl}");
                    if (rl > 0 && rl < 1024)
                    {
                        if (KsExtensionUnitNative.TryXuGetDirect(hH, node.NodeId, xuGuidH, 1, rl, out var rd, out var rb, out _))
                            Console.WriteLine($"    Response: {BitConverter.ToString(rd, 0, Math.Min(rb, 32))}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"    AT S1/S2 not available on this node");
            }
        }

        return 0;
    }
}
