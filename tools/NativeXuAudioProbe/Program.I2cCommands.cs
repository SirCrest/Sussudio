using System.Globalization;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;
using static NativeXuProbeI2cTransport;

static partial class NativeXuProbeI2cCommands
{
    public static async Task<int> RunAsync(string[] args)
    {
        // Send I2C AT commands via the AT envelope (opcode 0x1C=GET, 0x1B=SET)
        // Usage: i2c-cmd get <i2c_opcode_hex> <param_hex>
        //        i2c-cmd set <i2c_opcode_hex> <value_byte_hex>
        //        i2c-cmd scan  (scan I2C opcodes 0x00-0x20)
        if (args.Length < 2) { Console.Error.WriteLine("Usage: i2c-cmd get|set|scan ..."); return 1; }
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
        Console.WriteLine($"Device: {dev.Name}");

        var subCmd = args[1].ToLowerInvariant();

        if (subCmd == "scan")
        {
            // Scan I2C opcodes to find which ones return data
            int start = args.Length > 2 ? int.Parse(args[2].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber) : 0x00;
            int end = args.Length > 3 ? int.Parse(args[3].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber) : 0x20;
            Console.WriteLine($"Scanning I2C AT opcodes 0x{start:X2} - 0x{end:X2}...");
            for (int op = start; op <= end; op++)
            {
                // I2C GET: [00 4A 02 00 opcode 00]
                var i2cFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)op, 0x00 };
                var resp = await SendI2cAtGetAsync(dev, i2cFrame);
                if (resp != null)
                {
                    Console.WriteLine($"  I2C 0x{op:X2}: {BitConverter.ToString(resp)} ({resp.Length} bytes) int32={BitConverter.ToInt32(resp.Length >= 4 ? resp[..4] : resp.Concat(new byte[4 - resp.Length]).ToArray(), 0)}");
                }
                else
                {
                    Console.WriteLine($"  I2C 0x{op:X2}: (no response)");
                }
            }
            return 0;
        }

        if (subCmd == "get" && args.Length >= 4)
        {
            int i2cOp = int.Parse(args[2].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber);
            byte param = byte.Parse(args[3].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber);
            var i2cFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)i2cOp, param };
            Console.WriteLine($"I2C GET opcode=0x{i2cOp:X2} param=0x{param:X2}");
            var resp = await SendI2cAtGetAsync(dev, i2cFrame);
            Console.WriteLine(resp != null
                ? $"  Response: {BitConverter.ToString(resp)} ({resp.Length} bytes)"
                : "  No response");
            return 0;
        }

        if (subCmd == "set" && args.Length >= 4)
        {
            int i2cOp = int.Parse(args[2].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber);
            byte value = byte.Parse(args[3].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber);
            var i2cFrame = new byte[] { 0x00, 0x4A, 0x01, 0x00, (byte)i2cOp, value };
            Console.WriteLine($"I2C SET opcode=0x{i2cOp:X2} value=0x{value:X2}");
            var ok = await SendI2cAtSetAsync(dev, i2cFrame);
            Console.WriteLine($"  Result: {(ok ? "OK" : "failed")}");
            return 0;
        }

        if (subCmd == "sel-probe")
        {
            return await RunSelectorProbeAsync(dev);
        }

        if (subCmd == "verify")
        {
            // Verify the AT envelope I2C path actually works by doing SET+readback
            // Tests: read I2C 0x04 â†’ SET I2C 0x04=1 â†’ read I2C 0x04 â†’ restore
            if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(dev, out var vid3, out var pid3))
            {
                Console.Error.WriteLine("Cannot parse device IDs");
                return 1;
            }
            var xuGuid3 = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");
            var ifaces3 = GetSelectedKsInterfaces(dev);
            var ksIf3 = ifaces3.FirstOrDefault();
            using var h3 = KsExtensionUnitNative.TryOpen(ksIf3.Path, out _);
            if (h3 == null) { Console.Error.WriteLine("Cannot open"); return 1; }
            if (!KsExtensionUnitNative.TryReadTopologyNodes(h3, out var ns3, out _)) { Console.Error.WriteLine("No topology"); return 1; }
            var devNode3 = (ns3 ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>()).FirstOrDefault(n => n.IsDevSpecific);
            int nid3 = devNode3.NodeId;

            // Helper: send AT frame and get raw response
            byte[]? SendAtAndGetResponse(byte[] atFrame)
            {
                var trig = new byte[] { (byte)(atFrame.Length & 0xFF), (byte)((atFrame.Length >> 8) & 0xFF) };
                if (!KsExtensionUnitNative.TryXuSetViaOutput(h3, nid3, xuGuid3, 2, trig, out _)) return null;
                if (!KsExtensionUnitNative.TryXuSetViaOutput(h3, nid3, xuGuid3, 1, atFrame, out _)) return null;
                if (!KsExtensionUnitNative.TryXuGetDirect(h3, nid3, xuGuid3, 2, 2, out var ld, out var lb, out _)) return null;
                int rl = lb >= 2 ? BitConverter.ToUInt16(ld, 0) : 0;
                if (rl <= 0 || rl > 1024) return Array.Empty<byte>();
                if (!KsExtensionUnitNative.TryXuGetDirect(h3, nid3, xuGuid3, 1, rl, out var rd, out var rb, out _)) return null;
                return rd[..rb];
            }

            // Test 1: Read I2C opcodes 0x03, 0x04, 0x07, 0x09, 0x0E, 0x10 via AT 0x1C
            Console.WriteLine("--- I2C GET via AT envelope (opcode 0x1C) ---");
            foreach (int i2cOp in new[] { 0x03, 0x04, 0x07, 0x09, 0x0E, 0x0F, 0x10, 0x11 })
            {
                var i2cFrame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)i2cOp, 0x00 };
                var atFrame = BuildAtFrameWithPayload(0x1C, i2cFrame);
                var resp = SendAtAndGetResponse(atFrame);
                Console.WriteLine($"  I2C GET 0x{i2cOp:X2}: AT={BitConverter.ToString(atFrame)} â†’ {(resp != null ? BitConverter.ToString(resp) : "(null)")}");
            }

            // Test 2: I2C SET via AT 0x1B: set opcode 0x04 to 0x01, read back, restore to 0x00
            Console.WriteLine("\n--- I2C SET/verify via AT envelope ---");
            // Read before
            var readBefore = SendAtAndGetResponse(BuildAtFrameWithPayload(0x1C, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x00 }));
            Console.WriteLine($"  BEFORE I2C GET 0x04: {(readBefore != null ? BitConverter.ToString(readBefore) : "(null)")}");

            // SET I2C 0x04 = 0x01 via AT 0x1B
            var setFrame = BuildAtFrameWithPayload(0x1B, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x01 });
            Console.WriteLine($"  SET frame: {BitConverter.ToString(setFrame)}");
            var setResp = SendAtAndGetResponse(setFrame);
            Console.WriteLine($"  SET response: {(setResp != null ? BitConverter.ToString(setResp) : "(null)")}");

            await Task.Delay(200);

            // Read after
            var readAfter = SendAtAndGetResponse(BuildAtFrameWithPayload(0x1C, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x00 }));
            Console.WriteLine($"  AFTER I2C GET 0x04: {(readAfter != null ? BitConverter.ToString(readAfter) : "(null)")}");

            // Check if value changed
            var beforeHex = readBefore != null ? BitConverter.ToString(readBefore) : "";
            var afterHex = readAfter != null ? BitConverter.ToString(readAfter) : "";
            Console.WriteLine(beforeHex != afterHex ? "  *** VALUE CHANGED! I2C SET via AT envelope WORKS! ***" : "  Value unchanged - SET may not have been dispatched");

            // Restore: SET I2C 0x04 = 0x00
            var restoreFrame = BuildAtFrameWithPayload(0x1B, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x00 });
            var restoreResp = SendAtAndGetResponse(restoreFrame);
            Console.WriteLine($"  RESTORE response: {(restoreResp != null ? BitConverter.ToString(restoreResp) : "(null)")}");
            await Task.Delay(200);
            var readRestored = SendAtAndGetResponse(BuildAtFrameWithPayload(0x1C, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x00 }));
            Console.WriteLine($"  RESTORED I2C GET 0x04: {(readRestored != null ? BitConverter.ToString(readRestored) : "(null)")}");

            // Test 3: Also check if regular AT 0x35 (InputSource) changed
            var atInputBefore = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, 0x35, "InputSource");
            Console.WriteLine($"\n  UVC AT 0x35 InputSource: {(atInputBefore != null ? BitConverter.ToString(atInputBefore) : "(null)")}");

            return 0;
        }

        if (subCmd == "high-sel")
        {
            return await RunHighSelectorProbeAsync(dev);
        }

        if (subCmd == "topology")
        {
            // Dump full topology with node type GUIDs and test each as a property set
            if (!NativeXuAtCommandProvider.TryGetSupported4kXIds(dev, out var vidT, out var pidT))
            {
                Console.Error.WriteLine("Cannot parse device IDs");
                return 1;
            }
            var ifacesT = GetSelectedKsInterfaces(dev);
            foreach (var ksIfT in ifacesT)
            {
                Console.WriteLine($"\n=== Interface: {ksIfT.Path} ===");
                using var hT = KsExtensionUnitNative.TryOpen(ksIfT.Path, out _);
                if (hT == null) continue;
                if (!KsExtensionUnitNative.TryReadTopologyNodes(hT, out var nsT, out _)) continue;

                foreach (var node in nsT ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>())
                {
                    Console.WriteLine($"\n  Node {node.NodeId}: type={node.NodeType} devSpec={node.IsDevSpecific}");

                    // Try using the node's own type GUID as a property set
                    Console.WriteLine($"    Testing with own GUID as property set:");
                    for (int sel = 1; sel <= 5; sel++)
                    {
                        foreach (int bufSz in new[] { 256, 1024 })
                        {
                            if (KsExtensionUnitNative.TryXuGetDirect(hT, node.NodeId, node.NodeType, sel, bufSz, out var gd, out var gb, out var gw))
                            {
                                var hasData = gd.Take(gb).Any(b => b != 0);
                                Console.WriteLine($"      Sel {sel} ({bufSz}B): OK ({gb}B) {(hasData ? BitConverter.ToString(gd, 0, Math.Min(gb, 16)) + " ***DATA***" : "all-zero")}");
                                break;
                            }
                            else if (gw == 122) // needs bigger buffer
                            {
                                continue;
                            }
                            else
                            {
                                if (bufSz == 256) // only print once per selector
                                    Console.WriteLine($"      Sel {sel}: failed win32={gw}");
                                break;
                            }
                        }
                    }

                    // Also try the XU GUID on this node
                    var xuGuidT = new Guid("961073c7-49f7-44f2-ab42-e940405940c2");
                    if (node.NodeType != xuGuidT)
                    {
                        Console.WriteLine($"    Testing with XU GUID on this node:");
                        for (int sel = 1; sel <= 3; sel++)
                        {
                            if (KsExtensionUnitNative.TryXuGetDirect(hT, node.NodeId, xuGuidT, sel, 256, out var gd2, out var gb2, out var gw2))
                            {
                                var hasData = gd2.Take(gb2).Any(b => b != 0);
                                Console.WriteLine($"      Sel {sel}: OK ({gb2}B) {(hasData ? BitConverter.ToString(gd2, 0, Math.Min(gb2, 16)) : "all-zero")}");
                            }
                            else
                            {
                                Console.WriteLine($"      Sel {sel}: failed win32={gw2}");
                            }
                        }
                    }
                }
            }
            return 0;
        }

        Console.Error.WriteLine("Usage: i2c-cmd get|set|scan|sel-probe|verify|high-sel|topology");
        return 1;
    }
}
